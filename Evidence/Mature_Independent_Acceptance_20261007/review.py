"""Read-only independent acceptance. No blend saves, exports or asset changes."""
import ast
import json
import sys
from pathlib import Path
from mathutils.bvhtree import BVHTree

OUT = Path(__file__).resolve().parent
ROOT = OUT.parents[1]
helper = ROOT / 'Evidence/Material_Independent_Review_20261006/audit_materials.py'
tree = ast.parse(helper.read_text(encoding='utf-8'))
exec(compile(ast.Module(body=[n for n in tree.body if isinstance(n, (ast.Import, ast.ImportFrom, ast.FunctionDef))], type_ignores=[]), str(helper), 'exec'), globals())
if '--audit-only' in sys.argv:
    def render(objects,directory,modes):
        pass
paths = list((ROOT/'ArtSource/Models').glob('*.blend')) + list((ROOT/'ArtSource/Exports').glob('*.fbx')) + list((ROOT/'ArtSource/Textures/Tree_Common').glob('*.png')) + list((ROOT/'Assets/_Project/Art').glob('Bonsai_Tree.*'))
before = {str(p):sha(p) for p in paths if p.is_file()}
previous = json.loads((ROOT/'Evidence/Mature_Leaf_Parts_Review_20261007/audit_report.json').read_text())
unchanged = {p:sha(p)==h for p,h in previous['input_hashes'].items() if 'WT_MatureTree' not in p}
backup = ROOT/'Evidence/Mature_Leaf_Parts_Fix_20261007/backup_20261007/WT_MatureTree.blend'
report = {'blender_version':bpy.app.version_string, 'input_hashes':before, 'protected_inputs_match_previous_review':unchanged, 'backup_matches_previous_review':sha(backup)==previous['input_hashes'][str(ROOT/'ArtSource/Models/WT_MatureTree.blend')], 'lods':{}}

def objects_for_lod(lod):
    suffix = '' if lod==0 else '_LOD%d'%lod
    return [bpy.data.objects['WT_MatureTree_'+part+suffix] for part in ('Roots','Trunk_Branches','Foliage')]

def soup(objects):
    vertices=[]; triangles=[]
    for o in objects:
        o.data.calc_loop_triangles(); offset=len(vertices)
        vertices.extend(o.matrix_world@v.co for v in o.data.vertices)
        triangles.extend(tuple(offset+i for i in t.vertices) for t in o.data.loop_triangles)
    return vertices,triangles

def summary(o):
    result=geom(o); result.pop('_positions')
    result.update(zero_area_faces=sum(p.area==0 for p in o.data.polygons),location=list(o.location),rotation=list(o.rotation_euler),scale=list(o.scale))
    return result

def midpoints(o,start):
    v=o.data.vertices
    return np.array([list((v[start+b*10+i].co+v[start+b*10+i+5].co)*.5) for b in range(4) for i in range(5)])

def fit(a,b):
    ac=a-a.mean(0); bc=b-b.mean(0)
    u,s,vt=np.linalg.svd(ac.T@bc); d=np.eye(3); d[2,2]=np.linalg.det(u@vt)
    rotation=u@d@vt; scale=np.sum((ac@rotation)*bc)/np.sum(ac*ac)
    return float(np.max(np.linalg.norm((ac@rotation)*scale-bc,axis=1))),float(scale)

views=[('front',(0,-1,.08),None,None),('back',(0,1,.08),None,None),('left',(-1,0,.08),None,None),('right',(1,0,.08),None,None),('perspective',(1,-1,.45),None,None),('back_perspective',(-1,1,.45),None,None),('top',(.01,-.01,1),None,None),('underside',(1,-2,-.2),None,None)]
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'ArtSource/Models/Tree_Large.blend'),use_scripts=False)
ref=bpy.data.objects['Foliage']; templates=[midpoints(ref,i*40) for i in range(8)]
report['reference_material']=mats()['M_Foliage']
render([o for o in bpy.context.scene.objects if o.type=='MESH'],OUT/'large',[views[4]])
bpy.ops.wm.open_mainfile(filepath=str(backup),use_scripts=False)
old_wood={}
for lod in range(3):
    suffix='' if lod==0 else '_LOD%d'%lod
    old_wood[lod]=soup([bpy.data.objects['WT_MatureTree_'+n+suffix] for n in ('TrunkAndRoots','Branches')])
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'ArtSource/Models/WT_MatureTree.blend'),use_scripts=False)
for collection in bpy.data.collections:
    collection.hide_render=False
    collection.hide_viewport=False
report['source_materials']=mats()
report['empty_anchors']=[{'name':o.name,'position':list(o.location)} for o in bpy.context.scene.objects if o.type=='EMPTY']
saved={}
for lod in range(3):
    objects=objects_for_lod(lod); saved[lod]={o.name:geom(o) for o in objects}
    new_vertices,new_triangles=soup(objects[:2]); old_vertices,old_triangles=old_wood[lod]
    old_bvh=BVHTree.FromPolygons(old_vertices,old_triangles,all_triangles=True)
    new_bvh=BVHTree.FromPolygons(new_vertices,new_triangles,all_triangles=True)
    wood={'new_to_old_surface_max_m':max(old_bvh.find_nearest(p)[3] for p in new_vertices),'old_to_new_surface_max_m':max(new_bvh.find_nearest(p)[3] for p in old_vertices)}
    foliage=objects[2]; n=len(foliage.data.vertices)//40; fits=[]
    for i in range(n):
        measured=midpoints(foliage,i*40)
        fits.append(min((fit(t,measured)[0]/max(fit(t,measured)[1],1e-9),j) for j,t in enumerate(templates)))
    report['lods'][str(lod)]={'source':{o.name:summary(o) for o in objects},'wood_surface':wood,'leaf_sprigs':n,'best_source_template_fit_max_normalized_m':max(f[0] for f in fits),'source_template_fit_bad_count_1e_5':sum(f[0]>1e-5 for f in fits)}
    for o in bpy.context.scene.objects:
        if o.type=='MESH':o.hide_render=o not in objects
    render(objects,OUT/('source_lod%d'%lod),views if lod==0 else [views[4],views[5],views[7]])
    if lod==0:
        for label,selection in [('roots',objects[:1]),('trunk',objects[1:2]),('wood',objects[:2])]:
            for o in objects:o.hide_render=o not in selection
            # Keep whole-tree framing in isolates and focus at collar for continuity.
            render(objects,OUT/label,[views[4],('collar',(1,-2,.25),(0,0,.475),2.5)])
for lod in range(3):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(ROOT/'ArtSource/Exports'/('WT_MatureTree_LOD%d.fbx'%lod)),use_custom_normals=True)
    objects=[o for o in bpy.context.scene.objects if o.type=='MESH']; imported={o.name:geom(o) for o in objects}; checks={}
    for name,src in saved[lod].items():
        dst=imported[name]
        checks[name]={'tris_match':src['triangles']==dst['triangles'],'verts_match':src['vertices']==dst['vertices'],'uv_match':src['uv_hashes']==dst['uv_hashes'],'slots_match':src['materials']==dst['materials'],'max_world_vertex_error_m':max(distance(src['_positions'],dst['_positions']),distance(dst['_positions'],src['_positions']))}
    report['lods'][str(lod)].update(fbx={o.name:summary(o) for o in objects},roundtrip=checks,fbx_materials=mats(),fbx_object_types=[o.type for o in bpy.context.scene.objects])
    render(objects,OUT/('fbx_lod%d'%lod),[views[4],views[5],views[7]])
report['all_review_inputs_unchanged']=all(sha(p)==h for p,h in before.items())
assert report['all_review_inputs_unchanged']
(OUT/'report.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print('INDEPENDENT_ACCEPTANCE_COMPLETE',flush=True)
