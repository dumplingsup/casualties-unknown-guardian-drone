# Guardian Drone 0.2.0

Casualties: Unknown, checked against Steam build **26666273**. Requires BepInEx 5
(tested with 5.4.23.6) and CUCoreLib **1.1**. Single player only. Other builds,
multiplayer, and unlisted mod combinations are unverified.

0.1.0 kept firing through the player and could fail to reach a wound in a cave.
That package remains available as the old test build. 0.2.0 checks the lane first
and waits when it has no open cell. What this version actually passed is in
[docs/V0.2.0.md](docs/V0.2.0.md).

Merge the archive's `BepInEx` folder into the game directory. The mod file belongs at
`BepInEx/plugins/GuardianDrone/GuardianDrone.dll`.

Your character receives one free controller after entering a run or loading a save.
A second one is not granted while any controller already exists. If it is truly gone,
one replacement is issued after a few seconds. Keep it in your carried inventory.
Dropping it recalls the drone and says why.

- Use the controller or **F6**: deploy / recall.
- **F7**: escort fire / ceasefire. Automatic medical support remains enabled.
- **F8**: prioritize supported external wounds.

An icon at the top left shows escort or ceasefire separately from what the drone is
doing: patrol, reposition, repair, waiting, or blocked.

The drone is indestructible and uses no batteries, ammunition, or medicine. In open
space it stays about **6–9** world units away. A low cave uses a closer side position.
With no open cell it stows and returns only onto a free cell.

It shoots nearby supported threats. Rounds have travel time and a little spread.
If you or a bystander is already in the lane, it holds fire and tries to move.
**A round that has already left the gun can still hit you.** Press F7 to cease fire.

Severe bleeding, unconsciousness, or F8 keeps it on medical support until the
bleeding has actually fallen. Light scrapes do not pull it into your face. The green
beam appears only while a clear, in-range repair is increasing. It stops external
bleeding and restores skin and muscle on existing organic limbs. It does not restore
lost blood or treat internal bleeding, fractures, dislocations, infections, toxins,
oxygen loss, brain damage, prosthetics, or missing limbs.

Deployment and ceasefire are saved with the character. Loading creates one drone
near you; it does not restore the old flight position. Wayfarer Mech can be installed
beside it. While you pilot, the drone uses the mech and pauses treatment of the
hidden body. That combination was not in the 0.2.0 isolated run.

`BepInEx/config/local.cu.guardiandrone.cfg` has the keys, patrol distance, damage,
shot interval, repair rates, and volume. Zero volume mutes the drone.

Back up saves before removing the mod. Multiplayer, guards, turrets, and unknown
creatures were not part of this run. The archive has no test probe, game assemblies,
or third-party art.
