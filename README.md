# HSK-RH2-Collection

HSK/CE/Vile's-patched conversions of **Chicken Plucker's RH2** mod series, tuned for the [Hardcore SK modpack](https://github.com/skyarkhangel/Hardcore-SK) and bundled with Combat Extended compatibility patches.

This single repository contains **three separate mods**, each in its own subfolder. The HSK launcher (and RimWorld's mod manager) scan for `About/About.xml` files, so each subfolder is picked up as an independent mod.

## Included mods

| Folder | Upstream | Status | Notes |
|---|---|---|---|
| [`HSK-RH2-UncleBorisFurniture/`](./HSK-RH2-UncleBorisFurniture) | [RH2] Uncle Boris - Used Furniture | Stable | Full HSK/CE conversion |
| [`HSK-RH2-RimmuNation-Clothing/`](./HSK-RH2-RimmuNation-Clothing) | [RH2] Rimmu-Nation² - Clothing | In progress | recipeMaker removal + HSK RecipeDef creation pending |
| [`HSK-RH2-RimmuNation-Weapons/`](./HSK-RH2-RimmuNation-Weapons) | [RH2] Rimmu-Nation² - Weapons | Mostly stable | Known issue: customizations reset during load — fix pending |

## Requirements

All three mods require at minimum:

- **Hardcore SK** (Core_SK and its ecosystem)
- **Combat Extended**
- **Harmony**

## Install

Clone or download this repo. Place each subfolder in your RimWorld `Mods/` directory (or point your HSK launcher at this repo URL — it will scan the subfolders automatically). Enable the individual mods in your modlist.

Do **not** enable the upstream Chicken Plucker versions alongside these — each HSK conversion is marked `incompatibleWith` its upstream ID in its `About.xml`.

## Authorship

- Original mods: **Chicken Plucker** and the RH2 team
- HSK/CE conversion, Vile's compat integration: **CarbineAction**
- CE patches by the **Combat Extended community**

## License

Each subfolder follows the original mod author's license where applicable. The HSK conversion / compatibility work is released under the same terms as the upstream mods — free use, modification, and redistribution, credit appreciated.

## Contact

Issues / suggestions / PRs → open an issue on this repo.
