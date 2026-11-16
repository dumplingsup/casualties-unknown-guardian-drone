# Guardian Drone 0.2.0 — Nexus page material

Published 2026-11-16: [Nexus #985](https://www.nexusmods.com/scavprototype/mods/985). Actual submission and file verification: [PUBLISH-STATUS.md](PUBLISH-STATUS.md).

## Form fields

- Game: Casualties: Unknown (Former Scav Prototype), site slug `scavprototype`.
- Title: Guardian Drone - Combat and Medical Companion
- Version: 0.2.0
- File title: Guardian Drone 0.2.0 - Single-player Beta
- File: `artifacts/nexus-0.2.0/GuardianDrone-0.2.0-Nexus.zip`
- Summary: A free, indestructible drone that follows you, fires at nearby supported threats, and treats external wounds. Checks its firing lane; rounds in flight can still hit you. Single-player beta.
- Actual category: Miscellaneous (the category offered for this game).
- In-game languages: English, Simplified Chinese.
- Installation: manual, Windows x64. Vortex installation has not been tested.
- Nexus requirement: [CUCoreLib](https://www.nexusmods.com/scavprototype/mods/341), tested version 1.1.
- Off-site requirement: [BepInEx 5.4.23.6, Windows x64](https://github.com/BepInEx/BepInEx/releases/tag/v5.4.23.6).
- AI disclosure: AI-Generated Content (most code and scripted sprite production); AI Media for the drafted page text if the form offers a separate field. Do not select No AI or describe the work as limited code completion.
- Permissions: the repository has no open-source licence. Keep reuse, reupload, conversion, asset modification, forks and training permissions at Ask me / permission required; do not invent an MIT/CC grant.
- Do not claim multiplayer, Steam Deck/Linux, all enemies, complete medical treatment, Vortex support, or 0.2.0 Wayfarer compatibility as tested.

## Main description — English

Guardian Drone adds a free quadcopter companion for your descent. Carry its controller to deploy it, let it patrol nearby, and call it in to treat external wounds.

This is a **single-player beta for Windows x64**, tested with Steam build **26666273**, BepInEx **5.4.23.6** and CUCoreLib **1.1**.

### What it does

- Follows the player and stays roughly 6–9 world units away in open spaces. Low caves use closer side positions.
- Fires at nearby supported threats. It checks the firing lane before shooting and attempts to move when a body or obstacle blocks the shot.
- Keeps physical projectiles: **a round already in flight can still hit you or a bystander**. F7 toggles ceasefire.
- Moves close to reduce external bleeding and repair skin and muscle on existing organic limbs. F8 requests medical priority.
- Shows escort/ceasefire and current activity in a small status panel.
- Stows while no usable flight position is available, then redeploys when space opens.
- Saves deployment intent and ceasefire with the character.

The drone is indestructible and consumes no ammunition, batteries, medicine or crafting resources.

### Install

1. Install [BepInEx 5.4.23.6 for Windows x64](https://github.com/BepInEx/BepInEx/releases/tag/v5.4.23.6) beside `CasualtiesUnknown.exe`. Run the game once and close it.
2. Install [CUCoreLib 1.1](https://www.nexusmods.com/scavprototype/mods/341) in `BepInEx/plugins`.
3. Extract the Guardian Drone ZIP into the game folder. The final plugin path is `BepInEx/plugins/GuardianDrone/GuardianDrone.dll`.
4. Enter a run and keep the free Guardian drone controller in your carried inventory. If the inventory was full, look at your feet.

Only the Guardian DLL and documentation are included. The dependencies are separate downloads.

### Controls

- **F6** — deploy / recall. The controller also has a Use action.
- **F7** — escort fire / ceasefire. Medical support stays enabled.
- **F8** — prioritize a supported external wound.

Dropping the controller recalls the drone. A controller still lying in the world prevents another free grant. If it is completely destroyed, one replacement is issued after a short delay; another replacement requires reloading the character or changing layers.

Keys, patrol radius, damage, shot interval, repair rates and drone volume are in `BepInEx/config/local.cu.guardiandrone.cfg`. Set volume to zero to mute the drone.

### Medical and combat limits

The green beam means external wound treatment. It **does not restore lost blood**, regrow limbs, treat prosthetics, fractures, internal bleeding, organ injuries, infection, toxins, oxygen loss or brain damage. Embedded fragments still need removal.

Target selection uses a limited list of native creature types, distance, and some sight checks. It can engage a nearby creature before that creature attacks you. This version has no complete hostility/aggro tracking. Press F7 when you want to avoid a fight.

### Compatibility and known limits

- Tested as a single-player mod. Multiplayer is unverified.
- Actual damage was checked against wallbiter, including an enabled-AI target. Guards and turret classes exist in the selection code but have not been verified in live tests.
- Wayfarer Mech is optional. The code contains an integration, but the 0.2.0 preflight did not test that combination.
- The status panel can overlap the vanilla item label at the top left, particularly in a smaller window.
- Final preflight was muted. Audio is present in the code, but sound quality and audibility were not audited there.
- Other game builds, large combat groups and long-session performance remain unverified.
- Old save schema and item IDs are retained. The final preflight is a 0.2.0 save/reload test, not a separate 0.1.0-to-0.2.0 migration test.

### Upgrade or uninstall

Close the game and back up saves before changing versions. For an upgrade, replace the old DLL and keep only one Guardian Drone plugin copy. Before uninstalling, recall the drone, remove the controller, save normally and keep the backup. A save containing custom items may require their mod and CUCoreLib to load.

### Source, credits and development disclosure

[Source repository](https://github.com/dumplingsup/casualties-unknown-guardian-drone) · [0.2.0 source revision](https://github.com/dumplingsup/casualties-unknown-guardian-drone/tree/795c9b7094b18355494dbb7402db550d474cbe5c) · [Test record](https://github.com/dumplingsup/casualties-unknown-guardian-drone/blob/main/docs/V0.2.0.md).

OpenAI Codex produced the mod code, pixel placement script, procedural audio code and documentation under the author's direction. The original drone sprites are rendered from that editable script; no third-party drone sprite is distributed. The English cover and gallery use actual game captures with cropping, integer nearest-neighbour enlargement and typography composed in Pillow. No image-generation model, inpainting or synthetic voice was used for those images. The staged test arena and prepared wound are labelled in the captions.

Visual study references: [CraftPix / Free Game Assets](https://free-game-assets.itch.io/free-drones-pack-pixel-art), [Elthen](https://elthen.itch.io/2d-pixel-art-robotic-drone-sprites), [Papoy Core](https://papoycore.itch.io/free-drone). Those reference images are not included. Casualties: Unknown, Unity, BepInEx, Harmony and CUCoreLib remain the work of their respective authors.

## Changelog field

Initial Nexus beta, based on Guardian Drone 0.2.0. Adds pre-fire lane checks and repositioning, reachable external-wound treatment with medical priority, space-dependent stowing, clearer quadcopter pixels, status display, procedural sounds, and a free replacement-controller path. Compared with 0.1.0, the already-fired projectiles still retain friendly fire. The Nexus ZIP changes documentation only; its plugin DLL is the same byte-for-byte 0.2.0 binary.

## Screenshot captions

- Main cover, `images/cover-screenshots-en.png`: English typography with a detail from a real Guardian Drone 0.2.0 game capture. Staged test arena, original game lighting, cropped and enlarged. No image-generation model used.
- Gallery, `images/01-patrol-en.png`: Guardian Drone 0.2.0 patrolling in a staged in-game test arena. Screenshot detail, original game lighting, cropped and enlarged.
- Gallery, `images/02-treatment-en.png`: External-wound treatment in an isolated test run. The wound was prepared by a test fixture; the beam and treatment are the running mod. Screenshot detail, cropped and enlarged.
- Unedited capture sources remain `images/01-patrol.png` and `images/02-treatment.png`; their hashes and crop provenance are in `images/media-provenance.json`.

## Policy references checked on 2026-11-16

Nexus already requires basic AI disclosure, including new uploads during its transition period. Follow the fields shown by the live upload form. Sources: [AI policy](https://help.nexusmods.com/article/180-ai-policy-changes-2026), [file submission guidelines](https://help.nexusmods.com/article/28-file-submission-guidelines). Observed publication and site-scan results are recorded separately in [PUBLISH-STATUS.md](PUBLISH-STATUS.md).

## Reproduce the upload archive

Place the frozen [GitHub 0.2.0 player ZIP](https://github.com/dumplingsup/casualties-unknown-guardian-drone/releases/download/v0.2.0/GuardianDrone-0.2.0.zip) at `artifacts/GuardianDrone-0.2.0.zip`, then run `python tools/prepare_nexus.py`. The helper requires the published ZIP and DLL hashes, reads that DLL directly from the archive, and refuses to overwrite an existing Nexus ZIP. Do not substitute a fresh build from `bin` for the tested binary. The manifest and checksums beside the output record the documentation-only packaging change.
