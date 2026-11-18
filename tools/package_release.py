"""Zip the built mod DLL with the player readmes. Never copies into the game."""
import hashlib
import sys
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
VERSION = "0.2.1"
DLL = ROOT / "src/GuardianDrone/bin/Release/net48/GuardianDrone.dll"
ENTRIES = {
    f"BepInEx/plugins/GuardianDrone/GuardianDrone.dll": DLL,
    "README.zh-CN.md": ROOT / "README.zh-CN.md",
    "README.en.md": ROOT / "README.en.md",
    "NOTICE.txt": ROOT / "NOTICE.txt",
}


def digest(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def main() -> int:
    missing = [str(path) for path in ENTRIES.values() if not path.is_file()]
    if missing:
        print("missing files:\n" + "\n".join(missing), file=sys.stderr)
        return 1
    out = ROOT / "artifacts"
    out.mkdir(exist_ok=True)
    archive = out / f"GuardianDrone-{VERSION}.zip"
    if archive.exists():
        print(f"refuse to overwrite {archive}", file=sys.stderr)
        return 1
    payload = {name: path.read_bytes() for name, path in ENTRIES.items()}
    with zipfile.ZipFile(archive, "x", zipfile.ZIP_DEFLATED) as package:
        for name, data in payload.items():
            package.writestr(name, data)
    with zipfile.ZipFile(archive) as package:
        names = package.namelist()
        if set(names) != set(payload):
            raise SystemExit(f"unexpected archive entries: {names}")
        dlls = [name for name in names if name.endswith(".dll")]
        if dlls != ["BepInEx/plugins/GuardianDrone/GuardianDrone.dll"]:
            raise SystemExit(f"unexpected dll entries: {dlls}")
    sums = out / "SHA256SUMS.txt"
    lines = [
        f"{digest(archive.read_bytes())}  {archive.name}",
        f"{digest(payload['BepInEx/plugins/GuardianDrone/GuardianDrone.dll'])}  GuardianDrone.dll",
    ]
    sums.write_text("\n".join(lines) + "\n", encoding="ascii")
    print("\n".join(lines))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
