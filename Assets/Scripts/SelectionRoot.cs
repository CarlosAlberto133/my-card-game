using UnityEngine;

// Marca o objeto-pai de uma peça feita de vários pedaços (o fundo do tabuleiro
// são dezenas de lajotas). Com [SelectionBase], clicar em qualquer pedaço na
// Scene view seleciona o PAI — e Delete leva a peça inteira.
//
// Sem isto acontece o que já aconteceu com as tochas: o clique cai no pedaço,
// Delete apaga uma lajota só e fica um buraco no meio do fundo.
//
// Arquivo próprio de propósito: MonoBehaviour fora de um arquivo com o mesmo
// nome vira Missing Script ao recarregar a cena (ver FlameLook.cs).
[SelectionBase]
public class SelectionRoot : MonoBehaviour
{
}
