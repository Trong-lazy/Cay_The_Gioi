"""Inspect a Blender/FBX asset and optionally render it without saving the source."""
import bpy, bmesh, json, sys, os, math, hashlib
from pathlib import Path
from mathutils import Vector

args = sys.argv[sys.argv.index('--') + 1:]
source, output, mode = args[:3]
out = Path(output)
out.mkdir(parents=True, exist_ok=True)
before = hashlib.sha256(Path(source).read_bytes()).hexdigest()
if mode == 'blend':
    bpy.ops.wm.open_mainfile(filepath=source, use_scripts=False)
else:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=source)

scene = bpy.context.scene
objects, points = [], []
deps = bpy.context.evaluated_depsgraph_get()
for obj in scene.objects:
    entry = {'name': obj.name, 'type': obj.type, 'parent': obj.parent.name if obj.parent else None,
             'location': list(obj.location), 'rotation_euler': list(obj.rotation_euler), 'scale': list(obj.scale),
             'dimensions': list(obj.dimensions), 'hide_render': obj.hide_render,
             'modifiers': [{'name': m.name, 'type': m.type} for m in obj.modifiers]}
    if obj.type == 'MESH':
        raw = obj.data
        evaluated = obj.evaluated_get(deps)
        mesh = evaluated.to_mesh()
        mesh.calc_loop_triangles()
        bm = bmesh.new(); bm.from_mesh(raw)
        entry.update(vertices=len(raw.vertices), faces=len(raw.polygons), evaluated_triangles=len(mesh.loop_triangles),
                     boundary_edges=sum(e.is_boundary for e in bm.edges),
                     non_manifold_edges=sum(not e.is_manifold for e in bm.edges),
                     loose_vertices=sum(not v.link_edges for v in bm.verts),
                     degenerate_faces=sum(f.calc_area() < 1e-10 for f in bm.faces),
                     uv_layers=[uv.name for uv in raw.uv_layers],
                     materials=[m.name if m else None for m in raw.materials])
        entry['uv_statistics'] = []
        for layer in raw.uv_layers:
            uvs = [d.uv for d in layer.data]
            entry['uv_statistics'].append({'name': layer.name, 'loops':len(uvs),
                'min': [min(v[i] for v in uvs) for i in range(2)] if uvs else None,
                'max': [max(v[i] for v in uvs) for i in range(2)] if uvs else None})
        bm.free(); evaluated.to_mesh_clear()
        if not obj.hide_render:
            points.extend(obj.matrix_world @ Vector(p) for p in obj.bound_box)
    objects.append(entry)

materials = []
for mat in bpy.data.materials:
    nodes = []
    if mat.use_nodes and mat.node_tree:
        for n in mat.node_tree.nodes:
            d = {'name': n.name, 'type': n.type}
            if n.type == 'BSDF_PRINCIPLED':
                d['base_color'] = list(n.inputs['Base Color'].default_value)
                d['roughness'] = n.inputs['Roughness'].default_value
                d['alpha'] = n.inputs['Alpha'].default_value
                d['linked_inputs'] = [i.name for i in n.inputs if i.is_linked]
            if n.type == 'TEX_IMAGE':
                d['image'] = n.image.name if n.image else None
            nodes.append(d)
    materials.append({'name': mat.name, 'diffuse_color': list(mat.diffuse_color),
                      'backface_culling': getattr(mat, 'use_backface_culling', None),
                      'surface_render_method': getattr(mat, 'surface_render_method', None), 'nodes': nodes})
images = [{'name': i.name, 'filepath': i.filepath, 'packed': bool(i.packed_file),
           'exists': os.path.exists(bpy.path.abspath(i.filepath)) if i.filepath else None}
          for i in bpy.data.images]
report = {'source': source, 'sha256_before': before, 'blender': bpy.app.version_string,
          'units': {'system': scene.unit_settings.system, 'scale_length': scene.unit_settings.scale_length},
          'objects': objects, 'materials': materials, 'images': images,
          'total_evaluated_triangles': sum(o.get('evaluated_triangles', 0) for o in objects),
          'empty_objects': [o['name'] for o in objects if o['type'] == 'EMPTY']}
if points:
    lo = Vector(tuple(min(p[i] for p in points) for i in range(3)))
    hi = Vector(tuple(max(p[i] for p in points) for i in range(3)))
    report['world_bounds'] = {'min': list(lo), 'max': list(hi), 'size': list(hi-lo)}

if len(args) > 3 and args[3] == 'render' and points:
    review = bpy.data.scenes.new('__ReadOnlyReview')
    for obj in list(scene.objects):
        if obj.type in {'MESH', 'CURVE', 'SURFACE', 'FONT'} and not obj.hide_render:
            review.collection.objects.link(obj)
    review.render.engine = 'BLENDER_EEVEE'
    review.render.resolution_x = 960; review.render.resolution_y = 720
    review.render.resolution_percentage = 100
    review.world = bpy.data.worlds.new('__ReviewWorld')
    review.world.use_nodes = True
    review.world.node_tree.nodes['Background'].inputs[0].default_value = (0.18, 0.20, 0.22, 1)
    review.world.node_tree.nodes['Background'].inputs[1].default_value = 0.7
    review.view_settings.view_transform = 'AgX'
    center = (lo+hi)*0.5; size = hi-lo; extent = max(size)
    cam_data = bpy.data.cameras.new('__ReviewCamera'); cam = bpy.data.objects.new('__ReviewCamera',cam_data)
    review.collection.objects.link(cam); review.camera = cam
    cam_data.type = 'ORTHO'; cam_data.ortho_scale = extent*1.5
    for index, direction in enumerate([(1,-1,2),(-1,-1,1),(0,1,2)]):
        ld = bpy.data.lights.new('__ReviewLight'+str(index),'AREA')
        ld.energy = 1000 if index == 0 else 650; ld.shape='DISK'; ld.size=extent*1.1
        light = bpy.data.objects.new(ld.name,ld); review.collection.objects.link(light)
        light.location = center + Vector(direction)*extent
        light.rotation_euler = (center-light.location).to_track_quat('-Z','Y').to_euler()
    report['review_renders'] = []
    for name, direction in [('front',(0,-1,0.12)),('back',(0,1,0.12)),('right',(1,0,0.12)),('perspective',(1,-1,0.65))]:
        cam.location = center + Vector(direction).normalized()*extent*2.5
        cam.rotation_euler = (center-cam.location).to_track_quat('-Z','Y').to_euler()
        path = str(out/(name+'.png')); review.render.filepath=path
        bpy.ops.render.render(write_still=True, scene=review.name)
        report['review_renders'].append(path)
report['sha256_after'] = hashlib.sha256(Path(source).read_bytes()).hexdigest()
report['source_unchanged'] = report['sha256_before'] == report['sha256_after']
(out/'report.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
print('READONLY_AUDIT',json.dumps({'triangles':report['total_evaluated_triangles'],'source_unchanged':report['source_unchanged'],'report':str(out/'report.json')}))
