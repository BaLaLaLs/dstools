!include "MUI2.nsh"

; Basic information
Name "DSTools"
OutFile "DSTools-Setup.exe"
InstallDir "$PROGRAMFILES\DSTools"
InstallDirRegKey HKCU "Software\DSTools" ""

; Interface settings
!define MUI_ABORTWARNING
!define MUI_ICON "${NSISDIR}\Contrib\Graphics\Icons\modern-install.ico"
!define MUI_UNICON "${NSISDIR}\Contrib\Graphics\Icons\modern-uninstall.ico"

; Pages
!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH

!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES

; Language
!insertmacro MUI_LANGUAGE "SimpChinese"

; Installation section
Section "Install" SecInstall
  SetOutPath "$INSTDIR"
  
  ; Add files
  File /r "..\dstools\bin\Release\net8.0\win-x64\publish\*.*"
  
  ; Create uninstaller
  WriteUninstaller "$INSTDIR\Uninstall.exe"
  
  ; Create start menu shortcuts
  CreateDirectory "$SMPROGRAMS\DSTools"
  CreateShortcut "$SMPROGRAMS\DSTools\DSTools.lnk" "$INSTDIR\dstools.exe"
  CreateShortcut "$SMPROGRAMS\DSTools\Uninstall.lnk" "$INSTDIR\Uninstall.exe"
  
  ; Write registry keys
  WriteRegStr HKCU "Software\DSTools" "" $INSTDIR
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\DSTools" "DisplayName" "DSTools"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\DSTools" "UninstallString" "$\"$INSTDIR\Uninstall.exe$\""
SectionEnd

; Uninstall section
Section "Uninstall"
  ; Delete files
  RMDir /r "$INSTDIR\*.*"
  RMDir "$INSTDIR"
  
  ; Delete shortcuts
  Delete "$SMPROGRAMS\DSTools\*.*"
  RMDir "$SMPROGRAMS\DSTools"
  
  ; Delete registry keys
  DeleteRegKey HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\DSTools"
  DeleteRegKey HKCU "Software\DSTools"
SectionEnd