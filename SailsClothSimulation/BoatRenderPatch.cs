using HarmonyLib;
using System.Reflection;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace SailsClothSimulation
{
    [HarmonyPatch]
    public static class BoatRenderPatch
    {
        static MethodBase TargetMethod()
        {
            
            return AccessTools.Method(typeof(EntityBoat), "OnRenderFrame");
        }

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
