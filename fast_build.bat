@echo off
del /s /q bin >nul
cd .\RobinSR.Proxy
dotnet publish -c Release -r win-x64 /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true

for %%c in ("%~dp0bin\Release\net9.0\win-x64\publish\*") do (
    if exist "%%~c" (
        move "%%~c" "%~dp0bin" >nul
    )
)

for %%d in (
	"%~dp0bin\Debug"
	"%~dp0bin\Release"
	"%~dp0RobinSR.Proxy\obj"
) do (
    if exist "%%~d" (
        rmdir /s /q "%%~d" >nul
    )
)

for %%e in (
    "%~dp0bin\RobinSR.Proxy.pdb"
) do (
    if exist "%%~e" del "%%~e" >nul
)

pause
taskkill /F /IM dotnet.exe >nul
exit
