# Publish client and server separately, then merge

# delete publish folders
Remove-Item -Recurse -Force .\publish\ -ErrorAction SilentlyContinue
Remove-Item -Recurse -Force .\publishs\ -ErrorAction SilentlyContinue

# publish client to publish/
dotnet publish src/launch/Launcher.csproj -r "linux-x64" -c Release

# publish server to publishs/
dotnet publish src/launchsv/LauncherServer.csproj -r "linux-x64" -c Release

# copy server exe and dll to main publish folder
Copy-Item -Force .\publishs\server.exe .\publish\
Copy-Item -Force .\publishs\server.dll .\publish\
Copy-Item -Force .\publishs\server.deps.json .\publish\
Copy-Item -Force .\publishs\server.pdb .\publish\
Copy-Item -Force .\publishs\server.runtimeconfig.json .\publish\

# copy server dll (srv.dll) to client publish folder
Copy-Item -Force .\publishs\libs\srv.dll .\publish\libs\
Copy-Item -Force .\publishs\libs\srv.pdb .\publish\libs\

# publish tools
dotnet publish SNBT2NBT/SNBT2NBT.csproj -r "linux-x64" -c Release
dotnet publish NBT2SNBT/NBT2SNBT.csproj -r "linux-x64" -c Release
dotnet publish win10fix/win10fix.csproj -r "linux-x64" -c Release

# cleanup
Remove-Item -Recurse -Force .\publishs\