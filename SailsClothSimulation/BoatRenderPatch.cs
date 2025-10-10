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
            // Only run during visible render stages
            if (stage != EnumRenderStage.Before) return;

            var capi = (ICoreClientAPI)typeof(EntityBoat)
                .GetField("capi", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.GetValue(__instance);

            if (capi == null) return;

            SailVisualModifier.ApplyWindEffect(__instance, capi, dt);
        }
    }

}
