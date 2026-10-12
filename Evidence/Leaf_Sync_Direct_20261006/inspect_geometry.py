import bpy
import json
import sys
from collections import Counter
from mathutils import Vector

paths = sys.argv[sys.argv.index('--') + 1:]
for path in paths:
    bpy.ops.wm.open_mainfile(filepath=path, use_scripts=False)
    mesh = bpy.data.objects['Foliage'].data
    uv = mesh.uv_layers.active
    adjacency = [[] for _ in mesh.vertices]
    for e in mesh.edges:
        a, b = e.vertices
        adjacency[a].append(b)
        adjacency[b].append(a)
    seen = set()
    comps = []
    for start in range(len(adjacency)):
        if start in seen:
            continue
        todo = [start]; seen.add(start); comp = []
        while todo:
            v = todo.pop(); comp.append(v)
            for n in adjacency[v]:
                if n not in seen:
                    seen.add(n); todo.append(n)
        comps.append(sorted(comp))
    samples = []
    for comp in comps[:8]:
        mapping = {}
        for p in mesh.polygons:
            if p.vertices[0] not in comp:
                continue
            for loop in p.loop_indices:
                k = tuple(round(x, 2) for x in uv.data[loop].uv)
                mapping.setdefault(k, set()).add(mesh.loops[loop].vertex_index)
        points = {str(k): [list(mesh.vertices[i].co) for i in sorted(ids)]
                  for k, ids in mapping.items() if k != (0.5, 0.5)}
        samples.append({'indices': comp, 'points_by_uv': points})
    print(json.dumps({'file': path, 'components': len(comps),
                      'component_sizes': dict(Counter(map(len, comps))), 'samples': samples}))
