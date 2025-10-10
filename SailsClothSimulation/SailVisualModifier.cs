using System;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace SailsClothSimulation
{
    /// <summary>
    /// Applies a dynamic wind-based visual effect to a boat's sail.
    /// This doesn't replace the mesh – it offsets rotation angles
    /// to mimic realistic cloth flutter driven by wind strength and direction.
    /// </summary>
    public static class SailVisualModifier
    {
        private static readonly Random Rand = new();

        // Controls how reactive the sail is to wind.
        private const float MaxFlutterAngle = 10f;   // degrees
        private const float FlutterSpeed = 2.5f;     // oscillation speed multiplier
        private const float WindInfluence = 0.6f;    // how much the global wind direction affects swing
        private const float Damping = 0.9f;          // smooths oscillation over time

        private static float currentSailBendX = 0f;
        private static float currentSailBendZ = 0f;
        private static float flutterPhase = 0f;

        public static void ApplyWindEffect(EntityBoat boat, ICoreClientAPI capi, float dt)
        {
            if (boat?.Properties?.Client?.Renderer is not EntityShapeRenderer renderer) return;
            if (!boat.Alive) return;

            var wind = GlobalConstants.CurrentWindSpeedClient;
            float windSpeed = wind.Length();
            if (windSpeed < 0.05f) return; // calm, skip for performance

            // Simulate gentle sine-based flutter using wind + time
            flutterPhase += dt * FlutterSpeed * (0.8f + (float)Rand.NextDouble() * 0.4f);

            float flutter = MathF.Sin(flutterPhase * 2f) * (0.5f + 0.5f * (float)Math.Sin(flutterPhase));
            float gustEffect = 0.5f + 0.5f * MathF.Sin(flutterPhase * 0.3f + (float)Rand.NextDouble() * 2f);

            // Calculate desired bend due to wind
            float targetBendX = wind.Z * MaxFlutterAngle * WindInfluence * windSpeed * gustEffect;
            float targetBendZ = -wind.X * MaxFlutterAngle * WindInfluence * windSpeed * gustEffect;

            // Smooth interpolation to prevent snapping
            currentSailBendX += (targetBendX - currentSailBendX) * (1 - Damping) * 5f * dt;
            currentSailBendZ += (targetBendZ - currentSailBendZ) * (1 - Damping) * 5f * dt;

            // Add flutter offset
            float flutterOffsetX = MathF.Sin(flutterPhase * 3f) * 1.2f;
            float flutterOffsetZ = MathF.Cos(flutterPhase * 2.1f) * 1.2f;

            // Apply total effect to renderer rotation
            renderer.xangle += (currentSailBendX + flutterOffsetX) * dt;
            renderer.zangle += (currentSailBendZ + flutterOffsetZ) * dt;
        }
    }
}

