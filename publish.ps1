# YES, this will publish multiple projects into the same folder.
# THIS IS WHAT WE WANT.
# (there will be a warning, ignore it)
dotnet publish BlockGame.csproj --output ./publish -r "win-x64" -c Release
dotnet publish NBTTool/NBTTool.csproj --output ./publish -r "win-x64" -c Release