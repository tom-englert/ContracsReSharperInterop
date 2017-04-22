(new-object Net.WebClient).DownloadString("https://raw.github.com/tom-englert/BuildScripts/master/BuildScripts.ps1") | iex

Source-SetBuildVersion ContracsReSharperInterop\Properties\AssemblyInfo.cs
Vsix-SetBuildVersion ContracsReSharperInterop.Vsix\source.extension.vsixmanifest