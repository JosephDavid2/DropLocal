param([string]$DotnetRoot='C:\Program Files\dotnet',[string]$WixPath='', [string]$PackageVersion='0.8.2',[switch]$TestPackage,[string]$TestIdentity='',[string]$PayloadPath='')
$ErrorActionPreference='Stop'
$taskRoot=$PSScriptRoot
$work=Join-Path $taskRoot '.installer-work'
$env:DOTNET_CLI_HOME=Join-Path $taskRoot '.dotnet-home'
$env:APPDATA=Join-Path $taskRoot '.build-appdata'
$env:NUGET_PACKAGES=Join-Path $taskRoot '.nuget-packages'
$env:DOTNET_ROLL_FORWARD='LatestMajor'
if(!$WixPath){$WixPath=Join-Path $work 'wix/wix.exe'}
if(!(Test-Path -LiteralPath $WixPath)){throw 'Instale WiX 6.0.2 pelo NuGet oficial: dotnet tool install wix --version 6.0.2 --tool-path .installer-work/wix --allow-roll-forward'}
$session=Join-Path $work ('msi-'+[guid]::NewGuid().ToString('N').Substring(0,8));New-Item -ItemType Directory $session | Out-Null
$runtimeVersion='10.0.12'
if(!$PayloadPath){
    $payload=Join-Path $session 'payload';New-Item -ItemType Directory $payload | Out-Null
    & (Join-Path $DotnetRoot 'dotnet.exe') publish (Join-Path $taskRoot 'windows/DropLocal.csproj') -c Release --self-contained false -o $payload --configfile (Join-Path $taskRoot 'NuGet.Config')
    if($LASTEXITCODE -ne 0){throw 'Publicação Windows falhou'}

    foreach($relative in @('host/fxr','shared/Microsoft.NETCore.App','shared/Microsoft.WindowsDesktop.App')){
        $source=Join-Path $DotnetRoot "$relative/$runtimeVersion";if(!(Test-Path -LiteralPath $source)){throw "Runtime privado ausente: $source"}
        $parent=Join-Path $payload "runtime/$relative";New-Item -ItemType Directory -Path $parent -Force | Out-Null;Copy-Item -LiteralPath $source -Destination (Join-Path $parent $runtimeVersion) -Recurse
    }
    Copy-Item -LiteralPath (Join-Path $DotnetRoot 'dotnet.exe') -Destination (Join-Path $payload 'runtime/dotnet.exe')
    foreach($name in @('LICENSE.txt','ThirdPartyNotices.txt')){Copy-Item -LiteralPath (Join-Path $DotnetRoot $name) -Destination (Join-Path $payload "runtime/$name")}
    Copy-Item -LiteralPath (Join-Path $taskRoot 'USAR-0.8.2.md') -Destination (Join-Path $payload 'LEIA-ME.md')
}else{$payload=[IO.Path]::GetFullPath($PayloadPath)}
$files=@(Get-ChildItem -LiteralPath $payload -File -Recurse | Sort-Object FullName)
if($files.Name -contains 'Uninstall.exe' -or $files.Name -contains 'Uninstall.ps1'){throw 'Payload contém desinstalador personalizado bloqueado'}
$upgrade='77EC6458-5B36-4D5F-AEBF-9ED9D91614E4';$title='Drop Local';$suffix='';$legacy='Software\Microsoft\Windows\CurrentVersion\Uninstall\DropLocal.JosephDavid'
if($TestPackage){if(!$TestIdentity){throw 'Teste exige TestIdentity GUID isolado'};$upgrade=([guid]$TestIdentity).ToString().ToUpperInvariant();$suffix='Test'+$upgrade.Replace('-','').Substring(0,8);$title='Drop Local MSI teste '+$suffix;$legacy='Software\Microsoft\Windows\CurrentVersion\Uninstall\DropLocal.MsiTest.'+$suffix}
function Escape([string]$s){[Security.SecurityElement]::Escape($s)}
function Identifier([string]$prefix,[string]$s){$hash=[Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($s));$prefix+[Convert]::ToHexString($hash).Substring(0,24)}
$product=([guid]::NewGuid()).ToString().ToUpperInvariant()
$xml=[xml]@"
<Wix xmlns="http://wixtoolset.org/schemas/v4/wxs"><Package Name="$(Escape $title)" Manufacturer="Joseph David" Version="$PackageVersion" UpgradeCode="$upgrade" ProductCode="$product" Scope="perUser" Language="1046" Codepage="1252" InstallerVersion="500">
<MajorUpgrade DowngradeErrorMessage="Uma versão mais nova do Drop Local já está instalada." Schedule="afterInstallInitialize"/><MediaTemplate EmbedCab="yes" CompressionLevel="high"/>
<Property Id="ARPCOMMENTS" Value="by Joseph David — transferência pela rede local"/><Property Id="ARPNOMODIFY" Value="1"/><Property Id="ARPURLINFOABOUT" Value="https://github.com/JosephDavid2/DropLocal"/><Icon Id="AppIcon" SourceFile="$(Escape (Join-Path $payload 'DropLocal.ico'))"/><Property Id="ARPPRODUCTICON" Value="AppIcon"/>
<Property Id="INSTALLFOLDER"><RegistrySearch Id="PreviousMsiLocation" Root="HKCU" Key="Software\Joseph David\DropLocal\MSI$suffix" Name="InstallPath" Type="raw"/></Property>
<Property Id="LEGACYINSTALLDIR"><RegistrySearch Id="LegacyLocation" Root="HKCU" Key="$(Escape $legacy)" Name="InstallLocation" Type="raw"/></Property>
<SetDirectory Id="INSTALLFOLDER" Value="[LEGACYINSTALLDIR]" Condition="LEGACYINSTALLDIR AND NOT Installed"/>
<StandardDirectory Id="LocalAppDataFolder"><Directory Id="ProgramsFolder" Name="Programs"><Directory Id="INSTALLFOLDER" Name="DropLocal$suffix"/></Directory></StandardDirectory>
<StandardDirectory Id="ProgramMenuFolder"><Directory Id="AppMenu" Name="$(Escape $title)"/></StandardDirectory>
<Feature Id="App" Title="Drop Local" Level="1"/>
</Package></Wix>
"@
$ns=$xml.DocumentElement.NamespaceURI
function Node([string]$name,[hashtable]$attributes){$n=$xml.CreateElement($name,$ns);foreach($key in $attributes.Keys){$n.SetAttribute($key,[string]$attributes[$key])};$n}
$package=$xml.DocumentElement.FirstChild;$feature=$package.SelectSingleNode('*[local-name()="Feature"]');$install=$package.SelectSingleNode('.//*[local-name()="Directory" and @Id="INSTALLFOLDER"]');$directories=@{''=$install}
foreach($file in $files){
    $relative=$file.FullName.Substring($payload.Length+1).Replace('\','/');$folder=([IO.Path]::GetDirectoryName($relative)).Replace('\','/');$parts=if($folder){$folder.Split('/')}else{@()};$parent=''
    foreach($part in $parts){$path=if($parent){$parent+'/'+$part}else{$part};if(!$directories.ContainsKey($path)){$dir=Node 'Directory' @{Id=(Identifier 'D' $path);Name=$part};$null=$directories[$parent].AppendChild($dir);$directories[$path]=$dir};$parent=$path}
    $id=Identifier 'C' $relative;$component=Node 'Component' @{Id=$id;Guid=([guid]::new([byte[]]([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($upgrade+'/'+$relative))[0..15]))).ToString();Bitness='always64'};$null=$directories[$folder].AppendChild($component)
    $null=$component.AppendChild((Node 'File' @{Id=(Identifier 'F' $relative);Source=$file.FullName;KeyPath='no'}));$null=$component.AppendChild((Node 'RegistryValue' @{Root='HKCU';Key=('Software\Joseph David\DropLocal\MSI'+$suffix+'\Components');Name=$id;Value='1';Type='integer';KeyPath='yes'}));$null=$feature.AppendChild((Node 'ComponentRef' @{Id=$id}))
}
$control=Node 'Component' @{Id='ShellIntegration';Guid='*';Bitness='always64'};$null=$install.AppendChild($control)
$null=$control.AppendChild((Node 'RegistryValue' @{Root='HKCU';Key=('Software\Joseph David\DropLocal\MSI'+$suffix);Name='InstallPath';Value='[INSTALLFOLDER]';Type='string';KeyPath='yes'}))
$null=$control.AppendChild((Node 'Shortcut' @{Id='AppShortcut';Directory='AppMenu';Name='Drop Local';Target='[INSTALLFOLDER]DropLocal.exe';WorkingDirectory='INSTALLFOLDER';Icon='AppIcon';Description='Drop Local — by Joseph David'}))
$null=$control.AppendChild((Node 'Shortcut' @{Id='RemoveShortcut';Directory='AppMenu';Name='Desinstalar Drop Local';Target='[SystemFolder]msiexec.exe';Arguments='/x [ProductCode]';WorkingDirectory='SystemFolder';Icon='AppIcon';Description='Desinstalar pelo Windows Installer'}))
$null=$control.AppendChild((Node 'RemoveFolder' @{Id='RemoveAppMenu';Directory='AppMenu';On='uninstall'}))
$null=$control.AppendChild((Node 'RemoveRegistryKey' @{Root='HKCU';Key=$legacy;Action='removeOnInstall'}))
foreach($name in @('Uninstall.ps1','installed-files.txt','upgrade-backup.txt')){$null=$control.AppendChild((Node 'RemoveFile' @{Id=(Identifier 'Legacy' $name);Name=$name;On='install'}))}
foreach($directory in $directories.Values){$null=$control.AppendChild((Node 'RemoveFolder' @{Id=('Remove'+$directory.GetAttribute('Id'));Directory=$directory.GetAttribute('Id');On='uninstall'}))}
$null=$feature.AppendChild((Node 'ComponentRef' @{Id='ShellIntegration'}))
$source=Join-Path $session 'DropLocal.wxs';$xml.Save($source)
$out=if($TestPackage){Join-Path $session "DropLocal-Test-$PackageVersion.msi"}else{Join-Path $taskRoot "dist/DropLocal-Setup-$PackageVersion.msi"}
& $WixPath build $source -arch x64 -intermediatefolder (Join-Path $session 'wix') -pdbtype none -cc (Join-Path (Split-Path $payload) 'cab-cache') -out $out
if($LASTEXITCODE -ne 0){throw 'WiX falhou'}
$result=[ordered]@{Version=$PackageVersion;ProductCode=$product;UpgradeCode=$upgrade;Test=[bool]$TestPackage;LegacyKey=$legacy;Payload=$payload;Package=$out;Files=$files.Count;CustomUninstaller=$false}
$result | ConvertTo-Json | Set-Content (Join-Path $session 'package.json') -Encoding utf8
$result | ConvertTo-Json | Set-Content (Join-Path $work 'last-msi-package.json') -Encoding utf8
Write-Host "MSI: $out"
Write-Host "Metadados: $(Join-Path $session 'package.json')"