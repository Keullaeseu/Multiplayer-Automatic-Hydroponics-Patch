using System.Reflection;
using HarmonyLib;
using Multiplayer.API;
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
///     All desync risk comes from PipeSystem processor UI (process selection,
///     reorder, suspend, target counts, paste/copy, overclock) and pipe nets,
///     which we sync here. Safe to run alongside Multiplayer Compatibility's
///     Vanilla Expanded Framework patch - registrations are guarded.
/// </summary>
[MpCompatFor("Poncho.AutomaticHydroponics")]
public partial class AutomaticHydroponics
{
    private const string LogPrefix = "[Multiplayer Automatic Hydroponics Patch]";

    public AutomaticHydroponics(ModContentPack content)
    {
        LongEventHandler.ExecuteWhenFinished(LatePatch);
    }

    private static void LatePatch()
    {
        try
        {
            Log.Message($"{LogPrefix} Initializing...");
            PatchProcessor();
            PatchPipeSystem();
            Log.Message($"{LogPrefix} Initialized.");
        }
        catch (Exception exception)
        {
            Log.Error($"{LogPrefix} LatePatch failed: {exception}");
        }
    }

    #region Safe registration helpers

    private static Type SafeType(string typeName)
    {
        try
        {
            var type = AccessTools.TypeByName(typeName);
            if (type == null) Log.Warning($"{LogPrefix} Type not found (skipped): {typeName}");

            return type;
        }
        catch (Exception exception)
        {
            Log.Warning($"{LogPrefix} SafeType failed for {typeName}: {exception.Message}");
            return null;
        }
    }

    private static void SafeSyncMethod(Type type, string methodName, Type[] argTypes = null)
    {
        try
        {
            if (type == null) return;

            var method = argTypes == null
                ? AccessTools.DeclaredMethod(type, methodName)
                : AccessTools.DeclaredMethod(type, methodName, argTypes);
            if (method == null)
            {
                Log.Warning($"{LogPrefix} Method not found (skipped): {type.FullName}:{methodName}");
                return;
            }

            try
            {
                MP.RegisterSyncMethod(method);
            }
            catch (Exception exception)
            {
                // Already synced (e.g. by Multiplayer Compatibility) - not fatal, just note it.
                Log.Warning(
                    $"{LogPrefix} RegisterSyncMethod skipped for {type.FullName}:{methodName}: {exception.Message}");
            }
        }
        catch (Exception exception)
        {
            Log.Warning($"{LogPrefix} SafeSyncMethod failed for {type?.FullName}:{methodName}: {exception.Message}");
        }
    }

    private static void SafeSyncMethod(string typeName, string methodName)
    {
        SafeSyncMethod(SafeType(typeName), methodName);
    }

    private static void SafeLambdaMethod(string typeName, string parentMethod, MethodType methodType,
        params int[] ordinals)
    {
        try
        {
            var type = SafeType(typeName);
            if (type == null) return;

            try
            {
                MpCompat.RegisterLambdaMethod(type, parentMethod, methodType, ordinals);
            }
            catch (Exception exception)
            {
                Log.Warning(
                    $"{LogPrefix} RegisterLambdaMethod skipped for {typeName}:{parentMethod} [{string.Join(",", ordinals)}]: {exception.Message}");
            }
        }
        catch (Exception exception)
        {
            Log.Warning(
                $"{LogPrefix} SafeLambdaMethod failed for {typeName}:{parentMethod} [{string.Join(",", ordinals)}]: {exception.Message}");
        }
    }

    private static void SafeLambdaMethod(string typeName, string parentMethod, params int[] ordinals)
    {
        SafeLambdaMethod(typeName, parentMethod, MethodType.Normal, ordinals);
    }

    private static void SafeLambdaDelegate(string typeName, string parentMethod, MethodType methodType,
        params int[] ordinals)
    {
        try
        {
            var type = SafeType(typeName);
            if (type == null) return;

            try
            {
                MpCompat.RegisterLambdaDelegate(type, parentMethod, methodType, ordinals);
            }
            catch (Exception exception)
            {
                Log.Warning(
                    $"{LogPrefix} RegisterLambdaDelegate skipped for {typeName}:{parentMethod} [{string.Join(",", ordinals)}]: {exception.Message}");
            }
        }
        catch (Exception exception)
        {
            Log.Warning(
                $"{LogPrefix} SafeLambdaDelegate failed for {typeName}:{parentMethod} [{string.Join(",", ordinals)}]: {exception.Message}");
        }
    }

    private static void SafeLambdaDelegate(string typeName, string parentMethod, params int[] ordinals)
    {
        SafeLambdaDelegate(typeName, parentMethod, MethodType.Normal, ordinals);
    }

    private static ISyncField SafeSyncField(string typeName, string fieldName)
    {
        try
        {
            var type = SafeType(typeName);
            if (type == null) return null;

            try
            {
                return MP.RegisterSyncField(type, fieldName);
            }
            catch (Exception exception)
            {
                Log.Warning($"{LogPrefix} RegisterSyncField skipped for {typeName}:{fieldName}: {exception.Message}");
                return null;
            }
        }
        catch (Exception exception)
        {
            Log.Warning($"{LogPrefix} SafeSyncField failed for {typeName}:{fieldName}: {exception.Message}");
            return null;
        }
    }

    private static void SafePatch(MethodBase target, HarmonyMethod prefix = null, HarmonyMethod postfix = null,
        HarmonyMethod transpiler = null)
    {
        try
        {
            if (target == null) return;

            MpCompat.harmony.Patch(target, prefix, postfix, transpiler);
        }
        catch (Exception exception)
        {
            Log.Warning(
                $"{LogPrefix} SafePatch failed for {target?.DeclaringType?.FullName}:{target?.Name}: {exception.Message}");
        }
    }

    private static bool IsMpCompatProcessorSyncActive()
    {
        try
        {
            var vefCompatType = AccessTools.TypeByName("Multiplayer.Compat.VanillaExpandedFramework");
            if (vefCompatType != null) return true;

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var assemblyName = assembly.GetName().Name;
                if (assemblyName == "Multiplayer_Compat" || assemblyName == "MultiplayerCompat") return true;
            }
        }
        catch (Exception exception)
        {
            Log.Warning($"{LogPrefix} Failed to check for MP Compat presence: {exception.Message}");
        }

        return false;
    }

    #endregion
}