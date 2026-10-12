import bpy
import hashlib
import json
import math
from pathlib import Path
from mathutils import Vector
import numpy as np

ROOT = Path(r"C:\Users\Asus\foder game 3D\Cay_The_Gioi")
EVIDENCE_DIR = ROOT / "Evidence" / "Material_Fixes_Antigravity_20261006"
AFTER_DIR = EVIDENCE_DIR / "after"
AFTER_DIR.mkdir(parents=True, exist_ok=True)

MODELS = {
    "seedling": "Tree_Seedling_Simple_v01",
    "young": "Tree_Young_Simple_v01",
    "large": "Tree_Large",
    "ancient": "Bonsai_Trunk_Candidate_v01",
}

EXPECTED_LEAF_TEX = {
    "seedling": "Leaf_Seedling_BaseColor.png",
    "young": "Leaf_Young_BaseColor.png",
    "large": "Leaf_Large_BaseColor.png",
    "ancient": "Leaf_Ancient_BaseColor.png",
}


def sha256_file(path: Path) -> str:
    h = hashlib.sha256()
    with open(path, "rb") as f:
        for chunk in iter(lambda: f.read(1 << 20), b""):
            h.update(chunk)
    return h.hexdigest()


def digest(data):
    return hashlib.sha256(json.dumps(data, separators=(',', ':')).encode('utf-8')).hexdigest()


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
        "polygon_topology_hash": digest([list(p.vertices) for p in m.polygons]),
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
        "_positions": positions,
    }


def inspect_material_graph(mat):
    info = {
        "name": mat.name,
        "diffuse_color": [round(float(x), 5) for x in mat.diffuse_color],
        "roughness_prop": round(float(mat.roughness), 5),
        "metallic_prop": round(float(mat.metallic), 5),
        "use_nodes": bool(mat.use_nodes),
        "nodes": [],
        "links": [],
        "principled": {},
        "images": [],
    }
    if not mat.use_nodes or not mat.node_tree:
        return info
    nt = mat.node_tree
    for n in nt.nodes:
        n_entry = {"name": n.name, "type": n.bl_idname}
        if n.bl_idname == "ShaderNodeTexImage":
            img = n.image
            filepath_abs = Path(bpy.path.abspath(img.filepath)) if img and img.filepath else None
            n_entry["image_name"] = img.name if img else None
            n_entry["filepath"] = img.filepath if img else None
            n_entry["exists"] = bool(filepath_abs and filepath_abs.exists()) if filepath_abs else False
            n_entry["colorspace"] = img.colorspace_settings.name if img else None
            n_entry["size"] = list(img.size) if img else None
            info["images"].append(n_entry)
        elif n.bl_idname == "ShaderNodeMapping":
            n_entry["scale"] = [round(float(x), 4) for x in n.inputs["Scale"].default_value]
        elif n.bl_idname == "ShaderNodeNormalMap":
            n_entry["strength"] = round(float(n.inputs["Strength"].default_value), 4)
        elif n.bl_idname == "ShaderNodeBsdfPrincipled":
            info["principled"] = {
                "Base Color": [round(float(x), 5) for x in n.inputs["Base Color"].default_value],
                "Roughness": round(float(n.inputs["Roughness"].default_value), 5),
                "Metallic": round(float(n.inputs["Metallic"].default_value), 5),
                "Specular IOR Level": round(float(n.inputs["Specular IOR Level"].default_value), 5)
                if "Specular IOR Level" in n.inputs else None,
            }
        info["nodes"].append(n_entry)
    for l in nt.links:
        info["links"].append({
            "from_node": l.from_node.name,
            "from_type": l.from_node.bl_idname,
            "from_socket": l.from_socket.name,
            "to_node": l.to_node.name,
            "to_type": l.to_node.bl_idname,
            "to_socket": l.to_socket.name,
        })
    return info


def combined_bounds(objs):
    pts = []
    for o in objs:
        for b in o.bound_box:
            pts.append(o.matrix_world @ Vector(b))
    mins = Vector((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts)))
    maxs = Vector((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts)))
    return mins, maxs


def setup_world_and_lights(scene, center, radius):
    world = bpy.data.worlds.new("AuditWorld")
    scene.world = world
    world.use_nodes = True
    bg = world.node_tree.nodes.get("Background")
    if bg:
        bg.inputs[0].default_value = (0.76, 0.80, 0.84, 1.0)
        bg.inputs[1].default_value = 0.85

    key_data = bpy.data.lights.new("AuditKey", type='SUN')
    key_data.energy = 3.2
    key_data.angle = math.radians(7.5)
    key = bpy.data.objects.new("AuditKey", key_data)
    scene.collection.objects.link(key)
    key.location = center + Vector((radius * 2, -radius * 2, radius * 2.5))
    key.rotation_euler = (math.radians(48), math.radians(8), math.radians(32))

    fill_data = bpy.data.lights.new("AuditFill", type='SUN')
    fill_data.energy = 1.1
    fill_data.angle = math.radians(25.0)
    fill = bpy.data.objects.new("AuditFill", fill_data)
    scene.collection.objects.link(fill)
    fill.location = center + Vector((-radius * 2, radius * 1.5, radius * 1.6))
    fill.rotation_euler = (math.radians(62), 0.0, math.radians(-135))


def render_standard_views(objs, out_dir: Path, views, res=(900, 900)):
    out_dir.mkdir(parents=True, exist_ok=True)
    scene = bpy.context.scene
    for o in list(scene.objects):
        if o.type in {'LIGHT', 'CAMERA'}:
            bpy.data.objects.remove(o, do_unlink=True)
    mins, maxs = combined_bounds(objs)
    center = (mins + maxs) * 0.5
    size = maxs - mins
    radius = max(size.x, size.y, size.z) * 0.5
    setup_world_and_lights(scene, center, radius)

    cam_data = bpy.data.cameras.new("AuditCam")
    cam_data.lens = 50
    cam_data.clip_start = 0.01
    cam_data.clip_end = 200.0
    cam = bpy.data.objects.new("AuditCam", cam_data)
    scene.collection.objects.link(cam)
    scene.camera = cam

    scene.render.engine = 'BLENDER_EEVEE'
    scene.render.resolution_x = res[0]
    scene.render.resolution_y = res[1]
    scene.render.film_transparent = False
    scene.view_settings.view_transform = 'AgX'
    scene.view_settings.look = 'None'

    dist = max(radius / math.tan(math.radians(17.5)), 0.8) * 1.28
    for label, offset, target_override, dist_override in views:
        target = Vector(target_override) if target_override else center
        d = dist_override if dist_override else dist
        vec = Vector(offset).normalized()
        cam.location = target + vec * d
        cam.rotation_euler = (target - cam.location).to_track_quat('-Z', 'Y').to_euler()
        scene.render.filepath = str(out_dir / f"{label}.png")
        bpy.ops.render.render(write_still=True)


def render_lineup(out_path: Path):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene = bpy.context.scene

    # Append objects from each .blend and place along X
    x_positions = {
        "seedling": -5.0,
        "young": -3.2,
        "large": -0.6,
        "ancient": 4.2,
    }
    all_objs = []
    for label, name in MODELS.items():
        blend_path = ROOT / "ArtSource" / "Models" / f"{name}.blend"
        with bpy.data.libraries.load(str(blend_path), link=False) as (data_from, data_to):
            data_to.objects = list(data_from.objects)
        for o in data_to.objects:
            if o is not None and o.type == 'MESH':
                scene.collection.objects.link(o)
                o.location.x += x_positions[label]
                all_objs.append(o)

    bpy.context.view_layer.update()
    mins, maxs = combined_bounds(all_objs)
    center = (mins + maxs) * 0.5
    size = maxs - mins
    radius = max(size.x, size.y, size.z) * 0.5
    setup_world_and_lights(scene, center, radius)

    cam_data = bpy.data.cameras.new("LineupCam")
    cam_data.lens = 45
    cam_data.sensor_fit = 'HORIZONTAL'
    cam_data.clip_start = 0.05
    cam_data.clip_end = 200.0
    cam = bpy.data.objects.new("LineupCam", cam_data)
    scene.collection.objects.link(cam)
    scene.camera = cam

    scene.render.engine = 'BLENDER_EEVEE'
    scene.render.resolution_x = 1920
    scene.render.resolution_y = 1080
    scene.render.film_transparent = False
    scene.view_settings.view_transform = 'AgX'
    scene.view_settings.look = 'None'

    # Compute distance so both width and height fit comfortably with >=20% margin
    aspect = 1920.0 / 1080.0
    hfov = 2.0 * math.atan(cam_data.sensor_width / (2.0 * cam_data.lens))
    vfov = 2.0 * math.atan(math.tan(hfov * 0.5) / aspect)
    dist_w = (size.x * 0.62) / math.tan(hfov * 0.5)
    dist_h = (size.z * 0.68) / math.tan(vfov * 0.5)
    dist = max(dist_w, dist_h) * 1.18

    target = Vector((center.x, center.y, mins.z + size.z * 0.48))
    cam_dir = Vector((0.0, -1.0, 0.14)).normalized()
    cam.location = target + cam_dir * dist
    cam.rotation_euler = (target - cam.location).to_track_quat('-Z', 'Y').to_euler()

    scene.render.filepath = str(out_path)
    bpy.ops.render.render(write_still=True)


def main():
    baseline = json.loads((EVIDENCE_DIR / "baseline_before_fixes.json").read_text(encoding="utf-8"))

    # 1. Verify protected files unchanged
    protected_rel = [
        "Assets/_Project/Art/Bonsai_Tree.blend",
        "Assets/_Project/Art/Bonsai_Tree.fbx",
        "ArtSource/Textures/Tree_Common/Leaf_Common_BaseColor.png",
        "ArtSource/Textures/Tree_Common/Leaf_Common_Normal.png",
    ]
    protected_checks = {}
    for rel in protected_rel:
        actual = sha256_file(ROOT / rel)
        expected = baseline["pre_hashes"][rel]
        protected_checks[rel] = {
            "expected": expected,
            "actual": actual,
            "unchanged": actual == expected,
        }
        assert actual == expected, f"Protected file {rel} was modified!"

    # 2. Measure all textures in Tree_Common
    textures_after = {}
    for p in sorted((ROOT / "ArtSource/Textures/Tree_Common").glob("*.png")):
        im = bpy.data.images.load(str(p), check_existing=False)
        im.colorspace_settings.name = 'Non-Color'
        rgba = np.asarray(im.pixels[:], dtype=np.float32).reshape(im.size[1], im.size[0], 4)
        rgb = rgba[:, :, :3]
        c00 = [int(round(x * 255)) for x in rgb[0, 0, :]]
        tex_entry = {
            "sha256": sha256_file(p),
            "size": list(im.size),
            "corner_00_srgb_u8": c00,
            "corner_00_hex": "#{:02X}{:02X}{:02X}".format(*c00),
            "rgb_min": [round(float(x), 6) for x in rgb.min(axis=(0, 1))],
            "rgb_max": [round(float(x), 6) for x in rgb.max(axis=(0, 1))],
            "rgb_mean": [round(float(x), 6) for x in rgb.mean(axis=(0, 1))],
            "alpha_min": round(float(rgba[:, :, 3].min()), 6),
            "alpha_max": round(float(rgba[:, :, 3].max()), 6),
        }
        if "Normal" in p.name:
            normals = rgb * 2.0 - 1.0
            lengths = np.linalg.norm(normals, axis=2)
            tex_entry.update({
                "normal_length_min": round(float(lengths.min()), 6),
                "normal_length_mean": round(float(lengths.mean()), 6),
                "normal_length_max": round(float(lengths.max()), 6),
                "normal_z_min": round(float(normals[:, :, 2].min()), 6),
            })
        textures_after[p.name] = tex_entry
        bpy.data.images.remove(im)

    # 3. Verify each model (.blend and .fbx) and render after images
    models_after = {}
    for label, name in MODELS.items():
        blend_path = ROOT / "ArtSource" / "Models" / f"{name}.blend"
        fbx_path = ROOT / "ArtSource" / "Exports" / f"{name}.fbx"

        bpy.ops.wm.open_mainfile(filepath=str(blend_path), use_scripts=False)
        objs = [o for o in bpy.context.scene.objects if o.type == 'MESH']
        blend_geom = {o.name: geom(o) for o in sorted(objs, key=lambda x: x.name)}
        blend_mats = {m.name: inspect_material_graph(m) for m in sorted(bpy.data.materials, key=lambda x: x.name)}

        # Compare .blend geometry with pre-fix baseline
        base_meshes = baseline["models"][label]["meshes"]
        geom_checks = {}
        for mname, g_after in blend_geom.items():
            g_before = base_meshes[mname]
            locked_keys = [
                "vertices", "edges", "polygons", "triangles",
                "materials", "uv_order", "world_matrix",
                "position_hash", "topology_hash", "normal_hash",
            ]
            diffs = {k: (g_before[k], g_after[k]) for k in locked_keys if g_before[k] != g_after[k]}
            assert not diffs, f"Locked geometry changed on {label}/{mname}: {diffs}"

            uv_match = (g_before["uv_hashes"] == g_after["uv_hashes"])
            if label == "ancient" and mname in ("Trunk_Branches", "Roots"):
                assert not uv_match, f"Expected Ancient {mname} UV hash to update!"
            else:
                assert uv_match, f"Unexpected UV change on {label}/{mname}: {g_before['uv_hashes']} vs {g_after['uv_hashes']}"

            geom_checks[mname] = {
                "geometry_locked_ok": True,
                "uv_before": g_before["uv_hashes"],
                "uv_after": g_after["uv_hashes"],
                "uv_unchanged": uv_match,
                "degenerate_uv_triangles": g_after["degenerate_uv_triangles"],
            }

        # Verify Leaf material in .blend
        leaf_mat_name = "M_Leaves" if label in ("seedling", "young") else "M_Foliage"
        leaf_mat_info = blend_mats[leaf_mat_name]
        node_types = [n["type"] for n in leaf_mat_info["nodes"]]
        assert "ShaderNodeMix" not in node_types, f"{label} {leaf_mat_name} still has ShaderNodeMix!"
        base_links = [
            l for l in leaf_mat_info["links"]
            if l["to_type"] == "ShaderNodeBsdfPrincipled" and l["to_socket"] == "Base Color"
        ]
        assert len(base_links) == 1 and base_links[0]["from_type"] == "ShaderNodeTexImage", (
            f"{label} {leaf_mat_name} Base Color is not directly driven by ShaderNodeTexImage!"
        )

        # Render .blend views
        views = [("perspective", (1, -1, 0.45), None, None)]
        if label == "ancient":
            views.extend([
                ("front", (0, -1, 0.12), None, None),
                ("back", (0, 1, 0.12), None, None),
                ("left", (-1, 0, 0.12), None, None),
                ("right", (1, 0, 0.12), None, None),
                ("perspective_3_4_front", (0.85, -1.0, 0.28), None, None),
                ("perspective_3_4_back", (-0.85, 1.0, 0.28), None, None),
                ("bark_trunk", (1, -2, 0.25), (0, 0, 1.05), 2.0),
                ("bark_fork", (0.8, -1.8, 0.35), (0.1, 0.0, 1.75), 1.6),
                ("bark_roots", (1.0, -1.6, 0.45), (0.0, -0.2, 0.35), 2.2),
            ])
        render_standard_views(objs, AFTER_DIR / f"{label}_source", views)

        # For ancient, render in-memory normal disabled diagnostic without saving .blend
        if label == "ancient":
            mbark = bpy.data.materials["M_Bark"]
            nmap_node = next(n for n in mbark.node_tree.nodes if n.bl_idname == "ShaderNodeNormalMap")
            orig_strength = nmap_node.inputs["Strength"].default_value
            nmap_node.inputs["Strength"].default_value = 0.0
            render_standard_views(
                objs,
                AFTER_DIR / "ancient_source",
                [
                    ("bark_trunk_normal_disabled", (1, -2, 0.25), (0, 0, 1.05), 2.0),
                    ("bark_fork_normal_disabled", (0.8, -1.8, 0.35), (0.1, 0.0, 1.75), 1.6),
                ],
            )
            nmap_node.inputs["Strength"].default_value = orig_strength

        # Reimport FBX into clean factory scene
        bpy.ops.wm.read_factory_settings(use_empty=True)
        bpy.ops.import_scene.fbx(filepath=str(fbx_path), use_custom_normals=True)
        fbx_objs = [o for o in bpy.context.scene.objects if o.type == 'MESH']
        fbx_geom = {o.name: geom(o) for o in sorted(fbx_objs, key=lambda x: x.name)}
        fbx_mats = {m.name: inspect_material_graph(m) for m in sorted(bpy.data.materials, key=lambda x: x.name)}

        # Compare reimported FBX geometry + UVs with saved .blend
        assert set(blend_geom.keys()) == set(fbx_geom.keys()), f"FBX object mismatch on {label}"
        roundtrip_checks = {}
        for mname in blend_geom:
            for k in ["vertices", "triangles", "polygons", "edges", "materials", "uv_order", "polygon_topology_hash", "uv_hashes"]:
                assert blend_geom[mname][k] == fbx_geom[mname][k], (
                    f"FBX mismatch vs .blend on {label}/{mname}/{k}: {blend_geom[mname][k]} vs {fbx_geom[mname][k]}"
                )
            pos_a = np.asarray(blend_geom[mname]["_positions"], dtype=np.float64)
            pos_b = np.asarray(fbx_geom[mname]["_positions"], dtype=np.float64)
            max_vert_err = float(np.max(np.linalg.norm(pos_a - pos_b, axis=1)))
            assert max_vert_err < 1e-5, f"FBX world vertex error too large on {label}/{mname}: {max_vert_err}"
            wm_err = float(np.max(np.abs(np.asarray(blend_geom[mname]["world_matrix"]) - np.asarray(fbx_geom[mname]["world_matrix"]))))
            assert wm_err < 1e-5, f"FBX world_matrix error too large on {label}/{mname}: {wm_err}"
            roundtrip_checks[mname] = {
                "vertices_match": True,
                "triangles_match": True,
                "polygon_topology_hash_match": True,
                "uv_hashes_match": True,
                "materials_match": True,
                "max_world_vertex_error": max_vert_err,
                "max_world_matrix_error": wm_err,
            }
            blend_geom[mname].pop("_positions", None)
            fbx_geom[mname].pop("_positions", None)

        # Verify reimported FBX leaf material has BaseColor image directly linked
        fbx_leaf_mat = fbx_mats[leaf_mat_name]
        fbx_base_links = [
            l for l in fbx_leaf_mat["links"]
            if l["to_type"] == "ShaderNodeBsdfPrincipled" and l["to_socket"] == "Base Color"
        ]
        assert len(fbx_base_links) == 1 and fbx_base_links[0]["from_type"] == "ShaderNodeTexImage", (
            f"Reimported FBX {label}/{leaf_mat_name} lost Base Color texture!"
        )
        fbx_leaf_imgs = [im["image_name"] for im in fbx_leaf_mat["images"] if im["exists"]]
        assert EXPECTED_LEAF_TEX[label] in fbx_leaf_imgs, (
            f"Reimported FBX {label}/{leaf_mat_name} missing {EXPECTED_LEAF_TEX[label]}: got {fbx_leaf_imgs}"
        )

        # Render raw reimported FBX without touching materials
        fbx_views = [("perspective", (1, -1, 0.45), None, None)]
        if label == "ancient":
            fbx_views.extend([
                ("bark_trunk", (1, -2, 0.25), (0, 0, 1.05), 2.0),
                ("bark_fork", (0.8, -1.8, 0.35), (0.1, 0.0, 1.75), 1.6),
            ])
        render_standard_views(fbx_objs, AFTER_DIR / f"{label}_fbx_raw", fbx_views)

        models_after[label] = {
            "blend_sha256": sha256_file(blend_path),
            "fbx_sha256": sha256_file(fbx_path),
            "blend_changed": sha256_file(blend_path) != baseline["models"][label]["blend_sha256"],
            "fbx_changed": sha256_file(fbx_path) != baseline["models"][label]["fbx_sha256"],
            "total_vertices": sum(g["vertices"] for g in blend_geom.values()),
            "total_triangles": sum(g["triangles"] for g in blend_geom.values()),
            "geometry_and_uv_checks": geom_checks,
            "fbx_roundtrip_checks": roundtrip_checks,
            "blend_materials": blend_mats,
            "fbx_reimported_materials": fbx_mats,
        }

    # 4. Render 4-tree lineup
    render_lineup(AFTER_DIR / "lineup_all_4_trees.png")

    report = {
        "protected_checks": protected_checks,
        "textures_after": textures_after,
        "models_after": models_after,
    }
    out_json = EVIDENCE_DIR / "verification_after_fixes.json"
    out_json.write_text(json.dumps(report, indent=2), encoding="utf-8")
    print("VERIFICATION_AND_RENDERS_COMPLETED_SUCCESSFULLY:", out_json)


if __name__ == "__main__":
    main()
