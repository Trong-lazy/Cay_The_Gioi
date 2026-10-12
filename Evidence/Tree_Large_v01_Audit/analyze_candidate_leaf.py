import bpy
import bmesh
from mathutils import Vector

foliage = bpy.data.objects.get("Foliage")
bm = bmesh.new()
bm.from_mesh(foliage.data)
bm.faces.ensure_lookup_table()

# Group leaves (each leaf is 4 connected faces)
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
    # compute center of leaf
    bm_verts = list(set(v for f in comp_faces for v in f.verts))
    cen = sum((v.co for v in bm_verts), Vector((0,0,0))) / len(bm_verts)
    leaves.append((cen, comp_faces, bm_verts))

print(f"Total leaves: {len(leaves)}")

# Check distance between leaf bases to see sprays / rosettes
# For leaf 0, find closest 10 leaves
cen0 = leaves[0][0]
dists = sorted([( (l[0] - cen0).length, i) for i, l in enumerate(leaves)])
print(f"Closest leaves to leaf 0: {[round(d, 3) for d, i in dists[:10]]}")

# Check local coordinate geometry of leaf 0 in its own local frame:
verts0 = leaves[0][2]
# Find base, left, mid, right, tip using UVs
uv_lay = bm.loops.layers.uv.verify()
v_map = {}
for f in leaves[0][1]:
    for loop in f.loops:
        uv = loop[uv_lay].uv
        co = loop.vert.co
        key = (round(uv.x, 2), round(uv.y, 2))
        v_map[key] = co

print("\n--- LEAF 0 VERTICES BY UV ---")
for k, co in v_map.items():
    print(f"  UV {k}: {co}")

base = v_map.get((0.5, 0.0))
mid = v_map.get((0.5, 0.5))
tip = v_map.get((0.5, 1.0))
left = v_map.get((0.0, 0.5))
right = v_map.get((1.0, 0.5))

if base and mid and tip and left and right:
    # Compute local axes
    length_vec = tip - base
    width_vec = right - left
    print(f"\nLeaf length: {length_vec.length:.4f} m")
    print(f"Leaf width:  {width_vec.length:.4f} m")
    # Fold angle: angle between triangle (base, left, mid) and (base, mid, right)
    norm_left = (left - base).cross(mid - base).normalized()
    norm_right = (mid - base).cross(right - base).normalized()
    fold_angle_deg = norm_left.angle(norm_right) * 180.0 / 3.14159
    print(f"Leaf V-fold angle: {fold_angle_deg:.2f} degrees")

bm.free()
