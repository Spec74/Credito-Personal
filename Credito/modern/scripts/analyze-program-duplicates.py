#!/usr/bin/env python3
import re
from collections import Counter
from pathlib import Path

text = Path(__file__).resolve().parents[1] / "Credito.Modern.Api" / "program.cs"
content = text.read_text(encoding="utf-8")
names = re.findall(r'\.WithName\("([^"]+)"\)', content)
c = Counter(names)
dups = [(n, v) for n, v in c.items() if v > 1]
print("duplicate WithName count:", len(dups))
print("total duplicate instances:", sum(v - 1 for _, v in dups))
for n, v in sorted(dups, key=lambda x: (-x[1], x[0]))[:30]:
    print(f"  {n}: {v}x")

routes = re.findall(r'app\.Map(?:Get|Post|Put|Delete|Patch)\(\s*["\']([^"\']+)["\']', content)
rc = Counter(routes)
rd = [(r, v) for r, v in rc.items() if v > 1]
print("duplicate routes count:", len(rd))
print("total duplicate route instances:", sum(v - 1 for _, v in rd))
