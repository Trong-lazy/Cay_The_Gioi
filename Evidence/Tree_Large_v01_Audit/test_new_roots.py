import bpy
import bmesh
from mathutils import Vector
import math

# Clear any previous test objects
for name in ["TestNewRoots", "TestTrunkBase"]:
    obj = bpy.data.objects.get(name)
    if obj:
        bpy.data.objects.remove(obj, do_unlink=True)

mesh_roots = bpy.data.meshes.new("TestNewRoots")
obj_roots = bpy.data.objects.new("TestNewRoots", mesh_roots)
bpy.context.collection.objects.link(obj_roots)

bm = bmesh.new()

def create_fluted_tube_bmesh(bm, path_points_with_radius, num_radial=12, cap_start=False, cap_end=True, 
                             flute_count=3, flute_depth=0.04, mat_idx=0, flatten_z=1.0):
    n_pts = len(path_points_with_radius)
    if n_pts < 2:
        return []
    
    frames = []
    prev_n = None
    
    for i in range(n_pts):
        pt, r = path_points_with_radius[i]
        if i == 0:
            tangent = (path_points_with_radius[1][0] - pt).normalized()
        elif i == n_pts - 1:
            tangent = (pt - path_points_with_radius[i-1][0]).normalized()
        else:
            t1 = (pt - path_points_with_radius[i-1][0]).normalized()
            t2 = (path_points_with_radius[i+1][0] - pt).normalized()
            tangent = (t1 + t2).normalized()
            
        if prev_n is None:
            up = Vector((0, 0, 1))
            if abs(tangent.dot(up)) > 0.85:
                up = Vector((0, 1, 0))
            normal = tangent.cross(up).normalized()
            binormal = tangent.cross(normal).normalized()
        else:
            normal = (prev_n - tangent * tangent.dot(prev_n)).normalized()
            binormal = tangent.cross(normal).normalized()
            
        prev_n = normal
        frames.append((pt, r, normal, binormal))
        
    rings = []
    for pt, r, norm, binorm in frames:
        ring_verts = []
        for j in range(num_radial):
            theta = 2.0 * math.pi * j / num_radial
            flute = 1.0 + flute_depth * math.cos(flute_count * theta) if flute_count > 0 else 1.0
            rf = r * flute
            # Apply slight vertical flattening near the ground if specified
            offset = (norm * math.cos(theta) + binorm * math.sin(theta) * flatten_z) * rf
            v = bm.verts.new(pt + offset)
            ring_verts.append(v)
        rings.append(ring_verts)
        
    for i in range(n_pts - 1):
        r1 = rings[i]
        r2 = rings[i+1]
        for j in range(num_radial):
            jn = (j + 1) % num_radial
            f = bm.faces.new([r1[j], r1[jn], r2[jn], r2[j]])
            f.material_index = mat_idx
            f.smooth = True
            
    if cap_start:
        f = bm.faces.new(list(reversed(rings[0])))
        f.material_index = mat_idx
        f.smooth = True
    if cap_end:
        f = bm.faces.new(rings[-1])
        f.material_index = mat_idx
        f.smooth = True
        
    return rings

# 1. Broad Root Flare Core (Chân thân bè rộng chuyển tiếp tự nhiên)
core_flare = [
    (Vector((0.000, 0.000, 0.030)), 0.170),
    (Vector((0.005, -0.005, 0.000)), 0.205),   # wide soft flare at z=0
    (Vector((0.008, -0.008, -0.040)), 0.218),  # buttress zone
    (Vector((0.005, -0.005, -0.090)), 0.150),  # tapers into ground anchor
    (Vector((0.000, 0.000, -0.150)), 0.050),   # rounded base
]
create_fluted_tube_bmesh(bm, core_flare, num_radial=16, cap_start=False, cap_end=True, flute_count=5, flute_depth=0.06)

# 2. Asymmetric Primary Roots
# Root 1: Main Left-Front Anchor (Thickest, crawls then dips)
root1_main = [
    (Vector((-0.070, -0.080, -0.010)), 0.105),
    (Vector((-0.220, -0.200, -0.035)), 0.085),
    (Vector((-0.380, -0.320, -0.075)), 0.065),
    (Vector((-0.540, -0.420, -0.140)), 0.045),
    (Vector((-0.680, -0.480, -0.240)), 0.020), # blunt natural tip
]
create_fluted_tube_bmesh(bm, root1_main, num_radial=10, cap_start=False, cap_end=True, flute_count=3, flute_depth=0.04)
# Root 1 secondary branch (splits at mid-length)
root1_sec = [
    (Vector((-0.380, -0.320, -0.075)), 0.052),
    (Vector((-0.520, -0.240, -0.120)), 0.034),
    (Vector((-0.640, -0.200, -0.190)), 0.015),
]
create_fluted_tube_bmesh(bm, root1_sec, num_radial=8, cap_start=False, cap_end=True, flute_count=0)

# Root 2: Front-Right Anchor (Curves forward-right)
root2_main = [
    (Vector((0.080, -0.070, -0.010)), 0.090),
    (Vector((0.240, -0.180, -0.035)), 0.072),
    (Vector((0.400, -0.280, -0.080)), 0.052),
    (Vector((0.540, -0.380, -0.150)), 0.035),
    (Vector((0.660, -0.460, -0.250)), 0.018),
]
create_fluted_tube_bmesh(bm, root2_main, num_radial=10, cap_start=False, cap_end=True, flute_count=3, flute_depth=0.04)
# Root 2 secondary branch (splits near end, turns forward)
root2_sec = [
    (Vector((0.400, -0.280, -0.080)), 0.042),
    (Vector((0.480, -0.420, -0.140)), 0.026),
    (Vector((0.540, -0.540, -0.220)), 0.014),
]
create_fluted_tube_bmesh(bm, root2_sec, num_radial=8, cap_start=False, cap_end=True, flute_count=0)

# Root 3: Far Right Lateral Surface Root (Crawls horizontally along surface)
root3_main = [
    (Vector((0.110, 0.030, -0.010)), 0.088),
    (Vector((0.320, 0.080, -0.030)), 0.070),
    (Vector((0.560, 0.140, -0.060)), 0.052),
    (Vector((0.760, 0.200, -0.110)), 0.034),
    (Vector((0.920, 0.240, -0.180)), 0.018),
]
create_fluted_tube_bmesh(bm, root3_main, num_radial=10, cap_start=False, cap_end=True, flute_count=3, flute_depth=0.04)
# Root 3 secondary branch (dips into soil)
root3_sec = [
    (Vector((0.320, 0.080, -0.030)), 0.045),
    (Vector((0.440, 0.040, -0.110)), 0.028),
    (Vector((0.520, 0.020, -0.210)), 0.014),
]
create_fluted_tube_bmesh(bm, root3_sec, num_radial=8, cap_start=False, cap_end=True, flute_count=0)

# Root 4: Rear-Right Anchor (Stocky structural root, NO fork)
root4_main = [
    (Vector((0.060, 0.100, -0.015)), 0.082),
    (Vector((0.180, 0.240, -0.045)), 0.064),
    (Vector((0.280, 0.380, -0.100)), 0.045),
    (Vector((0.360, 0.480, -0.180)), 0.020),
]
create_fluted_tube_bmesh(bm, root4_main, num_radial=8, cap_start=False, cap_end=True, flute_count=3, flute_depth=0.035)

# Root 5: Rear-Left Anchor (Curving backward-left)
root5_main = [
    (Vector((-0.070, 0.090, -0.015)), 0.084),
    (Vector((-0.200, 0.220, -0.045)), 0.066),
    (Vector((-0.320, 0.340, -0.095)), 0.048),
    (Vector((-0.420, 0.440, -0.170)), 0.022),
]
create_fluted_tube_bmesh(bm, root5_main, num_radial=8, cap_start=False, cap_end=True, flute_count=3, flute_depth=0.035)
# Root 5 secondary branch
root5_sec = [
    (Vector((-0.200, 0.220, -0.045)), 0.042),
    (Vector((-0.160, 0.360, -0.110)), 0.026),
    (Vector((-0.140, 0.460, -0.190)), 0.014),
]
create_fluted_tube_bmesh(bm, root5_sec, num_radial=8, cap_start=False, cap_end=True, flute_count=0)

bm.to_mesh(mesh_roots)
bm.free()

print(f"TestNewRoots created: {len(mesh_roots.vertices)} verts, {len(mesh_roots.polygons)} polys")
