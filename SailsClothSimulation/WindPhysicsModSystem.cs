using HarmonyLib;
using System;
using System.Linq;
using System.Reflection;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace SailsClothSimulation
{
    public class WindPhysicsModSystem : ModSystem
    {
        private Harmony harmony; // <-- field reference

        public override bool ShouldLoad(EnumAppSide side) => side == EnumAppSide.Client;

        public override void StartClientSide(ICoreClientAPI api)
        {
            // Initialize Harmony using your unique ID
            Harmony harmony = new Harmony("WindSailsPhysics.dynamicsails");

            // Patch all classes with [HarmonyPatch] in this assembly
            harmony.PatchAll();
            api.Logger.Notification("[DynamicSails] Harmony PatchAll executed");

            // Optional: list all found patch classes
            var patchTypes = AccessTools.GetTypesFromAssembly(Assembly.GetExecutingAssembly())
                                        .Where(t => Attribute.IsDefined(t, typeof(HarmonyPatch)));
            foreach (var type in patchTypes)
            {
                api.Logger.Notification($"[DynamicSails] Found patch class: {type.Name}");
            }

            // Optional: inspect EntityBoat methods
            var boatType = typeof(Vintagestory.GameContent.EntityBoat);
            api.Logger.Notification("[DynamicSails] Inspecting methods on EntityBoat...");
            foreach (var m in boatType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (m.Name.Contains("Render", StringComparison.OrdinalIgnoreCase) ||
                    m.Name.Contains("Frame", StringComparison.OrdinalIgnoreCase))
                {
                    api.Logger.Notification($"[DynamicSails] Found method: {m.Name}({string.Join(", ", m.GetParameters().Select(p => p.ParameterType.Name))})");
                }
            }

            // Define a debug wind push setting if it doesn't exist yet
            // Set default value at mod startup
            api.Settings.Bool["showDebugWindPush"] = api.Settings.Bool["showDebugWindPush"]; // ensures key exists


            if (api.Settings.Bool["showDebugWindPush"])
            {
                // render debug wind push line
            }
        }

        public override void Dispose()
        {
            harmony?.UnpatchAll("WindSailsPhysics.dynamicsails");
        }
    }
}


