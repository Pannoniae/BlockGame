# Publish client and server separately, then merge

# restore once for all projects
Write-Host "Restoring dependencies..." -ForegroundColor Cyan
dotnet restore BlockGame.slnx -r win-x64

# delete publish folders
Remove-Item -Recurse -Force .\publish\ -ErrorAction SilentlyContinue
Remove-Item -Recurse -Force .\publishs\ -ErrorAction SilentlyContinue

# publish client to publish/
dotnet publish src/launch/Launcher.csproj -r "win-x64" -c Release --no-restore

# publish server to publishs/
dotnet publish src/launchsv/LauncherServer.csproj -r "win-x64" -c Release --no-restore

# copy server exe and dll to main publish folder
Copy-Item -Force .\publishs\server.exe .\publish\
Copy-Item -Force .\publishs\server.dll .\publish\
Copy-Item -Force .\publishs\server.deps.json .\publish\
Copy-Item -Force .\publishs\server.pdb .\publish\
Copy-Item -Force .\publishs\server.runtimeconfig.json .\publish\

# publish tools
dotnet publish SNBT2NBT/SNBT2NBT.csproj -r "win-x64" -c Release --no-restore
dotnet publish NBT2SNBT/NBT2SNBT.csproj -r "win-x64" -c Release --no-restore
dotnet publish win10fix/win10fix.csproj -r "win-x64" -c Release --no-restore

# cleanup
Remove-Item -Recurse -Force .\publishs\