$ErrorActionPreference = "Stop"

Push-Location "$PSScriptRoot\..\src\OpenPanel.Ui"
try {
    npm run typecheck
    if ($LASTEXITCODE -ne 0) {
        throw "UI type-check failed with exit code $LASTEXITCODE."
    }
}
finally {
    Pop-Location
}

dotnet test "$PSScriptRoot\..\src\OpenPanel.sln" -c Release --no-restore
if ($LASTEXITCODE -ne 0) {
    throw ".NET tests failed with exit code $LASTEXITCODE."
}
