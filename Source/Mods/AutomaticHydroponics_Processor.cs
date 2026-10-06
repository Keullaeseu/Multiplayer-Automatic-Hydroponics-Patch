using System.Collections;
using System.Reflection;
using HarmonyLib;
using Multiplayer.API;
using Multiplayer.Compat;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace MultiplayerAutomaticHydroponicsPatch.Source.Mods;

/// <summary>
///     Processor sync for Automatic Hydroponics buildings.
///     Adapted from Multiplayer Compatibility's Vanilla Expanded Framework
///     PatchAdvancedResourceProcessor (PipeSystem.CompAdvancedResourceProcessor,
///     PipeSystem.Process, PipeSystem.ProcessStack, PipeSystem.ITab_Processor,
///     PipeSystem.Window_Overclock). Needed because AutoHydroponic and
///     SmallAutoHydroponic are pure PipeSystem processors.
/// </summary>
public partial class AutomaticHydroponics
{
    private static void PatchProcessor()
    {
        try
        {
            if (IsMpCompatProcessorSyncActive())
            {
                Log.Message(
                    $"{LogPrefix} MP Compat VEF patch detected, applying logic fixes only (UI handled by Multiplayer Compatibility).");
                SafeLambdaMethod("PipeSystem.Process", "Options", MethodType.Getter, 2);
                BindProcessorFieldsForPasteFix();
                PatchMpCompatPasteFix();
                return;
            }

            var type = advancedResourceProcessorType = SafeType("PipeSystem.CompAdvancedResourceProcessor");
            if (type == null)
            {
                Log.Warning($"{LogPrefix} PipeSystem.CompAdvancedResourceProcessor not found, processor sync skipped.");
                return;
            }

            try
            {
                advancedResourceProcessorProcessStackField = AccessTools.FieldRefAccess<object>(type, "processStack");
            }
            catch (Exception exception)
            {
                Log.Warning($"{LogPrefix} Failed to bind processStack field: {exception.Message}");
                return;
            }

            SafeLambdaDelegate("PipeSystem.CompAdvancedResourceProcessor", "ProcessesOptions", MethodType.Getter, 2);
            SafeLambdaMethod("PipeSystem.CompAdvancedResourceProcessor", "Settings", MethodType.Getter, 0);
            SafeLambdaMethod("PipeSystem.CompAdvancedResourceProcessor", nameof(ThingComp.CompGetGizmosExtra), 0);

            try
            {
                foreach (var syncDelegate in MpCompat.RegisterLambdaDelegate(type, nameof(ThingComp.CompGetGizmosExtra),
                             3, 4, 5)) syncDelegate.SetDebugOnly();
            }
            catch (Exception exception)
            {
                Log.Warning(
                    $"{LogPrefix} Failed to register debug gizmos for CompAdvancedResourceProcessor: {exception.Message}");
            }

            try
            {
                MethodBase pasteLambda =
                    MpMethodUtil.GetLambda(type, nameof(ThingComp.CompGetGizmosExtra), lambdaOrdinal: 1);
                SafePatch(pasteLambda, new HarmonyMethod(typeof(AutomaticHydroponics), nameof(PrePasteProcessesGizmo)));
            }
            catch (Exception exception)
            {
                Log.Warning($"{LogPrefix} Failed to patch paste gizmo lambda: {exception.Message}");
            }

            PatchProcessType();
            PatchProcessStackType();
            PatchProcessorITab();
            PatchOverclockWindow();
        }
        catch (Exception exception)
        {
            Log.Error($"{LogPrefix} PatchProcessor failed: {exception}");
        }
    }

    private static void PatchProcessType()
    {
        try
        {
            var type = SafeType("PipeSystem.Process");
            if (type == null) return;

            try
            {
                processParentField = AccessTools.FieldRefAccess<ThingWithComps>(type, "parent");
                processIdField = AccessTools.FieldRefAccess<string>(type, "id");
                processDefField = AccessTools.FieldRefAccess<Def>(type, "def");
                processTargetCountField = AccessTools.FieldRefAccess<int>(type, "targetCount");
                processCountField = AccessTools.FieldRefAccess<int>(type, "processCount");
                processRepeatModeField = AccessTools.FieldRefAccess<BillRepeatModeDef>(type, "repeatMode");
                processQualityField = AccessTools.FieldRefAccess<QualityCategory>(type, "qualityToOutput");
                processProgressField = AccessTools.FieldRefAccess<float>(type, "progress");
                processSuspendedField = AccessTools.FieldRefAccess<bool>(type, "suspended");
            }
            catch (Exception exception)
            {
                Log.Warning($"{LogPrefix} Failed to bind PipeSystem.Process fields: {exception.Message}");
                return;
            }

            try
            {
                MP.RegisterSyncWorker<object>(SyncProcess, type, true);
            }
            catch (Exception exception)
            {
                Log.Warning($"{LogPrefix} RegisterSyncWorker skipped for PipeSystem.Process: {exception.Message}");
            }

            SafeSyncMethod(type, "ResetProcess");
            SafeLambdaMethod("PipeSystem.Process", "Options", MethodType.Getter, 0, 1, 2);

            try
            {
                MP.RegisterSyncMethod(MpMethodUtil.MethodOf(SyncedProcessModifyTargetCountButton));
            }
            catch (Exception exception)
            {
                Log.Warning(
                    $"{LogPrefix} RegisterSyncMethod skipped for SyncedProcessModifyTargetCountButton: {exception.Message}");
            }

            try
            {
                MP.RegisterSyncMethod(MpMethodUtil.MethodOf(SyncedProcessSuspendButton));
            }
            catch (Exception exception)
            {
                Log.Warning(
                    $"{LogPrefix} RegisterSyncMethod skipped for SyncedProcessSuspendButton: {exception.Message}");
            }

            try
            {
                MethodBase doInterface = AccessTools.DeclaredMethod(type, "DoInterface");
                SafePatch(doInterface,
                    transpiler: new HarmonyMethod(typeof(AutomaticHydroponics), nameof(ReplaceProcessImageButtons)));
            }
            catch (Exception exception)
            {
                Log.Warning($"{LogPrefix} Failed to patch PipeSystem.Process.DoInterface: {exception.Message}");
            }
        }
        catch (Exception exception)
        {
            Log.Warning($"{LogPrefix} PatchProcessType failed: {exception.Message}");
        }
    }

    private static void PatchProcessStackType()
    {
        try
        {
            var type = SafeType("PipeSystem.ProcessStack");
            if (type == null) return;

            try
            {
                processStackAddProcessMethodInfo = AccessTools.DeclaredMethod(type, "AddProcess");
                processStackAddProcessMethod = MethodInvoker.GetHandler(processStackAddProcessMethodInfo);
                processStackAddProcessParamCount = processStackAddProcessMethodInfo.GetParameters().Length;
                processStackNotifyProcessChangeMethod =
                    MethodInvoker.GetHandler(AccessTools.DeclaredMethod(type, "Notify_ProcessChange"));
                processStackProcessesField = AccessTools.FieldRefAccess<IList>(type, "processes");
            }
            catch (Exception exception)
            {
                Log.Warning($"{LogPrefix} Failed to bind PipeSystem.ProcessStack members: {exception.Message}");
                return;
            }

            try
            {
                MethodBase reorder = AccessTools.DeclaredMethod(type, "Reorder");
                SafePatch(reorder, new HarmonyMethod(typeof(AutomaticHydroponics), nameof(PreProcessStackReorder)));
            }
            catch (Exception exception)
            {
                Log.Warning($"{LogPrefix} Failed to patch ProcessStack.Reorder: {exception.Message}");
            }

            try
            {
                MP.RegisterSyncMethod(type, "AddProcess")
                    .CancelIfAnyArgNull()
                    .TransformTarget(
                        Serializer.New((object _, object _, object[] args) => (ThingWithComps)args[1],
                            GetProcessStackFromThingWithComps), true);
            }
            catch (Exception exception)
            {
                Log.Warning($"{LogPrefix} RegisterSyncMethod skipped for ProcessStack.AddProcess: {exception.Message}");
            }

            try
            {
                MP.RegisterSyncMethod(type, "Delete")
                    .CancelIfAnyArgNull()
                    .TransformTarget(
                        Serializer.New((object _, object _, object[] args) => processParentField(args[0]),
                            GetProcessStackFromThingWithComps), true);
            }
            catch (Exception exception)
            {
                Log.Warning($"{LogPrefix} RegisterSyncMethod skipped for ProcessStack.Delete: {exception.Message}");
            }

            try
            {
                MP.RegisterSyncMethod(type, "Reorder")
                    .CancelIfAnyArgNull()
                    .TransformTarget(
                        Serializer.New((object _, object _, object[] args) => processParentField(args[0]),
                            GetProcessStackFromThingWithComps), true);
            }
            catch (Exception exception)
            {
                Log.Warning($"{LogPrefix} RegisterSyncMethod skipped for ProcessStack.Reorder: {exception.Message}");
            }
        }
        catch (Exception exception)
        {
            Log.Warning($"{LogPrefix} PatchProcessStackType failed: {exception.Message}");
        }
    }

    private static void PatchProcessorITab()
    {
        try
        {
            var utilityType = SafeType("PipeSystem.ProcessUtility");
            if (utilityType != null)
                try
                {
                    processUtilityClipboardField =
                        AccessTools.StaticFieldRefAccess<IDictionary>(
                            AccessTools.DeclaredField(utilityType, "Clipboard"));
                }
                catch (Exception exception)
                {
                    Log.Warning($"{LogPrefix} Failed to bind ProcessUtility.Clipboard: {exception.Message}");
                }

            var tabType = SafeType("PipeSystem.ITab_Processor");
            if (tabType == null) return;

            try
            {
                MP.RegisterSyncMethod(MpMethodUtil.MethodOf(SyncedPasteProcesses)).CancelIfAnyArgNull();
            }
            catch (Exception exception)
            {
                Log.Warning($"{LogPrefix} RegisterSyncMethod skipped for SyncedPasteProcesses: {exception.Message}");
            }

            try
            {
                MethodBase fillTab = AccessTools.DeclaredMethod(tabType, nameof(ITab.FillTab));
                SafePatch(fillTab,
                    transpiler: new HarmonyMethod(typeof(AutomaticHydroponics), nameof(ReplaceProcessorITabButtons)));
            }
            catch (Exception exception)
            {
                Log.Warning($"{LogPrefix} Failed to patch ITab_Processor.FillTab: {exception.Message}");
            }
        }
        catch (Exception exception)
        {
            Log.Warning($"{LogPrefix} PatchProcessorITab failed: {exception.Message}");
        }
    }

    private static void BindProcessorFieldsForPasteFix()
    {
        try
        {
            var processorType = advancedResourceProcessorType = SafeType("PipeSystem.CompAdvancedResourceProcessor");
            if (processorType != null)
                try
                {
                    advancedResourceProcessorProcessStackField =
                        AccessTools.FieldRefAccess<object>(processorType, "processStack");
                }
                catch (Exception exception)
                {
                    Log.Warning($"{LogPrefix} Failed to bind processStack field (paste fix): {exception.Message}");
                }

            var processType = SafeType("PipeSystem.Process");
            if (processType != null)
                try
                {
                    processParentField = AccessTools.FieldRefAccess<ThingWithComps>(processType, "parent");
                    processIdField = AccessTools.FieldRefAccess<string>(processType, "id");
                    processDefField = AccessTools.FieldRefAccess<Def>(processType, "def");
                    processTargetCountField = AccessTools.FieldRefAccess<int>(processType, "targetCount");
                    processCountField = AccessTools.FieldRefAccess<int>(processType, "processCount");
                    processRepeatModeField = AccessTools.FieldRefAccess<BillRepeatModeDef>(processType, "repeatMode");
                    processQualityField = AccessTools.FieldRefAccess<QualityCategory>(processType, "qualityToOutput");
                    processProgressField = AccessTools.FieldRefAccess<float>(processType, "progress");
                    processSuspendedField = AccessTools.FieldRefAccess<bool>(processType, "suspended");
                }
                catch (Exception exception)
                {
                    Log.Warning($"{LogPrefix} Failed to bind Process fields (paste fix): {exception.Message}");
                }

            var stackType = SafeType("PipeSystem.ProcessStack");
            if (stackType != null)
                try
                {
                    processStackAddProcessMethodInfo = AccessTools.DeclaredMethod(stackType, "AddProcess");
                    processStackAddProcessMethod = MethodInvoker.GetHandler(processStackAddProcessMethodInfo);
                    processStackAddProcessParamCount = processStackAddProcessMethodInfo.GetParameters().Length;
                    processStackNotifyProcessChangeMethod =
                        MethodInvoker.GetHandler(AccessTools.DeclaredMethod(stackType, "Notify_ProcessChange"));
                    processStackProcessesField = AccessTools.FieldRefAccess<IList>(stackType, "processes");
                }
                catch (Exception exception)
                {
                    Log.Warning($"{LogPrefix} Failed to bind ProcessStack members (paste fix): {exception.Message}");
                }
        }
        catch (Exception exception)
        {
            Log.Warning($"{LogPrefix} BindProcessorFieldsForPasteFix failed: {exception.Message}");
        }
    }

    private static void PatchMpCompatPasteFix()
    {
        try
        {
            var vefCompatType = AccessTools.TypeByName("Multiplayer.Compat.VanillaExpandedFramework");
            if (vefCompatType == null) return;

            var vefPaste = AccessTools.DeclaredMethod(vefCompatType, "SyncedPasteProcesses");
            if (vefPaste == null)
            {
                Log.Warning($"{LogPrefix} MP Compat SyncedPasteProcesses not found, paste compat fix skipped.");
                return;
            }

            SafePatch(vefPaste,
                new HarmonyMethod(typeof(AutomaticHydroponics), nameof(PreMpCompatSyncedPasteProcesses)));
            Log.Message($"{LogPrefix} Patched MP Compat SyncedPasteProcesses for new PipeSystem.AddProcess signature.");
        }
        catch (Exception exception)
        {
            Log.Warning($"{LogPrefix} PatchMpCompatPasteFix failed: {exception.Message}");
        }
    }

    private static bool PreMpCompatSyncedPasteProcesses(ThingComp comp, List<(Def, int)> clipboard)
    {
        try
        {
            if (!MP.IsInMultiplayer) return true;

            if (comp == null || clipboard == null || clipboard.Count == 0) return false;

            var processStack = GetProcessStackFromComp(comp);
            if (processStack == null) return false;

            var processes = processStackProcessesField(processStack);
            processes.Clear();

            foreach (object entry in clipboard)
            {
                Def def = null;
                var repeatMode = BillRepeatModeDefOf.RepeatCount;
                var targetCount = 1;

                try
                {
                    var entryType = entry.GetType();
                    def = entryType.GetField("Item1")?.GetValue(entry) as Def
                          ?? entryType.GetProperty("Item1")?.GetValue(entry) as Def;
                    repeatMode = entryType.GetField("Item2")?.GetValue(entry) as BillRepeatModeDef
                                 ?? entryType.GetProperty("Item2")?.GetValue(entry) as BillRepeatModeDef
                                 ?? BillRepeatModeDefOf.RepeatCount;
                    var countObj = entryType.GetField("Item3")?.GetValue(entry)
                                   ?? entryType.GetProperty("Item3")?.GetValue(entry);
                    if (countObj is int count)
                    {
                        targetCount = count;
                    }
                    else if (entry is ValueTuple<Def, int> pair)
                    {
                        def = pair.Item1;
                        targetCount = pair.Item2;
                    }
                }
                catch (Exception exception)
                {
                    Log.Warning(
                        $"{LogPrefix} Failed to read MP Compat clipboard entry, using defaults: {exception.Message}");
                }

                if (def == null) continue;

                AddProcessToStack(processStack, def, comp.parent, repeatMode, targetCount, QualityCategory.Normal);
            }

            foreach (var process in processes) processProgressField(process) = 0f;
        }
        catch (Exception exception)
        {
            Log.Error($"{LogPrefix} PreMpCompatSyncedPasteProcesses failed: {exception}");
        }

        return false;
    }

    private static void PatchOverclockWindow()
    {
        try
        {
            if (advancedResourceProcessorType == null) return;

            try
            {
                var field = AccessTools.Field(advancedResourceProcessorType, "overclockMultiplier");
                if (field != null) overclockMultiplierField = MP.RegisterSyncField(field);
            }
            catch (Exception exception)
            {
                Log.Warning($"{LogPrefix} RegisterSyncField skipped for overclockMultiplier: {exception.Message}");
            }

            var windowType = SafeType("PipeSystem.Window_Overclock");
            if (windowType == null) return;

            try
            {
                processorField = AccessTools.FieldRefAccess<ThingComp>(windowType, "building");
            }
            catch (Exception exception)
            {
                Log.Warning($"{LogPrefix} Failed to bind Window_Overclock.building: {exception.Message}");
                return;
            }

            try
            {
                MethodBase doWindowContents = AccessTools.Method(windowType, "DoWindowContents");
                SafePatch(
                    doWindowContents,
                    new HarmonyMethod(typeof(AutomaticHydroponics), nameof(PreOverclockWindow)),
                    new HarmonyMethod(typeof(AutomaticHydroponics), nameof(PostOverclockWindow)));
            }
            catch (Exception exception)
            {
                Log.Warning($"{LogPrefix} Failed to patch Window_Overclock.DoWindowContents: {exception.Message}");
            }
        }
        catch (Exception exception)
        {
            Log.Warning($"{LogPrefix} PatchOverclockWindow failed: {exception.Message}");
        }
    }

    #region Processor sync workers

    private static void SyncProcess(SyncWorker sync, ref object process)
    {
        if (sync.isWriting)
        {
            if (process == null)
            {
                sync.Write<ThingComp>(null);
                Log.Error($"{LogPrefix} Trying to sync a null process.");
                return;
            }

            var parent = GetProcessorCompFromThingWithComps(processParentField(process));
            sync.Write(parent);

            if (parent == null)
            {
                Log.Error($"{LogPrefix} Trying to sync a process with no parent. Process={process}");
                return;
            }

            sync.Write(processIdField(process));
        }
        else
        {
            var comp = sync.Read<ThingComp>();
            if (comp == null) return;

            var stack = advancedResourceProcessorProcessStackField(comp);
            if (stack == null)
            {
                Log.Error($"{LogPrefix} The process has no stack. Comp={comp}");
                return;
            }

            var list = processStackProcessesField(stack);
            if (list == null)
            {
                Log.Error($"{LogPrefix} The process has ProcessStack with no process list. Comp={comp}");
                return;
            }

            var id = sync.Read<string>();
            process = GetProcessById(list, id);
            if (process == null) Log.Error($"{LogPrefix} Could not find the correct process. Comp={comp}, id={id}");
        }
    }

    #endregion

    #region Processor fields

    private static Type advancedResourceProcessorType;
    private static AccessTools.FieldRef<ThingComp, object> advancedResourceProcessorProcessStackField;

    private static AccessTools.FieldRef<object, ThingWithComps> processParentField;
    private static AccessTools.FieldRef<object, string> processIdField;
    private static AccessTools.FieldRef<object, Def> processDefField;
    private static AccessTools.FieldRef<object, int> processTargetCountField;
    private static AccessTools.FieldRef<object, int> processCountField;
    private static AccessTools.FieldRef<object, BillRepeatModeDef> processRepeatModeField;
    private static AccessTools.FieldRef<object, QualityCategory> processQualityField;
    private static AccessTools.FieldRef<object, float> processProgressField;
    private static AccessTools.FieldRef<object, bool> processSuspendedField;

    private static FastInvokeHandler processStackAddProcessMethod;
    private static MethodInfo processStackAddProcessMethodInfo;
    private static int processStackAddProcessParamCount;
    private static FastInvokeHandler processStackNotifyProcessChangeMethod;
    private static AccessTools.FieldRef<object, IList> processStackProcessesField;

    private static AccessTools.FieldRef<IDictionary> processUtilityClipboardField;

    private static AccessTools.FieldRef<object, ThingComp> processorField;
    private static ISyncField overclockMultiplierField;

    #endregion

    #region Processor utility methods

    private static object GetProcessStackFromComp(ThingComp comp)
    {
        return advancedResourceProcessorProcessStackField(comp);
    }

    private static object GetProcessStackFromThingWithComps(ThingWithComps thing)
    {
        return GetProcessStackFromComp(GetProcessorCompFromThingWithComps(thing));
    }

    private static ThingComp GetProcessorCompFromThingWithComps(ThingWithComps thing)
    {
        return thing.AllComps.Find(comp => advancedResourceProcessorType.IsInstanceOfType(comp));
    }

    private static object GetProcessById(IList processes, string id)
    {
        return processes?.Cast<object>().FirstOrDefault(process => processIdField(process) == id);
    }

    #endregion

    #region Processor minor patches

    private static void PreProcessStackReorder(object process, ref int offset, IList ___processes)
    {
        if (!MP.IsExecutingSyncCommand) return;

        var index = ___processes.IndexOf(process);
        var num = Mathf.Clamp(offset + index, 0, ___processes.Count - 1);
        offset = num - index;
    }

    private static bool PrePasteProcessesGizmo(ThingComp __instance)
    {
        if (!MP.IsInMultiplayer) return true;

        var clipboard = processUtilityClipboardField();
        if (!clipboard.Contains(__instance.parent.def)) return false;

        if (clipboard[__instance.parent.def] is not IList processes || processes.Count == 0) return false;

        var list = processes.Cast<object>()
            .Select(process => (processDefField(process), processRepeatModeField(process),
                processTargetCountField(process), processQualityField(process)))
            .ToList();
        SyncedPasteProcesses(__instance, list);
        return false;
    }

    private static void PreOverclockWindow(Window __instance)
    {
        var processor = processorField(__instance);
        MP.WatchBegin();
        overclockMultiplierField.Watch(processor);
    }

    private static void PostOverclockWindow(Window __instance)
    {
        MP.WatchEnd();
    }

    #endregion

    #region Processor ITab patches

    private static void SyncedPasteProcesses(ThingComp comp,
        List<(Def, BillRepeatModeDef, int, QualityCategory)> clipboard)
    {
        Log.Message(
            $"{LogPrefix} SyncedPasteProcesses running for {comp?.parent?.def?.defName}, entries: {clipboard?.Count}, executing: {MP.IsExecutingSyncCommand}.");
        if (clipboard.NullOrEmpty()) return;

        var processStack = GetProcessStackFromComp(comp);
        var processes = processStackProcessesField(processStack);
        processes.Clear();

        foreach (var (processDef, repeatMode, targetCount, quality) in clipboard)
            AddProcessToStack(processStack, processDef, comp.parent, repeatMode, targetCount, quality);

        foreach (var process in processes) processProgressField(process) = 0f;
    }

    private static void AddProcessToStack(object processStack, Def processDef, ThingWithComps parent,
        BillRepeatModeDef repeatMode, int targetCount, QualityCategory quality)
    {
        try
        {
            if (processStackAddProcessParamCount >= 5)
                processStackAddProcessMethod(processStack, processDef, parent, repeatMode, targetCount, quality);
            else if (processStackAddProcessParamCount == 4)
                processStackAddProcessMethod(processStack, processDef, parent, repeatMode, targetCount);
            else
                processStackAddProcessMethodInfo.Invoke(processStack,
                    new object[] { processDef, parent, repeatMode, targetCount, quality });
        }
        catch (Exception exception)
        {
            Log.Warning($"{LogPrefix} AddProcessToStack failed, trying legacy signature: {exception.Message}");
            try
            {
                processStackAddProcessMethodInfo.Invoke(processStack, new object[] { processDef, parent, repeatMode });
            }
            catch (Exception innerException)
            {
                Log.Error($"{LogPrefix} AddProcessToStack legacy fallback failed: {innerException}");
            }
        }
    }

    private static bool ReplacedPasteProcesses(Rect butRect, Texture2D tex, bool doMouseoverSound, string tooltip)
    {
        var result = Widgets.ButtonImage(butRect, tex, doMouseoverSound, tooltip);
        if (!result || !MP.IsInMultiplayer) return result;

        if (Find.Selector.SingleSelectedThing is not ThingWithComps thing)
        {
            Log.Message($"{LogPrefix} Paste clicked but SingleSelectedThing is not a building.");
            return false;
        }

        var comp = GetProcessorCompFromThingWithComps(thing);
        if (comp == null)
        {
            Log.Message($"{LogPrefix} Paste clicked but no processor comp on {thing.def.defName}.");
            return false;
        }

        var clipboard = processUtilityClipboardField();
        if (!clipboard.Contains(thing.def))
        {
            Log.Message(
                $"{LogPrefix} Paste clicked but clipboard has no entry for {thing.def.defName}. Keys: {string.Join(",", clipboard.Keys.Cast<Def>().Select(key => key.defName))}.");
            return false;
        }

        if (clipboard[thing.def] is not IList processes || processes.Count == 0)
        {
            Log.Message($"{LogPrefix} Paste clicked but clipboard list empty for {thing.def.defName}.");
            return false;
        }

        var list = processes.Cast<object>()
            .Select(process => (processDefField(process), processRepeatModeField(process),
                processTargetCountField(process), processQualityField(process)))
            .ToList();
        SyncedPasteProcesses(comp, list);

        return false;
    }

    private static void SyncedProcessModifyTargetCountButton(ThingComp comp, string processId, int offset)
    {
        var stack = GetProcessStackFromComp(comp);
        var process = GetProcessById(processStackProcessesField(stack), processId);
        if (process == null) return;

        ref var targetCount = ref processTargetCountField(process);

        if (targetCount == -1)
        {
            processRepeatModeField(process) = BillRepeatModeDefOf.RepeatCount;
            processCountField(process) = 0;
            processTargetCountField(process) = 0;
            targetCount = 1;
        }
        else if (targetCount > -1)
        {
            targetCount = Mathf.Max(0, targetCount + offset);
        }

        processStackNotifyProcessChangeMethod(stack);
    }

    private static bool ReplacedProcessModifyTargetCountButton(WidgetRow row, Texture2D texture, string tooltip,
        Color? mouseoverColor, Color? backgroundColor, Color? mouseoverBackgroundColor, bool doMouseoverSound,
        float overrideSize, object process)
    {
        var result = row.ButtonIcon(texture, tooltip, mouseoverColor, backgroundColor, mouseoverBackgroundColor,
            doMouseoverSound, overrideSize);
        if (!result || !MP.IsInMultiplayer) return result;

        var parent = processParentField(process);
        if (parent == null) return false;

        var comp = GetProcessorCompFromThingWithComps(parent);
        if (comp == null) return false;

        var id = processIdField(process);
        var offset = GenUI.CurrentAdjustmentMultiplier();
        if (texture == TexButton.Minus) offset = -offset;

        SyncedProcessModifyTargetCountButton(comp, id, offset);

        SoundDefOf.DragSlider.PlayOneShotOnCamera();
        return false;
    }

    private static void SyncedProcessSuspendButton(ThingComp comp, string processId)
    {
        var stack = GetProcessStackFromComp(comp);
        var process = GetProcessById(processStackProcessesField(stack), processId);
        if (process == null) return;

        ref var suspended = ref processSuspendedField(process);
        suspended = !suspended;
        processStackNotifyProcessChangeMethod(stack);
    }

    private static bool ReplacedProcessSuspendButton(Rect butRect, Texture2D tex, Color baseColor,
        bool doMouseoverSound, string tooltip, object process)
    {
        var result = Widgets.ButtonImage(butRect, tex, baseColor, doMouseoverSound, tooltip);
        if (!result || !MP.IsInMultiplayer) return result;

        var parent = processParentField(process);
        if (parent == null) return false;

        var comp = GetProcessorCompFromThingWithComps(parent);
        if (comp == null) return false;

        var id = processIdField(process);
        SyncedProcessSuspendButton(comp, id);

        SoundDefOf.DragSlider.PlayOneShotOnCamera();
        return false;
    }

    private static IEnumerable<CodeInstruction> ReplaceProcessorITabButtons(IEnumerable<CodeInstruction> instructions,
        MethodBase baseMethod)
    {
        var target = AccessTools.DeclaredMethod(typeof(Widgets), nameof(Widgets.ButtonImage),
            [typeof(Rect), typeof(Texture2D), typeof(bool), typeof(string)]);
        var replacement = MpMethodUtil.MethodOf(ReplacedPasteProcesses);

        var result = instructions;

        try
        {
            var vefCompatType = AccessTools.TypeByName("Multiplayer.Compat.VanillaExpandedFramework");
            if (vefCompatType != null)
            {
                var vefReplacement = AccessTools.DeclaredMethod(vefCompatType, "ReplacedPasteProcesses");
                if (vefReplacement != null)
                    result = result.ReplaceMethod(vefReplacement, replacement, baseMethod, expectedReplacements: -1,
                        targetText: "PipeSystem_PasteProcesses");
                else
                    result = result.ReplaceMethod(target, replacement, baseMethod, expectedReplacements: -1,
                        targetText: "PipeSystem_PasteProcesses");
            }
            else
            {
                result = result.ReplaceMethod(target, replacement, baseMethod, expectedReplacements: -1,
                    targetText: "PipeSystem_PasteProcesses");
            }
        }
        catch (Exception exception)
        {
            Log.Warning($"{LogPrefix} Failed to patch paste replacement: {exception.Message}");
        }

        return result;
    }

    private static IEnumerable<CodeInstruction> ReplaceProcessImageButtons(IEnumerable<CodeInstruction> instructions,
        MethodBase baseMethod)
    {
        var widgetRowTarget = AccessTools.DeclaredMethod(typeof(WidgetRow), nameof(WidgetRow.ButtonIcon));
        var widgetRowReplacement = MpMethodUtil.MethodOf(ReplacedProcessModifyTargetCountButton);
        var firstTargetField = AccessTools.DeclaredField(typeof(TexButton), nameof(TexButton.Plus));
        var secondTargetField = AccessTools.DeclaredField(typeof(TexButton), nameof(TexButton.Minus));

        var widgetButtonTarget = AccessTools.DeclaredMethod(typeof(Widgets), nameof(Widgets.ButtonImage),
            [typeof(Rect), typeof(Texture2D), typeof(Color), typeof(bool), typeof(string)]);
        var widgetButtonReplacement = MpMethodUtil.MethodOf(ReplacedProcessSuspendButton);
        var widgetButtonField = AccessTools.DeclaredField(typeof(TexButton), nameof(TexButton.Suspend));

        IEnumerable<CodeInstruction> ExtraInstructions(CodeInstruction _)
        {
            yield return CodeInstruction.LoadArgument(0);
        }

        return instructions
            .ReplaceMethod(widgetRowTarget, widgetRowReplacement, baseMethod, ExtraInstructions,
                expectedReplacements: 2,
                targetInstruction: instruction =>
                    instruction.LoadsField(firstTargetField) || instruction.LoadsField(secondTargetField))
            .ReplaceMethod(widgetButtonTarget, widgetButtonReplacement, baseMethod, ExtraInstructions,
                expectedReplacements: 1, targetInstruction: instruction => instruction.LoadsField(widgetButtonField));
    }

    #endregion
}