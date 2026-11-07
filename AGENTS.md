# AGENTS.md — 리플레이 분석기 (ReplayAnalyzer)

EDOPro 리플레이(`.yrpX`)를 읽어 **무슨 일이 있었는지** 분석하는 CLI 도구.
같은 워크스페이스의 `WindBotAIEditor`(봇 편집기)와는 **별개 프로젝트**다.

## 경로

| 항목 | 경로 |
|---|---|
| 소스 | `C:\Users\Main PC\Documents\기본 프로젝트\ReplayAnalyzer` |
| 리플레이 | `D:\edopro\replay\*.yrpX` (읽기 전용) |
| 카드 DB | `D:\edopro\expansions\cards.cdb` |
| 공통 문서 | `C:\Users\Main PC\Documents\기본 프로젝트\_shared\EDOPRO-COMMON.md` |
| 참고 구현 | `C:\Users\Main PC\Documents\기본 프로젝트\WindBotAIEditor\Services\ReplayParser.cs` |

## 기술 스택 (확정)

- **C# / .NET 8 / 콘솔** (`net8.0`)
- 리플레이 파서는 **이미 검증된 C# 구현**(`WindBotAIEditor\Services\ReplayParser.cs`)을 이식/공유한다.
  - 파이썬 재구현 금지(이중 구현 → drift)
- 압축 해제: `SharpCompress` (`Decoder`, 원시 LZMA1)
- 카드 이름: `Microsoft.Data.Sqlite` 로 `cards.cdb` 조회

## 빌드 / 실행

```powershell
Set-Location "C:\Users\Main PC\Documents\기본 프로젝트\ReplayAnalyzer"
& "C:\Program Files\dotnet\dotnet.exe" build -c Release
& ".\bin\Release\net8.0\ReplayAnalyzer.exe" "D:\edopro\replay\_LastReplay.yrpX"
```

## 현재 상태 (뼈대)

- [x] 헤더 파싱(매직/flag/압축 여부/크기/시각) — `--info`
- [x] 폴더 내 리플레이 나열 — 인자 없이 실행
- [ ] LZMA 해제 + 이름/파라미터 파싱
- [ ] 메시지 스트림 파싱 + 통계(패킷 수/턴/카드별 사용)
- [ ] 덱/카드 이름 매핑
- [ ] 전개 시퀀스 추출(내 턴 기준) → CSV/JSON
- [ ] 배치 모드(폴더 전체를 한 프로세스에서)

## 설계 원칙

1. **읽기 전용**: `D:\edopro` 를 수정하지 않는다.
2. **배치 우선**: 파일마다 프로세스를 띄우지 않는다(폴더 스캔은 한 번에).
3. 출력은 **사람이 읽는 표** + `--json` 옵션.
4. 실패한 파일은 건너뛰고 **요약에 카운트**.

## 검증 규칙

- `_LastReplay.yrpX` 회귀 기준: **패킷 1,259 / 잔여 0B / 턴 4**
- 압축 리플레이 예: `D:\edopro\replay\2026-10-26 23-02-43.yrpX` (flag 0x331)

## 작업 방식 (공통)

- 창을 띄우지 않는다(콘솔 CLI만). 실행 중 프로세스 강제 종료 금지.
- 무거운 빌드/스캔은 하나씩.
- 사용자에게는 **원인 → 수정 → 검증 결과 → 경로** 순서로 짧게 보고.
