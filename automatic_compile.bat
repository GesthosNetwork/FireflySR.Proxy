@echo off
rd /s /q bin >nul 2>&1 & md bin\tool
pushd FireflySR.Proxy
dotnet publish
popd

for /r "FireflySR.Proxy\bin\Release" %%f in (win-x64\publish\*.exe win-x64\publish\config.json) do move "%%f" "bin" >nul 2>&1
for /r "Guardian\bin\Release" %%f in (win-x64\publish\*.exe) do move "%%f" "bin\tool" >nul 2>&1
rd /s /q FireflySR.Proxy\bin FireflySR.Proxy\obj Guardian\bin Guardian\obj >nul 2>&1

pause
taskkill /F /IM dotnet.exe