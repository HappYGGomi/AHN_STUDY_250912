@echo off
echo ========================================
echo 문서 복호화 도구 빌드 스크립트
echo ========================================
echo.

echo .NET SDK 확인 중...
for /f "tokens=1 delims=." %%i in ('dotnet --version 2^>nul') do set MAJOR_VERSION=%%i
if %errorLevel% neq 0 (
    echo 오류: .NET SDK가 설치되어 있지 않습니다.
    echo https://dotnet.microsoft.com/download 에서 .NET 6.0 이상 SDK를 다운로드하여 설치해주세요.
    pause
    exit /b 1
)

echo 현재 .NET 버전: 
dotnet --version

REM .NET 6.0 이상인지 확인
if %MAJOR_VERSION% LSS 6 (
    echo 오류: .NET 6.0 이상이 필요합니다. 현재 버전: %MAJOR_VERSION%
    echo https://dotnet.microsoft.com/download 에서 .NET 6.0 이상 SDK를 다운로드하여 설치해주세요.
    pause
    exit /b 1
) else (
    echo .NET 버전 확인 완료 (6.0 이상)
)

echo.
echo 프로젝트 복원 중...
dotnet restore
if %errorLevel% neq 0 (
    echo 오류: 프로젝트 복원에 실패했습니다.
    pause
    exit /b 1
)

echo.
echo 프로젝트 빌드 중...
dotnet build --configuration Release
if %errorLevel% neq 0 (
    echo 오류: 프로젝트 빌드에 실패했습니다.
    pause
    exit /b 1
)

echo.
echo 실행 파일 게시 중...
dotnet publish --configuration Release --runtime win-x64 --self-contained true --output ./publish
if %errorLevel% neq 0 (
    echo 오류: 실행 파일 게시에 실패했습니다.
    pause
    exit /b 1
)

echo.
echo 빌드가 완료되었습니다!
echo 실행 파일 위치: .\publish\DocumentDecryptor.exe
echo.
pause
