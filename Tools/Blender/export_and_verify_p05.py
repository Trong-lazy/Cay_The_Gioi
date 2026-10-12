"""P05 — Finalize textures, export FBXs, independent reimport verification, and render evidence.

Models:
1. Seedling: ArtSource/Models/Tree_Seedling_Simple_v01.blend -> ArtSource/Exports/Tree_Seedling_Simple_v01.fbx
2. Young: ArtSource/Models/Tree_Young_Simple_v01.blend -> ArtSource/Exports/Tree_Young_Simple_v01.fbx
3. Large: ArtSource/Models/Tree_Large.blend -> ArtSource/Exports/Tree_Large.fbx
4. Ancient: ArtSource/Models/Bonsai_Trunk_Candidate_v01.blend -> ArtSource/Exports/Bonsai_Trunk_Candidate_v01.fbx

Protected assets:
- Assets/_Project/Art/Bonsai_Tree.blend
- Assets/_Project/Art/Bonsai_Tree.fbx
"""

import os
import math
import hashlib
import json
from pathlib import Path
import bpy
from mathutils import Vector, Euler

ROOT = Path(r"c:\Users\Asus\foder game 3D\Cay_The_Gioi")
MODELS_DIR = ROOT / "ArtSource/Models"
EXPORTS_DIR = ROOT / "ArtSource/Exports"
TEXTURES_DIR = ROOT / "ArtSource/Textures/Tree_Common"
EVIDENCE_DIR = ROOT / "Evidence/Tree_Materials_P05_20261006/final"
EVIDENCE_DIR.mkdir(parents=True, exist_ok=True)

MODELS = {
    "Seedling": {
        "blend": MODELS_DIR / "Tree_Seedling_Simple_v01.blend",
        "fbx": EXPORTS_DIR / "Tree_Seedling_Simple_v01.fbx",
        "target_tris": 2778,
        "target_verts": 1436,
    },
    "Young": {
        "blend": MODELS_DIR / "Tree_Young_Simple_v01.blend",
        "fbx": EXPORTS_DIR / "Tree_Young_Simple_v01.fbx",
        "target_tris": 4574,
        "target_verts": 2420,
    },
    "Large": {
        "blend": MODELS_DIR / "Tree_Large.blend",
        "fbx": EXPORTS_DIR / "Tree_Large.fbx",
        "target_tris": 26682,
        "target_verts": 16632,
    },
    "Ancient": {
        "blend": MODELS_DIR / "Bonsai_Trunk_Candidate_v01.blend",
        "fbx": EXPORTS_DIR / "Bonsai_Trunk_Candidate_v01.fbx",
        "target_tris": 73888,
        "target_verts": 51332,
    }
}

PROTECTED_FILES = {
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
    
    world = bpy.data.worlds.new('Neutral_World_P05')
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

def run_p05():
    print("=== STARTING P05 EXECUTION ===")
    
    # 1. Record pre-hashes of protected assets
    protected_pre_hashes = {k: sha256_file(v) for k, v in PROTECTED_FILES.items()}
    
    model_reports = {}
    
    # 2. Process each model: Make relative, export FBX, reimport verify, render views
    for label, mcfg in MODELS.items():
        print(f"\n--- Processing {label} ({mcfg['blend'].name}) ---")
        blend_path = mcfg['blend']
        fbx_path = mcfg['fbx']
        
        pre_blend_sha = sha256_file(blend_path)
        
        # Open .blend file
        bpy.ops.wm.open_mainfile(filepath=str(blend_path), use_scripts=False)
        mesh_objects = [o for o in bpy.context.scene.objects if o.type == 'MESH']
        
        # Make texture paths relative
        bpy.ops.file.make_paths_relative()
        
        # Capture geometry stats
        mesh_stats_before = {}
        for o in mesh_objects:
            o.data.calc_loop_triangles()
            mesh_stats_before[o.name] = {
                "vertices": len(o.data.vertices),
                "triangles": len(o.data.loop_triangles),
                "polygons": len(o.data.polygons),
                "edges": len(o.data.edges),
                "fingerprint": get_pure_geometry_fingerprint(o),
                "uv_layers": [l.name for l in o.data.uv_layers],
                "active_uv": o.data.uv_layers.active.name if o.data.uv_layers.active else None,
                "materials": [s.name for s in o.material_slots]
            }
            
        total_verts = sum(s["vertices"] for s in mesh_stats_before.values())
        total_tris = sum(s["triangles"] for s in mesh_stats_before.values())
        assert total_verts == mcfg["target_verts"], f"Vert mismatch on {label}: expected {mcfg['target_verts']}, got {total_verts}"
        assert total_tris == mcfg["target_tris"], f"Tri mismatch on {label}: expected {mcfg['target_tris']}, got {total_tris}"
        print(f"Verified {label} mesh geometry: {total_verts} verts, {total_tris} tris.")
        
        # Select ONLY mesh objects for clean export (no lights/cameras)
        bpy.ops.object.select_all(action='DESELECT')
        for o in mesh_objects:
            o.select_set(True)
        bpy.context.view_layer.objects.active = mesh_objects[0]
        
        # Export FBX
        print(f"Exporting FBX to {fbx_path}...")
        bpy.ops.export_scene.fbx(
            filepath=str(fbx_path),
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
        fbx_sha = sha256_file(fbx_path)
        print(f"FBX exported successfully. SHA256: {fbx_sha}, Size: {os.path.getsize(fbx_path)} bytes")
        
        # Save .blend file directly with save_version = 0
        bpy.context.preferences.filepaths.save_version = 0
        bpy.ops.wm.save_as_mainfile(filepath=str(blend_path))
        saved_blend_sha = sha256_file(blend_path)
        print(f"Saved .blend directly. SHA256: {saved_blend_sha}")
        
        # Render verification views for this model
        cam_obj, key_sun = setup_render_environment()
        scene = bpy.context.scene
        
        all_corners = []
        for obj in mesh_objects:
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
        
        model_evidence_dir = EVIDENCE_DIR / label.lower()
        model_evidence_dir.mkdir(parents=True, exist_ok=True)
        rendered_views = {}
        
        views = {
            'perspective': ('spherical', 40, 22, 1.12),
            'front': ('direct', 0, -base_dist, 0, (math.radians(90), 0, 0)),
            'back': ('direct', 0, base_dist, 0, (math.radians(90), 0, math.radians(180))),
            'left': ('direct', -base_dist, 0, 0, (math.radians(90), 0, math.radians(-90))),
            'right': ('direct', base_dist, 0, 0, (math.radians(90), 0, math.radians(90))),
            'top': ('direct', 0, 0, base_dist * 0.95, (0, 0, 0)),
            'low': ('spherical', 25, -18, 1.05),
        }
        
        for vname, vcfg in views.items():
            out_file = model_evidence_dir / f"{vname}.png"
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
            rendered_views[vname] = str(out_file)
            
        # Closeup foliage
        fol_obj = bpy.data.objects.get('Foliage')
        if fol_obj:
            out_fol = model_evidence_dir / "closeup_foliage.png"
            fol_corners = [fol_obj.matrix_world @ Vector(c) for c in fol_obj.bound_box]
            fol_center = sum(fol_corners, Vector()) / len(fol_corners)
            focus_pt = Vector((fol_center.x * 0.5, fol_center.y - 0.2, fol_center.z + 0.1))
            cam_obj.data.lens = 85
            cam_dist = max(0.4, size.z * 0.45)
            cam_obj.location = Vector((focus_pt.x + cam_dist * 0.4, focus_pt.y - cam_dist, focus_pt.z + cam_dist * 0.35))
            direction = focus_pt - cam_obj.location
            rot_quat = direction.to_track_quat('-Z', 'Y')
            cam_obj.rotation_euler = rot_quat.to_euler()
            scene.render.filepath = str(out_fol)
            bpy.ops.render.render(write_still=True)
            rendered_views['closeup_foliage'] = str(out_fol)
            cam_obj.data.lens = 55
            
        # Closeup bark (Large and Ancient)
        if label in ['Large', 'Ancient']:
            out_bark = model_evidence_dir / "closeup_bark.png"
            cam_obj.data.lens = 75
            focus_trunk = Vector((0.0, 0.0, 0.8 if label == 'Large' else 1.1))
            cam_obj.location = Vector((0.8 if label == 'Large' else 1.8, -1.8 if label == 'Large' else -3.2, 0.95 if label == 'Large' else 1.6))
            direction = focus_trunk - cam_obj.location
            rot_quat = direction.to_track_quat('-Z', 'Y')
            cam_obj.rotation_euler = rot_quat.to_euler()
            scene.render.filepath = str(out_bark)
            bpy.ops.render.render(write_still=True)
            rendered_views['closeup_bark'] = str(out_bark)
            cam_obj.data.lens = 55
            
        print(f"Rendered {len(rendered_views)} views to {model_evidence_dir}")
        
        # 3. Independent Reimport Verification in clean scene
        print(f"Performing independent reimport verification for {fbx_path.name}...")
        bpy.ops.wm.read_homefile(use_empty=True)
        bpy.ops.import_scene.fbx(filepath=str(fbx_path))
        
        reimported_objs = [o for o in bpy.context.scene.objects if o.type == 'MESH']
        reimported_stats = {}
        for o in reimported_objs:
            o.data.calc_loop_triangles()
            reimported_stats[o.name] = {
                "vertices": len(o.data.vertices),
                "triangles": len(o.data.loop_triangles),
                "polygons": len(o.data.polygons),
                "edges": len(o.data.edges),
                "scale": list(round(c, 4) for c in o.scale),
                "rotation": list(round(math.degrees(c), 2) for c in o.rotation_euler),
                "uv_layers": [l.name for l in o.data.uv_layers],
                "active_uv": o.data.uv_layers.active.name if o.data.uv_layers.active else None,
                "materials": [s.name for s in o.material_slots]
            }
            # Verify scale = (1, 1, 1) and rotation = (0, 0, 0)
            assert all(abs(s - 1.0) < 1e-4 for s in o.scale), f"Scale mismatch on reimported {o.name}: {o.scale}"
            assert all(abs(r) < 1e-4 for r in o.rotation_euler), f"Rotation mismatch on reimported {o.name}: {o.rotation_euler}"
            # Verify UV layers match source blend
            expected_uv_count = len(mesh_stats_before[o.name]["uv_layers"])
            assert len(o.data.uv_layers) == expected_uv_count, f"UV layer mismatch on reimported {o.name}: expected {expected_uv_count}, got {len(o.data.uv_layers)}"
            if expected_uv_count >= 1:
                assert o.data.uv_layers.active is not None, f"Active UV missing on reimported {o.name}!"
            
        reimport_total_verts = sum(s["vertices"] for s in reimported_stats.values())
        reimport_total_tris = sum(s["triangles"] for s in reimported_stats.values())
        assert reimport_total_tris == mcfg["target_tris"], f"Reimport tri count mismatch: expected {mcfg['target_tris']}, got {reimport_total_tris}"
        print(f"Reimport verification PASSED: {reimport_total_verts} verts, {reimport_total_tris} tris, Scale (1,1,1), Rot (0,0,0), UV0 intact.")
        
        model_reports[label] = {
            "blend_path": str(blend_path),
            "fbx_path": str(fbx_path),
            "pre_blend_sha": pre_blend_sha,
            "saved_blend_sha": saved_blend_sha,
            "fbx_sha": fbx_sha,
            "target_vertices": mcfg["target_verts"],
            "target_triangles": mcfg["target_tris"],
            "reimported_vertices": reimport_total_verts,
            "reimported_triangles": reimport_total_tris,
            "mesh_stats_blend": mesh_stats_before,
            "reimported_stats": reimported_stats,
            "renders": rendered_views
        }

    # 4. Render All 4 Trees in Lineup Progression
    print("\nRendering All 4 Trees Lineup Progression...")
    bpy.ops.wm.read_homefile(use_empty=True)
    cam_obj, key_sun = setup_render_environment()
    scene = bpy.context.scene
    
    # Offsets along X for natural lineup
    lineup_positions = {
        "Seedling": -6.5,
        "Young": -4.2,
        "Large": -1.2,
        "Ancient": 3.8
    }
    
    for label, x_pos in lineup_positions.items():
        blend_file = MODELS[label]["blend"]
        with bpy.data.libraries.load(str(blend_file)) as (data_from, data_to):
            data_to.objects = list(data_from.objects)
        for obj in data_to.objects:
            if obj and obj.type == 'MESH':
                scene.collection.objects.link(obj)
                obj.location.x += x_pos
            
    # Fix relative image paths for clean rendering in unsaved lineup scene
    for img in bpy.data.images:
        if img.source == 'FILE' and img.filepath:
            if img.filepath.startswith('//'):
                clean_path = os.path.normpath(os.path.join(str(MODELS_DIR), img.filepath.replace('//', '')))
                img.filepath = clean_path
                try:
                    img.reload()
                except Exception:
                    pass

    # Camera setup for wide lineup
    cam_obj.data.lens = 38
    cam_obj.location = Vector((-0.5, -16.0, 3.0))
    direction = Vector((-0.5, 0.0, 1.4)) - cam_obj.location
    rot_quat = direction.to_track_quat('-Z', 'Y')
    cam_obj.rotation_euler = rot_quat.to_euler()
    
    scene.render.resolution_x = 1920
    scene.render.resolution_y = 1080
    out_lineup = EVIDENCE_DIR / "final_lineup_progression.png"
    scene.render.filepath = str(out_lineup)
    bpy.ops.render.render(write_still=True)
    print(f"Rendered final_lineup_progression.png -> {out_lineup}")
    
    # 5. Check protected assets unchanged
    protected_post_hashes = {k: sha256_file(v) for k, v in PROTECTED_FILES.items()}
    for k in PROTECTED_FILES:
        assert protected_pre_hashes[k] == protected_post_hashes[k], f"Protected file {k} was modified!"
    print("\nProtected assets verification PASSED: 100% untouched.")
    
    # 6. Save final report
    final_report = {
        "task": "T10",
        "prompt": "P05",
        "models": model_reports,
        "final_lineup_render": str(out_lineup),
        "protected_assets_verified": True,
        "protected_hashes": protected_post_hashes
    }
    
    report_path = EVIDENCE_DIR / "apply_p05_report.json"
    with open(report_path, "w", encoding="utf-8") as f:
        json.dump(final_report, f, indent=2)
    print(f"Saved P05 final report to {report_path}")
    print("=== P05 EXECUTION COMPLETED SUCCESSFULLY ===")

if __name__ == '__main__':
    run_p05()
