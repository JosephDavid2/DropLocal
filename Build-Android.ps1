param([switch]$Offline, [string]$CacheRoot = '', [string]$BuildRoot = '')
$ErrorActionPreference = 'Stop'
$taskRoot = $PSScriptRoot
$pathConfig = Join-Path $taskRoot '.android-tools/build-paths.json'
if (!$CacheRoot -and !$BuildRoot -and (Test-Path -LiteralPath $pathConfig)) {
    $savedPaths = Get-Content -LiteralPath $pathConfig -Raw | ConvertFrom-Json
    $CacheRoot = $savedPaths.CacheRoot
    $BuildRoot = $savedPaths.BuildRoot
}
$jdk = Get-ChildItem -LiteralPath (Join-Path $taskRoot '.android-tools/java') -Directory | Select-Object -First 1
$gradle = Join-Path $taskRoot '.android-tools/gradle/gradle-8.11.1/bin/gradle.bat'
if (!$jdk -or !(Test-Path -LiteralPath $gradle)) { throw 'Ferramentas locais ausentes. Consulte README.md.' }
$env:JAVA_HOME = $jdk.FullName
$env:ANDROID_HOME = Join-Path $taskRoot '.android-sdk'
$env:ANDROID_USER_HOME = Join-Path $taskRoot '.android-user'
$env:GRADLE_USER_HOME = if ($CacheRoot) { [IO.Path]::GetFullPath($CacheRoot) } else { Join-Path $taskRoot '.gradle-user' }
$env:PATH = (Join-Path $env:JAVA_HOME 'bin') + ';' + $env:PATH
"sdk.dir=$($env:ANDROID_HOME.Replace('\','/'))" | Set-Content -LiteralPath (Join-Path $taskRoot 'android/local.properties') -Encoding utf8
$gradleArgs = @('--project-dir', (Join-Path $taskRoot 'android'), '--no-daemon', '--console=plain', 'assembleDebug', 'lintDebug')
if ($Offline) { $gradleArgs += '--offline' }
if ($CacheRoot) { $gradleArgs += @('--project-cache-dir', (Join-Path $env:GRADLE_USER_HOME 'project-cache')) }
if ($BuildRoot) { $gradleArgs += "-PdropLocalBuildRoot=$([IO.Path]::GetFullPath($BuildRoot).Replace('\','/'))" }
& $gradle @gradleArgs
if ($LASTEXITCODE -ne 0) { throw 'A compilação/verificação Android falhou.' }
$apk = if ($BuildRoot) { Join-Path $BuildRoot 'app/outputs/apk/debug/app-debug.apk' } else { Join-Path $taskRoot 'android/app/build/outputs/apk/debug/app-debug.apk' }
$destination = Join-Path $taskRoot 'dist/DropLocal-Android.apk'
& (Join-Path $env:ANDROID_HOME 'build-tools/35.0.0/apksigner.bat') verify --verbose $apk
if ($LASTEXITCODE -ne 0) { throw 'A assinatura do APK é inválida.' }
Copy-Item -LiteralPath $apk -Destination $destination -Force
$metadata = Get-Content -LiteralPath (Join-Path ([IO.Path]::GetDirectoryName($apk)) 'output-metadata.json') -Raw | ConvertFrom-Json
$version = $metadata.elements[0].versionName
if ($version -match '^[0-9]+(\.[0-9]+){1,2}$') { Copy-Item -LiteralPath $apk -Destination (Join-Path $taskRoot "dist/DropLocal-Android-$version.apk") -Force }
if ($CacheRoot -or $BuildRoot) {
    @{ CacheRoot = $CacheRoot; BuildRoot = $BuildRoot } | ConvertTo-Json | Set-Content -LiteralPath $pathConfig
}
Write-Host "APK gerado: $destination"

