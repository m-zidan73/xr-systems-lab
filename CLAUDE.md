# XR Systems Lab

One Unity project and one small system per build day. Each system is a piece of a larger XR or game system.

## Layout

- `unity/`: one Unity `6000.4.9f1` URP project, Windows platform, new Input System only.
- `unity/Assets/Lab/_Core/`: shared parts only (orbit camera, demo template, lighting).
- `unity/Assets/Lab/W01-SurgicalKit/D0-VariableScrew/`: one folder per system, with `Runtime/`, `Editor/`, `Tests/`, `Art/`, optional `XR/`, `Demo*.unity`, `card.json` and a generated `README.md`.
- `blender/`: `.blend` sources, named `<week>-<day>-<name>.blend`.
- `media/`: GIFs under 5 MB. No videos in the repo.
- `web/`: only when a card calls for the web.
- `site/`: the README and portfolio page generator. `tools/`: `clip-to-gif.mjs`, `scrub-paths.mjs`.

`README.md` files and `site/dist/` are generated from `card.json`. Edit the card, not the output.

## Code rules

1. **Systems never read input.** A system exposes methods and events, such as `Screw.Set(spec, lengthMm)`. Only `_Core` and `XR/` adapters read input, so new controls never change a system.
2. **One assembly per system.** Dependencies only point backwards: a system may use `_Core` and earlier systems of the same week; a day 5 integration may use any earlier system. System assemblies never reference XR packages; only `Lab.Core.XR` and a system's own `XR/` assembly do.
3. **Real-world scale.** 1 unit = 1 m. Small parts use snapping logic, not physics. `OrbitCamera` applies its small near clip at runtime, because Unity raises a saved near clip below 0.01 when a scene loads.
4. **Every day ends compiling clean, with a commit.** Revert a broken day rather than leaving it in place.
5. **Pinned versions.** Commit `Packages/manifest.json` and `packages-lock.json`. Packages use release versions, never `#main`. No upgrades in the middle of a week.
6. **Namespaces** follow `Lab.W01.SystemName`, with `.EditorTools` and `.Tests`. Never end a namespace in `.Editor`: it shadows `UnityEditor.Editor`.
7. **Custom URP shaders** declare `#pragma multi_compile _ _CLUSTER_LIGHT_LOOP` in their forward pass. Without it, direct light drops to zero under Forward+ and nothing reports an error.
8. **`.cs` files may contain `Ø` and `×`.** Edit them with a UTF-8-safe tool.

## Platforms

Unity on PC is the default: mouse and keyboard, tested in the Editor. VR through the XR Interaction Simulator, web demos and phone AR are used only when a card asks for them. No system or publishing step assumes them.

State `testedOn` honestly on every card. Simulator work is "tested in the XR simulator, not on a headset". Never claim headset testing that hasn't happened. Medical content is illustrative and says so.

## Human input

The owner picks the approach on each card, makes one change by hand and commits it separately, and writes `howItWorks`, `decisions` and `next` in their own words. Claude pastes that text as written and fixes spelling only. Claude never writes those fields or the hand-made change.

The credit line in `builtWith` names the tools used and ends with exactly: `Design decisions and explanation: Mohitur Rahman Zidan.`

## Assets

Only the owner's own models, CC0 assets, or CC-BY assets with credit, all listed in the card's `assets`. An unknown licence stays out. AI-generated models stay out.

Blender can store local paths inside the files it writes: an `.fbx` holds the `.blend` file's full path, and a `.blend` can hold folders from a saved file browser panel. A local pre-commit check reads the staged version of these files and blocks those paths.

1. Save `.blend` files with Compress off, so the check can read them.
2. Run `node tools/scrub-paths.mjs <file>` on each exported `.fbx` and saved `.blend`. It blanks the paths and keeps the file's length and layout.
3. Then `git add` the file. A blocked commit stays blocked until the cleaned file is staged again.

## No private notes in this repo

Plans, weekly notes and drafts live outside this repo. Never bypass the pre-commit check. If it blocks a commit, remove what it reports.

## Commands

Close the project in the Unity Editor before running Unity commands. They take minutes.

```bash
unity test unity --mode EditMode --output "$TEMP/lab-tests.xml"
node --test "site/test/*.test.mjs"
node --test "tools/test/*.test.mjs"
node site/build.mjs
node tools/clip-to-gif.mjs "<recording.mp4>" media/<id>.gif
node tools/scrub-paths.mjs <file.fbx or file.blend>
```

## A day is done when

1. The project compiles with no errors, and the demo scene plays.
2. The owner has play-tested it on the card's platform against the card's "done" sentence.
3. The owner's hand-made change is committed separately.
4. `card.json` has the owner's `howItWorks`, `decisions` and `next`, and an honest `testedOn`.
5. The GIF exists and is under 5 MB.
6. Everything is pushed, and the Pages deploy succeeded.
