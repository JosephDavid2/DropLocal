# Validação Drop Local 0.8.2

Validação local em 4 de outubro de 2026, Windows x64, com Kaspersky 21.26 ativo.

## Pacote publicado

DropLocal-Setup.msi é uma cópia byte a byte de DropLocal-Setup-0.8.2.msi, SHA256:

C776AD28CE7E97DB0CB8894FAD72D7766C8D639707C7844E82C1605F6CEB41A2

MSI WiX 6.0.2, ProductCode 3FEABADD-48FF-49F9-982A-4C7EBD22D3FA, UpgradeCode 77EC6458-5B36-4D5F-AEBF-9ED9D91614E4. Instalação por usuário. Inventário de 487 arquivos. Auditoria da tabela CustomAction encontrou apenas SetINSTALLFOLDER tipo 35, que define um diretório; nenhuma ação executável, DLL ou script personalizado. Desinstalação nativa por msiexec, sem Uninstall.exe personalizado.

## Resultados

- Ciclo de instalação, migração de registro antigo sem executar script antigo, atualização com app aberto e remoção: PASS em instalação e registro isolados de teste. Windows Installer fechou o app do teste, removeu os 487 arquivos do programa, registro e atalhos, e preservou conteúdo pessoal desconhecido, inclusive dentro da pasta do runtime.
- Apphost nativo do SDK: runtime privado carregado, janela normal, nenhum console e fechamento completo do processo.
- Interface: recebimento automático sem selecionar remetente e com outro aparelho selecionado; estabilidade dos controles e foco; arrastar arquivos para dois destinos; confirmação por lote e bytes exatos: PASS.
- Android 0.8.2 (versionCode 12): compilação e lint passaram; assinatura APK v2 verificada e certificado preservado. SHA256 do certificado: dcfb59a9db5197e4d58ea9fc3c5e4a9b894adcbe40e33a5a557c19a1d3cbedb8.
- Kaspersky scan do payload final: 487 objetos OK, zero detecções, suspeitas, erros ou arquivos corrompidos.
- Kaspersky scan incluindo MSI final e dois MSIs isolados de teste: 983 objetos OK, zero detecções, suspeitas, erros ou arquivos corrompidos. Concluído às 15:05:28.
- Inspetor do Sistema: comparação dos relatórios antes/depois do teste MSI não encontrou novos eventos. Proteção, monitoramento de arquivos e Inspetor do Sistema estavam ativos. Não foram criadas exclusões nem restaurada quarentena.

Os MSIs de teste usam identidade própria e o mesmo payload final; o baseline usa versão de arquivo anterior para exercitar substituição real. Os logs completos permanecem locais para evitar publicar caminhos e telemetria pessoais. O fluxo da interface 0.8.0 foi confirmado pelo usuário nos aparelhos físicos; esta revisão preserva esse fluxo. Os resultados não abrangem todos os dispositivos, redes e produtos antivírus.

## Incidente anterior

O protótipo 0.8.1 permanece bloqueado e não foi publicado. A detecção não foi classificada como falso positivo. Seu empacotamento personalizado foi retirado do build. Uma tentativa anterior de limpar auxiliares foi rejeitada pela revisão automática e não foi contornada. Consulte [registro](INCIDENTE-DESINSTALADOR-0.8.1.md).
