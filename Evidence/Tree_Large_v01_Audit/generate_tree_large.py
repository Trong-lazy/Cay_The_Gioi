import bpy
import bmesh
import mathutils
from mathutils import Vector, Matrix
import math
import os

def clear_scene():
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    for block in list(bpy.data.meshes):
        bpy.data.meshes.remove(block)
    for block in list(bpy.data.materials):
        bpy.data.materials.remove(block)

def create_material(name, color, roughness=0.45):
    mat = bpy.data.materials.new(name=name)
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    bsdf = nodes.get("Principled BSDF")
    if bsdf:
        bsdf.inputs['Base Color'].default_value = color
        bsdf.inputs['Roughness'].default_value = roughness
    return mat

def create_tube_bmesh(bm, path_points_with_radius, num_radial=10, cap_start=False, cap_end=True, mat_idx=0):
    n_pts = len(path_points_with_radius)
    if n_pts < 2:
        return []
    
    frames = []
    prev_n = None
    
    for i in range(n_pts):
        pt, r = path_points_with_radius[i]
        if i == 0:
            tangent = (path_points_with_radius[1][0] - pt).normalized()
        elif i == n_pts - 1:
            tangent = (pt - path_points_with_radius[i-1][0]).normalized()
        else:
            t1 = (pt - path_points_with_radius[i-1][0]).normalized()
            t2 = (path_points_with_radius[i+1][0] - pt).normalized()
            tangent = (t1 + t2).normalized()
            
        if prev_n is None:
            up = Vector((0, 0, 1))
            if abs(tangent.dot(up)) > 0.85:
                up = Vector((0, 1, 0))
            normal = tangent.cross(up).normalized()
            binormal = tangent.cross(normal).normalized()
        else:
            normal = (prev_n - tangent * tangent.dot(prev_n)).normalized()
            binormal = tangent.cross(normal).normalized()
            
        prev_n = normal
        frames.append((pt, r, normal, binormal))
        
    rings = []
    for pt, r, norm, binorm in frames:
        ring_verts = []
        for j in range(num_radial):
            angle = 2.0 * math.pi * j / num_radial
            offset = (norm * math.cos(angle) + binorm * math.sin(angle)) * r
            v = bm.verts.new(pt + offset)
            ring_verts.append(v)
        rings.append(ring_verts)
        
    for i in range(n_pts - 1):
        r1 = rings[i]
        r2 = rings[i+1]
        for j in range(num_radial):
            jn = (j + 1) % num_radial
            f = bm.faces.new([r1[j], r1[jn], r2[jn], r2[j]])
            f.material_index = mat_idx
            
    if cap_start:
        f = bm.faces.new(list(reversed(rings[0])))
        f.material_index = mat_idx
    if cap_end:
        f = bm.faces.new(rings[-1])
        f.material_index = mat_idx
        
    return rings

def create_puffy_cloud_cluster(cluster_name, center_pos, lobe_defs, voxel_size=0.026, decimate_ratio=0.18):
    """
    Creates stylized plump rounded cloud clusters with pronounced pillowy lobes.
    """
    temp_objs = []
    for offset, scale in lobe_defs:
        pos = center_pos + offset
        bpy.ops.mesh.primitive_uv_sphere_add(segments=20, ring_count=14, radius=1.0, location=pos)
        sphere = bpy.context.active_object
        sphere.scale = scale
        bpy.ops.object.transform_apply(scale=True)
        temp_objs.append(sphere)
        
    bpy.ops.object.select_all(action='DESELECT')
    for o in temp_objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = temp_objs[0]
    bpy.ops.object.join()
    
    cluster_obj = bpy.context.active_object
    cluster_obj.name = cluster_name
    
    # Voxel remesh at fine resolution to preserve deep valleys between lobes
    mod_remesh = cluster_obj.modifiers.new("Remesh", 'REMESH')
    mod_remesh.mode = 'VOXEL'
    mod_remesh.voxel_size = voxel_size
    mod_remesh.adaptivity = 0.0
    bpy.ops.object.modifier_apply(modifier=mod_remesh.name)
    
    # Gentle smoothing to keep the bulbous lobe volumes
    mod_smooth = cluster_obj.modifiers.new("Smooth", 'SMOOTH')
    mod_smooth.iterations = 2
    mod_smooth.factor = 0.5
    bpy.ops.object.modifier_apply(modifier=mod_smooth.name)
    
    # Decimate
    mod_dec = cluster_obj.modifiers.new("Decimate", 'DECIMATE')
    mod_dec.ratio = decimate_ratio
    bpy.ops.object.modifier_apply(modifier=mod_dec.name)
    
    for poly in cluster_obj.data.polygons:
        poly.use_smooth = True
        
    return cluster_obj

def build_foliage():
    clusters = []
    
    # 1. TOP CROWN CLUSTER (Apex dome: multi-lobed prominent crown)
    c1_lobes = [
        (Vector((0.00, 0.00, 0.00)), Vector((0.34, 0.28, 0.22))),
        (Vector((-0.18, 0.00, 0.12)), Vector((0.26, 0.22, 0.18))),
        (Vector((0.18, 0.00, 0.12)), Vector((0.26, 0.22, 0.18))),
        (Vector((0.00, 0.02, 0.22)), Vector((0.24, 0.20, 0.18))),
        (Vector((-0.26, -0.02, -0.06)), Vector((0.24, 0.20, 0.16))),
        (Vector((0.26, 0.02, -0.06)), Vector((0.24, 0.20, 0.16))),
        (Vector((0.02, -0.16, -0.04)), Vector((0.22, 0.19, 0.15))),
        (Vector((-0.02, 0.16, -0.04)), Vector((0.22, 0.19, 0.15))),
    ]
    clusters.append(create_puffy_cloud_cluster("Cluster_Top_Crown", Vector((0.00, 0.02, 2.05)), c1_lobes))
    
    # 2. UPPER RIGHT CLUSTER (Tier 2 right, leaning slightly forward: Y ~ -0.12)
    c2_lobes = [
        (Vector((0.00, 0.00, 0.00)), Vector((0.30, 0.24, 0.20))),
        (Vector((-0.12, 0.00, 0.10)), Vector((0.22, 0.18, 0.15))),
        (Vector((0.14, -0.02, 0.08)), Vector((0.23, 0.19, 0.15))),
        (Vector((0.24, -0.02, -0.04)), Vector((0.22, 0.18, 0.15))),
        (Vector((-0.18, 0.02, -0.04)), Vector((0.20, 0.17, 0.14))),
        (Vector((0.04, -0.14, -0.03)), Vector((0.20, 0.17, 0.14))),
    ]
    clusters.append(create_puffy_cloud_cluster("Cluster_Upper_Right", Vector((0.58, -0.12, 1.62)), c2_lobes))
    
    # 3. UPPER LEFT CLUSTER (Tier 2 left, leaning slightly back: Y ~ +0.18)
    c3_lobes = [
        (Vector((0.00, 0.00, 0.00)), Vector((0.30, 0.24, 0.20))),
        (Vector((0.12, 0.00, 0.10)), Vector((0.22, 0.18, 0.15))),
        (Vector((-0.14, 0.02, 0.08)), Vector((0.23, 0.19, 0.15))),
        (Vector((-0.24, 0.00, -0.04)), Vector((0.22, 0.18, 0.15))),
        (Vector((0.18, -0.02, -0.04)), Vector((0.20, 0.17, 0.14))),
        (Vector((-0.04, 0.14, -0.03)), Vector((0.20, 0.17, 0.14))),
    ]
    clusters.append(create_puffy_cloud_cluster("Cluster_Upper_Left", Vector((-0.52, 0.18, 1.58)), c3_lobes))
    
    # 4. FAR LOWER LEFT CLUSTER (Wide, dramatic lower tier reaching far left: Y ~ -0.22)
    c4_lobes = [
        (Vector((0.00, 0.00, 0.00)), Vector((0.34, 0.26, 0.21))),
        (Vector((-0.16, 0.00, 0.10)), Vector((0.24, 0.20, 0.16))),
        (Vector((0.14, 0.02, 0.08)), Vector((0.23, 0.19, 0.15))),
        (Vector((-0.26, 0.00, -0.04)), Vector((0.24, 0.19, 0.15))),
        (Vector((0.22, 0.02, -0.04)), Vector((0.22, 0.18, 0.14))),
        (Vector((0.02, -0.15, -0.04)), Vector((0.22, 0.18, 0.14))),
    ]
    clusters.append(create_puffy_cloud_cluster("Cluster_Far_Lower_Left", Vector((-0.88, -0.22, 1.10)), c4_lobes))
    
    # 5. FAR LOWER RIGHT CLUSTER (Tier 1 outer right: Y ~ +0.22)
    c5_lobes = [
        (Vector((0.00, 0.00, 0.00)), Vector((0.33, 0.25, 0.20))),
        (Vector((0.16, 0.00, 0.09)), Vector((0.23, 0.19, 0.15))),
        (Vector((-0.14, -0.02, 0.08)), Vector((0.22, 0.18, 0.14))),
        (Vector((0.26, 0.00, -0.04)), Vector((0.23, 0.19, 0.15))),
        (Vector((-0.20, -0.02, -0.04)), Vector((0.21, 0.17, 0.14))),
        (Vector((0.02, 0.14, -0.04)), Vector((0.21, 0.17, 0.14))),
    ]
    clusters.append(create_puffy_cloud_cluster("Cluster_Far_Lower_Right", Vector((0.86, 0.22, 1.00)), c5_lobes))
    
    # 6. MID INNER RIGHT CLUSTER (Tucked below upper right, reaching forward: Y ~ -0.28)
    c6_lobes = [
        (Vector((0.00, 0.00, 0.00)), Vector((0.28, 0.22, 0.18))),
        (Vector((0.08, 0.00, 0.08)), Vector((0.20, 0.17, 0.14))),
        (Vector((-0.14, 0.00, -0.03)), Vector((0.19, 0.16, 0.13))),
        (Vector((0.16, 0.00, -0.03)), Vector((0.19, 0.16, 0.13))),
        (Vector((-0.02, -0.12, -0.03)), Vector((0.18, 0.15, 0.12))),
    ]
    clusters.append(create_puffy_cloud_cluster("Cluster_Mid_Inner_Right", Vector((0.38, -0.28, 1.20)), c6_lobes))
    
    # 7. MID BACK DEPTH CLUSTER (Provides full 3D canopy volume reaching back: Y ~ +0.52)
    c7_lobes = [
        (Vector((0.00, 0.00, 0.00)), Vector((0.30, 0.24, 0.19))),
        (Vector((0.00, 0.02, 0.09)), Vector((0.22, 0.18, 0.14))),
        (Vector((-0.18, 0.00, -0.03)), Vector((0.20, 0.16, 0.13))),
        (Vector((0.18, 0.00, -0.03)), Vector((0.20, 0.16, 0.13))),
        (Vector((0.00, 0.15, -0.03)), Vector((0.19, 0.16, 0.13))),
    ]
    clusters.append(create_puffy_cloud_cluster("Cluster_Mid_Back_Depth", Vector((-0.08, 0.52, 1.40)), c7_lobes))
    
    # Join into one Foliage object
    bpy.ops.object.select_all(action='DESELECT')
    for o in clusters:
        o.select_set(True)
    bpy.context.view_layer.objects.active = clusters[0]
    bpy.ops.object.join()
    
    foliage_obj = bpy.context.active_object
    foliage_obj.name = "Foliage"
    
    # Rich vibrant lime/spring green matching reference (0.35, 0.72, 0.10)
    mat_foliage = create_material("M_Foliage", (0.35, 0.72, 0.10, 1.0), roughness=0.45)
    foliage_obj.data.materials.append(mat_foliage)
    
    return foliage_obj

def build_trunk_and_branches():
    mesh = bpy.data.meshes.new("Trunk_Branches")
    obj = bpy.data.objects.new("Trunk_Branches", mesh)
    bpy.context.collection.objects.link(obj)
    
    bm = bmesh.new()
    
    # =========================================================================
    # MAIN TRUNK SPINE
    # Curves right (convex bow at z ~ 0.5), then leans back left into canopy
    # =========================================================================
    trunk_pts = [
        (Vector((0.000, 0.000, -0.030)), 0.180),   # deep buttress overlap with roots
        (Vector((0.000, 0.000, 0.000)), 0.165),    # ground level collar
        (Vector((0.040, -0.015, 0.180)), 0.150),   # flaring bow right
        (Vector((0.110, -0.020, 0.380)), 0.132),   # prominent rightward curve
        (Vector((0.160, 0.000, 0.580)), 0.115),    # apex of right curve / branch split
        (Vector((0.140, 0.020, 0.820)), 0.098),    # inward S-curve
        (Vector((0.080, 0.030, 1.080)), 0.084),    # middle trunk
        (Vector((0.025, 0.035, 1.350)), 0.070),    # upper trunk
        (Vector((0.005, 0.035, 1.620)), 0.056),    # crown junction
        (Vector((0.000, 0.030, 1.820)), 0.040),    # inside top crown cluster
    ]
    create_tube_bmesh(bm, trunk_pts, num_radial=12, cap_start=False, cap_end=True)
    
    # -------------------------------------------------------------------------
    # BRANCH LIMBS (Curved, organic limbs penetrating into foliage clusters)
    # -------------------------------------------------------------------------
    
    # 1. Lower Left Main Limb -> Far Lower Left Cluster (c4 at (-0.88, -0.22, 1.10))
    limb_lower_l = [
        (Vector((0.130, 0.005, 0.650)), 0.065),
        (Vector((-0.080, -0.060, 0.740)), 0.056),
        (Vector((-0.340, -0.120, 0.840)), 0.046),
        (Vector((-0.620, -0.180, 0.960)), 0.036),
        (Vector((-0.840, -0.220, 1.080)), 0.022),   # inside cluster 4
    ]
    create_tube_bmesh(bm, limb_lower_l, num_radial=10, cap_start=False, cap_end=True)
    
    # Sub-fork of Lower Left
    fork_lower_l = [
        (Vector((-0.340, -0.120, 0.840)), 0.038),
        (Vector((-0.520, -0.240, 0.920)), 0.028),
        (Vector((-0.720, -0.280, 1.040)), 0.018),   # inside front lobe of cluster 4
    ]
    create_tube_bmesh(bm, fork_lower_l, num_radial=8, cap_start=False, cap_end=True)
    
    # 2. Lower Right Main Limb -> Far Lower Right (c5) & Mid Inner Right (c6)
    # Branches off right bow at z ~ 0.58m
    limb_lower_r = [
        (Vector((0.160, 0.000, 0.580)), 0.070),
        (Vector((0.280, -0.020, 0.680)), 0.058),
        (Vector((0.440, -0.040, 0.780)), 0.048),
    ]
    create_tube_bmesh(bm, limb_lower_r, num_radial=10, cap_start=False, cap_end=False)
    
    # Fork 2A -> Far Lower Right (c5 at (0.86, 0.22, 1.00))
    fork_lower_r_far = [
        (Vector((0.440, -0.040, 0.780)), 0.044),
        (Vector((0.640, 0.080, 0.860)), 0.034),
        (Vector((0.840, 0.200, 0.970)), 0.020),     # inside cluster 5
    ]
    create_tube_bmesh(bm, fork_lower_r_far, num_radial=8, cap_start=False, cap_end=True)
    
    # Fork 2B -> Mid Inner Right (c6 at (0.38, -0.28, 1.20))
    fork_lower_r_mid = [
        (Vector((0.440, -0.040, 0.780)), 0.040),
        (Vector((0.400, -0.180, 0.950)), 0.030),
        (Vector((0.380, -0.260, 1.160)), 0.018),    # inside cluster 6
    ]
    create_tube_bmesh(bm, fork_lower_r_mid, num_radial=8, cap_start=False, cap_end=True)
    
    # 3. Upper Left Limb -> Upper Left Cluster (c3 at (-0.52, 0.18, 1.58))
    limb_upper_l = [
        (Vector((0.050, 0.028, 1.200)), 0.050),
        (Vector((-0.160, 0.080, 1.320)), 0.040),
        (Vector((-0.360, 0.140, 1.450)), 0.030),
        (Vector((-0.500, 0.170, 1.560)), 0.018),    # inside cluster 3
    ]
    create_tube_bmesh(bm, limb_upper_l, num_radial=8, cap_start=False, cap_end=True)
    
    # 4. Upper Right Limb -> Upper Right Cluster (c2 at (0.58, -0.12, 1.62))
    limb_upper_r = [
        (Vector((0.040, 0.030, 1.260)), 0.052),
        (Vector((0.240, -0.030, 1.380)), 0.040),
        (Vector((0.440, -0.080, 1.500)), 0.030),
        (Vector((0.560, -0.110, 1.600)), 0.018),    # inside cluster 2
    ]
    create_tube_bmesh(bm, limb_upper_r, num_radial=8, cap_start=False, cap_end=True)
    
    # 5. Mid Back Depth Limb -> Mid Back Depth Cluster (c7 at (-0.08, 0.52, 1.40))
    limb_back = [
        (Vector((0.065, 0.028, 1.120)), 0.045),
        (Vector((0.000, 0.200, 1.220)), 0.036),
        (Vector((-0.050, 0.380, 1.320)), 0.028),
        (Vector((-0.075, 0.500, 1.380)), 0.018),    # inside cluster 7
    ]
    create_tube_bmesh(bm, limb_back, num_radial=8, cap_start=False, cap_end=True)
    
    # 6. Crown Forks -> Top Crown Cluster (c1 at (0.00, 0.02, 2.05))
    crown_fork_l = [
        (Vector((0.005, 0.035, 1.620)), 0.044),
        (Vector((-0.100, 0.025, 1.760)), 0.032),
        (Vector((-0.160, 0.020, 1.950)), 0.018),    # inside left lobe of crown
    ]
    create_tube_bmesh(bm, crown_fork_l, num_radial=8, cap_start=False, cap_end=True)
    
    crown_fork_r = [
        (Vector((0.005, 0.035, 1.620)), 0.044),
        (Vector((0.120, 0.035, 1.760)), 0.032),
        (Vector((0.180, 0.030, 1.950)), 0.018),     # inside right lobe of crown
    ]
    create_tube_bmesh(bm, crown_fork_r, num_radial=8, cap_start=False, cap_end=True)
    
    bm.to_mesh(mesh)
    bm.free()
    
    # Warm caramel wood brown matching reference (0.76, 0.52, 0.32)
    mat_bark = create_material("M_Bark", (0.68, 0.44, 0.24, 1.0), roughness=0.52)
    mesh.materials.append(mat_bark)
    for poly in mesh.polygons:
        poly.use_smooth = True
        
    return obj

def build_roots():
    mesh = bpy.data.meshes.new("Roots")
    obj = bpy.data.objects.new("Roots", mesh)
    bpy.context.collection.objects.link(obj)
    
    bm = bmesh.new()
    
    # =========================================================================
    # ROOT BUTTRESS COLLAR
    # Connects seamlessly to trunk base at z = 0.000 (r = 0.165m)
    # Flares smoothly into the spreading root claws without central spike
    # =========================================================================
    core_pts = [
        (Vector((0.000, 0.000, 0.010)), 0.160),    # inside trunk base
        (Vector((0.000, 0.000, 0.000)), 0.165),    # ground level z = 0, exact match with trunk
        (Vector((0.000, 0.000, -0.040)), 0.178),   # thick root buttress flare
        (Vector((0.002, 0.002, -0.090)), 0.130),   # tapers into root anchors
        (Vector((0.000, 0.000, -0.140)), 0.040),   # rounded bottom base, no spike!
    ]
    create_tube_bmesh(bm, core_pts, num_radial=12, cap_start=False, cap_end=True)
    
    def add_buttress_root(spine, tip_a, tip_b, num_radial=10):
        create_tube_bmesh(bm, spine, num_radial=num_radial, cap_start=False, cap_end=False)
        fork_pt = spine[-1][0]
        fork_r = spine[-1][1]
        
        branch_a = [(fork_pt, fork_r * 0.85)] + tip_a
        create_tube_bmesh(bm, branch_a, num_radial=max(6, num_radial - 2), cap_start=False, cap_end=True)
        
        branch_b = [(fork_pt, fork_r * 0.85)] + tip_b
        create_tube_bmesh(bm, branch_b, num_radial=max(6, num_radial - 2), cap_start=False, cap_end=True)

    # 1. Root Front-Left (-X, -Y)
    spine_fl = [
        (Vector((-0.050, -0.050, -0.020)), 0.085),
        (Vector((-0.200, -0.160, -0.060)), 0.068),
        (Vector((-0.340, -0.260, -0.130)), 0.050),
        (Vector((-0.460, -0.320, -0.220)), 0.032),
    ]
    tip_fl_a = [
        (Vector((-0.560, -0.360, -0.280)), 0.018),
        (Vector((-0.640, -0.380, -0.320)), 0.006),
    ]
    tip_fl_b = [
        (Vector((-0.500, -0.420, -0.280)), 0.016),
        (Vector((-0.540, -0.490, -0.330)), 0.006),
    ]
    add_buttress_root(spine_fl, tip_fl_a, tip_fl_b)
    
    # 2. Root Front-Center (subtle -X, strong -Y)
    spine_fc = [
        (Vector((-0.010, -0.070, -0.020)), 0.088),
        (Vector((-0.030, -0.220, -0.070)), 0.070),
        (Vector((-0.040, -0.360, -0.140)), 0.048),
        (Vector((-0.050, -0.460, -0.230)), 0.030),
    ]
    tip_fc_a = [
        (Vector((-0.130, -0.540, -0.280)), 0.016),
        (Vector((-0.190, -0.600, -0.320)), 0.006),
    ]
    tip_fc_b = [
        (Vector((0.040, -0.540, -0.270)), 0.016),
        (Vector((0.090, -0.600, -0.310)), 0.006),
    ]
    add_buttress_root(spine_fc, tip_fc_a, tip_fc_b)
    
    # 3. Root Front-Right (+X, -Y)
    spine_fr = [
        (Vector((0.060, -0.050, -0.020)), 0.084),
        (Vector((0.220, -0.150, -0.060)), 0.066),
        (Vector((0.360, -0.240, -0.130)), 0.048),
        (Vector((0.480, -0.300, -0.220)), 0.030),
    ]
    tip_fr_a = [
        (Vector((0.580, -0.340, -0.270)), 0.016),
        (Vector((0.660, -0.360, -0.310)), 0.006),
    ]
    tip_fr_b = [
        (Vector((0.520, -0.400, -0.280)), 0.016),
        (Vector((0.560, -0.480, -0.320)), 0.006),
    ]
    add_buttress_root(spine_fr, tip_fr_a, tip_fr_b)
    
    # 4. Root Right-Back (+X, +Y)
    spine_rb = [
        (Vector((0.070, 0.050, -0.020)), 0.080),
        (Vector((0.240, 0.160, -0.060)), 0.064),
        (Vector((0.380, 0.260, -0.130)), 0.046),
        (Vector((0.500, 0.340, -0.220)), 0.030),
    ]
    tip_rb_a = [
        (Vector((0.600, 0.400, -0.270)), 0.016),
        (Vector((0.680, 0.440, -0.310)), 0.006),
    ]
    tip_rb_b = [
        (Vector((0.520, 0.440, -0.280)), 0.016),
        (Vector((0.560, 0.520, -0.320)), 0.006),
    ]
    add_buttress_root(spine_rb, tip_rb_a, tip_rb_b)
    
    # 5. Root Back-Center (+Y)
    spine_bc = [
        (Vector((0.010, 0.070, -0.020)), 0.082),
        (Vector((0.020, 0.240, -0.060)), 0.065),
        (Vector((0.040, 0.380, -0.140)), 0.046),
        (Vector((0.050, 0.480, -0.220)), 0.030),
    ]
    tip_bc_a = [
        (Vector((-0.040, 0.560, -0.270)), 0.016),
        (Vector((-0.090, 0.620, -0.310)), 0.006),
    ]
    tip_bc_b = [
        (Vector((0.130, 0.550, -0.270)), 0.016),
        (Vector((0.180, 0.610, -0.310)), 0.006),
    ]
    add_buttress_root(spine_bc, tip_bc_a, tip_bc_b)
    
    # 6. Root Left-Back (-X, +Y)
    spine_lb = [
        (Vector((-0.060, 0.050, -0.020)), 0.080),
        (Vector((-0.220, 0.170, -0.060)), 0.064),
        (Vector((-0.360, 0.280, -0.130)), 0.046),
        (Vector((-0.470, 0.360, -0.220)), 0.030),
    ]
    tip_lb_a = [
        (Vector((-0.560, 0.420, -0.270)), 0.016),
        (Vector((-0.630, 0.460, -0.310)), 0.006),
    ]
    tip_lb_b = [
        (Vector((-0.490, 0.460, -0.280)), 0.016),
        (Vector((-0.520, 0.540, -0.320)), 0.006),
    ]
    add_buttress_root(spine_lb, tip_lb_a, tip_lb_b)
    
    bm.to_mesh(mesh)
    bm.free()
    
    # Same warm bark material
    mat_bark = create_material("M_Bark", (0.74, 0.50, 0.30, 1.0), roughness=0.48)
    mesh.materials.append(mat_bark)
    for poly in mesh.polygons:
        poly.use_smooth = True
        
    return obj

def setup_camera_and_lights():
    cam_data = bpy.data.cameras.new(name="PreviewCam")
    cam_data.lens = 45.0
    cam_obj = bpy.data.objects.new("PreviewCam", cam_data)
    bpy.context.collection.objects.link(cam_obj)
    bpy.context.scene.camera = cam_obj
    
    # Key light: warm sunlight with balanced exposure
    sun_data = bpy.data.lights.new(name="KeyLight", type='SUN')
    sun_data.energy = 2.4
    sun_data.color = (1.0, 0.96, 0.90)
    sun_obj = bpy.data.objects.new("KeyLight", sun_data)
    bpy.context.collection.objects.link(sun_obj)
    sun_obj.rotation_euler = (math.radians(48), math.radians(16), math.radians(-35))
    
    # Fill light: soft sky fill from upper right
    fill_data = bpy.data.lights.new(name="FillLight", type='SUN')
    fill_data.energy = 1.4
    fill_data.color = (0.88, 0.94, 1.0)
    fill_obj = bpy.data.objects.new("FillLight", fill_data)
    bpy.context.collection.objects.link(fill_obj)
    fill_obj.rotation_euler = (math.radians(40), math.radians(-25), math.radians(45))
    
    # Rim light from behind
    rim_data = bpy.data.lights.new(name="RimLight", type='SUN')
    rim_data.energy = 1.2
    rim_data.color = (1.0, 1.0, 1.0)
    rim_obj = bpy.data.objects.new("RimLight", rim_data)
    bpy.context.collection.objects.link(rim_obj)
    rim_obj.rotation_euler = (math.radians(-50), math.radians(10), math.radians(175))
    
    return cam_obj

def render_multi_angles_and_turntable(cam_obj, output_dir):
    os.makedirs(output_dir, exist_ok=True)
    turntable_dir = os.path.join(output_dir, "turntable")
    os.makedirs(turntable_dir, exist_ok=True)
    
    scene = bpy.context.scene
    scene.render.resolution_x = 1024
    scene.render.resolution_y = 1024
    scene.render.film_transparent = True
    scene.render.image_settings.file_format = 'PNG'
    
    target = Vector((0.0, 0.0, 0.95))
    dist = 4.30
    
    # 7 standard inspection angles
    angles = [
        ("preview_front.png", 0.0, 6.0, dist),
        ("preview_back.png", 180.0, 6.0, dist),
        ("preview_left.png", 90.0, 6.0, dist),
        ("preview_right.png", -90.0, 6.0, dist),
        ("preview_top_down.png", 0.0, 84.0, dist * 0.95),
        ("preview_perspective_front_3_4.png", 35.0, 14.0, dist),
        ("preview_perspective_back_3_4.png", 215.0, 14.0, dist),
    ]
    
    for name, yaw_deg, pitch_deg, d in angles:
        yaw = math.radians(yaw_deg)
        pitch = math.radians(pitch_deg)
        
        cx = target.x + d * math.sin(yaw) * math.cos(pitch)
        cy = target.y - d * math.cos(yaw) * math.cos(pitch)
        cz = target.z + d * math.sin(pitch)
        
        cam_obj.location = Vector((cx, cy, cz))
        direction = target - cam_obj.location
        cam_obj.rotation_euler = direction.to_track_quat('-Z', 'Y').to_euler()
        
        scene.render.filepath = os.path.join(output_dir, name)
        bpy.ops.render.render(write_still=True)
        print(f"Rendered: {scene.render.filepath}")
        
    # Render 360 Turntable sequence (12 frames: every 30 degrees)
    print("\nRendering 360-degree turntable frames...")
    turntable_pitch = math.radians(10.0)
    for frame_idx, yaw_deg in enumerate(range(0, 360, 30)):
        yaw = math.radians(yaw_deg)
        cx = target.x + dist * math.sin(yaw) * math.cos(turntable_pitch)
        cy = target.y - dist * math.cos(yaw) * math.cos(turntable_pitch)
        cz = target.z + dist * math.sin(turntable_pitch)
        
        cam_obj.location = Vector((cx, cy, cz))
        direction = target - cam_obj.location
        cam_obj.rotation_euler = direction.to_track_quat('-Z', 'Y').to_euler()
        
        frame_name = f"turntable_{frame_idx:02d}_{yaw_deg:03d}deg.png"
        scene.render.filepath = os.path.join(turntable_dir, frame_name)
        bpy.ops.render.render(write_still=True)
        print(f"  Turntable [{frame_idx+1}/12]: {frame_name}")

def main():
    clear_scene()
    
    foliage = build_foliage()
    trunk = build_trunk_and_branches()
    roots = build_roots()
    
    all_objs = [roots, trunk, foliage]
    
    for obj in all_objs:
        obj.location = Vector((0, 0, 0))
        obj.rotation_euler = (0, 0, 0)
        obj.scale = (1, 1, 1)
        bpy.context.view_layer.objects.active = obj
        obj.select_set(True)
        bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
        obj.select_set(False)
        
    total_tris = 0
    total_verts = 0
    print("\n=== TREE_LARGE MESH STATISTICS (REFINED AESTHETICS) ===")
    for obj in all_objs:
        depsgraph = bpy.context.evaluated_depsgraph_get()
        eval_obj = obj.evaluated_get(depsgraph)
        m = eval_obj.to_mesh()
        m.calc_loop_triangles()
        tris = len(m.loop_triangles)
        verts = len(m.vertices)
        total_tris += tris
        total_verts += verts
        print(f"Object '{obj.name:15}': {verts:5} vertices, {len(m.polygons):5} quads/polys, {tris:5} triangles")
        eval_obj.to_mesh_clear()
    print(f"TOTAL VERTS: {total_verts}")
    print(f"TOTAL TRIANGLES: {total_tris}\n")
    
    # Save directly to ArtSource/Models/Tree_Large.blend
    blend_path = os.path.abspath(r"ArtSource\Models\Tree_Large.blend")
    os.makedirs(os.path.dirname(blend_path), exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=blend_path)
    print(f"Saved directly to target blend file: {blend_path}")
    
    # Render previews and turntable
    cam_obj = setup_camera_and_lights()
    evidence_dir = os.path.abspath(r"Evidence\Tree_Large_v01_Audit")
    render_multi_angles_and_turntable(cam_obj, evidence_dir)
    
    # Clean up temporary render camera and lights before export
    bpy.ops.object.select_all(action='DESELECT')
    cam_obj.select_set(True)
    for light in [o for o in bpy.data.objects if o.type == 'LIGHT']:
        light.select_set(True)
    bpy.ops.object.delete()
    
    # Select only the 3 tree mesh objects
    bpy.ops.object.select_all(action='DESELECT')
    for obj in all_objs:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = all_objs[0]
    
    # Export directly to ArtSource/Exports/Tree_Large.fbx
    fbx_path = os.path.abspath(r"ArtSource\Exports\Tree_Large.fbx")
    os.makedirs(os.path.dirname(fbx_path), exist_ok=True)
    
    bpy.ops.export_scene.fbx(
        filepath=fbx_path,
        use_selection=True,
        global_scale=1.0,
        apply_unit_scale=True,
        apply_scale_options='FBX_SCALE_ALL',
        axis_forward='-Z',
        axis_up='Y',
        bake_space_transform=False,
        object_types={'MESH'},
        mesh_smooth_type='FACE',
        add_leaf_bones=False,
        primary_bone_axis='Y',
        secondary_bone_axis='X',
        use_armature_deform_only=False,
        bake_anim=False
    )
    print(f"Exported directly to target FBX: {fbx_path}")

if __name__ == "__main__":
    main()
