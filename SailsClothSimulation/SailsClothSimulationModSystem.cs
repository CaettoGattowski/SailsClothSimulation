using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace SailsClothSimulation
{
    public class SailCloth
    {
        public ClothSystemNew Cloth;
        int widthPoints;
        int heightPoints;
        float restDistance;

        public SailCloth(ICoreAPI api, int widthPoints, int heightPoints, float restDistance, Vec3d startPos)
        {
            this.widthPoints = widthPoints;
            this.heightPoints = heightPoints;
            this.restDistance = restDistance;

            Vec3d endPos = startPos + new Vec3d((widthPoints - 1) * restDistance, 0, 0);

            // Create sail without any AssetLocation (procedural mesh)
            Cloth = ClothSystemNew.CreateSail(api, api.ModLoader.GetModSystem<ClothManager>(), startPos, endPos, null);

            // Override size info in ClothSystemNew
            Cloth.widthPoints = widthPoints;
            Cloth.heightPoints = heightPoints;
            Cloth.restDistance = restDistance;

            // Generate constraints and mesh
            Cloth.GenerateSailConstraints(widthPoints, heightPoints, restDistance);
            Cloth.MeshData = new CustomMeshDataPartFloat(5) // 3 xyz pos + 2 uv
            {
                Count = 0,
                Values = new float[widthPoints * heightPoints * 8] // rough estimate
            };
            Cloth.BuildSailMesh(widthPoints, heightPoints, restDistance);
        }

        public void AnchorCorner(int x, int y)
        {
            Cloth.Anchored.Add((x, y));
        }

        public void Simulate(float dt)
        {
            Cloth.updateFixedStep(dt);
        }
    }

    public class SailClothSystem : ModSystem
    {
        ICoreClientAPI capi;
        List<SailCloth> activeSails = new();
        SailClothRenderer sailRenderer;

        public override bool ShouldLoad(EnumAppSide forSide) => forSide == EnumAppSide.Client;

        public override void StartClientSide(ICoreClientAPI api)
        {
            capi = api;

            // Register sail shader (optional, for proper rendering)
            //to test later with actual shaders capi.Shader.RegisterFileShaderProgram("Sail", "Sail", out _);

            // Create example sail
            var Sail = new SailCloth(capi, 12, 9, 0.25f, new Vec3d(0, 2, 0));
            Sail.AnchorCorner(0, 0);
            Sail.AnchorCorner(11, 0);
            Sail.AnchorCorner(0, 8);
            activeSails.Add(Sail);

            // Renderer
            sailRenderer = new SailClothRenderer(api, activeSails.ConvertAll(s => s.Cloth));
            api.Event.RegisterRenderer(sailRenderer, EnumRenderStage.Opaque);

            // Game tick
            api.Event.RegisterGameTickListener(OnGameTick, 50);
        }

        void OnGameTick(float dt)
        {
            foreach (var sail in activeSails)
                sail.Simulate(dt / 1000f); // dt in seconds
        }
    }
}
