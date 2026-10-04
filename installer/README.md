# Empacotamento Windows

Build-Windows-Installer.ps1 gera um MSI com WiX 6.0.2 obtido do NuGet oficial. Instalação por usuário, UpgradeCode estável, Windows Installer/Restart Manager, componentes com arquivos explícitos, atalhos e registro nativos. O apphost do SDK .NET usa apenas o runtime privado relativo em runtime/, sem launcher intermediário e sem console.

Não há desinstalador personalizado nem executável/script em ações de instalação/remoção. A única ação de dados define o diretório de uma instalação anterior registrada. Metadados antigos conhecidos são retirados pelas tabelas padrão; dados desconhecidos e arquivos recebidos são preservados.

O protótipo 0.8.1 foi retirado do build e retido como evidência privada em .installer-work/retired-installer-source. Não executar ou distribuir seus binários. A recusa automática da limpeza dos auxiliares antigos não foi contornada. Consulte INCIDENTE-DESINSTALADOR-0.8.1.md e VALIDACAO-0.8.2.md.
