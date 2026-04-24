using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace Vending
{
    public class PerpetualTrader : IExposable, ITrader, IThingHolder
    {
        public TraderKindDef traderKindDef;

        private ThingOwner things;

        private List<Pawn> soldPrisoners = new List<Pawn>();

        private int randomPriceFactorSeed = -1;
        public int Silver => CountHeldOf(ThingDefOf.Silver);
        public TradeCurrency TradeCurrency => TraderKind.tradeCurrency;
        public IThingHolder ParentHolder => null;
        public TraderKindDef TraderKind => traderKindDef;
        public int RandomPriceFactorSeed => randomPriceFactorSeed;
        public float TradePriceImprovementOffsetForPlayer => 0f;
        public IEnumerable<Thing> Goods
        {
            get
            {
                for (int i = 0; i < things.Count; i++)
                {
                    yield return things[i];
                }
            }
        }
        public string TraderName => traderKindDef.LabelCap;
        public bool CanTradeNow => true;
        public Faction Faction => null;

        public ThingWithComps vendingMachine;
        public CompVendingMachine CompVendingMachine => vendingMachine.TryGetComp<CompVendingMachine>();
        public PerpetualTrader()
        {
            things = new ThingOwner<Thing>(this);
            randomPriceFactorSeed = Rand.RangeInclusive(1, 10000000);
        }
        public PerpetualTrader(ThingWithComps vendingMachine, TraderKindDef traderKindDef)
        {
            things = new ThingOwner<Thing>(this);
            randomPriceFactorSeed = Rand.RangeInclusive(1, 10000000);
            this.vendingMachine = vendingMachine;
            this.traderKindDef = traderKindDef;
        }
        public void Restock()
        {
            things.ClearAndDestroyContents();
            GenerateThings();
        }
        public IEnumerable<Thing> ColonyThingsWillingToBuy(Pawn playerNegotiator)
        {
            IEnumerable<Thing> enumerable = playerNegotiator.Map.listerThings.AllThings.Where((Thing x) => x.def.category == ThingCategory.Item &&
            TradeUtility.PlayerSellableNow(x, playerNegotiator) && !x.Position.Fogged(x.Map) && (playerNegotiator.Map.areaManager.Home[x.Position] || x.IsInAnyStorage())
            && ReachableForTrade(playerNegotiator, x));
            foreach (Thing item in enumerable)
            {
                yield return item;
            }
            if (playerNegotiator.GetLord() == null)
            {
                yield break;
            }
            foreach (Pawn item2 in TradeUtility.AllSellableColonyPawns(playerNegotiator.Map).Where(x => !x.Downed))
            {
                yield return item2;
            }
        }

        private bool ReachableForTrade(Pawn playerNegotiator, Thing thing)
        {
            if (playerNegotiator.Map != thing.Map)
            {
                return false;
            }
            return playerNegotiator.Map.reachability.CanReach(playerNegotiator.Position, thing, PathEndMode.Touch, TraverseMode.PassDoors, Danger.Some);
        }

        public IEnumerable<Pawn> AllSellableColonyPawns(Pawn negotiator)
        {
            foreach (Pawn item in negotiator.Map.mapPawns.PrisonersOfColonySpawned)
            {
                if (item.guest.PrisonerIsSecure)
                {
                    item.guest.joinStatus = JoinStatus.JoinAsColonist;
                    yield return item;
                }
            }

            foreach (Pawn item2 in negotiator.Map.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer))
            {
                if (negotiator != item2 && (item2.IsColonistPlayerControlled && item2.HostFaction == null && !item2.InMentalState && !item2.Downed || item2.IsSlave))
                {
                    item2.guest.joinStatus = JoinStatus.JoinAsColonist;
                    yield return item2;
                }
            }
        }

        public void GenerateThings()
        {
            ThingSetMakerParams parms = default(ThingSetMakerParams);
            parms.traderDef = TraderKind;
            parms.tile = vendingMachine.Map.Tile;
            var generatedThings = ThingSetMakerDefOf.TraderStock.root.Generate(parms);
            things.TryAddRangeOrTransfer(generatedThings);
        }

        public void TraderTick()
        {
            for (int num = things.Count - 1; num >= 0; num--)
            {
                Pawn pawn = things[num] as Pawn;
                if (pawn != null)
                {
                    pawn.Tick();
                    if (pawn.Dead)
                    {
                        things.Remove(pawn);
                    }
                }
            }
        }

        public void ExposeData()
        {
            Scribe_Deep.Look(ref things, "things", this);
            Scribe_Collections.Look(ref soldPrisoners, "soldPrisoners", LookMode.Reference);
            Scribe_Values.Look(ref randomPriceFactorSeed, "randomPriceFactorSeed", 0);
            Scribe_References.Look(ref vendingMachine, "tradingPost");
            Scribe_Defs.Look(ref traderKindDef, "traderKindDef");
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                soldPrisoners.RemoveAll((Pawn x) => x == null);
            }
        }
        public int CountHeldOf(ThingDef thingDef, ThingDef stuffDef = null)
        {
            return HeldThingMatching(thingDef, stuffDef)?.stackCount ?? 0;
        }

        public void GiveSoldThingToTrader(Thing toGive, int countToGive, Pawn playerNegotiator)
        {
            Thing thing = toGive.SplitOff(countToGive);
            thing.PreTraded(TradeAction.None, playerNegotiator, this);
            Thing thing2 = TradeUtility.ThingFromStockToMergeWith(this, thing);
            if (thing2 != null)
            {
                if (!thing2.TryAbsorbStack(thing, respectStackLimit: false))
                {
                    thing.Destroy();
                }
                return;
            }
            Pawn pawn = thing as Pawn;
            if (pawn != null && pawn.RaceProps.Humanlike)
            {
                soldPrisoners.Add(pawn);
            }
            things.TryAddOrTransfer(thing, canMergeWithExistingStacks: false);
        }
        public void GiveSoldThingToPlayer(Thing toGive, int countToGive, Pawn playerNegotiator)
        {
            Thing thing = toGive.SplitOff(countToGive);
            thing.PreTraded(TradeAction.PlayerBuys, playerNegotiator, this);
            Pawn pawn = thing as Pawn;
            if (pawn != null)
            {
                soldPrisoners.Remove(pawn);
            }
            var cell = DropCellFinder.TradeDropSpot(playerNegotiator.Map);
            TradeUtility.SpawnDropPod(cell, playerNegotiator.Map, thing);
        }

        private Thing HeldThingMatching(ThingDef thingDef, ThingDef stuffDef)
        {
            for (int i = 0; i < things.Count; i++)
            {
                if (things[i].def == thingDef && things[i].Stuff == stuffDef)
                {
                    return things[i];
                }
            }
            return null;
        }

        public void ChangeCountHeldOf(ThingDef thingDef, ThingDef stuffDef, int count)
        {
            Thing thing = HeldThingMatching(thingDef, stuffDef);
            if (thing == null)
            {
                Log.Error("Changing count of thing trader doesn't have: " + thingDef);
            }
            thing.stackCount += count;
        }

        public ThingOwner GetDirectlyHeldThings()
        {
            return things;
        }

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
        }
    }
}
