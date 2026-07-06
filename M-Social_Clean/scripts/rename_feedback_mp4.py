# -*- coding: utf-8 -*-
"""Rename StreamingAssets feedback MP4s to fb_XXX.mp4; update scenes; write feedback_names.json."""
import json
import os
import shutil
import sys

ROOT = os.path.normpath(sys.argv[1] if len(sys.argv) > 1 else os.path.join(os.path.dirname(__file__), ".."))
DRY = len(sys.argv) > 2 and sys.argv[2] == "1"

SA = os.path.join(ROOT, "Assets", "StreamingAssets")
SCENES = [
    os.path.join(ROOT, "Assets", "Scenes", "MSocial_Stage1.unity"),
    os.path.join(ROOT, "Assets", "Scenes", "MainScene.unity"),
]

MAPPINGS = [
    {"index": 0, "id": "fb_001", "legacyFileName": "\uC22B\uC790\uC138\uAE30 \uD2B8\uB808\uC774\uB2DD \uC601\uC0C1.mp4", "category": "CountNumber", "feedbackPage": "CountNumber"},
    {"index": 1, "id": "fb_002", "legacyFileName": "M-SOCIAL \uC5F0\uC8FC\uD574\uBCF4\uAE30_\uBC14\uB514_1(Adventure Band).mp4", "category": "PlayInstrument", "feedbackPage": "PlayInstrument", "group": "body"},
    {"index": 2, "id": "fb_003", "legacyFileName": "M-SOCIAL \uC5F0\uC8FC\uD574\uBCF4\uAE30_\uBC14\uB514_2 (Fly With Me).mp4", "category": "PlayInstrument", "feedbackPage": "PlayInstrument", "group": "body"},
    {"index": 3, "id": "fb_004", "legacyFileName": "M-SOCIAL \uC5F0\uC8FC\uD574\uBCF4\uAE30_\uBC14\uB514_3 (Ready And Go).mp4", "category": "PlayInstrument", "feedbackPage": "PlayInstrument", "group": "body"},
    {"index": 4, "id": "fb_005", "legacyFileName": "M-SOCIAL \uC5F0\uC8FC\uD574\uBCF4\uAE30_\uBC14\uB514_4 (Wind Maker).mp4", "category": "PlayInstrument", "feedbackPage": "PlayInstrument", "group": "body"},
    {"index": 5, "id": "fb_006", "legacyFileName": "M-SOCIAL \uC5F0\uC8FC\uD574\uBCF4\uAE30_\uC0AC\uC6B4\uB4DC 1.mp4", "category": "PlayInstrument", "feedbackPage": "PlayInstrument", "group": "sound"},
    {"index": 6, "id": "fb_007", "legacyFileName": "M-SOCIAL \uC5F0\uC8FC\uD574\uBCF4\uAE30_\uC0AC\uC6B4\uB4DC 2.mp4", "category": "PlayInstrument", "feedbackPage": "PlayInstrument", "group": "sound"},
    {"index": 7, "id": "fb_008", "legacyFileName": "M-SOCIAL \uC5F0\uC8FC\uD574\uBCF4\uAE30_\uC0AC\uC6B4\uB4DC 3.mp4", "category": "PlayInstrument", "feedbackPage": "PlayInstrument", "group": "sound"},
    {"index": 8, "id": "fb_009", "legacyFileName": "M-SOCIAL \uC5F0\uC8FC\uD574\uBCF4\uAE30_\uC0AC\uC6B4\uB4DC 4.mp4", "category": "PlayInstrument", "feedbackPage": "PlayInstrument", "group": "sound"},
    {"index": 9, "id": "fb_010", "legacyFileName": "M-SOCIAL \uCDA4\uCD94\uAE30_1 (\uB0A0\uBD10\uC918).mp4", "category": "Dance", "feedbackPage": "Dance"},
    {"index": 10, "id": "fb_011", "legacyFileName": "M-SOCIAL \uCDA4\uCD94\uAE30_2 (\uB0A0\uC544\uBD10).mp4", "category": "Dance", "feedbackPage": "Dance"},
    {"index": 11, "id": "fb_012", "legacyFileName": "M-SOCIAL \uCDA4\uCD94\uAE30_3 (\uC2A4\uC6E8\uADF8).mp4", "category": "Dance", "feedbackPage": "Dance"},
    {"index": 12, "id": "fb_013", "legacyFileName": "M-SOCIAL \uCDA4\uCD94\uAE30_4 (\uC5F0\uC8FC).mp4", "category": "Dance", "feedbackPage": "Dance"},
    {"index": 13, "id": "fb_014", "legacyFileName": "M-SOCIAL \uD638\uD761 \uD2B8\uB808\uC774\uB2DD_\uADE0\uC77C.mp4", "category": "Breath", "feedbackPage": "Breath"},
    {"index": 14, "id": "fb_015", "legacyFileName": "M-SOCIAL \uD638\uD761 \uD2B8\uB808\uC774\uB2DD_\uBC15\uC2A4.mp4", "category": "Breath", "feedbackPage": "Breath"},
]


def unity_quoted(s: str) -> str:
    out = ['"']
    for ch in s:
        o = ord(ch)
        if o < 128 and ch not in ("\\", '"'):
            out.append(ch)
        else:
            out.append(f"\\u{o:04X}")
    out.append('"')
    return "".join(out)


def enrich(m):
    m = dict(m)
    m["newFileName"] = m["id"] + ".mp4"
    m["legacyRel"] = m["legacyFileName"]
    m["newRel"] = m["newFileName"]
    return m


def main():
    entries = [enrich(m) for m in MAPPINGS]
    doc = {
        "version": 1,
        "description": "Stage1 feedback StreamingAssets MP4: Korean legacyFileName -> fb_XXX",
        "entries": entries,
    }

    json_path = os.path.join(SA, "feedback_names.json")
    if not DRY:
        with open(json_path, "w", encoding="utf-8", newline="\n") as f:
            json.dump(doc, f, ensure_ascii=False, indent=2)
            f.write("\n")
        print("Wrote", json_path)

    on_disk = {name: os.path.join(SA, name) for name in os.listdir(SA) if name.lower().endswith(".mp4")}
    renamed = 0
    for m in entries:
        legacy = m["legacyFileName"]
        new_name = m["newFileName"]
        src = on_disk.get(legacy)
        if not src:
            if new_name in on_disk:
                print("SKIP already", new_name)
                continue
            print("On disk:", list(on_disk.keys()))
            raise SystemExit("Missing source file: " + legacy)
        dst = os.path.join(SA, new_name)
        if DRY:
            print("RENAME", legacy, "->", new_name)
            continue
        if os.path.exists(dst) and os.path.abspath(src) != os.path.abspath(dst):
            os.remove(dst)
            meta_dst = dst + ".meta"
            if os.path.exists(meta_dst):
                os.remove(meta_dst)
        shutil.move(src, dst)
        meta_src = src + ".meta"
        if os.path.exists(meta_src):
            shutil.move(meta_src, dst + ".meta")
        on_disk.pop(legacy, None)
        on_disk[new_name] = dst
        renamed += 1
        print("RENAMED", legacy, "->", new_name)

    for scene_path in SCENES:
        if not os.path.isfile(scene_path):
            continue
        with open(scene_path, encoding="utf-8") as f:
            text = f.read()
        original = text
        for m in entries:
            old_q = unity_quoted(m["legacyFileName"])
            new_q = '"' + m["newFileName"] + '"'
            text = text.replace(old_q, new_q)
            text = text.replace('"' + m["legacyFileName"] + '"', new_q)
        if text != original:
            if DRY:
                print("WOULD UPDATE", scene_path)
            else:
                with open(scene_path, "w", encoding="utf-8", newline="\n") as f:
                    f.write(text)
                print("UPDATED", scene_path)
        else:
            print("NO CHANGE", scene_path)

    print("Done renamed=", renamed, "dry=", DRY)


if __name__ == "__main__":
    main()
