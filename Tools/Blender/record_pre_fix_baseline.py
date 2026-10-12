"""Read-only baseline capture before applying R1, R2, R3 material fixes.
Records:
- SHA256 of all 4 source .blend files, 4 .fbx files, 6 .png textures, and 2 protected Assets/ files.
- Full mesh geometry fingerprints (position_hash, topology_hash, normal_hash, world_matrix, uv_hashes, uv_order, degenerate_uv_triangles, materials, modifiers, bounds).
- Exact leaf Mix node parameters (Factor, Tint A_Color, BaseColor texture, Normal strength, Roughness) from the saved .blend files.
- Before renders of all 4 source .blend files and 4 reimported .fbx files under identical neutral lighting & AgX color management.
"""
import os
import json
import math
import hashlib
import numpy as np
from pathlib import Path
import bpy
from mathutils import Vector

ROOT = Path(r"C:\Users\Asus\foder game 3D\Cay_The_Gioi")
EVIDENCE_DIR = ROOT / "Evidence/Material_Fixes_Antigravity_20261006"
BEFORE_DIR = EVIDENCE_DIR / "before"
BEFORE_DIR.mkdir(parents=True, exist_ok=True)

MODELS = {
    "seedling": "Tree_Seedling_Simple_v01",
    "young": "Tree_Young_Simple_v01",
    "large": "Tree_Large",
    "ancient": "Bonsai_Trunk_Candidate_v01",
}

def sha256_file(p):
    return hashlib.sha256(Path(p).read_bytes()).hexdigest()

def digest(data):
    return hashlib.sha256(json.dumps(data, separators=(',', ':')).encode('utf-8')).hexdigest()

def val(x):
    if isinstance(x, (str, float, int, bool)):
        return x
    try:
        return list(x)
    except TypeError:
        return str(x)

def geom(o):
    m = o.data
    m.calc_loop_triangles()
    positions = [list(o.matrix_world @ v.co) for v in m.vertices]
    uv = {u.name: digest([list(d.uv) for d in u.data]) for u in m.uv_layers}
    uv_degenerate = {}
    for u in m.uv_layers:
        count = 0
        for t in m.loop_triangles:
            a, b, c = [u.data[i].uv for i in t.loops]
            if abs((b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x)) < 1e-10:
                count += 1
        uv_degenerate[u.name] = count
    return {
        "vertices": len(m.vertices),
        "triangles": len(m.loop_triangles),
        "polygons": len(m.polygons),
        "edges": len(m.edges),
        "position_hash": digest([list(v.co) for v in m.vertices]),
        "topology_hash": digest([[list(p.vertices) for p in m.polygons], [list(e.vertices) for e in m.edges]]),
        "normal_hash": digest([list(n.vector) for n in m.corner_normals]),
        "world_matrix": [list(r) for r in o.matrix_world],
        "uv_hashes": uv,
        "uv_order": [u.name for u in m.uv_layers],
        "degenerate_uv_triangles": uv_degenerate,
        "materials": [s.material.name if s.material else None for s in o.material_slots],
        "modifiers": [mod.type for mod in o.modifiers],
        "bounds": {
            "min": [min(p[i] for p in positions) for i in range(3)],
            "max": [max(p[i] for p in positions) for i in range(3)],
        },
    }

def extract_leaf_graph_params(mat):
    nt = mat.node_tree
    bsdf = next(n for n in nt.nodes if n.type == 'BSDF_PRINCIPLED')
    mix = next((n for n in nt.nodes if n.type == 'MIX'), None)
    tex_bc = next((n for n in nt.nodes if n.type == 'TEX_IMAGE' and n.image and 'BaseColor' in n.image.name), None)
    tex_nm = next((n for n in nt.nodes if n.type == 'TEX_IMAGE' and n.image and 'Normal' in n.image.name), None)
    nmap = next((n for n in nt.nodes if n.type == 'NORMAL_MAP'), None)
    return {
        "material_name": mat.name,
        "diffuse_color": list(mat.diffuse_color),
        "roughness": float(bsdf.inputs['Roughness'].default_value),
        "metallic": float(bsdf.inputs['Metallic'].default_value),
        "mix_blend_type": mix.blend_type if mix else None,
        "mix_factor": float(mix.inputs[0].default_value) if mix else None,
        "mix_tint_A_linear": list(mix.inputs[6].default_value) if mix else None,
        "basecolor_image": tex_bc.image.name if tex_bc and tex_bc.image else None,
        "basecolor_colorspace": tex_bc.image.colorspace_settings.name if tex_bc and tex_bc.image else None,
        "normal_image": tex_nm.image.name if tex_nm and tex_nm.image else None,
        "normal_colorspace": tex_nm.image.colorspace_settings.name if tex_nm and tex_nm.image else None,
        "normal_strength": float(nmap.inputs['Strength'].default_value) if nmap else None,
        "normal_space": nmap.space if nmap else None,
        "normal_uv_map": nmap.uv_map if nmap else None,
    }

def render_standard_views(objects, out_dir, views):
    out_dir.mkdir(parents=True, exist_ok=True)
    scene = bpy.context.scene
    for o in list(scene.objects):
        if o.type in {'CAMERA', 'LIGHT'}:
            bpy.data.objects.remove(o, do_unlink=True)
    pts = [o.matrix_world @ v.co for o in objects for v in o.data.vertices]
    lo = Vector([min(p[i] for p in pts) for i in range(3)])
    hi = Vector([max(p[i] for p in pts) for i in range(3)])
    center = (lo + hi) / 2.0
    extent = max(hi - lo)

    scene.render.engine = 'BLENDER_EEVEE'
    if hasattr(scene.eevee, 'taa_render_samples'):
        scene.eevee.taa_render_samples = 64
    scene.render.resolution_x = 900
    scene.render.resolution_y = 900
    scene.render.resolution_percentage = 100
    scene.render.film_transparent = False
    scene.view_settings.view_transform = 'AgX'
    scene.view_settings.look = 'None'
    scene.view_settings.exposure = 0.0
    scene.view_settings.gamma = 1.0

    world = bpy.data.worlds.new('__AuditWorld')
    scene.world = world
    world.use_nodes = True
    bg = world.node_tree.nodes['Background']
    bg.inputs[0].default_value = (0.16, 0.18, 0.20, 1.0)
    bg.inputs[1].default_value = 0.5

    for direction, power in [((1, -2, 3), 90), ((-2, -1, 1), 45), ((0, 2, 2), 65)]:
        data = bpy.data.lights.new('__AuditArea', 'AREA')
        data.energy = power * extent * extent
        data.size = extent * 1.1
        light = bpy.data.objects.new(data.name, data)
        scene.collection.objects.link(light)
        light.location = center + Vector(direction) * extent
        light.rotation_euler = (center - light.location).to_track_quat('-Z', 'Y').to_euler()

    cd = bpy.data.cameras.new('__AuditCam')
    cam = bpy.data.objects.new(cd.name, cd)
    scene.collection.objects.link(cam)
    scene.camera = cam
    cd.type = 'ORTHO'

    for label, direction, focus, span in views:
        target = center if focus is None else Vector(focus)
        cd.ortho_scale = extent * 1.25 if span is None else span
        cam.location = target + Vector(direction).normalized() * extent * 4.0
        cam.rotation_euler = (target - cam.location).to_track_quat('-Z', 'Y').to_euler()
        scene.render.filepath = str(out_dir / f"{label}.png")
        bpy.ops.render.render(write_still=True)

def main():
    tracked_paths = [
        ROOT / "ArtSource" / folder / f"{name}{ext}"
        for name in MODELS.values()
        for folder, ext in [("Models", ".blend"), ("Exports", ".fbx")]
    ]
    tracked_paths += sorted((ROOT / "ArtSource/Textures/Tree_Common").glob("*.png"))
    tracked_paths += [
        ROOT / "Assets/_Project/Art/Bonsai_Tree.blend",
        ROOT / "Assets/_Project/Art/Bonsai_Tree.fbx",
    ]
    pre_hashes = {str(p.relative_to(ROOT)).replace("\\", "/"): sha256_file(p) for p in tracked_paths}

    baseline_data = {
        "pre_hashes": pre_hashes,
        "models": {},
        "textures": {},
    }

    for label, name in MODELS.items():
        blend_path = ROOT / "ArtSource/Models" / f"{name}.blend"
        fbx_path = ROOT / "ArtSource/Exports" / f"{name}.fbx"
        bpy.ops.wm.open_mainfile(filepath=str(blend_path), use_scripts=False)
        objs = [o for o in bpy.context.scene.objects if o.type == 'MESH']
        mesh_geom = {o.name: geom(o) for o in sorted(objs, key=lambda x: x.name)}

        leaf_mat_name = "M_Leaves" if label in ("seedling", "young") else "M_Foliage"
        leaf_params = extract_leaf_graph_params(bpy.data.materials[leaf_mat_name])

        views = [("perspective", (1, -1, 0.45), None, None)]
        if label == "ancient":
            views.extend([
                ("bark_trunk", (1, -2, 0.25), (0, 0, 1.05), 2.0),
                ("bark_fork", (0.8, -1.8, 0.35), (0.1, 0.0, 1.75), 1.6),
                ("bark_roots", (1.0, -1.6, 0.45), (0.0, -0.2, 0.35), 2.2),
            ])
        render_standard_views(objs, BEFORE_DIR / f"{label}_source", views)

        # Render raw FBX import before fix
        bpy.ops.wm.read_factory_settings(use_empty=True)
        bpy.ops.import_scene.fbx(filepath=str(fbx_path), use_custom_normals=True)
        fbx_objs = [o for o in bpy.context.scene.objects if o.type == 'MESH']
        render_standard_views(fbx_objs, BEFORE_DIR / f"{label}_fbx_raw", [("perspective", (1, -1, 0.45), None, None)])

        baseline_data["models"][label] = {
            "blend_sha256": sha256_file(blend_path),
            "fbx_sha256": sha256_file(fbx_path),
            "total_vertices": sum(g["vertices"] for g in mesh_geom.values()),
            "total_triangles": sum(g["triangles"] for g in mesh_geom.values()),
            "meshes": mesh_geom,
            "leaf_graph_before": leaf_params,
        }

    # Measure existing textures
    for p in sorted((ROOT / "ArtSource/Textures/Tree_Common").glob("*.png")):
        im = bpy.data.images.load(str(p), check_existing=False)
        im.colorspace_settings.name = 'Non-Color'
        rgba = np.asarray(im.pixels[:], dtype=np.float32).reshape(im.size[1], im.size[0], 4)
        rgb = rgba[:, :, :3]
        c00 = [int(round(x * 255)) for x in rgb[0, 0, :]]
        baseline_data["textures"][p.name] = {
            "sha256": sha256_file(p),
            "size": list(im.size),
            "corner_00_srgb_u8": c00,
            "corner_00_hex": "#{:02X}{:02X}{:02X}".format(*c00),
            "rgb_min": [round(float(x), 6) for x in rgb.min(axis=(0, 1))],
            "rgb_max": [round(float(x), 6) for x in rgb.max(axis=(0, 1))],
            "rgb_mean": [round(float(x), 6) for x in rgb.mean(axis=(0, 1))],
        }
        bpy.data.images.remove(im)

    # Confirm zero files changed
    post_hashes = {str(p.relative_to(ROOT)).replace("\\", "/"): sha256_file(p) for p in tracked_paths}
    assert pre_hashes == post_hashes, "Read-only baseline script modified a tracked file!"

    out_json = EVIDENCE_DIR / "baseline_before_fixes.json"
    out_json.write_text(json.dumps(baseline_data, indent=2), encoding="utf-8")
    print("BASELINE_RECORDED_SUCCESSFULLY:", out_json)

if __name__ == "__main__":
    main()
