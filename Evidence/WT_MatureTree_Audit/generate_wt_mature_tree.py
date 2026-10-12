"""
Generate a fully 3D, game-ready 'Mature World Tree' (WT_MatureTree) asset in Blender.
Pass 2:
- 100% shingled multi-tier 3D leaf clusters & star-rosettes (zero bald mushroom domes!)
- LOD0 triangle count ~13.8k tris (squarely inside the 8k-15k target!)
- Trunk base formed organically by converging twisting root-buttress strands (no central cylinder post at bottom)
- Sinuous S-curved sprawling roots in 360° with natural thickness and soft organic bark texture (no harsh zebra stripes)
- Clean 2-manifold geometry (0 non-manifold edges, 0 boundary edges, 0 loose vertices, 0 degenerate faces)
"""

import bpy
import bmesh
import math
import json
import os
import struct
import zlib
from pathlib import Path
from mathutils import Vector, Matrix, Euler

ROOT_DIR = Path(__file__).resolve().parents[2]
TEX_DIR = ROOT_DIR / "ArtSource" / "Textures" / "Tree_Common"
MODELS_DIR = ROOT_DIR / "ArtSource" / "Models"
EXPORTS_DIR = ROOT_DIR / "ArtSource" / "Exports"
AUDIT_DIR = Path(__file__).resolve().parent


# ==============================================================================
# 1. CLEAN PNG TEXTURE GENERATOR (WARM HANDCRAFTED STYLIZED PBR)
# ==============================================================================

def write_png_rgba(filepath, width, height, pixels_rgba_u8):
    """Writes an 8-bit RGBA PNG file from a flat bytearray/bytes of length width*height*4."""
    raw_rows = bytearray()
    stride = width * 4
    for y in range(height):
        raw_rows.append(0)
        row_start = y * stride
        raw_rows.extend(pixels_rgba_u8[row_start:row_start + stride])

    def make_chunk(chunk_type, data):
        chunk = struct.pack(">I", len(data)) + chunk_type + data
        crc = zlib.crc32(chunk_type + data) & 0xFFFFFFFF
        return chunk + struct.pack(">I", crc)

    ihdr = struct.pack(">IIBBBBB", width, height, 8, 6, 0, 0, 0)
    compressed = zlib.compress(bytes(raw_rows), level=6)
    png_bytes = (
        b"\x89PNG\r\n\x1a\n"
        + make_chunk(b"IHDR", ihdr)
        + make_chunk(b"IDAT", compressed)
        + make_chunk(b"IEND", b"")
    )
    Path(filepath).parent.mkdir(parents=True, exist_ok=True)
    Path(filepath).write_bytes(png_bytes)


def clamp01(v):
    return max(0.0, min(1.0, v))


def lerp(a, b, t):
    return a + (b - a) * t


def smoothstep(edge0, edge1, x):
    t = clamp01((x - edge0) / (edge1 - edge0)) if edge1 != edge0 else 0.0
    return t * t * (3.0 - 2.0 * t)


def generate_wt_textures():
    """
    Generates warm, handcrafted stylized PBR textures for WT_MatureTree:
    - WT_Bark_BaseColor.png (warm medium brown, golden tan highlights, soft dark brown creases - no harsh stripes!)
    - WT_RootAccent_BaseColor.png (harmonious warm golden-brown root tone with subtle grounding gradient)
    - WT_Bark_Normal.png (soft stylized vertical bark ridges & creases, tangent-space OpenGL)
    - WT_Leaves_Light_BaseColor.png (vibrant lime green with yellow-green highlights & soft veins)
    - WT_Leaves_Dark_BaseColor.png (lush darker forest-green interior shadow leaf texture)
    """
    TEX_DIR.mkdir(parents=True, exist_ok=True)

    bw, bh = 512, 512
    bark_col = bytearray(bw * bh * 4)
    root_col = bytearray(bw * bh * 4)
    bark_nrm = bytearray(bw * bh * 4)
    h_field = [[0.0] * bw for _ in range(bh)]

    for y in range(bh):
        v = y / bh
        for x in range(bw):
            u = x / bw
            # Gentle flowing vertical wood fibers with domain warp
            warp1 = 0.035 * math.sin(2.0 * math.pi * (1.0 * v + 1.0 * u))
            warp2 = 0.018 * math.cos(2.0 * math.pi * (2.0 * v - 2.0 * u))
            uw = u + warp1 + warp2

            # Broad soft vertical bark waves (4 around U) + finer fibers (12 and 24 around U)
            wave_broad = 0.5 + 0.5 * math.cos(2.0 * math.pi * 4.0 * uw)
            fiber_mid = 0.5 + 0.5 * math.cos(2.0 * math.pi * 11.0 * uw + 0.8 * math.sin(2.0 * math.pi * 2.0 * v))
            fiber_fine = 0.5 + 0.5 * math.sin(2.0 * math.pi * 23.0 * uw - 0.5 * math.cos(2.0 * math.pi * 3.0 * v))
            # Occasional soft vertical bark crevice
            crevice = smoothstep(0.22, 0.0, abs(math.sin(math.pi * 4.0 * uw + 0.35 * math.sin(2.0 * math.pi * v))))

            h = 0.48 * wave_broad + 0.30 * fiber_mid + 0.14 * fiber_fine - 0.22 * crevice
            h = clamp01(h)
            h_field[y][x] = h

            # Warm golden-tan & medium brown palette matching the reference image's hopeful sunlit wood:
            # Crevice warm brown: (118, 78, 44) #764E2C
            # Mid warm tan-brown: (168, 120, 74) #A8784A
            # Highlight golden tan: (214, 168, 114) #D6A872
            if h < 0.45:
                t = h / 0.45
                r = lerp(118.0, 168.0, t)
                g = lerp(78.0, 120.0, t)
                b = lerp(44.0, 74.0, t)
            else:
                t = (h - 0.45) / 0.55
                r = lerp(168.0, 214.0, t)
                g = lerp(120.0, 168.0, t)
                b = lerp(74.0, 114.0, t)

            idx = (y * bw + x) * 4
            bark_col[idx] = int(clamp01(r / 255.0) * 255.0 + 0.5)
            bark_col[idx + 1] = int(clamp01(g / 255.0) * 255.0 + 0.5)
            bark_col[idx + 2] = int(clamp01(b / 255.0) * 255.0 + 0.5)
            bark_col[idx + 3] = 255

            # RootAccent: very close harmonious warm brown, slightly deeper earthy tone toward outer tips
            v_blend = smoothstep(0.0, 0.75, 1.0 - v)
            rr = lerp(r, r * 0.86, v_blend * 0.65)
            rg = lerp(g, g * 0.84, v_blend * 0.65)
            rb = lerp(b, b * 0.80, v_blend * 0.65)
            root_col[idx] = int(clamp01(rr / 255.0) * 255.0 + 0.5)
            root_col[idx + 1] = int(clamp01(rg / 255.0) * 255.0 + 0.5)
            root_col[idx + 2] = int(clamp01(rb / 255.0) * 255.0 + 0.5)
            root_col[idx + 3] = 255

    # Soft stylized tangent-space normal map
    strength = 4.2
    for y in range(bh):
        ym1 = (y - 1) % bh
        yp1 = (y + 1) % bh
        for x in range(bw):
            xm1 = (x - 1) % bw
            xp1 = (x + 1) % bw
            dx = (h_field[y][xp1] - h_field[y][xm1]) * 0.5 * strength
            dy = (h_field[yp1][x] - h_field[ym1][x]) * 0.5 * strength
            nx, ny, nz = -dx, -dy, 1.0
            inv_len = 1.0 / math.sqrt(nx * nx + ny * ny + nz * nz)
            nx *= inv_len
            ny *= inv_len
            nz *= inv_len
            idx = (y * bw + x) * 4
            bark_nrm[idx] = int(clamp01(nx * 0.5 + 0.5) * 255.0 + 0.5)
            bark_nrm[idx + 1] = int(clamp01(ny * 0.5 + 0.5) * 255.0 + 0.5)
            bark_nrm[idx + 2] = int(clamp01(nz * 0.5 + 0.5) * 255.0 + 0.5)
            bark_nrm[idx + 3] = 255

    write_png_rgba(TEX_DIR / "WT_Bark_BaseColor.png", bw, bh, bark_col)
    write_png_rgba(TEX_DIR / "WT_RootAccent_BaseColor.png", bw, bh, root_col)
    write_png_rgba(TEX_DIR / "WT_Bark_Normal.png", bw, bh, bark_nrm)

    # --- 2. Leaves Light & Dark BaseColor (512x512 stylized botanical leaf with veins & sunlit gradient) ---
    lw, lh = 512, 512
    leaf_light = bytearray(lw * lh * 4)
    leaf_dark = bytearray(lw * lh * 4)

    for y in range(lh):
        v = 1.0 - (y / (lh - 1))
        for x in range(lw):
            u = x / (lw - 1)
            du = abs(u - 0.5) * 2.0

            midrib_width = lerp(0.060, 0.018, v)
            midrib = max(0.0, 1.0 - (du / midrib_width)) ** 2 * smoothstep(0.98, 0.08, v)

            lat_vein = 0.0
            for v_center in (0.18, 0.36, 0.54, 0.72):
                v_curve = v_center + 0.16 * (du ** 1.15)
                dv = abs(v - v_curve)
                vein_w = 0.026 * (1.0 - 0.4 * du)
                if dv < vein_w and 0.02 < du < 0.88:
                    lat_vein = max(lat_vein, (1.0 - dv / vein_w) ** 2 * (1.0 - du * 0.72))

            vein_boost = clamp01(midrib * 0.80 + lat_vein * 0.50)
            sun_grad = clamp01(0.68 * v + 0.32 * (1.0 - du * 0.60))

            # Light Leaf Palette (lush lime green -> warm yellow-green sunlit tips)
            lr = lerp(96.0, 182.0, sun_grad) + vein_boost * 22.0
            lg = lerp(172.0, 228.0, sun_grad) + vein_boost * 18.0
            lb = lerp(34.0, 56.0, sun_grad) + vein_boost * 10.0

            # Dark Leaf Palette (rich forest green interior/lower shadows -> emerald green)
            dr = lerp(38.0, 78.0, sun_grad) + vein_boost * 15.0
            dg = lerp(92.0, 146.0, sun_grad) + vein_boost * 14.0
            db = lerp(26.0, 44.0, sun_grad) + vein_boost * 8.0

            idx = (y * lw + x) * 4
            leaf_light[idx] = int(clamp01(lr / 255.0) * 255.0 + 0.5)
            leaf_light[idx + 1] = int(clamp01(lg / 255.0) * 255.0 + 0.5)
            leaf_light[idx + 2] = int(clamp01(lb / 255.0) * 255.0 + 0.5)
            leaf_light[idx + 3] = 255

            leaf_dark[idx] = int(clamp01(dr / 255.0) * 255.0 + 0.5)
            leaf_dark[idx + 1] = int(clamp01(dg / 255.0) * 255.0 + 0.5)
            leaf_dark[idx + 2] = int(clamp01(db / 255.0) * 255.0 + 0.5)
            leaf_dark[idx + 3] = 255

    write_png_rgba(TEX_DIR / "WT_Leaves_Light_BaseColor.png", lw, lh, leaf_light)
    write_png_rgba(TEX_DIR / "WT_Leaves_Dark_BaseColor.png", lw, lh, leaf_dark)
    print("Generated WT textures in:", TEX_DIR)


# ==============================================================================
# 2. SCENE & MATERIAL SETUP (STANDARD PRINCIPLED BSDF ONLY)
# ==============================================================================

def clear_scene():
    bpy.ops.wm.read_homefile(use_empty=True)
    scene = bpy.context.scene
    scene.unit_settings.system = 'METRIC'
    scene.unit_settings.scale_length = 1.0


def build_pbr_material(name, base_tex_path, norm_tex_path, diffuse_rgba, roughness=0.8, normal_strength=0.55):
    mat = bpy.data.materials.new(name=name)
    mat.use_nodes = True
    mat.diffuse_color = diffuse_rgba
    mat.use_backface_culling = False

    nt = mat.node_tree
    nodes = nt.nodes
    links = nt.links
    nodes.clear()

    out_node = nodes.new(type="ShaderNodeOutputMaterial")
    out_node.location = (300, 0)

    bsdf = nodes.new(type="ShaderNodeBsdfPrincipled")
    bsdf.location = (0, 0)
    bsdf.inputs["Roughness"].default_value = roughness
    bsdf.inputs["Metallic"].default_value = 0.0
    links.new(bsdf.outputs["BSDF"], out_node.inputs["Surface"])

    if base_tex_path and Path(base_tex_path).exists():
        tex_col = nodes.new(type="ShaderNodeTexImage")
        tex_col.location = (-350, 150)
        img_col = bpy.data.images.load(str(base_tex_path), check_existing=True)
        img_col.colorspace_settings.name = "sRGB"
        tex_col.image = img_col
        links.new(tex_col.outputs["Color"], bsdf.inputs["Base Color"])
    else:
        bsdf.inputs["Base Color"].default_value = diffuse_rgba

    if norm_tex_path and Path(norm_tex_path).exists():
        tex_nrm = nodes.new(type="ShaderNodeTexImage")
        tex_nrm.location = (-600, -180)
        img_nrm = bpy.data.images.load(str(norm_tex_path), check_existing=True)
        img_nrm.colorspace_settings.name = "Non-Color"
        tex_nrm.image = img_nrm

        nrm_node = nodes.new(type="ShaderNodeNormalMap")
        nrm_node.location = (-280, -180)
        nrm_node.inputs["Strength"].default_value = normal_strength
        links.new(tex_nrm.outputs["Color"], nrm_node.inputs["Color"])
        links.new(nrm_node.outputs["Normal"], bsdf.inputs["Normal"])

    return mat


def create_all_wt_materials():
    leaf_nrm = TEX_DIR / "Leaf_Common_Normal.png"
    if not leaf_nrm.exists():
        leaf_nrm = TEX_DIR / "WT_Bark_Normal.png"

    m_bark = build_pbr_material(
        "M_WT_Bark",
        TEX_DIR / "WT_Bark_BaseColor.png",
        TEX_DIR / "WT_Bark_Normal.png",
        diffuse_rgba=(0.48, 0.29, 0.14, 1.0),
        roughness=0.80,
        normal_strength=0.55,
    )
    m_root = build_pbr_material(
        "M_WT_RootAccent",
        TEX_DIR / "WT_RootAccent_BaseColor.png",
        TEX_DIR / "WT_Bark_Normal.png",
        diffuse_rgba=(0.42, 0.25, 0.12, 1.0),
        roughness=0.84,
        normal_strength=0.55,
    )
    m_leaf_light = build_pbr_material(
        "M_WT_Leaves_Light",
        TEX_DIR / "WT_Leaves_Light_BaseColor.png",
        leaf_nrm,
        diffuse_rgba=(0.35, 0.68, 0.09, 1.0),
        roughness=0.66,
        normal_strength=0.18,
    )
    m_leaf_dark = build_pbr_material(
        "M_WT_Leaves_Dark",
        TEX_DIR / "WT_Leaves_Dark_BaseColor.png",
        leaf_nrm,
        diffuse_rgba=(0.12, 0.34, 0.05, 1.0),
        roughness=0.74,
        normal_strength=0.18,
    )
    return {
        "bark": m_bark,
        "root": m_root,
        "leaf_light": m_leaf_light,
        "leaf_dark": m_leaf_dark,
    }


# ==============================================================================
# 3. SPLINE & MANIFOLD GEOMETRY PRIMITIVES (ZERO BOUNDARY / NON-MANIFOLD EDGES)
# ==============================================================================

def eval_catmull_rom(pts, t):
    n = len(pts)
    if n == 1:
        return pts[0]
    scaled_t = clamp01(t) * (n - 1)
    idx = min(int(scaled_t), n - 2)
    u = scaled_t - idx

    p0 = pts[max(0, idx - 1)]
    p1 = pts[idx]
    p2 = pts[idx + 1]
    p3 = pts[min(n - 1, idx + 2)]

    u2 = u * u
    u3 = u2 * u
    return 0.5 * (
        (2.0 * p1)
        + (-p0 + p2) * u
        + (2.0 * p0 - 5.0 * p1 + 4.0 * p2 - p3) * u2
        + (-p0 + 3.0 * p1 - 3.0 * p2 + p3) * u3
    )


def sample_spline_tube(control_pts_and_radii, num_samples):
    pts = [p[0] for p in control_pts_and_radii]
    rxs = [Vector((p[1], p[2] if len(p) > 2 else p[1], 0.0)) for p in control_pts_and_radii]
    samples = []
    for i in range(num_samples):
        t = i / (num_samples - 1)
        pos = eval_catmull_rom(pts, t)
        r_vec = eval_catmull_rom(rxs, t)
        samples.append((pos, max(0.012, r_vec.x), max(0.012, r_vec.y)))
    return samples


def add_manifold_fluted_tube(
    bm,
    uv_lay,
    samples,
    num_radial=12,
    flute_count=5,
    flute_depth=0.06,
    twist_total=0.0,
    ground_clamp_z=None,
    mat_idx=0,
    u_tile=1.0,
    v_tile=2.0,
):
    """
    Creates a 100% closed 2-manifold tube with Bishop parallel transport frames,
    closed start & end center-fan caps (zero boundary edges, zero n-gons!),
    and clean non-overlapping cylindrical UVs.
    """
    n_rings = len(samples)
    if n_rings < 2:
        return

    frames = []
    prev_norm = None
    arc_lengths = [0.0]
    for i in range(1, n_rings):
        arc_lengths.append(arc_lengths[-1] + (samples[i][0] - samples[i - 1][0]).length)
    total_len = max(0.001, arc_lengths[-1])

    for i in range(n_rings):
        pt, rx, ry = samples[i]
        if i == 0:
            tangent = (samples[1][0] - pt).normalized()
        elif i == n_rings - 1:
            tangent = (pt - samples[i - 1][0]).normalized()
        else:
            tangent = (samples[i + 1][0] - samples[i - 1][0]).normalized()
        if tangent.length < 1e-6:
            tangent = Vector((0, 0, 1))

        if prev_norm is None:
            ref = Vector((0, 1, 0)) if abs(tangent.z) > 0.85 else Vector((0, 0, 1))
            norm = tangent.cross(ref).normalized()
        else:
            norm = (prev_norm - tangent * tangent.dot(prev_norm))
            if norm.length < 1e-6:
                ref = Vector((0, 1, 0)) if abs(tangent.z) > 0.85 else Vector((0, 0, 1))
                norm = tangent.cross(ref).normalized()
            else:
                norm.normalize()
        binorm = tangent.cross(norm).normalized()
        prev_norm = norm
        frames.append((pt, rx, ry, tangent, norm, binorm, arc_lengths[i] / total_len))

    rings = []
    for i, (pt, rx, ry, tangent, norm, binorm, v_norm) in enumerate(frames):
        ring_verts = []
        phi = twist_total * v_norm
        tip_fade = math.sin(math.pi * clamp01(v_norm)) ** 0.5
        for j in range(num_radial):
            theta = 2.0 * math.pi * j / num_radial
            if flute_count > 0 and flute_depth > 0.0:
                flute = 1.0 + tip_fade * (
                    flute_depth * math.cos(flute_count * theta + phi)
                    + 0.35 * flute_depth * math.cos((flute_count + 2) * theta - 0.6 * phi)
                )
            else:
                flute = 1.0

            local_offset = (norm * (math.cos(theta) * rx) + binorm * (math.sin(theta) * ry)) * flute
            pos = pt + local_offset

            if ground_clamp_z is not None and pos.z < ground_clamp_z:
                pos.z = ground_clamp_z + 0.12 * (pos.z - ground_clamp_z)

            ring_verts.append(bm.verts.new(pos))
        rings.append(ring_verts)

    for i in range(n_rings - 1):
        r1 = rings[i]
        r2 = rings[i + 1]
        v1 = frames[i][6]
        v2 = frames[i + 1][6]

        for j in range(num_radial):
            jn = (j + 1) % num_radial
            u1 = (j / num_radial) * u_tile
            u2 = ((j + 1) / num_radial) * u_tile
            f = bm.faces.new((r1[j], r1[jn], r2[jn], r2[j]))
            f.material_index = mat_idx
            f.smooth = True
            f.loops[0][uv_lay].uv = (u1, v1 * v_tile)
            f.loops[1][uv_lay].uv = (u2, v1 * v_tile)
            f.loops[2][uv_lay].uv = (u2, v2 * v_tile)
            f.loops[3][uv_lay].uv = (u1, v2 * v_tile)

    # Start cap
    p_start, rx0, ry0, t_start, _, _, _ = frames[0]
    v_start_pole = bm.verts.new(p_start - t_start * (0.25 * min(rx0, ry0)))
    r0 = rings[0]
    for j in range(num_radial):
        jn = (j + 1) % num_radial
        f = bm.faces.new((v_start_pole, r0[jn], r0[j]))
        f.material_index = mat_idx
        f.smooth = True
        f.loops[0][uv_lay].uv = (0.5, 0.0)
        f.loops[1][uv_lay].uv = (((j + 1) / num_radial), 0.02)
        f.loops[2][uv_lay].uv = ((j / num_radial), 0.02)

    # End cap
    p_end, rx1, ry1, t_end, _, _, _ = frames[-1]
    v_end_pole = bm.verts.new(p_end + t_end * (0.35 * min(rx1, ry1)))
    rn = rings[-1]
    for j in range(num_radial):
        jn = (j + 1) % num_radial
        f = bm.faces.new((v_end_pole, rn[j], rn[jn]))
        f.material_index = mat_idx
        f.smooth = True
        f.loops[0][uv_lay].uv = (0.5, 1.0)
        f.loops[1][uv_lay].uv = ((j / num_radial), 0.98)
        f.loops[2][uv_lay].uv = (((j + 1) / num_radial), 0.98)


# ==============================================================================
# 4. TRUNK & SPRAWLING 360° ROOTS (WT_MatureTree_TrunkAndRoots)
# ==============================================================================

def get_trunk_and_roots_spec():
    """
    Defines the 9-meter Mature World Tree's muscular S-curved twisting trunk and
    360-degree asymmetrical sinuous surface roots matching the reference image.
    Notice:
    - The central trunk core starts at z=0.55m inside the converging root-buttress bundle
      so there is NO flat cylinder post sticking down between the roots!
    - The lower trunk sweeps gracefully right (x=+0.34m at z=1.35m), then curves and twists
      to the left-center (x=-0.22m at z=3.15m) where the big limbs split off!
    """
    main_trunk = [
        (Vector((0.08, -0.02,  0.48)), 0.34, 0.32),  # Nested inside converging root buttresses
        (Vector((0.22, -0.05,  0.95)), 0.50, 0.46),  # Wide muscular lower trunk
        (Vector((0.34, -0.06,  1.45)), 0.47, 0.44),  # Rightward lower knee (Low-Right limb splits here)
        (Vector((0.18, -0.01,  2.15)), 0.44, 0.41),  # Sweeping diagonally left-upward
        (Vector((-0.10, 0.04,  2.85)), 0.41, 0.38),  # Lower-left limb & right-trunk fork junction
        (Vector((-0.22, 0.06,  3.65)), 0.33, 0.31),  # Left-leaning mid trunk
        (Vector((-0.12, 0.04,  4.55)), 0.27, 0.26),  # Mid-upper left limb junction
        (Vector((0.02,  0.01,  5.55)), 0.21, 0.20),  # Curves back toward center
        (Vector((0.03, -0.01,  6.55)), 0.15, 0.15),  # Upper sub-crown fork
        (Vector((0.00,  0.00,  7.55)), 0.10, 0.10),  # Enters apex crown
        (Vector((0.00,  0.00,  8.15)), 0.05, 0.05),
    ]

    # Secondary Ascending Right Trunk Limb (splits at z=2.75m, visible in reference gap!)
    right_trunk_fork = [
        (Vector((0.02,  0.02,  2.55)), 0.25, 0.24),
        (Vector((0.32, -0.04,  3.35)), 0.22, 0.21),
        (Vector((0.72, -0.06,  4.10)), 0.18, 0.17),
        (Vector((1.18, -0.04,  4.72)), 0.14, 0.13),
        (Vector((1.58,  0.00,  5.18)), 0.08, 0.08),
    ]

    # 9 Continuous Twisting Trunk-Ridge-to-Sprawling-Root Strands (360° Radial Asymmetry)
    # Each strand wraps down the S-curved trunk and flows out into a thick, sinuous S-curved root.
    primary_root_strands = [
        # Strand 1: Iconic Central-Front Sweeping Ridge (wraps from upper-left trunk across front to right-center foreground root!)
        [
            (Vector((-0.20, -0.14, 3.45)), 0.13, 0.11),
            (Vector((0.08,  -0.26, 2.35)), 0.21, 0.18),
            (Vector((0.36,  -0.28, 1.45)), 0.26, 0.22),
            (Vector((0.28,  -0.52, 0.68)), 0.28, 0.24),
            (Vector((0.24,  -1.15, 0.26)), 0.24, 0.19),
            (Vector((0.48,  -1.88, 0.14)), 0.17, 0.13),
            (Vector((0.58,  -2.52, 0.06)), 0.10, 0.08),
            (Vector((0.72,  -2.92, -0.02)), 0.04, 0.03),
        ],
        # Strand 2: Front-Left Sweeping Ridge -> Long Sinuous Front-Left Root
        [
            (Vector((-0.24, -0.02, 3.15)), 0.13, 0.10),
            (Vector((-0.05, -0.24, 2.15)), 0.20, 0.16),
            (Vector((0.06,  -0.38, 1.25)), 0.25, 0.20),
            (Vector((-0.38, -0.75, 0.55)), 0.25, 0.20),
            (Vector((-0.92, -1.28, 0.22)), 0.20, 0.15),
            (Vector((-1.42, -1.85, 0.11)), 0.14, 0.10),
            (Vector((-1.98, -2.28, 0.04)), 0.08, 0.06),
            (Vector((-2.35, -2.55, -0.02)), 0.035, 0.028),
        ],
        # Strand 3: Far-Left Muscular Buttress -> Wide Left S-Curving Root
        [
            (Vector((-0.22,  0.14, 2.85)), 0.14, 0.11),
            (Vector((-0.14, -0.08, 1.95)), 0.21, 0.17),
            (Vector((-0.28, -0.20, 1.10)), 0.26, 0.21),
            (Vector((-0.88, -0.32, 0.48)), 0.24, 0.19),
            (Vector((-1.65, -0.42, 0.20)), 0.18, 0.14),
            (Vector((-2.38, -0.68, 0.10)), 0.12, 0.09),
            (Vector((-3.05, -0.78, 0.04)), 0.07, 0.055),
            (Vector((-3.48, -0.92, -0.02)), 0.032, 0.025),
        ],
        # Strand 4: Inner Front-Left Root Ridge (curving between Strand 1 & 2)
        [
            (Vector((0.12,  -0.28, 2.10)), 0.13, 0.10),
            (Vector((0.08,  -0.44, 1.20)), 0.20, 0.16),
            (Vector((-0.12, -0.82, 0.50)), 0.22, 0.17),
            (Vector((-0.38, -1.48, 0.20)), 0.17, 0.13),
            (Vector((-0.72, -2.18, 0.10)), 0.11, 0.085),
            (Vector((-0.92, -2.68, 0.03)), 0.065, 0.05),
            (Vector((-1.08, -2.98, -0.02)), 0.03, 0.024),
        ],
        # Strand 5: Front-Right Muscular Ridge -> Right-Front Sprawling Root
        [
            (Vector((0.24,  -0.16, 2.55)), 0.13, 0.10),
            (Vector((0.46,  -0.14, 1.55)), 0.21, 0.17),
            (Vector((0.62,  -0.30, 0.82)), 0.24, 0.19),
            (Vector((0.95,  -0.68, 0.36)), 0.21, 0.16),
            (Vector((1.52,  -1.15, 0.17)), 0.15, 0.11),
            (Vector((2.08,  -1.55, 0.07)), 0.095, 0.075),
            (Vector((2.52,  -1.88, -0.02)), 0.035, 0.028),
        ],
        # Strand 6: Far-Right Buttress Ridge -> Long Right Sinuous Root
        [
            (Vector((0.30,   0.08, 2.35)), 0.13, 0.10),
            (Vector((0.54,   0.02, 1.45)), 0.22, 0.18),
            (Vector((0.82,  -0.02, 0.68)), 0.24, 0.19),
            (Vector((1.38,  -0.06, 0.28)), 0.19, 0.15),
            (Vector((2.12,  -0.25, 0.13)), 0.13, 0.10),
            (Vector((2.78,  -0.32, 0.05)), 0.075, 0.06),
            (Vector((3.25,  -0.45, -0.02)), 0.032, 0.025),
        ],
        # Strand 7: Back-Right Ridge -> Rear-Right Sprawling Root (+X, +Y)
        [
            (Vector((0.18,   0.22, 2.70)), 0.13, 0.10),
            (Vector((0.42,   0.24, 1.65)), 0.21, 0.17),
            (Vector((0.65,   0.46, 0.78)), 0.24, 0.19),
            (Vector((1.12,   0.92, 0.30)), 0.19, 0.15),
            (Vector((1.72,   1.55, 0.14)), 0.13, 0.10),
            (Vector((2.28,   2.02, 0.05)), 0.075, 0.06),
            (Vector((2.68,   2.35, -0.02)), 0.032, 0.025),
        ],
        # Strand 8: Back-Center Buttress Ridge -> Deep Rear Root (+Y)
        [
            (Vector((-0.04,  0.26, 3.00)), 0.14, 0.11),
            (Vector((0.14,   0.32, 1.90)), 0.22, 0.18),
            (Vector((0.12,   0.56, 0.88)), 0.26, 0.21),
            (Vector((-0.02,  1.15, 0.34)), 0.21, 0.17),
            (Vector((-0.22,  1.88, 0.16)), 0.15, 0.11),
            (Vector((-0.35,  2.55, 0.06)), 0.085, 0.065),
            (Vector((-0.48,  3.02, -0.02)), 0.035, 0.028),
        ],
        # Strand 9: Back-Left Buttress Ridge -> Rear-Left Sprawling Root (-X, +Y)
        [
            (Vector((-0.20,  0.20, 2.65)), 0.13, 0.10),
            (Vector((-0.22,  0.30, 1.65)), 0.21, 0.17),
            (Vector((-0.48,  0.56, 0.75)), 0.24, 0.19),
            (Vector((-1.02,  1.05, 0.30)), 0.19, 0.15),
            (Vector((-1.68,  1.62, 0.14)), 0.13, 0.10),
            (Vector((-2.25,  2.12, 0.05)), 0.075, 0.06),
            (Vector((-2.65,  2.45, -0.02)), 0.032, 0.025),
        ],
    ]

    # 12 Sinuous Secondary Root Forks (M_WT_RootAccent)
    secondary_roots = [
        # Left outer undulating forks
        [
            (Vector((-1.45, -0.40, 0.22)), 0.13, 0.10),
            (Vector((-2.08, -0.08, 0.11)), 0.09, 0.07),
            (Vector((-2.68,  0.22, 0.04)), 0.055, 0.042),
            (Vector((-3.08,  0.32, -0.02)), 0.028, 0.022),
        ],
        [
            (Vector((-1.05, -1.32, 0.20)), 0.12, 0.095),
            (Vector((-1.78, -1.25, 0.10)), 0.085, 0.065),
            (Vector((-2.38, -1.35, 0.04)), 0.05, 0.04),
            (Vector((-2.78, -1.48, -0.02)), 0.026, 0.02),
        ],
        # Front-left inner fork
        [
            (Vector((-0.40, -1.52, 0.18)), 0.11, 0.085),
            (Vector((-0.22, -2.12, 0.09)), 0.075, 0.058),
            (Vector((-0.15, -2.62, -0.02)), 0.028, 0.022),
        ],
        # Front-center-right Y-fork (prominent foreground root split in reference!)
        [
            (Vector((0.30,  -1.35, 0.22)), 0.15, 0.12),
            (Vector((0.12,  -2.05, 0.10)), 0.10, 0.08),
            (Vector((0.02,  -2.62, 0.04)), 0.058, 0.045),
            (Vector((-0.05, -2.98, -0.02)), 0.028, 0.022),
        ],
        [
            (Vector((0.50,  -1.88, 0.13)), 0.11, 0.085),
            (Vector((1.02,  -2.32, 0.06)), 0.07, 0.055),
            (Vector((1.38,  -2.65, -0.02)), 0.028, 0.022),
        ],
        # Front-right forks
        [
            (Vector((1.28,  -0.98, 0.19)), 0.12, 0.09),
            (Vector((1.52,  -1.68, 0.09)), 0.08, 0.06),
            (Vector((1.72,  -2.25, -0.02)), 0.028, 0.022),
        ],
        [
            (Vector((1.88,  -0.18, 0.15)), 0.11, 0.085),
            (Vector((2.45,  -0.78, 0.07)), 0.07, 0.055),
            (Vector((2.85,  -1.15, -0.02)), 0.028, 0.022),
        ],
        # Right-back fork
        [
            (Vector((1.42,   0.00, 0.24)), 0.13, 0.10),
            (Vector((2.08,   0.52, 0.12)), 0.09, 0.07),
            (Vector((2.65,   0.85, 0.04)), 0.055, 0.042),
            (Vector((3.02,   1.02, -0.02)), 0.028, 0.022),
        ],
        # Back-right & Back-center forks
        [
            (Vector((1.38,   1.22, 0.18)), 0.12, 0.09),
            (Vector((1.15,   1.98, 0.09)), 0.08, 0.06),
            (Vector((1.02,   2.55, -0.02)), 0.028, 0.022),
        ],
        [
            (Vector((-0.10,  1.52, 0.20)), 0.13, 0.10),
            (Vector((0.38,   2.18, 0.09)), 0.085, 0.065),
            (Vector((0.72,   2.72, -0.02)), 0.028, 0.022),
        ],
        # Back-left forks
        [
            (Vector((-0.15,  1.65, 0.18)), 0.12, 0.09),
            (Vector((-0.85,  2.28, 0.09)), 0.08, 0.06),
            (Vector((-1.28,  2.75, -0.02)), 0.028, 0.022),
        ],
        [
            (Vector((-1.38,  1.35, 0.18)), 0.12, 0.09),
            (Vector((-2.15,  1.18, 0.09)), 0.08, 0.06),
            (Vector((-2.72,  1.08, -0.02)), 0.028, 0.022),
        ],
    ]

    return main_trunk, right_trunk_fork, primary_root_strands, secondary_roots


def build_trunk_and_roots_mesh(name, materials, lod=0):
    mesh = bpy.data.meshes.new(name)
    obj = bpy.data.objects.new(name, mesh)

    mesh.materials.append(materials["bark"])  # slot 0: M_WT_Bark
    mesh.materials.append(materials["root"])  # slot 1: M_WT_RootAccent

    bm = bmesh.new()
    uv_lay = bm.loops.layers.uv.verify()

    main_trunk, right_trunk_fork, primary_strands, secondary_roots = get_trunk_and_roots_spec()

    if lod == 0:
        trunk_samples, trunk_rad = 18, 12
        rfork_samples, rfork_rad = 9, 8
        strand_samples, strand_rad = 12, 7
        sec_samples, sec_rad = 7, 5
    elif lod == 1:
        trunk_samples, trunk_rad = 12, 9
        rfork_samples, rfork_rad = 6, 6
        strand_samples, strand_rad = 8, 5
        sec_samples, sec_rad = 5, 4
    else:
        trunk_samples, trunk_rad = 8, 7
        rfork_samples, rfork_rad = 5, 5
        strand_samples, strand_rad = 5, 4
        sec_samples, sec_rad = 0, 0

    # Main Trunk (M_WT_Bark)
    add_manifold_fluted_tube(
        bm, uv_lay,
        sample_spline_tube(main_trunk, trunk_samples),
        num_radial=trunk_rad,
        flute_count=6,
        flute_depth=0.055,
        twist_total=math.radians(50.0),
        mat_idx=0,
        u_tile=2.0,
        v_tile=4.0,
    )

    # Secondary Upper-Right Trunk Fork (M_WT_Bark)
    add_manifold_fluted_tube(
        bm, uv_lay,
        sample_spline_tube(right_trunk_fork, rfork_samples),
        num_radial=rfork_rad,
        flute_count=4,
        flute_depth=0.04,
        twist_total=math.radians(25.0),
        mat_idx=0,
        u_tile=1.0,
        v_tile=2.5,
    )

    # 9 Continuous Trunk-Ridge-to-Sprawling-Root Strands (entire strand uses M_WT_Bark so zero ring seam!)
    for idx, strand in enumerate(primary_strands):
        if lod == 2 and idx % 2 == 1:
            continue
        add_manifold_fluted_tube(
            bm, uv_lay,
            sample_spline_tube(strand, strand_samples),
            num_radial=strand_rad,
            flute_count=3 if lod == 0 else 0,
            flute_depth=0.035 if lod == 0 else 0.0,
            twist_total=math.radians(18.0),
            ground_clamp_z=-0.04,
            mat_idx=0,
            u_tile=1.0,
            v_tile=3.0,
        )

    # 12 Secondary Forking Root Tendrils (uses M_WT_RootAccent smoothly from junction to tip!)
    if sec_samples > 0:
        for idx, sroot in enumerate(secondary_roots):
            if lod == 1 and idx % 2 == 1:
                continue
            add_manifold_fluted_tube(
                bm, uv_lay,
                sample_spline_tube(sroot, sec_samples),
                num_radial=sec_rad,
                flute_count=0,
                flute_depth=0.0,
                ground_clamp_z=-0.04,
                mat_idx=1,
                u_tile=1.0,
                v_tile=1.5,
            )

    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(mesh)
    bm.free()
    return obj


# ==============================================================================
# 5. ORGANIC CURVING BONSAI LIMBS & CANOPY PAD DEFINITIONS
# ==============================================================================

def get_branches_and_canopy_spec():
    """
    Defines curving bonsai-style limbs and 24 canopy pads (20 main pads + 4 small trunk/limb sprouts).
    Notice how every primary limb dips/sweeps horizontally and then curves gracefully upward at its tip!
    """
    primary_limbs = [
        # 1. Low-Right Sweeping Limb (splits at z=1.42m from rightward trunk knee)
        [
            (Vector((0.28, -0.04, 1.40)), 0.24, 0.23),
            (Vector((0.98,  0.00, 1.72)), 0.19, 0.18),
            (Vector((1.75,  0.06, 2.18)), 0.14, 0.14),
            (Vector((2.42,  0.08, 2.78)), 0.10, 0.10),
            (Vector((2.88,  0.05, 3.32)), 0.06, 0.06),
        ],
        # 2. Lower-Left Sweeping Muscular Limb (splits at z=2.60m, dips slightly then sweeps up-left)
        [
            (Vector((-0.02,  0.02, 2.58)), 0.26, 0.25),
            (Vector((-0.95, -0.08, 2.85)), 0.21, 0.20),
            (Vector((-1.92, -0.14, 3.22)), 0.16, 0.15),
            (Vector((-2.78, -0.10, 3.68)), 0.11, 0.11),
            (Vector((-3.42, -0.05, 4.12)), 0.06, 0.06),
        ],
        # 3. Lower-Back Sweeping Limb (splits at z=2.35m into +Y for rear lower-mid depth)
        [
            (Vector((0.08,  0.06, 2.32)), 0.21, 0.20),
            (Vector((-0.22, 0.95, 2.68)), 0.16, 0.15),
            (Vector((-0.52, 1.82, 3.18)), 0.11, 0.11),
            (Vector((-0.72, 2.45, 3.68)), 0.06, 0.06),
        ],
        # 4. Lower-Front-Left Depth Limb (splits at z=3.15m into -Y/-X)
        [
            (Vector((-0.14, -0.02, 3.15)), 0.17, 0.16),
            (Vector((-0.68, -0.95, 3.55)), 0.13, 0.12),
            (Vector((-1.18, -1.78, 4.05)), 0.09, 0.09),
            (Vector((-1.48, -2.32, 4.52)), 0.05, 0.05),
        ],
        # 5. Mid-Right Sweeping Limb (from right_trunk_fork at z=4.05m out to far right)
        [
            (Vector((0.68, -0.05, 4.02)), 0.19, 0.18),
            (Vector((1.52, -0.08, 4.42)), 0.15, 0.14),
            (Vector((2.35, -0.06, 4.85)), 0.11, 0.11),
            (Vector((3.02, -0.02, 5.32)), 0.06, 0.06),
        ],
        # 6. Mid-Upper Left Sweeping Limb (splits at z=4.30m, sweeps wide left and up)
        [
            (Vector((-0.14, 0.04, 4.28)), 0.21, 0.20),
            (Vector((-1.02, 0.08, 4.68)), 0.16, 0.15),
            (Vector((-1.95, 0.06, 5.15)), 0.12, 0.11),
            (Vector((-2.78, 0.00, 5.58)), 0.08, 0.08),
            (Vector((-3.32, -0.05, 5.95)), 0.05, 0.05),
        ],
        # 7. Mid-Back Sweeping Limb (splits at z=4.50m into +Y for lush rear silhouette)
        [
            (Vector((-0.08, 0.06, 4.48)), 0.18, 0.17),
            (Vector((0.18,  1.05, 4.88)), 0.14, 0.13),
            (Vector((0.35,  1.92, 5.35)), 0.10, 0.10),
            (Vector((0.42,  2.55, 5.82)), 0.05, 0.05),
        ],
        # 8. Mid-Front-Right Depth Limb (splits from right fork at z=4.05m into -Y/+X)
        [
            (Vector((0.64, -0.06, 4.02)), 0.15, 0.14),
            (Vector((0.95, -1.02, 4.45)), 0.11, 0.11),
            (Vector((1.24, -1.85, 4.92)), 0.08, 0.08),
            (Vector((1.42, -2.35, 5.32)), 0.05, 0.05),
        ],
        # 9. Upper-Right Limb (splits at z=5.60m, supports Upper-Right Canopy Tier)
        [
            (Vector((0.02,  0.00, 5.58)), 0.16, 0.15),
            (Vector((0.82, -0.04, 6.02)), 0.12, 0.12),
            (Vector((1.62,  0.02, 6.48)), 0.09, 0.09),
            (Vector((2.18,  0.05, 6.95)), 0.05, 0.05),
        ],
        # 10. Upper-Left Sub-Crown Limb (splits at z=6.15m, supports Upper-Left Sub-Crown)
        [
            (Vector((0.02,  0.00, 6.15)), 0.15, 0.14),
            (Vector((-0.68, -0.04, 6.58)), 0.11, 0.11),
            (Vector((-1.38,  0.02, 7.05)), 0.08, 0.08),
            (Vector((-1.85,  0.05, 7.45)), 0.05, 0.05),
        ],
        # 11. Upper-Back & Upper-Front Depth Limbs (splits at z=6.40m into +Y and -Y)
        [
            (Vector((0.02,  0.02, 6.40)), 0.13, 0.13),
            (Vector((0.08,  0.88, 6.85)), 0.09, 0.09),
            (Vector((0.12,  1.62, 7.35)), 0.05, 0.05),
        ],
        [
            (Vector((0.02, -0.02, 6.42)), 0.12, 0.12),
            (Vector((-0.08, -0.82, 6.88)), 0.08, 0.08),
            (Vector((-0.15, -1.48, 7.38)), 0.05, 0.05),
        ],
    ]

    secondary_forks = [
        # Low-Right sub-forks & low trunk sprout twig
        [(Vector((0.82, -0.02, 1.65)), 0.07, 0.07), (Vector((1.12, -0.18, 1.64)), 0.045, 0.045), (Vector((1.35, -0.26, 1.72)), 0.03, 0.03)],
        [(Vector((1.75,  0.06, 2.18)), 0.11, 0.11), (Vector((1.95, -0.55, 2.68)), 0.075, 0.075), (Vector((2.10, -0.92, 3.18)), 0.045, 0.045)],
        [(Vector((1.95,  0.07, 2.38)), 0.10, 0.10), (Vector((2.32,  0.68, 2.88)), 0.07, 0.07),   (Vector((2.58,  1.08, 3.35)), 0.045, 0.045)],
        [(Vector((1.85,  0.06, 2.28)), 0.10, 0.10), (Vector((1.65,  0.12, 2.95)), 0.065, 0.065), (Vector((1.55,  0.15, 3.45)), 0.04, 0.04)],

        # Lower-Left sub-forks & inner sprout twigs
        [(Vector((-0.95, -0.08, 2.85)), 0.08, 0.08), (Vector((-1.28, -0.22, 2.68)), 0.05, 0.05), (Vector((-1.55, -0.28, 2.68)), 0.03, 0.03)],
        [(Vector((-0.98, -0.08, 2.88)), 0.08, 0.08), (Vector((-1.02, -0.18, 3.35)), 0.05, 0.05), (Vector((-1.05, -0.22, 3.75)), 0.03, 0.03)],
        [(Vector((-1.92, -0.14, 3.22)), 0.12, 0.12), (Vector((-2.22, -0.82, 3.65)), 0.08, 0.08), (Vector((-2.45, -1.28, 4.05)), 0.045, 0.045)],
        [(Vector((-2.15, -0.12, 3.38)), 0.11, 0.11), (Vector((-2.45,  0.62, 3.78)), 0.075, 0.075), (Vector((-2.68,  1.10, 4.15)), 0.045, 0.045)],
        [(Vector((-1.95, -0.14, 3.25)), 0.10, 0.10), (Vector((-1.98, -0.05, 3.82)), 0.065, 0.065), (Vector((-2.02, -0.05, 4.22)), 0.04, 0.04)],

        # Lower-Back sub-fork
        [(Vector((-0.42, 1.55, 3.05)), 0.10, 0.10), (Vector((0.25, 2.05, 3.42)), 0.065, 0.065), (Vector((0.65, 2.38, 3.75)), 0.04, 0.04)],

        # Mid-Right sub-forks
        [(Vector((1.18, -0.04, 4.35)), 0.11, 0.11), (Vector((1.15, -0.25, 4.82)), 0.07, 0.07), (Vector((1.15, -0.32, 5.18)), 0.045, 0.045)],
        [(Vector((1.82, -0.07, 4.60)), 0.11, 0.11), (Vector((1.98,  0.72, 5.02)), 0.075, 0.075), (Vector((2.12,  1.32, 5.38)), 0.045, 0.045)],

        # Mid-Upper Left sub-forks
        [(Vector((-1.15, 0.07, 4.78)), 0.11, 0.11), (Vector((-1.02, -0.22, 5.28)), 0.07, 0.07), (Vector((-0.92, -0.32, 5.68)), 0.045, 0.045)],
        [(Vector((-1.95, 0.06, 5.15)), 0.11, 0.11), (Vector((-2.10,  0.82, 5.55)), 0.075, 0.075), (Vector((-2.20,  1.38, 5.88)), 0.045, 0.045)],
        [(Vector((-1.95, 0.06, 5.15)), 0.10, 0.10), (Vector((-1.88,  0.05, 5.55)), 0.065, 0.065), (Vector((-1.85,  0.05, 5.82)), 0.04, 0.04)],

        # Mid-Back sub-fork
        [(Vector((0.28, 1.55, 5.18)), 0.10, 0.10), (Vector((-0.52, 2.12, 5.52)), 0.065, 0.065), (Vector((-0.92, 2.45, 5.82)), 0.04, 0.04)],

        # Upper-Right & Mid-Upper Trunk Sprout sub-forks
        [(Vector((0.02, 0.01, 5.45)), 0.07, 0.07), (Vector((0.48, 0.15, 5.78)), 0.05, 0.05), (Vector((0.75, 0.22, 6.02)), 0.03, 0.03)],
        [(Vector((1.35, 0.00, 6.32)), 0.09, 0.09), (Vector((1.52, -0.58, 6.68)), 0.06, 0.06), (Vector((1.65, -0.95, 7.02)), 0.04, 0.04)],

        # Apex Crown Forks
        [(Vector((0.00, 0.00, 7.30)), 0.09, 0.09), (Vector((-0.55, 0.05, 7.88)), 0.06, 0.06), (Vector((-0.88, 0.08, 8.28)), 0.035, 0.035)],
        [(Vector((0.00, 0.00, 7.30)), 0.09, 0.09), (Vector((0.55, -0.05, 7.85)), 0.06, 0.06), (Vector((0.88, -0.08, 8.25)), 0.035, 0.035)],
        [(Vector((0.00, 0.00, 7.50)), 0.08, 0.08), (Vector((0.00,  0.02, 8.15)), 0.05, 0.05), (Vector((0.00,  0.02, 8.58)), 0.03, 0.03)],
    ]

    # 26 Canopy Pads (4 small trunk/limb sprouts + 22 main canopy pads)
    canopy_clusters = [
        # --- 4 Small Charming Trunk/Limb Sprout Tufts ---
        {"name": "Sprout_Low_Right",   "center": Vector((1.38, -0.28, 1.75)), "rx": 0.48, "ry": 0.45, "rz": 0.30, "tilt": Vector((0.30, -0.18, 0.94)), "sprout": True},
        {"name": "Sprout_Low_Left_In", "center": Vector((-1.58, -0.30, 2.70)), "rx": 0.52, "ry": 0.48, "rz": 0.32, "tilt": Vector((-0.28, -0.18, 0.94)), "sprout": True},
        {"name": "Sprout_Low_Left_Up", "center": Vector((-1.05, -0.24, 3.80)), "rx": 0.50, "ry": 0.48, "rz": 0.30, "tilt": Vector((-0.15, -0.15, 0.98)), "sprout": True},
        {"name": "Sprout_Upper_Trunk", "center": Vector((0.78,  0.24, 6.08)), "rx": 0.56, "ry": 0.52, "rz": 0.34, "tilt": Vector((0.20,  0.10, 0.97)), "sprout": True},

        # --- Tier 1: Lower-Right Canopy Mass (z ~ 3.0 - 3.9m) ---
        {"name": "LowRight_Main",      "center": Vector((2.88,  0.05, 3.42)), "rx": 1.22, "ry": 1.12, "rz": 0.62, "tilt": Vector((0.22,  0.00, 0.97)), "sprout": False},
        {"name": "LowRight_Inner",     "center": Vector((1.58,  0.15, 3.55)), "rx": 1.02, "ry": 0.96, "rz": 0.54, "tilt": Vector((-0.08, 0.05, 0.99)), "sprout": False},
        {"name": "LowRight_Front",     "center": Vector((2.12, -0.92, 3.28)), "rx": 1.02, "ry": 0.96, "rz": 0.54, "tilt": Vector((0.16, -0.22, 0.96)), "sprout": False},
        {"name": "LowRight_Back",      "center": Vector((2.58,  1.08, 3.42)), "rx": 1.02, "ry": 0.96, "rz": 0.54, "tilt": Vector((0.16,  0.22, 0.96)), "sprout": False},

        # --- Tier 2: Lower-Left Sweeping Canopy Tier (z ~ 3.5 - 4.7m) ---
        {"name": "LowLeft_Outer",      "center": Vector((-3.38, -0.05, 4.18)), "rx": 1.22, "ry": 1.12, "rz": 0.62, "tilt": Vector((-0.24, 0.00, 0.97)), "sprout": False},
        {"name": "LowLeft_Mid",        "center": Vector((-2.02, -0.05, 4.30)), "rx": 1.15, "ry": 1.08, "rz": 0.58, "tilt": Vector((-0.10, 0.00, 0.99)), "sprout": False},
        {"name": "LowLeft_Front",      "center": Vector((-2.45, -1.28, 4.12)), "rx": 1.05, "ry": 1.00, "rz": 0.54, "tilt": Vector((-0.18, -0.22, 0.96)), "sprout": False},
        {"name": "LowLeft_Back",       "center": Vector((-2.68,  1.10, 4.20)), "rx": 1.05, "ry": 1.00, "rz": 0.54, "tilt": Vector((-0.18,  0.22, 0.96)), "sprout": False},

        # --- Tier 3: Lower-Back & Lower-Front Depth Clusters ---
        {"name": "LowBack_Center",     "center": Vector((-0.72,  2.42, 3.76)), "rx": 1.22, "ry": 1.14, "rz": 0.60, "tilt": Vector((-0.08, 0.24, 0.97)), "sprout": False},
        {"name": "LowBack_Right",      "center": Vector((0.65,   2.38, 3.80)), "rx": 1.08, "ry": 1.02, "rz": 0.56, "tilt": Vector((0.10,  0.24, 0.96)), "sprout": False},
        {"name": "LowFront_Left",      "center": Vector((-1.48, -2.30, 4.56)), "rx": 1.14, "ry": 1.08, "rz": 0.58, "tilt": Vector((-0.14, -0.24, 0.96)), "sprout": False},

        # --- Tier 4: Mid-Right Broad Canopy Tier (z ~ 4.8 - 6.0m) ---
        {"name": "MidRight_Outer",     "center": Vector((3.02, -0.02, 5.38)), "rx": 1.26, "ry": 1.15, "rz": 0.62, "tilt": Vector((0.22,  0.00, 0.97)), "sprout": False},
        {"name": "MidRight_Inner",     "center": Vector((1.15, -0.32, 5.25)), "rx": 1.15, "ry": 1.06, "rz": 0.58, "tilt": Vector((0.06, -0.08, 0.99)), "sprout": False},
        {"name": "MidRight_Front",     "center": Vector((1.42, -2.32, 5.36)), "rx": 1.14, "ry": 1.08, "rz": 0.58, "tilt": Vector((0.14, -0.24, 0.96)), "sprout": False},
        {"name": "MidRight_Back",      "center": Vector((2.12,  1.32, 5.42)), "rx": 1.12, "ry": 1.05, "rz": 0.58, "tilt": Vector((0.16,  0.20, 0.97)), "sprout": False},

        # --- Tier 5: Mid-Upper Left Broad Canopy Tier (z ~ 5.2 - 6.5m) ---
        {"name": "MidLeft_Outer",      "center": Vector((-3.28, -0.05, 5.98)), "rx": 1.24, "ry": 1.14, "rz": 0.62, "tilt": Vector((-0.22, 0.00, 0.97)), "sprout": False},
        {"name": "MidLeft_Mid",        "center": Vector((-1.85,  0.05, 5.88)), "rx": 1.20, "ry": 1.12, "rz": 0.60, "tilt": Vector((-0.10, 0.00, 0.99)), "sprout": False},
        {"name": "MidLeft_Inner",      "center": Vector((-0.92, -0.32, 5.74)), "rx": 1.02, "ry": 0.96, "rz": 0.52, "tilt": Vector((-0.05, -0.08, 0.99)), "sprout": False},
        {"name": "MidLeft_Back",       "center": Vector((-2.20,  1.38, 5.92)), "rx": 1.10, "ry": 1.04, "rz": 0.56, "tilt": Vector((-0.16, 0.20, 0.97)), "sprout": False},

        # --- Tier 6: Mid-Back Canopy Tier (z ~ 5.5 - 6.4m) ---
        {"name": "MidBack_Center",     "center": Vector((0.42,   2.52, 5.86)), "rx": 1.24, "ry": 1.15, "rz": 0.60, "tilt": Vector((0.05,  0.24, 0.97)), "sprout": False},
        {"name": "MidBack_Left",       "center": Vector((-0.92,  2.45, 5.86)), "rx": 1.12, "ry": 1.05, "rz": 0.56, "tilt": Vector((-0.10, 0.22, 0.97)), "sprout": False},

        # --- Tier 7: Upper-Right & Upper-Left Sub-Crown Tiers (z ~ 6.6 - 7.9m) ---
        {"name": "UpperRight_Main",    "center": Vector((2.18,   0.05, 7.00)), "rx": 1.20, "ry": 1.12, "rz": 0.60, "tilt": Vector((0.20,  0.00, 0.98)), "sprout": False},
        {"name": "UpperRight_Front",   "center": Vector((1.65,  -0.95, 7.05)), "rx": 1.00, "ry": 0.95, "rz": 0.52, "tilt": Vector((0.14, -0.18, 0.97)), "sprout": False},
        {"name": "UpperLeft_Main",     "center": Vector((-1.85,  0.05, 7.48)), "rx": 1.20, "ry": 1.12, "rz": 0.60, "tilt": Vector((-0.20, 0.00, 0.98)), "sprout": False},
        {"name": "UpperBack_Main",     "center": Vector((0.12,   1.62, 7.40)), "rx": 1.20, "ry": 1.12, "rz": 0.58, "tilt": Vector((0.02,  0.22, 0.97)), "sprout": False},
        {"name": "UpperFront_Main",    "center": Vector((-0.15, -1.48, 7.42)), "rx": 1.15, "ry": 1.08, "rz": 0.58, "tilt": Vector((-0.02, -0.22, 0.97)), "sprout": False},

        # --- Tier 8: Central Apex Crown (z ~ 7.8 - 9.15m) ---
        {"name": "Apex_Left",          "center": Vector((-0.88,  0.08, 8.32)), "rx": 1.04, "ry": 1.00, "rz": 0.56, "tilt": Vector((-0.14, 0.02, 0.99)), "sprout": False},
        {"name": "Apex_Right",         "center": Vector((0.88,  -0.08, 8.28)), "rx": 1.04, "ry": 1.00, "rz": 0.56, "tilt": Vector((0.14, -0.02, 0.99)), "sprout": False},
        {"name": "Apex_Top_Crown",     "center": Vector((0.00,   0.02, 8.60)), "rx": 1.22, "ry": 1.16, "rz": 0.62, "tilt": Vector((0.00,  0.00, 1.00)), "sprout": False},
    ]

    return primary_limbs, secondary_forks, canopy_clusters


def build_branches_mesh(name, materials, lod=0):
    mesh = bpy.data.meshes.new(name)
    obj = bpy.data.objects.new(name, mesh)
    mesh.materials.append(materials["bark"])

    bm = bmesh.new()
    uv_lay = bm.loops.layers.uv.verify()

    primary_limbs, secondary_forks, _ = get_branches_and_canopy_spec()

    if lod == 0:
        p_samples, p_rad = 9, 8
        s_samples, s_rad = 5, 5
    elif lod == 1:
        p_samples, p_rad = 6, 6
        s_samples, s_rad = 4, 4
    else:
        p_samples, p_rad = 5, 5
        s_samples, s_rad = 0, 0

    for limb in primary_limbs:
        add_manifold_fluted_tube(
            bm, uv_lay,
            sample_spline_tube(limb, p_samples),
            num_radial=p_rad,
            flute_count=3 if lod == 0 else 0,
            flute_depth=0.035 if lod == 0 else 0.0,
            twist_total=math.radians(15.0),
            mat_idx=0,
            u_tile=1.0,
            v_tile=2.5,
        )

    if s_samples > 0:
        for idx, fork in enumerate(secondary_forks):
            if lod == 1 and idx % 2 == 1 and idx > 4:
                continue
            add_manifold_fluted_tube(
                bm, uv_lay,
                sample_spline_tube(fork, s_samples),
                num_radial=s_rad,
                flute_count=0,
                flute_depth=0.0,
                mat_idx=0,
                u_tile=1.0,
                v_tile=1.5,
            )

    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(mesh)
    bm.free()
    return obj


# ==============================================================================
# 6. 100% SHINGLED 3D LEAF CANOPY PADS (ZERO BALD DOMES, 8K-15K TRIS TARGET!)
# ==============================================================================

def add_manifold_3d_ovate_leaf(bm, uv_lay, transform_mat, length=0.78, width=0.48, droop=0.14, mat_idx=0):
    """
    Creates a 100% closed 2-manifold 3D curved V-folded leaf (6 vertices, 8 triangles).
    Generous botanical width/length ratio so shingled rings overlap into lush, solid foliage clouds.
    """
    L = length
    W = width
    h_arch = 0.11 * L
    h_fold = 0.12 * W
    h_keel = 0.06 * W
    h_droop = -droop * L

    v_base = bm.verts.new(transform_mat @ Vector((0.0, 0.0, 0.0)))
    v_left = bm.verts.new(transform_mat @ Vector((-0.5 * W, 0.46 * L, h_arch)))
    v_right = bm.verts.new(transform_mat @ Vector((0.5 * W, 0.46 * L, h_arch)))
    v_tip = bm.verts.new(transform_mat @ Vector((0.0, L, h_droop)))
    v_top = bm.verts.new(transform_mat @ Vector((0.0, 0.44 * L, h_arch + h_fold)))
    v_bot = bm.verts.new(transform_mat @ Vector((0.0, 0.44 * L, h_arch - h_keel)))

    uv_base = (0.5, 0.02)
    uv_left = (0.04, 0.48)
    uv_right = (0.96, 0.48)
    uv_tip = (0.5, 0.98)
    uv_mid = (0.5, 0.46)

    tri_specs = [
        ((v_base, v_left, v_top), (uv_base, uv_left, uv_mid)),
        ((v_base, v_top, v_right), (uv_base, uv_mid, uv_right)),
        ((v_top, v_left, v_tip), (uv_mid, uv_left, uv_tip)),
        ((v_top, v_tip, v_right), (uv_mid, uv_tip, uv_right)),
        ((v_base, v_bot, v_left), (uv_base, uv_mid, uv_left)),
        ((v_base, v_right, v_bot), (uv_base, uv_right, uv_mid)),
        ((v_bot, v_tip, v_left), (uv_mid, uv_tip, uv_left)),
        ((v_bot, v_right, v_tip), (uv_mid, uv_right, uv_tip)),
    ]

    for verts, uvs in tri_specs:
        f = bm.faces.new(verts)
        f.material_index = mat_idx
        f.smooth = True
        for loop, uv in zip(f.loops, uvs):
            loop[uv_lay].uv = uv


def build_shingled_canopy_pad(bm, uv_lay, center, rx, ry, rz, tilt_dir, seed_idx=0, is_sprout=False, lod=0):
    """
    Builds a voluminous, 100% leaf-shingled botanical canopy pad (no smooth dome balls!).
    Uses concentric overlapping shingle rings + central star-crown + dark interior shadow cushion leaves:
    - Upper / outer sunlit layers use `M_WT_Leaves_Light` (slot 0)
    - Lower skirt / interior shadow cushion layers use `M_WT_Leaves_Dark` (slot 1)
    """
    n_up = tilt_dir.normalized()
    ref_fwd = Vector((0, 1, 0)) if abs(n_up.z) < 0.88 else Vector((1, 0, 0))
    n_right = ref_fwd.cross(n_up).normalized()
    n_fwd = n_up.cross(n_right).normalized()

    pad_basis = Matrix((
        (n_right.x, n_fwd.x, n_up.x, center.x),
        (n_right.y, n_fwd.y, n_up.y, center.y),
        (n_right.z, n_fwd.z, n_up.z, center.z),
        (0.0,       0.0,     0.0,    1.0),
    ))

    avg_r = 0.5 * (rx + ry)

    if is_sprout:
        # Compact 2-tier sprout tuft on trunk/lower branches
        rings = [
            # (z_ratio, rad_ratio, count, pitch_deg, len_sc, wid_sc, droop, mat_mode)
            (0.45, 0.08, 4 if lod == 0 else 3,  34.0, 0.52, 0.32, 0.10, "light"),
            (0.12, 0.32, 6 if lod == 0 else 4,   6.0, 0.58, 0.36, 0.14, "light"),
            (-0.15, 0.35, 5 if lod == 0 else 3, -22.0, 0.54, 0.34, 0.16, "dark"),
        ]
    elif lod == 0:
        # Primary LOD0: 44 overlapping 3D leaves per major pad (~352 tris/pad -> ~12.8k total asset tris!)
        rings = [
            # 1. Lively upward crest sprout & top star rosette (Light lime-green)
            (0.92, 0.05,  4,  32.0, 0.72 * avg_r, 0.46 * avg_r, 0.10, "light"),
            # 2. Inner upper shingle tier (Light lime-green)
            (0.72, 0.28,  7,  18.0, 0.82 * avg_r, 0.54 * avg_r, 0.12, "light"),
            # 3. Mid-upper broad shingle tier (Light lime-green)
            (0.44, 0.52, 10,   6.0, 0.88 * avg_r, 0.58 * avg_r, 0.14, "light"),
            # 4. Main outer equator silhouette rim (Alternating Light & Dark for lively leaf tufts)
            (0.14, 0.72, 11,  -8.0, 0.88 * avg_r, 0.56 * avg_r, 0.16, "alt_light"),
            # 5. Lower drooping shadow skirt (Dark forest-green underside)
            (-0.18, 0.68, 8, -26.0, 0.82 * avg_r, 0.54 * avg_r, 0.18, "dark"),
            # 6. Deep interior shadow core cushion (Broad Dark leaves sealing the interior against any gaps!)
            (0.18, 0.15,  5,   4.0, 1.05 * avg_r, 0.78 * avg_r, 0.08, "dark"),
        ]
    elif lod == 1:
        # LOD1: 24 leaves per major pad (~6.5k total asset tris)
        rings = [
            (0.88, 0.06, 3,  28.0, 0.85 * avg_r, 0.58 * avg_r, 0.10, "light"),
            (0.56, 0.36, 6,  12.0, 0.95 * avg_r, 0.65 * avg_r, 0.13, "light"),
            (0.18, 0.66, 8,  -6.0, 0.96 * avg_r, 0.66 * avg_r, 0.15, "alt_light"),
            (-0.18, 0.62, 5, -24.0, 0.88 * avg_r, 0.60 * avg_r, 0.18, "dark"),
            (0.20, 0.14, 3,   4.0, 1.15 * avg_r, 0.85 * avg_r, 0.08, "dark"),
        ]
    else:
        # LOD2: 11 broad leaves per major pad (~2.8k total asset tris)
        rings = [
            (0.75, 0.10, 3,  22.0, 1.08 * avg_r, 0.78 * avg_r, 0.12, "light"),
            (0.24, 0.52, 5,   0.0, 1.15 * avg_r, 0.82 * avg_r, 0.15, "light"),
            (-0.15, 0.48, 3, -22.0, 1.08 * avg_r, 0.78 * avg_r, 0.18, "dark"),
        ]

    for r_idx, (z_u, rad_u, count, pitch_deg, l_len, l_wid, drp, mat_mode) in enumerate(rings):
        phase = seed_idx * 0.63 + r_idx * 0.78
        for i in range(count):
            # Organic angular & radial jitter so leaves form natural clusters rather than mechanical gears
            ang_jitter = 0.14 * math.sin(i * 2.7 + r_idx * 1.9 + seed_idx)
            ang = (2.0 * math.pi * i / count) + phase + ang_jitter

            # 3-lobe subtle horizontal cloud undulation
            lobe_mod = 1.0 + 0.10 * math.cos(3.0 * ang + seed_idx * 1.3)
            lx = math.cos(ang) * rx * rad_u * lobe_mod
            ly = math.sin(ang) * ry * rad_u * lobe_mod
            lz = rz * z_u + 0.04 * rz * math.cos(i * 1.7 + r_idx)
            local_pos = Vector((lx, ly, lz))

            local_fwd = Vector((math.cos(ang) * rx, math.sin(ang) * ry, 0.0)).normalized()
            local_up = Vector((0.0, 0.0, 1.0))
            local_rt = local_fwd.cross(local_up).normalized()

            local_frame = Matrix((
                (local_rt.x, local_fwd.x, local_up.x, local_pos.x),
                (local_rt.y, local_fwd.y, local_up.y, local_pos.y),
                (local_rt.z, local_fwd.z, local_up.z, local_pos.z),
                (0.0,        0.0,         0.0,        1.0),
            ))

            pitch_var = pitch_deg + 6.0 * math.sin(i * 3.1 + seed_idx)
            roll_var = 9.0 * math.cos(i * 2.3 + r_idx)
            yaw_var = 10.0 * math.sin(i * 1.9 - r_idx)
            rot = (
                Matrix.Rotation(math.radians(yaw_var), 4, 'Z')
                @ Matrix.Rotation(math.radians(-pitch_var), 4, 'X')
                @ Matrix.Rotation(math.radians(roll_var), 4, 'Y')
            )
            leaf_world_mat = pad_basis @ local_frame @ rot

            sc_var = 0.92 + 0.16 * (0.5 + 0.5 * math.sin(i * 4.1 + r_idx + seed_idx))
            if mat_mode == "light":
                mat_idx = 0
            elif mat_mode == "dark":
                mat_idx = 1
            else:
                mat_idx = 1 if (i % 4 == 0) else 0

            add_manifold_3d_ovate_leaf(
                bm, uv_lay,
                leaf_world_mat,
                length=l_len * sc_var,
                width=l_wid * sc_var,
                droop=drp,
                mat_idx=mat_idx,
            )


def build_foliage_mesh(name, materials, lod=0):
    mesh = bpy.data.meshes.new(name)
    obj = bpy.data.objects.new(name, mesh)

    # Slot 0: M_WT_Leaves_Light, Slot 1: M_WT_Leaves_Dark
    mesh.materials.append(materials["leaf_light"])
    mesh.materials.append(materials["leaf_dark"])

    bm = bmesh.new()
    uv_lay = bm.loops.layers.uv.verify()

    _, _, canopy_clusters = get_branches_and_canopy_spec()

    for c_idx, cluster in enumerate(canopy_clusters):
        if lod == 2 and cluster["sprout"]:
            continue
        build_shingled_canopy_pad(
            bm, uv_lay,
            center=cluster["center"],
            rx=cluster["rx"],
            ry=cluster["ry"],
            rz=cluster["rz"],
            tilt_dir=cluster["tilt"],
            seed_idx=c_idx,
            is_sprout=cluster["sprout"],
            lod=lod,
        )

    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(mesh)
    bm.free()
    return obj


# ==============================================================================
# 7. MESH HEALTH AUDIT & FBX EXPORT
# ==============================================================================

def audit_mesh_objects(objs):
    report = {"objects": {}, "total_verts": 0, "total_tris": 0}
    all_min = [1e9, 1e9, 1e9]
    all_max = [-1e9, -1e9, -1e9]

    for obj in objs:
        m = obj.data
        bm = bmesh.new()
        bm.from_mesh(m)
        verts = len(bm.verts)
        tris = sum(len(f.verts) - 2 for f in bm.faces)
        non_manifold = sum(1 for e in bm.edges if not e.is_manifold)
        boundary = sum(1 for e in bm.edges if e.is_boundary)
        loose_v = sum(1 for v in bm.verts if not v.link_edges)
        degen_f = sum(1 for f in bm.faces if f.calc_area() < 1e-7)

        for v in bm.verts:
            co = obj.matrix_world @ v.co
            for k in range(3):
                all_min[k] = min(all_min[k], co[k])
                all_max[k] = max(all_max[k], co[k])
        bm.free()

        report["objects"][obj.name] = {
            "verts": verts,
            "tris": tris,
            "non_manifold_edges": non_manifold,
            "boundary_edges": boundary,
            "loose_verts": loose_v,
            "degenerate_faces": degen_f,
            "location": list(obj.location),
            "rotation_euler": list(obj.rotation_euler),
            "scale": list(obj.scale),
            "materials": [s.material.name for s in obj.material_slots if s.material],
        }
        report["total_verts"] += verts
        report["total_tris"] += tris

    report["bbox_min"] = [round(x, 4) for x in all_min]
    report["bbox_max"] = [round(x, 4) for x in all_max]
    report["dimensions"] = [round(all_max[k] - all_min[k], 4) for k in range(3)]
    return report


def export_lod_fbx(objs, filepath):
    Path(filepath).parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.object.select_all(action='DESELECT')
    for o in objs:
        o.hide_set(False)
        o.select_set(True)

    bpy.ops.export_scene.fbx(
        filepath=str(filepath),
        use_selection=True,
        global_scale=1.0,
        apply_unit_scale=True,
        apply_scale_options='FBX_SCALE_ALL',
        axis_forward='-Z',
        axis_up='Y',
        object_types={'MESH'},
        use_mesh_modifiers=True,
        mesh_smooth_type='FACE',
        path_mode='RELATIVE',
        embed_textures=False,
        bake_anim=False,
    )
    bpy.ops.object.select_all(action='DESELECT')


# ==============================================================================
# 8. NEUTRAL STUDIO LIGHTING & MULTI-ANGLE RENDERING (PBR + SOLID CAVITY)
# ==============================================================================

def setup_studio_preview(collection):
    scene = bpy.context.scene
    scene.render.engine = 'BLENDER_EEVEE'
    scene.render.resolution_x = 1200
    scene.render.resolution_y = 1200
    scene.render.film_transparent = False
    scene.render.image_settings.file_format = 'PNG'

    world = bpy.data.worlds.new("WT_Studio_World")
    world.use_nodes = True
    bg = next(n for n in world.node_tree.nodes if n.type == 'BACKGROUND')
    bg.inputs[0].default_value = (0.11, 0.12, 0.14, 1.0)
    bg.inputs[1].default_value = 0.90
    scene.world = world

    cam_data = bpy.data.cameras.new("WT_PreviewCamera")
    cam_data.lens = 45.0
    cam_data.clip_end = 200.0
    cam_obj = bpy.data.objects.new("WT_PreviewCamera", cam_data)
    collection.objects.link(cam_obj)
    scene.camera = cam_obj

    lights_spec = [
        ("WT_KeyLight",  3.3, (1.00, 0.96, 0.88), (math.radians(48),  math.radians(12), math.radians(-36))),
        ("WT_FillLight", 1.6, (0.88, 0.95, 1.00), (math.radians(52),  math.radians(-18), math.radians(55))),
        ("WT_RimLight",  1.6, (0.98, 1.00, 0.92), (math.radians(-45), math.radians(10), math.radians(165))),
    ]
    for lname, energy, col, rot in lights_spec:
        ld = bpy.data.lights.new(lname, type='SUN')
        ld.energy = energy
        ld.color = col
        lo = bpy.data.objects.new(lname, ld)
        lo.rotation_euler = rot
        collection.objects.link(lo)

    return cam_obj


def render_all_views(cam_obj, out_dir):
    out_dir = Path(out_dir)
    out_dir.mkdir(parents=True, exist_ok=True)
    scene = bpy.context.scene

    target = Vector((0.0, 0.0, 4.55))
    dist = 15.6

    views = [
        ("WT_MatureTree_front.png",          0.0,   7.0, dist, target),
        ("WT_MatureTree_back.png",         180.0,   7.0, dist, target),
        ("WT_MatureTree_left.png",         -90.0,   7.0, dist, target),
        ("WT_MatureTree_right.png",         90.0,   7.0, dist, target),
        ("WT_MatureTree_three_quarter.png", 34.0,  13.0, dist * 1.02, target),
        ("WT_MatureTree_back_3_4.png",     214.0,  13.0, dist * 1.02, target),
        ("WT_MatureTree_top.png",            0.0,  85.0, dist * 1.05, Vector((0.0, 0.0, 4.5))),
        ("WT_MatureTree_roots_closeup.png", 26.0,   8.0, 7.2, Vector((0.0, 0.0, 1.15))),
    ]

    # 1. Render PBR Studio Material Preview (EEVEE)
    scene.render.engine = 'BLENDER_EEVEE'
    for fname, yaw_deg, pitch_deg, d, tgt in views:
        yaw = math.radians(yaw_deg)
        pitch = math.radians(pitch_deg)
        cx = tgt.x + d * math.sin(yaw) * math.cos(pitch)
        cy = tgt.y - d * math.cos(yaw) * math.cos(pitch)
        cz = tgt.z + d * math.sin(pitch)
        cam_obj.location = Vector((cx, cy, cz))
        cam_obj.rotation_euler = (tgt - cam_obj.location).to_track_quat('-Z', 'Y').to_euler()
        scene.render.filepath = str(out_dir / fname)
        bpy.ops.render.render(write_still=True)

    # 2. Render Solid Mode with Cavity (Workbench) to inspect pure 3D geometry & silhouette
    scene.render.engine = 'BLENDER_WORKBENCH'
    scene.display.shading.light = 'STUDIO'
    scene.display.shading.color_type = 'MATERIAL'
    scene.display.shading.show_cavity = True
    scene.display.shading.cavity_type = 'BOTH'
    for fname, yaw_deg, pitch_deg, d, tgt in views[:6]:
        yaw = math.radians(yaw_deg)
        pitch = math.radians(pitch_deg)
        cx = tgt.x + d * math.sin(yaw) * math.cos(pitch)
        cy = tgt.y - d * math.cos(yaw) * math.cos(pitch)
        cz = tgt.z + d * math.sin(pitch)
        cam_obj.location = Vector((cx, cy, cz))
        cam_obj.rotation_euler = (tgt - cam_obj.location).to_track_quat('-Z', 'Y').to_euler()
        solid_name = fname.replace("WT_MatureTree_", "WT_MatureTree_solid_")
        scene.render.filepath = str(out_dir / solid_name)
        bpy.ops.render.render(write_still=True)

    # Restore EEVEE and three-quarter camera pose before final save
    scene.render.engine = 'BLENDER_EEVEE'
    yaw, pitch = math.radians(34.0), math.radians(13.0)
    cam_obj.location = Vector((
        target.x + dist * 1.02 * math.sin(yaw) * math.cos(pitch),
        target.y - dist * 1.02 * math.cos(yaw) * math.cos(pitch),
        target.z + dist * 1.02 * math.sin(pitch),
    ))
    cam_obj.rotation_euler = (target - cam_obj.location).to_track_quat('-Z', 'Y').to_euler()


# ==============================================================================
# 9. MAIN BUILD PIPELINE
# ==============================================================================

def main():
    generate_wt_textures()
    clear_scene()
    materials = create_all_wt_materials()

    scene = bpy.context.scene
    root_coll = scene.collection

    coll_lod0 = bpy.data.collections.new("WT_MatureTree_LOD0")
    coll_lod1 = bpy.data.collections.new("WT_MatureTree_LOD1")
    coll_lod2 = bpy.data.collections.new("WT_MatureTree_LOD2")
    coll_studio = bpy.data.collections.new("Preview_Studio")
    for c in (coll_lod0, coll_lod1, coll_lod2, coll_studio):
        root_coll.children.link(c)

    # Build LOD0 (Primary Asset with exact required object names)
    lod0_objs = [
        build_trunk_and_roots_mesh("WT_MatureTree_TrunkAndRoots", materials, lod=0),
        build_branches_mesh("WT_MatureTree_Branches", materials, lod=0),
        build_foliage_mesh("WT_MatureTree_Foliage", materials, lod=0),
    ]
    for o in lod0_objs:
        coll_lod0.objects.link(o)

    # Build LOD1
    lod1_objs = [
        build_trunk_and_roots_mesh("WT_MatureTree_TrunkAndRoots_LOD1", materials, lod=1),
        build_branches_mesh("WT_MatureTree_Branches_LOD1", materials, lod=1),
        build_foliage_mesh("WT_MatureTree_Foliage_LOD1", materials, lod=1),
    ]
    for o in lod1_objs:
        coll_lod1.objects.link(o)

    # Build LOD2
    lod2_objs = [
        build_trunk_and_roots_mesh("WT_MatureTree_TrunkAndRoots_LOD2", materials, lod=2),
        build_branches_mesh("WT_MatureTree_Branches_LOD2", materials, lod=2),
        build_foliage_mesh("WT_MatureTree_Foliage_LOD2", materials, lod=2),
    ]
    for o in lod2_objs:
        coll_lod2.objects.link(o)

    # Apply transforms to all meshes
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    bpy.ops.object.select_all(action='DESELECT')

    # Make texture paths relative BEFORE exporting FBX & saving .blend
    MODELS_DIR.mkdir(parents=True, exist_ok=True)
    blend_path = MODELS_DIR / "WT_MatureTree.blend"
    bpy.context.preferences.filepaths.save_version = 0
    bpy.ops.wm.save_as_mainfile(filepath=str(blend_path))
    try:
        bpy.ops.file.make_paths_relative()
    except Exception as e:
        print("make_paths_relative note:", e)

    # Audit all 3 LODs
    audit_report = {
        "LOD0": audit_mesh_objects(lod0_objs),
        "LOD1": audit_mesh_objects(lod1_objs),
        "LOD2": audit_mesh_objects(lod2_objs),
    }
    (AUDIT_DIR / "wt_mature_tree_audit.json").write_text(json.dumps(audit_report, indent=2), encoding="utf-8")
    print("Audit Report:", json.dumps(audit_report, indent=2))

    # Export FBX files for LOD0, LOD1, LOD2
    export_lod_fbx(lod0_objs, EXPORTS_DIR / "WT_MatureTree_LOD0.fbx")
    export_lod_fbx(lod1_objs, EXPORTS_DIR / "WT_MatureTree_LOD1.fbx")
    export_lod_fbx(lod2_objs, EXPORTS_DIR / "WT_MatureTree_LOD2.fbx")

    # Hide LOD1 and LOD2 collections in viewport & render so LOD0 is the active visible tree
    layer_colls = bpy.context.view_layer.layer_collection.children
    layer_colls["WT_MatureTree_LOD1"].exclude = True
    layer_colls["WT_MatureTree_LOD2"].exclude = True

    # Setup Studio Preview Lights & Camera and Render Multi-Angle Previews
    cam_obj = setup_studio_preview(coll_studio)
    render_all_views(cam_obj, AUDIT_DIR)

    # Save final WT_MatureTree.blend
    bpy.ops.wm.save_as_mainfile(filepath=str(blend_path))
    print("Successfully saved:", blend_path)


if __name__ == "__main__":
    main()
