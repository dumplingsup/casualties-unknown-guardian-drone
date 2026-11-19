# Guardian Drone 0.3.0

Casualties: Unknown, checked against Steam build **26666273**. Requires BepInEx 5
(tested with 5.4.23.6) and CUCoreLib **1.1**. Single player only. Other builds,
multiplayer, and unlisted mod combinations are unverified.

0.3.0 reworks combat and treatment priority. F8 now cycles **Auto → Defense →
Medical**. Auto fights an active target and treats wounds when clear; a critical
bleed threshold still overrides a fight. Defense keeps fighting while a supported
hostile is visible on screen and pauses treatment the moment one appears, then
resumes after the screen has been clear for a moment. Medical keeps treatment
first even under fire. The update also fixes a treatment latch that cleared the
combat target, and enemy recognition for guards, turrets and `CrystalEnemy`
(the crystal enemy's collider is on the Descriptor layer; guard and turret
prefabs are marked indestructible but handle their own death). The isolated test
record is in [the 0.3.0 record](https://github.com/dumplingsup/casualties-unknown-guardian-drone/blob/main/docs/V0.3.0.md).

0.1.0 kept firing through the player and could fail to reach a wound in a cave.
That package remains available as the old test build. 0.2.0 checks the lane first
and waits when it has no open cell. What 0.2.0 actually passed is in
[the 0.2.0 test record](https://github.com/dumplingsup/casualties-unknown-guardian-drone/blob/main/docs/V0.2.0.md).
0.2.1 fixed duplicate controllers after save-anywhere loads and the layer-transition
hang; details are in [the 0.2.1 fix record](https://github.com/dumplingsup/casualties-unknown-guardian-drone/blob/main/docs/V0.2.1.md).

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
- **F8**: cycle support priority: **Auto → Defense → Medical**.

An icon at the top left shows escort or ceasefire and the current priority separately
from what the drone is doing: patrol, defend, reposition, repair, waiting, or blocked.
When treatment is paused because a threat is on screen, the second line says so.

The drone is indestructible and uses no batteries, ammunition, or medicine. In open
space it stays about **6–9** world units away. A low cave uses a closer side position.
With no open cell it stows and returns only onto a free cell.

It shoots nearby supported threats. The supported list now includes spiders, cave
ticks, laser drones, guards, turrets, ceiling turrets and crystal enemies; a trader
is treated as hostile only after its own hostility flag reaches the hostile state.
Rounds have travel time and a little spread. Selection uses creature type, distance,
engagement range, and sight checks; it can start a fight with a nearby supported
creature before that creature attacks. If you or a bystander is already in the lane,
it holds fire and tries to move. **A round that has already left the gun can still
hit you.** Press F7 to cease fire.

Auto treats you when no supported target is engaging, and returns to combat if one
appears. A total bleed of 10 or more overrides that and forces treatment so you do
not bleed out during a fight; the threshold is configurable. Defense uses the
player's screen as the boundary: a supported hostile visible on screen, within
about 30 world units and with line of sight, holds the drone in combat and pauses
treatment. It resumes after the screen has been clear for about one second.
Medical keeps treatment first even under fire. Light scrapes do not pull it into
your face. The green beam appears only while a clear, in-range repair is increasing.
It stops external bleeding and restores skin and muscle on existing organic limbs.
It does not restore lost blood or treat internal bleeding, fractures, dislocations,
infections, toxins, oxygen loss, brain damage, prosthetics, or missing limbs.

Deployment, ceasefire and the Auto/Defense/Medical priority are saved with the
character. Descending or loading recalls the drone as soon as the descent panel
appears; it stays out until the new layer is loaded and settled for about 2.5
seconds, then redeploys near you. It does not restore the old flight position.
Old 0.2.1 saves have no priority field and load as Auto.

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
and pauses treatment of the hidden body. That combination was not in the 0.3.0
isolated run.

`BepInEx/config/local.cu.guardiandrone.cfg` has the keys, patrol distance, damage,
shot interval, repair rates, the Auto critical-bleed threshold, and volume. Zero
volume mutes the drone.

Back up saves before removing the mod. Multiplayer, mech co-op and unlisted
creatures were not part of this run. Guards, turrets and crystal enemies were
recognized in the 0.3.0 isolated copy; a hostile trader was wired but not spawned
in-game. The archive has no test probe, game assemblies, or third-party art.

For a manual upgrade, close the game and replace the existing GuardianDrone.dll;
keep only one copy. Before uninstalling, recall the drone and remove its controller,
then save and keep a backup. Saves with custom items may require the mod to load.
The HUD may overlap the vanilla item label at the top left, particularly in a
smaller window. Sound quality has not been audited in the final preflight, which
runs muted.
