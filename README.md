# HSK-RH2-Collection

HSK/CE-patched conversions of **Chicken Plucker's RH2** mod series, tuned for the [Hardcore SK](https://github.com/skyarkhangel/Hardcore-SK) modpack and bundled with Combat Extended compatibility patches.

This repository contains **three separate mods**, each in its own subfolder. RimWorld's mod manager (and the HSK launcher) scan for `About/About.xml`, so each subfolder is recognized as an independent mod.

## Requirements

- **RimWorld 1.5**
- **Harmony**
- **Hardcore SK Modpack**
- **Combat Extended**

## What's Inside

### 🛋 HSK-RH2-UncleBorisFurniture
Full HSK/CE conversion of **[RH2] Uncle Boris — Used Furniture**. All furniture defs re-recipe'd to HSK conventions, tradeability tags adjusted, integrated with HSK's research and component economy.

### 👕 HSK-RH2-RimmuNation-Clothing
Full HSK/CE conversion of **[RH2] Rimmu-Nation² — Clothing**. 153 apparel items (tops, pants, shells, hats, masks, body armor, helmets, webbing, backpacks) re-recipe'd to HSK benches with HSK material conventions. Auto-generated `recipeMaker` blocks stripped — all recipes are explicit `RecipeDef`s using HSK's tailoring + electric tailoring benches.

### 🔫 HSK-RH2-RimmuNation-Weapons
Full HSK/CE conversion of **[RH2] Rimmu-Nation² — Weapons**. Includes the **HSKRNRouter** Harmony patch, which auto-routes weapon recipes to the correct RN bench (CQB / LBW / Sharpshooter / etc.) based on each weapon's CE `ammoSet`. Modern calibers go to RN benches; preindustrial calibers (musket, Minie, Govt, Rimfire) stay at the cast-iron armory.

**HSKRNRouter is mod-friendly:** the routing table is data-driven — adding new weapons / calibers to the right bench just means appending a line to the routing list and rebuilding the DLL. No hardcoded recipe paths.

## Installation

1. Clone or download this repo
2. Place each subfolder in your RimWorld `Mods/` directory (or point your HSK launcher at this repo URL — it will scan the subfolders automatically)
3. Enable the individual mods in your modlist
4. Load **after** Hardcore SK, Combat Extended, and the upstream RH2 mods if you have them disabled

⚠ **Do not enable the upstream Chicken Plucker versions alongside these.** Each HSK conversion is marked `incompatibleWith` its upstream `packageId` in `About.xml` — the launcher will warn you if both are active.

## How It Works

Each conversion replaces or extends the upstream mod's def files in-place:

- **Recipes** are re-pointed at HSK benches (`HandTailoringBench`, `ElectricTailoringBench`, `RN_*_WeaponBench`, etc.) with HSK research gates
- **Materials** are mapped from vanilla generics (Cloth/Steel/Plasteel) to HSK-specific equivalents where appropriate (Kevlar, SyntheticFibers, ComponentIndustrial tiers)
- **Combat Extended** patches add `Verb_ShootCE`, `CompProperties_AmmoUser`, `AmmoSet`, and proper projectile bindings to every weapon
- **HSKRNRouter** (in the Weapons mod) is a `StaticConstructorOnStartup` Harmony patch that runs once at game load and assigns each weapon recipe to its matching RN bench based on the weapon's ammo set

## Reporting Issues

If you find a bug, please attach your `Player.log` and a description of which subfolder the issue is in. Issues that don't include logs may be closed.

## Authorship

- Original mods: **Chicken Plucker** and the RH2 team
- HSK/CE conversion, Vile's compat integration, HSKRNRouter: **CarbineAction**
- CE patches by the **Combat Extended community**
- HSK material economy and bench conventions: **Hardcore SK Team**

## License

Each subfolder follows the original mod author's license where applicable. The HSK conversion / compatibility work is released under the same terms as the upstream mods — free use, modification, and redistribution. Credit appreciated.

## Contact

Issues / suggestions / PRs → open an issue on this repo.
