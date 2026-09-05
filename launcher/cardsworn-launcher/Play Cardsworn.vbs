' Abre o launcher do Cardsworn sem mostrar a janela preta do PowerShell.
' O NOME DESTE ARQUIVO nao muda: os atalhos que ja estao na Area de
' Trabalho dos jogadores apontam para ele.
' Basta dar dois cliques neste arquivo.
Dim fso, folder, ps1, shell
Set fso = CreateObject("Scripting.FileSystemObject")
folder = fso.GetParentFolderName(WScript.ScriptFullName)
ps1 = folder & "\Launcher.ps1"
Set shell = CreateObject("WScript.Shell")
shell.Run "powershell -NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File """ & ps1 & """", 0, False
