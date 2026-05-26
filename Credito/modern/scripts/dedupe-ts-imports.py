#!/usr/bin/env python3
"""Remove duplicate single-line import statements in TS/TSX."""

from pathlib import Path

root = Path(__file__).resolve().parents[1] / "Credito.Modern.Web" / "src"
fixed = 0
for path in sorted(root.rglob("*")):
    if path.suffix not in (".ts", ".tsx"):
        continue
    lines = path.read_text(encoding="utf-8").splitlines(keepends=True)
    seen_imports: set[str] = set()
    out: list[str] = []
    changed = False
    for line in lines:
        s = line.strip()
        # Solo imports de una línea (import ... from '...') para no romper multilínea.
        if s.startswith("import ") and " from " in s:
            if s in seen_imports:
                changed = True
                continue
            seen_imports.add(s)
        out.append(line)
    if changed:
        path.write_text("".join(out), encoding="utf-8")
        fixed += 1
print(f"Fixed duplicate single-line imports in {fixed} files")
