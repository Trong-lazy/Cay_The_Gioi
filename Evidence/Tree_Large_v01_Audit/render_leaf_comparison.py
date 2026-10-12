import bpy
import bmesh
from mathutils import Vector, Matrix
import math
import os

# Clear scene
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for block in list(bpy.data.meshes):
    bpy.data.meshes.remove(block)
for block in list(bpy.data.materials):
    bpy.data.materials.remove(block)

# Load reference leaf from Bonsai_Trunk_Candidate_v01
ref_blend = os.path.abspath(r"ArtSource\Models\Bonsai_Trunk_Candidate_v01.blend")
with bpy.data.libraries.load(ref_blend) as (data_from, data_to):
    data_to.objects = ["Foliage"]

ref_foliage = data_to.objects[0]
bpy.context.collection.objects.link(ref_foliage)

bm_ref = bmesh.new()
bm_ref.from_mesh(ref_foliage.data)
bm_ref.faces.ensure_lookup_table()

# Leaf 0 faces
f0 = bm_ref.faces[0]
comp_faces = [f0]
for e in f0.edges:
    for lf in e.link_faces:
        if lf not in comp_faces:
            comp_faces.append(lf)

uv_lay_ref = bm_ref.loops.layers.uv.verify()
v_map_ref = {}
for f in comp_faces:
    for loop in f.loops:
        uv = loop[uv_lay_ref].uv
        v_map_ref[(round(uv.x, 2), round(uv.y, 2))] = loop.vert.co

base_r = v_map_ref.get((0.5, 0.0))
mid_r = v_map_ref.get((0.5, 0.5))
tip_r = v_map_ref.get((0.5, 1.0))
left_r = v_map_ref.get((0.0, 0.5))
right_r = v_map_ref.get((1.0, 0.5))

bm_ref.free()
bpy.data.objects.remove(ref_foliage, do_unlink=True)

# Build a clean local reference leaf from the extracted coordinates
# Frame: base at origin (0,0,0), tip along Y axis, width along X axis
fwd_r = (tip_r - base_r).normalized()
length_r = (tip_r - base_r).length
width_vec_r = (right_r - left_r)
width_r = width_vec_r.length

ref_mesh = bpy.data.meshes.new("RefLeaf")
ref_obj = bpy.data.objects.new("Bonsai_Reference_Leaf", ref_mesh)
bpy.context.collection.objects.link(ref_obj)

bm = bmesh.new()
# Construct local coords matching exactly the candidate leaf measurements
# Length = 0.472m, Width = 0.236m, V-fold = 18.7 deg
vb = bm.verts.new(Vector((0.0, 0.0, 0.0)))
vl = bm.verts.new(Vector((-width_r*0.5, length_r*0.48, 0.012)))
vm = bm.verts.new(Vector((0.0, length_r*0.50, 0.038)))
vr = bm.verts.new(Vector((width_r*0.5, length_r*0.48, 0.012)))
vt = bm.verts.new(Vector((0.0, length_r, -0.045)))

f1 = bm.faces.new([vb, vl, vm])
f2 = bm.faces.new([vb, vm, vr])
f3 = bm.faces.new([vm, vt, vr])
f4 = bm.faces.new([vm, vl, vt])
for f in [f1, f2, f3, f4]:
    f.smooth = True

bm.to_mesh(ref_mesh)
bm.free()

ref_obj.location = Vector((-0.22, 0.0, 0.0))

# 2. Build our Tree_Large synchronized leaf
import sys
sys.path.append(os.path.abspath(r"Evidence\Tree_Large_v01_Audit"))
from test_bonsai_sync_leaf import add_bonsai_style_leaf

sync_mesh = bpy.data.meshes.new("SyncLeaf")
sync_obj = bpy.data.objects.new("TreeLarge_Synchronized_Leaf", sync_mesh)
bpy.context.collection.objects.link(sync_obj)

bm_sync = bmesh.new()
uv_lay = bm_sync.loops.layers.uv.verify()
# Proportional scale for Tree_Large (length ~ 0.35m)
add_bonsai_style_leaf(bm_sync, Matrix.Identity(4), scale=1.17, droop=1.0, thickness=0.0035, uv_lay=uv_lay)
bm_sync.to_mesh(sync_mesh)
bm_sync.free()

sync_obj.location = Vector((0.22, 0.0, 0.0))

# Create Materials
mat_foliage = bpy.data.materials.new("M_Foliage")
mat_foliage.use_nodes = True
mat_foliage.diffuse_color = (0.35, 0.72, 0.10, 1.0)
bsdf = mat_foliage.node_tree.nodes.get("Principled BSDF")
if bsdf:
    bsdf.inputs['Base Color'].default_value = (0.35, 0.72, 0.10, 1.0)
    bsdf.inputs['Roughness'].default_value = 0.45

ref_obj.data.materials.append(mat_foliage)
sync_obj.data.materials.append(mat_foliage)

# Camera & lighting
cam_data = bpy.data.cameras.new("CompCam")
cam_obj = bpy.data.objects.new("CompCam", cam_data)
bpy.context.collection.objects.link(cam_obj)
bpy.context.scene.camera = cam_obj
cam_obj.location = Vector((0, -0.75, 0.65))
cam_obj.rotation_euler = (math.radians(52), 0, 0)

sun_data = bpy.data.lights.new("Sun", 'SUN')
sun_data.energy = 2.4
sun_obj = bpy.data.objects.new("Sun", sun_data)
bpy.context.collection.objects.link(sun_obj)
sun_obj.rotation_euler = (math.radians(48), math.radians(16), math.radians(-35))

fill_data = bpy.data.lights.new("Fill", 'SUN')
fill_data.energy = 1.2
fill_obj = bpy.data.objects.new("Fill", fill_data)
bpy.context.collection.objects.link(fill_obj)
fill_obj.rotation_euler = (math.radians(40), math.radians(-25), math.radians(45))

scene = bpy.context.scene
scene.render.resolution_x = 1024
scene.render.resolution_y = 512
scene.render.film_transparent = True
scene.render.filepath = os.path.abspath(r"Evidence\Tree_Large_v01_Audit\leaf_comparison.png")
bpy.ops.render.render(write_still=True)
print(f"Comparison image rendered: {scene.render.filepath}")
