import hashlib
import glob
import os

files = [
    "Assets/_Project/Art/Models/Tree_Seedling_Simple_v01.fbx",
    "Assets/_Project/Art/Models/Tree_Young_Simple_v01.fbx",
    "Assets/_Project/Art/Models/Tree_Large.fbx",
    "Assets/_Project/Prefabs/Trees/seedling.prefab",
    "Assets/_Project/Prefabs/Trees/young_tree.prefab",
    "Assets/_Project/Prefabs/Trees/large_tree.prefab",
]

# Add any blend files if present
blend_files = glob.glob("Assets/_Project/Art/Models/*.blend") + glob.glob("Art/SourceModels/*.blend")
files.extend(blend_files)

print("=== PROTECTED TREE ASSETS SHA256 ===")
for f in files:
    if os.path.exists(f):
        h = hashlib.sha256(open(f, "rb").read()).hexdigest()
        print(f"{f}: {h}")
    else:
        print(f"{f}: MISSING")

save_path = os.path.expandvars(r"%LOCALAPPDATA%Low\DefaultCompany\Cay_The_Gioi\world_tree_save.json")
if os.path.exists(save_path):
    h_save = hashlib.sha256(open(save_path, "rb").read()).hexdigest()
    print(f"\nREAL SAVE SHA256: {h_save}")
