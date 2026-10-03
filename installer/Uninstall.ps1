param([switch]$VerifyOnly)
$ErrorActionPreference='Stop'
$installRoot=[IO.Path]::GetFullPath($PSScriptRoot)
$manifestPath=Join-Path $installRoot 'installed-files.txt'
$lines=Get-Content -LiteralPath $manifestPath
if($lines.Count -lt 3 -or $lines[0] -ne 'DropLocal.Install/1'){throw 'Instalação não reconhecida. Nenhum arquivo removido.'}
$targets=@(foreach($relative in $lines | Select-Object -Skip 2){
    if([IO.Path]::IsPathRooted($relative) -or $relative.Contains(':')){throw 'Manifesto inválido'}
    $absolute=[IO.Path]::GetFullPath((Join-Path $installRoot $relative))
    if(!$absolute.StartsWith($installRoot+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw 'Manifesto fora da instalação'}
    if(Test-Path -LiteralPath $absolute){$item=Get-Item -LiteralPath $absolute -Force;if($item.LinkType){throw 'Link inesperado na instalação'}}
    $parent=[IO.Path]::GetDirectoryName($absolute)
    while($parent -and ($parent -eq $installRoot -or $parent.StartsWith($installRoot+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase))){if(Test-Path -LiteralPath $parent){if((Get-Item -LiteralPath $parent -Force).LinkType){throw 'Diretório vinculado não pode ser removido'}};$parent=[IO.Path]::GetDirectoryName($parent)}
    $absolute
})
if($VerifyOnly){[pscustomobject]@{Root=$installRoot;Version=$lines[1];Files=$targets.Count;Valid=$true} | ConvertTo-Json;exit 0}
Add-Type -AssemblyName System.Windows.Forms
$answer=[Windows.Forms.MessageBox]::Show("Desinstalar Drop Local by Joseph David?`nOs arquivos recebidos fora da pasta do aplicativo serão preservados.",'Drop Local',[Windows.Forms.MessageBoxButtons]::YesNo,[Windows.Forms.MessageBoxIcon]::Question)
if($answer -ne [Windows.Forms.DialogResult]::Yes){exit 0}
try {
    # First prove the app/runtime is not locked. No forced termination of other apps.
    foreach($probe in $targets){if(Test-Path -LiteralPath $probe){$lock=[IO.File]::Open($probe,[IO.FileMode]::Open,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None);$lock.Dispose()}}
    $registration='HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\DropLocal.JosephDavid'
    if(Test-Path $registration){$registered=(Get-ItemProperty $registration).InstallLocation;if([IO.Path]::GetFullPath($registered) -eq $installRoot){Remove-Item -LiteralPath $registration -ErrorAction Stop}}
    foreach($shortcut in @((Join-Path ([Environment]::GetFolderPath('Programs')) 'Drop Local.lnk'),(Join-Path ([Environment]::GetFolderPath('Desktop')) 'Drop Local.lnk'))){
        if(Test-Path -LiteralPath $shortcut){$shell=New-Object -ComObject WScript.Shell;$link=$shell.CreateShortcut($shortcut);if($link.TargetPath -eq (Join-Path $installRoot 'DropLocal.exe')){Remove-Item -LiteralPath $shortcut}}
    }
    # Delete only enumerated installed files. Never recursively delete unknown/user content.
    foreach($target in $targets){if(Test-Path -LiteralPath $target){Remove-Item -LiteralPath $target -Force -ErrorAction Stop}}
    Remove-Item -LiteralPath $manifestPath -Force
    $directories=@(foreach($target in $targets){$parent=[IO.Path]::GetDirectoryName($target);while($parent -and $parent.StartsWith($installRoot+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){$parent;$parent=[IO.Path]::GetDirectoryName($parent)}}) | Sort-Object -Unique | Sort-Object Length -Descending
    foreach($directory in $directories){if((Test-Path -LiteralPath $directory) -and !(Get-ChildItem -LiteralPath $directory -Force | Select-Object -First 1)){[IO.Directory]::Delete($directory,$false)}}
    if(!(Get-ChildItem -LiteralPath $installRoot -Force | Select-Object -First 1)){[IO.Directory]::Delete($installRoot,$false)}
    [Windows.Forms.MessageBox]::Show('Drop Local removido. Arquivos recebidos preservados.','Drop Local') | Out-Null
}catch{[Windows.Forms.MessageBox]::Show("Não foi possível concluir. Feche Drop Local e tente novamente.`n"+$_.Exception.Message,'Drop Local',[Windows.Forms.MessageBoxButtons]::OK,[Windows.Forms.MessageBoxIcon]::Error) | Out-Null;exit 1}
