using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using BaseX;
using CodeX;
using FrooxEngine;
using FrooxEngine.LogiX;
using FrooxEngine.LogiX.Data;
using FrooxEngine.LogiX.ProgramFlow;
using FrooxEngine.UIX;
using HarmonyLib;
using NeosModLoader;

namespace StopNarcissistParents
{
    public class StopNarcissistParents : NeosMod
    {
        public override string Author => "Banane9";
        public override string Link => "https://github.com/Banane9/NeosStopNarcissistParents";
        public override string Name => "StopNarcissistParents";
        public override string Version => "1.0.0";

        public override void OnEngineInit()
        {
            Harmony harmony = new($"{Author}.{Name}");
            harmony.PatchAll();
        }

        [HarmonyPatch(typeof(SceneInspector))]
        private static class SceneInspectorPatches
        {
            [HarmonyTranspiler]
            [HarmonyPatch(nameof(SceneInspector.OnInsertParentPressed))]
            private static IEnumerable<CodeInstruction> OnInsertParentPressedTranspiler(IEnumerable<CodeInstruction> codeInstructions)
            {
                var instructions = codeInstructions.ToList();
                var syncRefTargetProperty = typeof(SyncRef<Slot>).GetProperty(nameof(SyncRef<Slot>.Target), AccessTools.all);
                var slotHierarchyDepthProperty = typeof(Slot).GetProperty(nameof(Slot.HierachyDepth), AccessTools.all);

                var assignmentIndex = instructions.FindLastIndex(instruction => instruction.Calls(syncRefTargetProperty.SetMethod));

                // Label on the return instruction
                var skipAssignmentTargetLabel = new Label();
                instructions[assignmentIndex + 1] = instructions[assignmentIndex + 1].WithLabels(skipAssignmentTargetLabel);

                // Add an if (Root.Target.HierarchyDepth < slot.HierarchyDepth) in front of the Root assignment
                instructions.InsertRange(assignmentIndex - 3, new[]
                {
                    // this.Root.Target.HierarchyDepth
                    instructions[assignmentIndex - 3],
                    instructions[assignmentIndex - 2],
                    new CodeInstruction(OpCodes.Callvirt, syncRefTargetProperty.GetMethod),
                    new CodeInstruction(OpCodes.Callvirt, slotHierarchyDepthProperty.GetMethod),

                    // slot.HierarchyDepth
                    instructions[assignmentIndex - 1],
                    new CodeInstruction(OpCodes.Callvirt, slotHierarchyDepthProperty.GetMethod),

                    // <
                    new CodeInstruction(OpCodes.Blt_S, skipAssignmentTargetLabel)
                });

                return instructions;
            }
        }
    }
}