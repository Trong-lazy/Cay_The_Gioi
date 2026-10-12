"""Generate seamlessly tileable bark textures for Tree_Large.

Outputs:
- ArtSource/Textures/Tree_Common/Large_Bark_BaseColor.png (1024x1024)
- ArtSource/Textures/Tree_Common/Large_Bark_Normal.png (1024x1024)
"""

import os
import bpy
import numpy as np

def periodic_box_blur(arr, rx, ry, passes=2):
    """Separable periodic box blur using cumsum."""
    h, w = arr.shape
    res = arr.copy()
    for _ in range(passes):
        # Horizontal pass
        if rx > 0:
            padded_x = np.pad(res, ((0, 0), (rx, rx)), mode='wrap')
            c_x = np.cumsum(padded_x, axis=1)
            res = (c_x[:, 2 * rx:] - c_x[:, :-2 * rx]) / (2 * rx + 1)
        # Vertical pass
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

def generate_large_bark_textures(output_dir):
    os.makedirs(output_dir, exist_ok=True)
    w, h = 1024, 1024
    
    # In NumPy coordinate layout for Blender images:
    # row 0 is V=0 (bottom of image), row H-1 is V=1 (top of image).
    u = np.linspace(0, 1, w, endpoint=False, dtype=np.float32)
    v = np.linspace(0, 1, h, endpoint=False, dtype=np.float32)
    U, V = np.meshgrid(u, v)
    
    # 1. Domain warping for organic meandering growth lines
    u_warp = (U + 0.030 * np.sin(2 * np.pi * V) + 
              0.015 * np.cos(4 * np.pi * V + 0.7) + 
              0.008 * np.sin(6 * np.pi * V + 1.3)) % 1.0
    v_warp = (V + 0.008 * np.sin(2 * np.pi * U) + 
              0.004 * np.cos(4 * np.pi * U + 0.5)) % 1.0
              
    # 2. Anisotropic periodic Voronoi for bark plates and furrows
    # 8 columns around circumference, 4 rows along trunk segment
    nx, ny = 8, 4
    np.random.seed(108)
    seeds = []
    for j in range(ny):
        for i in range(nx):
            jx = (np.random.rand() - 0.5) * 0.45
            jy = (np.random.rand() - 0.5) * 0.45
            sx = (i + 0.5 + jx) / nx
            sy = (j + 0.5 + jy) / ny
            seeds.append((sx, sy))
            
    d1 = np.full((h, w), np.inf, dtype=np.float32)
    d2 = np.full((h, w), np.inf, dtype=np.float32)
    
    for sx, sy in seeds:
        dx = np.abs(u_warp - sx)
        dx = np.minimum(dx, 1.0 - dx) * 1.0
        dy = np.abs(v_warp - sy)
        dy = np.minimum(dy, 1.0 - dy) * 0.22  # anisotropic stretch along height
        d_sq = dx * dx + dy * dy
        
        mask = d_sq < d1
        d2 = np.where(mask, d1, np.minimum(d2, d_sq))
        d1 = np.where(mask, d_sq, d1)
        
    voronoi_border = np.sqrt(d2) - np.sqrt(d1)
    
    # Normalize plate height: 0 in furrows, 1 on ridge plateaus
    plate_norm = np.clip(voronoi_border / 0.040, 0.0, 1.0)
    # Smoothstep curve for soft-shouldered ridges
    plate_height = plate_norm * plate_norm * (3.0 - 2.0 * plate_norm)
    
    # 3. Longitudinal wood grain fibers (fine ridges running parallel to furrows)
    fiber_height = np.zeros((h, w), dtype=np.float32)
    np.random.seed(2026)
    for k in range(1, 16):
        ku = int(k * 4 + np.random.randint(-1, 2))
        kv = int(np.random.randint(1, 4))
        phase = np.random.rand() * 2 * np.pi
        amp = 1.0 / (k ** 0.7)
        fiber_height += amp * np.cos(2 * np.pi * (ku * u_warp + kv * v_warp) + phase)
        
    fiber_height = (fiber_height - fiber_height.mean()) / (fiber_height.std() + 1e-6)
    fiber_height = np.clip(fiber_height * 0.25, -0.5, 0.5)
    
    # 4. Subtle lenticels / notches (scattered horizontal small dashes)
    lenticel_map = np.zeros((h, w), dtype=np.float32)
    num_lenticels = 48
    for _ in range(num_lenticels):
        lx = np.random.rand()
        ly = np.random.rand()
        ldx = np.abs(U - lx)
        ldx = np.minimum(ldx, 1.0 - ldx) * 40.0   # elongated horizontally
        ldy = np.abs(V - ly)
        ldy = np.minimum(ldy, 1.0 - ldy) * 160.0  # thin vertically
        l_dist = ldx * ldx + ldy * ldy
        lenticel_map += np.exp(-l_dist * 2.0)
    lenticel_map = np.clip(lenticel_map, 0.0, 1.0) * 0.25
    
    # 5. Combined height field
    # 0 is base plateau level, > 0 are ridge highlights, < 0 are furrows
    height = (plate_height - 0.6) * 1.2 + fiber_height * 0.35 - lenticel_map
    
    # Apply gentle periodic smoothing to eliminate any aliasing
    height = periodic_box_blur(height, rx=2, ry=2, passes=2)
    
    # 6. Generate BaseColor
    # Palette colors (sRGB):
    # - Base: #8A6848 -> (138, 104, 72)
    # - Highlight: #AD8860 -> (173, 136, 96)
    # - Furrow: #594A39 -> (89, 74, 57)
    c_base = np.array([138.0, 104.0, 72.0], dtype=np.float32)
    c_high = np.array([173.0, 136.0, 96.0], dtype=np.float32)
    c_furrow = np.array([89.0, 74.0, 57.0], dtype=np.float32)
    
    base_color = np.zeros((h, w, 3), dtype=np.float32)
    
    pos_mask = height >= 0.0
    t_high = np.clip(height / 0.5, 0.0, 1.0)[:, :, np.newaxis]
    color_high = (1.0 - t_high) * c_base + t_high * c_high
    
    t_furrow = np.clip(-height / 0.6, 0.0, 1.0)[:, :, np.newaxis]
    color_furrow = (1.0 - t_furrow) * c_base + t_furrow * c_furrow
    
    base_color = np.where(pos_mask[:, :, np.newaxis], color_high, color_furrow)
    
    # Add subtle micro-variation (+-2%) along fiber lines
    fiber_tint = (fiber_height[:, :, np.newaxis] * 8.0)
    base_color = np.clip(base_color + fiber_tint, 0.0, 255.0)
    
    # Add alpha channel
    alpha_chan = np.full((h, w, 1), 255.0, dtype=np.float32)
    base_color_rgba = np.concatenate([base_color, alpha_chan], axis=2).astype(np.uint8)
    
    base_color_path = os.path.join(output_dir, 'Large_Bark_BaseColor.png')
    save_rgba_image('Large_Bark_BaseColor', base_color_rgba, base_color_path, w, h)
    
    # 7. Generate Tangent Normal Map (OpenGL +Y up)
    # Periodic central differences:
    # dh/dx = (H[y, (x+1)%w] - H[y, (x-1)%w]) / 2
    # dh/dy = (H[(y+1)%h, x] - H[(y-1)%h, x]) / 2
    h_x_plus = np.roll(height, -1, axis=1)
    h_x_minus = np.roll(height, 1, axis=1)
    dh_dx = (h_x_plus - h_x_minus) * 0.5
    
    h_y_plus = np.roll(height, -1, axis=0)
    h_y_minus = np.roll(height, 1, axis=0)
    dh_dy = (h_y_plus - h_y_minus) * 0.5
    
    # Normal strength factor (calibrated for mature, readable bark without harsh distortion)
    strength = 14.0
    nx_map = -dh_dx * strength
    ny_map = -dh_dy * strength  # OpenGL convention +Y up
    nz_map = np.ones((h, w), dtype=np.float32)
    
    inv_len = 1.0 / np.sqrt(nx_map * nx_map + ny_map * ny_map + nz_map * nz_map)
    nx_norm = nx_map * inv_len
    ny_norm = ny_map * inv_len
    nz_norm = nz_map * inv_len
    
    # Map [-1, 1] to [0, 255]
    r_chan = np.clip((nx_norm * 0.5 + 0.5) * 255.0, 0.0, 255.0)
    g_chan = np.clip((ny_norm * 0.5 + 0.5) * 255.0, 0.0, 255.0)
    b_chan = np.clip((nz_norm * 0.5 + 0.5) * 255.0, 0.0, 255.0)
    
    normal_rgba = np.stack([r_chan, g_chan, b_chan, alpha_chan[:, :, 0]], axis=2).astype(np.uint8)
    normal_path = os.path.join(output_dir, 'Large_Bark_Normal.png')
    save_rgba_image('Large_Bark_Normal', normal_rgba, normal_path, w, h)
    
    return base_color_path, normal_path

if __name__ == '__main__':
    out_dir = r'c:\Users\Asus\foder game 3D\Cay_The_Gioi\ArtSource\Textures\Tree_Common'
    generate_large_bark_textures(out_dir)
