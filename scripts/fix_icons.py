"""
Fixes the bad icon matches from the initial fetch run.
Downloads correct SVGs and re-patches the affected .tres files.
"""

import os, re, json, time, requests

ITEMS_DIR  = r'I:\Projects\Godot\big-man-ting\resources\items'
ICONS_DIR  = r'I:\Projects\Godot\big-man-ting\assets\art\sprites\items'
CACHE_FILE = r'I:\Projects\Godot\big-man-ting\scripts\icon_mapping.json'
RAW_BASE   = "https://raw.githubusercontent.com/game-icons/icons/master"

FIXES = {
    "air":      "air-balloon",
    "apple":    "shiny-apple",
    "bone":     "crossed-bones",
    "clock":    "alarm-clock",
    "comet":    "comet-spark",
    "crow":     "raven",
    "dream":    "dream-catcher",
    "dough":    "dough-roller",
    "duct_tape":"measure-tape",
    "earth":    "earth-crack",
    "egg":      "fried-eggs",
    "fish":     "circling-fish",
    "glue":     "bandage-roll",
    "joy":      "happy",
    "mirror":   "mirror-mirror",
    "mud":      "swamp",
    "ocean":    "wave-crest",
    "sadness":  "sad",
    "smoke":    "smoke-bomb",
    "storm":    "lightning-storm",
    "tree":     "willow-tree",
    "venom":    "poison",
    "whale":    "sperm-whale",
    "yeast":    "boiling-bubbles",
    "zebra":    "zebra-shield",
}

with open(CACHE_FILE, encoding="utf-8") as f:
    cache = json.load(f)
icon_index = cache["index"]

for item_id, slug in FIXES.items():
    tree_path  = icon_index[slug]
    local_file = os.path.join(ICONS_DIR, f"{item_id}.svg")
    res_path   = f"res://assets/art/sprites/items/{item_id}.svg"

    # Download
    url = f"{RAW_BASE}/{tree_path}"
    resp = requests.get(url, timeout=15)
    resp.raise_for_status()
    with open(local_file, "w", encoding="utf-8") as f:
        f.write(resp.text)
    print(f"  {item_id:20s} <- {slug}")
    time.sleep(0.1)

    # Patch .tres
    tres_path = os.path.join(ITEMS_DIR, f"{item_id}.tres")
    if not os.path.exists(tres_path):
        continue
    with open(tres_path, encoding="utf-8") as f:
        content = f.read()
    new_line    = f'[ext_resource type="Texture2D" path="{res_path}" id="1_icon"]'
    new_content = re.sub(r'\[ext_resource type="Texture2D"[^\]]*\]', new_line, content)
    with open(tres_path, "w", encoding="utf-8") as f:
        f.write(new_content)

print("\nDone.")
