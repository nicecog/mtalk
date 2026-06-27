#!/usr/bin/env python3
"""Validate MBody.unity ButtonFlow / Button onClick wiring."""
import re
import sys
from collections import defaultdict

BUTTONFLOW_GUID = "0899471f9df43cc4b8da7dc8e66066b7"
TIMERFLOW_GUID = "180275a496f12ae4d95454da9f5bba5b"


def parse_scene(path: str):
    text = open(path, encoding="utf-8", errors="replace").read()

    go_name: dict[str, str] = {}
    go_tag: dict[str, str] = {}
    for m in re.finditer(r"--- !u!1 &(\d+)\nGameObject:.*?(?=--- !u!|\Z)", text, re.S):
        block = m.group(0)
        fid = m.group(1)
        nm = re.search(r"\n  m_Name: (.+)", block)
        tg = re.search(r"\n  m_TagString: (.+)", block)
        if nm:
            go_name[fid] = nm.group(1)
        if tg:
            go_tag[fid] = tg.group(1)

    mb_on_go: dict[str, list[tuple[str, str, str]]] = defaultdict(list)
    for m in re.finditer(r"--- !u!114 &(\d+)\nMonoBehaviour:.*?(?=--- !u!|\Z)", text, re.S):
        block = m.group(0)
        mb_id = m.group(1)
        go = re.search(r"\n  m_GameObject: \{fileID: (\d+)\}", block)
        script = re.search(r"guid: ([0-9a-f]+)", block)
        if go:
            mb_on_go[go.group(1)].append((mb_id, block, script.group(1) if script else ""))

    flows = []
    flow_issues = []
    for go_id, mbs in mb_on_go.items():
        for _mb_id, block, guid in mbs:
            if guid not in (BUTTONFLOW_GUID, TIMERFLOW_GUID):
                continue
            typ = "ButtonFlow" if guid == BUTTONFLOW_GUID else "TimerFlow"
            sp = re.search(r"SelfPage: \{fileID: (\d+)\}", block)
            np = re.search(r"NextPage: \{fileID: (\d+)\}", block)
            npn = re.search(r"NextPageName: (.+)", block)
            sp_id = sp.group(1) if sp else "0"
            np_id = np.group(1) if np else "0"
            next_name = npn.group(1).strip() if npn else ""
            host = go_name.get(go_id, f"go{go_id}")
            flows.append((typ, host, sp_id, np_id, next_name))
            if sp_id == "0":
                flow_issues.append(f"{typ} on '{host}': missing SelfPage")
            elif sp_id not in go_name:
                flow_issues.append(f"{typ} on '{host}': SelfPage id {sp_id} not found")
            elif np_id == "0" and not next_name:
                flow_issues.append(f"{typ} on '{host}': missing NextPage/NextPageName")
            elif np_id != "0" and np_id not in go_name:
                flow_issues.append(f"{typ} on '{host}': NextPage id {np_id} not found")

    fm_first = fm_body = fm_dance = "?"
    for m in re.finditer(r"--- !u!114 &(\d+)\nMonoBehaviour:.*?(?=--- !u!|\Z)", text, re.S):
        block = m.group(0)
        if "FirstPage:" not in block or "BodyPage:" not in block:
            continue
        if "FlowManager" not in block and "setFirstPage" not in block:
            # FlowManager script block has these fields
            if "WebCamObject:" not in block:
                continue
        fp = re.search(r"FirstPage: \{fileID: (\d+)\}", block)
        bp = re.search(r"BodyPage: \{fileID: (\d+)\}", block)
        dp = re.search(r"DancePage: \{fileID: (\d+)\}", block)
        if fp:
            fm_first = go_name.get(fp.group(1), fp.group(1))
        if bp:
            fm_body = go_name.get(bp.group(1), bp.group(1))
        if dp:
            fm_dance = go_name.get(dp.group(1), dp.group(1))
        break

    btn_issues = []
    btn_ok = 0
    legacy_types = ("CamImageRead",)
    for m in re.finditer(r"--- !u!114 &(\d+)\nMonoBehaviour:.*?(?=--- !u!|\Z)", text, re.S):
        block = m.group(0)
        if "m_OnClick:" not in block:
            continue
        go = re.search(r"m_GameObject: \{fileID: (\d+)\}", block)
        go_n = go_name.get(go.group(1), "?") if go else "?"
        calls = re.findall(
            r"m_Target: \{fileID: (\d+)\}.*?m_TargetAssemblyTypeName: ([^\n]+).*?m_MethodName: (\w+)",
            block,
            re.S,
        )
        if not calls:
            if "m_Calls: []" in block.replace(" ", ""):
                btn_issues.append(f"Button '{go_n}': onClick empty")
            continue
        for tgt, type_name, method in calls:
            if any(legacy in type_name for legacy in legacy_types):
                btn_issues.append(
                    f"Button '{go_n}': legacy type {type_name.strip()}, method={method}"
                )
            elif tgt == "0":
                btn_issues.append(f"Button '{go_n}': missing target, method={method}")
            else:
                btn_ok += 1

    scene_flow = sorted(go_name[i] for i, t in go_tag.items() if t == "SceneFlow" and i in go_name)
    edges = set()
    for typ, host, sp, np, npn in flows:
        next_label = npn if np == "0" and npn else go_name.get(np, np)
        edges.add((go_name.get(sp, sp), next_label, typ, host))

    return {
        "scene_flow": scene_flow,
        "flows": flows,
        "edges": edges,
        "flow_issues": flow_issues,
        "btn_issues": btn_issues,
        "btn_ok": btn_ok,
        "fm_first": fm_first,
        "fm_body": fm_body,
        "fm_dance": fm_dance,
    }


def main():
    root = sys.argv[1] if len(sys.argv) > 1 else "."
    import os
    import glob

    scene_dir = os.path.join(root, "Assets", "Scenes")
    scene_files = sorted(glob.glob(os.path.join(scene_dir, "*.unity")))
    if not scene_files:
        scene_files = [os.path.join(scene_dir, "MBody.unity")]

    merged = {
        "scene_flow": [],
        "flows": [],
        "edges": set(),
        "flow_issues": [],
        "btn_issues": [],
        "btn_ok": 0,
        "fm_first": "?",
        "fm_body": "?",
        "fm_dance": "?",
    }

    for path in scene_files:
        if not os.path.isfile(path):
            continue
        r = parse_scene(path)
        merged["scene_flow"].extend(r["scene_flow"])
        merged["flows"].extend(r["flows"])
        merged["edges"].update(r["edges"])
        merged["flow_issues"].extend(r["flow_issues"])
        merged["btn_issues"].extend(r["btn_issues"])
        merged["btn_ok"] += r["btn_ok"]
        if r["fm_first"] != "?":
            merged["fm_first"] = r["fm_first"]
        if r["fm_body"] != "?":
            merged["fm_body"] = r["fm_body"]
        if r["fm_dance"] != "?":
            merged["fm_dance"] = r["fm_dance"]

    r = merged
    fail = len(r["flow_issues"]) + len(r["btn_issues"])

    print("=== MBody Scene Flow Validation ===")
    print(f"Scenes scanned:         {len(scene_files)}")
    print(f"SceneFlow pages: {len(r['scene_flow'])}")
    print(f"FlowManager FirstPage: {r['fm_first']}")
    print(f"FlowManager BodyPage:   {r['fm_body']}")
    print(f"FlowManager DancePage:  {r['fm_dance']}")
    print(f"ButtonFlow/TimerFlow:   {len(r['flows'])}")
    print(f"Unique transitions:     {len(r['edges'])}")
    print(f"Button onClick wired:   {r['btn_ok']}")
    print(f"FAIL flow wiring:       {len(r['flow_issues'])}")
    print(f"FAIL button onClick:    {len(r['btn_issues'])}")

    if r["flow_issues"]:
        print("\n-- Flow issues --")
        for i in r["flow_issues"]:
            print(" ", i)

    if r["btn_issues"]:
        print("\n-- Button onClick issues --")
        for i in r["btn_issues"]:
            print(" ", i)

    print("\n-- SceneFlow page names --")
    for n in r["scene_flow"]:
        print(" ", n)

    print("\n-- Transitions (SelfPage -> NextPage) --")
    for sp, np, typ, host in sorted(r["edges"], key=lambda x: (x[0], x[1])):
        print(f"  [{typ}] {sp} -> {np}  (via {host})")

    return 1 if fail else 0


if __name__ == "__main__":
    sys.exit(main())
