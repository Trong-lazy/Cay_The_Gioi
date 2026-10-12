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
    """
    Creates a tube mesh along a spine path with subtle longitudinal fluting (bark ridges).
    """
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

def add_stylized_leaf(bm, transform_mat, scale=1.0, droop=1.2, v_ang=24.0, mat_idx=0):
    """
    Builds a single stylized 3D leaf with 3 scalloped lobes on each side,
    pointed tip, central V-crease, arch/droop, and petiole stem.
    """
    thickness = 0.0035 * scale
    v_rad = math.radians(v_ang)
    
    # 3 scalloped lobes with broad stylized shape matching reference
    profile = [
        (0.000, 0.000, 0.010),
        (0.030, 0.010, 0.036),  # lobe 1
        (0.060, 0.018, 0.026),  # notch 1
        (0.100, 0.024, 0.055),  # lobe 2 (widest, 11cm broad)
        (0.140, 0.020, 0.040),  # notch 2
        (0.180, 0.014, 0.046),  # lobe 3
        (0.220, 0.005, 0.024),  # notch 3
        (0.255, -0.015 * droop, 0.000), # tip
    ]
    
    top_spine, bot_spine = [], []
    top_left, bot_left = [], []
    top_right, bot_right = [], []
    
    for y_rel, z_arch, w_half in profile:
        y = y_rel * scale
        z = z_arch * scale
        w = w_half * scale
        z_edge = z + w * math.sin(v_rad)
        
        top_spine.append(bm.verts.new(transform_mat @ Vector((0.0, y, z + thickness * 0.5))))
        bot_spine.append(bm.verts.new(transform_mat @ Vector((0.0, y, z - thickness * 0.5))))
        
        if w > 1e-4:
            top_left.append(bm.verts.new(transform_mat @ Vector((-w, y, z_edge + thickness * 0.5))))
            bot_left.append(bm.verts.new(transform_mat @ Vector((-w, y, z_edge - thickness * 0.5))))
            top_right.append(bm.verts.new(transform_mat @ Vector((w, y, z_edge + thickness * 0.5))))
            bot_right.append(bm.verts.new(transform_mat @ Vector((w, y, z_edge - thickness * 0.5))))
            
    n = len(top_spine)
    for i in range(n - 1):
        ts0, ts1 = top_spine[i], top_spine[i+1]
        bs0, bs1 = bot_spine[i], bot_spine[i+1]
        
        if i < len(top_left) - 1:
            tl0, tl1 = top_left[i], top_left[i+1]
            bl0, bl1 = bot_left[i], bot_left[i+1]
            tr0, tr1 = top_right[i], top_right[i+1]
            br0, br1 = bot_right[i], bot_right[i+1]
            
            f1 = bm.faces.new([ts0, tl0, tl1, ts1])
            f2 = bm.faces.new([ts0, ts1, tr1, tr0])
            f3 = bm.faces.new([bs0, bs1, bl1, bl0])
            f4 = bm.faces.new([bs0, br0, br1, bs1])
            f5 = bm.faces.new([tl0, bl0, bl1, tl1])
            f6 = bm.faces.new([tr0, tr1, br1, br0])
            for f in [f1, f2, f3, f4, f5, f6]:
                f.material_index = mat_idx
                f.smooth = True
        else:
            tl0, bl0 = top_left[-1], bot_left[-1]
            tr0, br0 = top_right[-1], bot_right[-1]
            tip_t, tip_b = top_spine[-1], bot_spine[-1]
            
            f1 = bm.faces.new([ts0, tl0, tip_t])
            f2 = bm.faces.new([ts0, tip_t, tr0])
            f3 = bm.faces.new([bs0, tip_b, bl0])
            f4 = bm.faces.new([bs0, br0, tip_b])
            f5 = bm.faces.new([tl0, bl0, tip_b, tip_t])
            f6 = bm.faces.new([tr0, tip_t, tip_b, br0])
            for f in [f1, f2, f3, f4, f5, f6]:
                f.material_index = mat_idx
                f.smooth = True
                
    # Petiole stem
    pet_len = 0.045 * scale
    pet_r = 0.003 * scale
    num_p = 6
    p_base, p_leaf = [], []
    for a in range(num_p):
        ang = 2.0 * math.pi * a / num_p
        p_base.append(bm.verts.new(transform_mat @ Vector((math.cos(ang) * pet_r, -pet_len, math.sin(ang) * pet_r))))
        p_leaf.append(bm.verts.new(transform_mat @ Vector((math.cos(ang) * pet_r, 0.0, math.sin(ang) * pet_r))))
    for a in range(num_p):
        an = (a + 1) % num_p
        f = bm.faces.new([p_base[a], p_base[an], p_leaf[an], p_leaf[a]])
        f.material_index = mat_idx
        f.smooth = True

def build_shingled_canopy_pad(bm, center_pos, rx, ry, rz, normal_dir, scale=1.0, mat_idx=0):
    """
    Builds a dense, multi-tiered shingled canopy dome of overlapping stylized leaves.
    center_pos: center of the canopy pad.
    rx, ry, rz: semi-axes of the canopy pad dome.
    normal_dir: orientation normal (upward axis of the dome).
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
    
    # Tiered rings from apex to bottom skirt
    rings = [
        # (u_height, rad_ratio, count, pitch_deg, l_sc, droop)
        (0.96, 0.06,  3,  32.0, 1.05 * scale, 0.8),   # Center apex cap (3 leaves meeting at center)
        (0.82, 0.32,  6,  24.0, 1.10 * scale, 0.9),   # Inner upper ring
        (0.60, 0.60, 10,  16.0, 1.14 * scale, 1.0),   # Mid upper ring
        (0.32, 0.84, 14,   8.0, 1.18 * scale, 1.2),   # Main outer rim (broadest)
        (0.00, 0.94, 16,  -6.0, 1.16 * scale, 1.3),   # Horizontal rim / edge
        (-0.25, 0.82, 12, -24.0, 1.08 * scale, 1.5),  # Drooping lower skirt
        (-0.45, 0.55,  8, -36.0, 0.95 * scale, 1.4),  # Underside tuck
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
            
            # Local outward direction
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
            
            add_stylized_leaf(bm, leaf_world_mat, scale=l_sc, droop=drp, mat_idx=mat_idx)
            leaf_count += 1
            
    return leaf_count

# Definition of the 7 Canopy Pads matching prompt 1 positions and reference artwork
CANOPY_PADS = [
    # 0: TOP CROWN (Apex Dome)
    {
        "name": "Pad_Top_Crown",
        "center": Vector((0.00, 0.02, 2.05)),
        "rx": 0.46, "ry": 0.38, "rz": 0.30,
        "normal": Vector((0.0, -0.05, 1.0)),
        "scale": 1.15,
        "limb_tip": Vector((0.00, 0.02, 1.95)),
    },
    # 1: UPPER RIGHT
    {
        "name": "Pad_Upper_Right",
        "center": Vector((0.58, -0.12, 1.62)),
        "rx": 0.40, "ry": 0.32, "rz": 0.25,
        "normal": Vector((0.30, -0.25, 0.91)),
        "scale": 1.08,
        "limb_tip": Vector((0.56, -0.11, 1.60)),
    },
    # 2: UPPER LEFT
    {
        "name": "Pad_Upper_Left",
        "center": Vector((-0.52, 0.18, 1.58)),
        "rx": 0.40, "ry": 0.32, "rz": 0.25,
        "normal": Vector((-0.30, 0.25, 0.91)),
        "scale": 1.08,
        "limb_tip": Vector((-0.50, 0.17, 1.56)),
    },
    # 3: FAR LOWER LEFT (Wide, dramatic lower spread)
    {
        "name": "Pad_Far_Lower_Left",
        "center": Vector((-0.88, -0.22, 1.10)),
        "rx": 0.48, "ry": 0.36, "rz": 0.26,
        "normal": Vector((-0.35, -0.15, 0.92)),
        "scale": 1.12,
        "limb_tip": Vector((-0.84, -0.22, 1.08)),
    },
    # 4: FAR LOWER RIGHT (Wide lower spread)
    {
        "name": "Pad_Far_Lower_Right",
        "center": Vector((0.86, 0.22, 1.00)),
        "rx": 0.46, "ry": 0.36, "rz": 0.26,
        "normal": Vector((0.35, 0.15, 0.92)),
        "scale": 1.10,
        "limb_tip": Vector((0.84, 0.20, 0.97)),
    },
    # 5: MID INNER RIGHT (Front-facing depth pad)
    {
        "name": "Pad_Mid_Inner_Right",
        "center": Vector((0.38, -0.28, 1.20)),
        "rx": 0.34, "ry": 0.28, "rz": 0.22,
        "normal": Vector((0.15, -0.45, 0.88)),
        "scale": 1.02,
        "limb_tip": Vector((0.38, -0.26, 1.16)),
    },
    # 6: MID BACK DEPTH (Rear-facing 3D depth pad)
    {
        "name": "Pad_Mid_Back_Depth",
        "center": Vector((-0.08, 0.52, 1.40)),
        "rx": 0.38, "ry": 0.34, "rz": 0.25,
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
    
    # ROOT BUTTRESS COLLAR
    core_pts = [
        (Vector((0.000, 0.000, 0.010)), 0.160),
        (Vector((0.000, 0.000, 0.000)), 0.165),
        (Vector((0.000, 0.000, -0.040)), 0.178),
        (Vector((0.002, 0.002, -0.090)), 0.130),
        (Vector((0.000, 0.000, -0.140)), 0.040),
    ]
    create_fluted_tube_bmesh(bm, core_pts, num_radial=16, cap_start=False, cap_end=True, 
                             flute_count=5, flute_depth=0.06, twist_rate=0.0)
    
    def add_buttress_root(spine, tip_a, tip_b, num_radial=10):
        create_fluted_tube_bmesh(bm, spine, num_radial=num_radial, cap_start=False, cap_end=False, 
                                 flute_count=3, flute_depth=0.045, twist_rate=0.4)
        fork_pt = spine[-1][0]
        fork_r = spine[-1][1]
        
        branch_a = [(fork_pt, fork_r * 0.85)] + tip_a
        create_fluted_tube_bmesh(bm, branch_a, num_radial=max(6, num_radial - 2), cap_start=False, cap_end=True,
                                 flute_count=0)
        
        branch_b = [(fork_pt, fork_r * 0.85)] + tip_b
        create_fluted_tube_bmesh(bm, branch_b, num_radial=max(6, num_radial - 2), cap_start=False, cap_end=True,
                                 flute_count=0)

    # 1. Root Front-Left (-X, -Y)
    spine_fl = [
        (Vector((-0.050, -0.050, -0.020)), 0.085),
        (Vector((-0.200, -0.160, -0.060)), 0.068),
        (Vector((-0.340, -0.260, -0.130)), 0.050),
        (Vector((-0.460, -0.320, -0.220)), 0.032),
    ]
    tip_fl_a = [
        (Vector((-0.560, -0.360, -0.280)), 0.018),
        (Vector((-0.640, -0.380, -0.320)), 0.006),
    ]
    tip_fl_b = [
        (Vector((-0.500, -0.420, -0.280)), 0.016),
        (Vector((-0.540, -0.490, -0.330)), 0.006),
    ]
    add_buttress_root(spine_fl, tip_fl_a, tip_fl_b)
    
    # 2. Root Front-Center (subtle -X, strong -Y)
    spine_fc = [
        (Vector((-0.010, -0.070, -0.020)), 0.088),
        (Vector((-0.030, -0.220, -0.070)), 0.070),
        (Vector((-0.040, -0.360, -0.140)), 0.048),
        (Vector((-0.050, -0.460, -0.230)), 0.030),
    ]
    tip_fc_a = [
        (Vector((-0.130, -0.540, -0.280)), 0.016),
        (Vector((-0.190, -0.600, -0.320)), 0.006),
    ]
    tip_fc_b = [
        (Vector((0.040, -0.540, -0.270)), 0.016),
        (Vector((0.090, -0.600, -0.310)), 0.006),
    ]
    add_buttress_root(spine_fc, tip_fc_a, tip_fc_b)
    
    # 3. Root Front-Right (+X, -Y)
    spine_fr = [
        (Vector((0.060, -0.050, -0.020)), 0.084),
        (Vector((0.220, -0.150, -0.060)), 0.066),
        (Vector((0.360, -0.240, -0.130)), 0.048),
        (Vector((0.480, -0.300, -0.220)), 0.030),
    ]
    tip_fr_a = [
        (Vector((0.580, -0.340, -0.270)), 0.016),
        (Vector((0.660, -0.360, -0.310)), 0.006),
    ]
    tip_fr_b = [
        (Vector((0.520, -0.400, -0.280)), 0.016),
        (Vector((0.560, -0.480, -0.320)), 0.006),
    ]
    add_buttress_root(spine_fr, tip_fr_a, tip_fr_b)
    
    # 4. Root Right-Back (+X, +Y)
    spine_rb = [
        (Vector((0.070, 0.050, -0.020)), 0.080),
        (Vector((0.240, 0.160, -0.060)), 0.064),
        (Vector((0.380, 0.260, -0.130)), 0.046),
        (Vector((0.500, 0.340, -0.220)), 0.030),
    ]
    tip_rb_a = [
        (Vector((0.600, 0.400, -0.270)), 0.016),
        (Vector((0.680, 0.440, -0.310)), 0.006),
    ]
    tip_rb_b = [
        (Vector((0.520, 0.440, -0.280)), 0.016),
        (Vector((0.560, 0.520, -0.320)), 0.006),
    ]
    add_buttress_root(spine_rb, tip_rb_a, tip_rb_b)
    
    # 5. Root Back-Center (+Y)
    spine_bc = [
        (Vector((0.010, 0.070, -0.020)), 0.082),
        (Vector((0.020, 0.240, -0.060)), 0.065),
        (Vector((0.040, 0.380, -0.140)), 0.046),
        (Vector((0.050, 0.480, -0.220)), 0.030),
    ]
    tip_bc_a = [
        (Vector((-0.040, 0.560, -0.270)), 0.016),
        (Vector((-0.090, 0.620, -0.310)), 0.006),
    ]
    tip_bc_b = [
        (Vector((0.130, 0.550, -0.270)), 0.016),
        (Vector((0.180, 0.610, -0.310)), 0.006),
    ]
    add_buttress_root(spine_bc, tip_bc_a, tip_bc_b)
    
    # 6. Root Left-Back (-X, +Y)
    spine_lb = [
        (Vector((-0.060, 0.050, -0.020)), 0.080),
        (Vector((-0.220, 0.170, -0.060)), 0.064),
        (Vector((-0.360, 0.280, -0.130)), 0.046),
        (Vector((-0.470, 0.360, -0.220)), 0.030),
    ]
    tip_lb_a = [
        (Vector((-0.560, 0.420, -0.270)), 0.016),
        (Vector((-0.630, 0.460, -0.310)), 0.006),
    ]
    tip_lb_b = [
        (Vector((-0.490, 0.460, -0.280)), 0.016),
        (Vector((-0.520, 0.540, -0.320)), 0.006),
    ]
    add_buttress_root(spine_lb, tip_lb_a, tip_lb_b)
    
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
    
    # MAIN TRUNK SPINE (with 5 subtle longitudinal bark flutes)
    trunk_pts = [
        (Vector((0.000, 0.000, -0.030)), 0.180),
        (Vector((0.000, 0.000, 0.000)), 0.165),
        (Vector((0.040, -0.015, 0.180)), 0.150),
        (Vector((0.110, -0.020, 0.380)), 0.132),
        (Vector((0.160, 0.000, 0.580)), 0.115),
        (Vector((0.140, 0.020, 0.820)), 0.098),
        (Vector((0.080, 0.030, 1.080)), 0.084),
        (Vector((0.025, 0.035, 1.350)), 0.070),
        (Vector((0.005, 0.035, 1.620)), 0.056),
        (Vector((0.000, 0.030, 1.820)), 0.040),
    ]
    create_fluted_tube_bmesh(bm, trunk_pts, num_radial=16, cap_start=False, cap_end=True, 
                             flute_count=5, flute_depth=0.055, twist_rate=1.0)
    
    # PRIMARY BRANCHES & LIMBS
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
    
    # Fork 2A -> Far Lower Right
    fork_lower_r_far = [
        (Vector((0.440, -0.040, 0.780)), 0.044),
        (Vector((0.640, 0.080, 0.860)), 0.034),
        (Vector((0.840, 0.200, 0.970)), 0.022),
    ]
    create_fluted_tube_bmesh(bm, fork_lower_r_far, num_radial=8, cap_start=False, cap_end=True, flute_count=3, flute_depth=0.035)
    
    # Fork 2B -> Mid Inner Right
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

    # SECONDARY TWIG FORKS PENETRATING INTO CANOPY PADS
    for pad in CANOPY_PADS:
        tip = pad["limb_tip"]
        cen = pad["center"]
        norm = pad["normal"]
        # 3 secondary twigs branching outward inside the pad to support the leaf rings
        twig_offsets = [
            Vector((-pad["rx"] * 0.35, -pad["ry"] * 0.25, pad["rz"] * 0.15)),
            Vector((pad["rx"] * 0.35, pad["ry"] * 0.25, pad["rz"] * 0.15)),
            Vector((0.0, -pad["ry"] * 0.35, pad["rz"] * 0.10)),
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
            mat_idx=0
        )
        total_leaves += cnt
        
    print(f"Total leaves generated across all 7 canopy pads: {total_leaves}")
    
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

def main():
    clear_scene()
    
    print("\n--- BUILDING TREE LARGE V2 (SHINGLED INDIVIDUAL LEAF CANOPY & FLUTED BARK) ---")
    roots_obj = build_roots()
    trunk_obj = build_trunk_and_branches()
    foliage_obj = build_foliage()
    
    # Apply all transforms
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    
    # Save .blend
    blend_path = r"c:\Users\Asus\foder game 3D\Cay_The_Gioi\ArtSource\Models\Tree_Large.blend"
    os.makedirs(os.path.dirname(blend_path), exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=blend_path)
    print(f"Saved: {blend_path}")
    
    # Export FBX
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
