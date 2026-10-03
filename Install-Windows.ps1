param([string]$Source = '')
$ErrorActionPreference = 'Stop'
if (!$Source) {
    $Source = if (Test-Path -LiteralPath (Join-Path $PSScriptRoot 'DropLocal.exe')) { $PSScriptRoot } else { Join-Path $PSScriptRoot 'dist\windows-unified' }
}
if (!(Test-Path -LiteralPath (Join-Path $Source 'DropLocal.exe'))) { throw 'Compile/publique o programa antes de instalar. Consulte README.md.' }
$destination = Join-Path $env:LOCALAPPDATA 'Programs\DropLocal'
New-Item -ItemType Directory -Force -Path $destination | Out-Null
Copy-Item -Path (Join-Path $Source '*') -Destination $destination -Recurse -Force
$shell = New-Object -ComObject WScript.Shell
$shortcut = $shell.CreateShortcut((Join-Path ([Environment]::GetFolderPath('Programs')) 'Drop Local.lnk'))
$shortcut.TargetPath = Join-Path $destination 'DropLocal.exe'
$shortcut.WorkingDirectory = $destination
$shortcut.IconLocation = (Join-Path $destination 'DropLocal.exe') + ',0'
$shortcut.Save()
Write-Host "Instalado em $destination. Abra Drop Local pelo menu Iniciar. Requer .NET Desktop Runtime 10."

