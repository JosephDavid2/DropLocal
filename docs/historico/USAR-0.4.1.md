# Drop Local Android 0.4.1 / Windows 0.4 — dois sentidos, QR e arquivos recebidos

## Atualizar

Windows: feche o programa antigo e abra `dist/windows-bidirectional/DropLocal.exe`. Para copiar/instalar em outro PC, extraia `dist/DropLocal-Windows-0.4.zip`. O ZIP inclui todos os arquivos e `Install-Windows.ps1`; execute `powershell -ExecutionPolicy Bypass -File .\Install-Windows.ps1` na pasta extraída, se quiser o atalho no menu Iniciar. Requer .NET Desktop Runtime 10.

Android: copie `dist/DropLocal-Android-0.4.1.apk` por USB para Download, abra no aplicativo de arquivos e escolha Atualizar/Instalar. Mantive `com.droplocal` e a chave de assinatura; não desinstale a versão anterior. Android 8.0 ou superior.

## Android → computador

1. No Windows, clique em **Receber / QR**. Escolha a pasta, confira o IP da sua rede e clique **Iniciar recebimento**. Se o Firewall do Windows solicitar, permita o aplicativo na rede **privada**. A porta de entrada usada é TCP 45833.
2. No Android, vá ao cartão **Enviar para o computador**. Informe **IP do computador** e **código de verificação do Windows**, ou use **Escanear QR do Windows**. A câmera é opcional: a entrada manual continua disponível.
3. Para parear, aceite a confirmação no Windows. O QR apenas preenche os dados e autentica o pedido; não dispensa a aceitação dos arquivos.
4. Toque em **Enviar arquivo** no Android e escolha um ou mais arquivos. O aplicativo prepara uma cópia temporária na sua área privada para obter tamanhos corretos e manter os dados estáveis. Isso pode exigir espaço livre do tamanho do lote. As cópias são removidas ao terminar/cancelar.
5. Confira nomes, tamanhos e destino no Windows e clique **Aceitar**. Aguarde **Transferência concluída**. A janela de recebimento permanece disponível para mais envios e oferece **Abrir pasta recebida**.

A pasta padrão é `Downloads/DropLocal` no perfil do Windows; você pode escolher outra. Nomes salvos recebem prefixo único e caracteres incompatíveis com Windows são substituídos para evitar sobrescrita e caminhos indevidos. Não há retomada automática: reenvios criam novas cópias.

## Computador → Android

O fluxo manual anterior continua funcionando: no Android, escolha a pasta e inicie o recebimento; no Windows principal, informe **IP do Android** e **código de sessão do Android**, selecione arquivos e envie. Aceite no celular.

O QR também pode preencher esses dados no Windows: primeiro inicie o recebimento no Android; depois escaneie o QR da janela **Receber / QR** do Windows e confirme o pareamento no PC. O pedido autenticado informa ao Windows o IP e código atual do receptor Android. Se o código Android mudar ao reiniciar o receptor, toque **Parear / atualizar Windows** no celular e confirme no computador.

## Ao concluir no Android

A tela mostra **Transferência concluída**, a quantidade recebida e a mensagem secundária **Receptor disponível para novos envios**.

- Um arquivo: **Abrir arquivo** usa o aplicativo adequado ao tipo do arquivo.
- Vários arquivos: **Abrir arquivos (N)** mostra uma lista numerada com nomes e tamanhos para escolher qual abrir.
- **Abrir pasta / local** é habilitado quando um gerenciador instalado oferece abertura de diretórios. Se não houver suporte, abra a pasta escolhida pelo aplicativo Arquivos / Meus Arquivos.

O acesso ao arquivo aberto é concedido temporariamente ao aplicativo escolhido. Ao abrir um arquivo/local pelo próprio Drop Local, o receptor continua ativo enquanto o processo permanecer em execução. Para receber e aceitar novos pedidos, mantenha/retorne à tela do Drop Local. Não há serviço de segundo plano; o Android pode suspender/encerrar o processo quando ele fica fora de primeiro plano. Sair normalmente para a tela inicial interrompe o receptor. Arquivos já concluídos permanecem salvos.

## QR e rede

Use **Escanear QR do Windows** dentro do Drop Local. Permita a câmera somente se quiser usar esse recurso; dispositivos sem câmera continuam usando IP e código. O reconhecimento acontece automaticamente. A tela agora mostra imagens analisadas e a etapa da leitura; oferece Focar, Luz (quando suportada) e Reiniciar câmera. Se aparecer que a câmera não entrega imagens, use Reiniciar câmera. Ao reconhecer, o aplicativo retorna ao pareamento e solicita confirmação no Windows. O leitor é dedicado aos QR deste aplicativo (versão 5-L, máscara 0), ajusta o limiar à iluminação, corrige perspectiva pelo alvo de alinhamento e recupera até 13 bytes danificados pelo Reed–Solomon antes de validar os dados. Reflexos intensos, desfoque severo ou QR pequeno demais ainda podem impedir a leitura. A correção não foi validada na câmera física do usuário.

Ambos os aparelhos devem estar na mesma LAN. Redes de convidados com isolamento, VPNs ou bloqueio do Firewall podem impedir a conexão. Windows envia para Android na porta 45832; Android envia/pareia com Windows na porta 45833. Os códigos e os arquivos trafegam diretamente, sem servidor de terceiros. A versão mantém a autenticação básica por código e a confirmação de cada pedido; não usa TLS.

## Compilar e verificar

`dotnet publish windows/DropLocal.csproj -c Release --self-contained false -o dist/windows-bidirectional`

`Build-Android.ps1 -Offline` usa as ferramentas e caches locais preparados. O download de bibliotecas QR foi bloqueado pela rede desta sessão; a solução final usa implementação própria, sem acrescentar dependências de rede à compilação ou ao funcionamento.

`dotnet run --project tests/ProtocolTests.csproj -c Release` verifica envio anterior, receptor Windows, autenticação, pareamento, recusa, bytes de arquivos binários/Unicode/vazios, limpeza de arquivo parcial e disponibilidade para o envio seguinte. O teste também exporta a matriz QR.

Veja QR-0.4.1.md para comandos reproduzíveis. Os testes incluem quatro rotações, 14 quadros ópticos com contraste, iluminação, perspectiva e desfoque variados, 260 blocos com 1–13 bytes corrompidos e rejeição de dano excessivo, quadro vazio e ruído. A implementação 0.4 falhou em 7 dos 12 quadros legíveis; a 0.4.1 leu os 12.

Android 0.4.1 compilado, lint com 0 erros e 30 avisos, assinatura verificada e igual à versão anterior. Windows 0.4 compatível; não requer atualização. As janelas Windows foram renderizadas pelo aplicativo e inspecionadas. Sem teste físico desta versão: câmera, abertura pelo gerenciador/aplicativo do celular e envio Android → PC por Wi-Fi real ainda precisam de validação no aparelho.

