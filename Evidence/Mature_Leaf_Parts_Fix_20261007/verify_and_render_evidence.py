"""
Independent verification and mandatory evidence renderer for WT_MatureTree (Task T10).
Read-only on all .blend, .fbx, and .png files.
Reopens saved ArtSource/Models/WT_MatureTree.blend and reimports ArtSource/Exports/WT_MatureTree_LOD0..2.fbx
into clean Blender scenes to measure all required metrics and render all required visual evidence.
"""
import bpy
import bmesh
import hashlib
import json
import math
from pathlib import Path
from mathutils import Vector, Matrix
from mathutils.bvhtree import BVHTree
from mathutils.kdtree import KDTree

OUT = Path(__file__).resolve().parent
ROOT = OUT.parents[1]
BACKUP_DIR = OUT / "backup_20261007"


def sha256_file(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def digest(data) -> str:
    return hashlib.sha256(json.dumps(data, separators=(',', ':')).encode()).hexdigest()


def val(x):
    if isinstance(x, (str, float, int, bool)):
        return x
    try:
        return list(x)
    except TypeError:
        return str(x)


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


def geom_metrics(obj):
    m = obj.data
    m.calc_loop_triangles()
    positions = [list(obj.matrix_world @ v.co) for v in m.vertices]
    uv = {u.name: digest([list(d.uv) for d in u.data]) for u in m.uv_layers}
    uv_degenerate = {}
    for u in m.uv_layers:
        count = 0
        for t in m.loop_triangles:
            a, b, c = [u.data[i].uv for i in t.loops]
            if abs((b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x)) < 1e-10:
                count += 1
        uv_degenerate[u.name] = count

    bm = bmesh.new()
    bm.from_mesh(m)
    zero_area = sum(1 for f in bm.faces if f.calc_area() <= 1e-10)
    loose_verts = sum(1 for v in bm.verts if not v.link_edges)
    loose_edges = sum(1 for e in bm.edges if not e.link_faces)
    boundary_edges = sum(1 for e in bm.edges if e.is_boundary)
    non_manifold_non_boundary = sum(1 for e in bm.edges if (not e.is_manifold) and (not e.is_boundary))
    bm.free()

    lo = [min(p[i] for p in positions) for i in range(3)]
    hi = [max(p[i] for p in positions) for i in range(3)]
    return {
        "vertices": len(m.vertices),
        "polygons": len(m.polygons),
        "triangles": len(m.loop_triangles),
        "position_hash": digest([list(v.co) for v in m.vertices]),
        "topology_hash": digest([[list(p.vertices) for p in m.polygons], [list(e.vertices) for e in m.edges]]),
        "normal_hash": digest([list(n.vector) for n in m.corner_normals]),
        "location": [round(float(x), 6) for x in obj.location],
        "rotation_euler": [round(float(x), 6) for x in obj.rotation_euler],
        "scale": [round(float(x), 6) for x in obj.scale],
        "world_matrix": [list(r) for r in obj.matrix_world],
        "uv_hashes": uv,
        "uv_order": [u.name for u in m.uv_layers],
        "degenerate_uv_triangles": uv_degenerate,
        "zero_area_faces": zero_area,
        "loose_vertices": loose_verts,
        "loose_edges": loose_edges,
        "boundary_edges": boundary_edges,
        "non_manifold_non_boundary_edges": non_manifold_non_boundary,
        "materials": [s.material.name if s.material else None for s in obj.material_slots],
        "modifiers": [mod.type for mod in obj.modifiers],
        "collections": [c.name for c in obj.users_collection],
        "bounds": {
            "min": lo,
            "max": hi,
            "size": [hi[i] - lo[i] for i in range(3)],
        },
        "_positions": positions,
    }


def inspect_materials():
    result = {}
    used = {s.material for o in bpy.data.objects if o.type == "MESH" for s in o.material_slots if s.material}
    for m in sorted(used, key=lambda x: x.name):
        nodes = []
        if m.node_tree:
            for n in m.node_tree.nodes:
                item = {
                    "name": n.name,
                    "type": n.type,
                    "inputs": [
                        {
                            "name": i.name,
                            "identifier": i.identifier,
                            "value": val(i.default_value),
                            "linked": i.is_linked,
                        }
                        for i in n.inputs
                        if hasattr(i, "default_value")
                    ],
                }
                if n.type == "TEX_IMAGE" and n.image:
                    img = n.image
                    p = Path(bpy.path.abspath(img.filepath))
                    item.update(
                        image=img.name,
                        filepath=img.filepath,
                        is_relative=img.filepath.startswith("//"),
                        resolved_path=str(p),
                        exists=p.is_file(),
                        size=list(img.size),
                        colorspace=img.colorspace_settings.name,
                        interpolation=n.interpolation,
                        extension=n.extension,
                    )
                if n.type == "NORMAL_MAP":
                    item.update(space=n.space, uv_map=n.uv_map)
                nodes.append(item)
        links = [[l.from_node.name, l.from_socket.identifier, l.to_node.name, l.to_socket.identifier]
                 for l in m.node_tree.links] if m.node_tree else []
        result[m.name] = {
            "diffuse_color": list(m.diffuse_color),
            "backface_culling": m.use_backface_culling,
            "nodes": nodes,
            "links": links,
        }
    return result


def max_vertex_distance(a, b):
    kd = KDTree(len(b))
    for i, p in enumerate(b):
        kd.insert(Vector(p), i)
    kd.balance()
    return max(kd.find(Vector(p))[2] for p in a)


def setup_studio_and_render(objects, directory: Path, views, res=(900, 900), clay_override=False, fixed_bounds=None):
    """
    Renders the specified objects from the given views in an isolated temporary setup.
    """
    directory.mkdir(parents=True, exist_ok=True)
    scene = bpy.context.scene

    # Ensure all collections are visible in viewport/render during render pass
    saved_coll_vis = {}
    for c in bpy.data.collections:
        saved_coll_vis[c.name] = (c.hide_viewport, c.hide_render)
        c.hide_viewport = False
        c.hide_render = False

    # Hide all mesh objects except those in `objects`
    target_set = set(objects)
    saved_obj_vis = {}
    for o in bpy.data.objects:
        if o.type == "MESH":
            saved_obj_vis[o.name] = (o.hide_viewport, o.hide_render)
            vis = o in target_set
            o.hide_viewport = not vis
            o.hide_render = not vis
        elif o.type in {"CAMERA", "LIGHT"}:
            saved_obj_vis[o.name] = (o.hide_viewport, o.hide_render)
            o.hide_viewport = True
            o.hide_render = True

    saved_mats = {}
    clay_mat = None
    if clay_override:
        clay_mat = bpy.data.materials.new("__ClayOverride")
        clay_mat.diffuse_color = (0.46, 0.46, 0.46, 1.0)
        bsdf = clay_mat.node_tree.nodes.get("Principled BSDF")
        bsdf.inputs["Base Color"].default_value = (0.46, 0.46, 0.46, 1.0)
        bsdf.inputs["Roughness"].default_value = 0.78
        for o in objects:
            saved_mats[o.name] = [s.material for s in o.material_slots]
            for s in o.material_slots:
                s.material = clay_mat

    if fixed_bounds is not None:
        lo, hi = fixed_bounds
    else:
        pts = [o.matrix_world @ v.co for o in objects for v in o.data.vertices]
        lo = Vector([min(p[i] for p in pts) for i in range(3)])
        hi = Vector([max(p[i] for p in pts) for i in range(3)])
    center = (lo + hi) * 0.5
    extent = max(hi - lo)

    scene.render.engine = "BLENDER_EEVEE"
    if hasattr(scene.eevee, "taa_render_samples"):
        scene.eevee.taa_render_samples = 128
    scene.render.resolution_x = res[0]
    scene.render.resolution_y = res[1]
    scene.render.resolution_percentage = 100
    scene.render.film_transparent = False
    scene.view_settings.view_transform = "AgX"
    scene.view_settings.exposure = 0.0

    old_world = scene.world
    world = bpy.data.worlds.new("__VerifyWorld")
    world.use_nodes = True
    bg = world.node_tree.nodes["Background"]
    bg.inputs[0].default_value = (0.16, 0.18, 0.20, 1.0)
    bg.inputs[1].default_value = 0.55
    scene.world = world

    temp_lights = []
    for direction, power in [((1, -2, 3), 90), ((-2, -1, 1), 45), ((0, 2, 2), 65), ((0, -1, -2), 30)]:
        ld = bpy.data.lights.new("__VerifyArea", "AREA")
        ld.energy = power * extent * extent
        ld.size = extent * 1.15
        l_obj = bpy.data.objects.new(ld.name, ld)
        scene.collection.objects.link(l_obj)
        l_obj.location = center + Vector(direction) * extent
        l_obj.rotation_euler = (center - l_obj.location).to_track_quat("-Z", "Y").to_euler()
        temp_lights.append((l_obj, ld))

    cd = bpy.data.cameras.new("__VerifyCam")
    cam = bpy.data.objects.new(cd.name, cd)
    scene.collection.objects.link(cam)
    old_cam = scene.camera
    scene.camera = cam
    cd.type = "ORTHO"

    for label, direction, focus, span in views:
        target = center if focus is None else Vector(focus)
        cd.ortho_scale = extent * 1.18 if span is None else span
        cam.location = target + Vector(direction).normalized() * extent * 4.0
        cam.rotation_euler = (target - cam.location).to_track_quat("-Z", "Y").to_euler()
        scene.render.filepath = str(directory / f"{label}.png")
        bpy.ops.render.render(write_still=True)

    # Cleanup temporary objects
    scene.camera = old_cam
    bpy.data.objects.remove(cam, do_unlink=True)
    bpy.data.cameras.remove(cd)
    for l_obj, ld in temp_lights:
        bpy.data.objects.remove(l_obj, do_unlink=True)
        bpy.data.lights.remove(ld)
    scene.world = old_world
    bpy.data.worlds.remove(world)

    if clay_override:
        for o in objects:
            for s, orig_m in zip(o.material_slots, saved_mats[o.name]):
                s.material = orig_m
        bpy.data.materials.remove(clay_mat)

    for name, (hv, hr) in saved_obj_vis.items():
        if name in bpy.data.objects:
            bpy.data.objects[name].hide_viewport = hv
            bpy.data.objects[name].hide_render = hr
    for name, (hv, hr) in saved_coll_vis.items():
        if name in bpy.data.collections:
            bpy.data.collections[name].hide_viewport = hv
            bpy.data.collections[name].hide_render = hr


def full_8_views():
    return [
        ("front", (0.0, -1.0, 0.12), None, None),
        ("back", (0.0, 1.0, 0.12), None, None),
        ("left", (-1.0, 0.0, 0.12), None, None),
        ("right", (1.0, 0.0, 0.12), None, None),
        ("top", (0.0, 0.0, 1.0), None, None),
        ("3quarter_front", (1.0, -1.0, 0.42), None, None),
        ("3quarter_back", (-1.0, 1.0, 0.40), None, None),
        ("low_underside", (0.8, -1.0, -0.32), (0.0, 0.0, 4.8), 8.8),
    ]


def render_before_baseline():
    backup_blend = BACKUP_DIR / "WT_MatureTree.blend"
    bpy.ops.wm.open_mainfile(filepath=str(backup_blend), use_scripts=False)
    objs = [bpy.data.objects[n] for n in ["WT_MatureTree_TrunkAndRoots", "WT_MatureTree_Branches", "WT_MatureTree_Foliage"]]
    setup_studio_and_render(objs, OUT / "before_full_tree", full_8_views())


def render_sprig_comparison():
    """
    Extracts Sprig 0 (vertices 0..39) from Tree_Large.blend::Foliage and Sprig 0 (vertices 0..39)
    from WT_MatureTree.blend::WT_MatureTree_Foliage, normalizes both to unit radius (uniform scale),
    and renders both side-by-side in Solid/Clay and Material Preview under identical lighting.
    """
    bpy.ops.wm.read_factory_settings(use_empty=True)
    with bpy.data.libraries.load(str(ROOT / "ArtSource/Models/Tree_Large.blend"), link=False) as (src, dst):
        dst.objects = ["Foliage"]
    large_fol = dst.objects[0]

    with bpy.data.libraries.load(str(ROOT / "ArtSource/Models/WT_MatureTree.blend"), link=False) as (src2, dst2):
        dst2.objects = ["WT_MatureTree_Foliage"]
    mature_fol = dst2.objects[0]

    def extract_normalized_sprig_obj(source_obj, name, x_offset):
        sm = source_obj.data
        s_uv = sm.uv_layers[0].data
        s_cn = sm.corner_normals
        # First 40 vertices (4 blades * 10 verts) and their polygons
        verts_raw = [Vector(sm.vertices[i].co) for i in range(40)]
        # Normalize using UV landmarks of the 4 blades just like sync_actual_sprigs.py
        blade_lms = []
        for b_idx in range(4):
            b_set = set(range(b_idx * 10, b_idx * 10 + 10))
            pts = {}
            for p in sm.polygons:
                if p.vertices[0] not in b_set:
                    continue
                uvs = [tuple(round(x, 3) for x in s_uv[l].uv) for l in p.loop_indices]
                if len(set(uvs)) < 3:
                    continue
                for l, uv in zip(p.loop_indices, uvs):
                    pts.setdefault(uv, set()).add(sm.loops[l].vertex_index)
            lm = {uv: sum((sm.vertices[i].co for i in inds), Vector()) / len(inds) for uv, inds in pts.items()}
            blade_lms.append(lm)

        anchor = sum((lm[(0.5, 0.0)] for lm in blade_lms), Vector()) / 4.0
        p0 = blade_lms[0]
        y_ax = (p0[(0.5, 1.0)] - p0[(0.5, 0.0)]).normalized()
        x_ax = p0[(1.0, 0.5)] - p0[(0.0, 0.5)]
        x_ax = (x_ax - y_ax * x_ax.dot(y_ax)).normalized()
        z_ax = x_ax.cross(y_ax).normalized()
        basis = Matrix((x_ax, y_ax, z_ax)).transposed()
        inv = basis.transposed()

        local_verts = [inv @ (v - anchor) for v in verts_raw]
        rad = max(v.length for v in local_verts)
        norm_verts = [list((v / rad) + Vector((x_offset, 0.0, 0.0))) for v in local_verts]

        faces = []
        f_uvs = []
        f_nrms = []
        for p in sm.polygons:
            if all(v < 40 for v in p.vertices):
                faces.append(list(p.vertices))
                f_uvs.append([list(s_uv[l].uv) for l in p.loop_indices])
                f_nrms.append([list((inv @ Vector(s_cn[l].vector)).normalized()) for l in p.loop_indices])

        nm = bpy.data.meshes.new(name)
        nm.from_pydata(norm_verts, [], faces)
        nm.update()
        uv_l = nm.uv_layers.new(name="Float2")
        loop_nrms = []
        for p, uv_list, nrm_list in zip(nm.polygons, f_uvs, f_nrms):
            p.use_smooth = True
            for c_idx, l_idx in enumerate(p.loop_indices):
                uv_l.data[l_idx].uv = uv_list[c_idx]
                loop_nrms.append(nrm_list[c_idx])
        nm.materials.append(sm.materials[0])
        nm.normals_split_custom_set(loop_nrms)
        ob = bpy.data.objects.new(name, nm)
        bpy.context.scene.collection.objects.link(ob)
        return ob

    ob_large = extract_normalized_sprig_obj(large_fol, "Ref_TreeLarge_Sprig0", -0.85)
    ob_mature = extract_normalized_sprig_obj(mature_fol, "Mature_Sprig0", 0.85)

    sprig_views = [
        ("sprig_compare_top_front", (0.0, -0.7, 1.0), (0.0, 0.35, 0.0), 3.2),
        ("sprig_compare_side", (1.0, -0.3, 0.35), (0.0, 0.35, 0.0), 3.2),
        ("sprig_compare_underside", (0.0, -0.7, -1.0), (0.0, 0.35, 0.0), 3.2),
    ]
    setup_studio_and_render([ob_large, ob_mature], OUT / "leaf_sprig_comparison" / "material", sprig_views, res=(1200, 750), clay_override=False)
    setup_studio_and_render([ob_large, ob_mature], OUT / "leaf_sprig_comparison" / "solid", sprig_views, res=(1200, 750), clay_override=True)


def render_progression_lineup():
    """
    Renders Tree_Large, WT_MatureTree (LOD0), and Bonsai_Trunk_Candidate_v01 side-by-side
    in the same environment at true scale, plus normalized leaf crop close-ups.
    """
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene = bpy.context.scene

    def load_tree_group(blend_path: Path, obj_names, x_offset):
        with bpy.data.libraries.load(str(blend_path), link=False) as (src, dst):
            dst.objects = obj_names
        loaded = []
        for ob in dst.objects:
            if ob is not None:
                scene.collection.objects.link(ob)
                ob.location.x += x_offset
                loaded.append(ob)
        bpy.context.view_layer.update()
        return loaded

    g_large = load_tree_group(ROOT / "ArtSource/Models/Tree_Large.blend", ["Roots", "Trunk_Branches", "Foliage"], -7.5)
    g_mature = load_tree_group(ROOT / "ArtSource/Models/WT_MatureTree.blend", ["WT_MatureTree_Roots", "WT_MatureTree_Trunk_Branches", "WT_MatureTree_Foliage"], 0.0)
    g_ancient = load_tree_group(ROOT / "ArtSource/Models/Bonsai_Trunk_Candidate_v01.blend", ["Roots", "Trunk_Branches", "Foliage"], 8.5)

    all_trees = g_large + g_mature + g_ancient
    lineup_views = [
        ("lineup_true_scale_front", (0.0, -1.0, 0.12), (0.5, 0.0, 4.2), 23.5),
        ("lineup_true_scale_perspective", (0.65, -1.0, 0.32), (0.5, 0.0, 4.2), 24.5),
    ]
    setup_studio_and_render(all_trees, OUT / "progression_lineup", lineup_views, res=(1600, 900))

    # Normalized canopy leaf crop comparison for each of the 3 trees under identical lighting
    crop_views_large = [("crop_large_canopy", (0.5, -1.0, 0.25), (-7.5, -0.3, 1.55), 1.45)]
    crop_views_mature = [("crop_mature_canopy", (0.5, -1.0, 0.25), (2.2, -0.5, 3.45), 2.65)]
    crop_views_ancient = [("crop_ancient_canopy", (0.5, -1.0, 0.25), (8.5, -0.5, 2.55), 1.85)]
    setup_studio_and_render(all_trees, OUT / "progression_lineup", crop_views_large + crop_views_mature + crop_views_ancient, res=(900, 900))


def main():
    hashes_start = collect_all_hashes()

    # 1. Render Before Baseline (8 views including low_underside and 3quarter_back)
    render_before_baseline()

    # 2. Reopen saved ArtSource/Models/WT_MatureTree.blend and inspect all meshes & materials
    mature_blend = ROOT / "ArtSource/Models/WT_MatureTree.blend"
    bpy.ops.wm.open_mainfile(filepath=str(mature_blend), use_scripts=False)
    for c in bpy.data.collections:
        c.hide_viewport = False
        c.hide_render = False

    saved_meshes = {}
    for o in sorted([ob for ob in bpy.data.objects if ob.type == "MESH"], key=lambda x: x.name):
        saved_meshes[o.name] = geom_metrics(o)
    materials_saved = inspect_materials()

    # Render After Full Tree (LOD0 - 8 views in Material and Clay/Solid)
    lod0_objs = [bpy.data.objects[n] for n in ["WT_MatureTree_Roots", "WT_MatureTree_Trunk_Branches", "WT_MatureTree_Foliage"]]
    pts_lod0 = [o.matrix_world @ v.co for o in lod0_objs for v in o.data.vertices]
    lo_lod0 = Vector([min(p[i] for p in pts_lod0) for i in range(3)])
    hi_lod0 = Vector([max(p[i] for p in pts_lod0) for i in range(3)])
    fixed_lod0_bounds = (lo_lod0, hi_lod0)

    setup_studio_and_render(lod0_objs, OUT / "after_full_tree_material", full_8_views(), fixed_bounds=fixed_lod0_bounds)
    setup_studio_and_render(lod0_objs, OUT / "after_full_tree_solid", full_8_views(), clay_override=True, fixed_bounds=fixed_lod0_bounds)

    # 4. Render Isolated Parts (Roots only, Trunk_Branches only, Foliage only, Wood Assembled) for LOD0, LOD1, LOD2
    for lod in [0, 1, 2]:
        suffix = "" if lod == 0 else f"_LOD{lod}"
        r_ob = bpy.data.objects[f"WT_MatureTree_Roots{suffix}"]
        t_ob = bpy.data.objects[f"WT_MatureTree_Trunk_Branches{suffix}"]
        f_ob = bpy.data.objects[f"WT_MatureTree_Foliage{suffix}"]
        iso_dir = OUT / "isolated_parts" / f"LOD{lod}"
        iso_views = [
            ("perspective", (1.0, -1.0, 0.42), None, None),
            ("front", (0.0, -1.0, 0.12), None, None),
        ]
        setup_studio_and_render([r_ob], iso_dir / "roots_only", iso_views, fixed_bounds=fixed_lod0_bounds)
        setup_studio_and_render([t_ob], iso_dir / "trunk_branches_only", iso_views, fixed_bounds=fixed_lod0_bounds)
        setup_studio_and_render([f_ob], iso_dir / "foliage_only", iso_views, fixed_bounds=fixed_lod0_bounds)
        setup_studio_and_render([r_ob, t_ob], iso_dir / "wood_assembled", iso_views, fixed_bounds=fixed_lod0_bounds)
        setup_studio_and_render([r_ob, t_ob, f_ob], iso_dir / "full_assembled", iso_views, fixed_bounds=fixed_lod0_bounds)

        # Root collar close-up for LOD0, LOD1, LOD2
        collar_views = [
            ("collar_front_closeup", (0.25, -1.0, 0.22), (0.0, 0.0, 0.65), 2.8),
            ("collar_3quarter_closeup", (1.0, -1.0, 0.28), (0.0, 0.0, 0.65), 2.8),
        ]
        setup_studio_and_render([r_ob], iso_dir / "collar_roots_only", collar_views, fixed_bounds=fixed_lod0_bounds)
        setup_studio_and_render([t_ob], iso_dir / "collar_trunk_only", collar_views, fixed_bounds=fixed_lod0_bounds)
        setup_studio_and_render([r_ob, t_ob], iso_dir / "collar_assembled", collar_views, fixed_bounds=fixed_lod0_bounds)

    # 3. Render Sprig Comparison (Tree_Large vs WT_MatureTree at uniform scale)
    render_sprig_comparison()

    # 5. Render Progression Lineup (Tree_Large - WT_MatureTree - Ancient)
    render_progression_lineup()

    # 6. Clean FBX Roundtrip Import & Renders for LOD0, LOD1, LOD2
    fbx_roundtrip = {}
    for lod in [0, 1, 2]:
        fbx_path = ROOT / "ArtSource/Exports" / f"WT_MatureTree_LOD{lod}.fbx"
        bpy.ops.wm.read_factory_settings(use_empty=True)
        bpy.ops.import_scene.fbx(filepath=str(fbx_path), use_custom_normals=True)
        imported_objs = sorted([o for o in bpy.context.scene.objects if o.type == "MESH"], key=lambda x: x.name)
        imported_metrics = {o.name: geom_metrics(o) for o in imported_objs}
        materials_fbx = inspect_materials()

        # Render FBX roundtrip under identical camera/lighting
        fbx_views = [
            ("front", (0.0, -1.0, 0.12), None, None),
            ("3quarter_front", (1.0, -1.0, 0.42), None, None),
            ("3quarter_back", (-1.0, 1.0, 0.40), None, None),
            ("low_underside", (0.8, -1.0, -0.32), (0.0, 0.0, 4.8), 8.8),
        ]
        setup_studio_and_render(imported_objs, OUT / "fbx_roundtrip_renders" / f"LOD{lod}", fbx_views, fixed_bounds=fixed_lod0_bounds)

        # Compare against saved .blend metrics
        correspondence = {}
        for name, imp_m in imported_metrics.items():
            sav_m = saved_meshes[name]
            err = max(max_vertex_distance(sav_m["_positions"], imp_m["_positions"]),
                      max_vertex_distance(imp_m["_positions"], sav_m["_positions"]))
            correspondence[name] = {
                "max_world_vertex_error_m": float(err),
                "vertices_match": sav_m["vertices"] == imp_m["vertices"],
                "polygons_match": sav_m["polygons"] == imp_m["polygons"],
                "triangles_match": sav_m["triangles"] == imp_m["triangles"],
                "uv_hashes_match": sav_m["uv_hashes"] == imp_m["uv_hashes"],
                "material_slots_match": sav_m["materials"] == imp_m["materials"],
                "zero_area_faces": imp_m["zero_area_faces"],
                "degenerate_uv_triangles": imp_m["degenerate_uv_triangles"],
            }
            assert err < 1e-5, f"FBX position mismatch on {name}: {err}"
            assert correspondence[name]["vertices_match"] and correspondence[name]["triangles_match"]
            assert correspondence[name]["uv_hashes_match"] and correspondence[name]["material_slots_match"]

        for item in imported_metrics.values():
            item.pop("_positions", None)

        fbx_roundtrip[f"LOD{lod}"] = {
            "fbx_path": str(fbx_path.relative_to(ROOT)).replace('\\', '/'),
            "fbx_sha256": sha256_file(fbx_path),
            "object_names": list(imported_metrics.keys()),
            "total_vertices": sum(m["vertices"] for m in imported_metrics.values()),
            "total_triangles": sum(m["triangles"] for m in imported_metrics.values()),
            "imported_meshes": imported_metrics,
            "materials_fbx": materials_fbx,
            "correspondence_with_saved_blend": correspondence,
        }

    for item in saved_meshes.values():
        item.pop("_positions", None)

    hashes_end = collect_all_hashes()
    assert hashes_start == hashes_end, "Verification script modified a file!"

    # Load baseline and fix execution summaries
    baseline_data = json.loads((OUT / "baseline_before_fix.json").read_text(encoding="utf-8"))
    fix_data = json.loads((OUT / "fix_execution_summary.json").read_text(encoding="utf-8"))

    full_report = {
        "task": "T10",
        "asset": "WT_MatureTree",
        "review_status": "READY_FOR_REVIEW",
        "blender_version": bpy.app.version_string,
        "hashes_before_fix": baseline_data["hashes_before_fix"],
        "hashes_after_fix": hashes_end,
        "dated_backups": baseline_data["dated_backups"],
        "unreferenced_legacy_textures_kept_on_disk": [
            "ArtSource/Textures/Tree_Common/WT_Leaves_Light_BaseColor.png",
            "ArtSource/Textures/Tree_Common/WT_Leaves_Dark_BaseColor.png",
        ],
        "lod_summary_after_fix": {
            "LOD0": {
                "total_vertices": fbx_roundtrip["LOD0"]["total_vertices"],
                "total_triangles": fbx_roundtrip["LOD0"]["total_triangles"],
                "meshes": {
                    k: {
                        "vertices": saved_meshes[k]["vertices"],
                        "triangles": saved_meshes[k]["triangles"],
                        "materials": saved_meshes[k]["materials"],
                        "bounds": saved_meshes[k]["bounds"],
                    }
                    for k in ["WT_MatureTree_Roots", "WT_MatureTree_Trunk_Branches", "WT_MatureTree_Foliage"]
                },
            },
            "LOD1": {
                "total_vertices": fbx_roundtrip["LOD1"]["total_vertices"],
                "total_triangles": fbx_roundtrip["LOD1"]["total_triangles"],
                "meshes": {
                    k: {
                        "vertices": saved_meshes[k]["vertices"],
                        "triangles": saved_meshes[k]["triangles"],
                        "materials": saved_meshes[k]["materials"],
                        "bounds": saved_meshes[k]["bounds"],
                    }
                    for k in ["WT_MatureTree_Roots_LOD1", "WT_MatureTree_Trunk_Branches_LOD1", "WT_MatureTree_Foliage_LOD1"]
                },
            },
            "LOD2": {
                "total_vertices": fbx_roundtrip["LOD2"]["total_vertices"],
                "total_triangles": fbx_roundtrip["LOD2"]["total_triangles"],
                "meshes": {
                    k: {
                        "vertices": saved_meshes[k]["vertices"],
                        "triangles": saved_meshes[k]["triangles"],
                        "materials": saved_meshes[k]["materials"],
                        "bounds": saved_meshes[k]["bounds"],
                    }
                    for k in ["WT_MatureTree_Roots_LOD2", "WT_MatureTree_Trunk_Branches_LOD2", "WT_MatureTree_Foliage_LOD2"]
                },
            },
        },
        "recommended_anchors": {
            "RootAnchor": {
                "region": "root",
                "description": "Visible root collar / foreground root flare center on WT_MatureTree_Roots",
                "blender_local_z_up_m": [0.0, 0.0, 0.28],
                "unity_imported_y_up_m": [0.0, 0.28, 0.0],
                "foreground_root_option_blender_m": [0.12, -0.95, 0.22],
                "foreground_root_option_unity_m": [0.12, 0.22, 0.95],
            },
            "TrunkAnchor": {
                "region": "trunk",
                "description": "Main muscular trunk mid-section below primary limb split (also serves as Anchor_TrunkAura)",
                "blender_local_z_up_m": [0.08, 0.0, 2.40],
                "unity_imported_y_up_m": [0.08, 2.40, 0.0],
            },
            "LeavesAnchor": {
                "region": "leaves",
                "description": "Volumetric center of the multi-tier foliage canopy (Anchor_LeavesCenter; distinct from upper lightning target)",
                "blender_local_z_up_m": [0.0, 0.0, 5.45],
                "unity_imported_y_up_m": [0.0, 5.45, 0.0],
            },
            "Anchor_LightningTarget": {
                "region": "upper_crown",
                "description": "Upper apex crown target inside top canopy pad (distinct from LeavesAnchor and below max bound 9.20m)",
                "blender_local_z_up_m": [0.0, 0.0, 8.45],
                "unity_imported_y_up_m": [0.0, 8.45, 0.0],
            },
        },
        "wood_partition_verification": fix_data["wood_partition_reports"],
        "foliage_sprig_sync_verification": {
            "reference_sprig_cross_verification_max_error_m": fix_data["reference_sprig_cross_verification_max_error_m"],
            "reference_sprig_templates": fix_data["reference_sprig_templates"],
            "lod_reports": fix_data["foliage_sync_reports"],
        },
        "saved_blend_meshes": saved_meshes,
        "saved_blend_materials": materials_saved,
        "fbx_roundtrip": fbx_roundtrip,
    }

    report_path = OUT / "verification_report.json"
    report_path.write_text(json.dumps(full_report, indent=2), encoding="utf-8")
    print("VERIFICATION_AND_EVIDENCE_COMPLETE", str(report_path))


if __name__ == "__main__":
    main()
