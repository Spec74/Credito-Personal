#!/usr/bin/env python3
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
api_dir = ROOT / "Credito.Modern.Api"
api_sources = "\n".join(p.read_text(encoding="utf-8") for p in api_dir.rglob("*.cs"))
program = api_sources
tests = (ROOT / "Credito.Modern.Tests" / "reportpdfendpointsunauthorizedtests.cs").read_text(encoding="utf-8")

paths = set()
for line in tests.splitlines():
    m = re.search(r'"(/api/v1/[^"]+-pdf[^"]*)"', line)
    if m:
        paths.add(m.group(1).split("?")[0])

missing = sorted(p for p in paths if p not in program)
present = sorted(p for p in paths if p in program)
print(f"expected unique pdf paths: {len(paths)}")
print(f"present: {len(present)}")
print(f"missing: {len(missing)}")
for p in missing:
    print(p)
