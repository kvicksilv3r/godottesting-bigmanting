"""
Downloads game-icons.net SVGs for each item and patches resources/items/*.tres
to reference the downloaded icon instead of the placeholder res://icon.svg.

Requirements: pip install requests
"""

import os
import re
import time
import json
import requests
from difflib import SequenceMatcher

# ── Config ────────────────────────────────────────────────────────────────────

ITEMS_DIR = r'I:\Projects\Godot\big-man-ting\resources\items'
ICONS_DIR = r'I:\Projects\Godot\big-man-ting\assets\art\sprites\items'
MAPPING_CACHE = r'I:\Projects\Godot\big-man-ting\scripts\icon_mapping.json'

RAW_BASE = "https://raw.githubusercontent.com/game-icons/icons/master"
TREE_API = "https://api.github.com/repos/game-icons/icons/git/trees/master?recursive=1"

# Preference overrides: item_id -> icon path fragment (author/icon-name) or full path in tree
OVERRIDES = {
    "black_hole":   "delapouite/ringed-planet",
    "duct_tape":    "delapouite/duct-tape",
    "echo":         "delapouite/echo-ripples",
    "gossip":       "delapouite/talk",
    "silence":      "delapouite/quiet",
    "mud":          "delapouite/mud",
    "lava":         "lorc/lava",
    "steam":        "delapouite/steam",
    "smoke":        "delapouite/smoke",
    "shadow":       "delapouite/shadow-follower",
    "dream":        "delapouite/dream",
    "luck":         "delapouite/clover",
    "time":         "delapouite/clock",
    "sadness":      "delapouite/crying",
    "fear":         "lorc/screaming",
    "anger":        "lorc/angry-eyes",
    "joy":          "lorc/smiling-face",
    "love":         "lorc/heart",
    "secret":       "delapouite/secret-book",
    "ghost":        "lorc/ghost",
    "tear":         "delapouite/tear-tracks",
    "blood":        "lorc/blood",
    "venom":        "lorc/venom",
    "web":          "lorc/spiderweb",
    "feather":      "lorc/feather",
    "seed":         "lorc/acorn",
    "ocean":        "lorc/ocean",
    "forest":       "lorc/pine-tree",
    "mountain":     "delapouite/mountain",
    "galaxy":       "lorc/galaxy",
    "meteor":       "lorc/meteor-impact",
    "comet":        "lorc/comet",
    "planet":       "lorc/ringed-planet",
    "lightning":    "lorc/lightning-bolt",
    "rainbow":      "lorc/rainbow",
    "fog":          "lorc/fog",
    "wind":         "lorc/wind-slap",
    "flour":        "delapouite/wheat",
    "dough":        "delapouite/bread-slice",
    "yeast":        "delapouite/beer-stein",
    "cheese":       "delapouite/cheese-wedge",
    "chili":        "delapouite/chili-pepper",
    "chocolate":    "delapouite/chocolate-bar",
    "pizza":        "delapouite/pizza-slice",
    "soup":         "delapouite/hot-meal",
    "beer":         "delapouite/beer-stein",
    "wine":         "lorc/wine-glass",
    "coffee":       "delapouite/coffee-mug",
    "sugar":        "delapouite/sugar-cane",
    "salt":         "delapouite/salt-shaker",
    "honey":        "delapouite/honeycomb",
    "butter":       "delapouite/butter",
    "milk":         "delapouite/milk-carton",
    "egg":          "delapouite/egg",
    "bread":        "delapouite/bread-slice",
    "cake":         "delapouite/cake-slice",
    "candle":       "delapouite/candle",
    "clock":        "delapouite/wall-clock",
    "mirror":       "delapouite/mirror",
    "book":         "delapouite/book-cover",
    "key":          "delapouite/key",
    "hammer":       "delapouite/hammer",
    "nail":         "delapouite/nail",
    "saw":          "delapouite/hand-saw",
    "drill":        "delapouite/drill",
    "rope":         "delapouite/rope-coil",
    "glue":         "delapouite/glue",
    "gorilla":      "delapouite/gorilla",
    "zebra":        "delapouite/zebra",
    "whale":        "delapouite/whale",
    "snail":        "delapouite/snail",
    "crow":         "lorc/crow",
    "spider":       "lorc/spider",
    "owl":          "lorc/owl",
    "tiger":        "lorc/tiger",
    "bee":          "lorc/bee",
    "fish":         "delapouite/fish",
    "cat":          "delapouite/sitting-cat",
    "dog":          "delapouite/dog-bowl",
    "coconut":      "delapouite/coconuts",
    "watermelon":   "delapouite/watermelon",
    "banana":       "delapouite/banana-bunch",
    "grape":        "delapouite/grapes",
    "peach":        "delapouite/peach",
    "lemon":        "delapouite/lemon",
    "cherry":       "delapouite/cherry",
    "apple":        "delapouite/apple",
    "eye":          "lorc/eye",
    "bone":         "lorc/bone",
    "tooth":        "lorc/tooth",
    "heart":        "lorc/anatomical-heart",
    "music":        "lorc/musical-notes",
    "echo":         "delapouite/echo-ripples",
}

# ── Helpers ───────────────────────────────────────────────────────────────────

def to_id(name):
    return re.sub(r'[^a-z0-9]+', '_', name.lower()).strip('_')

def similarity(a, b):
    return SequenceMatcher(None, a, b).ratio()

def best_match(item_id, icon_names):
    """Return the icon name with highest similarity score."""
    search = item_id.replace('_', '-')
    best, best_score = None, 0.0
    for name in icon_names:
        s = similarity(search, name)
        if s > best_score:
            best_score = s
            best = name
    return best, best_score

# ── Fetch icon tree ────────────────────────────────────────────────────────────

def fetch_icon_index():
    """Returns dict: icon_slug -> raw_download_path (relative to RAW_BASE)"""
    print("Fetching icon tree from GitHub API...")
    headers = {"Accept": "application/vnd.github.v3+json"}
    r = requests.get(TREE_API, headers=headers, timeout=30)
    r.raise_for_status()
    data = r.json()

    if data.get("truncated"):
        print("WARNING: tree was truncated — some icons may be missing from auto-match")

    index = {}
    for entry in data.get("tree", []):
        path = entry["path"]
        if not path.endswith(".svg"):
            continue
        parts = path.split("/")
        icon_name = parts[-1][:-4]   # strip .svg
        index[icon_name] = path

    print(f"  Found {len(index)} SVG icons")
    return index

# ── Main ──────────────────────────────────────────────────────────────────────

def main():
    os.makedirs(ICONS_DIR, exist_ok=True)

    # Load or build icon index
    if os.path.exists(MAPPING_CACHE):
        print(f"Loading cached icon index from {MAPPING_CACHE}")
        with open(MAPPING_CACHE, encoding="utf-8") as f:
            cache = json.load(f)
        icon_index = cache.get("index", {})
        resolved  = cache.get("resolved", {})
    else:
        icon_index = fetch_icon_index()
        resolved = {}
        with open(MAPPING_CACHE, "w", encoding="utf-8") as f:
            json.dump({"index": icon_index, "resolved": resolved}, f, indent=2)

    icon_names = list(icon_index.keys())

    # Collect all item .tres files
    tres_files = [f for f in os.listdir(ITEMS_DIR) if f.endswith(".tres")]
    tres_files.sort()

    print(f"\nProcessing {len(tres_files)} items...\n")

    results = {}   # item_id -> local svg path (res:// relative)

    for filename in tres_files:
        item_id = filename[:-5]   # strip .tres

        # Determine icon path to download
        if item_id in OVERRIDES:
            slug_hint = OVERRIDES[item_id].split("/")[-1]
            if slug_hint in icon_index:
                icon_slug = slug_hint
                tree_path = icon_index[slug_hint]
            else:
                # Hint slug not in index — fall through to fuzzy
                icon_slug, score = best_match(item_id, icon_names)
                tree_path = icon_index[icon_slug]
                print(f"  OVERRIDE miss for {item_id}, fell back to '{icon_slug}' ({score:.2f})")
        elif item_id in resolved:
            icon_slug = resolved[item_id]
            tree_path = icon_index.get(icon_slug, "")
        else:
            icon_slug, score = best_match(item_id, icon_names)
            tree_path = icon_index[icon_slug]
            resolved[item_id] = icon_slug

        if not tree_path:
            print(f"  SKIP (no path): {item_id}")
            continue

        # Download SVG
        local_filename = f"{item_id}.svg"
        local_path = os.path.join(ICONS_DIR, local_filename)
        res_path = f"res://assets/art/sprites/items/{local_filename}"

        if not os.path.exists(local_path):
            url = f"{RAW_BASE}/{tree_path}"
            try:
                resp = requests.get(url, timeout=15)
                resp.raise_for_status()
                with open(local_path, "w", encoding="utf-8") as f:
                    f.write(resp.text)
                print(f"  OK  {item_id:25s} <- {icon_slug}")
                time.sleep(0.1)  # be polite
            except Exception as e:
                print(f"  ERR {item_id}: {e}")
                continue
        else:
            print(f"  --  {item_id:25s} (already downloaded)")

        results[item_id] = res_path

    # Persist updated resolved map
    with open(MAPPING_CACHE, "w", encoding="utf-8") as f:
        json.dump({"index": icon_index, "resolved": resolved}, f, indent=2)

    # Patch .tres files
    print("\nPatching .tres files...")
    patched = 0
    for filename in tres_files:
        item_id = filename[:-5]
        res_path = results.get(item_id)
        if not res_path:
            continue

        tres_path = os.path.join(ITEMS_DIR, filename)
        with open(tres_path, encoding="utf-8") as f:
            content = f.read()

        # Replace any existing Texture2D ext_resource line
        new_line = f'[ext_resource type="Texture2D" path="{res_path}" id="1_icon"]'
        new_content = re.sub(
            r'\[ext_resource type="Texture2D"[^\]]*\]',
            new_line,
            content
        )

        if new_content != content:
            with open(tres_path, "w", encoding="utf-8") as f:
                f.write(new_content)
            patched += 1

    print(f"Patched {patched} .tres files.")
    print("\nDone. Open Godot — it will import all SVGs automatically.")
    print("Tip: select all icons in the FileSystem and set Import > Mode to 'Texture2D' if needed.")

if __name__ == "__main__":
    main()
