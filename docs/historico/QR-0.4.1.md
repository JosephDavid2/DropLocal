# Drop Local 0.4.1 — correção do leitor QR Android

Instale dist/DropLocal-Android-0.4.1.apk por cima da versão anterior, sem desinstalar. Pacote com.droplocal, versionCode 5, versão 0.4.1, mesma chave de assinatura. A tela do scanner também identifica 0.4.1. O Windows 0.4 e seu QR continuam compatíveis.

## Falha investigada

O relato é da câmera interna do Drop Local aberta, apontada ao QR completo, sem reconhecimento nem feedback. Não há gravação dos quadros desse aparelho, então a causa exata nele não foi reproduzida. A revisão encontrou problemas demonstráveis no software: binarização fixa em 128, projeção apenas afim, nenhuma recuperação de erros Reed–Solomon, prévia esticada e seleção de captura com preferência por 640 pixels de largura. Exceções de análise eram ocultadas.

A suíte óptica reproduz limitações da implementação: o leitor 0.4 falha em 7 dos 12 quadros legíveis (contraste e perspectiva); o novo lê os 12. Isso demonstra melhora nas condições testadas, sem atribuir a falha à posição do usuário ou provar funcionamento no aparelho.

## Correção

- Binarização local de 8 × 8 com vizinhança, alternativa global Otsu.
- Projeção usando os três marcadores e o alvo de alinhamento do QR; amostragem em cinco pontos de cada módulo.
- Reed–Solomon em GF(256): recupera até 13 bytes danificados no bloco 134/108 desta versão 5-L. Recalcula síndromes e valida conteúdo antes de aceitar; danos excessivos falham.
- Captura NV21 com preferência por largura 1280 (limite 1920 × 1080). Usa o tamanho realmente aplicado pelo dispositivo. A orientação visual mantém a proporção da prévia; o detector funciona nas quatro rotações dos dados.
- Foco contínuo quando suportado; foco automático inicial/periódico nos dispositivos sem modo contínuo; botão Focar/toque na prévia e luz opcional.
- Feedback de quadros analisados, padrões detectados e tentativa de recuperação; erros de análise visíveis e no log DropLocalQR. Aviso de ausência de quadros e botão Reiniciar câmera.
- Uma análise por vez, cópia somente do plano de luminância, descarte dos resultados de uma sessão antiga após reiniciar/pausar. Câmera liberada ao sair.

QR continua opcional. IP/código manual, protocolo, assinatura, visual, envio nos dois sentidos e abertura dos arquivos recebidos permanecem disponíveis. Nenhuma dependência adicional no APK.

A orientação de exibição não altera os bytes de preview, conforme [documentação Android Camera](https://developer.android.com/reference/android/hardware/Camera#setDisplayOrientation(int)); a leitura usa as dimensões brutas, sem assumir que a imagem está na orientação da tela.

## Validação reproduzível

No PowerShell, dentro do checkout:

    dotnet run --project tests/ProtocolTests.csproj -c Release --no-restore
    python tests/generate_qr_frames.py
    javac -d tests/qr-classes android/app/src/main/java/com/droplocal/PairQrDecoder.java tests/QrDecoderCheck.java tests/QrRobustnessCheck.java
    java -cp tests/qr-classes QrDecoderCheck dist/qr-test.matrix
    java -cp tests/qr-classes com.droplocal.QrRobustnessCheck dist/qr-test.matrix tests/qr-frames
    ./Build-Android.ps1 -Offline

O gerador exige Python com Pillow e NumPy; o runtime local contém ambos. Gera luminância de 8 bits e PNGs com transformações projetivas independentes, módulos fracionários, brilho/contraste variados, gradiente, ruído e desfoque determinísticos. A matriz é exportada pelo encoder Windows. Referência congelada do leitor anterior: tests/PairQrDecoder-0.4.java.txt. Evidências em dist/QR-0.4-baseline.txt e dist/QR-0.4.1-validation.txt; montagem em dist/qr-optical-fixtures.png.

Resultados: testes de protocolo existentes passaram; quatro rotações simples passaram; 260 blocos aleatórios com 1–13 bytes danificados foram recuperados; dano excessivo rejeitado; 12 imagens degradadas legíveis reconhecidas; quadro vazio e ruído rejeitados. Compilação assembleDebug + lintDebug concluída, 0 erros/30 avisos de lint (incluem textos não internacionalizados e APIs antigas). APK assinado em v2 com certificado SHA-256 dcfb59a9db5197e4d58ea9fc3c5e4a9b894adcbe40e33a5a557c19a1d3cbedb8.

## Limites e teste no aparelho

Não houve captura/ensaio em câmera física nesta sessão. Os tempos observados são no computador, não representam desempenho no celular. O leitor segue dedicado ao QR atual do Drop Local. Reflexos que apagam módulos, foco severamente errado, código muito pequeno e grandes distorções ainda podem exceder a recuperação. Não se afirma que a falha específica do aparelho esteja fisicamente confirmada como resolvida.

Para verificar: atualize o APK; confirme 0.4.1 no título do scanner; no Windows 0.4, inicie Receber / QR; abra o scanner interno e confira que o contador aumenta. O reconhecimento deve retornar ao pareamento, seguido de pedido de confirmação no Windows. Depois confirme um envio em cada sentido. Se não reconhecer, o texto diferencia câmera sem quadros de padrões detectados sem decodificação; a entrada manual segue disponível. Não é necessário instalar um leitor externo.

