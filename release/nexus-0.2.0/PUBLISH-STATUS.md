# Nexus publication status — 2026-11-16

**Not uploaded or published. Waiting at the Nexus Cloudflare human-verification step.**

The user authorized uploading and publishing Guardian Drone with English copy and a screenshot-based English cover. That authorization remains active. The separate confirmation request for clicking the displayed CAPTCHA is pending; no CAPTCHA interaction has been attempted.

## Checked workflow

Reviewed the earlier chat **完成机甲 Mod 英文发布** and local Wayfarer 0.4.9 release materials. The previous mod is [Wayfarer Mech #907](https://www.nexusmods.com/scavprototype/mods/907), under the Nexus account `nicechynice`. Local preparation records predate the later public page, so their old “not published” status is not a statement about the current mech page.

Mirrored the useful release structure: mod-only ZIP, English description, separate BepInEx/CUCoreLib requirements, actual game captures with English typography, honest test limits, AI development disclosure, and permission-required asset reuse. No other author's mod or the mech page was edited.

## Current browser state

- Local Chrome is signed in as `nicechynice`, matching the previous mod.
- Native window accessibility exposed the full Nexus URL and profile menu. The tab connector itself timed out; window control worked after bringing the browser forward.
- Opening **My mods** reached `https://www.nexusmods.com/scavprototype/users/myaccount?tab=mods` and displayed Cloudflare's “请验证您是真人” checkbox.
- No Guardian draft, file upload or form submission has been made.
- After the verification is resolved, inspect existing mods/drafts to avoid duplicates, use Upload to create the Guardian page, and follow the live form. Do not alter Wayfarer Mech.

## Ready files

- `artifacts/nexus-0.2.0/GuardianDrone-0.2.0-Nexus.zip` — SHA-256 `5a6b6e480203e0a9afc9350bf2c7730c6dd4350cd561def5d42c90eeac5c3138`.
- `artifacts/nexus-0.2.0/DESCRIPTION.en.bbcode.txt` and `DESCRIPTION.en.md` — only the reviewed public English description, with internal upload notes excluded.
- `artifacts/nexus-0.2.0/images/cover-screenshots-en.png` — 1920×1080 English cover.
- `artifacts/nexus-0.2.0/images/01-patrol-en.png` and `02-treatment-en.png` — 1600×1000 English gallery layouts.
- `artifacts/nexus-0.2.0/images/media-provenance.json` — original capture hashes and output hashes; source captures retained beside the layouts.

The cover and gallery use real gameplay captures, cropping, integer nearest-neighbour enlargement and flat text/layout. No image-generation model, inpainting or reconstruction was used. Scene and wound fixtures are labelled. The original DLL and player ZIP have not changed during media preparation.

## Resume at the live site

1. Confirm that the visible challenge has been resolved, or obtain the specific CAPTCHA confirmation before interacting with it.
2. Check the author mod list for an existing Guardian draft; otherwise create a new page for Casualties: Unknown (`scavprototype`).
3. Use the fields and English body in [NEXUS.md](NEXUS.md), upload the ZIP, cover and gallery, and add CUCoreLib plus BepInEx requirements.
4. Keep this a Windows single-player beta. Do not promise complete hostility recognition, all enemies, multiplayer or tested Vortex installation.
5. Review the actual AI disclosure and permission fields. The cover contains game captures, while code and copy have the documented Codex involvement.
6. Publish under the user's existing authorization once the live page and files are correct, subject to any required action-time confirmation shown by the controlling tool. Record the actual mod URL, file details and observed visibility. Do not mark publication complete from an upload attempt alone.
