@echo off
setlocal EnableDelayedExpansion
title Hydralum - BepInEx 6 IL2CPP Auto-Setup Assistant

:: ============================================================
:: Configuration & URLs
:: ============================================================
set "DEFAULT_AU_DIR=C:\Program Files (x86)\Steam\steamapps\common\Among Us"
set "BEPINEX_ZIP_URL=https://builds.bepinex.dev/projects/bepinex_be/785/BepInEx-Unity.IL2CPP-win-x64-6.0.0-be.785%%2B6abdba4.zip"

echo ======================================================================
echo             BEPINEX 6 (IL2CPP) AUTOMATED SETUP ASSISTANT
echo ======================================================================
echo.
echo  Among Us is an IL2CPP game and requires BepInEx 6 (IL2CPP x64).
echo  This tool will automatically download, extract, and configure BepInEx
echo  for your Among Us installation.
echo ======================================================================
echo.

:: 1. Auto-Detect Platform (Steam / Epic Games / Custom)
set "STEAM_PATH="
set "EPIC_PATH="

:: Check Steam Registry & Common Directories
for /f "tokens=2* delims=	 " %%A in ('reg query "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 945360" /v InstallLocation 2^>nul') do (
    if exist "%%B\Among Us.exe" set "STEAM_PATH=%%B"
)
if not defined STEAM_PATH (
    if exist "C:\Program Files (x86)\Steam\steamapps\common\Among Us\Among Us.exe" set "STEAM_PATH=C:\Program Files (x86)\Steam\steamapps\common\Among Us"
)
if not defined STEAM_PATH (
    if exist "C:\Program Files\Steam\steamapps\common\Among Us\Among Us.exe" set "STEAM_PATH=C:\Program Files\Steam\steamapps\common\Among Us"
)
if not defined STEAM_PATH (
    if exist "D:\SteamLibrary\steamapps\common\Among Us\Among Us.exe" set "STEAM_PATH=D:\SteamLibrary\steamapps\common\Among Us"
)
if not defined STEAM_PATH (
    if exist "E:\SteamLibrary\steamapps\common\Among Us\Among Us.exe" set "STEAM_PATH=E:\SteamLibrary\steamapps\common\Among Us"
)

:: Check Epic Games Common Directories
if exist "C:\Program Files\Epic Games\AmongUs\Among Us.exe" set "EPIC_PATH=C:\Program Files\Epic Games\AmongUs"
if not defined EPIC_PATH (
    if exist "C:\Program Files (x86)\Epic Games\AmongUs\Among Us.exe" set "EPIC_PATH=C:\Program Files (x86)\Epic Games\AmongUs"
)
if not defined EPIC_PATH (
    if exist "D:\Epic Games\AmongUs\Among Us.exe" set "EPIC_PATH=D:\Epic Games\AmongUs"
)
if not defined EPIC_PATH (
    if exist "E:\Epic Games\AmongUs\Among Us.exe" set "EPIC_PATH=E:\Epic Games\AmongUs"
)

:: Branch based on discovered paths
if defined STEAM_PATH if defined EPIC_PATH goto select_multi_platform
if defined STEAM_PATH goto select_steam_platform
if defined EPIC_PATH goto select_epic_platform
goto select_custom_platform

:select_multi_platform
echo [?] Multiple Among Us installations detected:
echo     [1] Steam:       !STEAM_PATH!
echo     [2] Epic Games:  !EPIC_PATH!
echo     [3] Custom Path
echo.
set /p "PLAT_CHOICE=Select platform [1-3] (Default is 1): "
if "!PLAT_CHOICE!"=="2" (
    set "AU_DIR=!EPIC_PATH!"
    set "PLATFORM=Epic Games"
    goto platform_done
)
if "!PLAT_CHOICE!"=="3" (
    set /p "AU_DIR=Enter custom Among Us folder path: "
    set "PLATFORM=Custom"
    goto platform_done
)
set "AU_DIR=!STEAM_PATH!"
set "PLATFORM=Steam"
goto platform_done

:select_steam_platform
set "AU_DIR=!STEAM_PATH!"
set "PLATFORM=Steam"
echo [OK] Detected Platform: Steam
echo      Location: "!AU_DIR!"
goto platform_done

:select_epic_platform
set "AU_DIR=!EPIC_PATH!"
set "PLATFORM=Epic Games"
echo [OK] Detected Platform: Epic Games
echo      Location: "!AU_DIR!"
goto platform_done

:select_custom_platform
echo [!] Could not auto-detect Among Us installation.
echo     [1] Steam default:      C:\Program Files (x86)\Steam\steamapps\common\Among Us
echo     [2] Epic Games default: C:\Program Files\Epic Games\AmongUs
echo     [3] Enter custom folder path
echo.
set /p "PLAT_CHOICE=Select an option [1-3] (Default is 1): "
if "!PLAT_CHOICE!"=="2" (
    set "AU_DIR=C:\Program Files\Epic Games\AmongUs"
    set "PLATFORM=Epic Games"
    goto platform_done
)
if "!PLAT_CHOICE!"=="3" (
    set /p "AU_DIR=Enter custom Among Us folder path: "
    set "PLATFORM=Custom"
    goto platform_done
)
set "AU_DIR=C:\Program Files (x86)\Steam\steamapps\common\Among Us"
set "PLATFORM=Steam"
goto platform_done

:platform_done
if not exist "!AU_DIR!\Among Us.exe" (
    echo.
    echo [WARNING] "Among Us.exe" was not found in:
    echo           "!AU_DIR!"
    echo           Please make sure this is your real game directory.
    echo.
)

echo [*] Target Platform:    !PLATFORM!
echo [*] Target Game Folder: "!AU_DIR!"
echo.

:: 2. Check for conflicting BepInEx 5 installations
if exist "%AU_DIR%\BepInEx\core\BepInEx.dll" (
    if not exist "%AU_DIR%\BepInEx\core\BepInEx.Unity.IL2CPP.dll" (
        echo [!] WARNING: Found an old Mono version of BepInEx 5!
        echo     BepInEx 5 is incompatible with Among Us and will cause crashes.
        echo.
        set /p "DEL_OLD=Would you like to remove the old BepInEx 5 files? (Y/n): "
        if /i "!DEL_OLD!" NEQ "n" (
            echo [*] Cleaning old BepInEx 5 files...
            rmdir /s /q "%AU_DIR%\BepInEx" 2>nul
            del /f /q "%AU_DIR%\winhttp.dll" 2>nul
            del /f /q "%AU_DIR%\doorstop_config.ini" 2>nul
            echo [OK] Old version removed.
        )
    )
)

:: 3. Download BepInEx 6 IL2CPP x64 Build 785
set "BEP_ZIP=%AU_DIR%\BepInEx_IL2CPP_setup.zip"

echo [*] Downloading BepInEx 6 IL2CPP (Build 785)...
echo     From: %BEPINEX_ZIP_URL%

curl.exe -L -f -s -S -o "%BEP_ZIP%" "%BEPINEX_ZIP_URL%"
if %ERRORLEVEL% NEQ 0 (
    echo [*] curl failed, using PowerShell download fallback...
    powershell -NoProfile -Command "[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; Invoke-WebRequest -Uri '%BEPINEX_ZIP_URL%' -OutFile '%BEP_ZIP%'"
)

if not exist "%BEP_ZIP%" (
    echo.
    echo [ERROR] Failed to download BepInEx zip file!
    echo         Please check your internet connection or download it manually from:
    echo         %BEPINEX_ZIP_URL%
    echo.
    pause
    exit /b 1
)
echo [OK] BepInEx 6 archive downloaded successfully.
echo.

:: 4. Extract BepInEx into Game Folder
echo [*] Extracting BepInEx directly into game folder...
powershell -NoProfile -Command "Expand-Archive -Path '%BEP_ZIP%' -DestinationPath '%AU_DIR%' -Force"
if %ERRORLEVEL% NEQ 0 (
    echo [ERROR] Failed to extract BepInEx archive.
    del /f /q "%BEP_ZIP%" 2>nul
    pause
    exit /b 1
)

:: Delete downloaded zip archive
if exist "%BEP_ZIP%" del /f /q "%BEP_ZIP%" 2>nul

echo [OK] Files extracted:
echo      - winhttp.dll
echo      - doorstop_config.ini
echo      - BepInEx/ folder
echo.

:: Ensure plugins directory exists
if not exist "%AU_DIR%\BepInEx\plugins" (
    mkdir "%AU_DIR%\BepInEx\plugins" 2>nul
)

:: 5. First-Time Initialization
echo ======================================================================
echo                     STEP 2: FIRST-TIME INITIALIZATION
echo ======================================================================
echo.
echo  BepInEx 6 (IL2CPP) has been extracted to your game folder.
echo  When Among Us launches for the first time, BepInEx will automatically
echo  generate the required IL2CPP game interop files at the Main Menu.
echo.
set /p "LAUNCH_NOW=Would you like to launch Among Us now to initialize BepInEx? (Y/n): "

if /i "!LAUNCH_NOW!" NEQ "n" (
    echo.
    echo [*] Launching Among Us for !PLATFORM!...
    if "!PLATFORM!"=="Steam" (
        start "" "steam://rungameid/945360" 2>nul || start "" "%AU_DIR%\Among Us.exe"
    ) else if "!PLATFORM!"=="Epic Games" (
        start "" "com.epicgames.launcher://apps/AmongUs?action=launch&silent=true" 2>nul || start "" "%AU_DIR%\Among Us.exe"
    ) else (
        start "" "%AU_DIR%\Among Us.exe"
    )
    echo [OK] Among Us launched!
    echo.
    echo [*] Waiting 20 seconds for BepInEx to initialize game files...
    timeout /t 20 /nobreak >nul 2>&1 || ping 127.0.0.1 -n 21 >nul 2>&1
    echo [*] Closing Among Us automatically...
    taskkill /f /im "Among Us.exe" >nul 2>&1
    echo [OK] Among Us closed. BepInEx file structure initialized!
)

echo.
echo ======================================================================
echo                         SETUP COMPLETE!
echo ======================================================================
echo.
echo  BepInEx 6 (IL2CPP) is now installed in your game directory.
echo.
echo  NEXT STEP:
echo  Run "Install_Hydralum.bat" to automatically download, build, and
echo  install Hydralum (HydraMenu + MalumMenuPlus) into your plugins folder!
echo ======================================================================
echo.
pause
exit /b 0
