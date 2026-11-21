param([Parameter(Mandatory=$true)][string]$Installer)
$ErrorActionPreference = 'Stop'
$installerPath = (Resolve-Path $Installer).Path
$appDir = Join-Path $env:LOCALAPPDATA 'Programs\AuralObservatory'
$exe = Join-Path $appDir 'AuralObservatory.SearchDesktop.exe'
$uninstaller = Join-Path $appDir 'Uninstall.exe'
$registryKey = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\AuralObservatory'
function Invoke-ExpectedSuccess([string]$Command, [string]$Phase) {
    # NSIS is a GUI-subsystem process. PowerShell's direct-call LASTEXITCODE can be null;
    # Start-Process -Wait captures the actual process exit code without a false failure.
    $proc = Start-Process -FilePath $Command -ArgumentList '/S' -PassThru -Wait
    if ($proc.ExitCode -ne 0) { throw "$Phase exited $($proc.ExitCode)" }
}
function Test-InstalledDesktopSession([string]$Executable) {
    $stdout = Join-Path $env:TEMP ("aural-session-" + [guid]::NewGuid().ToString('N') + ".stdout.log")
    $stderr = $stdout.Replace(".stdout.log", ".stderr.log")
    $process = $null
    try {
        $process = Start-Process -FilePath $Executable -ArgumentList '--no-browser' -PassThru -RedirectStandardOutput $stdout -RedirectStandardError $stderr
        $url = $null
        for ($i=0; $i -lt 45; $i++) {
            if ($process.HasExited) { throw "Installed app exited before startup with code $($process.ExitCode)" }
            if (Test-Path $stdout) {
                $printed = [IO.File]::ReadAllText($stdout)
                $match = [regex]::Match($printed, 'Aural Observatory local session:\s+(http://127\.0\.0\.1:\d+)')
                if ($match.Success) { $url = $match.Groups[1].Value; break }
            }
            Start-Sleep -Milliseconds 500
        }
        if (!$url) { throw "Installed app did not report a local HTTP endpoint" }
        $root = Invoke-WebRequest -Uri $url -UseBasicParsing -TimeoutSec 15
        if ($root.StatusCode -ne 200 -or !$root.Content.Contains('id="quiet"')) {
            throw "Installed app did not serve expected interactive UI"
        }
        $tokenMatch = [regex]::Match($root.Content, "const token = '([A-Fa-f0-9]{64})';")
        if (!$tokenMatch.Success) { throw "Session token not found in the locally served UI" }
        $headers = @{ 'X-Aural-Token' = $tokenMatch.Groups[1].Value }
        # Token never appears in CI logs or uploaded artifacts.
        $response = Invoke-RestMethod -Method Post -Uri ($url + '/api/exit') -Headers $headers -ContentType 'application/json' -Body '{}' -TimeoutSec 15
        if (!$response.stopping) { throw 'Local desktop shutdown action was not accepted' }
        if (!$process.WaitForExit(15000)) { throw 'Installed desktop process did not stop after requested exit' }
        if ($process.ExitCode -ne 0) { throw "Desktop exited with $($process.ExitCode)" }
        Write-Host 'PASS: installed desktop served its real local UI, authenticated exit and released the host'
    } finally {
        if ($process -and !$process.HasExited) { Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue }
        Remove-Item $stdout, $stderr -Force -ErrorAction SilentlyContinue
    }
}
if (Test-Path $exe) { throw 'Preflight: non-clean test user profile; refusing to overwrite it' }
Invoke-ExpectedSuccess $installerPath 'install'
if (!(Test-Path $exe) -or !(Test-Path $uninstaller) -or !(Test-Path $registryKey)) {
    throw 'First install omitted executable, uninstaller or registration'
}
Test-InstalledDesktopSession $exe
Invoke-ExpectedSuccess $installerPath 'repeat-install'
if (!(Test-Path $exe)) { throw 'Repeat install lost product executable' }
Test-InstalledDesktopSession $exe  # Reopen proves stale lock filename cannot block a new session.
Invoke-ExpectedSuccess $uninstaller 'uninstall'
Start-Sleep -Seconds 2
if (Test-Path $exe) { throw 'Executable still present after uninstall' }
if (Test-Path $registryKey) { throw 'Uninstall registry key still present' }
Write-Host 'PASS: clean install / repeat install / uninstall lifecycle (ephemeral runner)'
