@echo off
for /f "usebackq tokens=*" %%i in (`"%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe" -latest -property installationPath`) do (
  set "VSPATH=%%i"
)
"%VSPATH%\MSBuild\Current\Bin\MSBuild.exe" diplom.csproj /t:Rebuild /p:Configuration=Debug
