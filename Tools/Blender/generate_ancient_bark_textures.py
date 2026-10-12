"""Generate seamlessly tileable bark textures for Ancient Tree (Bonsai_Trunk_Candidate_v01).

Outputs:
- ArtSource/Textures/Tree_Common/Ancient_Bark_BaseColor.png (1024x1024)
- ArtSource/Textures/Tree_Common/Ancient_Bark_Normal.png (1024x1024)

Botanical characteristics:
- Same botanical species as Tree_Large, but older and more weathered.
- Palette:
  - Base background: #70634F (112, 99, 79)
  - Pale ridge: #9B896C (155, 137, 108)
  - Crevice / furrow: #463F34 (70, 63, 52)
- Wider, more broken bark plates, branching furrows, rugged ridges with micro-fissures.
- Seamlessly tileable in U and V.
"""

import os
import bpy
import numpy as np

def periodic_box_blur(arr, rx, ry, passes=2):
    """Separable periodic box blur using cumsum."""
    h, w = arr.shape
    res = arr.copy()
    for _ in range(passes):
        if rx > 0:
            padded_x = np.pad(res, ((0, 0), (rx, rx)), mode='wrap')
            c_x = np.cumsum(padded_x, axis=1)
            res = (c_x[:, 2 * rx:] - c_x[:, :-2 * rx]) / (2 * rx + 1)
        if ry > 0:
            padded_y = np.pad(res, ((ry, ry), (0, 0)), mode='wrap')
            c_y = np.cumsum(padded_y, axis=0)
            res = (c_y[2 * ry:, :] - c_y[:-2 * ry, :]) / (2 * ry + 1)
    return res

def save_rgba_image(img_name, rgba_array, file_path, w, h):
    img = bpy.data.images.get(img_name)
    if img:
        bpy.data.images.remove(img)
    img = bpy.data.images.new(img_name, width=w, height=h, alpha=True)
    float_pixels = (rgba_array.astype(np.float32) / 255.0).flatten()
    img.pixels.foreach_set(float_pixels)
    img.filepath_raw = str(file_path)
    img.file_format = 'PNG'
    img.save()
    print(f"Saved {file_path} ({os.path.getsize(file_path)} bytes)")

def generate_ancient_bark_textures(output_dir):
    os.makedirs(output_dir, exist_ok=True)
    w, h = 1024, 1024
    
    u = np.linspace(0, 1, w, endpoint=False, dtype=np.float32)
    v = np.linspace(0, 1, h, endpoint=False, dtype=np.float32)
    U, V = np.meshgrid(u, v)
    
    # 1. Enhanced domain warping for older, undulating, winding grain
    u_warp = (U + 0.040 * np.sin(2 * np.pi * V) + 
              0.022 * np.cos(4 * np.pi * V + 0.8) + 
              0.012 * np.sin(6 * np.pi * V + 1.5) +
              0.006 * np.cos(8 * np.pi * V + 2.1)) % 1.0
    v_warp = (V + 0.014 * np.sin(2 * np.pi * U) + 
              0.007 * np.cos(4 * np.pi * U + 0.6)) % 1.0
              
    # 2. Anisotropic periodic Voronoi with wider, weathered plates and branching furrows
    # 6 columns around circumference (wider plates than Large's 8), 5 rows along height
    nx, ny = 6, 5
    np.random.seed(314)
    seeds = []
    for j in range(ny):
        for i in range(nx):
            jx = (np.random.rand() - 0.5) * 0.50
            jy = (np.random.rand() - 0.5) * 0.50
            sx = (i + 0.5 + jx) / nx
            sy = (j + 0.5 + jy) / ny
            seeds.append((sx, sy))
            
    d1 = np.full((h, w), np.inf, dtype=np.float32)
    d2 = np.full((h, w), np.inf, dtype=np.float32)
    
    for sx, sy in seeds:
        dx = np.abs(u_warp - sx)
        dx = np.minimum(dx, 1.0 - dx) * 1.0
        dy = np.abs(v_warp - sy)
        dy = np.minimum(dy, 1.0 - dy) * 0.28  # anisotropic vertical elongation
        d_sq = dx * dx + dy * dy
        
        mask = d_sq < d1
        d2 = np.where(mask, d1, np.minimum(d2, d_sq))
        d1 = np.where(mask, d_sq, d1)
        
    voronoi_border = np.sqrt(d2) - np.sqrt(d1)
    
    # Plateau with rugged, slightly raised borders (characteristic of mature bark plates)
    plate_norm = np.clip(voronoi_border / 0.052, 0.0, 1.0)
    # Rugged plate profile: steeper edges, slightly convex plate
    plate_height = np.power(plate_norm, 0.75) * (1.0 - 0.15 * np.power(plate_norm, 4.0))
    
    # 3. Secondary micro-furrows (fine longitudinal fissures dividing plates)
    fissure_height = np.zeros((h, w), dtype=np.float32)
    np.random.seed(777)
    for k in range(1, 14):
        ku = int(k * 5 + np.random.randint(-1, 2))
        kv = int(np.random.randint(1, 5))
        phase = np.random.rand() * 2 * np.pi
        amp = 1.0 / (k ** 0.65)
        fissure_height += amp * np.cos(2 * np.pi * (ku * u_warp + kv * v_warp) + phase)
        
    fissure_height = (fissure_height - fissure_height.mean()) / (fissure_height.std() + 1e-6)
    fissure_height = np.clip(fissure_height * 0.30, -0.6, 0.6)
    
    # 4. Weathered callouses / lenticels (rough patches around branch growth nodes)
    lenticel_map = np.zeros((h, w), dtype=np.float32)
    num_lenticels = 64
    np.random.seed(999)
    for _ in range(num_lenticels):
        lx = np.random.rand()
        ly = np.random.rand()
        ldx = np.abs(U - lx)
        ldx = np.minimum(ldx, 1.0 - ldx) * 35.0
        ldy = np.abs(V - ly)
        ldy = np.minimum(ldy, 1.0 - ldy) * 120.0
        l_dist = ldx * ldx + ldy * ldy
        lenticel_map += np.exp(-l_dist * 2.0)
    lenticel_map = np.clip(lenticel_map, 0.0, 1.0) * 0.35
    
    # 5. Combined height field
    # 0 is base plateau level, > 0 are ridge highlights, < 0 are deep furrows
    height = (plate_height - 0.55) * 1.5 + fissure_height * 0.40 - lenticel_map
    height = periodic_box_blur(height, rx=2, ry=2, passes=2)
    
    # 6. Generate BaseColor
    # Palette colors (sRGB):
    # - Base: #70634F -> (112, 99, 79)
    # - Highlight: #9B896C -> (155, 137, 108)
    # - Furrow: #463F34 -> (70, 63, 52)
    c_base = np.array([112.0, 99.0, 79.0], dtype=np.float32)
    c_high = np.array([155.0, 137.0, 108.0], dtype=np.float32)
    c_furrow = np.array([70.0, 63.0, 52.0], dtype=np.float32)
    
    base_color = np.zeros((h, w, 3), dtype=np.float32)
    
    pos_mask = height >= 0.0
    t_high = np.clip(height / 0.6, 0.0, 1.0)[:, :, np.newaxis]
    color_high = (1.0 - t_high) * c_base + t_high * c_high
    
    t_furrow = np.clip(-height / 0.7, 0.0, 1.0)[:, :, np.newaxis]
    color_furrow = (1.0 - t_furrow) * c_base + t_furrow * c_furrow
    
    base_color = np.where(pos_mask[:, :, np.newaxis], color_high, color_furrow)
    
    # Add subtle organic fiber tint (+-3%)
    fiber_tint = (fissure_height[:, :, np.newaxis] * 7.0)
    base_color = np.clip(base_color + fiber_tint, 0.0, 255.0)
    
    alpha_chan = np.full((h, w, 1), 255.0, dtype=np.float32)
    base_color_rgba = np.concatenate([base_color, alpha_chan], axis=2).astype(np.uint8)
    
    base_color_path = os.path.join(output_dir, 'Ancient_Bark_BaseColor.png')
    save_rgba_image('Ancient_Bark_BaseColor', base_color_rgba, base_color_path, w, h)
    
    # 7. Generate Tangent Normal Map (OpenGL +Y up)
    h_x_plus = np.roll(height, -1, axis=1)
    h_x_minus = np.roll(height, 1, axis=1)
    dh_dx = (h_x_plus - h_x_minus) * 0.5
    
    h_y_plus = np.roll(height, -1, axis=0)
    h_y_minus = np.roll(height, 1, axis=0)
    dh_dy = (h_y_plus - h_y_minus) * 0.5
    
    # Strength ~ 1.6x of Large tree (22.0 vs 14.0) to reflect older, deeper furrows
    strength = 22.0
    nx_map = -dh_dx * strength
    ny_map = -dh_dy * strength  # OpenGL convention +Y up
    nz_map = np.ones((h, w), dtype=np.float32)
    
    inv_len = 1.0 / np.sqrt(nx_map * nx_map + ny_map * ny_map + nz_map * nz_map)
    nx_norm = nx_map * inv_len
    ny_norm = ny_map * inv_len
    nz_norm = nz_map * inv_len
    
    r_chan = np.clip((nx_norm * 0.5 + 0.5) * 255.0, 0.0, 255.0)
    g_chan = np.clip((ny_norm * 0.5 + 0.5) * 255.0, 0.0, 255.0)
    b_chan = np.clip((nz_norm * 0.5 + 0.5) * 255.0, 0.0, 255.0)
    
    normal_rgba = np.stack([r_chan, g_chan, b_chan, alpha_chan[:, :, 0]], axis=2).astype(np.uint8)
    normal_path = os.path.join(output_dir, 'Ancient_Bark_Normal.png')
    save_rgba_image('Ancient_Bark_Normal', normal_rgba, normal_path, w, h)
    
    return base_color_path, normal_path

if __name__ == '__main__':
    out_dir = r'c:\Users\Asus\foder game 3D\Cay_The_Gioi\ArtSource\Textures\Tree_Common'
    generate_ancient_bark_textures(out_dir)
