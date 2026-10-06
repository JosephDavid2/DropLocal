# Validação local 0.8.3 — 05/10/2026

## Alterações

- Destinatário de envio Android preservado durante o seletor de arquivos: FilePickDestination separa a operação da lista de descoberta e atualiza somente o mesmo id. Cancelamento e retorno consomem a operação.
- Botão Atualizar em Windows e Android. Consulta HTTPS da última release estável em JosephDavid2/DropLocal; comparação numérica da versão, assets de nome fixo, tamanho limitado e digest SHA-256 obrigatório.
- Windows baixa e verifica antes de abrir msiexec /i; fecha o aplicativo para a instalação nativa. Android usa DownloadManager com id/digest/tamanho persistidos, verifica o arquivo ao voltar e abre a instalação nativa, incluindo a permissão de instalar apps quando necessária.
- Versão Windows/MSI 0.8.3; Android versionName 0.8.3, versionCode 13. UpgradeCode Windows preservado: 77EC6458-5B36-4D5F-AEBF-9ED9D91614E4.

## Checks executados

- dotnet build/publish: sucesso, zero erros e avisos.
- tests/ProtocolTests.csproj: protocolo, QR, recebimentos, descoberta real multicast, pares e transporte v3 passaram. UpdateChecks verificou versões iguais/inferiores, draft/prerelease, versão numérica 0.10.0, asset ausente, URL de outro host/repo, SHA inválido e tamanhos inválidos; download íntegro, corrupção, truncamento, excesso, erro HTTP e cancelamento.
- Check real GitHub, sem instalação: última release v0.8.2; MSI baixado pelo downloader de produção, tamanho e SHA-256 conferidos após redirecionamentos; 0.8.3 local não oferece downgrade.
- JVM: AndroidUpdateCheck passou para versões e URL do repositório; AndroidFilePickCheck, AndroidDiscoveryStateCheck e AndroidStateCheck passaram, preservando a regressão do seletor.
- Build-Android.ps1 -Offline: assembleDebug, lintDebug e assinatura v2 passaram. Lint mantém avisos, sem erros; não se afirma zero avisos Android.
- Certificado do APK 0.8.3 comparado com o APK baixado da release v0.8.2: SHA-256 idêntico dcfb59a9db5197e4d58ea9fc3c5e4a9b894adcbe40e33a5a557c19a1d3cbedb8. Certificado atual Android Debug, a chave deve ser preservada.
- Build-Windows-Installer.ps1 gerou MSI local 0.8.3. Leitura da tabela Property do pacote, sem instalação: ProductVersion 0.8.3, ProductCode 91A1C554-77CD-4C21-A2DF-CD454E100544 e UpgradeCode estável confirmado. Assembly Windows 0.8.3.0. Authenticode: NotSigned. Interface Windows renderizada por --render-preview e inspecionada visualmente: botão Atualizar visível, sem sobreposição; imagem em docs/images/windows-receber.png.

Pacotes locais finais:

- dist/DropLocal-Setup-0.8.3.msi — SHA-256 983fe148f7f88d3e6eb710f00075799f589e193e2cdecb831b0ef23fdc8f9dd8
- dist/DropLocal-Android-0.8.3.apk — SHA-256 86f32bbeb1e9f0d09080b0e19bfda49f18494cd8f07f06bf6b627ae3a529bdfa

## Limites

Nenhuma release, push ou instalação realizada. A API pública ainda oferece 0.8.2; a nova versão só será distribuída após publicação autorizada de release com MSI/APK. O botão não aparece retroativamente em instalações antigas: instalar a primeira versão 0.8.3 por cima é necessário uma vez.

Não há aparelho Android conectado ao ADB. DownloadManager, permissão e instalador Android foram compilados e revisados, sem validação física/instrumentada. Parser JSON Android não foi executado na JVM; os checks JVM Android cobrem versão e URL. Instalação/upgrade Windows não foi executada nesta revisão; identidade MSI é verificada sem instalar e o ciclo de vida anterior está documentado na validação 0.8.2.

O MSI não tem Authenticode; a confiança vem da release HTTPS do repositório e da conferência SHA-256 do asset. Android mantém a verificação nativa da assinatura. SHA-256 não substitui a proteção da conta do publicador. A pasta de downloads de atualização é separada dos arquivos recebidos. Pacotes baixados podem permanecer no cache do aplicativo.

O estado do seletor é em memória: destruição da Activity/encerramento do processo durante a seleção ainda não está coberto. O download de atualização Android, por sua vez, tem id e metadados persistidos.

## Teste final antes de publicar

Em Windows e Android físicos, instalar 0.8.3 por cima de 0.8.2 e conferir arquivos/preferências preservados. Com uma release futura estável e versão maior, clicar em Atualizar; confirmar apenas o instalador nativo. Exercitar recusa/cancelamento da instalação, falta de internet, permissão Android negada/concedida, saída e retorno durante download e transferência iniciada durante download. Para o seletor, permanecer mais de 30 segundos antes de escolher, cancelar e repetir, e desligar/reabrir o computador durante a seleção.
