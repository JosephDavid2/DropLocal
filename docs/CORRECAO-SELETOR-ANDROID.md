# Destinatário durante seleção de arquivos no Android

Correção de 05/10/2026 para envio Android → Windows.

## Causa identificada no código

LocalDiscovery remove aparelhos após 10 segundos sem anúncio. MainActivity.renderNearby limpa selectedDevice quando o aparelho não está no snapshot. Antes desta correção, o retorno de ACTION_OPEN_DOCUMENT consultava essa seleção mutável, perdendo o destinatário ou usando uma conexão manual existente. Não foi reproduzida a suspensão dos anúncios em um celular físico.

## Correção

Cada abertura do seletor captura uma FilePickDestination. A lista pode expirar sem apagar essa operação. Novos anúncios do mesmo id atualizam endereço, token e disponibilidade; anúncios de outros aparelhos não alteram o destino. O retorno consome e limpa a operação, inclusive em cancelamento, resultado vazio e falha ao abrir o seletor. O envio não usa conexão manual como alternativa a um destinatário próximo perdido. Indisponibilidade explicitamente anunciada bloqueia o envio; na ausência de anúncios, a conexão TCP com timeout e a aceitação normal do receptor verificam o destino.

## Validação

- AndroidFilePickCheck executou o estado usado em produção: dois minutos de snapshots vazios, outro aparelho, atualização de endereço/token do mesmo id, ready=false, conexão manual capturada e operação seguinte independente.
- AndroidDiscoveryStateCheck e AndroidStateCheck passaram.
- Build-Android.ps1 -Offline: assembleDebug e lintDebug passaram; assinatura v2 do APK verificada. APK em dist/DropLocal-Android.apk. Nenhuma instalação ou publicação realizada.

## Limites e verificação física pendente

ADB não encontrou aparelho conectado. Cancelamento e integração do callback foram inspecionados e compilados, sem teste de UI instrumentado. O estado da operação é em memória; destruição da Activity ou encerramento do processo enquanto o seletor está aberto continuam fora desta correção. Não se afirma que a causa de suspensão dos anúncios foi comprovada fisicamente.

Em Android e Windows reais, escolher o computador, permanecer no seletor por mais de 30 segundos e enviar um arquivo. Repetir com cancelamento e nova seleção, vários arquivos, computador fechado durante a seleção e computador reaberto com novo anúncio. Conferir destinatário, mensagem de falha quando indisponível e arquivo recebido.
