"""Read-only model/material/FBX review using existing reviewed audit helpers.
Never saves blend files or exports assets. New evidence only.
"""
import ast
import json
from pathlib import Path

OUT = Path(__file__).resolve().parent
ROOT = OUT.parents[1]
helper = ROOT / 'Evidence/Material_Independent_Review_20261006/audit_materials.py'
# Load only imports and helper definitions; do not execute the prior audit or overwrite it.
tree = ast.parse(helper.read_text(encoding='utf-8'))
defs = ast.Module(body=[n for n in tree.body if isinstance(n, (ast.Import, ast.ImportFrom, ast.FunctionDef))], type_ignores=[])
exec(compile(defs, str(helper), 'exec'), globals())
MODELS = {'seedling': 'Tree_Seedling_Simple_v01', 'young': 'Tree_Young_Simple_v01',
          'large': 'Tree_Large', 'ancient': 'Bonsai_Trunk_Candidate_v01'}
prior = json.loads((helper.parent / 'audit_report.json').read_text(encoding='utf-8'))
claimed = json.loads((ROOT / 'Evidence/Material_Fixes_Antigravity_20261006/verification_after_fixes.json').read_text(encoding='utf-8'))
paths = [ROOT / 'ArtSource' / folder / (name + ext) for name in MODELS.values()
         for folder, ext in [('Models', '.blend'), ('Exports', '.fbx')]]
paths += list((ROOT / 'ArtSource/Textures/Tree_Common').glob('*.png'))
paths += [ROOT / 'Assets/_Project/Art/Bonsai_Tree.blend', ROOT / 'Assets/_Project/Art/Bonsai_Tree.fbx']
hashes = {str(p): sha(p) for p in paths}
report = {'blender_version': bpy.app.version_string, 'input_hashes': hashes,
          'saved_files_only': True, 'asset_writes': False, 'models': {}, 'textures': {}}

def structure(o):
    m = o.data
    counts = [0] * len(m.edges)
    edge_lookup = {tuple(sorted(e.vertices)): e.index for e in m.edges}
    for polygon in m.polygons:
        for key in polygon.edge_keys:
            counts[edge_lookup[tuple(sorted(key))]] += 1
    adjacency = [[] for _ in m.vertices]
    for edge in m.edges:
        a, b = edge.vertices
        adjacency[a].append(b); adjacency[b].append(a)
    unseen = set(range(len(m.vertices)))
    sizes = []
    while unseen:
        seed = unseen.pop(); stack = [seed]; count = 0
        while stack:
            v = stack.pop(); count += 1
            for neighbour in adjacency[v]:
                if neighbour in unseen:
                    unseen.remove(neighbour); stack.append(neighbour)
        sizes.append(count)
    evaluated = o.evaluated_get(bpy.context.evaluated_depsgraph_get())
    em = evaluated.to_mesh(); em.calc_loop_triangles()
    evaluated_triangles = len(em.loop_triangles); evaluated.to_mesh_clear()
    return {'boundary_edges': sum(c == 1 for c in counts),
            'loose_edges': sum(c == 0 for c in counts),
            'over_two_face_edges': sum(c > 2 for c in counts),
            'zero_area_faces': sum(p.area < 1e-12 for p in m.polygons),
            'connected_components': len(sizes), 'smallest_components': sorted(sizes)[:10],
            'evaluated_triangles': evaluated_triangles,
            'modifiers_detail': [{'name': mod.name, 'type': mod.type, 'show_viewport': mod.show_viewport,
                                  'show_render': mod.show_render,
                                  'levels': getattr(mod, 'levels', None),
                                  'render_levels': getattr(mod, 'render_levels', None)} for mod in o.modifiers]}

for label, name in MODELS.items():
    blend = ROOT / 'ArtSource/Models' / (name + '.blend')
    fbx = ROOT / 'ArtSource/Exports' / (name + '.fbx')
    bpy.ops.wm.open_mainfile(filepath=str(blend), use_scripts=False)
    objects = [o for o in bpy.context.scene.objects if o.type == 'MESH']
    saved = {o.name: geom(o) for o in objects}
    diagnostics = {o.name: structure(o) for o in objects}
    before = prior['models'][label]['saved']
    comparisons = {n: {k: now[k] == before[n][k] for k in
                       ['position_hash', 'topology_hash', 'normal_hash', 'world_matrix', 'uv_hashes', 'modifiers']}
                   for n, now in saved.items() if n in before}
    source_materials = mats()
    views = [('perspective', (1,-1,.45), None, None), ('back', (-1,1,.4), None, None)]
    if label == 'ancient':
        views += [('bark', (1,-2,.25), (0,0,1.05), 2.0),
                  ('roots', (1,-2,.35), (0,0,.15), 2.1)]
    if label == 'large':
        views += [('bark', (1,-2,.25), (0,0,.85), 1.15)]
    render(objects, OUT / label, views)
    if label == 'ancient':
        bark = bpy.data.materials['M_Bark']
        socket = next(n for n in bark.node_tree.nodes if n.type == 'BSDF_PRINCIPLED').inputs['Normal']
        for link in list(socket.links): bark.node_tree.links.remove(link)
        render(objects, OUT / label, [('bark_normal_disabled', (1,-2,.25), (0,0,1.05), 2.0)])
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(fbx), use_custom_normals=True)
    imported_objects = [o for o in bpy.context.scene.objects if o.type == 'MESH']
    imported = {o.name: geom(o) for o in imported_objects}
    imported_materials = mats()
    roundtrip = {}
    for mesh, a in saved.items():
        b = imported.get(mesh)
        if b:
            roundtrip[mesh] = {'max_world_vertex_error': max(distance(a['_positions'], b['_positions']), distance(b['_positions'], a['_positions'])),
                              'vertices_match': a['vertices'] == b['vertices'],
                              'triangles_match': a['triangles'] == b['triangles'],
                              'uv_hashes_match': a['uv_hashes'] == b['uv_hashes'],
                              'material_slots_match': a['materials'] == b['materials']}
    render(imported_objects, OUT / (label + '_fbx'), [('perspective', (1,-1,.45), None, None)])
    for group in [saved, imported]:
        for item in group.values(): item.pop('_positions')
    agent = claimed['models_after'][label]
    report['models'][label] = {'saved': saved, 'materials_saved': source_materials,
                             'diagnostics': diagnostics, 'changes_vs_independent_review': comparisons,
                             'imported': imported, 'materials_fbx': imported_materials, 'roundtrip': roundtrip,
                             'source_sha_matches_agent': sha(blend) == agent['blend_sha256'],
                             'fbx_sha_matches_agent': sha(fbx) == agent['fbx_sha256'],
                             'agent_total_triangles': agent['total_triangles'],
                             'actual_total_triangles': sum(x['triangles'] for x in saved.values())}
    print('AUDITED_CURRENT', label, flush=True)

for path in (ROOT / 'ArtSource/Textures/Tree_Common').glob('*.png'):
    im = bpy.data.images.load(str(path), check_existing=False)
    im.colorspace_settings.name = 'Non-Color'
    rgba = np.asarray(im.pixels[:], dtype=np.float32).reshape(im.size[1], im.size[0], 4)
    data = {'size': list(im.size), 'sha256': sha(path), 'rgb_mean': rgba[:,:,:3].mean(axis=(0,1)).tolist(),
            'corner_rgb': rgba[0,0,:3].tolist(), 'alpha_min': float(rgba[:,:,3].min()), 'alpha_max': float(rgba[:,:,3].max())}
    if 'Normal' in path.name:
        ns = rgba[:,:,:3]*2-1; lengths = np.linalg.norm(ns, axis=2)
        data.update(normal_length_min=float(lengths.min()), normal_length_max=float(lengths.max()),
                    normal_z_min=float(ns[:,:,2].min()))
    report['textures'][path.name] = data
    bpy.data.images.remove(im)
report['all_inputs_unchanged'] = all(sha(path) == h for path, h in hashes.items())
assert report['all_inputs_unchanged'], 'Input assets changed during review; do not claim consistent snapshot.'
(OUT / 'audit_report.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
print('CURRENT_MODEL_REVIEW_COMPLETE', flush=True)
