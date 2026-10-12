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

def build_trunk_and_branches():
    mesh = bpy.data.meshes.new("Trunk_Branches")
    obj = bpy.data.objects.new("Trunk_Branches", mesh)
    bpy.context.collection.objects.link(obj)
    
    bm = bmesh.new()
    
    # 1. Main Stem: starts right inside seed shells at z=-0.005 up to fork at z=0.185
    stem_pts = [
        (Vector((0.000, 0.000, -0.005)), 0.0080),
        (Vector((0.000, 0.000, 0.025)), 0.0076),
        (Vector((0.001, 0.000, 0.065)), 0.0070),
        (Vector((0.003, 0.001, 0.105)), 0.0064),
        (Vector((0.004, 0.001, 0.142)), 0.0058),
        (Vector((0.002, 0.001, 0.168)), 0.0052),
        (Vector((0.000, 0.000, 0.186)), 0.0046),
    ]
    create_tube_bmesh(bm, stem_pts, num_radial=10, cap_start=True, cap_end=False)
    
    # 2. Left Petiole: curves out left to meet left cotyledon leaf
    petiole_l = [
        (Vector((0.000, 0.000, 0.186)), 0.0045),
        (Vector((-0.006, 0.002, 0.188)), 0.0040),
        (Vector((-0.014, 0.003, 0.190)), 0.0035),
        (Vector((-0.022, 0.003, 0.188)), 0.0030),
    ]
    create_tube_bmesh(bm, petiole_l, num_radial=8, cap_start=False, cap_end=True)
    
    # 3. Right Petiole: curves out right to meet right cotyledon leaf
    petiole_r = [
        (Vector((0.000, 0.000, 0.186)), 0.0045),
        (Vector((0.006, -0.002, 0.188)), 0.0040),
        (Vector((0.014, -0.003, 0.190)), 0.0035),
        (Vector((0.022, -0.003, 0.188)), 0.0030),
    ]
    create_tube_bmesh(bm, petiole_r, num_radial=8, cap_start=False, cap_end=True)
    
    # 4. Central bud stalk
    stalk_bud = [
        (Vector((0.000, 0.000, 0.186)), 0.0038),
        (Vector((0.000, 0.000, 0.200)), 0.0030),
    ]
    create_tube_bmesh(bm, stalk_bud, num_radial=8, cap_start=False, cap_end=True)
    
    bm.to_mesh(mesh)
    bm.free()
    
    mat_stem = create_material("M_Stem", (0.38, 0.68, 0.15, 1.0), roughness=0.45)
    mesh.materials.append(mat_stem)
    
    for poly in mesh.polygons:
        poly.use_smooth = True
        
    return obj

def build_roots():
    mesh = bpy.data.meshes.new("Roots")
    obj = bpy.data.objects.new("Roots", mesh)
    bpy.context.collection.objects.link(obj)
    
    bm = bmesh.new()
    
    # 1. Main taproot: starts tucked right under the seed shells at z=0.012, tapers down to z=-0.195
    taproot_pts = [
        (Vector((0.000, 0.000, 0.012)), 0.0084),
        (Vector((0.000, 0.000, -0.010)), 0.0078),
        (Vector((0.001, -0.001, -0.038)), 0.0070),
        (Vector((0.003, 0.001, -0.070)), 0.0060),
        (Vector((0.006, 0.003, -0.100)), 0.0048),
        (Vector((0.007, 0.003, -0.130)), 0.0036),
        (Vector((0.005, 0.001, -0.158)), 0.0025),
        (Vector((0.007, 0.002, -0.180)), 0.0014),
        (Vector((0.009, 0.003, -0.198)), 0.0004),
    ]
    create_tube_bmesh(bm, taproot_pts, num_radial=10, cap_start=True, cap_end=True)
    
    # 2. Lateral root 1 (left): branches off at z=-0.036
    root_l = [
        (Vector((-0.002, -0.001, -0.036)), 0.0038),
        (Vector((-0.018, -0.006, -0.050)), 0.0030),
        (Vector((-0.038, -0.010, -0.066)), 0.0022),
        (Vector((-0.056, -0.014, -0.080)), 0.0014),
        (Vector((-0.072, -0.016, -0.094)), 0.0004),
    ]
    create_tube_bmesh(bm, root_l, num_radial=8, cap_start=False, cap_end=True)
    
    # 3. Lateral root 2 (right): branches off at z=-0.060
    root_r = [
        (Vector((0.004, 0.002, -0.060)), 0.0035),
        (Vector((0.020, 0.006, -0.074)), 0.0028),
        (Vector((0.038, 0.012, -0.092)), 0.0020),
        (Vector((0.054, 0.016, -0.110)), 0.0012),
        (Vector((0.065, 0.018, -0.124)), 0.0004),
    ]
    create_tube_bmesh(bm, root_r, num_radial=8, cap_start=False, cap_end=True)
    
    bm.to_mesh(mesh)
    bm.free()
    
    mat_roots = create_material("M_Roots", (0.94, 0.90, 0.80, 1.0), roughness=0.55)
    mesh.materials.append(mat_roots)
    
    for poly in mesh.polygons:
        poly.use_smooth = True
        
    return obj

def create_plump_leaf_bmesh(length=0.122, max_width=0.088, thickness=0.0028, u_segs=10, v_segs=8):
    """
    Creates one solid curved cotyledon leaf.
    Length along +X, width along Y.
    Base at (0, 0, 0), tip at (+length, 0, z_drop).
    Broad, plump oval shape matching 01_seedling.png.
    """
    bm = bmesh.new()
    top_verts = []
    bot_verts = []
    
    for i in range(u_segs + 1):
        u = i / float(u_segs)
        
        # Plump oval profile factor
        w_factor = math.sin(math.pi * (u ** 0.58)) * (1.0 - u * 0.16)
        half_w = (max_width * 0.5) * max(0.0, w_factor)
        
        # Longitudinal arch: rises gently at base (+0.010), then curves gracefully downward (-0.032 at tip)
        z_arch = 0.010 * math.sin(math.pi * u) - 0.032 * (u ** 1.6)
        
        row_top = []
        row_bot = []
        for j in range(v_segs + 1):
            v = -1.0 + 2.0 * j / float(v_segs) # -1 to +1
            y_pos = v * half_w
            
            # Transverse curve: convex upper surface (drooping at margins)
            z_cup = -0.013 * (v * v) * math.sin(math.pi * (u ** 0.8))
            
            # Subtle central depression (midrib crease)
            z_midrib = 0.0016 * (1.0 - abs(v)) * math.sin(math.pi * (u ** 0.8))
            
            z_base = z_arch + z_cup - z_midrib
            t_half = (thickness * 0.5) * (1.0 - 0.45 * abs(v))
            
            pt_top = Vector((u * length, y_pos, z_base + t_half))
            pt_bot = Vector((u * length, y_pos, z_base - t_half))
            
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

def build_foliage():
    mesh = bpy.data.meshes.new("Foliage")
    obj = bpy.data.objects.new("Foliage", mesh)
    bpy.context.collection.objects.link(obj)
    
    master_bm = bmesh.new()
    
    # 1. Left Cotyledon Leaf
    # Plump oval leaf, spanning to left -X, tilted forward to face camera and upward
    bm_leaf_l = create_plump_leaf_bmesh(length=0.126, max_width=0.092, thickness=0.0028, u_segs=10, v_segs=8)
    rot_l = (Matrix.Rotation(math.radians(178), 4, 'Z') @ 
             Matrix.Rotation(math.radians(-35), 4, 'X') @ 
             Matrix.Rotation(math.radians(-10), 4, 'Y'))
    trans_l = Matrix.Translation(Vector((-0.022, 0.003, 0.188)))
    merge_bmesh_into(bm_leaf_l, master_bm, trans_l @ rot_l)
    bm_leaf_l.free()
    
    # 2. Right Cotyledon Leaf
    # Plump oval leaf, spanning to right +X, symmetrically tilted forward to face camera and upward
    bm_leaf_r = create_plump_leaf_bmesh(length=0.126, max_width=0.092, thickness=0.0028, u_segs=10, v_segs=8)
    rot_r = (Matrix.Rotation(math.radians(-2), 4, 'Z') @ 
             Matrix.Rotation(math.radians(35), 4, 'X') @ 
             Matrix.Rotation(math.radians(10), 4, 'Y'))
    trans_r = Matrix.Translation(Vector((0.022, -0.003, 0.188)))
    merge_bmesh_into(bm_leaf_r, master_bm, trans_r @ rot_r)
    bm_leaf_r.free()
    
    # 3. Apical Bud (3 small erect leaflets pointing STRAIGHT UP like a crown)
    # Center upright leaflet (pointing straight UP to z ~ 0.258m)
    bm_bud_c = create_plump_leaf_bmesh(length=0.058, max_width=0.026, thickness=0.0018, u_segs=6, v_segs=5)
    rot_bc = Matrix.Rotation(math.radians(-90), 4, 'Y') @ Matrix.Rotation(math.radians(10), 4, 'X')
    trans_bc = Matrix.Translation(Vector((0.000, 0.000, 0.200)))
    merge_bmesh_into(bm_bud_c, master_bm, trans_bc @ rot_bc)
    bm_bud_c.free()
    
    # Left inner bud leaflet
    bm_bud_l = create_plump_leaf_bmesh(length=0.046, max_width=0.020, thickness=0.0015, u_segs=5, v_segs=4)
    rot_bl = (Matrix.Rotation(math.radians(-75), 4, 'Y') @ 
              Matrix.Rotation(math.radians(18), 4, 'X') @ 
              Matrix.Rotation(math.radians(15), 4, 'Z'))
    trans_bl = Matrix.Translation(Vector((-0.004, 0.002, 0.198)))
    merge_bmesh_into(bm_bud_l, master_bm, trans_bl @ rot_bl)
    bm_bud_l.free()
    
    # Right inner bud leaflet
    bm_bud_r = create_plump_leaf_bmesh(length=0.044, max_width=0.019, thickness=0.0015, u_segs=5, v_segs=4)
    rot_br = (Matrix.Rotation(math.radians(-105), 4, 'Y') @ 
              Matrix.Rotation(math.radians(18), 4, 'X') @ 
              Matrix.Rotation(math.radians(-15), 4, 'Z'))
    trans_br = Matrix.Translation(Vector((0.004, -0.002, 0.198)))
    merge_bmesh_into(bm_bud_r, master_bm, trans_br @ rot_br)
    bm_bud_r.free()
    
    master_bm.to_mesh(mesh)
    master_bm.free()
    
    mat_leaves = create_material("M_Leaves", (0.42, 0.74, 0.18, 1.0), roughness=0.38)
    mesh.materials.append(mat_leaves)
    
    for poly in mesh.polygons:
        poly.use_smooth = True
        
    return obj

def create_nut_shell_half_bmesh(rx=0.035, ry=0.028, rz=0.040, thickness=0.0045, u_segs=8, v_segs=16):
    """
    Creates one half of a split seed shell (like half a walnut/chestnut shell).
    Concave interior (mat_idx=1), convex exterior (mat_idx=0), rounded bottom, scalloped cracked rim.
    Local space: cup bottom at (0, 0, 0), opens upward along +Z.
    """
    bm = bmesh.new()
    
    outer_rings = []
    inner_rings = []
    
    for i in range(1, u_segs + 1):
        u = i / float(u_segs)
        theta = (math.pi * 0.5) * u
        sin_t = math.sin(theta)
        cos_t = math.cos(theta)
        
        # Rim scalloping: wavy rim near the top opening
        rim_wave = 0.0035 * (u ** 2.2)
        
        out_row = []
        inn_row = []
        for j in range(v_segs):
            phi = 2.0 * math.pi * j / float(v_segs)
            
            # Scalloped variation along circumference
            wave = (math.sin(5.0 * phi) * 0.7 + math.cos(3.0 * phi) * 0.3) * rim_wave
            
            # Outer
            ox = rx * sin_t * math.cos(phi)
            oy = ry * sin_t * math.sin(phi)
            oz = rz * (1.0 - cos_t) + wave
            
            # Inner
            ix = max(0.002, rx - thickness) * sin_t * math.cos(phi)
            iy = max(0.002, ry - thickness) * sin_t * math.sin(phi)
            iz = oz + thickness * (0.85 + 0.15 * cos_t)
            
            out_row.append(bm.verts.new(Vector((ox, oy, oz))))
            inn_row.append(bm.verts.new(Vector((ix, iy, iz))))
            
        outer_rings.append(out_row)
        inner_rings.append(inn_row)
        
    # Bottom pole
    pole_out = bm.verts.new(Vector((0, 0, 0)))
    pole_inn = bm.verts.new(Vector((0, 0, thickness)))
    for j in range(v_segs):
        jn = (j + 1) % v_segs
        f_out = bm.faces.new([pole_out, outer_rings[0][j], outer_rings[0][jn]])
        f_out.material_index = 0 # Outer shell
        f_inn = bm.faces.new([pole_inn, inner_rings[0][jn], inner_rings[0][j]])
        f_inn.material_index = 1 # Inner cavity
        
    # Outer side quads
    for i in range(u_segs - 1):
        for j in range(v_segs):
            jn = (j + 1) % v_segs
            f = bm.faces.new([outer_rings[i][j], outer_rings[i+1][j], outer_rings[i+1][jn], outer_rings[i][jn]])
            f.material_index = 0 # Outer
            
    # Inner side quads
    for i in range(u_segs - 1):
        for j in range(v_segs):
            jn = (j + 1) % v_segs
            f = bm.faces.new([inner_rings[i][j], inner_rings[i+1][j], inner_rings[i+1][jn], inner_rings[i][jn]])
            f.material_index = 1 # Inner
            
    # Rim connecting outer and inner
    top_i = u_segs - 1
    for j in range(v_segs):
        jn = (j + 1) % v_segs
        f = bm.faces.new([outer_rings[top_i][j], outer_rings[top_i][jn], inner_rings[top_i][jn], inner_rings[top_i][j]])
        f.material_index = 0 # Rim
        
    return bm

def build_seed_shell():
    mesh = bpy.data.meshes.new("SeedShell")
    obj = bpy.data.objects.new("SeedShell", mesh)
    bpy.context.collection.objects.link(obj)
    
    master_bm = bmesh.new()
    
    # In reference photo:
    # Two open nut shells cradle the base of the stem right at the root collar z ~ 0.00m.
    # The shells hug the stem closely, and their hollow cavities are clearly visible from the front!
    
    # Left shell: hugging stem from left, tilted outward and forward to display hollow interior
    bm_shell_l = create_nut_shell_half_bmesh(rx=0.035, ry=0.028, rz=0.040, thickness=0.0046, u_segs=8, v_segs=16)
    rot_l = (Matrix.Rotation(math.radians(-22), 4, 'Z') @ 
             Matrix.Rotation(math.radians(52), 4, 'X') @ 
             Matrix.Rotation(math.radians(-35), 4, 'Y'))
    trans_l = Matrix.Translation(Vector((-0.016, 0.001, 0.000)))
    merge_bmesh_into(bm_shell_l, master_bm, trans_l @ rot_l)
    bm_shell_l.free()
    
    # Right shell: hugging stem from right, tilted outward and forward to display hollow interior
    bm_shell_r = create_nut_shell_half_bmesh(rx=0.035, ry=0.028, rz=0.040, thickness=0.0046, u_segs=8, v_segs=16)
    rot_r = (Matrix.Rotation(math.radians(22), 4, 'Z') @ 
             Matrix.Rotation(math.radians(52), 4, 'X') @ 
             Matrix.Rotation(math.radians(35), 4, 'Y'))
    trans_r = Matrix.Translation(Vector((0.016, -0.001, 0.000)))
    merge_bmesh_into(bm_shell_r, master_bm, trans_r @ rot_r)
    bm_shell_r.free()
    
    master_bm.to_mesh(mesh)
    master_bm.free()
    
    # Material Slot 0: Outer shell (rich warm chestnut brown)
    mat_shell_out = create_material("M_SeedShell_Outer", (0.50, 0.25, 0.10, 1.0), roughness=0.65)
    mesh.materials.append(mat_shell_out)
    
    # Material Slot 1: Inner shell cavity (warm lighter tan/golden wood)
    mat_shell_inn = create_material("M_SeedShell_Inner", (0.68, 0.42, 0.22, 1.0), roughness=0.58)
    mesh.materials.append(mat_shell_inn)
    
    for poly in mesh.polygons:
        poly.use_smooth = True
        
    return obj

def setup_camera_and_lights():
    cam_data = bpy.data.cameras.new("RenderCam")
    cam_data.lens = 72.0
    cam_obj = bpy.data.objects.new("RenderCam", cam_data)
    bpy.context.collection.objects.link(cam_obj)
    bpy.context.scene.camera = cam_obj
    
    # Key light: warm key from front-upper-left
    light_data = bpy.data.lights.new(name="KeyLight", type='SUN')
    light_data.energy = 4.0
    light_data.color = (1.0, 0.98, 0.95)
    light_obj = bpy.data.objects.new("KeyLight", light_data)
    bpy.context.collection.objects.link(light_obj)
    light_obj.rotation_euler = (math.radians(45), math.radians(20), math.radians(-30))
    
    # Fill light: soft warm-white fill from front-upper-right to illuminate both leaves evenly
    fill_data = bpy.data.lights.new(name="FillLight", type='SUN')
    fill_data.energy = 3.2
    fill_data.color = (0.95, 0.97, 1.0)
    fill_obj = bpy.data.objects.new("FillLight", fill_data)
    bpy.context.collection.objects.link(fill_obj)
    fill_obj.rotation_euler = (math.radians(45), math.radians(-25), math.radians(40))
    
    # Rim light from behind to highlight silhouette
    rim_data = bpy.data.lights.new(name="RimLight", type='SUN')
    rim_data.energy = 1.6
    rim_data.color = (1.0, 1.0, 1.0)
    rim_obj = bpy.data.objects.new("RimLight", rim_data)
    bpy.context.collection.objects.link(rim_obj)
    rim_obj.rotation_euler = (math.radians(-60), math.radians(10), math.radians(180))
    
    return cam_obj

def render_angles(cam_obj, output_dir):
    os.makedirs(output_dir, exist_ok=True)
    scene = bpy.context.scene
    scene.render.resolution_x = 1024
    scene.render.resolution_y = 1024
    scene.render.film_transparent = True
    scene.render.image_settings.file_format = 'PNG'
    
    target = Vector((0.0, 0.0, 0.030))
    dist = 0.92
    
    angles = [
        ("preview_front.png", 0.0, 4.0),
        ("preview_back.png", 180.0, 4.0),
        ("preview_perspective.png", 32.0, 14.0),
        ("preview_side_left.png", 90.0, 4.0),
    ]
    
    for name, yaw_deg, pitch_deg in angles:
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
    shell = build_seed_shell()
    
    all_objs = [roots, trunk, foliage, shell]
    
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
    print("\n--- SEEDLING MESH STATISTICS ---")
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
    print(f"TOTAL TRIANGLES: {total_tris} (Target: < 3,000)\n")
    
    # Save .blend
    blend_path = os.path.abspath(r"ArtSource\Models\Tree_Seedling_Simple_v01.blend")
    os.makedirs(os.path.dirname(blend_path), exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=blend_path)
    print(f"Saved blend file: {blend_path}")
    
    # Setup camera and lights for preview
    cam_obj = setup_camera_and_lights()
    evidence_dir = os.path.abspath(r"Evidence\Seedling_v01_Audit")
    render_angles(cam_obj, evidence_dir)
    
    # Remove camera and lights before export
    bpy.ops.object.select_all(action='DESELECT')
    cam_obj.select_set(True)
    for light in [o for o in bpy.data.objects if o.type == 'LIGHT']:
        light.select_set(True)
    bpy.ops.object.delete()
    
    # Select only the 4 mesh objects for export
    bpy.ops.object.select_all(action='DESELECT')
    for obj in all_objs:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = all_objs[0]
    
    fbx_path = os.path.abspath(r"ArtSource\Exports\Tree_Seedling_Simple_v01.fbx")
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
    print(f"Exported FBX: {fbx_path}")

if __name__ == "__main__":
    main()
