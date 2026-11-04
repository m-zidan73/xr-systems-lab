#ifndef SCREW_COMMON_INCLUDED
#define SCREW_COMMON_INCLUDED

// Shared by every pass in Screw.shader. The forward, shadow caster and depth passes
// must all apply the same displacement, or the screw's shadow and its depth will be
// the wrong length.

// Three slice along the local Z axis:
//   head  stays put, because it has to mate with the plate hole
//   shaft stretches
//   tip   translates rigidly
//
// saturate() does the whole job: below the head it clamps to 0, across the shaft it
// ramps, past the shaft it clamps to 1.
// coreScale narrows the threaded portion to the screw's core diameter, leaving the head
// alone. A screw is named by its MAJOR diameter, which is the crest of the thread, so the
// shaft between threads has to be thinner than the nominal size. Pass 1.0 to disable,
// which is what the normal-mapped variant wants: there the painted thread reads as
// grooves cut into the surface, so that surface is the crest, not the core.
float3 ScrewStretchPosition(float3 positionOS, float headEnd, float shaftLength,
                            float stretch, float coreScale)
{
    float t = saturate((positionOS.z - headEnd) / max(shaftLength, 1e-5));

    // Everything past the underside of the head narrows, including the tip, so there is
    // no step where the shaft meets it.
    float narrow = lerp(1.0, coreScale, step(headEnd, positionOS.z));

    return float3(positionOS.xy * narrow, positionOS.z + stretch * t);
}

// Two jobs here.
//
// Keeping the pitch constant: the thread is a tiling normal map, so retiling V by the
// same factor the shaft grew by holds the pitch fixed in world units at any length.
// V spans 0 to 1 across the shaft whatever its length, so the crest spacing in
// millimetres never changes; only the number of crests does.
//
// Making it a helix rather than stacked rings: U runs around the circumference, so
// adding U to V advances the texture by exactly one thread repeat per revolution, which
// is the definition of a single start thread. The seam where U wraps from 1 back to 0
// is invisible because the jump is an integer number of repeats, so the texture lands
// on the same phase.
//
// Both terms are masked to the shaft, so the head and the tip keep their own UVs.
float2 ScrewThreadUV(float2 uv, float shaftMask, float shaftLength, float stretch,
                     float tiling, float starts)
{
    float safeShaft  = max(shaftLength, 1e-5);
    float pitchScale = (safeShaft + stretch) / safeShaft;
    float mask       = saturate(shaftMask);

    float v = uv.y * tiling * lerp(1.0, pitchScale, mask);
    v += uv.x * starts * mask;

    return float2(uv.x, v);
}

#endif
