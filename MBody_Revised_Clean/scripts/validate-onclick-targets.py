#!/usr/bin/env python3
"""Definitive onClick validator.

For every Button onClick PersistentCall it resolves the *actual* target
component (by fileID), determines its script class, and confirms that the
invoked method exists as a public member on that class (or a Unity built-in
like GameObject.SetActive / Behaviour.set_enabled).

Catches the real failure mode: a button wired to a component that does NOT
implement the method name -> UnityEvent silently does nothing at runtime.
"""
import glob
import os
import re
import sys

UNITY_BUILTINS = {
    ("GameObject", "SetActive"),
    ("Behaviour", "set_enabled"),
    ("MonoBehaviour", "set_enabled"),
    ("AudioSource", "Play"),
    ("AudioSource", "Stop"),
    ("AudioSource", "Pause"),
    ("Animator", "SetTrigger"),
}


def build_guid_to_class(root):
    guid_class = {}
    for meta in glob.glob(os.path.join(root, "Assets", "**", "*.cs.meta"), recursive=True):
        txt = open(meta, encoding="utf-8", errors="replace").read()
        m = re.search(r"guid: ([0-9a-f]+)", txt)
        if m:
            cls = os.path.basename(meta)[:-len(".cs.meta")]
            guid_class[m.group(1)] = cls
    return guid_class


def build_class_methods(root):
    methods = {}
    for cs in glob.glob(os.path.join(root, "Assets", "**", "*.cs"), recursive=True):
        txt = open(cs, encoding="utf-8", errors="replace").read()
        cls = os.path.basename(cs)[:-3]
        names = set()
        # public methods / coroutines
        for m in re.finditer(
            r"\bpublic\s+(?:static\s+)?(?:virtual\s+|override\s+)?[\w<>\[\],\s\.]+?\s+(\w+)\s*\(",
            txt,
        ):
            names.add(m.group(1))
        # public fields/properties (for set_X setter style and direct field sets)
        for m in re.finditer(r"\bpublic\s+[\w<>\[\],\s\.]+?\s+(\w+)\s*[;={]", txt):
            names.add(m.group(1))
            names.add("set_" + m.group(1))
        methods[cls] = names
    return methods


def parse_scene(path, guid_class):
    text = open(path, encoding="utf-8", errors="replace").read()

    go_name, go_active = {}, {}
    for m in re.finditer(r"--- !u!1 &(\d+)\nGameObject:.*?(?=--- !u!|\Z)", text, re.S):
        b = m.group(0)
        nm = re.search(r"\n  m_Name: (.+)", b)
        act = re.search(r"\n  m_IsActive: (\d)", b)
        if nm:
            go_name[m.group(1)] = nm.group(1).strip()
        go_active[m.group(1)] = (act.group(1) == "1") if act else True

    # component fileID -> (class, hostGoId)
    comp_class, comp_host = {}, {}
    for m in re.finditer(r"--- !u!114 &(\d+)\nMonoBehaviour:.*?(?=--- !u!|\Z)", text, re.S):
        b = m.group(0)
        fid = m.group(1)
        go = re.search(r"m_GameObject: \{fileID: (\d+)\}", b)
        gd = re.search(r"m_Script: \{fileID: \d+, guid: ([0-9a-f]+)", b)
        if go:
            comp_host[fid] = go.group(1)
        if gd:
            comp_class[fid] = guid_class.get(gd.group(1), f"guid:{gd.group(1)[:8]}")

    issues = []
    for m in re.finditer(r"--- !u!114 &(\d+)\nMonoBehaviour:.*?(?=--- !u!|\Z)", text, re.S):
        b = m.group(0)
        if "m_OnClick:" not in b:
            continue
        host_go = comp_host.get(m.group(1))
        btn_name = go_name.get(host_go, "?")
        for c in re.finditer(
            r"m_Target: \{fileID: (\d+)\}\s*\n\s*m_TargetAssemblyTypeName: ([^\n,]+)[^\n]*\n"
            r"(?:.*?\n)?\s*m_MethodName: (\w+)",
            b,
            re.S,
        ):
            tgt, type_name, method = c.group(1), c.group(2).strip(), c.group(3)
            yield_target = comp_class.get(tgt)
            issues_local = None
            if tgt == "0":
                issues_local = f"'{btn_name}': MISSING target -> {type_name}.{method}"
            elif yield_target is None:
                # target is a GameObject (not a MonoBehaviour component)
                short = type_name.split(".")[-1]
                if (short, method) not in UNITY_BUILTINS:
                    # could be GameObject.SetActive etc. accept builtins only
                    if method not in ("SetActive",):
                        issues_local = (
                            f"'{btn_name}': target {tgt} not a known component "
                            f"({type_name}.{method})"
                        )
            else:
                cls_methods = ALL_METHODS.get(yield_target, set())
                if method not in cls_methods and method not in (
                    "SetActive", "set_enabled", "Stop", "Play", "Pause",
                ):
                    issues_local = (
                        f"'{btn_name}': {yield_target} has NO method '{method}' "
                        f"(declared as {type_name})"
                    )
            if issues_local:
                issues.append((os.path.basename(path), issues_local))
    return issues


ALL_METHODS = {}


def main():
    root = sys.argv[1] if len(sys.argv) > 1 else "."
    guid_class = build_guid_to_class(root)
    global ALL_METHODS
    ALL_METHODS = build_class_methods(root)

    all_issues = []
    for path in sorted(glob.glob(os.path.join(root, "Assets", "Scenes", "*.unity"))):
        all_issues.extend(parse_scene(path, guid_class))

    print("=== onClick target method validation ===")
    if not all_issues:
        print("PASS: every onClick targets a component that implements its method.")
        return 0

    print(f"FOUND {len(all_issues)} suspicious onClick wirings:\n")
    for scene, msg in all_issues:
        print(f"  [{scene}] {msg}")
    return 1


if __name__ == "__main__":
    sys.exit(main())
