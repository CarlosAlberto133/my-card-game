using UnityEngine;
using System.Collections.Generic;

public class HandManager : MonoBehaviour
{
    [Header("Jogador")]
    public int playerNumber = 1; // 1 ou 2

    [Header("Configurações da Mão")]
    public float handYPosition = 1.5f;
    public float handZPosition = -12f; // Parte inferior da tela
    public float cardSpacing = 3f;
    public float cardScale = 1.5f;

    [Header("Limite de Cartas")]
    public int maxCardsInHand = 10;

    private List<GameObject> cardsInHand = new List<GameObject>();

    void Awake()
    {
        // Força por código (valor da cena estava desatualizado): cartas na mão
        // agora têm escala 2 (3.6 de largura), espaçamento 4 evita sobreposição
        cardSpacing = 4f;
        // Altura correta para a nova escala (a base da carta não afunda no chão)
        handYPosition = CardDisplay.GroundY(CardDisplay.HandScale);
        // Mão mais afastada do tabuleiro (cartas 2x maiores invadiam a visão do campo).
        // Cada jogador tem a mão do seu lado: P1 embaixo (-Z), P2 em cima (+Z)
        handZPosition = playerNumber == 2 ? 46f : -46f;
    }

    // Adiciona uma carta à mão
    public bool AddCardToHand(GameObject cardObject)
    {
        // Verifica se a mão está cheia
        if (cardsInHand.Count >= maxCardsInHand)
        {
            Debug.Log("Mão cheia! Máximo de cartas atingido.");
            return false;
        }

        // Adiciona a carta à lista
        cardsInHand.Add(cardObject);

        // Reorganiza todas as cartas na mão
        ArrangeCardsInHand();

        Debug.Log($"Carta adicionada à mão! Total: {cardsInHand.Count}/{maxCardsInHand}");
        return true;
    }

    // Remove uma carta da mão
    public void RemoveCardFromHand(GameObject cardObject)
    {
        if (cardsInHand.Contains(cardObject))
        {
            cardsInHand.Remove(cardObject);
            ArrangeCardsInHand();
            Debug.Log($"Carta removida da mão. Total: {cardsInHand.Count}");
        }
    }

    // Reorganiza as cartas na mão
    void ArrangeCardsInHand()
    {
        if (cardsInHand.Count == 0) return;

        // Calcula largura total
        float totalWidth = (cardsInHand.Count - 1) * cardSpacing;

        // Posição inicial centralizada
        Vector3 startPosition = new Vector3(-totalWidth / 2f, handYPosition, handZPosition);

        // Posiciona cada carta
        for (int i = 0; i < cardsInHand.Count; i++)
        {
            GameObject card = cardsInHand[i];
            if (card != null)
            {
                Vector3 targetPosition = startPosition + new Vector3(i * cardSpacing, 0, 0);

                // Move suavemente para a posição
                StartCoroutine(MoveCardToPosition(card, targetPosition));
            }
        }
    }

    // Move a carta suavemente para uma posição
    System.Collections.IEnumerator MoveCardToPosition(GameObject card, Vector3 targetPosition)
    {
        float duration = 0.3f;
        float elapsed = 0f;
        Vector3 startPosition = card.transform.position;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Interpolação suave
            card.transform.position = Vector3.Lerp(startPosition, targetPosition, t);

            yield return null;
        }

        card.transform.position = targetPosition;
    }

    // Retorna o número de cartas na mão
    public int GetCardCount()
    {
        return cardsInHand.Count;
    }

    // Retorna o índice de uma carta na mão (-1 se não está)
    public int GetCardIndex(GameObject cardObject)
    {
        return cardsInHand.IndexOf(cardObject);
    }

    // Retorna a carta da mão em um índice específico
    public GameObject GetCardAtIndex(int index)
    {
        if (index < 0 || index >= cardsInHand.Count) return null;
        return cardsInHand[index];
    }

    // Verifica se a mão está cheia
    public bool IsHandFull()
    {
        return cardsInHand.Count >= maxCardsInHand;
    }

    // Limpa todas as cartas da mão
    public void ClearHand()
    {
        foreach (GameObject card in cardsInHand)
        {
            if (card != null)
            {
                Destroy(card);
            }
        }
        cardsInHand.Clear();
    }

    // Retorna todas as cartas na mão
    public List<GameObject> GetCardsInHand()
    {
        return new List<GameObject>(cardsInHand);
    }

    // Aumenta escudo de todas as cartas na mão (Tank 5)
    public void BoostHandShield(int targetPlayerNumber, int amount)
    {
        // Verifica se este HandManager é do jogador correto
        if (playerNumber != targetPlayerNumber) return;

        int boostedCount = 0;
        foreach (GameObject cardObject in cardsInHand)
        {
            CardDisplay cardDisplay = cardObject.GetComponent<CardDisplay>();
            if (cardDisplay != null)
            {
                cardDisplay.currentShield += amount;
                cardDisplay.UpdateDisplay();
                boostedCount++;
            }
        }

        Debug.Log($"[TankEffect5] Boosted shield of {boostedCount} cards in hand by {amount}");
    }
}
