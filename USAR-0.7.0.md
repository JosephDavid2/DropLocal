# Drop Local 0.7.0 — by Joseph David

Aplicativos nativos Windows e Android, com descoberta na rede local e confirmação de cada envio. Esta versão foi preparada localmente; consulte a página de releases para saber quais pacotes já foram publicados.

## Instalação e atualização

Windows x64: feche o Drop Local e execute `DropLocal-Setup-0.7.0.exe`. O instalador mantém um runtime .NET Desktop privado e cria os atalhos. O lançamento normal não abre um console adicional.

Android 8 ou superior: transfira `DropLocal-Android-0.7.0.apk` ao celular e instale sobre a versão anterior. A assinatura de atualização foi preservada. No aparelho Android 10 do usuário, a versão 0.6.0 instalou por transferência via PairDrop; abrir o download da 0.5.1 pelo Chrome e pelo gerenciador fechava um componente do Android. A causa dessa falha anterior não foi identificada.

## Envio simples

1. Abra Drop Local nos aparelhos conectados à mesma rede local.
2. Na primeira utilização do Android, escolha uma pasta para receber. A permissão é guardada quando o provedor permite; nas próximas aberturas o receptor é iniciado automaticamente. O Windows usa Downloads/DropLocal por padrão e inicia o recebimento após concluir ou pular o tutorial.
3. Na aba Aparelhos, aparecem os dispositivos com nome e indicação de celular ou computador. Um aparelho que ainda não ativou o recebimento aparece como “preparando recebimento”.
4. Windows: arraste arquivos diretamente para a linha do destinatário. Também pode dar dois cliques no aparelho para escolher arquivos, ou selecionar o destinatário e usar a aba Arquivos.
5. Android: toque no destinatário e escolha os arquivos. O destino recebe um único pedido para aceitar ou recusar o lote.
6. Acompanhe o progresso. Ao concluir, abra os arquivos ou a pasta recebida. Cada novo lote pede aceitação novamente.

Não há navegador ou serviço externo. A descoberta anuncia os aparelhos localmente a cada dois segundos; eles saem da lista aproximadamente dez a doze segundos depois de parar de anunciar. IP e QR continuam disponíveis em “IP / QR: conexão manual”, para redes que bloqueiam a descoberta ou para versões anteriores.

## Tutorial e ciclo de vida

O tutorial destaca a ação atual, permite Pular tutorial e oferece demonstrações sem enviar arquivos nem criar uma conexão. “Como usar” reabre o guia. O novo fluxo possui sua própria marca de conclusão.

Mudar de aba preserva a recepção. No Android, câmera, seletor de arquivos e abertura de arquivos recebidos preservam o receptor. Ao sair para outro aplicativo, descoberta e recepção são suspensas; ao voltar, a recepção é retomada enquanto a atividade/processo existir. A descoberta recomeça. Não há serviço em segundo plano permanente.

Encerrar explicitamente a recepção invalida sua credencial de descoberta. As credenciais e o pareamento manual ficam em memória; não há confiança permanente nem expiração fixa da sessão manual.

## Rede e limites

Descoberta IPv4: multicast 239.255.45.83, UDP 45834, limitado ao segmento da rede. Recepção: TCP 45833 no Windows e 45832 no Android. O firewall do Windows e o isolamento de clientes do roteador podem impedir a comunicação; nessas redes, a conexão manual também depende de haver comunicação entre os aparelhos.

O nome anunciado é informado pelo próprio aparelho; a janela de aceitação também mostra seu endereço. A descoberta dá permissão para pedir um envio, nunca para gravar arquivos sem aceitação. Os arquivos trafegam sem TLS, como nas versões anteriores. Limite de mil arquivos por lote, buffers de 64 KiB, sem retomada de transferência interrompida.

## Validação e pendências

Os testes automatizados verificam protocolos anteriores, recusa/cancelamento/interrupção, bytes binários, Unicode e arquivos vazios; três instâncias reais de descoberta multicast no computador, estado indisponível e desaparecimento da lista; transferências alternadas v3 sem pareamento separado, uma aceitação por lote, recusa sem gravação e rejeição de credenciais antigas. A compilação Android inclui lint e validação da assinatura.

A descoberta entre aparelhos físicos Windows/Android e o novo fluxo Android precisam do teste do usuário. A instalação bem-sucedida e o envio celular→Windows relatados nesta conversa referem-se à versão 0.6.0.

O desinstalador executável continua adiado em PENDENCIAS.md. Na remoção pelo script, o usuário observou que arquivos e registro foram removidos, mas a exclusão final da pasta vazia falhou por estar em uso. A implementação de remoção não foi alterada nesta versão.
