import bpy
import bmesh
import mathutils
from mathutils import Vector, Matrix
import math
import os

def clear_scene():
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    for block in bpy.data.meshes:
        bpy.data.meshes.remove(block)
    for block in bpy.data.materials:
        bpy.data.materials.remove(block)

def create_material(name, color, roughness=0.5):
    mat = bpy.data.materials.new(name=name)
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    bsdf = nodes.get("Principled BSDF")
    if bsdf:
        bsdf.inputs['Base Color'].default_value = color
        bsdf.inputs['Roughness'].default_value = roughness
    return mat

def merge_bmesh_into(src_bm, dst_bm, transform=None, mat_offset=0):
    vert_map = {}
    for v in src_bm.verts:
        pos = v.co
        if transform:
            pos = transform @ pos
        new_v = dst_bm.verts.new(pos)
        vert_map[v] = new_v
    for f in src_bm.faces:
        new_face_verts = [vert_map[v] for v in f.verts]
        try:
            new_f = dst_bm.faces.new(new_face_verts)
            new_f.material_index = f.material_index + mat_offset
        except ValueError:
            pass

def create_tube_bmesh(bm, path_points_with_radius, num_radial=8, cap_start=True, cap_end=True, mat_idx=0):
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
            angle = 2.0 * math.pi * j / num_radial
            offset = (norm * math.cos(angle) + binorm * math.sin(angle)) * r
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
            
    if cap_start:
        f = bm.faces.new(list(reversed(rings[0])))
        f.material_index = mat_idx
    if cap_end:
        f = bm.faces.new(rings[-1])
        f.material_index = mat_idx
        
    return rings

def create_plump_leaf_bmesh(length=0.185, max_width=0.118, thickness=0.0035, twist_deg=0.0, u_segs=8, v_segs=6):
    """
    Creates one solid curved young tree leaf with 3D organic curl, twist, and thickness.
    Length along +X, width along Y.
    Base at (0, 0, 0), tip at (+length, 0, z_drop).
    """
    bm = bmesh.new()
    top_verts = []
    bot_verts = []
    
    twist_rad = math.radians(twist_deg)
    
    for i in range(u_segs + 1):
        u = i / float(u_segs)
        
        # Broad, plump oval profile: wide across mid-body (u ~ 0.38 - 0.45)
        w_factor = math.sin(math.pi * (u ** 0.48)) * (1.0 - u * 0.14)
        half_w = (max_width * 0.5) * max(0.0, w_factor)
        
        # Longitudinal arch: rises at base, crests at ~25%, droops gracefully toward acute tip
        z_arch = 0.026 * math.sin(math.pi * u) - 0.058 * (u ** 1.5)
        
        # Organic twist along length
        twist_i = twist_rad * u
        cos_tw = math.cos(twist_i)
        sin_tw = math.sin(twist_i)
        
        row_top = []
        row_bot = []
        for j in range(v_segs + 1):
            v = -1.0 + 2.0 * j / float(v_segs)
            y_base = v * half_w
            
            # Transverse curve: convex upper surface (drooping at margins)
            z_cup = -0.018 * (v * v) * math.sin(math.pi * (u ** 0.7))
            
            # Subtle central depression (midrib crease)
            z_midrib = 0.0024 * (1.0 - abs(v)) * math.sin(math.pi * (u ** 0.7))
            
            z_base = z_arch + z_cup - z_midrib
            t_half = (thickness * 0.5) * (1.0 - 0.45 * abs(v))
            
            # Apply twist rotation around local X axis
            y_top = y_base * cos_tw - (z_base + t_half) * sin_tw
            z_top = y_base * sin_tw + (z_base + t_half) * cos_tw
            
            y_bot = y_base * cos_tw - (z_base - t_half) * sin_tw
            z_bot = y_base * sin_tw + (z_base - t_half) * cos_tw
            
            pt_top = Vector((u * length, y_top, z_top))
            pt_bot = Vector((u * length, y_bot, z_bot))
            
            row_top.append(bm.verts.new(pt_top))
            row_bot.append(bm.verts.new(pt_bot))
            
        top_verts.append(row_top)
        bot_verts.append(row_bot)
        
    # Build upper surface faces (normals pointing up +Z)
    for i in range(u_segs):
        for j in range(v_segs):
            v0 = top_verts[i][j]
            v1 = top_verts[i+1][j]
            v2 = top_verts[i+1][j+1]
            v3 = top_verts[i][j+1]
            bm.faces.new([v0, v1, v2, v3])
            
    # Build lower surface faces (normals pointing down -Z)
    for i in range(u_segs):
        for j in range(v_segs):
            v0 = bot_verts[i][j]
            v1 = bot_verts[i][j+1]
            v2 = bot_verts[i+1][j+1]
            v3 = bot_verts[i+1][j]
            bm.faces.new([v0, v1, v2, v3])
            
    # Side edge walls - Left margin (j = 0)
    for i in range(u_segs):
        v_top0 = top_verts[i][0]
        v_top1 = top_verts[i+1][0]
        v_bot0 = bot_verts[i][0]
        v_bot1 = bot_verts[i+1][0]
        bm.faces.new([v_top0, v_bot0, v_bot1, v_top1])
        
    # Side edge walls - Right margin (j = v_segs)
    for i in range(u_segs):
        v_top0 = top_verts[i][v_segs]
        v_top1 = top_verts[i+1][v_segs]
        v_bot0 = bot_verts[i][v_segs]
        v_bot1 = bot_verts[i+1][v_segs]
        bm.faces.new([v_top0, v_top1, v_bot1, v_bot0])
        
    # Base wall (i = 0)
    for j in range(v_segs):
        vt0 = top_verts[0][j]
        vt1 = top_verts[0][j+1]
        vb0 = bot_verts[0][j]
        vb1 = bot_verts[0][j+1]
        bm.faces.new([vt0, vt1, vb1, vb0])
        
    # Acute Tip wall (i = u_segs)
    for j in range(v_segs):
        vt0 = top_verts[u_segs][j]
        vt1 = top_verts[u_segs][j+1]
        vb0 = bot_verts[u_segs][j]
        vb1 = bot_verts[u_segs][j+1]
        bm.faces.new([vt0, vb0, vb1, vt1])
        
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    return bm

def build_trunk_and_branches():
    mesh = bpy.data.meshes.new("Trunk_Branches")
    obj = bpy.data.objects.new("Trunk_Branches", mesh)
    bpy.context.collection.objects.link(obj)
    
    bm = bmesh.new()
    
    # Main trunk spine: from root collar z=0.0 up to apex z=0.76m
    trunk_pts = [
        (Vector((0.000, 0.000, 0.000)), 0.0145),   # Base at root collar
        (Vector((0.003, 0.001, 0.060)), 0.0135),   # Slight initial lean right
        (Vector((0.008, 0.002, 0.140)), 0.0125),   # Transition out of base
        (Vector((0.011, 0.001, 0.220)), 0.0115),   # Under lower left leaf
        (Vector((0.010, -0.002, 0.300)), 0.0102),  # Under side shoot
        (Vector((0.004, -0.003, 0.380)), 0.0090),  # Inward curve
        (Vector((-0.004, -0.002, 0.460)), 0.0078), # S-curve crest
        (Vector((-0.010, 0.000, 0.540)), 0.0068),  # Lean left
        (Vector((-0.012, 0.001, 0.620)), 0.0058),  # Upper mid
        (Vector((-0.008, 0.001, 0.690)), 0.0048),  # Tapering to apex
        (Vector((-0.002, 0.000, 0.740)), 0.0038),  # Terminal stem
        (Vector((0.000, 0.000, 0.760)), 0.0028),   # Apex bud base
    ]
    create_tube_bmesh(bm, trunk_pts, num_radial=10, cap_start=True, cap_end=True)
    
    # Short petioles connecting leaves to the trunk:
    
    # 1. Petiole 1 (lower left, forward-left: az ~ 165 deg)
    petiole_1 = [
        (Vector((0.011, 0.001, 0.218)), 0.0045),
        (Vector((0.000, 0.002, 0.218)), 0.0038),
        (Vector((-0.010, 0.000, 0.218)), 0.0030),
    ]
    create_tube_bmesh(bm, petiole_1, num_radial=8, cap_start=False, cap_end=True)
    
    # 2. Side shoot branch (lower right, forward-right: az ~ -20 deg)
    side_shoot = [
        (Vector((0.010, -0.002, 0.278)), 0.0050),
        (Vector((0.035, -0.004, 0.282)), 0.0042),
        (Vector((0.060, -0.005, 0.285)), 0.0034),
        (Vector((0.078, -0.006, 0.284)), 0.0028),
    ]
    create_tube_bmesh(bm, side_shoot, num_radial=8, cap_start=False, cap_end=True)
    
    # 3. Petiole 2 (mid left, backward-left: az ~ 145 deg)
    petiole_2 = [
        (Vector((0.007, 0.000, 0.340)), 0.0042),
        (Vector((-0.004, 0.012, 0.340)), 0.0035),
        (Vector((-0.016, 0.030, 0.340)), 0.0028),
    ]
    create_tube_bmesh(bm, petiole_2, num_radial=8, cap_start=False, cap_end=True)
    
    # 4. Petiole 3 (mid right, forward-right: az ~ -10 deg)
    petiole_3 = [
        (Vector((0.001, -0.003, 0.430)), 0.0040),
        (Vector((0.024, -0.010, 0.430)), 0.0033),
        (Vector((0.048, -0.016, 0.430)), 0.0026),
    ]
    create_tube_bmesh(bm, petiole_3, num_radial=8, cap_start=False, cap_end=True)
    
    # 5. Petiole 4 (upper left, forward-left: az ~ 175 deg)
    petiole_4 = [
        (Vector((-0.008, 0.000, 0.520)), 0.0038),
        (Vector((-0.026, -0.005, 0.520)), 0.0031),
        (Vector((-0.046, -0.010, 0.520)), 0.0024),
    ]
    create_tube_bmesh(bm, petiole_4, num_radial=8, cap_start=False, cap_end=True)
    
    # 6. Petiole 5 (upper right, backward-right: az ~ 25 deg)
    petiole_5 = [
        (Vector((-0.016, 0.000, 0.600)), 0.0034),
        (Vector((0.004, 0.010, 0.606)), 0.0028),
        (Vector((0.024, 0.020, 0.610)), 0.0023),
    ]
    create_tube_bmesh(bm, petiole_5, num_radial=8, cap_start=False, cap_end=True)
    
    # 7. Petiole 6 (high left, backward-left: az ~ 150 deg)
    petiole_6 = [
        (Vector((-0.009, 0.001, 0.680)), 0.0032),
        (Vector((-0.022, 0.008, 0.685)), 0.0026),
        (Vector((-0.034, 0.016, 0.688)), 0.0021),
    ]
    create_tube_bmesh(bm, petiole_6, num_radial=8, cap_start=False, cap_end=True)
    
    # 8. Petiole 7 (high right, forward-right: az ~ -15 deg)
    petiole_7 = [
        (Vector((-0.004, 0.001, 0.720)), 0.0029),
        (Vector((0.008, -0.005, 0.725)), 0.0024),
        (Vector((0.020, -0.010, 0.728)), 0.0020),
    ]
    create_tube_bmesh(bm, petiole_7, num_radial=8, cap_start=False, cap_end=True)
    
    # Apical bud stalk (z~0.755m up to 0.770m)
    bud_stalk = [
        (Vector((0.000, 0.000, 0.755)), 0.0028),
        (Vector((0.000, 0.000, 0.770)), 0.0020),
    ]
    create_tube_bmesh(bm, bud_stalk, num_radial=8, cap_start=False, cap_end=True)
    
    bm.to_mesh(mesh)
    bm.free()
    
    mat_stem = create_material("M_Stem", (0.38, 0.68, 0.15, 1.0), roughness=0.45)
    mesh.materials.append(mat_stem)
    
    for poly in mesh.polygons:
        poly.use_smooth = True
        
    return obj

def build_foliage():
    mesh = bpy.data.meshes.new("Foliage")
    obj = bpy.data.objects.new("Foliage", mesh)
    bpy.context.collection.objects.link(obj)
    
    master_bm = bmesh.new()
    
    def add_leaf(length, width, pos, rot_mat, twist=0.0):
        bm_l = create_plump_leaf_bmesh(length=length, max_width=width, thickness=0.0035, twist_deg=twist, u_segs=8, v_segs=6)
        trans_mat = Matrix.Translation(pos)
        merge_bmesh_into(bm_l, master_bm, trans_mat @ rot_mat)
        bm_l.free()
        
    # --- 3D Spatial Phyllotaxis Leaves ---
    
    # 1. Leaf 1: Lower left (forward-left, z~0.218m)
    rot_1 = (Matrix.Rotation(math.radians(165), 4, 'Z') @ 
             Matrix.Rotation(math.radians(-38), 4, 'X') @ 
             Matrix.Rotation(math.radians(-10), 4, 'Y'))
    add_leaf(0.180, 0.112, Vector((-0.010, 0.000, 0.218)), rot_1, twist=5.0)
    
    # 2. Side shoot leaf (forward-right, z~0.284m)
    rot_side_l = (Matrix.Rotation(math.radians(-20), 4, 'Z') @ 
                  Matrix.Rotation(math.radians(38), 4, 'X') @ 
                  Matrix.Rotation(math.radians(12), 4, 'Y'))
    add_leaf(0.118, 0.072, Vector((0.078, -0.006, 0.284)), rot_side_l, twist=-4.0)
    
    # Side shoot tiny bud leaflets
    rot_sb1 = Matrix.Rotation(math.radians(-85), 4, 'Y') @ Matrix.Rotation(math.radians(15), 4, 'X')
    add_leaf(0.046, 0.024, Vector((0.072, -0.004, 0.288)), rot_sb1)
    rot_sb2 = Matrix.Rotation(math.radians(-70), 4, 'Y') @ Matrix.Rotation(math.radians(22), 4, 'X')
    add_leaf(0.038, 0.020, Vector((0.068, -0.002, 0.286)), rot_sb2)
    
    # 3. Leaf 2: Mid left (backward-left, z~0.340m)
    rot_2 = (Matrix.Rotation(math.radians(145), 4, 'Z') @ 
             Matrix.Rotation(math.radians(-36), 4, 'X') @ 
             Matrix.Rotation(math.radians(-12), 4, 'Y'))
    add_leaf(0.195, 0.122, Vector((-0.016, 0.030, 0.340)), rot_2, twist=-6.0)
    
    # 4. Leaf 3: Mid right (forward-right, z~0.430m)
    rot_3 = (Matrix.Rotation(math.radians(-10), 4, 'Z') @ 
             Matrix.Rotation(math.radians(38), 4, 'X') @ 
             Matrix.Rotation(math.radians(10), 4, 'Y'))
    add_leaf(0.205, 0.128, Vector((0.048, -0.016, 0.430)), rot_3, twist=6.0)
    
    # 5. Leaf 4: Upper left (forward-left, z~0.520m)
    rot_4 = (Matrix.Rotation(math.radians(175), 4, 'Z') @ 
             Matrix.Rotation(math.radians(-36), 4, 'X') @ 
             Matrix.Rotation(math.radians(-12), 4, 'Y'))
    add_leaf(0.188, 0.115, Vector((-0.046, -0.010, 0.520)), rot_4, twist=4.0)
    
    # 6. Leaf 5: Upper right (backward-right, z~0.610m)
    rot_5 = (Matrix.Rotation(math.radians(25), 4, 'Z') @ 
             Matrix.Rotation(math.radians(35), 4, 'X') @ 
             Matrix.Rotation(math.radians(14), 4, 'Y'))
    add_leaf(0.178, 0.108, Vector((0.024, 0.020, 0.610)), rot_5, twist=-5.0)
    
    # 7. Leaf 6: High left (backward-left, z~0.688m)
    rot_6 = (Matrix.Rotation(math.radians(150), 4, 'Z') @ 
             Matrix.Rotation(math.radians(-32), 4, 'X') @ 
             Matrix.Rotation(math.radians(-15), 4, 'Y'))
    add_leaf(0.158, 0.095, Vector((-0.034, 0.016, 0.688)), rot_6, twist=-4.0)
    
    # 8. Leaf 7: High right (forward-right, z~0.728m)
    rot_7 = (Matrix.Rotation(math.radians(-15), 4, 'Z') @ 
             Matrix.Rotation(math.radians(34), 4, 'X') @ 
             Matrix.Rotation(math.radians(16), 4, 'Y'))
    add_leaf(0.148, 0.088, Vector((0.020, -0.010, 0.728)), rot_7, twist=4.0)
    
    # 9. Apical Bud at apex (z~0.770m): 3 young tender leaflets standing upright
    rot_top_c = Matrix.Rotation(math.radians(-90), 4, 'Y') @ Matrix.Rotation(math.radians(10), 4, 'X')
    add_leaf(0.068, 0.032, Vector((0.000, 0.000, 0.770)), rot_top_c)
    
    rot_top_l = (Matrix.Rotation(math.radians(-75), 4, 'Y') @ 
                 Matrix.Rotation(math.radians(18), 4, 'X') @ 
                 Matrix.Rotation(math.radians(35), 4, 'Z'))
    add_leaf(0.054, 0.026, Vector((-0.004, 0.003, 0.768)), rot_top_l)
    
    rot_top_r = (Matrix.Rotation(math.radians(-105), 4, 'Y') @ 
                 Matrix.Rotation(math.radians(18), 4, 'X') @ 
                 Matrix.Rotation(math.radians(-35), 4, 'Z'))
    add_leaf(0.052, 0.025, Vector((0.004, 0.003, 0.768)), rot_top_r)
    
    master_bm.to_mesh(mesh)
    master_bm.free()
    
    mat_leaves = create_material("M_Leaves", (0.36, 0.72, 0.14, 1.0), roughness=0.38)
    mesh.materials.append(mat_leaves)
    
    for poly in mesh.polygons:
        poly.use_smooth = True
        
    return obj

def build_roots():
    mesh = bpy.data.meshes.new("Roots")
    obj = bpy.data.objects.new("Roots", mesh)
    bpy.context.collection.objects.link(obj)
    
    bm = bmesh.new()
    
    # =========================================================================
    # 3D RADIAL ROOT SYSTEM WITH ORGANIC Y-AXIS SPREAD
    # =========================================================================
    
    # 1. Main taproot: from collar z=0.012 down to z=-0.480m with sinuous 3D curves in both X and Y
    taproot_pts = [
        (Vector((0.000, 0.000, 0.012)), 0.0160),
        (Vector((0.001, 0.002, -0.020)), 0.0145),
        (Vector((0.005, 0.015, -0.075)), 0.0125),   # subtle forward drift (+Y)
        (Vector((0.014, 0.025, -0.150)), 0.0105),   # reaching forward (+Y)
        (Vector((0.012, 0.008, -0.235)), 0.0085),   # curving backward
        (Vector((0.004, -0.018, -0.320)), 0.0065),  # backward drift (-Y)
        (Vector((0.008, -0.012, -0.400)), 0.0042),
        (Vector((0.006, 0.002, -0.480)), 0.0008),   # delicate taproot tip
    ]
    create_tube_bmesh(bm, taproot_pts, num_radial=10, cap_start=True, cap_end=True)
    
    # -------------------------------------------------------------------------
    # 2. PROMINENT FORWARD ROOT (+Y Axis - vươn mạnh về phía trước trục Y)
    # -------------------------------------------------------------------------
    root_front = [
        (Vector((0.002, 0.010, -0.020)), 0.0076),
        (Vector((0.010, 0.065, -0.045)), 0.0062),
        (Vector((0.016, 0.130, -0.078)), 0.0048),
        (Vector((0.022, 0.185, -0.125)), 0.0032),
        (Vector((0.026, 0.222, -0.175)), 0.0006),
    ]
    create_tube_bmesh(bm, root_front, num_radial=8, cap_start=False, cap_end=True)
    
    # Sub-branch 1A (branching forward-right)
    sub_front_r = [
        (Vector((0.016, 0.130, -0.078)), 0.0038),
        (Vector((0.052, 0.165, -0.130)), 0.0025),
        (Vector((0.078, 0.192, -0.185)), 0.0006),
    ]
    create_tube_bmesh(bm, sub_front_r, num_radial=8, cap_start=False, cap_end=True)
    
    # -------------------------------------------------------------------------
    # 3. PROMINENT BACKWARD ROOT (-Y Axis - vươn mạnh về phía sau trục Y)
    # -------------------------------------------------------------------------
    root_back = [
        (Vector((-0.002, -0.010, -0.024)), 0.0076),
        (Vector((-0.010, -0.068, -0.048)), 0.0062),
        (Vector((-0.016, -0.135, -0.082)), 0.0048),
        (Vector((-0.020, -0.188, -0.130)), 0.0030),
        (Vector((-0.024, -0.226, -0.180)), 0.0006),
    ]
    create_tube_bmesh(bm, root_back, num_radial=8, cap_start=False, cap_end=True)
    
    # Sub-branch 2A (branching backward-left)
    sub_back_l = [
        (Vector((-0.016, -0.135, -0.082)), 0.0036),
        (Vector((-0.052, -0.170, -0.135)), 0.0024),
        (Vector((-0.078, -0.196, -0.190)), 0.0006),
    ]
    create_tube_bmesh(bm, sub_back_l, num_radial=8, cap_start=False, cap_end=True)
    
    # -------------------------------------------------------------------------
    # 4. WIDE LATERAL ROOT: LEFT-FORWARD (-X, +Y: chéo trước bên trái)
    # -------------------------------------------------------------------------
    root_left_fwd = [
        (Vector((-0.010, 0.005, -0.018)), 0.0080),
        (Vector((-0.065, 0.040, -0.042)), 0.0065),
        (Vector((-0.135, 0.070, -0.072)), 0.0050),
        (Vector((-0.195, 0.092, -0.112)), 0.0035),
        (Vector((-0.245, 0.105, -0.160)), 0.0006),
    ]
    create_tube_bmesh(bm, root_left_fwd, num_radial=8, cap_start=False, cap_end=True)
    
    # Sub-branch of Left-Forward
    sub_left_sub = [
        (Vector((-0.135, 0.070, -0.072)), 0.0038),
        (Vector((-0.172, 0.105, -0.125)), 0.0025),
        (Vector((-0.202, 0.130, -0.178)), 0.0006),
    ]
    create_tube_bmesh(bm, sub_left_sub, num_radial=8, cap_start=False, cap_end=True)
    
    # -------------------------------------------------------------------------
    # 5. WIDE LATERAL ROOT: RIGHT-BACKWARD (+X, -Y: chéo sau bên phải)
    # -------------------------------------------------------------------------
    root_right_back = [
        (Vector((0.010, -0.005, -0.020)), 0.0080),
        (Vector((0.068, -0.042, -0.044)), 0.0065),
        (Vector((0.138, -0.072, -0.076)), 0.0050),
        (Vector((0.198, -0.095, -0.118)), 0.0035),
        (Vector((0.248, -0.108, -0.168)), 0.0006),
    ]
    create_tube_bmesh(bm, root_right_back, num_radial=8, cap_start=False, cap_end=True)
    
    # Sub-branch of Right-Backward
    sub_right_sub = [
        (Vector((0.138, -0.072, -0.076)), 0.0038),
        (Vector((0.176, -0.106, -0.128)), 0.0025),
        (Vector((0.206, -0.132, -0.182)), 0.0006),
    ]
    create_tube_bmesh(bm, sub_right_sub, num_radial=8, cap_start=False, cap_end=True)
    
    # -------------------------------------------------------------------------
    # 6. MID-DEPTH ROOT: LEFT-BACKWARD (-X, -Y: chéo sau bên trái, z~-0.10m)
    # -------------------------------------------------------------------------
    root_mid_left_b = [
        (Vector((-0.004, -0.006, -0.100)), 0.0058),
        (Vector((-0.046, -0.044, -0.145)), 0.0046),
        (Vector((-0.095, -0.078, -0.200)), 0.0032),
        (Vector((-0.142, -0.105, -0.265)), 0.0018),
        (Vector((-0.175, -0.120, -0.320)), 0.0006),
    ]
    create_tube_bmesh(bm, root_mid_left_b, num_radial=8, cap_start=False, cap_end=True)
    
    # -------------------------------------------------------------------------
    # 7. MID-DEPTH ROOT: RIGHT-FORWARD (+X, +Y: chéo trước bên phải, z~-0.13m)
    # -------------------------------------------------------------------------
    root_mid_right_f = [
        (Vector((0.008, 0.010, -0.130)), 0.0056),
        (Vector((0.050, 0.048, -0.175)), 0.0042),
        (Vector((0.098, 0.082, -0.230)), 0.0030),
        (Vector((0.142, 0.105, -0.288)), 0.0016),
        (Vector((0.175, 0.118, -0.342)), 0.0006),
    ]
    create_tube_bmesh(bm, root_mid_right_f, num_radial=8, cap_start=False, cap_end=True)
    
    bm.to_mesh(mesh)
    bm.free()
    
    mat_roots = create_material("M_Roots", (0.94, 0.89, 0.79, 1.0), roughness=0.55)
    mesh.materials.append(mat_roots)
    
    for poly in mesh.polygons:
        poly.use_smooth = True
        
    return obj

def setup_camera_and_lights():
    cam_data = bpy.data.cameras.new(name="PreviewCam")
    cam_data.lens = 50.0
    cam_obj = bpy.data.objects.new("PreviewCam", cam_data)
    bpy.context.collection.objects.link(cam_obj)
    bpy.context.scene.camera = cam_obj
    
    # Key light: bright neutral key light from front-left
    sun_data = bpy.data.lights.new(name="KeyLight", type='SUN')
    sun_data.energy = 4.5
    sun_data.color = (1.0, 0.98, 0.95)
    sun_obj = bpy.data.objects.new("KeyLight", sun_data)
    bpy.context.collection.objects.link(sun_obj)
    sun_obj.rotation_euler = (math.radians(52), math.radians(18), math.radians(-35))
    
    # Fill light: soft fill from front-right
    fill_data = bpy.data.lights.new(name="FillLight", type='SUN')
    fill_data.energy = 3.2
    fill_data.color = (0.95, 0.97, 1.0)
    fill_obj = bpy.data.objects.new("FillLight", fill_data)
    bpy.context.collection.objects.link(fill_obj)
    fill_obj.rotation_euler = (math.radians(45), math.radians(-25), math.radians(40))
    
    # Rim light from behind
    rim_data = bpy.data.lights.new(name="RimLight", type='SUN')
    rim_data.energy = 1.8
    rim_data.color = (1.0, 1.0, 1.0)
    rim_obj = bpy.data.objects.new("RimLight", rim_data)
    bpy.context.collection.objects.link(rim_obj)
    rim_obj.rotation_euler = (math.radians(-60), math.radians(10), math.radians(180))
    
    return cam_obj

def render_multi_angles(cam_obj, output_dir):
    os.makedirs(output_dir, exist_ok=True)
    scene = bpy.context.scene
    scene.render.resolution_x = 1024
    scene.render.resolution_y = 1024
    scene.render.film_transparent = True
    scene.render.image_settings.file_format = 'PNG'
    
    target = Vector((0.0, 0.0, 0.160))
    
    angles = [
        ("preview_front.png", 0.0, 4.0, 2.45),
        ("preview_back.png", 180.0, 4.0, 2.45),
        ("preview_left.png", 90.0, 4.0, 2.45),
        ("preview_right.png", -90.0, 4.0, 2.45),
        ("preview_top_down.png", 0.0, 84.0, 2.30),
        ("preview_perspective_front_3_4.png", 35.0, 14.0, 2.45),
        ("preview_perspective_back_3_4.png", 215.0, 14.0, 2.45),
    ]
    
    for name, yaw_deg, pitch_deg, dist in angles:
        yaw = math.radians(yaw_deg)
        pitch = math.radians(pitch_deg)
        
        cx = target.x + dist * math.sin(yaw) * math.cos(pitch)
        cy = target.y - dist * math.cos(yaw) * math.cos(pitch)
        cz = target.z + dist * math.sin(pitch)
        
        cam_obj.location = Vector((cx, cy, cz))
        direction = target - cam_obj.location
        rot_quat = direction.to_track_quat('-Z', 'Y')
        cam_obj.rotation_euler = rot_quat.to_euler()
        
        scene.render.filepath = os.path.join(output_dir, name)
        bpy.ops.render.render(write_still=True)
        print(f"Rendered: {scene.render.filepath}")

def main():
    clear_scene()
    
    roots = build_roots()
    trunk = build_trunk_and_branches()
    foliage = build_foliage()
    
    all_objs = [roots, trunk, foliage]
    
    # Verify transforms & apply
    for obj in all_objs:
        obj.location = Vector((0, 0, 0))
        obj.rotation_euler = (0, 0, 0)
        obj.scale = (1, 1, 1)
        bpy.context.view_layer.objects.active = obj
        obj.select_set(True)
        bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
        obj.select_set(False)
        
    # Triangle stats
    total_tris = 0
    print("\n--- YOUNG TREE MESH STATISTICS (3D ROOTS) ---")
    for obj in all_objs:
        depsgraph = bpy.context.evaluated_depsgraph_get()
        eval_obj = obj.evaluated_get(depsgraph)
        m = eval_obj.to_mesh()
        m.calc_loop_triangles()
        tris = len(m.loop_triangles)
        verts = len(m.vertices)
        total_tris += tris
        print(f"Object '{obj.name}': {verts} verts, {len(m.polygons)} quads/polys, {tris} triangles")
        eval_obj.to_mesh_clear()
    print(f"TOTAL TRIANGLES: {total_tris} (Target: < 5,000)\n")
    
    # Save directly to the existing .blend file
    blend_path = os.path.abspath(r"ArtSource\Models\Tree_Young_Simple_v01.blend")
    os.makedirs(os.path.dirname(blend_path), exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=blend_path)
    print(f"Saved directly to target blend file: {blend_path}")
    
    # Setup camera and lights for multi-angle preview
    cam_obj = setup_camera_and_lights()
    evidence_dir = os.path.abspath(r"Evidence\YoungTree_v01_Audit")
    render_multi_angles(cam_obj, evidence_dir)
    
    # Remove camera and lights before export
    bpy.ops.object.select_all(action='DESELECT')
    cam_obj.select_set(True)
    for light in [o for o in bpy.data.objects if o.type == 'LIGHT']:
        light.select_set(True)
    bpy.ops.object.delete()
    
    # Select only the 3 mesh objects for export
    bpy.ops.object.select_all(action='DESELECT')
    for obj in all_objs:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = all_objs[0]
    
    # Overwrite the exact existing FBX file
    fbx_path = os.path.abspath(r"ArtSource\Exports\Tree_Young_Simple_v01.fbx")
    os.makedirs(os.path.dirname(fbx_path), exist_ok=True)
    
    bpy.ops.export_scene.fbx(
        filepath=fbx_path,
        use_selection=True,
        global_scale=1.0,
        apply_unit_scale=True,
        apply_scale_options='FBX_SCALE_ALL',
        axis_forward='-Z',
        axis_up='Y',
        bake_space_transform=False,
        object_types={'MESH'},
        mesh_smooth_type='FACE',
        add_leaf_bones=False,
        primary_bone_axis='Y',
        secondary_bone_axis='X',
        use_armature_deform_only=False,
        bake_anim=False
    )
    print(f"Exported directly over existing FBX: {fbx_path}")

if __name__ == "__main__":
    main()
