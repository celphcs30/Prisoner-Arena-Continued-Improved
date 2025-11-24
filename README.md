# Prisoner Arena (Continued) Improved

RimWorld mod that allows you to arrange fights between prisoners, with spectators gaining joy from watching the matches.

## Original Mod

This is a continuation of **Prisoner Arena (Continued)** by Mlie, which was itself an update of the original mod by Gl0b.

- **Original Steam Workshop**: https://steamcommunity.com/sharedfiles/filedetails/?id=1668624708
- **Continued by Mlie**: https://steamcommunity.com/sharedfiles/filedetails/?id=2022581505
- **Original GitHub**: https://github.com/emipa606/PrisonerArena

## Performance Fixes

This version includes critical performance fixes that resolve game lockup issues when selecting Arena Spot buildings:

### Issues Fixed

1. **Static Shared List Race Condition**
   - **Problem**: A static `List<IntVec3>` was shared across all Arena Spot instances, causing race conditions and potential data corruption when multiple spots were used simultaneously.
   - **Solution**: Converted to instance-specific lists to ensure thread safety and prevent data corruption.

2. **Expensive Per-Frame Calculations**
   - **Problem**: `PostDrawExtraSelectionOverlays()` recalculated overlay cell positions every single frame, performing expensive region traversal and array operations.
   - **Solution**: Implemented caching system that only recalculates overlay data when radius, audience, or shape mode changes.

3. **Inefficient Contains() Operations**
   - **Problem**: Used `Array.Contains()` (O(n) complexity) in nested loops during region traversal, causing severe performance degradation on large maps.
   - **Solution**: Replaced with `HashSet` lookups (O(1) complexity) for square mode cell validation.

4. **Unnecessary Array Allocations**
   - **Problem**: Created large arrays from `CellRect.Cells` enumerables every frame, causing memory pressure and GC spikes.
   - **Solution**: Eliminated per-frame allocations by caching results and using HashSet for efficient lookups.

### Performance Impact

- **Before**: Complete game lockup when selecting Arena Spot buildings, especially on larger maps
- **After**: Smooth performance with overlay calculations only occurring when settings change

## Features

- Arrange fights between prisoners, slaves, animals, mechs, and mutants
- Fights can go until death or until one fighter is downed
- Scoreboard tracks all winners
- Winners can be rewarded with freedom or glory
- Spectators gain joy from watching matches
- Configurable arena radius and spectator buffer zones
- Support for both square and circular arena shapes

## Installation

Place the `PrisonerArena` folder in your RimWorld `Mods` directory.

## Compatibility

- RimWorld 1.0 - 1.6
- Compatible with most mods
- Safe to add to existing colonies

## Credits

- **Original Mod**: Gl0b
- **Continued by**: Mlie (emipa606)
- **Performance Fixes**: celphcs30

## License

MIT License - See LICENSE.md for details
