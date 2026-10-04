# Drop Local 0.6.0
by Joseph David

## Instalar e atualizar

Windows x64: execute DropLocal-Setup-0.6.0.exe com o aplicativo fechado. Instalação por usuário com runtime privado .NET Desktop 10.0.12, atalho no menu Iniciar, atalho opcional na área de trabalho e desinstalador. O launcher inicia somente a interface: nenhum console extra. O assistente/launcher exige .NET Framework 4.x. Sem certificado Authenticode; crédito de autoria não é assinatura digital.

Android 8+: instale DropLocal-Android-0.6.0.apk por cima do anterior. Pacote com.droplocal, versionCode 8, chave de atualização preservada. Atualize os dois aparelhos para usar o pareamento bidirecional v2. O transporte legado v1 continua aceitando pedidos com código e confirmação, mas a interface nova usa sessão v2.

## Uma conexão para os dois sentidos

Use a mesma rede local. Nos dois aparelhos, abra Conexão / QR (Windows) ou Conexão (Android), escolha a pasta e toque Ativar sessão. O Windows já sugere Downloads/DropLocal.

Android → Windows: leia o QR do PC ou informe IP e código do PC no Android e toque Parear. Confirme no PC. Isso configura as duas direções; não é necessário informar um segundo código para enviar do PC ao celular.

Windows → Android: informe IP e código mostrados no Android, escolha Android na lista de tipo e clique Parear. Confirme no celular. As duas direções ficam prontas.

Windows ↔ Windows: um PC informa IP e código do outro, seleciona Windows e clica Parear. O outro confirma. Depois, qualquer um abre Arquivos, seleciona/arrasta o lote e clica Enviar arquivos. Cada lote ainda exige aceitação no destino.

A mensagem Pareado representa uma sessão autorizada em memória, não um teste permanente de disponibilidade da rede. Nenhuma confiança é salva em disco e não há expiração fixa durante o uso. Desconectar, encerrar a sessão, reiniciar/fechar o app ou trocar para outro aparelho exige novo pareamento. Ao retomar depois de uma perda de rede, tente novamente; se o outro app foi reiniciado, desconecte e pareie outra vez.

Android preserva a sessão ao mudar de aba, usar câmera, permissões, seletor ou abrir um arquivo. Ao ir para outro aplicativo, o receptor pode ser suspenso; volta ao retornar, mantendo a sessão enquanto a atividade/processo existir. O sistema pode encerrar o processo em segundo plano: nesse caso, pareie novamente. Não há serviço persistente. Rotação de tela não recria a atividade principal. Deixe o aplicativo em primeiro plano durante transferências.

## Tutorial

Na primeira abertura o tutorial destaca o controle da etapa e bloqueia temporariamente as outras ações. Avança por ações reais: abrir Conexão, escolher pasta, ativar, parear, abrir Arquivos e selecionar/enviar. Pular tutorial está sempre disponível. A demonstração nas etapas dependentes de outro aparelho não conecta dispositivos, cria confiança ou envia arquivos. Ao concluir/pular, não abre automaticamente de novo; Como usar permite reabrir.

## Arquivos e limites

Abra arquivos/pasta após receber. Android oferece lista quando há vários arquivos; depende de aplicativos compatíveis no aparelho. Windows oferece Abrir pasta recebida. Lotes recusados não gravam arquivos. Arquivo parcial é removido quando possível; arquivos concluídos ficam salvos. Sem retomada.

TCP 45833 no Windows, 45832 no Android; permita na rede privada se o Firewall solicitar. VPN ou isolamento de clientes Wi-Fi pode impedir conexão. A sessão troca chaves aleatórias de 256 bits vinculadas ao IP do parceiro, mas o transporte não usa TLS. Use rede confiável. Máximo de 1000 arquivos, buffers 64 KiB, timeout de operação Windows de cinco minutos. Android prepara cópias temporárias: precisa de espaço livre.

Instalação em %LOCALAPPDATA%/Programs/DropLocal. Atualizações preservam a pasta anterior em DropLocal.backup-DATA. Desinstalador remove somente os arquivos do manifesto e seus atalhos, preservando recebidos e arquivos desconhecidos.

## Validação e limites dos testes

Testes automatizados: protocolo v1 e v2; um pareamento Windows↔Windows e três lotes alternados; consentimento por lote, recusa, chaves/IP e revogação; interoperabilidade Java↔Windows com a classe de sessão Android e um fixture JVM; bytes binários/Unicode/vazios; tutorial Windows com ações reais, demonstração sem envio, persistência e reabertura; abertura normal EXE/atalho, ausência de console e encerramento do launcher; extração do instalador e manifesto. O fixture JVM não substitui um teste físico do Android.

Android compilado, lint e assinatura verificados. Não houve teste físico da interface/tutorial 0.6.0, câmera ou lifecycle nesta sessão. Recursos anteriores da 0.5 e QR 0.4.1 foram confirmados pelo usuário. Instalação padrão de registro/atalhos e desinstalação destrutiva na conta do usuário não foram executadas; os testes de atalho usam a mesma função do instalador em pasta de teste.
