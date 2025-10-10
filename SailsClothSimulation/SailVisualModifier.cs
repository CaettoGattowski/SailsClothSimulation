using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace SailsClothSimulation
{
    public static class SailVisualModifier
    {
        private static readonly Random Rand = new();

        private const float MaxLeanAngle = 35f;   // degrees — very visible
        private const float FlutterAngle = 5f;    // small fast flutter
        private const float FlutterSpeed = 3f;    // speed multiplier

        public static void ApplyWindEffect(EntityBoat boat, ICoreClientAPI capi, float dt)
        {
            if (boat?.Properties?.Client?.Renderer is not EntityShapeRenderer renderer) return;
            if (!boat.Alive) return;

            // Use reflection to get the private field "shape"
            var shapeField = HarmonyLib.AccessTools.Field(renderer.GetType(), "shape");
            var shape = shapeField?.GetValue(renderer) as Shape;
            if (shape == null) return;

            // Figure out which sail is active
            int sailPos = boat.WatchedAttributes.GetInt("sailPosition");
            string sailName = sailPos switch
            {
                0 => "SailUnfurled",
                1 => "SailHalf",
                2 => "SailHalf",
                _ => "SailUnfurled"
            };

            var sail = shape.GetElementByName(sailName);
            if (sail == null) return;

            var wind = GlobalConstants.CurrentWindSpeedClient;
            float windSpeed = wind.Length();
            if (windSpeed < 0.05f) return;

            // Simulate flutter + large lean
            float flutterPhase = (float)capi.World.ElapsedMilliseconds / 1000f * FlutterSpeed;
            float flutter = MathF.Sin(flutterPhase * 10f) * FlutterAngle * windSpeed;

            // Compute visible lean angles
            float targetLeanX = wind.Z * MaxLeanAngle * windSpeed;
            float targetLeanZ = -wind.X * MaxLeanAngle * windSpeed;

            // Combine base lean + flutter
            sail.RotationX = targetLeanX + flutter;
            sail.RotationZ = targetLeanZ + flutter * 0.5f;

            // Make the rotation look like it's pivoting near the bottom of the sail
            if (sail.RotationOrigin != null && sail.RotationOrigin.Length == 3)
            {
                // Push origin downward slightly for visible swing
                sail.RotationOrigin[1] = Math.Clamp(sail.RotationOrigin[1] - 1.5, -4, 4);
            }

            // Optional: scale slightly to look like the sail stretches under pressure
            sail.ScaleZ = 1.0 + windSpeed * 0.3;
        }
    }
}



