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

#### Para Round Info (centro superior, abaixo de TurnInfo):

- Text (TMP): "RoundText" (posição: Anchor Top-Center, Y: -80)
  - Font Size: 32
  - Style: Bold
  - Color: Amarelo/Dourado

#### Botão Iniciar Partida (lado esquerdo):

- Button (TMP): "StartGameButton" (posição: Anchor Left-Middle)
  - Pos X: 150, Y: 0
  - Width: 200, Height: 60
  - Text: "INICIAR PARTIDA"

#### Botão Passar a Vez (mesmo local do botão iniciar):

- Button (TMP): "EndTurnButton" (posição: Anchor Left-Middle)
  - Pos X: 150, Y: 0
  - Width: 200, Height: 60
  - Text: "PASSAR A VEZ"

### 4. Configurar GameUIManager

- Selecione o GameObject "GameUIManager"
- Arraste os textos criados para os campos correspondentes:
  - Player 1 Name Text → Player1Name
  - Player 1 Gold Text → Player1Gold
  - Player 2 Name Text → Player2Name
  - Player 2 Gold Text → Player2Gold
  - Turn Info Text → TurnInfo
  - Round Text → RoundText
  - Start Game Button → StartGameButton
  - End Turn Button → EndTurnButton

### 5. Configurar HandManagers para cada jogador

**IMPORTANTE**: Você precisa de **2 HandManagers** separados, um para cada jogador!

- Localize o GameObject "HandManager" existente (se houver)
- Renomeie-o para "HandManager_Player1"
- No Inspector, configure:
  - **Player Number**: 1
  - **Hand Z Position**: -12 (área inferior esquerda)
- Duplique o "HandManager_Player1" (Ctrl+D)
- Renomeie a cópia para "HandManager_Player2"
- No Inspector, configure:
  - **Player Number**: 2
  - **Hand Z Position**: -12 (área inferior direita)

**Se não existir HandManager ainda:**

- Crie um GameObject vazio chamado "HandManager_Player1"
- Adicione o componente `HandManager`
- Configure Player Number = 1
- Crie outro GameObject vazio chamado "HandManager_Player2"
- Adicione o componente `HandManager`
- Configure Player Number = 2

### 6. Configurar Split Screen

- Crie um GameObject vazio chamado "SplitScreenManager"
- Adicione o componente `SplitScreenManager`
- Crie uma segunda câmera:
  - Duplique a câmera principal (Ctrl+D)
  - Renomeie para "Player2Camera"
- No SplitScreenManager:
  - Player 1 Camera → Main Camera
  - Player 2 Camera → Player2Camera
  - Enable Split Screen → marque como true

### 7. Ajustar CameraController (opcional)

Se você quiser que cada câmera tenha controles independentes, você pode:

- Desabilitar o CameraController na câmera principal
- OU criar uma versão que funcione apenas na área do jogador correspondente

## Como Funciona:

### Sistema de Lobby:

- **Antes do Jogo**: Jogadores veem cartas no centro da tela
- **Botão "Iniciar Partida"**: Ambos jogadores devem clicar
- **Jogador 1** clica → aguarda Jogador 2
- **Jogador 2** clica → jogo inicia automaticamente
- **Quando inicia**: Cartas movem para o lado direito do tabuleiro

### Sistema de Ouro:

- Cada jogador começa com **10 de ouro**
- O custo da carta é igual ao seu **tier** (tier 1 = 1 ouro, tier 5 = 5 ouro)

### Sistema de Turnos:

- Começa com o **Jogador 1** (após ambos iniciarem)
- Cada jogador pode comprar **apenas 1 carta por turno**
- Clique em **"Passar a Vez"** para passar (ou ESPAÇO temporariamente)
- Quando passa a vez, o contador de cartas compradas reseta

### Sistema de Rounds:

- **Round 1**: Inicia quando o jogo começa
- **Round completo**: Quando J1 passa vez → J2 joga → J2 passa vez → volta J1
- **A cada novo round**: 5 novas cartas aparecem na loja
- **Display**: Mostra "ROUND X" no topo da tela

### Fluxo de Jogo:

**LOBBY:**

1. Ambos jogadores veem cartas no centro
2. Jogador 1 clica "Iniciar Partida" → aguarda
3. Jogador 2 clica "Iniciar Partida" → jogo inicia

**ROUND 1:**

1. Cartas movem para o lado direito
2. Jogador 1 pode comprar 1 carta (se tiver ouro)
3. Jogador 1 clica "Passar a Vez"
4. Jogador 2 pode comprar 1 carta
5. Jogador 2 clica "Passar a Vez"

**ROUND 2:**

1. 5 novas cartas aparecem (antigas desaparecem)
2. Repete o processo do Round 1
3. Continua infinitamente...

**IMPORTANTE:** Cada jogador tem sua própria mão! As cartas compradas pelo Jogador 1 vão para HandManager_Player1, e as do Jogador 2 vão para HandManager_Player2.

### Split Screen:

- **Metade esquerda**: Visão do Jogador 1
- **Metade direita**: Visão do Jogador 2
- Cada lado mostra o tabuleiro da perspectiva de seu jogador

## Próximos Passos Sugeridos:

1. ✅ Sistema de lobby com botão "Iniciar Partida"
2. ✅ Botão "Passar a Vez" na UI
3. ✅ Sistema de rounds com refresh de 5 cartas
4. ✅ Movimentação das cartas para lado direito ao iniciar
5. Sistema de combate entre cartas
6. Condição de vitória (rounds limitados ou HP)
7. Animações de transição
8. Efeitos visuais para mudança de round

## Testes:

### Teste 1: Lobby

1. Entre no Play Mode
2. Veja cartas **no centro da tela**
3. Veja botão "INICIAR PARTIDA" no lado esquerdo
4. Veja "LOBBY" no topo
5. Veja "Aguardando: Jogador 1"

### Teste 2: Iniciar Partida

1. Clique em "Iniciar Partida" (como Jogador 1)
2. Veja mensagem: "Jogador 1 está pronto!"
3. Agora mostra "Aguardando: Jogador 2"
4. Clique "Iniciar Partida" novamente (como Jogador 2)
5. Observe:
   - Cartas **movem para o lado direito** do tabuleiro
   - Aparece "ROUND 1"
   - Botão muda para "PASSAR A VEZ"
   - Ouro de ambos jogadores = 10

### Teste 3: Compra e Turnos

1. **Jogador 1:** Clique em uma carta - ela deve ir para a mão esquerda
2. Veja ouro diminuir
3. Clique "Passar a Vez"
4. Veja "Turno: Jogador 2"
5. **Jogador 2:** Clique em outra carta - ela deve ir para a mão direita
6. Clique "Passar a Vez"

### Teste 4: Sistema de Rounds

1. Complete um turno completo (J1 → J2 → volta J1)
2. Observe:
   - Display muda para "ROUND 2"
   - **5 novas cartas** aparecem (antigas somem)
3. Repita para verificar Round 3, 4, etc.

### Teste 5: Separação de Mãos

1. Veja no canto superior esquerdo: "Jogador 1" e "Ouro: X"
2. Veja no canto superior direito: "Jogador 2" e "Ouro: Y"
3. Cada jogador só pode comprar 1 carta por turno
4. As cartas ficam em mãos separadas (Player1 ≠ Player2)
