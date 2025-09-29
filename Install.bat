@echo off
echo ========================================
echo 문서 복호화 도구 설치 스크립트
echo ========================================
echo.

REM 관리자 권한 확인
net session >nul 2>&1
if %errorLevel% == 0 (
    echo 관리자 권한으로 실행 중...
) else (
    echo 이 스크립트는 관리자 권한이 필요합니다.
    echo 관리자 권한으로 다시 실행해주세요.
    pause
    exit /b 1
)

echo.
echo 1. 프로그램 디렉토리 생성 중...
if not exist "C:\DecryptTools" mkdir "C:\DecryptTools"

echo 2. 프로그램 파일 복사 중...
copy "DocumentDecryptor.exe" "C:\DecryptTools\"
if %errorLevel% neq 0 (
    echo 오류: DocumentDecryptor.exe 파일을 찾을 수 없습니다.
    echo 먼저 프로그램을 빌드해주세요.
    pause
    exit /b 1
)

echo 3. 컨텍스트 메뉴 등록 중...
regedit /s "AddContextMenu.reg"
if %errorLevel% == 0 (
    echo 컨텍스트 메뉴가 성공적으로 등록되었습니다.
) else (
    echo 경고: 컨텍스트 메뉴 등록에 실패했습니다.
)

echo.
echo ========================================
echo 설치가 완료되었습니다!
echo ========================================
echo.
echo 사용 방법:
echo 1. 파일 탐색기에서 복호화할 파일을 선택
echo 2. 오른쪽 클릭하여 "문서 복호화" 선택
echo 3. 복호화된 파일은 원본 파일과 같은 폴더에 .decrypted 확장자로 저장됩니다.
echo.
pause


