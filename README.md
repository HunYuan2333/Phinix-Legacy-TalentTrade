# Phinix Legacy Talent Trade

<p align="center">
  English · <a href="./README.zh-CN.md">简体中文</a>
</p>

The official Phinix managed plugin port of the legacy Talent Trade mod (Package ID: `phinix.legacy.talent-trade`), enabling inter-colony trading, hiring, and pawn transfers in RimWorld 1.6.

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
