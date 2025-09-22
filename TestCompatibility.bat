@echo off
echo ========================================
echo .NET 호환성 테스트 스크립트
echo ========================================
echo.

echo 현재 .NET 버전 확인 중...
dotnet --version
if %errorLevel% neq 0 (
    echo 오류: .NET SDK가 설치되어 있지 않습니다.
    pause
    exit /b 1
)

echo.
echo .NET 6.0 이상 호환성 테스트...
for /f "tokens=1 delims=." %%i in ('dotnet --version 2^>nul') do set MAJOR_VERSION=%%i

if %MAJOR_VERSION% LSS 6 (
    echo ❌ .NET 6.0 이상이 필요합니다. 현재 버전: %MAJOR_VERSION%
    echo https://dotnet.microsoft.com/download 에서 .NET 6.0 이상 SDK를 다운로드하여 설치해주세요.
    pause
    exit /b 1
) else (
    echo ✅ .NET 버전 확인 완료 (6.0 이상)
)

echo.
echo 프로젝트 복원 테스트...
dotnet restore
if %errorLevel% neq 0 (
    echo ❌ 프로젝트 복원에 실패했습니다.
    pause
    exit /b 1
) else (
    echo ✅ 프로젝트 복원 성공
)

echo.
echo 프로젝트 빌드 테스트...
dotnet build --configuration Release --no-restore
if %errorLevel% neq 0 (
    echo ❌ 프로젝트 빌드에 실패했습니다.
    pause
    exit /b 1
) else (
    echo ✅ 프로젝트 빌드 성공
)

echo.
echo ========================================
echo 호환성 테스트 완료!
echo ========================================
echo.
echo 현재 .NET 버전: %MAJOR_VERSION%
echo 지원되는 .NET 버전: 6.0, 7.0, 8.0, 9.0
echo.
pause
