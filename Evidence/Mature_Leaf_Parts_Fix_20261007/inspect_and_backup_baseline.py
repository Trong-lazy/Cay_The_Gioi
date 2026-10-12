"""
Read-only baseline inspection and single dated backup before modifying WT_MatureTree assets.
Records SHA256 hashes of all source/export/texture files, full baseline metrics of WT_MatureTree.blend,
and inspects the exact leaf sprig geometry in Tree_Large.blend (Foliage) and wood islands in WT_MatureTree.blend.
"""
import bpy
import bmesh
import hashlib
import json
import math
import shutil
from pathlib import Path
from mathutils import Vector

OUT = Path(__file__).resolve().parent
ROOT = OUT.parents[1]
BACKUP_DIR = OUT / "backup_20261007"


def sha256_file(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def digest(data) -> str:
    return hashlib.sha256(json.dumps(data, separators=(',', ':')).encode()).hexdigest()


def collect_all_hashes():
    tracked = []
    for folder in [ROOT / "ArtSource/Models", ROOT / "ArtSource/Exports", ROOT / "ArtSource/Textures/Tree_Common"]:
        if folder.exists():
            for p in sorted(folder.iterdir()):
                if p.is_file() and p.suffix.lower() in {'.blend', '.fbx', '.png'}:
                    tracked.append(p)
    for extra in [ROOT / "Assets/_Project/Art/Bonsai_Tree.blend", ROOT / "Assets/_Project/Art/Bonsai_Tree.fbx"]:
        if extra.exists():
            tracked.append(extra)
    return {str(p.relative_to(ROOT)).replace('\\', '/'): sha256_file(p) for p in tracked}


def create_single_dated_backup():
    BACKUP_DIR.mkdir(parents=True, exist_ok=True)
    targets = [
        ROOT / "ArtSource/Models/WT_MatureTree.blend",
        ROOT / "ArtSource/Exports/WT_MatureTree_LOD0.fbx",
        ROOT / "ArtSource/Exports/WT_MatureTree_LOD1.fbx",
        ROOT / "ArtSource/Exports/WT_MatureTree_LOD2.fbx",
    ]
    backup_records = {}
    for src in targets:
        dst = BACKUP_DIR / src.name
        if not dst.exists():
            shutil.copy2(src, dst)
        backup_records[src.name] = {
            "source_path": str(src.relative_to(ROOT)).replace('\\', '/'),
            "backup_path": str(dst.relative_to(ROOT)).replace('\\', '/'),
            "source_sha256": sha256_file(src),
            "backup_sha256": sha256_file(dst),
            "matches": sha256_file(src) == sha256_file(dst),
        }
    return backup_records


def inspect_mesh_summary(obj):
    m = obj.data
    m.calc_loop_triangles()
    positions = [[round(c, 7) for c in (obj.matrix_world @ v.co)] for v in m.vertices]
    uv_hashes = {u.name: digest([[round( d.uv.x, 7), round(d.uv.y, 7)] for d in u.data]) for u in m.uv_layers}
    return {
        "vertices": len(m.vertices),
        "polygons": len(m.polygons),
        "triangles": len(m.loop_triangles),
        "position_hash": digest([[round(c, 7) for c in v.co] for v in m.vertices]),
        "world_position_hash": digest(positions),
        "topology_hash": digest([[list(p.vertices) for p in m.polygons], [list(e.vertices) for e in m.edges]]),
        "normal_hash": digest([[round(c, 6) for c in n.vector] for n in m.corner_normals]),
        "uv_hashes": uv_hashes,
        "uv_layers": [u.name for u in m.uv_layers],
        "materials": [s.material.name if s.material else None for s in obj.material_slots],
        "location": [round(c, 6) for c in obj.location],
        "rotation_euler": [round(c, 6) for c in obj.rotation_euler],
        "scale": [round(c, 6) for c in obj.scale],
        "world_matrix": [[round(c, 6) for c in row] for row in obj.matrix_world],
        "bounds": {
            "min": [min(p[i] for p in positions) for i in range(3)],
            "max": [max(p[i] for p in positions) for i in range(3)],
        },
        "collections": [c.name for c in obj.users_collection],
    }


def analyze_wood_islands(obj):
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    bm.verts.ensure_lookup_table()
    bm.faces.ensure_lookup_table()
    visited = set()
    islands = []
    for v in bm.verts:
        if v.index in visited:
            continue
        comp = []
        stack = [v]
        visited.add(v.index)
        while stack:
            curr = stack.pop()
            comp.append(curr)
            for e in curr.link_edges:
                other = e.other_vert(curr)
                if other.index not in visited:
                    visited.add(other.index)
                    stack.append(other)
        zs = [ (obj.matrix_world @ vert.co).z for vert in comp ]
        rs = [ math.hypot((obj.matrix_world @ vert.co).x, (obj.matrix_world @ vert.co).y) for vert in comp ]
        v_indices = sorted(vert.index for vert in comp)
        faces = [f for f in bm.faces if all(fv.index in set(v_indices) for fv in f.verts)]
        mat_counts = {}
        for f in faces:
            mat_counts[f.material_index] = mat_counts.get(f.material_index, 0) + 1
        islands.append({
            "first_vert": v_indices[0],
            "last_vert": v_indices[-1],
            "num_verts": len(v_indices),
            "num_faces": len(faces),
            "z_min": round(min(zs), 4),
            "z_max": round(max(zs), 4),
            "r_min": round(min(rs), 4),
            "r_max": round(max(rs), 4),
            "material_face_counts": mat_counts,
        })
    bm.free()
    return islands


def inspect_tree_large_foliage():
    blend_path = ROOT / "ArtSource/Models/Tree_Large.blend"
    bpy.ops.wm.open_mainfile(filepath=str(blend_path), use_scripts=False)
    fol = bpy.data.objects["Foliage"]
    m = fol.data
    m.calc_loop_triangles()
    bm = bmesh.new()
    bm.from_mesh(m)
    bm.verts.ensure_lookup_table()
    bm.faces.ensure_lookup_table()
    visited = set()
    components = []
    for v in bm.verts:
        if v.index in visited:
            continue
        comp = []
        stack = [v]
        visited.add(v.index)
        while stack:
            curr = stack.pop()
            comp.append(curr)
            for e in curr.link_edges:
                other = e.other_vert(curr)
                if other.index not in visited:
                    visited.add(other.index)
                    stack.append(other)
        components.append(sorted(vert.index for vert in comp))
    bm.free()

    # Inspect first blade and first 4-blade sprig (vertices 0..39)
    uv_layer = m.uv_layers[0]
    first_blade_verts = components[0]
    blade_coords = [Vector(m.vertices[i].co) for i in first_blade_verts[:5]]
    length = (blade_coords[2] - blade_coords[0]).length
    width = (blade_coords[4] - blade_coords[1]).length
    thickness = (Vector(m.vertices[first_blade_verts[5]].co) - Vector(m.vertices[first_blade_verts[0]].co)).length

    # Midrib fold angle
    v0, v1, v2, v3, v4 = blade_coords
    n_left = (v1 - v0).cross(v2 - v0).normalized()
    n_right = (v2 - v0).cross(v4 - v0).normalized()
    fold_angle_deg = math.degrees(math.acos(max(-1.0, min(1.0, n_left.dot(n_right)))))

    # First sprig (4 blades = vertices 0..39)
    sprig0_verts = list(range(0, 40))
    sprig0_pts = [Vector(m.vertices[i].co) for i in sprig0_verts]
    lo = Vector([min(p[k] for p in sprig0_pts) for k in range(3)])
    hi = Vector([max(p[k] for p in sprig0_pts) for k in range(3)])

    return {
        "object_name": fol.name,
        "total_vertices": len(m.vertices),
        "total_polygons": len(m.polygons),
        "total_triangles": len(m.loop_triangles),
        "num_connected_blade_components": len(components),
        "num_4blade_sprigs": len(components) // 4,
        "verts_per_blade": len(first_blade_verts),
        "first_blade_vert_indices": first_blade_verts,
        "first_sprig_vert_range": [0, 39],
        "sample_blade_metrics": {
            "length_m": round(length, 5),
            "width_m": round(width, 5),
            "length_to_width_ratio": round(length / max(width, 1e-6), 3),
            "thickness_m": round(thickness, 5),
            "midrib_fold_angle_deg": round(fold_angle_deg, 2),
        },
        "sample_sprig_size_m": [round(hi[k] - lo[k], 5) for k in range(3)],
        "uv_layer_name": uv_layer.name,
    }


def main():
    hashes_before = collect_all_hashes()
    backups = create_single_dated_backup()

    # Inspect WT_MatureTree.blend
    mature_path = ROOT / "ArtSource/Models/WT_MatureTree.blend"
    bpy.ops.wm.open_mainfile(filepath=str(mature_path), use_scripts=False)
    mature_meshes = {}
    wood_island_analysis = {}
    for obj in sorted([o for o in bpy.data.objects if o.type == 'MESH'], key=lambda x: x.name):
        mature_meshes[obj.name] = inspect_mesh_summary(obj)
        if "TrunkAndRoots" in obj.name:
            wood_island_analysis[obj.name] = analyze_wood_islands(obj)

    large_foliage_info = inspect_tree_large_foliage()

    hashes_after = collect_all_hashes()
    assert hashes_before == hashes_after, "Read-only inspection modified a tracked file!"

    report = {
        "hashes_before_fix": hashes_before,
        "dated_backups": backups,
        "mature_baseline_meshes": mature_meshes,
        "mature_wood_islands": wood_island_analysis,
        "tree_large_foliage_reference": large_foliage_info,
    }
    out_file = OUT / "baseline_before_fix.json"
    out_file.write_text(json.dumps(report, indent=2), encoding="utf-8")
    print("BASELINE_AND_BACKUP_COMPLETE", str(out_file))


if __name__ == "__main__":
    main()
