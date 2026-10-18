# Tarkam Arena — MSIX package for the Microsoft Store (Windows).
#   pwsh packaging/msix/build-msix.ps1 -Version 0.1.0 -PublishDir out/win-x64 -OutDir out/installers
# Needs makeappx.exe from the Windows 10/11 SDK. The package is unsigned; the Store signs it on
# submission. For sideloading, sign it with signtool and a trusted certificate.
param(
    [string]$Version = "0.1.0",
    [string]$PublishDir = "out/win-x64",
    [string]$OutDir = "out/installers"
)
$ErrorActionPreference = "Stop"
$root = Resolve-Path "$PSScriptRoot/../.."
$work = Join-Path ([IO.Path]::GetTempPath()) "tarkam-msix"
Remove-Item $work -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force $work, "$work/Assets", $OutDir | Out-Null
Copy-Item "$PublishDir/*" $work -Recurse
(Get-Content "$PSScriptRoot/AppxManifest.xml") -replace '@VERSION@', $Version | Set-Content "$work/AppxManifest.xml" -Encoding utf8

Add-Type -AssemblyName System.Drawing
$icon = [System.Drawing.Image]::FromFile("$root/assets/ui/icon.png")
foreach ($pair in @(@("StoreLogo.png", 50), @("Square150x150Logo.png", 150), @("Square44x44Logo.png", 44))) {
    $bmp = New-Object System.Drawing.Bitmap $icon, $pair[1], $pair[1]
    $bmp.Save("$work/Assets/$($pair[0])")
    $bmp.Dispose()
}
$icon.Dispose()

$makeappx = Get-ChildItem "${env:ProgramFiles(x86)}\Windows Kits\10\bin" -Recurse -Filter makeappx.exe |
    Where-Object { $_.FullName -match "x64" } | Sort-Object FullName -Descending | Select-Object -First 1
if (-not $makeappx) { throw "makeappx.exe not found; install the Windows SDK." }
& $makeappx.FullName pack /o /d $work /p (Join-Path $OutDir "TarkamArena.msix")
Remove-Item $work -Recurse -Force
