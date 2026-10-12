"""Test preview script for P03 bark material and textures on Tree_Large.
"""
import os
import bpy
from mathutils import Vector, Euler

BLEND_PATH = r'c:\Users\Asus\foder game 3D\Cay_The_Gioi\ArtSource\Models\Tree_Large.blend'
TEXTURE_DIR = r'c:\Users\Asus\foder game 3D\Cay_The_Gioi\ArtSource\Textures\Tree_Common'
EVIDENCE_DIR = r'c:\Users\Asus\foder game 3D\Cay_The_Gioi\Evidence\Tree_Materials_P03_20261006\preview'

os.makedirs(EVIDENCE_DIR, exist_ok=True)

bpy.ops.wm.open_mainfile(filepath=BLEND_PATH)

# 1. Unwrap Trunk_Branches and Roots
for obj_name in ['Trunk_Branches', 'Roots']:
    obj = bpy.data.objects[obj_name]
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    
    for window in bpy.context.window_manager.windows:
        for area in window.screen.areas:
            if area.type == 'VIEW_3D':
                for region in area.regions:
                    if region.type == 'WINDOW':
                        with bpy.context.temp_override(window=window, area=area, region=region, active_object=obj, selected_objects=[obj], selected_editable_objects=[obj]):
                            bpy.ops.object.mode_set(mode='EDIT')
                            bpy.ops.mesh.select_all(action='SELECT')
                            bpy.ops.uv.smart_project(angle_limit=66.0, island_margin=0.01)
                            bpy.ops.object.mode_set(mode='OBJECT')
                        break
    obj.select_set(False)

# 2. Setup M_Bark material
mat = bpy.data.materials.get('M_Bark')
if not mat:
    mat = bpy.data.materials.new('M_Bark')
    mat.use_nodes = True
else:
    mat.use_nodes = True

nodes = mat.node_tree.nodes
links = mat.node_tree.links
nodes.clear()

# Output node
node_out = nodes.new('ShaderNodeOutputMaterial')
node_out.location = (600, 0)

# Principled BSDF
node_bsdf = nodes.new('ShaderNodeBsdfPrincipled')
node_bsdf.location = (300, 0)
node_bsdf.inputs['Roughness'].default_value = 0.85
node_bsdf.inputs['Metallic'].default_value = 0.0
if 'Emission Color' in node_bsdf.inputs:
    node_bsdf.inputs['Emission Color'].default_value = (0, 0, 0, 1)

# TexCoord & Mapping
node_coord = nodes.new('ShaderNodeTexCoord')
node_coord.location = (-600, 0)

node_map = nodes.new('ShaderNodeMapping')
node_map.location = (-400, 0)
node_map.inputs['Scale'].default_value = (2.5, 2.5, 1.0)
links.new(node_coord.outputs['UV'], node_map.inputs['Vector'])

# BaseColor Texture
node_tex_bc = nodes.new('ShaderNodeTexImage')
node_tex_bc.location = (-150, 150)
bc_img_path = os.path.join(TEXTURE_DIR, 'Large_Bark_BaseColor.png')
bc_img = bpy.data.images.load(bc_img_path, check_existing=True)
node_tex_bc.image = bc_img
links.new(node_map.outputs['Vector'], node_tex_bc.inputs['Vector'])
links.new(node_tex_bc.outputs['Color'], node_bsdf.inputs['Base Color'])

# NormalMap Texture
node_tex_norm = nodes.new('ShaderNodeTexImage')
node_tex_norm.location = (-150, -150)
norm_img_path = os.path.join(TEXTURE_DIR, 'Large_Bark_Normal.png')
norm_img = bpy.data.images.load(norm_img_path, check_existing=True)
norm_img.colorspace_settings.name = 'Non-Color'
node_tex_norm.image = norm_img
links.new(node_map.outputs['Vector'], node_tex_norm.inputs['Vector'])

# NormalMap Node
node_norm_map = nodes.new('ShaderNodeNormalMap')
node_norm_map.location = (100, -150)
node_norm_map.space = 'TANGENT'
node_norm_map.inputs['Strength'].default_value = 0.85
links.new(node_tex_norm.outputs['Color'], node_norm_map.inputs['Color'])
links.new(node_norm_map.outputs['Normal'], node_bsdf.inputs['Normal'])

links.new(node_bsdf.outputs['BSDF'], node_out.inputs['Surface'])

# 3. Setup Camera & Lights for Neutral Studio Render
# Clean existing scene review elements
for o in list(bpy.data.objects):
    if o.type in ('CAMERA', 'LIGHT'):
        bpy.data.objects.remove(o, do_unlink=True)

# Add Camera
cam_data = bpy.data.cameras.new('ReviewCam')
cam_obj = bpy.data.objects.new('ReviewCam', cam_data)
bpy.context.scene.collection.objects.link(cam_obj)
bpy.context.scene.camera = cam_obj
cam_data.sensor_fit = 'AUTO'
cam_data.lens = 50.0

# Add Key, Fill, Rim, and Oblique Sunlight
light_data_sun = bpy.data.lights.new('Sun_Oblique', type='SUN')
light_data_sun.energy = 2.5
light_data_sun.angle = 0.1
light_obj_sun = bpy.data.objects.new('Sun_Oblique', light_data_sun)
light_obj_sun.rotation_euler = Euler((0.8, 0.4, 0.6), 'XYZ')
bpy.context.scene.collection.objects.link(light_obj_sun)

light_data_fill = bpy.data.lights.new('Fill_Point', type='POINT')
light_data_fill.energy = 150.0
light_obj_fill = bpy.data.objects.new('Fill_Point', light_data_fill)
light_obj_fill.location = (-4.0, -3.0, 4.0)
bpy.context.scene.collection.objects.link(light_obj_fill)

# Render settings
scene = bpy.context.scene
scene.render.engine = 'BLENDER_EEVEE_NEXT' if hasattr(bpy.types, 'RenderSettings') and 'BLENDER_EEVEE_NEXT' in [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties['engine'].enum_items] else 'BLENDER_EEVEE'
scene.render.resolution_x = 1024
scene.render.resolution_y = 1024
scene.render.resolution_percentage = 100
scene.render.film_transparent = True
scene.view_settings.view_transform = 'Standard'

# Views to render
views = {
    'perspective': (Vector((3.2, -4.5, 2.2)), Vector((0.0, 0.0, 1.15))),
    'front': (Vector((0.0, -5.5, 1.2)), Vector((0.0, 0.0, 1.15))),
    'back': (Vector((0.0, 5.5, 1.2)), Vector((0.0, 0.0, 1.15))),
    'left': (Vector((-5.5, 0.0, 1.2)), Vector((0.0, 0.0, 1.15))),
    'right': (Vector((5.5, 0.0, 1.2)), Vector((0.0, 0.0, 1.15))),
    'closeup_fork': (Vector((0.3, -1.2, 1.35)), Vector((0.0, 0.0, 1.25))),
}

for view_name, (cam_pos, target_pos) in views.items():
    cam_obj.location = cam_pos
    direction = target_pos - cam_pos
    rot_quat = direction.to_track_quat('-Z', 'Y')
    cam_obj.rotation_euler = rot_quat.to_euler()
    
    out_img = os.path.join(EVIDENCE_DIR, f'{view_name}.png')
    scene.render.filepath = out_img
    bpy.ops.render.render(write_still=True)
    print(f'Rendered {view_name} -> {out_img}')

print('PREVIEW RENDERS FINISHED!')
