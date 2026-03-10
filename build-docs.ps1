$ErrorActionPreference = 'Stop'

Push-Location site
try {
    dotnet tool restore
    dotnet tool run lunet --stacktrace build
}
finally {
    Pop-Location
}
