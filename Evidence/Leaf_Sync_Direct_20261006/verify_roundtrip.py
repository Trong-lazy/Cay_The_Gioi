"""Read-only saved blend and independent FBX geometry roundtrip verification."""
import bpy
import bmesh
import hashlib
import json
from pathlib import Path
from mathutils import Vector
from mathutils.kdtree import KDTree

OUT = Path(__file__).resolve().parent
ROOT = OUT.parents[1]
BLEND = ROOT / 'ArtSource/Models/Tree_Large.blend'
FBX = ROOT / 'ArtSource/Exports/Tree_Large.fbx'


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def snapshot():
    result = {}
    for obj in bpy.context.scene.objects:
        if obj.type != 'MESH':
            continue
        m = obj.data
        m.calc_loop_triangles()
        bm = bmesh.new(); bm.from_mesh(m)
        positions = [list(obj.matrix_world @ v.co) for v in m.vertices]
        result[obj.name] = {
            'positions': positions, 'vertices': len(m.vertices),
            'triangles': len(m.loop_triangles), 'uv_layers': [l.name for l in m.uv_layers],
            'materials': [s.material.name if s.material else None for s in obj.material_slots],
            'scale': list(obj.scale), 'location': list(obj.location),
            'rotation_euler': list(obj.rotation_euler),
            'non_manifold_edges': sum(not e.is_manifold for e in bm.edges),
            'loose_vertices': sum(not v.link_edges for v in bm.verts),
            'degenerate_faces': sum(f.calc_area() < 1e-10 for f in bm.faces),
            'bounds': {'min': [min(p[i] for p in positions) for i in range(3)],
                       'max': [max(p[i] for p in positions) for i in range(3)]}}
        bm.free()
    return result


def distance(a, b):
    kd = KDTree(len(b))
    for i, p in enumerate(b):
        kd.insert(Vector(p), i)
    kd.balance()
    return max(kd.find(Vector(p))[2] for p in a)


def render_neutral(label):
    # Read-only process: temporary display material is never saved/exported.
    scene = bpy.context.scene
    objs = [o for o in scene.objects if o.type == 'MESH']
    for o in list(scene.objects):
        if o.type in {'CAMERA', 'LIGHT'}:
            bpy.data.objects.remove(o, do_unlink=True)
    mat = bpy.data.materials.new('__VerifyClay')
    mat.use_nodes = True
    mat.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value = (.42, .42, .42, 1)
    mat.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value = .8
    pts = []
    for o in objs:
        o.data.materials.clear(); o.data.materials.append(mat)
        for p in o.data.polygons:
            p.material_index = 0
        pts.extend(o.matrix_world @ v.co for v in o.data.vertices)
    lo = Vector([min(p[i] for p in pts) for i in range(3)])
    hi = Vector([max(p[i] for p in pts) for i in range(3)])
    center = (lo + hi) / 2; extent = max(hi - lo)
    cam_data = bpy.data.cameras.new('__VerifyCamera')
    cam = bpy.data.objects.new('__VerifyCamera', cam_data)
    scene.collection.objects.link(cam); scene.camera = cam
    cam_data.type = 'ORTHO'; cam_data.ortho_scale = extent * 1.25
    cam.location = center + Vector((1, -1, .45)).normalized() * extent * 4
    cam.rotation_euler = (center - cam.location).to_track_quat('-Z', 'Y').to_euler()
    for direction in [(1, -2, 3), (-2, -1, 1), (0, 2, 2)]:
        data = bpy.data.lights.new('__VerifyLight', 'AREA')
        data.energy = 90 * extent * extent; data.shape = 'DISK'; data.size = extent * 1.5
        obj = bpy.data.objects.new(data.name, data); scene.collection.objects.link(obj)
        obj.location = center + Vector(direction) * extent
        obj.rotation_euler = (center - obj.location).to_track_quat('-Z', 'Y').to_euler()
    scene.world = bpy.data.worlds.new('__VerifyWorld'); scene.world.use_nodes = True
    bg = scene.world.node_tree.nodes['Background']
    bg.inputs[0].default_value = (.14, .16, .18, 1); bg.inputs[1].default_value = .6
    scene.render.engine = 'BLENDER_EEVEE'
    if hasattr(scene.eevee, 'taa_render_samples'):
        scene.eevee.taa_render_samples = 128
    scene.render.resolution_x = 900; scene.render.resolution_y = 900
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = 'PNG'
    scene.render.filepath = str(OUT / (label + '.png'))
    bpy.ops.render.render(write_still=True)


applied = json.loads((OUT / 'apply_report.json').read_text(encoding='utf-8'))
assert sha(BLEND) == applied['output_hash']
assert sha(FBX) == applied['export_hash']
bpy.ops.wm.open_mainfile(filepath=str(BLEND), use_scripts=False)
saved = snapshot()
assert set(saved) == {'Roots', 'Trunk_Branches', 'Foliage'}
render_neutral('saved_blend_perspective')
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(FBX), use_custom_normals=True)
imported = snapshot()
assert set(imported) == set(saved)
matches = {}
for name in saved:
    a = saved[name]; b = imported[name]
    error = max(distance(a['positions'], b['positions']), distance(b['positions'], a['positions']))
    assert error < 1e-5, (name, error)
    assert a['vertices'] == b['vertices']
    assert a['triangles'] == b['triangles']
    assert a['materials'] == b['materials']
    assert a['uv_layers'] == b['uv_layers']
    assert all(abs(x - 1) < 1e-6 for x in b['scale'])
    matches[name] = {'world_vertex_max_error': error, 'triangles': b['triangles'],
                     'vertex_count_match': True, 'material_slots_match': True, 'uv_layers_match': True}
render_neutral('reimport_fbx_perspective')
assert sha(BLEND) == applied['output_hash']
assert sha(FBX) == applied['export_hash']
assert all(sha(p) == h for p, h in applied['protected_input_hashes'].items())
for group in [saved, imported]:
    for o in group.values():
        o.pop('positions')
report = {'blender': bpy.app.version_string, 'saved': saved, 'imported': imported,
          'roundtrip': matches, 'total_triangles': sum(o['triangles'] for o in saved.values()),
          'inputs_unchanged': True, 'protected_models_unchanged': True,
          'unity_editor_validation': 'NOT_RUN', 'blender_gui_validation': 'NOT_RUN'}
(OUT / 'roundtrip_report.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
print('ROUNDTRIP_PASS', json.dumps(report))
