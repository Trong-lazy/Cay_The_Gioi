"""Apply P04 ancient bark material and texture linkage directly to Bonsai_Trunk_Candidate_v01.blend.

Performs:
1. Audit and hash recording before modification.
2. Setup M_Bark material with Ancient_Bark_BaseColor.png and Ancient_Bark_Normal.png.
3. Save directly with save_version = 0 (no .blend1).
4. Independent reopen and full geometry roundtrip verification.
5. Render verification views for Ancient tree (perspective, front, back, left, right, closeup_fork, closeup_roots).
6. Render side-by-side comparison: Large vs Ancient (large_ancient_bark_comparison.png).
7. Report generation in Evidence/Tree_Materials_P04_20261006/final/apply_p04_report.json.
"""

import os
import math
import hashlib
import json
from pathlib import Path
import bpy
from mathutils import Vector, Euler

ROOT = Path(r"c:\Users\Asus\foder game 3D\Cay_The_Gioi")
BLEND_ANCIENT = ROOT / "ArtSource/Models/Bonsai_Trunk_Candidate_v01.blend"
BLEND_LARGE = ROOT / "ArtSource/Models/Tree_Large.blend"
TEXTURES_DIR = ROOT / "ArtSource/Textures/Tree_Common"
EVIDENCE_DIR = ROOT / "Evidence/Tree_Materials_P04_20261006/final"
EVIDENCE_DIR.mkdir(parents=True, exist_ok=True)

PROTECTED_FILES = {
    "Seedling": ROOT / "ArtSource/Models/Tree_Seedling_Simple_v01.blend",
    "Young": ROOT / "ArtSource/Models/Tree_Young_Simple_v01.blend",
    "Large": ROOT / "ArtSource/Models/Tree_Large.blend",
    "Bonsai_Tree.blend": ROOT / "Assets/_Project/Art/Bonsai_Tree.blend",
    "Bonsai_Tree.fbx": ROOT / "Assets/_Project/Art/Bonsai_Tree.fbx",
}

def sha256_file(path):
    h = hashlib.sha256()
    with open(path, "rb") as f:
        while chunk := f.read(65536):
            h.update(chunk)
    return h.hexdigest()

def get_pure_geometry_fingerprint(obj):
    me = obj.data
    me.calc_loop_triangles()
    v_coords = []
    for v in me.vertices:
        v_coords.extend([round(v.co.x, 5), round(v.co.y, 5), round(v.co.z, 5)])
    tri_indices = []
    for tri in me.loop_triangles:
        tri_indices.extend(sorted([tri.vertices[0], tri.vertices[1], tri.vertices[2]]))
    data_str = f"{len(me.vertices)}:{len(me.loop_triangles)}:{len(me.polygons)}:{len(me.edges)}|{v_coords}|{tri_indices}"
    return hashlib.sha256(data_str.encode('utf-8')).hexdigest()

def setup_render_environment():
    scene = bpy.context.scene
    scene.render.engine = 'BLENDER_EEVEE_NEXT' if hasattr(bpy.types, 'RenderSettings') and 'BLENDER_EEVEE_NEXT' in [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties['engine'].enum_items] else 'BLENDER_EEVEE'
    scene.render.resolution_x = 1024
    scene.render.resolution_y = 1024
    scene.render.film_transparent = True
    scene.view_settings.view_transform = 'Standard'
    scene.view_settings.look = 'None'
    scene.view_settings.exposure = 0.0
    scene.view_settings.gamma = 1.0
    
    world = bpy.data.worlds.new('Neutral_World_P04')
    scene.world = world
    world.use_nodes = True
    bg_node = [n for n in world.node_tree.nodes if n.type == 'BACKGROUND'][0]
    bg_node.inputs['Color'].default_value = (0.86, 0.86, 0.86, 1.0)
    bg_node.inputs['Strength'].default_value = 0.7
    
    for o in list(scene.objects):
        if o.type in ['LIGHT', 'CAMERA']:
            bpy.data.objects.remove(o, do_unlink=True)
            
    # Key Sun
    key_data = bpy.data.lights.new('Key_Sun', type='SUN')
    key_data.color = (1.0, 1.0, 1.0)
    key_data.energy = 2.5
    key_obj = bpy.data.objects.new('Key_Sun', key_data)
    scene.collection.objects.link(key_obj)
    key_obj.rotation_euler = (math.radians(50), math.radians(15), math.radians(-35))
    
    # Fill Sun
    fill_data = bpy.data.lights.new('Fill_Sun', type='SUN')
    fill_data.color = (1.0, 1.0, 1.0)
    fill_data.energy = 1.2
    fill_obj = bpy.data.objects.new('Fill_Sun', fill_data)
    scene.collection.objects.link(fill_obj)
    fill_obj.rotation_euler = (math.radians(35), math.radians(-25), math.radians(140))
    
    # Camera
    cam_data = bpy.data.cameras.new('Render_Cam')
    cam_data.lens = 55
    cam_data.sensor_width = 36
    cam_obj = bpy.data.objects.new('Render_Cam', cam_data)
    scene.collection.objects.link(cam_obj)
    scene.camera = cam_obj
    
    return cam_obj, key_obj

def run_p04():
    print("=== STARTING P04 IMPLEMENTATION FOR ANCIENT TREE ===")
    
    # 1. Record pre-hashes
    initial_ancient_sha = sha256_file(BLEND_ANCIENT)
    protected_pre_hashes = {k: sha256_file(v) for k, v in PROTECTED_FILES.items()}
    print(f"Ancient tree initial SHA256: {initial_ancient_sha}")
    
    # 2. Open Bonsai_Trunk_Candidate_v01.blend
    bpy.ops.wm.open_mainfile(filepath=str(BLEND_ANCIENT), use_scripts=False)
    mesh_objects = [o for o in bpy.context.scene.objects if o.type == 'MESH']
    
    pre_fingerprints = {o.name: get_pure_geometry_fingerprint(o) for o in mesh_objects}
    pre_vertices = {o.name: len(o.data.vertices) for o in mesh_objects}
    pre_triangles = {o.name: len(o.data.loop_triangles) if len(o.data.loop_triangles) > 0 else o.data.calc_loop_triangles() or len(o.data.loop_triangles) for o in mesh_objects}
    pre_edges = {o.name: len(o.data.edges) for o in mesh_objects}
    pre_polygons = {o.name: len(o.data.polygons) for o in mesh_objects}
    
    total_pre_verts = sum(pre_vertices.values())
    total_pre_tris = sum(pre_triangles.values())
    assert total_pre_verts == 51332, f"Expected 51332 verts, got {total_pre_verts}"
    assert total_pre_tris == 73888, f"Expected 73888 tris, got {total_pre_tris}"
    print(f"Verified pre-geometry: {total_pre_verts} verts, {total_pre_tris} tris.")
    
    # 3. Setup M_Bark material with Ancient Bark textures
    mat_bark = bpy.data.materials.get('M_Bark')
    if not mat_bark:
        mat_bark = bpy.data.materials.new('M_Bark')
    mat_bark.use_nodes = True
    
    nodes = mat_bark.node_tree.nodes
    links = mat_bark.node_tree.links
    nodes.clear()
    
    node_out = nodes.new('ShaderNodeOutputMaterial')
    node_out.location = (600, 0)
    
    node_bsdf = nodes.new('ShaderNodeBsdfPrincipled')
    node_bsdf.location = (300, 0)
    node_bsdf.inputs['Roughness'].default_value = 0.88
    node_bsdf.inputs['Metallic'].default_value = 0.0
    if 'Emission Color' in node_bsdf.inputs:
        node_bsdf.inputs['Emission Color'].default_value = (0, 0, 0, 1)
        
    node_coord = nodes.new('ShaderNodeTexCoord')
    node_coord.location = (-600, 0)
    
    node_map = nodes.new('ShaderNodeMapping')
    node_map.location = (-400, 0)
    node_map.inputs['Scale'].default_value = (2.5, 2.5, 1.0)
    links.new(node_coord.outputs['UV'], node_map.inputs['Vector'])
    
    # BaseColor image
    bc_path = str(TEXTURES_DIR / "Ancient_Bark_BaseColor.png")
    node_tex_bc = nodes.new('ShaderNodeTexImage')
    node_tex_bc.location = (-150, 150)
    img_bc = bpy.data.images.load(bc_path, check_existing=True)
    node_tex_bc.image = img_bc
    links.new(node_map.outputs['Vector'], node_tex_bc.inputs['Vector'])
    links.new(node_tex_bc.outputs['Color'], node_bsdf.inputs['Base Color'])
    
    # NormalMap image
    norm_path = str(TEXTURES_DIR / "Ancient_Bark_Normal.png")
    node_tex_norm = nodes.new('ShaderNodeTexImage')
    node_tex_norm.location = (-150, -150)
    img_norm = bpy.data.images.load(norm_path, check_existing=True)
    img_norm.colorspace_settings.name = 'Non-Color'
    node_tex_norm.image = img_norm
    links.new(node_map.outputs['Vector'], node_tex_norm.inputs['Vector'])
    
    node_norm_map = nodes.new('ShaderNodeNormalMap')
    node_norm_map.location = (100, -150)
    node_norm_map.space = 'TANGENT'
    node_norm_map.inputs['Strength'].default_value = 1.05
    links.new(node_tex_norm.outputs['Color'], node_norm_map.inputs['Color'])
    links.new(node_norm_map.outputs['Normal'], node_bsdf.inputs['Normal'])
    
    links.new(node_bsdf.outputs['BSDF'], node_out.inputs['Surface'])
    print("M_Bark shader network configured for Ancient tree.")
    
    # 4. Check geometry in memory before saving
    in_mem_fingerprints = {o.name: get_pure_geometry_fingerprint(o) for o in mesh_objects}
    for o in mesh_objects:
        assert pre_fingerprints[o.name] == in_mem_fingerprints[o.name], f"Geometry modified in memory for {o.name}!"
    print("Pre-save geometry check PASSED: 0 vertices or faces changed.")
    
    # 5. Save directly with save_version = 0
    bpy.context.preferences.filepaths.save_version = 0
    bpy.ops.wm.save_as_mainfile(filepath=str(BLEND_ANCIENT))
    saved_ancient_sha = sha256_file(BLEND_ANCIENT)
    print(f"Saved directly to {BLEND_ANCIENT}. New SHA256: {saved_ancient_sha}")
    
    # 6. Independent reopen and verify from disk
    bpy.ops.wm.open_mainfile(filepath=str(BLEND_ANCIENT), use_scripts=False)
    reopened_mesh_objects = [o for o in bpy.context.scene.objects if o.type == 'MESH']
    
    post_fingerprints = {o.name: get_pure_geometry_fingerprint(o) for o in reopened_mesh_objects}
    post_vertices = {o.name: len(o.data.vertices) for o in reopened_mesh_objects}
    post_triangles = {o.name: len(o.data.loop_triangles) if len(o.data.loop_triangles) > 0 else o.data.calc_loop_triangles() or len(o.data.loop_triangles) for o in reopened_mesh_objects}
    post_edges = {o.name: len(o.data.edges) for o in reopened_mesh_objects}
    post_polygons = {o.name: len(o.data.polygons) for o in reopened_mesh_objects}
    
    for o in reopened_mesh_objects:
        assert pre_fingerprints[o.name] == post_fingerprints[o.name], f"Fingerprint mismatch on {o.name}!"
        assert pre_vertices[o.name] == post_vertices[o.name], f"Vertex count mismatch on {o.name}!"
        assert pre_triangles[o.name] == post_triangles[o.name], f"Triangle count mismatch on {o.name}!"
        assert pre_edges[o.name] == post_edges[o.name], f"Edge count mismatch on {o.name}!"
        assert pre_polygons[o.name] == post_polygons[o.name], f"Polygon count mismatch on {o.name}!"
        
    total_post_verts = sum(post_vertices.values())
    total_post_tris = sum(post_triangles.values())
    print(f"Roundtrip verification PASSED: {total_post_verts} verts, {total_post_tris} tris (100% matched baseline).")
    
    # 7. Check protected assets unchanged
    protected_post_hashes = {k: sha256_file(v) for k, v in PROTECTED_FILES.items()}
    for k in PROTECTED_FILES:
        assert protected_pre_hashes[k] == protected_post_hashes[k], f"Protected file {k} modified!"
    print("Protected assets verification PASSED: 100% untouched.")
    
    # 8. Render final verification views for Ancient tree
    cam_obj, key_sun = setup_render_environment()
    scene = bpy.context.scene
    
    all_corners = []
    for obj in reopened_mesh_objects:
        for c in obj.bound_box:
            all_corners.append(obj.matrix_world @ Vector(c))
    min_x, max_x = min(v.x for v in all_corners), max(v.x for v in all_corners)
    min_y, max_y = min(v.y for v in all_corners), max(v.y for v in all_corners)
    min_z, max_z = min(v.z for v in all_corners), max(v.z for v in all_corners)
    center = Vector(((min_x + max_x) / 2.0, (min_y + max_y) / 2.0, (min_z + max_z) / 2.0))
    size = Vector((max_x - min_x, max_y - min_y, max_z - min_z))
    
    max_dim = max(size.x, size.y, size.z)
    fov = 2.0 * math.atan((36.0 / 2.0) / cam_obj.data.lens)
    base_dist = (max_dim / 2.0) / math.tan(fov / 2.0) * 1.30
    
    rendered_files = {}
    
    views = {
        'perspective': ('spherical', 40, 22, 1.12),
        'front': ('direct', 0, -base_dist, 0, (math.radians(90), 0, 0)),
        'back': ('direct', 0, base_dist, 0, (math.radians(90), 0, math.radians(180))),
        'left': ('direct', -base_dist, 0, 0, (math.radians(90), 0, math.radians(-90))),
        'right': ('direct', base_dist, 0, 0, (math.radians(90), 0, math.radians(90))),
    }
    
    for vname, vcfg in views.items():
        out_file = EVIDENCE_DIR / f"{vname}.png"
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
        print(f"Rendered {vname} -> {out_file}")
        
    # Closeup of trunk & branch fork
    out_closeup_fork = EVIDENCE_DIR / "closeup_fork.png"
    cam_obj.data.lens = 70
    focus_trunk = Vector((0.0, 0.0, 1.1))
    cam_obj.location = Vector((1.8, -3.2, 1.6))
    direction = focus_trunk - cam_obj.location
    rot_quat = direction.to_track_quat('-Z', 'Y')
    cam_obj.rotation_euler = rot_quat.to_euler()
    scene.render.filepath = str(out_closeup_fork)
    bpy.ops.render.render(write_still=True)
    rendered_files['closeup_fork'] = str(out_closeup_fork)
    print(f"Rendered closeup_fork -> {out_closeup_fork}")
    
    # Closeup of roots base
    out_closeup_roots = EVIDENCE_DIR / "closeup_roots.png"
    cam_obj.data.lens = 65
    focus_roots = Vector((0.0, 0.0, 0.15))
    cam_obj.location = Vector((2.2, -3.8, 0.6))
    direction = focus_roots - cam_obj.location
    rot_quat = direction.to_track_quat('-Z', 'Y')
    cam_obj.rotation_euler = rot_quat.to_euler()
    scene.render.filepath = str(out_closeup_roots)
    bpy.ops.render.render(write_still=True)
    rendered_files['closeup_roots'] = str(out_closeup_roots)
    print(f"Rendered closeup_roots -> {out_closeup_roots}")
    
    # 9. Render side-by-side comparison: Large vs Ancient
    print("Rendering side-by-side comparison: Large vs Ancient...")
    # Import Tree_Large into scene offset to the left
    with bpy.data.libraries.load(str(BLEND_LARGE)) as (data_from, data_to):
        data_to.objects = [name for name in data_from.objects if name in ['Foliage', 'Roots', 'Trunk_Branches']]
        
    large_objs = []
    for obj in data_to.objects:
        scene.collection.objects.link(obj)
        large_objs.append(obj)
        obj.location.x -= 4.2  # place to the left of Ancient tree
        
    # Center camera between Large and Ancient
    comparison_center = Vector((-2.1, 0.0, 1.2))
    cam_obj.data.lens = 45
    cam_dist = 11.5
    cam_obj.location = Vector((-2.1, -cam_dist, 2.8))
    direction = comparison_center - cam_obj.location
    rot_quat = direction.to_track_quat('-Z', 'Y')
    cam_obj.rotation_euler = rot_quat.to_euler()
    
    scene.render.resolution_x = 1920
    scene.render.resolution_y = 1080
    out_comp = EVIDENCE_DIR / "large_ancient_bark_comparison.png"
    scene.render.filepath = str(out_comp)
    bpy.ops.render.render(write_still=True)
    rendered_files['large_ancient_bark_comparison'] = str(out_comp)
    print(f"Rendered large_ancient_bark_comparison -> {out_comp}")
    
    # 10. Save JSON report
    report = {
        "task": "T10",
        "prompt": "P04",
        "target_model": str(BLEND_ANCIENT),
        "initial_sha256": initial_ancient_sha,
        "saved_sha256": saved_ancient_sha,
        "total_vertices": total_post_verts,
        "total_triangles": total_post_tris,
        "total_edges": sum(post_edges.values()),
        "total_polygons": sum(post_polygons.values()),
        "mesh_stats": {
            o.name: {
                "vertices": post_vertices[o.name],
                "triangles": post_triangles[o.name],
                "polygons": post_polygons[o.name],
                "edges": post_edges[o.name],
                "uv_layer": o.data.uv_layers.active.name,
                "material_slot": o.material_slots[0].name if o.material_slots else None
            } for o in reopened_mesh_objects
        },
        "material_settings": {
            "name": "M_Bark",
            "base_color_map": str(TEXTURES_DIR / "Ancient_Bark_BaseColor.png"),
            "normal_map": str(TEXTURES_DIR / "Ancient_Bark_Normal.png"),
            "normal_convention": "OpenGL (+Y up, Tangent Space)",
            "normal_strength": 1.05,
            "roughness": 0.88,
            "metallic": 0.0,
            "emission": 0.0,
            "tiling_scale": [2.5, 2.5, 1.0]
        },
        "renders": rendered_files,
        "protected_assets_verified": True,
        "protected_hashes": protected_post_hashes
    }
    
    report_path = EVIDENCE_DIR / "apply_p04_report.json"
    with open(report_path, "w", encoding="utf-8") as f:
        json.dump(report, f, indent=2)
    print(f"Saved report to {report_path}")
    print("=== P04 EXECUTION COMPLETED SUCCESSFULLY ===")

if __name__ == '__main__':
    run_p04()
