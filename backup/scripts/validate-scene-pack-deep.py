#!/usr/bin/env python3
"""Deep validation: cross-pack transitions, build scenes, PackServiceProxy."""
import glob
import os
import re
import sys

BODY_PACK = "MBodyBodyPack"
DANCE_PACK = "MBodyDancePack"
CORE_SCENES = {"MBody", "Login", "AndroidBootstrap"}

BODY_PAGES = {
    "MusicSelect", "SoundSelectYellow", "SoundSelectBlue", "SpeedSelect", "SpeedSelectPop",
    "MainGameCali", "ReadyMessage", "MainGame", "EndMessage", "Alert",
}
DANCE_PAGES = {
    "DanceSelect", "DanceLevelSelect", "LV1", "LV1End", "LV2", "LV2End",
    "LV3Intro", "LV3Preview", "LV3Calib", "LV3Play", "LV3End", "LV3ThirdEnd",
    "LV4MusicSelect", "LV4Next", "LV4Cali", "LV4Game", "LV4End", "PoseGuide", "PoseGuidePost",
}

BUTTONFLOW_GUID = "0899471f9df43cc4b8da7dc8e66066b7"
TIMERFLOW_GUID = "180275a496f12ae4d95454da9f5bba5b"
PACK_PROXY_GUID = None  # resolved at runtime from .meta if needed


def scene_name_from_path(path: str) -> str:
    return os.path.splitext(os.path.basename(path))[0]


def parse_flows(path: str):
    text = open(path, encoding="utf-8", errors="replace").read()
    scene = scene_name_from_path(path)

    go_name = {}
    for m in re.finditer(r"--- !u!1 &(\d+)\nGameObject:.*?(?=--- !u!|\Z)", text, re.S):
        nm = re.search(r"\n  m_Name: (.+)", m.group(0))
        if nm:
            go_name[m.group(1)] = nm.group(1)

    flows = []
    for m in re.finditer(r"--- !u!114 &(\d+)\nMonoBehaviour:.*?(?=--- !u!|\Z)", text, re.S):
        block = m.group(0)
        script = re.search(r"guid: ([0-9a-f]+)", block)
        if not script or script.group(1) not in (BUTTONFLOW_GUID, TIMERFLOW_GUID):
            continue
        go = re.search(r"m_GameObject: \{fileID: (\d+)\}", block)
        host = go_name.get(go.group(1), "?") if go else "?"
        sp = re.search(r"SelfPage: \{fileID: (\d+)\}", block)
        npn = re.search(r"NextPageName: (.+)", block)
        pack = re.search(r"PackSceneToLoad: (.+)", block)
        sp_id = sp.group(1) if sp else "0"
        self_name = go_name.get(sp_id, "") if sp_id != "0" else ""
        next_name = npn.group(1).strip() if npn else ""
        pack_name = pack.group(1).strip() if pack else ""
        flows.append({
            "scene": scene,
            "host": host,
            "self": self_name,
            "next": next_name,
            "pack": pack_name,
        })
    return flows, set(go_name.values())


def parse_build_scenes(root: str):
    path = os.path.join(root, "ProjectSettings", "EditorBuildSettings.asset")
    text = open(path, encoding="utf-8", errors="replace").read()
    scenes = re.findall(r"path: (Assets/Scenes/[^\n]+)", text)
    names = [os.path.splitext(os.path.basename(p))[0] for p in scenes]
    # Build prepends AndroidBootstrap
    if "AndroidBootstrap" not in names:
        names = ["AndroidBootstrap"] + names
    return names


def main():
    root = sys.argv[1] if len(sys.argv) > 1 else "."
    scene_dir = os.path.join(root, "Assets", "Scenes")
    scene_files = sorted(glob.glob(os.path.join(scene_dir, "*.unity")))

    all_pages = set()
    all_flows = []
    for path in scene_files:
        flows, pages = parse_flows(path)
        all_flows.extend(flows)
        all_pages.update(pages)

    # SceneFlow tagged pages (approximate: known page names)
    known_pages = BODY_PAGES | DANCE_PAGES | {
        "IntroLogo", "MBodyLogin", "BodyDanceSelect", "IntroVideo", "EndSelect (1)",
        "Result", "Review",
    }

    issues = []
    build_scenes = set(parse_build_scenes(root))

    for f in all_flows:
        nxt = f["next"]
        if not nxt:
            continue
        if nxt not in known_pages and nxt not in all_pages:
            issues.append(f"NextPageName '{nxt}' on {f['scene']}/{f['host']} not found in scenes")
        if f["pack"] and f["pack"] not in build_scenes:
            issues.append(f"PackSceneToLoad '{f['pack']}' on {f['host']} not in build list")
        if nxt in BODY_PAGES and f["pack"] not in ("", BODY_PACK):
            if f["pack"] != BODY_PACK:
                issues.append(f"{f['host']}: next={nxt} expects pack {BODY_PACK}, got '{f['pack']}'")
        if nxt in DANCE_PAGES and f["pack"] not in ("", DANCE_PACK):
            if f["pack"] != DANCE_PACK:
                issues.append(f"{f['host']}: next={nxt} expects pack {DANCE_PACK}, got '{f['pack']}'")

    required_build = {BODY_PACK, DANCE_PACK, "MBody", "Login", "AndroidBootstrap"}
    missing_build = required_build - build_scenes
    for m in sorted(missing_build):
        issues.append(f"Build settings missing scene: {m}")

    proxy_scenes = set()
    for path in scene_files:
        text = open(path, encoding="utf-8", errors="replace").read()
        if "PackServiceProxy" in text:
            proxy_scenes.add(scene_name_from_path(path))
    for pack in (BODY_PACK, DANCE_PACK):
        if pack not in proxy_scenes:
            issues.append(f"PackServiceProxy missing in {pack}.unity")

    print("=== Deep Scene Pack Validation ===")
    print(f"Scenes files:     {len(scene_files)}")
    print(f"Build scenes:     {', '.join(sorted(build_scenes))}")
    print(f"Flows w/NextName: {sum(1 for f in all_flows if f['next'])}")
    print(f"PackServiceProxy: {', '.join(sorted(proxy_scenes)) or 'none'}")
    print(f"ISSUES:           {len(issues)}")
    for i in issues:
        print(" ", i)

    return 1 if issues else 0


if __name__ == "__main__":
    sys.exit(main())
