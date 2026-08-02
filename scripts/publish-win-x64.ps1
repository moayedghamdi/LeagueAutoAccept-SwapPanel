$ErrorActionPreference = "Stop"

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repositoryRoot "Leauge Auto Accept\Leauge Auto Accept.csproj"
$artifactsPath = Join-Path $repositoryRoot "artifacts"
$publishPath = Join-Path $artifactsPath "win-x64"
$archivePath = Join-Path $artifactsPath "LeagueAutoAccept-win-x64.zip"

if (Test-Path -LiteralPath $publishPath) {
    Remove-Item -LiteralPath $publishPath -Recurse -Force
}

if (Test-Path -LiteralPath $archivePath) {
    Remove-Item -LiteralPath $archivePath -Force
}

New-Item -ItemType Directory -Path $publishPath -Force | Out-Null

dotnet restore $projectPath -r win-x64 --ignore-failed-sources

if ($LASTEXITCODE -ne 0) {
    throw "dotnet restore failed with exit code $LASTEXITCODE."
}

dotnet publish $projectPath `
    -c Release `
    -r win-x64 `
    --self-contained true `
    --no-restore `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:DebugType=None `
    -o $publishPath

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE."
}

Compress-Archive -Path (Join-Path $publishPath "*") -DestinationPath $archivePath
Write-Host "Created $archivePath"
