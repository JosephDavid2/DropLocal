# Drop Local

**by Joseph David** · Windows e Android · versão 0.8.2

Abra, veja os aparelhos da mesma rede e envie. No Windows, arraste arquivos para o destinatário ou clique nele para selecionar. No Android, toque no aparelho e escolha os arquivos. O pedido aparece automaticamente no destino: basta aceitar ou recusar. Aplicativos nativos, sem navegador, servidor externo, tutorial ou tour.

[Baixar Windows x64 (MSI)](https://github.com/JosephDavid2/DropLocal/releases/latest/download/DropLocal-Setup.msi) · [Baixar Android](https://github.com/JosephDavid2/DropLocal/releases/latest/download/DropLocal-Android.apk) · [Releases](https://github.com/JosephDavid2/DropLocal/releases)

Windows usa Downloads/DropLocal e inicia a recepção ao abrir. Android pede a pasta uma vez e lembra a autorização quando o provedor permite. Mantenha o app aberto no Android. Configuração e conexão manual ficam fora do fluxo principal. Cada lote exige uma aceitação, independentemente do aparelho selecionado no destino.

![Tela Windows com aparelhos de demonstração](docs/images/windows-receber.png)

O MSI usa Windows Installer para instalar, atualizar e desinstalar, com runtime privado e atalhos no menu Iniciar. Não inclui desinstalador personalizado. Arquivos recebidos e dados pessoais são preservados. Consulte [guia de uso](USAR-0.8.2.md), [atualização](ATUALIZAR.md) e [validação da versão](VALIDACAO-0.8.2.md).

## Compilar

Windows requer .NET SDK 10 e WiX 6.0.2 do NuGet oficial:

```powershell
dotnet tool install wix --version 6.0.2 --tool-path .installer-work/wix --allow-roll-forward
./Build-Windows-Installer.ps1
./Build-Android.ps1 -Offline
dotnet run --project tests/ProtocolTests.csproj -c Release
```

Windows usa WinForms e runtime Desktop privado 10.0.12. O apphost nativo do SDK procura o runtime em runtime/, sem launcher intermediário ou console. Android usa Java, JDK 17, Gradle 8.11.1 e SDK/Build Tools 35, mínimo Android 8. Preserve sua chave de assinatura para atualizar instalações existentes. Ferramentas, caches, binários gerados e chaves privadas não são versionados.

A descoberta usa multicast IPv4 UDP 45834; recepção TCP 45833 no Windows e 45832 no Android. Redes com isolamento ou bloqueio de multicast podem impedir comunicação. O transporte atual não usa TLS; use uma rede de confiança. Consulte o guia para limites e ciclo de vida Android.

O protótipo de instalação 0.8.1 foi bloqueado após detecção comportamental e substituído pelo MSI 0.8.2. [Registro do incidente](INCIDENTE-DESINSTALADOR-0.8.1.md). Os scans e testes desta versão passaram com o Kaspersky ativo; isso descreve a validação realizada, sem garantia para todos os ambientes.
