"""Prepare the Nexus ZIP from the frozen 0.2.1 release DLL; no build or deploy."""
import hashlib
import json
from pathlib import Path
import zipfile

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "artifacts/GuardianDrone-0.2.1.zip"
ZIP_SHA = "15b7214b73d351ea16556d6766dd4d0dd8a47ec1b9be5d0b24db22e6cf3c0fc7"
DLL_SHA = "51f49c000a57c6b4ef25960ce877a9543bb5a164f8403d1e4531f7637d33a380"
DLL_NAME = "BepInEx/plugins/GuardianDrone/GuardianDrone.dll"
SOURCE_COMMIT = "6c90cb1b5c8fe008934ba06ac5614bf11361d22a"


def sha(data):
    return hashlib.sha256(data).hexdigest()


def main():
    if sha(SOURCE.read_bytes()) != ZIP_SHA:
        raise SystemExit("Source release ZIP does not match the frozen 0.2.1 release.")
    with zipfile.ZipFile(SOURCE) as package:
        if package.testzip() is not None:
            raise SystemExit("Source archive CRC failed.")
        dll = package.read(DLL_NAME)
    if sha(dll) != DLL_SHA:
        raise SystemExit("Source DLL does not match the tested 0.2.1 DLL.")
    out = ROOT / "artifacts/nexus-0.2.1"
    out.mkdir(exist_ok=True)
    archive = out / "GuardianDrone-0.2.1-Nexus.zip"
    if archive.exists():
        raise SystemExit(f"refuse to overwrite {archive}")
    payload = {DLL_NAME: dll}
    for name in ["README.en.md", "README.zh-CN.md", "NOTICE.txt"]:
        # Canonical LF keeps Windows autocrlf checkouts byte-identical to the ZIP.
        payload[name] = (ROOT / name).read_text(encoding="utf-8").encode("utf-8")
    with zipfile.ZipFile(archive, "x", zipfile.ZIP_DEFLATED) as package:
        for name, data in payload.items():
            entry = zipfile.ZipInfo(name, (2026, 11, 18, 0, 0, 0))
            entry.compress_type = zipfile.ZIP_DEFLATED
            package.writestr(entry, data)
    with zipfile.ZipFile(archive) as package:
        if package.testzip() is not None or set(package.namelist()) != set(payload):
            raise SystemExit("Prepared archive integrity check failed.")
        if any(package.read(name) != data for name, data in payload.items()):
            raise SystemExit("Prepared archive content differs from inputs.")
    manifest = {
        "mod": "local.cu.guardiandrone", "version": "0.2.1", "game_build": "26666273",
        "preparation_date": "2026-11-18", "dll_source_commit": SOURCE_COMMIT,
        "original_release_zip_sha256": ZIP_SHA, "archive": archive.name,
        "archive_sha256": sha(archive.read_bytes()), "files": {name: sha(data) for name, data in payload.items()},
        "changes_from_github_zip": "Documentation only; the DLL is byte-identical.",
        "nexus_submission_status": "NOT_RUN",
    }
    (out / "build-info.json").write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
    (out / "SHA256SUMS.txt").write_text(f"{manifest['archive_sha256']}  {archive.name}\n{DLL_SHA}  GuardianDrone.dll\n", encoding="ascii")
    print(json.dumps(manifest, indent=2))


if __name__ == "__main__":
    main()
