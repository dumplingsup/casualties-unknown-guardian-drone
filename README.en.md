# Guardian Drone 0.2.1

Casualties: Unknown, checked against Steam build **26666273**. Requires BepInEx 5
(tested with 5.4.23.6) and CUCoreLib **1.1**. Single player only. Other builds,
multiplayer, and unlisted mod combinations are unverified.

0.1.0 kept firing through the player and could fail to reach a wound in a cave.
That package remains available as the old test build. 0.2.0 checks the lane first
and waits when it has no open cell. What 0.2.0 actually passed is in
[the 0.2.0 test record](https://github.com/dumplingsup/casualties-unknown-guardian-drone/blob/main/docs/V0.2.0.md).

0.2.1 fixes two Nexus bug reports: save-anywhere style mods could leave a duplicate
controller after loading, and descending a layer with the drone deployed could hang
the game. The drone now recalls as soon as the descent panel appears, stays out until
the new layer is fully loaded and settled, then redeploys. Details and the isolated
test record are in [the 0.2.1 fix record](https://github.com/dumplingsup/casualties-unknown-guardian-drone/blob/main/docs/V0.2.1.md).

Merge the archive's `BepInEx` folder into the game directory. The mod file belongs at
`BepInEx/plugins/GuardianDrone/GuardianDrone.dll`.

Your character receives one free controller after entering a run or loading a save.
A controller in the hotbar, a worn container, or a storage box all counts as owned,
so a second one is not granted. If it is truly gone, a replacement is issued after a
few seconds, but not within 12 seconds of loading a save so the save mod can finish
restoring the layer. If the original appears after a replacement was issued, the mod
removes the one it created. Keep it in your carried inventory. Dropping it recalls
the drone and says why.

- Use the controller or **F6**: deploy / recall.
- **F7**: escort fire / ceasefire. Automatic medical support remains enabled.
- **F8**: prioritize supported external wounds.

An icon at the top left shows escort or ceasefire separately from what the drone is
doing: patrol, reposition, repair, waiting, or blocked.

The drone is indestructible and uses no batteries, ammunition, or medicine. In open
space it stays about **6–9** world units away. A low cave uses a closer side position.
With no open cell it stows and returns only onto a free cell.

It shoots nearby supported threats. Rounds have travel time and a little spread.
Threat selection currently uses creature type, distance, and some sight checks;
it can start a fight with a nearby supported creature before that creature attacks.
If you or a bystander is already in the lane, it holds fire and tries to move.
**A round that has already left the gun can still hit you.** Press F7 to cease fire.

Severe bleeding, unconsciousness, or F8 keeps it on medical support until the
bleeding has actually fallen. Light scrapes do not pull it into your face. The green
beam appears only while a clear, in-range repair is increasing. It stops external
bleeding and restores skin and muscle on existing organic limbs. It does not restore
lost blood or treat internal bleeding, fractures, dislocations, infections, toxins,
oxygen loss, brain damage, prosthetics, or missing limbs.

Deployment and ceasefire are saved with the character. Descending or loading recalls
the drone as soon as the descent panel appears; it stays out until the new layer is
loaded and settled for about 2.5 seconds, then redeploys near you. It does not restore
the old flight position.

Tested alongside Save Anywhere 1.5.0: a controller in the hotbar or a carried
container still loads as exactly one. A controller left on the ground or in a world
container cannot be restored by Save Anywhere's layer snapshot because it only
resolves vanilla resources; the mod issues one replacement after a 12 second safety
window, still leaving exactly one.

Also tested with **QoL Unknown 7 v1.1.4**, the save mod from the bug report: its
in-depth save restores the custom controller as an inactive clone. 0.2.1 finds and
activates that clone instead of issuing a second controller. Descending with the
drone deployed no longer hangs.

Wayfarer Mech can be installed beside it. While you pilot, the drone uses the mech
and pauses treatment of the hidden body. That combination was not in the 0.2.0
isolated run.

`BepInEx/config/local.cu.guardiandrone.cfg` has the keys, patrol distance, damage,
shot interval, repair rates, and volume. Zero volume mutes the drone.

Back up saves before removing the mod. Multiplayer, guards, turrets, and unknown
creatures were not part of this run. The archive has no test probe, game assemblies,
or third-party art.

For a manual upgrade, close the game and replace the existing GuardianDrone.dll;
keep only one copy. Before uninstalling, recall the drone and remove its controller,
then save and keep a backup. Saves with custom items may require the mod to load.
The 0.2.0 HUD may overlap the vanilla item label at the top left. Sound quality has
not been audited in the final preflight, which runs muted.
