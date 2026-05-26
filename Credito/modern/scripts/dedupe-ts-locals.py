#!/usr/bin/env python3
"""Remove duplicate local const/let/function declarations (same name + indent) in TS/TSX."""

from __future__ import annotations

import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1] / "Credito.Modern.Web" / "src"
ALLOW_PREFIXES = ("pages/", "components/")
DECL = re.compile(r"^(\s*)(?:const|let|function)\s+(\w+)\s*=")
SCOPE = re.compile(r"^(\s*)(?:export\s+)?function\s+\w+")


def find_statement_end(lines: list[str], start: int) -> int:
    i = start
    depth_paren = 0
    depth_brace = 0
    started = False
    while i < len(lines):
        line = lines[i]
        for ch in line:
            if ch == "(":
                depth_paren += 1
                started = True
            elif ch == ")":
                depth_paren = max(0, depth_paren - 1)
            elif ch == "{":
                depth_brace += 1
                started = True
            elif ch == "}":
                depth_brace = max(0, depth_brace - 1)
        if started and depth_paren == 0 and depth_brace == 0 and (
            line.rstrip().endswith(";") or line.rstrip().endswith(")") or line.rstrip().endswith("}")
        ):
            return i + 1
        if started and depth_paren == 0 and depth_brace == 0 and i > start and line.strip() == "":
            return i
        i += 1
    return len(lines)


def dedupe_locals(text: str) -> tuple[str, int]:
    lines = text.splitlines(keepends=True)
    scopes: list[set[tuple[str, str]]] = [set()]
    out: list[str] = []
    i = 0
    removed = 0
    while i < len(lines):
        scope_match = SCOPE.match(lines[i])
        if scope_match:
            indent = len(scope_match.group(1))
            while len(scopes) > 1 and indent <= len(scopes) - 1:
                scopes.pop()
            scopes.append(set())
            out.append(lines[i])
            i += 1
            continue

        m = DECL.match(lines[i])
        if not m:
            out.append(lines[i])
            i += 1
            continue
        indent, name = m.group(1), m.group(2)
        key = (indent, name)
        end = find_statement_end(lines, i)
        block = lines[i:end]
        if key in scopes[-1]:
            removed += 1
            i = end
            continue
        scopes[-1].add(key)
        out.extend(block)
        i = end
    return "".join(out), removed


def main() -> None:
    total = 0
    files = 0
    for path in sorted(ROOT.rglob("*")):
        if path.suffix not in (".ts", ".tsx"):
            continue
        rel = path.relative_to(ROOT).as_posix()
        if not rel.startswith(ALLOW_PREFIXES):
            continue
        text = path.read_text(encoding="utf-8")
        new_text, n = dedupe_locals(text)
        if n:
            path.write_text(new_text, encoding="utf-8")
            total += n
            files += 1
    print(f"Removed {total} duplicate local declarations in {files} files")


if __name__ == "__main__":
    main()
