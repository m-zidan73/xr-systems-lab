using UnityEngine;

namespace Lab.W01.VariableScrew
{
    /// <summary>
    /// One place that decides how big the world is.
    ///
    /// <para><b>Leave this at 1 for VR.</b> One Unity unit is one metre, the XR runtime
    /// reports hand and head positions in metres, so a 20 mm screw at 0.02 units looks like
    /// a 20 mm screw through a headset. That is the point. Scaling the model up makes a
    /// surgeon see an oversized prop, which is exactly the thing a training sim cannot
    /// afford to get wrong.</para>
    ///
    /// <para>The legitimate reason to raise it is numerical, not visual: Unity's physics
    /// defaults assume human-scale objects, and a 2 cm rigidbody sits below several of them.
    /// If you hit that, raise <see cref="WorldScale"/> AND scale your XR Origin by the same
    /// factor. Apparent size through the headset is then unchanged, because the player grew
    /// with the world, while every number the solver sees got bigger.</para>
    ///
    /// <para>Scaling the world without scaling the rig is the mistake. Then the screw really
    /// does look wrong.</para>
    /// </summary>
    public static class ScrewUnits
    {
        /// <summary>
        /// 1 = true scale, one unit is one metre. 10 = a ten times world, which needs an
        /// XR Origin scaled to 10 as well.
        /// </summary>
        public const float WorldScale = 1f;

        /// <summary>Millimetres to Unity units, before world scale.</summary>
        public const float MillimetresToUnits = 0.001f;

        /// <summary>A length in millimetres, in the authored metre space the shaders use.</summary>
        public static float Mm(float millimetres) => millimetres * MillimetresToUnits;

        /// <summary>A length in millimetres, in final world units including world scale.</summary>
        public static float MmWorld(float millimetres) => millimetres * MillimetresToUnits * WorldScale;

        /// <summary>
        /// Near clip plane that suits the current scale. Unity's 0.3 default would put the
        /// whole screw behind the camera.
        /// </summary>
        public static float NearClip => 0.001f * WorldScale;
    }
}
