@echo off
echo Create symbolic links to PDB files, to allow debugging the webrtc source files
REM NOT USED YET
REM  $(ProjectDir)create_pdb_links.bat "$(ProjectDir)dependencies\webrtc-build\pdb\windows_$(Configuration)_$(PlatformShortName)" $(OutDir)
CD /D %1
FOR /r %%f in (*.pdb) do (
    IF NOT EXIST %2%%~NXf (
        mklink %2%%~NXf %%f 
    )
)
