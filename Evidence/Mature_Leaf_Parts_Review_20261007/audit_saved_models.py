"""Independent read-only saved model audit; writes new evidence only."""
import ast
import json
from pathlib import Path

OUT = Path(__file__).resolve().parent
ROOT = OUT.parents[1]
helper = ROOT / 'Evidence/Material_Independent_Review_20261006/audit_materials.py'
tree = ast.parse(helper.read_text(encoding='utf-8'))
exec(compile(ast.Module(body=[n for n in tree.body if isinstance(n, (ast.Import, ast.ImportFrom, ast.FunctionDef))], type_ignores=[]), str(helper), 'exec'), globals())
paths = list((ROOT / 'ArtSource/Models').glob('*.blend')) + list((ROOT / 'ArtSource/Exports').glob('*.fbx')) + list((ROOT / 'ArtSource/Textures/Tree_Common').glob('*.png'))
before = {str(p): sha(p) for p in paths}
report = {'blender_version': bpy.app.version_string, 'input_hashes': before, 'models': {}}
for key, name in [('seedling', 'Tree_Seedling_Simple_v01'), ('young', 'Tree_Young_Simple_v01'), ('large', 'Tree_Large'), ('ancient', 'Bonsai_Trunk_Candidate_v01'), ('mature', 'WT_MatureTree')]:
    bpy.ops.wm.open_mainfile(filepath=str(ROOT / 'ArtSource/Models' / (name + '.blend')), use_scripts=False)
    objects = [o for o in bpy.context.scene.objects if o.type == 'MESH']
    meshes = {}
    for o in objects:
        data = geom(o)
        data.pop('_positions')
        data.update(parent=o.parent.name if o.parent else None, collections=[c.name for c in o.users_collection], hide_render=o.hide_render, hide_viewport=o.hide_viewport, zero_area_faces=sum(p.area == 0 for p in o.data.polygons))
        meshes[o.name] = data
    report['models'][key] = {'meshes': meshes, 'materials': mats(), 'empties': [{'name': o.name, 'location': list(o.location)} for o in bpy.context.scene.objects if o.type == 'EMPTY']}
    if key in ('large', 'mature'):
        visible = [o for o in objects if not o.hide_render and (key != 'mature' or any(c.name == 'WT_MatureTree_LOD0' for c in o.users_collection))]
        for o in objects:
            o.hide_render = o not in visible
        render(visible, OUT / key, [('perspective', (1,-1,.45), None, None), ('back', (-1,1,.45), None, None)])
    print('AUDITED', key, flush=True)
for lod in range(3):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(ROOT / 'ArtSource/Exports' / ('WT_MatureTree_LOD%d.fbx' % lod)), use_custom_normals=True)
    meshes = {}
    for o in bpy.context.scene.objects:
        if o.type == 'MESH':
            data = geom(o); data.pop('_positions'); meshes[o.name] = data
    report['models']['mature_fbx_lod%d' % lod] = {'meshes': meshes, 'materials': mats()}
report['all_inputs_unchanged'] = all(sha(p) == h for p,h in before.items())
assert report['all_inputs_unchanged']
(OUT / 'audit_report.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
print('REVIEW_COMPLETE', flush=True)
