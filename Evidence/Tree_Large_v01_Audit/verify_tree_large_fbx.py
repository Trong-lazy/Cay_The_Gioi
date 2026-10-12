import bpy
import os
import math
from mathutils import Vector

def run_fbx_verification():
    # Clear scene
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    for block in list(bpy.data.meshes):
        bpy.data.meshes.remove(block)
    for block in list(bpy.data.materials):
        bpy.data.materials.remove(block)

    fbx_path = os.path.abspath(r"ArtSource\Exports\Tree_Large.fbx")
    print(f"Importing FBX: {fbx_path}")
    
    bpy.ops.import_scene.fbx(filepath=fbx_path)
    
    expected_objs = ["Roots", "Trunk_Branches", "Foliage"]
    imported_objs = [o for o in bpy.data.objects if o.type == 'MESH']
    
    print("\n=== TREE_LARGE FBX REIMPORT AUDIT ===")
    print(f"Total Mesh Objects: {len(imported_objs)}")
    
    total_tris = 0
    total_verts = 0
    bounds_min = Vector((1e9, 1e9, 1e9))
    bounds_max = Vector((-1e9, -1e9, -1e9))
    
    for obj in imported_objs:
        bpy.context.view_layer.objects.active = obj
        depsgraph = bpy.context.evaluated_depsgraph_get()
        eval_obj = obj.evaluated_get(depsgraph)
        m = eval_obj.to_mesh()
        m.calc_loop_triangles()
        
        tris = len(m.loop_triangles)
        verts = len(m.vertices)
        total_tris += tris
        total_verts += verts
        
        scale = tuple(round(s, 4) for s in obj.scale)
        rot = tuple(round(math.degrees(r), 2) for r in obj.rotation_euler)
        
        for corner in obj.bound_box:
            world_corner = obj.matrix_world @ Vector(corner)
            bounds_min.x = min(bounds_min.x, world_corner.x)
            bounds_min.y = min(bounds_min.y, world_corner.y)
            bounds_min.z = min(bounds_min.z, world_corner.z)
            bounds_max.x = max(bounds_max.x, world_corner.x)
            bounds_max.y = max(bounds_max.y, world_corner.y)
            bounds_max.z = max(bounds_max.z, world_corner.z)
            
        print(f"Object: {obj.name:15} | Verts: {verts:4} | Tris: {tris:5} | Scale: {scale} | Rot: {rot}")
        eval_obj.to_mesh_clear()
        
    print(f"\nTOTAL VERTS: {total_verts}")
    print(f"TOTAL TRIANGLES: {total_tris}")
    width_x = bounds_max.x - bounds_min.x
    depth_y = bounds_max.y - bounds_min.y
    height_z = bounds_max.z - bounds_min.z
    print(f"Overall Bounding Box:")
    print(f"  X: [{bounds_min.x:.3f}, {bounds_max.x:.3f}] (width: {width_x:.3f}m)")
    print(f"  Y: [{bounds_min.y:.3f}, {bounds_max.y:.3f}] (depth: {depth_y:.3f}m)")
    print(f"  Z: [{bounds_min.z:.3f}, {bounds_max.z:.3f}] (height: {height_z:.3f}m)")
    print(f"  Depth to Width Ratio (Y/X): {(depth_y / width_x) * 100:.1f}% (Target: 60-80%)")
    print(f"  Shoot height above collar (z > 0): {bounds_max.z:.3f}m")
    print(f"  Root depth below collar (z < 0): {abs(bounds_min.z):.3f}m")
    
    # Render FBX reimport image
    cam_data = bpy.data.cameras.new("RenderCam")
    cam_data.lens = 45.0
    cam_obj = bpy.data.objects.new("RenderCam", cam_data)
    bpy.context.collection.objects.link(cam_obj)
    bpy.context.scene.camera = cam_obj
    
    light_data = bpy.data.lights.new(name="KeyLight", type='SUN')
    light_data.energy = 2.4
    light_obj = bpy.data.objects.new("KeyLight", light_data)
    bpy.context.collection.objects.link(light_obj)
    light_obj.rotation_euler = (math.radians(48), math.radians(16), math.radians(-35))
    
    fill_data = bpy.data.lights.new(name="FillLight", type='SUN')
    fill_data.energy = 1.4
    fill_obj = bpy.data.objects.new("FillLight", fill_data)
    bpy.context.collection.objects.link(fill_obj)
    fill_obj.rotation_euler = (math.radians(40), math.radians(-25), math.radians(45))
    
    target = Vector((0.0, 0.0, 0.95))
    dist = 4.30
    yaw = math.radians(35.0)
    pitch = math.radians(14.0)
    cx = target.x + dist * math.sin(yaw) * math.cos(pitch)
    cy = target.y - dist * math.cos(yaw) * math.cos(pitch)
    cz = target.z + dist * math.sin(pitch)
    cam_obj.location = Vector((cx, cy, cz))
    cam_obj.rotation_euler = (target - cam_obj.location).to_track_quat('-Z', 'Y').to_euler()
    
    scene = bpy.context.scene
    scene.render.resolution_x = 1024
    scene.render.resolution_y = 1024
    scene.render.film_transparent = True
    scene.render.filepath = os.path.abspath(r"Evidence\Tree_Large_v01_Audit\reimport_fbx_perspective.png")
    bpy.ops.render.render(write_still=True)
    print(f"Rendered FBX verification image: {scene.render.filepath}")

if __name__ == "__main__":
    run_fbx_verification()
