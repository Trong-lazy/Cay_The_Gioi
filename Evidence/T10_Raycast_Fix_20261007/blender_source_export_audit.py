import bpy
import os
import json
import hashlib
from datetime import datetime, timezone
from mathutils import Vector

PROJECT_ROOT = r"c:\Users\Asus\foder game 3D\Cay_The_Gioi"
OUT_JSON = os.path.join(PROJECT_ROOT, "Evidence", "T10_Raycast_Fix_20261007", "blender_source_export_audit.json")

MODELS = [
    ("seedling", os.path.join(PROJECT_ROOT, "ArtSource", "Models", "Tree_Seedling_Simple_v01.blend"), os.path.join(PROJECT_ROOT, "ArtSource", "Exports", "Tree_Seedling_Simple_v01.fbx")),
    ("young_tree", os.path.join(PROJECT_ROOT, "ArtSource", "Models", "Tree_Young_Simple_v01.blend"), os.path.join(PROJECT_ROOT, "ArtSource", "Exports", "Tree_Young_Simple_v01.fbx")),
    ("large_tree", os.path.join(PROJECT_ROOT, "ArtSource", "Models", "Tree_Large.blend"), os.path.join(PROJECT_ROOT, "ArtSource", "Exports", "Tree_Large.fbx")),
]


def sha256_file(path):
    h = hashlib.sha256()
    with open(path, "rb") as f:
        while True:
            chunk = f.read(65536)
            if not chunk:
                break
            h.update(chunk)
    return h.hexdigest().upper()


def inspect_current_scene():
    meshes = []
    total_verts = 0
    total_tris = 0
    min_v = [float("inf")] * 3
    max_v = [float("-inf")] * 3

    for obj in sorted(bpy.data.objects, key=lambda o: o.name):
        if obj.type != "MESH":
            continue
        me = obj.data
        me.calc_loop_triangles()
        v_count = len(me.vertices)
        t_count = len(me.loop_triangles)
        total_verts += v_count
        total_tris += t_count
        zero_area = sum(1 for p in me.polygons if p.area <= 1e-8)

        for v in me.vertices:
            wv = obj.matrix_world @ v.co
            for i in range(3):
                if wv[i] < min_v[i]:
                    min_v[i] = wv[i]
                if wv[i] > max_v[i]:
                    max_v[i] = wv[i]

        mats = []
        for slot in obj.material_slots:
            if slot.material:
                m_info = {"name": slot.material.name, "nodes": []}
                if slot.material.use_nodes and slot.material.node_tree:
                    for n in slot.material.node_tree.nodes:
                        if n.type == "MAPPING":
                            m_info["nodes"].append({
                                "type": "MAPPING",
                                "vector_type": n.vector_type,
                                "scale": [round( float(x), 4) for x in n.inputs["Scale"].default_value],
                            })
                        elif n.type == "TEX_IMAGE" and n.image:
                            m_info["nodes"].append({
                                "type": "TEX_IMAGE",
                                "filepath": n.image.filepath,
                                "colorspace": n.image.colorspace_settings.name,
                            })
                mats.append(m_info)
            else:
                mats.append(None)

        meshes.append({
            "name": obj.name,
            "location": [round(float(x), 5) for x in obj.location],
            "rotation_euler": [round(float(x), 5) for x in obj.rotation_euler],
            "scale": [round(float(x), 5) for x in obj.scale],
            "vertices": v_count,
            "triangles": t_count,
            "zero_area_faces": zero_area,
            "uv_layers": [uv.name for uv in me.uv_layers],
            "materials": mats,
        })

    return {
        "mesh_count": len(meshes),
        "total_vertices": total_verts,
        "total_triangles": total_tris,
        "world_bounds_min": [round(x, 5) for x in min_v],
        "world_bounds_max": [round(x, 5) for x in max_v],
        "world_size": [round(max_v[i] - min_v[i], 5) for i in range(3)],
        "meshes": meshes,
    }


def main():
    report = {
        "generated_at_utc": datetime.now(timezone.utc).isoformat(),
        "blender_version": bpy.app.version_string,
        "mode": "read_only_audit",
        "stages": {},
    }

    for stage_id, blend_path, fbx_path in MODELS:
        before_blend_sha = sha256_file(blend_path)
        before_fbx_sha = sha256_file(fbx_path)

        bpy.ops.wm.open_mainfile(filepath=blend_path)
        blend_info = inspect_current_scene()

        bpy.ops.wm.read_homefile(use_empty=True)
        bpy.ops.import_scene.fbx(filepath=fbx_path)
        fbx_info = inspect_current_scene()

        after_blend_sha = sha256_file(blend_path)
        after_fbx_sha = sha256_file(fbx_path)

        report["stages"][stage_id] = {
            "blend_path": blend_path,
            "fbx_path": fbx_path,
            "blend_sha256": before_blend_sha,
            "fbx_sha256": before_fbx_sha,
            "hashes_unchanged_after_audit": (before_blend_sha == after_blend_sha and before_fbx_sha == after_fbx_sha),
            "blend_scene": blend_info,
            "fbx_reimport": fbx_info,
        }

    os.makedirs(os.path.dirname(OUT_JSON), exist_ok=True)
    with open(OUT_JSON, "w", encoding="utf-8") as f:
        json.dump(report, f, indent=2)
    print("Saved audit to", OUT_JSON)


if __name__ == "__main__":
    main()
