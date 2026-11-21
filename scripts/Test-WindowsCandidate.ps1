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
    $process = $null
    try {
        # Read the live child's stdout via a pipe. Redirecting to a file and reading
        # concurrently is unreliable on Windows because the writer can deny sharing.
        $start = New-Object System.Diagnostics.ProcessStartInfo
        $start.FileName = $Executable
        $start.Arguments = '--no-browser'
        $start.UseShellExecute = $false
        $start.CreateNoWindow = $true
        $start.RedirectStandardOutput = $true
        $start.RedirectStandardError = $true
        $process = New-Object System.Diagnostics.Process
        $process.StartInfo = $start
        if (!$process.Start()) { throw 'Installed executable process did not start' }
        $deadline = [DateTime]::UtcNow.AddSeconds(40)
        $url = $null
        while ([DateTime]::UtcNow -lt $deadline) {
            $remaining = [Math]::Max(1, [int]($deadline - [DateTime]::UtcNow).TotalMilliseconds)
            $lineTask = $process.StandardOutput.ReadLineAsync()
            if (!$lineTask.Wait($remaining)) { throw 'Timed out waiting for actual loopback listener' }
            $line = $lineTask.Result
            if ($null -eq $line) { break }
            $match = [regex]::Match($line, 'Aural Observatory local session:\s+(http://127\.0\.0\.1:\d+)')
            if ($match.Success) { $url = $match.Groups[1].Value; break }
        }
        if (!$url) { throw "Installed app did not publish its local URL (exit=$($process.ExitCode))" }
        $root = Invoke-WebRequest -Uri $url -UseBasicParsing -TimeoutSec 15
        if ($root.StatusCode -ne 200 -or !$root.Content.Contains('id="quiet"')) {
            throw 'Installed app did not serve the expected interactive HTML'
        }
        $tokenMatch = [regex]::Match($root.Content, "const token = '([A-Fa-f0-9]{64})';")
        if (!$tokenMatch.Success) { throw 'Token missing from local browser session' }
        $headers = @{ 'X-Aural-Token' = $tokenMatch.Groups[1].Value }
        # Never write the ephemeral token to a log or artifact.
        $response = Invoke-RestMethod -Method Post -Uri ($url + '/api/exit') -Headers $headers -ContentType 'application/json' -Body '{}' -TimeoutSec 15
        if (!$response.stopping) { throw 'Authenticated application-exit action rejected' }
        if (!$process.WaitForExit(15000)) { throw 'Installed desktop did not terminate after exit' }
        if ($process.ExitCode -ne 0) { throw "Installed desktop terminated abnormally ($($process.ExitCode))" }
        Write-Host 'PASS: installed desktop started real local UI and exited after token-authorized request'
    }
    finally {
        if ($process) {
            if (!$process.HasExited) { $process.Kill(); [void]$process.WaitForExit(5000) }
            $process.Dispose()
        }
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
