"""Export the reviewed English listing to pasteable BBCode and plain text."""
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "release/nexus-0.2.0/NEXUS.md"

def inline(text):
    text = re.sub(r"\[([^\]]+)\]\((https?://[^)]+)\)", r"[url=\2]\1[/url]", text)
    text = re.sub(r"\*\*(.+?)\*\*", r"[b]\1[/b]", text)
    return text.replace("`", "")

def main():
    source = SOURCE.read_text(encoding="utf-8")
    body = source.split("## Main description — English\n", 1)[1].split("## Changelog field", 1)[0].strip()
    lines = []
    in_list = False
    for line in body.splitlines():
        is_list = line.startswith("- ") or bool(re.match(r"^\d+\. ", line))
        if in_list and not is_list:
            lines.append("[/list]")
            in_list = False
        if is_list:
            if not in_list:
                lines.append("[list=1]" if re.match(r"^\d+\. ", line) else "[list]")
                in_list = True
            content = re.sub(r"^(?:- |\d+\. )", "", line)
            lines.append("[*]" + inline(content))
        elif line.startswith("### "):
            lines.append("[b]" + inline(line[4:]) + "[/b]")
        else:
            lines.append(inline(line))
    if in_list:
        lines.append("[/list]")
    out = ROOT / "artifacts/nexus-0.2.0"
    out.mkdir(exist_ok=True)
    (out / "DESCRIPTION.en.bbcode.txt").write_text("\n".join(lines) + "\n", encoding="utf-8")
    (out / "DESCRIPTION.en.md").write_text(body + "\n", encoding="utf-8")
    print(f"Exported {len(body)} characters of reviewed English body.")

if __name__ == "__main__":
    main()
