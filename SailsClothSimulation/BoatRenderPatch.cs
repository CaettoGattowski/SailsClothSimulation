using System.Reflection;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.GameContent;

namespace SailsClothSimulation
{
    [HarmonyPatch]
    public static class BoatRenderPatch
    {
        // Target EntityBoat.OnRenderFrame
        static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(EntityBoat), "OnRenderFrame");
        }

        // Postfix: run after the original render logic
        static void Postfix(object __instance, float dt, EnumRenderStage stage)
        {
            var boat = __instance as EntityBoat;
            if (boat == null) return;

            var capiField = AccessTools.Field(typeof(EntityBoat), "capi");
            var capi = (ICoreClientAPI)capiField?.GetValue(boat);
            if (capi == null) return;

            SailVisualModifier.ApplyWindEffect(boat, capi, dt);
        }
    }
}

