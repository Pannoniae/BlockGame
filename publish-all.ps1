# Publish for both Windows and Linux

$startTime = Get-Date

function Build-Platform {
    param(
        [string]$runtime,
        [string]$outputDir,
        [bool]$selfContained
    )

    Write-Host "Restoring for $runtime..." -ForegroundColor Cyan
    dotnet restore BlockGame.slnx -r $runtime

    Write-Host "Building for $runtime..." -ForegroundColor Cyan

    # delete publish folders
    Remove-Item -Recurse -Force .\publish\ -ErrorAction SilentlyContinue
    Remove-Item -Recurse -Force .\publishs\ -ErrorAction SilentlyContinue
    Remove-Item -Recurse -Force ".\$outputDir" -ErrorAction SilentlyContinue

    $scFlag = if ($selfContained) { "--sc" } else { "" }
    $exe = if ($runtime -like "win-*") { ".exe" } else { "" }

    # publish client to publish/
    dotnet publish -bl src/launch/Launcher.csproj -r $runtime -c Release $scFlag --no-restore

    # publish server to publishs/
    dotnet publish -bl src/launchsv/LauncherServer.csproj -r $runtime -c Release $scFlag --no-restore

    # copy server exe and dll to main publish folder
    Copy-Item -Force ".\publishs\server$exe" .\publish\
    Copy-Item -Force .\publishs\server.dll .\publish\
    Copy-Item -Force .\publishs\server.deps.json .\publish\
    Copy-Item -Force .\publishs\server.pdb .\publish\
    Copy-Item -Force .\publishs\server.runtimeconfig.json .\publish\

    # copy server dll (srv.dll) to client publish folder
    Copy-Item -Force .\publishs\libs\srv.dll .\publish\libs\
    Copy-Item -Force .\publishs\libs\srv.pdb .\publish\libs\

    # publish tools (they go to publish/ root via NetBeauty config)
    dotnet publish SNBT2NBT/SNBT2NBT.csproj -r $runtime -c Release --no-restore
    dotnet publish NBT2SNBT/NBT2SNBT.csproj -r $runtime -c Release --no-restore
    dotnet publish win10fix/win10fix.csproj -r $runtime -c Release --no-restore

    # cleanup
    Remove-Item -Recurse -Force .\publishs\

    # rename publish to final output dir
    Rename-Item .\publish\ $outputDir

    Write-Host "Completed $runtime build to .\$outputDir\" -ForegroundColor Green

    # compress to 7z
    $archiveName = "$outputDir.7z"
    Remove-Item -Force ".\$archiveName" -ErrorAction SilentlyContinue
    Write-Host "Compressing to $archiveName..." -ForegroundColor Cyan
    & 7z a -t7z -m0=lzma2 -mx3 -mmt=on ".\$archiveName" ".\$outputDir\*"
    Write-Host "Created $archiveName" -ForegroundColor Green
}

# Extract version from Constants.cs omg this is such a stupid hack but im lazy
$constantsFile = ".\src\util\Constants.cs"
$versionLine = Select-String -Path $constantsFile -Pattern 'private const string _ver = "(.+)"'
if ($versionLine -and $versionLine.Matches.Groups[1].Success) {
    $rawVersion = $versionLine.Matches.Groups[1].Value
    # Extract just the version number (e.g., "BlockGame v0.0.3_01" -> "0.0.3_01")
    $version = $rawVersion -replace "^BlockGame v", ""
    Write-Host "Detected version: $version" -ForegroundColor Cyan
} else {
    Write-Host "Warning: Could not extract version from Constants.cs, using 'unknown'" -ForegroundColor Yellow
    $version = "unknown"
}

# Build both platforms
Build-Platform -runtime "win-x64" -outputDir "BlockGame-win-$version" -selfContained $false
Build-Platform -runtime "linux-x64" -outputDir "BlockGame-linux-$version" -selfContained $true

$endTime = Get-Date
$duration = $endTime - $startTime

Write-Host "`nAll builds complete!" -ForegroundColor Green
Write-Host "Windows: .\BlockGame-win-$version\ (.\BlockGame-win-$version.7z)" -ForegroundColor Yellow
Write-Host "Linux:   .\BlockGame-linux-$version\ (.\BlockGame-linux-$version.7z)" -ForegroundColor Yellow
Write-Host "`nTotal time: $($duration.ToString('mm\:ss'))" -ForegroundColor Cyan