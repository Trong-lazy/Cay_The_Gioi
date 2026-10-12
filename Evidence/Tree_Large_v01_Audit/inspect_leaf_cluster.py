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
# Find all leaves within 0.1m of cen0
cluster_leaves = [i for i, l in enumerate(leaves) if (l[0] - cen0).length < 0.1]
print(f"Leaves in cluster around leaf 0 (within 0.1m): {len(cluster_leaves)}")

for idx in cluster_leaves:
    comp_faces = leaves[idx][1]
    verts = leaves[idx][2]
    print(f"Leaf {idx}: cen={tuple(round(c, 3) for c in leaves[idx][0])}")
    # Face normals
    norms = [tuple(round(c, 3) for c in f.normal) for f in comp_faces]
    print(f"  Face normals: {norms}")

# Also check materials of Foliage in Bonsai_Trunk_Candidate_v01
print(f"Foliage material slots: {[s.material.name if s.material else None for s in foliage.material_slots]}")

bm.free()
