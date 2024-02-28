@echo off
set "OUTPUT_DIR=..\release\"

dotnet build

copy "bin\Debug\BoomboxSynthesizer.exe" "%OUTPUT_DIR%\BoomboxSynthesizer.exe"
copy "libs\System.Speech.dll" "%OUTPUT_DIR%"
exit