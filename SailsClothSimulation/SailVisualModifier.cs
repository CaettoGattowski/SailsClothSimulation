using HarmonyLib;
using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace SailsClothSimulation
{
    [HarmonyPatch(typeof(EntityBoat))]
    [HarmonyPatch("OnRenderFrame")]
    public static class BoatSailWindPostfix
    {
        static void Postfix(EntityBoat __instance, float dt, EnumRenderStage stage)
        {
            if (__instance?.Api is not ICoreClientAPI capi) return;

            SailVisualModifier.ApplyWindEffect(__instance, capi, dt);
        }
    }

    public static class SailVisualModifier
    {
        private static float phase;

        private const float RotationMultiplier = 40f;   // huge rotation effect
        private const float FlutterFrequency = 5f;      // faster flutter
        private const float WindEffectMultiplier = 2f;  // amplify effect by wind speed
        private const float MaxPushBack = 5f;           // push mesh backwards (units)
        private const float MaxVerticalBob = 1f;        // vertical sway

        // Reflection field for the private 'weatherVaneAnimCode' in EntityBoat
        private static readonly System.Reflection.FieldInfo WeatherVaneField =
            AccessTools.Field(typeof(EntityBoat), "weatherVaneAnimCode");

        public static void ApplyWindEffect(EntityBoat boat, ICoreClientAPI capi, float dt)
        {
            if (boat == null)
            {
                capi.TriggerChatMessage("[SailVisualModifier] Boat is null!");
                return;
            }

            if (boat.AnimManager?.Animator == null)
            {
                capi.TriggerChatMessage("[SailVisualModifier] AnimManager.Animator is null!");
                return;
            }

            if (!boat.Alive)
            {
                capi.TriggerChatMessage("[SailVisualModifier] Boat is not alive!");
                return;
            }

            phase += dt * FlutterFrequency;

            // Wind vector
            Vec3f wind = GlobalConstants.CurrentWindSpeedClient;
            float windSpeed = wind.Length();
            capi.TriggerChatMessage($"[SailVisualModifier] Wind vector: ({wind.X:0.00}, {wind.Y:0.00}, {wind.Z:0.00}), speed={windSpeed:0.00}");

            if (windSpeed < 0.05f)
            {
                capi.TriggerChatMessage("[SailVisualModifier] Wind too low, skipping effect.");
                return;
            }

            // Exaggerated swing for the sail animation
            float swingX = (float)Math.Sin(phase * 2f) * RotationMultiplier * windSpeed * WindEffectMultiplier;
            float swingY = (float)Math.Sin(phase * 3f) * RotationMultiplier * 0.5f * windSpeed;
            float swingZ = (float)Math.Cos(phase * 1.5f) * RotationMultiplier * 0.7f * windSpeed;

            // --- Access private weatherVaneAnimCode via reflection ---
            string weatherVaneAnimCode = WeatherVaneField?.GetValue(boat) as string;
            capi.TriggerChatMessage($"[SailVisualModifier] weatherVaneAnimCode (reflection) = {weatherVaneAnimCode ?? "null"}");

            if (!string.IsNullOrEmpty(weatherVaneAnimCode))
            {
                if (!boat.AnimManager.IsAnimationActive(weatherVaneAnimCode))
                {
                    boat.AnimManager.StartAnimation(weatherVaneAnimCode);
                    capi.TriggerChatMessage($"[SailVisualModifier] Started animation {weatherVaneAnimCode}");
                }

                var sailAnim = boat.AnimManager.GetAnimationState(weatherVaneAnimCode);
                if (sailAnim != null)
                {
                    float totalSwing = swingX + swingY + swingZ;
                    sailAnim.CurrentFrame += totalSwing;
                    sailAnim.BlendedWeight = 1f;
                    sailAnim.EasingFactor = 1f;

                    capi.TriggerChatMessage(
                        $"[SailAnim] CurrentFrame updated by {totalSwing:0.00}, " +
                        $"BlendedWeight={sailAnim.BlendedWeight}, Easing={sailAnim.EasingFactor}, " +
                        $"AnimState exists={sailAnim != null}"
                    );
                }
                else
                {
                    capi.TriggerChatMessage("[SailVisualModifier] GetAnimationState returned null!");
                }
            }
            else
            {
                capi.TriggerChatMessage("[SailVisualModifier] weatherVaneAnimCode is empty, cannot animate sail!");
            }

            // --- Push the sail mesh backward along the wind + vertical bob ---
            if (boat.Properties?.Client?.Renderer is EntityShapeRenderer renderer && renderer.ModelMat != null)
            {
                Vec3f pushDir = new Vec3f(-wind.X, 0f, -wind.Z);
                pushDir.Normalize();

                float px = pushDir.X * MaxPushBack * windSpeed;
                float pz = pushDir.Z * MaxPushBack * windSpeed;
                float py = (float)Math.Sin(phase * 1.2f) * MaxVerticalBob * windSpeed;

                float[] m = renderer.ModelMat;
                if (m.Length >= 16)
                {
                    m[12] += px;
                    m[13] += py;
                    m[14] += pz;

                    capi.TriggerChatMessage(
                        $"[SailPush] Applied push vector: ({px:0.00}, {py:0.00}, {pz:0.00}), windSpeed={windSpeed:0.00}"
                    );
                }
                else
                {
                    capi.TriggerChatMessage("[SailVisualModifier] Renderer.ModelMat length < 16, cannot push mesh!");
                }
            }
            else
            {
                capi.TriggerChatMessage("[SailVisualModifier] Renderer null or ModelMat null, cannot push sail!");
            }
        }
    }

}





