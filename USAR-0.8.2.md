# Drop Local 0.8.2 — by Joseph David

Aplicativos nativos Windows e Android para transferir arquivos na mesma rede local. Sem navegador e sem servidor externo. Esta versão simplifica a interface e remove completamente tutorial, tour, demonstrações, etapas e bloqueios do guia.

## Instalar e usar

1. Atualize os dois aparelhos: `DropLocal-Setup.msi` no Windows x64 e `DropLocal-Android-0.8.2.apk` no Android 8 ou superior. Feche o aplicativo Windows antes de atualizar. A assinatura do APK foi preservada para instalar sobre a versão anterior.
2. Abra Drop Local na mesma rede. A tela principal mostra os aparelhos, com nome e ícone de computador ou celular.
3. Windows: arraste arquivos para a linha do destinatário ou clique nela para escolher os arquivos. O pedido é enviado logo depois da seleção.
4. Android: toque no destinatário e escolha os arquivos.
5. Quem recebe vê automaticamente um pedido com remetente e arquivos. Basta Aceitar ou Recusar. Não precisa escolher, procurar ou adivinhar quem está enviando.
6. Acompanhe o progresso e a conclusão. Os arquivos recebidos podem ser abertos pelo aplicativo.

Funciona nos dois sentidos entre Windows e Windows e entre Windows e Android. Cada lote pede uma única aceitação; a descoberta não exige um pareamento separado.

## Onde salvar

Windows inicia o recebimento automaticamente e usa `Downloads/DropLocal`. Para mudar, abra Configurações; a pasta escolhida é lembrada nas próximas aberturas.

Android precisa de uma escolha inicial de pasta pelo seletor do sistema. Toque em “Escolher pasta para receber”. Depois disso, a recepção começa automaticamente. A escolha é lembrada quando o provedor permite manter a autorização. Não há botão para ativar uma sessão. Se cancelar a troca de pasta, o receptor retoma a pasta anterior.

Conexão manual fica fora do fluxo principal, como alternativa. As versões anteriores continuam aceitas pelo transporte; para o fluxo automático, atualize os dois aparelhos.

## Disponibilidade e rede

Deixe o aplicativo aberto no Android. Ao sair para outro aplicativo, descoberta e recepção são suspensas e retomam ao voltar, enquanto o processo existir. Seletores de arquivos, câmera e abertura dos arquivos recebidos preservam a recepção. Não há serviço permanente em segundo plano.

A lista só atualiza seus controles quando um aparelho entra, sai ou muda de estado/endereço/credencial. Anúncios repetidos não recriam a interface nem mudam o foco ou a seleção. Um aparelho que ainda não escolheu sua pasta aparece preparando o recebimento.

Descoberta multicast IPv4 UDP 45834; recepção TCP 45833 no Windows e 45832 no Android. Redes com isolamento de clientes ou bloqueio de multicast/firewall podem impedir comunicação. Sem TLS, como nas versões anteriores; o remetente anuncia seu próprio nome, e a confirmação também mostra o endereço. Limite de mil arquivos por lote, buffers de 64 KiB, sem retomada de transferência interrompida.

## Validação

Os testes Windows verificam a interface sem tutorial, atualização estável da lista, troca de credenciais e desaparecimento dos dispositivos; o manipulador real de arrastar e soltar com dois destinos; confirmação automática com nenhum destinatário selecionado e com outros aparelhos selecionados, três recebimentos e uma recusa, sem gravação na recusa e com bytes exatos.

Os testes de protocolo cobrem versões anteriores, descoberta multicast com três instâncias no computador, estado indisponível e expiração, transferências v3 alternadas com uma aceitação por lote e interoperabilidade com fixture Java. O teste JVM Android verifica o filtro de atualização da lista e o armazenamento de sessão de produção. Compilação Android inclui lint e verificação da assinatura.

O usuário confirmou que o fluxo atual funcionou no teste físico. A revisão 0.8.2 mantém essa interface e acrescenta o desinstalador Windows. Os testes automatizados não cobrem todos os modelos e redes.

Desinstale pelas Configurações do Windows ou pelo atalho Desinstalar Drop Local no menu Iniciar. O pacote MSI usa o Windows Installer, sem Uninstall.exe personalizado. Arquivos recebidos e configurações pessoais ficam fora do inventário de instalação e são preservados. A validação de instalação e segurança está documentada em [VALIDACAO-0.8.2.md](VALIDACAO-0.8.2.md). O protótipo 0.8.1 permanece bloqueado.

