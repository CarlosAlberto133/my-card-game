using UnityEngine;

// Marca o ENVELOPE de uma peça de luz (Tocha, Lamparina, Fogueira, Cristal...).
//
// O que ele faz é uma coisa só, e é [SelectionBase]: clicar em qualquer filho
// na Scene view (o modelo, a chama) seleciona o envelope inteiro. Sem isto o
// clique cai no modelo, Delete apaga só a malha e sobra um envelope invisível
// com chama e luz de 90 flutuando no lugar — foram as "luzinhas espalhadas"
// de 04/set/2026: nove tochas e lamparinas "apagadas" que continuavam acesas.
//
// Arquivo próprio de propósito: MonoBehaviour fora de um arquivo com o mesmo
// nome vira Missing Script ao recarregar a cena (ver FlameLook.cs).
[SelectionBase]
public class LightPiece : MonoBehaviour
{
}
