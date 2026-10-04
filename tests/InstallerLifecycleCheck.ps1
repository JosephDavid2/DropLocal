param([Parameter(Mandatory)][string]$Setup,[Parameter(Mandatory)][string]$PreviousSetup)
$ErrorActionPreference='Stop'
$workspace=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$sandbox=Join-Path $workspace ('.installer-work/lc-'+[guid]::NewGuid().ToString('N').Substring(0,8))
New-Item -ItemType Directory $sandbox | Out-Null
function Run-Setup([string]$exe,[string]$mode,[string]$root){$p=Start-Process -FilePath $exe -ArgumentList @($mode,('"'+$root+'"')) -WorkingDirectory $sandbox -WindowStyle Hidden -Wait -PassThru;if($p.ExitCode -ne 0){throw "Setup failed: $mode ($($p.ExitCode))"}}
function Registry-For([string]$root){$sha=[Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($root.ToLowerInvariant()));'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\DropLocal.JosephDavid.Test.'+[Convert]::ToHexString($sha).Substring(0,20)}
function Launch-App([string]$root){$p=Start-Process -FilePath (Join-Path $root 'DropLocal.exe') -WorkingDirectory $sandbox -WindowStyle Hidden -PassThru;for($i=0;$i -lt 50;$i++){Start-Sleep -Milliseconds 100;$children=@(Get-CimInstance Win32_Process -Filter "ParentProcessId=$($p.Id)");if($children.Count -gt 0){Start-Sleep -Milliseconds 500;return $p}};throw 'App did not start'}
function Remove-Test([string]$root,[string]$report){$null=Start-Process -FilePath (Join-Path $root 'Uninstall.exe') -ArgumentList @('--test','--quiet','--report',('"'+$report+'"')) -WorkingDirectory $root -WindowStyle Hidden -PassThru;for($i=0;$i -lt 250 -and !(Test-Path -LiteralPath $report);$i++){Start-Sleep -Milliseconds 100};if(!(Test-Path -LiteralPath $report)){throw 'No uninstall result'};if(!(Get-Content -LiteralPath $report -Raw).StartsWith('PASS')){throw (Get-Content -LiteralPath $report -Raw)}}
$root=Join-Path $sandbox 'DropLocal';$outside=Join-Path $sandbox 'received-outside.bin';$inside=Join-Path $root 'received\personal.bin';$bytes=[byte[]](1,3,5,7,255);[IO.File]::WriteAllBytes($outside,$bytes)
Run-Setup $PreviousSetup '--extract-to' $root
New-Item -ItemType Directory (Split-Path $inside) | Out-Null;[IO.File]::WriteAllBytes($inside,$bytes)
$oldProcess=Launch-App $root
Run-Setup $Setup '--install-test' $root
if(!$oldProcess.WaitForExit(10000)){throw 'Upgrade left previous app running'}
$key=Registry-For $root;$registration=Get-ItemProperty -LiteralPath $key
if($registration.DisplayVersion -ne '0.8.1' -or $registration.UninstallString -notlike '*Uninstall.exe*'){throw 'Incorrect Windows registration'}
if(!(Test-Path (Join-Path $sandbox 'shortcuts/Desinstalar Drop Local.lnk'))){throw 'Missing uninstall shortcut'}
if((Get-ChildItem $sandbox -Directory | Where-Object Name -Like '*.backup-*').Count -ne 0){throw 'Upgrade left backup'}
if((Get-FileHash $inside).Hash -ne (Get-FileHash $outside).Hash){throw 'Upgrade changed personal content'}
$manifest=Join-Path $root 'installed-files.txt';$original=[IO.File]::ReadAllText($manifest);[IO.File]::AppendAllText($manifest,"../received-outside.bin`n")
$denied=Join-Path $sandbox 'invalid.txt';$p=Start-Process (Join-Path $root 'Uninstall.exe') -ArgumentList @('--verify','--quiet','--report',('"'+$denied+'"')) -WindowStyle Hidden -Wait -PassThru
if($p.ExitCode -eq 0 -or !(Test-Path $outside)){throw 'Unsafe manifest accepted'};[IO.File]::WriteAllText($manifest,$original)
$lock=[IO.File]::Open((Join-Path $root 'DropLocal.dll'),[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::None)
try{$report=Join-Path $sandbox 'locked.txt';$null=Start-Process (Join-Path $root 'Uninstall.exe') -ArgumentList @('--test','--quiet','--report',('"'+$report+'"')) -WindowStyle Hidden;for($i=0;$i -lt 150 -and !(Test-Path $report);$i++){Start-Sleep -Milliseconds 100};if(!(Test-Path $report) -or (Get-Content $report -Raw).StartsWith('PASS')){throw 'Locked file was not rejected'};if(!(Test-Path $key) -or !(Test-Path (Join-Path $root 'DropLocal.exe'))){throw 'Lock failure partially removed app'}}finally{$lock.Dispose()}
$app=Launch-App $root;Remove-Test $root (Join-Path $sandbox 'uninstall-personal.txt');if(!$app.WaitForExit(10000)){throw 'Uninstall left app running'}
if(Test-Path $key){throw 'Registration left behind'};if(Test-Path (Join-Path $root 'DropLocal.exe')){throw 'App file left behind'}
if((Get-FileHash $inside).Hash -ne (Get-FileHash $outside).Hash){throw 'Uninstall changed personal content'}
if((Get-ChildItem (Join-Path $sandbox 'shortcuts') -File).Count -ne 0){throw 'Shortcuts left behind'}
$clean=Join-Path $sandbox 'Clean/DropLocal';Run-Setup $Setup '--install-test' $clean;Remove-Test $clean (Join-Path $sandbox 'uninstall-clean.txt');if(Test-Path $clean){throw 'Empty installation directory left behind'}
'PASS upgrade 0.8.0→0.8.1 closes only test app; registration and shortcuts; exact personal bytes preserved; invalid manifest and locks rejected before removal; uninstall closes app and removes owned files; empty directory removed.' | Set-Content (Join-Path $sandbox 'result.txt')
Get-Content (Join-Path $sandbox 'result.txt');'Evidence: '+$sandbox
