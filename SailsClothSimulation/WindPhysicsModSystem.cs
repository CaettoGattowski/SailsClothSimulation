using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Client;

namespace SailsClothSimulation
{
    public class WindPhysicsModSystem : ModSystem
    {
        private Harmony harmony;
        private ICoreClientAPI capi;

        public override void StartClientSide(ICoreClientAPI api)
        {
            capi = api;
            harmony = new Harmony("WindSailsPhysics.boatpatch");
            harmony.PatchAll();

            api.Logger.Notification("[DynamicSails] Harmony patch applied to EntityBoat.OnRenderFrame");
        }

        public override void Dispose()
        {
            harmony?.UnpatchAll("WindSailsPhysics.boatpatch");
        }
    }
}

