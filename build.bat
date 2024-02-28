set "BUILD_PATH=.\release
mkdir ".\release"

cd ./BoomboxSynthesizer
echo Building BoomboxSynthesizer
start /b /wait "" build || (
   echo Build failed for BoomboxSynthesizer
   exit			
)
echo Building BoomboxMod
cd ../BoomboxMod
start /b /wait "" build || (
   echo Build failed for BoomboxMod
   exit			
)
cd ..
xcopy /y "%BUILD_PATH"