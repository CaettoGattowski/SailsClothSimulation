using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace SailsClothSimulation
{
    public static class SailVisualModifier
    {
        public static void ApplyWindEffect(EntityBoat boat, ICoreClientAPI capi, float dt)
        {
            var renderer = boat.Properties?.Client?.Renderer as EntityShapeRenderer;
            if (renderer == null) return;

            // Access wind info
            Vec3f wind = GlobalConstants.CurrentWindSpeedClient;
            float windStrength = wind.Length();
            float windDir = (float)System.Math.Atan2(wind.X, wind.Z);

            // Example: gently bend the sail visually based on wind
            // (You can later enhance this to modify shader uniforms or vertex transforms)
            float sailBend = GameMath.Sin((float)capi.InWorldEllapsedMilliseconds / 800f) * windStrength * 0.05f;

            // Apply subtle deformation to renderer rotation or scale
            renderer.zangle += sailBend;  // tilt sideways slightly
            renderer.xangle += sailBend * 0.5f; // small forward bend
        }
    }
}
