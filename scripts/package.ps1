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

dotnet publish "$PSScriptRoot\..\src\OpenPanel.Host\OpenPanel.Host.csproj" `
    -c Release `
    -r win-x64 `
    --self-contained false `
    -o "$PSScriptRoot\..\publish\win-x64"
if ($LASTEXITCODE -ne 0) {
    throw ".NET publish failed with exit code $LASTEXITCODE."
}
