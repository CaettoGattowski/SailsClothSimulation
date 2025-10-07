using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using static Vintagestory.API.Client.MeshRef;

namespace SailsClothSimulation
{
    public class SailClothRenderer : IRenderer
    {
        ICoreClientAPI capi;
        List<ClothSystemNew> Sail;
        private int widthPoints;
        private int heightPoints;
        private float restDistance;

        public double RenderOrder => 0.5;
        public int RenderRange => 100;

        public SailClothRenderer(ICoreClientAPI api, List<ClothSystemNew> Sail)
        {
            this.capi = api;
            this.Sail = Sail;
        }

        public void OnRenderFrame(float dt, EnumRenderStage stage)
        {
            var prog = capi.Shader.GetProgram("Sail");
            if (prog == null) return;

            foreach (var Sail in Sail)
            {
                // Build/update mesh for sail
                Sail.BuildSailMesh(widthPoints, heightPoints, restDistance);

                // Set uniforms
                prog.Use();
                prog.Uniform("time", Sail.TimeSeconds);
                Vec3f windDir = (Vec3f)Sail.windSpeed;
                windDir.Normalize();
                prog.Uniform("windDir", windDir);
                prog.Uniform("windSpeed", (float)Sail.windSpeed.Length());

                // Render mesh
                capi.Render.RenderMesh(Sail.MeshRef);

                prog.Stop();
            }
        }

        public void Dispose() { }
    }
}
