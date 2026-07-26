# Cat App (`com.actionfit.cat.app`)

Cat App provides shared Cat product services for loop, countdown, rewards, sound, analytics routing, and order completion snapshots.

## Install

After an approved package release, add the immutable tag to the consuming project's manifest.

```json
{
  "dependencies": {
    "com.actionfit.cat.app": "https://github.com/ActionFit-Editor/Cat_App.git#0.3.1"
  }
}
```

## Runtime

- `CatLoop` - shared frame/update dispatch.
- `CatCountdown` - shared text countdown registrations.
- `CatContentRewardService` - Content Core reward mapping, persistence, and idempotent grant boundary.
- `CatSoundService` - Cat sound selection/playback model.
- `CatAnalyticsRouter` - product analytics destination routing.
- `CatOrderCompletionSnapshot` - stable order-completion input.

## Lava Rush

This package no longer owns Cat Merge Lava Rush production. Production is the restored build-5.7.0 local implementation under `Assets/_Project/Content/LavaRush`. See:

- `Docs/AI/architecture/lava-rush-570-source-parity.md`
- `Docs/AI/contents/lava-rush.md`

There is no Cat App Lava Rush Addressables registration menu. The existing project Addressable entries and original mixed local/package prefab ownership are preserved.

## Unity Menu

- README: `Tools > Package > Cat App > README`

## Release Boundary

Repository creation, push, tags, catalog registration, and package publication require separate approval.
