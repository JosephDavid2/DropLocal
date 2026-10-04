# Incidente no protótipo do desinstalador Windows 0.8.1

Estado: distribuição suspensa em 4 de outubro de 2026. Nenhuma release ou binário 0.8.1 foi publicado. Conferência do GitHub autenticado mostrou apenas a release 0.5.1. Foram publicados alguns fontes e testes na main, mas a atualização de fontes foi interrompida e não representa uma entrega completa.

## Evidência confirmada

Os prints enviados pelo usuário mostram Kaspersky Premium / Inspetor do Sistema (System Watcher), detecção PDM:Trojan.Win32.Generic sobre Uninstall.exe, aplicativo identificado como “Desinstalar Drop Local — by Joseph David”. Eventos: cópia de backup às 14:21:59 e exclusões às 14:23:23 e 14:23:25. O caminho truncado no primeiro print termina na pasta de teste ...1ba\DropLocal\Uninstall.exe, compatível com .installer-work/lc-8eb061ba.

O primeiro teste de remoção gerou erro de bloqueio em Uninstall.exe às 14:22:05. O teste seguinte gerou resultado de preservação de arquivos pessoais às 14:23:29. Auxiliares de teste permaneceram com threads suspensas em consultas posteriores. Isso é compatível com intervenção do antivírus, mas não demonstra sozinho a causa de cada espera. Uma consulta posterior não encontrou mais processos Uninstall.exe.

O pacote local posterior foi compilado às 14:25:53, com outro hash de desinstalador, e o teste funcional posterior escreveu PASS às 14:27:09. Esse PASS verifica resultados locais de arquivos/registro/processos; não prova ausência de detecção nem aprovação de segurança. Não liberar essa recompilação com base apenas no PASS.

## Comportamentos implementados e hipóteses

O protótipo copia seu próprio executável para uma pasta temporária e inicia um processo auxiliar para remover arquivos do manifesto, atalhos e registro, após fechar os processos daquela instalação. Uma tentativa anterior também iniciou PowerShell oculto com comando codificado para esperar o auxiliar e apagar sua cópia temporária. Essa limpeza por PowerShell foi removida na recompilação posterior, mas a autocópia e o auxiliar permaneceram.

Esses comportamentos são hipóteses para o gatilho comportamental. Os prints não contêm a regra ou sequência completa de operações responsável pela detecção. Não classificar o alerta como falso positivo, não atribuir culpa ao Kaspersky e não supor que mudar o hash resolveu o problema.

## Artefatos locais retidos para identificação

- dist/DropLocal-Setup-0.8.1.exe e alias dist/DropLocal-Setup.exe: SHA256 080132D67E2388458D242157C76499A8A1986DDDDF2AD898B29090F77273AFF8.
- .installer-work/final081/Uninstall.exe: SHA256 0C787DCB88649AFA000D2B2AE645C089E4872DF896915B129BEDE1F881D74D27.
- Build anterior do auxiliar, compilado às 14:21:13, retido em um payload de teste: SHA256 A5CAC6AEB99A652BF9D9B82A2E1333EE09C24671013E9A8E8EE3E7CD11676CCC. Esse horário antecede o primeiro evento observado; não há hash do objeto quarentenado nos prints para estabelecer identidade definitiva.

Os binários não foram alterados nem executados novamente após o alerta. Não houve restauração de quarentena, exceção ou mudança de proteção. A tentativa de limpar auxiliares antigos de teste foi rejeitada pela política automática de execução e não foi contornada.

## Solução recomendada

Abandonar o desinstalador e o assistente personalizados para a próxima distribuição. Preferir MSI/Windows Installer com WiX e Restart Manager, ou Inno Setup padrão, com inventário explícito dos arquivos do programa, runtime privado, instalação por usuário, remoção convencional e preservação de arquivos recebidos/configurações pessoais. Evitar scripts de limpeza codificados, autocópia personalizada, encerramento forçado e remoção recursiva de dados desconhecidos.

Preservar a interface aprovada da 0.8.0, o launcher sem console, autoria e assinatura Android. Antes de liberar: revisar o novo pacote, testar instalar/atualizar/remover em ambiente descartável com o antivírus ativo, verificar ausência de eventos e confirmar preservação de dados e encerramento dos processos. Usar uma nova versão identificável para não confundir com o protótipo bloqueado. Empacotamento padrão reduz código próprio; não garante por si só ausência de alertas.

Referências oficiais: https://learn.microsoft.com/en-us/windows/win32/msi/using-windows-installer-with-restart-manager ; https://jrsoftware.org/ishelp/topic_setup_closeapplications.htm ; https://jrsoftware.org/ishelp/topic_filessection.htm .

## Substituição 0.8.2

A versão 0.8.2 substituiu o empacotamento personalizado por MSI WiX/Windows Installer e apphost nativo do SDK .NET. Ciclo de vida e preservação de dados passaram com Kaspersky ativo; scans não encontraram detecções e o Inspetor do Sistema não registrou novos eventos durante o teste. Evidências e limites em [VALIDACAO-0.8.2.md](VALIDACAO-0.8.2.md). O protótipo anterior permanece bloqueado e não deve ser executado ou distribuído.
