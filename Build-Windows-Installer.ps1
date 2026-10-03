param([string]$DotnetRoot='C:\Program Files\dotnet')
$ErrorActionPreference='Stop'
$taskRoot=$PSScriptRoot
$work=Join-Path $taskRoot '.installer-work'
$publish=Join-Path $work 'app'
$payload=Join-Path $work ('payload-'+[guid]::NewGuid().ToString('N'))
$runtimeVersion='10.0.12'
$env:DOTNET_CLI_HOME=Join-Path $taskRoot '.dotnet-home'
$env:APPDATA=Join-Path $taskRoot '.build-appdata'
$env:NUGET_PACKAGES=Join-Path $taskRoot '.nuget-packages'
& (Join-Path $DotnetRoot 'dotnet.exe') publish (Join-Path $taskRoot 'windows/DropLocal.csproj') -c Release --self-contained false -o $publish --configfile (Join-Path $taskRoot 'NuGet.Config')
if($LASTEXITCODE -ne 0){throw 'Publicação Windows falhou'}
New-Item -ItemType Directory -Force $payload,(Join-Path $payload 'runtime/host/fxr'),(Join-Path $payload 'runtime/shared/Microsoft.NETCore.App'),(Join-Path $payload 'runtime/shared/Microsoft.WindowsDesktop.App') | Out-Null
Copy-Item -Path (Join-Path $publish '*') -Destination $payload -Recurse
Move-Item -LiteralPath (Join-Path $payload 'DropLocal.exe') -Destination (Join-Path $payload 'DropLocal.Host.exe')
Copy-Item -LiteralPath (Join-Path $DotnetRoot 'dotnet.exe') -Destination (Join-Path $payload 'runtime/dotnet.exe')
foreach($name in @('LICENSE.txt','ThirdPartyNotices.txt')){Copy-Item -LiteralPath (Join-Path $DotnetRoot $name) -Destination (Join-Path $payload "runtime/$name")}
foreach($relative in @('host/fxr','shared/Microsoft.NETCore.App','shared/Microsoft.WindowsDesktop.App')){
    $source=Join-Path $DotnetRoot "$relative/$runtimeVersion";if(!(Test-Path -LiteralPath $source)){throw "Runtime privado ausente: $source"}
    Copy-Item -LiteralPath $source -Destination (Join-Path $payload "runtime/$relative/$runtimeVersion") -Recurse
}
Copy-Item -LiteralPath (Join-Path $taskRoot 'installer/Uninstall.ps1') -Destination (Join-Path $payload 'Uninstall.ps1')
Copy-Item -LiteralPath (Join-Path $taskRoot 'USAR-0.5.1.md') -Destination (Join-Path $payload 'LEIA-ME.md')
$framework=Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319'
$compiler=Join-Path $framework 'csc.exe'
if(!(Test-Path -LiteralPath $compiler)){throw 'Compilador .NET Framework local ausente'}
& $compiler /nologo /target:winexe /platform:x64 "/out:$(Join-Path $payload 'DropLocal.exe')" "/win32icon:$(Join-Path $taskRoot 'assets/drop-local.ico')" /reference:System.Windows.Forms.dll (Join-Path $taskRoot 'installer/Launcher.cs')
if($LASTEXITCODE -ne 0){throw 'Launcher falhou'}
$zip=Join-Path $work ('payload-'+[guid]::NewGuid().ToString('N')+'.zip')
Add-Type -AssemblyName System.IO.Compression.FileSystem
[IO.Compression.ZipFile]::CreateFromDirectory($payload,$zip,[IO.Compression.CompressionLevel]::Optimal,$false)
$setup=Join-Path $taskRoot 'dist/DropLocal-Setup-0.5.1.exe'
& $compiler /nologo /target:winexe /platform:x64 "/out:$setup" "/win32icon:$(Join-Path $taskRoot 'assets/drop-local.ico')" "/resource:$zip,DropLocal.Payload" "/resource:$(Join-Path $taskRoot 'assets/drop-local.ico'),DropLocal.Icon" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:Microsoft.CSharp.dll "/reference:$(Join-Path $framework 'System.IO.Compression.dll')" "/reference:$(Join-Path $framework 'System.IO.Compression.FileSystem.dll')" (Join-Path $taskRoot 'installer/Setup.cs')
if($LASTEXITCODE -ne 0){throw 'Instalador falhou'}
Write-Host "Instalador: $setup"
Write-Host "Runtime .NET privado: $runtimeVersion (Windows x64)"
