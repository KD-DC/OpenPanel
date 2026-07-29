$ErrorActionPreference = "Stop"

Push-Location "$PSScriptRoot\..\src\OpenPanel.Ui"
try {
    npm run build
    if ($LASTEXITCODE -ne 0) {
        throw "UI build failed with exit code $LASTEXITCODE."
    }
}
finally {
    Pop-Location
}

dotnet build "$PSScriptRoot\..\src\OpenPanel.sln" -c Release --no-restore
if ($LASTEXITCODE -ne 0) {
    throw ".NET build failed with exit code $LASTEXITCODE."
}
