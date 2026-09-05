# ============================================================
#  Cardsworn - Launcher  (v4)
#
#  v4: o jogo se chamava "Card Game" ate set/2026. Como todo mundo
#  reinstalou na virada do nome, nada aqui responde mais pelo nome
#  antigo - o repo, o zip e a pasta de instalacao mudaram todos de uma
#  vez. A unica heranca e a limpeza da pasta velha, la embaixo.
#  Baixa automaticamente a versao mais nova publicada no
#  GitHub Releases e abre o jogo.
#
#  v2: o jogo agora e instalado em %LOCALAPPDATA%\Cardsworn
#  (fora do OneDrive/Desktop sincronizado, que travava a
#  extracao), download mais robusto e log em launcher.log.
#
#  v3: NAO usa mais a api.github.com (limite de 60 req/hora
#  por IP -> dava "403 Proibido" que virava "sem conexao",
#  ainda pior com varios amigos no mesmo provedor/CGNAT).
#  Agora le a versao pelo REDIRECT da pagina de releases
#  (github.com/.../releases/latest -> .../tag/vXX), que nao
#  tem esse limite, e monta a URL de download pela convencao.
# ============================================================

# ---------- CONFIGURACAO ----------
#  Cuidado ao mexer nestas tres linhas: este launcher NAO se auto-atualiza.
#  Quem instalou tem uma copia deste arquivo gravada no PC, entao mudar o
#  repo ou o nome do zip derruba a atualizacao de todos de uma vez - so
#  volta quem reinstalar pelo site. Foi exatamente o que aconteceu na
#  virada do nome, e por isso a virada exigiu que todos reinstalassem.
$RepoOwner = "CarlosAlberto133"        # seu usuario do GitHub
$RepoName  = "cardsworn-releases"      # repositorio PUBLICO so para as builds
$AssetName = "cardsworn.zip"           # nome do .zip que voce sobe em cada release

# Login com Google (Supabase) — a sessao vai para session.json e o JOGO a usa
# para salvar as partidas/logs na conta do jogador
$SupabaseUrl  = "https://zutdbgltjphsbakeeoda.supabase.co"
$SupabaseKey  = "sb_publishable_sIC5NDivItmQ_IuVOmWSdQ_LnyaSSOO"
$AuthPort     = 53682                  # porta local que recebe o retorno do Google
# -----------------------------------------------------------------

# GitHub exige TLS 1.2 (o PowerShell antigo usa 1.0 por padrao e falharia)
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

# ---------- Caminhos ----------
# O jogo mora SEMPRE em LocalAppData: pasta local, rapida e fora de
# qualquer sincronizacao (OneDrive etc.). O launcher pode ficar onde quiser.
$InstallRoot = Join-Path $env:LOCALAPPDATA "Cardsworn"
$GameDir     = Join-Path $InstallRoot "game"
$VersionFile = Join-Path $InstallRoot "installed.txt"
$ZipTemp     = Join-Path $InstallRoot "update.zip"
$LogFile     = Join-Path $InstallRoot "launcher.log"
$SessionFile = Join-Path $InstallRoot "session.json"   # sessao do login (o jogo le daqui)

if (-not (Test-Path $InstallRoot)) { New-Item -ItemType Directory -Path $InstallRoot | Out-Null }

function Write-Log([string]$msg) {
    try { Add-Content -Path $LogFile -Value ("[{0}] {1}" -f (Get-Date -Format "yyyy-MM-dd HH:mm:ss"), $msg) } catch {}
}
Write-Log "----- Launcher iniciado -----"

# ---------- Evita duas instancias abertas ao mesmo tempo ----------
$mutex = New-Object System.Threading.Mutex($false, "CardswornLauncherMutex")
if (-not $mutex.WaitOne(0, $false)) {
    [System.Windows.Forms.MessageBox]::Show("O launcher do Cardsworn ja esta aberto.", "Cardsworn") | Out-Null
    exit
}

# ---------- Limpa instalacao antiga ao lado do launcher (versoes v1) ----------
$OldRoot = Split-Path -Parent $MyInvocation.MyCommand.Definition
foreach ($legacy in @((Join-Path $OldRoot "game"), (Join-Path $OldRoot "installed.txt"))) {
    if (Test-Path $legacy) {
        try { Remove-Item $legacy -Recurse -Force -ErrorAction Stop; Write-Log "Removido legado: $legacy" } catch {}
    }
}

# ---------- Recolhe a instalacao do tempo do nome "Card Game" ----------
# O jogo morava em %LOCALAPPDATA%\CardGame. Sem isto, a pasta antiga (com
# a build inteira dentro) ficaria esquecida no PC de todo mundo para
# sempre. O session.json e trazido junto para o jogador nao ter que
# entrar com o Google de novo.
$LegacyRoot = Join-Path $env:LOCALAPPDATA "CardGame"
if (Test-Path $LegacyRoot) {
    $legacySession = Join-Path $LegacyRoot "session.json"
    if ((Test-Path $legacySession) -and -not (Test-Path $SessionFile)) {
        try {
            Copy-Item $legacySession $SessionFile -Force -ErrorAction Stop
            Write-Log "Login herdado da instalacao antiga"
        } catch { Write-Log "Nao consegui herdar o login: $($_.Exception.Message)" }
    }
    try {
        Remove-Item $LegacyRoot -Recurse -Force -ErrorAction Stop
        Write-Log "Pasta antiga removida: $LegacyRoot"
    } catch { Write-Log "Pasta antiga resistiu: $($_.Exception.Message)" }
}

# ---------- Estado ----------
$script:latestTag   = $null
$script:assetUrl    = $null
$script:assetSize   = 0
$script:dlTask      = $null
$script:webClient   = $null
$script:timer       = $null   # PRECISA ser script: — variavel local da funcao
$script:installDone = $false  # nao existe mais quando o evento Tick dispara!

# Login com Google
$script:authListener = $null
$script:authCtxTask  = $null
$script:authTimer    = $null
$script:pkceVerifier = $null
$script:authDeadline = $null

# ============================================================
#  Janela
# ============================================================
#  Layout em duas colunas, no estilo dos launchers de MMO: as
#  novidades ocupam a area grande da esquerda e a coluna da
#  direita concentra conta, status, progresso e o botao Jogar.
#
#  Continua sendo WinForms (e nao WPF) de proposito: o launcher
#  e a UNICA peca que nao se auto-atualiza, entao uma reescrita
#  para outro toolkit obrigaria todo mundo a reinstalar se algo
#  desse errado. Aqui a pintura e feita na mao com GDI+ (o mesmo
#  que o make-icon.ps1 ja usa), e as variaveis dos controles tem
#  os MESMOS nomes de antes, entao nenhuma linha da logica de
#  download, update ou login precisou mudar.
# ============================================================

# ---------- Paleta (identica a do site) ----------
$C_Bg      = [System.Drawing.Color]::FromArgb(21, 16, 10)
$C_Bg2     = [System.Drawing.Color]::FromArgb(36, 26, 16)
$C_Panel   = [System.Drawing.Color]::FromArgb(31, 22, 13)
$C_Line    = [System.Drawing.Color]::FromArgb(58, 46, 31)
$C_Ink     = [System.Drawing.Color]::FromArgb(243, 232, 211)
$C_Muted   = [System.Drawing.Color]::FromArgb(182, 160, 124)
$C_Muted2  = [System.Drawing.Color]::FromArgb(138, 120, 92)
$C_Gold    = [System.Drawing.Color]::FromArgb(245, 196, 81)
$C_Gold2   = [System.Drawing.Color]::FromArgb(255, 216, 118)
$C_GoldDk  = [System.Drawing.Color]::FromArgb(185, 138, 36)
$C_Green   = [System.Drawing.Color]::FromArgb(79, 208, 160)
$C_Red     = [System.Drawing.Color]::FromArgb(255, 91, 74)

$F_Title   = New-Object System.Drawing.Font("Segoe UI", 21, [System.Drawing.FontStyle]::Bold)
$F_H       = New-Object System.Drawing.Font("Segoe UI", 10.5, [System.Drawing.FontStyle]::Bold)
$F_Body    = New-Object System.Drawing.Font("Segoe UI", 9)
$F_Small   = New-Object System.Drawing.Font("Segoe UI", 8)
$F_Btn     = New-Object System.Drawing.Font("Segoe UI", 13, [System.Drawing.FontStyle]::Bold)
$F_Mono    = New-Object System.Drawing.Font("Consolas", 8)

# Retangulo arredondado reaproveitado por todo mundo aqui
function New-RoundPath([int]$x, [int]$y, [int]$w, [int]$h, [int]$r) {
    $p = New-Object System.Drawing.Drawing2D.GraphicsPath
    $d = $r * 2
    if ($d -gt $w) { $d = $w }
    if ($d -gt $h) { $d = $h }
    $p.AddArc($x, $y, $d, $d, 180, 90)
    $p.AddArc($x + $w - $d, $y, $d, $d, 270, 90)
    $p.AddArc($x + $w - $d, $y + $h - $d, $d, $d, 0, 90)
    $p.AddArc($x, $y + $h - $d, $d, $d, 90, 90)
    $p.CloseFigure()
    return $p
}

# ---------- Janela sem borda (a barra de titulo e desenhada aqui) ----------
$form = New-Object System.Windows.Forms.Form
$form.Text            = "Cardsworn"
$form.ClientSize      = New-Object System.Drawing.Size(940, 588)
$form.StartPosition   = "CenterScreen"
$form.FormBorderStyle = "None"
$form.MaximizeBox     = $false
$form.BackColor       = $C_Bg
# DoubleBuffered e protegida: sem isso a janela pisca ao redesenhar, e o
# unico caminho pelo PowerShell e por reflexao
try {
    $form.GetType().GetProperty("DoubleBuffered",
        [Reflection.BindingFlags]"Instance,NonPublic").SetValue($form, $true, $null)
} catch { Write-Log "DoubleBuffered nao aplicado: $($_.Exception.Message)" }

# O icone do dado dourado. Sem ele a barra de tarefas mostra o icone do
# PowerShell, que e quem de fato hospeda esta janela. O AppUserModelID
# desgruda a janela do grupo "Windows PowerShell" na barra de tarefas -
# e best-effort: se a P/Invoke falhar, o icone da janela ja resolve o
# grosso e o launcher segue funcionando.
$IconPath = Join-Path $PSScriptRoot "icon.ico"
if (Test-Path $IconPath) {
    try { $form.Icon = New-Object System.Drawing.Icon($IconPath) } catch {}
}
try {
    Add-Type -Namespace Shell32 -Name Win -MemberDefinition @'
[System.Runtime.InteropServices.DllImport("shell32.dll", SetLastError=true)]
public static extern int SetCurrentProcessExplicitAppUserModelID(
    [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] string AppID);
'@ -ErrorAction Stop
    [void][Shell32.Win]::SetCurrentProcessExplicitAppUserModelID("Cardsworn.Launcher")
} catch { Write-Log "AppUserModelID nao aplicado: $($_.Exception.Message)" }

# Fundo: degrade quente de cima para baixo, como a mesa a luz de vela
$form.add_Paint({
    param($s, $e)
    $r = New-Object System.Drawing.Rectangle(0, 0, $s.ClientSize.Width, $s.ClientSize.Height)
    $b = New-Object System.Drawing.Drawing2D.LinearGradientBrush($r, $C_Bg2, $C_Bg, 90)
    $e.Graphics.FillRectangle($b, $r)
    $b.Dispose()
    # Fio dourado separando a barra de titulo
    $pen = New-Object System.Drawing.Pen($C_Line, 1)
    $e.Graphics.DrawLine($pen, 0, 44, $s.ClientSize.Width, 44)
    $e.Graphics.DrawRectangle($pen, 0, 0, $s.ClientSize.Width - 1, $s.ClientSize.Height - 1)
    $pen.Dispose()
})

# ---------- Barra de titulo propria (arrastar + minimizar + fechar) ----------
$bar_top = New-Object System.Windows.Forms.Panel
$bar_top.Size      = New-Object System.Drawing.Size(940, 44)
$bar_top.Location  = New-Object System.Drawing.Point(0, 0)
$bar_top.BackColor = [System.Drawing.Color]::Transparent
$form.Controls.Add($bar_top)

$bar_top.add_Paint({
    param($s, $e)
    $g = $e.Graphics
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    # Dado dourado em miniatura
    $p = New-RoundPath 16 12 20 20 6
    $rr = New-Object System.Drawing.Rectangle(16, 12, 20, 20)
    $br = New-Object System.Drawing.Drawing2D.LinearGradientBrush($rr, $C_Gold2, $C_GoldDk, 90)
    $g.FillPath($br, $p); $br.Dispose(); $p.Dispose()
    $ink = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(26, 20, 5))
    foreach ($pt in @(@(21,17), @(29,17), @(25,21), @(21,25), @(29,25))) {
        $g.FillEllipse($ink, $pt[0], $pt[1], 3.2, 3.2)
    }
    $ink.Dispose()
    # CARD + SWORN, o segundo em dourado (igual ao logo do site)
    $fb = New-Object System.Drawing.Font("Segoe UI", 11, [System.Drawing.FontStyle]::Bold)
    $w1 = $g.MeasureString("CARD", $fb).Width
    $g.DrawString("CARD", $fb, (New-Object System.Drawing.SolidBrush($C_Ink)), 46, 13)
    $g.DrawString("SWORN", $fb, (New-Object System.Drawing.SolidBrush($C_Gold)), (46 + $w1 - 4), 13)
    $fb.Dispose()
})

# Arrastar a janela pela barra (a janela nao tem moldura do Windows)
$script:dragging = $false
$script:dragOff  = New-Object System.Drawing.Point(0, 0)
$bar_top.add_MouseDown({ param($s,$e) if ($e.Button -eq "Left") { $script:dragging = $true; $script:dragOff = $e.Location } })
$bar_top.add_MouseUp({   $script:dragging = $false })
$bar_top.add_MouseMove({
    param($s, $e)
    if ($script:dragging) {
        $form.Location = New-Object System.Drawing.Point(
            ($form.Location.X + $e.X - $script:dragOff.X),
            ($form.Location.Y + $e.Y - $script:dragOff.Y))
    }
})

function New-TitleButton([string]$glifo, [int]$x, [scriptblock]$acao, [System.Drawing.Color]$hover) {
    $b = New-Object System.Windows.Forms.Label
    $b.Text      = $glifo
    $b.Font      = New-Object System.Drawing.Font("Segoe UI", 11)
    $b.ForeColor = $C_Muted
    $b.Size      = New-Object System.Drawing.Size(40, 44)
    $b.Location  = New-Object System.Drawing.Point($x, 0)
    $b.TextAlign = "MiddleCenter"
    $b.add_MouseEnter({ param($s,$e) $s.ForeColor = $hover }.GetNewClosure())
    $b.add_MouseLeave({ param($s,$e) $s.ForeColor = $C_Muted }.GetNewClosure())
    $b.add_Click($acao)
    $bar_top.Controls.Add($b)
    return $b
}
[void](New-TitleButton "-" 856 { $form.WindowState = "Minimized" } $C_Ink)
[void](New-TitleButton "X" 896 { $form.Close() } $C_Red)

# ---------- Coluna esquerda: novidades ----------
$newsHead = New-Object System.Windows.Forms.Label
$newsHead.Text      = "NOVIDADES"
$newsHead.Font      = New-Object System.Drawing.Font("Consolas", 9, [System.Drawing.FontStyle]::Bold)
$newsHead.ForeColor = $C_Muted2
$newsHead.AutoSize  = $false
$newsHead.Size      = New-Object System.Drawing.Size(300, 20)
$newsHead.Location  = New-Object System.Drawing.Point(28, 62)
$newsHead.BackColor = [System.Drawing.Color]::Transparent
$form.Controls.Add($newsHead)

$newsBox = New-Object System.Windows.Forms.Panel
$newsBox.Size       = New-Object System.Drawing.Size(568, 424)
$newsBox.Location   = New-Object System.Drawing.Point(24, 88)
$newsBox.BackColor  = [System.Drawing.Color]::Transparent
$newsBox.AutoScroll = $false
$form.Controls.Add($newsBox)

$verTudo = New-Object System.Windows.Forms.Label
$verTudo.Text      = "Ver todas as novidades no forum  >"
$verTudo.Font      = New-Object System.Drawing.Font("Segoe UI", 9, [System.Drawing.FontStyle]::Bold)
$verTudo.ForeColor = $C_Gold
$verTudo.AutoSize  = $false
$verTudo.Size      = New-Object System.Drawing.Size(300, 22)
$verTudo.Location  = New-Object System.Drawing.Point(28, 524)
$verTudo.BackColor = [System.Drawing.Color]::Transparent
$verTudo.Cursor    = "Hand"
$verTudo.add_Click({ Start-Process "https://cardsworn.vercel.app/forum" })
$verTudo.add_MouseEnter({ param($s,$e) $s.ForeColor = $C_Gold2 })
$verTudo.add_MouseLeave({ param($s,$e) $s.ForeColor = $C_Gold })
$form.Controls.Add($verTudo)

# ---------- Coluna direita ----------
$RX = 624          # x da coluna
$RW = 292          # largura util

$brand = New-Object System.Windows.Forms.Label
$brand.Text      = "CARDSWORN"
$brand.Font      = $F_Title
$brand.ForeColor = $C_Gold
$brand.AutoSize  = $false
$brand.Size      = New-Object System.Drawing.Size($RW, 34)
$brand.Location  = New-Object System.Drawing.Point($RX, 74)
$brand.TextAlign = "MiddleCenter"
$brand.BackColor = [System.Drawing.Color]::Transparent
$form.Controls.Add($brand)

$tagline = New-Object System.Windows.Forms.Label
$tagline.Text      = "duelos de cartas na mesa de RPG"
$tagline.Font      = $F_Small
$tagline.ForeColor = $C_Muted2
$tagline.AutoSize  = $false
$tagline.Size      = New-Object System.Drawing.Size($RW, 18)
$tagline.Location  = New-Object System.Drawing.Point($RX, 108)
$tagline.TextAlign = "MiddleCenter"
$tagline.BackColor = [System.Drawing.Color]::Transparent
$form.Controls.Add($tagline)

# --- Cartao da conta ---
$acctCard = New-Object System.Windows.Forms.Panel
$acctCard.Size      = New-Object System.Drawing.Size($RW, 96)
$acctCard.Location  = New-Object System.Drawing.Point($RX, 146)
$acctCard.BackColor = [System.Drawing.Color]::Transparent
$form.Controls.Add($acctCard)
$acctCard.add_Paint({
    param($s, $e)
    $e.Graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $p = New-RoundPath 0 0 ($s.Width - 1) ($s.Height - 1) 12
    $e.Graphics.FillPath((New-Object System.Drawing.SolidBrush($C_Panel)), $p)
    $e.Graphics.DrawPath((New-Object System.Drawing.Pen($C_Line, 1)), $p)
    $p.Dispose()
})

$userLabel = New-Object System.Windows.Forms.Label
$userLabel.Text      = "Voce nao esta logado. Entre para salvar suas partidas!"
$userLabel.Font      = $F_Body
$userLabel.ForeColor = $C_Muted
$userLabel.AutoSize  = $false
$userLabel.TextAlign = "MiddleCenter"
$userLabel.Size      = New-Object System.Drawing.Size(($RW - 24), 34)
$userLabel.Location  = New-Object System.Drawing.Point(12, 10)
$userLabel.BackColor = [System.Drawing.Color]::Transparent
$acctCard.Controls.Add($userLabel)

$authBtn = New-Object System.Windows.Forms.Button
$authBtn.Text      = "Entrar com Google"
$authBtn.Font      = New-Object System.Drawing.Font("Segoe UI", 9.5, [System.Drawing.FontStyle]::Bold)
$authBtn.Size      = New-Object System.Drawing.Size(($RW - 32), 32)
$authBtn.Location  = New-Object System.Drawing.Point(16, 50)
$authBtn.FlatStyle = "Flat"
$authBtn.BackColor = [System.Drawing.Color]::White
$authBtn.ForeColor = [System.Drawing.Color]::FromArgb(30, 30, 30)
$authBtn.FlatAppearance.BorderSize = 0
$authBtn.Cursor    = "Hand"
$acctCard.Controls.Add($authBtn)
$authBtn.Region = New-Object System.Drawing.Region((New-RoundPath 0 0 $authBtn.Width $authBtn.Height 9))

# --- Atalhos ---
function New-LinkBotao([string]$texto, [int]$y, [string]$url) {
    $b = New-Object System.Windows.Forms.Button
    $b.Text      = $texto
    $b.Font      = New-Object System.Drawing.Font("Segoe UI", 9.5)
    $b.Size      = New-Object System.Drawing.Size($RW, 34)
    $b.Location  = New-Object System.Drawing.Point($RX, $y)
    $b.FlatStyle = "Flat"
    # Sem borda: a Region arredondada corta 1px de contorno e o resultado
    # vira um risco embaixo do texto. O fundo do painel ja separa do fundo.
    $b.BackColor = $C_Panel
    $b.ForeColor = $C_Muted
    $b.FlatAppearance.BorderSize = 0
    $b.TextAlign = "MiddleLeft"
    $b.Padding   = New-Object System.Windows.Forms.Padding(14, 0, 0, 0)
    $b.Cursor    = "Hand"
    $b.add_Click({ Start-Process $url }.GetNewClosure())
    $b.add_MouseEnter({ param($s,$e) $s.ForeColor = $C_Ink }.GetNewClosure())
    $b.add_MouseLeave({ param($s,$e) $s.ForeColor = $C_Muted }.GetNewClosure())
    $form.Controls.Add($b)
    $b.Region = New-Object System.Drawing.Region((New-RoundPath 0 0 $b.Width $b.Height 9))
    return $b
}
[void](New-LinkBotao "Site do jogo"        262 "https://cardsworn.vercel.app")
[void](New-LinkBotao "Novidades e bugs"    304 "https://cardsworn.vercel.app/forum")
[void](New-LinkBotao "Reportar um problema" 346 "https://cardsworn.vercel.app/forum")

# Onde o jogo mora, para quem quiser apagar na mao
$pathLabel = New-Object System.Windows.Forms.Label
$pathLabel.Text      = "Instalado em %LOCALAPPDATA%\Cardsworn"
$pathLabel.Font      = $F_Mono
$pathLabel.ForeColor = [System.Drawing.Color]::FromArgb(104, 90, 68)
$pathLabel.AutoSize  = $false
$pathLabel.TextAlign = "MiddleLeft"
$pathLabel.Size      = New-Object System.Drawing.Size($RW, 18)
$pathLabel.Location  = New-Object System.Drawing.Point($RX, 390)
$pathLabel.BackColor = [System.Drawing.Color]::Transparent
$form.Controls.Add($pathLabel)

# --- Status + barra de progresso ---
$status = New-Object System.Windows.Forms.Label
$status.Text      = "Verificando atualizacoes..."
$status.Font      = $F_Body
$status.ForeColor = $C_Muted
$status.AutoSize  = $false
$status.TextAlign = "MiddleLeft"
$status.Size      = New-Object System.Drawing.Size($RW, 22)
$status.Location  = New-Object System.Drawing.Point($RX, 424)
$status.BackColor = [System.Drawing.Color]::Transparent
$form.Controls.Add($status)

# Barra dourada desenhada na mao. O ProgressBar do Windows nao aceita cor
# (ele segue o tema do sistema), entao aqui e um Panel pintado: guarda o
# valor no .Tag e Set-Progress redesenha.
$bar = New-Object System.Windows.Forms.Panel
$bar.Size      = New-Object System.Drawing.Size($RW, 10)
$bar.Location  = New-Object System.Drawing.Point($RX, 450)
$bar.BackColor = [System.Drawing.Color]::Transparent
$bar.Tag       = 0
$form.Controls.Add($bar)
$bar.add_Paint({
    param($s, $e)
    $g = $e.Graphics
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $trilho = New-RoundPath 0 0 ($s.Width - 1) ($s.Height - 1) 5
    $g.FillPath((New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(46, 35, 22))), $trilho)
    $trilho.Dispose()
    $pct = [int]$s.Tag
    if ($pct -gt 0) {
        $w = [int](($s.Width - 1) * $pct / 100)
        if ($w -lt 10) { $w = 10 }
        $cheio = New-RoundPath 0 0 $w ($s.Height - 1) 5
        $rr = New-Object System.Drawing.Rectangle(0, 0, $w, $s.Height)
        $br = New-Object System.Drawing.Drawing2D.LinearGradientBrush($rr, $C_Gold2, $C_GoldDk, 0)
        $g.FillPath($br, $cheio); $br.Dispose(); $cheio.Dispose()
    }
})

function Set-Progress([int]$pct) {
    if ($pct -lt 0)   { $pct = 0 }
    if ($pct -gt 100) { $pct = 100 }
    $bar.Tag = $pct
    $bar.Invalidate()
    $bar.Update()
}

# --- Botao Jogar ---
$playBtn = New-Object System.Windows.Forms.Button
$playBtn.Text      = "JOGAR"
$playBtn.Font      = $F_Btn
$playBtn.Size      = New-Object System.Drawing.Size($RW, 52)
$playBtn.Location  = New-Object System.Drawing.Point($RX, 474)
$playBtn.FlatStyle = "Flat"
$playBtn.BackColor = $C_Gold
$playBtn.ForeColor = [System.Drawing.Color]::FromArgb(26, 20, 5)
$playBtn.FlatAppearance.BorderSize = 0
$playBtn.Enabled   = $false
$playBtn.Cursor    = "Hand"
$form.Controls.Add($playBtn)
$playBtn.Region = New-Object System.Drawing.Region((New-RoundPath 0 0 $playBtn.Width $playBtn.Height 12))
# Enabled=false no WinForms deixa o texto cinza-claro ilegivel sobre o ouro:
# repinta o botao conforme o estado
$playBtn.add_EnabledChanged({
    param($s, $e)
    if ($s.Enabled) {
        $s.BackColor = $C_Gold
        $s.ForeColor = [System.Drawing.Color]::FromArgb(26, 20, 5)
    } else {
        $s.BackColor = [System.Drawing.Color]::FromArgb(58, 46, 31)
        $s.ForeColor = $C_Muted2
    }
})
$playBtn.BackColor = [System.Drawing.Color]::FromArgb(58, 46, 31)
$playBtn.ForeColor = $C_Muted2

$verLabel = New-Object System.Windows.Forms.Label
$verLabel.Text      = ""
$verLabel.Font      = $F_Mono
$verLabel.ForeColor = $C_Muted2
$verLabel.AutoSize  = $false
$verLabel.TextAlign = "MiddleRight"
$verLabel.Size      = New-Object System.Drawing.Size($RW, 18)
$verLabel.Location  = New-Object System.Drawing.Point($RX, 536)
$verLabel.BackColor = [System.Drawing.Color]::Transparent
$form.Controls.Add($verLabel)

# ---------- Novidades: leitura da mesma tabela que o forum usa ----------
# O forum do site le `posts` no Supabase; aqui vai a mesma consulta pela API
# REST, so que limitada aos 6 mais recentes. Se falhar (offline, Supabase
# fora), o launcher mostra um aviso discreto e continua funcionando - a
# noticia e enfeite, o que importa e o botao Jogar.
function Get-Noticias {
    $url = "$SupabaseUrl/rest/v1/posts?select=category,title,body,version,created_at" +
           "&order=created_at.desc&limit=6"
    $req = [System.Net.HttpWebRequest]::Create($url)
    $req.UserAgent = "CardswornLauncher"
    $req.Method    = "GET"
    $req.Timeout   = 10000
    $req.Headers.Add("apikey", $SupabaseKey)
    $req.Headers.Add("Authorization", "Bearer $SupabaseKey")
    $resp = $req.GetResponse()
    try {
        $sr = New-Object System.IO.StreamReader($resp.GetResponseStream())
        $txt = $sr.ReadToEnd(); $sr.Close()
    } finally { $resp.Close() }
    return ($txt | ConvertFrom-Json)
}

function Add-CartaoNoticia($post, [int]$y) {
    $card = New-Object System.Windows.Forms.Panel
    $card.Size      = New-Object System.Drawing.Size(536, 96)
    $card.Location  = New-Object System.Drawing.Point(4, $y)
    $card.BackColor = [System.Drawing.Color]::Transparent
    $card.Tag       = $post
    $newsBox.Controls.Add($card)

    $card.add_Paint({
        param($s, $e)
        $g = $e.Graphics
        $g.SmoothingMode     = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
        $g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::ClearTypeGridFit
        $p = New-RoundPath 0 0 ($s.Width - 1) ($s.Height - 1) 12
        $g.FillPath((New-Object System.Drawing.SolidBrush($C_Panel)), $p)
        $g.DrawPath((New-Object System.Drawing.Pen($C_Line, 1)), $p)
        $p.Dispose()

        $po = $s.Tag
        $ehBug = ($po.category -eq "bug")
        $cor   = if ($ehBug) { $C_Red } else { $C_Gold }
        $rot   = if ($ehBug) { "BUG" } else { "ATUALIZACAO" }

        # Selo da categoria
        $fSelo = New-Object System.Drawing.Font("Consolas", 7.5, [System.Drawing.FontStyle]::Bold)
        $wSelo = [int]$g.MeasureString($rot, $fSelo).Width + 16
        $selo  = New-RoundPath 16 14 $wSelo 18 9
        $g.FillPath((New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(38, $cor.R, $cor.G, $cor.B))), $selo)
        $g.DrawPath((New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(110, $cor.R, $cor.G, $cor.B), 1)), $selo)
        $g.DrawString($rot, $fSelo, (New-Object System.Drawing.SolidBrush($cor)), 24, 17)
        $selo.Dispose(); $fSelo.Dispose()

        # Versao e data, alinhadas a direita
        $dir = ""
        if ($po.version) { $dir = "$($po.version)   " }
        if ($po.created_at) {
            try { $dir += ([datetime]$po.created_at).ToString("dd/MM/yyyy") } catch {}
        }
        $fD = New-Object System.Drawing.Font("Consolas", 7.5)
        $wD = $g.MeasureString($dir, $fD).Width
        $g.DrawString($dir, $fD, (New-Object System.Drawing.SolidBrush($C_Muted2)), ($s.Width - 16 - $wD), 17)
        $fD.Dispose()

        # Titulo
        $fT = New-Object System.Drawing.Font("Segoe UI", 11, [System.Drawing.FontStyle]::Bold)
        $rT = New-Object System.Drawing.RectangleF(16, 38, ($s.Width - 32), 22)
        $sf = New-Object System.Drawing.StringFormat
        $sf.Trimming = [System.Drawing.StringTrimming]::EllipsisCharacter
        $sf.FormatFlags = [System.Drawing.StringFormatFlags]::NoWrap
        $g.DrawString([string]$po.title, $fT, (New-Object System.Drawing.SolidBrush($C_Ink)), $rT, $sf)
        $fT.Dispose()

        # Resumo do corpo: 2 linhas, sem marcacao
        $corpo = [string]$po.body
        $corpo = $corpo -replace '<[^>]+>', ' ' -replace '[#*_`>]', ' '
        $corpo = $corpo -replace '[^\u0020-\u007E\u00C0-\u00FF]', ' ' -replace '\s+', ' '
        $corpo = $corpo.Trim()
        $fB = New-Object System.Drawing.Font("Segoe UI", 8.5)
        $rB = New-Object System.Drawing.RectangleF(16, 60, ($s.Width - 32), 30)
        $sf2 = New-Object System.Drawing.StringFormat
        $sf2.Trimming = [System.Drawing.StringTrimming]::EllipsisWord
        $g.DrawString($corpo, $fB, (New-Object System.Drawing.SolidBrush($C_Muted)), $rB, $sf2)
        $fB.Dispose()
    })

    # Clicar no cartao abre o forum no navegador
    $abrir = { Start-Process "https://cardsworn.vercel.app/forum" }
    $card.add_Click($abrir)
    $card.Cursor = "Hand"
    return $card
}

function Carregar-Noticias {
    $newsBox.Controls.Clear()
    try {
        $posts = Get-Noticias
        if (-not $posts -or $posts.Count -eq 0) {
            $vazio = New-Object System.Windows.Forms.Label
            $vazio.Text      = "Ainda nao ha novidades publicadas."
            $vazio.Font      = $F_Body
            $vazio.ForeColor = $C_Muted2
            $vazio.AutoSize  = $false
            $vazio.Size      = New-Object System.Drawing.Size(500, 24)
            $vazio.Location  = New-Object System.Drawing.Point(6, 6)
            $vazio.BackColor = [System.Drawing.Color]::Transparent
            $newsBox.Controls.Add($vazio)
            return
        }
        $y = 0
        foreach ($p in ($posts | Select-Object -First 4)) {
            [void](Add-CartaoNoticia $p $y)
            $y += 106
        }
        Write-Log "Novidades carregadas: $($posts.Count)"
    } catch {
        Write-Log "Novidades indisponiveis: $($_.Exception.Message)"
        $erro = New-Object System.Windows.Forms.Label
        $erro.Text      = "Nao consegui carregar as novidades agora."
        $erro.Font      = $F_Body
        $erro.ForeColor = $C_Muted2
        $erro.AutoSize  = $false
        $erro.Size      = New-Object System.Drawing.Size(500, 24)
        $erro.Location  = New-Object System.Drawing.Point(6, 6)
        $erro.BackColor = [System.Drawing.Color]::Transparent
        $newsBox.Controls.Add($erro)
    }
}

# ============================================================
#  Funcoes
# ============================================================
function Set-Status([string]$text) {
    $status.Text = $text
    $status.Refresh()   # atualiza mesmo se a interface for travar logo em seguida
}

function Set-Ready([string]$text) {
    Set-Status $text
    Set-Progress 100
    $playBtn.Enabled = $true
}

# Localiza o .exe do jogo (ignora o UnityCrashHandler)
function Get-GameExe {
    if (-not (Test-Path $GameDir)) { return $null }
    $exe = Get-ChildItem -Path $GameDir -Recurse -Filter *.exe -ErrorAction SilentlyContinue |
           Where-Object { $_.Name -notlike "UnityCrashHandler*" } |
           Sort-Object { if ($_.Name -ieq "Cardsworn.exe") { 0 } else { 1 } } |
           Select-Object -First 1
    if ($exe) { return $exe.FullName } else { return $null }
}

# Extrai o zip baixado por cima da pasta 'game'
function Install-Game {
    Write-Log "Instalando em $GameDir"
    if (Test-Path $GameDir) { Remove-Item $GameDir -Recurse -Force }
    New-Item -ItemType Directory -Path $GameDir | Out-Null

    # O antivirus pode segurar o zip recem-baixado por 1-2s: tenta algumas vezes
    $attempt = 0
    while ($true) {
        $attempt++
        try {
            Expand-Archive -Path $ZipTemp -DestinationPath $GameDir -Force -ErrorAction Stop
            break
        } catch {
            Write-Log ("Extracao falhou (tentativa {0}): {1}" -f $attempt, $_.Exception.Message)
            if ($attempt -ge 5) { throw }
            Start-Sleep -Milliseconds 1200
        }
    }

    # Confere se realmente saiu um executavel do zip
    if (-not (Get-GameExe)) { throw "O zip foi extraido mas nenhum executavel foi encontrado." }

    Set-Content -Path $VersionFile -Value $script:latestTag -Encoding UTF8
    Remove-Item $ZipTemp -Force -ErrorAction SilentlyContinue
    Write-Log "Instalacao concluida: $($script:latestTag)"
}

# Baixa o zip em segundo plano SEM eventos (Task do .NET) e acompanha o
# progresso pelo tamanho do arquivo — mais robusto que os eventos do WebClient
function Start-Download {
    Set-Status "Baixando atualizacao..."
    Write-Log "Baixando: $($script:assetUrl) ($([math]::Round($script:assetSize/1MB,1)) MB)"

    Remove-Item $ZipTemp -Force -ErrorAction SilentlyContinue

    $script:webClient = New-Object System.Net.WebClient
    $script:webClient.Headers.Add("User-Agent", "CardswornLauncher")
    $script:dlTask = $script:webClient.DownloadFileTaskAsync($script:assetUrl, $ZipTemp)

    $script:installDone = $false
    $script:timer = New-Object System.Windows.Forms.Timer
    $script:timer.Interval = 300
    $script:timer.add_Tick({
        # Progresso = tamanho atual do arquivo / tamanho informado pela API
        if ($script:assetSize -gt 0 -and (Test-Path $ZipTemp)) {
            $item = Get-Item $ZipTemp -ErrorAction SilentlyContinue
            if ($item) {
                $pct = [Math]::Max(0, [Math]::Min(100, [int](100 * $item.Length / $script:assetSize)))
                Set-Progress $pct
            }
        }

        if ($script:dlTask -eq $null -or -not $script:dlTask.IsCompleted) { return }

        # Trava dupla contra reexecucao: para o timer (agora em escopo script:,
        # visivel daqui de dentro) E marca que a instalacao ja rodou
        $script:timer.Stop()
        if ($script:installDone) { return }
        $script:installDone = $true

        $script:webClient.Dispose()

        if ($script:dlTask.IsFaulted) {
            $err = "desconhecido"
            if ($script:dlTask.Exception -and $script:dlTask.Exception.InnerException) {
                $err = $script:dlTask.Exception.InnerException.Message
            }
            Write-Log "Download falhou: $err"
            Set-Status "Erro no download. Feche e tente novamente."
            if (Get-GameExe) { $playBtn.Enabled = $true }
            return
        }

        try {
            Set-Status "Instalando..."
            Install-Game
            Set-Ready ("Atualizado (versao " + $script:latestTag + ")")
        } catch {
            Write-Log "Instalacao falhou: $($_.Exception.Message)"
            Set-Status ("Erro ao instalar: " + $_.Exception.Message)
            if (Get-GameExe) { $playBtn.Enabled = $true }
        }
    })
    $script:timer.Start()
}

# ============================================================
#  Login com Google (Supabase, fluxo PKCE + navegador)
# ============================================================

function Get-Session {
    if (-not (Test-Path $SessionFile)) { return $null }
    try { return (Get-Content $SessionFile -Raw | ConvertFrom-Json) } catch { return $null }
}

function Update-AuthUI {
    $s = Get-Session
    if ($s -and $s.email) {
        $who = if ($s.name) { "$($s.name) ($($s.email))" } else { $s.email }
        $userLabel.Text      = "Logado: $who"
        $userLabel.ForeColor = [System.Drawing.Color]::FromArgb(120, 220, 160)
        $authBtn.Text        = "Sair da conta"
    } else {
        $userLabel.Text      = "Voce nao esta logado. Entre para salvar suas partidas!"
        $userLabel.ForeColor = [System.Drawing.Color]::FromArgb(180, 190, 220)
        $authBtn.Text        = "Entrar com Google"
    }
}

function Stop-AuthFlow {
    if ($script:authTimer)    { try { $script:authTimer.Stop() } catch {} ; $script:authTimer = $null }
    if ($script:authListener) { try { $script:authListener.Stop(); $script:authListener.Close() } catch {} ; $script:authListener = $null }
    $script:authCtxTask = $null
    $authBtn.Enabled = $true
}

function Start-GoogleLogin {
    # PKCE: verifier aleatorio + challenge = base64url(SHA256(verifier))
    $chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789"
    $script:pkceVerifier = -join (1..64 | ForEach-Object { $chars[(Get-Random -Maximum $chars.Length)] })
    $sha = [System.Security.Cryptography.SHA256]::Create()
    $hash = $sha.ComputeHash([System.Text.Encoding]::ASCII.GetBytes($script:pkceVerifier))
    $challenge = [Convert]::ToBase64String($hash).TrimEnd('=').Replace('+','-').Replace('/','_')

    # Servidor local que recebe o retorno do Google
    try {
        $script:authListener = New-Object System.Net.HttpListener
        $script:authListener.Prefixes.Add("http://localhost:$AuthPort/")
        $script:authListener.Start()
    } catch {
        Write-Log "Login: porta $AuthPort ocupada: $($_.Exception.Message)"
        $userLabel.Text = "Erro: porta de login ocupada. Feche e tente de novo."
        return
    }
    $script:authCtxTask  = $script:authListener.GetContextAsync()
    $script:authDeadline = (Get-Date).AddMinutes(3)

    # Abre o navegador na tela de login do Google (via Supabase)
    $redirect = [uri]::EscapeDataString("http://localhost:$AuthPort/callback")
    $url = "$SupabaseUrl/auth/v1/authorize?provider=google&redirect_to=$redirect" +
           "&code_challenge=$challenge&code_challenge_method=s256"
    Start-Process $url
    Write-Log "Login: navegador aberto, aguardando retorno na porta $AuthPort"

    $authBtn.Enabled = $false
    $userLabel.Text  = "Aguardando login no navegador..."

    $script:authTimer = New-Object System.Windows.Forms.Timer
    $script:authTimer.Interval = 250
    $script:authTimer.add_Tick({
        if ((Get-Date) -gt $script:authDeadline) {
            Write-Log "Login: tempo esgotado"
            Stop-AuthFlow
            Update-AuthUI
            return
        }
        if ($script:authCtxTask -eq $null -or -not $script:authCtxTask.IsCompleted) { return }

        $script:authTimer.Stop()
        try {
            $ctx  = $script:authCtxTask.Result
            $code = $ctx.Request.QueryString["code"]

            # Resposta simpatica no navegador
            $html = "<html><body style='font-family:Segoe UI;background:#15100a;color:#f3e8d3;text-align:center;padding-top:80px'>" +
                    "<h2>Login concluido!</h2><p>Pode fechar esta aba e voltar ao launcher.</p></body></html>"
            $buf = [System.Text.Encoding]::UTF8.GetBytes($html)
            $ctx.Response.ContentType = "text/html; charset=utf-8"
            $ctx.Response.OutputStream.Write($buf, 0, $buf.Length)
            $ctx.Response.Close()

            if (-not $code) { throw "retorno sem codigo (login cancelado?)" }

            # Troca o codigo pelos tokens (PKCE — sem segredo embutido)
            $body = (@{ auth_code = $code; code_verifier = $script:pkceVerifier } | ConvertTo-Json -Compress)
            $tok = Invoke-RestMethod -Uri "$SupabaseUrl/auth/v1/token?grant_type=pkce" -Method Post `
                     -ContentType "application/json" -Headers @{ apikey = $SupabaseKey } -Body $body

            $name = $null
            if ($tok.user.user_metadata) {
                if ($tok.user.user_metadata.full_name) { $name = $tok.user.user_metadata.full_name }
                elseif ($tok.user.user_metadata.name)  { $name = $tok.user.user_metadata.name }
            }
            $session = @{
                access_token  = $tok.access_token
                refresh_token = $tok.refresh_token
                user_id       = $tok.user.id
                email         = $tok.user.email
                name          = $name
                expires_at    = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds() + [long]$tok.expires_in
            }
            ($session | ConvertTo-Json -Compress) | Set-Content -Path $SessionFile -Encoding UTF8
            Write-Log "Login OK: $($tok.user.email)"

            # Registra/atualiza o perfil no banco (nao-fatal se falhar)
            try {
                $avatar = $null
                if ($tok.user.user_metadata -and $tok.user.user_metadata.avatar_url) { $avatar = $tok.user.user_metadata.avatar_url }
                $profile = (@{ id = $tok.user.id; email = $tok.user.email; full_name = $name;
                               avatar_url = $avatar; last_login = (Get-Date).ToUniversalTime().ToString("o") } | ConvertTo-Json -Compress)
                Invoke-RestMethod -Uri "$SupabaseUrl/rest/v1/profiles" -Method Post -ContentType "application/json" `
                    -Headers @{ apikey = $SupabaseKey; Authorization = "Bearer $($tok.access_token)"; Prefer = "resolution=merge-duplicates" } `
                    -Body ([System.Text.Encoding]::UTF8.GetBytes($profile)) | Out-Null
            } catch { Write-Log "Perfil: upsert falhou (nao-fatal): $($_.Exception.Message)" }
        } catch {
            Write-Log "Login falhou: $($_.Exception.Message)"
            $userLabel.Text = "Login falhou. Tente novamente."
        } finally {
            Stop-AuthFlow
            Update-AuthUI
        }
    })
    $script:authTimer.Start()
}

# Descobre a tag mais nova SEM a API: a pagina github.com/.../releases/latest
# devolve um 302 apontando para .../releases/tag/vXX. Lemos so o cabecalho
# Location (sem seguir o redirect) e pegamos o ultimo pedaco da URL = a tag.
# Isso nao conta no limite de 60 req/hora da api.github.com.
function Get-LatestTag {
    # Seguimos os redirects NA MAO (um de cada vez) porque so nos interessa o
    # que aponta para /releases/tag/. Se o repositorio tiver sido renomeado, o
    # primeiro salto vai para o MESMO caminho no nome novo (.../releases/latest)
    # e so o segundo chega na tag - lendo um salto so, o resultado seria a
    # string "latest", que viraria uma URL de download invalida e um "sem
    # conexao" sem explicacao. Tres saltos cobrem rename em cima de rename.
    $url = "https://github.com/$RepoOwner/$RepoName/releases/latest"
    for ($salto = 0; $salto -lt 3; $salto++) {
        $req = [System.Net.HttpWebRequest]::Create($url)
        $req.UserAgent        = "CardswornLauncher"
        $req.Method           = "GET"
        $req.AllowAutoRedirect = $false   # queremos LER o redirect, nao segui-lo
        $req.Timeout          = 15000
        $resp = $req.GetResponse()        # 302 nao lanca excecao (so 4xx/5xx lancam)
        try { $loc = $resp.Headers["Location"] } finally { $resp.Close() }
        if (-not $loc) { throw "releases/latest nao redirecionou (repo sem releases?)" }

        if ($loc -match "/releases/tag/") {
            Write-Log "Tag lida em $($salto + 1) salto(s): $loc"
            return ($loc -split "/")[-1]   # .../releases/tag/v38  ->  v38
        }

        Write-Log "Redirect intermediario (repo renomeado?): $loc"
        $url = $loc
    }
    throw "Nao cheguei na tag depois de 3 redirects (ultimo: $url)"
}

# Tamanho do arquivo (para o progresso) via HEAD, seguindo o redirect ate o CDN
function Get-RemoteSize([string]$url) {
    try {
        $req = [System.Net.HttpWebRequest]::Create($url)
        $req.UserAgent        = "CardswornLauncher"
        $req.Method           = "HEAD"
        $req.AllowAutoRedirect = $true
        $req.Timeout          = 15000
        $resp = $req.GetResponse()
        try { return [long]$resp.ContentLength } finally { $resp.Close() }
    } catch { return 0 }
}

# Consulta a release mais recente e decide se precisa baixar
function Check-Updates {
    try {
        $script:latestTag = Get-LatestTag
        # URL de download por convencao (o asset se chama sempre $AssetName).
        # Downloads de release NAO tem o limite da api.github.com.
        $script:assetUrl  = "https://github.com/$RepoOwner/$RepoName/releases/download/$($script:latestTag)/$AssetName"
        $script:assetSize = Get-RemoteSize $script:assetUrl

        $installed = ""
        if (Test-Path $VersionFile) { $installed = (Get-Content $VersionFile -Raw).Trim() }
        Write-Log "Instalado: '$installed' | Mais novo: '$($script:latestTag)'"

        $verLabel.Text = if ($installed) { "instalada $installed  |  nova $($script:latestTag)" }
                        else { "nova $($script:latestTag)" }

        if ($installed -ne $script:latestTag -or -not (Get-GameExe)) {
            Start-Download
        } else {
            Set-Ready ("Atualizado (versao " + $script:latestTag + ")")
        }
    } catch {
        Write-Log "Sem conexao / erro: $($_.Exception.Message)"
        if (Get-GameExe) {
            Set-Ready "Sem conexao - jogando versao instalada"
        } else {
            Set-Status "Sem conexao e nenhum jogo instalado ainda."
        }
    }
}

# ============================================================
#  Eventos
# ============================================================
$playBtn.add_Click({
    $exe = Get-GameExe
    if ($exe) {
        Write-Log "Abrindo o jogo: $exe"
        Start-Process -FilePath $exe -WorkingDirectory (Split-Path $exe)
        $form.Close()
    } else {
        Set-Status "Executavel do jogo nao encontrado."
    }
})

$authBtn.add_Click({
    $s = Get-Session
    if ($s -and $s.email) {
        # Sair da conta: apaga a sessao local (o jogo para de enviar partidas)
        Remove-Item $SessionFile -Force -ErrorAction SilentlyContinue
        Write-Log "Logout: sessao removida"
        Update-AuthUI
    } else {
        Start-GoogleLogin
    }
})

$form.add_Shown({
    $form.Activate()
    Update-AuthUI
    Carregar-Noticias
    Check-Updates
})

[System.Windows.Forms.Application]::EnableVisualStyles()
[void]$form.ShowDialog()
Stop-AuthFlow   # encerra o servidor local de login se ainda estiver aberto
$mutex.ReleaseMutex()
