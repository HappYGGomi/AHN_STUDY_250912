# 문서 복호화 도구 (Document Decryptor)

## 개요
이 프로그램은 DSCS(Document Security Control System)로 암호화된 문서를 복호화하는 도구입니다. 
Parallels Windows 11 환경에서 사용할 수 있도록 개발되었습니다.

## 주요 기능
- DSCS DLL을 사용한 원본 복호화 방식 지원
- DSCS DLL이 없는 환경에서의 대체 복호화 방식 제공
- Windows 탐색기 오른쪽 클릭 컨텍스트 메뉴 지원
- GUI 및 명령행 인터페이스 제공

## 시스템 요구사항
- Windows 10/11
- .NET 6.0 Runtime (자체 포함 버전으로 빌드 가능)
- DSCSLink.dll (선택사항, 원본 복호화 방식 사용 시)

## 설치 방법

### 1. 빌드
```bash
# Windows에서 실행
Build.bat
```

### 2. 설치
```bash
# 관리자 권한으로 실행
Install.bat
```

## 사용 방법

### GUI 모드
1. `DocumentDecryptor.exe` 실행
2. "파일 선택" 버튼을 클릭하여 복호화할 파일 선택
3. "복호화 실행" 버튼 클릭

### 명령행 모드
```bash
DocumentDecryptor.exe "암호화된파일경로"
```

### 컨텍스트 메뉴 사용
1. 파일 탐색기에서 복호화할 파일 선택
2. 오른쪽 클릭
3. "문서 복호화" 선택

## 파일 구조
```
DocumentDecryptor/
├── DocumentDecryptor.cs      # 핵심 복호화 로직
├── Program.cs                # 메인 프로그램 진입점
├── MainForm.cs              # GUI 폼
├── DocumentDecryptor.csproj  # 프로젝트 파일
├── AddContextMenu.reg       # 컨텍스트 메뉴 등록
├── Install.bat              # 설치 스크립트
├── Build.bat                # 빌드 스크립트
└── README.md                # 이 파일
```

## 복호화 방식

### 1. DSCS 방식 (권장)
- 원본 프로그램과 동일한 방식
- DSCSLink.dll 필요
- 더 안정적이고 정확한 복호화

### 2. 대체 방식
- DSCS DLL이 없는 환경에서 사용
- XOR 기반 간단한 복호화
- 실제 암호화 알고리즘 분석 필요

## 문제 해결

### DSCS DLL 오류
- DSCSLink.dll이 C:\WINDOWS\ 폴더에 있는지 확인
- 원본 시스템에서 DLL 파일을 복사

### 복호화 실패
- 파일이 실제로 암호화되어 있는지 확인
- 파일 권한 확인
- 대체 복호화 방식 사용

## 개발자 정보
- 개발자: HaapYGGomi
- 프로젝트: AHN_STUDY_250912
- 버전: 1.0.0

## 라이선스
이 프로젝트는 개인 사용 목적으로 개발되었습니다.
