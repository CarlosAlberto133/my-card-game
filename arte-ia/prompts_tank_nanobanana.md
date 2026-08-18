# Prompts das cartas TANK — Nano Banana Pro / Gemini Image

> Diferente do SDXL: aqui **não** se usa lista de tags. O modelo lê texto corrido e
> obedece instruções. Se você não disser o estilo, ele assume foto realista.

> ## 🏁 CLASSE TANK FECHADA — as 21 artes estão aprovadas
>
> As 20 do baralho + Atlas, o Baluarte. Todas foram conferidas **contra o efeito real da
> carta** (lido nos `.asset` e no `CardEffectSimple.cs`), todas em 1024×1024 e no mesmo
> estilo. **Nada a refazer.** Os prompts ficam aqui como referência caso queira regerar
> alguma variação — e como molde para as outras 3 classes.
>
> 📌 **Antes de importar no Unity:** os arquivos em `arte-ia/tanks/` são **JPEG com
> extensão `.png`** (o Nano Banana entrega JPEG e o download renomeia). Converta de
> verdade ou renomeie pra `.jpg`, senão o importador reclama.
>
> **Próximo passo:** Arqueiro, Mago e Healer — escrever os prompts **a partir do efeito**
> de cada carta desde o começo, em vez de corrigir depois. Ver o molde no fim do arquivo.

> ⚠️ **Armadilhas do Nano Banana (histórico das rodadas 1 e 2):**
> 1. Às vezes ele desenha uma **moldura preta dentro da imagem** (aconteceu no
>    Porta-Bandeira, Penitente e Titã de Bronze). Se sair assim, regere.
> 2. O download vem como **JPEG com extensão `.png`**. Converta de verdade (ou renomeie
>    pra `.jpg`) antes de jogar em `Assets/`, senão o importador do Unity reclama.
> 3. Sempre gere **quadrado 1024×1024** (todas as 18 boas estão assim) — Vanguarda e
>    Escudeiro Arcano saíram 2400×1792 em paisagem e destoam do resto.

---

## 🎨 BLOCO DE ESTILO (cole SEMPRE antes da descrição da carta)

```
Ilustração 2D pintada à mão para uma carta de card game de fantasia medieval.
Estilo: arte de Hearthstone misturada com anime de Yu-Gi-Oh — contornos
definidos, formas estilizadas e exageradas, pincelada pintada visível, cores
saturadas e vibrantes, iluminação dramática com luz de contorno forte.
Proporções heroicas exageradas: ombros largos, mãos e armas grandes, silhueta
imediatamente legível. Personagem único centralizado, plano médio heroico em
ângulo de baixo para cima. Fundo simples e atmosférico com névoa, partículas de
energia e vinheta escura nas bordas — o fundo nunca compete com o personagem.
Enquadramento vertical, proporção 2:3.

IMPORTANTE: isto NÃO é uma fotografia, NÃO é render 3D, NÃO é realista, NÃO é
cosplay. É desenho/pintura digital estilizada. Sem texto, sem letras, sem
moldura de carta, sem logotipo, sem marca d'água, sem borda.

A carta desta vez é:
```

Depois do bloco acima, cole **um** dos 20 prompts abaixo.

---

## 🥇 O TRUQUE DA CONSISTÊNCIA (o mais importante)

O Nano Banana aceita **imagem de entrada**. Então:

1. Gere as cartas até sair **uma** que te encantou. Essa vira a sua "carta mestra".
2. Nas próximas, **anexe a carta mestra** e escreva:

```
Use EXATAMENTE o mesmo estilo de arte, paleta, tipo de pincelada, iluminação e
enquadramento da imagem anexada. Não mude o estilo. Apenas troque o personagem por:
<descrição da nova carta>
```

Isso trava o visual muito melhor do que repetir o bloco de estilo, e é o que vai
fazer as 80 cartas parecerem do mesmo jogo.

---

## ⚔️ TIER 1 — recrutas. Armadura simples, pouca luz, escala humana

**✅ Vanguarda** (1/2/5) — *aprovada na rodada 2*
> *Efeito: NENHUM — é a carta vanilla pura. A arte atual está certa de conteúdo, o
> problema é técnico: ela é do lote antigo (2400×1792 paisagem, estilo cartoon lisinho
> sem a pincelada pintada). Gere quadrada 1024×1024, e de preferência **anexando uma das
> novas como referência de estilo** (ver "O truque da consistência" acima).*
>
> Um jovem soldado humano de armadura de ferro gasta e escudo redondo simples de madeira,
> rosto determinado com uma cicatriz na bochecha, espada curta baixada ao lado do corpo.
> Postura firme de quem vai aguentar o primeiro golpe, plano médio heroico. Paleta de aço,
> couro marrom e cinza — **nada de brilho mágico, nenhuma runa, nenhum efeito de luz
> colorida**: ele é o mais simples e mais humano da coleção inteira, e é isso que o
> identifica.

**✅ Escudeiro Arcano** (1/2/4) — *aprovada na rodada 2*
> *Efeito: se houver um mago em campo, ao entrar concede +1 de armadura a todos os
> aliados. Mesmo caso da Vanguarda — o conteúdo está certo, o formato é que está fora do
> padrão (2400×1792 paisagem). Gere quadrada 1024×1024 no estilo das novas.*
>
> Um escudeiro **jovem e franzino** de armadura de aço gravada com runas azuis brilhantes,
> segurando um escudo alongado envolto em luz mágica suave. Do escudo dele **partem fios
> finos de luz azul que se espalham para fora do quadro**, como se estivesse passando a
> proteção adiante para os companheiros. Fios de energia azul correm pela armadura e o
> brilho ilumina o rosto dele por baixo. Paleta fria de azul-safira e prata.

**✅ Guarda Rúnico** (0/2/5) — *já refeita e aprovada*
> *Efeito: ganha +1 de ataque, e mais +1 por cada mago aliado em campo. É a única T1 que
> fica mais FORTE (ofensiva) perto de magia — a arte tem que mostrar isso.*
>
> Um guerreiro atarracado de armadura escura coberta de runas entalhadas, mas as runas
> queimam em **azul-arcano brilhante**, não em fogo. Ele ergue um **machado pesado de
> guerra** cuja lâmina está envolta na mesma energia azul, cada vez mais intensa perto do
> gume. Fios de luz azul sobem do chão e entram pelas runas da armadura, como se ele
> estivesse **absorvendo magia e transformando em força bruta**. O escudo fica secundário,
> nas costas. Expressão de esforço contido. Paleta de preto, ferro frio e azul-safira
> luminoso.

**✅ Penitente** (0/2/4) — *aprovada na rodada 2*
> *Efeito: ganha +1 de ataque toda vez que ganhar vida. A arte atual está boa e temática,
> mas não tem nenhuma pista de CURA — e a cura é o gatilho dele. Só refaça se sobrar
> paciência; a atual não está errada.*
>
> Um guerreiro penitente descalço, de manto rasgado com capuz sobre uma cota de malha
> enferrujada, correntes penduradas nos pulsos, carregando um escudo de ferro pesado
> demais para ele. **Uma luz dourada e suave de cura desce sobre os ombros dele vinda de
> cima, e onde essa luz toca as correntes elas brilham em laranja de brasa e a mão que
> segura a arma se fecha com mais força** — o alívio vira raiva. Expressão de sofrimento e
> teimosia. Paleta de cinza, ferrugem e pano sujo, com o dourado da cura como único ponto
> de cor.

**✅ Abençoado** (1/1/5) — *aprovada*
> Um guerreiro humilde de armadura simples ajoelhado, banhado por um facho de luz dourada
> que vem de cima. Partículas douradas sobem ao redor dele e a armadura reflete o brilho
> sagrado. Expressão serena, olhos fechados. Paleta de dourado quente e branco.

---

## 🛡️ TIER 2 — elite romana. Uniforme, disciplina, vermelho e bronze

### 🦁 O BRASÃO DA TRÍADE (cole nos TRÊS prompts abaixo, igualzinho)

Pretoriano, Legionário e Centurião são a **tríade**: quando os três estão em campo juntos,
invocam Atlas, o Baluarte. Hoje as artes não contam isso. A regra é dar aos três (e ao
Atlas) o **mesmo emblema, a mesma capa e o mesmo detalhe de cor**, para o jogador bater o
olho e sacar que eles são um conjunto:

```
Ele pertence à mesma ordem militar dos outros: usa uma CAPA CARMESIM presa no ombro
direito por um broche dourado, e traz pintado no escudo o MESMO BRASÃO da ordem — uma
cabeça de leão dourada de frente, dentro de uma coroa circular de louros. O brasão
aparece grande e bem visível. Detalhes em dourado nas bordas da armadura.
```

**✅ Pretoriano** (0/3/5) — *já refeita e aprovada*
> *Efeito: carta-tríade. É o mais defensivo dos três (0 de ataque).*
>
> Um guarda pretoriano de elite com lorica ornamentada em carmesim e bronze, elmo de crista
> alta, escudo retangular enorme travado à frente do corpo cobrindo quase todo o peito —
> postura de muralha, ele não ataca, ele barra. `[COLE AQUI O BRASÃO DA TRÍADE]`
> Paleta de vermelho profundo, bronze polido e dourado.

**✅ Legionário** (1/2/5) — *já refeita e aprovada*
> *Efeito: carta-tríade. É o soldado raso do trio, o mais gasto de todos.*
>
> Um legionário romano marcado pela batalha, armadura de placas segmentadas amassada,
> túnica vermelha, grande escudo retangular com a tinta descascando, poeira de campo de
> batalha no ar. `[COLE AQUI O BRASÃO DA TRÍADE]` — no caso dele o brasão do leão está
> **arranhado e desbotado pelo uso**, mas ainda é claramente o mesmo símbolo.
> Paleta de ferro, vermelho desbotado e areia.

**✅ Centurião** (1/3/4) — *já refeita e aprovada*
> *Efeito: carta-tríade. É o líder dos três — a arte dele fecha o conjunto.*
>
> Um centurião veterano de armadura de bronze, elmo com crista transversal, braço erguido
> dando ordem de avanço, bastão de vinha na mão. Olhar duro e experiente de quem comanda
> os outros dois. `[COLE AQUI O BRASÃO DA TRÍADE]` — no dele o brasão é o mais rico,
> **em relevo e polido**, e a capa é a mais longa. Paleta de vermelho escuro, bronze e dourado.

**✅ Guarda-Costas** (1/3/5) — *aprovada (a que melhor casa com o efeito)*
> Um guarda-costas enorme de armadura grossa, braços abertos protegendo alguém que está
> fora do quadro, escudo maciço de placa fincado no chão à frente. Corpo inteiro virado
> para o perigo. Paleta de aço escuro, azul-marinho e couro preto.

---

## ✨ TIER 3 — heróis. Armadura ornamentada, aura mágica visível

**✅ Égide Arcana** (2/4/7) — *já refeita e aprovada*
> *Efeito: ao entrar, concede +3 de armadura e +1 de ataque a UM MAGO à sua escolha.
> O escudo dela não é para ela — é para o mago. A arte precisa mostrar o destinatário.*
>
> Uma cavaleira de armadura prateada ornamentada, de pé em posição protetora, com o braço
> estendido para o lado conjurando **um grande escudo circular de luz azul que flutua na
> frente de OUTRA pessoa**: atrás e ao lado dela, um **mago encapuzado de manto azul**
> visto de costas/em silhueta, protegido pela barreira. Glifos geométricos brilhantes
> viajam do braço dela até o escudo, deixando claro de quem para quem a proteção vai.
> Ela olha para o mago, não para a câmera. Paleta de prata, safira e branco luminoso.

**✅ Irmão de Armas** (2/4/6) — *aprovada na rodada 2 (brasão da Alliance resolvido)*
> *Efeito: recebe +2 de armadura para cada OUTRO tank em campo. A arte atual é ótima, MAS
> o estandarte que o Nano Banana desenhou atrás dele é praticamente o brasão da Aliança
> do World of Warcraft (leão dourado sobre azul) — marca registrada da Blizzard. Num jogo
> que você distribui, isso precisa sair. Aproveitei pra reforçar a "irmandade".*
>
> Um cavaleiro veterano grisalho de armadura pesada e amassada, com ombreiras esculpidas
> em forma de cabeça de lobo cinza, punho fechado sobre o coração em juramento.
> **NÃO use nenhum brasão heráldico, nenhum leão, nenhum escudo com emblema**: atrás dele
> tremula um estandarte **liso e rasgado de pano vermelho-escuro, sem símbolo nenhum**, e
> **três outros cavaleiros de armadura pesada estão em silhueta na névoa atrás dele, todos
> na mesma pose de juramento com o punho no peito** — quanto mais irmãos ao lado, mais
> forte ele fica. Paleta de cinza-ferro, marrom couro e um detalhe de vermelho no manto.

**✅ Capitão de Ferro** (2/3/6) — *aprovada*
> Um capitão de armadura completa enegrecida, elmo com chifres, capa de guerra rasgada,
> avançando na linha de frente com brasas e faíscas girando ao redor. Postura agressiva,
> de quem lidera o ataque. Paleta de preto fosco, laranja de brasa e cinza fumaça.

**✅ Guardião da Fé** (2/3/7) — *aprovada na rodada 2*
> *Efeito: concede +2 de armadura a todos os HEALERS em campo a cada 2 turnos. A arte
> atual ficou linda e a luz sagrada está lá, mas — mesma crítica que fiz à Égide — não
> aparece PARA QUEM a luz vai. Refaça só se quiser a leitura perfeita.*
>
> Um guardião sagrado de armadura branca e dourada, elmo alado, segurando um escudo-torre
> radiante com o emblema de um sol nascente. **Da face do escudo saem raios largos de luz
> dourada que vão em direção a duas figuras encapuzadas de manto branco ao lado dele —
> curandeiras vistas em silhueta, de mãos postas** — e onde a luz as toca aparece um brilho
> de escudo protetor ao redor delas. Ele olha para elas, protetor. Paleta de branco, ouro e
> luz quente.

---

## 👑 TIER 4 — lendas. Escala imponente, o efeito aparece na arte

**✅ Baluarte** (3/7/8) — *aprovada na rodada 2 (leão removido)*
> *Efeito: com healer, mago e arqueiro em campo, ataca com +2 e dá 1 de armadura por turno
> aos vizinhos. A arte acertou em cheio (as 4 luzes elementais desenham a condição da
> carta!) — o único problema é que o Nano Banana pôs **o leão da tríade no escudo dele**.
> Ele não é da tríade. Se o leão aparecer em 5 cartas, ele para de significar "esses três
> se juntam". Só refaça se quiser preservar o símbolo.*
>
> Um cavaleiro-fortaleza colossal de armadura em camadas como muralhas de castelo,
> carregando um escudo do tamanho de um portão. **No escudo há apenas runas entalhadas
> brilhando em dourado — NENHUM brasão, NENHUM leão, NENHUMA cabeça de animal.** Quatro
> luzes elementais coloridas — dourada, azul, verde e vermelha — giram ao redor dele
> representando as quatro classes aliadas. Escala épica, ele domina o quadro.

**✅ Porta-Bandeira** (2/6/8) — *já refeita e aprovada*
> *Efeito: com as 4 classes em campo e ele na linha de frente, os ARQUEIROS aliados atacam
> DUAS VEZES. A arte atual não tem uma flecha sequer — o efeito é todo sobre arqueiros.*
>
> Um porta-estandarte de armadura reluzente e ornamentada, erguendo com as duas mãos um
> estandarte de guerra gigantesco e rasgado que ocupa todo o fundo. **Dezenas de flechas
> estão cravadas no pano e no mastro do estandarte**, e atrás dele, em silhueta contra o
> céu, **arqueiros de arco erguido disparam uma chuva de flechas douradas que cruzam o
> quadro por cima**. O estandarte é o sinal de ataque: ele grita e as flechas partem.
> Vento forte, poeira dourada, ângulo heroico de baixo para cima. Paleta de dourado,
> vermelho e céu de tempestade.

**✅ Quebra-Golpes** (2/7/7) — *aprovada na rodada 2 (leão removido)*
> *Efeito: 1x por turno recebe o ataque no lugar de um aliado adjacente. A arte é
> impecável — o instante do bloqueio congelado. Mesmo caso do Baluarte: só saiu com o
> **leão da tríade no escudo**, que não é dele.*
>
> Um defensor gigantesco de armadura de ferro rebitada, escudo erguido no exato instante
> em que intercepta um golpe destinado a outra pessoa. **O escudo é de ferro liso e
> amassado, coberto de marcas de batalha e runas laranja — SEM brasão, SEM leão, SEM
> emblema de animal.** Onda de choque do impacto, flechas se partindo no ar, faíscas
> voando. **Ao lado dele, meio escondida atrás do escudo, a silhueta de um aliado menor
> encolhido** — fica claro que ele levou o golpe no lugar de alguém. A cena inteira é o
> momento do bloqueio.

**✅ Titã de Bronze** (2/6/7) — *aprovada (só saiu com moldura preta; regere se incomodar)*
> Um titã guerreiro colossal de bronze, armadura com pátina esverdeada e rachaduras
> incandescentes de metal derretido por baixo. Uma das mãos erguida chamando o inimigo
> para a briga, provocando, com uma expressão de desafio. Brilho laranja saindo das
> fendas. Paleta de bronze, verde-azinhavre e laranja de lava.

---

## 🔥 TIER 5 — mitos. Presença esmagadora, luz dramática, sensação de "acabou"

**✅ Colosso** (3/7/11) — *aprovada na rodada 2 (virou pedra, não confunde mais com o Titã)*
> *Efeito: sempre que sofrer dano, aumenta o próprio ataque. O efeito está bem contado na
> arte atual — o problema é OUTRO: ele e o Titã de Ferro viraram quase a mesma imagem
> (dois gigantes escuros com rachaduras incandescentes num céu apocalíptico). Em miniatura
> no tabuleiro você não distingue as duas T5. A regra nova: **Colosso = PEDRA CLARA,
> Titã de Ferro = METAL PRETO.** Ele é o de 11 de vida, o inquebrável, não o de fogo.*
>
> Um colosso gigantesco esculpido em **granito claro cinza-esbranquiçado e mármore**, no
> formato de uma estátua antiga de guerreiro, com **musgo e hera crescendo nas juntas de
> pedra** e cintas de ferro enferrujado prendendo os blocos. **Nada de lava, nada de fogo,
> nada de metal derretido**: pelas rachaduras da pedra corre apenas uma **luz vermelha
> fria e seca**, mais forte nos lugares onde ele já foi golpeado e lascado. Pedaços
> quebrados faltando no ombro e no braço mostram que ele apanhou muito e continua de pé.
> Silhueta do tamanho de uma montanha, tempestade de poeira **clara e bege** aos pés dele,
> céu cinza aberto. Ângulo bem de baixo para cima. Paleta de pedra clara, bege, ferrugem e
> um vermelho contido.

**✅ Titã de Ferro** (3/7/10) — *aprovada na rodada 2*
> *Efeito: ganha +1 de ataque ao receber dano E a cada 2 turnos concede +2 de armadura a
> um aliado. Na rodada 1 os fragmentos saíram voando como EXPLOSÃO, não como armadura
> sendo doada. O truque é: em vez de vários caquinhos, **uma placa só, grande e inteira**,
> e o ombro dele visivelmente **vazio** de onde ela saiu. Também precisa ficar
> claramente METÁLICO, pra não se confundir com o Colosso de pedra.*
>
> Um titã monstruoso de **metal preto polido**, armadura recortada e angular de placas
> rebitadas — claramente **forjado, uma máquina de guerra, não uma criatura de pedra ou
> lava**. Um núcleo de metal derretido brilha através do peito aberto e vapor sai das
> juntas. **UMA única placa de blindagem grande e inteira se desprendeu do ombro DIREITO
> dele e flutua no ar em direção à lateral do quadro, brilhando em laranja; o ombro de onde
> ela saiu ficou visivelmente descoberto, mostrando a estrutura interna exposta.** Ele
> abaixa a cabeça enquanto entrega a peça — **arranca a própria blindagem para dar a
> outro**. Céu apocalíptico atrás. Paleta de preto metálico, azul elétrico e laranja do
> núcleo.

**✅ Senhor da Guerra** (3/8/9) — *aprovada*
> Um senhor da guerra supremo em armadura dourada e negra, elmo em forma de coroa de
> espinhos, manto de cota de malha, braços cruzados com calma absoluta enquanto
> silhuetas de um exército inteiro se perfilam atrás dele. Relâmpago iluminando a cena.
> Paleta de ouro, preto e luz de tempestade.

---

## 🏛️ CARTA EXCLUSIVA — não sai do baralho, nasce da tríade

**✅ Atlas, o Baluarte** (2/8/10) — *aprovado — fecha a coleção Tank*
> *Efeito: só entra em campo quando Pretoriano + Legionário + Centurião estão juntos.
> Ao entrar, concede +5 de armadura a todos os OUTROS aliados. É o lendário do set —
> merece a arte mais imponente dos Tanks, e tem que carregar o brasão da tríade.*
>
> Um colosso de guerra em armadura de bronze e mármore branco, no estilo de uma estátua
> imperial que ganhou vida — ombros largos como um arco de triunfo, elmo coroado por uma
> crista carmesim enorme. No peito dele, gravado em relevo dourado e brilhando com luz
> própria, está o **MESMO BRASÃO da ordem: uma cabeça de leão dourada de frente dentro de
> uma coroa circular de louros** — é o símbolo dos três soldados que o invocaram, agora
> gigante. Ele bate a base de um **escudo-portão colossal no chão** e da batida uma **onda
> circular de luz dourada se espalha para fora**, envolvendo silhuetas de aliados
> ajoelhados ao redor dele, que ficam banhadas por essa luz protetora. Ao fundo, apagadas
> na névoa, três capas carmesim ao vento. Ângulo bem de baixo para cima, escala
> esmagadora. Paleta de bronze, mármore, carmesim e ouro luminoso.

---

## 🧩 Molde para as outras classes

Mantenha o **mesmo bloco de estilo** e mude só a "assinatura" de cada classe:

| Classe | Paleta guia | Pose / clima |
|---|---|---|
| **Tank** | ferro, bronze, dourado, vermelho | plantado, defensivo, ângulo de baixo |
| **Arqueiro** | verde, couro, âmbar | ágil, em movimento, ao ar livre |
| **Mago** | azul, roxo, ciano | flutuando, energia nas mãos, interior arcano |
| **Healer** | branco, dourado claro, rosa suave | luz que emana do peito, expressão gentil |

E a **progressão de tier** vale para todas: T1 sem magia e luz fraca → T5 mítico
com efeito luminoso dominando a cena. É isso que faz bater o olho e saber o poder da carta.
