import bpy
import bmesh
from mathutils import Vector

foliage = bpy.data.objects.get("Foliage")
bm = bmesh.new()
bm.from_mesh(foliage.data)
bm.faces.ensure_lookup_table()

# Find first component
visited = set()
comp_faces = []
queue = [bm.faces[0]]
visited.add(bm.faces[0])
while queue:
    f = queue.pop()
    comp_faces.append(f)
    for e in f.edges:
        for linked in e.link_faces:
            if linked not in visited:
                visited.add(linked)
                queue.append(linked)

print(f"\n--- SAMPLE LEAF COMPONENT 0 ---")
print(f"Number of faces: {len(comp_faces)}")
comp_verts = list(set(v for f in comp_faces for v in f.verts))
print(f"Number of vertices: {len(comp_verts)}")
for i, v in enumerate(comp_verts):
    print(f"  v{i}: {tuple(round(c, 5) for c in v.co)}")

for i, f in enumerate(comp_faces):
    face_v_indices = [comp_verts.index(v) for v in f.verts]
    print(f"  face {i}: {face_v_indices} (len: {len(f.verts)})")

# Check UVs
uv_lay = bm.loops.layers.uv.verify()
for i, f in enumerate(comp_faces):
    uvs = [tuple(round(c, 4) for c in loop[uv_lay].uv) for loop in f.loops]
    print(f"  face {i} UVs: {uvs}")

# Check dimensions and bounding box
xs = [v.co.x for v in comp_verts]
ys = [v.co.y for v in comp_verts]
zs = [v.co.z for v in comp_verts]
dx = max(xs) - min(xs)
dy = max(ys) - min(ys)
dz = max(zs) - min(zs)
print(f"Dimensions: dx={dx:.4f}, dy={dy:.4f}, dz={dz:.4f}")

# Check 5 different components to see if they are identical leaves scaled/rotated
for c_idx in [0, 10, 50, 100, 500]:
    # find face
    f_start = bm.faces[c_idx * 4]
    v_set = list(set(v for e in f_start.edges for f in e.link_faces for v in f.verts))
    print(f"Comp {c_idx} vert count: {len(v_set)}")

bm.free()
