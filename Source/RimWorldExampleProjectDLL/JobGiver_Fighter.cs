using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace ArenaBell;

public class JobGiver_Fighter : JobGiver_AIFightEnemy
{
    private const float MaxAttackDistance = 900f;

    private const float WaitChance = 0.5f;

    private const int WaitTicks = 1;

    private const int MinMeleeChaseTicks = 420;

    private const int MaxMeleeChaseTicks = 900;

    protected override Job TryGiveJob(Pawn pawn)
    {
        var state = (MentalState_Fighter)pawn.MentalState;
        var pawnTarget = state.otherPawn;
        var bellRef = state.bellRef;
        var fighterTarget = bellRef.GetFighter(pawnTarget);

        if (!fighterTarget.isInFight || Rand.Value < WaitChance)
        {
            return new Job(JobDefOf.Wait_Combat)
            {
                expiryInterval = 10
            };
        }

        // Hard incapability gate
        if (pawn.Dead || pawn.Downed || pawn.WorkTagIsDisabled(WorkTags.Violent))
        {
            return null;
        }

        // Get target as Thing for verb checks
        Thing targetThing = (pawnTarget != null && pawnTarget.Spawned) ? pawnTarget : null;

        // 1) Melee (includes fists/body tools and melee weapons)
        Verb meleeVerb = null;
        bool canFightMelee = false;
        if (targetThing != null)
        {
            meleeVerb = pawn.meleeVerbs?.TryGetMeleeVerb(targetThing);
            canFightMelee = meleeVerb != null;
        }

        // 2) Any other usable attack verb for this target (incl. manual cast)
        var allowManualCastWeapons = !pawn.IsColonist;
        Verb attackVerb = null;
        bool canFightRanged = false;
        if (targetThing != null)
        {
            attackVerb = pawn.TryGetAttackVerb(targetThing, allowManualCastWeapons);
            canFightRanged = attackVerb != null;
        }

        // Check if pawn needs a weapon (doesn't have one equipped)
        // This runs regardless of whether they can fight with fists, so they'll still pick up weapons when available
        var primary = pawn.equipment?.Primary;
        if (bellRef.currentState == Building_Bell.State.fight && pawn.equipment != null && primary == null &&
            pawn.RaceProps?.Animal == false)
        {
            var suitableWeapons = new List<Thing>();
            foreach (var c in bellRef.GetComp<CompBell>().ValidCells)
            {
                var thingList = c.GetThingList(bellRef.Map);
                foreach (var thing in thingList)
                {
                    if (thing.def.IsWeapon && !suitableWeapons.Contains(thing) && thing.Spawned &&
                        pawn.CanReserve(thing))
                    {
                        suitableWeapons.Add(thing);
                    }
                }
            }

            Thing weapon = null;
            var maxDistance = 9999;
            foreach (var t2 in suitableWeapons)
            {
                if ((t2.Position - pawn.Position).LengthManhattan >= maxDistance)
                {
                    continue;
                }

                weapon = t2;
                maxDistance = (t2.Position - pawn.Position).LengthManhattan;
            }

            if (weapon != null)
            {
                return new Job(JobDefOf.Equip, weapon);
            }
        }

        // If we can't fight at all (no fists, no ranged, and no weapon available), return null
        if (!canFightMelee && !canFightRanged)
        {
            return null;
        }

        // If we have melee capability (fists), we can fight - continue to job assignment
        // If we have ranged capability, we can fight - continue to job assignment
        // Weapon pickup was attempted above if no weapon was equipped

        if (pawnTarget == null || !pawn.CanReach(pawnTarget, PathEndMode.ClosestTouch, Danger.Deadly))
        {
            return new Job(JobDefOf.Wait);
        }

        pawn.mindState.enemyTarget = pawnTarget;
        pawn.mindState.enemyTarget = pawnTarget;
        UpdateEnemyTarget(pawn);
        var enemyTarget = pawn.mindState.enemyTarget;

        if (enemyTarget == null)
        {
            return null;
        }

        // Use the verb we already checked, or get it again with the target
        var verb = attackVerb ?? pawn.TryGetAttackVerb(enemyTarget, allowManualCastWeapons);
        if (verb == null)
        {
            // Fallback: try melee verb if attack verb failed
            if (meleeVerb != null)
            {
                verb = meleeVerb;
            }
            else
            {
                return null;
            }
        }

        var isMeleeAttack = verb.verbProps.IsMeleeAttack;
        if (isMeleeAttack || pawnTarget.Downed)
        {
            var result = MeleeAttackJob(pawn, enemyTarget);
            result.killIncappedTarget = bellRef.toDeath;
            return result;
        }

        if (CoverUtility.CalculateOverallBlockChance(pawn, enemyTarget.Position, pawn.Map) >
            0.01f
            && pawn.Position.Standable(pawn.Map)
            && verb.CanHitTarget(enemyTarget) || (pawn.Position - enemyTarget.Position)
            .LengthHorizontalSquared < 25
            && verb.CanHitTarget(enemyTarget))
        {
            return new Job(JobDefOf.AttackStatic, pawnTarget)
            {
                maxNumMeleeAttacks = 1,
                expiryInterval = Rand.Range(MinMeleeChaseTicks, MaxMeleeChaseTicks),
                canBashDoors = true,
                canBashFences = true
            };
        }

        if (!TryFindShootingPosition(pawn, out var intVec))
        {
            return null;
        }

        if (intVec == pawn.Position)
        {
            return new Job(JobDefOf.AttackStatic, pawnTarget)
            {
                maxNumMeleeAttacks = 1,
                expiryInterval = Rand.Range(MinMeleeChaseTicks, MaxMeleeChaseTicks),
                canBashDoors = true,
                canBashFences = true
            };
        }

        return new Job(JobDefOf.Goto, intVec)
        {
            expiryInterval = ExpiryInterval_ShooterSucceeded.RandomInRange,
            checkOverrideOnExpire = true
        };
    }

    protected override bool TryFindShootingPosition(Pawn pawn, out IntVec3 dest, Verb verbToUse = null)
    {
        var enemyTarget = pawn.mindState.enemyTarget;
        var allowManualCastWeapons = !pawn.IsColonist;
        var attackVerb = pawn.TryGetAttackVerb(enemyTarget, allowManualCastWeapons);
        bool result;
        if (attackVerb == null)
        {
            dest = IntVec3.Invalid;
            result = false;
        }
        else
        {
            result = CastPositionFinder.TryFindCastPosition(new CastPositionRequest
            {
                caster = pawn,
                target = enemyTarget,
                verb = attackVerb,
                maxRangeFromTarget = attackVerb.verbProps.range,
                wantCoverFromTarget = attackVerb.verbProps.range > 5.0
            }, out dest);
        }

        return result;
    }

    protected override void UpdateEnemyTarget(Pawn pawn)
    {
        var state = (MentalState_Fighter)pawn.MentalState;
        var pawnTarget = state.otherPawn;
        pawn.mindState.enemyTarget = pawnTarget;
        Find.TickManager.slower.SignalForceNormalSpeed();
    }
}