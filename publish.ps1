# Publish client and server separately, then merge

# restore once for all projects
Write-Host "Restoring dependencies..." -ForegroundColor Cyan
dotnet restore BlockGame.slnx -r win-x64

# delete publish folders
Remove-Item -Recurse -Force .\publish\ -ErrorAction SilentlyContinue
Remove-Item -Recurse -Force .\publishs\ -ErrorAction SilentlyContinue

# publish client to publish/
dotnet publish src/launch/Launcher.csproj -r "win-x64" -c Release --sc --no-restore

# publish server to publishs/
dotnet publish src/launchsv/LauncherServer.csproj -r "win-x64" -c Release --sc --no-restore

# copy server exe and dll to main publish folder
Copy-Item -Force .\publishs\server.exe .\publish\
Copy-Item -Force .\publishs\server.dll .\publish\
Copy-Item -Force .\publishs\server.deps.json .\publish\
Copy-Item -Force .\publishs\server.pdb .\publish\
Copy-Item -Force .\publishs\server.runtimeconfig.json .\publish\

# publish tools to publisht/, NetBeauty refuses to beautify several SCD apps into the same dir
dotnet publish SNBT2NBT/SNBT2NBT.csproj -r "win-x64" -c Release --sc --no-restore
dotnet publish NBT2SNBT/NBT2SNBT.csproj -r "win-x64" -c Release --sc --no-restore
dotnet publish win10fix/win10fix.csproj -r "win-x64" -c Release --sc --no-restore

# tools are self-contained too, but their runtime files are identical to the client's in libs/, so only copy the tools' own files
foreach ($t in "snbt2nbt", "nbt2snbt", "win10fix") {
    Copy-Item -Force ".\publisht\$t\$t.exe" .\publish\
    Copy-Item -Force ".\publisht\$t\libs\$t.dll", ".\publisht\$t\libs\$t.pdb", ".\publisht\$t\libs\$t.deps.json", ".\publisht\$t\libs\$t.runtimeconfig.json" .\publish\libs\
}
# the tools' app dir is libs/, so their host has to live there too
Copy-Item -Force .\publisht\snbt2nbt\libs\hostfxr.dll, .\publisht\snbt2nbt\libs\hostpolicy.dll .\publish\libs\

# cleanup
Remove-Item -Recurse -Force .\publishs\
Remove-Item -Recurse -Force .\publisht\