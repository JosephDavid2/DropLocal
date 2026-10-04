param([string]$Package = '')
$ErrorActionPreference='Stop'
if(!$Package){$Package=Join-Path $PSScriptRoot 'dist/DropLocal-Setup-0.8.2.msi'}
if(!(Test-Path -LiteralPath $Package -PathType Leaf)){throw 'Baixe o MSI atual da release ou execute Build-Windows-Installer.ps1.'}
Start-Process -FilePath msiexec.exe -ArgumentList @('/i',('"'+[IO.Path]::GetFullPath($Package)+'"')) -Wait
