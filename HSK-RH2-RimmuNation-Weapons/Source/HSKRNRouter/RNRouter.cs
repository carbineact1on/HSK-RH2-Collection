// HSK RN Weapon Router (C#) — routes weapon recipes to RN benches by ammoSet.
//
// Runs once at game load (StaticConstructorOnStartup). For every RecipeDef
// that produces a weapon, looks up the weapon's CE ammoSet via reflection
// (no hard CE dependency), maps the ammoSet defName to one of the three RN
// benches (RNBench_CQB / RNBench_LBW / RNBench_HeavyWeapons), and replaces
// the recipe's recipeUsers list with that bench.
//
// Skips:
//   - Abstract recipes
//   - Recipes with no products
//   - Non-weapon products (apparel, etc.)
//   - Weapons without an ammoSet (melee, neolithic, etc.)
//   - AmmoSets not in the routing table (silently)
//   - Recipes already at one of the RN benches (RH2 native — leave alone)
//
// Logs a one-line summary at the end.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using Verse;

namespace HSKRNRouter
{
    [StaticConstructorOnStartup]
    public static class RNRouter
    {
        // RN bench defNames
        private const string BenchCQB = "RNBench_CQB";
        private const string BenchLBW = "RNBench_LBW";
        private const string BenchHeavy = "RNBench_HeavyWeapons";

        // RN bench defNames the recipe might already point at — skip those.
        private static readonly HashSet<string> AlreadyRN = new HashSet<string>
        {
            BenchCQB, BenchLBW, BenchHeavy
        };

        // AmmoSet defName prefix → RN bench mapping.
        // First matching prefix wins. Order matters — put more specific
        // patterns before broader ones if there's overlap.
        private static readonly (string Prefix, string Bench)[] Routing = new (string, string)[]
        {
            // ============================================================
            // CQB — pistols, SMGs, PDWs, shotguns
            // ============================================================
            ("AmmoSet_22LR",                   BenchCQB),
            ("AmmoSet_25ACP",                  BenchCQB),
            ("AmmoSet_32ACP",                  BenchCQB),
            ("AmmoSet_38Special",              BenchCQB),
            ("AmmoSet_357Magnum",              BenchCQB),
            ("AmmoSet_357SIG",                 BenchCQB),
            ("AmmoSet_380ACP",                 BenchCQB),
            ("AmmoSet_40SW",                   BenchCQB),
            ("AmmoSet_44Magnum",               BenchCQB),
            ("AmmoSet_45ACP",                  BenchCQB),
            ("AmmoSet_50AE",                   BenchCQB),
            ("AmmoSet_454Casull",              BenchCQB),
            ("AmmoSet_460SW",                  BenchCQB),
            ("AmmoSet_500SW",                  BenchCQB),
            ("AmmoSet_4_6x30mm",               BenchCQB),
            ("AmmoSet_46x30mm",                BenchCQB),
            ("AmmoSet_5_45x18mm",              BenchCQB),
            ("AmmoSet_545x18mm",               BenchCQB),
            ("AmmoSet_57x28mm",                BenchCQB),
            ("AmmoSet_FN57x28mm",              BenchCQB),
            ("AmmoSet_5_7x28mm",               BenchCQB),
            ("AmmoSet_762x25mm",               BenchCQB),
            ("AmmoSet_762x33mm",               BenchCQB),
            ("AmmoSet_9x18mmMakarov",          BenchCQB),
            ("AmmoSet_9x19mmPara",             BenchCQB),
            ("AmmoSet_9x21mm",                 BenchCQB),
            ("AmmoSet_Pistol",                 BenchCQB),
            ("AmmoSet_PistolMagnum",           BenchCQB),
            ("AmmoSet_HandgunCharged",         BenchCQB),
            // Shotguns
            ("AmmoSet_12Gauge",                BenchCQB),
            ("AmmoSet_20Gauge",                BenchCQB),
            ("AmmoSet_410Bore",                BenchCQB),
            ("AmmoSet_Shotgun",                BenchCQB),
            ("AmmoSet_Slugthrower",            BenchCQB),
            // Charged pistol-tier
            ("AmmoSet_5x35mmCharged",          BenchCQB),
            ("AmmoSet_8x50mmCharged",          BenchCQB),
            ("AmmoSet_10x18mmCharged",         BenchCQB),
            ("AmmoSet_ChargedPistol",          BenchCQB),
            // Misc CQB (added v2 from log audit)
            ("AmmoSet_44Rimfire",              BenchCQB),
            ("AmmoSet_762x25mmTT",             BenchCQB),
            ("AmmoSet_Flare",                  BenchCQB),
            ("AmmoSet_SlugthrowerShell",       BenchCQB),
            // CQB additions v3 (from runtime log diagnostic)
            ("AmmoSet_5mmVersatile",           BenchCQB),
            ("AmmoSet_9mmVersatile",           BenchCQB),
            ("AmmoSet_MauserP",                BenchCQB),
            ("AmmoSet_763x25mmM",              BenchCQB),
            ("AmmoSet_12Mag",                  BenchCQB),

            // ============================================================
            // LBW — rifles, snipers, DMRs, BattleRifles
            // ============================================================
            ("AmmoSet_223Rem",                 BenchLBW),
            ("AmmoSet_270Win",                 BenchLBW),
            ("AmmoSet_300WinMag",              BenchLBW),
            ("AmmoSet_300WinchesterMagnum",    BenchLBW),
            ("AmmoSet_300NormaMagnum",         BenchLBW),
            ("AmmoSet_303British",             BenchLBW),
            ("AmmoSet_30-06",                  BenchLBW),
            ("AmmoSet_3006",                   BenchLBW),
            ("AmmoSet_3030Win",                BenchLBW),
            ("AmmoSet_338LapuaMagnum",         BenchLBW),
            ("AmmoSet_338Lapua",               BenchLBW),
            ("AmmoSet_338Norma",               BenchLBW),
            ("AmmoSet_338NormaMagnum",         BenchLBW),
            ("AmmoSet_366TKM",                 BenchLBW),
            ("AmmoSet_408Cheyenne",            BenchLBW),
            ("AmmoSet_408CheyenneTactical",    BenchLBW),
            ("AmmoSet_416Barrett",             BenchLBW),
            ("AmmoSet_50BMG",                  BenchLBW),  // .50 BMG sniper rifles
            ("AmmoSet_50Beowulf",              BenchLBW),
            ("AmmoSet_4_5x39",                 BenchLBW),
            ("AmmoSet_45x39",                  BenchLBW),
            ("AmmoSet_5_45x39mm",              BenchLBW),
            ("AmmoSet_545x39",                 BenchLBW),
            ("AmmoSet_545x39mm",               BenchLBW),
            ("AmmoSet_5_45x39mmSoviet",        BenchLBW),
            ("AmmoSet_545x39mmSoviet",         BenchLBW),
            ("AmmoSet_556x45",                 BenchLBW),
            ("AmmoSet_5_56x45mmNATO",          BenchLBW),
            ("AmmoSet_556x45mmNATO",           BenchLBW),
            ("AmmoSet_6_5Creedmoor",           BenchLBW),
            ("AmmoSet_65Creedmoor",            BenchLBW),
            ("AmmoSet_6_5Grendel",             BenchLBW),
            ("AmmoSet_6_8SPC",                 BenchLBW),
            ("AmmoSet_762x39",                 BenchLBW),
            ("AmmoSet_762x39mm",               BenchLBW),
            ("AmmoSet_762x39mmSoviet",         BenchLBW),
            ("AmmoSet_762x51",                 BenchLBW),
            ("AmmoSet_762x51mm",               BenchLBW),
            ("AmmoSet_762x51mmNATO",           BenchLBW),
            ("AmmoSet_762x54",                 BenchLBW),
            ("AmmoSet_762x54mm",               BenchLBW),
            ("AmmoSet_762x54mmR",              BenchLBW),
            ("AmmoSet_792x57",                 BenchLBW),
            ("AmmoSet_792x57mm",               BenchLBW),
            ("AmmoSet_792x57mmMauser",         BenchLBW),
            ("AmmoSet_8x57",                   BenchLBW),
            ("AmmoSet_8x57JS",                 BenchLBW),
            ("AmmoSet_9x39",                   BenchLBW),
            ("AmmoSet_9x39mm",                 BenchLBW),
            ("AmmoSet_9x39mmSoviet",           BenchLBW),
            ("AmmoSet_Rifle",                  BenchLBW),
            ("AmmoSet_RifleIntermediate",      BenchLBW),
            ("AmmoSet_RifleMagnum",            BenchLBW),
            ("AmmoSet_BattleRifle",            BenchLBW),
            ("AmmoSet_AssaultRifle",           BenchLBW),
            ("AmmoSet_SniperRifle",            BenchLBW),
            // Charged rifle-tier
            ("AmmoSet_6x22mmCharged",          BenchLBW),
            ("AmmoSet_6x24mmCharged",          BenchLBW),
            ("AmmoSet_8x35mmCharged",          BenchLBW),
            ("AmmoSet_12x64mmCharged",         BenchLBW),
            ("AmmoSet_12x72mmCharged",         BenchLBW),
            ("AmmoSet_ChargedRifle",           BenchLBW),
            ("AmmoSet_ChargedShot",            BenchLBW),
            // Misc LBW (added v2 from log audit)
            ("AmmoSet_5070Govt",               BenchLBW),
            ("AmmoSet_5x50mmCaseless",         BenchLBW),
            ("AmmoSet_69Musket",               BenchLBW),
            ("AmmoSet_MinieBall",              BenchLBW),
            ("AmmoSet_BlasterBolt",            BenchLBW),
            ("AmmoSet_BlasterPelletBolt",      BenchLBW),
            ("AmmoSet_ChargedLaser",           BenchLBW),
            ("AmmoSet_LaserCenti",             BenchLBW),
            ("AmmoSet_NerveSpikerBolt",        BenchLBW),
            ("AmmoSet_PlasmaBolt",             BenchLBW),
            ("AmmoSet_PlasmaCellRifle",        BenchLBW),
            ("AmmoSet_PlasmaPelletBolt",       BenchLBW),
            ("AmmoSet_SalvagedLaserEradicator",BenchLBW),
            ("AmmoSet_RailRay",                BenchLBW),
            // LBW additions v3 (from runtime log diagnostic)
            ("AmmoSet_700NE",                  BenchLBW),
            ("AmmoSet_338LM",                  BenchLBW),
            ("AmmoSet_408CT",                  BenchLBW),
            ("AmmoSet_127x55mmAR",             BenchLBW),
            ("AmmoSet_127x42mmB",              BenchLBW),
            ("AmmoSet_473x33mmCaseless",       BenchLBW),
            ("AmmoSet_GaussCharged",           BenchLBW),
            ("AmmoSet_DC",                     BenchLBW),
            ("AmmoSet_8mmM",                   BenchLBW),
            ("AmmoSet_K98k",                   BenchLBW),
            ("AmmoSet_K90M",                   BenchLBW),
            ("AmmoSet_792x33mmK",              BenchLBW),
            ("AmmoSet_CGRMU",                  BenchLBW),
            ("AmmoSet_FLASH",                  BenchLBW),
            ("AmmoSet_SP",                     BenchLBW),
            ("AmmoSet_SR",                     BenchLBW),
            ("AmmoSet_132TuF",                 BenchLBW),
            ("AmmoSet_Schiessbecher",          BenchLBW),
            ("AmmoSet_Winchester",             BenchLBW),
            ("AmmoSet_67x29mm",                BenchLBW),  // covers Mag MG (LBW compromise) + Mag Rifle
            ("AmmoSet_Iguf_67x29mm",           BenchLBW),
            ("AmmoSet_IgufSniper_67x29mm",     BenchLBW),
            ("AmmoSet_XcomLaser",              BenchLBW),  // catches XcomLaser, XcomLaserLance, XcomLaserScatter (StartsWith)

            // ============================================================
            // HEAVY — anti-material rifles, autocannons, MGs, launchers
            // ============================================================
            // Anti-material / autocannon
            ("AmmoSet_127x99",                 BenchHeavy),
            ("AmmoSet_127x108",                BenchHeavy),
            ("AmmoSet_145x114",                BenchHeavy),
            ("AmmoSet_14_5x114mm",             BenchHeavy),
            ("AmmoSet_145x114mmSoviet",        BenchHeavy),
            ("AmmoSet_20mm",                   BenchHeavy),
            ("AmmoSet_20x82",                  BenchHeavy),
            ("AmmoSet_20x82mm",                BenchHeavy),
            ("AmmoSet_20x82mmMauser",          BenchHeavy),
            ("AmmoSet_20x102",                 BenchHeavy),
            ("AmmoSet_20x102mm",               BenchHeavy),
            ("AmmoSet_20x102mmNATO",           BenchHeavy),
            ("AmmoSet_20x110",                 BenchHeavy),
            ("AmmoSet_20x120",                 BenchHeavy),
            ("AmmoSet_23x115",                 BenchHeavy),
            ("AmmoSet_23x152",                 BenchHeavy),
            ("AmmoSet_25x137",                 BenchHeavy),
            ("AmmoSet_25x137mm",               BenchHeavy),
            ("AmmoSet_25x137mmNATO",           BenchHeavy),
            ("AmmoSet_30x113",                 BenchHeavy),
            ("AmmoSet_30x165",                 BenchHeavy),
            ("AmmoSet_30x173",                 BenchHeavy),
            ("AmmoSet_35x228",                 BenchHeavy),
            ("AmmoSet_40x46",                  BenchHeavy),
            ("AmmoSet_40x46mm",                BenchHeavy),
            ("AmmoSet_40x46mmGrenade",         BenchHeavy),
            ("AmmoSet_40x53",                  BenchHeavy),
            ("AmmoSet_40x53mm",                BenchHeavy),
            ("AmmoSet_40x53mmGrenade",         BenchHeavy),
            ("AmmoSet_40x311",                 BenchHeavy),
            ("AmmoSet_40x311mm",               BenchHeavy),
            ("AmmoSet_40x311mmR",              BenchHeavy),
            ("AmmoSet_50mmRocket",             BenchHeavy),
            ("AmmoSet_50x330",                 BenchHeavy),
            ("AmmoSet_57x438",                 BenchHeavy),
            ("AmmoSet_60mm",                   BenchHeavy),
            ("AmmoSet_75mm",                   BenchHeavy),
            ("AmmoSet_88mm",                   BenchHeavy),
            ("AmmoSet_100mm",                  BenchHeavy),
            ("AmmoSet_105mm",                  BenchHeavy),
            ("AmmoSet_120mm",                  BenchHeavy),
            ("AmmoSet_122mm",                  BenchHeavy),
            ("AmmoSet_125mm",                  BenchHeavy),
            ("AmmoSet_130mm",                  BenchHeavy),
            ("AmmoSet_140mm",                  BenchHeavy),
            ("AmmoSet_152mm",                  BenchHeavy),
            ("AmmoSet_155mm",                  BenchHeavy),
            ("AmmoSet_762x385",                BenchHeavy),
            // Generic categories
            ("AmmoSet_Autocannon",             BenchHeavy),
            ("AmmoSet_HeavyCannon",            BenchHeavy),
            ("AmmoSet_Cannon",                 BenchHeavy),
            ("AmmoSet_Howitzer",               BenchHeavy),
            ("AmmoSet_Mortar",                 BenchHeavy),
            ("AmmoSet_HeavyMortar",            BenchHeavy),
            ("AmmoSet_LauncherGrenade",        BenchHeavy),
            ("AmmoSet_LauncherRocket",         BenchHeavy),
            ("AmmoSet_RocketLauncher",         BenchHeavy),
            ("AmmoSet_Rocket",                 BenchHeavy),
            ("AmmoSet_Flamethrower",           BenchHeavy),
            ("AmmoSet_Plasma",                 BenchHeavy),
            ("AmmoSet_PlasmaCannon",           BenchHeavy),
            // Charged heavy-tier
            ("AmmoSet_ChargedHeavy",           BenchHeavy),
            ("AmmoSet_HeavyCharged",           BenchHeavy),
            ("AmmoSet_MechCharged",            BenchHeavy),
            // Misc Heavy (added v2 from log audit)
            ("AmmoSet_81mm",                   BenchHeavy),
            ("AmmoSet_90mm",                   BenchHeavy),
            ("AmmoSet_200Pounder",             BenchHeavy),
            ("AmmoSet_3inCannon",              BenchHeavy),
            ("AmmoSet_30x29mmGrenade",         BenchHeavy),
            ("AmmoSet_30x64mmFuel",            BenchHeavy),
            ("AmmoSet_80x256mmFuel",           BenchHeavy),
            ("AmmoSet_15x65mmDiffusingCharged",BenchHeavy),
            ("AmmoSet_66mmThermalBolt",        BenchHeavy),
            ("AmmoSet_CannonBall",             BenchHeavy),
            ("AmmoSet_Catapult_Cannon",        BenchHeavy),
            ("AmmoSet_Grapeshot",              BenchHeavy),
            ("AmmoSet_TsarGrapeshot",          BenchHeavy),
            ("AmmoSet_Incinerator",            BenchHeavy),
            ("AmmoSet_M72LAW",                 BenchHeavy),
            ("AmmoSet_RPG7",                   BenchHeavy),
            ("AmmoSet_SPG9",                   BenchHeavy),
            ("AmmoSet_SwivelGun",              BenchHeavy),
            ("AmmoSet_WarBolter",              BenchHeavy),
            ("AmmoSet_PlasmaCannon",           BenchHeavy),
            ("AmmoSet_PlasmaBoltSniper",       BenchLBW),
            // Heavy additions v3 (from runtime log diagnostic)
            ("AmmoSet_FatManShell",            BenchHeavy),
            ("AmmoSet_25x40mmGrenade",         BenchHeavy),
            ("AmmoSet_40x68mmDemo",            BenchHeavy),
            ("AmmoSet_Madsen",                 BenchHeavy),
            ("AmmoSet_RPzB",                   BenchHeavy),
            ("AmmoSet_XM109AMPR",              BenchHeavy),
        };

        // Cached reflection lookup of the CE ammoSet field on a verb.
        // Set on first hit; null until then.
        private static FieldInfo _verbAmmoSetField;
        private static bool _verbAmmoSetFieldChecked;

        // Cached reflection lookup of the CE comp ammoSet field.
        private static FieldInfo _compAmmoSetField;
        private static bool _compAmmoSetFieldChecked;

        static RNRouter()
        {
            // Defs are loaded by the time StaticConstructorOnStartup fires.
            try
            {
                Run();
            }
            catch (Exception e)
            {
                Log.Error("[HSK RN Weapon Router] Top-level exception: " + e);
            }
        }

        private static void Run()
        {
            // Verify the RN benches exist. If not, RH2 isn't loaded — bail silently.
            if (DefDatabase<ThingDef>.GetNamedSilentFail(BenchCQB) == null
                || DefDatabase<ThingDef>.GetNamedSilentFail(BenchLBW) == null
                || DefDatabase<ThingDef>.GetNamedSilentFail(BenchHeavy) == null)
            {
                Log.Message("[HSK RN Weapon Router] RN benches not found (RH2 not loaded?) — skipping.");
                return;
            }

            var benchCqb = DefDatabase<ThingDef>.GetNamed(BenchCQB);
            var benchLbw = DefDatabase<ThingDef>.GetNamed(BenchLBW);
            var benchHeavy = DefDatabase<ThingDef>.GetNamed(BenchHeavy);

            // Pre-pass: identify weapons that have a HSK-style recipe.
            // We use a set of HSK-specific component / material defNames as
            // markers. Any recipe whose ingredients reference one of these is
            // considered "HSK canonical". For weapons that have a HSK canonical
            // recipe, we skip OTHER recipes producing them (those are usually
            // auto-gen duplicates from ThingDef.recipeMaker with raw costList
            // ingredients — Steel, ComponentIndustrial, WoodLog).
            var hskMarkers = new HashSet<string>
            {
                "Pistol_Component", "SMG_Component", "Shotgun_Component",
                "Rifle_Component", "AdvRifle_Component", "AdvSniper_Component",
                "Heavy_Component", "Weapon_Parts",
                "SLDBar", "USLDBar", "USLDHBar", "HeavyBar"
            };
            var hskCanonicalRecipes = new HashSet<RecipeDef>();
            var weaponsWithHskRecipe = new HashSet<ThingDef>();
            foreach (var r in DefDatabase<RecipeDef>.AllDefs)
            {
                if (r == null || r.ingredients == null) continue;
                bool isHsk = false;
                foreach (var ing in r.ingredients)
                {
                    if (ing == null || ing.filter == null) continue;
                    // Iterate the filter's allowed thingDefs
                    foreach (var td in ing.filter.AllowedThingDefs)
                    {
                        if (td != null && hskMarkers.Contains(td.defName))
                        {
                            isHsk = true;
                            break;
                        }
                    }
                    if (!isHsk)
                    {
                        // Also check if filter allows any of the HSK
                        // ThingCategoryDefs (SLDBar etc. as categories)
                        foreach (var cat in ing.filter.AllowedThingDefs)
                        {
                            if (cat != null && cat.thingCategories != null)
                            {
                                foreach (var tc in cat.thingCategories)
                                {
                                    if (tc != null && hskMarkers.Contains(tc.defName))
                                    {
                                        isHsk = true;
                                        break;
                                    }
                                }
                                if (isHsk) break;
                            }
                        }
                    }
                    if (isHsk) break;
                }
                if (isHsk)
                {
                    hskCanonicalRecipes.Add(r);
                    if (r.products != null)
                    {
                        foreach (var p in r.products)
                        {
                            if (p.thingDef != null) weaponsWithHskRecipe.Add(p.thingDef);
                        }
                    }
                }
            }

            int routedCqb = 0;
            int routedLbw = 0;
            int routedHeavy = 0;
            int skippedNoAmmoSet = 0;
            int skippedUnknown = 0;
            int skippedAlreadyRN = 0;
            int skippedAutoGenDuplicate = 0;

            // Track unknown ammoSets and the recipes still at HSK benches
            // so we can diagnose what's getting missed.
            var unknownAmmoSets = new Dictionary<string, List<string>>();
            var stillAtHSKBench = new List<string>();
            var hskBenchNames = new HashSet<string> {
                "WeaponCraftingWorkTable","AdvWeaponCraftingWorkTable",
                "MechWeaponCraftingWorkTable","WeaponsBench",
                "HeavyArmsBench","GunsmithingBench"
            };

            // DefDatabase<RecipeDef>.AllDefs only contains concrete (non-abstract)
            // defs by this point — abstracts are filtered during def loading.
            foreach (var recipe in DefDatabase<RecipeDef>.AllDefs)
            {
                if (recipe == null) continue;
                if (recipe.products == null || recipe.products.Count == 0) continue;

                var weapon = recipe.products[0].thingDef;
                if (weapon == null) continue;
                if (weapon.IsApparel) continue;
                // Must be a weapon-class ThingDef (have verbs, or be in WeaponMelee/Ranged categories)
                if (weapon.Verbs == null || weapon.Verbs.Count == 0) continue;

                // Auto-gen duplicate check FIRST (before already-RN), because
                // some auto-gen recipes inherit their recipeMaker.recipeUsers
                // from RNMakeable* abstract bases — meaning they're already at
                // an RN bench but with vanilla costList ingredients (Plasteel,
                // raw Steel, etc.) on display. We must hide these duplicates.
                //
                // If this weapon has a HSK-canonical recipe and THIS recipe
                // isn't it, treat this recipe as a duplicate. Clear its
                // recipeUsers so it doesn't appear at any bench. The
                // HSK canonical recipe (with proper ingredients) handles it.
                if (weaponsWithHskRecipe.Contains(weapon)
                    && !hskCanonicalRecipes.Contains(recipe))
                {
                    recipe.recipeUsers = new List<ThingDef>();
                    skippedAutoGenDuplicate++;
                    continue;
                }

                // If the recipe is already pointing at an RN bench, don't touch it
                if (recipe.recipeUsers != null
                    && recipe.recipeUsers.Any(u => u != null && AlreadyRN.Contains(u.defName)))
                {
                    skippedAlreadyRN++;
                    continue;
                }

                string ammoSetName = GetAmmoSetName(weapon);
                if (string.IsNullOrEmpty(ammoSetName))
                {
                    skippedNoAmmoSet++;
                    continue;
                }

                ThingDef targetBench = null;
                foreach (var rule in Routing)
                {
                    if (ammoSetName.StartsWith(rule.Prefix, StringComparison.OrdinalIgnoreCase))
                    {
                        switch (rule.Bench)
                        {
                            case BenchCQB:   targetBench = benchCqb;   break;
                            case BenchLBW:   targetBench = benchLbw;   break;
                            case BenchHeavy: targetBench = benchHeavy; break;
                        }
                        break;
                    }
                }

                if (targetBench == null)
                {
                    skippedUnknown++;
                    if (!unknownAmmoSets.ContainsKey(ammoSetName))
                        unknownAmmoSets[ammoSetName] = new List<string>();
                    unknownAmmoSets[ammoSetName].Add(recipe.defName + " -> " + weapon.defName);
                    continue;
                }

                recipe.recipeUsers = new List<ThingDef> { targetBench };

                if (targetBench == benchCqb) routedCqb++;
                else if (targetBench == benchLbw) routedLbw++;
                else routedHeavy++;
            }

            Log.Message(string.Format(
                "[HSK RN Weapon Router] Routed {0} weapons by ammoSet — CQB:{1} LBW:{2} Heavy:{3} | skipped: already-RN:{4} no-ammoSet:{5} unknown-ammoSet:{6} autogen-dup:{7}",
                routedCqb + routedLbw + routedHeavy,
                routedCqb, routedLbw, routedHeavy,
                skippedAlreadyRN, skippedNoAmmoSet, skippedUnknown, skippedAutoGenDuplicate));

            // Dump the unknown ammoSets so we can add rules for them.
            if (unknownAmmoSets.Count > 0)
            {
                var sb = new System.Text.StringBuilder("[HSK RN Weapon Router] Unknown ammoSets (" + unknownAmmoSets.Count + "):");
                foreach (var kv in unknownAmmoSets)
                {
                    sb.Append("\n  ").Append(kv.Key).Append("  (").Append(kv.Value.Count).Append("x): ");
                    sb.Append(string.Join(", ", kv.Value.Take(3).ToArray()));
                    if (kv.Value.Count > 3) sb.Append(" ...");
                }
                Log.Message(sb.ToString());
            }

            // After routing+invalidation, scan for any FIREARM recipes still
            // claiming to live at an HSK weapon bench. These are diagnostic —
            // they tell us what slipped through the filter.
            foreach (var recipe in DefDatabase<RecipeDef>.AllDefs)
            {
                if (recipe == null || recipe.recipeUsers == null) continue;
                if (!recipe.recipeUsers.Any(u => u != null && hskBenchNames.Contains(u.defName))) continue;
                if (recipe.products == null || recipe.products.Count == 0) continue;
                var w = recipe.products[0].thingDef;
                if (w == null) continue;
                // Only care about weapons (have verbs, not apparel)
                if (w.IsApparel) continue;
                if (w.Verbs == null || w.Verbs.Count == 0) continue;
                stillAtHSKBench.Add(recipe.defName + " -> " + w.defName);
            }
            if (stillAtHSKBench.Count > 0)
            {
                var sb = new System.Text.StringBuilder("[HSK RN Weapon Router] Firearm recipes STILL at HSK bench (" + stillAtHSKBench.Count + "):");
                foreach (var s in stillAtHSKBench.Take(50)) sb.Append("\n  ").Append(s);
                if (stillAtHSKBench.Count > 50) sb.Append("\n  ... and ").Append(stillAtHSKBench.Count - 50).Append(" more");
                Log.Message(sb.ToString());
            }

            // CRITICAL: invalidate ThingDef.AllRecipes cache on every ThingDef.
            // ThingDef caches the list of recipes whose recipeUsers includes it.
            // Since we just rewrote recipeUsers on hundreds of recipes, the
            // existing cache is stale — workbenches would still show recipes
            // at their OLD bench. Null out the private cached field so the
            // next access (when player opens a bench) recomputes fresh.
            int invalidated = 0;
            var cacheField = typeof(ThingDef).GetField("allRecipesCached",
                BindingFlags.NonPublic | BindingFlags.Instance);
            if (cacheField != null)
            {
                foreach (var td in DefDatabase<ThingDef>.AllDefs)
                {
                    if (td == null) continue;
                    cacheField.SetValue(td, null);
                    invalidated++;
                }
                Log.Message("[HSK RN Weapon Router] Invalidated AllRecipes cache on " + invalidated + " ThingDefs.");
            }
            else
            {
                Log.Warning("[HSK RN Weapon Router] Could not find ThingDef.allRecipesCached field — workbenches may not pick up routing changes until reload.");
            }
        }

        // Reflectively get the CE ammoSet defName for a weapon ThingDef.
        // CE stores ammoSet on VerbPropertiesCE (field "ammoSet") and on
        // CompProperties_AmmoUser (also "ammoSet"). We don't reference the CE
        // assembly directly — read the field by name to avoid a hard dep.
        private static string GetAmmoSetName(ThingDef weapon)
        {
            // Try verb properties first
            if (weapon.Verbs != null && weapon.Verbs.Count > 0)
            {
                var verbProps = weapon.Verbs[0];
                if (verbProps != null)
                {
                    if (!_verbAmmoSetFieldChecked)
                    {
                        _verbAmmoSetField = verbProps.GetType().GetField("ammoSet",
                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        _verbAmmoSetFieldChecked = true;
                    }
                    if (_verbAmmoSetField != null)
                    {
                        var ammoSet = _verbAmmoSetField.GetValue(verbProps) as Def;
                        if (ammoSet != null) return ammoSet.defName;
                    }
                }
            }

            // Try comps
            if (weapon.comps != null)
            {
                foreach (var comp in weapon.comps)
                {
                    if (comp == null) continue;
                    if (!_compAmmoSetFieldChecked)
                    {
                        var f = comp.GetType().GetField("ammoSet",
                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        if (f != null) _compAmmoSetField = f;
                        // Don't mark checked until we actually find one — different comps
                        // have different shapes, only set the cache when we find a hit.
                        if (f != null) _compAmmoSetFieldChecked = true;
                    }
                    var fld = comp.GetType().GetField("ammoSet",
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (fld != null)
                    {
                        var ammoSet = fld.GetValue(comp) as Def;
                        if (ammoSet != null) return ammoSet.defName;
                    }
                }
            }

            return null;
        }
    }
}
