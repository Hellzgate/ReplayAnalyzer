# ReplayAnalyzer

EDOPro 리플레이(`.yrpX`)를 분석하는 CLI 도구. **뼈대 단계**.

## 지금 되는 것

- 리플레이 폴더 요약(개수 / 압축 여부 / 최신 10개)
- 개별 파일 헤더 정보(매직·flag·확장헤더·압축 여부·본문 크기)

## 실행

```powershell
& "C:\Program Files\dotnet\dotnet.exe" build -c Release

# 폴더 요약
.\bin\Release\net8.0\ReplayAnalyzer.exe

# 파일 정보
.\bin\Release\net8.0\ReplayAnalyzer.exe "D:\edopro\replay\_LastReplay.yrpX"
```

## 다음 단계

1. LZMA 해제 (`SharpCompress` Decoder — 원시 LZMA1, `props[5]` + `datasize`)
2. 이름/파라미터 + 메시지 스트림 파싱
3. 카드 이름 매핑 (`cards.cdb`)
4. 전개 시퀀스 추출 / 통계 / `--json`
5. 폴더 일괄 모드 (한 프로세스)

자세한 포맷은 `../_shared/EDOPRO-COMMON.md`, 작업 규칙은 `AGENTS.md`.
