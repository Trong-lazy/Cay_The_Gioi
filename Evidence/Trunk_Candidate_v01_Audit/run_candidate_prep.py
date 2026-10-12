"""Audit source, create candidate v01, compute trunk-only mask, and render baseline & mask views."""
import bpy
import bmesh
import hashlib
import json
import math
import os
import shutil
import sys
from pathlib import Path
from mathutils import Vector
from mathutils.kdtree import KDTree

def compute_sha256(filepath):
    p = Path(filepath)
    if not p.exists():
        return None
    return hashlib.sha256(p.read_bytes()).hexdigest().upper()

def digest_json(obj):
    return hashlib.sha256(json.dumps(obj, separators=(",", ":")).encode("utf-8")).hexdigest()

def extract_mesh_fingerprint(obj):
    mesh = obj.data
    mesh.calc_loop_triangles()
    bm = bmesh.new()
    bm.from_mesh(mesh)
    
    positions = [list(v.co) for v in mesh.vertices]
    edges = [list(e.vertices) for e in mesh.edges]
    faces = [list(p.vertices) for p in mesh.polygons]
    uv_data = {u.name: [list(d.uv) for d in u.data] for u in mesh.uv_layers}
    corner_normals = [list(n.vector) for n in mesh.corner_normals]
    
    fp = {
        "name": obj.name,
        "vertex_count": len(mesh.vertices),
        "face_count": len(mesh.polygons),
        "evaluated_triangles": len(mesh.loop_triangles),
        "boundary_edges": sum(e.is_boundary for e in bm.edges),
        "non_manifold_edges": sum(not e.is_manifold for e in bm.edges),
        "loose_vertices": sum(not v.link_edges for v in bm.verts),
        "degenerate_faces": sum(f.calc_area() < 1e-10 for f in bm.faces),
        "positions_digest": digest_json(positions),
        "topology_digest": digest_json({"edges": edges, "faces": faces}),
        "uv_digest": digest_json(uv_data),
        "corner_normals_digest": digest_json(corner_normals),
        "matrix_world": [list(row) for row in obj.matrix_world],
        "materials": [m.name if m else None for m in mesh.materials],
        "modifiers": [m.type for m in obj.modifiers],
        "vertex_groups": [vg.name for vg in obj.vertex_groups]
    }
    bm.free()
    return fp

def main():
    args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    project_root = Path(r"c:\Users\Asus\foder game 3D\Cay_The_Gioi")
    source_blend = project_root / "Assets" / "_Project" / "Art" / "Bonsai_Tree.blend"
    source_fbx = project_root / "Assets" / "_Project" / "Art" / "Bonsai_Tree.fbx"
    candidate_blend = project_root / "ArtSource" / "Models" / "Bonsai_Trunk_Candidate_v01.blend"
    evidence_dir = project_root / "Evidence" / "Trunk_Candidate_v01_Audit"
    evidence_dir.mkdir(parents=True, exist_ok=True)
    candidate_blend.parent.mkdir(parents=True, exist_ok=True)
    
    expected_source_hash = "18BB67FC27BEA34137777EC00ACD0AD12B4FDF408537F3DA2E833CE48095BEF3"
    expected_fbx_hash = "52B63D9F9F555BBBC85302DA9BA3AA0E6F1BF82F4E193E070F5B1B8981D0B7D5"
    
    # Step 1: Check source file hash
    source_hash_initial = compute_sha256(source_blend)
    fbx_hash_initial = compute_sha256(source_fbx)
    print(f"Source SHA256: {source_hash_initial}")
    assert source_hash_initial == expected_source_hash, f"Source hash mismatch: {source_hash_initial} != {expected_source_hash}"
    assert fbx_hash_initial == expected_fbx_hash, f"FBX hash mismatch: {fbx_hash_initial} != {expected_fbx_hash}"
    
    # Step 2: Extract baseline fingerprints from source
    bpy.ops.wm.open_mainfile(filepath=str(source_blend), use_scripts=False)
    baseline_fingerprints = {}
    for obj_name in ["Roots", "Trunk_Branches", "Foliage"]:
        obj = bpy.data.objects[obj_name]
        baseline_fingerprints[obj_name] = extract_mesh_fingerprint(obj)
    
    # Step 3: Copy source to candidate
    print(f"Creating candidate copy at: {candidate_blend}")
    shutil.copy2(source_blend, candidate_blend)
    candidate_hash_initial = compute_sha256(candidate_blend)
    assert candidate_hash_initial == source_hash_initial
    
    # Step 4: Open candidate and configure Trunk_Editable_Mask
    bpy.ops.wm.open_mainfile(filepath=str(candidate_blend), use_scripts=False)
    tb_obj = bpy.data.objects["Trunk_Branches"]
    roots_obj = bpy.data.objects["Roots"]
    fol_obj = bpy.data.objects["Foliage"]
    
    tb_mesh = tb_obj.data
    bm = bmesh.new()
    bm.from_mesh(tb_mesh)
    
    # Decompose into connected components
    visited = set()
    components = []
    for v in bm.verts:
        if v not in visited:
            comp = []
            stack = [v]
            visited.add(v)
            while stack:
                curr = stack.pop()
                comp.append(curr)
                for e in curr.link_edges:
                    other = e.other_vert(curr)
                    if other not in visited:
                        visited.add(other)
                        stack.append(other)
            components.append(comp)
    
    # Comp 0..5 are Trunk Core (6 fluted columns)
    trunk_verts = set()
    for i in range(6):
        trunk_verts.update(components[i])
    
    trunk_kd = KDTree(len(trunk_verts))
    trunk_vlist = list(trunk_verts)
    for i, v in enumerate(trunk_vlist):
        trunk_kd.insert(v.co, i)
    trunk_kd.balance()
    
    roots_kd = KDTree(len(roots_obj.data.vertices))
    for i, v in enumerate(roots_obj.data.vertices):
        roots_kd.insert(roots_obj.matrix_world @ v.co, i)
    roots_kd.balance()
    
    fol_kd = KDTree(len(fol_obj.data.vertices))
    for i, v in enumerate(fol_obj.data.vertices):
        fol_kd.insert(fol_obj.matrix_world @ v.co, i)
    fol_kd.balance()
    
    vertex_weights = [0.0] * len(bm.verts)
    vertex_classes = ["LOCKED_DISTAL_BRANCH"] * len(bm.verts)
    
    # 1. Base Trunk Comp 0..5
    for i in range(6):
        for v in components[i]:
            z = v.co.z
            r_dist = roots_kd.find(v.co)[2]
            if z <= 0.25 or r_dist <= 0.03:
                w = 0.0
                cls = "LOCKED_ROOT_JUNCTION"
            elif z < 0.60:
                t = (z - 0.25) / (0.60 - 0.25)
                w = t * t * (3.0 - 2.0 * t)
                if r_dist < 0.08:
                    r_factor = (r_dist - 0.03) / 0.05
                    w *= max(0.0, min(1.0, r_factor))
                cls = "TRANSITION_ROOT_FLARE"
            elif z > 3.30:
                t = max(0.0, min(1.0, (3.43 - z) / (3.43 - 3.30)))
                w = t * t * (3.0 - 2.0 * t)
                cls = "TRANSITION_APEX_SPLIT"
            else:
                w = 1.0
                cls = "EDITABLE_TRUNK"
            vertex_weights[v.index] = w
            vertex_classes[v.index] = cls
            
    # 2. Branch Collar Transitions for primary branches
    primary_branch_indices = [6, 9, 11, 13, 16, 19, 22, 24, 26, 28, 30, 32, 34, 35, 36, 37, 38]
    collar_max_dist = 0.20
    for bi in primary_branch_indices:
        for v in components[bi]:
            d = trunk_kd.find(v.co)[2]
            if d <= collar_max_dist:
                t = 1.0 - (d / collar_max_dist)
                w = t * t * (3.0 - 2.0 * t)
                if w > vertex_weights[v.index]:
                    vertex_weights[v.index] = w
                    vertex_classes[v.index] = "TRANSITION_BRANCH_COLLAR"
                    
    # 3. Enforce strict lock on boundary edges and foliage proximity
    for v in bm.verts:
        f_dist = fol_kd.find(v.co)[2]
        if f_dist < 0.15:
            vertex_weights[v.index] = 0.0
            vertex_classes[v.index] = "LOCKED_FOLIAGE_PROXIMITY"
        if v.is_boundary:
            vertex_weights[v.index] = 0.0
            if v.co.z < 0.5:
                vertex_classes[v.index] = "LOCKED_ROOT_JUNCTION"
            else:
                vertex_classes[v.index] = "LOCKED_BRANCH_TIP"
                
    bm.free()
    
    # Create or update Vertex Group on tb_obj
    vg_name = "Trunk_Editable_Mask"
    vg = tb_obj.vertex_groups.get(vg_name)
    if vg is None:
        vg = tb_obj.vertex_groups.new(name=vg_name)
    for idx, w in enumerate(vertex_weights):
        vg.add([idx], float(w), "REPLACE")
        
    # Create color attribute on Trunk_Branches for mask visualization
    col_attr_name = "Mask_Color"
    if col_attr_name in tb_mesh.color_attributes:
        tb_mesh.color_attributes.remove(tb_mesh.color_attributes[col_attr_name])
    col_attr = tb_mesh.color_attributes.new(name=col_attr_name, type="FLOAT_COLOR", domain="POINT")
    
    # Locked = Slate Blue (0.18, 0.22, 0.35, 1.0)
    # Transition = Amber Gold (0.95, 0.72, 0.12, 1.0)
    # Editable = Emerald Green (0.12, 0.85, 0.45, 1.0)
    for idx, w in enumerate(vertex_weights):
        if w == 0.0:
            c = (0.18, 0.22, 0.35, 1.0)
        elif w < 1.0:
            # blend from slate to amber to green
            c = (0.95, 0.72, 0.12, 1.0)
        else:
            c = (0.12, 0.85, 0.45, 1.0)
        col_attr.data[idx].color = c
        
    # Save candidate file
    bpy.ops.wm.save_mainfile(filepath=str(candidate_blend))
    candidate_hash_saved = compute_sha256(candidate_blend)
    print(f"Candidate saved. Hash: {candidate_hash_saved}")
    
    # Step 5: Verify candidate integrity against baseline
    bpy.ops.wm.open_mainfile(filepath=str(candidate_blend), use_scripts=False)
    candidate_fingerprints = {}
    for obj_name in ["Roots", "Trunk_Branches", "Foliage"]:
        obj = bpy.data.objects[obj_name]
        candidate_fingerprints[obj_name] = extract_mesh_fingerprint(obj)
        
    # Re-acquire object references after open_mainfile
    roots_obj = bpy.data.objects["Roots"]
    tb_obj = bpy.data.objects["Trunk_Branches"]
    fol_obj = bpy.data.objects["Foliage"]
    
    # Verify Roots and Foliage match baseline 100%
    for obj_name in ["Roots", "Foliage"]:
        for field in ["vertex_count", "face_count", "evaluated_triangles", "boundary_edges",
                      "positions_digest", "topology_digest", "uv_digest", "corner_normals_digest",
                      "matrix_world", "materials", "modifiers"]:
            base_val = baseline_fingerprints[obj_name][field]
            cand_val = candidate_fingerprints[obj_name][field]
            assert base_val == cand_val, f"{obj_name} {field} mismatch: {base_val} != {cand_val}"
            
    # Verify Trunk_Branches geometry matches baseline 100%
    for field in ["vertex_count", "face_count", "evaluated_triangles", "boundary_edges",
                  "positions_digest", "topology_digest", "uv_digest", "corner_normals_digest",
                  "matrix_world", "modifiers"]:
        base_val = baseline_fingerprints["Trunk_Branches"][field]
        cand_val = candidate_fingerprints["Trunk_Branches"][field]
        assert base_val == cand_val, f"Trunk_Branches geometry {field} mismatch: {base_val} != {cand_val}"
        
    print("Verification SUCCESS: Geometry and transforms are 100% preserved!")
    
    # Step 6: Render Baseline and Mask views
    # Setup camera definitions
    render_cameras = {
        "front": {"type": "ORTHO", "scale": 8.8, "loc": (0.0, -11.0, 2.13), "rot": (math.radians(90), 0.0, 0.0)},
        "back": {"type": "ORTHO", "scale": 8.8, "loc": (0.0, 11.0, 2.13), "rot": (math.radians(90), 0.0, math.radians(180))},
        "left": {"type": "ORTHO", "scale": 8.8, "loc": (-11.0, 0.0, 2.13), "rot": (math.radians(90), 0.0, math.radians(-90))},
        "right": {"type": "ORTHO", "scale": 8.8, "loc": (11.0, 0.0, 2.13), "rot": (math.radians(90), 0.0, math.radians(90))},
        "perspective": {"type": "PERSP", "fov": math.radians(38), "loc": (6.8, -6.8, 4.5), "target": (0.0, 0.0, 2.1)},
        "closeup_trunk": {"type": "ORTHO", "scale": 3.2, "loc": (0.0, -5.0, 1.8), "rot": (math.radians(90), 0.0, 0.0)},
        "closeup_branch_collar": {"type": "ORTHO", "scale": 2.2, "loc": (0.0, -4.0, 2.2), "rot": (math.radians(90), 0.0, 0.0)},
        "closeup_root_junction": {"type": "ORTHO", "scale": 2.2, "loc": (0.0, -4.0, 0.6), "rot": (math.radians(90), 0.0, 0.0)},
    }
    
    # Create temporary review scene
    review_scene = bpy.data.scenes.new("__ReviewScene")
    for obj_name in ["Roots", "Trunk_Branches", "Foliage"]:
        review_scene.collection.objects.link(bpy.data.objects[obj_name])
        
    review_scene.render.engine = "BLENDER_EEVEE"
    review_scene.render.resolution_x = 1280
    review_scene.render.resolution_y = 960
    review_scene.render.resolution_percentage = 100
    review_scene.view_settings.view_transform = "AgX"
    
    # World lighting
    world = bpy.data.worlds.new("__ReviewWorld")
    world.use_nodes = True
    bg_node = world.node_tree.nodes["Background"]
    bg_node.inputs["Color"].default_value = (0.20, 0.22, 0.25, 1.0)
    bg_node.inputs["Strength"].default_value = 0.8
    review_scene.world = world
    
    # Key light, fill light, rim light
    lights_config = [
        {"name": "KeyLight", "type": "AREA", "shape": "DISK", "size": 9.0, "energy": 1200, "loc": (4.5, -6.5, 6.0), "target": (0.0, 0.0, 2.1)},
        {"name": "FillLight", "type": "AREA", "shape": "DISK", "size": 8.0, "energy": 650, "loc": (-5.5, -4.5, 4.5), "target": (0.0, 0.0, 2.1)},
        {"name": "RimLight", "type": "AREA", "shape": "DISK", "size": 9.0, "energy": 800, "loc": (0.0, 6.5, 5.5), "target": (0.0, 0.0, 2.1)},
    ]
    for lc in lights_config:
        ld = bpy.data.lights.new(lc["name"], lc["type"])
        ld.shape = lc["shape"]
        ld.size = lc["size"]
        ld.energy = lc["energy"]
        lo = bpy.data.objects.new(lc["name"], ld)
        lo.location = Vector(lc["loc"])
        lo.rotation_euler = (Vector(lc["target"]) - lo.location).to_track_quat("-Z", "Y").to_euler()
        review_scene.collection.objects.link(lo)
        
    # Camera object
    cam_data = bpy.data.cameras.new("__ReviewCam")
    cam_obj = bpy.data.objects.new("__ReviewCam", cam_data)
    review_scene.collection.objects.link(cam_obj)
    review_scene.camera = cam_obj
    
    # Create Clay Material
    clay_mat = bpy.data.materials.new("__ClayMaterial")
    clay_mat.use_nodes = True
    clay_bsdf = clay_mat.node_tree.nodes.get("Principled BSDF")
    clay_bsdf.inputs["Base Color"].default_value = (0.75, 0.74, 0.72, 1.0)
    clay_bsdf.inputs["Roughness"].default_value = 0.65
    
    # Create Mask Material for Trunk_Branches
    mask_tb_mat = bpy.data.materials.new("__MaskTB_Material")
    mask_tb_mat.use_nodes = True
    tree = mask_tb_mat.node_tree
    tree.nodes.clear()
    out_node = tree.nodes.new("ShaderNodeOutputMaterial")
    bsdf_node = tree.nodes.new("ShaderNodeBsdfPrincipled")
    attr_node = tree.nodes.new("ShaderNodeAttribute")
    attr_node.attribute_name = "Mask_Color"
    attr_node.attribute_type = "GEOMETRY"
    tree.links.new(attr_node.outputs["Color"], bsdf_node.inputs["Base Color"])
    bsdf_node.inputs["Roughness"].default_value = 0.5
    tree.links.new(bsdf_node.outputs["BSDF"], out_node.inputs["Surface"])
    
    # Mask Context Material for Roots
    mask_roots_mat = bpy.data.materials.new("__MaskRoots_Material")
    mask_roots_mat.use_nodes = True
    bsdf_r = mask_roots_mat.node_tree.nodes.get("Principled BSDF")
    bsdf_r.inputs["Base Color"].default_value = (0.24, 0.25, 0.28, 1.0) # Dark slate
    bsdf_r.inputs["Roughness"].default_value = 0.7
    
    # Mask Context Material for Foliage
    mask_fol_mat = bpy.data.materials.new("__MaskFol_Material")
    mask_fol_mat.use_nodes = True
    bsdf_f = mask_fol_mat.node_tree.nodes.get("Principled BSDF")
    bsdf_f.inputs["Base Color"].default_value = (0.32, 0.38, 0.32, 1.0) # Muted sage
    bsdf_f.inputs["Roughness"].default_value = 0.7
    
    def set_camera(name, cfg):
        cam_data.type = cfg["type"]
        if cfg["type"] == "ORTHO":
            cam_data.ortho_scale = cfg["scale"]
            cam_obj.location = Vector(cfg["loc"])
            cam_obj.rotation_euler = cfg["rot"]
        else:
            cam_data.lens_unit = "FOV"
            cam_data.angle = cfg["fov"]
            cam_obj.location = Vector(cfg["loc"])
            cam_obj.rotation_euler = (Vector(cfg["target"]) - cam_obj.location).to_track_quat("-Z", "Y").to_euler()
            
    rendered_files = {}
    
    # 1. Baseline Clay Renders (use ViewLayer material_override)
    review_scene.view_layers[0].material_override = clay_mat
    for cam_name, cam_cfg in render_cameras.items():
        set_camera(cam_name, cam_cfg)
        out_path = str(evidence_dir / f"baseline_{cam_name}.png")
        review_scene.render.filepath = out_path
        bpy.ops.render.render(write_still=True, scene=review_scene.name)
        rendered_files[f"baseline_{cam_name}"] = out_path
        print(f"Rendered: {out_path}")
        
    # 2. Mask Renders (remove override, assign temporary per-object materials)
    review_scene.view_layers[0].material_override = None
    roots_obj.data.materials.clear()
    roots_obj.data.materials.append(mask_roots_mat)
    fol_obj.data.materials.clear()
    fol_obj.data.materials.append(mask_fol_mat)
    tb_obj.data.materials.clear()
    tb_obj.data.materials.append(mask_tb_mat)
    
    for cam_name, cam_cfg in render_cameras.items():
        set_camera(cam_name, cam_cfg)
        out_path = str(evidence_dir / f"mask_{cam_name}.png")
        review_scene.render.filepath = out_path
        bpy.ops.render.render(write_still=True, scene=review_scene.name)
        rendered_files[f"mask_{cam_name}"] = out_path
        print(f"Rendered: {out_path}")
        
    # Step 7: Final Source Verification
    source_hash_final = compute_sha256(source_blend)
    fbx_hash_final = compute_sha256(source_fbx)
    assert source_hash_final == expected_source_hash, "SOURCE WAS ALTERED!"
    assert fbx_hash_final == expected_fbx_hash, "FBX WAS ALTERED!"
    
    # Step 8: Write JSON manifests
    stats = {
        "total_vertices": len(vertex_weights),
        "locked_count": sum(1 for w in vertex_weights if w == 0.0),
        "transition_count": sum(1 for w in vertex_weights if 0.0 < w < 1.0),
        "editable_count": sum(1 for w in vertex_weights if w == 1.0),
        "classes": {cls: sum(1 for c in vertex_classes if c == cls) for cls in set(vertex_classes)}
    }
    
    manifest = {
        "source": {
            "path": str(source_blend),
            "sha256": source_hash_final,
            "unchanged": source_hash_final == expected_source_hash
        },
        "source_fbx": {
            "path": str(source_fbx),
            "sha256": fbx_hash_final,
            "unchanged": fbx_hash_final == expected_fbx_hash
        },
        "candidate": {
            "path": str(candidate_blend),
            "sha256": candidate_hash_saved,
            "version": "v01"
        },
        "mesh_stats": {
            "total_evaluated_triangles": 73888,
            "objects": baseline_fingerprints
        },
        "candidate_fingerprints": candidate_fingerprints,
        "mask_stats": stats,
        "reference_image": {
            "path": r"C:\Users\Asus\Downloads\pexels-quang-nguyen-vinh-222549-14771841.jpg",
            "exists": Path(r"C:\Users\Asus\Downloads\pexels-quang-nguyen-vinh-222549-14771841.jpg").exists()
        },
        "rendered_files": rendered_files
    }
    
    (evidence_dir / "audit_manifest.json").write_text(json.dumps(manifest, indent=2, ensure_ascii=False), encoding="utf-8")
    
    # Mask definition
    mask_def = {
        "vertex_count": len(vertex_weights),
        "statistics": stats,
        "weights": vertex_weights,
        "classes": vertex_classes
    }
    (evidence_dir / "mask_definition.json").write_text(json.dumps(mask_def, indent=2), encoding="utf-8")
    
    # Render config
    render_cfg = {
        "engine": "BLENDER_EEVEE",
        "resolution": [1280, 960],
        "view_transform": "AgX",
        "cameras": render_cameras,
        "lights": lights_config,
        "world": {"color": [0.20, 0.22, 0.25, 1.0], "strength": 0.8}
    }
    (evidence_dir / "render_config.json").write_text(json.dumps(render_cfg, indent=2), encoding="utf-8")
    
    print("\nALL TASKS COMPLETED SUCCESSFULLY!")
    print(f"Source SHA256 (verified unchanged): {source_hash_final}")
    print(f"Candidate SHA256: {candidate_hash_saved}")
    print(f"Mask: {stats['locked_count']} locked, {stats['transition_count']} transition, {stats['editable_count']} editable.")

if __name__ == "__main__":
    main()
