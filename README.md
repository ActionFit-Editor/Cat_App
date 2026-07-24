# Cat App (com.actionfit.cat.app)

Cat App은 Cat Merge Cafe를 위한 Public 제품 소유 composition root 패키지입니다. `0.2.1`은 `0.2.0`의 Lava Rush production 조합, 단일 engine, controller cache, 구독 수명, 호환 facade와 명시적 Addressables 등록 도구를 보존하면서 Cat 전용 fallback/font policy를 `com.actionfit.cat.fonts@1.0.0`에 직접 조합합니다. Lava Rush UI `0.2.1`의 shared-font 자산 경계와 호환되며, 프로젝트 저장소·에셋·생성 테이블·SDK·Addressable handle과 Unity lifecycle은 하나의 Project Shell seam에서 명시적으로 주입합니다.

## 설치

저장소 공개 범위는 Public입니다. 별도 승인된 수동 publish가 완료되면 프로젝트가 패키지를 `Packages/manifest.json`에 추가할 수 있습니다.

```json
{
  "dependencies": {
    "com.actionfit.cat.app": "https://github.com/ActionFit-Editor/Cat_App.git#0.2.1"
  }
}
```

이 패키지에는 자격 증명, private 설정 또는 제품 구현 소스가 포함되지 않습니다.

## 현재 경계

- `AI_GUIDE.md`는 Cat Merge Cafe 제품 composition root와 AI Refactor 목표에 대한 유일한 패키지 소유 선언입니다.
- `Runtime/com.actionfit.cat.app.asmdef`은 승인된 Cat 제품 owner만 포함하며 `Assembly-CSharp`, `Main`, `DatabaseManager`, `TimeProvider`, 생성 Table SO와 프로젝트 에셋을 참조하지 않습니다.
- `CatLoop`과 `CatCountdown`은 frame/late/game/1초 dispatch, speed gate, trusted-time 대기, 등록·취소·만료·formatter 정책을 소유합니다.
- `CatLavaRushTimingAdapter`는 동일한 Loop/Countdown 인스턴스를 UI 패키지의 timing port에 연결합니다.
- `CatLavaRushStateStore`, `CatLavaRushPersistenceOwner`, `CatLavaRushCatalogResolver`는 고정 runtime state, legacy import/marker/reset 순서와 생성 row 변환 정책을 소유하며 실제 key/flush/table 읽기는 소비 프로젝트가 주입합니다.
- `CatContentRewardService`는 reward ID 정규화, 중복 합산, attachment receipt, 확인과 replay를 소유하며 실제 ledger와 economy mutation은 소비 프로젝트가 주입합니다.
- `CatLavaRushComposition`은 production `LavaRushEngine` 하나, controller context/cache, day/mode/unlock/order/merge 구독, EventAccess와 prewarm 수명을 단독 소유합니다.
- global `LavaRushManager`는 `Main.LavaRush`, `Controller`, `GetAsync`, `StartEvent`, `StartPopup`, schedule/gameplay/reward/timer/reset 호출을 패키지 composition에 위임하는 호환 facade입니다.
- 기존 `Main.Loop`, `Main.Countdown`과 shared reward service는 package owner에 위임하는 호환 facade로 유지됩니다.
- Lava Rush 프로필 roster, 사운드 cue 정책, 18개 semantic localization mapping과 6개 분석 event schema는 SDK·프로젝트 타입을 모르는 제품 서비스로 제공됩니다.
- Order completion snapshot과 priority-100 reward adapter는 중복 item level을 보존하고 effect-before-progress 순서를 유지하며 enabled lifetime에만 등록됩니다.
- EventAccess adapter는 `UI_LavaRush_Icon`/`UI_LavaRush_Cell` key와 Cat adapter type을 분리하고, slot `2`, post-load 상태 재검사, click/countdown/frame 전달과 retryable bind를 보존합니다.
- `CatLavaRushDynamicController`는 `UI_LavaRush`/Half/camera/font 요청과 단일 cache/gate를 소유합니다. Project Shell은 outer instance 생성/파괴와 Addressable handle을 계속 소유합니다.
- `com.actionfit.cat.fonts@1.0.0`은 fallback asset, global `FontFallbackBinder`, locale 정책과 Editor guard/tooling을 소유합니다. Project Shell은 시작·옵션·동적 UI 수명 지점에서 이 패키지 API를 호출합니다.
- `DataStore`, `BotNameSO`, `ProfileStringData`, `Main.Sound`, General StringTable, TD/Singular와 생성 reward table은 Project Shell adapter 뒤에 남습니다.
- Cat Merge 생산 binding은 `Assets/_Project/_Shared/Main/Base/Main.LavaRush.cs` 하나이며, `Assets/_Project/Content/LavaRush/Scripts`의 production Runtime은 존재하지 않습니다.
- Editor 등록 도구는 canonical package prefab GUID를 검증한 뒤 `UI_LavaRush`, `UI_LavaRush_Icon`, `UI_LavaRush_Cell` 중 누락된 항목만 현재 writable bundled default group에 만듭니다. 기존 항목·group·label·address를 이동하거나 덮어쓰지 않습니다.
- 선언과 이 전환은 Lava Rush 범위만 다루며 다른 `Assets`, 어셈블리, 패키지 또는 runtime 소유권 마이그레이션이 완료됐다는 증거는 아닙니다.
- 이 패키지를 설치해도 AI Code Convention profile이 선택되지 않습니다. 사용하는 프로젝트는 primary AI router의 명시적 profile selector를 유지합니다.

## Unity 메뉴

- README: `Tools > Package > Cat App > README`
- 읽기 전용 등록 점검: `Tools > Package > Cat App > Lava Rush Addressables > Preview Registration`
- 누락 항목 생성: `Tools > Package > Cat App > Lava Rush Addressables > Apply Missing Entries`

설치·복구·package import·Editor 시작·batchmode는 Addressables를 수정하지 않습니다. Apply는 preview와 별도 확인을 요구하며, address/GUID 충돌·설정 없음·호환되지 않는 default group이면 전체 작업을 차단합니다. 생성 도중 실패하면 그 시도에서 만든 항목을 모두 되돌립니다.

## AI 가이드

- Cat Merge Cafe 제품 composition, 패키지 소유권, project-shell 마이그레이션 또는 패키지 의존성 구조를 분석하거나 변경하기 전에 `AI_GUIDE.md`를 읽습니다.

## 어셈블리

- **Runtime** (`com.actionfit.cat.app`): Cat 제품 Loop, Countdown과 Lava Rush core/durability/product-service/Event Shell owner입니다.
- **Editor** (`com.actionfit.cat.app.Editor`): README와 create-only Lava Rush Addressables 등록 메뉴입니다.

## 릴리스 경계

저장소 생성, push, 태그 생성, 카탈로그 등록과 패키지 publish는 별도 승인이 필요하며 이 embedded 패키지를 추가하는 것만으로 실행되지 않습니다.
