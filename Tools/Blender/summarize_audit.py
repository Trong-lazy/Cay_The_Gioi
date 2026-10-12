import json
from pathlib import Path

ROOT = Path("c:/Users/Asus/foder game 3D/Cay_The_Gioi")
report_file = ROOT / "Evidence/P01_Audit_Initial.json"

with open(report_file, 'r', encoding='utf-8') as f:
    data = json.load(f)

for label, model in data.items():
    print(f"=== {label} ===")
    print(f"  Path: {model['filepath']}")
    print(f"  SHA256: {model['sha256']}")
    print(f"  Total verts: {model['total_vertices']}, Total tris: {model['total_triangles']}")
    for obj in model['objects']:
        if obj.get('type') != 'MESH':
            print(f"  Non-mesh object: {obj['name']} ({obj.get('type')})")
            continue
        uvs = [u['name'] + ' (u:' + str(u['u_range']) + ', v:' + str(u['v_range']) + ')' for u in obj.get('uv_layers', [])]
        mats = []
        for s in obj.get('material_slots', []):
            m = s.get('material')
            if m:
                p = m.get('principled', {})
                bc = p.get('Base Color', 'none') if p else 'none'
                r = p.get('Roughness', 'none') if p else 'none'
                mats.append(f"{m['name']} (faces: {s['assigned_faces']}, BC: {bc}, R: {r})")
            else:
                mats.append(f"None (faces: {s['assigned_faces']})")
        print(f"  Mesh: {obj['name']} (verts: {obj.get('vertices', 0)}, tris: {obj.get('triangles', 0)})")
        print(f"    UVs: {uvs}")
        print(f"    Materials: {mats}")
    print()
