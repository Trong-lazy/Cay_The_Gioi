import bpy
import bmesh
from mathutils import Vector, Matrix
import math

def add_bonsai_style_leaf(bm, transform_mat, scale=1.0, droop=1.0, thickness=0.003, mat_idx=0, uv_lay=None):
    """
    Creates a stylized leaf exactly matching the geometry, proportions (1:2),
    V-fold (18.7 deg), arch, and UV mapping of Bonsai_Trunk_Candidate_v01,
    with physical two-sided thickness so it never culls or loses faces from behind.
    """
    # Proportions from Bonsai candidate:
    # Length = 0.30 * scale, Width = 0.15 * scale (ratio 1:2)
    L = 0.30 * scale
    W = 0.15 * scale
    H_fold = W * 0.5 * math.tan(math.radians(18.7))  # height of V-fold ridge
    H_arch = 0.025 * scale                           # longitudinal arch
    H_droop = -0.035 * scale * droop                 # tip droop
    th = thickness * scale
    
    # 5 key feature points in local coordinates:
    # (x, y, z): base is at y=0, tip is at y=L
    p_base = Vector((0.0,     0.0,     0.0))
    p_left = Vector((-W*0.5,  L*0.48,  H_arch * 0.5))
    p_mid  = Vector((0.0,     L*0.50,  H_arch + H_fold))
    p_right= Vector((W*0.5,   L*0.48,  H_arch * 0.5))
    p_tip  = Vector((0.0,     L,       H_droop))
    
    # Top vertices (offset by +th/2 along Z)
    v_base_t  = bm.verts.new(transform_mat @ (p_base  + Vector((0, 0,  th*0.5))))
    v_left_t  = bm.verts.new(transform_mat @ (p_left  + Vector((0, 0,  th*0.5))))
    v_mid_t   = bm.verts.new(transform_mat @ (p_mid   + Vector((0, 0,  th*0.5))))
    v_right_t = bm.verts.new(transform_mat @ (p_right + Vector((0, 0,  th*0.5))))
    v_tip_t   = bm.verts.new(transform_mat @ (p_tip   + Vector((0, 0,  th*0.5))))
    
    # Bottom vertices (offset by -th/2 along Z)
    v_base_b  = bm.verts.new(transform_mat @ (p_base  + Vector((0, 0, -th*0.5))))
    v_left_b  = bm.verts.new(transform_mat @ (p_left  + Vector((0, 0, -th*0.5))))
    v_mid_b   = bm.verts.new(transform_mat @ (p_mid   + Vector((0, 0, -th*0.5))))
    v_right_b = bm.verts.new(transform_mat @ (p_right + Vector((0, 0, -th*0.5))))
    v_tip_b   = bm.verts.new(transform_mat @ (p_tip   + Vector((0, 0, -th*0.5))))
    
    # Top 4 faces (counter-clockwise when viewed from above)
    f_t0 = bm.faces.new([v_base_t, v_left_t, v_mid_t])
    f_t1 = bm.faces.new([v_base_t, v_mid_t, v_right_t])
    f_t2 = bm.faces.new([v_mid_t, v_tip_t, v_right_t])
    f_t3 = bm.faces.new([v_mid_t, v_left_t, v_tip_t])
    
    # Bottom 4 faces (clockwise when viewed from above, so outward normal points down)
    f_b0 = bm.faces.new([v_base_b, v_mid_b, v_left_b])
    f_b1 = bm.faces.new([v_base_b, v_right_b, v_mid_b])
    f_b2 = bm.faces.new([v_mid_b, v_right_b, v_tip_b])
    f_b3 = bm.faces.new([v_mid_b, v_tip_b, v_left_b])
    
    # Side rim quad faces
    f_s0 = bm.faces.new([v_base_t, v_base_b, v_left_b, v_left_t])
    f_s1 = bm.faces.new([v_left_t, v_left_b, v_tip_b, v_tip_t])
    f_s2 = bm.faces.new([v_tip_t, v_tip_b, v_right_b, v_right_t])
    f_s3 = bm.faces.new([v_right_t, v_right_b, v_base_b, v_base_t])
    
    all_faces = [f_t0, f_t1, f_t2, f_t3, f_b0, f_b1, f_b2, f_b3, f_s0, f_s1, f_s2, f_s3]
    for f in all_faces:
        f.material_index = mat_idx
        f.smooth = True
        
    # Assign UVs matching Bonsai candidate
    if uv_lay:
        # Top faces UVs
        # f_t0: base=(0.5, 0.0), left=(0.0, 0.5), mid=(0.5, 0.5)
        f_t0.loops[0][uv_lay].uv = Vector((0.5, 0.0))
        f_t0.loops[1][uv_lay].uv = Vector((0.0, 0.5))
        f_t0.loops[2][uv_lay].uv = Vector((0.5, 0.5))
        
        # f_t1: base=(0.5, 0.0), mid=(0.5, 0.5), right=(1.0, 0.5)
        f_t1.loops[0][uv_lay].uv = Vector((0.5, 0.0))
        f_t1.loops[1][uv_lay].uv = Vector((0.5, 0.5))
        f_t1.loops[2][uv_lay].uv = Vector((1.0, 0.5))
        
        # f_t2: mid=(0.5, 0.5), tip=(0.5, 1.0), right=(1.0, 0.5)
        f_t2.loops[0][uv_lay].uv = Vector((0.5, 0.5))
        f_t2.loops[1][uv_lay].uv = Vector((0.5, 1.0))
        f_t2.loops[2][uv_lay].uv = Vector((1.0, 0.5))
        
        # f_t3: mid=(0.5, 0.5), left=(0.0, 0.5), tip=(0.5, 1.0)
        f_t3.loops[0][uv_lay].uv = Vector((0.5, 0.5))
        f_t3.loops[1][uv_lay].uv = Vector((0.0, 0.5))
        f_t3.loops[2][uv_lay].uv = Vector((0.5, 1.0))
        
        # Bottom faces share same UV coordinates
        f_b0.loops[0][uv_lay].uv = Vector((0.5, 0.0))
        f_b0.loops[1][uv_lay].uv = Vector((0.5, 0.5))
        f_b0.loops[2][uv_lay].uv = Vector((0.0, 0.5))
        
        f_b1.loops[0][uv_lay].uv = Vector((0.5, 0.0))
        f_b1.loops[1][uv_lay].uv = Vector((1.0, 0.5))
        f_b1.loops[2][uv_lay].uv = Vector((0.5, 0.5))
        
        f_b2.loops[0][uv_lay].uv = Vector((0.5, 0.5))
        f_b2.loops[1][uv_lay].uv = Vector((1.0, 0.5))
        f_b2.loops[2][uv_lay].uv = Vector((0.5, 1.0))
        
        f_b3.loops[0][uv_lay].uv = Vector((0.5, 0.5))
        f_b3.loops[1][uv_lay].uv = Vector((0.5, 1.0))
        f_b3.loops[2][uv_lay].uv = Vector((0.0, 0.5))
        
        # Rim faces UVs along border
        for f in [f_s0, f_s1, f_s2, f_s3]:
            for loop in f.loops:
                loop[uv_lay].uv = Vector((0.5, 0.5))

print("add_bonsai_style_leaf ready")
