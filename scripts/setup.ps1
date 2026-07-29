$ErrorActionPreference = "Stop"

function Require-Command($Name, $InstallHint) {
    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        throw "$Name was not found. $InstallHint"
    }
}

Require-Command dotnet "Install the .NET 10 SDK with Windows Desktop support."
Require-Command node "Install Node.js LTS."
Require-Command npm "Install Node.js LTS, which includes npm."

$sdkList = dotnet --list-sdks
if (-not ($sdkList -match "^10\.")) {
    throw ".NET 10 SDK was not found. Installed SDKs:`n$sdkList"
}

Push-Location "$PSScriptRoot\..\src\OpenPanel.Ui"
try {
    npm install
    if ($LASTEXITCODE -ne 0) {
        throw "npm install failed with exit code $LASTEXITCODE."
    }
}
finally {
    Pop-Location
}

dotnet restore "$PSScriptRoot\..\src\OpenPanel.sln"
if ($LASTEXITCODE -ne 0) {
    throw "dotnet restore failed with exit code $LASTEXITCODE."
}
