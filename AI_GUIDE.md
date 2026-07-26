# AI Guide - Cat App

## Package Identity

- Package ID: `com.actionfit.cat.app`
- Display name: Cat App
- Repository: `https://github.com/ActionFit-Editor/Cat_App.git`
- Current package version at generation time: `0.3.1`
- Unity version: `6000.2`
- Runtime dependencies: `com.actionfit.cat.fonts@2.0.0`, `com.actionfit.content-core@0.3.0`

## Project Router Registration

Requested router entry:

- `Packages/com.actionfit.cat.app/AI_GUIDE.md` - Cat App owns shared Cat loop, countdown, reward, sound, analytics, and order-completion services; it does not own Lava Rush production.

## Package Boundary

`com.actionfit.cat.app` contains shared Cat product services that remain useful across content integrations:

- `CatLoop`;
- `CatCountdown`;
- `CatContentRewardService` and reward mapping/persistence boundaries;
- `CatSoundService`;
- `CatAnalyticsRouter`;
- `CatOrderCompletionSnapshot`.

Cat App does not own or compose Lava Rush production. The post-5.7 `CatLavaRushComposition`, package `LavaRushManager` facade, dynamic controller, timing adapter, persistence owner, access/order adapters, profile/audio/localization adapters, and Lava Rush Addressables editor tool are retired from this package.

For Cat Merge Lava Rush, read `Docs/AI/architecture/lava-rush-570-source-parity.md`. The local manager and controllers under `Assets/_Project/Content/LavaRush` are production authority.

## Dependency Rules

- Keep dependencies limited to assemblies used by the remaining shared Cat services.
- Do not re-add Lava Rush engine/UI, Addressables, Localization, UI Popup, or UGUI dependencies merely to recreate the retired production composition.
- `CatContentRewardService` may depend on Content Core.
- `CatCountdown` may depend on TextMeshPro.

## Validation

- Compile Runtime and Editor test assemblies.
- Run Cat timing and reward tests that still correspond to package-owned services.
- No Cat App test or documentation may require `CatLavaRush*` types or the retired Lava Rush Addressables menu.

Package publication, tagging, catalog registration, and remote repository changes require separate approval.
