"""Generate Leaf_Common_BaseColor.png and Leaf_Common_Normal.png for P02.
Size: 512x512
Coordinates:
- Cuong (stem base): (0.5, 0.0)
- Chop (tip): (0.5, 1.0)
- Mep trai (left margin): (0.0, 0.5)
- Mep phai (right margin): (1.0, 0.5)
- Midrib runs along U=0.5
- 4 pairs of subtle, organic lateral secondary veins
- Tangent-space Normal map (OpenGL +Y, flat = (128, 128, 255))
- Tintable BaseColor map (neutral off-white with luminance modulation for veins)
"""
import math
import numpy as np
from pathlib import Path
import bpy

ROOT = Path("c:/Users/Asus/foder game 3D/Cay_The_Gioi")
OUT_DIR = ROOT / "ArtSource/Textures/Tree_Common"
OUT_DIR.mkdir(parents=True, exist_ok=True)

W, H = 512, 512
X, Y = np.meshgrid(np.arange(W), np.arange(H))
U = (X + 0.5) / float(W)
V = (Y + 0.5) / float(H)

# 1. Height map initialization
H_map = np.zeros((H, W), dtype=np.float32)

# Midrib along U=0.5
w_mid = 0.016 * (1.0 - 0.65 * V) + 0.003
d_mid = np.abs(U - 0.5)
h_mid = np.exp(-((d_mid / w_mid) ** 2)) * (1.0 - 0.20 * V)
H_map += h_mid * 0.85

# 2. Secondary veins (Bézier curves)
def bezier_curve(p0, p1, p2, num_pts=200):
    t = np.linspace(0, 1, num_pts)[:, None]
    return (1 - t)**2 * p0 + 2 * (1 - t) * t * p1 + t**2 * p2

vein_curves = [
    # Left side (branching from 0.5 to <0.5)
    (np.array([0.50, 0.16]), np.array([0.28, 0.23]), np.array([0.10, 0.33])),
    (np.array([0.50, 0.36]), np.array([0.25, 0.43]), np.array([0.06, 0.53])),
    (np.array([0.50, 0.56]), np.array([0.28, 0.63]), np.array([0.12, 0.73])),
    (np.array([0.50, 0.75]), np.array([0.35, 0.81]), np.array([0.22, 0.89])),
    # Right side (branching from 0.5 to >0.5, slight asymmetry)
    (np.array([0.50, 0.18]), np.array([0.72, 0.25]), np.array([0.90, 0.35])),
    (np.array([0.50, 0.39]), np.array([0.75, 0.47]), np.array([0.94, 0.57])),
    (np.array([0.50, 0.58]), np.array([0.72, 0.65]), np.array([0.88, 0.75])),
    (np.array([0.50, 0.77]), np.array([0.65, 0.83]), np.array([0.78, 0.90])),
]

grid_pts = np.stack([U, V], axis=-1) # (H, W, 2)

for p0, p1, p2 in vein_curves:
    pts = bezier_curve(p0, p1, p2, num_pts=150) # (150, 2)
    # Compute bounding box to optimize distance calculation
    min_u, max_u = np.min(pts[:, 0]) - 0.05, np.max(pts[:, 0]) + 0.05
    min_v, max_v = np.min(pts[:, 1]) - 0.05, np.max(pts[:, 1]) + 0.05
    
    mask = (U >= min_u) & (U <= max_u) & (V >= min_v) & (V <= max_v)
    sub_grid = grid_pts[mask]
    if len(sub_grid) == 0:
        continue
        
    diff = sub_grid[:, None, :] - pts[None, :, :] # (M, 150, 2)
    dist_sq = np.sum(diff**2, axis=-1) # (M, 150)
    closest_idx = np.argmin(dist_sq, axis=-1) # (M,)
    min_dist = np.sqrt(np.min(dist_sq, axis=-1)) # (M,)
    
    t_param = closest_idx / 150.0
    w_vein = 0.007 * (1.0 - 0.55 * t_param) + 0.002
    h_vein = np.exp(-((min_dist / w_vein) ** 2)) * (1.0 - 0.45 * t_param) * 0.45
    
    H_map[mask] += h_vein

# 3. Soft organic micro-surface (isotropic fine grain, no directional stripes)
np.random.seed(42)
white_noise = np.random.randn(H, W).astype(np.float32)
# Multi-pass separable box blur yields smooth isotropic Gaussian-like grain
fine_grain = white_noise.copy()
for _ in range(3):
    fine_grain = (np.roll(fine_grain, -1, 0) + fine_grain + np.roll(fine_grain, 1, 0)) / 3.0
    fine_grain = (np.roll(fine_grain, -1, 1) + fine_grain + np.roll(fine_grain, 1, 1)) / 3.0
fine_grain *= 0.003
H_map += fine_grain

# 4. Border fade to 0 so UV borders are completely flat
border_dist_u = np.minimum(U, 1.0 - U)
border_dist_v = np.minimum(V, 1.0 - V)
border_dist = np.minimum(border_dist_u, border_dist_v)
border_fade = np.clip(border_dist / 0.035, 0.0, 1.0)
border_fade = 3 * border_fade**2 - 2 * border_fade**3 # smoothstep
H_map *= border_fade

# 5. Compute Normal Map via central differences (OpenGL standard: +X right, +Y up, +Z out)
dH_dx = np.zeros_like(H_map)
dH_dy = np.zeros_like(H_map)

dH_dx[:, 1:-1] = (H_map[:, 2:] - H_map[:, :-2]) / 2.0
dH_dy[1:-1, :] = (H_map[2:, :] - H_map[:-2, :]) / 2.0

normal_scale = 16.0
Nx = -dH_dx * normal_scale
Ny = -dH_dy * normal_scale # +Y up
Nz = np.ones_like(H_map)

norm = np.sqrt(Nx**2 + Ny**2 + Nz**2)
Nx /= norm
Ny /= norm
Nz /= norm

norm_R = np.clip((Nx * 0.5 + 0.5) * 255.0, 0, 255).astype(np.uint8)
norm_G = np.clip((Ny * 0.5 + 0.5) * 255.0, 0, 255).astype(np.uint8)
norm_B = np.clip((Nz * 0.5 + 0.5) * 255.0, 0, 255).astype(np.uint8)
norm_A = np.full((H, W), 255, dtype=np.uint8)

normal_rgba = np.stack([norm_R, norm_G, norm_B, norm_A], axis=-1)

# 6. Compute BaseColor Map (tintable neutral off-white with luminance modulation)
# Base neutral luminance: ~0.86 (220/255)
# Veins get +12% luminance (~250/255)
# Intervein valleys get -6% luminance (~205/255)
vein_luminance = H_map * 0.16 - 0.04 * (1.0 - np.clip(H_map * 2.0, 0.0, 1.0))
base_lum = np.clip(0.86 + vein_luminance, 0.70, 0.98)

# Subtle warm/natural organic balance (R=1.0, G=1.0, B=0.96)
bc_R = np.clip(base_lum * 255.0, 0, 255).astype(np.uint8)
bc_G = np.clip(base_lum * 255.0, 0, 255).astype(np.uint8)
bc_B = np.clip(base_lum * 0.96 * 255.0, 0, 255).astype(np.uint8)
bc_A = np.full((H, W), 255, dtype=np.uint8)

basecolor_rgba = np.stack([bc_R, bc_G, bc_B, bc_A], axis=-1)

# 7. Save PNGs using Blender bpy.data.images
def save_rgba_image(img_name, rgba_array, file_path):
    img = bpy.data.images.get(img_name)
    if img:
        bpy.data.images.remove(img)
    img = bpy.data.images.new(img_name, width=W, height=H, alpha=True)
    # Blender expects float pixels [0, 1] ordered from bottom-to-top
    # Note: NumPy row 0 is top or bottom depending on convention.
    # In Y array, Y=0 is V=0 (cuong / bottom), Y=511 is V=1 (chop / top).
    # Blender image pixel layout matches Y=0 at bottom!
    float_pixels = (rgba_array.astype(np.float32) / 255.0).flatten()
    img.pixels.foreach_set(float_pixels)
    img.filepath_raw = str(file_path)
    img.file_format = 'PNG'
    img.save()
    print(f"Saved {file_path}")

normal_path = OUT_DIR / "Leaf_Common_Normal.png"
basecolor_path = OUT_DIR / "Leaf_Common_BaseColor.png"

save_rgba_image("Leaf_Common_Normal", normal_rgba, normal_path)
save_rgba_image("Leaf_Common_BaseColor", basecolor_rgba, basecolor_path)

print("TEXTURE GENERATION COMPLETE!")
