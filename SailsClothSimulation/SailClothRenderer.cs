using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace SailsClothSimulation
{
    public class SailClothRenderer : IRenderer
    {
        private readonly ICoreClientAPI capi;
        private readonly List<ClothSystemNew> sails;

        public double RenderOrder => 0.5;
        public int RenderRange => 100;

        public SailClothRenderer(ICoreClientAPI api, List<ClothSystemNew> sails)
        {
            this.capi = api;
            this.sails = sails;
        }

        public void OnRenderFrame(float dt, EnumRenderStage stage)
        {
            foreach (var sail in sails)
            {
                // Update/build mesh if necessary
                sail.BuildSailMesh(sail.widthPoints, sail.heightPoints, sail.restDistance);

                // Render the mesh using default material
                if (sail.MeshRef != null)
                {
                    capi.Render.RenderMesh(sail.MeshRef);
                }
            }
        }

        public void Dispose()
        {
            // Dispose meshes if needed
            foreach (var sail in sails)
            {
                sail.MeshRef?.Dispose();
                sail.MeshRef = null;
            }
        }
    }
}

