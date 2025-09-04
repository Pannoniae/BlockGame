# YES, this will publish multiple projects into the same folder.
# THIS IS WHAT WE WANT.
# (there will be a warning, ignore it)
dotnet publish BlockGame.csproj -r "win-x64" -c Release
dotnet publish SNBT2NBT/SNBT2NBT.csproj -r "win-x64" -c Release
dotnet publish NBT2SNBT/NBT2SNBT.csproj -r "win-x64" -c Release
dotnet publish win10fix/win10fix.csproj -r "win-x64" -c Release