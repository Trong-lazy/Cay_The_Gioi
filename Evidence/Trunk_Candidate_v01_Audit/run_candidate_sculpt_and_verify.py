"""Apply controlled trunk-only sculpt to Candidate v01, verify preservation, render comparisons, and export/reimport FBX."""
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
    project_root = Path(r"c:\Users\Asus\foder game 3D\Cay_The_Gioi")
    source_blend = project_root / "Assets" / "_Project" / "Art" / "Bonsai_Tree.blend"
    source_fbx = project_root / "Assets" / "_Project" / "Art" / "Bonsai_Tree.fbx"
    candidate_blend = project_root / "ArtSource" / "Models" / "Bonsai_Trunk_Candidate_v01.blend"
    candidate_export_fbx = project_root / "ArtSource" / "Exports" / "Bonsai_Trunk_Candidate_v01.fbx"
    candidate_export_fbx.parent.mkdir(parents=True, exist_ok=True)
    evidence_dir = project_root / "Evidence" / "Trunk_Candidate_v01_Audit"
    
    expected_source_hash = "18BB67FC27BEA34137777EC00ACD0AD12B4FDF408537F3DA2E833CE48095BEF3"
    expected_fbx_hash = "52B63D9F9F555BBBC85302DA9BA3AA0E6F1BF82F4E193E070F5B1B8981D0B7D5"
    
    # Step 1: Pre-execution Safety Verification
    assert compute_sha256(source_blend) == expected_source_hash, "Source .blend altered before execution!"
    assert compute_sha256(source_fbx) == expected_fbx_hash, "Source .fbx altered before execution!"
    
    # Read baseline fingerprints from audit_manifest.json
    manifest_path = evidence_dir / "audit_manifest.json"
    audit_data = json.loads(manifest_path.read_text(encoding="utf-8"))
    baseline_fp = audit_data["mesh_stats"]["objects"]
    
    # Step 2: Open candidate .blend and perform controlled sculpt
    bpy.ops.wm.open_mainfile(filepath=str(candidate_blend), use_scripts=False)
    
    tb_obj = bpy.data.objects["Trunk_Branches"]
    roots_obj = bpy.data.objects["Roots"]
    fol_obj = bpy.data.objects["Foliage"]
    
    vg = tb_obj.vertex_groups.get("Trunk_Editable_Mask")
    assert vg is not None, "Vertex group Trunk_Editable_Mask not found!"
    
    tb_mesh = tb_obj.data
    bm = bmesh.new()
    bm.from_mesh(tb_mesh)
    
    # Read weights
    weights = [0.0] * len(bm.verts)
    for v in bm.verts:
        try:
            weights[v.index] = vg.weight(v.index)
        except:
            weights[v.index] = 0.0
            
    # Cache original positions
    orig_coords = [Vector(v.co) for v in bm.verts]
    
    # Extract spine curve from Comp 0 (trunk central core)
    visited = set()
    comps = []
    for v in bm.verts:
        if v not in visited:
            c = []
            q = [v]
            visited.add(v)
            while q:
                curr = q.pop()
                c.append(curr)
                for e in curr.link_edges:
                    o = e.other_vert(curr)
                    if o not in visited:
                        visited.add(o)
                        q.append(o)
            comps.append(c)
            
    c0 = comps[0]
    spine_zs = [0.04 * i for i in range(2, 86)]
    spine = []
    for z in spine_zs:
        sv = [v for v in c0 if abs(v.co.z - z) < 0.08]
        if sv:
            cx = sum(v.co.x for v in sv) / len(sv)
            cy = sum(v.co.y for v in sv) / len(sv)
            spine.append((z, cx, cy))
            
    def get_spine_center(z):
        if z <= spine[0][0]:
            return spine[0][1], spine[0][2]
        if z >= spine[-1][0]:
            return spine[-1][1], spine[-1][2]
        for i in range(len(spine) - 1):
            if spine[i][0] <= z <= spine[i+1][0]:
                t = (z - spine[i][0]) / (spine[i+1][0] - spine[i][0])
                cx = (1 - t) * spine[i][1] + t * spine[i+1][1]
                cy = (1 - t) * spine[i][2] + t * spine[i+1][2]
                return cx, cy
        return 0.0, 0.0

    # Build KDTree for trunk core to measure collar distance
    trunk_verts_set = set()
    for i in range(6):
        trunk_verts_set.update(comps[i])
    trunk_kd = KDTree(len(trunk_verts_set))
    for i, v in enumerate(trunk_verts_set):
        trunk_kd.insert(v.co, i)
    trunk_kd.balance()

    # Apply Artistic Controlled Sculpt
    # Objectives:
    # 1. Thân vững chãi ở gốc, thuôn tự nhiên lên ngọn, không thắt eo.
    # 2. Sống dọc, rãnh nông hữu cơ theo ảnh tham chiếu.
    # 3. Vai đỡ cổ cành (branch collar flare).
    # 4. Giữ nguyên 100% các đỉnh có weight == 0.
    displacements = [Vector((0.0, 0.0, 0.0)) for _ in bm.verts]
    
    for v in bm.verts:
        w = weights[v.index]
        if w <= 0.0:
            continue
            
        z = v.co.z
        cx, cy = get_spine_center(z)
        rx = v.co.x - cx
        ry = v.co.y - cy
        r = math.sqrt(rx * rx + ry * ry)
        if r < 1e-5:
            continue
        rx_norm = rx / r
        ry_norm = ry / r
        
        # 1. Bulk radial expansion factor S_rad(z)
        if z < 0.25:
            s_rad = 0.0
        elif z < 0.60:
            t = (z - 0.25) / 0.35
            s_rad = 0.24 * t * t * (3.0 - 2.0 * t)
        elif z < 1.80:
            t = (z - 0.60) / 1.20
            # Peak expansion ~+31% in mid-trunk to cure slenderness
            s_rad = 0.24 + 0.07 * math.sin(math.pi * t)
        elif z < 2.80:
            t = (z - 1.80) / 1.00
            # Gradual organic taper
            s_rad = 0.27 * (1.0 - 0.32 * t)
        elif z < 3.35:
            t = (z - 2.80) / 0.55
            s_rad = 0.18 * (1.0 - t * t)
        else:
            s_rad = 0.0
            
        # 2. Flutes & longitudinal ridges (Sống dọc và rãnh nông cổ thụ theo ảnh tham chiếu)
        theta = math.atan2(ry, rx)
        # 5 organic longitudinal flutes with subtle spiral
        ridge_primary = 0.032 * math.cos(5.0 * theta + 0.5 * z)
        ridge_secondary = 0.012 * math.sin(3.0 * theta - 0.7 * z + 1.2)
        flute_envelope = min(1.0, max(0.0, (z - 0.35) / 0.5)) * min(1.0, max(0.0, (3.35 - z) / 0.35))
        ridge_disp = flute_envelope * (ridge_primary + ridge_secondary)
        
        # 3. Asymmetric muscular base buttress flare (Bạnh gốc cổ thụ)
        asym_flare = 0.038 * max(0.0, math.cos(theta + 0.25)) ** 2 * math.exp(-((z - 0.75) / 0.40) ** 2)
        
        # 4. Branch collar shoulder flare
        collar_disp = 0.0
        if v not in trunk_verts_set:
            d_trunk = trunk_kd.find(v.co)[2]
            if d_trunk <= 0.20:
                t_col = 1.0 - (d_trunk / 0.20)
                collar_disp = 0.045 * t_col * t_col * (3.0 - 2.0 * t_col)
                
        # Total displacement vector
        total_radial = s_rad * r + ridge_disp + asym_flare + collar_disp
        disp_x = w * total_radial * rx_norm
        disp_y = w * total_radial * ry_norm
        
        # Small vertical lift at collars to form muscular bracket
        disp_z = 0.0
        if collar_disp > 0:
            disp_z = -0.012 * w * collar_disp
            
        displacements[v.index] = Vector((disp_x, disp_y, disp_z))
        v.co = orig_coords[v.index] + displacements[v.index]

    # Gentle weighted Laplacian relaxation on editable interior vertices (w > 0.4)
    # to guarantee smooth organic curvature without surface pinch
    for _ in range(2):
        new_coords = [Vector(v.co) for v in bm.verts]
        for v in bm.verts:
            w = weights[v.index]
            if w > 0.4:
                neighbors = [e.other_vert(v) for e in v.link_edges]
                if neighbors:
                    centroid = Vector((sum(n.co.x for n in neighbors)/len(neighbors),
                                       sum(n.co.y for n in neighbors)/len(neighbors),
                                       sum(n.co.z for n in neighbors)/len(neighbors)))
                    # Blend 15% towards neighbor centroid
                    new_coords[v.index] = v.co.lerp(centroid, 0.15 * w)
        for v in bm.verts:
            if weights[v.index] > 0.0:
                v.co = new_coords[v.index]
            else:
                v.co = orig_coords[v.index] # Strictly lock 0-weight vertices

    # Verify zero displacement on all locked vertices
    max_locked_err = 0.0
    for v in bm.verts:
        if weights[v.index] == 0.0:
            err = (v.co - orig_coords[v.index]).length
            if err > max_locked_err:
                max_locked_err = err
    assert max_locked_err < 1e-7, f"Locked vertices moved! Max error: {max_locked_err}"
    print(f"Verified locked vertices: max error = {max_locked_err:.10f} (PASSED)")

    # Write back to mesh and recalculate normals
    bm.to_mesh(tb_mesh)
    bm.free()
    tb_mesh.update()
    
    # Save candidate .blend
    bpy.ops.wm.save_mainfile(filepath=str(candidate_blend))
    candidate_blend_hash = compute_sha256(candidate_blend)
    print(f"Candidate v01 updated & saved: {candidate_blend_hash}")
    
    # Step 3: Verification of Candidate v01
    bpy.ops.wm.open_mainfile(filepath=str(candidate_blend), use_scripts=False)
    cand_roots = bpy.data.objects["Roots"]
    cand_tb = bpy.data.objects["Trunk_Branches"]
    cand_fol = bpy.data.objects["Foliage"]
    
    cand_fp_roots = extract_mesh_fingerprint(cand_roots)
    cand_fp_fol = extract_mesh_fingerprint(cand_fol)
    cand_fp_tb = extract_mesh_fingerprint(cand_tb)
    
    # Verify Roots and Foliage match baseline 100%
    for obj_name, cand_fp in [("Roots", cand_fp_roots), ("Foliage", cand_fp_fol)]:
        base_fp = baseline_fp[obj_name]
        for field in ["vertex_count", "face_count", "evaluated_triangles", "boundary_edges",
                      "positions_digest", "topology_digest", "uv_digest", "corner_normals_digest",
                      "matrix_world", "materials", "modifiers"]:
            assert cand_fp[field] == base_fp[field], f"{obj_name} {field} altered: {cand_fp[field]} != {base_fp[field]}"
            
    # Verify Trunk_Branches topology matches baseline 100%
    base_fp_tb = baseline_fp["Trunk_Branches"]
    for field in ["vertex_count", "face_count", "evaluated_triangles", "boundary_edges",
                  "non_manifold_edges", "loose_vertices", "degenerate_faces", "topology_digest",
                  "matrix_world", "modifiers"]:
        assert cand_fp_tb[field] == base_fp_tb[field], f"Trunk_Branches {field} altered: {cand_fp_tb[field]} != {base_fp_tb[field]}"
        
    print("Mesh verification SUCCESS: Topology, boundaries, Roots, and Foliage are 100% preserved!")
    
    # Step 4: Render Candidate Views with identical baseline configuration
    render_cfg = json.loads((evidence_dir / "render_config.json").read_text(encoding="utf-8"))
    render_cameras = render_cfg["cameras"]
    
    review_scene = bpy.data.scenes.new("__CandidateReviewScene")
    for obj in [cand_roots, cand_tb, cand_fol]:
        review_scene.collection.objects.link(obj)
        
    review_scene.render.engine = "BLENDER_EEVEE"
    review_scene.render.resolution_x = 1280
    review_scene.render.resolution_y = 960
    review_scene.render.resolution_percentage = 100
    review_scene.view_settings.view_transform = "AgX"
    
    world = bpy.data.worlds.new("__CandidateReviewWorld")
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.20, 0.22, 0.25, 1.0)
    world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.8
    review_scene.world = world
    
    for lc in render_cfg["lights"]:
        ld = bpy.data.lights.new(lc["name"], lc["type"])
        ld.shape = lc["shape"]
        ld.size = lc["size"]
        ld.energy = lc["energy"]
        lo = bpy.data.objects.new(lc["name"], ld)
        lo.location = Vector(lc["loc"])
        lo.rotation_euler = (Vector(lc["target"]) - lo.location).to_track_quat("-Z", "Y").to_euler()
        review_scene.collection.objects.link(lo)
        
    cam_data = bpy.data.cameras.new("__CandidateReviewCam")
    cam_obj = bpy.data.objects.new("__CandidateReviewCam", cam_data)
    review_scene.collection.objects.link(cam_obj)
    review_scene.camera = cam_obj
    
    clay_mat = bpy.data.materials.new("__CandidateClayMat")
    clay_mat.use_nodes = True
    clay_bsdf = clay_mat.node_tree.nodes.get("Principled BSDF")
    clay_bsdf.inputs["Base Color"].default_value = (0.75, 0.74, 0.72, 1.0)
    clay_bsdf.inputs["Roughness"].default_value = 0.65
    review_scene.view_layers[0].material_override = clay_mat
    
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

    rendered_candidate_files = {}
    for cam_name, cam_cfg in render_cameras.items():
        set_camera(cam_name, cam_cfg)
        out_path = str(evidence_dir / f"candidate_{cam_name}.png")
        review_scene.render.filepath = out_path
        bpy.ops.render.render(write_still=True, scene=review_scene.name)
        rendered_candidate_files[f"candidate_{cam_name}"] = out_path
        print(f"Rendered: {out_path}")
        
    # Step 5: Export Candidate FBX (Prompt 3 requirement)
    # Select only the 3 meshes
    bpy.ops.wm.open_mainfile(filepath=str(candidate_blend), use_scripts=False)
    for obj in bpy.context.scene.objects:
        obj.select_set(False)
    for obj_name in ["Roots", "Trunk_Branches", "Foliage"]:
        bpy.data.objects[obj_name].select_set(True)
    bpy.context.view_layer.objects.active = bpy.data.objects["Trunk_Branches"]
    
    print(f"Exporting Candidate FBX to: {candidate_export_fbx}")
    bpy.ops.export_scene.fbx(
        filepath=str(candidate_export_fbx),
        use_selection=True,
        object_types={"MESH"},
        apply_scale_options="FBX_SCALE_ALL",
        axis_forward="-Z",
        axis_up="Y",
        apply_unit_scale=True,
        bake_space_transform=False,
        mesh_smooth_type="FACE"
    )
    assert candidate_export_fbx.exists(), "FBX export failed to produce file!"
    candidate_fbx_hash = compute_sha256(candidate_export_fbx)
    print(f"FBX Export SUCCESS. Hash: {candidate_fbx_hash}")
    
    # Step 6: Reimport FBX Verification (Prompt 3 requirement)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(candidate_export_fbx))
    
    reimport_scene = bpy.context.scene
    reimport_objs = {obj.name: obj for obj in reimport_scene.objects if obj.type == "MESH"}
    assert set(reimport_objs.keys()) == {"Roots", "Trunk_Branches", "Foliage"}, f"Reimported objects mismatch: {set(reimport_objs.keys())}"
    
    reimport_stats = {}
    total_reimport_tris = 0
    for name, obj in reimport_objs.items():
        m = obj.data
        m.calc_loop_triangles()
        total_reimport_tris += len(m.loop_triangles)
        reimport_stats[name] = {
            "scale": list(obj.scale),
            "rotation": list(obj.rotation_euler),
            "triangles": len(m.loop_triangles),
            "vertices": len(m.vertices),
            "dimensions": list(obj.dimensions)
        }
        assert obj.scale == Vector((1.0, 1.0, 1.0)), f"Scale is not 1.0 for {name}: {obj.scale}"
    assert total_reimport_tris == 73888, f"Total triangles mismatch: {total_reimport_tris} != 73888"
    print(f"Reimport FBX SUCCESS: Total triangles = {total_reimport_tris}, scales = 1.0.")
    
    # Render Reimported FBX perspective verification
    reimport_scene.render.engine = "BLENDER_EEVEE"
    reimport_scene.render.resolution_x = 1280
    reimport_scene.render.resolution_y = 960
    reimport_scene.view_settings.view_transform = "AgX"
    
    w_re = bpy.data.worlds.new("__ReWorld")
    w_re.use_nodes = True
    w_re.node_tree.nodes["Background"].inputs["Color"].default_value = (0.20, 0.22, 0.25, 1.0)
    w_re.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.8
    reimport_scene.world = w_re
    
    for lc in render_cfg["lights"]:
        ld = bpy.data.lights.new(lc["name"], lc["type"])
        ld.shape = lc["shape"]
        ld.size = lc["size"]
        ld.energy = lc["energy"]
        lo = bpy.data.objects.new(lc["name"], ld)
        lo.location = Vector(lc["loc"])
        lo.rotation_euler = (Vector(lc["target"]) - lo.location).to_track_quat("-Z", "Y").to_euler()
        reimport_scene.collection.objects.link(lo)
        
    cam_d = bpy.data.cameras.new("__ReCam")
    cam_o = bpy.data.objects.new("__ReCam", cam_d)
    reimport_scene.collection.objects.link(cam_o)
    reimport_scene.camera = cam_o
    
    cam_d.lens_unit = "FOV"
    cam_d.angle = render_cameras["perspective"]["fov"]
    cam_o.location = Vector(render_cameras["perspective"]["loc"])
    cam_o.rotation_euler = (Vector(render_cameras["perspective"]["target"]) - cam_o.location).to_track_quat("-Z", "Y").to_euler()
    
    re_clay = bpy.data.materials.new("__ReClay")
    re_clay.use_nodes = True
    re_clay.node_tree.nodes.get("Principled BSDF").inputs["Base Color"].default_value = (0.75, 0.74, 0.72, 1.0)
    reimport_scene.view_layers[0].material_override = re_clay
    
    reimport_render_path = str(evidence_dir / "reimport_fbx_perspective.png")
    reimport_scene.render.filepath = reimport_render_path
    bpy.ops.render.render(write_still=True, scene=reimport_scene.name)
    print(f"Reimport FBX Rendered: {reimport_render_path}")

    # Step 7: Final Source Verification
    source_blend_final_hash = compute_sha256(source_blend)
    source_fbx_final_hash = compute_sha256(source_fbx)
    assert source_blend_final_hash == expected_source_hash, "SOURCE .BLEND ALTERED!"
    assert source_fbx_final_hash == expected_fbx_hash, "SOURCE .FBX ALTERED!"
    
    # Step 8: Save candidate execution report
    report = {
        "status": "SUCCESS",
        "source": {
            "path": str(source_blend),
            "sha256": source_blend_final_hash,
            "unchanged": True
        },
        "source_fbx": {
            "path": str(source_fbx),
            "sha256": source_fbx_final_hash,
            "unchanged": True
        },
        "candidate": {
            "path": str(candidate_blend),
            "sha256": candidate_blend_hash,
            "version": "v01"
        },
        "candidate_export_fbx": {
            "path": str(candidate_export_fbx),
            "sha256": candidate_fbx_hash
        },
        "mesh_preservation": {
            "roots_unchanged": True,
            "foliage_unchanged": True,
            "trunk_topology_preserved": True,
            "locked_vertices_max_error": max_locked_err,
            "total_triangles": 73888,
            "boundary_edges": 936
        },
        "reimport_verification": {
            "objects": reimport_stats,
            "total_triangles": total_reimport_tris,
            "render": reimport_render_path
        },
        "rendered_files": rendered_candidate_files
    }
    
    (evidence_dir / "candidate_v01_report.json").write_text(json.dumps(report, indent=2, ensure_ascii=False), encoding="utf-8")
    print("\nALL TASKS COMPLETED AND VERIFIED!")

if __name__ == "__main__":
    main()
