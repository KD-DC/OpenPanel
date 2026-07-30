[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^\d+\.\d+\.\d+(?:-[0-9A-Za-z.-]+)?$')]
    [string] $Version,

    [string] $InnoSetupCompiler = $env:ISCC_PATH
)

$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$uiRoot = Join-Path $repoRoot "src\OpenPanel.Ui"
$hostProject = Join-Path $repoRoot "src\OpenPanel.Host\OpenPanel.Host.csproj"
$publishRoot = Join-Path $repoRoot "publish\win-x64"
$artifactRoot = Join-Path $repoRoot "artifacts"
$installerScript = Join-Path $repoRoot "installer\OpenPanel.iss"
$numericVersion = ($Version -split "-", 2)[0]

if (-not $InnoSetupCompiler) {
    $compilerCandidates = @(
        "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe",
        "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
        "$env:ProgramFiles\Inno Setup 6\ISCC.exe"
    )
    $InnoSetupCompiler = $compilerCandidates |
        Where-Object { $_ -and (Test-Path -LiteralPath $_) } |
        Select-Object -First 1
}

if (-not $InnoSetupCompiler -or -not (Test-Path -LiteralPath $InnoSetupCompiler)) {
    throw "Inno Setup 6 compiler not found. Install Inno Setup 6.7.3 or set ISCC_PATH."
}

foreach ($directory in @($publishRoot, $artifactRoot)) {
    $fullDirectory = [System.IO.Path]::GetFullPath($directory)
    $repoPrefix = $repoRoot.TrimEnd(
        [System.IO.Path]::DirectorySeparatorChar,
        [System.IO.Path]::AltDirectorySeparatorChar) +
        [System.IO.Path]::DirectorySeparatorChar
    if (-not $fullDirectory.StartsWith(
            $repoPrefix,
            [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to clean packaging output outside the repository: $directory"
    }

    if (Test-Path -LiteralPath $directory) {
        Remove-Item -LiteralPath $directory -Recurse -Force
    }
    New-Item -ItemType Directory -Path $directory | Out-Null
}

Push-Location $uiRoot
try {
    npm.cmd run build
    if ($LASTEXITCODE -ne 0) {
        throw "UI build failed with exit code $LASTEXITCODE."
    }
}
finally {
    Pop-Location
}

dotnet publish $hostProject `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:Version=$Version `
    -p:FileVersion="$numericVersion.0" `
    -p:AssemblyVersion="$numericVersion.0" `
    -p:DebugType=None `
    -p:DebugSymbols=false `
    -o $publishRoot
if ($LASTEXITCODE -ne 0) {
    throw ".NET publish failed with exit code $LASTEXITCODE."
}

& $InnoSetupCompiler `
    "/DAppVersion=$Version" `
    "/DPublishDir=$publishRoot" `
    "/DOutputDir=$artifactRoot" `
    $installerScript
if ($LASTEXITCODE -ne 0) {
    throw "Inno Setup failed with exit code $LASTEXITCODE."
}

$installerName = "OpenPanel-Setup-$Version.exe"
$installerPath = Join-Path $artifactRoot $installerName
$checksumPath = "$installerPath.sha256"
$hash = (Get-FileHash -LiteralPath $installerPath -Algorithm SHA256).Hash.ToLowerInvariant()
Set-Content -LiteralPath $checksumPath -Value "$hash  $installerName" -Encoding ascii

Write-Host "Installer: $installerPath"
Write-Host "SHA256:   $hash"
