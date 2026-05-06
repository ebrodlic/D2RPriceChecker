dotnet publish D2RCompanion.UI -c Release -r win-x64 --self-contained true

vpk pack `
  --packId D2RCompanion `
  --packVersion 0.2.6 `
  --packTitle "D2R Companion" `
  --packDir "D2RCompanion.UI/bin/Release/net10.0-windows/win-x64/publish" `
  --mainExe D2RCompanion.exe `
  --outputDir .\Releases `
  --icon "D2RCompanion.UI/bin/Release/net10.0-windows/win-x64/publish/Resources/1.ico"

##vpk pack -u D2RCompanion -v 0.2.4 -p "D2RCompanion.UI/bin/Release/net10.0-windows/win-x64/publish" -e D2RCompanion.exe

vpk upload github --repoUrl https://github.com/ebrodlic/D2RCompanion --token $env:GITHUB_TOKEN --releaseName v0.2.6


### reference
https://github.com/velopack/velopack/blob/develop/samples/CSharpWpf/build.bat