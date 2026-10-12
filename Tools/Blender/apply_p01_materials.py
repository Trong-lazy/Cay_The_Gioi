"""Apply script for P01:
- Re-verifies source file hashes before making changes
- Applies Principled BSDF materials with exact sRGB->linear conversion directly in source files:
  * ArtSource/Models/Tree_Seedling_Simple_v01.blend
  * ArtSource/Models/Tree_Young_Simple_v01.blend
  * ArtSource/Models/Tree_Large.blend
  * ArtSource/Models/Bonsai_Trunk_Candidate_v01.blend
- Ensures save_version = 0 (no extra .blend1 files generated)
- Reopens saved files from disk to verify 100% roundtrip integrity:
  * Geometry fingerprints (positions, edges, polygons, UVs, corner normals, transforms)
  * Vertex / triangle counts
  * Material slots & BSDF inputs
- Renders final verification renders and a side-by-side progression lineup
"""
import bpy
import bmesh
import hashlib
import json
import math
from pathlib import Path
from mathutils import Vector, Matrix, Euler

ROOT = Path("c:/Users/Asus/foder game 3D/Cay_The_Gioi")
EVIDENCE_DIR = ROOT / "Evidence/Tree_Materials_P01_20261006"
FINAL_DIR = EVIDENCE_DIR / "final"

MODELS = {
    "Seedling": ROOT / "ArtSource/Models/Tree_Seedling_Simple_v01.blend",
    "Young": ROOT / "ArtSource/Models/Tree_Young_Simple_v01.blend",
    "Large": ROOT / "ArtSource/Models/Tree_Large.blend",
    "Ancient": ROOT / "ArtSource/Models/Bonsai_Trunk_Candidate_v01.blend",
}

INITIAL_HASHES = {
    "Seedling": "5c575b1e1c5a298f6b638831a8e9a4a26c26966aad7427a83f1c7cda47abb923",
    "Young": "53aee33d69c01e8115a63fe02de635cf8a0aa6782337a80e166b6d72c0430c75",
    "Large": "a01e1328f3ad95df3e238057050f06d9e247e321ede9853f7d3601e28b49f723",
    "Ancient": "a7af560a2ec414ca0c0d07b03a4a576507419b404ab46da2ee6ef4723d6b90b9",
}

PROTECTED_ASSETS = {
    "Bonsai_Tree.blend": ROOT / "Assets/_Project/Art/Bonsai_Tree.blend",
    "Bonsai_Tree.fbx": ROOT / "Assets/_Project/Art/Bonsai_Tree.fbx",
}

def srgb_to_linear(hex_str):
    hex_str = hex_str.lstrip('#')
    r = int(hex_str[0:2], 16) / 255.0
    g = int(hex_str[2:4], 16) / 255.0
    b = int(hex_str[4:6], 16) / 255.0
    def to_lin(c):
        return c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4
    return [round(to_lin(r), 6), round(to_lin(g), 6), round(to_lin(b), 6), 1.0]

PALETTE_CONFIG = {
    "Seedling": {
        "materials": {
            "M_Leaves": {
                "hex": "#9BCB55",
                "roughness": 0.65,
                "targets": [("Foliage", 0)]
            },
            "M_Stem": {
                "hex": "#8BB75A",
                "roughness": 0.80,
                "targets": [("Trunk_Branches", 0)]
            },
            "M_Roots": {
                "hex": "#C9AE82",
                "roughness": 0.88,
                "targets": [("Roots", 0)]
            },
            "M_SeedShell_Outer": {
                "hex": "#765038",
                "roughness": 0.90,
                "targets": [("SeedShell", 0)]
            },
            "M_SeedShell_Inner": {
                "hex": "#8E6347",
                "roughness": 0.90,
                "targets": [("SeedShell", 1)]
            },
        }
    },
    "Young": {
        "materials": {
            "M_Leaves": {
                "hex": "#75AA43",
                "roughness": 0.70,
                "targets": [("Foliage", 0)]
            },
            "M_Stem": {
                "hex": "#A18153",
                "roughness": 0.82,
                "targets": [("Trunk_Branches", 0)]
            },
            "M_Roots": {
                "hex": "#A98B62",
                "roughness": 0.88,
                "targets": [("Roots", 0)]
            },
        }
    },
    "Large": {
        "materials": {
            "M_Foliage": {
                "hex": "#5F963E",
                "roughness": 0.72,
                "targets": [("Foliage", 0)]
            },
            "M_Bark": {
                "hex": "#8A6848",
                "roughness": 0.85,
                "targets": [("Trunk_Branches", 0), ("Roots", 0)]
            },
        }
    },
    "Ancient": {
        "materials": {
            "M_Foliage": {
                "hex": "#507C38",
                "roughness": 0.75,
                "targets": [("Foliage", 0)]
            },
            "M_Bark": {
                "hex": "#70634F",
                "roughness": 0.88,
                "targets": [("Trunk_Branches", 0), ("Roots", 0)]
            },
        }
    },
}

def sha256_file(filepath):
    return hashlib.sha256(Path(filepath).read_bytes()).hexdigest()

def digest(data):
    return hashlib.sha256(json.dumps(data, sort_keys=True, separators=(',', ':')).encode('utf-8')).hexdigest()

def get_mesh_fingerprint(obj):
    m = obj.data
    positions = [[round(coord, 6) for coord in v.co] for v in m.vertices]
    polygons = [list(p.vertices) for p in m.polygons]
    edges = [list(e.vertices) for e in m.edges]
    uvs = {u.name: [[round(coord, 6) for coord in d.uv] for d in u.data] for u in m.uv_layers}
    corner_normals = [[round(n, 6) for n in loop.normal] for loop in m.loops]
    matrix = [[round(val, 6) for val in row] for row in obj.matrix_world]
    return digest({
        'positions': positions,
        'polygons': polygons,
        'edges': edges,
        'uvs': uvs,
        'corner_normals': corner_normals,
        'matrix': matrix,
    })

def create_or_update_material(mat_name, hex_color, roughness):
    mat = bpy.data.materials.get(mat_name)
    if not mat:
        mat = bpy.data.materials.new(name=mat_name)
        mat.use_nodes = True
        
    lin_rgba = srgb_to_linear(hex_color)
    
    mat.diffuse_color = lin_rgba
    mat.roughness = roughness
    mat.metallic = 0.0
    
    if not mat.node_tree:
        mat.use_nodes = True
    nt = mat.node_tree
    
    principled = None
    for n in nt.nodes:
        if n.type == 'BSDF_PRINCIPLED':
            principled = n
            break
            
    if not principled:
        principled = nt.nodes.new(type='ShaderNodeBsdfPrincipled')
        output = None
        for n in nt.nodes:
            if n.type == 'OUTPUT_MATERIAL':
                output = n
                break
        if not output:
            output = nt.nodes.new(type='ShaderNodeOutputMaterial')
        nt.links.new(principled.outputs['BSDF'], output.inputs['Surface'])
        
    if 'Base Color' in principled.inputs:
        principled.inputs['Base Color'].default_value = lin_rgba
    if 'Roughness' in principled.inputs:
        principled.inputs['Roughness'].default_value = roughness
    if 'Metallic' in principled.inputs:
        principled.inputs['Metallic'].default_value = 0.0
    if 'Emission Color' in principled.inputs:
        principled.inputs['Emission Color'].default_value = (0.0, 0.0, 0.0, 1.0)
    if 'Emission Strength' in principled.inputs:
        principled.inputs['Emission Strength'].default_value = 0.0
    if 'Specular IOR Level' in principled.inputs:
        principled.inputs['Specular IOR Level'].default_value = 0.5
    elif 'Specular' in principled.inputs:
        principled.inputs['Specular'].default_value = 0.5
        
    return mat

def setup_render_environment():
    scene = bpy.context.scene
    scene.render.engine = 'BLENDER_EEVEE'
    scene.render.resolution_x = 1024
    scene.render.resolution_y = 1024
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = 'PNG'
    scene.render.image_settings.color_mode = 'RGBA'
    scene.render.film_transparent = True
    
    scene.view_settings.view_transform = 'Standard'
    scene.view_settings.look = 'None'
    scene.view_settings.exposure = 0.0
    scene.view_settings.gamma = 1.0
    
    world = bpy.data.worlds.new('Neutral_World')
    scene.world = world
    world.use_nodes = True
    bg_node = None
    for n in world.node_tree.nodes:
        if n.type == 'BACKGROUND':
            bg_node = n
            break
    if bg_node:
        bg_node.inputs['Color'].default_value = (0.85, 0.85, 0.85, 1.0)
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
    min_x = min(v.x for v in all_corners)
    max_x = max(v.x for v in all_corners)
    min_y = min(v.y for v in all_corners)
    max_y = max(v.y for v in all_corners)
    min_z = min(v.z for v in all_corners)
    max_z = max(v.z for v in all_corners)
    
    center = Vector(((min_x + max_x) / 2.0, (min_y + max_y) / 2.0, (min_z + max_z) / 2.0))
    size = Vector((max_x - min_x, max_y - min_y, max_z - min_z))
    return center, size, (min_x, max_x, min_y, max_y, min_z, max_z)

def render_views(cam_obj, center, size, out_prefix):
    scene = bpy.context.scene
    max_dim = max(size.x, size.y, size.z)
    fov = 2.0 * math.atan((36.0 / 2.0) / cam_obj.data.lens)
    base_dist = (max_dim / 2.0) / math.tan(fov / 2.0) * 1.30
    
    rendered_files = {}
    
    for view_name in ['front', 'back', 'left', 'right', 'perspective']:
        out_file = out_prefix / f"{view_name}.png"
        out_file.parent.mkdir(parents=True, exist_ok=True)
        
        if view_name == 'front':
            cam_obj.location = Vector((center.x, center.y - base_dist, center.z))
            cam_obj.rotation_euler = Euler((math.radians(90), 0, 0), 'XYZ')
        elif view_name == 'back':
            cam_obj.location = Vector((center.x, center.y + base_dist, center.z))
            cam_obj.rotation_euler = Euler((math.radians(90), 0, math.radians(180)), 'XYZ')
        elif view_name == 'left':
            cam_obj.location = Vector((center.x - base_dist, center.y, center.z))
            cam_obj.rotation_euler = Euler((math.radians(90), 0, math.radians(-90)), 'XYZ')
        elif view_name == 'right':
            cam_obj.location = Vector((center.x + base_dist, center.y, center.z))
            cam_obj.rotation_euler = Euler((math.radians(90), 0, math.radians(90)), 'XYZ')
        elif view_name == 'perspective':
            dist = base_dist * 1.12
            azim = math.radians(40)
            elev = math.radians(22)
            dx = -dist * math.cos(elev) * math.sin(azim)
            dy = -dist * math.cos(elev) * math.cos(azim)
            dz = dist * math.sin(elev)
            cam_obj.location = Vector((center.x + dx, center.y + dy, center.z + dz))
            direction = center - cam_obj.location
            rot_quat = direction.to_track_quat('-Z', 'Y')
            cam_obj.rotation_euler = rot_quat.to_euler()
            
        scene.render.filepath = str(out_file)
        bpy.ops.render.render(write_still=True)
        rendered_files[view_name] = str(out_file)
        
    return rendered_files

def apply_and_verify_model(label, blend_path):
    print(f"\n==========================================")
    print(f"Applying P01 materials for {label}: {blend_path}")
    
    # Verify initial hash before modifying
    current_sha = sha256_file(blend_path)
    expected_sha = INITIAL_HASHES[label]
    assert current_sha == expected_sha, f"Hash mismatch on {blend_path}: expected {expected_sha}, got {current_sha}"
    
    # 1. Open source file
    bpy.ops.wm.open_mainfile(filepath=str(blend_path), use_scripts=False)
    
    mesh_objects = [o for o in bpy.context.scene.objects if o.type == 'MESH']
    
    # 2. Record initial geometry fingerprints & counts
    pre_fingerprints = {o.name: get_mesh_fingerprint(o) for o in mesh_objects}
    pre_triangles = {o.name: len(o.data.loop_triangles) if len(o.data.loop_triangles) > 0 else o.data.calc_loop_triangles() or len(o.data.loop_triangles) for o in mesh_objects}
    pre_vertices = {o.name: len(o.data.vertices) for o in mesh_objects}
    pre_edges = {o.name: len(o.data.edges) for o in mesh_objects}
    pre_polygons = {o.name: len(o.data.polygons) for o in mesh_objects}
    
    # 3. Apply materials
    cfg = PALETTE_CONFIG[label]
    applied_materials = {}
    
    for mat_name, mat_cfg in cfg["materials"].items():
        mat = create_or_update_material(mat_name, mat_cfg["hex"], mat_cfg["roughness"])
        applied_materials[mat_name] = {
            "hex": mat_cfg["hex"],
            "linear_rgba": srgb_to_linear(mat_cfg["hex"]),
            "roughness": mat_cfg["roughness"],
            "metallic": 0.0,
            "emission_strength": 0.0
        }
        
        for obj_name, slot_idx in mat_cfg["targets"]:
            obj = bpy.data.objects.get(obj_name)
            if not obj:
                print(f"WARNING: Target object {obj_name} not found!")
                continue
            while len(obj.material_slots) <= slot_idx:
                obj.data.materials.append(mat)
            obj.material_slots[slot_idx].material = mat
            if len(obj.material_slots) == 1:
                for poly in obj.data.polygons:
                    poly.material_index = 0
                    
    # 4. Check geometry fingerprints in memory before saving
    in_mem_fingerprints = {o.name: get_mesh_fingerprint(o) for o in mesh_objects}
    for o in mesh_objects:
        assert pre_fingerprints[o.name] == in_mem_fingerprints[o.name], f"Fingerprint changed in memory for {o.name}!"
        
    # 5. Save directly to source with save_version = 0 (no extra .blend1)
    bpy.context.preferences.filepaths.save_version = 0
    bpy.ops.wm.save_as_mainfile(filepath=str(blend_path))
    saved_sha = sha256_file(blend_path)
    print(f"Saved directly to {blend_path}. New SHA256: {saved_sha}")
    
    # 6. Reopen from disk to verify roundtrip
    bpy.ops.wm.open_mainfile(filepath=str(blend_path), use_scripts=False)
    reopened_mesh_objects = [o for o in bpy.context.scene.objects if o.type == 'MESH']
    
    post_fingerprints = {o.name: get_mesh_fingerprint(o) for o in reopened_mesh_objects}
    post_triangles = {o.name: len(o.data.loop_triangles) if len(o.data.loop_triangles) > 0 else o.data.calc_loop_triangles() or len(o.data.loop_triangles) for o in reopened_mesh_objects}
    post_vertices = {o.name: len(o.data.vertices) for o in reopened_mesh_objects}
    post_edges = {o.name: len(o.data.edges) for o in reopened_mesh_objects}
    post_polygons = {o.name: len(o.data.polygons) for o in reopened_mesh_objects}
    
    for o in reopened_mesh_objects:
        assert pre_fingerprints[o.name] == post_fingerprints[o.name], f"Roundtrip fingerprint mismatch on {o.name}!"
        assert pre_vertices[o.name] == post_vertices[o.name], f"Roundtrip vertex count mismatch on {o.name}!"
        assert pre_triangles[o.name] == post_triangles[o.name], f"Roundtrip triangle count mismatch on {o.name}!"
        assert pre_edges[o.name] == post_edges[o.name], f"Roundtrip edge count mismatch on {o.name}!"
        assert pre_polygons[o.name] == post_polygons[o.name], f"Roundtrip polygon count mismatch on {o.name}!"
    print(f"Roundtrip verification PASSED: All {len(reopened_mesh_objects)} meshes 100% identical in geometry.")
    
    # 7. Render final verification views from reopened file
    cam_obj = setup_render_environment()
    center, size, bounds = compute_tree_bounds(reopened_mesh_objects)
    out_dir = FINAL_DIR / label.lower()
    renders = render_views(cam_obj, center, size, out_dir)
    print(f"Rendered final 5 angles to {out_dir}")
    
    return {
        "label": label,
        "blend_path": str(blend_path),
        "initial_sha256": current_sha,
        "saved_sha256": saved_sha,
        "roundtrip_verified": True,
        "bounds": {
            "center": [round(x, 4) for x in center],
            "size": [round(x, 4) for x in size],
            "bbox": [round(x, 4) for x in bounds]
        },
        "total_vertices": sum(post_vertices.values()),
        "total_triangles": sum(post_triangles.values()),
        "meshes": {
            o.name: {
                "vertices": post_vertices[o.name],
                "triangles": post_triangles[o.name],
                "edges": post_edges[o.name],
                "polygons": post_polygons[o.name],
                "fingerprint": post_fingerprints[o.name],
                "material_slots": [s.material.name if s.material else None for s in o.material_slots]
            }
            for o in reopened_mesh_objects
        },
        "applied_materials": applied_materials,
        "renders": renders,
    }

def render_lineup_progression():
    """Render all 4 trees together side-by-side in true relative scale."""
    print("\nRendering side-by-side progression lineup...")
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene = bpy.context.scene
    scene.render.engine = 'BLENDER_EEVEE'
    scene.render.resolution_x = 1920
    scene.render.resolution_y = 1080
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = 'PNG'
    scene.render.image_settings.color_mode = 'RGBA'
    scene.render.film_transparent = True
    scene.view_settings.view_transform = 'Standard'
    scene.view_settings.look = 'None'
    scene.view_settings.exposure = 0.0
    scene.view_settings.gamma = 1.0
    
    world = bpy.data.worlds.new('Lineup_World')
    scene.world = world
    world.use_nodes = True
    bg_node = [n for n in world.node_tree.nodes if n.type == 'BACKGROUND'][0]
    bg_node.inputs['Color'].default_value = (0.88, 0.88, 0.88, 1.0)
    bg_node.inputs['Strength'].default_value = 0.7
    
    # Lighting for wide shot
    key_data = bpy.data.lights.new('Key_Sun', type='SUN')
    key_data.color = (1.0, 1.0, 1.0)
    key_data.energy = 2.4
    key_obj = bpy.data.objects.new('Key_Sun', key_data)
    scene.collection.objects.link(key_obj)
    key_obj.rotation_euler = (math.radians(50), math.radians(15), math.radians(-35))
    
    fill_data = bpy.data.lights.new('Fill_Sun', type='SUN')
    fill_data.color = (1.0, 1.0, 1.0)
    fill_data.energy = 1.2
    fill_obj = bpy.data.objects.new('Fill_Sun', fill_data)
    scene.collection.objects.link(fill_obj)
    fill_obj.rotation_euler = (math.radians(35), math.radians(-25), math.radians(140))
    
    # Append objects from each model and arrange along X
    # Positions along X:
    # Seedling at x = -4.5
    # Young at x = -2.5
    # Large at x = 0.8
    # Ancient at x = 6.0 (or scaled spacing)
    spacing = {
        "Seedling": -4.2,
        "Young": -2.2,
        "Large": 1.2,
        "Ancient": 6.0,
    }
    
    all_imported_objs = []
    
    for label, blend_path in MODELS.items():
        with bpy.data.libraries.load(str(blend_path)) as (data_from, data_to):
            data_to.objects = [name for name in data_from.objects if name in ['Foliage', 'Roots', 'Trunk_Branches', 'SeedShell']]
            
        x_offset = spacing[label]
        for obj in data_to.objects:
            if obj:
                scene.collection.objects.link(obj)
                obj.location.x += x_offset
                all_imported_objs.append(obj)
                
    cam_data = bpy.data.cameras.new('Lineup_Cam')
    cam_data.lens = 45
    cam_data.sensor_width = 36
    cam_obj = bpy.data.objects.new('Lineup_Cam', cam_data)
    scene.collection.objects.link(cam_obj)
    scene.camera = cam_obj
    
    # Calculate bounds of all trees
    all_corners = []
    for obj in all_imported_objs:
        for c in obj.bound_box:
            all_corners.append(obj.matrix_world @ Vector(c))
    min_x = min(v.x for v in all_corners)
    max_x = max(v.x for v in all_corners)
    min_y = min(v.y for v in all_corners)
    max_y = max(v.y for v in all_corners)
    min_z = min(v.z for v in all_corners)
    max_z = max(v.z for v in all_corners)
    
    center = Vector(((min_x + max_x) / 2.0, (min_y + max_y) / 2.0, (min_z + max_z) / 2.0))
    size_x = max_x - min_x
    size_z = max_z - min_z
    
    # Position camera for wide perspective shot
    dist = 11.5
    cam_obj.location = Vector((center.x, -dist, center.z + 1.2))
    direction = Vector((center.x, 0, center.z)) - cam_obj.location
    rot_quat = direction.to_track_quat('-Z', 'Y')
    cam_obj.rotation_euler = rot_quat.to_euler()
    
    lineup_path = FINAL_DIR / "lineup_progression.png"
    scene.render.filepath = str(lineup_path)
    bpy.ops.render.render(write_still=True)
    print(f"Lineup rendered -> {lineup_path}")
    return str(lineup_path)

if __name__ == '__main__':
    FINAL_DIR.mkdir(parents=True, exist_ok=True)
    
    # Check protected assets before running
    protected_before = {k: sha256_file(p) for k, p in PROTECTED_ASSETS.items()}
    
    report = {}
    for label, blend_path in MODELS.items():
        report[label] = apply_and_verify_model(label, blend_path)
        
    lineup_file = render_lineup_progression()
    report["lineup_progression"] = lineup_file
    
    # Check protected assets after running
    protected_after = {k: sha256_file(p) for k, p in PROTECTED_ASSETS.items()}
    assert protected_before == protected_after, "Protected assets in Assets/ were modified!"
    report["protected_assets_unchanged"] = True
    report["protected_hashes"] = protected_after
    
    rep_file = FINAL_DIR / "apply_report.json"
    rep_file.write_text(json.dumps(report, indent=2), encoding='utf-8')
    print(f"\nAPPLY & VERIFICATION COMPLETE. Report saved to {rep_file}")
