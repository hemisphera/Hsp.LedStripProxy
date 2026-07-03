$OutFolderRoot = $env:OneDriveConsumer
if (-not $OutFolderRoot) {
    $OutFolderRoot = "$PSScriptRoot\..\_out"
}

$OutFolder = "$OutFolderRoot\Hemisphera\LedProxy"
dotnet publish ./Hsp.LedStripController/Hsp.LedStripController.csproj -c Release -r win-x64 -o "$OutFolder/plugin"
dotnet publish ./Hsp.LedStripEmulator/Hsp.LedStripEmulator.csproj -c Release -o "$OutFolder/emulator"
Get-ChildItem "$OutFolder" -Filter *.pdb -Recurse | Remove-Item