"""Audit all 4 tree source models for P01:
- Tree_Seedling_Simple_v01.blend
- Tree_Young_Simple_v01.blend
- Tree_Large.blend
- Bonsai_Trunk_Candidate_v01.blend
"""
import bpy
import bmesh
import hashlib
import json
import math
from pathlib import Path

ROOT = Path("c:/Users/Asus/foder game 3D/Cay_The_Gioi")
MODELS = {
    "Seedling": ROOT / "ArtSource/Models/Tree_Seedling_Simple_v01.blend",
    "Young": ROOT / "ArtSource/Models/Tree_Young_Simple_v01.blend",
    "Large": ROOT / "ArtSource/Models/Tree_Large.blend",
    "Ancient": ROOT / "ArtSource/Models/Bonsai_Trunk_Candidate_v01.blend",
}

def sha256_file(filepath):
    return hashlib.sha256(Path(filepath).read_bytes()).hexdigest()

def digest(data):
    return hashlib.sha256(json.dumps(data, sort_keys=True, separators=(',', ':')).encode('utf-8')).hexdigest()

def get_mesh_fingerprint(obj):
    m = obj.data
    # Normalize normals and vectors with fixed precision
    positions = [[round(coord, 6) for coord in v.co] for v in m.vertices]
    polygons = [list(p.vertices) for p in m.polygons]
    edges = [list(e.vertices) for e in m.edges]
    uvs = {u.name: [[round(coord, 6) for coord in d.uv] for d in u.data] for u in m.uv_layers}
    # corner normals
    corner_normals = [[round(n, 6) for n in loop.normal] for loop in m.loops]
    matrix = [[round(val, 6) for val in row] for row in obj.matrix_world]
    materials = [s.material.name if s.material else None for s in obj.material_slots]
    return digest({
        'positions': positions,
        'polygons': polygons,
        'edges': edges,
        'uvs': uvs,
        'corner_normals': corner_normals,
        'matrix': matrix,
        'materials': materials
    })

def audit_file(label, path):
    file_sha = sha256_file(path)
    bpy.ops.wm.open_mainfile(filepath=str(path), use_scripts=False)
    
    scene = bpy.context.scene
    unit_settings = {
        'system': scene.unit_settings.system,
        'scale_length': scene.unit_settings.scale_length,
        'length_unit': scene.unit_settings.length_unit,
    }
    
    objects_info = []
    total_triangles = 0
    total_vertices = 0
    
    for obj in sorted(scene.objects, key=lambda o: o.name):
        if obj.type != 'MESH':
            objects_info.append({
                'name': obj.name,
                'type': obj.type,
                'location': list(obj.location),
            })
            continue
            
        mesh = obj.data
        mesh.calc_loop_triangles()
        tri_count = len(mesh.loop_triangles)
        vert_count = len(mesh.vertices)
        poly_count = len(mesh.polygons)
        edge_count = len(mesh.edges)
        
        total_triangles += tri_count
        total_vertices += vert_count
        
        # Bmesh checks
        bm = bmesh.new()
        bm.from_mesh(mesh)
        degenerate = sum(f.calc_area() < 1e-10 for f in bm.faces)
        loose_verts = sum(not v.link_edges for v in bm.verts)
        boundary_edges = sum(e.is_boundary for e in bm.edges)
        non_manifold_edges = sum(not e.is_manifold for e in bm.edges)
        bm.free()
        
        # Bounding box
        bbox = [obj.matrix_world @ mathutils.Vector(corner) for corner in obj.bound_box]
        min_x = min(v.x for v in bbox)
        max_x = max(v.x for v in bbox)
        min_y = min(v.y for v in bbox)
        max_y = max(v.y for v in bbox)
        min_z = min(v.z for v in bbox)
        max_z = max(v.z for v in bbox)
        
        # UV layers
        uv_info = []
        for uv_layer in mesh.uv_layers:
            uv_data = uv_layer.data
            if len(uv_data) > 0:
                min_u = min(d.uv.x for d in uv_data)
                max_u = max(d.uv.x for d in uv_data)
                min_v = min(d.uv.y for d in uv_data)
                max_v = max(d.uv.y for d in uv_data)
            else:
                min_u = max_u = min_v = max_v = 0.0
            uv_info.append({
                'name': uv_layer.name,
                'active': uv_layer.active,
                'active_render': uv_layer.active_render,
                'u_range': [round(min_u, 4), round(max_u, 4)],
                'v_range': [round(min_v, 4), round(max_v, 4)],
            })
            
        # Material slots
        mat_slots = []
        for slot_idx, slot in enumerate(obj.material_slots):
            mat = slot.material
            mat_data = None
            if mat:
                node_info = []
                principled_info = None
                if mat.use_nodes and mat.node_tree:
                    for node in mat.node_tree.nodes:
                        node_summary = {'name': node.name, 'type': node.type}
                        if node.type == 'BSDF_PRINCIPLED':
                            inputs = {}
                            for inp in node.inputs:
                                if hasattr(inp, 'default_value'):
                                    val = inp.default_value
                                    if hasattr(val, '__len__'):
                                        inputs[inp.name] = [round(x, 4) for x in val]
                                    else:
                                        inputs[inp.name] = round(val, 4) if isinstance(val, float) else val
                            principled_info = inputs
                        node_info.append(node_summary)
                mat_data = {
                    'name': mat.name,
                    'use_nodes': mat.use_nodes,
                    'diffuse_color': [round(x, 4) for x in mat.diffuse_color],
                    'principled': principled_info,
                    'nodes': node_info
                }
            
            face_count = sum(1 for p in mesh.polygons if p.material_index == slot_idx)
            mat_slots.append({
                'slot_index': slot_idx,
                'material': mat_data,
                'assigned_faces': face_count
            })
            
        objects_info.append({
            'name': obj.name,
            'type': obj.type,
            'location': [round(x, 6) for x in obj.location],
            'rotation_euler': [round(math.degrees(x), 4) for x in obj.rotation_euler],
            'scale': [round(x, 6) for x in obj.scale],
            'vertices': vert_count,
            'edges': edge_count,
            'polygons': poly_count,
            'triangles': tri_count,
            'degenerate_faces': degenerate,
            'loose_vertices': loose_verts,
            'boundary_edges': boundary_edges,
            'non_manifold_edges': non_manifold_edges,
            'bounds': {
                'x': [round(min_x, 4), round(max_x, 4)],
                'y': [round(min_y, 4), round(max_y, 4)],
                'z': [round(min_z, 4), round(max_z, 4)],
                'size': [round(max_x - min_x, 4), round(max_y - min_y, 4), round(max_z - min_z, 4)]
            },
            'uv_layers': uv_info,
            'material_slots': mat_slots,
            'fingerprint': get_mesh_fingerprint(obj),
        })
        
    return {
        'label': label,
        'filepath': str(path),
        'sha256': file_sha,
        'unit_settings': unit_settings,
        'total_vertices': total_vertices,
        'total_triangles': total_triangles,
        'objects': objects_info,
    }

if __name__ == '__main__':
    import mathutils
    results = {}
    for label, path in MODELS.items():
        print(f"Auditing {label}: {path}...")
        results[label] = audit_file(label, path)
    
    out_path = ROOT / "Evidence/P01_Audit_Initial.json"
    out_path.parent.mkdir(parents=True, exist_ok=True)
    out_path.write_text(json.dumps(results, indent=2), encoding='utf-8')
    print("AUDIT_COMPLETE")
    print(f"Saved audit report to {out_path}")
