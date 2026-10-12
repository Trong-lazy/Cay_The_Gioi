"""Read-only comparison of saved Blender mesh data; never save input files."""
import bpy
import hashlib
import json
import sys
from pathlib import Path


def digest(value):
    return hashlib.sha256(json.dumps(value, separators=(",", ":")).encode()).hexdigest()


def inspect(path):
    before = hashlib.sha256(path.read_bytes()).hexdigest()
    bpy.ops.wm.open_mainfile(filepath=str(path), use_scripts=False)
    result = {}
    for obj in bpy.context.scene.objects:
        if obj.type != "MESH":
            continue
        mesh = obj.data
        mesh.calc_loop_triangles()
        result[obj.name] = {
            "vertices": len(mesh.vertices),
            "triangles": len(mesh.loop_triangles),
            "positions": digest([list(v.co) for v in mesh.vertices]),
            "topology": digest({"edges": [list(e.vertices) for e in mesh.edges],
                                "faces": [list(p.vertices) for p in mesh.polygons]}),
            "uv": digest({u.name: [list(d.uv) for d in u.data] for u in mesh.uv_layers}),
            "corner_normals": digest([list(n.vector) for n in mesh.corner_normals]),
            "matrix_world": [list(row) for row in obj.matrix_world],
            "materials": [m.name if m else None for m in mesh.materials],
            "modifiers": [m.type for m in obj.modifiers],
        }
    return {"path": str(path), "hash_before": before,
            "hash_after": hashlib.sha256(path.read_bytes()).hexdigest(), "meshes": result}


args = sys.argv[sys.argv.index("--") + 1:]
current, baseline = (inspect(Path(p)) for p in args[:2])
checks = {name: {key: data[key] == baseline["meshes"].get(name, {}).get(key)
                 for key in data} for name, data in current["meshes"].items()}
report = {"current": current, "baseline": baseline, "equal_fields": checks,
          "same_mesh_names": set(current["meshes"]) == set(baseline["meshes"])}
Path(args[2]).write_text(json.dumps(report, indent=2), encoding="utf-8")
print(json.dumps({"same_mesh_names": report["same_mesh_names"], "equal_fields": checks}))
