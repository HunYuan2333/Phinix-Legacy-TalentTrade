# Phinix Legacy Talent Trade

<p align="center">
  English · <a href="./README.zh-CN.md">简体中文</a>
</p>

The official Phinix managed plugin port of the legacy Talent Trade mod (Package ID: `phinix.legacy.talent-trade`), enabling inter-colony trading, hiring, and pawn transfers in RimWorld 1.6.

> [!NOTE]
> - **Technical Guide**: For full documentation on public APIs, lifecycle, threading, and resource ownership, see the [Phinix Plugin Development Guide](https://github.com/HunYuan2333/Phinix-Rework/blob/dev/docs/Plugin-Development.md).
> - **Lifecycle Status**: Current source uses `ClientExtensionModule.Compose`; the plugin's old `Register` entry is removed. Packages require abstractions `>=1.9.0 <3.0.0`. Full DI/static-context and callback-lifecycle remediation remains pending; local compilation and package validation do not certify release or game acceptance.
> - **Harmony Patching Constraint**: All Harmony patches are applied during `Activate` and removed during `Shutdown`; patching in static constructors is strictly prohibited to ensure that disabling the extension achieves true zero-patch isolation.
> - **Save Persistence Boundary**: Active market listings and rental return records reside in a save-local `GameComponent`. Never uninstall this plugin while transactions or outbound rentals are active, as missing types upon save loading can corrupt pawn state.
> - **CI & Automated Builds**: GitHub Actions automated compilation (build/package on main push only, no dev build) is actively scheduled under F7; packaging currently uses the local `pack.py` entry point.
> - **Skill Notice**: The Phinix plugin development skill is currently in preparation; integration links will be added once ready.

---

## Overview & Attribution

- **Purpose**: Provides pawn marketplaces, direct transfers, and rental contracts between colonies via the Phinix networking infrastructure.
- **Attribution**: Based on the original Phinix Talent Trade extension by **iniad**. Original author rights and license terms are preserved; this repository provides the standalone managed plugin port for Phinix Rework.
- **Delivery Model**: Unbundled from the core Phinix Mod package. It runs as an independent managed DLL plugin conforming to the standard Phinix extension lifecycle.

---

## Installation & Requirements

### In-Game Installation (Recommended)

1. Open the Phinix window in RimWorld and switch to the **Store** (`商店`) tab.
2. Locate **Talent trade** (v1.0.1) and click **Install** (`安装`).
3. **Restart RimWorld** for the game engine to load newly installed assemblies.

### Prerequisites

- **RimWorld 1.6**
- **Phinix Rework** (provides host extension runtime and client abstractions)
- **Harmony 2.3.6+** (required for pawn serialization and transfer patches)

---

## Features & Usage

1. **Talent Market (`市场`)**:
   - Browse colonists and slaves listed by other colonies.
   - Inspect pawn attributes, traits, skills, and equipment summaries before purchasing.
2. **Direct Trade (`直接交易`)**:
   - Initiate point-to-point negotiations with online colonies.
   - Propose mutual pawn transfers or trade pawns for silver.
3. **Rental Contracts (`租赁`)**:
   - Rent out colonists to other colonies for a specified period and fee.
   - Rented pawns automatically return to their home colony once the contract expires.
4. **Input Protection**:
   - Guarded input prevents negative pricing, invalid character entries, and malformed contract parameters.

---

## Save Data & Persistence Boundaries

- **GameComponent Storage**: Rented pawns and active market listings are tracked via a dedicated `GameComponent` stored within the colony's save file (`.rws`).
- **Authoritative Server Confirmation**: Pawn transfers require authoritative server acknowledgment. Submitting an offer locally does not guarantee transaction completion.
- **In-Memory Purchase Intents**: Purchase requests in flight exist in memory during the game session. An abrupt game crash or restart before server settlement cannot automatically resume in-memory transactions.

---

## Disabling & Uninstalling Safely

> [!CAUTION]
> If a save contains active pawn listings, outbound rented pawns, or pending return records, **do not disable or uninstall this plugin**. Loading a save without this plugin assembly active can cause missing-type deserialization errors and permanently corrupt pawn return tracking.

### Safe Removal Procedure

1. Cancel all active marketplace listings and retrieve pawns.
2. Wait for all outbound rented colonists to return safely to the home map.
3. Save the game and verify no pending contracts remain in the Talent Trade ledger.
4. Open **Extension Manager** (`扩展管理`), disable or uninstall **Talent trade**, and restart RimWorld.

---

## Build & Candidate Verification

Requires .NET 10 SDK, local Phinix-Rework source, and RimWorld 1.6 references:

```bash
# Verify source and references
python3 check-source.py

# Package candidate ZIP
python3 pack.py \
  --phinix-package <path-to-Phinix-Rework> \
  --game-references <path-to-RimWorld-Managed> \
  --harmony-references <path-to-Harmony-Assemblies> \
  --packager <path-to-ManagedPackageTool.dll> \
  --output <path-to-output>/phinix-legacy-talenttrade-1.0.1.zip
```

Never commit or redistribute RimWorld, Harmony, or host assemblies in the candidate package.

---

## Troubleshooting & Support

- **Transfer Pending**: Check network connectivity to the Phinix server; transactions pend until the server returns authoritative confirmation.
- **Log Submission**: When reporting issues on GitHub, provide sanitized player logs (`Player.log`), exact game version, Phinix version, and plugin version. Remove any private player identities or server credentials before posting logs.


2026-10-10：按用户确认新增客户端抽象 1.9 维护线，和 2.0 分别编译；默认新版本 1.1.0。范围、版本号及命令参数见 [客户端兼容构建](Client-Compatibility.md)。原先“仅 2.0”说明由本节更新。本地包已完成最终静态和宿主引用校验，游戏内验收及正式发布仍待完成。


**当前方案已修正为单包内部适配**：版本 1.1.1 的同一 ZIP 支持 API 1.9/2.0；之前两条发行线说明已撤销。旧接口和运行时识别仅在内部 adapter。构建基线、宿主绑定过渡和验证限制见 [客户端兼容说明](Client-Compatibility.md)。

## T4 local theme integration

The unpublished 1.1.2 candidate keeps one binary for client APIs 1.9/current. A private theme adapter binds ordinary current theme registration and immutable palettes; 1.9 uses plugin defaults. UI/business code has no version branches. Online publication remains deferred. See Client-Compatibility.md; game acceptance is pending.

Publication requires a fixed source commit, an immutable package, a reviewed index Issue and the maintainer's `plugin-approved` label. RedPacket pending-send/restart reconciliation and TalentTrade late GameComponent loading/missing-package resaving are release gates. Until these are passed, candidates are not admitted to the official catalog and bundled DLLs remain.

## main/dev and official releases

Develop on dev; it triggers no Actions. Every accepted push to main reserves the next patch version in the configured major/minor series, builds against fixed client/Common source and private compile-only references, then publishes an official GitHub Release with the plugin ZIP, SHA256SUMS and source/build summary. Retrying the same commit reuses its reserved version; failed builds leave an unpublished draft for retry and never replace published bytes. Parallel pushes reserve distinct versions without canceling pending builds.

Maintainers configure BUILD_REFERENCES_TOKEN as a repository secret, with Contents:Read only on the private compile-reference repository named in ci/config.json. This is a maintainer CI setup; third-party authors supply their own licensed references. No game/host/Harmony DLL is uploaded in public artifacts. Assembly identity versions remain independently controlled by source; the automatically assigned package release version is passed to pack.py --version.

Merging into main means the author has accepted publication. A GitHub Release does not bypass Index admission/source-update policy or prove game acceptance. Test changes on dev before merging. Fixed host/reference inputs are maintained explicitly in ci/config.json. Do not overwrite released ZIPs or expose the reference token.
