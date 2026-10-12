import bpy
import bmesh
from mathutils import Vector

print("\n=== BONSAI_TRUNK_CANDIDATE_V01 INSPECTION ===")
for obj in bpy.data.objects:
    if obj.type == 'MESH':
        m = obj.data
        m.calc_loop_triangles()
        print(f"Object: {obj.name:15} | Verts: {len(m.vertices):5} | Faces: {len(m.polygons):5} | Tris: {len(m.loop_triangles):5}")
        print(f"  Materials: {[s.material.name if s.material else None for s in obj.material_slots]}")
        print(f"  UV Layers: {[uv.name for uv in m.uv_layers]}")
        print(f"  Dimensions: {obj.dimensions}")

# Inspect Foliage structure specifically
foliage = bpy.data.objects.get("Foliage")
if foliage:
    bm = bmesh.new()
    bm.from_mesh(foliage.data)
    # Check connected components (individual leaves or leaf clusters)
    visited = set()
    components = []
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
        components.append(comp_faces)
    print(f"\nFoliage Connected Components (Islands): {len(components)}")
    if components:
        sample_counts = [len(c) for c in components[:10]]
        print(f"  Sample component face counts: {sample_counts}")
        # Analyze first component
        sample_comp = components[0]
        verts = set()
        for f in sample_comp:
            for v in f.verts:
                verts.add(v.co)
        xs = [v.x for v in verts]
        ys = [v.y for v in verts]
        zs = [v.z for v in verts]
        print(f"  Sample component 0 bounds: dx={max(xs)-min(xs):.4f}, dy={max(ys)-min(ys):.4f}, dz={max(zs)-min(zs):.4f}")
        print(f"  Total vertices in sample component: {len(verts)}")
    bm.free()
