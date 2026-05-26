#!/usr/bin/env python3
"""Restore Credito/modern from Cursor local file history + agent transcript."""
from __future__ import annotations

import json
import re
import urllib.parse
from collections import defaultdict
from pathlib import Path

ROOT = Path(r"D:\GitHub\Credito\modern")
HISTORY = Path(r"C:\Users\ASUS TUF GAMING\AppData\Roaming\Cursor\User\History")
TRANSCRIPT = Path(
    r"C:\Users\ASUS TUF GAMING\.cursor\projects\d-GitHub\agent-transcripts"
    r"\bb95f695-d01f-4c57-8e8f-a1d36c0c56de\bb95f695-d01f-4c57-8e8f-a1d36c0c56de.jsonl"
)
REPORT = ROOT / "RECOVERY-REPORT.txt"

MODERN_MARKERS = (
    "/credito/modern/",
    "\\credito\\modern\\",
    "file:///d%3a/github/credito/modern/",
)


def normalize_resource(resource: str) -> Path | None:
    if resource.startswith("file:///"):
        path = urllib.parse.unquote(resource.replace("file:///", ""))
        if path.startswith("/"):
            path = path[1:]
        p = Path(path)
    else:
        p = Path(resource)
    s = str(p).replace("\\", "/").lower()
    if "/credito/modern/" not in s:
        return None
    idx = s.index("/credito/modern/")
    rel = s[idx + len("/credito/modern/") :]
    return ROOT / rel.replace("/", "\\")


def recover_from_cursor_history() -> dict[str, tuple[int, str]]:
    restored: dict[str, tuple[int, str]] = {}
    entries_files = list(HISTORY.glob("*/entries.json"))
    for entries_path in entries_files:
        try:
            data = json.loads(entries_path.read_text(encoding="utf-8"))
        except Exception:
            continue
        resource = data.get("resource", "")
        target = normalize_resource(resource)
        if target is None:
            continue
        entries = data.get("entries") or []
        if not entries:
            continue
        latest = max(entries, key=lambda e: e.get("timestamp", 0))
        snap = entries_path.parent / latest["id"]
        if not snap.exists():
            continue
        key = str(target).lower()
        ts = int(latest.get("timestamp", 0))
        prev = restored.get(key)
        if prev is None or ts >= prev[0]:
            restored[key] = (ts, str(snap))
    return restored


def recover_from_transcript() -> dict[str, str]:
    files: dict[str, str] = {}
    if not TRANSCRIPT.exists():
        return files
    for line in TRANSCRIPT.read_text(encoding="utf-8").splitlines():
        try:
            obj = json.loads(line)
        except json.JSONDecodeError:
            continue
        for part in obj.get("message", {}).get("content", []):
            if part.get("type") != "tool_use" or part.get("name") != "Write":
                continue
            inp = part.get("input", {})
            path = inp.get("path", "")
            contents = inp.get("contents")
            if not path or contents is None:
                continue
            pnorm = path.replace("\\", "/").lower()
            if "/credito/modern/" not in pnorm:
                continue
            idx = pnorm.index("/credito/modern/") + len("/credito/modern/")
            rel = pnorm[idx:]
            target = ROOT / rel.replace("/", "\\")
            files[str(target).lower()] = contents
    return files


def apply_strreplace_patches() -> dict[str, int]:
    patched = 0
    if not TRANSCRIPT.exists():
        return {"patched": 0}
    for line in TRANSCRIPT.read_text(encoding="utf-8").splitlines():
        try:
            obj = json.loads(line)
        except json.JSONDecodeError:
            continue
        for part in obj.get("message", {}).get("content", []):
            if part.get("type") != "tool_use" or part.get("name") != "StrReplace":
                continue
            inp = part.get("input", {})
            path = inp.get("path", "")
            old = inp.get("old_string")
            new = inp.get("new_string")
            if not path or old is None or new is None:
                continue
            pnorm = path.replace("\\", "/").lower()
            if "/credito/modern/" not in pnorm:
                continue
            idx = pnorm.index("/credito/modern/") + len("/credito/modern/")
            target = ROOT / pnorm[idx:].replace("/", "\\")
            if not target.exists() or not target.is_file():
                continue
            text = target.read_text(encoding="utf-8")
            if old not in text:
                continue
            target.write_text(text.replace(old, new, 1), encoding="utf-8", newline="\n")
            patched += 1
    return {"patched": patched}


def write_file(target: Path, content: str, source: str) -> None:
    target.parent.mkdir(parents=True, exist_ok=True)
    target.write_text(content, encoding="utf-8", newline="\n")


def main() -> None:
    stats: dict[str, int] = defaultdict(int)
    history = recover_from_cursor_history()
    transcript = recover_from_transcript()

    for key, (_ts, snap_path) in history.items():
        target = Path(key) if key[1] == ":" else Path(key)
        # key stored lower-case path string; recover original casing from snap metadata path
        # Re-derive from snap's entries.json resource for casing
        pass

    # history keys are lowercased; map back using transcript/history scan
    restored_targets: dict[Path, tuple[str, str]] = {}

    for entries_path in HISTORY.glob("*/entries.json"):
        try:
            data = json.loads(entries_path.read_text(encoding="utf-8"))
        except Exception:
            continue
        target = normalize_resource(data.get("resource", ""))
        if target is None:
            continue
        entries = data.get("entries") or []
        if not entries:
            continue
        latest = max(entries, key=lambda e: e.get("timestamp", 0))
        snap = entries_path.parent / latest["id"]
        if not snap.exists():
            continue
        ts = int(latest.get("timestamp", 0))
        prev = restored_targets.get(target)
        if prev is None or ts >= prev[1]:
            restored_targets[target] = ("cursor-history", ts)

    for key, content in transcript.items():
        # find matching path case-insensitively
        target = None
        for t in restored_targets:
            if str(t).lower() == key:
                target = t
                break
        if target is None:
            # path only in transcript
            rel_key = key.split("credito\\modern\\", 1)[-1]
            target = ROOT / rel_key
        prev = restored_targets.get(target)
        if prev is None:
            restored_targets[target] = ("transcript", 0)

    # Write cursor history (preferred - latest editor state)
    for entries_path in HISTORY.glob("*/entries.json"):
        try:
            data = json.loads(entries_path.read_text(encoding="utf-8"))
        except Exception:
            continue
        target = normalize_resource(data.get("resource", ""))
        if target is None:
            continue
        entries = data.get("entries") or []
        if not entries:
            continue
        latest = max(entries, key=lambda e: e.get("timestamp", 0))
        snap = entries_path.parent / latest["id"]
        if not snap.exists():
            continue
        content = snap.read_text(encoding="utf-8", errors="replace")
        write_file(target, content, "cursor-history")
        stats["from_history"] += 1

    # Fill gaps from transcript
    for key, content in transcript.items():
        rel_key = key.split("credito\\modern\\", 1)[-1]
        target = ROOT / rel_key
        if target.exists():
            stats["skipped_existing"] += 1
            continue
        write_file(target, content, "transcript")
        stats["from_transcript"] += 1

    patch_stats = apply_strreplace_patches()
    stats["strreplace_patches"] = patch_stats["patched"]

    # Count restored files by extension
    exts: dict[str, int] = defaultdict(int)
    for p in ROOT.rglob("*"):
        if not p.is_file():
            continue
        if any(part in {"node_modules", "bin", "obj", ".git"} for part in p.parts):
            continue
        exts[p.suffix.lower() or "(noext)"] += 1

    lines = [
        "Credito modern recovery report",
        f"from_history={stats['from_history']}",
        f"from_transcript={stats['from_transcript']}",
        f"skipped_existing={stats['skipped_existing']}",
        f"strreplace_patches={stats['strreplace_patches']}",
        "",
        "Extensions restored (excluding bin/obj/node_modules):",
    ]
    for ext, count in sorted(exts.items(), key=lambda x: -x[1])[:25]:
        lines.append(f"  {ext}: {count}")

    REPORT.write_text("\n".join(lines), encoding="utf-8")
    print("\n".join(lines))


if __name__ == "__main__":
    main()
