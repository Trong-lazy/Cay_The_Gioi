import bpy
import bmesh
from mathutils import Vector

roots = bpy.data.objects.get("Roots")
if roots:
    print(f"Bonsai Roots: {len(roots.data.vertices)} verts, {len(roots.data.polygons)} polys")
    print(f"Bounds: {roots.dimensions}")
    # Inspect vertices near z=0
    verts = roots.data.vertices
    z_coords = [v.co.z for v in verts]
    print(f"Roots z-range: [{min(z_coords):.3f}, {max(z_coords):.3f}]")
