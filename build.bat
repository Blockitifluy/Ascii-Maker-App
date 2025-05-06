@echo off

REM This script builds the project and copies the output to the specified directories.
REM It also copies the patterns.json file to the server directory.

REM Usage: build.bat
REM Make sure to run this script from the root of the project directory.

REM Set the path to the project directory

dotnet build
if %errorlevel% neq 0 (
	echo "Build failed"
	exit /b %errorlevel%
)

echo "build finished"

xcopy dist bin\Debug\net9.0\dist /E /I /Y
xcopy server\picture\patterns.json bin\Debug\net9.0\server\picture\patterns.json /I /Y
echo "copied dist and patterns.json"