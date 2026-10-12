"""Apply script for P02: Shared leaf venation textures.
- Checks source file hashes before modifying
- Saves shared leaf textures to ArtSource/Textures/Tree_Common/
- Connects Leaf_Common_BaseColor.png and Leaf_Common_Normal.png to leaf materials:
  * Seedling: M_Leaves (Strength=0.08, Tint factor=0.70, unwrap to UVMap)
  * Young: M_Leaves (Strength=0.14, Tint factor=0.85, unwrap to UVMap)
  * Large: M_Foliage (Strength=0.20, Tint factor=1.00, Float2 UV preserved)
  * Ancient: M_Foliage (Strength=0.22, Tint factor=1.00, UVMap UV preserved)
- Preserves 100% geometry (positions, edges, polygons, corner normals, transforms)
- Saves directly with save_version = 0 (no extra .blend1 files)
- Reopens from disk and verifies roundtrip integrity
- Renders final 5 views per tree + Large vs Ancient comparison
- Re-verifies Assets/ assets are untouched
"""
import bpy
import bmesh
import hashlib
import json
import math
import numpy as np
from pathlib import Path
from mathutils import Vector, Matrix, Euler

ROOT = Path("c:/Users/Asus/foder game 3D/Cay_The_Gioi")
EVIDENCE_DIR = ROOT / "Evidence/Tree_Materials_P02_20261006"
FINAL_DIR = EVIDENCE_DIR / "final"
TEXTURES_DIR = ROOT / "ArtSource/Textures/Tree_Common"

MODELS = {
    "Seedling": ROOT / "ArtSource/Models/Tree_Seedling_Simple_v01.blend",
    "Young": ROOT / "ArtSource/Models/Tree_Young_Simple_v01.blend",
    "Large": ROOT / "ArtSource/Models/Tree_Large.blend",
    "Ancient": ROOT / "ArtSource/Models/Bonsai_Trunk_Candidate_v01.blend",
}

INITIAL_HASHES_P02 = {
    "Seedling": "cdbdd09dcf57071eb4052fd286653cd671afc241722c2357e4a970bf3340b141",
    "Young": "2aa6df9dd3a81c0ebf78945a537e5bbc27d776a94db812c1895cb8cefaebe454",
    "Large": "7a8ac8ffabaa7b296f4207602f0a12b4951ef52ab037a915812283563fa50e3e",
    "Ancient": "16c043bbc936504a19e0c5d2f5a87204bc6a57e78dea87da5d512aad7f386219",
}

PROTECTED_ASSETS = {
    "Bonsai_Tree.blend": ROOT / "Assets/_Project/Art/Bonsai_Tree.blend",
    "Bonsai_Tree.fbx": ROOT / "Assets/_Project/Art/Bonsai_Tree.fbx",
}

BASECOLOR_TEX = TEXTURES_DIR / "Leaf_Common_BaseColor.png"
NORMAL_TEX = TEXTURES_DIR / "Leaf_Common_Normal.png"

CONFIG_P02 = {
    "Seedling": {
        "mat_name": "M_Leaves",
        "linear_tint": [0.3278, 0.5972, 0.0908, 1.0],
        "tint_factor": 0.70,
        "normal_strength": 0.08,
        "roughness": 0.65,
        "needs_unwrap": True,
        "uv_layer_name": "UVMap",
    },
    "Young": {
        "mat_name": "M_Leaves",
        "linear_tint": [0.1779, 0.4020, 0.0561, 1.0],
        "tint_factor": 0.85,
        "normal_strength": 0.14,
        "roughness": 0.70,
        "needs_unwrap": True,
        "uv_layer_name": "UVMap",
    },
    "Large": {
        "mat_name": "M_Foliage",
        "linear_tint": [0.1144, 0.3050, 0.0482, 1.0],
        "tint_factor": 1.0,
        "normal_strength": 0.20,
        "roughness": 0.72,
        "needs_unwrap": False,
        "uv_layer_name": "Float2",
    },
    "Ancient": {
        "mat_name": "M_Foliage",
        "linear_tint": [0.0802, 0.2016, 0.0395, 1.0],
        "tint_factor": 1.0,
        "normal_strength": 0.22,
        "roughness": 0.75,
        "needs_unwrap": False,
        "uv_layer_name": "UVMap",
    },
}

def sha256_file(filepath):
    return hashlib.sha256(Path(filepath).read_bytes()).hexdigest()

def digest(data):
    return hashlib.sha256(json.dumps(data, sort_keys=True, separators=(',', ':')).encode('utf-8')).hexdigest()

def get_pure_geometry_fingerprint(obj):
    m = obj.data
    positions = [[round(coord, 6) for coord in v.co] for v in m.vertices]
    polygons = [list(p.vertices) for p in m.polygons]
    edges = [list(e.vertices) for e in m.edges]
    corner_normals = [[round(n, 6) for n in loop.normal] for loop in m.loops]
    matrix = [[round(val, 6) for val in row] for row in obj.matrix_world]
    return digest({
        'positions': positions,
        'polygons': polygons,
        'edges': edges,
        'corner_normals': corner_normals,
        'matrix': matrix,
    })

def unwrap_foliage_mesh(mesh_obj, uv_name="UVMap"):
    mesh = mesh_obj.data
    bm = bmesh.new()
    bm.from_mesh(mesh)
    
    uv_layer = bm.loops.layers.uv.get(uv_name)
    if not uv_layer:
        uv_layer = bm.loops.layers.uv.new(uv_name)
        
    islands = []
    seen = set()
    for f in bm.faces:
        if f in seen: continue
        isl = []; stack = [f]; seen.add(f)
        while stack:
            c = stack.pop(); isl.append(c)
            for e in c.edges:
                for lf in e.link_faces:
                    if lf not in seen:
                        seen.add(lf); stack.append(lf)
        islands.append(isl)
        
    for isl in islands:
        verts = list(set(v for f in isl for v in f.verts))
        base_v = min(verts, key=lambda v: Vector((v.co.x, v.co.y)).length)
        tip_v = max(verts, key=lambda v: (v.co - base_v.co).length)
        
        vec_l = tip_v.co - base_v.co
        length = vec_l.length
        if length < 1e-5:
            continue
        dir_l = vec_l.normalized()
        
        avg_norm = sum((f.normal for f in isl), Vector()).normalized()
        dir_w = avg_norm.cross(dir_l).normalized()
        if dir_w.length < 1e-5:
            dir_w = Vector((0, 1, 0)).cross(dir_l).normalized()
            
        w_vals = [(v.co - base_v.co).dot(dir_w) for v in verts]
        max_w = max(abs(min(w_vals)), abs(max(w_vals)))
        if max_w < 1e-5:
            max_w = 0.01
            
        for f in isl:
            for loop in f.loops:
                v = loop.vert
                l_pos = (v.co - base_v.co).dot(dir_l)
                w_pos = (v.co - base_v.co).dot(dir_w)
                v_coord = np.clip(l_pos / length, 0.0, 1.0)
                u_coord = np.clip(0.5 + 0.5 * (w_pos / max_w), 0.0, 1.0)
                loop[uv_layer].uv = Vector((u_coord, v_coord))
                
    bm.to_mesh(mesh)
    bm.free()
    mesh.update()

def setup_leaf_shader(mat, cfg, blend_file_path):
    mat.use_nodes = True
    nt = mat.node_tree
    nt.nodes.clear()
    
    output = nt.nodes.new(type='ShaderNodeOutputMaterial')
    output.location = (600, 0)
    
    principled = nt.nodes.new(type='ShaderNodeBsdfPrincipled')
    principled.location = (300, 0)
    principled.inputs['Roughness'].default_value = cfg['roughness']
    principled.inputs['Metallic'].default_value = 0.0
    principled.inputs['Emission Color'].default_value = (0, 0, 0, 1)
    principled.inputs['Emission Strength'].default_value = 0.0
    
    if 'Specular IOR Level' in principled.inputs:
        principled.inputs['Specular IOR Level'].default_value = 0.5
    elif 'Specular' in principled.inputs:
        principled.inputs['Specular'].default_value = 0.5
        
    nt.links.new(principled.outputs['BSDF'], output.inputs['Surface'])
    
    # BaseColor Texture with relative path
    tex_bc = nt.nodes.new(type='ShaderNodeTexImage')
    tex_bc.location = (-350, 100)
    img_bc = bpy.data.images.load(str(BASECOLOR_TEX), check_existing=True)
    img_bc.colorspace_settings.name = 'sRGB'
    tex_bc.image = img_bc
    
    mix = nt.nodes.new(type='ShaderNodeMix')
    mix.location = (0, 100)
    mix.data_type = 'RGBA'
    mix.blend_type = 'MULTIPLY'
    mix.inputs['Factor'].default_value = cfg['tint_factor']
    mix.inputs[6].default_value = cfg['linear_tint'] # Input B
    
    nt.links.new(tex_bc.outputs['Color'], mix.inputs[7]) # Input A
    nt.links.new(mix.outputs['Result'], principled.inputs['Base Color'])
    
    # Normal Map Texture
    tex_nm = nt.nodes.new(type='ShaderNodeTexImage')
    tex_nm.location = (-350, -200)
    img_nm = bpy.data.images.load(str(NORMAL_TEX), check_existing=True)
    img_nm.colorspace_settings.name = 'Non-Color'
    tex_nm.image = img_nm
    
    nmap = nt.nodes.new(type='ShaderNodeNormalMap')
    nmap.location = (0, -200)
    nmap.space = 'TANGENT'
    nmap.uv_map = cfg['uv_layer_name']
    nmap.inputs['Strength'].default_value = cfg['normal_strength']
    
    nt.links.new(tex_nm.outputs['Color'], nmap.inputs['Color'])
    nt.links.new(nmap.outputs['Normal'], principled.inputs['Normal'])
    
    mat.diffuse_color = cfg['linear_tint']
    mat.roughness = cfg['roughness']
    mat.metallic = 0.0

def setup_render_environment():
    scene = bpy.context.scene
    scene.render.engine = 'BLENDER_EEVEE'
    scene.render.resolution_x = 1024
    scene.render.resolution_y = 1024
    scene.render.film_transparent = True
    scene.view_settings.view_transform = 'Standard'
    scene.view_settings.look = 'None'
    scene.view_settings.exposure = 0.0
    scene.view_settings.gamma = 1.0
    
    world = bpy.data.worlds.new('Neutral_World')
    scene.world = world
    world.use_nodes = True
    bg_node = [n for n in world.node_tree.nodes if n.type == 'BACKGROUND'][0]
    bg_node.inputs['Color'].default_value = (0.86, 0.86, 0.86, 1.0)
    bg_node.inputs['Strength'].default_value = 0.7
    
    for o in list(scene.objects):
        if o.type in ['LIGHT', 'CAMERA']:
            bpy.data.objects.remove(o, do_unlink=True)
            
    key_data = bpy.data.lights.new('Key_Sun', type='SUN')
    key_data.color = (1.0, 1.0, 1.0)
    key_data.energy = 2.5
    key_obj = bpy.data.objects.new('Key_Sun', key_data)
    scene.collection.objects.link(key_obj)
    key_obj.rotation_euler = (math.radians(50), math.radians(15), math.radians(-35))
    
    fill_data = bpy.data.lights.new('Fill_Sun', type='SUN')
    fill_data.color = (1.0, 1.0, 1.0)
    fill_data.energy = 1.2
    fill_obj = bpy.data.objects.new('Fill_Sun', fill_data)
    scene.collection.objects.link(fill_obj)
    fill_obj.rotation_euler = (math.radians(35), math.radians(-25), math.radians(140))
    
    cam_data = bpy.data.cameras.new('Render_Cam')
    cam_data.lens = 55
    cam_data.sensor_width = 36
    cam_obj = bpy.data.objects.new('Render_Cam', cam_data)
    scene.collection.objects.link(cam_obj)
    scene.camera = cam_obj
    
    return cam_obj

def compute_tree_bounds(mesh_objects):
    all_corners = []
    for obj in mesh_objects:
        for c in obj.bound_box:
            all_corners.append(obj.matrix_world @ Vector(c))
    min_x, max_x = min(v.x for v in all_corners), max(v.x for v in all_corners)
    min_y, max_y = min(v.y for v in all_corners), max(v.y for v in all_corners)
    min_z, max_z = min(v.z for v in all_corners), max(v.z for v in all_corners)
    center = Vector(((min_x + max_x) / 2.0, (min_y + max_y) / 2.0, (min_z + max_z) / 2.0))
    size = Vector((max_x - min_x, max_y - min_y, max_z - min_z))
    return center, size, (min_x, max_x, min_y, max_y, min_z, max_z)

def render_views_p02(cam_obj, center, size, foliage_obj, out_prefix):
    scene = bpy.context.scene
    max_dim = max(size.x, size.y, size.z)
    fov = 2.0 * math.atan((36.0 / 2.0) / cam_obj.data.lens)
    base_dist = (max_dim / 2.0) / math.tan(fov / 2.0) * 1.30
    
    rendered_files = {}
    
    view_configs = {
        'perspective': ('spherical', 40, 22, 1.12),
        'back': ('direct', 0, base_dist, 0, (math.radians(90), 0, math.radians(180))),
        'top': ('direct', 0, 0, base_dist * 0.95, (0, 0, 0)),
        'low': ('spherical', 25, -18, 1.05),
    }
    
    for vname, vcfg in view_configs.items():
        out_file = out_prefix / f"{vname}.png"
        out_file.parent.mkdir(parents=True, exist_ok=True)
        
        if vcfg[0] == 'direct':
            cam_obj.location = Vector((center.x + vcfg[1], center.y + vcfg[2], center.z + vcfg[3]))
            cam_obj.rotation_euler = Euler(vcfg[4], 'XYZ')
        elif vcfg[0] == 'spherical':
            azim = math.radians(vcfg[1])
            elev = math.radians(vcfg[2])
            dist = base_dist * vcfg[3]
            dx = -dist * math.cos(elev) * math.sin(azim)
            dy = -dist * math.cos(elev) * math.cos(azim)
            dz = dist * math.sin(elev)
            cam_obj.location = Vector((center.x + dx, center.y + dy, center.z + dz))
            direction = center - cam_obj.location
            rot_quat = direction.to_track_quat('-Z', 'Y')
            cam_obj.rotation_euler = rot_quat.to_euler()
            
        scene.render.filepath = str(out_file)
        bpy.ops.render.render(write_still=True)
        rendered_files[vname] = str(out_file)
        
    out_closeup = out_prefix / "closeup_foliage.png"
    fol_corners = [foliage_obj.matrix_world @ Vector(c) for c in foliage_obj.bound_box]
    fol_center = sum(fol_corners, Vector()) / len(fol_corners)
    focus_pt = Vector((fol_center.x * 0.5, fol_center.y - 0.2, fol_center.z + 0.1))
    cam_obj.data.lens = 85
    cam_dist = max(0.4, size.z * 0.45)
    cam_obj.location = Vector((focus_pt.x + cam_dist * 0.4, focus_pt.y - cam_dist, focus_pt.z + cam_dist * 0.35))
    direction = focus_pt - cam_obj.location
    rot_quat = direction.to_track_quat('-Z', 'Y')
    cam_obj.rotation_euler = rot_quat.to_euler()
    
    scene.render.filepath = str(out_closeup)
    bpy.ops.render.render(write_still=True)
    rendered_files['closeup_foliage'] = str(out_closeup)
    cam_obj.data.lens = 55
    
    return rendered_files

def apply_and_verify_model_p02(label, blend_path):
    print(f"\n==========================================")
    print(f"Applying P02 leaf venation for {label}: {blend_path}")
    
    # 1. Verify hash before modifying
    curr_sha = sha256_file(blend_path)
    expected_sha = INITIAL_HASHES_P02[label]
    assert curr_sha == expected_sha, f"Hash mismatch for {blend_path}: expected {expected_sha}, got {curr_sha}"
    
    # 2. Open file
    bpy.ops.wm.open_mainfile(filepath=str(blend_path), use_scripts=False)
    mesh_objects = [o for o in bpy.context.scene.objects if o.type == 'MESH']
    
    pre_fingerprints = {o.name: get_pure_geometry_fingerprint(o) for o in mesh_objects}
    pre_vertices = {o.name: len(o.data.vertices) for o in mesh_objects}
    pre_triangles = {o.name: len(o.data.loop_triangles) if len(o.data.loop_triangles) > 0 else o.data.calc_loop_triangles() or len(o.data.loop_triangles) for o in mesh_objects}
    pre_edges = {o.name: len(o.data.edges) for o in mesh_objects}
    pre_polygons = {o.name: len(o.data.polygons) for o in mesh_objects}
    
    cfg = CONFIG_P02[label]
    foliage = bpy.data.objects['Foliage']
    
    # 3. Unwrap if needed (Seedling / Young)
    if cfg['needs_unwrap']:
        unwrap_foliage_mesh(foliage, cfg['uv_layer_name'])
        
    # 4. Setup leaf shader with textures
    mat = foliage.material_slots[0].material
    setup_leaf_shader(mat, cfg, blend_path)
    
    # 5. Check pure geometry in memory before saving
    in_mem_fingerprints = {o.name: get_pure_geometry_fingerprint(o) for o in mesh_objects}
    for o in mesh_objects:
        assert pre_fingerprints[o.name] == in_mem_fingerprints[o.name], f"Geometry modified in memory for {o.name}!"
        
    # 6. Save directly with save_version = 0 (no extra .blend1)
    bpy.context.preferences.filepaths.save_version = 0
    bpy.ops.wm.save_as_mainfile(filepath=str(blend_path))
    saved_sha = sha256_file(blend_path)
    print(f"Saved directly to {blend_path}. New SHA256: {saved_sha}")
    
    # 7. Reopen from disk to verify roundtrip
    bpy.ops.wm.open_mainfile(filepath=str(blend_path), use_scripts=False)
    reopened_mesh_objects = [o for o in bpy.context.scene.objects if o.type == 'MESH']
    
    post_fingerprints = {o.name: get_pure_geometry_fingerprint(o) for o in reopened_mesh_objects}
    post_vertices = {o.name: len(o.data.vertices) for o in reopened_mesh_objects}
    post_triangles = {o.name: len(o.data.loop_triangles) if len(o.data.loop_triangles) > 0 else o.data.calc_loop_triangles() or len(o.data.loop_triangles) for o in reopened_mesh_objects}
    post_edges = {o.name: len(o.data.edges) for o in reopened_mesh_objects}
    post_polygons = {o.name: len(o.data.polygons) for o in reopened_mesh_objects}
    
    for o in reopened_mesh_objects:
        assert pre_fingerprints[o.name] == post_fingerprints[o.name], f"Roundtrip fingerprint mismatch on {o.name}!"
        assert pre_vertices[o.name] == post_vertices[o.name], f"Roundtrip vertex count mismatch on {o.name}!"
        assert pre_triangles[o.name] == post_triangles[o.name], f"Roundtrip triangle count mismatch on {o.name}!"
        assert pre_edges[o.name] == post_edges[o.name], f"Roundtrip edge count mismatch on {o.name}!"
        assert pre_polygons[o.name] == post_polygons[o.name], f"Roundtrip polygon count mismatch on {o.name}!"
    print(f"Roundtrip verification PASSED: All {len(reopened_mesh_objects)} meshes 100% identical in geometry.")
    
    # 8. Render final verification views
    cam_obj = setup_render_environment()
    center, size, bounds = compute_tree_bounds(reopened_mesh_objects)
    out_dir = FINAL_DIR / label.lower()
    foliage_reopened = bpy.data.objects['Foliage']
    renders = render_views_p02(cam_obj, center, size, foliage_reopened, out_dir)
    print(f"Rendered final views to {out_dir}")
    
    return {
        "label": label,
        "blend_path": str(blend_path),
        "initial_sha256": curr_sha,
        "saved_sha256": saved_sha,
        "total_vertices": sum(post_vertices.values()),
        "total_triangles": sum(post_triangles.values()),
        "normal_strength": cfg['normal_strength'],
        "tint_factor": cfg['tint_factor'],
        "renders": renders,
    }

def render_large_ancient_comparison_final():
    print("\nRendering final Large vs Ancient foliage comparison...")
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene = bpy.context.scene
    scene.render.engine = 'BLENDER_EEVEE'
    scene.render.resolution_x = 1920
    scene.render.resolution_y = 1080
    scene.render.film_transparent = True
    scene.view_settings.view_transform = 'Standard'
    
    world = bpy.data.worlds.new('Comp_World')
    scene.world = world
    world.use_nodes = True
    bg_node = [n for n in world.node_tree.nodes if n.type == 'BACKGROUND'][0]
    bg_node.inputs['Color'].default_value = (0.86, 0.86, 0.86, 1.0)
    bg_node.inputs['Strength'].default_value = 0.7
    
    key_data = bpy.data.lights.new('Key_Sun', type='SUN')
    key_data.energy = 2.5
    key_obj = bpy.data.objects.new('Key_Sun', key_data)
    scene.collection.objects.link(key_obj)
    key_obj.rotation_euler = (math.radians(50), math.radians(15), math.radians(-35))
    
    fill_data = bpy.data.lights.new('Fill_Sun', type='SUN')
    fill_data.energy = 1.2
    fill_obj = bpy.data.objects.new('Fill_Sun', fill_data)
    scene.collection.objects.link(fill_obj)
    fill_obj.rotation_euler = (math.radians(35), math.radians(-25), math.radians(140))
    
    spacing = {"Large": -3.2, "Ancient": 3.2}
    all_imported = []
    
    for label in ["Large", "Ancient"]:
        p = MODELS[label]
        with bpy.data.libraries.load(str(p)) as (df, dt):
            dt.objects = [n for n in df.objects if n in ['Foliage', 'Roots', 'Trunk_Branches']]
        for o in dt.objects:
            if o:
                scene.collection.objects.link(o)
                o.name = f"{label}_{o.name}"
                o.location.x += spacing[label]
                all_imported.append(o)
                
    bpy.context.view_layer.update()
    
    corners = [o.matrix_world @ Vector(c) for o in all_imported for c in o.bound_box]
    min_x, max_x = min(v.x for v in corners), max(v.x for v in corners)
    min_y, max_y = min(v.y for v in corners), max(v.y for v in corners)
    min_z, max_z = min(v.z for v in corners), max(v.z for v in corners)
    
    center = Vector(((min_x + max_x) / 2.0, (min_y + max_y) / 2.0, (min_z + max_z) / 2.0))
    size_x = max_x - min_x
    
    cam_data = bpy.data.cameras.new('Comp_Cam')
    cam_data.lens = 40
    cam_obj = bpy.data.objects.new('Comp_Cam', cam_data)
    scene.collection.objects.link(cam_obj)
    scene.camera = cam_obj
    
    fov_x = 2.0 * math.atan((36.0 / 2.0) / cam_data.lens)
    dist = (size_x / 2.0) / math.tan(fov_x / 2.0) * 1.15
    cam_obj.location = Vector((center.x, -dist, center.z + 1.2))
    direction = Vector((center.x, 0, center.z)) - cam_obj.location
    rot_quat = direction.to_track_quat('-Z', 'Y')
    cam_obj.rotation_euler = rot_quat.to_euler()
    
    out_comp = FINAL_DIR / "large_ancient_comparison.png"
    scene.render.filepath = str(out_comp)
    bpy.ops.render.render(write_still=True)
    print(f"Saved final comparison to {out_comp}")
    return str(out_comp)

if __name__ == '__main__':
    FINAL_DIR.mkdir(parents=True, exist_ok=True)
    
    protected_before = {k: sha256_file(p) for k, p in PROTECTED_ASSETS.items()}
    
    report = {}
    for label, blend_path in MODELS.items():
        report[label] = apply_and_verify_model_p02(label, blend_path)
        
    comp_file = render_large_ancient_comparison_final()
    report["large_ancient_comparison"] = comp_file
    
    protected_after = {k: sha256_file(p) for k, p in PROTECTED_ASSETS.items()}
    assert protected_before == protected_after, "Protected assets in Assets/ were modified!"
    report["protected_assets_unchanged"] = True
    report["protected_hashes"] = protected_after
    
    rep_file = FINAL_DIR / "apply_p02_report.json"
    rep_file.write_text(json.dumps(report, indent=2), encoding='utf-8')
    print(f"\nP02 APPLY & VERIFICATION COMPLETE. Report saved to {rep_file}")
