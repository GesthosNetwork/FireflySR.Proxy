@echo off

if exist "%~dp0bin" (
    rd /s /q "%~dp0bin"
)
mkdir "%~dp0bin\tool"
cd .\FireflySR.Proxy
dotnet publish -c Release

for %%c in (
   "%~dp0FireflySR.Proxy\bin\Release\net9.0\win-x64\publish\FireflySR.Proxy.exe"
   "%~dp0FireflySR.Proxy\bin\Release\net9.0\win-x64\publish\config.json"
) do (
    if exist "%%~c" (
       move "%%~c" "%~dp0bin"
    )
)

for %%c in (
   "%~dp0Guardian\bin\Release\net9.0\win-x64\publish\Guardian.exe"
) do (
    if exist "%%~c" (
       move "%%~c" "%~dp0bin\tool"
    )
)

for %%d in (
    "%~dp0FireflySR.Proxy\bin"
    "%~dp0FireflySR.Proxy\obj"
    "%~dp0Guardian\bin"
    "%~dp0Guardian\obj"
) do (
    if exist "%%~d" (
        rd /s /q "%%~d" >nul
    )
)

pause
taskkill /F /IM dotnet.exe