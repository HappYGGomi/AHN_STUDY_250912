# Document Decryptor

Windows Forms 기반 문서 복호화 도구입니다.

## 프로젝트 개요

이 프로젝트는 SoftCamp의 DSCS(Document Security Control System)를 사용하여 암호화된 문서를 복호화하는 Windows Forms 애플리케이션입니다.

## 주요 기능

- 파일 선택 및 복호화
- DSCS DLL 기반 암호화 파일 감지
- 복호화 결과 메시지 파일 생성
- 사용자 친화적인 GUI 인터페이스

## 시스템 요구사항

- Windows 10/11
- .NET 9.0 Runtime
- DSCSLink.dll (SoftCamp 제공)

## 빌드 방법

```bash
dotnet build --configuration Release
```

## 배포 방법

```bash
dotnet publish --configuration Release --runtime win-x86 --self-contained true
```

## 파일 구조

- `Program.cs` - 애플리케이션 진입점
- `MainForm.cs` - 메인 사용자 인터페이스
- `LocalDocumentDecryptor.cs` - DSCS DLL 기반 복호화 로직
- `WebDocumentDecryptor.cs` - 웹 기반 복호화 로직 (대체 방법)
- `DocumentDecryptor.csproj` - 프로젝트 설정

## 사용법

1. 애플리케이션 실행
2. "파일 선택" 버튼으로 암호화된 파일 선택
3. "복호화" 버튼 클릭
4. 복호화된 파일과 결과 메시지 파일 확인

## 주의사항

- 실제 복호화를 위해서는 DSCS DLL이 필요합니다
- 현재 버전은 테스트용으로 파일 복사만 수행합니다
- SoftCamp의 공식 DSCS 시스템과 연동되어야 완전한 기능을 사용할 수 있습니다

## 라이선스

이 프로젝트는 교육 및 연구 목적으로 제작되었습니다.

## 개발자

HaapYGGomi