$ErrorActionPreference = "Stop"

$Version = "2.5.1"
$ExpectedSha256 = "9bec3083069f58f911e6a512f4806db51a27bd096103087bc1d05ef54c80a191"
$DestinationDirectory = Join-Path $PSScriptRoot "..\tools\PresentMon"
$Destination = Join-Path $DestinationDirectory "PresentMon.exe"
$DownloadUrl = "https://github.com/GameTechDev/PresentMon/releases/download/v$Version/PresentMon-$Version-x64.exe"

New-Item -ItemType Directory -Force -Path $DestinationDirectory | Out-Null

if (Test-Path $Destination) {
    $CurrentHash = (Get-FileHash -Algorithm SHA256 $Destination).Hash.ToLowerInvariant()
    if ($CurrentHash -eq $ExpectedSha256) {
        Write-Host "PresentMon $Version is already installed."
        return
    }
}

$TemporaryFile = Join-Path ([System.IO.Path]::GetTempPath()) "OpenPanel-PresentMon-$Version.exe"
try {
    Invoke-WebRequest -UseBasicParsing -Uri $DownloadUrl -OutFile $TemporaryFile
    $DownloadedHash = (Get-FileHash -Algorithm SHA256 $TemporaryFile).Hash.ToLowerInvariant()
    if ($DownloadedHash -ne $ExpectedSha256) {
        throw "PresentMon checksum mismatch. Expected $ExpectedSha256, received $DownloadedHash."
    }

    Move-Item -Force -Path $TemporaryFile -Destination $Destination
    Write-Host "Installed PresentMon $Version at $Destination."
}
finally {
    Remove-Item -Force -ErrorAction SilentlyContinue $TemporaryFile
}
