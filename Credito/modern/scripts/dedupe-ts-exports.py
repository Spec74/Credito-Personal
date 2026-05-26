#!/usr/bin/env python3
"""Remove duplicate top-level exports and module functions in TS/TSX (keeps first)."""

from __future__ import annotations

import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1] / "Credito.Modern.Web" / "src"

START = re.compile(
    r"^(export\s+(?:async\s+)?(?:function|class|interface|type|enum)\s+(\w+)|function\s+(\w+))",
    re.MULTILINE,
)


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
        if ch in ("'", '"', "`"):
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
    return len(text) - 1


def skip_return_type(text: str, i: int) -> int:
    while i < len(text) and text[i].isspace():
        i += 1
    if i >= len(text) or text[i] != ":":
        return i
    i += 1
    depth_angle = 0
    depth_brace = 0
    depth_paren = 0
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
        if ch in ("'", '"', "`"):
            in_str = ch
        elif ch == "<":
            depth_angle += 1
        elif ch == ">":
            depth_angle = max(0, depth_angle - 1)
        elif ch == "{":
            depth_brace += 1
            if depth_angle == 0 and depth_paren == 0:
                return i
        elif ch == "}":
            depth_brace = max(0, depth_brace - 1)
        elif ch == "(":
            depth_paren += 1
        elif ch == ")":
            depth_paren = max(0, depth_paren - 1)
        elif ch == ";" and depth_angle == 0 and depth_brace == 0 and depth_paren == 0:
            return i + 1
        elif text.startswith("=>", i) and depth_angle == 0 and depth_brace == 0 and depth_paren == 0:
            return i + 2
        i += 1
    return i


def end_of_declaration(text: str, start: int) -> int:
    i = start
    if text.startswith("export", i) and (
        text.startswith("export type", i)
        or text.startswith("export interface", i)
        or text.startswith("export enum", i)
    ):
        brace = text.find("{", i)
        eq = text.find("=", i)
        if brace != -1 and (eq == -1 or brace < eq + 80):
            end = find_matching_brace(text, brace) + 1
        elif eq != -1:
            j = eq + 1
            depth = 0
            in_str = None
            escape = False
            while j < len(text):
                ch = text[j]
                if in_str:
                    if escape:
                        escape = False
                    elif ch == "\\":
                        escape = True
                    elif ch == in_str:
                        in_str = None
                    j += 1
                    continue
                if ch in ("'", '"', "`"):
                    in_str = ch
                elif ch in "([{":
                    depth += 1
                elif ch in ")]}":
                    depth -= 1
                elif ch == ";" and depth == 0:
                    return j + 1
                j += 1
            end = len(text)
        else:
            end = start
    elif text.startswith("export", i) or text.startswith("function", i):
        paren = text.find("(", i)
        if paren != -1:
            depth = 0
            in_str = None
            escape = False
            k = paren
            while k < len(text):
                ch = text[k]
                if in_str:
                    if escape:
                        escape = False
                    elif ch == "\\":
                        escape = True
                    elif ch == in_str:
                        in_str = None
                    k += 1
                    continue
                if ch in ("'", '"', "`"):
                    in_str = ch
                elif ch == "(":
                    depth += 1
                elif ch == ")":
                    depth -= 1
                    if depth == 0:
                        paren_end = k + 1
                        break
                k += 1
            else:
                paren_end = paren + 1
            body_start = skip_return_type(text, paren_end)
            while body_start < len(text) and text[body_start].isspace():
                body_start += 1
            if body_start + 1 < len(text) and text[body_start : body_start + 2] == "=>":
                semi = text.find(";", body_start)
                end = semi + 1 if semi != -1 else len(text)
            elif body_start < len(text) and text[body_start] == "{":
                end = find_matching_brace(text, body_start) + 1
            else:
                semi = text.find(";", body_start)
                end = semi + 1 if semi != -1 else body_start
        else:
            end = start
    else:
        brace = text.find("{", start)
        eq = text.find("=", start)
        candidates = [x for x in (brace, eq) if x != -1]
        if not candidates:
            return start
        first = min(candidates)
        if first == brace:
            end = find_matching_brace(text, brace) + 1
        else:
            i = first + 1
            depth = 0
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
                if ch in ("'", '"', "`"):
                    in_str = ch
                elif ch in "([{":
                    depth += 1
                elif ch in ")]}":
                    depth -= 1
                elif ch == ";" and depth == 0:
                    return i + 1
                i += 1
            end = len(text)
    while end < len(text) and text[end] in "\r\n":
        end += 1
    return end


def dedupe(content: str) -> tuple[str, int]:
    seen: set[str] = set()
    removed = 0
    out: list[str] = []
    last = 0
    for m in START.finditer(content):
        start = m.start()
        end = end_of_declaration(content, start)
        if end <= start:
            end = m.end()
        out.append(content[last:start])
        block = content[start:end]
        name = m.group(1) or m.group(2)
        key = f"{m.group(0).split()[0]}:{name}" if m.group(0).startswith("export") else f"fn:{name}"
        if key in seen:
            removed += 1
        else:
            seen.add(key)
            out.append(block)
        last = max(last, end)
    out.append(content[last:])
    return "".join(out), removed


def main() -> int:
    total = 0
    skip = {"config/legacyreporturls.ts"}
    for path in sorted(ROOT.rglob("*")):
        if path.suffix not in (".ts", ".tsx") or path.name.endswith(".d.ts"):
            continue
        rel = path.relative_to(ROOT).as_posix()
        if rel in skip:
            continue
        text = path.read_text(encoding="utf-8")
        new_text, n = dedupe(text)
        if n:
            path.write_text(new_text, encoding="utf-8", newline="\n")
            print(f"{path.relative_to(ROOT.parent.parent)}: {n}")
            total += n
    print(f"Removed {total} duplicate declaration(s).")
    return 0


if __name__ == "__main__":
    sys.exit(main())
