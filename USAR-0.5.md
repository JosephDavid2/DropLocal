# Drop Local 0.5 — uma interface para enviar e receber

## Atualizar

Windows: feche a versão anterior, extraia dist/DropLocal-Windows-0.5.zip e abra DropLocal.exe na pasta extraída. Para criar/atualizar o atalho do menu Iniciar, execute Install-Windows.ps1 incluído no ZIP. O PC requer .NET Desktop Runtime 10. Não copie apenas o EXE: mantenha DLL e arquivos de configuração juntos.

Neste checkout, o executável atualizado fica em dist/windows-unified/DropLocal.exe.

Android: copie dist/DropLocal-Android-0.5.apk para Download do celular, abra pelo gerenciador de arquivos e escolha Atualizar/Instalar. Não desinstale a instalação anterior. Pacote com.droplocal e a chave de assinatura foram preservados. versionCode 6, versionName 0.5. Requer Android 8.0 ou superior.

## Organização

As duas plataformas usam um só cabeçalho Drop Local e abas Enviar e Receber. A aba selecionada aparece em turquesa. O Windows inclui QR dentro de Receber / QR, na janela principal; não abre um segundo aplicativo/janela de recebimento.

As abas mantêm os dados e as operações em andamento. O rótulo Receber • ativo indica que o receptor permanece disponível mesmo na aba Enviar. As confirmações de autorização continuam aparecendo quando chega um pedido. Fechar o programa Windows interrompe o receptor.

No Android, o cabeçalho e as abas ficam visíveis enquanto o conteúdo rola. A área Atividade é comum às duas abas e reúne estado, progresso, conclusão e ações para abrir os arquivos. Em telas curtas ou com fonte ampliada, essa área entra na rolagem para manter todos os controles acessíveis.

## Android → Windows

1. Windows: escolha Receber / QR, selecione a pasta e clique Iniciar recebimento. Escolha o IP da interface usada na mesma rede do celular. Se o Firewall perguntar, permita na rede privada.
2. Android: escolha Enviar. Use Escanear QR do Windows ou digite IP e código de oito dígitos mostrados no Windows. O QR continua opcional.
3. Para parear, confirme no Windows. O pareamento preenche os dados e pode atualizar o IP/código do Android no Windows se o receptor Android já estiver ativo.
4. Toque Enviar arquivo no Android e selecione um ou mais arquivos. O aplicativo prepara cópias temporárias privadas, removidas ao finalizar/cancelar; precisa de espaço suficiente para o lote.
5. Confira nomes e tamanhos no Windows e aceite. Acompanhe o progresso na aba Receber / QR. Ao concluir, use Abrir pasta recebida.

Destino padrão Windows: Downloads/DropLocal. Nomes recebem prefixo único e caracteres incompatíveis são substituídos. Reenvios criam novas cópias; não há retomada automática.

## Windows → Android

1. Android: escolha Receber, escolha a pasta e toque Iniciar recebimento. IP e código da sessão aparecem nessa aba.
2. Windows: escolha Enviar, preencha IP e código do Android, selecione ou arraste arquivos e clique Enviar arquivos.
3. Confirme o pedido no celular. A área Atividade mostra progresso e Transferência concluída.
4. Use Abrir arquivo ou Abrir arquivos (N) para escolher em uma lista. Abrir pasta / local é habilitado quando o gerenciador instalado oferece suporte a diretórios.

Você também pode iniciar o receptor Android, mudar para Enviar e escanear o QR do Windows com o receptor Windows iniciado. Ao confirmar o pareamento no PC, o Windows recebe o IP e o código atual do receptor Android. Reiniciar o receptor Android altera o código: use Parear / atualizar Windows e confirme novamente no PC.

Trocar de aba não para o receptor. Sair normalmente para a tela inicial do Android interrompe o receptor, como antes. Ao abrir arquivos/local/seletores/scanner pelo Drop Local, o receptor continua enquanto o processo existir; o Android pode suspender ou encerrar processos fora do primeiro plano. Para aceitar novos pedidos, retorne ao Drop Local.

## Rede e recursos preservados

Mesma rede local, Windows recebe/pareia em TCP 45833 e Android recebe em TCP 45832. Redes com isolamento entre clientes, VPN ou Firewall podem impedir a conexão. Cada pedido exige aceitação no receptor. Autorização básica por código; dados locais sem TLS e sem servidor de terceiros.

O usuário confirmou QR, envio e transferência no Android 0.4.1. O leitor e sua lógica de captura não foram refeitos na revisão 0.5; apenas o título do scanner passou a usar Drop Local. A revisão concentra-se na navegação e na interface.

## Validação e limites

Windows 0.5 compilado e renderizado pelo próprio aplicativo nas duas abas. Teste integrado verifica uma só janela principal, dados retidos ao alternar abas, receptor ativo com a aba Enviar visível, confirmação vinculada à janela principal, recebimento de arquivo binário e vazio, e liberação do receptor ao parar. Testes existentes de protocolo também passaram.

Android 0.5 compilado com lint e assinatura verificados. Prévia nativa Android não foi obtida nesta sessão; não há emulador preparado. A nova organização da tela precisa de conferência no celular, especialmente com fontes grandes. O funcionamento do QR na versão anterior já foi confirmado pelo usuário.

Prévias reais Windows:
- dist/Windows-0.5-enviar.png
- dist/Windows-0.5-receber.png

Compilar: dotnet publish windows/DropLocal.csproj -c Release --self-contained false -o dist/windows-unified
Android: ./Build-Android.ps1 -Offline
Testes: dotnet run --project tests/ProtocolTests.csproj -c Release
Navegação integrada: dist/windows-unified/DropLocal.exe --verify-navigation dist/UI-0.5-check

Ferramentas/cache local e chave de assinatura preservados. Nenhuma configuração do OneDrive foi alterada.
