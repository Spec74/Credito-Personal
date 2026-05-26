#!/usr/bin/env python3
"""Remove duplicate export function blocks by name in legacyreporturls.ts."""

from pathlib import Path
import re

path = Path(__file__).resolve().parents[1] / "Credito.Modern.Web" / "src" / "config" / "legacyreporturls.ts"
lines = path.read_text(encoding="utf-8").splitlines(keepends=True)

seen: set[str] = set()
out: list[str] = []
i = 0
removed = 0
while i < len(lines):
    m = re.match(r"^export function (\w+)", lines[i])
    if not m:
        out.append(lines[i])
        i += 1
        continue
    name = m.group(1)
    start = i
    depth = 0
    started = False
    while i < len(lines):
        for ch in lines[i]:
            if ch == "{":
                depth += 1
                started = True
            elif ch == "}":
                depth -= 1
        i += 1
        if started and depth == 0:
            break
    block = lines[start:i]
    if name in seen:
        removed += 1
    else:
        seen.add(name)
        out.extend(block)
path.write_text("".join(out), encoding="utf-8")
print(f"Removed {removed} duplicate export functions from {path.name} ({len(out)} lines)")
