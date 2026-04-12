using UnityEngine;
using System.Collections.Generic;

public class HandManager : MonoBehaviour
{
    [Header("Configurações da Mão")]
    public float handYPosition = 1.5f;
    public float handZPosition = -12f; // Parte inferior da tela
    public float cardSpacing = 3f;
    public float cardScale = 1.5f;

    [Header("Limite de Cartas")]
    public int maxCardsInHand = 10;

    private List<GameObject> cardsInHand = new List<GameObject>();

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
}
