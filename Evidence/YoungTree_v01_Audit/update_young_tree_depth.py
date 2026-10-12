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
        
    # Top faces
    for i in range(u_segs):
        for j in range(v_segs):
            bm.faces.new([top_verts[i][j], top_verts[i+1][j], top_verts[i+1][j+1], top_verts[i][j+1]])
            
    # Bottom faces
    for i in range(u_segs):
        for j in range(v_segs):
            bm.faces.new([bot_verts[i][j], bot_verts[i][j+1], bot_verts[i+1][j+1], bot_verts[i+1][j]])
            
    # Margins
    for i in range(u_segs):
        bm.faces.new([top_verts[i][0], bot_verts[i][0], bot_verts[i+1][0], top_verts[i+1][0]])
        bm.faces.new([top_verts[i][v_segs], top_verts[i+1][v_segs], bot_verts[i+1][v_segs], bot_verts[i][v_segs]])
        
    # Base
    for j in range(v_segs):
        bm.faces.new([top_verts[0][j], top_verts[0][j+1], bot_verts[0][j+1], bot_verts[0][j]])
        
    # Tip
    for j in range(v_segs):
        bm.faces.new([top_verts[u_segs][j], bot_verts[u_segs][j], bot_verts[u_segs][j+1], top_verts[u_segs][j+1]])
        
    return bm

def build_trunk_and_branches():
    mesh = bpy.data.meshes.new("Trunk_Branches")
    obj = bpy.data.objects.new("Trunk_Branches", mesh)
    bpy.context.collection.objects.link(obj)
    
    bm = bmesh.new()
    
    # 1. Main Stem: starts at collar z=0.00 up to apex at z=0.76m
    # Dynamic S-curve matching 02_young_tree.png
    stem_pts = [
        (Vector((0.000, 0.000, -0.005)), 0.0135),
        (Vector((0.006, 0.002, 0.050)), 0.0120),
        (Vector((0.020, 0.005, 0.130)), 0.0108),
        (Vector((0.034, 0.008, 0.220)), 0.0094),
        (Vector((0.028, 0.005, 0.310)), 0.0084),
        (Vector((0.010, 0.002, 0.400)), 0.0074),
        (Vector((-0.010, -0.001, 0.500)), 0.0065),
        (Vector((-0.016, 0.000, 0.600)), 0.0055),
        (Vector((-0.008, 0.001, 0.690)), 0.0044),
        (Vector((0.000, 0.000, 0.755)), 0.0036),
    ]
    create_tube_bmesh(bm, stem_pts, num_radial=10, cap_start=True, cap_end=False)
    
    # Petioles with 3D spatial directions matching the leaves:
    # 1. Petiole 1 (lower left, forward-left: az ~ 165 deg)
    petiole_1 = [
        (Vector((0.032, 0.007, 0.210)), 0.0044),
        (Vector((0.012, 0.004, 0.216)), 0.0036),
        (Vector((-0.010, 0.000, 0.218)), 0.0028),
    ]
    create_tube_bmesh(bm, petiole_1, num_radial=8, cap_start=False, cap_end=True)
    
    # 2. Side branch (lower-mid right, forward-right: az ~ -20 deg)
    side_branch = [
        (Vector((0.032, 0.007, 0.260)), 0.0046),
        (Vector((0.056, 0.001, 0.274)), 0.0038),
        (Vector((0.078, -0.006, 0.284)), 0.0030),
    ]
    create_tube_bmesh(bm, side_branch, num_radial=8, cap_start=False, cap_end=True)
    
    # 3. Petiole 2 (mid left, backward-left: az ~ 145 deg)
    petiole_2 = [
        (Vector((0.024, 0.004, 0.330)), 0.0040),
        (Vector((0.004, 0.018, 0.336)), 0.0034),
        (Vector((-0.016, 0.030, 0.340)), 0.0028),
    ]
    create_tube_bmesh(bm, petiole_2, num_radial=8, cap_start=False, cap_end=True)
    
    # 4. Petiole 3 (mid right, forward-right: az ~ -10 deg)
    petiole_3 = [
        (Vector((0.007, 0.001, 0.420)), 0.0040),
        (Vector((0.028, -0.008, 0.426)), 0.0034),
        (Vector((0.048, -0.016, 0.430)), 0.0028),
    ]
    create_tube_bmesh(bm, petiole_3, num_radial=8, cap_start=False, cap_end=True)
    
    # 5. Petiole 4 (upper left, forward-left: az ~ 175 deg)
    petiole_4 = [
        (Vector((-0.011, -0.001, 0.510)), 0.0036),
        (Vector((-0.028, -0.006, 0.516)), 0.0030),
        (Vector((-0.046, -0.010, 0.520)), 0.0025),
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
    # Azimuth 165 deg (pointing -X, slightly -Y), tilted upward and forward
    rot_1 = (Matrix.Rotation(math.radians(165), 4, 'Z') @ 
             Matrix.Rotation(math.radians(-38), 4, 'X') @ 
             Matrix.Rotation(math.radians(-10), 4, 'Y'))
    add_leaf(0.180, 0.112, Vector((-0.010, 0.000, 0.218)), rot_1, twist=5.0)
    
    # 2. Side shoot leaf (forward-right, z~0.284m)
    # Azimuth -20 deg (pointing +X, -Y)
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
    # Azimuth 145 deg (pointing -X, +Y: reaching into background depth!)
    rot_2 = (Matrix.Rotation(math.radians(145), 4, 'Z') @ 
             Matrix.Rotation(math.radians(-36), 4, 'X') @ 
             Matrix.Rotation(math.radians(-12), 4, 'Y'))
    add_leaf(0.195, 0.122, Vector((-0.016, 0.030, 0.340)), rot_2, twist=-6.0)
    
    # 4. Leaf 3: Mid right (forward-right, z~0.430m)
    # Azimuth -10 deg (pointing +X, -Y: reaching into foreground depth!)
    rot_3 = (Matrix.Rotation(math.radians(-10), 4, 'Z') @ 
             Matrix.Rotation(math.radians(38), 4, 'X') @ 
             Matrix.Rotation(math.radians(10), 4, 'Y'))
    add_leaf(0.205, 0.128, Vector((0.048, -0.016, 0.430)), rot_3, twist=6.0)
    
    # 5. Leaf 4: Upper left (forward-left, z~0.520m)
    # Azimuth 175 deg (pointing -X, slightly -Y)
    rot_4 = (Matrix.Rotation(math.radians(175), 4, 'Z') @ 
             Matrix.Rotation(math.radians(-36), 4, 'X') @ 
             Matrix.Rotation(math.radians(-12), 4, 'Y'))
    add_leaf(0.188, 0.115, Vector((-0.046, -0.010, 0.520)), rot_4, twist=4.0)
    
    # 6. Leaf 5: Upper right (backward-right, z~0.610m)
    # Azimuth 25 deg (pointing +X, +Y: reaching into background depth!)
    rot_5 = (Matrix.Rotation(math.radians(25), 4, 'Z') @ 
             Matrix.Rotation(math.radians(35), 4, 'X') @ 
             Matrix.Rotation(math.radians(14), 4, 'Y'))
    add_leaf(0.178, 0.108, Vector((0.024, 0.020, 0.610)), rot_5, twist=-5.0)
    
    # 7. Leaf 6: High left (backward-left, z~0.688m)
    # Azimuth 150 deg (pointing -X, +Y)
    rot_6 = (Matrix.Rotation(math.radians(150), 4, 'Z') @ 
             Matrix.Rotation(math.radians(-32), 4, 'X') @ 
             Matrix.Rotation(math.radians(-15), 4, 'Y'))
    add_leaf(0.158, 0.095, Vector((-0.034, 0.016, 0.688)), rot_6, twist=-4.0)
    
    # 8. Leaf 7: High right (forward-right, z~0.728m)
    # Azimuth -15 deg (pointing +X, -Y)
    rot_7 = (Matrix.Rotation(math.radians(-15), 4, 'Z') @ 
             Matrix.Rotation(math.radians(34), 4, 'X') @ 
             Matrix.Rotation(math.radians(16), 4, 'Y'))
    add_leaf(0.148, 0.088, Vector((0.020, -0.010, 0.728)), rot_7, twist=4.0)
    
    # 9. Apical Bud at apex (z~0.770m): 3 young tender leaflets standing upright
    # Central upright leaflet reaching z~0.835m
    rot_top_c = Matrix.Rotation(math.radians(-90), 4, 'Y') @ Matrix.Rotation(math.radians(10), 4, 'X')
    add_leaf(0.068, 0.032, Vector((0.000, 0.000, 0.770)), rot_top_c)
    
    # Left bud leaflet (tilted left-back)
    rot_top_l = (Matrix.Rotation(math.radians(-75), 4, 'Y') @ 
                 Matrix.Rotation(math.radians(18), 4, 'X') @ 
                 Matrix.Rotation(math.radians(35), 4, 'Z'))
    add_leaf(0.054, 0.026, Vector((-0.004, 0.003, 0.768)), rot_top_l)
    
    # Right bud leaflet (tilted right-back)
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
    
    # 1. Main taproot: from collar z=0.010 down to z=-0.480m with natural organic curves
    taproot_pts = [
        (Vector((0.000, 0.000, 0.012)), 0.0160),
        (Vector((0.000, 0.000, -0.018)), 0.0145),
        (Vector((0.003, -0.003, -0.065)), 0.0125),
        (Vector((0.008, 0.003, -0.135)), 0.0105),
        (Vector((0.014, 0.005, -0.215)), 0.0088),
        (Vector((0.010, 0.003, -0.295)), 0.0070),
        (Vector((0.003, 0.001, -0.365)), 0.0052),
        (Vector((0.007, 0.002, -0.425)), 0.0034),
        (Vector((0.010, 0.003, -0.480)), 0.0008),
    ]
    create_tube_bmesh(bm, taproot_pts, num_radial=10, cap_start=True, cap_end=True)
    
    # 2. Upper Lateral Root 1 (wide spreading left): arches out and curves down
    root_l1 = [
        (Vector((-0.002, 0.000, -0.018)), 0.0080),
        (Vector((-0.045, -0.008, -0.038)), 0.0068),
        (Vector((-0.105, -0.014, -0.065)), 0.0055),
        (Vector((-0.165, -0.018, -0.098)), 0.0040),
        (Vector((-0.215, -0.020, -0.135)), 0.0026),
        (Vector((-0.255, -0.022, -0.170)), 0.0006),
    ]
    create_tube_bmesh(bm, root_l1, num_radial=8, cap_start=False, cap_end=True)
    
    # Sub-branch of L1
    sub_l1 = [
        (Vector((-0.105, -0.014, -0.065)), 0.0042),
        (Vector((-0.145, -0.010, -0.115)), 0.0030),
        (Vector((-0.180, -0.008, -0.175)), 0.0006),
    ]
    create_tube_bmesh(bm, sub_l1, num_radial=8, cap_start=False, cap_end=True)
    
    # 3. Upper Lateral Root 2 (wide spreading right): arches out and curves down
    root_r1 = [
        (Vector((0.003, 0.001, -0.022)), 0.0078),
        (Vector((0.048, 0.008, -0.042)), 0.0066),
        (Vector((0.108, 0.014, -0.070)), 0.0052),
        (Vector((0.162, 0.018, -0.105)), 0.0038),
        (Vector((0.208, 0.020, -0.142)), 0.0024),
        (Vector((0.245, 0.022, -0.175)), 0.0006),
    ]
    create_tube_bmesh(bm, root_r1, num_radial=8, cap_start=False, cap_end=True)
    
    # Sub-branch of R1
    sub_r1 = [
        (Vector((0.108, 0.014, -0.070)), 0.0040),
        (Vector((0.140, 0.010, -0.120)), 0.0028),
        (Vector((0.170, 0.008, -0.180)), 0.0006),
    ]
    create_tube_bmesh(bm, sub_r1, num_radial=8, cap_start=False, cap_end=True)
    
    # 4. Mid Lateral Root 3 (mid-depth left): emerges at z~-0.105m
    root_l2 = [
        (Vector((0.006, 0.001, -0.105)), 0.0062),
        (Vector((-0.038, -0.006, -0.145)), 0.0050),
        (Vector((-0.088, -0.012, -0.195)), 0.0038),
        (Vector((-0.135, -0.015, -0.250)), 0.0024),
        (Vector((-0.170, -0.018, -0.310)), 0.0006),
    ]
    create_tube_bmesh(bm, root_l2, num_radial=8, cap_start=False, cap_end=True)
    
    # 5. Mid Lateral Root 4 (mid-depth right): emerges at z~-0.145m
    root_r2 = [
        (Vector((0.010, 0.002, -0.145)), 0.0058),
        (Vector((0.045, 0.006, -0.185)), 0.0046),
        (Vector((0.092, 0.010, -0.235)), 0.0034),
        (Vector((0.138, 0.012, -0.290)), 0.0020),
        (Vector((0.172, 0.014, -0.350)), 0.0006),
    ]
    create_tube_bmesh(bm, root_r2, num_radial=8, cap_start=False, cap_end=True)
    
    bm.to_mesh(mesh)
    bm.free()
    
    mat_roots = create_material("M_Roots", (0.94, 0.89, 0.79, 1.0), roughness=0.55)
    mesh.materials.append(mat_roots)
    
    for poly in mesh.polygons:
        poly.use_smooth = True
        
    return obj

def setup_camera_and_lights():
    cam_data = bpy.data.cameras.new("RenderCam")
    cam_data.lens = 65.0
    cam_obj = bpy.data.objects.new("RenderCam", cam_data)
    bpy.context.collection.objects.link(cam_obj)
    bpy.context.scene.camera = cam_obj
    
    # Key light: warm key from front-upper-left
    light_data = bpy.data.lights.new(name="KeyLight", type='SUN')
    light_data.energy = 4.2
    light_data.color = (1.0, 0.98, 0.95)
    light_obj = bpy.data.objects.new("KeyLight", light_data)
    bpy.context.collection.objects.link(light_obj)
    light_obj.rotation_euler = (math.radians(45), math.radians(20), math.radians(-30))
    
    # Fill light: soft warm fill from front-upper-right
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
    """
    Renders 7 required validation angles:
    1. Front (Trước)
    2. Back (Sau)
    3. Left (Trái)
    4. Right (Phải)
    5. Top-down (Trên xuống)
    6. 3/4 Front Perspective (Phối cảnh trước)
    7. 3/4 Back Perspective (Phối cảnh sau)
    """
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
    print("\n--- YOUNG TREE MESH STATISTICS ---")
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
