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
if (Test-Path $exe) { throw 'Preflight: non-clean test user profile; refusing to overwrite it' }
Invoke-ExpectedSuccess $installerPath 'install'
if (!(Test-Path $exe) -or !(Test-Path $uninstaller) -or !(Test-Path $registryKey)) {
    throw 'First install omitted executable, uninstaller or registration'
}
Invoke-ExpectedSuccess $installerPath 'repeat-install'
if (!(Test-Path $exe)) { throw 'Repeat install lost product executable' }
Invoke-ExpectedSuccess $uninstaller 'uninstall'
Start-Sleep -Seconds 2
if (Test-Path $exe) { throw 'Executable still present after uninstall' }
if (Test-Path $registryKey) { throw 'Uninstall registry key still present' }
Write-Host 'PASS: clean install / repeat install / uninstall lifecycle (ephemeral runner)'
