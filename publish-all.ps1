# Publish for both Windows and Linux

function Build-Platform {
    param(
        [string]$runtime,
        [string]$outputDir,
        [bool]$selfContained
    )

    Write-Host "Building for $runtime..." -ForegroundColor Cyan

    # delete publish folders
    Remove-Item -Recurse -Force .\publish\ -ErrorAction SilentlyContinue
    Remove-Item -Recurse -Force .\publishs\ -ErrorAction SilentlyContinue
    Remove-Item -Recurse -Force ".\$outputDir" -ErrorAction SilentlyContinue

    $scFlag = if ($selfContained) { "--sc" } else { "" }
    $exe = if ($runtime -like "win-*") { ".exe" } else { "" }

    # publish client to publish/
    dotnet publish src/launch/Launcher.csproj -r $runtime -c Release $scFlag

    # publish server to publishs/
    dotnet publish src/launchsv/LauncherServer.csproj -r $runtime -c Release $scFlag

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
    dotnet publish SNBT2NBT/SNBT2NBT.csproj -r $runtime -c Release
    dotnet publish NBT2SNBT/NBT2SNBT.csproj -r $runtime -c Release
    dotnet publish win10fix/win10fix.csproj -r $runtime -c Release

    # cleanup
    Remove-Item -Recurse -Force .\publishs\

    # rename publish to final output dir
    Rename-Item .\publish\ $outputDir

    Write-Host "Completed $runtime build to .\$outputDir\" -ForegroundColor Green

    # compress to 7z
    $archiveName = "$outputDir.7z"
    Remove-Item -Force ".\$archiveName" -ErrorAction SilentlyContinue
    Write-Host "Compressing to $archiveName..." -ForegroundColor Cyan
    & 7z a -t7z -mx9 ".\$archiveName" ".\$outputDir\*"
    Write-Host "Created $archiveName" -ForegroundColor Green
}

# Build both platforms
Build-Platform -runtime "win-x64" -outputDir "BlockGame-win" -selfContained $false
Build-Platform -runtime "linux-x64" -outputDir "BlockGame-linux" -selfContained $true

Write-Host "`nAll builds complete!" -ForegroundColor Green
Write-Host "Windows: .\publish-win\ (.\BlockGame-win.7z)" -ForegroundColor Yellow
Write-Host "Linux:   .\publish-linux\ (.\BlockGame-linux.7z)" -ForegroundColor Yellow