# Cat App (com.actionfit.cat.app)

Cat App은 Cat Merge Cafe를 위한 private 제품 소유 composition root 선언 패키지입니다. 버전 `0.1.0`은 runtime composition이나 프로젝트 콘텐츠가 이미 이동했다고 간주하지 않고, 패키지 지향 project-shell 마이그레이션 목표만 기록합니다.

## 설치

저장소는 Private입니다. 별도 승인된 수동 publish 후 권한이 있는 프로젝트가 패키지를 `Packages/manifest.json`에 추가할 수 있습니다.

```json
{
  "dependencies": {
    "com.actionfit.cat.app": "https://github.com/ActionFitGames/Cat_App.git#0.1.1"
  }
}
```

저장소 접근 권한이 필요합니다. 이 패키지에는 자격 증명이나 private 설정이 포함되지 않습니다.

## 현재 경계

- `AI_GUIDE.md`는 Cat Merge Cafe 제품 composition root와 AI Refactor 목표에 대한 유일한 패키지 소유 선언입니다.
- 이 릴리스에는 Runtime 어셈블리, 게임플레이 구현, 패키지 의존성, 프로젝트 adapter, 에셋 마이그레이션 또는 Agent Skill이 없습니다.
- 선언은 읽기 전용 아키텍처 분석 대상을 식별합니다. `Assets`, 어셈블리, 패키지 또는 runtime 소유권 마이그레이션이 완료되었다는 증거는 아닙니다.
- 이 패키지를 설치해도 AI Code Convention profile이 선택되지 않습니다. 사용하는 프로젝트는 primary AI router의 명시적 profile selector를 유지합니다.

## Unity 메뉴

- README: `Tools > Package > Cat App > README`
- 실행 가능한 명령이나 설정 에셋은 없습니다.

## AI 가이드

- Cat Merge Cafe 제품 composition, 패키지 소유권, project-shell 마이그레이션 또는 패키지 의존성 구조를 분석하거나 변경하기 전에 `AI_GUIDE.md`를 읽습니다.

## 어셈블리

- **Editor** (`com.actionfit.cat.app.Editor`): README 전용 패키지 메뉴입니다.

## 릴리스 경계

저장소 생성, push, 태그 생성, 카탈로그 등록과 패키지 publish는 별도 승인이 필요하며 이 embedded 패키지를 추가하는 것만으로 실행되지 않습니다.
