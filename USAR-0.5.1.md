# Drop Local 0.5.1
by Joseph David

## Windows

Abra DropLocal-Setup-0.5.1.exe e clique Instalar. A instalação é por usuário, sem instalação global de ferramentas. Inclui um runtime privado .NET Desktop 10.0.12 x64, atalho no menu Iniciar, atalho opcional na área de trabalho e entrada de desinstalação nos Aplicativos do Windows.

Destino: %LOCALAPPDATA%/Programs/DropLocal. Windows x64 com .NET Framework 4.x para o assistente/launcher (presente nesta máquina); o .NET 10 necessário ao aplicativo já está incluído. O instalador não é assinado com certificado Authenticode: “by Joseph David” é crédito de autoria, não uma assinatura criptográfica.

Feche Drop Local antes de atualizar. A versão anterior, se existir no destino, é preservada em uma pasta DropLocal.backup-DATA ao lado da nova instalação; isso evita apagar arquivos desconhecidos. O instalador reverte a troca de pasta se falhar. Backups podem ser removidos manualmente depois de conferir a atualização.

Para desinstalar, use Aplicativos do Windows → Drop Local → Desinstalar. A rotina valida o manifesto, pede confirmação e remove somente os arquivos instalados; os arquivos recebidos em Downloads ou outra pasta escolhida ficam preservados. Arquivos desconhecidos na pasta do aplicativo e backups anteriores também são preservados. Se o aplicativo estiver aberto, feche-o antes de desinstalar.

## Android

Instale DropLocal-Android-0.5.1.apk sobre a instalação anterior, sem desinstalar. Android 8.0 ou superior, applicationId com.droplocal, versionCode 7, mesma assinatura de atualização. A autoria by Joseph David aparece no cabeçalho.

## Enviar e receber

Ambos os aplicativos usam um cabeçalho e abas Enviar / Receber. No Windows, o QR está dentro da aba Receber / QR. A entrada manual de IP e código continua disponível.

Android → Windows: inicie Receber / QR no PC, escolha o destino, use o scanner ou os dados manuais na aba Enviar do Android; confirme o pareamento/pedido no Windows e envie os arquivos escolhidos.

Windows → Android: escolha pasta e Iniciar recebimento na aba Receber do Android; preencha IP/código no Windows, selecione/arraste arquivos e envie. Confirme no celular. Ao concluir, abra o arquivo, escolha na lista de arquivos ou abra a pasta quando o gerenciador oferecer suporte.

Mudar de aba preserva os dados e o receptor ativo. Todo pedido exige aceitação. Parar/cancelar e as ações de abertura permanecem disponíveis.

## Rede e limites

Mesma rede local. Windows recebe/pareia em TCP 45833; Android recebe em TCP 45832. O Firewall pode precisar permitir o aplicativo na rede privada. Redes com isolamento entre clientes ou VPN podem impedir a conexão.

Autorização básica por código e confirmação, sem TLS e sem servidor externo. Buffers 64 KiB, máximo 1000 arquivos, sem retomada automática. Android prepara cópias temporárias antes de enviar; precisa de espaço livre. O receptor Android depende da atividade/processo, sem serviço persistente: mantenha o aplicativo em primeiro plano para aceitar pedidos; trocar de aba é seguro.

## Verificação

O usuário confirmou que QR e transferência funcionaram na 0.4.1 e que todos os recursos da interface 0.5 funcionaram em seu teste. A 0.5.1 acrescenta o crédito de autoria e o pacote instalável; não muda o protocolo nem o leitor QR.

O instalador é verificado por leitura do payload, extração em uma pasta de teste, manifesto de desinstalação e execução pelo runtime privado. A instalação padrão/registro/atalhos e a desinstalação destrutiva não foram executados na conta do usuário nesta sessão. A nova assinatura visual Android é validada por compilação; não se inventam testes físicos da 0.5.1.
