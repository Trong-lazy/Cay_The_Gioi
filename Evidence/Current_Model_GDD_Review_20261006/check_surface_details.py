"""Read-only follow-up: mapping modes, surface normals and tiny leaf faces."""
import bpy
import ast
import json
from pathlib import Path
ROOT = Path(__file__).resolve().parents[2]
OUT = Path(__file__).resolve().parent
result = {'near_zero_leaf_faces': {}, 'large_mapping': {}}
for label, name in [('seedling','Tree_Seedling_Simple_v01'), ('young','Tree_Young_Simple_v01')]:
    bpy.ops.wm.open_mainfile(filepath=str(ROOT/'ArtSource/Models'/(name+'.blend')), use_scripts=False)
    m = bpy.data.objects['Foliage'].data
    areas = [p.area for p in m.polygons if p.area < 1e-12]
    result['near_zero_leaf_faces'][label] = {'count_below_1e_12': len(areas),
        'exactly_zero': sum(a == 0 for a in areas), 'areas': areas}

def mapping():
    return [{'name': n.name, 'vector_type': n.vector_type,
             'location': list(n.inputs['Location'].default_value),
             'rotation': list(n.inputs['Rotation'].default_value),
             'scale': list(n.inputs['Scale'].default_value)}
            for n in bpy.data.materials['M_Bark'].node_tree.nodes if n.type == 'MAPPING']

bpy.ops.wm.open_mainfile(filepath=str(ROOT/'ArtSource/Models/Tree_Large.blend'), use_scripts=False)
result['large_mapping']['source'] = mapping()
obj = bpy.data.objects['Trunk_Branches']
norms = [(obj.matrix_world.to_3x3().inverted().transposed() @ n.vector).normalized()
         for n in obj.data.corner_normals]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(ROOT/'ArtSource/Exports/Tree_Large.fbx'), use_custom_normals=True)
result['large_mapping']['fbx'] = mapping()
obj = bpy.data.objects['Trunk_Branches']
other = [(obj.matrix_world.to_3x3().inverted().transposed() @ n.vector).normalized()
         for n in obj.data.corner_normals]
result['large_world_corner_normals'] = {'counts': [len(norms),len(other)],
    'min_dot': min(a.dot(b) for a,b in zip(norms,other)),
    'max_distance': max((a-b).length for a,b in zip(norms,other))}
# Diagnostic only in memory: isolate Blender FBX importer Mapping mode inversion.
for node in bpy.data.materials['M_Bark'].node_tree.nodes:
    if node.type == 'MAPPING': node.vector_type = 'POINT'
helper = ROOT/'Evidence/Material_Independent_Review_20261006/audit_materials.py'
tree = ast.parse(helper.read_text(encoding='utf-8'))
defs = ast.Module(body=[n for n in tree.body if isinstance(n,(ast.Import,ast.ImportFrom,ast.FunctionDef))],type_ignores=[])
exec(compile(defs,str(helper),'exec'),globals())
render([o for o in bpy.context.scene.objects if o.type == 'MESH'], OUT/'large_fbx_mapping_checked',
       [('perspective',(1,-1,.45),None,None)])
def pixels(path):
    im=bpy.data.images.load(str(path),check_existing=False); im.colorspace_settings.name='Non-Color'
    return np.asarray(im.pixels[:],dtype=np.float32).reshape(im.size[1],im.size[0],4)[:,:,:3]
source=pixels(OUT/'large/perspective.png')
raw=pixels(OUT/'large_fbx/perspective.png')
corrected=pixels(OUT/'large_fbx_mapping_checked/perspective.png')
result['large_mapping_render_difference'] = {
    'raw_rgb_rmse_0_to_1':float(np.sqrt(np.mean((source-raw)**2))),
    'mapping_point_diagnostic_rgb_rmse_0_to_1':float(np.sqrt(np.mean((source-corrected)**2))),
    'asset_saved':False}
(OUT/'surface_details.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
print(json.dumps(result))
