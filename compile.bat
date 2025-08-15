@echo off
cd .\Proxy
dotnet publish -c Release
pause
taskkill /F /IM dotnet.exe