using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace Vending
{
    public class JobDriver_TradeWith : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(base.TargetThingA, job, 1, -1, null, errorOnFailed);
        }
        public override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(TargetIndex.A);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);
            Toil trade = new Toil();
            trade.initAction = delegate
            {
                Pawn actor = trade.actor;
                var comp = base.TargetThingA.TryGetComp<CompVendingMachine>();
                Find.WindowStack.Add(new Dialog_Trade(actor, comp.trader));
            };
            yield return trade;
        }
    }
}
