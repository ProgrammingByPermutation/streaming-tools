echo off

:: Get the version number
FOR /F "tokens=* USEBACKQ" %%F IN (`git tag --points-at HEAD`) DO (SET version=%%F)

if [%version%]==[] (
	echo This build doesn't have a tag
	exit 1
)

echo on
call "C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\Common7\Tools\VsDevCmd.bat"
dotnet build streaming-tools/streaming-tools.sln /p:Configuration=Release /p:Platform="Any CPU"
xcopy streaming-tools\streaming-tools\bin\Release\net6.0 "v%version%\streaming-tools\" /s /e
xcopy streaming-tools\WindowsKeyboardHook\bin\Release\net6.0 "v%version%\WindowsKeyboardHook\" /s /e

"C:\Program Files\7-Zip\7z.exe" a "v%version%.zip" "v%version%"
