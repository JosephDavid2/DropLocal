# Atualizar Drop Local

A partir da versão 0.8.3, clique em **Atualizar** na tela principal. O app consulta a última release estável de [JosephDavid2/DropLocal](https://github.com/JosephDavid2/DropLocal/releases/latest), compara a versão e oferece o download quando existe uma versão mais nova. A consulta usa internet; a transferência dos seus arquivos continua pela rede local.

No Windows, confirme o download. O app verifica o tamanho e o SHA-256 publicado pelo GitHub, abre o Windows Installer e fecha para permitir a atualização. Confirme a instalação na janela nativa e abra o Drop Local novamente. Não é necessário desinstalar. Arquivos recebidos e preferências da pasta ficam preservados pelo MSI com UpgradeCode estável.

No Android, confirme o download e volte ao Drop Local quando terminar. O download continua pelo gerenciador do sistema, mesmo fora da tela do aplicativo. O app verifica o tamanho e o SHA-256 antes de abrir o instalador. Se solicitado, autorize o Drop Local a instalar aplicativos na tela do Android, volte e confirme a atualização. A assinatura do APK deve ser a mesma da versão instalada; o Android verifica isso e mantém os dados e a pasta autorizada.

Antes de atualizar, termine as transferências. Se uma transferência começar durante o download, a instalação aguarda; no Windows, clique em Atualizar novamente depois dela. Cancelar a instalação mantém a versão atual.

## Quem já tem 0.8.2 ou anterior

Essas versões ainda não possuem o botão. Uma vez publicada a 0.8.3, baixe o MSI/APK da release e instale **sobre** o aplicativo existente, sem desinstalar. Depois disso, as próximas releases podem ser obtidas pelo botão. A 0.8.3 foi preparada localmente; não foi publicada por esta alteração.

## Publicar atualizações no GitHub

Enviar código com git push não disponibiliza uma atualização instalável. Para cada versão:

1. Aumente a versão em windows/DropLocal.csproj e android/app/build.gradle; aumente também o versionCode Android e a versão do MSI em Build-Windows-Installer.ps1.
2. Compile o MSI e o APK. Preserve a chave Android existente e o UpgradeCode do MSI; não inclua chaves no GitHub. A distribuição atual usa certificado Android Debug; mudar para outra chave impediria atualizar instalações existentes por cima.
3. Crie uma release estável com tag numérica como v0.8.3. Rascunhos e prereleases não são oferecidos.
4. Anexe os instaladores com os nomes exatos **DropLocal-Setup.msi** e **DropLocal-Android.apk**. O nome local versionado do MSI precisa ser renomeado ao anexar. Verifique que o GitHub informa digest sha256 para cada asset e que as versões internas dos pacotes correspondem à tag.
5. Publique a release somente quando ambos os arquivos estiverem prontos. Teste o botão a partir de uma versão anterior.

Sem asset ou SHA-256 válido, o app informa o problema e não abre um instalador. Não oferece versões iguais ou inferiores. O SHA-256 confere integridade com a release; a conta e o repositório do publicador permanecem parte da confiança da atualização. O MSI atual não tem assinatura Authenticode; Windows pode apresentar aviso de editor desconhecido. O instalador Android mantém a verificação de assinatura nativa.

Para remover no Windows, use Configurações > Aplicativos > Drop Local > Desinstalar ou o atalho Desinstalar Drop Local no menu Iniciar. Ambos usam o Windows Installer. Arquivos pessoais e recebidos são preservados.

Não use o protótipo 0.8.1 nem seu antigo alias EXE. O pacote atual é MSI. Consulte [guia](USAR-0.8.3.md).
