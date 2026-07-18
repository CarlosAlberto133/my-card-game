# Rebuilda o instalador do zero: (1) gera o icone, (2) compila o setup.exe.
# Rode sempre que mudar o launcher ou o icone.
#   powershell -ExecutionPolicy Bypass -File build-installer.ps1

$ErrorActionPreference = "Stop"
$here = $PSScriptRoot

Write-Host "[1/2] Gerando icone..." -ForegroundColor Cyan
& (Join-Path $here "make-icon.ps1")

Write-Host "[2/2] Compilando o instalador..." -ForegroundColor Cyan
$iscc = @(
    "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe",
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
    "$env:ProgramFiles\Inno Setup 6\ISCC.exe"
) | Where-Object { Test-Path $_ } | Select-Object -First 1

if (-not $iscc) {
    Write-Host "Inno Setup nao encontrado. Instale com: winget install JRSoftware.InnoSetup" -ForegroundColor Red
    exit 1
}

& $iscc (Join-Path $here "card-game-setup.iss")
if ($LASTEXITCODE -eq 0) {
    Write-Host "`nPronto: $(Join-Path $here 'CardGameSetup.exe')" -ForegroundColor Green
} else {
    Write-Host "Falha na compilacao (codigo $LASTEXITCODE)." -ForegroundColor Red
    exit $LASTEXITCODE
}
