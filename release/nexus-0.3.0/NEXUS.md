# Guardian Drone 0.3.0 — Nexus update material

Prepared 2026-11-19 for Nexus mod **985**. This is a file/version update of the existing 0.2.0 page, not a new mod page. The actual publication result is recorded separately in `PUBLISH-STATUS.md`.

## Form fields

- Game: Casualties: Unknown (Former Scav Prototype), site slug `scavprototype`.
- Title: Guardian Drone - Combat and Medical Companion (unchanged)
- Version: 0.3.0
- File title: Guardian Drone 0.3.0 - Single-player Beta
- File: `artifacts/nexus-0.3.0/GuardianDrone-0.3.0-Nexus.zip`
- Archive SHA-256: `f33b12be43d878f32be6fe586bc055c74b06260b55f4ee38180968ee7e97644c`
- DLL SHA-256: `042ff53765ce8ca9f2af8ad797945c707b784800e1d82ba4870d4d1cb2e83b6f`
- Existing file ID: **3300** (edit/replace to keep one Main file; do not create a second file)
- Summary: A free, indestructible drone companion. 0.3.0 adds Auto/Defense/Medical priority on F8, stops treatment from overriding an active fight, and fixes guard/crystal/turret recognition.
- Actual category: Miscellaneous (unchanged).
- In-game languages: English, Simplified Chinese (unchanged).
- Installation: manual, Windows x64 (unchanged).
- Nexus requirement: [CUCoreLib](https://www.nexusmods.com/scavprototype/mods/341), tested version 1.1 (unchanged).
- Off-site requirement: [BepInEx 5.4.23.6, Windows x64](https://github.com/BepInEx/BepInEx/releases/tag/v5.4.23.6) (unchanged).
- AI disclosure: AI-Generated Content (most code and scripted sprite production); AI Media for drafted page text where offered. Do not select No AI.
- Permissions: unchanged from 0.2.0 (ask me / no re-upload, conversion or monetisation).
- Do not claim multiplayer, Steam Deck/Linux, all enemies, complete medical treatment, Vortex support, or a tested Wayfarer combination.

## Main description — English

Guardian Drone adds a free quadcopter companion for your descent. Carry its controller to deploy it, let it patrol nearby, and call it in to treat external wounds.

This is a **single-player beta for Windows x64**, tested with Steam build **26666273**, BepInEx **5.4.23.6** and CUCoreLib **1.1**.

### What changed in 0.3.0

- **F8 now cycles Auto / Defense / Medical priority.** Auto fights an active target and treats you when the area is clear; a critical-bleed threshold still forces treatment. Defense keeps fighting while a supported enemy is visible on your screen and pauses treatment the moment one appears; it resumes after the screen has been clear for about a second. Medical keeps treatment first even under fire.
- **Treatment no longer silently disables the drone.** The old treatment latch cleared the combat target, so a single wound could stop it shooting for most of a fight. The drone now holds the target, stops treatment when combat takes priority, and resumes after the threat clears.
- **Enemy recognition fixes.** Crystal enemies were on a layer the drone never scanned; guards and turrets are marked indestructible only to bypass the generic destruction path and were being ignored. All three are now recognized. A trader is treated as hostile only after its hostility flag becomes hostile.
- **Priority is saved with the character.** Old 0.2.1 saves have no priority field and load as Auto.
- Tested with a real 0.2.1 checkpoint as well as a fresh 0.3.0 save. The isolated test record is in the source repository's `docs/V0.3.0.md`.

### What it does

- Follows the player and stays roughly 6–9 world units away in open spaces. Low caves use closer side positions.
- Fires at nearby supported threats. It checks the firing lane before shooting and attempts to move when a body or obstacle blocks the shot.
- Keeps physical projectiles: **a round already in flight can still hit you or a bystander**. F7 toggles ceasefire.
- Moves close to reduce external bleeding and repair skin and muscle on existing organic limbs. F8 selects the support priority.
- Shows escort/ceasefire, Auto/Defense/Medical priority and the current activity in a small status panel. When treatment is paused by an on-screen threat, the panel says so.
- Stows while no usable flight position is available, then redeploys when space opens.
- Saves deployment intent, ceasefire and support priority with the character.

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
- **F8** — cycle support priority: Auto → Defense → Medical.

Dropping the controller recalls the drone. A controller in the hotbar, a worn container or a storage box counts as owned. If it is completely destroyed, a replacement is issued after a short delay. After loading a save, the mod waits 12 seconds for the save system to finish before issuing anything. If the original appears after a replacement was issued, the mod removes the copy it created.

Keys, patrol radius, damage, shot interval, repair rates, the Auto critical-bleed threshold and drone volume are in `BepInEx/config/local.cu.guardiandrone.cfg`. Set volume to zero to mute the drone.

### Medical and combat limits

The green beam means external wound treatment. It **does not restore lost blood**, regrow limbs, treat prosthetics, fractures, internal bleeding, organ injuries, infection, toxins, oxygen loss or brain damage. Embedded fragments still need removal.

Target selection uses a limited list of native creature types, distance, engagement range, and some sight checks. It can engage a nearby creature before that creature attacks you. Press F7 when you want to avoid a fight. Defense priority uses the camera view plus line of sight and a 30 world-unit scan cap; a hostile beyond that cap does not pause treatment even if it is technically on screen.

### Compatibility and known limits

- Tested as a single-player mod. Multiplayer is unverified.
- Save Anywhere 1.5.0 and QoL Unknown 7 v1.1.4 were tested for 0.2.1; the 0.3.0 change only adds an optional save field, but their combination was not re-run for 0.3.0.
- Wayfarer Mech is optional. The code contains an integration, but the combination was not retested for 0.3.0.
- The status panel can overlap the vanilla item label at the top left, particularly in a smaller window.
- Final preflight was muted. Audio is present in the code, but sound quality and audibility were not audited there.
- Other game builds, large combat groups and long-session performance remain unverified.
- Old save schema and item IDs are retained. 0.2.1 saves can be read by 0.3.0 and default to Auto priority.

### Upgrade or uninstall

Close the game and back up saves before changing versions. For an upgrade, replace the old DLL and keep only one Guardian Drone plugin copy. Before uninstalling, recall the drone, remove the controller, save normally and keep the backup. A save containing custom items may require their mod and CUCoreLib to load.

### Source, credits and development disclosure

[Source repository](https://github.com/dumplingsup/casualties-unknown-guardian-drone) · [0.3.0 source revision](https://github.com/dumplingsup/casualties-unknown-guardian-drone/tree/2e457ff65df60607cbc7ecc8657ceb1ad77da8c4) · [0.3.0 test record](https://github.com/dumplingsup/casualties-unknown-guardian-drone/blob/main/docs/V0.3.0.md).

OpenAI Codex produced the mod code, pixel placement script, procedural audio code and documentation under the author's direction. The original drone sprites are rendered from that editable script; no third-party drone sprite is distributed. The English cover and gallery use actual game captures with cropping, integer nearest-neighbour enlargement and typography composed in Pillow. No image-generation model, inpainting or synthetic voice was used for those images. The staged test arena and prepared wound are labelled in the captions.

Visual study references: [CraftPix / Free Game Assets](https://free-game-assets.itch.io/free-drones-pack-pixel-art), [Elthen](https://elthen.itch.io/2d-pixel-art-robotic-drone-sprites), [Papoy Core](https://papoycore.itch.io/free-drone). Those reference images are not included. Casualties: Unknown, Unity, BepInEx, Harmony and CUCoreLib remain the work of their respective authors.

## Changelog field

0.3.0 adds Auto/Defense/Medical support priority on F8. Auto fights an active target and treats when clear; a critical-bleed threshold forces treatment. Defense holds combat while a supported enemy is visible on screen and pauses treatment immediately, resuming after the screen clears. Medical keeps treatment first. It also fixes treatment clearing the combat target, and recognition of crystal enemies, guards and turrets. The Nexus ZIP contains the tested DLL (`042ff5…`) and updated player documentation; the 0.2.1 archive is unchanged.

## Screenshot captions

The 0.2.0 gallery images are unchanged; this is a behavior update and does not add new media. The source repository's local test runs contain new HUD captures for review, but they are not part of the upload archive.

- Main cover, `images/cover-screenshots-en.png`: English typography with a detail from a real Guardian Drone 0.2.0 game capture. Staged test arena, original game lighting, cropped and enlarged. No image-generation model used.
- Gallery, `images/01-patrol-en.png`: Guardian Drone 0.2.0 patrolling in a staged in-game test arena. Screenshot detail, original game lighting, cropped and enlarged.
- Gallery, `images/02-treatment-en.png`: External-wound treatment in an isolated test run. The wound was prepared by a test fixture; the beam and treatment are the running mod. Screenshot detail, original game lighting, cropped and enlarged.
- Unedited capture sources remain `images/01-patrol.png` and `images/02-treatment.png`; their hashes and crop provenance are in `images/media-provenance.json`.

## Policy references checked on 2026-11-16

Nexus already requires basic AI disclosure, including new uploads during its transition period. Follow the fields shown by the live upload form. Sources: [AI policy](https://help.nexusmods.com/article/180-ai-policy-changes-2026), [file submission guidelines](https://help.nexusmods.com/article/28-file-submission-guidelines). The 0.3.0 update does not change the disclosure or permission choices.

## Reproduce the upload archive

Place the frozen 0.3.0 player ZIP at `artifacts/GuardianDrone-0.3.0.zip`, then run `python tools/prepare_nexus_030.py`. The helper requires the release ZIP hash `05229b…` and DLL hash `042ff5…`, reads the DLL directly from the archive, and refuses to overwrite an existing Nexus ZIP. Do not substitute a fresh build from `bin` for the tested binary; C# builds here are not byte-reproducible.
