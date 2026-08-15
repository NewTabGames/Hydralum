@echo off
setlocal EnableDelayedExpansion
title Hydralum - Auto Downloader, Builder & Installer

:: ============================================================
:: Configuration & Directories
:: ============================================================
set "DEFAULT_AU_DIR=C:\Program Files (x86)\Steam\steamapps\common\Among Us"
set "REPO_ZIP_URL=https://github.com/NewTabGames/Hydralum/archive/refs/heads/main.zip"

echo ======================================================================
echo                 HYDRALUM AUTO-BUILDER ^& INSTALLER
echo ======================================================================
echo.

:: 1. Auto-Detect Platform (Steam / Epic Games / Custom)
set "STEAM_PATH="
set "EPIC_PATH="

:: Check Steam Registry & Common Directories
for /f "tokens=2* delims=	 " %%A in ('reg query "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 945360" /v InstallLocation 2^>nul') do (
    if exist "%%B\Among Us.exe" set "STEAM_PATH=%%B"
)
if "!STEAM_PATH!"=="" (
    if exist "C:\Program Files (x86)\Steam\steamapps\common\Among Us\Among Us.exe" (
        set "STEAM_PATH=C:\Program Files (x86)\Steam\steamapps\common\Among Us"
    ) else if exist "C:\Program Files\Steam\steamapps\common\Among Us\Among Us.exe" (
        set "STEAM_PATH=C:\Program Files\Steam\steamapps\common\Among Us"
    ) else if exist "D:\SteamLibrary\steamapps\common\Among Us\Among Us.exe" (
        set "STEAM_PATH=D:\SteamLibrary\steamapps\common\Among Us"
    ) else if exist "E:\SteamLibrary\steamapps\common\Among Us\Among Us.exe" (
        set "STEAM_PATH=E:\SteamLibrary\steamapps\common\Among Us"
    )
)

:: Check Epic Games Common Directories
if exist "C:\Program Files\Epic Games\AmongUs\Among Us.exe" (
    set "EPIC_PATH=C:\Program Files\Epic Games\AmongUs"
) else if exist "C:\Program Files (x86)\Epic Games\AmongUs\Among Us.exe" (
    set "EPIC_PATH=C:\Program Files (x86)\Epic Games\AmongUs"
) else if exist "D:\Epic Games\AmongUs\Among Us.exe" (
    set "EPIC_PATH=D:\Epic Games\AmongUs"
) else if exist "E:\Epic Games\AmongUs\Among Us.exe" (
    set "EPIC_PATH=E:\Epic Games\AmongUs"
)

:: Search Epic Games Launcher Manifests if not found in default paths
if "!EPIC_PATH!"=="" (
    if exist "%ProgramData%\Epic\EpicGamesLauncher\Data\Manifests" (
        for %%F in ("%ProgramData%\Epic\EpicGamesLauncher\Data\Manifests\*.item") do (
            for /f "tokens=2 delims=:, " %%I in ('findstr /i "InstallLocation" "%%F" 2^>nul') do (
                set "TEMP_EPIC=%%~I"
                set "TEMP_EPIC=!TEMP_EPIC:\\=\!"
                if exist "!TEMP_EPIC!\Among Us.exe" set "EPIC_PATH=!TEMP_EPIC!"
            )
        )
    )
)

set "AU_DIR="
set "PLATFORM="

if defined STEAM_PATH if defined EPIC_PATH (
    echo [?] Multiple Among Us installations detected:
    echo     [1] Steam:       !STEAM_PATH!
    echo     [2] Epic Games:  !EPIC_PATH!
    echo     [3] Custom Path
    echo.
    set /p "PLAT_CHOICE=Select platform [1-3] (Default is 1): "
    if "!PLAT_CHOICE!"=="2" (
        set "AU_DIR=!EPIC_PATH!"
        set "PLATFORM=Epic Games"
    ) else if "!PLAT_CHOICE!"=="3" (
        set /p "AU_DIR=Enter custom Among Us folder path: "
        set "PLATFORM=Custom"
    ) else (
        set "AU_DIR=!STEAM_PATH!"
        set "PLATFORM=Steam"
    )
) else if defined STEAM_PATH (
    set "AU_DIR=!STEAM_PATH!"
    set "PLATFORM=Steam"
    echo [OK] Detected Platform: Steam
    echo      Location: !AU_DIR!
) else if defined EPIC_PATH (
    set "AU_DIR=!EPIC_PATH!"
    set "PLATFORM=Epic Games"
    echo [OK] Detected Platform: Epic Games
    echo      Location: !AU_DIR!
) else (
    echo [!] Could not auto-detect Among Us installation.
    echo     [1] Steam default:      C:\Program Files (x86)\Steam\steamapps\common\Among Us
    echo     [2] Epic Games default: C:\Program Files\Epic Games\AmongUs
    echo     [3] Enter custom folder path
    echo.
    set /p "PLAT_CHOICE=Select an option [1-3] (Default is 1): "
    if "!PLAT_CHOICE!"=="2" (
        set "AU_DIR=C:\Program Files\Epic Games\AmongUs"
        set "PLATFORM=Epic Games"
    ) else if "!PLAT_CHOICE!"=="3" (
        set /p "AU_DIR=Enter custom Among Us folder path: "
        set "PLATFORM=Custom"
    ) else (
        set "AU_DIR=C:\Program Files (x86)\Steam\steamapps\common\Among Us"
        set "PLATFORM=Steam"
    )
)

set "PLUGINS_DIR=%AU_DIR%\BepInEx\plugins"
set "CONFIG_DIR=%AU_DIR%\BepInEx\config"
set "PREFS_FILE=%CONFIG_DIR%\.hydralum_installer_prefs"

echo [*] Target Platform:    !PLATFORM!
echo [*] Target Game Folder: "!AU_DIR!"
echo [*] Target Plugins:     "!PLUGINS_DIR!"
echo.

:: Ensure plugins and config directories exist
if not exist "%PLUGINS_DIR%" (
    echo [*] Creating BepInEx plugins folder...
    mkdir "%PLUGINS_DIR%" 2>nul
)
if not exist "%CONFIG_DIR%" (
    mkdir "%CONFIG_DIR%" 2>nul
)

:: 2. Check for .NET SDK (dotnet)
echo [*] Checking for .NET 6.0 SDK...
dotnet --version >nul 2>&1
if %ERRORLEVEL% NEQ 0 (
    echo.
    echo [ERROR] .NET SDK is not installed or not in your system PATH!
    echo         Please download and install the .NET 6.0 SDK from:
    echo         https://dotnet.microsoft.com/download/dotnet/6.0
    echo.
    pause
    exit /b 1
)

for /f "tokens=*" %%v in ('dotnet --version') do set "DOTNET_VER=%%v"
echo [OK] Found .NET SDK version: %DOTNET_VER%
echo.

:: 3. Setup Temp Work Directory inside Plugins folder
set "TEMP_WORK_DIR=%PLUGINS_DIR%\__hydralum_temp_build"
set "ZIP_FILE=%PLUGINS_DIR%\hydralum_source.zip"

if exist "%TEMP_WORK_DIR%" rmdir /s /q "%TEMP_WORK_DIR%" 2>nul
mkdir "%TEMP_WORK_DIR%" 2>nul

:: 4. Download Source Code Zip
echo [*] Downloading latest Hydralum source from GitHub...
echo     URL: %REPO_ZIP_URL%

curl.exe -L -f -s -S -o "%ZIP_FILE%" "%REPO_ZIP_URL%"
if %ERRORLEVEL% NEQ 0 (
    echo [*] curl failed, trying PowerShell download...
    powershell -NoProfile -Command "[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; Invoke-WebRequest -Uri '%REPO_ZIP_URL%' -OutFile '%ZIP_FILE%'"
)

if not exist "%ZIP_FILE%" (
    echo.
    echo [ERROR] Failed to download source zip file from GitHub!
    echo         Please check your internet connection.
    rmdir /s /q "%TEMP_WORK_DIR%" 2>nul
    pause
    exit /b 1
)
echo [OK] Source zip downloaded successfully.
echo.

:: 5. Extract Source Code
echo [*] Extracting source code...
powershell -NoProfile -Command "Expand-Archive -Path '%ZIP_FILE%' -DestinationPath '%TEMP_WORK_DIR%' -Force"
if %ERRORLEVEL% NEQ 0 (
    echo [ERROR] Failed to extract source archive.
    del /f /q "%ZIP_FILE%" 2>nul
    rmdir /s /q "%TEMP_WORK_DIR%" 2>nul
    pause
    exit /b 1
)
echo [OK] Extracted successfully.
echo.

:: Find root extracted folder (usually Hydralum-main)
for /d %%D in ("%TEMP_WORK_DIR%\*") do (
    set "SOURCE_ROOT=%%D"
)

if not exist "!SOURCE_ROOT!\Hydra-main" (
    set "SOURCE_ROOT=%TEMP_WORK_DIR%"
)

:: 6. Build Both DLL Projects
echo ======================================================================
echo                     BUILDING MOD BINARIES
echo ======================================================================
echo.

echo [*] Building HydraMenu (Release)...
cd /d "!SOURCE_ROOT!\Hydra-main"
dotnet build src/HydraMenu.csproj -c Release --no-incremental
if %ERRORLEVEL% NEQ 0 (
    echo.
    echo [ERROR] Failed to build HydraMenu.dll!
    goto cleanup_fail
)
echo [OK] HydraMenu.dll built successfully.
echo.

echo [*] Building MalumMenuPlus (Release)...
cd /d "!SOURCE_ROOT!\MalumMenuPlus\MalumMenu-main"
dotnet build src/MalumMenu.csproj -c Release --no-incremental
if %ERRORLEVEL% NEQ 0 (
    echo.
    echo [ERROR] Failed to build MalumMenuPlus.dll!
    goto cleanup_fail
)
echo [OK] MalumMenuPlus.dll built successfully.
echo.

:: 7. Locate Built DLLs
set "BUILT_HYDRA=!SOURCE_ROOT!\Hydra-main\src\bin\Release\net6.0\HydraMenu.dll"
set "BUILT_MALUM=!SOURCE_ROOT!\MalumMenuPlus\MalumMenu-main\src\bin\Release\net6.0\MalumMenuPlus.dll"

if not exist "!BUILT_HYDRA!" (
    echo [ERROR] Compiled HydraMenu.dll not found at expected path!
    goto cleanup_fail
)
if not exist "!BUILT_MALUM!" (
    echo [ERROR] Compiled MalumMenuPlus.dll not found at expected path!
    goto cleanup_fail
)

:: 8. Smart-Delete Old DLLs from Plugins folder
echo [*] Cleaning up old Malum / Hydra DLLs from plugins folder...
del /f /q "%PLUGINS_DIR%\HydraMenu.dll" 2>nul
del /f /q "%PLUGINS_DIR%\Hydra.dll" 2>nul
del /f /q "%PLUGINS_DIR%\MalumMenuPlus.dll" 2>nul
del /f /q "%PLUGINS_DIR%\MalumMenu.dll" 2>nul
del /f /q "%PLUGINS_DIR%\Hydralum*.dll" 2>nul

:: 9. Copy Newly Built DLLs to Plugins Folder
echo [*] Installing fresh DLLs into "%PLUGINS_DIR%"...
copy /y "!BUILT_HYDRA!" "%PLUGINS_DIR%\" >nul
copy /y "!BUILT_MALUM!" "%PLUGINS_DIR%\" >nul

if exist "%PLUGINS_DIR%\HydraMenu.dll" (
    if exist "%PLUGINS_DIR%\MalumMenuPlus.dll" (
        echo [OK] Successfully installed HydraMenu.dll and MalumMenuPlus.dll!
    )
)
echo.

:: 10. Handle Configs (Prompt with saved preference option)
set "CONFIG_ACTION="

if exist "%PREFS_FILE%" (
    set /p SAVED_PREF=<"%PREFS_FILE%"
    if "!SAVED_PREF!"=="KEEP" (
        echo [*] Auto-keeping existing configs (preference saved).
        set "CONFIG_ACTION=KEEP"
    ) else if "!SAVED_PREF!"=="DELETE" (
        echo [*] Auto-deleting old configs (preference saved).
        set "CONFIG_ACTION=DELETE"
    )
)

if "!CONFIG_ACTION!"=="" (
    echo ======================================================================
    echo                        CONFIG MANAGEMENT
    echo ======================================================================
    echo  This build can generate fresh, default configuration files.
    echo  Would you like to delete your old config files?
    echo.
    echo  [1] Yes - Delete old configs (Fresh reset)
    echo  [2] No  - Keep existing configs (Recommended)
    echo  [3] No  - Keep existing configs and DO NOT ask again
    echo  [4] Yes - Delete old configs and DO NOT ask again
    echo.
    set /p "CHOICE=Select an option [1-4] (Default is 2): "
    if "!CHOICE!"=="" set "CHOICE=2"

    if "!CHOICE!"=="1" (
        set "CONFIG_ACTION=DELETE"
    ) else if "!CHOICE!"=="2" (
        set "CONFIG_ACTION=KEEP"
    ) else if "!CHOICE!"=="3" (
        set "CONFIG_ACTION=KEEP"
        echo KEEP>"%PREFS_FILE%"
        echo [*] Preference saved: Will keep configs without asking in future.
    ) else if "!CHOICE!"=="4" (
        set "CONFIG_ACTION=DELETE"
        echo DELETE>"%PREFS_FILE%"
        echo [*] Preference saved: Will delete configs without asking in future.
    ) else (
        set "CONFIG_ACTION=KEEP"
    )
)

if "!CONFIG_ACTION!"=="DELETE" (
    echo [*] Deleting old mod configuration files...
    del /f /q "%CONFIG_DIR%\com.mrd.hydramenu.cfg" 2>nul
    del /f /q "%CONFIG_DIR%\com.scp222thj.malummenu.cfg" 2>nul
    del /f /q "%CONFIG_DIR%\MalumProfile.txt" 2>nul
    echo [OK] Old config files deleted. Fresh configs will be generated in-game.
) else (
    echo [OK] Existing configurations preserved.
)
echo.

:: 11. Cleanup Temporary Files
echo [*] Cleaning up temporary build artifacts and downloaded zip...
cd /d "%AU_DIR%"
if exist "%ZIP_FILE%" del /f /q "%ZIP_FILE%" 2>nul
if exist "%TEMP_WORK_DIR%" rmdir /s /q "%TEMP_WORK_DIR%" 2>nul

echo ======================================================================
echo                     INSTALLATION COMPLETE!
echo ======================================================================
echo.
echo  Hydralum (HydraMenu + MalumMenuPlus) has been successfully built
echo  and installed into your Among Us plugins folder!
echo.
echo  Launch Among Us and press [Delete] in-game to open the menu.
echo ======================================================================
echo.
pause
exit /b 0

:cleanup_fail
echo.
echo [!] Cleaning up temporary build files...
cd /d "%AU_DIR%"
if exist "%ZIP_FILE%" del /f /q "%ZIP_FILE%" 2>nul
if exist "%TEMP_WORK_DIR%" rmdir /s /q "%TEMP_WORK_DIR%" 2>nul
echo.
echo [FAIL] Installation failed. Please check the error messages above.
echo.
pause
exit /b 1
