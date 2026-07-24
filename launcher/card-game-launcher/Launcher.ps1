# ============================================================
#  Card Game - Launcher  (v3)
#  Baixa automaticamente a versao mais nova publicada no
#  GitHub Releases e abre o jogo.
#
#  v2: o jogo agora e instalado em %LOCALAPPDATA%\CardGame
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

# ---------- CONFIGURACAO (edite se mudar o repositorio) ----------
$RepoOwner = "CarlosAlberto133"        # seu usuario do GitHub
$RepoName  = "card-game-releases"      # repositorio PUBLICO so para as builds
$AssetName = "card-game.zip"           # nome do .zip que voce sobe em cada release

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
$InstallRoot = Join-Path $env:LOCALAPPDATA "CardGame"
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

# Login com Google
$script:authListener = $null
$script:authCtxTask  = $null
$script:authTimer    = $null
$script:pkceVerifier = $null
$script:authDeadline = $null

# ============================================================
#  Janela
# ============================================================
$form = New-Object System.Windows.Forms.Form
$form.Text            = "Card Game"
$form.Size            = New-Object System.Drawing.Size(440, 316)
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

# ---------- Área de login (Google) ----------
$userLabel = New-Object System.Windows.Forms.Label
$userLabel.Text      = "Voce nao esta logado. Entre para salvar suas partidas!"
$userLabel.Font      = New-Object System.Drawing.Font("Segoe UI", 9)
$userLabel.ForeColor = [System.Drawing.Color]::FromArgb(180, 190, 220)
$userLabel.AutoSize  = $false
$userLabel.TextAlign = "MiddleCenter"
$userLabel.Size      = New-Object System.Drawing.Size(410, 22)
$userLabel.Location  = New-Object System.Drawing.Point(8, 182)
$form.Controls.Add($userLabel)

$authBtn = New-Object System.Windows.Forms.Button
$authBtn.Text      = "Entrar com Google"
$authBtn.Font      = New-Object System.Drawing.Font("Segoe UI", 10, [System.Drawing.FontStyle]::Bold)
$authBtn.Size      = New-Object System.Drawing.Size(390, 36)
$authBtn.Location  = New-Object System.Drawing.Point(20, 210)
$authBtn.FlatStyle = "Flat"
$authBtn.BackColor = [System.Drawing.Color]::White
$authBtn.ForeColor = [System.Drawing.Color]::FromArgb(30, 30, 30)
$form.Controls.Add($authBtn)

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
    $url = "https://github.com/$RepoOwner/$RepoName/releases/latest"
    $req = [System.Net.HttpWebRequest]::Create($url)
    $req.UserAgent        = "CardGameLauncher"
    $req.Method           = "GET"
    $req.AllowAutoRedirect = $false   # queremos LER o redirect, nao segui-lo
    $req.Timeout          = 15000
    $resp = $req.GetResponse()        # 302 nao lanca excecao (so 4xx/5xx lancam)
    try { $loc = $resp.Headers["Location"] } finally { $resp.Close() }
    if (-not $loc) { throw "releases/latest nao redirecionou (repo sem releases?)" }
    return ($loc -split "/")[-1]       # .../releases/tag/v30  ->  v30
}

# Tamanho do arquivo (para o progresso) via HEAD, seguindo o redirect ate o CDN
function Get-RemoteSize([string]$url) {
    try {
        $req = [System.Net.HttpWebRequest]::Create($url)
        $req.UserAgent        = "CardGameLauncher"
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

$form.add_Shown({ $form.Activate(); Update-AuthUI; Check-Updates })

[System.Windows.Forms.Application]::EnableVisualStyles()
[void]$form.ShowDialog()
Stop-AuthFlow   # encerra o servidor local de login se ainda estiver aberto
$mutex.ReleaseMutex()
