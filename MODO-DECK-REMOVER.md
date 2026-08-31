# MODO DECK (teste) — mapa completo pra desativar ou remover

O modo deck é um modo de jogo EXPERIMENTAL (estilo Hearthstone: deck de 30,
sem loja, sem ouro, mana em rampa 1→10, compra 1 carta por turno). Foi
construído pra ser removível sem tocar no modo padrão.

## Desativar (1 minuto, reversível)

Comente estas DUAS linhas — o modo some do jogo e todo o resto fica inerte
(nenhuma partida consegue ligar a room property "mode"):

1. `Assets/Scripts/PhotonLobbyManager.cs` → `DeckBuilderUI.AddEntryButton(canvas);`
2. `Assets/Scripts/LobbyUI.cs` → o bloco de 4 linhas do toggle "Modo de jogo"
   no `BuildHostPanel` (e, se quiser, o bloco `botModePadrao/botModeDeck` no
   popup do treino).

## Remover de vez

**Apagar os 4 arquivos exclusivos do modo (+ .meta):**
- `Assets/Scripts/DeckMode.cs` — núcleo (regras, sync, embaralho, compra, fadiga)
- `Assets/Scripts/DeckBuilderUI.cs` — construtor de decks do lobby
- `Assets/Scripts/DeckCatalog.cs` — catálogo estático das 80 unidades
- `MODO-DECK-REMOVER.md` — este arquivo

**Remover as guardas nos arquivos existentes** — todas marcadas com o
comentário `[DECKMODE]` (basta `grep -rn "\[DECKMODE\]" Assets/Scripts`):

| Arquivo | O que a guarda faz |
|---|---|
| `LobbyUI.cs` | toggles de modo (host + treino) + `SelectedGameMode`/`SelectedBotGameMode` |
| `PhotonLobbyManager.cs` | `props["mode"]` (2 lugares) + botão do construtor |
| `PhotonGameManager.cs` | `DeckMode.OnSeedKnown(seed)` no ApplySeedAndSpawnShop e no DoRestart |
| `CardManager.cs` | `SpawnRandomCards` vira no-op no modo deck (sem loja) |
| `TurnManager.cs` | `OnMatchStart` (mãos iniciais + mana 1/1) no StartGame; `OnTurnStarted` (rampa + compra) no EndTurn |
| `HandManager.cs` | limite de mão 10 (padrão: 8) |
| `TowerSystem.cs` | carta de torre gratuita (custo 0) |
| `TowerMagicShopUI.cs` | rótulos "grátis" + botão sempre habilitado |
| `GameUIManager.cs` | `TickSync` no Update; linha "Deck: N" no lugar do ouro; esconde reset da loja; trava o Iniciar até os decks sincronizarem |
| `BotController.cs` | bot pega carta de torre sem checar ouro |
| `CardDisplay.cs` | `AssignHandManager` (setter usado pela compra do baralho) |
| `PlayerData.cs` | campo `manaCap` (no padrão é sempre 6 — comportamento idêntico ao anterior) |

⚠️ `PlayerData.manaCap` é a única guarda que NÃO deve ser removida às cegas:
o HUD (`GameUIManager`) e o `ResetTurn` a usam. Se remover o modo, pode
substituir `manaCap` por `MaxMana` nesses dois pontos e apagar o campo.

**PlayerPrefs usados** (limpar se quiser): `deckmode_deck_v1`.

## Regras do modo (pra referência de teste)

- Deck: 30 cartas · máx. 3 cópias (tier 5: 1) · sem as 6 healers de ouro
  (Esmoleira, Mecenas, Provedora, Tesoureira, Guardiã do Cofre, Benfeitora)
- Feitiços entram no deck; custo em MANA = custo em ouro deles
- Mana: round 1 = 1, round 2 = 2... teto 10 (invocar custa o tier)
- Mão inicial: 4 pra cada · compra 1 no início de CADA turno · mão máx. 10
  (carta comprada com mão cheia é QUEIMADA)
- Deck vazio: FADIGA — 1, 2, 3... de dano na própria torre por compra
- Sem loja, sem ouro; cartas de torre GRATUITAS (1 das 3 por janela)
- Sem deck salvo válido → deck inicial automático (o mesmo do bot)
