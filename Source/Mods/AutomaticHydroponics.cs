using Multiplayer.Compat;
using Verse;

namespace MultiplayerAutomaticHydroponicsPatch.Source.Mods;

/// <summary>
///     Multiplayer Patch for Automatic Hydroponics by Poncho,
///     Last Update: 20 Jul @ 5:17am 2026
///     <see href="https://steamcommunity.com/sharedfiles/filedetails/?id=3528490718" />
///     The mod itself is XML defs (AutoHydroponic, SmallAutoHydroponic) using
///     PipeSystem.CompAdvancedResourceProcessor plus a single visual Harmony patch
///     (AutomaticHydroponics.CompAdvancedResourceProcessor_Patch.PostDraw_Prefix).
///     The visual patch is render-only (graphicCache, GraphicDatabase, DrawFromDef),
///     uses no RNG and touches no simulation state, so it needs no sync.
///     All processor and pipe net sync is covered by Multiplayer Vanilla Expanded
///     Framework Patch, which is a required dependency (see About.xml).
/// </summary>
[MpCompatFor("Poncho.AutomaticHydroponics")]
public class AutomaticHydroponics
{
    private const string LogPrefix = "[Multiplayer Automatic Hydroponics Patch]";

    public AutomaticHydroponics(ModContentPack content)
    {
        Log.Message($"{LogPrefix} Initialized. Processor and pipe sync handled by Multiplayer Vanilla Expanded Framework Patch.");
    }
}
