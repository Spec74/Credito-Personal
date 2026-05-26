#!/usr/bin/env python3
"""Remove duplicate top-level exports (TS/TSX) and duplicate class members (C#). Keeps first occurrence."""

from __future__ import annotations

import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


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


def dedupe_ts(content: str) -> tuple[str, int]:
    patterns = [
        re.compile(
            r"(^export\s+(?:async\s+)?function\s+(\w+)\s*\()",
            re.MULTILINE,
        ),
        re.compile(r"(^export\s+const\s+(\w+)\s*=)", re.MULTILINE),
        re.compile(r"(^export\s+type\s+(\w+)\s*=)", re.MULTILINE),
        re.compile(r"(^export\s+interface\s+(\w+)\s*[{<])", re.MULTILINE),
        re.compile(r"(^export\s+class\s+(\w+)\s*[{<])", re.MULTILINE),
        re.compile(r"(^export\s+enum\s+(\w+)\s*\{)", re.MULTILINE),
    ]
    seen: set[str] = set()
    removed = 0
    out: list[str] = []
    i = 0
    while i < len(content):
        match = None
        for pat in patterns:
            m = pat.search(content, i)
            if m and (match is None or m.start() < match.start()):
                match = m
        if not match:
            out.append(content[i:])
            break
        start = match.start()
        name = match.group(2)
        out.append(content[i:start])
        brace = content.find("{", match.end() - 1)
        paren_end = content.find(")", match.end() - 1)
        semi = content.find(";", match.end() - 1)
        if brace != -1 and (semi == -1 or brace < semi):
            end = find_matching_brace(content, brace) + 1
            while end < len(content) and content[end] in "\r\n":
                end += 1
        elif semi != -1:
            end = semi + 1
            while end < len(content) and content[end] in "\r\n":
                end += 1
        else:
            end = match.end()
        block = content[start:end]
        if name in seen:
            removed += 1
        else:
            seen.add(name)
            out.append(block)
        i = end
    return "".join(out), removed


def dedupe_cs(content: str) -> tuple[str, int]:
    # Records at namespace level (duplicate type names)
    record_pat = re.compile(
        r"^(\s*(?:public|internal|private|protected)\s+sealed\s+record\s+(\w+)\s*\()",
        re.MULTILINE,
    )
    seen_records: set[str] = set()
    removed = 0
    out: list[str] = []
    i = 0
    while i < len(content):
        m = record_pat.search(content, i)
        if not m:
            out.append(content[i:])
            break
        start = m.start()
        out.append(content[i:start])
        semi = content.find(";", m.end() - 1)
        end = semi + 1 if semi != -1 else m.end()
        while end < len(content) and content[end] in "\r\n":
            end += 1
        name = m.group(2)
        block = content[start:end]
        if name in seen_records:
            removed += 1
        else:
            seen_records.add(name)
            out.append(block)
        i = end
    content = "".join(out)

    # Class / struct members
    member_pat = re.compile(
        r"(^(\s*)(?:(?:public|private|protected|internal)\s+(?:static\s+|async\s+|override\s+|virtual\s+|sealed\s+)*)"
        r"(?:[\w<>\[\],\s.?]+\s+)?(\w+)\s*(?:<[^>]*>)?\s*\([^;]*\)\s*(?:where[^{;]+)?(?:=>|\{|;))",
        re.MULTILINE,
    )
    seen_members: set[str] = set()
    out = []
    i = 0
    removed_members = 0
    while i < len(content):
        m = member_pat.search(content, i)
        if not m:
            out.append(content[i:])
            break
        start = m.start()
        out.append(content[i:start])
        sig_key = m.group(3) + m.group(0)[m.group(0).find("(") :]
        body = m.group(0)
        pos_after = start + len(body)
        if body.rstrip().endswith("{"):
            brace = content.rfind("{", start, pos_after)
            pos_after = find_matching_brace(content, brace) + 1
            while pos_after < len(content) and content[pos_after] in "\r\n":
                pos_after += 1
        block = content[start:pos_after]
        key = re.sub(r"\s+", " ", sig_key.strip())
        if key in seen_members:
            removed_members += 1
        else:
            seen_members.add(key)
            out.append(block)
        i = pos_after
    return "".join(out), removed + removed_members


def process_file(path: Path) -> int:
    text = path.read_text(encoding="utf-8")
    if path.suffix in (".ts", ".tsx"):
        new_text, n = dedupe_ts(text)
    elif path.suffix == ".cs":
        new_text, n = dedupe_cs(text)
    else:
        return 0
    if n and new_text != text:
        path.write_text(new_text, encoding="utf-8", newline="\n")
    return n


def main() -> int:
    total = 0
    files: list[Path] = []
    for ext in ("*.ts", "*.tsx", "*.cs"):
        files.extend(ROOT.rglob(ext))
    for path in sorted(files):
        if "node_modules" in path.parts or path.name.endswith(".d.ts"):
            continue
        n = process_file(path)
        if n:
            print(f"{path.relative_to(ROOT)}: removed {n} duplicate(s)")
            total += n
    print(f"Done. Removed {total} duplicate block(s).")
    return 0


if __name__ == "__main__":
    sys.exit(main())
