# ============================================================
#  Card Game - Launcher  (v2)
#  Baixa automaticamente a versao mais nova publicada no
#  GitHub Releases e abre o jogo.
#
#  v2: o jogo agora e instalado em %LOCALAPPDATA%\CardGame
#  (fora do OneDrive/Desktop sincronizado, que travava a
#  extracao), download mais robusto e log em launcher.log.
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
# O jogo mora SEMPRE em LocalAppData: pasta local, rapida e fora de
# qualquer sincronizacao (OneDrive etc.). O launcher pode ficar onde quiser.
$InstallRoot = Join-Path $env:LOCALAPPDATA "CardGame"
$GameDir     = Join-Path $InstallRoot "game"
$VersionFile = Join-Path $InstallRoot "installed.txt"
$ZipTemp     = Join-Path $InstallRoot "update.zip"
$LogFile     = Join-Path $InstallRoot "launcher.log"

if (-not (Test-Path $InstallRoot)) { New-Item -ItemType Directory -Path $InstallRoot | Out-Null }

function Write-Log([string]$msg) {
    try { Add-Content -Path $LogFile -Value ("[{0}] {1}" -f (Get-Date -Format "yyyy-MM-dd HH:mm:ss"), $msg) } catch {}
}
Write-Log "----- Launcher iniciado -----"

# ---------- Evita duas instancias abertas ao mesmo tempo ----------
$mutex = New-Object System.Threading.Mutex($false, "CardGameLauncherMutex")
if (-not $mutex.WaitOne(0, $false)) {
    [System.Windows.Forms.MessageBox]::Show("O launcher do Card Game ja esta aberto.", "Card Game") | Out-Null
    exit
}

# ---------- Limpa instalacao antiga ao lado do launcher (versoes v1) ----------
$OldRoot = Split-Path -Parent $MyInvocation.MyCommand.Definition
foreach ($legacy in @((Join-Path $OldRoot "game"), (Join-Path $OldRoot "installed.txt"))) {
    if (Test-Path $legacy) {
        try { Remove-Item $legacy -Recurse -Force -ErrorAction Stop; Write-Log "Removido legado: $legacy" } catch {}
    }
}

# ---------- Estado ----------
$script:latestTag   = $null
$script:assetUrl    = $null
$script:assetSize   = 0
$script:dlTask      = $null
$script:webClient   = $null
$script:timer       = $null   # PRECISA ser script: — variavel local da funcao
$script:installDone = $false  # nao existe mais quando o evento Tick dispara!

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
function Set-Status([string]$text) {
    $status.Text = $text
    $status.Refresh()   # atualiza mesmo se a interface for travar logo em seguida
}

function Set-Ready([string]$text) {
    Set-Status $text
    $bar.Value       = 100
    $playBtn.Enabled = $true
}

# Localiza o .exe do jogo (ignora o UnityCrashHandler)
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
    $script:webClient.Headers.Add("User-Agent", "CardGameLauncher")
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
                $bar.Value = $pct
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

# Consulta a release mais recente e decide se precisa baixar
function Check-Updates {
    try {
        $api = "https://api.github.com/repos/$RepoOwner/$RepoName/releases/latest"
        $rel = Invoke-RestMethod -Uri $api -Headers @{ "User-Agent" = "CardGameLauncher" }

        $script:latestTag = $rel.tag_name
        $asset = $rel.assets | Where-Object { $_.name -eq $AssetName } | Select-Object -First 1
        if (-not $asset) {
            Write-Log "Release $($rel.tag_name) sem o asset $AssetName"
            Set-Status "Release sem o arquivo $AssetName."
            if (Get-GameExe) { $playBtn.Enabled = $true }
            return
        }
        $script:assetUrl  = $asset.browser_download_url
        $script:assetSize = [long]$asset.size

        $installed = ""
        if (Test-Path $VersionFile) { $installed = (Get-Content $VersionFile -Raw).Trim() }
        Write-Log "Instalado: '$installed' | Mais novo: '$($script:latestTag)'"

        if ($installed -ne $script:latestTag -or -not (Get-GameExe)) {
            Start-Download
        } else {
            Set-Ready ("Atualizado (versao " + $script:latestTag + ")")
        }
    } catch {
        Write-Log "Sem conexao / erro na API: $($_.Exception.Message)"
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

$form.add_Shown({ $form.Activate(); Check-Updates })

[System.Windows.Forms.Application]::EnableVisualStyles()
[void]$form.ShowDialog()
$mutex.ReleaseMutex()
