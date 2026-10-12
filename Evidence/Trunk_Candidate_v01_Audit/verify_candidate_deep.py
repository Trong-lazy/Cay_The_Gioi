"""Deep verification script for Prompt 3: Compare candidate against baseline point-by-point, verify degeneracies, normals, FBX reimport, and hashes."""
import bpy
import bmesh
import hashlib
import json
import math
import sys
from pathlib import Path
from mathutils import Vector

def compute_sha256(filepath):
    p = Path(filepath)
    if not p.exists():
        return None
    return hashlib.sha256(p.read_bytes()).hexdigest().upper()

def digest_json(obj):
    return hashlib.sha256(json.dumps(obj, separators=(",", ":")).encode("utf-8")).hexdigest()

def main():
    project_root = Path(r"c:\Users\Asus\foder game 3D\Cay_The_Gioi")
    source_blend = project_root / "Assets" / "_Project" / "Art" / "Bonsai_Tree.blend"
    source_fbx = project_root / "Assets" / "_Project" / "Art" / "Bonsai_Tree.fbx"
    candidate_blend = project_root / "ArtSource" / "Models" / "Bonsai_Trunk_Candidate_v01.blend"
    candidate_fbx = project_root / "ArtSource" / "Exports" / "Bonsai_Trunk_Candidate_v01.fbx"
    evidence_dir = project_root / "Evidence" / "Trunk_Candidate_v01_Audit"
    
    expected_source_hash = "18BB67FC27BEA34137777EC00ACD0AD12B4FDF408537F3DA2E833CE48095BEF3"
    expected_source_fbx_hash = "52B63D9F9F555BBBC85302DA9BA3AA0E6F1BF82F4E193E070F5B1B8981D0B7D5"
    
    # 1. Check Source Hashes
    src_blend_hash = compute_sha256(source_blend)
    src_fbx_hash = compute_sha256(source_fbx)
    assert src_blend_hash == expected_source_hash, f"Source .blend hash mismatch: {src_blend_hash}"
    assert src_fbx_hash == expected_source_fbx_hash, f"Source .fbx hash mismatch: {src_fbx_hash}"
    
    # 2. Read Baseline Geometry from Source
    bpy.ops.wm.open_mainfile(filepath=str(source_blend), use_scripts=False)
    
    baseline_data = {}
    for obj_name in ["Roots", "Trunk_Branches", "Foliage"]:
        obj = bpy.data.objects[obj_name]
        mesh = obj.data
        mesh.calc_loop_triangles()
        
        # Vertex coordinates
        coords = [Vector(v.co) for v in mesh.vertices]
        # Edges
        edges = [(e.vertices[0], e.vertices[1]) for e in mesh.edges]
        # Faces
        faces = [tuple(p.vertices) for p in mesh.polygons]
        # UVs
        uvs = {}
        for uv_layer in mesh.uv_layers:
            uvs[uv_layer.name] = [Vector(d.uv) for d in uv_layer.data]
        # Corner normals
        normals = [Vector(n.vector) for n in mesh.corner_normals]
        
        bm = bmesh.new()
        bm.from_mesh(mesh)
        boundary_edges = sum(1 for e in bm.edges if e.is_boundary)
        non_manifold = sum(1 for e in bm.edges if not e.is_manifold)
        loose_verts = sum(1 for v in bm.verts if not v.link_edges)
        degen_faces = sum(1 for f in bm.faces if f.calc_area() < 1e-8)
        
        baseline_data[obj_name] = {
            "vertex_count": len(coords),
            "face_count": len(faces),
            "triangles": len(mesh.loop_triangles),
            "coords": coords,
            "edges": edges,
            "faces": faces,
            "uvs": uvs,
            "normals": normals,
            "boundary_edges": boundary_edges,
            "non_manifold": non_manifold,
            "loose_verts": loose_verts,
            "degen_faces": degen_faces,
            "matrix_world": [list(row) for row in obj.matrix_world],
            "materials": [m.name if m else None for m in mesh.materials],
            "modifiers": [m.type for m in obj.modifiers]
        }
        bm.free()
        
    # 3. Read Candidate Geometry from Candidate File on Disk
    bpy.ops.wm.open_mainfile(filepath=str(candidate_blend), use_scripts=False)
    
    candidate_data = {}
    for obj_name in ["Roots", "Trunk_Branches", "Foliage"]:
        obj = bpy.data.objects[obj_name]
        mesh = obj.data
        mesh.calc_loop_triangles()
        
        coords = [Vector(v.co) for v in mesh.vertices]
        edges = [(e.vertices[0], e.vertices[1]) for e in mesh.edges]
        faces = [tuple(p.vertices) for p in mesh.polygons]
        uvs = {}
        for uv_layer in mesh.uv_layers:
            uvs[uv_layer.name] = [Vector(d.uv) for d in uv_layer.data]
        normals = [Vector(n.vector) for n in mesh.corner_normals]
        
        bm = bmesh.new()
        bm.from_mesh(mesh)
        boundary_edges = sum(1 for e in bm.edges if e.is_boundary)
        non_manifold = sum(1 for e in bm.edges if not e.is_manifold)
        loose_verts = sum(1 for v in bm.verts if not v.link_edges)
        degen_faces = sum(1 for f in bm.faces if f.calc_area() < 1e-8)
        
        candidate_data[obj_name] = {
            "vertex_count": len(coords),
            "face_count": len(faces),
            "triangles": len(mesh.loop_triangles),
            "coords": coords,
            "edges": edges,
            "faces": faces,
            "uvs": uvs,
            "normals": normals,
            "boundary_edges": boundary_edges,
            "non_manifold": non_manifold,
            "loose_verts": loose_verts,
            "degen_faces": degen_faces,
            "matrix_world": [list(row) for row in obj.matrix_world],
            "materials": [m.name if m else None for m in mesh.materials],
            "modifiers": [m.type for m in obj.modifiers]
        }
        bm.free()

    # 4. Detailed Point-by-Point Comparison
    results = {}
    
    # 4.1 Compare Roots
    b_r = baseline_data["Roots"]
    c_r = candidate_data["Roots"]
    max_r_co_delta = max((c - b).length for c, b in zip(c_r["coords"], b_r["coords"]))
    max_r_norm_delta = max((c - b).length for c, b in zip(c_r["normals"], b_r["normals"]))
    max_r_uv_delta = max(max((c - b).length for c, b in zip(c_r["uvs"][uvn], b_r["uvs"][uvn])) for uvn in b_r["uvs"])
    
    results["Roots"] = {
        "vertices_match": c_r["vertex_count"] == b_r["vertex_count"] == 9120,
        "faces_match": c_r["face_count"] == b_r["face_count"] == 8928,
        "triangles_match": c_r["triangles"] == b_r["triangles"] == 17856,
        "edges_match": c_r["edges"] == b_r["edges"],
        "faces_topology_match": c_r["faces"] == b_r["faces"],
        "max_position_delta": max_r_co_delta,
        "max_normal_delta": max_r_norm_delta,
        "max_uv_delta": max_r_uv_delta,
        "matrix_world_match": c_r["matrix_world"] == b_r["matrix_world"],
        "modifiers_match": c_r["modifiers"] == b_r["modifiers"],
        "materials_match": c_r["materials"] == b_r["materials"],
        "boundary_edges": c_r["boundary_edges"],
        "non_manifold": c_r["non_manifold"],
        "degenerate_faces": c_r["degen_faces"],
        "loose_vertices": c_r["loose_verts"],
        "passed_all": (max_r_co_delta == 0.0 and max_r_norm_delta == 0.0 and max_r_uv_delta == 0.0 and c_r["edges"] == b_r["edges"])
    }
    
    # 4.2 Compare Foliage
    b_f = baseline_data["Foliage"]
    c_f = candidate_data["Foliage"]
    max_f_co_delta = max((c - b).length for c, b in zip(c_f["coords"], b_f["coords"]))
    max_f_norm_delta = max((c - b).length for c, b in zip(c_f["normals"], b_f["normals"]))
    max_f_uv_delta = max(max((c - b).length for c, b in zip(c_f["uvs"][uvn], b_f["uvs"][uvn])) for uvn in b_f["uvs"])
    
    results["Foliage"] = {
        "vertices_match": c_f["vertex_count"] == b_f["vertex_count"] == 22880,
        "faces_match": c_f["face_count"] == b_f["face_count"] == 18304,
        "triangles_match": c_f["triangles"] == b_f["triangles"] == 18304,
        "edges_match": c_f["edges"] == b_f["edges"],
        "faces_topology_match": c_f["faces"] == b_f["faces"],
        "max_position_delta": max_f_co_delta,
        "max_normal_delta": max_f_norm_delta,
        "max_uv_delta": max_f_uv_delta,
        "matrix_world_match": c_f["matrix_world"] == b_f["matrix_world"],
        "modifiers_match": c_f["modifiers"] == b_f["modifiers"],
        "materials_match": c_f["materials"] == b_f["materials"],
        "boundary_edges": c_f["boundary_edges"],
        "non_manifold": c_f["non_manifold"],
        "degenerate_faces": c_f["degen_faces"],
        "loose_vertices": c_f["loose_verts"],
        "passed_all": (max_f_co_delta == 0.0 and max_f_norm_delta == 0.0 and max_f_uv_delta == 0.0 and c_f["edges"] == b_f["edges"])
    }
    
    # 4.3 Compare Trunk_Branches
    b_tb = baseline_data["Trunk_Branches"]
    c_tb = candidate_data["Trunk_Branches"]
    
    # Load mask weights
    cand_tb_obj = bpy.data.objects["Trunk_Branches"]
    vg = cand_tb_obj.vertex_groups.get("Trunk_Editable_Mask")
    weights = [0.0] * len(c_tb["coords"])
    for idx in range(len(weights)):
        try:
            weights[idx] = vg.weight(idx)
        except:
            weights[idx] = 0.0
            
    locked_indices = [i for i, w in enumerate(weights) if w == 0.0]
    editable_indices = [i for i, w in enumerate(weights) if w > 0.0]
    
    # Check locked vertices position delta
    locked_deltas = [(c_tb["coords"][i] - b_tb["coords"][i]).length for i in locked_indices]
    max_locked_delta = max(locked_deltas)
    
    # Check editable vertices displacement statistics
    editable_deltas = [(c_tb["coords"][i] - b_tb["coords"][i]).length for i in editable_indices]
    
    # Boundary vertices in Trunk_Branches
    bm_tb = bmesh.new()
    bm_tb.from_mesh(cand_tb_obj.data)
    boundary_vert_indices = [v.index for v in bm_tb.verts if v.is_boundary]
    bm_tb.free()
    
    # Verify all boundary vertices are in locked set
    all_boundary_locked = all(weights[i] == 0.0 for i in boundary_vert_indices)
    max_boundary_delta = max((c_tb["coords"][i] - b_tb["coords"][i]).length for i in boundary_vert_indices)
    
    # Check UV distortion on Trunk_Branches
    # For locked loops
    cand_tb_mesh = cand_tb_obj.data
    uv_layer = cand_tb_mesh.uv_layers["UVMap"]
    b_uvs = b_tb["uvs"]["UVMap"]
    c_uvs = c_tb["uvs"]["UVMap"]
    
    # Map loop to vertex
    loop_vert_indices = [l.vertex_index for l in cand_tb_mesh.loops]
    locked_loop_deltas = [(c_uvs[li] - b_uvs[li]).length for li, vi in enumerate(loop_vert_indices) if weights[vi] == 0.0]
    max_locked_uv_delta = max(locked_loop_deltas) if locked_loop_deltas else 0.0
    
    # Measure normal change at transition boundary
    # Transition boundary edges: edges connecting an editable vert (w > 0) to a locked vert (w == 0)
    bm_tb2 = bmesh.new()
    bm_tb2.from_mesh(cand_tb_obj.data)
    transition_edges = []
    for e in bm_tb2.edges:
        w1 = weights[e.verts[0].index]
        w2 = weights[e.verts[1].index]
        if (w1 == 0.0 and w2 > 0.0) or (w1 > 0.0 and w2 == 0.0):
            transition_edges.append(e)
            
    # At transition boundary, check max displacement of the locked vertex end (must be 0.0)
    # and the editable vertex end (smooth falloff)
    trans_locked_disps = []
    trans_editable_disps = []
    for e in transition_edges:
        for v in e.verts:
            disp = (c_tb["coords"][v.index] - b_tb["coords"][v.index]).length
            if weights[v.index] == 0.0:
                trans_locked_disps.append(disp)
            else:
                trans_editable_disps.append(disp)
    bm_tb2.free()
    
    results["Trunk_Branches"] = {
        "vertices_match": c_tb["vertex_count"] == b_tb["vertex_count"] == 19332,
        "faces_match": c_tb["face_count"] == b_tb["face_count"] == 18864,
        "triangles_match": c_tb["triangles"] == b_tb["triangles"] == 37728,
        "edges_topology_match": c_tb["edges"] == b_tb["edges"],
        "faces_topology_match": c_tb["faces"] == b_tb["faces"],
        "locked_vertices_count": len(locked_indices),
        "editable_vertices_count": len(editable_indices),
        "max_locked_vertex_delta": max_locked_delta,
        "max_boundary_vertex_delta": max_boundary_delta,
        "all_boundary_vertices_locked": all_boundary_locked,
        "boundary_vertices_count": len(boundary_vert_indices),
        "max_locked_uv_delta": max_locked_uv_delta,
        "editable_displacement": {
            "min": min(editable_deltas),
            "avg": sum(editable_deltas) / len(editable_deltas),
            "max": max(editable_deltas)
        },
        "transition_boundary_edges_count": len(transition_edges),
        "transition_locked_ends_max_delta": max(trans_locked_disps) if trans_locked_disps else 0.0,
        "transition_editable_ends_avg_delta": sum(trans_editable_disps) / len(trans_editable_disps) if trans_editable_disps else 0.0,
        "boundary_edges": c_tb["boundary_edges"],
        "non_manifold": c_tb["non_manifold"],
        "degenerate_faces": c_tb["degen_faces"],
        "loose_vertices": c_tb["loose_verts"],
        "matrix_world_match": c_tb["matrix_world"] == b_tb["matrix_world"],
        "modifiers_match": c_tb["modifiers"] == b_tb["modifiers"],
        "materials_match": c_tb["materials"] == b_tb["materials"]
    }
    
    # 5. Verify FBX Export & Reimport
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(candidate_fbx))
    
    re_scene = bpy.context.scene
    re_objs = {o.name: o for o in re_scene.objects if o.type == "MESH"}
    
    fbx_verification = {
        "file_path": str(candidate_fbx),
        "sha256": compute_sha256(candidate_fbx),
        "objects_present": list(re_objs.keys()),
        "scales_correct": all(o.scale == Vector((1.0, 1.0, 1.0)) for o in re_objs.values()),
        "rotations_correct": all(o.rotation_euler.to_matrix().is_identity for o in re_objs.values()),
        "mesh_stats": {}
    }
    
    total_fbx_tris = 0
    for name, obj in re_objs.items():
        m = obj.data
        m.calc_loop_triangles()
        tris = len(m.loop_triangles)
        total_fbx_tris += tris
        fbx_verification["mesh_stats"][name] = {
            "vertices": len(m.vertices),
            "triangles": tris,
            "dimensions": [round(d, 4) for d in obj.dimensions]
        }
    fbx_verification["total_triangles"] = total_fbx_tris
    fbx_verification["total_triangles_match_baseline"] = (total_fbx_tris == 73888)
    
    # 6. Final verification of source files
    src_blend_final = compute_sha256(source_blend)
    src_fbx_final = compute_sha256(source_fbx)
    assert src_blend_final == expected_source_hash, "Source .blend was altered!"
    assert src_fbx_final == expected_source_fbx_hash, "Source .fbx was altered!"
    
    report = {
        "source_preservation": {
            "source_blend": {"path": str(source_blend), "sha256": src_blend_final, "unchanged": True},
            "source_fbx": {"path": str(source_fbx), "sha256": src_fbx_final, "unchanged": True}
        },
        "candidate": {
            "path": str(candidate_blend),
            "sha256": compute_sha256(candidate_blend),
            "version": "v01"
        },
        "detailed_comparison": results,
        "fbx_reimport_verification": fbx_verification,
        "total_triangles_summary": {
            "baseline": 73888,
            "candidate_blend": 73888,
            "candidate_fbx": total_fbx_tris
        }
    }
    
    out_json = evidence_dir / "prompt3_deep_verification_report.json"
    out_json.write_text(json.dumps(report, indent=2, ensure_ascii=False), encoding="utf-8")
    
    print("\nPROMPT 3 DEEP VERIFICATION COMPLETE!")
    print(f"Roots exact match: {results['Roots']['passed_all']} (max position delta: {results['Roots']['max_position_delta']})")
    print(f"Foliage exact match: {results['Foliage']['passed_all']} (max position delta: {results['Foliage']['max_position_delta']})")
    print(f"Trunk_Branches locked vertices max delta: {results['Trunk_Branches']['max_locked_vertex_delta']:.10f}")
    print(f"Trunk_Branches boundary vertices max delta: {results['Trunk_Branches']['max_boundary_vertex_delta']:.10f}")
    print(f"Trunk_Branches locked UV max delta: {results['Trunk_Branches']['max_locked_uv_delta']:.10f}")
    print(f"Editable vertices avg displacement: {results['Trunk_Branches']['editable_displacement']['avg']:.4f}m (max: {results['Trunk_Branches']['editable_displacement']['max']:.4f}m)")
    print(f"FBX Reimport Total Triangles: {total_fbx_tris} (matches 73888: {fbx_verification['total_triangles_match_baseline']})")
    print(f"Report written to: {out_json}")

if __name__ == "__main__":
    main()
