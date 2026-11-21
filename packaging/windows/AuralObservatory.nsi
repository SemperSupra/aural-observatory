Unicode true
Name "Aural Observatory Search Desktop"
!ifndef PRODUCT_DIR
  !error "PRODUCT_DIR must point to a prebuilt, reviewed self-contained dotnet publish directory"
!endif
!ifndef OUTPUT_FILE
  !error "OUTPUT_FILE must point to a staging path"
!endif
OutFile "${OUTPUT_FILE}"
InstallDir "$LOCALAPPDATA\\Programs\\AuralObservatory"
RequestExecutionLevel user
ShowInstDetails show
ShowUninstDetails show

Section "Install"
  SetOutPath "$INSTDIR"
  File /r "${PRODUCT_DIR}\\*"
  CreateDirectory "$SMPROGRAMS"
  CreateShortCut "$SMPROGRAMS\\Aural Observatory Search.lnk" "$INSTDIR\\AuralObservatory.SearchDesktop.exe"
  WriteUninstaller "$INSTDIR\\Uninstall.exe"
  WriteRegStr HKCU "Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\AuralObservatory" "DisplayName" "Aural Observatory Search Desktop (research candidate)"
  WriteRegStr HKCU "Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\AuralObservatory" "UninstallString" '"$INSTDIR\\Uninstall.exe"'
SectionEnd

Section "Uninstall"
  Delete "$SMPROGRAMS\\Aural Observatory Search.lnk"
  DeleteRegKey HKCU "Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\AuralObservatory"
  RMDir /r "$INSTDIR"
SectionEnd
