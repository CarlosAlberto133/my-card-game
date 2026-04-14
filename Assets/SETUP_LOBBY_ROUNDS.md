# Sistema de Lobby e Rounds - Instruções de Configuração

## Novos Recursos Adicionados:

1. **Sistema de Lobby** - Aguarda ambos jogadores clicarem "Iniciar Partida"
2. **Botão "Iniciar Partida"** - Visível apenas no lobby
3. **Botão "Passar a Vez"** - Visível apenas durante o jogo
4. **Sistema de Rounds** - A cada round completo, 5 novas cartas aparecem
5. **Movimentação das cartas** - Vão para o lado direito do tabuleiro quando o jogo inicia

## Scripts Modificados:

### Arquivos Novos/Atualizados:

- **TurnManager_New.cs** - Versão atualizada com sistema de lobby e rounds
- **CardManager.cs** - Adicionado OnGameStart() e RefreshShop()
- **GameUIManager.cs** - Controle de botões e display de rounds
- **PlayerData.cs** - Construtor adicional que aceita string

## Configuração Passo a Passo:

### 1. Substituir TurnManager

1. No Unity, localize o arquivo `TurnManager.cs` em Assets/Scripts/
2. **Delete o arquivo antigo** `TurnManager.cs`
3. **Renomeie** `TurnManager_New.cs` para `TurnManager.cs`
4. Aguarde o Unity recompilar

### 2. Adicionar UI Elements para Botões

No Canvas existente, adicione:

#### Botão "Iniciar Partida" (lado esquerdo):

1. Clique direito no Canvas > UI > Button - TextMeshPro
2. Renomeie para "StartGameButton"
3. Posicione no **lado esquerdo** da tela:
   - Anchor: Left-Middle
   - Pos X: 150
   - Pos Y: 0
   - Width: 200
   - Height: 60
4. No Text (TMP) filho do botão:
   - Mude o texto para: **"INICIAR PARTIDA"**
   - Font Size: 18
   - Alignment: Center

#### Botão "Passar a Vez" (mesmo local):

1. Duplique o "StartGameButton" (Ctrl+D)
2. Renomeie para "EndTurnButton"
3. No Text (TMP) filho:
   - Mude o texto para: **"PASSAR A VEZ"**

### 3. Adicionar Text para Round

No Canvas, crie:

1. Clique direito > UI > Text - TextMeshPro
2. Renomeie para "RoundText"
3. Posicione no **topo central**:
   - Anchor: Top-Center
   - Pos X: 0
   - Pos Y: -80 (abaixo do TurnInfo)
   - Width: 300
   - Height: 50
4. Configure o texto:
   - Text: "ROUND 1"
   - Font Size: 32
   - Alignment: Center
   - Font Style: Bold
   - Color: Amarelo ou dourado

### 4. Configurar GameUIManager

Selecione o GameObject "GameUIManager" e arraste:

- **Start Game Button** → StartGameButton
- **End Turn Button** → EndTurnButton
- **Round Text** → RoundText

### 5. Configurar CardManager

Selecione o GameObject "CardManager" (ou onde está o script):

1. No Inspector, ajuste:
   - **Center Position**: (0, 1.5, 0) - posição inicial no lobby
   - **Shop Position**: (10, 1.5, 0) - posição à direita durante o jogo
   - **Number Of Cards**: 5
   - **Card Spacing**: 4

## Como Funciona:

### Estado: LOBBY

1. **Cartas aparecem** no centro da tela (Center Position)
2. **Botão visível**: "INICIAR PARTIDA"
3. **Round Text mostra**: "LOBBY"
4. **Turn Info mostra**: "Aguardando: Jogador X"

### Iniciando o Jogo:

1. **Jogador 1** clica em "Iniciar Partida"
   - Mensagem: "Jogador 1 está pronto!"
   - Turno passa para Jogador 2
2. **Jogador 2** clica em "Iniciar Partida"
   - Mensagem: "Jogador 2 está pronto!"
   - **Jogo inicia automaticamente**

### Quando o Jogo Inicia:

1. ✅ Estado muda para **PLAYING**
2. ✅ **Round 1** começa
3. ✅ Cartas se movem para o **lado direito** (Shop Position)
4. ✅ 5 novas cartas aparecem na loja
5. ✅ Botão "Iniciar Partida" **desaparece**
6. ✅ Botão "Passar a Vez" **aparece**
7. ✅ Jogadores começam com **10 de ouro** cada

### Sistema de Rounds:

**Round 1:**

- Jogador 1 joga (pode comprar 1 carta)
- Jogador 1 clica "Passar a Vez"
- Jogador 2 joga (pode comprar 1 carta)
- Jogador 2 clica "Passar a Vez"

**Round 2 inicia:**

- ✅ Round Text muda para "ROUND 2"
- ✅ **5 novas cartas** aparecem na loja (antigas são destruídas)
- ✅ Volta para Jogador 1

**Round 3, 4, 5...:**

- Processo se repete infinitamente
- A cada round completo (J1 → J2 → J1), novas cartas

### Fluxo de Compra:

Durante cada turno:

1. Jogador pode comprar **1 carta** (se tiver ouro)
2. Carta vai para a **mão do jogador**
3. Ouro é descontado
4. Jogador clica "Passar a Vez"
5. Contador de cartas compradas **reseta**

## Detalhes Técnicos:

### GameState Enum:

```csharp
Lobby   // Aguardando jogadores
Playing // Jogo em andamento
```

### Variáveis do TurnManager:

- `gameState` - Estado atual
- `currentRound` - Round atual (0 no lobby, 1+ no jogo)
- `player1Ready` / `player2Ready` - Quem está pronto
- `currentPlayerNumber` - Jogador atual (1 ou 2)

### Métodos Principais:

- `OnPlayerReadyToStart(int playerNumber)` - Chamado quando jogador clica "Iniciar"
- `StartGame()` - Inicia o jogo quando ambos prontos
- `EndTurn()` - Passa a vez e verifica se completa round
- `CardManager.OnGameStart()` - Move cartas para direita
- `CardManager.RefreshShop()` - Spawna 5 novas cartas

## Testes:

### Teste 1: Lobby

1. Entre no Play Mode
2. Veja cartas **no centro**
3. Veja botão "INICIAR PARTIDA" à esquerda
4. Veja "LOBBY" no topo

### Teste 2: Iniciar Jogo

1. Clique "Iniciar Partida" (como J1)
2. Veja mensagem no Console: "Jogador 1 está pronto!"
3. Aguarde estar como J2
4. Clique "Iniciar Partida" novamente
5. Veja:
   - Cartas **moverem para a direita**
   - "ROUND 1" aparecer
   - Botão mudar para "PASSAR A VEZ"

### Teste 3: Sistema de Rounds

1. Como J1, compre uma carta
2. Clique "Passar a Vez"
3. Como J2, compre uma carta
4. Clique "Passar a Vez"
5. Veja:
   - "ROUND 2" aparecer
   - **5 novas cartas** surgirem
   - Voltar para J1

### Teste 4: Cartas por Round

1. Anote as 5 cartas do Round 1
2. Complete o round (J1 → J2 → J1)
3. No Round 2, veja que são **5 cartas diferentes**
4. Repita para Round 3, 4, etc.

## Problemas Comuns:

### "Botões não aparecem"

- Verifique se os botões estão no Canvas
- Confirme que GameUIManager tem as referências corretas
- Veja se os botões têm componente Button

### "Cartas não movem para a direita"

- Verifique Shop Position no CardManager (deve ser ~10, 1.5, 0)
- Confirme que CardManager.Instance existe

### "Rounds não mudam"

- Veja no Console se há mensagens de "Round X iniciado"
- Confirme que TurnManager.Instance.currentRound está incrementando
- Verifique se GameUIManager.roundText está atribuído

### "Novas cartas não aparecem"

- Confirme que CardPool tem cartas disponíveis
- Veja se CardManager.RefreshShop() está sendo chamado
- Verifique no Console: "CardManager: Refresh da loja..."

## Próximos Passos:

- [ ] Ajustar posição exata das cartas na loja
- [ ] Adicionar animação de transição das cartas
- [ ] Limitar número de rounds (win condition)
- [ ] Sistema de combate entre cartas
- [ ] Efeitos visuais para mudança de round
