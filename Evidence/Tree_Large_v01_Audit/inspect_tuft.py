import bpy
import bmesh
from mathutils import Vector

foliage = bpy.data.objects.get("Foliage")
bm = bmesh.new()
bm.from_mesh(foliage.data)
bm.faces.ensure_lookup_table()

# Group leaves
visited = set()
leaves = []
for face in bm.faces:
    if face in visited:
        continue
    comp_faces = []
    queue = [face]
    visited.add(face)
    while queue:
        f = queue.pop()
        comp_faces.append(f)
        for e in f.edges:
            for linked in e.link_faces:
                if linked not in visited:
                    visited.add(linked)
                    queue.append(linked)
    bm_verts = list(set(v for f in comp_faces for v in f.verts))
    cen = sum((v.co for v in bm_verts), Vector((0,0,0))) / len(bm_verts)
    leaves.append((cen, comp_faces, bm_verts))

cen0 = leaves[0][0]
cluster_leaves = [i for i, l in enumerate(leaves) if (l[0] - cen0).length < 0.1]
uv_lay = bm.loops.layers.uv.verify()

print("\n--- CLUSTER LEAVES AROUND CEN0 ---")
for idx in cluster_leaves:
    v_map = {}
    for f in leaves[idx][1]:
        for loop in f.loops:
            uv = loop[uv_lay].uv
            co = loop.vert.co
            key = (round(uv.x, 2), round(uv.y, 2))
            v_map[key] = co
    base = v_map.get((0.5, 0.0))
    tip = v_map.get((0.5, 1.0))
    mid = v_map.get((0.5, 0.5))
    left = v_map.get((0.0, 0.5))
    right = v_map.get((1.0, 0.5))
    vec = tip - base
    print(f"Leaf {idx}: base={tuple(round(c, 3) for c in base)}, tip={tuple(round(c, 3) for c in tip)}, len={vec.length:.3f}, dir={tuple(round(c, 3) for c in vec.normalized())}")

bm.free()
