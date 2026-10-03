# Ícone Drop Local

Identidade única: símbolo Wi-Fi azul-marinho `#0C1825` sobre fundo turquesa `#12E0DE`, com cantos arredondados. `tools/generate_icons.py` mantém uma única geometria e gera SVG, PNGs, ICO e recursos vetoriais Android. Para regenerar, use Python com Pillow e execute `python tools/generate_icons.py`.

- `drop-local.svg`: fonte vetorial escalável.
- `drop-local-preview.png`: prévia do ícone (512 px).
- `drop-local-256.png`: marca embutida no cabeçalho Windows.
- `drop-local.ico`: nove resoluções: 16, 20, 24, 32, 40, 48, 64, 128 e 256 px. Embutido no executável e nos recursos gerenciados; também publicado como `DropLocal.ico`.
- Android: foreground e background separados em canvas 108 × 108; o desenho, incluindo espessura das linhas, está dentro do círculo central de 66 dp. Variante adaptativa para API 26+ e camada monocromática para API 33+. O launcher aplica a máscara do aparelho e pode aplicar cores de tema quando o usuário ativa ícones temáticos. A variante vetorial combinada é usada no cabeçalho e como fallback.

A compilação Windows foi verificada por extração do ícone nativo do EXE, inicialização STA e renderização da janela. No Android foram verificados os recursos adaptativos e monocromáticos do APK, minSdk 26, versão 0.3/código 3, `com.droplocal` e a assinatura mantida. Não houve inspeção do launcher em celular/emulador. O lint terminou sem erros, com avisos não bloqueantes.

Referências: [ícones adaptativos Android](https://developer.android.com/develop/ui/compose/system/icon_design_adaptive), [ícone de aplicativo no Visual Studio](https://learn.microsoft.com/visualstudio/ide/how-to-specify-an-application-icon-visual-basic-csharp).
