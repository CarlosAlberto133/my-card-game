; ============================================================
;  Card Game - Instalador (Inno Setup)
;  Gera CardGameSetup.exe: instala o launcher, cria atalho na
;  Area de Trabalho e no Menu Iniciar (com icone do dado
;  dourado) e um desinstalador. NAO precisa de admin (instala
;  na pasta do usuario). O JOGO em si continua se atualizando
;  sozinho pelo launcher (isso aqui instala so o launcher).
;
;  Para compilar:  ISCC.exe card-game-setup.iss
; ============================================================

#define AppName    "Card Game"
#define AppVersion "1.0.0"
#define AppExe     "Play Card Game.vbs"
#define LauncherSrc "..\launcher\card-game-launcher"

[Setup]
; AppId identifica o programa para atualizar/desinstalar. NAO mudar entre
; versoes (senao o Windows trata como outro programa).
AppId={{8F2C6A41-3B7D-4E9A-9C15-CARD0GAME0001}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher=Card Game
AppPublisherURL=https://card-game-online.vercel.app
DefaultDirName={autopf}\{#AppName}
DisableProgramGroupPage=yes
DisableDirPage=auto
; Instala na conta do usuario -> sem pedir senha de administrador (sem UAC)
PrivilegesRequired=lowest
OutputDir=.
OutputBaseFilename=CardGameSetup
SetupIconFile=icon.ico
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

[Files]
Source: "{#LauncherSrc}\Launcher.ps1";      DestDir: "{app}"; Flags: ignoreversion
Source: "{#LauncherSrc}\Play Card Game.vbs"; DestDir: "{app}"; Flags: ignoreversion
Source: "icon.ico";                          DestDir: "{app}"; Flags: ignoreversion

[Icons]
; Atalho no Menu Iniciar
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\{#AppExe}"; IconFilename: "{app}\icon.ico"; Comment: "Abrir o Card Game"
; Atalho na Area de Trabalho (se a tarefa foi marcada)
Name: "{autodesktop}\{#AppName}";  Filename: "{app}\{#AppExe}"; IconFilename: "{app}\icon.ico"; Comment: "Abrir o Card Game"; Tasks: desktopicon

[Run]
; Oferece abrir o jogo ao terminar a instalacao
Filename: "{app}\{#AppExe}"; Description: "Abrir o Card Game agora"; Flags: shellexec postinstall skipifsilent nowait
