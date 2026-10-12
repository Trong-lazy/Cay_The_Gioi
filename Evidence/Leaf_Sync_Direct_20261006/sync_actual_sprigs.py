"""Transfer real reference sprigs, changing ONLY Tree_Large foliage in place.

preview: construct/render in memory only. apply: validate, save existing blend,
export existing FBX, reopen and verify. No duplicate model files are created.
"""
import bpy
import bmesh
import hashlib
import json
import math
import sys
from pathlib import Path
from mathutils import Vector, Matrix

ROOT = Path(__file__).resolve().parents[2]
OUT = Path(__file__).resolve().parent
TARGET = ROOT / 'ArtSource/Models/Tree_Large.blend'
REFERENCE = ROOT / 'ArtSource/Models/Bonsai_Trunk_Candidate_v01.blend'
EXPORT = ROOT / 'ArtSource/Exports/Tree_Large.fbx'
MODE = sys.argv[sys.argv.index('--') + 1]


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def digest(value):
    return hashlib.sha256(json.dumps(value, separators=(',', ':')).encode()).hexdigest()


def fingerprint(obj):
    m = obj.data
    return digest({'positions': [list(v.co) for v in m.vertices],
                   'polygons': [list(p.vertices) for p in m.polygons],
                   'edges': [list(e.vertices) for e in m.edges],
                   'uv': {u.name: [list(d.uv) for d in u.data] for u in m.uv_layers},
                   'normals': [list(n.vector) for n in m.corner_normals],
                   'matrix': [list(r) for r in obj.matrix_world],
                   'materials': [s.material.name if s.material else None for s in obj.material_slots]})


def leaves(mesh):
    """Connected islands plus UV landmarks; average duplicate top/bottom points."""
    adjacency = [[] for _ in mesh.vertices]
    for e in mesh.edges:
        a, b = e.vertices
        adjacency[a].append(b); adjacency[b].append(a)
    seen = set(); islands = []
    for start in range(len(mesh.vertices)):
        if start in seen:
            continue
        todo = [start]; seen.add(start); ids = []
        while todo:
            i = todo.pop(); ids.append(i)
            for n in adjacency[i]:
                if n not in seen:
                    seen.add(n); todo.append(n)
        islands.append(sorted(ids))
    owner = {i: c for c, ids in enumerate(islands) for i in ids}
    face_groups = [[] for _ in islands]
    for p in mesh.polygons:
        face_groups[owner[p.vertices[0]]].append(p)
    result = []
    layer = mesh.uv_layers.active
    assert layer is not None
    for ids, faces in zip(islands, face_groups):
        points = {}
        for p in faces:
            # Side rims collapse to center UV; only top/bottom encode landmarks.
            uvs = [tuple(round(x, 4) for x in layer.data[l].uv) for l in p.loop_indices]
            if len(set(uvs)) < 3:
                continue
            for l, uv in zip(p.loop_indices, uvs):
                points.setdefault(uv, set()).add(mesh.loops[l].vertex_index)
        landmarks = {uv: sum((mesh.vertices[i].co for i in inds), Vector()) / len(inds)
                     for uv, inds in points.items()}
        assert all(k in landmarks for k in [(0.5, 0), (0, 0.5), (1, 0.5), (0.5, 1)])
        result.append({'ids': ids, 'faces': faces, 'points': landmarks})
    return result


def frame(base, tip, left, right):
    y = (tip - base).normalized()
    x = right - left
    x = (x - y * x.dot(y)).normalized()
    z = x.cross(y).normalized()
    return Matrix((x, y, z)).transposed()


def bounds(obj):
    pts = [obj.matrix_world @ v.co for v in obj.data.vertices]
    lo = [min(p[i] for p in pts) for i in range(3)]
    hi = [max(p[i] for p in pts) for i in range(3)]
    return {'min': lo, 'max': hi, 'size': [hi[i] - lo[i] for i in range(3)]}


def render_clay(name, objects, directions, scale=1.0):
    """Temporary isolated scene, copied objects/materials, no persistent changes."""
    scene = bpy.data.scenes.new('__SyncReview_' + name)
    copies = []
    mat = bpy.data.materials.new('__NeutralClay_' + name)
    mat.diffuse_color = (0.42, 0.42, 0.42, 1)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = mat.diffuse_color
    bsdf.inputs['Roughness'].default_value = 0.8
    for source in objects:
        clone = source.copy(); clone.data = source.data.copy()
        clone.data.materials.clear(); clone.data.materials.append(mat)
        for p in clone.data.polygons:
            p.material_index = 0
        scene.collection.objects.link(clone); copies.append(clone)
    pts = [o.matrix_world @ v.co for o in copies for v in o.data.vertices]
    lo = Vector([min(p[i] for p in pts) for i in range(3)])
    hi = Vector([max(p[i] for p in pts) for i in range(3)])
    center = (lo + hi) / 2; extent = max(hi - lo)
    cam_data = bpy.data.cameras.new('__SyncReviewCam')
    cam = bpy.data.objects.new('__SyncReviewCam', cam_data)
    scene.collection.objects.link(cam); scene.camera = cam
    cam_data.type = 'ORTHO'; cam_data.ortho_scale = extent * 1.25 * scale
    scene.render.engine = 'BLENDER_EEVEE'
    if hasattr(scene.eevee, 'taa_render_samples'):
        scene.eevee.taa_render_samples = 128
    scene.render.resolution_x = 900; scene.render.resolution_y = 900
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = 'PNG'
    scene.world = bpy.data.worlds.new('__SyncReviewWorld')
    scene.world.use_nodes = True
    bg = scene.world.node_tree.nodes['Background']
    bg.inputs[0].default_value = (0.14, 0.16, 0.18, 1)
    bg.inputs[1].default_value = 0.6
    for idx, direction in enumerate([(1, -2, 3), (-2, -1, 1), (0, 2, 2)]):
        ld = bpy.data.lights.new('__SyncReviewLight', 'AREA')
        ld.energy = 90 * extent * extent; ld.shape = 'DISK'; ld.size = extent * 1.5
        ob = bpy.data.objects.new(ld.name, ld); scene.collection.objects.link(ob)
        ob.location = center + Vector(direction) * extent
        ob.rotation_euler = (center - ob.location).to_track_quat('-Z', 'Y').to_euler()
    directory = OUT / name; directory.mkdir(exist_ok=True)
    for label, direction in directions:
        cam.location = center + Vector(direction).normalized() * extent * 4
        cam.rotation_euler = (center - cam.location).to_track_quat('-Z', 'Y').to_euler()
        scene.render.filepath = str(directory / (label + '.png'))
        bpy.ops.render.render(write_still=True, scene=scene.name)
    for ob in list(scene.objects):
        data = ob.data
        bpy.data.objects.remove(ob, do_unlink=True)
        if isinstance(data, bpy.types.Mesh):
            bpy.data.meshes.remove(data)
        elif isinstance(data, bpy.types.Camera):
            bpy.data.cameras.remove(data)
        elif isinstance(data, bpy.types.Light):
            bpy.data.lights.remove(data)
    world = scene.world
    bpy.data.scenes.remove(scene); bpy.data.worlds.remove(world)
    bpy.data.materials.remove(mat)


protected_paths = [REFERENCE, ROOT / 'Assets/_Project/Art/Bonsai_Tree.blend',
                   ROOT / 'ArtSource/Models/Tree_Seedling_Simple_v01.blend',
                   ROOT / 'ArtSource/Models/Tree_Young_Simple_v01.blend']
protected_hashes = {str(p): sha(p) for p in protected_paths}
input_hash = sha(TARGET)
if MODE == 'apply':
    approved_preview = json.loads((OUT / 'preview_report.json').read_text(encoding='utf-8'))
    assert input_hash == approved_preview['input_hash'], 'Target changed since visual preview.'
    assert protected_hashes == approved_preview['protected_input_hashes']
bpy.ops.wm.open_mainfile(filepath=str(TARGET), use_scripts=False)
scene = bpy.context.scene
foliage = bpy.data.objects['Foliage']
before_fingerprints = {name: fingerprint(bpy.data.objects[name])
                       for name in ['Roots', 'Trunk_Branches']}
old_mesh = foliage.data
old_bounds = bounds(foliage)
old_leaves = leaves(old_mesh)
assert len(old_leaves) == 385, 'Source changed; audit placements before proceeding.'
material_before = [m for m in old_mesh.materials]
material_state_before = digest([(m.name, list(m.diffuse_color),
    [(n.name, n.type, [(s.name, list(s.default_value) if hasattr(s.default_value, '__len__') else s.default_value)
      for s in n.inputs if hasattr(s, 'default_value')]) for n in m.node_tree.nodes]
    if m.use_nodes else []) for m in material_before])
old_leaf_descriptors = []
for leaf in old_leaves:
    p = leaf['points']; base = p[(0.5, 0)]; tip = p[(0.5, 1)]
    basis = frame(base, tip, p[(0, 0.5)], p[(1, 0.5)])
    old_leaf_descriptors.append((base.copy(), basis, (tip - base).length))

with bpy.data.libraries.load(str(REFERENCE), link=False) as (source, destination):
    destination.objects = ['Foliage']
ref_object = destination.objects[0]
assert ref_object is not None
ref_mesh = ref_object.data
ref_leaves = leaves(ref_mesh)
assert len(ref_leaves) == 4576
assert all(len(l['ids']) == 5 and len(l['faces']) == 4 for l in ref_leaves)

# Extract the ACTUAL four-blade fan (20 vertices) and UVs, without inventing
# dimensions/fold/droop. Adjacent reference islands are the authored sprig.
templates = []
for group in range(8):
    group_leaves = ref_leaves[group * 4:group * 4 + 4]
    bases = [l['points'][(0.5, 0)] for l in group_leaves]
    anchor = sum(bases, Vector()) / 4
    primary = group_leaves[0]['points']
    ref_frame = frame(primary[(0.5, 0)], primary[(0.5, 1)],
                      primary[(0, 0.5)], primary[(1, 0.5)])
    inverse = ref_frame.transposed()
    blade_templates = []
    for l in group_leaves:
        mapping = {idx: i for i, idx in enumerate(l['ids'])}
        coords = [inverse @ (ref_mesh.vertices[i].co - anchor) for i in l['ids']]
        faces = [[mapping[v] for v in f.vertices] for f in l['faces']]
        uvs = [[list(ref_mesh.uv_layers.active.data[j].uv) for j in f.loop_indices]
               for f in l['faces']]
        normals = [[inverse @ ref_mesh.corner_normals[j].vector for j in f.loop_indices]
                   for f in l['faces']]
        blade_templates.append((coords, faces, uvs, normals))
    # Match existing outer extent while making four smaller blades per sprig.
    radius = max(c.length for coords, _, _, _ in blade_templates for c in coords)
    templates.append((blade_templates, radius))

directions = [('front', (0, -1, .14)), ('back', (0, 1, .14)),
              ('left', (-1, 0, .14)), ('right', (1, 0, .14)),
              ('top', (0, 0, 1)), ('perspective', (1, -1, .45)),
              ('back_perspective', (-1, 1, .45)), ('low', (1, -1, -.15))]
tree_objects = [bpy.data.objects[n] for n in ['Roots', 'Trunk_Branches', 'Foliage']]
if MODE == 'preview':
    render_clay('before', tree_objects, directions)
    render_clay('reference_tree', [ref_object], [('perspective', (1, -1, .45))])

vertices = []; faces = []; uvs = []; desired_normals = []; max_shape_error = 0.0
for i, (base, basis, length) in enumerate(old_leaf_descriptors):
    template, radius = templates[i % len(templates)]
    scale = length / radius
    # Tiny deterministic angular variation around the existing anchor; this
    # preserves each pad's distribution and XYZ placement, avoiding rigid rows.
    angle = math.radians(6 * math.sin(i * 2.399963))
    orient = basis @ Matrix.Rotation(angle, 3, 'Y')
    for coords, triangles, face_uvs, face_normals in template:
        center_points = [base + orient @ (c * scale) for c in coords]
        # Minimal closed thickness; mid-surface matches reference exactly.
        normal = (center_points[triangles[0][1]] - center_points[triangles[0][0]]).cross(
            center_points[triangles[0][2]] - center_points[triangles[0][0]]).normalized()
        offset = normal * (0.0015 * scale / 2)
        start = len(vertices)
        vertices.extend([list(c + offset) for c in center_points])
        vertices.extend([list(c - offset) for c in center_points])
        for tri, uv, n in zip(triangles, face_uvs, face_normals):
            faces.append([start + v for v in tri]); uvs.append(uv)
            desired_normals.append([list((orient @ v).normalized()) for v in n])
            faces.append([start + 5 + v for v in reversed(tri)]); uvs.append(list(reversed(uv)))
            desired_normals.append([list(-(orient @ v).normalized()) for v in reversed(n)])
        edge_count = {}
        for tri in triangles:
            for a, b in zip(tri, tri[1:] + tri[:1]):
                key = tuple(sorted((a, b)))
                edge_count.setdefault(key, []).append((a, b))
        for occurrences in edge_count.values():
            if len(occurrences) == 1:
                a, b = occurrences[0]
                faces.append([start + b, start + a, start + 5 + a, start + 5 + b])
                uvs.append([[.5, .5]] * 4); desired_normals.append(None)
        for j, c in enumerate(coords):
            mid = (Vector(vertices[start+j]) + Vector(vertices[start+j+5])) / 2
            recovered = orient.transposed() @ (mid - base) / scale
            max_shape_error = max(max_shape_error, (recovered - c).length)

new_mesh = bpy.data.meshes.new('__Actual_Bonsai_Sprigs')
new_mesh.from_pydata(vertices, [], faces); new_mesh.update()
layer = new_mesh.uv_layers.new(name=old_mesh.uv_layers.active.name)
loop_normals = []
for p, uv, normals in zip(new_mesh.polygons, uvs, desired_normals):
    p.use_smooth = True
    for corner, loop in enumerate(p.loop_indices):
        layer.data[loop].uv = uv[corner]
        loop_normals.append(normals[corner] if normals else list(p.normal))
for material in material_before:
    new_mesh.materials.append(material)
new_mesh.normals_split_custom_set(loop_normals)
foliage.data = new_mesh
bm = bmesh.new(); bm.from_mesh(new_mesh)
checks = {'degenerate_faces': sum(f.calc_area() < 1e-10 for f in bm.faces),
          'loose_vertices': sum(not v.link_edges for v in bm.verts),
          'non_manifold_edges': sum(not e.is_manifold for e in bm.edges)}
bm.free()
assert checks == {'degenerate_faces': 0, 'loose_vertices': 0, 'non_manifold_edges': 0}, checks
assert max_shape_error < 1e-6
assert all(fingerprint(bpy.data.objects[n]) == f for n, f in before_fingerprints.items())
assert [m.name for m in new_mesh.materials] == [m.name for m in material_before]
new_mesh.calc_loop_triangles()
report = {'mode': MODE, 'source_reference': str(REFERENCE), 'target': str(TARGET),
          'input_hash': input_hash, 'reference_hash': protected_hashes[str(REFERENCE)],
          'before_foliage_triangles': 6160, 'after_foliage_triangles': len(new_mesh.loop_triangles),
          'sprig_count': len(old_leaf_descriptors), 'blades_per_sprig': 4,
          'actual_reference_blades': 1540, 'exact_source_template_count': 8,
          'normalized_mid_surface_max_error': max_shape_error, 'geometry_checks': checks,
          'before_bounds': old_bounds, 'after_bounds': bounds(foliage),
          'unchanged_meshes': before_fingerprints,
          'material_slots_unchanged': True, 'protected_input_hashes': protected_hashes}

# Show exact real source shape alongside the transferred scaled shape, under
# identical clay lighting. Recover transferred positions from the actual new
# mesh, rather than rendering the source twice.
if MODE == 'preview':
    render_clay('after', tree_objects, directions)
    for index, label in [(0, 'reference_sprig'), (1, 'transferred_sprig')]:
        # Full four-blade sprig, normalized to identical size for comparison.
        vv = []; ff = []
        first_base, first_basis, first_length = old_leaf_descriptors[0]
        first_scale = first_length / templates[0][1]
        for blade_index, (blade, ts, _, _) in enumerate(templates[0][0]):
            start = len(vv)
            if index == 0:
                vv.extend([list(c) for c in blade])
            else:
                for vertex_index in range(5):
                    mesh_index = blade_index * 10 + vertex_index
                    mid = (new_mesh.vertices[mesh_index].co + new_mesh.vertices[mesh_index + 5].co) / 2
                    vv.append(list(first_basis.transposed() @ (mid - first_base) / first_scale))
            ff.extend([[start + v for v in t] for t in ts])
        mesh = bpy.data.meshes.new('__Sample'); mesh.from_pydata(vv, [], ff)
        obj = bpy.data.objects.new('__Sample', mesh)
        for p in mesh.polygons:
            p.use_smooth = False
        render_clay(label, [obj], [('front', (0, -.5, 1)), ('side', (1, -.1, .3))])
        bpy.data.objects.remove(obj); bpy.data.meshes.remove(mesh)

# Remove appended reference before saving; source never altered or saved.
ref_data = ref_object.data
bpy.data.objects.remove(ref_object, do_unlink=True)
if ref_data.users == 0:
    bpy.data.meshes.remove(ref_data)
if MODE == 'apply':
    assert sha(TARGET) == input_hash, 'Target changed during operation.'
    assert all(sha(p) == h for p, h in protected_hashes.items())
    old_name = old_mesh.name
    if old_mesh.users == 0:
        bpy.data.meshes.remove(old_mesh)
    new_mesh.name = old_name
    foliage_fingerprint = fingerprint(foliage)
    # Respect request to avoid proliferating .blend1 model copies.
    bpy.context.preferences.filepaths.save_version = 0
    bpy.ops.wm.save_as_mainfile(filepath=str(TARGET))
    bpy.ops.object.select_all(action='DESELECT')
    for obj in tree_objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = foliage
    bpy.ops.export_scene.fbx(filepath=str(EXPORT), use_selection=True,
        global_scale=1, apply_unit_scale=True, apply_scale_options='FBX_SCALE_ALL',
        axis_forward='-Z', axis_up='Y', object_types={'MESH'}, use_mesh_modifiers=True,
        mesh_smooth_type='FACE', bake_anim=False)
    bpy.ops.wm.open_mainfile(filepath=str(TARGET), use_scripts=False)
    assert fingerprint(bpy.data.objects['Foliage']) == foliage_fingerprint
    saved_materials = list(bpy.data.objects['Foliage'].data.materials)
    material_state_after = digest([(m.name, list(m.diffuse_color),
        [(n.name, n.type, [(s.name, list(s.default_value) if hasattr(s.default_value, '__len__') else s.default_value)
          for s in n.inputs if hasattr(s, 'default_value')]) for n in m.node_tree.nodes]
        if m.use_nodes else []) for m in saved_materials])
    assert material_state_after == material_state_before
    report['material_nodes_unchanged'] = True
    report['saved_foliage_fingerprint'] = foliage_fingerprint
    assert all(fingerprint(bpy.data.objects[n]) == f for n, f in before_fingerprints.items())
    assert all(sha(p) == h for p, h in protected_hashes.items())
    report['output_hash'] = sha(TARGET); report['export_hash'] = sha(EXPORT)
    report['saved_file_reopened'] = True
else:
    assert sha(TARGET) == input_hash
report['protected_inputs_unchanged'] = all(sha(p) == h for p, h in protected_hashes.items())
(OUT / (MODE + '_report.json')).write_text(json.dumps(report, indent=2), encoding='utf-8')
print('LEAF_SYNC_RESULT', json.dumps(report))
