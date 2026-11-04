# Variable Screw: technical notes

## The slider

`ScrewEditor` draws an `IntSlider` over the **index** into `availableLengthsMm`, not
over millimetres. It snaps to exactly n positions by construction and cannot land on a
length the system does not ship. The swatch above it shows the tray colour, so the
inspector reads like the physical tray.

`Screw.OnValidate` also snaps, so a plain float slider still lands on real values if you
prefer that shape.

## Watch out

- **Colliders do not follow the shader.** Vertex displacement is a rendering effect;
  `Screw.cs` resizes the capsule and moves `TipPoint` by hand. Without them, physics and
  any depth check still use the base mesh length. This is the most common bug with this
  technique.
- **Forward+ needs a keyword.** A custom URP shader must declare
  `#pragma multi_compile _ _CLUSTER_LIGHT_LOOP`. Without it, direct light silently drops to
  zero under the Forward+ path and only the ambient remains. `PortTests` checks this by
  rendering each scene with the lights on and off.
- **Scale.** A 20 mm screw is 0.02 Unity units. Fine here, a problem once hand tracking
  and physics arrive. Decide the scene scale before modelling anything else.
- **`MaterialPropertyBlock` breaks the SRP Batcher** in URP. Irrelevant at a dozen
  screws; at hundreds you would move to instanced properties instead.

## Provenance

The colours come from a product reference photo. The lengths and drill sizes are
plausible but invented, and every generated asset says so in its `notes` field. Nothing
here is clinical guidance.
