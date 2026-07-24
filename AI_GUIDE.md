# AI Guide - Cat App

This file is shipped inside the public product-owned package so an AI assistant can resolve the Cat Merge Cafe composition target without copying that target into the consuming project's local convention documents.

## Package Identity

- Package ID: `com.actionfit.cat.app`
- Display name: Cat App
- Repository: `https://github.com/ActionFit-Editor/Cat_App.git`
- Repository visibility: Public
- Current package version at generation time: `0.1.5`
- Unity version: `6000.2`

AI Product Composition Root: com.actionfit.cat.app
AI Refactor target: package-oriented-product

## Purpose And Boundary

This product-owned package is the composition root for Cat Merge Cafe's progressive package-oriented migration. It allows AI Code Convention and AI Refactor to resolve one explicit product target while the Unity project remains a thin shell for project settings, environment selection, safety guidance, and factual migration state.

Version `0.1.5` adds the Lava Rush Order/EventAccess compatibility adapters and the outer dynamic-controller cache/gate seam. It consumes neutral UI and MCC-1627 timing ports while leaving Order, EventAccessAnchor, Addressable handles, canvases, camera/font policy, generated tables, and the production switch in Project Shell or MCC-1631; Runtime must not reference `Assembly-CSharp`, `Main`, project managers, generated project types, project assets, or SDKs.

The stable Runtime surface is `CatLoop`, `CatCountdown`, `CatLavaRushTimingAdapter`, `CatLavaRushStateStore`, `CatLavaRushPersistenceOwner`, `CatLavaRushCatalogResolver`, and `CatContentRewardService` plus their narrow input models/interfaces. Preserve fixed-key semantics, runtime-first restore, first-only corrupt backup, snapshot-before-marker durability, exact reset scope, canonical reward sorting, attachment receipts, and reload verification. Do not move physical Cat keys, generated SO reads, broad flush, trusted clock selection, or economy mutation into this package merely to remove an adapter.

## Project Router Registration

This package should be listed in `Packages/com.actionfit.custompackagemanager/PACKAGE_AI_GUIDE_ROUTER.md`.

Requested router entry:

- `Packages/com.actionfit.cat.app/AI_GUIDE.md` - Cat App declares the Cat Merge Cafe product composition root and package-oriented project-shell migration target. Read when analyzing or changing product composition, package ownership, project-shell migration, or Cat package dependency structure.

If the package router is not already included in the AI assistant's default reading sequence, connect that router through the consuming project's existing primary AI entry point. Do not copy this package's declaration into project-local convention documents.

Read this file when:

- analyzing or changing the Cat Merge Cafe product composition root;
- planning migration from project-owned `Assets` code into product or reusable packages;
- comparing package, asmdef, and runtime ownership graphs;
- changing files under `Packages/com.actionfit.cat.app/`;
- preparing a later manual release for `com.actionfit.cat.app`.

## Product Composition Contract

- Keep both declaration lines as exact, complete, standalone lines inside `Package Identity`.
- Keep the declared product root equal to the sibling `package.json` `name`.
- The marker pair selects only the package-oriented product-composition target. It does not select `actionfit-unity`; that profile remains an explicit project-router decision.
- Resolve at most one installed product composition root. Missing, incomplete, duplicate, mismatched, misplaced, or unsupported declarations are diagnostics rather than permission to infer intent.
- Treat this package as product-owned and non-reusable. Reusable dependencies remain project-neutral and keep one-way dependency boundaries.
- Distinguish declared target edges from observed package, asmdef, and runtime edges. Do not report migration as complete without direct evidence.
- Keep project safety, workflow, current-state, compatibility, credential, environment, and migration facts with their factual owners until an explicitly approved owner preserves them elsewhere.
- Do not add dependencies merely to populate a target graph. Add a dependency only when an approved implementation creates a real product composition edge.
- The declaration does not authorize code or asset migration, document deletion, repository creation, publication, deployment, or external-system mutation.

## How To Work On This Package

- Treat `package.json` as the source for package ID, version, Unity version, and dependencies.
- Treat `Editor/PackageInfo/ActionFitPackageInfo_SO.asset` as the source for repository visibility, catalog metadata, owner, status, description, and the single-version release note.
- Keep `README.md` focused on human installation and the current product Runtime boundary.
- Keep this guide as the sole Cat product-composition target declaration and route it through the existing package guide router.
- Add Runtime owners and dependencies only when a separately approved Jira unit provides concrete ownership and dependency evidence. Keep project adapters narrow and one-way.
- Keep `ILavaRushProfileRoster`, `ILavaRushAudio`, semantic localization keys, and `ILavaRushAnalyticsSink` reusable-facing; Cat selection, routing, and mapping policy belongs in this product package.
- Keep `CatOrderCompletionSnapshot`, `CatLavaRushOrderProgressSource`, the priority-100 reward adapter, and the EventAccess descriptor/adapters Product-owned. Bind project Order and EventAccess implementations only at the later Project Shell composition edge.
- Keep `CatLavaRushDynamicController` specific to the `UI_LavaRush` Product boundary. Its injected binding may create or destroy the outer instance, but Product `Clear()` must not release the project Addressable handle or take over the inner `ILavaRushUIViewHost` lifetime.
- Keep DataStore/DataKeys, BotNameSO/ProfileStringData, Main.Sound/AudioClip, Unity project tables, generated reward rows, TDAnalytics, and SG_Manager in Project Shell leaves.

## Package Tools Menu

- Unity menu root: `Tools/Package/Cat App/`.
- `README` opens this package README at priority `901` in the README-only package band.
- This package has no executable command or settings ScriptableObject.

## Agent Skills

- This version has no `Skills~/manifest.json` and installs no Agent Skill.
- The package guide router and AI Refactor own discovery and read-only analysis; do not duplicate those workflows here.

## Release Note Rules

- `ActionFitPackageInfo_SO.ReleaseNote` is Korean and contains only the single version being prepared.
- Publishing remains manual through Custom Package Manager after a separate approval.
- Before reusing a version, check the remote repository and exact remote tags. Published tags are immutable.
- If this package changes after its version is tagged, use the next unused patch version.
