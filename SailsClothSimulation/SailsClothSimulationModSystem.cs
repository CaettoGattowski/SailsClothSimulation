using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace SailsClothSimulation
{
    public class SailClothSystem : ModSystem
    {
        ICoreClientAPI capi;
        List<SailCloth> activeSails = new();
        SailClothRenderer sailRenderer;

        public override bool ShouldLoad(EnumAppSide forSide) => forSide == EnumAppSide.Client;

        public override void StartClientSide(ICoreClientAPI api)
        {
            capi = api;

            // Register sail shader
            capi.Shader.RegisterFileShaderProgram("sail", "sail", out _);

            // Create example sail
            var sail = new SailCloth(capi, 12, 9, 0.25f, new Vec3d(0, 2, 0));
            sail.AnchorCorner(0, 0);
            sail.AnchorCorner(11, 0);
            sail.AnchorCorner(0, 8);
            activeSails.Add(sail);

            // Renderer
            sailRenderer = new SailClothRenderer(api, activeSails);
            api.Event.RegisterRenderer(sailRenderer, EnumRenderStage.Opaque);

            // Game tick
            api.Event.RegisterGameTickListener(OnGameTick, 50);
        }

        void OnGameTick(float dt)
        {
            foreach (var sail in activeSails)
                sail.Simulate(dt / 1000f);
        }
    }
}
