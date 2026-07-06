#!/usr/bin/env python3
"""Audit every Button onClick PersistentCall across MBody scenes.

Groups calls by (TargetAssemblyType, MethodName) and reports which GameObject
hosts each button, so we can spot navigation handlers that bypass ButtonFlow.
"""
import glob
import os
import re
import sys
from collections import defaultdict

BUTTONFLOW_GUID = "0899471f9df43cc4b8da7dc8e66066b7"


def load(path):
    text = open(path, encoding="utf-8", errors="replace").read()

    go_name = {}
    for m in re.finditer(r"--- !u!1 &(\d+)\nGameObject:.*?(?=--- !u!|\Z)", text, re.S):
        nm = re.search(r"\n  m_Name: (.+)", m.group(0))
        if nm:
            go_name[m.group(1)] = nm.group(1).strip()

    # map MonoBehaviour fileID -> (hostGoName)
    mb_host = {}
    for m in re.finditer(r"--- !u!114 &(\d+)\nMonoBehaviour:.*?(?=--- !u!|\Z)", text, re.S):
        go = re.search(r"m_GameObject: \{fileID: (\d+)\}", m.group(0))
        if go:
            mb_host[m.group(1)] = go_name.get(go.group(1), f"go{go.group(1)}")

    results = []
    for m in re.finditer(r"--- !u!114 &(\d+)\nMonoBehaviour:.*?(?=--- !u!|\Z)", text, re.S):
        block = m.group(0)
        if "m_OnClick:" not in block:
            continue
        go = re.search(r"m_GameObject: \{fileID: (\d+)\}", block)
        host = go_name.get(go.group(1), "?") if go else "?"
        for c in re.finditer(
            r"m_Target: \{fileID: (\d+)\}\s*\n\s*m_TargetAssemblyTypeName: ([^\n]+?)\s*\n"
            r"(?:.*?\n)?\s*m_MethodName: (\w+)",
            block,
            re.S,
        ):
            tgt, type_name, method = c.group(1), c.group(2).strip(), c.group(3)
            short_type = type_name.split(",")[0]
            results.append((host, tgt, short_type, method))
    return results, os.path.basename(path)


def main():
    root = sys.argv[1] if len(sys.argv) > 1 else "."
    scene_dir = os.path.join(root, "Assets", "Scenes")
    by_method = defaultdict(list)
    for path in sorted(glob.glob(os.path.join(scene_dir, "*.unity"))):
        calls, scene = load(path)
        for host, tgt, typ, method in calls:
            key = f"{typ}.{method}"
            by_method[key].append((scene, host, tgt))

    print("=== onClick handlers grouped by Type.Method ===\n")
    for key in sorted(by_method):
        rows = by_method[key]
        missing = sum(1 for _, _, tgt in rows if tgt == "0")
        flag = "  <-- MISSING TARGET" if missing else ""
        print(f"{key}  x{len(rows)}{flag}")
        # show host buttons (unique)
        hosts = sorted({(s, h) for s, h, _ in rows})
        for s, h in hosts:
            print(f"      {s}: '{h}'")
    print()


if __name__ == "__main__":
    main()
