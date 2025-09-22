@echo off
echo ========================================
echo Parallels Windows 11로 파일 복사 스크립트
echo ========================================
echo.

REM Parallels 공유 폴더 경로 (일반적인 경로)
set PARALLELS_SHARED=%USERPROFILE%\Desktop

echo 1. 원본 복호화 프로그램 복사 중...
copy "%~dp0..\..\..\..\Downloads\복tn5101.exe" "%PARALLELS_SHARED%\복tn5101.exe"
if %errorLevel% == 0 (
    echo 원본 프로그램이 데스크톱에 복사되었습니다.
) else (
    echo 경고: 원본 프로그램 복사에 실패했습니다.
    echo 수동으로 복사해주세요.
)

echo.
echo 2. 새로 개발된 프로그램 복사 중...
if exist "publish\DocumentDecryptor.exe" (
    copy "publish\DocumentDecryptor.exe" "%PARALLELS_SHARED%\DocumentDecryptor.exe"
    if %errorLevel% == 0 (
        echo 새 프로그램이 데스크톱에 복사되었습니다.
    ) else (
        echo 경고: 새 프로그램 복사에 실패했습니다.
    )
) else (
    echo 경고: publish\DocumentDecryptor.exe 파일을 찾을 수 없습니다.
    echo 먼저 Build.bat을 실행해주세요.
)

echo.
echo 3. 설치 스크립트 복사 중...
copy "Install.bat" "%PARALLELS_SHARED%\Install.bat"
copy "AddContextMenu.reg" "%PARALLELS_SHARED%\AddContextMenu.reg"

echo.
echo ========================================
echo 복사가 완료되었습니다!
echo ========================================
echo.
echo Parallels Windows 11에서 다음 작업을 수행하세요:
echo 1. 데스크톱에서 복사된 파일들 확인
echo 2. DocumentDecryptor.exe 실행하여 테스트
echo 3. Install.bat을 관리자 권한으로 실행하여 설치
echo.
pause
