using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace ArenaBell;

[StaticConstructorOnStartup]
public class CompBell : ThingComp
{
    public const float circleAddition = 1.3f;
    // Instance-specific list to avoid race conditions
    private readonly List<IntVec3> validCells = new List<IntVec3>();
    public int audience = 1;

    public float radius = 9.9f;

    public bool useCircle;

    // Cached overlay data to avoid recalculating every frame
    private List<IntVec3> cachedOuterCells;
    private List<IntVec3> cachedInnerCells;
    private float cachedRadius = -1f;
    private int cachedAudience = -1;
    private bool cachedUseCircle;
    private IntVec3 cachedPosition = IntVec3.Invalid;

    public IEnumerable<IntVec3> ValidCells
    {
        get
        {
            if (useCircle)
            {
                validCells.Clear();
                var region = parent.Position.GetRegion(parent.Map);
                if (region == null)
                {
                    return validCells;
                }

                RegionTraverser.BreadthFirstTraverse(region, (_, r) => r.door == null, delegate(Region r)
                {
                    foreach (var item in r.Cells)
                    {
                        if (item.InHorDistOf(parent.Position, radius + circleAddition - audience))
                        {
                            validCells.Add(item);
                        }
                    }

                    return false;
                }, 13);

                return validCells;
            }

            var cellRect = CellRect.CenteredOn(parent.Position, 1).ExpandedBy(Mathf.RoundToInt(radius));
            return validCellsAround(parent.Position, parent.Map, cellRect.ContractedBy(audience));
        }
    }

    private void decreaseRad()
    {
        radius = Mathf.Max(1f, radius - 1f);
        if (audience > radius - 1)
        {
            audience = (int)radius - 1;
        }

        audience = Mathf.Max(1, audience);
        InvalidateCache();
    }

    private void increaseRad()
    {
        radius = Mathf.Min(25f, radius + 1f);
        audience = Mathf.Max(1, audience);
        InvalidateCache();
    }

    private void decreaseAudience()
    {
        audience = Mathf.Max(1, audience - 1);
        InvalidateCache();
    }

    private void increaseAudience()
    {
        audience = Mathf.Min((int)radius - 1, audience + 1);
        InvalidateCache();
    }

    private void InvalidateCache()
    {
        cachedOuterCells = null;
        cachedInnerCells = null;
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref radius, "radius", 9.9f);
        Scribe_Values.Look(ref audience, "audience", 1);
        Scribe_Values.Look(ref useCircle, "useCircle");
    }

    public override void PostDrawExtraSelectionOverlays()
    {
        base.PostDrawExtraSelectionOverlays();

        var region = parent.Position.GetRegion(parent.Map);
        if (region == null)
        {
            return;
        }

        // Check if we need to recalculate cached overlay data
        var needsRecalc = cachedOuterCells == null || 
                          cachedInnerCells == null ||
                          cachedRadius != radius ||
                          cachedAudience != audience ||
                          cachedUseCircle != useCircle ||
                          cachedPosition != parent.Position;

        if (needsRecalc)
        {
            RecalculateOverlayCells(region);
            cachedRadius = radius;
            cachedAudience = audience;
            cachedUseCircle = useCircle;
            cachedPosition = parent.Position;
        }

        GenDraw.DrawFieldEdges(cachedOuterCells, Color.gray);
        GenDraw.DrawFieldEdges(cachedInnerCells);
    }

    private void RecalculateOverlayCells(Region region)
    {
        cachedOuterCells = new List<IntVec3>();
        cachedInnerCells = new List<IntVec3>();

        if (useCircle)
        {
            // For circle mode, use distance checks directly
            RegionTraverser.BreadthFirstTraverse(region, (_, r) => r.door == null, delegate(Region r)
            {
                foreach (var item in r.Cells)
                {
                    if (item.InHorDistOf(parent.Position, radius + circleAddition))
                    {
                        cachedOuterCells.Add(item);
                    }

                    if (item.InHorDistOf(parent.Position, radius + circleAddition - audience))
                    {
                        cachedInnerCells.Add(item);
                    }
                }

                return false;
            }, 13);
        }
        else
        {
            // For square mode, use HashSet for O(1) lookups instead of O(n) Contains()
            var outerRect = CellRect.CenteredOn(parent.Position, 1).ExpandedBy(Mathf.RoundToInt(radius));
            var innerRect = outerRect.ContractedBy(audience);
            var outerHashSet = new HashSet<IntVec3>(outerRect.Cells);
            var innerHashSet = new HashSet<IntVec3>(innerRect.Cells);

            RegionTraverser.BreadthFirstTraverse(region, (_, r) => r.door == null, delegate(Region r)
            {
                foreach (var item in r.Cells)
                {
                    if (outerHashSet.Contains(item))
                    {
                        cachedOuterCells.Add(item);
                    }

                    if (innerHashSet.Contains(item))
                    {
                        cachedInnerCells.Add(item);
                    }
                }

                return false;
            }, 13);
        }
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (var baseGizmo in base.CompGetGizmosExtra())
        {
            yield return baseGizmo;
        }

        yield return new Command_Action
        {
            action = increaseRad,
            defaultLabel = "PA.IncreaseRadius".Translate(),
            defaultDesc = "PA.IncreaseRadiusTT".Translate(),
            hotKey = KeyBindingDefOf.Misc5,
            icon = ContentFinder<Texture2D>.Get("UI/Commands/ExpandRadius")
        };

        yield return new Command_Action
        {
            action = decreaseRad,
            defaultLabel = "PA.DecreaseRadius".Translate(),
            defaultDesc = "PA.DecreaseRadiusTT".Translate(),
            hotKey = KeyBindingDefOf.Misc6,
            icon = ContentFinder<Texture2D>.Get("UI/Commands/ShrinkRadius"),
            Disabled = radius <= 1f
        };

        yield return new Command_Action
        {
            action = decreaseAudience,
            defaultLabel = "PA.DecreaseAudienceBuffer".Translate(),
            defaultDesc = "PA.DecreaseAudienceBufferTT".Translate(),
            icon = ContentFinder<Texture2D>.Get("UI/Commands/DecreaseAudienceBuffer"),
            Disabled = radius <= 1f || audience <= 1
        };

        yield return new Command_Action
        {
            action = increaseAudience,
            defaultLabel = "PA.IncreaseAudienceBuffer".Translate(),
            defaultDesc = "PA.IncreaseAudienceBufferTT".Translate(),
            icon = ContentFinder<Texture2D>.Get("UI/Commands/IncreaseAudienceBuffer"),
            Disabled = radius <= 1f
        };

        if (!useCircle)
        {
            yield return new Command_Action
            {
                action = () => { useCircle = !useCircle; InvalidateCache(); },
                defaultLabel = "PA.SwitchToCircle".Translate(),
                defaultDesc = "PA.SwitchToCircleTT".Translate(),
                hotKey = KeyBindingDefOf.Misc7,
                icon = ContentFinder<Texture2D>.Get("UI/Commands/UseCircle")
            };
        }
        else
        {
            yield return new Command_Action
            {
                action = () => { useCircle = !useCircle; InvalidateCache(); },
                defaultLabel = "PA.SwitchToSquare".Translate(),
                defaultDesc = "PA.SwitchToSquareTT".Translate(),
                hotKey = KeyBindingDefOf.Misc7,
                icon = ContentFinder<Texture2D>.Get("UI/Commands/UseSquare")
            };
        }
    }

    private List<IntVec3> validCellsAround(IntVec3 pos, Map map, CellRect rect)
    {
        validCells.Clear();
        if (!pos.InBounds(map))
        {
            return validCells;
        }

        var region = pos.GetRegion(map);
        if (region != null)
        {
            RegionTraverser.BreadthFirstTraverse(region, (_, r) => r.door == null, delegate(Region r)
            {
                foreach (var item in r.Cells)
                {
                    if (inDistOfRect(item, rect))
                    {
                        validCells.Add(item);
                    }
                }

                return false;
            }, 13);
        }

        return validCells;
    }

    private static bool inDistOfRect(IntVec3 pos, CellRect rect)
    {
        var num = (float)pos.x;
        var num2 = (float)pos.z;
        return num <= rect.maxX && num >= rect.minX && num2 <= rect.maxZ && num2 >= rect.minZ;
    }
}