#ifndef SCREW_HELIX_INCLUDED
#define SCREW_HELIX_INCLUDED

// Wraps a straight encoded ribbon into a real helix, in the vertex stage.
//
// The ribbon is authored dimensionless. Each vertex carries:
//   positionOS.x = radial depth, in units of thread pitch (0 sits on the shaft surface)
//   positionOS.y = axial offset, in units of thread pitch
//   positionOS.z = s, the parameter from 0 to 1 along the run
//
// Because the angle is derived from the distance actually travelled divided by the
// pitch, this is not a stretch. It is a re-evaluation of the helix at the current
// length, so the pitch is constant by construction: a longer screw gets MORE turns,
// not longer ones. Encoding the profile in pitch units means one ribbon also serves
// every pitch and, with a matching transform scale, every diameter.
//
// The cost of the approach is that the ribbon is authored for the worst case number of
// turns, so a short screw carries the same vertex count as a long one. Segments per
// turn go up on short screws rather than geometry going to waste.

// The depth channel as authored in Blender: the crest sits at this value, the root a
// little below zero so it stays buried inside the core.
#define SCREW_RIBBON_CREST 0.62

void ScrewHelixWrap(float3 positionOS, float3 normalOS,
                    float startZ, float run, float pitch,
                    float coreRadius, float majorRadius, float taperFraction,
                    out float3 outPosition, out float3 outNormal)
{
    float s         = saturate(positionOS.z);
    float safePitch = max(pitch, 1e-5);

    // Run the thread out at both ends, so it sinks into the shaft instead of stopping
    // at an open cross section. Real screws do this too.
    float t     = max(taperFraction, 1e-4);
    float taper = smoothstep(0.0, t, s) * smoothstep(0.0, t, 1.0 - s);
    float depth = lerp(-0.05, positionOS.x, taper);

    float along = s * run;
    float angle = 6.28318530718 * along / safePitch;

    float sinA, cosA;
    sincos(angle, sinA, cosA);

    // A screw is named by its MAJOR diameter, so the crest lands on the nominal radius
    // and the thread is cut inwards to the core. Adding depth on top of the nominal
    // radius instead makes every screw oversized, and worse for coarser pitches.
    float frac = depth / SCREW_RIBBON_CREST;
    float r    = lerp(coreRadius, majorRadius, frac);

    outPosition = float3(r * cosA,
                         r * sinA,
                         startZ + along + positionOS.y * safePitch);

    // The profile normal is expressed in the ribbon's own frame: x radial, y axial,
    // z along the run. Rotate that frame into place at this point on the helix.
    float3 radialDir     = float3( cosA, sinA, 0.0);
    float3 axialDir      = float3( 0.0,  0.0,  1.0);
    float3 tangentialDir = float3(-sinA, cosA, 0.0);

    outNormal = normalize(normalOS.x * radialDir +
                          normalOS.y * axialDir +
                          normalOS.z * tangentialDir);
}

#endif
