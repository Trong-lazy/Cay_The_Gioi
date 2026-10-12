import bpy
import bmesh
import mathutils
from mathutils import Vector, Matrix, Euler
import math
import os

def clear_scene():
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    for block in list(bpy.data.meshes):
        bpy.data.meshes.remove(block)
    for block in list(bpy.data.materials):
        bpy.data.materials.remove(block)

def create_material(name, color, roughness=0.45):
    mat = bpy.data.materials.get(name)
    if not mat:
        mat = bpy.data.materials.new(name=name)
    mat.use_nodes = True
    mat.diffuse_color = color
    nodes = mat.node_tree.nodes
    bsdf = nodes.get("Principled BSDF")
    if bsdf:
        bsdf.inputs['Base Color'].default_value = color
        bsdf.inputs['Roughness'].default_value = roughness
    return mat

def create_fluted_tube_bmesh(bm, path_points_with_radius, num_radial=12, cap_start=False, cap_end=True, 
                             flute_count=5, flute_depth=0.055, twist_rate=1.0, mat_idx=0):
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
        phi = pt.z * twist_rate
        for j in range(num_radial):
            theta = 2.0 * math.pi * j / num_radial
            if flute_count > 0:
                flute = 1.0 + flute_depth * math.cos(flute_count * theta + phi) + (flute_depth * 0.4) * math.cos((flute_count - 2) * theta - phi * 0.5)
            else:
                flute = 1.0
            rf = r * flute
            offset = (norm * math.cos(theta) + binorm * math.sin(theta)) * rf
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

def add_bonsai_sync_leaf(bm, transform_mat, scale=1.0, droop=1.0, thickness=0.0035, mat_idx=0, uv_lay=None):
    """
    Creates a stylized leaf matching Bonsai_Trunk_Candidate_v01 proportions (1:2),
    V-fold (18.7 deg), arch, and UV layout, with physical two-sided thickness.
    """
    L = 0.32 * scale
    W = 0.16 * scale
    H_fold = W * 0.5 * math.tan(math.radians(18.7))
    H_arch = 0.024 * scale
    H_droop = -0.038 * scale * droop
    th = thickness * scale
    
    p_base = Vector((0.0,     0.0,     0.0))
    p_left = Vector((-W*0.5,  L*0.48,  H_arch * 0.5))
    p_mid  = Vector((0.0,     L*0.50,  H_arch + H_fold))
    p_right= Vector((W*0.5,   L*0.48,  H_arch * 0.5))
    p_tip  = Vector((0.0,     L,       H_droop))
    
    v_base_t  = bm.verts.new(transform_mat @ (p_base  + Vector((0, 0,  th*0.5))))
    v_left_t  = bm.verts.new(transform_mat @ (p_left  + Vector((0, 0,  th*0.5))))
    v_mid_t   = bm.verts.new(transform_mat @ (p_mid   + Vector((0, 0,  th*0.5))))
    v_right_t = bm.verts.new(transform_mat @ (p_right + Vector((0, 0,  th*0.5))))
    v_tip_t   = bm.verts.new(transform_mat @ (p_tip   + Vector((0, 0,  th*0.5))))
    
    v_base_b  = bm.verts.new(transform_mat @ (p_base  + Vector((0, 0, -th*0.5))))
    v_left_b  = bm.verts.new(transform_mat @ (p_left  + Vector((0, 0, -th*0.5))))
    v_mid_b   = bm.verts.new(transform_mat @ (p_mid   + Vector((0, 0, -th*0.5))))
    v_right_b = bm.verts.new(transform_mat @ (p_right + Vector((0, 0, -th*0.5))))
    v_tip_b   = bm.verts.new(transform_mat @ (p_tip   + Vector((0, 0, -th*0.5))))
    
    f_t0 = bm.faces.new([v_base_t, v_left_t, v_mid_t])
    f_t1 = bm.faces.new([v_base_t, v_mid_t, v_right_t])
    f_t2 = bm.faces.new([v_mid_t, v_tip_t, v_right_t])
    f_t3 = bm.faces.new([v_mid_t, v_left_t, v_tip_t])
    
    f_b0 = bm.faces.new([v_base_b, v_mid_b, v_left_b])
    f_b1 = bm.faces.new([v_base_b, v_right_b, v_mid_b])
    f_b2 = bm.faces.new([v_mid_b, v_right_b, v_tip_b])
    f_b3 = bm.faces.new([v_mid_b, v_tip_b, v_left_b])
    
    f_s0 = bm.faces.new([v_base_t, v_base_b, v_left_b, v_left_t])
    f_s1 = bm.faces.new([v_left_t, v_left_b, v_tip_b, v_tip_t])
    f_s2 = bm.faces.new([v_tip_t, v_tip_b, v_right_b, v_right_t])
    f_s3 = bm.faces.new([v_right_t, v_right_b, v_base_b, v_base_t])
    
    all_faces = [f_t0, f_t1, f_t2, f_t3, f_b0, f_b1, f_b2, f_b3, f_s0, f_s1, f_s2, f_s3]
    for f in all_faces:
        f.material_index = mat_idx
        f.smooth = True
        
    if uv_lay:
        f_t0.loops[0][uv_lay].uv = Vector((0.5, 0.0))
        f_t0.loops[1][uv_lay].uv = Vector((0.0, 0.5))
        f_t0.loops[2][uv_lay].uv = Vector((0.5, 0.5))
        
        f_t1.loops[0][uv_lay].uv = Vector((0.5, 0.0))
        f_t1.loops[1][uv_lay].uv = Vector((0.5, 0.5))
        f_t1.loops[2][uv_lay].uv = Vector((1.0, 0.5))
        
        f_t2.loops[0][uv_lay].uv = Vector((0.5, 0.5))
        f_t2.loops[1][uv_lay].uv = Vector((0.5, 1.0))
        f_t2.loops[2][uv_lay].uv = Vector((1.0, 0.5))
        
        f_t3.loops[0][uv_lay].uv = Vector((0.5, 0.5))
        f_t3.loops[1][uv_lay].uv = Vector((0.0, 0.5))
        f_t3.loops[2][uv_lay].uv = Vector((0.5, 1.0))
        
        f_b0.loops[0][uv_lay].uv = Vector((0.5, 0.0))
        f_b0.loops[1][uv_lay].uv = Vector((0.5, 0.5))
        f_b0.loops[2][uv_lay].uv = Vector((0.0, 0.5))
        
        f_b1.loops[0][uv_lay].uv = Vector((0.5, 0.0))
        f_b1.loops[1][uv_lay].uv = Vector((1.0, 0.5))
        f_b1.loops[2][uv_lay].uv = Vector((0.5, 0.5))
        
        f_b2.loops[0][uv_lay].uv = Vector((0.5, 0.5))
        f_b2.loops[1][uv_lay].uv = Vector((1.0, 0.5))
        f_b2.loops[2][uv_lay].uv = Vector((0.5, 1.0))
        
        f_b3.loops[0][uv_lay].uv = Vector((0.5, 0.5))
        f_b3.loops[1][uv_lay].uv = Vector((0.5, 1.0))
        f_b3.loops[2][uv_lay].uv = Vector((0.0, 0.5))
        
        for f in [f_s0, f_s1, f_s2, f_s3]:
            for loop in f.loops:
                loop[uv_lay].uv = Vector((0.5, 0.5))

def build_shingled_canopy_pad(bm, center_pos, rx, ry, rz, normal_dir, scale=1.0, mat_idx=0, uv_lay=None):
    """
    Builds a voluminous, tiered canopy pad using Bonsai-synchronized leaves.
    """
    n_up = normal_dir.normalized()
    ref_fwd = Vector((0, 1, 0))
    if abs(n_up.dot(ref_fwd)) > 0.85:
        ref_fwd = Vector((1, 0, 0))
    n_right = ref_fwd.cross(n_up).normalized()
    n_fwd = n_up.cross(n_right).normalized()
    
    pad_basis = Matrix((
        (n_right.x, n_fwd.x, n_up.x, center_pos.x),
        (n_right.y, n_fwd.y, n_up.y, center_pos.y),
        (n_right.z, n_fwd.z, n_up.z, center_pos.z),
        (0.0,       0.0,     0.0,    1.0)
    ))
    
    # 5 tiered concentric rings of diamond leaves
    rings = [
        # (u_height, rad_ratio, count, pitch_deg, l_sc, droop)
        (0.96, 0.08,  3,  28.0, 1.05 * scale, 0.8),   # Center apex cap
        (0.78, 0.35,  6,  20.0, 1.10 * scale, 0.9),   # Inner upper ring
        (0.52, 0.62, 10,  12.0, 1.14 * scale, 1.0),   # Mid upper ring
        (0.24, 0.86, 14,   4.0, 1.18 * scale, 1.2),   # Main outer rim
        (-0.06, 0.94, 14, -14.0, 1.12 * scale, 1.3),  # Drooping skirt
        (-0.30, 0.68,  8, -28.0, 0.96 * scale, 1.3),  # Underside tuck
    ]
    
    leaf_count = 0
    for u, rad, count, pitch, l_sc, drp in rings:
        phase = (0.5 * math.pi / count) if (count % 2 == 0) else 0.0
        for i in range(count):
            ang = 2.0 * math.pi * i / count + phase
            lx = math.cos(ang) * rx * rad
            ly = math.sin(ang) * ry * rad
            lz = rz * u
            local_pos = Vector((lx, ly, lz))
            
            local_fwd = Vector((math.cos(ang) * rx, math.sin(ang) * ry, 0.0)).normalized()
            local_up = Vector((0, 0, 1))
            local_rt = local_fwd.cross(local_up).normalized()
            local_up = local_rt.cross(local_fwd).normalized()
            
            local_frame = Matrix((
                (local_rt.x, local_fwd.x, local_up.x, local_pos.x),
                (local_rt.y, local_fwd.y, local_up.y, local_pos.y),
                (local_rt.z, local_fwd.z, local_up.z, local_pos.z),
                (0.0,        0.0,         0.0,        1.0)
            ))
            
            roll = math.sin(i * 3.3) * 6.0
            rot = Matrix.Rotation(math.radians(-pitch), 4, 'X') @ Matrix.Rotation(math.radians(roll), 4, 'Y')
            leaf_world_mat = pad_basis @ local_frame @ rot
            
            add_bonsai_sync_leaf(bm, leaf_world_mat, scale=l_sc, droop=drp, thickness=0.0035, mat_idx=mat_idx, uv_lay=uv_lay)
            leaf_count += 1
            
    return leaf_count

# Definition of the 7 Canopy Pads
CANOPY_PADS = [
    {
        "name": "Pad_Top_Crown",
        "center": Vector((0.00, 0.02, 2.05)),
        "rx": 0.46, "ry": 0.38, "rz": 0.28,
        "normal": Vector((0.0, -0.05, 1.0)),
        "scale": 1.15,
        "limb_tip": Vector((0.00, 0.02, 1.95)),
    },
    {
        "name": "Pad_Upper_Right",
        "center": Vector((0.58, -0.12, 1.62)),
        "rx": 0.40, "ry": 0.32, "rz": 0.24,
        "normal": Vector((0.30, -0.25, 0.91)),
        "scale": 1.08,
        "limb_tip": Vector((0.56, -0.11, 1.60)),
    },
    {
        "name": "Pad_Upper_Left",
        "center": Vector((-0.52, 0.18, 1.58)),
        "rx": 0.40, "ry": 0.32, "rz": 0.24,
        "normal": Vector((-0.30, 0.25, 0.91)),
        "scale": 1.08,
        "limb_tip": Vector((-0.50, 0.17, 1.56)),
    },
    {
        "name": "Pad_Far_Lower_Left",
        "center": Vector((-0.88, -0.22, 1.10)),
        "rx": 0.48, "ry": 0.36, "rz": 0.25,
        "normal": Vector((-0.35, -0.15, 0.92)),
        "scale": 1.12,
        "limb_tip": Vector((-0.84, -0.22, 1.08)),
    },
    {
        "name": "Pad_Far_Lower_Right",
        "center": Vector((0.86, 0.22, 1.00)),
        "rx": 0.46, "ry": 0.36, "rz": 0.25,
        "normal": Vector((0.35, 0.15, 0.92)),
        "scale": 1.10,
        "limb_tip": Vector((0.84, 0.20, 0.97)),
    },
    {
        "name": "Pad_Mid_Inner_Right",
        "center": Vector((0.38, -0.28, 1.20)),
        "rx": 0.34, "ry": 0.28, "rz": 0.22,
        "normal": Vector((0.15, -0.45, 0.88)),
        "scale": 1.02,
        "limb_tip": Vector((0.38, -0.26, 1.16)),
    },
    {
        "name": "Pad_Mid_Back_Depth",
        "center": Vector((-0.08, 0.52, 1.40)),
        "rx": 0.38, "ry": 0.34, "rz": 0.24,
        "normal": Vector((-0.05, 0.45, 0.89)),
        "scale": 1.06,
        "limb_tip": Vector((-0.075, 0.50, 1.38)),
    },
]

def build_roots():
    mesh = bpy.data.meshes.new("Roots")
    obj = bpy.data.objects.new("Roots", mesh)
    bpy.context.collection.objects.link(obj)
    
    bm = bmesh.new()
    uv_lay = bm.loops.layers.uv.verify()
    
    # ROOT BUTTRESS COLLAR - Flared soft base connecting to trunk at z=0.000 (r=0.205m)
    core_flare = [
        (Vector((0.005, -0.005, 0.035)), 0.175),
        (Vector((0.005, -0.005, 0.000)), 0.205),
        (Vector((0.005, -0.005, -0.030)), 0.185),
        (Vector((0.005, -0.005, -0.060)), 0.130),
    ]
    create_fluted_tube_bmesh(bm, core_flare, num_radial=16, cap_start=False, cap_end=True, 
                             flute_count=5, flute_depth=0.045, twist_rate=0.0)
    
    # 5 ASYMMETRICAL ORGANIC ROOTS
    # Root 1: Main Left-Front Anchor (Thickest, crawls then dips)
    r1_main = [
        (Vector((-0.070, -0.080, -0.010)), 0.105),
        (Vector((-0.220, -0.200, -0.035)), 0.085),
        (Vector((-0.380, -0.320, -0.075)), 0.065),
        (Vector((-0.540, -0.420, -0.140)), 0.045),
        (Vector((-0.680, -0.480, -0.240)), 0.020),
    ]
    create_fluted_tube_bmesh(bm, r1_main, num_radial=10, cap_start=False, cap_end=True, flute_count=3, flute_depth=0.04)
    r1_sec = [
        (Vector((-0.380, -0.320, -0.075)), 0.052),
        (Vector((-0.520, -0.240, -0.120)), 0.034),
        (Vector((-0.640, -0.200, -0.190)), 0.015),
    ]
    create_fluted_tube_bmesh(bm, r1_sec, num_radial=8, cap_start=False, cap_end=True, flute_count=0)

    # Root 2: Front-Right Anchor
    r2_main = [
        (Vector((0.080, -0.070, -0.010)), 0.090),
        (Vector((0.240, -0.180, -0.035)), 0.072),
        (Vector((0.400, -0.280, -0.080)), 0.052),
        (Vector((0.540, -0.380, -0.150)), 0.035),
        (Vector((0.660, -0.460, -0.250)), 0.018),
    ]
    create_fluted_tube_bmesh(bm, r2_main, num_radial=10, cap_start=False, cap_end=True, flute_count=3, flute_depth=0.04)
    r2_sec = [
        (Vector((0.400, -0.280, -0.080)), 0.042),
        (Vector((0.480, -0.420, -0.140)), 0.026),
        (Vector((0.540, -0.540, -0.220)), 0.014),
    ]
    create_fluted_tube_bmesh(bm, r2_sec, num_radial=8, cap_start=False, cap_end=True, flute_count=0)

    # Root 3: Far Right Lateral Surface Root (Crawls horizontally along surface)
    r3_main = [
        (Vector((0.110, 0.030, -0.010)), 0.088),
        (Vector((0.320, 0.080, -0.030)), 0.070),
        (Vector((0.560, 0.140, -0.060)), 0.052),
        (Vector((0.760, 0.200, -0.110)), 0.034),
        (Vector((0.920, 0.240, -0.180)), 0.018),
    ]
    create_fluted_tube_bmesh(bm, r3_main, num_radial=10, cap_start=False, cap_end=True, flute_count=3, flute_depth=0.04)
    r3_sec = [
        (Vector((0.320, 0.080, -0.030)), 0.045),
        (Vector((0.440, 0.040, -0.110)), 0.028),
        (Vector((0.520, 0.020, -0.210)), 0.014),
    ]
    create_fluted_tube_bmesh(bm, r3_sec, num_radial=8, cap_start=False, cap_end=True, flute_count=0)

    # Root 4: Rear-Right Anchor (Stocky single root, NO fork)
    r4_main = [
        (Vector((0.060, 0.100, -0.015)), 0.082),
        (Vector((0.180, 0.240, -0.045)), 0.064),
        (Vector((0.280, 0.380, -0.100)), 0.045),
        (Vector((0.360, 0.480, -0.180)), 0.020),
    ]
    create_fluted_tube_bmesh(bm, r4_main, num_radial=8, cap_start=False, cap_end=True, flute_count=3, flute_depth=0.035)

    # Root 5: Rear-Left Anchor
    r5_main = [
        (Vector((-0.070, 0.090, -0.015)), 0.084),
        (Vector((-0.200, 0.220, -0.045)), 0.066),
        (Vector((-0.320, 0.340, -0.095)), 0.048),
        (Vector((-0.420, 0.440, -0.170)), 0.022),
    ]
    create_fluted_tube_bmesh(bm, r5_main, num_radial=8, cap_start=False, cap_end=True, flute_count=3, flute_depth=0.035)
    r5_sec = [
        (Vector((-0.200, 0.220, -0.045)), 0.042),
        (Vector((-0.160, 0.360, -0.110)), 0.026),
        (Vector((-0.140, 0.460, -0.190)), 0.014),
    ]
    create_fluted_tube_bmesh(bm, r5_sec, num_radial=8, cap_start=False, cap_end=True, flute_count=0)

    bm.to_mesh(mesh)
    bm.free()
    
    mat_bark = create_material("M_Bark", (0.68, 0.44, 0.24, 1.0), roughness=0.52)
    mesh.materials.append(mat_bark)
    for poly in mesh.polygons:
        poly.use_smooth = True
        
    return obj

def build_trunk_and_branches():
    mesh = bpy.data.meshes.new("Trunk_Branches")
    obj = bpy.data.objects.new("Trunk_Branches", mesh)
    bpy.context.collection.objects.link(obj)
    
    bm = bmesh.new()
    uv_lay = bm.loops.layers.uv.verify()
    
    # MAIN TRUNK SPINE (with wide root flare at z=0 connecting to Roots)
    trunk_pts = [
        (Vector((0.005, -0.005, -0.025)), 0.190),  # nested inside root core
        (Vector((0.005, -0.005, 0.000)), 0.205),   # ground collar z=0
        (Vector((0.035, -0.012, 0.160)), 0.155),   # gentle flare taper
        (Vector((0.095, -0.018, 0.360)), 0.134),   # rightward bow
        (Vector((0.155, 0.000, 0.580)), 0.115),    # apex of curve / branch junction
        (Vector((0.140, 0.020, 0.820)), 0.098),    # S-curve inwards
        (Vector((0.080, 0.030, 1.080)), 0.084),    # middle trunk
        (Vector((0.025, 0.035, 1.350)), 0.070),    # upper trunk
        (Vector((0.005, 0.035, 1.620)), 0.056),    # crown junction
        (Vector((0.000, 0.030, 1.820)), 0.040),    # inside crown cluster
    ]
    create_fluted_tube_bmesh(bm, trunk_pts, num_radial=16, cap_start=False, cap_end=True, 
                             flute_count=5, flute_depth=0.055, twist_rate=1.0)
    
    # 1. Lower Left Main Limb
    limb_lower_l = [
        (Vector((0.130, 0.005, 0.650)), 0.065),
        (Vector((-0.080, -0.060, 0.740)), 0.056),
        (Vector((-0.340, -0.120, 0.840)), 0.046),
        (Vector((-0.620, -0.180, 0.960)), 0.036),
        (Vector((-0.840, -0.220, 1.080)), 0.024),
    ]
    create_fluted_tube_bmesh(bm, limb_lower_l, num_radial=10, cap_start=False, cap_end=True, flute_count=4, flute_depth=0.04)
    
    fork_lower_l = [
        (Vector((-0.340, -0.120, 0.840)), 0.038),
        (Vector((-0.520, -0.240, 0.920)), 0.028),
        (Vector((-0.720, -0.280, 1.040)), 0.018),
    ]
    create_fluted_tube_bmesh(bm, fork_lower_l, num_radial=8, cap_start=False, cap_end=True, flute_count=3, flute_depth=0.035)
    
    # 2. Lower Right Main Limb
    limb_lower_r = [
        (Vector((0.160, 0.000, 0.580)), 0.070),
        (Vector((0.280, -0.020, 0.680)), 0.058),
        (Vector((0.440, -0.040, 0.780)), 0.048),
    ]
    create_fluted_tube_bmesh(bm, limb_lower_r, num_radial=10, cap_start=False, cap_end=False, flute_count=4, flute_depth=0.04)
    
    fork_lower_r_far = [
        (Vector((0.440, -0.040, 0.780)), 0.044),
        (Vector((0.640, 0.080, 0.860)), 0.034),
        (Vector((0.840, 0.200, 0.970)), 0.022),
    ]
    create_fluted_tube_bmesh(bm, fork_lower_r_far, num_radial=8, cap_start=False, cap_end=True, flute_count=3, flute_depth=0.035)
    
    fork_lower_r_mid = [
        (Vector((0.440, -0.040, 0.780)), 0.040),
        (Vector((0.400, -0.180, 0.950)), 0.030),
        (Vector((0.380, -0.260, 1.160)), 0.020),
    ]
    create_fluted_tube_bmesh(bm, fork_lower_r_mid, num_radial=8, cap_start=False, cap_end=True, flute_count=3, flute_depth=0.035)
    
    # 3. Upper Left Limb
    limb_upper_l = [
        (Vector((0.050, 0.028, 1.200)), 0.050),
        (Vector((-0.160, 0.080, 1.320)), 0.040),
        (Vector((-0.360, 0.140, 1.450)), 0.030),
        (Vector((-0.500, 0.170, 1.560)), 0.020),
    ]
    create_fluted_tube_bmesh(bm, limb_upper_l, num_radial=8, cap_start=False, cap_end=True, flute_count=3, flute_depth=0.035)
    
    # 4. Upper Right Limb
    limb_upper_r = [
        (Vector((0.040, 0.030, 1.260)), 0.052),
        (Vector((0.240, -0.030, 1.380)), 0.040),
        (Vector((0.440, -0.080, 1.500)), 0.030),
        (Vector((0.560, -0.110, 1.600)), 0.020),
    ]
    create_fluted_tube_bmesh(bm, limb_upper_r, num_radial=8, cap_start=False, cap_end=True, flute_count=3, flute_depth=0.035)
    
    # 5. Mid Back Depth Limb
    limb_back = [
        (Vector((0.065, 0.028, 1.120)), 0.045),
        (Vector((0.000, 0.200, 1.220)), 0.036),
        (Vector((-0.050, 0.380, 1.320)), 0.028),
        (Vector((-0.075, 0.500, 1.380)), 0.020),
    ]
    create_fluted_tube_bmesh(bm, limb_back, num_radial=8, cap_start=False, cap_end=True, flute_count=3, flute_depth=0.035)
    
    # 6. Crown Forks
    crown_fork_l = [
        (Vector((0.005, 0.035, 1.620)), 0.044),
        (Vector((-0.100, 0.025, 1.760)), 0.032),
        (Vector((-0.160, 0.020, 1.950)), 0.020),
    ]
    create_fluted_tube_bmesh(bm, crown_fork_l, num_radial=8, cap_start=False, cap_end=True, flute_count=3, flute_depth=0.03)
    
    crown_fork_r = [
        (Vector((0.005, 0.035, 1.620)), 0.044),
        (Vector((0.120, 0.035, 1.760)), 0.032),
        (Vector((0.180, 0.030, 1.950)), 0.020),
    ]
    create_fluted_tube_bmesh(bm, crown_fork_r, num_radial=8, cap_start=False, cap_end=True, flute_count=3, flute_depth=0.03)

    crown_fork_c = [
        (Vector((0.000, 0.030, 1.820)), 0.038),
        (Vector((0.000, 0.025, 1.980)), 0.024),
        (Vector((0.000, 0.020, 2.050)), 0.016),
    ]
    create_fluted_tube_bmesh(bm, crown_fork_c, num_radial=8, cap_start=False, cap_end=True, flute_count=3, flute_depth=0.03)

    # SECONDARY TWIG FORKS SUPPORTING CANOPY PADS
    for pad in CANOPY_PADS:
        tip = pad["limb_tip"]
        cen = pad["center"]
        twig_offsets = [
            Vector((-pad["rx"] * 0.35, -pad["ry"] * 0.25, pad["rz"] * 0.12)),
            Vector((pad["rx"] * 0.35, pad["ry"] * 0.25, pad["rz"] * 0.12)),
            Vector((0.0, -pad["ry"] * 0.35, pad["rz"] * 0.08)),
        ]
        for off in twig_offsets:
            dest = cen + off
            mid = (tip + dest) * 0.5 + Vector((0, 0, 0.015))
            twig_path = [
                (tip, 0.015),
                (mid, 0.010),
                (dest, 0.006),
            ]
            create_fluted_tube_bmesh(bm, twig_path, num_radial=6, cap_start=False, cap_end=True, flute_count=0)

    bm.to_mesh(mesh)
    bm.free()
    
    mat_bark = create_material("M_Bark", (0.68, 0.44, 0.24, 1.0), roughness=0.52)
    mesh.materials.append(mat_bark)
    for poly in mesh.polygons:
        poly.use_smooth = True
        
    return obj

def build_foliage():
    mesh = bpy.data.meshes.new("Foliage")
    obj = bpy.data.objects.new("Foliage", mesh)
    bpy.context.collection.objects.link(obj)
    
    bm = bmesh.new()
    uv_lay = bm.loops.layers.uv.verify()
    
    total_leaves = 0
    for pad in CANOPY_PADS:
        cnt = build_shingled_canopy_pad(
            bm,
            center_pos=pad["center"],
            rx=pad["rx"],
            ry=pad["ry"],
            rz=pad["rz"],
            normal_dir=pad["normal"],
            scale=pad["scale"],
            mat_idx=0,
            uv_lay=uv_lay
        )
        total_leaves += cnt
        
    print(f"Total synchronized leaves generated: {total_leaves}")
    
    bm.to_mesh(mesh)
    bm.free()
    
    mat_foliage = create_material("M_Foliage", (0.35, 0.72, 0.10, 1.0), roughness=0.45)
    mesh.materials.append(mat_foliage)
    for poly in mesh.polygons:
        poly.use_smooth = True
        
    return obj

def setup_camera_and_lights():
    cam_data = bpy.data.cameras.new(name="PreviewCam")
    cam_data.lens = 45.0
    cam_obj = bpy.data.objects.new("PreviewCam", cam_data)
    bpy.context.collection.objects.link(cam_obj)
    bpy.context.scene.camera = cam_obj
    
    sun_data = bpy.data.lights.new(name="KeyLight", type='SUN')
    sun_data.energy = 2.4
    sun_data.color = (1.0, 0.96, 0.90)
    sun_obj = bpy.data.objects.new("KeyLight", sun_data)
    bpy.context.collection.objects.link(sun_obj)
    sun_obj.rotation_euler = (math.radians(48), math.radians(16), math.radians(-35))
    
    fill_data = bpy.data.lights.new(name="FillLight", type='SUN')
    fill_data.energy = 1.4
    fill_data.color = (0.88, 0.94, 1.0)
    fill_obj = bpy.data.objects.new("FillLight", fill_data)
    bpy.context.collection.objects.link(fill_obj)
    fill_obj.rotation_euler = (math.radians(40), math.radians(-25), math.radians(45))
    
    rim_data = bpy.data.lights.new(name="RimLight", type='SUN')
    rim_data.energy = 1.2
    rim_data.color = (1.0, 1.0, 1.0)
    rim_obj = bpy.data.objects.new("RimLight", rim_data)
    bpy.context.collection.objects.link(rim_obj)
    rim_obj.rotation_euler = (math.radians(-50), math.radians(10), math.radians(175))
    
    return cam_obj

def render_multi_angles_and_turntable(cam_obj, output_dir):
    os.makedirs(output_dir, exist_ok=True)
    turntable_dir = os.path.join(output_dir, "turntable")
    os.makedirs(turntable_dir, exist_ok=True)
    
    scene = bpy.context.scene
    scene.render.resolution_x = 1024
    scene.render.resolution_y = 1024
    scene.render.film_transparent = True
    scene.render.image_settings.file_format = 'PNG'
    
    target = Vector((0.0, 0.0, 0.95))
    dist = 4.30
    
    angles = [
        ("preview_front.png", 0.0, 6.0, dist),
        ("preview_back.png", 180.0, 6.0, dist),
        ("preview_left.png", 90.0, 6.0, dist),
        ("preview_right.png", -90.0, 6.0, dist),
        ("preview_top_down.png", 0.0, 84.0, dist * 0.95),
        ("preview_perspective_front_3_4.png", 35.0, 14.0, dist),
        ("preview_perspective_back_3_4.png", 215.0, 14.0, dist),
    ]
    
    for filename, yaw_deg, pitch_deg, d in angles:
        yaw = math.radians(yaw_deg)
        pitch = math.radians(pitch_deg)
        
        cx = target.x + d * math.sin(yaw) * math.cos(pitch)
        cy = target.y - d * math.cos(yaw) * math.cos(pitch)
        cz = target.z + d * math.sin(pitch)
        
        cam_obj.location = Vector((cx, cy, cz))
        direction = (target - cam_obj.location).normalized()
        cam_obj.rotation_euler = direction.to_track_quat('-Z', 'Y').to_euler()
        
        filepath = os.path.join(output_dir, filename)
        scene.render.filepath = filepath
        bpy.ops.render.render(write_still=True)
        print(f"Rendered: {filename}")
        
    for i in range(12):
        yaw_deg = i * 30.0
        pitch_deg = 12.0
        yaw = math.radians(yaw_deg)
        pitch = math.radians(pitch_deg)
        
        cx = target.x + dist * math.sin(yaw) * math.cos(pitch)
        cy = target.y - dist * math.cos(yaw) * math.cos(pitch)
        cz = target.z + dist * math.sin(pitch)
        
        cam_obj.location = Vector((cx, cy, cz))
        direction = (target - cam_obj.location).normalized()
        cam_obj.rotation_euler = direction.to_track_quat('-Z', 'Y').to_euler()
        
        filename = f"turntable_{i:02d}_{int(yaw_deg):03d}deg.png"
        filepath = os.path.join(turntable_dir, filename)
        scene.render.filepath = filepath
        bpy.ops.render.render(write_still=True)
        print(f"Rendered turntable frame {i+1}/12: {filename}")

    # Render low angle root inspection view
    target_root = Vector((0.0, 0.0, -0.05))
    cam_obj.location = Vector((0.8, -1.6, 0.15))
    cam_obj.rotation_euler = (target_root - cam_obj.location).to_track_quat('-Z', 'Y').to_euler()
    scene.render.filepath = os.path.join(output_dir, "closeup_roots_low_angle.png")
    bpy.ops.render.render(write_still=True)
    print(f"Rendered low angle root view: {scene.render.filepath}")

def main():
    clear_scene()
    
    print("\n--- BUILDING TREE LARGE V3 (BONSAI-SYNCHRONIZED LEAVES & ORGANIC ROOTS) ---")
    roots_obj = build_roots()
    trunk_obj = build_trunk_and_branches()
    foliage_obj = build_foliage()
    
    # Apply all transforms
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    
    # Save .blend directly to Tree_Large.blend
    blend_path = r"c:\Users\Asus\foder game 3D\Cay_The_Gioi\ArtSource\Models\Tree_Large.blend"
    os.makedirs(os.path.dirname(blend_path), exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=blend_path)
    print(f"Saved: {blend_path}")
    
    # Export FBX directly to Tree_Large.fbx
    fbx_path = r"c:\Users\Asus\foder game 3D\Cay_The_Gioi\ArtSource\Exports\Tree_Large.fbx"
    os.makedirs(os.path.dirname(fbx_path), exist_ok=True)
    
    bpy.ops.object.select_all(action='DESELECT')
    for o in [roots_obj, trunk_obj, foliage_obj]:
        o.select_set(True)
        
    bpy.ops.export_scene.fbx(
        filepath=fbx_path,
        use_selection=True,
        global_scale=1.0,
        apply_unit_scale=True,
        apply_scale_options='FBX_SCALE_ALL',
        axis_forward='-Z',
        axis_up='Y',
        object_types={'MESH'},
        use_mesh_modifiers=True,
        mesh_smooth_type='FACE',
        bake_anim=False
    )
    print(f"Exported FBX: {fbx_path}")
    
    # Setup rendering
    cam_obj = setup_camera_and_lights()
    audit_dir = r"c:\Users\Asus\foder game 3D\Cay_The_Gioi\Evidence\Tree_Large_v01_Audit"
    render_multi_angles_and_turntable(cam_obj, audit_dir)
    print("Multi-angle rendering and turntable completed successfully.")

if __name__ == "__main__":
    main()
