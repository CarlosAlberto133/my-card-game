# Gera icon.ico (multi-resolucao) desenhando o "dado dourado" do jogo:
# quadrado arredondado dourado com 5 pontos escuros (o mesmo simbolo da marca
# no site). Sem depender de nenhum asset externo.
Add-Type -AssemblyName System.Drawing

$outIco = Join-Path $PSScriptRoot "icon.ico"
$sizes  = @(16, 24, 32, 48, 64, 128, 256)

# Cores da marca (iguais ao site: gold #f5c451, tinta escura #1a1405)
$gold     = [System.Drawing.Color]::FromArgb(245, 196, 81)
$goldDark = [System.Drawing.Color]::FromArgb(217, 160, 40)
$ink      = [System.Drawing.Color]::FromArgb(26, 20, 5)

function New-DieBitmap([int]$S) {
    $bmp = New-Object System.Drawing.Bitmap($S, $S, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.Clear([System.Drawing.Color]::Transparent)

    # Quadrado arredondado ocupando ~86% do canvas, centralizado
    $margin = [Math]::Round($S * 0.07)
    $side   = $S - 2 * $margin
    $radius = [Math]::Round($side * 0.26)
    $d = $radius * 2

    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $x = $margin; $y = $margin; $w = $side; $h = $side
    $path.AddArc($x, $y, $d, $d, 180, 90)
    $path.AddArc($x + $w - $d, $y, $d, $d, 270, 90)
    $path.AddArc($x + $w - $d, $y + $h - $d, $d, $d, 0, 90)
    $path.AddArc($x, $y + $h - $d, $d, $d, 90, 90)
    $path.CloseFigure()

    # Preenchimento com leve degrade dourado (topo mais claro)
    $rect = New-Object System.Drawing.Rectangle($x, $y, $w, $h)
    $brush = New-Object System.Drawing.Drawing2D.LinearGradientBrush($rect, $gold, $goldDark, 90)
    $g.FillPath($brush, $path)

    # 5 pontos (face 5 do dado), escuros
    [double]$r   = [Math]::Max(1.0, $side * 0.085)
    [double]$cx  = $margin + $side / 2.0
    [double]$cy  = $margin + $side / 2.0
    [double]$off = $side * 0.26
    $dot = New-Object System.Drawing.SolidBrush($ink)

    function Draw-Dot([double]$px, [double]$py) {
        $g.FillEllipse($dot, [float]($px - $r), [float]($py - $r), [float]($r * 2), [float]($r * 2))
    }
    Draw-Dot ($cx - $off) ($cy - $off)   # topo-esquerda
    Draw-Dot ($cx + $off) ($cy - $off)   # topo-direita
    Draw-Dot  $cx          $cy           # centro
    Draw-Dot ($cx - $off) ($cy + $off)   # baixo-esquerda
    Draw-Dot ($cx + $off) ($cy + $off)   # baixo-direita

    $g.Dispose(); $brush.Dispose(); $dot.Dispose(); $path.Dispose()
    return $bmp
}

# Monta um .ico multi-imagem na mao (cada frame como PNG embutido — suporta
# 256px e canal alfa, ao contrario do formato BMP antigo)
$pngs = @()
foreach ($s in $sizes) {
    $bmp = New-DieBitmap $s
    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $pngs += ,($ms.ToArray())
    $bmp.Dispose(); $ms.Dispose()
}

$fs = [System.IO.File]::Create($outIco)
$bw = New-Object System.IO.BinaryWriter($fs)
# ICONDIR
$bw.Write([UInt16]0)            # reserved
$bw.Write([UInt16]1)            # type = icon
$bw.Write([UInt16]$sizes.Count) # count
# ICONDIRENTRYs
$offset = 6 + 16 * $sizes.Count
for ($i = 0; $i -lt $sizes.Count; $i++) {
    $s = $sizes[$i]; $data = $pngs[$i]
    $bw.Write([Byte]($(if ($s -ge 256) { 0 } else { $s })))  # width  (0 = 256)
    $bw.Write([Byte]($(if ($s -ge 256) { 0 } else { $s })))  # height (0 = 256)
    $bw.Write([Byte]0)   # palette
    $bw.Write([Byte]0)   # reserved
    $bw.Write([UInt16]1) # planes
    $bw.Write([UInt16]32)# bpp
    $bw.Write([UInt32]$data.Length)
    $bw.Write([UInt32]$offset)
    $offset += $data.Length
}
foreach ($data in $pngs) { $bw.Write($data) }
$bw.Flush(); $bw.Close(); $fs.Close()

Write-Output ("icon.ico gerado: {0} ({1} bytes, {2} resolucoes)" -f $outIco, (Get-Item $outIco).Length, $sizes.Count)
