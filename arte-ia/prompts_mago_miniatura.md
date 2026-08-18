# Prompts das cartas MAGO — **ESTILO MINIATURA DE RPG DE MESA**

> Mesmo estilo que já venceu nos tanques (`prompts_tank_miniatura.md`): **diorama de
> miniaturas low-poly** em cima da mesa de jogo, luz quente de vela, fundo escuro de
> taverna. As 21 artes de tank já estão no jogo neste visual — **o estilo está decidido**,
> aqui é só aplicar.
>
> São **20 cartas de mago + Arcanor**, o lendário da tríade que não sai do baralho. 21 no
> total, igual aos tanques.

> 🧪 **Comece pelos 3 testes** marcados com 🧪 (Evocador, Usurpador e Arcanor). São os
> extremos da classe: o mais simples e sem efeito, o que precisa mostrar um efeito de roubo
> sem colocar a vítima em cena, e o mais épico. Se os três passarem, o baralho inteiro passa.

---

## 🎲 BLOCO DE ESTILO (cole SEMPRE antes da descrição da carta)

Igual ao dos tanques, de propósito — **a mesa, a luz de vela e o enquadramento são a cola
que faz as 4 classes parecerem a mesma coleção**. Só muda o terreno da base e a cor do
efeito.

```
Render 3D estilizado de uma MINIATURA DE RPG DE MESA pintada à mão, fotografada
de pertinho em cima da mesa de jogo.

Estilo: low-poly caprichado com toon shading suave — faces chanfradas visíveis,
superfícies foscas de tinta acrílica, cores saturadas e alegres, contornos macios,
zero textura fotográfica. Personagem CHIBI: cabeça grande, corpo baixinho de umas
3 cabeças de altura, mãos e cajado exagerados, rosto simplificado com traços
mínimos, sem detalhe realista de pele — muitas vezes o rosto fica na sombra do
capuz e só aparecem os olhos.

A miniatura está em pé sobre um pedaço de terreno low-poly (mosaico de pedra
polida, lajota com runas gravadas ou cristal) apoiado nas tábuas escuras de
madeira de uma mesa de taverna. Ao redor, fora de foco, pedacinhos do cenário de
mesa: barris, arvorezinhas cônicas low-poly, moedas, um baú, um dado, uma
lanterna acesa.

Iluminação: luz quente e aconchegante de vela/lanterna vindo do lado, sombra
suave projetada na mesa, fundo escuro em degradê com bokeh laranja de tochas. A
magia do personagem é uma SEGUNDA fonte de luz, fria (ciano ou roxo), que bate
no rosto e nas mãos dele por baixo e briga com a luz quente do ambiente. A
miniatura é a coisa mais iluminada do quadro.

Câmera baixa, quase na altura da mesa, olhando levemente de baixo para cima para
a miniatura parecer imponente. Profundidade de campo: fundo bem desfocado.
Enquadramento QUADRADO 1024x1024, personagem centralizado e grande no quadro.

IMPORTANTE: é fofo e cartunesco, NÃO é realista, NÃO é fotografia de uma
miniatura de verdade, NÃO é pintura 2D, NÃO é pixel art, NÃO é cosplay. Sem
texto, sem letras, sem números, sem moldura de carta, sem logotipo, sem marca
d'água, sem borda preta.

A miniatura desta vez é:
```

---

## ⚠️ As 3 regras que a classe MAGO exige

1. **Todo mago é um roupão com capuz.** Esse é o problema da classe inteira: 20 túnicas
   azuis viram 20 cartas iguais. O que separa um do outro são **4 itens que aparecem em
   TODO prompt abaixo, sempre nesta ordem**:

   | item         | opções                                                                               |
   | ------------ | ------------------------------------------------------------------------------------ |
   | **cabeça**   | capuz baixado / capuz fundo sem rosto / chapéu pontudo / tiara / careca / máscara    |
   | **foco**     | cajado / varinha / orbe flutuante / livro / pergaminho / mãos nuas                   |
   | **traje**    | túnica de lã crua / roupão longo / manto com estola / armadura leve de tecido        |
   | **elemento** | fogo laranja / gelo ciano / raio branco-azulado / arcano roxo / corrosão verde-ácida |

   Se dois magos batem em **três** desses quatro, refaça um.

2. **A cor do elemento é livre, a do traje não.** O traje e a base ficam no azul/roxo/ciano
   da classe — é o que diz "isto é um mago". O VFX é que carrega a cor do efeito (o fogo do
   Pirotécnico é laranja mesmo). Assim o baralho tem unidade e as cartas ainda se distinguem
   de longe.

3. **UM personagem por imagem, sempre.** _(regra revista na geração dos magos)_ A tentação é
   colocar uma segunda miniatura em cena quando o efeito é "no outro" — mas a mesma arte
   depois vira o **modelo 3D** da carta, e aí duas figuras no quadro atrapalham. Então o
   efeito tem que se ler **no próprio mago**: em vez de mostrar o inimigo perdendo poder,
   mostre o **braço do Usurpador inchado de luz**; em vez do healer atrás, mostre a **cura
   verde entrando pelas costas e saindo azul pelas mãos**; em vez do inimigo perdendo os
   bônus, mostre as **fitas douradas se rasgando no ar à frente da mão**. A causa fica de
   fora, a consequência fica no boneco.

## 🥇 O truque da consistência

O primeiro mago você gera **anexando uma arte de tank já aprovada** (a Vanguarda é a mais
neutra) com este texto:

```
Use EXATAMENTE o mesmo estilo de render, nível de low-poly, paleta de mesa,
temperatura de luz, tipo de madeira e enquadramento da imagem anexada. Não mude o
estilo. Apenas troque a miniatura por:
<descrição da nova carta>
```

Assim que **um mago** sair perfeito, ele vira a carta mestra da classe e passa a ser a
imagem anexada nos outros 20 — é ele que trava o formato do capuz e o tom de azul.

---

## 🚨 A armadilha desta classe: mago genérico = personagem de franquia

Nos tanques o problema foi o brasão da Alliance. Aqui é pior, porque **mago velho de chapéu
pontudo tem dono**. Estes cinco vão puxar cópia se você não negar explicitamente:

| carta             | o que ele vai tentar entregar | negativa que entra no prompt                                                                                               |
| ----------------- | ----------------------------- | -------------------------------------------------------------------------------------------------------------------------- |
| **Arquimago**     | Gandalf / Dumbledore          | sem barba branca comprida até o peito, sem chapéu cinza de aba mole, sem cajado de madeira torta                           |
| **Lorde do Gelo** | Lich King (WoW)               | **não é morto-vivo**, sem caveira, sem elmo de coroa de espinhos, sem armadura preta de placas, sem olhos vazios brilhando |
| **Eletromante**   | Thor / Raiden                 | sem martelo, sem chapéu de palha cônico, sem armadura de samurai                                                           |
| **Usurpador**     | Doutor Estranho               | sem capa vermelha, sem cavanhaque, sem mandala circular de faíscas laranja                                                 |
| **Metamorfo**     | Mística (X-Men)               | sem pele azul escamosa, sem corpo nu, sem olhos amarelos                                                                   |

Fecha **todo** prompt de mago com esta linha:

```
Personagem 100% original. NÃO copie nenhum mago de filme, livro, série ou jogo
existente. Sem símbolos de franquias reais.
```

---

## 🔵 TIER 1 — aprendizes. Base pequena de mosaico simples, túnica de pano, magia mínima

**🧪 Evocador** (3/0/4) — _TESTE 1 — o estilo puro, sem efeito nenhum_

> _Efeito: NENHUM. É a carta vanilla da classe — e é isso que precisa ficar claro na arte._
>
> Um estudante de magia baixinho, **capuz baixado nos ombros mostrando o rosto jovem e
> concentrado**, **cajado simples de madeira lisa sem cristal nenhum na ponta**, **túnica de
> lã crua azul-acinzentada** puída na barra, cinto de couro com um saquinho de componentes e
> um livro amarrado. Postura firme, cajado plantado no chão com as duas mãos. Base de
> mosaico de pedra simples, sem runa. **Nenhum brilho mágico, nenhuma runa acesa, nenhuma
> partícula, nenhuma luz fria** — a única luz nele é a da vela da mesa. Ele é o mago mais
> simples e humano da coleção, e é isso que o identifica. Paleta de azul-acinzentado, lã
> bege e couro marrom.

**Fagulha** (1/0/4)

> _Efeito: ao entrar em campo, causa 1 de dano a todos os inimigos da coluna à frente._
>
> Uma **aprendiza pequenina e sardenta**, a menor da coleção, **cabelo curto espetado, sem capuz**, **sem cajado — mãos nuas**, **túnica azul curta** com as mangas queimadas e chamuscadas nas pontas. Ela **estalou os dedos** Base de mosaico com uma marca de queimado. Paleta de azul-marinho, laranja-fogo e cinza de fuligem.

**Criomante** (1/0/3)

> _Efeito: congela um monstro inimigo por 1 turno._
>
> Um mago magro de **capuz fundo branco-azulado com a sombra cobrindo os olhos**, **mãos
> nuas erguidas com as palmas abertas para a frente**, **manto comprido azul-claro** com a
> barra cristalizada de gelo. O **hálito dele sai em vapor branco**. Da mão dele parte um
> sopro de gelo ciano até uma **miniatura inimiga pequena logo à frente, que está
> COMPLETAMENTE presa dentro de um bloco de gelo azul translúcido**, em pé, congelada no meio
> do movimento. O bloco é o segundo ponto de luz do quadro. Base de mosaico coberto de
> geada. Paleta de branco-gelo, azul-claro e ciano.

**Conjurador** (2/0/4)

> _Efeito: causa 1 de dano a um inimigo à sua escolha ao entrar em campo._
>
> Um mago de **chapéu pontudo azul-escuro de aba larga**, **varinha curta de madeira escura**
> apontada para a frente com o braço estendido e o olho fechado como quem está mirando,
> **roupão azul-noite** com estrelinhas bordadas em prata. Da ponta da varinha **acaba de
> sair um único dardo de luz roxa concentrada**, com um rastro fino, indo em direção a **uma
> miniatura inimiga pequena ao fundo desfocado que solta um clarão roxo no ponto do
> impacto**. É um tiro só, preciso, nada de área. Base de mosaico. Paleta de azul-noite,
> prata e roxo-arcano.

**Encantador** (2/0/3)

> _Efeito: sempre que um healer aliado for atacado, o atacante leva 1 de dano de volta._
>
> Um mago de **tiara fina de prata na testa, careca, sem capuz**, **orbe roxo flutuando
> sozinho acima da palma da mão aberta** (ele não segura nada), **manto roxo com estola
> branca**. Ao lado dele, dividindo a mesma base, uma **miniatura pequena de curandeira de
> túnica branca** ajoelhada cuidando de algo. Em volta dos dois há uma **cúpula translúcida
> roxa fina**, e **uma espada inimiga acaba de bater na cúpula por fora: o ponto do impacto
> acende e devolve um choque de faíscas roxas para trás, na direção de onde veio o golpe**. A
> leitura é: quem encosta nela, se machuca. Base de mosaico com um círculo roxo desenhado.
> Paleta de roxo, branco e prata.

---

## 🔷 TIER 2 — a ordem dos três selos. Base de mosaico com o selo gravado no chão

### ✳️ O SELO DA TRÍADE — cole nos TRÊS primeiros, igualzinho

Runa, Glifo e Sigilo invocam o Arcanor, e precisam ser reconhecíveis como **a mesma ordem**.
O nome das três cartas já entrega o conceito: as três carregam **a mesma marca**, cada uma
num material diferente. Este bloco entra no meio do prompt dos três:

```
Ele pertence a uma ordem de magos que compartilha a MESMA MARCA ARCANA, sempre
idêntica: um LOSANGO vertical alongado, envolto por TRÊS ARCOS CONCÊNTRICOS abertos
em cima, com TRÊS PONTINHOS alinhados embaixo do losango. Nada mais. A marca brilha
em CIANO e aparece grande e bem visível nele.

A marca é simples, geométrica e minimalista. NÃO é um pentagrama, NÃO é um olho,
NÃO é um triângulo com círculo, NÃO é uma mandala de faíscas, NÃO é um floco de
neve, NÃO é uma engrenagem. NÃO copie o símbolo de nenhum jogo, filme ou franquia
existente. É um símbolo original.
```

Hierarquia: a marca é **gravada em pedra gasta** na Runa (a mais antiga e bruta), **pintada
na pele e no tecido** no Glifo (a intermediária), e **flutuando pronta no ar, desenhada em
luz** no Sigilo (o líder, o selo completo).

**Runa** (2/0/3)

> _Efeito: carta tríade — com os outros dois em campo, invoca Arcanor, o Primordial._
>
> A mais **velha e rústica** dos três: um mago idoso e curvado de **capuz de lã crua
> encardido**, **cajado de pedra bruta lascada** mais alto que ele, **túnica de lã grossa
> cinza-azulada** remendada. Pendurado no peito, um **amuleto de pedra chata** com a marca
> `[SELO DA TRÍADE — a marca dele está ENTALHADA na pedra do amuleto, gasta pelo tempo, com
a luz ciano vazando fraquinho de dentro dos sulcos]`. Postura de quem anda apoiado no
> cajado. Base de lajota antiga rachada. Paleta de cinza-pedra, lã crua e ciano fraco.

**Glifo** (2/0/4)

> _Efeito: carta tríade — com os outros dois em campo, invoca Arcanor, o Primordial._
>
> O **intermediário**: um mago adulto de **cabeça raspada e sem capuz**, **pergaminho aberto
> flutuando na frente dele** que ele lê com a mão espalmada por cima (o pergaminho é o foco
> dele, não tem cajado), **roupão azul-cobalto de mangas largas**. Os **antebraços e o rosto
> dele são cobertos de tatuagens luminosas** `[SELO DA TRÍADE — a marca dele está PINTADA na
pele do peito e repetida menor nas costas das mãos, acesa em tinta ciano fresca]`. Base de
> lajota com o mesmo símbolo desenhado a giz no chão. Paleta de azul-cobalto, pele morena e
> ciano vivo.

**Sigilo** (3/0/3)

> _Efeito: carta tríade — com os outros dois em campo, invoca Arcanor, o Primordial._
>
> O **líder** dos três: um mago de porte ereto, **capuz fundo azul-escuro com bordado
> prateado, rosto na sombra e só dois pontos de luz ciano no lugar dos olhos**, **as duas
> mãos abertas à frente do peito, sem foco nenhum nas mãos**, **manto azul-escuro pesado com
> ombreiras de tecido rígido**. `[SELO DA TRÍADE — a marca dele está FLUTUANDO INTEIRA NO AR
na frente do peito, desenhada em traços de luz ciano brilhante, girando devagar — é a
versão completa e acabada do símbolo, muito maior e mais luminosa que a dos outros dois]`.
> Base de mosaico polido escuro com o símbolo em incrustação de prata. Paleta de azul-escuro,
> prata e ciano intenso.

**Ferrugem** (3/0/4)

> _Efeito: todo round corrói 1 de armadura do inimigo mais blindado. Se ninguém tiver
> armadura, causa 1 de dano no inimigo com menos vida._
>
> A carta **mais feia de propósito** da classe, e a única que foge do azul: uma bruxa
> encurvada de **capuz esfarrapado verde-musgo**, **cajado torto com um caldeirãozinho
> pendurado na ponta soltando vapor**, **túnica de retalhos marrom e verde-ácido**. Do cajado
> escorre um **vapor verde-ácido pesado que desce e se espalha rasteiro pela mesa** até uma
> **miniatura pequena de guerreiro inimigo de armadura ao lado — e a armadura dele está
> visivelmente se desfazendo em pó laranja-ferrugem, com buracos comidos nas placas e as
> lascas caindo no chão**. Base de mosaico manchado e corroído. Paleta de verde-ácido,
> marrom-ferrugem e laranja apodrecido.

---

## 🔮 TIER 3 — heróis. Base ornamentada com círculo arcano aceso, VFX bem visível

**Pirotécnico** (2/0/5)

> _Efeito: ao entrar em campo, causa uma explosão: 2 de dano em um inimigo à sua escolha e 1
> nos inimigos adjacentes a ele._
>
> Um mago do FOGO, **jovem e travesso, sorrindo**, **cabelo bagunçado meio chamuscado nas
> pontas**, **capuz azul caído para trás**, **túnica curta azul-escura de mago com as barras
> queimadas e esfarrapadas**, braçadeiras de couro. Ele acabou de **juntar as duas mãos** e a
> explosão está acontecendo: uma **bola de fogo laranja e vermelha se abrindo entre as
> palmas**, com **fagulhas alaranjadas voando para fora** e **três labaredas laranjas
> saltando na base de pedra ao redor dos pés**. A luz laranja bate forte no rosto e no peito.
> Base de mosaico chamuscado no centro. Paleta de azul-escuro, laranja-fogo e vermelho brasa.
>
> `[+ negativa obrigatória: o fogo é LARANJA E VERMELHO, cor de brasa. Nada de chama azul,
nada de chama ciano, nada de gelo, nada de cristais. Sem óculos de proteção, sem frascos de
poção no cinto, sem cara de alquimista ou inventor — ele é um mago de fogo, não um
engenheiro]`

**Invernal** (3/0/4)

> _Efeito: escolhe um inimigo e decide entre congelar por um turno OU causar 1 de dano a cada
> 3 turnos. Com um healer e um tanque aliado em campo, faz os dois._
>
> Uma maga de **capuz de pele branca com gola alta felpuda, rosto sério e pálido à mostra**,
> **um orbe flutuando em cada mão, um de cada tipo**, **manto azul-gelo comprido arrastando**.
> Na **mão direita, um orbe de gelo maciço, opaco e sólido**; na **mão esquerda, um orbe
> escuro azul-petróleo com uma nevasca girando dentro**. Ela olha de uma mão para a outra,
> **escolhendo** — é essa a leitura da carta. Nenhum dos dois foi lançado ainda. Base de
> mosaico com gelo fino rachado em volta dos pés. Paleta de azul-gelo, branco e
> azul-petróleo escuro.

**Estilhaço** (3/0/5)

> _Efeito: seus ataques respingam — causam 1 de dano também aos inimigos adjacentes ao alvo._
>
> Um mago **coberto de cristal**: **capuz baixado, cabelo curto, cacos de cristal azul
> crescendo do ombro e da lateral do rosto**, **cajado com um cristal grande e rachado na
> ponta**, **manto azul-safira** com placas de cristal costuradas. Ele **bateu o cajado no
> chão e o cristal da ponta explodiu**: dezenas de **lascas de cristal azul afiadas voando
> para fora em leque, em várias direções ao mesmo tempo**. **Três miniaturas inimigas
> pequenas espalhadas lado a lado** estão sendo atingidas ao mesmo tempo, cada uma com um
> pontinho de brilho no ponto do impacto — o dano se espalhou. Base de mosaico com cacos
> cravados no chão. Paleta de azul-safira, cristal translúcido e branco.

**🧪 Usurpador** (1/0/3) — _TESTE 2 — o roubo lido num personagem só_

> _Efeito: rouba os status de um inimigo para si. Duplica o roubo de ataque se tiver um
> arqueiro em campo._
>
> Um mago ladrão de poder, **magro e faminto, rosto VIVO e encovado** — pele pálida, olhos
> fundos brilhando em **roxo**, boca fina e um **sorriso torto de ganância**. **Capuz
> roxo-escuro caído até a metade da testa**, **manto esfarrapado roxo e cinza**. Um braço
> está **esticado à frente com a mão em garra**, e desse gesto **fios de luz roxa vêm do ar à
> frente e entram no antebraço dele** — esse antebraço está **inchado, maior que o outro,
> cheio de luz roxa correndo por baixo da pele como veias acesas**. O outro braço, o normal,
> é **fino e murcho** por contraste, segurando um cajado torto de madeira com um cristal roxo
> na ponta. Base de pedra escura com um **anel de luz roxa liso, sem símbolos**. Paleta de
> roxo-escuro, cinza-morto e magenta.
>
> `[+ negativa do Doutor Estranho: sem capa vermelha, sem cavanhaque, sem mandala circular de
faíscas laranja]`
>
> `[+ negativa obrigatória: NÃO é morto-vivo. Sem caveira, sem crânio, sem ossos à mostra, sem
rosto de esqueleto. Na base NÃO tem pentagrama, NÃO tem estrela de cinco pontas, NÃO tem
círculo de invocação escrito]`

---

## ⚡ TIER 4 — lendas. Miniatura visivelmente maior, base grande, o efeito domina a cena

**Eletromante** (4/0/4)

> _Efeito: a cada 2 turnos lança um raio: 2 de dano em um inimigo à sua escolha e 1 nos
> adjacentes a ele._
>
> Um mago com o **cabelo todo em pé, arrepiado e eriçado, sem capuz nenhum**, **cajado de
> metal com uma bobina de cobre e duas hastes na ponta**, **manto curto azul-elétrico** com
> fios de cobre trançados nas bordas. Ele **ergue o cajado com um braço só**, e do céu
> escuro **desce um raio branco-azulado grosso e ramificado** que bate numa **miniatura
> inimiga à frente — e do ponto do impacto saem dois arcos elétricos menores que pulam para
> as duas miniaturas dos lados**. Pequenos raios curtos correm pelo corpo dele. Base de
> mosaico com marcas de queimado em forma de raiz. Paleta de azul-elétrico, branco
> incandescente e cobre.
>
> `[+ negativa do Thor/Raiden: sem martelo, sem chapéu cônico de palha, sem armadura de
samurai]`

**Aniquilador** (3/0/5)

> _Efeito: seleciona e destrói um inimigo de nível inferior. Com tanque, healer e arqueiro
> aliados em campo, absorve 50% do ataque dele._
>
> Um mago alto e imóvel de **máscara lisa de porcelana branca sem olhos nem boca**, **capuz
> preto por cima**, **mão direita erguida com a palma virada para baixo, dedos abertos —
> nenhum cajado, nenhum foco**, **roupão preto muito comprido** com forro roxo aparecendo nas
> mangas. Abaixo da mão dele, uma **miniatura inimiga pequena está sendo APAGADA de baixo
> para cima: os pés e as pernas já viraram cinza roxa que sobe em flocos, e só a parte de
> cima do corpo ainda existe**, com a borda do corte brilhando em roxo. Não é explosão, não é
> fogo — é dissolução silenciosa. Base de mosaico preto polido. Paleta de preto, branco de
> porcelana e roxo-escuro.

**Canalizador** (4/0/6)

> _Efeito: ganha +1 de ataque sempre que um aliado for curado (limite de +5)._
>
> Um mago canalizador de energia, **corpo largo e firme**, **capuz azul justo na cabeça com
> uma faixa de pano na testa**. Ele veste um **manto azul longo de mago, de mangas amplas,
> com uma estola cruzada amarelo-ouro no peito** — silhueta claramente de mago. Braços
> cobertos pelas mangas até o pulso.
>
> A magia atravessa o corpo dele: uma **luz verde-suave entra pelas costas em fios finos**,
> passa pelos ombros e **sai pelas duas mãos abertas à frente como energia azul-ciano
> concentrada, girando em espiral nas palmas**. Dá para ver a transformação: verde entrando
> atrás, azul saindo na frente. Base de mosaico de pedra com runas. Paleta de azul intenso,
> verde-suave e ouro.
>
> `[+ negativa obrigatória: NÃO é monge, NÃO é lutador de artes marciais. Sem braços nus, sem
punhos fechados, sem pés descalços, sem pose de luta — ele está de pé, calmo, palmas abertas]`

**Purificador** (4/0/5)

> _Efeito: escolhe um inimigo e retira todos os status bônus dele. Com healer e arqueiro em
> campo, pode lançar o feitiço mais uma vez._
>
> Um mago purificador **severo, de capuz**, rosto visível e expressão dura. **Manto azul-claro
> e prata com detalhes em azul-escuro** — nada de branco-e-dourado, nada de vestes de padre.
> Numa mão ele **segura por correntes um incensário de prata soltando fumaça ciano**; a outra
> **mão está estendida à frente com a palma aberta**, soltando um pulso de luz ciano.
>
> No ar, à frente da mão aberta, **fitas de luz dourada (os bônus roubados) estão se rasgando
> e virando pó**, dissolvendo no ar. Base de mosaico de pedra azulada com runas ciano.
> Paleta de azul-claro, prata e ciano.
>
> `[+ negativa obrigatória: ele é um MAGO, não um curandeiro. Sem símbolo religioso, sem cruz,
sem auréola, sem asas, sem branco-e-dourado de sacerdote]`

---

## 🌌 TIER 5 — mitos. A miniatura ocupa a mesa. Os outros bonecos viram peões ao lado

**Arquimago** (5/0/6)

> _Efeito: lança uma bola de fogo — 5 de dano num inimigo à escolha e 2 nos adjacentes.
> Aumenta o ataque de todos os magos a cada reset de turno._
>
> Uma miniatura **grande, dominando o quadro**: um mago poderoso de **capuz alto e rígido
> azul-real com colarinho em leque atrás da cabeça, rosto de meia-idade à mostra e barba
> curta e aparada**, **cajado longo dourado com uma esfera de fogo presa em um anel de metal
> na ponta**, **manto azul-real pesado com dourado** e a barra flutuando com a corrente de
> ar quente. Ele segura **acima da cabeça uma BOLA DE FOGO enorme, girando, quase do tamanho
> do torso dele**, que ilumina a mesa inteira de laranja. Ao redor dos pés dele, **duas ou
> três miniaturas de magos pequenininhas com os cajados erguidos, e uma linha de luz azul
> passando do manto dele para elas** (o buff de classe). Base de mosaico grande e
> ornamentado. Paleta de azul-real, dourado e laranja-fogo.
>
> `[+ negativa do Gandalf/Dumbledore: sem barba branca comprida até o peito, sem chapéu
cinza de aba mole, sem cajado de madeira torta, sem óculos de meia-lua]`

**Lorde do Gelo** (5/0/5)

> _Efeito: uma vez por round congela um inimigo à sua escolha. Efeito duplicado com um tank
> aliado._
>
> Uma miniatura **grande e imponente**: um senhor do inverno de **coroa de lascas de cristal
> transparente encaixadas direto no cabelo branco, sem capuz**, **rosto de homem idoso, VIVO,
> de pele azulada pálida, barba curta coberta de geada e olhos azul-claros**, **cetro de gelo
> maciço com uma ponta afiada**, **manto pesado azul-profundo com forro de pele branca e uma
> cauda comprida arrastando pela mesa e congelando o que toca**. Ele **encosta a ponta do
> cetro no chão** e uma **onda de gelo se espalha em placas pela mesa a partir dali,
> prendendo DUAS miniaturas inimigas pequenas em blocos de gelo azul translúcido**. Base de
> um pedaço de trono de gelo quebrado. Paleta de azul-profundo, branco-gelo e ciano.
>
> `[+ negativa do Lich King: NÃO é morto-vivo, sem caveira, sem elmo de coroa de espinhos,
sem armadura preta de placas, sem olhos vazios brilhando dentro de um capacete, sem espada
gigante]`

**Metamorfo** (4/0/6)

> _Efeito: uma vez por partida escolhe uma carta inimiga em campo e copia ataque e vida dela
> com +1 em cada._
>
> Uma miniatura grande com **o corpo dividido ao meio na vertical**: **a metade esquerda é um
> mago de capuz roxo, manto roxo e mão humana**; a **metade direita já virou a CÓPIA de um
> guerreiro inimigo — ombreira de metal, braço blindado, meio elmo** — e a **linha que separa
> as duas metades é uma costura vertical de luz roxa líquida escorrendo**, com gotas de luz
> pingando na base. O rosto também está partido: metade rosto de mago, metade viseira de
> metal. Ao lado, **uma miniatura inimiga pequena do guerreiro original, de pé e intacta**,
> para dar a leitura de que ele está copiando aquele ali. Base de mosaico com um espelho
> quebrado incrustado. Paleta de roxo, magenta e cinza-metal.
>
> `[+ negativa da Mística: sem pele azul escamosa, sem corpo nu, sem olhos amarelos, sem
cabelo vermelho]`

---

## 🏛️ CARTA EXCLUSIVA — não sai do baralho, nasce da tríade

**🧪 Arcanor, o Primordial** (6/0/7) — _TESTE 3 — épico sem perder a fofura_

> _Efeito: ao entrar em campo, Cataclisma — 1 de dano em TODOS os inimigos. Todo round,
> dispara um raio de 1 de dano num inimigo à sua escolha. Só aparece quando Runa, Glifo e
> Sigilo estão juntos em campo._
>
> Uma miniatura **muito maior que todas as outras** — um arquimago primordial chibi
> **FLUTUANDO alguns centímetros acima da base, com os pés soltos no ar e o manto pendendo
> para baixo**. **Capuz fundo enorme, nenhum rosto lá dentro, só DOIS PONTOS DE LUZ CIANO no
> escuro**. **Manto roxo-profundo cujo tecido, por dentro das dobras, é um céu estrelado
> escuro** em vez de pano. **Braços abertos para os lados, mãos nuas, sem cajado nenhum.**
> Girando em volta dele, em três órbitas inclinadas, os **TRÊS SELOS DA ORDEM desenhados em
> luz ciano — o losango com três arcos e três pontinhos** (o mesmo símbolo de Runa, Glifo e
> Sigilo, agora aceso, gigante e completo). Do peito dele **explode uma onda circular roxa
> que varre a mesa inteira**, e **todas as miniaturas inimigas pequenininhas do fundo estão
> se curvando ao mesmo tempo** com um pontinho de luz roxa cada uma — o dano pegou em todo
> mundo. Aos pés dele, **três miniaturas de magos bem pequenininhas ajoelhadas** com os
> cajados erguidos. Base de mosaico partido, flutuando em pedaços soltos no ar. Paleta de
> roxo-profundo, ciano e preto estrelado.

---

## 📋 Checklist antes de aprovar cada uma

- [ ] Quadrada **1024×1024** (o Nano Banana às vezes entrega paisagem)
- [ ] **Sem moldura preta** desenhada dentro da imagem
- [ ] Dá pra **adivinhar o efeito** olhando só a figura?
- [ ] O **tamanho da miniatura** condiz com o tier?
- [ ] **Cabeça, foco, traje e elemento** diferentes dos outros 20 magos?
- [ ] Tem **UM personagem só** no quadro, e o efeito se lê no corpo dele?
- [ ] Não saiu nenhum **personagem de franquia** (ver a tabela lá em cima)?
- [ ] A **mesa e a luz de vela** batem com as artes de tank que já estão no jogo?
- [ ] Converter de **JPEG para PNG de verdade** antes de importar em `Assets/`
      (o download vem JPEG com extensão `.png`)

## 📁 Como me entregar

Salve cada uma em `arte-ia/mages/` com o nome **`Nome-ataque-escudo-vida.png`**, igual aos
tanques (ex.: `Arquimago-5-0-6.png`, `Lorde do Gelo-5-0-5.png`, `Arcanor, o Primordial-6-0-7.png`).
Me avise quando estiverem lá que eu instalo — a arte entra por cima do sprite que a carta já
aponta, sem mexer no `.asset`, e faço a conferência de tamanho e formato de todas de uma vez.

## 🎨 Paleta por classe (as duas que faltam depois desta)

| Classe       | Cores                            | Terreno da base           | Luz do efeito        |
| ------------ | -------------------------------- | ------------------------- | -------------------- |
| **Tank** ✅  | ferro, bronze, dourado, carmesim | pedra, calçada, escombros | dourado / laranja    |
| **Mago** ⬅️  | azul, roxo, ciano                | mosaico, cristal, runa    | ciano / roxo         |
| **Arqueiro** | verde-floresta, couro, âmbar     | grama, folhas, tronco     | âmbar / verde        |
| **Healer**   | branco, dourado, rosa-claro      | mármore, flores           | branco / verde-suave |

Progressão de tier igual em todas: **T1 sem magia nenhuma → T5 luminoso e enorme**.
