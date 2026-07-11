# ============================================================
#  Card Game - Launcher
#  Baixa automaticamente a versao mais nova publicada no
#  GitHub Releases e abre o jogo. Nao precisa instalar nada:
#  usa a interface grafica que ja vem no Windows.
# ============================================================

# ---------- CONFIGURACAO (edite se mudar o repositorio) ----------
$RepoOwner = "CarlosAlberto133"        # seu usuario do GitHub
$RepoName  = "card-game-releases"      # repositorio PUBLICO so para as builds
$AssetName = "card-game.zip"           # nome do .zip que voce sobe em cada release
# -----------------------------------------------------------------

# GitHub exige TLS 1.2 (o PowerShell antigo usa 1.0 por padrao e falharia)
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

# ---------- Caminhos ----------
$Root        = Split-Path -Parent $MyInvocation.MyCommand.Definition
$GameDir     = Join-Path $Root "game"
$VersionFile = Join-Path $Root "installed.txt"
$ZipTemp     = Join-Path $env:TEMP "card-game-update.zip"

# ---------- Estado compartilhado com a thread de download ----------
$script:progress      = 0
$script:downloadDone  = $false
$script:downloadError = $null
$script:latestTag     = $null
$script:assetUrl      = $null

# ============================================================
#  Janela
# ============================================================
$form = New-Object System.Windows.Forms.Form
$form.Text            = "Card Game"
$form.Size            = New-Object System.Drawing.Size(440, 220)
$form.StartPosition   = "CenterScreen"
$form.FormBorderStyle = "FixedSingle"
$form.MaximizeBox     = $false
$form.BackColor       = [System.Drawing.Color]::FromArgb(18, 20, 34)   # azul-escuro espacial

$title = New-Object System.Windows.Forms.Label
$title.Text      = "CARD GAME"
$title.Font      = New-Object System.Drawing.Font("Segoe UI", 20, [System.Drawing.FontStyle]::Bold)
$title.ForeColor = [System.Drawing.Color]::White
$title.AutoSize  = $false
$title.TextAlign = "MiddleCenter"
$title.Size      = New-Object System.Drawing.Size(410, 45)
$title.Location  = New-Object System.Drawing.Point(8, 15)
$form.Controls.Add($title)

$status = New-Object System.Windows.Forms.Label
$status.Text      = "Verificando atualizacoes..."
$status.Font      = New-Object System.Drawing.Font("Segoe UI", 9)
$status.ForeColor = [System.Drawing.Color]::FromArgb(180, 190, 220)
$status.AutoSize  = $false
$status.TextAlign = "MiddleCenter"
$status.Size      = New-Object System.Drawing.Size(410, 22)
$status.Location  = New-Object System.Drawing.Point(8, 68)
$form.Controls.Add($status)

$bar = New-Object System.Windows.Forms.ProgressBar
$bar.Size     = New-Object System.Drawing.Size(390, 18)
$bar.Location = New-Object System.Drawing.Point(20, 96)
$bar.Minimum  = 0
$bar.Maximum  = 100
$form.Controls.Add($bar)

$playBtn = New-Object System.Windows.Forms.Button
$playBtn.Text      = "Jogar"
$playBtn.Font      = New-Object System.Drawing.Font("Segoe UI", 12, [System.Drawing.FontStyle]::Bold)
$playBtn.Size      = New-Object System.Drawing.Size(390, 42)
$playBtn.Location  = New-Object System.Drawing.Point(20, 126)
$playBtn.FlatStyle = "Flat"
$playBtn.BackColor = [System.Drawing.Color]::FromArgb(70, 110, 220)
$playBtn.ForeColor = [System.Drawing.Color]::White
$playBtn.Enabled   = $false
$form.Controls.Add($playBtn)

# ============================================================
#  Funcoes
# ============================================================
function Set-Status([string]$text) { $status.Text = $text }

function Set-Ready([string]$text) {
    $status.Text     = $text
    $bar.Value       = 100
    $playBtn.Enabled = $true
}

# Localiza o .exe do jogo dentro da pasta 'game' (ignora o UnityCrashHandler)
function Get-GameExe {
    if (-not (Test-Path $GameDir)) { return $null }
    $exe = Get-ChildItem -Path $GameDir -Recurse -Filter *.exe -ErrorAction SilentlyContinue |
           Where-Object { $_.Name -notlike "UnityCrashHandler*" } |
           Sort-Object { if ($_.Name -ieq "card-game.exe") { 0 } else { 1 } } |
           Select-Object -First 1
    if ($exe) { return $exe.FullName } else { return $null }
}

# Extrai o zip baixado por cima da pasta 'game'
function Install-Game {
    if (Test-Path $GameDir) { Remove-Item $GameDir -Recurse -Force }
    New-Item -ItemType Directory -Path $GameDir | Out-Null

    # O antivirus do Windows costuma travar o zip recem-baixado por
    # 1-2 segundos. Tentamos extrair algumas vezes antes de desistir.
    $attempt = 0
    while ($true) {
        $attempt++
        try {
            Expand-Archive -Path $ZipTemp -DestinationPath $GameDir -Force -ErrorAction Stop
            break
        } catch {
            if ($attempt -ge 5) { throw }
            Start-Sleep -Milliseconds 1200
        }
    }

    Set-Content -Path $VersionFile -Value $script:latestTag -Encoding UTF8
    Remove-Item $ZipTemp -Force -ErrorAction SilentlyContinue
}

# Baixa o zip da release em segundo plano, atualizando a barra
function Start-Download {
    Set-Status "Baixando atualizacao..."
    $script:progress     = 0
    $script:downloadDone = $false
    $script:downloadError = $null

    $wc = New-Object System.Net.WebClient
    $wc.Headers.Add("User-Agent", "CardGameLauncher")

    $wc.add_DownloadProgressChanged({ param($s, $e)
        $script:progress = $e.ProgressPercentage
    })
    $wc.add_DownloadFileCompleted({ param($s, $e)
        if ($e.Error) { $script:downloadError = $e.Error.Message }
        $script:downloadDone = $true
    })

    $wc.DownloadFileAsync([Uri]$script:assetUrl, $ZipTemp)

    # Timer na thread da interface: le o progresso e finaliza quando termina
    $timer = New-Object System.Windows.Forms.Timer
    $timer.Interval = 200
    $timer.add_Tick({
        $p = [Math]::Max(0, [Math]::Min(100, $script:progress))
        $bar.Value = $p
        if ($script:downloadDone) {
            $timer.Stop()
            if ($script:downloadError) {
                Set-Status "Erro no download. Tente novamente."
                # Se ja existe uma versao instalada, deixa jogar mesmo assim
                if (Get-GameExe) { $playBtn.Enabled = $true }
                return
            }
            try {
                Set-Status "Instalando..."
                Install-Game
                Set-Ready ("Atualizado (versao " + $script:latestTag + ")")
            } catch {
                Set-Status ("Erro ao instalar: " + $_.Exception.Message)
                if (Get-GameExe) { $playBtn.Enabled = $true }
            }
        }
    })
    $timer.Start()
}

# Consulta a release mais recente e decide se precisa baixar
function Check-Updates {
    try {
        $api = "https://api.github.com/repos/$RepoOwner/$RepoName/releases/latest"
        $rel = Invoke-RestMethod -Uri $api -Headers @{ "User-Agent" = "CardGameLauncher" }

        $script:latestTag = $rel.tag_name
        $asset = $rel.assets | Where-Object { $_.name -eq $AssetName } | Select-Object -First 1
        if (-not $asset) {
            Set-Status "Release sem o arquivo $AssetName."
            if (Get-GameExe) { $playBtn.Enabled = $true }
            return
        }
        $script:assetUrl = $asset.browser_download_url

        $installed = ""
        if (Test-Path $VersionFile) { $installed = (Get-Content $VersionFile -Raw).Trim() }

        if ($installed -ne $script:latestTag -or -not (Get-GameExe)) {
            Start-Download
        } else {
            Set-Ready ("Atualizado (versao " + $script:latestTag + ")")
        }
    } catch {
        # Sem internet ou GitHub indisponivel: joga a versao ja instalada, se houver
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
        Start-Process -FilePath $exe -WorkingDirectory (Split-Path $exe)
        $form.Close()
    } else {
        Set-Status "Executavel do jogo nao encontrado."
    }
})

$form.add_Shown({ $form.Activate(); Check-Updates })

[System.Windows.Forms.Application]::EnableVisualStyles()
[void]$form.ShowDialog()
