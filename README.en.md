# Guardian Drone 0.1.0 — prototype

Casualties: Unknown, verified against Steam build **26666273**. Requires BepInEx 5
(tested with 5.4.23.6) and CUCoreLib **1.1**. Single player only. Other builds and
unlisted mod combinations are unverified.

**Known serious issues in 0.1.0:** the drone keeps firing when the player blocks
its shot, can fail to reach a valid treatment position in caves, and has no
persistent ceasefire indicator. Recovery also fails in some cases with no hover
position. Companion reliability has not passed acceptance. See the
[review and proposed changes](docs/PLAYABILITY-REVIEW.md) (Chinese). The changes
are not implemented. Physical F6/F7/F8 input remains untested.

Merge the archive's `BepInEx` folder into the game directory. The mod file belongs at
`BepInEx/plugins/GuardianDrone/GuardianDrone.dll`.

Your character receives a free controller after entering a run or loading a save.
If your inventory is full, it appears at your feet. Keep it in your carried inventory.

- Use the controller or **F6**: deploy / recall.
- **F7**: escort fire / ceasefire. Automatic medical support remains enabled.
- **F8**: prioritize supported external wounds.

The drone is indestructible and uses no batteries, ammunition or medicine. In open
space it patrols roughly 6–9 world units from you along an overhead arc. It adjusts
to cave clearance and attempts to redeploy if stuck, with the recovery limitation
above. It cannot physically push the player.

It targets supported native SpiderHandler creatures, CaveTicks and LaserDroneScript
entities. Unknown modded NPCs are not automatically selected. Rounds have travel time
and slight spread: **the player and other bystanders can be hit**. Terrain absorbs
rounds without being destroyed.

At close range and with clear sight, a medical beam gradually reduces external
bleeding and restores skin/muscle on existing organic limbs. Severe bleeding takes
priority over shooting. It does not restore lost blood or treat internal bleeding,
fractures, dislocations, infections, toxins, oxygen loss, brain damage, prosthetics
or missing limbs. Embedded fragments still need removal.

Deployment intent and guard mode are stored through a CUCoreLib body save provider.
Loading recreates one drone near the owner. The flight position and current target
are transient. Layer transitions are supported.

Wayfarer Mech is optional. When piloted, the drone follows the visible mech and
temporarily suspends treatment of the hidden pilot. Multiplayer is unverified.

Controls and support rates are configurable in
`BepInEx/config/local.cu.guardiandrone.cfg`.

Back up saves before changing/removing mods. Recall the drone and remove the custom
controller before saving for uninstallation; saves containing custom items are not
guaranteed to load without the appropriate mod and dependencies.

This archive contains no test probe, game assemblies or third-party sprite assets.
