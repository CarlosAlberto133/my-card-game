# -*- coding: utf-8 -*-
"""Renderiza modelos FBX sem abrir o Unity, para conferir orientacao e tamanho.

Por que existe: a bounding box MENTE. O Guarda-Costas media 0.89 no Z contra
0.63 no Y e parecia deitado, mas o comprido era o escudo dele. O Porta-Bandeira
media igual aos outros e mesmo assim aparecia menor, porque metade da caixa era
bandeira. Olhar o modelo resolve em 5 segundos o que a medicao erra.

A convencao de giro aqui bate com o Unity (conferido com o abencoado, que o
Unity endireita com X -90 e aqui tambem). Um modelo esta certo quando encara
-Z, ou seja, olha para a camera nestas imagens.

Uso:
    python arte-ia/render_fbx.py                       # todas as figuras
    python arte-ia/render_fbx.py baluarte legionario   # so essas
    python arte-ia/render_fbx.py escudeiro-arcano --giros y:-90,y:90,x:-90

Precisa de: pillow e numpy.
"""
import io, os, sys, math, struct, zlib
import numpy as np
from PIL import Image, ImageDraw

PASTA = 'Assets/Resources/cards/figures'
SAIDA = 'arte-ia/render_fbx.png'
TAM = 300


def _arrays(data, nome, tipo):
    """Todos os arrays de um node do FBX binario, com deflate quando preciso."""
    tag = bytes([len(nome)]) + nome.encode()
    fmt, passo = {'d': ('<%dd', 8), 'i': ('<%di', 4)}[tipo]
    res, i = [], 0
    while True:
        i = data.find(tag, i)
        if i < 0:
            break
        k = i + len(tag)
        if data[k:k + 1] == tipo.encode():
            n, enc, comp = struct.unpack('<III', data[k + 1:k + 13])
            bruto = data[k + 13:k + 13 + (comp if enc == 1 else n * passo)]
            try:
                raw = zlib.decompress(bruto) if enc == 1 else bruto
                if len(raw) >= n * passo:
                    res.append(np.array(struct.unpack(fmt % n, raw[:n * passo])))
            except Exception:
                pass
        i = k
    return res


def malha(path):
    """Vertices + triangulos da maior malha do arquivo."""
    d = io.open(path, 'rb').read()
    vs = _arrays(d, 'Vertices', 'd')
    ix = _arrays(d, 'PolygonVertexIndex', 'i')
    if not vs or not ix:
        return None, None
    j = int(np.argmax([len(v) for v in vs]))
    V = vs[j].reshape(-1, 3)
    tris, poly = [], []
    for v in ix[min(j, len(ix) - 1)]:
        if v < 0:                      # indice negativo fecha o poligono
            poly.append(int(~v))
            for t in range(1, len(poly) - 1):
                tris.append((poly[0], poly[t], poly[t + 1]))
            poly = []
        else:
            poly.append(int(v))
    return V, np.array(tris, dtype=np.int64)


def R(eixo, graus):
    """Mesma convencao do Quaternion.Euler do Unity."""
    a = math.radians(graus)
    c, s = math.cos(a), math.sin(a)
    if eixo == 'x':
        return np.array([[1, 0, 0], [0, c, -s], [0, s, c]])
    if eixo == 'y':
        return np.array([[c, 0, s], [0, 1, 0], [-s, 0, c]])
    return np.array([[c, -s, 0], [s, c, 0], [0, 0, 1]])


def render(V, T, S=TAM, luz=np.array([0.4, 0.7, 0.6])):
    """Z-buffer de pobre: pintor (fundo primeiro) + difusa pela normal."""
    img = Image.new('RGB', (S, S), (20, 20, 24))
    if V is None or T is None or len(T) == 0:
        return img
    mn, mx = V.min(0), V.max(0)
    esc = (S - 30) / max((mx - mn)[:2].max(), 1e-6)
    P = (V - (mn + mx) / 2.0) * esc
    sx, sy = S / 2 + P[:, 0], S / 2 - P[:, 1]

    a, b, c = V[T[:, 0]], V[T[:, 1]], V[T[:, 2]]
    n = np.cross(b - a, c - a)
    comp = np.linalg.norm(n, axis=1, keepdims=True)
    comp[comp == 0] = 1
    lam = np.clip((n / comp) @ (luz / np.linalg.norm(luz)), 0, 1) * 0.75 + 0.25

    d = ImageDraw.Draw(img)
    for k in np.argsort((V[T[:, 0], 2] + V[T[:, 1], 2] + V[T[:, 2], 2]) / 3.0):
        t = T[k]
        g = int(255 * lam[k])
        d.polygon([(sx[t[0]], sy[t[0]]), (sx[t[1]], sy[t[1]]), (sx[t[2]], sy[t[2]])],
                  fill=(g, g, min(255, int(g * 1.05))))
    return img


def folha(itens, arq, cols=5):
    PAD, S = 14, TAM
    linhas = (len(itens) + cols - 1) // cols
    img = Image.new('RGB', (min(len(itens), cols) * (S + PAD) + PAD,
                            linhas * (S + PAD + 18) + PAD), (34, 34, 38))
    d = ImageDraw.Draw(img)
    for i, (rot, V, T) in enumerate(itens):
        x = PAD + (i % cols) * (S + PAD)
        y = PAD + (i // cols) * (S + PAD + 18)
        img.paste(render(V, T), (x, y))
        d.text((x + 2, y + S + 4), rot, fill=(235, 235, 235))
    img.save(arq)
    print('->', arq)


if __name__ == '__main__':
    args = [a for a in sys.argv[1:] if not a.startswith('--')]
    giros = None
    for a in sys.argv[1:]:
        if a.startswith('--giros'):
            giros = a.split('=', 1)[1] if '=' in a else sys.argv[sys.argv.index(a) + 1]

    nomes = args or sorted(f[:-4] for f in os.listdir(PASTA) if f.lower().endswith('.fbx'))
    itens = []
    for n in nomes:
        V, T = malha(os.path.join(PASTA, n + '.fbx'))
        if V is None:
            print('nao consegui ler', n)
            continue
        if giros and len(nomes) == 1:
            itens.append(('%s cru' % n, V, T))
            for g in giros.split(','):
                eixo, graus = g.split(':')
                itens.append(('%s %s' % (eixo.upper(), graus), V @ R(eixo, float(graus)).T, T))
        else:
            itens.append((n, V, T))
    folha(itens, SAIDA)
