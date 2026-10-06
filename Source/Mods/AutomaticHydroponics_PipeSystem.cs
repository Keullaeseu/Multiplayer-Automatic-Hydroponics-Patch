using System.Collections;
using HarmonyLib;
using Multiplayer.API;
using Multiplayer.Compat;
using RimWorld;
using Verse;

namespace MultiplayerAutomaticHydroponicsPatch.Source.Mods;

/// <summary>
///     PipeSystem support for Automatic Hydroponics buildings.
///     Adapted from Multiplayer Compatibility's Vanilla Expanded Framework
///     PatchPipeSystem. Covers CompResourceStorage / CompResourceProcessor gizmos
///     used alongside the hydroponics, plus pipe net and deconstruct designator
///     sync workers needed for networked outputs (nutrient paste, chemfuel, oxygen,
///     neutroamine). Automatic Hydroponics defs reference VNPE_NutrientPasteNet,
///     VCHE_ChemfuelNet, VGE_OxygenNet and VREA_NeutroamineNet.
/// </summary>
public partial class AutomaticHydroponics
{
    private static Type deconstructPipeDesignatorType;
    private static AccessTools.FieldRef<Designator_Deconstruct, Def> deconstructPipeDesignatorNetDefField;

    private static Type pipeNetManagerType;
    private static AccessTools.FieldRef<MapComponent, IList> pipeNetManagerPipeNetsListField;

    private static AccessTools.FieldRef<object, Map> pipeNetMapField;
    private static AccessTools.FieldRef<object, Def> pipeNetDefField;

    private static void PatchPipeSystem()
    {
        try
        {
            if (IsMpCompatProcessorSyncActive())
            {
                Log.Message(
                    $"{LogPrefix} MP Compat VEF patch detected, skipping duplicate pipe sync (handled by Multiplayer Compatibility).");
                return;
            }

            SafeLambdaMethod("PipeSystem.CompConvertToThing", "PostSpawnSetup", 0, 1, 2, 3);

            try
            {
                foreach (var syncMethod in MpCompat.RegisterLambdaMethod("PipeSystem.CompExplosiveContent",
                             "CompGetGizmosExtra", 0))
                    syncMethod.SetDebugOnly();
            }
            catch (Exception exception)
            {
                Log.Warning($"{LogPrefix} Failed to register CompExplosiveContent debug gizmo: {exception.Message}");
            }

            SafeLambdaMethod("PipeSystem.CompResourceProcessor", "PostSpawnSetup", 1);
            SafeLambdaMethod("PipeSystem.CompResourceStorage", "PostSpawnSetup", 0, 2, 3);

            try
            {
                foreach (var syncMethod in MpCompat.RegisterLambdaMethod("PipeSystem.CompResourceStorage",
                             "CompGetGizmosExtra", 0, 1, 2)) syncMethod.SetDebugOnly();
            }
            catch (Exception exception)
            {
                Log.Warning($"{LogPrefix} Failed to register CompResourceStorage debug gizmos: {exception.Message}");
            }

            try
            {
                foreach (var syncMethod in MpCompat.RegisterLambdaMethod("PipeSystem.CompSpawnerOrNet",
                             "CompGetGizmosExtra", 0)) syncMethod.SetDebugOnly();
            }
            catch (Exception exception)
            {
                Log.Warning($"{LogPrefix} Failed to register CompSpawnerOrNet debug gizmo: {exception.Message}");
            }

            PatchPipeDesignator();
            PatchPipeNet();
        }
        catch (Exception exception)
        {
            Log.Warning($"{LogPrefix} PatchPipeSystem failed: {exception.Message}");
        }
    }

    private static void PatchPipeDesignator()
    {
        try
        {
            var type = deconstructPipeDesignatorType = SafeType("PipeSystem.Designator_DeconstructPipe");
            if (type == null) return;

            try
            {
                deconstructPipeDesignatorNetDefField = AccessTools.FieldRefAccess<Def>(type, "pipeNetDef");
            }
            catch (Exception exception)
            {
                Log.Warning($"{LogPrefix} Failed to bind Designator_DeconstructPipe.pipeNetDef: {exception.Message}");
                return;
            }

            try
            {
                MP.RegisterSyncWorker<Designator_Deconstruct>(SyncDeconstructPipeDesignator, type);
            }
            catch (Exception exception)
            {
                Log.Warning(
                    $"{LogPrefix} RegisterSyncWorker skipped for Designator_DeconstructPipe: {exception.Message}");
            }
        }
        catch (Exception exception)
        {
            Log.Warning($"{LogPrefix} PatchPipeDesignator failed: {exception.Message}");
        }
    }

    private static void PatchPipeNet()
    {
        try
        {
            var managerType = pipeNetManagerType = SafeType("PipeSystem.PipeNetManager");
            if (managerType != null)
                try
                {
                    pipeNetManagerPipeNetsListField = AccessTools.FieldRefAccess<IList>(managerType, "pipeNets");
                }
                catch (Exception exception)
                {
                    Log.Warning($"{LogPrefix} Failed to bind PipeNetManager.pipeNets: {exception.Message}");
                    return;
                }
            else
                return;

            var pipeNetType = SafeType("PipeSystem.PipeNet");
            if (pipeNetType == null) return;

            try
            {
                pipeNetMapField = AccessTools.FieldRefAccess<Map>(pipeNetType, "map");
                pipeNetDefField = AccessTools.FieldRefAccess<Def>(pipeNetType, "def");
            }
            catch (Exception exception)
            {
                Log.Warning($"{LogPrefix} Failed to bind PipeSystem.PipeNet fields: {exception.Message}");
                return;
            }

            try
            {
                MP.RegisterSyncWorker<object>(SyncPipeNet, pipeNetType, true);
            }
            catch (Exception exception)
            {
                Log.Warning($"{LogPrefix} RegisterSyncWorker skipped for PipeNet: {exception.Message}");
            }
        }
        catch (Exception exception)
        {
            Log.Warning($"{LogPrefix} PatchPipeNet failed: {exception.Message}");
        }
    }

    private static void SyncDeconstructPipeDesignator(SyncWorker sync, ref Designator_Deconstruct designator)
    {
        if (sync.isWriting)
            sync.Write(deconstructPipeDesignatorNetDefField(designator));
        else
            designator =
                (Designator_Deconstruct)Activator.CreateInstance(deconstructPipeDesignatorType, sync.Read<Def>());
    }

    private static void SyncPipeNet(SyncWorker sync, ref object pipeNet)
    {
        if (sync.isWriting)
        {
            if (pipeNet == null)
            {
                sync.Write(-1);
                return;
            }

            var map = pipeNetMapField(pipeNet);
            if (map == null)
            {
                Log.Error($"{LogPrefix} Trying to sync a PipeNet with a null map. PipeNet={pipeNet}");
                sync.Write(-1);
                return;
            }

            var manager = map.GetComponent(pipeNetManagerType);
            if (manager == null)
            {
                Log.Error(
                    $"{LogPrefix} Trying to sync a PipeNet with a map that doesn't have PipeNetManager. PipeNet={pipeNet}, Map={map}");
                sync.Write(-1);
                return;
            }

            var def = pipeNetDefField(pipeNet);
            var list = pipeNetManagerPipeNetsListField(manager);
            var index = -1;
            var found = false;

            foreach (var currentPipeNet in list)
                if (def == pipeNetDefField(currentPipeNet))
                {
                    index++;
                    if (pipeNet == currentPipeNet)
                    {
                        found = true;
                        break;
                    }
                }

            if (!found)
            {
                Log.Error(
                    $"{LogPrefix} Trying to sync a PipeNet, but it's not held by the manager. PipeNet={pipeNet}, map={map}, manager={manager}");
                sync.Write(-1);
            }
            else
            {
                sync.Write(index);
                sync.Write(def);
                sync.Write(manager);
            }
        }
        else
        {
            var index = sync.Read<int>();
            if (index < 0) return;

            var def = sync.Read<Def>();
            var manager = sync.Read<MapComponent>();
            var list = pipeNetManagerPipeNetsListField(manager);
            var currentIndex = 0;

            foreach (var currentPipeNet in list)
                if (def == pipeNetDefField(currentPipeNet))
                {
                    if (currentIndex == index)
                    {
                        pipeNet = currentPipeNet;
                        break;
                    }

                    currentIndex++;
                }
        }
    }
}