"""Preview script for P01:
- Computes exact linear colors from sRGB hex palette
- Loads each source file in memory without saving to disk
- Checks geometry fingerprints before & after material assignment
- Sets up neutral lighting & camera for each tree size
- Renders front, back, left, right, and perspective 3/4 views
- Outputs preview images and report to Evidence/Tree_Materials_P01_20261006/preview/
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
PREVIEW_DIR = EVIDENCE_DIR / "preview"

MODELS = {
    "Seedling": ROOT / "ArtSource/Models/Tree_Seedling_Simple_v01.blend",
    "Young": ROOT / "ArtSource/Models/Tree_Young_Simple_v01.blend",
    "Large": ROOT / "ArtSource/Models/Tree_Large.blend",
    "Ancient": ROOT / "ArtSource/Models/Bonsai_Trunk_Candidate_v01.blend",
}

def srgb_to_linear(hex_str):
    hex_str = hex_str.lstrip('#')
    r = int(hex_str[0:2], 16) / 255.0
    g = int(hex_str[2:4], 16) / 255.0
    b = int(hex_str[4:6], 16) / 255.0
    def to_lin(c):
        return c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4
    return [round(to_lin(r), 6), round(to_lin(g), 6), round(to_lin(b), 6), 1.0]

# Palette specifications per prompt
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
    # Note: we exclude material slot names from pure geometry fingerprint
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
    
    # Viewport display color
    mat.diffuse_color = lin_rgba
    mat.roughness = roughness
    mat.metallic = 0.0
    
    # Node tree setup
    if not mat.node_tree:
        mat.use_nodes = True
    nt = mat.node_tree
    
    # Find or create Principled BSDF
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
        
    # Configure sockets
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
    # Specular
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
    
    # Neutral color management
    scene.view_settings.view_transform = 'Standard'
    scene.view_settings.look = 'None'
    scene.view_settings.exposure = 0.0
    scene.view_settings.gamma = 1.0
    
    # World setup - soft neutral background
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
        
    # Remove existing lights/cameras
    for o in list(scene.objects):
        if o.type in ['LIGHT', 'CAMERA']:
            bpy.data.objects.remove(o, do_unlink=True)
            
    # Key light: Neutral white sun from front-right elevated
    key_data = bpy.data.lights.new('Key_Sun', type='SUN')
    key_data.color = (1.0, 1.0, 1.0)
    key_data.energy = 2.5
    key_obj = bpy.data.objects.new('Key_Sun', key_data)
    scene.collection.objects.link(key_obj)
    key_obj.rotation_euler = (math.radians(50), math.radians(15), math.radians(-35))
    
    # Fill light: Softer neutral white sun from opposite side
    fill_data = bpy.data.lights.new('Fill_Sun', type='SUN')
    fill_data.color = (1.0, 1.0, 1.0)
    fill_data.energy = 1.2
    fill_obj = bpy.data.objects.new('Fill_Sun', fill_data)
    scene.collection.objects.link(fill_obj)
    fill_obj.rotation_euler = (math.radians(35), math.radians(-25), math.radians(140))
    
    # Ground shadow plane / subtle base
    # (film_transparent is True, so background stays transparent for easy inspection)
    
    # Camera
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
    # Vertical dimension dominates for trees
    # Calculate distance to frame 78% of the frame
    max_dim = max(size.x, size.y, size.z)
    fov = 2.0 * math.atan((36.0 / 2.0) / cam_obj.data.lens)
    base_dist = (max_dim / 2.0) / math.tan(fov / 2.0) * 1.30
    
    angles = {
        'front': (0.0, 0.0, -base_dist, 0.0, 90.0, 0.0),
        'back': (0.0, 0.0, base_dist, 0.0, -90.0, 180.0),
        'left': (-base_dist, 0.0, 0.0, 0.0, 90.0, -90.0),
        'right': (base_dist, 0.0, 0.0, 0.0, 90.0, 90.0),
        'perspective': (None, None, None, None, None, None) # calculated via spherical
    }
    
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
            # 3/4 perspective: 40 deg azimuth, 22 deg elevation
            dist = base_dist * 1.12
            azim = math.radians(40)
            elev = math.radians(22)
            dx = -dist * math.cos(elev) * math.sin(azim)
            dy = -dist * math.cos(elev) * math.cos(azim)
            dz = dist * math.sin(elev)
            cam_obj.location = Vector((center.x + dx, center.y + dy, center.z + dz))
            
            # Look at center
            direction = center - cam_obj.location
            rot_quat = direction.to_track_quat('-Z', 'Y')
            cam_obj.rotation_euler = rot_quat.to_euler()
            
        scene.render.filepath = str(out_file)
        bpy.ops.render.render(write_still=True)
        rendered_files[view_name] = str(out_file)
        print(f"  Rendered {view_name} -> {out_file}")
        
    return rendered_files

def preview_model(label, blend_path):
    print(f"\n==========================================")
    print(f"Processing preview for {label}: {blend_path}")
    
    # 1. Open blend file in memory
    bpy.ops.wm.open_mainfile(filepath=str(blend_path), use_scripts=False)
    
    mesh_objects = [o for o in bpy.context.scene.objects if o.type == 'MESH']
    
    # 2. Record initial geometry fingerprints
    initial_fingerprints = {o.name: get_mesh_fingerprint(o) for o in mesh_objects}
    initial_triangles = {o.name: len(o.data.loop_triangles) if len(o.data.loop_triangles) > 0 else o.data.calc_loop_triangles() or len(o.data.loop_triangles) for o in mesh_objects}
    initial_vertices = {o.name: len(o.data.vertices) for o in mesh_objects}
    
    # 3. Apply material updates in memory
    cfg = PALETTE_CONFIG[label]
    created_materials = {}
    
    for mat_name, mat_cfg in cfg["materials"].items():
        mat = create_or_update_material(mat_name, mat_cfg["hex"], mat_cfg["roughness"])
        created_materials[mat_name] = {
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
            # Ensure slot exists
            while len(obj.material_slots) <= slot_idx:
                obj.data.materials.append(mat)
            obj.material_slots[slot_idx].material = mat
            # Assign faces if needed
            if len(obj.material_slots) == 1:
                for poly in obj.data.polygons:
                    poly.material_index = 0
                    
    # 4. Verify geometry fingerprints remain 100% identical
    post_fingerprints = {o.name: get_mesh_fingerprint(o) for o in mesh_objects}
    post_triangles = {o.name: len(o.data.loop_triangles) for o in mesh_objects}
    post_vertices = {o.name: len(o.data.vertices) for o in mesh_objects}
    
    for o in mesh_objects:
        assert initial_fingerprints[o.name] == post_fingerprints[o.name], f"Fingerprint mismatch on {o.name}!"
        assert initial_triangles[o.name] == post_triangles[o.name], f"Triangle mismatch on {o.name}!"
        assert initial_vertices[o.name] == post_vertices[o.name], f"Vertex mismatch on {o.name}!"
    print(f"Geometry preservation check PASSED: All {len(mesh_objects)} meshes 100% preserved.")
    
    # 5. Setup rendering
    cam_obj = setup_render_environment()
    center, size, bounds = compute_tree_bounds(mesh_objects)
    print(f"Tree bounds: center={center}, size={size}")
    
    # 6. Render views
    out_dir = PREVIEW_DIR / label.lower()
    renders = render_views(cam_obj, center, size, out_dir)
    
    return {
        "label": label,
        "blend_path": str(blend_path),
        "source_sha256": sha256_file(blend_path),
        "bounds": {
            "center": [round(x, 4) for x in center],
            "size": [round(x, 4) for x in size],
            "bbox": [round(x, 4) for x in bounds]
        },
        "total_vertices": sum(post_vertices.values()),
        "total_triangles": sum(post_triangles.values()),
        "mesh_stats": {
            o.name: {
                "vertices": post_vertices[o.name],
                "triangles": post_triangles[o.name],
                "fingerprint": post_fingerprints[o.name],
                "slots": [s.material.name if s.material else None for s in o.material_slots]
            }
            for o in mesh_objects
        },
        "materials": created_materials,
        "renders": renders,
    }

if __name__ == '__main__':
    PREVIEW_DIR.mkdir(parents=True, exist_ok=True)
    report = {}
    for label, blend_path in MODELS.items():
        report[label] = preview_model(label, blend_path)
        
    rep_file = PREVIEW_DIR / "preview_report.json"
    rep_file.write_text(json.dumps(report, indent=2), encoding='utf-8')
    print(f"\nPREVIEW COMPLETE. Report saved to {rep_file}")
