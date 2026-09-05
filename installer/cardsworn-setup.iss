; ============================================================
;  Cardsworn - Instalador (Inno Setup)
;  Gera CardswornSetup.exe: instala o launcher, cria atalho na
;  Area de Trabalho e no Menu Iniciar (com icone do dado
;  dourado) e um desinstalador. NAO precisa de admin (instala
;  na pasta do usuario). O JOGO em si continua se atualizando
;  sozinho pelo launcher (isso aqui instala so o launcher).
;
;  Para compilar:  ISCC.exe cardsworn-setup.iss
; ============================================================

#define AppName    "Cardsworn"
#define AppVersion "1.0.2"
#define AppExe     "Play Cardsworn.vbs"
#define LauncherSrc "..\launcher\cardsworn-launcher"

[Setup]
; AppId identifica o programa para atualizar/desinstalar. NAO mudar entre
; versoes (senao o Windows trata como outro programa).
;
; Ele tem "CARD0GAME" no meio e fica assim mesmo: e justamente por manter o
; AppId que a instalacao nova SUBSTITUI a antiga em "Aplicativos e recursos".
; Trocar por um GUID novo faria o contrario do que parece - deixaria um
; "Card Game" orfao na lista, apontando para uma pasta que nao existe mais.
AppId={{8F2C6A41-3B7D-4E9A-9C15-CARD0GAME0001}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher=Cardsworn
AppPublisherURL=https://cardsworn.vercel.app
DefaultDirName={autopf}\{#AppName}
DisableProgramGroupPage=yes
DisableDirPage=auto
; Instala na conta do usuario -> sem pedir senha de administrador (sem UAC)
PrivilegesRequired=lowest
OutputDir=.
OutputBaseFilename=CardswornSetup
SetupIconFile={#LauncherSrc}\icon.ico
UninstallDisplayIcon={app}\icon.ico
UninstallDisplayName={#AppName}
WizardStyle=modern
Compression=lzma2/max
SolidCompression=yes
; O launcher/jogo funcionam em 64-bit; mantem a instalacao 64-bit quando possivel
ArchitecturesInstallIn64BitMode=x64compatible

[Languages]
Name: "brazilianportuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"

[Tasks]
Name: "desktopicon"; Description: "Criar um atalho na Area de Trabalho"; GroupDescription: "Atalhos:"

; Quem instalou quando o jogo se chamava "Card Game" tem o .vbs e os atalhos
; antigos na maquina. Sem isto sobrariam DOIS icones funcionando lado a lado
; ("Card Game" e "Cardsworn") apontando para o mesmo launcher.
[InstallDelete]
Type: files; Name: "{app}\Play Card Game.vbs"
Type: files; Name: "{autodesktop}\Card Game.lnk"
Type: files; Name: "{autoprograms}\Card Game.lnk"

[Files]
Source: "{#LauncherSrc}\Launcher.ps1";      DestDir: "{app}"; Flags: ignoreversion
Source: "{#LauncherSrc}\Play Cardsworn.vbs"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#LauncherSrc}\icon.ico";           DestDir: "{app}"; Flags: ignoreversion

[Icons]
; Atalho no Menu Iniciar
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\{#AppExe}"; IconFilename: "{app}\icon.ico"; Comment: "Abrir o Cardsworn"
; Atalho na Area de Trabalho (se a tarefa foi marcada)
Name: "{autodesktop}\{#AppName}";  Filename: "{app}\{#AppExe}"; IconFilename: "{app}\icon.ico"; Comment: "Abrir o Cardsworn"; Tasks: desktopicon

[Run]
; Oferece abrir o jogo ao terminar a instalacao
Filename: "{app}\{#AppExe}"; Description: "Abrir o Cardsworn agora"; Flags: shellexec postinstall skipifsilent nowait
