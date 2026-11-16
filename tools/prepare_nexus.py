"""Prepare a Nexus ZIP from the frozen, tested 0.2.0 release DLL; no build or deploy."""
import hashlib
import json
from pathlib import Path
import zipfile

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "artifacts/GuardianDrone-0.2.0.zip"
ZIP_SHA = "0b1a5e73c9ca1b5c710dae0c4bed16ec0cd88d5f4784bb576e1abcf86191ce1e"
DLL_SHA = "0c0af71b8a4a3fe97892281235aa513749a505f6d14c9b51966373cc26a9bc59"
DLL_NAME = "BepInEx/plugins/GuardianDrone/GuardianDrone.dll"
SOURCE_COMMIT = "795c9b7094b18355494dbb7402db550d474cbe5c"

def sha(data):
    return hashlib.sha256(data).hexdigest()

def main():
    if sha(SOURCE.read_bytes()) != ZIP_SHA:
        raise SystemExit("Source release ZIP does not match the frozen 0.2.0 release.")
    with zipfile.ZipFile(SOURCE) as package:
        if package.testzip() is not None:
            raise SystemExit("Source archive CRC failed.")
        dll = package.read(DLL_NAME)
    if sha(dll) != DLL_SHA:
        raise SystemExit("Source DLL does not match the tested 0.2.0 DLL.")
    out = ROOT / "artifacts/nexus-0.2.0"
    out.mkdir(exist_ok=True)
    archive = out / "GuardianDrone-0.2.0-Nexus.zip"
    payload = {DLL_NAME: dll}
    for name in ["README.en.md", "README.zh-CN.md", "NOTICE.txt"]:
        # Canonical LF keeps Windows autocrlf checkouts byte-identical to the ZIP.
        payload[name] = (ROOT / name).read_text(encoding="utf-8").encode("utf-8")
    with zipfile.ZipFile(archive, "x", zipfile.ZIP_DEFLATED) as package:
        for name, data in payload.items():
            entry = zipfile.ZipInfo(name, (2026, 11, 16, 0, 0, 0))
            entry.compress_type = zipfile.ZIP_DEFLATED
            package.writestr(entry, data)
    with zipfile.ZipFile(archive) as package:
        if package.testzip() is not None or set(package.namelist()) != set(payload):
            raise SystemExit("Prepared archive integrity check failed.")
        if any(package.read(name) != data for name, data in payload.items()):
            raise SystemExit("Prepared archive content differs from inputs.")
    manifest = {
        "mod": "local.cu.guardiandrone", "version": "0.2.0", "game_build": "26666273",
        "preparation_date": "2026-11-16", "dll_source_commit": SOURCE_COMMIT,
        "original_release_zip_sha256": ZIP_SHA, "archive": archive.name,
        "archive_sha256": sha(archive.read_bytes()), "files": {name:sha(data) for name,data in payload.items()},
        "changes_from_github_zip": "Documentation only; the DLL is byte-identical.",
        "nexus_submission_status": "NOT_RUN",
    }
    (out / "build-info.json").write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
    (out / "SHA256SUMS.txt").write_text(f"{manifest['archive_sha256']}  {archive.name}\n{DLL_SHA}  GuardianDrone.dll\n", encoding="ascii")
    print(json.dumps(manifest, indent=2))

if __name__ == "__main__":
    main()
