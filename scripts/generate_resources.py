import csv
import os
import re

def to_id(name):
    return re.sub(r'[^a-z0-9]+', '_', name.lower()).strip('_')

def to_itag(name):
    return 'i_' + to_id(name)

BASE_VALUES = {
    'Fruit': 2, 'Animal': 5, 'Tool': 3, 'Food': 2,
    'Element': 3, 'Space': 5, 'Weather': 3, 'Emotion': 4,
    'Abstract': 4, 'Body': 3, 'Misc': 3,
}

BONUS_POINTS = {'Easy': 5, 'Medium': 10, 'Hard': 20}

ITEMS_DIR  = r'I:\Projects\Godot\big-man-ting\resources\items'
COMBOS_DIR = r'I:\Projects\Godot\big-man-ting\resources\combos'

os.makedirs(ITEMS_DIR,  exist_ok=True)
os.makedirs(COMBOS_DIR, exist_ok=True)

# ── Items ────────────────────────────────────────────────────────────────────

with open(r'D:\Downloads\items_100.csv', newline='', encoding='utf-8') as f:
    reader = csv.DictReader(f)
    for i, row in enumerate(reader):
        name     = row['Item'].strip()
        category = row['Category'].strip()

        # identity tag first, then category, then attributes
        raw_tags = [to_itag(name), category.lower()]
        for j in range(1, 9):
            attr = row.get(f'Attribute {j}', '').strip()
            if attr:
                raw_tags.append(attr.lower().replace(' ', '_').replace('-', '_'))

        seen, tags = set(), []
        for t in raw_tags:
            if t not in seen:
                seen.add(t)
                tags.append(t)

        item_id    = 'item_' + to_id(name)
        base_value = BASE_VALUES.get(category, 2)
        uid        = f'uid://gen_item_{i+1:04d}'
        tags_str   = ', '.join(f'"{t}"' for t in tags)
        filename   = to_id(name) + '.tres'

        lines = [
            f'[gd_resource type="Resource" script_class="ItemResource" format=3 uid="{uid}"]',
            '',
            '[ext_resource type="Texture2D" uid="uid://bup8xqnpyg2mp" path="res://icon.svg" id="1_icon"]',
            '[ext_resource type="Script" uid="uid://c28rug6k7py1q" path="res://src/items/ItemResource.cs" id="1_ItemResource"]',
            '',
            '[resource]',
            'script = ExtResource("1_ItemResource")',
            f'Id = "{item_id}"',
            f'DisplayName = "{name}"',
            'Icon = ExtResource("1_icon")',
            f'Tags = PackedStringArray({tags_str})',
            f'BaseValue = {base_value}',
            '',
        ]

        with open(os.path.join(ITEMS_DIR, filename), 'w', encoding='utf-8') as out:
            out.write('\n'.join(lines))

        print(f'  item  {filename}')

# ── Combos ───────────────────────────────────────────────────────────────────

with open(r'D:\Downloads\combos_100.csv', newline='', encoding='utf-8') as f:
    reader = csv.DictReader(f)
    for i, row in enumerate(reader):
        combo_name  = row['Combo Name'].strip()
        difficulty  = row['Difficulty'].strip()
        hint        = row['Hint'].strip().replace('"', '\\"')
        unlocks     = row['Unlocks'].strip()

        req_items = []
        for j in range(1, 6):
            item = row.get(f'Item {j}', '').strip()
            if item:
                req_items.append(item)

        bonus_points   = BONUS_POINTS.get(difficulty, 10)
        reward_item_id = ('item_' + to_id(unlocks)) if unlocks else ''
        uid            = f'uid://gen_combo_{i+1:04d}'
        filename       = to_id(combo_name) + '.tres'

        lines = [
            f'[gd_resource type="Resource" script_class="ComboRule" format=3 uid="{uid}"]',
            '',
            '[ext_resource type="Script" uid="uid://cfsh482e26t2d" path="res://src/items/ComboRule.cs" id="1_ComboRule"]',
            '[ext_resource type="Script" uid="uid://b3di20sdpxocd" path="res://src/items/TagRequirement.cs" id="2_TagReq"]',
            '',
        ]

        sub_refs = []
        for j, item in enumerate(req_items):
            res_id = f'Req_{j + 1}'
            tag    = to_itag(item)
            lines += [
                f'[sub_resource type="Resource" id="{res_id}"]',
                'script = ExtResource("2_TagReq")',
                f'Tag = "{tag}"',
                'Count = 1',
                '',
            ]
            sub_refs.append(f'SubResource("{res_id}")')

        requirements = f'Array[ExtResource("2_TagReq")]([{", ".join(sub_refs)}])'

        lines += [
            '[resource]',
            'script = ExtResource("1_ComboRule")',
            f'ComboName = "{combo_name}"',
            f'Description = "{hint}"',
            f'Requirements = {requirements}',
            f'BonusPoints = {bonus_points}',
            f'RewardItemId = "{reward_item_id}"',
            '',
        ]

        with open(os.path.join(COMBOS_DIR, filename), 'w', encoding='utf-8') as out:
            out.write('\n'.join(lines))

        print(f'  combo {filename}')

print('\nDone.')
