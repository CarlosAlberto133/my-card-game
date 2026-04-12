using UnityEngine;
using TMPro;

public class GameUIManager : MonoBehaviour
{
    [Header("Player 1 UI")]
    public TextMeshProUGUI player1NameText;
    public TextMeshProUGUI player1GoldText;

    [Header("Player 2 UI")]
    public TextMeshProUGUI player2NameText;
    public TextMeshProUGUI player2GoldText;

    [Header("Turn Info")]
    public TextMeshProUGUI turnInfoText;

    void Update()
    {
        if (TurnManager.Instance == null) return;

        // Atualiza UI do Jogador 1
        if (player1NameText != null)
        {
            player1NameText.text = TurnManager.Instance.player1.playerName;
        }
        if (player1GoldText != null)
        {
            player1GoldText.text = $"Ouro: {TurnManager.Instance.player1.gold}";
        }

        // Atualiza UI do Jogador 2
        if (player2NameText != null)
        {
            player2NameText.text = TurnManager.Instance.player2.playerName;
        }
        if (player2GoldText != null)
        {
            player2GoldText.text = $"Ouro: {TurnManager.Instance.player2.gold}";
        }

        // Atualiza informação de turno
        if (turnInfoText != null)
        {
            PlayerData currentPlayer = TurnManager.Instance.GetCurrentPlayer();
            turnInfoText.text = $"Turno: {currentPlayer.playerName}\nCartas compradas: {currentPlayer.cardsBoughtThisTurn}/1\n(Pressione ESPAÇO para passar)";
        }
    }
}
