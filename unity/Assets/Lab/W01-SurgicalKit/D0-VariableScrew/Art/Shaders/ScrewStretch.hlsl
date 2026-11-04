// Shader Graph Custom Function node, Vertex stage.
//
// Wire it up:
//   Position (Object)  -> PositionOS
//   UV0                -> UV
//   Vertex Color .r    -> ShaftMask      (1 on the shaft, 0 on head and tip)
//   _HeadEnd           -> HeadEnd        float property, set by Screw.cs
//   _ShaftLength       -> ShaftLength    float property, set by Screw.cs
//   _Stretch           -> Stretch        float property, set by Screw.cs
//   OutPosition        -> Vertex Position
//   OutUV              -> the UV feeding the thread normal map sampler
//
// Three slice along local Z:
//   head  stays put, because it has to mate with the plate hole
//   shaft stretches
//   tip   translates rigidly
//
// The thread lives in a normal map rather than in geometry, so retiling V by the same
// factor keeps the pitch constant in world units at any length. A pure axial stretch
// also leaves a cylinder's radial normals correct, so nothing needs recomputing.
//
// Model the screw with its axis along +Z and the head at z = 0. In Blender, model
// along +Y so default FBX export lands it on Unity's +Z.

void ScrewStretch_float(
    float3 PositionOS,
    float2 UV,
    float  ShaftMask,     // 1 on the shaft, 0 on head and tip
    float  HeadEnd,       // metres, where the head stops
    float  ShaftLength,   // metres, authored shaft length
    float  Stretch,       // metres, target length minus base mesh length
    out float3 OutPosition,
    out float2 OutUV)
{
    float safeShaft = max(ShaftLength, 1e-5);

    // 0 below the head, ramps across the shaft, clamps to 1 for the tip
    float t = saturate((PositionOS.z - HeadEnd) / safeShaft);
    OutPosition = float3(PositionOS.x, PositionOS.y, PositionOS.z + Stretch * t);

    float pitchScale = (safeShaft + Stretch) / safeShaft;
    OutUV = float2(UV.x, UV.y * lerp(1.0, pitchScale, saturate(ShaftMask)));
}
