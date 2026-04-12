# Sistema Split Screen Multiplayer - Instruções de Configuração

## Novos Scripts Criados:

1. **PlayerData.cs** - Dados de cada jogador (ouro, nome, cartas compradas)
2. **TurnManager.cs** - Gerencia turnos entre jogadores
3. **GameUIManager.cs** - UI para mostrar informações dos jogadores
4. **SplitScreenManager.cs** - Configura split screen com 2 câmeras

## Passo a Passo para Configurar no Unity:

### 1. Criar TurnManager

- Crie um GameObject vazio chamado "TurnManager"
- Adicione o componente `TurnManager`

### 2. Criar GameUIManager

- Crie um GameObject vazio chamado "GameUIManager"
- Adicione o componente `GameUIManager`

### 3. Criar UI Canvas

- Clique direito na Hierarchy > UI > Canvas
- Dentro do Canvas, crie os seguintes elementos Text (TextMeshPro):

#### Para Player 1 (canto superior esquerdo):

- GameObject "Player1Panel" (posição: Anchor Left-Top)
  - Text (TMP): "Player1Name" (ex: posição X: 10, Y: -10)
  - Text (TMP): "Player1Gold" (ex: posição X: 10, Y: -40)

#### Para Player 2 (canto superior direito):

- GameObject "Player2Panel" (posição: Anchor Right-Top)
  - Text (TMP): "Player2Name" (ex: posição X: -10, Y: -10)
  - Text (TMP): "Player2Gold" (ex: posição X: -10, Y: -40)

#### Para Info de Turno (centro superior):

- Text (TMP): "TurnInfo" (posição: Anchor Top-Center)

### 4. Configurar GameUIManager

- Selecione o GameObject "GameUIManager"
- Arraste os textos criados para os campos correspondentes:
  - Player 1 Name Text → Player1Name
  - Player 1 Gold Text → Player1Gold
  - Player 2 Name Text → Player2Name
  - Player 2 Gold Text → Player2Gold
  - Turn Info Text → TurnInfo

### 5. Configurar Split Screen

- Crie um GameObject vazio chamado "SplitScreenManager"
- Adicione o componente `SplitScreenManager`
- Crie uma segunda câmera:
  - Duplique a câmera principal (Ctrl+D)
  - Renomeie para "Player2Camera"
- No SplitScreenManager:
  - Player 1 Camera → Main Camera
  - Player 2 Camera → Player2Camera
  - Enable Split Screen → marque como true

### 6. Ajustar CameraController (opcional)

Se você quiser que cada câmera tenha controles independentes, você pode:

- Desabilitar o CameraController na câmera principal
- OU criar uma versão que funcione apenas na área do jogador correspondente

## Como Funciona:

### Sistema de Ouro:

- Cada jogador começa com **10 de ouro**
- O custo da carta é igual ao seu **tier** (tier 1 = 1 ouro, tier 5 = 5 ouro)

### Sistema de Turnos:

- Começa com o **Jogador 1**
- Cada jogador pode comprar **apenas 1 carta por turno**
- Pressione **ESPAÇO** para passar a vez
- Quando passa a vez, o contador de cartas compradas reseta

### Fluxo de Jogo:

1. Jogador 1 clica em uma carta da loja
2. Sistema verifica se tem ouro e se pode comprar
3. Se sim, desconta o ouro e move a carta para a mão
4. Jogador 1 pressiona ESPAÇO para passar a vez
5. Agora é a vez do Jogador 2
6. Repete o processo

### Split Screen:

- **Metade esquerda**: Visão do Jogador 1
- **Metade direita**: Visão do Jogador 2
- Cada lado mostra o tabuleiro da perspectiva de seu jogador

## Próximos Passos Sugeridos:

1. Criar botão "End Turn" na UI ao invés de usar Espaço
2. Sistema de refresh da loja após ambos comprarem
3. Separar o tabuleiro em 2 áreas (uma para cada jogador)
4. Sistema de combate entre cartas
5. Condição de vitória

## Testes:

1. Entre no Play Mode
2. Veja no canto superior esquerdo: "Jogador 1" e "Ouro: 10"
3. Veja no canto superior direito: "Jogador 2" e "Ouro: 10"
4. Clique em uma carta - ela deve ser comprada e ir para a mão
5. Pressione ESPAÇO - deve passar para "Turno: Jogador 2"
6. Jogador 2 pode comprar uma carta
7. Observe o ouro diminuindo conforme compram cartas
