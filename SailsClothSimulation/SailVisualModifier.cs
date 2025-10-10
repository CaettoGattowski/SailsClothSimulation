using System;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace SailsClothSimulation
{
    public static class SailVisualModifier
    {
        // state variables for a simple spring-based motion
        static float sailAngle, sailVelocity;

        public static void ApplyWindEffect(object boatEntity, ICoreClientAPI capi, float dt)
        {
            // Retrieve the renderer or shape associated with this boat
            var renderer = GetEntityRenderer(boatEntity);
            if (renderer == null) return;

            var shape = renderer?.TesselationData?.Shape;
            if (shape == null) return;

            var sail = shape.GetElementByName("SailUnfurled");
            if (sail == null) return;

            // --- physics model ---
            Vec3f wind = GlobalConstants.CurrentWindSpeedClient;
            float strength = wind.Length();
            float dir = (float)Math.Atan2(wind.X, wind.Z);

            float target = GameMath.Clamp(strength * GameMath.Sin(dir - renderer.EntityPos.Yaw), -0.6f, 0.6f);
            float stiffness = 3f;
            float damping = 1.5f;

            sailVelocity += (target - sailAngle) * stiffness * dt;
            sailVelocity -= sailVelocity * damping * dt;
            sailAngle += sailVelocity * dt;

            float flutter = GameMath.Sin((float)capi.InWorldEllapsedMilliseconds / 120f + dir)
                            * strength * 0.05f;
            float finalAngle = sailAngle + flutter;

            // --- apply transform ---
            sail.Rotation.Z = finalAngle * GameMath.RAD2DEG;
            sail.Rotation.X = flutter * 10f;
        }

        // You implement a helper that uses reflection to find the renderer
        static dynamic GetEntityRenderer(object boatEntity)
        {
            // e.g. return the private "entityShapeRenderer" field from EntityBoat
            return null; // fill in
        }
    }

}
