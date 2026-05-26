#!/usr/bin/env python3
"""Remove duplicate minimal-API endpoint blocks in program.cs (keeps first WithName)."""

from __future__ import annotations

import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
PROGRAM = ROOT / "Credito.Modern.Api" / "program.cs"

MAP_START = re.compile(
    r"(?m)^(?P<indent>\s*)app\.Map(?:Get|Post|Put|Delete|Patch)\("
)
WITH_NAME = re.compile(r'\.WithName\("([^"]+)"\)')
MAP_EXTENSIONS = re.compile(r"(?m)^\s*app\.Map[A-Za-z]+\(\);?\s*$")


def split_blocks(text: str) -> tuple[str, list[tuple[str, str | None]], str]:
    matches = list(MAP_START.finditer(text))
    if not matches:
        return text, [], ""

    prefix = text[: matches[0].start()]
    blocks: list[tuple[str, str | None]] = []
    for i, m in enumerate(matches):
        start = m.start()
        end = matches[i + 1].start() if i + 1 < len(matches) else len(text)
        chunk = text[start:end]
        name_m = WITH_NAME.search(chunk)
        name = name_m.group(1) if name_m else None
        blocks.append((chunk, name))

    suffix = ""
    ext = MAP_EXTENSIONS.search(text[matches[-1].start() :])
    if ext and blocks:
        last_chunk, last_name = blocks[-1]
        cut = ext.start()
        kept = last_chunk[:cut]
        suffix = last_chunk[cut:]
        blocks[-1] = (kept, last_name)

    return prefix, blocks, suffix


def main() -> None:
    text = PROGRAM.read_text(encoding="utf-8")
    prefix, blocks, suffix = split_blocks(text)
    seen: set[str] = set()
    kept_blocks: list[str] = []
    removed = 0
    for chunk, name in blocks:
        if name and name in seen:
            removed += 1
            continue
        if name:
            seen.add(name)
        kept_blocks.append(chunk)

    new_text = prefix + "".join(kept_blocks) + suffix
    PROGRAM.write_text(new_text, encoding="utf-8", newline="\n")
    print(f"Removed {removed} duplicate endpoint block(s).")
    print(f"Lines: {text.count(chr(10))+1} -> {new_text.count(chr(10))+1}")


if __name__ == "__main__":
    main()
