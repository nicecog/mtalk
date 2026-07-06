# M-Social device log capture (USB debugging)
param(
    [string]$Package = "com.Spike.MSocial",
    [string]$OutDir = (Join-Path (Split-Path $PSScriptRoot -Parent) "Build\Android")
)

$adb = Get-ChildItem "C:\Program Files\Unity\Hub\Editor\6000.3.13f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe" -ErrorAction SilentlyContinue
if (-not $adb) {
    Write-Error "adb not found. Enable USB debugging and install Unity Android SDK."
    exit 1
}

$ts = Get-Date -Format "yyyyMMdd-HHmmss"
$logcat = Join-Path $OutDir "logcat-msocial-$ts.txt"

Write-Host "Devices:" -ForegroundColor Cyan
& $adb.FullName devices

Write-Host "Clearing logcat..." -ForegroundColor Yellow
& $adb.FullName logcat -c

Write-Host "Launch app, reproduce crash, then press Enter to capture logs..."
Read-Host

& $adb.FullName logcat -d -s Unity,AndroidRuntime,DEBUG,ActivityManager | Out-File -FilePath $logcat -Encoding utf8
Write-Host "Saved: $logcat" -ForegroundColor Green

$startup = & $adb.FullName shell "run-as $Package cat files/startup.log" 2>&1
if ($startup -match "BeforeSceneLoad") {
    $startupPath = Join-Path $OutDir "startup-log-$ts.txt"
    $startup | Out-File -FilePath $startupPath -Encoding utf8
    Write-Host "Startup log: $startupPath" -ForegroundColor Green
}
else {
    Write-Host "startup.log not available (app may have crashed before write or run-as denied)" -ForegroundColor Yellow
}
