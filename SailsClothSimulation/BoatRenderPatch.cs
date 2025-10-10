using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace SailsClothSimulation
{
    [HarmonyPatch(typeof(EntityBoat), "OnRenderFrame")]
    public static class BoatRenderPatch
    {
        public static void Postfix(EntityBoat __instance, float dt, EnumRenderStage stage)
        {
            // Only run during normal render stages
            if (stage != EnumRenderStage.Before || __instance.Api.Side != EnumAppSide.Client)
                return;

            // Access the client API
            ICoreClientAPI capi = __instance.Api as ICoreClientAPI;
            if (capi == null) return;

            // Call your sail modifier
            SailVisualModifier.ApplyWindEffect(__instance, capi, dt);
        }
    }
}
