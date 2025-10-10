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
        Harmony harmony;

        public override bool ShouldLoad(EnumAppSide side) => side == EnumAppSide.Client;

        public override void StartClientSide(ICoreClientAPI api)
        {
            var harmony = new Harmony("WindSailsPhysics.dynamicsails");
            harmony.PatchAll(); // <-- no assignment here

            api.Logger.Notification("[DynamicSails] Harmony PatchAll executed");

            foreach (var type in AccessTools.GetTypesFromAssembly(Assembly.GetExecutingAssembly()))
            {
                if (Attribute.IsDefined(type, typeof(HarmonyPatch)))
                    api.Logger.Notification($"[DynamicSails] Found patch class: {type.Name}");
            }

            var boatType = typeof(Vintagestory.GameContent.EntityBoat);
            api.Logger.Notification("[DynamicSails] Inspecting methods on EntityBoat...");

            foreach (var m in boatType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (m.Name.Contains("Render", StringComparison.OrdinalIgnoreCase))
                    api.Logger.Notification($"[DynamicSails] Found method: {m.Name}({string.Join(", ", m.GetParameters().Select(p => p.ParameterType.Name))})");
            }
        }



        public override void Dispose()
        {
            harmony?.UnpatchAll("WindSailsPhysics.dynamicsails");
        }
    }
}

