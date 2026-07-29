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

dotnet run --project "$PSScriptRoot\..\src\OpenPanel.Host\OpenPanel.Host.csproj" -c Debug
if ($LASTEXITCODE -ne 0) {
    throw "OpenPanel exited with code $LASTEXITCODE."
}
