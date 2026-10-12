"""Execute R1 and R2 material/texture/UV fixes, export canonical FBXs, and verify roundtrip.

R1:
- Generate 4 pre-baked/pre-tinted leaf BaseColor PNGs (512x512, sRGB) from Leaf_Common_BaseColor.png
  using the exact linear Mix Multiply formula C_lin = T * ((1 - f) + f * B_lin):
  * Leaf_Seedling_BaseColor.png (f = 0.70, T = [0.3278, 0.5972, 0.0908])
  * Leaf_Young_BaseColor.png    (f = 0.85, T = [0.1779, 0.4020, 0.0561])
  * Leaf_Large_BaseColor.png    (f = 1.00, T = [0.1144, 0.3050, 0.0482])
  * Leaf_Ancient_BaseColor.png  (f = 1.00, T = [0.0802, 0.2016, 0.0395])
- Measure quantitative color accuracy across the entire 512x512 grid and at representative UV points.
- Connect each stage's leaf BaseColor texture directly to Principled BSDF Base Color (removing Mix node),
  with Base Color default = (1,1,1,1) and diffuse_color = (1,1,1,1) to prevent double tinting.

R2:
- Regenerate Ancient_Bark_BaseColor.png (1024x1024, sRGB) and Ancient_Bark_Normal.png (1024x1024, Non-Color)
  from a shared height field with irregular longitudinal bark plates, deep vertical furrows, vertical fibrous
  micro-roughness, and short secondary horizontal fissures (no sinusoidal S-wave bands or stone rings).
- Re-parameterize UVs on Trunk_Branches and Roots of Bonsai_Trunk_Candidate_v01.blend so U wraps seamlessly
  around each tube's circumference (integer tile count k_u) and V runs strictly along each tube's 3D local
  growth curve (arc-length parameterized, base-to-tip, right-handed positive UV determinant on 100% of faces).
- Bake tiling into Ancient bark UVs so M_Bark on Ancient uses Tiling = (1.0, 1.0) directly.
"""

import os
import math
import json
import hashlib
import numpy as np
from pathlib import Path
import bpy
import bmesh
from mathutils import Vector, Euler
from mathutils.kdtree import KDTree

ROOT = Path(r"C:\Users\Asus\foder game 3D\Cay_The_Gioi")
TEXTURES_DIR = ROOT / "ArtSource/Textures/Tree_Common"
MODELS_DIR = ROOT / "ArtSource/Models"
EXPORTS_DIR = ROOT / "ArtSource/Exports"
EVIDENCE_DIR = ROOT / "Evidence/Material_Fixes_Antigravity_20261006"
AFTER_DIR = EVIDENCE_DIR / "after"
AFTER_DIR.mkdir(parents=True, exist_ok=True)

MODELS = {
    "seedling": {
        "name": "Tree_Seedling_Simple_v01",
        "leaf_mat": "M_Leaves",
        "leaf_tex": "Leaf_Seedling_BaseColor.png",
        "uv_name": "UVMap",
        "target_verts": 1436,
        "target_tris": 2778,
    },
    "young": {
        "name": "Tree_Young_Simple_v01",
        "leaf_mat": "M_Leaves",
        "leaf_tex": "Leaf_Young_BaseColor.png",
        "uv_name": "UVMap",
        "target_verts": 2420,
        "target_tris": 4574,
    },
    "large": {
        "name": "Tree_Large",
        "leaf_mat": "M_Foliage",
        "leaf_tex": "Leaf_Large_BaseColor.png",
        "uv_name": "Float2",
        "target_verts": 16632,
        "target_tris": 26682,
    },
    "ancient": {
        "name": "Bonsai_Trunk_Candidate_v01",
        "leaf_mat": "M_Foliage",
        "leaf_tex": "Leaf_Ancient_BaseColor.png",
        "uv_name": "UVMap",
        "target_verts": 51332,
        "target_tris": 73888,
    },
}

def sha256_file(p):
    return hashlib.sha256(Path(p).read_bytes()).hexdigest()

def digest(data):
    return hashlib.sha256(json.dumps(data, separators=(',', ':')).encode('utf-8')).hexdigest()

def srgb_to_linear(c):
    c = np.clip(c, 0.0, 1.0)
    return np.where(c <= 0.04045, c / 12.92, ((c + 0.055) / 1.055) ** 2.4)

def linear_to_srgb(c):
    c = np.clip(c, 0.0, 1.0)
    return np.where(c <= 0.0031308, 12.92 * c, 1.055 * (np.maximum(c, 0.0) ** (1.0 / 2.4)) - 0.055)

def save_rgba_png(img_name, rgba_u8, file_path, w, h):
    img = bpy.data.images.get(img_name)
    if img:
        bpy.data.images.remove(img)
    img = bpy.data.images.new(img_name, width=w, height=h, alpha=True)
    float_pixels = (rgba_u8.astype(np.float32) / 255.0).flatten()
    img.pixels.foreach_set(float_pixels)
    img.filepath_raw = str(file_path)
    img.file_format = 'PNG'
    img.save()
    bpy.data.images.remove(img)

def periodic_box_blur(arr, rx, ry, passes=2):
    res = arr.copy()
    for _ in range(passes):
        if rx > 0:
            pad_x = np.pad(res, ((0, 0), (rx, rx)), mode='wrap')
            cx = np.cumsum(pad_x, axis=1)
            res = (cx[:, 2 * rx:] - cx[:, :-2 * rx]) / (2 * rx + 1)
        if ry > 0:
            pad_y = np.pad(res, ((ry, ry), (0, 0)), mode='wrap')
            cy = np.cumsum(pad_y, axis=0)
            res = (cy[2 * ry:, :] - cy[:-2 * ry, :]) / (2 * ry + 1)
    return res

def generate_r1_leaf_textures(baseline):
    print("\n=== STEP R1: GENERATING 4 PRE-BAKED LEAF BASECOLOR TEXTURES ===")
    common_bc_path = TEXTURES_DIR / "Leaf_Common_BaseColor.png"
    im_common = bpy.data.images.load(str(common_bc_path), check_existing=False)
    im_common.colorspace_settings.name = 'Non-Color'
    w, h = im_common.size[0], im_common.size[1]
    rgba_srgb = np.asarray(im_common.pixels[:], dtype=np.float32).reshape(h, w, 4)
    bpy.data.images.remove(im_common)

    b_srgb = rgba_srgb[:, :, :3]
    b_lin = srgb_to_linear(b_srgb)

    sample_uvs = {
        "stem_base_(0.50,0.00)": (0.50, 0.00),
        "midrib_center_(0.50,0.50)": (0.50, 0.50),
        "secondary_vein_(0.30,0.42)": (0.30, 0.42),
        "blade_lamina_(0.25,0.50)": (0.25, 0.50),
        "leaf_tip_(0.50,0.95)": (0.50, 0.95),
        "corner_bg_(0.00,0.00)": (0.00, 0.00),
    }

    r1_metrics = {}
    for label, mcfg in MODELS.items():
        leaf_before = baseline["models"][label]["leaf_graph_before"]
        f = float(leaf_before["mix_factor"])
        tint_lin = np.array(leaf_before["mix_tint_A_linear"][:3], dtype=np.float32)

        # Exact Mix Multiply formula in linear space: C_lin = T * ((1 - f) + f * B_lin)
        c_lin_exact = tint_lin[None, None, :] * ((1.0 - f) + f * b_lin)
        c_srgb_exact = linear_to_srgb(c_lin_exact)

        rgb_u8 = np.clip(np.round(c_srgb_exact * 255.0), 0, 255).astype(np.uint8)
        alpha_u8 = np.full((h, w, 1), 255, dtype=np.uint8)
        rgba_u8 = np.concatenate([rgb_u8, alpha_u8], axis=2)

        out_path = TEXTURES_DIR / mcfg["leaf_tex"]
        save_rgba_png(mcfg["leaf_tex"], rgba_u8, out_path, w, h)

        # Reload from disk to verify actual PNG quantization & color accuracy
        im_check = bpy.data.images.load(str(out_path), check_existing=False)
        im_check.colorspace_settings.name = 'Non-Color'
        loaded_srgb = np.asarray(im_check.pixels[:], dtype=np.float32).reshape(h, w, 4)[:, :, :3]
        bpy.data.images.remove(im_check)

        loaded_lin = srgb_to_linear(loaded_srgb)
        abs_err_lin = np.abs(loaded_lin - c_lin_exact)
        abs_err_srgb = np.abs(loaded_srgb - c_srgb_exact)

        uv_samples_report = {}
        for sname, (u_val, v_val) in sample_uvs.items():
            px = min(w - 1, max(0, int(round(u_val * (w - 1)))))
            py = min(h - 1, max(0, int(round(v_val * (h - 1)))))
            u8_px = [int(x) for x in rgb_u8[py, px]]
            uv_samples_report[sname] = {
                "uv": [u_val, v_val],
                "pixel_xy": [px, py],
                "formula_linear_rgb": [round(float(x), 6) for x in c_lin_exact[py, px]],
                "png_decoded_linear_rgb": [round(float(x), 6) for x in loaded_lin[py, px]],
                "linear_abs_error": [round(float(x), 6) for x in abs_err_lin[py, px]],
                "png_srgb_u8": u8_px,
                "png_srgb_hex": "#{:02X}{:02X}{:02X}".format(*u8_px),
            }

        r1_metrics[label] = {
            "output_texture": mcfg["leaf_tex"],
            "sha256": sha256_file(out_path),
            "size": [w, h],
            "source_tint_linear": [round(float(x), 6) for x in tint_lin],
            "source_mix_factor": f,
            "max_linear_abs_error": round(float(abs_err_lin.max()), 6),
            "mean_linear_abs_error": round(float(abs_err_lin.mean()), 6),
            "max_srgb_abs_error": round(float(abs_err_srgb.max()), 6),
            "max_srgb_lsb_error": round(float(abs_err_srgb.max() * 255.0), 4),
            "mean_srgb_u8": [round(float(x), 2) for x in rgb_u8.mean(axis=(0, 1))],
            "uv_sample_checks": uv_samples_report,
        }
        print(f"  Generated {mcfg['leaf_tex']}: max_lin_err={r1_metrics[label]['max_linear_abs_error']:.6f}, max_lsb_err={r1_metrics[label]['max_srgb_lsb_error']:.3f} LSB")
    return r1_metrics

def generate_r2_ancient_bark_textures():
    print("\n=== STEP R2A: GENERATING IMPROVED ANCIENT BARK TEXTURES (1024x1024) ===")
    w, h = 1024, 1024
    u = np.linspace(0, 1, w, endpoint=False, dtype=np.float32)
    v = np.linspace(0, 1, h, endpoint=False, dtype=np.float32)
    U, V = np.meshgrid(u, v)

    def norm_field(arr):
        return (arr - arr.mean()) / (arr.std() + 1e-6)

    # 1. Local 2D periodic domain warps at two scales (breaks up smooth oval cells into craggy bark plates)
    np.random.seed(4101)
    warp_u_coarse = norm_field(periodic_box_blur(np.random.randn(h, w).astype(np.float32), rx=26, ry=64, passes=3)) * 0.022
    warp_v_coarse = norm_field(periodic_box_blur(np.random.randn(h, w).astype(np.float32), rx=22, ry=52, passes=3)) * 0.035

    np.random.seed(4102)
    warp_u_med = norm_field(periodic_box_blur(np.random.randn(h, w).astype(np.float32), rx=10, ry=28, passes=3)) * 0.010
    warp_v_med = norm_field(periodic_box_blur(np.random.randn(h, w).astype(np.float32), rx=9, ry=24, passes=3)) * 0.018

    Uw = (U + warp_u_coarse + warp_u_med) % 1.0
    Vw = (V + warp_v_coarse + warp_v_med) % 1.0

    # 2. Primary elongated bark plates (anisotropic Voronoi F1, F2, and per-plate random elevation)
    np.random.seed(4103)
    num_primary = 40
    px = np.random.rand(num_primary).astype(np.float32)
    py = np.random.rand(num_primary).astype(np.float32)
    p_elev = (np.random.rand(num_primary).astype(np.float32) - 0.5) * 0.32

    f1_p = np.full((h, w), 1e9, dtype=np.float32)
    f2_p = np.full((h, w), 1e9, dtype=np.float32)
    plate_id_elev = np.zeros((h, w), dtype=np.float32)

    scale_u_p = 6.4
    scale_v_p = 1.45
    for cx, cy, ce in zip(px, py, p_elev):
        dx = np.abs(Uw - cx)
        dx = np.minimum(dx, 1.0 - dx) * scale_u_p
        dy = np.abs(Vw - cy)
        dy = np.minimum(dy, 1.0 - dy) * scale_v_p
        d = np.sqrt(dx * dx + dy * dy + 1e-4)
        mask1 = d < f1_p
        f2_p = np.where(mask1, f1_p, np.minimum(f2_p, d))
        f1_p = np.where(mask1, d, f1_p)
        plate_id_elev = np.where(mask1, ce, plate_id_elev)

    edge_p = np.clip((f2_p - f1_p) / 0.26, 0.0, 1.0)
    plate_primary = np.power(edge_p, 0.55) * (0.82 + plate_id_elev)
    plate_id_smooth = periodic_box_blur(plate_id_elev, rx=4, ry=8, passes=2)

    # 3. Secondary medium bark plates & fissures
    np.random.seed(4104)
    num_sec = 78
    sx = np.random.rand(num_sec).astype(np.float32)
    sy = np.random.rand(num_sec).astype(np.float32)
    f1_s = np.full((h, w), 1e9, dtype=np.float32)
    f2_s = np.full((h, w), 1e9, dtype=np.float32)
    scale_u_s = 9.8
    scale_v_s = 2.35
    for cx, cy in zip(sx, sy):
        dx = np.abs(Uw - cx)
        dx = np.minimum(dx, 1.0 - dx) * scale_u_s
        dy = np.abs(Vw - cy)
        dy = np.minimum(dy, 1.0 - dy) * scale_v_s
        d = np.sqrt(dx * dx + dy * dy + 1e-4)
        mask1 = d < f1_s
        f2_s = np.where(mask1, f1_s, np.minimum(f2_s, d))
        f1_s = np.where(mask1, d, f1_s)

    edge_s = np.clip((f2_s - f1_s) / 0.22, 0.0, 1.0)
    plate_secondary = np.power(edge_s, 0.65)

    # 4. Multi-scale ridged longitudinal bark crevices (non-sinusoidal ridged multifractal)
    np.random.seed(4105)
    n_macro = norm_field(periodic_box_blur(np.random.randn(h, w).astype(np.float32), rx=14, ry=68, passes=3))
    ridge_macro = np.power(np.clip(1.0 - np.abs(n_macro) * 0.85, 0.0, 1.0), 1.6)

    np.random.seed(4106)
    n_mid = norm_field(periodic_box_blur(np.random.randn(h, w).astype(np.float32), rx=6, ry=32, passes=3))
    ridge_mid = np.power(np.clip(1.0 - np.abs(n_mid) * 0.90, 0.0, 1.0), 1.7)

    np.random.seed(4107)
    n_wood = norm_field(periodic_box_blur(np.random.randn(h, w).astype(np.float32), rx=3, ry=15, passes=2))
    n_micro = norm_field(periodic_box_blur(np.random.randn(h, w).astype(np.float32), rx=2, ry=7, passes=2))
    wood_grain = np.clip(0.18 * n_wood + 0.08 * n_micro, -0.45, 0.45)

    # 5. Short secondary horizontal plate cracks (strictly local across raised plates)
    np.random.seed(4108)
    n_horiz = norm_field(periodic_box_blur(np.random.randn(h, w).astype(np.float32), rx=14, ry=2, passes=2))
    n_horiz_mask = norm_field(periodic_box_blur(np.random.randn(h, w).astype(np.float32), rx=20, ry=24, passes=3))
    horiz_cracks = np.clip(n_horiz - 1.35, 0.0, 1.0) * np.clip(n_horiz_mask + 0.2, 0.0, 1.0) * 0.35 * edge_p

    # 6. Low-frequency organic weathering variation
    np.random.seed(4109)
    weather_var = norm_field(periodic_box_blur(np.random.randn(h, w).astype(np.float32), rx=48, ry=68, passes=3))

    # 7. Combined height field
    height = (
        (plate_primary - 0.56) * 1.15
        + (plate_secondary - 0.58) * 0.45
        + (ridge_macro - 0.50) * 0.55
        + (ridge_mid - 0.50) * 0.32
        + wood_grain * (0.40 + 0.45 * edge_p)
        - horiz_cracks
    )
    height = periodic_box_blur(height, rx=1, ry=2, passes=1)

    # 8. BaseColor mapping (sRGB aged gray-brown bonsai bark palette):
    # - Base mid-bark: #716450 (113, 100, 80)
    # - Weathered plate ridge: #9D8B6E (157, 139, 110)
    # - Deep bark crevice: #423B30 (66, 59, 48)
    c_base = np.array([113.0, 100.0, 80.0], dtype=np.float32)
    c_high = np.array([157.0, 139.0, 110.0], dtype=np.float32)
    c_furrow = np.array([66.0, 59.0, 48.0], dtype=np.float32)

    pos_mask = height >= 0.0
    t_high = np.clip(height / 0.68, 0.0, 1.0)[:, :, None]
    color_high = (1.0 - t_high) * c_base + t_high * c_high

    t_furrow = np.clip(-height / 0.65, 0.0, 1.0)[:, :, None]
    color_furrow = (1.0 - t_furrow) * c_base + t_furrow * c_furrow

    base_color = np.where(pos_mask[:, :, None], color_high, color_furrow)
    base_color += (
        wood_grain[:, :, None] * 7.5
        + plate_id_smooth[:, :, None] * 14.0
        + weather_var[:, :, None] * 4.5
    )
    base_color = np.clip(np.round(base_color), 0, 255).astype(np.uint8)

    alpha_u8 = np.full((h, w, 1), 255, dtype=np.uint8)
    bc_rgba = np.concatenate([base_color, alpha_u8], axis=2)
    bc_path = TEXTURES_DIR / "Ancient_Bark_BaseColor.png"
    save_rgba_png("Ancient_Bark_BaseColor.png", bc_rgba, bc_path, w, h)

    # 9. Tangent-space Normal Map (OpenGL +Y up) from the exact same height field
    dh_dx = (np.roll(height, -1, axis=1) - np.roll(height, 1, axis=1)) * 0.5
    dh_dy = (np.roll(height, -1, axis=0) - np.roll(height, 1, axis=0)) * 0.5

    strength = 18.5
    nx_map = -dh_dx * strength
    ny_map = -dh_dy * strength
    nz_map = np.ones((h, w), dtype=np.float32)
    inv_len = 1.0 / np.sqrt(nx_map * nx_map + ny_map * ny_map + nz_map * nz_map)

    r_u8 = np.clip(np.round((nx_map * inv_len * 0.5 + 0.5) * 255.0), 0, 255).astype(np.uint8)
    g_u8 = np.clip(np.round((ny_map * inv_len * 0.5 + 0.5) * 255.0), 0, 255).astype(np.uint8)
    b_u8 = np.clip(np.round((nz_map * inv_len * 0.5 + 0.5) * 255.0), 0, 255).astype(np.uint8)
    nm_rgba = np.stack([r_u8, g_u8, b_u8, alpha_u8[:, :, 0]], axis=2)

    nm_path = TEXTURES_DIR / "Ancient_Bark_Normal.png"
    save_rgba_png("Ancient_Bark_Normal.png", nm_rgba, nm_path, w, h)

    print(f"  Updated {bc_path.name} and {nm_path.name}")
    return {
        "Ancient_Bark_BaseColor.png": sha256_file(bc_path),
        "Ancient_Bark_Normal.png": sha256_file(nm_path),
    }


def reorient_ancient_bark_uvs_along_growth(mesh_obj, is_roots=False):
    """Re-parameterize UVs of each tubular component in Trunk_Branches / Roots using:
    - Pure-topology 12-gon cylinder ring extraction (100% independent of prior UV state).
    - Smooth 3D centerline c_k and twist-free parallel-transport (Bishop) frame (T_k, N1_k, N2_k).
    - 3D circumferential angle theta(v) around the smoothed axis so any sideways 3D vertex
      displacement in the mesh is compensated in U and bark grain runs straight in 3D space.
    - Seamless integer U-tiling k_u and 100% positive UV Jacobian determinant across all faces.
    - Zero vertex positions, edges, polygons, or corner normals are modified.
    """
    me = mesh_obj.data
    bm = bmesh.new()
    bm.from_mesh(me)
    bm.faces.ensure_lookup_table()
    bm.verts.ensure_lookup_table()
    uv_lay = bm.loops.layers.uv.active

    seen_faces = set()
    tubes = []
    for f in bm.faces:
        if f in seen_faces:
            continue
        comp = []
        stack = [f]
        seen_faces.add(f)
        while stack:
            cur = stack.pop()
            comp.append(cur)
            for e in cur.edges:
                for lf in e.link_faces:
                    if lf not in seen_faces:
                        seen_faces.add(lf)
                        stack.append(lf)
        tubes.append(comp)

    v_tile_meters = 0.65
    u_tile_meters = 0.55

    for idx, comp_faces in enumerate(tubes):
        comp_set = set(comp_faces)
        b_edges = set(
            e for f in comp_set for e in f.edges
            if sum(1 for lf in e.link_faces if lf in comp_set) == 1
        )
        b_verts = set(v for e in b_edges for v in e.verts)

        # Find the two boundary cap loops (12 vertices each)
        start_v = min(b_verts, key=lambda v: (v.co.z, v.index))
        cap_a = [start_v]
        prev_v = None
        cur_v = start_v
        while True:
            nbrs = [
                e.other_vert(cur_v) for e in cur_v.link_edges
                if e in b_edges and e.other_vert(cur_v) != prev_v
            ]
            nxt_v = nbrs[0]
            if nxt_v == start_v:
                break
            cap_a.append(nxt_v)
            prev_v, cur_v = cur_v, nxt_v

        # Step ring-by-ring from cap_a to the opposite cap
        rings = [cap_a]
        visited_v = set(cap_a)
        while True:
            nxt_ring = []
            for v in rings[-1]:
                nbrs = [
                    e.other_vert(v) for e in v.link_edges
                    if any(lf in comp_set for lf in e.link_faces) and e.other_vert(v) not in visited_v
                ]
                if nbrs:
                    nxt_ring.append(nbrs[0])
            if not nxt_ring:
                break
            rings.append(nxt_ring)
            visited_v.update(nxt_ring)

        # Ensure rings[0] -> rings[-1] runs from base to tip
        c_start = sum((v.co for v in rings[0]), Vector((0.0, 0.0, 0.0))) / len(rings[0])
        c_end = sum((v.co for v in rings[-1]), Vector((0.0, 0.0, 0.0))) / len(rings[-1])
        if is_roots:
            score_start = c_start.z - 0.8 * math.hypot(c_start.x, c_start.y)
            score_end = c_end.z - 0.8 * math.hypot(c_end.x, c_end.y)
            if score_start < score_end:
                rings.reverse()
        else:
            if abs(c_end.z - c_start.z) > 0.15:
                if c_start.z > c_end.z:
                    rings.reverse()
            elif c_start.length > c_end.length:
                rings.reverse()

        n_rings = len(rings)
        raw_centers = [
            sum((v.co for v in r), Vector((0.0, 0.0, 0.0))) / len(r)
            for r in rings
        ]
        circs = [
            2.0 * math.pi * (sum((v.co - c).length for v in r) / len(r))
            for r, c in zip(rings, raw_centers)
        ]

        # Extrapolate cap centers (rings 0 and -1 often pinch into parent trunk) and smooth centerline
        centers = [c.copy() for c in raw_centers]
        if n_rings >= 5:
            centers[0] = centers[2] + 2.0 * (centers[1] - centers[2])
            centers[-1] = centers[-3] + 2.0 * (centers[-2] - centers[-3])
        for _ in range(8):
            new_c = [centers[0].copy()]
            for k in range(1, n_rings - 1):
                new_c.append((centers[k - 1] + 2.0 * centers[k] + centers[k + 1]) * 0.25)
            new_c.append(centers[-1].copy())
            if n_rings >= 5:
                new_c[0] = new_c[1] + (new_c[1] - new_c[2])
                new_c[-1] = new_c[-2] + (new_c[-2] - new_c[-3])
            centers = new_c

        # Compute smooth tangents T_k and parallel-transport (Bishop) normal basis (N1_k, N2_k)
        tangents = []
        for k in range(n_rings):
            k0 = max(0, k - 2)
            k1 = min(n_rings - 1, k + 2)
            t_vec = centers[k1] - centers[k0]
            if t_vec.length < 1e-6:
                t_vec = Vector((0.0, 0.0, 1.0))
            else:
                t_vec.normalize()
            tangents.append(t_vec)

        ref = Vector((1.0, 0.0, 0.0)) if abs(tangents[0].x) < 0.8 else Vector((0.0, 1.0, 0.0))
        n1 = (ref - ref.dot(tangents[0]) * tangents[0]).normalized()
        n2 = tangents[0].cross(n1).normalized()
        frames = [(tangents[0], n1, n2)]
        for k in range(1, n_rings):
            tk = tangents[k]
            n1_prev = frames[-1][1]
            n1_k = n1_prev - n1_prev.dot(tk) * tk
            if n1_k.length < 1e-6:
                n1_k = frames[-1][1].copy()
            else:
                n1_k.normalize()
            n2_k = tk.cross(n1_k).normalized()
            frames.append((tk, n1_k, n2_k))

        # Cumulative 3D arc length along smoothed centerline (with minimum step for bevel caps)
        arc_len = [0.0]
        for k in range(1, n_rings):
            c_step = (centers[k] - centers[k - 1]).length
            v_step = sum((rings[k][j].co - rings[k - 1][j].co).length for j in range(12)) / 12.0
            step_d = max(c_step, 0.65 * v_step, 0.015)
            arc_len.append(arc_len[-1] + step_d)

        v_offset = ((idx * 0.29) % 1.0)
        u_offset = ((idx * 0.41) % 1.0)
        mean_circ = float(np.mean(circs))
        k_u = max(1, int(round(mean_circ / u_tile_meters)))

        # Compute per-vertex unwrapped circumferential fraction t_frac[k][j] in R and axial V[k][j]
        t_frac = np.zeros((n_rings, 12), dtype=np.float64)
        v_coord = np.zeros((n_rings, 12), dtype=np.float64)

        for k in range(n_rings):
            tk, n1_k, n2_k = frames[k]
            ck = centers[k]
            raw_angles = []
            for j in range(12):
                rel = rings[k][j].co - ck
                x_loc = rel.dot(n1_k)
                y_loc = rel.dot(n2_k)
                raw_angles.append(math.atan2(y_loc, x_loc))

            # Unwrap angles around j=0..11
            unwrapped = [raw_angles[0]]
            for j in range(1, 12):
                diff = math.atan2(math.sin(raw_angles[j] - unwrapped[-1]), math.cos(raw_angles[j] - unwrapped[-1]))
                unwrapped.append(unwrapped[-1] + diff)

            # Continuous alignment of j=0 with previous ring k-1
            if k > 0:
                prev_a0 = t_frac[k - 1, 0] * (2.0 * math.pi)
                shift = round((prev_a0 - unwrapped[0]) / (2.0 * math.pi)) * (2.0 * math.pi)
                unwrapped = [a + shift for a in unwrapped]

            # Normalize total span across 12 steps to exact +/- 2*pi while preserving true 3D angular spacing
            sign = 1.0 if (unwrapped[-1] - unwrapped[0]) >= 0.0 else -1.0
            diffs = []
            for j in range(12):
                a_cur = unwrapped[j]
                a_nxt = unwrapped[0] + sign * 2.0 * math.pi if j == 11 else unwrapped[j + 1]
                d_step = sign * (a_nxt - a_cur)
                diffs.append(max(d_step, (2.0 * math.pi / 12.0) * 0.25))
            total_d = sum(diffs)
            cum = 0.0
            for j in range(12):
                t_3d = (unwrapped[0] / (2.0 * math.pi)) + sign * (cum / total_d)
                t_uni = (unwrapped[0] / (2.0 * math.pi)) + sign * (j / 12.0)
                t_frac[k, j] = 0.85 * t_3d + 0.15 * t_uni
                cum += diffs[j]
                v_coord[k, j] = v_offset + (arc_len[k] / v_tile_meters)

        # Build lookup (ring_k, slot_j) for each vertex
        vert_to_kj = {}
        for k in range(n_rings):
            for j in range(12):
                vert_to_kj[rings[k][j]] = (k, j)

        # Check UV determinant sign on this tube and flip U if negative
        det_sum = 0.0
        for f in comp_faces:
            ks_js = [vert_to_kj[l.vert] for l in f.loops]
            j_set = set(j for _, j in ks_js)
            wrap_j = (0 in j_set and 11 in j_set)
            uvs_f = []
            for k_i, j_i in ks_js:
                tf = t_frac[k_i, 0] + (1.0 if t_frac[k_i, 11] >= t_frac[k_i, 0] else -1.0) if (wrap_j and j_i == 0) else t_frac[k_i, j_i]
                uvs_f.append((u_offset + k_u * tf, v_coord[k_i, j_i]))
            (u0, v0), (u1, v1), (u2, v2) = uvs_f[0], uvs_f[1], uvs_f[2]
            det_sum += (u1 - u0) * (v2 - v0) - (v1 - v0) * (u2 - u0)

        flip_u = -1.0 if det_sum < 0.0 else 1.0

        for f in comp_faces:
            ks_js = [vert_to_kj[l.vert] for l in f.loops]
            j_set = set(j for _, j in ks_js)
            wrap_j = (0 in j_set and 11 in j_set)
            for l, (k_i, j_i) in zip(f.loops, ks_js):
                tf = t_frac[k_i, 0] + (1.0 if t_frac[k_i, 11] >= t_frac[k_i, 0] else -1.0) if (wrap_j and j_i == 0) else t_frac[k_i, j_i]
                new_u = u_offset + flip_u * k_u * tf
                new_v = v_coord[k_i, j_i]
                l[uv_lay].uv = (float(new_u), float(new_v))

    # Verify positive UV determinant across all faces
    pos_faces = 0
    neg_faces = 0
    for f in bm.faces:
        l0, l1, l2 = f.loops[0], f.loops[1], f.loops[2]
        u0, v0 = l0[uv_lay].uv
        u1, v1 = l1[uv_lay].uv
        u2, v2 = l2[uv_lay].uv
        det = (u1 - u0) * (v2 - v0) - (v1 - v0) * (u2 - u0)
        if det > 1e-8:
            pos_faces += 1
        elif det < -1e-8:
            neg_faces += 1

    bm.to_mesh(me)
    bm.free()
    me.update()
    print(f"  Reoriented {mesh_obj.name} UVs along local 3D growth curves: {len(tubes)} tubes, pos_det={pos_faces}/{len(me.polygons)}, neg_det={neg_faces}")
    return {"tubes": len(tubes), "pos_det_faces": pos_faces, "neg_det_faces": neg_faces}

def update_leaf_material_direct(mat, leaf_tex_filename, roughness, normal_strength, uv_layer_name):
    mat.use_nodes = True
    nt = mat.node_tree
    nodes = nt.nodes
    links = nt.links
    nodes.clear()

    node_out = nodes.new('ShaderNodeOutputMaterial')
    node_out.location = (500, 0)

    node_bsdf = nodes.new('ShaderNodeBsdfPrincipled')
    node_bsdf.location = (200, 0)
    node_bsdf.inputs['Base Color'].default_value = (1.0, 1.0, 1.0, 1.0)
    node_bsdf.inputs['Roughness'].default_value = roughness
    node_bsdf.inputs['Metallic'].default_value = 0.0
    if 'Emission Color' in node_bsdf.inputs:
        node_bsdf.inputs['Emission Color'].default_value = (0.0, 0.0, 0.0, 1.0)
    if 'Emission Strength' in node_bsdf.inputs:
        node_bsdf.inputs['Emission Strength'].default_value = 0.0
    if 'Specular IOR Level' in node_bsdf.inputs:
        node_bsdf.inputs['Specular IOR Level'].default_value = 0.5

    # Direct BaseColor Image Texture (no Mix node!)
    tex_bc = nodes.new('ShaderNodeTexImage')
    tex_bc.name = "Image Texture"
    tex_bc.location = (-250, 150)
    img_bc = bpy.data.images.load(str(TEXTURES_DIR / leaf_tex_filename), check_existing=True)
    img_bc.colorspace_settings.name = 'sRGB'
    tex_bc.image = img_bc
    links.new(tex_bc.outputs['Color'], node_bsdf.inputs['Base Color'])

    # Shared Normal Map Image Texture
    tex_nm = nodes.new('ShaderNodeTexImage')
    tex_nm.name = "Image Texture.001"
    tex_nm.location = (-250, -180)
    img_nm = bpy.data.images.load(str(TEXTURES_DIR / "Leaf_Common_Normal.png"), check_existing=True)
    img_nm.colorspace_settings.name = 'Non-Color'
    tex_nm.image = img_nm

    node_nmap = nodes.new('ShaderNodeNormalMap')
    node_nmap.name = "Normal Map"
    node_nmap.location = (-20, -180)
    node_nmap.space = 'TANGENT'
    node_nmap.uv_map = uv_layer_name
    node_nmap.inputs['Strength'].default_value = normal_strength

    links.new(tex_nm.outputs['Color'], node_nmap.inputs['Color'])
    links.new(node_nmap.outputs['Normal'], node_bsdf.inputs['Normal'])
    links.new(node_bsdf.outputs['BSDF'], node_out.inputs['Surface'])

    # Set viewport/exporter diffuse_color to white (1,1,1,1) so no double tint occurs
    mat.diffuse_color = (1.0, 1.0, 1.0, 1.0)
    mat.roughness = roughness
    mat.metallic = 0.0

def update_ancient_bark_material_direct(mat):
    """Configure M_Bark on Ancient with baked UV tiling (Tiling = 1.0, 1.0, no Mapping node needed)."""
    mat.use_nodes = True
    nt = mat.node_tree
    nodes = nt.nodes
    links = nt.links
    nodes.clear()

    node_out = nodes.new('ShaderNodeOutputMaterial')
    node_out.location = (500, 0)

    node_bsdf = nodes.new('ShaderNodeBsdfPrincipled')
    node_bsdf.location = (200, 0)
    node_bsdf.inputs['Base Color'].default_value = (1.0, 1.0, 1.0, 1.0)
    node_bsdf.inputs['Roughness'].default_value = 0.88
    node_bsdf.inputs['Metallic'].default_value = 0.0
    if 'Emission Color' in node_bsdf.inputs:
        node_bsdf.inputs['Emission Color'].default_value = (0.0, 0.0, 0.0, 1.0)
    if 'Emission Strength' in node_bsdf.inputs:
        node_bsdf.inputs['Emission Strength'].default_value = 0.0

    tex_bc = nodes.new('ShaderNodeTexImage')
    tex_bc.name = "Image Texture"
    tex_bc.location = (-250, 150)
    img_bc = bpy.data.images.load(str(TEXTURES_DIR / "Ancient_Bark_BaseColor.png"), check_existing=True)
    img_bc.colorspace_settings.name = 'sRGB'
    tex_bc.image = img_bc
    links.new(tex_bc.outputs['Color'], node_bsdf.inputs['Base Color'])

    tex_nm = nodes.new('ShaderNodeTexImage')
    tex_nm.name = "Image Texture.001"
    tex_nm.location = (-250, -180)
    img_nm = bpy.data.images.load(str(TEXTURES_DIR / "Ancient_Bark_Normal.png"), check_existing=True)
    img_nm.colorspace_settings.name = 'Non-Color'
    tex_nm.image = img_nm

    node_nmap = nodes.new('ShaderNodeNormalMap')
    node_nmap.name = "Normal Map"
    node_nmap.location = (-20, -180)
    node_nmap.space = 'TANGENT'
    node_nmap.uv_map = "UVMap"
    node_nmap.inputs['Strength'].default_value = 1.05

    links.new(tex_nm.outputs['Color'], node_nmap.inputs['Color'])
    links.new(node_nmap.outputs['Normal'], node_bsdf.inputs['Normal'])
    links.new(node_bsdf.outputs['BSDF'], node_out.inputs['Surface'])

    mat.diffuse_color = (1.0, 1.0, 1.0, 1.0)
    mat.roughness = 0.88
    mat.metallic = 0.0

def main():
    baseline_path = EVIDENCE_DIR / "baseline_before_fixes.json"
    baseline = json.loads(baseline_path.read_text(encoding="utf-8"))

    # Verify protected files untouched before starting
    for rel_p in ["Assets/_Project/Art/Bonsai_Tree.blend", "Assets/_Project/Art/Bonsai_Tree.fbx"]:
        assert sha256_file(ROOT / rel_p) == baseline["pre_hashes"][rel_p], f"Protected file {rel_p} changed before start!"

    # 1. Generate R1 Leaf BaseColor PNGs
    r1_metrics = generate_r1_leaf_textures(baseline)

    # 2. Generate R2 Ancient Bark PNGs
    r2_tex_hashes = generate_r2_ancient_bark_textures()

    # 3. Apply changes to all 4 .blend files and export canonical .fbx files
    print("\n=== STEP 3: UPDATING 4 SOURCE .BLEND FILES & EXPORTING 4 CANONICAL FBX FILES ===")
    r2_uv_stats = {}

    prev_verif_path = EVIDENCE_DIR / "verification_after_fixes.json"
    prev_verif = json.loads(prev_verif_path.read_text(encoding="utf-8")) if prev_verif_path.exists() else None

    for label, mcfg in MODELS.items():
        blend_path = MODELS_DIR / f"{mcfg['name']}.blend"
        fbx_path = EXPORTS_DIR / f"{mcfg['name']}.fbx"
        expected_shas = {baseline["pre_hashes"][f"ArtSource/Models/{mcfg['name']}.blend"]}
        if prev_verif and label in prev_verif.get("models_after", {}):
            expected_shas.add(prev_verif["models_after"][label]["blend_sha256"])
        current_sha = sha256_file(blend_path)
        assert current_sha in expected_shas, f"Concurrent modification detected on {blend_path}!"

        bpy.ops.wm.open_mainfile(filepath=str(blend_path), use_scripts=False)
        leaf_before = baseline["models"][label]["leaf_graph_before"]

        # Update Leaf Material (R1)
        mat_leaf = bpy.data.materials[mcfg["leaf_mat"]]
        update_leaf_material_direct(
            mat_leaf,
            mcfg["leaf_tex"],
            leaf_before["roughness"],
            leaf_before["normal_strength"],
            mcfg["uv_name"],
        )

        # If Ancient, update Trunk_Branches and Roots UVs & M_Bark (R2)
        if label == "ancient":
            r2_uv_stats["Trunk_Branches"] = reorient_ancient_bark_uvs_along_growth(
                bpy.data.objects["Trunk_Branches"], is_roots=False
            )
            r2_uv_stats["Roots"] = reorient_ancient_bark_uvs_along_growth(
                bpy.data.objects["Roots"], is_roots=True
            )
            update_ancient_bark_material_direct(bpy.data.materials["M_Bark"])

        # Remove unused image datablocks so no stale references remain
        for img in list(bpy.data.images):
            if img.users == 0:
                bpy.data.images.remove(img)

        # Make all texture paths relative
        bpy.ops.file.make_paths_relative()

        # Select only MESH objects and export FBX
        mesh_objs = [o for o in bpy.context.scene.objects if o.type == 'MESH']
        bpy.ops.object.select_all(action='DESELECT')
        for o in mesh_objs:
            o.select_set(True)
        bpy.context.view_layer.objects.active = mesh_objs[0]

        bpy.ops.export_scene.fbx(
            filepath=str(fbx_path),
            use_selection=True,
            global_scale=1.0,
            apply_unit_scale=True,
            apply_scale_options='FBX_SCALE_ALL',
            axis_forward='-Z',
            axis_up='Y',
            object_types={'MESH'},
            use_mesh_modifiers=True,
            mesh_smooth_type='FACE',
            bake_anim=False,
            path_mode='AUTO',
        )

        # Save .blend with save_version = 0 (no new .blend1 backup created)
        bpy.context.preferences.filepaths.save_version = 0
        bpy.ops.wm.save_as_mainfile(filepath=str(blend_path))
        print(f"  Saved {blend_path.name} and exported {fbx_path.name}")

    # Save intermediate R1/R2 metrics
    summary = {
        "r1_leaf_texture_metrics": r1_metrics,
        "r2_ancient_bark_texture_hashes": r2_tex_hashes,
        "r2_ancient_uv_stats": r2_uv_stats,
    }
    (EVIDENCE_DIR / "r1_r2_generation_metrics.json").write_text(json.dumps(summary, indent=2), encoding="utf-8")
    print("\n=== R1 & R2 APPLIED AND EXPORTED SUCCESSFULLY ===")

if __name__ == "__main__":
    main()
