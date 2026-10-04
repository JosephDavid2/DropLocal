param([string]$BaselineMetadata,[string]$UpgradeMetadata)
$ErrorActionPreference='Stop'
$workspace=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$work=Join-Path $workspace ('.installer-work/msitest-'+[guid]::NewGuid().ToString('N').Substring(0,8));New-Item -ItemType Directory $work | Out-Null
$baseline=Get-Content -LiteralPath $BaselineMetadata -Raw | ConvertFrom-Json;$upgrade=Get-Content -LiteralPath $UpgradeMetadata -Raw | ConvertFrom-Json
if(!$baseline.Test -or !$upgrade.Test -or $baseline.UpgradeCode -ne $upgrade.UpgradeCode){throw 'Isolated MSI identities required'}
$root=Join-Path $work 'DropLocal';New-Item -ItemType Directory (Join-Path $root 'received'),(Join-Path $root 'runtime') -Force | Out-Null
$inside=Join-Path $root 'received/personal.bin';$outside=Join-Path $work 'outside.bin';$unknown=Join-Path $root 'runtime/user-note.txt';[IO.File]::WriteAllBytes($inside,[byte[]](0,1,127,255));Copy-Item -LiteralPath $inside -Destination $outside;[IO.File]::WriteAllText($unknown,'personal content, never package-owned')
$legacyKey='HKCU:\'+$baseline.LegacyKey;New-Item -Path $legacyKey -Force | Out-Null;New-ItemProperty -Path $legacyKey -Name InstallLocation -Value $root | Out-Null
[IO.File]::WriteAllText((Join-Path $root 'Uninstall.ps1'),"throw 'DO NOT EXECUTE THIS LEGACY TEST SCRIPT'");[IO.File]::WriteAllText((Join-Path $root 'installed-files.txt'),'legacy metadata marker')
function Install([string]$msi,[string]$log){$p=Start-Process msiexec.exe -ArgumentList @('/i',('"'+$msi+'"'),'/qn','/norestart','/l*v',('"'+$log+'"')) -WindowStyle Hidden -Wait -PassThru;if($p.ExitCode -ne 0){throw "MSI install returned $($p.ExitCode); see $log"}}
function Registration([string]$code){foreach($hive in @('HKCU','HKLM')){$candidate=$hive+':\Software\Microsoft\Windows\CurrentVersion\Uninstall\{'+$code+'}';if(Test-Path $candidate){return $candidate}};return 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\{'+$code+'}'}
Install $baseline.Package (Join-Path $work 'install.log')
if(!(Test-Path (Join-Path $root 'DropLocal.exe')) -or (Test-Path $legacyKey)){throw 'Legacy path migration or registration failed'}
if((Test-Path (Join-Path $root 'Uninstall.ps1')) -or (Test-Path (Join-Path $root 'installed-files.txt'))){throw 'Obsolete metadata left behind'}
$key=Registration $baseline.ProductCode;if((Get-ItemProperty $key).WindowsInstaller -ne 1){throw 'Not a Windows Installer registration'}
$app=Start-Process (Join-Path $root 'DropLocal.exe') -WindowStyle Hidden -PassThru;Start-Sleep -Seconds 2
Install $upgrade.Package (Join-Path $work 'upgrade.log')
if(!$app.WaitForExit(10000)){throw 'Restart Manager left previous test app running'}
if(Test-Path $key){throw 'Old MSI registration remained'}
$key=Registration $upgrade.ProductCode;if((Get-ItemProperty $key).DisplayVersion -ne $upgrade.Version){throw 'New MSI registration missing'}
if((Get-FileHash $inside).Hash -ne (Get-FileHash $outside).Hash -or [IO.File]::ReadAllText($unknown) -ne 'personal content, never package-owned'){throw 'Upgrade changed personal content'}
$programs=[Environment]::GetFolderPath('Programs');$menu=Join-Path $programs ('Drop Local MSI teste Test'+$upgrade.UpgradeCode.Replace('-','').Substring(0,8))
if(!(Test-Path (Join-Path $menu 'Desinstalar Drop Local.lnk'))){throw 'Uninstall shortcut missing'}
$shell=New-Object -ComObject WScript.Shell;$shortcut=$shell.CreateShortcut((Join-Path $menu 'Desinstalar Drop Local.lnk'));if($shortcut.TargetPath -notlike '*msiexec.exe' -or $shortcut.Arguments -notlike ('*'+$upgrade.ProductCode+'*')){throw 'Uninstall shortcut is not native MSI'}
$app=Start-Process (Join-Path $root 'DropLocal.exe') -WindowStyle Hidden -PassThru;Start-Sleep -Seconds 2
$log=Join-Path $work 'uninstall.log';$p=Start-Process msiexec.exe -ArgumentList @('/x',('{'+$upgrade.ProductCode+'}'),'/qn','/norestart','/l*v',('"'+$log+'"')) -WindowStyle Hidden -Wait -PassThru;if($p.ExitCode -ne 0){throw "MSI uninstall returned $($p.ExitCode)"}
if(!$app.WaitForExit(10000)){throw 'Uninstall left test app running'}
if((Test-Path $key) -or (Test-Path $menu)){throw 'MSI registration or shortcuts remained'};if(@(Get-Process DropLocal -ErrorAction SilentlyContinue | Where-Object {$_.Path -eq (Join-Path $root 'DropLocal.exe')}).Count -gt 0){throw 'Another test apphost remained'}
if((Get-FileHash $inside).Hash -ne (Get-FileHash $outside).Hash -or [IO.File]::ReadAllText($unknown) -ne 'personal content, never package-owned'){throw 'Uninstall changed personal content'}
$owned=@(Get-ChildItem -LiteralPath $upgrade.Payload -File -Recurse | ForEach-Object {$_.FullName.Substring($upgrade.Payload.Length+1)})
foreach($relative in $owned){if(Test-Path -LiteralPath (Join-Path $root $relative)){throw "Owned file remained: $relative"}}
$result=[ordered]@{Result='PASS';LegacyMigratedWithoutRunningOldScript=$true;NativeWindowsInstaller=$true;UpgradeClosesOnlyTestApp=$true;UninstallClosesTestApp=$true;PersonalFilesPreserved=$true;OwnedFilesRemoved=$owned.Count;RegistryAndShortcutsRemoved=$true;Evidence=$work}
$result | ConvertTo-Json | Set-Content (Join-Path $work 'result.json');$result | ConvertTo-Json
