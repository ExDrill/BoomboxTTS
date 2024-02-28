@echo off
set "OUTPUT_DIR=..\release\"

dotnet build

copy "bin\Debug\BoomboxMod.dll" "%OUTPUT_DIR%"
exit