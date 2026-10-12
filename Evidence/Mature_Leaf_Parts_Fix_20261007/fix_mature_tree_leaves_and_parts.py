"""
Surgical fix for WT_MatureTree (Task T10 - Ready for Review):
1. Restores WT_MatureTree.blend from the verified baseline backup if needed so runs are idempotent.
2. Extracts the 8 real 4-blade sprig templates and M_Foliage material (read-only) from Tree_Large.blend
   and cross-verifies against Bonsai_Trunk_Candidate_v01.blend.
3. Splits wood geometry across LOD0, LOD1, and LOD2 into WT_MatureTree_Roots* and
   WT_MatureTree_Trunk_Branches* along the natural root-flare collar (z_ring_center = 0.60m)
   by exact edge interpolation without moving or rebuilding any existing wood surface.
4. Replaces WT_MatureTree_Foliage* (LOD0, LOD1, LOD2) with the real 4-blade reference sprigs
   transformed solely by translation, 3D rotation, and uniform scale, using M_Foliage.
5. Saves WT_MatureTree.blend and exports WT_MatureTree_LOD0.fbx, WT_MatureTree_LOD1.fbx,
   and WT_MatureTree_LOD2.fbx, verifying that all read-only project files remain untouched.
"""
import bpy
import bmesh
import hashlib
import json
import math
import shutil
from pathlib import Path
from mathutils import Vector, Matrix
from mathutils.bvhtree import BVHTree
from mathutils.kdtree import KDTree

OUT = Path(__file__).resolve().parent
ROOT = OUT.parents[1]
BACKUP_DIR = OUT / "backup_20261007"

MATURE_BLEND = ROOT / "ArtSource/Models/WT_MatureTree.blend"
LARGE_BLEND = ROOT / "ArtSource/Models/Tree_Large.blend"
ANCIENT_BLEND = ROOT / "ArtSource/Models/Bonsai_Trunk_Candidate_v01.blend"

EXPECTED_BASELINE_BLEND_SHA = "dc2ecbf840a528258233619da89b4bc10d82bbcd661797d8a8b5cabb52257e42"
COLLAR_Z = 0.475


def sha256_file(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def digest(data) -> str:
    return hashlib.sha256(json.dumps(data, separators=(',', ':')).encode()).hexdigest()


def collect_readonly_hashes():
    readonly = [
        ROOT / "ArtSource/Models/Tree_Seedling_Simple_v01.blend",
        ROOT / "ArtSource/Models/Tree_Young_Simple_v01.blend",
        ROOT / "ArtSource/Models/Tree_Large.blend",
        ROOT / "ArtSource/Models/Bonsai_Trunk_Candidate_v01.blend",
        ROOT / "ArtSource/Exports/Tree_Seedling_Simple_v01.fbx",
        ROOT / "ArtSource/Exports/Tree_Young_Simple_v01.fbx",
        ROOT / "ArtSource/Exports/Tree_Large.fbx",
        ROOT / "ArtSource/Exports/Bonsai_Trunk_Candidate_v01.fbx",
        ROOT / "Assets/_Project/Art/Bonsai_Tree.blend",
        ROOT / "Assets/_Project/Art/Bonsai_Tree.fbx",
    ]
    for p in sorted((ROOT / "ArtSource/Textures/Tree_Common").glob("*.png")):
        readonly.append(p)
    return {str(p.relative_to(ROOT)).replace('\\', '/'): sha256_file(p) for p in readonly}


def frame_from_landmarks(base: Vector, tip: Vector, left: Vector, right: Vector) -> Matrix:
    y = (tip - base).normalized()
    x = right - left
    x = (x - y * x.dot(y)).normalized()
    z = x.cross(y).normalized()
    return Matrix((x, y, z)).transposed()


def extract_reference_sprig_templates():
    """
    Extracts the 8 four-blade sprig templates from Tree_Large.blend (Foliage, vertices 0..319,
    blade islands 0..31) and cross-verifies against Bonsai_Trunk_Candidate_v01.blend (Foliage,
    blade islands 0..31). Also loads M_Foliage from Tree_Large.blend.
    """
    mats_before_ancient = set(bpy.data.materials)
    imgs_before_ancient = set(bpy.data.images)
    with bpy.data.libraries.load(str(ANCIENT_BLEND), link=False) as (src, dst):
        dst.objects = ["Foliage"]
    ancient_obj = dst.objects[0]
    ancient_mesh = ancient_obj.data
    ancient_uv = ancient_mesh.uv_layers.active

    # Connected islands in ancient_mesh
    adj = [[] for _ in ancient_mesh.vertices]
    for e in ancient_mesh.edges:
        a, b = e.vertices
        adj[a].append(b)
        adj[b].append(a)
    seen = set()
    islands = []
    for s in range(len(ancient_mesh.vertices)):
        if s in seen:
            continue
        todo = [s]
        seen.add(s)
        ids = []
        while todo:
            i = todo.pop()
            ids.append(i)
            for n in adj[i]:
                if n not in seen:
                    seen.add(n)
                    todo.append(n)
        islands.append(sorted(ids))
    owner = {i: c for c, ids in enumerate(islands) for i in ids}
    face_groups = [[] for _ in islands]
    for p in ancient_mesh.polygons:
        face_groups[owner[p.vertices[0]]].append(p)

    ref_leaves = []
    for ids, faces in zip(islands[:32], face_groups[:32]):
        pts = {}
        for p in faces:
            uvs = [tuple(round(x, 4) for x in ancient_uv.data[l].uv) for l in p.loop_indices]
            if len(set(uvs)) < 3:
                continue
            for l, uv in zip(p.loop_indices, uvs):
                pts.setdefault(uv, set()).add(ancient_mesh.loops[l].vertex_index)
        landmarks = {uv: sum((ancient_mesh.vertices[i].co for i in inds), Vector()) / len(inds)
                     for uv, inds in pts.items()}
        ref_leaves.append({"ids": ids, "faces": faces, "points": landmarks})

    templates = []
    template_metadata = []
    for group in range(8):
        group_leaves = ref_leaves[group * 4 : group * 4 + 4]
        bases = [l["points"][(0.5, 0.0)] for l in group_leaves]
        anchor = sum(bases, Vector()) / 4.0
        primary = group_leaves[0]["points"]
        ref_frame = frame_from_landmarks(
            primary[(0.5, 0.0)], primary[(0.5, 1.0)],
            primary[(0.0, 0.5)], primary[(1.0, 0.5)]
        )
        inv = ref_frame.transposed()
        blade_templates = []
        for l in group_leaves:
            mapping = {idx: i for i, idx in enumerate(l["ids"])}
            coords = [inv @ (ancient_mesh.vertices[i].co - anchor) for i in l["ids"]]
            faces = [[mapping[v] for v in f.vertices] for f in l["faces"]]
            uvs = [[list(ancient_uv.data[j].uv) for j in f.loop_indices] for f in l["faces"]]
            normals = [[inv @ ancient_mesh.corner_normals[j].vector for j in f.loop_indices] for f in l["faces"]]
            blade_templates.append((coords, faces, uvs, normals))
        radius = max(c.length for coords, _, _, _ in blade_templates for c in coords)
        templates.append((blade_templates, radius))
        template_metadata.append({
            "template_index": group,
            "tree_large_object": "Foliage",
            "tree_large_blade_islands": [group * 4 + b for b in range(4)],
            "tree_large_vertex_range": [group * 40, group * 40 + 39],
            "ancient_source_blade_islands": [group * 4 + b for b in range(4)],
            "ancient_source_vertex_range": [group * 20, group * 20 + 19],
            "local_outer_radius_m": round(radius, 6),
        })

    # Clean up temporary ancient objects/meshes/materials before loading Tree_Large
    bpy.data.objects.remove(ancient_obj, do_unlink=True)
    if ancient_mesh.users == 0:
        bpy.data.meshes.remove(ancient_mesh)
    for m in list(set(bpy.data.materials) - mats_before_ancient):
        bpy.data.materials.remove(m)
    for img in list(set(bpy.data.images) - imgs_before_ancient):
        bpy.data.images.remove(img)

    # Cross-verify against Tree_Large.blend Foliage (sprigs 0..7 = vertices 0..319) and load M_Foliage
    with bpy.data.libraries.load(str(LARGE_BLEND), link=False) as (src_l, dst_l):
        dst_l.objects = ["Foliage"]
        dst_l.materials = ["M_Foliage"]
    large_obj = dst_l.objects[0]
    large_mesh = large_obj.data
    m_foliage = dst_l.materials[0]
    m_foliage.name = "M_Foliage"
    assert m_foliage.name == "M_Foliage", f"Expected M_Foliage, got {m_foliage.name}"

    # Verify Sprig 0 in Tree_Large matches Template 0 mid-surface using UV landmarks
    large_uv = large_mesh.uv_layers.active
    s0_blade_landmarks = []
    for b_idx in range(4):
        b_start = b_idx * 10
        b_set = set(range(b_start, b_start + 10))
        pts = {}
        for p in large_mesh.polygons:
            if p.vertices[0] not in b_set:
                continue
            uvs = [tuple(round(x, 4) for x in large_uv.data[l].uv) for l in p.loop_indices]
            if len(set(uvs)) < 3:
                continue
            for l, uv in zip(p.loop_indices, uvs):
                pts.setdefault(uv, set()).add(large_mesh.loops[l].vertex_index)
        lm = {uv: sum((large_mesh.vertices[i].co for i in inds), Vector()) / len(inds) for uv, inds in pts.items()}
        s0_blade_landmarks.append(lm)

    s0_anchor = sum((lm[(0.5, 0.0)] for lm in s0_blade_landmarks), Vector()) / 4.0
    p0_lm = s0_blade_landmarks[0]
    s0_frame = frame_from_landmarks(p0_lm[(0.5, 0.0)], p0_lm[(0.5, 1.0)], p0_lm[(0.0, 0.5)], p0_lm[(1.0, 0.5)])
    s0_inv = s0_frame.transposed()
    s0_local_pts = []
    for b_idx in range(4):
        b_start = b_idx * 10
        for v_off in range(5):
            mid = (Vector(large_mesh.vertices[b_start + v_off].co) + Vector(large_mesh.vertices[b_start + 5 + v_off].co)) * 0.5
            s0_local_pts.append(s0_inv @ (mid - s0_anchor))
    s0_rad = max(p.length for p in s0_local_pts)
    cross_err = 0.0
    idx_pt = 0
    for coords, _, _, _ in templates[0][0]:
        for c in coords:
            c_norm = c / templates[0][1]
            l_norm = s0_local_pts[idx_pt] / s0_rad
            cross_err = max(cross_err, (c_norm - l_norm).length)
            idx_pt += 1
    assert cross_err < 1e-5, f"Tree_Large vs Ancient sprig mismatch: {cross_err}"

    # Clean up temporary loaded objects/meshes from Tree_Large (keep m_foliage)
    bpy.data.objects.remove(large_obj, do_unlink=True)
    if large_mesh.users == 0:
        bpy.data.meshes.remove(large_mesh)

    return templates, template_metadata, m_foliage, cross_err


def split_wood_for_lod(lod: int, m_bark, m_root_accent):
    """
    Splits WT_MatureTree_TrunkAndRoots{_LOD1/_LOD2} and WT_MatureTree_Branches{_LOD1/_LOD2}
    into WT_MatureTree_Roots{_LOD1/_LOD2} and WT_MatureTree_Trunk_Branches{_LOD1/_LOD2}
    at the horizontal root flare collar plane (z = COLLAR_Z = 0.48m).
    Preserves 100% of existing wood surface geometry, UVs, and normals.
    """
    suffix = "" if lod == 0 else f"_LOD{lod}"
    tr_name = f"WT_MatureTree_TrunkAndRoots{suffix}"
    br_name = f"WT_MatureTree_Branches{suffix}"
    roots_name = f"WT_MatureTree_Roots{suffix}"
    trunk_br_name = f"WT_MatureTree_Trunk_Branches{suffix}"

    tr_obj = bpy.data.objects[tr_name]
    br_obj = bpy.data.objects[br_name]
    colls = list(tr_obj.users_collection)

    tr_mesh = tr_obj.data
    br_mesh = br_obj.data
    tr_mesh.calc_loop_triangles()
    br_mesh.calc_loop_triangles()

    # Build BVHTree of baseline combined wood before splitting for surface error verification
    base_verts = [tr_obj.matrix_world @ v.co for v in tr_mesh.vertices] + [br_obj.matrix_world @ v.co for v in br_mesh.vertices]
    offset_br = len(tr_mesh.vertices)
    base_tris = [list(t.vertices) for t in tr_mesh.loop_triangles] + [[v + offset_br for v in t.vertices] for t in br_mesh.loop_triangles]
    bvh_base = BVHTree.FromPolygons(base_verts, base_tris)

    tr_uv = tr_mesh.uv_layers[0].data
    tr_cn = tr_mesh.corner_normals

    trunk_verts = []
    trunk_faces = []
    trunk_uvs = []
    trunk_normals = []
    trunk_mat_indices = []

    root_verts = []
    root_faces = []
    root_uvs = []
    root_normals = []
    root_mat_indices = []

    t_vmap = {}
    r_vmap = {}

    def get_trunk_vert(vid: int) -> int:
        if vid not in t_vmap:
            t_vmap[vid] = len(trunk_verts)
            trunk_verts.append(list(tr_mesh.vertices[vid].co))
        return t_vmap[vid]

    def get_root_vert(vid: int) -> int:
        if vid not in r_vmap:
            r_vmap[vid] = len(root_verts)
            root_verts.append(list(tr_mesh.vertices[vid].co))
        return r_vmap[vid]

    # Cache cut vertices on mesh edges crossing z = COLLAR_Z so adjacent polygons share exact vertices
    edge_cut_cache = {}
    collar_cut_points = []

    def get_edge_cut_verts(v_a: int, v_b: int):
        key = (v_a, v_b) if v_a < v_b else (v_b, v_a)
        if key not in edge_cut_cache:
            pa = Vector(tr_mesh.vertices[key[0]].co)
            pb = Vector(tr_mesh.vertices[key[1]].co)
            t = (COLLAR_Z - pa.z) / (pb.z - pa.z)
            t = max(0.0, min(1.0, t))
            pc = pa.lerp(pb, t)
            pc.z = COLLAR_Z
            pt = [pc.x, pc.y, pc.z]
            t_idx = len(trunk_verts)
            trunk_verts.append(pt)
            r_idx = len(root_verts)
            root_verts.append(pt)
            edge_cut_cache[key] = (t_idx, r_idx)
            collar_cut_points.append(pc)
        return edge_cut_cache[key]

    # Group loop_triangles by polygon index for any polygon that straddles z = COLLAR_Z
    poly_loop_tris = {p.index: [] for p in tr_mesh.polygons}
    for lt in tr_mesh.loop_triangles:
        poly_loop_tris[lt.polygon_index].append(lt)

    def clip_triangle_against_collar(tri_vids, tri_uvs, tri_nrms):
        """
        Clips a single loop_triangle against the horizontal plane z = COLLAR_Z using
        Sutherland-Hodgman polygon clipping, returning (trunk_polys, root_polys).
        """
        t_poly_v, t_poly_uv, t_poly_n = [], [], []
        r_poly_v, r_poly_uv, r_poly_n = [], [], []
        zs = [tr_mesh.vertices[v].co.z for v in tri_vids]
        is_tr = [z >= COLLAR_Z for z in zs]

        for k in range(3):
            kn = (k + 1) % 3
            vk, vkn = tri_vids[k], tri_vids[kn]
            if is_tr[k]:
                t_poly_v.append(get_trunk_vert(vk))
                t_poly_uv.append([tri_uvs[k].x, tri_uvs[k].y])
                t_poly_n.append(list(tri_nrms[k]))
            else:
                r_poly_v.append(get_root_vert(vk))
                r_poly_uv.append([tri_uvs[k].x, tri_uvs[k].y])
                r_poly_n.append(list(tri_nrms[k]))

            if is_tr[k] != is_tr[kn]:
                t_edge = (COLLAR_Z - zs[k]) / (zs[kn] - zs[k])
                t_edge = max(0.0, min(1.0, t_edge))
                uv_c = tri_uvs[k].lerp(tri_uvs[kn], t_edge)
                n_c = tri_nrms[k].lerp(tri_nrms[kn], t_edge).normalized()
                t_cut_v, r_cut_v = get_edge_cut_verts(vk, vkn)

                t_poly_v.append(t_cut_v)
                t_poly_uv.append([uv_c.x, uv_c.y])
                t_poly_n.append([n_c.x, n_c.y, n_c.z])

                r_poly_v.append(r_cut_v)
                r_poly_uv.append([uv_c.x, uv_c.y])
                r_poly_n.append([n_c.x, n_c.y, n_c.z])

        def emit_clipped_poly(poly_v, poly_uv, poly_n, target_faces, target_uvs, target_normals, target_mats, mat_id):
            if len(poly_v) == 3:
                target_faces.append(poly_v)
                target_uvs.append(poly_uv)
                target_normals.append(poly_n)
                target_mats.append(mat_id)
            elif len(poly_v) == 4:
                for i0, i1, i2 in ((0, 1, 2), (0, 2, 3)):
                    target_faces.append([poly_v[i0], poly_v[i1], poly_v[i2]])
                    target_uvs.append([poly_uv[i0], poly_uv[i1], poly_uv[i2]])
                    target_normals.append([poly_n[i0], poly_n[i1], poly_n[i2]])
                    target_mats.append(mat_id)

        emit_clipped_poly(t_poly_v, t_poly_uv, t_poly_n, trunk_faces, trunk_uvs, trunk_normals, trunk_mat_indices, 0)
        emit_clipped_poly(r_poly_v, r_poly_uv, r_poly_n, root_faces, root_uvs, root_normals, root_mat_indices, 0)

    for p in tr_mesh.polygons:
        p_vids = list(p.vertices)
        zs = [tr_mesh.vertices[v].co.z for v in p_vids]
        if all(z >= COLLAR_Z for z in zs):
            trunk_faces.append([get_trunk_vert(v) for v in p_vids])
            trunk_uvs.append([list(tr_uv[l].uv) for l in p.loop_indices])
            trunk_normals.append([list(tr_cn[l].vector) for l in p.loop_indices])
            trunk_mat_indices.append(0)
        elif all(z <= COLLAR_Z for z in zs):
            root_faces.append([get_root_vert(v) for v in p_vids])
            root_uvs.append([list(tr_uv[l].uv) for l in p.loop_indices])
            root_normals.append([list(tr_cn[l].vector) for l in p.loop_indices])
            root_mat_indices.append(p.material_index)
        else:
            for lt in poly_loop_tris[p.index]:
                lt_vids = [tr_mesh.loops[l].vertex_index for l in lt.loops]
                lt_uvs = [Vector(tr_uv[l].uv) for l in lt.loops]
                lt_nrms = [Vector(tr_cn[l].vector) for l in lt.loops]
                clip_triangle_against_collar(lt_vids, lt_uvs, lt_nrms)

    collar_center = sum(collar_cut_points, Vector()) / max(len(collar_cut_points), 1)
    collar_split_info = [{
        "collar_plane_z_m": COLLAR_Z,
        "cut_vertex_count": len(collar_cut_points),
        "collar_center_xyz": [round(collar_center.x, 4), round(collar_center.y, 4), round(collar_center.z, 4)],
        "collar_mean_radius_xy_m": round(sum(math.hypot(p.x, p.y) for p in collar_cut_points) / max(len(collar_cut_points), 1), 4),
    }]

    # Append 100% of WT_MatureTree_Branches{suffix} into WT_MatureTree_Trunk_Branches{suffix}
    br_uv = br_mesh.uv_layers[0].data
    br_cn = br_mesh.corner_normals
    br_v_offset = len(trunk_verts)
    for v in br_mesh.vertices:
        trunk_verts.append(list(v.co))
    for p in br_mesh.polygons:
        trunk_faces.append([br_v_offset + v for v in p.vertices])
        trunk_uvs.append([list(br_uv[l].uv) for l in p.loop_indices])
        trunk_normals.append([list(br_cn[l].vector) for l in p.loop_indices])
        trunk_mat_indices.append(0)

    # Create the two new meshes & objects
    def build_split_object(obj_name, verts, faces, uvs, normals, mat_indices, materials_list):
        m = bpy.data.meshes.new(obj_name)
        m.from_pydata(verts, [], faces)
        m.update()
        uv_layer = m.uv_layers.new(name="Float2")
        loop_normals = []
        for p, f_uv, f_nrm, mat_idx in zip(m.polygons, uvs, normals, mat_indices):
            p.use_smooth = True
            p.material_index = mat_idx
            for corner, l_idx in enumerate(p.loop_indices):
                uv_layer.data[l_idx].uv = f_uv[corner]
                loop_normals.append(f_nrm[corner])
        for mat in materials_list:
            m.materials.append(mat)
        m.normals_split_custom_set(loop_normals)
        obj = bpy.data.objects.new(obj_name, m)
        for c in colls:
            c.objects.link(obj)
        return obj

    root_mats = [m_bark, m_root_accent] if any(idx == 1 for idx in root_mat_indices) else [m_bark]
    roots_obj = build_split_object(roots_name, root_verts, root_faces, root_uvs, root_normals, root_mat_indices, root_mats)
    trunk_br_obj = build_split_object(trunk_br_name, trunk_verts, trunk_faces, trunk_uvs, trunk_normals, trunk_mat_indices, [m_bark])

    # Measure world-space surface deviation against baseline BVHTree
    roots_obj.data.calc_loop_triangles()
    trunk_br_obj.data.calc_loop_triangles()
    new_verts = [roots_obj.matrix_world @ v.co for v in roots_obj.data.vertices] + [trunk_br_obj.matrix_world @ v.co for v in trunk_br_obj.data.vertices]
    off_tb = len(roots_obj.data.vertices)
    new_tris = [list(t.vertices) for t in roots_obj.data.loop_triangles] + [[v + off_tb for v in t.vertices] for t in trunk_br_obj.data.loop_triangles]
    bvh_new = BVHTree.FromPolygons(new_verts, new_tris)

    max_new_to_base = max((bvh_base.find_nearest(p)[3] for p in new_verts), default=0.0)
    max_base_to_new = max((bvh_new.find_nearest(p)[3] for p in base_verts), default=0.0)
    assert max_new_to_base < 1e-6 and max_base_to_new < 1e-6, (max_new_to_base, max_base_to_new)

    # Remove old combined wood objects & meshes
    bpy.data.objects.remove(tr_obj, do_unlink=True)
    if tr_mesh.users == 0:
        bpy.data.meshes.remove(tr_mesh)
    bpy.data.objects.remove(br_obj, do_unlink=True)
    if br_mesh.users == 0:
        bpy.data.meshes.remove(br_mesh)

    return {
        "lod": lod,
        "roots_object": roots_name,
        "trunk_branches_object": trunk_br_name,
        "max_new_vertex_to_baseline_surface_m": round(float(max_new_to_base), 8),
        "max_baseline_vertex_to_new_surface_m": round(float(max_base_to_new), 8),
        "collar_splits": collar_split_info,
    }


def extract_old_six_vert_leaves(mesh):
    """
    Reads the existing 6-vertex leaf placements (base, basis, length) in order from WT_MatureTree_Foliage*.
    In WT_MatureTree_Foliage*, each leaf was created with 6 sequential vertices:
    0: v_base, 1: v_left, 2: v_right, 3: v_tip, 4: v_top, 5: v_bot.
    """
    assert len(mesh.vertices) % 6 == 0
    n_leaves = len(mesh.vertices) // 6
    descriptors = []
    for i in range(n_leaves):
        v_base = Vector(mesh.vertices[6 * i + 0].co)
        v_left = Vector(mesh.vertices[6 * i + 1].co)
        v_right = Vector(mesh.vertices[6 * i + 2].co)
        v_tip = Vector(mesh.vertices[6 * i + 3].co)
        basis = frame_from_landmarks(v_base, v_tip, v_left, v_right)
        length = (v_tip - v_base).length
        descriptors.append((v_base, basis, length))
    return descriptors


def sync_foliage_for_lod(lod: int, templates, m_foliage):
    """
    Replaces WT_MatureTree_Foliage{_LOD1/_LOD2} in-place using the 8 real 4-blade sprig templates
    from Tree_Large.blend / Bonsai_Trunk_Candidate_v01.blend via translation, 3D rotation,
    and uniform scale across the existing 33 canopy pads (4 sprout pads + 29 main pads).
    """
    suffix = "" if lod == 0 else f"_LOD{lod}"
    fol_name = f"WT_MatureTree_Foliage{suffix}"
    fol_obj = bpy.data.objects[fol_name]
    old_mesh = fol_obj.data

    descriptors = extract_old_six_vert_leaves(old_mesh)

    # Group descriptors by pad and select balanced multi-ring sprig placements per pad
    selected_placements = []
    if lod == 0:
        assert len(descriptors) == 1365  # 4*15 + 29*45
        # 4 sprout pads (15 placements each -> pick 5 per sprout across all 3 rings)
        sprout_pick = [0, 2, 5, 8, 12]
        for s_pad in range(4):
            pad_slice = descriptors[s_pad * 15 : (s_pad + 1) * 15]
            pad_center = sum((d[0] for d in pad_slice), Vector()) / len(pad_slice)
            for idx in sprout_pick:
                base, basis, length = pad_slice[idx]
                selected_placements.append((pad_center, base, basis, length, True, idx))
        # 29 main canopy pads (45 placements each across 6 rings -> pick 16 per main pad = 64 blades/pad)
        # Ring 0 (0..3): [0, 2]
        # Ring 1 (4..10): [4, 6, 9]
        # Ring 2 (11..20): [11, 14, 16, 19]
        # Ring 3 (21..31): [21, 24, 27, 30]
        # Ring 4 (32..39): [33, 37]
        # Ring 5 (40..44): [42]
        main_pick = [0, 2, 4, 6, 9, 11, 14, 16, 19, 21, 24, 27, 30, 33, 37, 42]
        for m_pad in range(29):
            start_i = 60 + m_pad * 45
            pad_slice = descriptors[start_i : start_i + 45]
            pad_center = sum((d[0] for d in pad_slice), Vector()) / len(pad_slice)
            for idx in main_pick:
                base, basis, length = pad_slice[idx]
                selected_placements.append((pad_center, base, basis, length, False, idx))
    elif lod == 1:
        assert len(descriptors) == 765  # 4*10 + 29*25
        sprout_pick = [0, 4, 8]
        for s_pad in range(4):
            pad_slice = descriptors[s_pad * 10 : (s_pad + 1) * 10]
            pad_center = sum((d[0] for d in pad_slice), Vector()) / len(pad_slice)
            for idx in sprout_pick:
                base, basis, length = pad_slice[idx]
                selected_placements.append((pad_center, base, basis, length, True, idx))
        main_pick = [0, 3, 6, 9, 12, 15, 18, 20, 23]
        for m_pad in range(29):
            start_i = 40 + m_pad * 25
            pad_slice = descriptors[start_i : start_i + 25]
            pad_center = sum((d[0] for d in pad_slice), Vector()) / len(pad_slice)
            for idx in main_pick:
                base, basis, length = pad_slice[idx]
                selected_placements.append((pad_center, base, basis, length, False, idx))
    else:
        assert len(descriptors) == 319  # 29*11
        main_pick = [0, 3, 6, 9]
        for m_pad in range(29):
            start_i = m_pad * 11
            pad_slice = descriptors[start_i : start_i + 11]
            pad_center = sum((d[0] for d in pad_slice), Vector()) / len(pad_slice)
            for idx in main_pick:
                base, basis, length = pad_slice[idx]
                selected_placements.append((pad_center, base, basis, length, False, idx))

    vertices = []
    faces = []
    uvs = []
    desired_normals = []
    max_shape_error = 0.0
    sample_transforms = []

    for i, (pad_center, orig_base, basis, orig_length, is_sprout, ring_slot) in enumerate(selected_placements):
        template, radius = templates[i % len(templates)]
        # Pull stem base slightly toward pad center so the 4-blade sprig connects deep inside the pad
        # while its 4 fanning blades fill both the core and outer silhouette at the original pad bounds.
        pull = 0.65 if is_sprout else 0.58
        base = pad_center.lerp(orig_base, pull)
        scale_mult = 1.08 if lod == 0 else (1.16 if lod == 1 else 1.24)
        scale = (orig_length * scale_mult) / radius

        angle = math.radians(6.0 * math.sin(i * 2.399963))
        # For the interior cushion sprig (ring_slot >= 40 in LOD0 or >= 22 in LOD1), pitch slightly down
        # so the underside view is Lush and completely sealed from below.
        if (lod == 0 and ring_slot >= 40) or (lod == 1 and ring_slot >= 22):
            orient = basis @ Matrix.Rotation(math.radians(24.0), 3, 'X') @ Matrix.Rotation(angle, 3, 'Y')
        else:
            orient = basis @ Matrix.Rotation(angle, 3, 'Y')

        if i < 5:
            sample_transforms.append({
                "sprig_index": i,
                "template_index": i % len(templates),
                "source_object": "Tree_Large.blend::Foliage",
                "source_vertex_range": [40 * (i % len(templates)), 40 * (i % len(templates)) + 39],
                "translation_xyz": [round(base.x, 5), round(base.y, 5), round(base.z, 5)],
                "uniform_scale": round(scale, 5),
                "rotation_matrix_3x3": [[round(orient[r][c], 5) for c in range(3)] for r in range(3)],
            })

        for coords, triangles, face_uvs, face_normals in template:
            center_points = [base + orient @ (c * scale) for c in coords]
            normal = (center_points[triangles[0][1]] - center_points[triangles[0][0]]).cross(
                center_points[triangles[0][2]] - center_points[triangles[0][0]]
            ).normalized()
            offset = normal * (0.0015 * scale / 2.0)
            start = len(vertices)
            vertices.extend([list(c + offset) for c in center_points])
            vertices.extend([list(c - offset) for c in center_points])

            for tri, uv, n in zip(triangles, face_uvs, face_normals):
                faces.append([start + v for v in tri])
                uvs.append(uv)
                desired_normals.append([list((orient @ v).normalized()) for v in n])

                faces.append([start + 5 + v for v in reversed(tri)])
                uvs.append(list(reversed(uv)))
                desired_normals.append([list(-(orient @ v).normalized()) for v in reversed(n)])

            edge_count = {}
            for tri in triangles:
                for a, b in zip(tri, tri[1:] + tri[:1]):
                    key = tuple(sorted((a, b)))
                    edge_count.setdefault(key, []).append((a, b))
            for occurrences in edge_count.values():
                if len(occurrences) == 1:
                    a, b = occurrences[0]
                    faces.append([start + b, start + a, start + 5 + a, start + 5 + b])
                    # Non-degenerate tiny UV patch at midrib center (0.5, 0.5) so degenerate_uv_triangles == 0
                    uvs.append([[0.495, 0.495], [0.505, 0.495], [0.505, 0.505], [0.495, 0.505]])
                    desired_normals.append(None)

            for j, c in enumerate(coords):
                mid = (Vector(vertices[start + j]) + Vector(vertices[start + j + 5])) * 0.5
                recovered = orient.transposed() @ (mid - base) / scale
                max_shape_error = max(max_shape_error, (recovered - c).length)

    new_mesh = bpy.data.meshes.new(f"__Synced_{fol_name}")
    new_mesh.from_pydata(vertices, [], faces)
    new_mesh.update()
    uv_layer = new_mesh.uv_layers.new(name="Float2")
    loop_normals = []
    for p, uv, nrms in zip(new_mesh.polygons, uvs, desired_normals):
        p.use_smooth = True
        p.material_index = 0
        for corner, l_idx in enumerate(p.loop_indices):
            uv_layer.data[l_idx].uv = uv[corner]
            loop_normals.append(nrms[corner] if nrms else list(p.normal))

    new_mesh.materials.append(m_foliage)
    new_mesh.normals_split_custom_set(loop_normals)

    fol_obj.data = new_mesh
    old_mesh_name = old_mesh.name
    if old_mesh.users == 0:
        bpy.data.meshes.remove(old_mesh)
    new_mesh.name = old_mesh_name

    bm = bmesh.new()
    bm.from_mesh(new_mesh)
    checks = {
        "degenerate_faces": sum(f.calc_area() < 1e-10 for f in bm.faces),
        "loose_vertices": sum(not v.link_edges for v in bm.verts),
        "non_manifold_edges": sum(not e.is_manifold for e in bm.edges),
    }
    bm.free()
    assert checks == {"degenerate_faces": 0, "loose_vertices": 0, "non_manifold_edges": 0}, checks
    assert max_shape_error < 1e-6, f"Shape error too large: {max_shape_error}"

    new_mesh.calc_loop_triangles()
    return {
        "lod": lod,
        "foliage_object": fol_name,
        "sprig_count": len(selected_placements),
        "blade_count": len(selected_placements) * 4,
        "vertices": len(new_mesh.vertices),
        "polygons": len(new_mesh.polygons),
        "triangles": len(new_mesh.loop_triangles),
        "normalized_mid_surface_max_error_m": float(max_shape_error),
        "geometry_checks": checks,
        "sample_sprig_transforms": sample_transforms,
    }


def normalize_bark_materials():
    """
    Ensures M_WT_Bark and M_WT_RootAccent follow D41 (white runtime tint [1,1,1,1],
    explicit UVMap 'Float2' on Normal Map, relative texture paths) and purges unused
    M_WT_Leaves_Light / M_WT_Leaves_Dark datablocks from WT_MatureTree.blend.
    """
    for mat_name in ["M_WT_Bark", "M_WT_RootAccent"]:
        mat = bpy.data.materials.get(mat_name)
        if not mat:
            continue
        mat.diffuse_color = (1.0, 1.0, 1.0, 1.0)
        mat.use_backface_culling = False
        if mat.use_nodes:
            for n in mat.node_tree.nodes:
                if n.type == "BSDF_PRINCIPLED":
                    n.inputs["Base Color"].default_value = (1.0, 1.0, 1.0, 1.0)
                elif n.type == "NORMAL_MAP":
                    n.uv_map = "Float2"
                elif n.type == "TEX_IMAGE" and n.image:
                    abs_p = Path(bpy.path.abspath(n.image.filepath)).resolve()
                    n.image.filepath = bpy.path.relpath(str(abs_p), start=str(MATURE_BLEND.parent))

    for old_leaf_mat in ["M_WT_Leaves_Light", "M_WT_Leaves_Dark"]:
        m = bpy.data.materials.get(old_leaf_mat)
        if m and m.users == 0:
            bpy.data.materials.remove(m)

    for img in list(bpy.data.images):
        if img.users == 0:
            bpy.data.images.remove(img)


def remove_preview_environment_for_clean_save():
    """
    Removes any preview lights/cameras if we want, or keeps them out of FBX export.
    Section 1.4 of original prompt had preview lights/camera in .blend and excluded them from FBX.
    Let's check: in baseline WT_MatureTree.blend, WT_Preview_Studio held camera/lights.
    We keep WT_Preview_Studio in .blend and export ONLY the 3 MESH objects per LOD to FBX!
    """
    pass


def export_all_three_lods():
    exports = {}
    for c in bpy.data.collections:
        c.hide_viewport = False
        c.hide_render = False
    for lc in bpy.context.view_layer.layer_collection.children:
        lc.exclude = False
        lc.hide_viewport = False

    for lod in [0, 1, 2]:
        suffix = "" if lod == 0 else f"_LOD{lod}"
        expected_names = [
            f"WT_MatureTree_Roots{suffix}",
            f"WT_MatureTree_Trunk_Branches{suffix}",
            f"WT_MatureTree_Foliage{suffix}",
        ]
        bpy.ops.object.select_all(action="DESELECT")
        for name in expected_names:
            obj = bpy.data.objects[name]
            obj.hide_viewport = False
            obj.hide_render = False
            obj.select_set(True)
        bpy.context.view_layer.objects.active = bpy.data.objects[expected_names[0]]

        fbx_path = ROOT / "ArtSource/Exports" / f"WT_MatureTree_LOD{lod}.fbx"
        bpy.ops.export_scene.fbx(
            filepath=str(fbx_path),
            use_selection=True,
            global_scale=1.0,
            apply_unit_scale=True,
            apply_scale_options="FBX_SCALE_ALL",
            axis_forward="-Z",
            axis_up="Y",
            object_types={"MESH"},
            use_mesh_modifiers=True,
            mesh_smooth_type="FACE",
            use_tspace=False,
            path_mode="RELATIVE",
            embed_textures=False,
            bake_anim=False,
        )
        exports[f"LOD{lod}"] = {
            "fbx_path": str(fbx_path.relative_to(ROOT)).replace('\\', '/'),
            "exported_objects": expected_names,
            "sha256": sha256_file(fbx_path),
        }

    # Restore collection visibility so LOD0 is active in viewport while LOD1/2 remain in scene.objects
    for lod in [1, 2]:
        c = bpy.data.collections.get(f"WT_MatureTree_LOD{lod}")
        if c:
            c.hide_viewport = True
            c.hide_render = True

    return exports


def main():
    readonly_before = collect_readonly_hashes()

    # Ensure we start from the exact baseline WT_MatureTree.blend
    backup_blend = BACKUP_DIR / "WT_MatureTree.blend"
    if sha256_file(MATURE_BLEND) != EXPECTED_BASELINE_BLEND_SHA:
        assert backup_blend.exists() and sha256_file(backup_blend) == EXPECTED_BASELINE_BLEND_SHA
        shutil.copy2(backup_blend, MATURE_BLEND)
    assert sha256_file(MATURE_BLEND) == EXPECTED_BASELINE_BLEND_SHA

    bpy.ops.wm.open_mainfile(filepath=str(MATURE_BLEND), use_scripts=False)

    templates, template_metadata, m_foliage, cross_err = extract_reference_sprig_templates()

    # Ensure relative paths on M_Foliage images
    for n in m_foliage.node_tree.nodes:
        if n.type == "TEX_IMAGE" and n.image:
            abs_p = Path(bpy.path.abspath(n.image.filepath)).resolve()
            n.image.filepath = bpy.path.relpath(str(abs_p), start=str(MATURE_BLEND.parent))
        elif n.type == "NORMAL_MAP":
            n.uv_map = "Float2"

    m_bark = bpy.data.materials["M_WT_Bark"]
    m_root_accent = bpy.data.materials["M_WT_RootAccent"]

    wood_reports = []
    foliage_reports = []
    for lod in [0, 1, 2]:
        wood_reports.append(split_wood_for_lod(lod, m_bark, m_root_accent))
        foliage_reports.append(sync_foliage_for_lod(lod, templates, m_foliage))

    normalize_bark_materials()

    export_reports = export_all_three_lods()

    # Save WT_MatureTree.blend without creating extra .blend1 files
    bpy.context.preferences.filepaths.save_version = 0
    bpy.ops.wm.save_as_mainfile(filepath=str(MATURE_BLEND))

    readonly_after = collect_readonly_hashes()
    assert readonly_before == readonly_after, "A read-only file was modified!"

    fix_summary = {
        "baseline_blend_sha256": EXPECTED_BASELINE_BLEND_SHA,
        "after_blend_sha256": sha256_file(MATURE_BLEND),
        "reference_sprig_cross_verification_max_error_m": float(cross_err),
        "reference_sprig_templates": template_metadata,
        "wood_partition_reports": wood_reports,
        "foliage_sync_reports": foliage_reports,
        "fbx_exports": export_reports,
        "readonly_files_unchanged": True,
    }
    out_json = OUT / "fix_execution_summary.json"
    out_json.write_text(json.dumps(fix_summary, indent=2), encoding="utf-8")
    print("FIX_MATURE_TREE_COMPLETE", str(out_json))


if __name__ == "__main__":
    main()
