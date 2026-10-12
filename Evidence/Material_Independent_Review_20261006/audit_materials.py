"""Read-only saved asset/material/texture/FBX review. No save/export operations."""
import bpy
import hashlib
import json
import math
import numpy as np
from pathlib import Path
from mathutils import Vector
from mathutils.kdtree import KDTree

OUT = Path(__file__).resolve().parent
ROOT = OUT.parents[1]
MODELS = {'seedling': 'Tree_Seedling_Simple_v01', 'young': 'Tree_Young_Simple_v01',
          'large': 'Tree_Large', 'ancient': 'Bonsai_Trunk_Candidate_v01'}


def sha(p):
    return hashlib.sha256(Path(p).read_bytes()).hexdigest()


def digest(data):
    return hashlib.sha256(json.dumps(data, separators=(',', ':')).encode()).hexdigest()


def val(x):
    if isinstance(x, (str, float, int, bool)):
        return x
    try:
        return list(x)
    except TypeError:
        return str(x)


def geom(o):
    m = o.data; m.calc_loop_triangles()
    positions = [list(o.matrix_world @ v.co) for v in m.vertices]
    uv = {u.name: digest([list(d.uv) for d in u.data]) for u in m.uv_layers}
    uv_degenerate = {}
    for u in m.uv_layers:
        count = 0
        for t in m.loop_triangles:
            a, b, c = [u.data[i].uv for i in t.loops]
            if abs((b.x-a.x)*(c.y-a.y)-(b.y-a.y)*(c.x-a.x)) < 1e-10:
                count += 1
        uv_degenerate[u.name] = count
    return {'vertices': len(m.vertices), 'triangles': len(m.loop_triangles),
        'position_hash': digest([list(v.co) for v in m.vertices]),
        'topology_hash': digest([[list(p.vertices) for p in m.polygons], [list(e.vertices) for e in m.edges]]),
        'normal_hash': digest([list(n.vector) for n in m.corner_normals]),
        'world_matrix': [list(r) for r in o.matrix_world],
        'uv_hashes': uv, 'uv_order': [u.name for u in m.uv_layers],
        'degenerate_uv_triangles': uv_degenerate,
        'materials': [s.material.name if s.material else None for s in o.material_slots],
        'modifiers': [m.type for m in o.modifiers],
        'bounds': {'min': [min(p[i] for p in positions) for i in range(3)],
                   'max': [max(p[i] for p in positions) for i in range(3)]},
        '_positions': positions}


def mats():
    result = {}
    used = {s.material for o in bpy.context.scene.objects if o.type == 'MESH' for s in o.material_slots if s.material}
    for m in used:
        nodes = []
        if m.use_nodes:
            for n in m.node_tree.nodes:
                item = {'name': n.name, 'type': n.type,
                    'inputs': [{'name': i.name, 'identifier': i.identifier,
                        'value': val(i.default_value), 'linked': i.is_linked}
                        for i in n.inputs if hasattr(i, 'default_value')]}
                if n.type == 'TEX_IMAGE' and n.image:
                    image = n.image
                    p = Path(bpy.path.abspath(image.filepath))
                    item.update(image=image.name, filepath=image.filepath, resolved_path=str(p),
                        exists=p.is_file(), size=list(image.size), colorspace=image.colorspace_settings.name,
                        interpolation=n.interpolation, extension=n.extension)
                if n.type == 'NORMAL_MAP':
                    item.update(space=n.space, uv_map=n.uv_map)
                if n.type == 'MIX':
                    item.update(blend_type=n.blend_type, data_type=n.data_type)
                nodes.append(item)
        links = [[l.from_node.name, l.from_socket.identifier, l.to_node.name, l.to_socket.identifier]
                 for l in m.node_tree.links] if m.use_nodes else []
        result[m.name] = {'diffuse_color': list(m.diffuse_color), 'backface_culling': m.use_backface_culling,
                          'nodes': nodes, 'links': links}
    return result


def render(objects, directory, modes):
    directory.mkdir(exist_ok=True)
    scene = bpy.context.scene
    for o in list(scene.objects):
        if o.type in {'CAMERA', 'LIGHT'}:
            bpy.data.objects.remove(o, do_unlink=True)
    pts = [o.matrix_world @ v.co for o in objects for v in o.data.vertices]
    lo = Vector([min(p[i] for p in pts) for i in range(3)])
    hi = Vector([max(p[i] for p in pts) for i in range(3)])
    center = (lo+hi)/2; extent = max(hi-lo)
    scene.render.engine = 'BLENDER_EEVEE'
    if hasattr(scene.eevee, 'taa_render_samples'):
        scene.eevee.taa_render_samples = 128
    scene.render.resolution_x = 900; scene.render.resolution_y = 900
    scene.render.resolution_percentage = 100; scene.render.film_transparent = False
    scene.view_settings.view_transform = 'AgX'; scene.view_settings.exposure = 0
    scene.world = bpy.data.worlds.new('__IndependentWorld'); scene.world.use_nodes = True
    bg = scene.world.node_tree.nodes['Background']
    bg.inputs[0].default_value = (.16, .18, .20, 1); bg.inputs[1].default_value = .5
    for direction, power in [((1,-2,3),90), ((-2,-1,1),45), ((0,2,2),65)]:
        data = bpy.data.lights.new('__IndependentArea','AREA')
        data.energy=power*extent*extent; data.size=extent*1.1
        light=bpy.data.objects.new(data.name,data); scene.collection.objects.link(light)
        light.location=center+Vector(direction)*extent
        light.rotation_euler=(center-light.location).to_track_quat('-Z','Y').to_euler()
    cd=bpy.data.cameras.new('__IndependentCamera'); cam=bpy.data.objects.new(cd.name,cd)
    scene.collection.objects.link(cam); scene.camera=cam; cd.type='ORTHO'; cd.ortho_scale=extent*1.25
    for label, direction, focus, span in modes:
        target = center if focus is None else Vector(focus)
        cd.ortho_scale = extent*1.25 if span is None else span
        cam.location=target+Vector(direction).normalized()*extent*4
        cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler()
        scene.render.filepath=str(directory/(label+'.png'))
        bpy.ops.render.render(write_still=True)


def distance(a,b):
    kd=KDTree(len(b))
    for i,p in enumerate(b):
        kd.insert(Vector(p),i)
    kd.balance()
    return max(kd.find(Vector(p))[2] for p in a)


paths=[ROOT/'ArtSource'/folder/(name+extension) for name in MODELS.values()
       for folder,extension in [('Models','.blend'),('Exports','.fbx')]]
paths+=list((ROOT/'ArtSource/Textures/Tree_Common').glob('*.png'))
paths+=[ROOT/'Assets/_Project/Art/Bonsai_Tree.blend',ROOT/'Assets/_Project/Art/Bonsai_Tree.fbx']
hashes={str(p):sha(p) for p in paths}
claimed=json.loads((ROOT/'Evidence/Tree_Materials_P05_20261006/final/apply_p05_report.json').read_text())
review={'models':{},'textures':{},'input_hashes':hashes,'editor_unsaved_state':'NOT_INSPECTED',
        'unity_validation':'NOT_RUN','blender_version':bpy.app.version_string}
for label,name in MODELS.items():
    blend=ROOT/'ArtSource/Models'/(name+'.blend'); fbx=ROOT/'ArtSource/Exports'/(name+'.fbx')
    bpy.ops.wm.open_mainfile(filepath=str(blend),use_scripts=False)
    objects=[o for o in bpy.context.scene.objects if o.type=='MESH']
    saved={o.name:geom(o) for o in objects}
    material_saved=mats()
    baseline=ROOT/'Assets/_Project/Art/Bonsai_Tree.blend' if label=='ancient' else blend.with_suffix('.blend1')
    baseline_checks={}
    if baseline.is_file():
        with bpy.data.libraries.load(str(baseline),link=False) as (df,dt):
            dt.objects=[o.name for o in objects]
        for current,previous in zip(objects,dt.objects):
            if previous is not None:
                old=geom(previous); now=saved[current.name]
                baseline_checks[current.name]={k:old[k]==now[k] for k in
                    ['position_hash','topology_hash','normal_hash','world_matrix']}
                baseline_checks[current.name]['uv_unchanged']=old['uv_hashes']==now['uv_hashes']
                data=previous.data; bpy.data.objects.remove(previous,do_unlink=True)
                if data.users==0: bpy.data.meshes.remove(data)
    views=[('perspective',(1,-1,.45),None,None),('back',(-1,1,.4),None,None)]
    if label in ['large','ancient']:
        focus=(0,0,.85) if label=='large' else (0,0,1.05)
        span=1.15 if label=='large' else 2.0
        views.append(('bark',(1,-2,.25),focus,span))
    render(objects,OUT/label,views)
    if label in ['large','ancient']:
        # Diagnostic toggle only in process memory: demonstrate actual normal influence.
        material=bpy.data.materials['M_Bark']
        normal_socket=next(n for n in material.node_tree.nodes if n.type=='BSDF_PRINCIPLED').inputs['Normal']
        for l in list(normal_socket.links): material.node_tree.links.remove(l)
        render(objects,OUT/label,[('bark_normal_disabled',(1,-2,.25),focus,span)])
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(fbx),use_custom_normals=True)
    imported={o.name:geom(o) for o in bpy.context.scene.objects if o.type=='MESH'}
    material_fbx=mats(); roundtrip={}
    for mesh,a in saved.items():
        b=imported.get(mesh)
        if b:
            roundtrip[mesh]={'max_world_vertex_error':max(distance(a['_positions'],b['_positions']),distance(b['_positions'],a['_positions'])),
                'vertices_match':a['vertices']==b['vertices'],'triangles_match':a['triangles']==b['triangles'],
                'uv_hashes_match':a['uv_hashes']==b['uv_hashes'], 'material_slots_match':a['materials']==b['materials']}
    if label=='large':
        render([o for o in bpy.context.scene.objects if o.type=='MESH'],OUT/(label+'_fbx'),
               [('perspective',(1,-1,.45),None,None)])
    for group in [saved,imported]:
        for item in group.values(): item.pop('_positions')
    cr=claimed['models'][label.capitalize() if label!='ancient' else 'Ancient']
    review['models'][label]={'path':str(blend),'sha_matches_agent_report':sha(blend)==cr['saved_blend_sha'],
        'fbx_sha_matches_agent_report':sha(fbx)==cr['fbx_sha'], 'saved':saved,'materials_saved':material_saved,
        'imported':imported,'materials_fbx':material_fbx,'roundtrip':roundtrip,
        'baseline_path':str(baseline),'baseline_checks':baseline_checks}
    print('AUDITED',label,flush=True)

# Read actual PNG channel values as data, not colors, for normal validity tests.
for p in (ROOT/'ArtSource/Textures/Tree_Common').glob('*.png'):
    im=bpy.data.images.load(str(p),check_existing=False); im.colorspace_settings.name='Non-Color'
    rgba=np.asarray(im.pixels[:],dtype=np.float32).reshape(im.size[1],im.size[0],4)
    rgb=rgba[:,:,:3]
    data={'size':list(im.size),'sha256':sha(p),'rgb_min':rgb.min(axis=(0,1)).tolist(),
          'rgb_max':rgb.max(axis=(0,1)).tolist(),'rgb_mean':rgb.mean(axis=(0,1)).tolist(),
          'pixel_corner_rgb':rgb[0,0,:].tolist(),'unique_alpha':np.unique(rgba[:,:,3]).tolist()}
    if 'Normal' in p.name:
        normals=rgb*2-1; lengths=np.linalg.norm(normals,axis=2)
        data.update(normal_length_min=float(lengths.min()),normal_length_mean=float(lengths.mean()),
                    normal_length_max=float(lengths.max()),normal_z_min=float(normals[:,:,2].min()))
    review['textures'][p.name]=data
    bpy.data.images.remove(im)
review['all_inputs_unchanged']=all(sha(p)==h for p,h in hashes.items())
assert review['all_inputs_unchanged']
(OUT/'audit_report.json').write_text(json.dumps(review,indent=2),encoding='utf-8')
print('INDEPENDENT_REVIEW_COMPLETE',str(OUT/'audit_report.json'))
