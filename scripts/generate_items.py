import csv
import os
import shutil

ITEMS_DIR = os.path.join(os.path.dirname(__file__), "..", "resources", "items")
CSV_PATH = r"D:\Downloads\items_100.csv"

SCRIPT_REF = '[ext_resource type="Script" path="res://src/items/ItemResource.cs" id="1_ItemResource"]'

def to_snake_case(name):
    return name.lower().replace(" ", "_").replace("-", "_")

def to_tag(s):
    return s.lower().replace(" ", "-")

def make_tres(item_name, category, attrs):
    snake = to_snake_case(item_name)
    item_id = f"item_{snake}"
    individual_tag = f"i_{snake}"

    all_tags = []
    seen = set()
    for raw in [category] + attrs:
        t = to_tag(raw.strip())
        if t and t not in seen:
            seen.add(t)
            all_tags.append(t)
    all_tags.append(individual_tag)

    tags_str = ", ".join(f'"{t}"' for t in all_tags)

    return f"""[gd_resource type="Resource" script_class="ItemResource" format=3]

{SCRIPT_REF}

[resource]
script = ExtResource("1_ItemResource")
Id = "{item_id}"
DisplayName = "{item_name}"
Description = ""
Tags = PackedStringArray({tags_str})
BaseValue = 1
"""

# Clear existing items
if os.path.exists(ITEMS_DIR):
    for f in os.listdir(ITEMS_DIR):
        if f.endswith(".tres"):
            os.remove(os.path.join(ITEMS_DIR, f))
            print(f"Deleted: {f}")
os.makedirs(ITEMS_DIR, exist_ok=True)

# Parse CSV and generate new items
with open(CSV_PATH, newline="", encoding="utf-8") as f:
    reader = csv.reader(f)
    next(reader)  # skip header
    count = 0
    for row in reader:
        if not row or not row[0].strip():
            continue
        item_name = row[0].strip()
        category = row[1].strip() if len(row) > 1 else ""
        attrs = [row[i].strip() for i in range(2, 10) if i < len(row) and row[i].strip()]

        filename = to_snake_case(item_name) + ".tres"
        content = make_tres(item_name, category, attrs)
        path = os.path.join(ITEMS_DIR, filename)
        with open(path, "w", encoding="utf-8") as out:
            out.write(content)
        print(f"Created: {filename}")
        count += 1

print(f"\nDone: {count} items created.")
