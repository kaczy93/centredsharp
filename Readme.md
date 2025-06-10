# CentrED# Fork

Este projeto é um fork do [CentrED original](https://git.aksdb.de/aksdb/CentrED) com diversas melhorias e novas funcionalidades adicionadas ao longo do tempo. Abaixo estão os principais destaques encontrados no histórico de commits deste repositório.

## Principais diferenças em relação ao projeto original

- **Compatibilidade multiplataforma**: Suporte para Linux e macOS foi adicionado, permitindo compilar e executar o editor em diferentes sistemas operacionais.
- **Integração com ChatGPT**: O gerador procedural de terreno possui integração opcional com a API do ChatGPT para criação de tiles e grupos a partir de prompts. A chave de acesso pode ser salva e carregada diretamente na interface.
- **Gerador baseado em HeightMap**: Além do gerador procedural padrão, existe uma janela de geração por mapa de altura com seleção de quadrantes e salvamento de grupos em JSON.
- **Melhorias de desempenho e estabilidade**: Várias correções de concorrência, otimizações no carregamento e salvamento de blocos e tratamento de pacotes de rede desconhecidos.
- **Operações em larga escala**: Implementação de comandos e tratamento de Large Scale Operations (LSO) para modificar grandes áreas do mapa de forma segura.
- **BulkMode**: Novo modo de envio em lote no cliente para otimizar grandes gerações de mapa, utilizado pelo gerador de HeightMap.
- **Ferramentas de edição ampliadas**: Novas janelas e utilitários foram acrescentados, como gerenciador de LandBrush, suporte a conjuntos de tiles, filtros de estáticos, virtual layer configurável e histórico de ações (undo).
- **Interface modernizada**: Uso de docking no ImGui, múltiplas janelas (minimapa, opções, chat, toolbox, etc.) e opção de iniciar em modo janela.
- **Suporte a scripts e exportação**: Exemplos de scripts, exportação de mapas para PNG/JPG e outras utilidades que não estavam presentes na versão original.
- **Atualizações de dependências**: Projetos migrados para versões mais recentes do .NET, FNA e ImGui.NET.

## Como compilar

Requisitos:
- .NET 9 SDK

Clone o repositório com:

```bash
git clone --recursive https://github.com/kaczy93/centredsharp.git
```

Em seguida, execute:

```bash
dotnet build
```

## Links úteis

- [Download](https://kaczy93.github.io/centredsharp/#/Download/)
- [Discord](https://discord.gg/zpNCv36fQ8)
- [Server setup](https://github.com/kaczy93/centredsharp/wiki/Server-setup)

## Agradecimentos

- Andreas Schneider – criador do CentrED original
- [ServUO](https://github.com/ServUO/ServUO)
- [ModernUO](https://github.com/modernuo/ModernUO)
- [ClassicUO](https://github.com/ClassicUO/ClassicUO)
- [UOFiddler](https://github.com/polserver/UOFiddler)
- Jaedan e seu [UORenderer](https://github.com/jaedan/UORenderer)
- JetBrains pelo [Open Source Support](https://jb.gg/OpenSourceSupport)

<img src="https://resources.jetbrains.com/storage/products/company/brand/logos/jb_beam.png" width="128" height="128" />
