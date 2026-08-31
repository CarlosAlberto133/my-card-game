using System.Collections.Generic;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;

// ═══════════════ [DECKMODE] núcleo do MODO DECK (teste) ═══════════════
// Modo alternativo estilo TCG (Hearthstone): SEM loja e SEM ouro — cada
// jogador traz um deck de 30 cartas montado no lobby, compra 1 carta no
// início de cada turno seu e invoca pagando MANA em rampa (round 1 = 1,
// round 2 = 2... teto 10). Feitiços entram no deck (custo em mana = custo).
// Cartas de torre ficam GRATUITAS (1 das 3 por janela, como sempre).
//
// REGRA DE OURO DESTE ARQUIVO: o modo padrão NÃO passa por aqui. Todos os
// pontos de contato com o código existente são guardas de poucas linhas
// marcadas com [DECKMODE] — a lista completa está em MODO-DECK-REMOVER.md.
// Desativar o modo = esconder o toggle no lobby (uma linha).
//
// LOCKSTEP: o deck de cada jogador viaja UMA vez por room property com
// chave escopada pela seed (dk<seed>_p<n>, mesmo padrão da escolha de
// torre). Os DOIS clientes embaralham os DOIS decks com a seed e sacam
// pela mesma sequência dentro dos RPCs existentes (EndTurn) — nenhum RPC
// novo, nenhum sorteio fora de RPC.
public static class DeckMode
{
    // ── Regras do modo ──
    public const int DeckSize = 30;
    public const int MaxCopies = 3;       // cópias por carta no deck
    public const int MaxCopiesTier5 = 1;  // lendárias: 1 cópia
    public const int ManaCapMax = 10;     // rampa: min(round, 10)
    public const int OpeningHand = 4;     // mão inicial dos DOIS lados
    public const int DeckModeHandLimit = 10; // limite de mão neste modo (padrão: 8)
    public const string PrefKey = "deckmode_deck_v1";

    // ── O modo está ligado? ──
    // Lê a room property "mode" A CADA consulta (sem cache): funciona desde o
    // Awake da cena do jogo (a sala já existe antes do LoadGameScene) e zera
    // sozinho ao sair da sala. Sem sala = modo padrão, sempre.
    public static bool Active
    {
        get
        {
            if (!PhotonNetwork.inRoom || PhotonNetwork.room == null) return false;
            object v = PhotonNetwork.room.customProperties != null
                ? PhotonNetwork.room.customProperties["mode"] : null;
            return v != null && (int)v == 1;
        }
    }

    // ═══════════════ DECK SALVO (PlayerPrefs, montado no lobby) ═══════════════
    // Formato: entradas separadas por '|'. Unidade = cardName; feitiço = "s<id>".

    public static List<string> LoadSavedDeck()
    {
        string raw = PlayerPrefs.GetString(PrefKey, "");
        var list = new List<string>();
        if (!string.IsNullOrEmpty(raw))
            foreach (string e in raw.Split('|'))
                if (!string.IsNullOrWhiteSpace(e)) list.Add(e);
        return list;
    }

    public static void SaveDeck(List<string> deck)
    {
        PlayerPrefs.SetString(PrefKey, string.Join("|", deck));
        PlayerPrefs.Save();
    }

    // Entradas válidas: unidade do catálogo que não usa ouro, ou feitiço
    static bool EntryExists(string entry, out int tier)
    {
        tier = 1;
        if (entry.StartsWith("s"))
        {
            int id;
            if (!int.TryParse(entry.Substring(1), out id)) return false;
            SpellCard s = SpellCards.Get(id);
            if (s == null) return false;
            tier = (int)s.tier;
            return true;
        }
        foreach (var u in DeckCatalog.Units)
        {
            if (u.cardName != entry) continue;
            if (u.usesGold) return false; // sem ouro neste modo
            tier = u.tier;
            return true;
        }
        return false;
    }

    public static bool IsValidDeck(List<string> deck)
    {
        if (deck == null || deck.Count != DeckSize) return false;
        var counts = new Dictionary<string, int>();
        foreach (string e in deck)
        {
            int tier;
            if (!EntryExists(e, out tier)) return false;
            int c;
            counts.TryGetValue(e, out c);
            counts[e] = c + 1;
            int cap = tier >= 5 ? MaxCopiesTier5 : MaxCopies;
            if (counts[e] > cap) return false;
        }
        return true;
    }

    // Deck inicial determinístico e equilibrado (também é o deck do BOT e o
    // fallback de quem entra no modo sem deck salvo): curva de mana baixa,
    // as 4 classes representadas, sem cartas de ouro.
    public static List<string> StarterDeck()
    {
        var deck = new List<string>();

        // Por classe: 2 cópias das 2 primeiras T1, 2 cópias da 1ª T2, 1 da 1ª T3
        for (int cls = 0; cls < 4; cls++)
        {
            int t1Taken = 0;
            foreach (var u in DeckCatalog.Units)
            {
                if (u.classIdx != cls || u.usesGold) continue;
                if (u.tier == 1 && t1Taken < 2)
                {
                    deck.Add(u.cardName); deck.Add(u.cardName);
                    t1Taken++;
                }
            }
            foreach (var u in DeckCatalog.Units)
            {
                if (u.classIdx != cls || u.usesGold || u.tier != 2) continue;
                deck.Add(u.cardName); deck.Add(u.cardName);
                break;
            }
            foreach (var u in DeckCatalog.Units)
            {
                if (u.classIdx != cls || u.usesGold || u.tier != 3) continue;
                deck.Add(u.cardName);
                break;
            }
        }

        // Completa até 30 com T4 (1 cópia por classe, na ordem) e o que faltar
        foreach (var u in DeckCatalog.Units)
        {
            if (deck.Count >= DeckSize) break;
            if (u.usesGold || u.tier != 4) continue;
            if (deck.Contains(u.cardName)) continue;
            deck.Add(u.cardName);
        }
        foreach (var u in DeckCatalog.Units)
        {
            if (deck.Count >= DeckSize) break;
            if (u.usesGold || u.tier >= 5) continue;
            int copies = 0;
            foreach (string e in deck) if (e == u.cardName) copies++;
            if (copies < MaxCopies) deck.Add(u.cardName);
        }
        if (deck.Count > DeckSize) deck.RemoveRange(DeckSize, deck.Count - DeckSize);
        return deck;
    }

    // ═══════════════ SINCRONIZAÇÃO (início da partida) ═══════════════

    static int matchSeed = 0;
    static readonly List<string>[] decks = { null, null, null }; // [1] e [2], JÁ embaralhados
    static readonly int[] drawnCount = new int[3];
    static readonly int[] fatigue = new int[3];
    static bool published = false;
    static bool handsDealt = false;

    // Chamado quando a seed sincronizada é conhecida (e no restart, com a nova)
    public static void OnSeedKnown(int seed)
    {
        if (!Active) return;
        matchSeed = seed;
        decks[1] = decks[2] = null;
        drawnCount[1] = drawnCount[2] = 0;
        fatigue[1] = fatigue[2] = 0;
        published = false;
        handsDealt = false;

        List<string> mine = LoadSavedDeck();
        if (!IsValidDeck(mine))
        {
            Debug.Log("[DeckMode] Sem deck válido salvo — usando o deck inicial.");
            mine = StarterDeck();
        }

        if (PhotonNetwork.offlineMode || BotMode.Enabled)
        {
            // Treino: humano = P1, bot = P2 com o deck inicial. Tudo local.
            decks[1] = Shuffle(mine, 1);
            decks[2] = Shuffle(StarterDeck(), 2);
            Debug.Log($"[DeckMode] Treino: decks prontos (seed {seed})");
            return;
        }

        // Multiplayer: publica o MEU deck; o TickSync espera os dois chegarem
        int me = PhotonGameManager.Instance != null ? PhotonGameManager.Instance.myPlayerNumber : 1;
        Hashtable props = new Hashtable();
        props[DeckKey(me)] = string.Join("|", mine);
        PhotonNetwork.room.SetCustomProperties(props);
        published = true;
        Debug.Log($"[DeckMode] Deck local publicado ({DeckKey(me)})");
    }

    static string DeckKey(int player) { return $"dk{matchSeed}_p{player}"; }

    // Os dois decks já chegaram e foram embaralhados?
    public static bool DecksReady()
    {
        if (!Active) return false;
        if (decks[1] != null && decks[2] != null) return true;
        TickSync();
        return decks[1] != null && decks[2] != null;
    }

    // Poll das room properties (chamado pelo Update do GameUIManager e pelo
    // DecksReady). Determinístico: os DOIS clientes recebem as MESMAS props e
    // embaralham com a MESMA seed.
    public static void TickSync()
    {
        if (!Active || matchSeed == 0 || !published) return;
        if (decks[1] != null && decks[2] != null) return;
        if (PhotonNetwork.room == null || PhotonNetwork.room.customProperties == null) return;

        for (int p = 1; p <= 2; p++)
        {
            if (decks[p] != null) continue;
            object raw = PhotonNetwork.room.customProperties[DeckKey(p)];
            if (raw == null) continue;

            var list = new List<string>();
            foreach (string e in ((string)raw).Split('|'))
                if (!string.IsNullOrWhiteSpace(e)) list.Add(e);
            if (!IsValidDeck(list))
            {
                Debug.LogWarning($"[DeckMode] Deck do P{p} inválido — substituído pelo inicial (igual nos 2 clientes)");
                list = StarterDeck();
            }
            decks[p] = Shuffle(list, p);
            Debug.Log($"[DeckMode] Deck do P{p} pronto ({decks[p].Count} cartas)");
        }
    }

    // Fisher-Yates com System.Random próprio (seed + jogador): idêntico nos 2
    // clientes e não toca o stream global do UnityEngine.Random (lockstep)
    static List<string> Shuffle(List<string> deck, int player)
    {
        var list = new List<string>(deck);
        var rng = new System.Random(matchSeed * 131 + player * 17);
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            string tmp = list[i]; list[i] = list[j]; list[j] = tmp;
        }
        return list;
    }

    // ═══════════════ PARTIDA ═══════════════

    // Chamado pelo StartGame (dentro do fluxo de ready dos 2 clientes):
    // mão inicial de 4 para cada lado + mana do round 1 (1/1 para os dois)
    public static void OnMatchStart()
    {
        if (!Active || handsDealt) return;
        if (!DecksReady())
        {
            // Não deve acontecer (o botão Iniciar espera os decks), mas se
            // acontecer os 2 clientes falham IGUAL (props ainda não chegaram)
            Debug.LogError("[DeckMode] StartGame sem os decks prontos!");
            return;
        }
        handsDealt = true;

        TurnManager tm = TurnManager.Instance;
        if (tm != null)
        {
            tm.player1.manaCap = 1; tm.player1.mana = 1;
            tm.player2.manaCap = 1; tm.player2.mana = 1;
        }

        for (int i = 0; i < OpeningHand; i++) { DrawCard(1); DrawCard(2); }
        Debug.Log($"[DeckMode] Mãos iniciais distribuídas ({OpeningHand} cartas cada)");
    }

    // Chamado pelo EndTurn quando um turno COMEÇA (dentro do RPC, nos 2
    // clientes): rampa de mana + compra do turno
    public static void OnTurnStarted(int player, int round)
    {
        if (!Active) return;
        TurnManager tm = TurnManager.Instance;
        PlayerData p = tm != null ? tm.GetPlayer(player) : null;
        if (p == null) return;

        p.manaCap = Mathf.Min(round, ManaCapMax);
        p.mana = p.manaCap;
        DrawCard(player);
    }

    // Cartas restantes no deck (HUD)
    public static int Remaining(int player)
    {
        if (decks[player] == null) return 0;
        return Mathf.Max(0, decks[player].Count - drawnCount[player]);
    }

    // Compra 1 carta do topo do deck do jogador. Deck vazio = FADIGA:
    // dano crescente na própria torre (1, 2, 3...) — impede partida eterna
    public static void DrawCard(int player)
    {
        if (decks[player] == null) return;
        TurnManager tm = TurnManager.Instance;
        if (tm == null) return;

        if (drawnCount[player] >= decks[player].Count)
        {
            fatigue[player]++;
            PlayerData pd = tm.GetPlayer(player);
            pd.TakeDamage(fatigue[player]);
            Debug.Log($"[DeckMode] P{player} sem cartas: FADIGA {fatigue[player]} de dano na torre!");
            if (GameUIManager.Instance != null)
                GameUIManager.Instance.ShowDecisionPopup(
                    $"Deck do Jogador {player} vazio!\nFadiga: {fatigue[player]} de dano na torre.",
                    "Entendi", () => { }, "Fechar", () => { });
            if (pd.IsDefeated() && GameUIManager.Instance != null)
                GameUIManager.Instance.ShowVictoryScreen(player == 1 ? 2 : 1);
            return;
        }

        string entry = decks[player][drawnCount[player]];
        drawnCount[player]++;

        Card data = ResolveEntry(entry);
        if (data == null)
        {
            Debug.LogError($"[DeckMode] Carta '{entry}' do deck do P{player} não existe no catálogo do jogo — compra perdida");
            return;
        }
        GiveCardToHand(player, data);
    }

    // Resolve a entrada do deck contra a FONTE DA VERDADE: feitiço via
    // SpellCards, unidade por NOME no CardPool.allBaseCards
    static Card ResolveEntry(string entry)
    {
        if (entry.StartsWith("s"))
        {
            int id;
            if (int.TryParse(entry.Substring(1), out id)) return SpellCards.GetCard(id);
            return null;
        }
        CardPool pool = Object.FindObjectOfType<CardPool>();
        if (pool == null) return null;
        foreach (Card c in pool.allBaseCards)
            if (c != null && c.cardName == entry) return c;
        return null;
    }

    // Cria o objeto da carta e entrega na mão (mesmo estado do ExecuteBuy).
    // Mão cheia = a carta é QUEIMADA (descartada), estilo TCG
    static void GiveCardToHand(int player, Card data)
    {
        HandManager hand = null;
        foreach (HandManager hm in Object.FindObjectsOfType<HandManager>())
            if (hm.playerNumber == player) { hand = hm; break; }
        if (hand == null || CardManager.Instance == null) return;

        if (hand.IsHandFull())
        {
            Debug.Log($"[DeckMode] Mão do P{player} cheia — '{data.cardName}' foi queimada!");
            return;
        }

        GameObject obj = CardManager.Instance.SpawnCard(data,
            new Vector3(0f, CardDisplay.GroundY(CardDisplay.HandScale), player == 2 ? 29.5f : -29.5f));
        if (obj == null) return;

        CardDisplay cd = obj.GetComponent<CardDisplay>();
        if (cd == null) { Object.Destroy(obj); return; }

        cd.isInShop = false;
        cd.ownerPlayerNumber = player;
        cd.AssignHandManager(hand);
        cd.UpdateDisplay();

        if (hand.AddCardToHand(obj))
        {
            cd.isInHand = true;
            obj.transform.localScale = Vector3.one * CardDisplay.HandScale;

            // Carta do OPONENTE: este cliente só vê o verso
            if (PhotonNetwork.inRoom && PhotonGameManager.Instance != null &&
                player != PhotonGameManager.Instance.myPlayerNumber)
            {
                cd.SetFaceDown(true);
            }
        }
        else
        {
            Object.Destroy(obj);
        }
    }
}
