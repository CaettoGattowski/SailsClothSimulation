using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace SailsClothSimulation
{
    public class SailClothRenderer : IRenderer
    {
        ICoreClientAPI capi;
        List<SailCloth> sails;

        public double RenderOrder => 0.5;
        public int RenderRange => 100;

        public SailClothRenderer(ICoreClientAPI api, List<SailCloth> sails)
        {
            this.capi = api;
            this.sails = sails;
        }

        public void OnRenderFrame(float dt, EnumRenderStage stage)
        {
            var prog = capi.Shader.GetProgram("sail");
            if (prog == null) return;

            foreach (var sail in sails)
            {
                // Build/update mesh for sail
                sail.BuildMesh();

                // Set uniforms
                prog.Use();
                prog.Uniform("time", sail.TimeSeconds);
                Vec3f windDir = (Vec3f)sail.windSpeed;
                windDir.Normalize();
                prog.Uniform("windDir", windDir);
                prog.Uniform("windSpeed", (float)sail.windSpeed.Length());

                // Render mesh
                capi.Render.RenderMesh(sail.meshRef);

                prog.Stop();
            }
        }

        public void Dispose() { }
    }
}
