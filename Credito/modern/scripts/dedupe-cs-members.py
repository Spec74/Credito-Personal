#!/usr/bin/env python3
"""Remove duplicate C# members reported as CS0111/CS0101/CS8863 (keeps earlier definition)."""

from __future__ import annotations

import re
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SLN = ROOT / "Credito.Modern.sln"


def find_matching_brace(text: str, open_idx: int) -> int:
    depth = 0
    i = open_idx
    in_str = None
    escape = False
    while i < len(text):
        ch = text[i]
        if in_str:
            if escape:
                escape = False
            elif ch == "\\":
                escape = True
            elif ch == in_str:
                in_str = None
            i += 1
            continue
        if ch in ('"', "'"):
            in_str = ch
            i += 1
            continue
        if ch == "{":
            depth += 1
        elif ch == "}":
            depth -= 1
            if depth == 0:
                return i
        i += 1
    return open_idx


def remove_member_at_line(path: Path, line_no: int) -> bool:
    text = path.read_text(encoding="utf-8")
    lines = text.splitlines(keepends=True)
    start = sum(len(lines[i]) for i in range(line_no - 1))
    # walk back to member start
    i = start
    while i > 0 and text[i - 1] not in "\n":
        i -= 1
    while i > 0:
        prev = text.rfind("\n", 0, i - 1)
        chunk = text[prev + 1 : i]
        if chunk.strip() == "" or chunk.lstrip().startswith(("///", "[", "//")):
            i = prev + 1 if prev >= 0 else 0
            continue
        break
    member_start = i
    brace = text.find("{", start)
    semi = text.find(";", start)
    if brace != -1 and (semi == -1 or brace < semi):
        member_end = find_matching_brace(text, brace) + 1
    else:
        member_end = semi + 1 if semi != -1 else start
    while member_end < len(text) and text[member_end] in "\r\n":
        member_end += 1
    new_text = text[:member_start] + text[member_end:]
    if new_text != text:
        path.write_text(new_text, encoding="utf-8", newline="\n")
        return True
    return False


def collect_errors() -> list[tuple[Path, int]]:
    proc = subprocess.run(
        ["dotnet", "build", str(SLN), "-c", "Debug", "-v", "q"],
        cwd=ROOT,
        capture_output=True,
        text=True,
        encoding="utf-8",
        errors="replace",
    )
    out = proc.stdout + proc.stderr
    hits: list[tuple[Path, int]] = []
    pat = re.compile(r"^(?P<file>.+\.cs)\((?P<line>\d+),\d+\): error CS(?:0111|0101|0102|8863):")
    for line in out.splitlines():
        m = pat.match(line.strip())
        if m:
            hits.append((Path(m.group("file")), int(m.group("line"))))
    return hits


def main() -> None:
    for _ in range(200):
        errors = collect_errors()
        if not errors:
            print("No duplicate-member errors remain.")
            return
        path, line_no = errors[-1]
        rel = path.relative_to(ROOT.parent) if path.is_absolute() else path
        if remove_member_at_line(path, line_no):
            print(f"Removed duplicate at {rel}:{line_no}")
        else:
            print(f"Failed to remove duplicate at {rel}:{line_no}")
            break
    print("Stopped after iteration limit or failure.")


if __name__ == "__main__":
    main()
