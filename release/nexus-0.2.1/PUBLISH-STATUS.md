# Nexus publication status — 0.2.1

Prepared 2026-11-18. **Nexus submission not performed.** The author chose to update the Nexus page manually; this file is the handoff record.

## Published elsewhere

- GitHub Release: [v0.2.1](https://github.com/dumplingsup/casualties-unknown-guardian-drone/releases/tag/v0.2.1).
- Uploaded asset: `GuardianDrone-0.2.1.zip`, asset ID **761553838**, 32,114 bytes.
- Asset SHA-256: `15b7214b73d351ea16556d6766dd4d0dd8a47ec1b9be5d0b24db22e6cf3c0fc7`.
- Source commit: `6c90cb1b5c8fe008934ba06ac5614bf11361d22a`.
- GitHub release notes carry the tested DLL hash and the 0.2.1 scope.

## Nexus handoff

Target: [Guardian Drone - Combat and Medical Companion](https://www.nexusmods.com/scavprototype/mods/985), mod ID 985, existing file ID 3300. This is a version/file replacement of the existing page, not a new mod page.

Upload archive:

- `artifacts/nexus-0.2.1/GuardianDrone-0.2.1-Nexus.zip`
- SHA-256: `c22db749d46f237d82e95834934d663162c85b7b8e251374d659df6d6647d1c1`
- DLL SHA-256: `51f49c000a57c6b4ef25960ce877a9543bb5a164f8403d1e4531f7637d33a380`
- Includes the DLL, updated English/Chinese player readmes and NOTICE only.

Form values and text:

- Version: `0.2.1`
- File title: `Guardian Drone 0.2.1 - Single-player Beta`
- Description body: `release/nexus-0.2.1/NEXUS.md` → `artifacts/nexus-0.2.1/DESCRIPTION.en.md` / `DESCRIPTION.en.bbcode.txt`
- Changelog: the "Changelog field" section of `release/nexus-0.2.1/NEXUS.md`
- Keep the existing gallery and cover; no new media upload is required.
- Keep the existing CUCoreLib 1.1 requirement, BepInEx 5.4.23.6 off-site requirement, AI disclosure, permissions and category.
- Edit/replace file 3300 so the page keeps one Main file; do not add a second file.

## To verify after the manual update

- Page version reads 0.2.1 and the Main file still shows one file (ID 3300), with the new archive.
- File title is `Guardian Drone 0.2.1 - Single-player Beta`.
- Virus scan field and file size are observed on the live page.
- Description/changelog mention the two fixed reports and the tested Save Anywhere 1.5.0 / QoL Unknown 7 v1.1.4 versions.
- The 0.2.0 archive, 0.1.0 archive and earlier tags are not replaced by this update.

The Nexus page is still 0.2.0 until this manual step is completed.
