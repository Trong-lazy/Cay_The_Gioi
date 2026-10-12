using System.Collections.Generic;
using System.Linq;
using CayTheGioi.MapEnvironment;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CayTheGioi.Tests
{
    public sealed class EnvironmentReviewTests
    {
        [Test]
        public void Terrain_IsClosedWithOutwardLowerSphereAndNoNeedlesAcrossPath()
        {
            var config = new WorldEnvironmentConfig();
            var mesh = SphericalWorldTerrainBuilder.BuildTerrainMesh(config);
            try
            {
                var vertices = mesh.vertices;
                var edges = new Dictionary<(int, int), int>();
                var winding = new Dictionary<(int, int), int>();
                var triangles = mesh.triangles;
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        int a = triangles[i + j], b = triangles[i + (j + 1) % 3];
                        var key = (Mathf.Min(a, b), Mathf.Max(a, b));
                        edges.TryGetValue(key, out int count); edges[key] = count + 1;
                        winding.TryGetValue(key, out int direction); winding[key] = direction + (a < b ? 1 : -1);
                    }
                    Vector3 v0 = vertices[triangles[i]], v1 = vertices[triangles[i + 1]], v2 = vertices[triangles[i + 2]];
                    Vector3 center = (v0 + v1 + v2) / 3;
                    if (center.y < -config.sphereRadius)
                        Assert.That(Vector3.Dot(Vector3.Cross(v1 - v0, v2 - v0), center + Vector3.up * config.sphereRadius), Is.GreaterThan(0));
                }
                Assert.That(edges.Count(pair => pair.Value != 2), Is.Zero, "Every edge must have exactly two faces, including biome cuts");
                Assert.That(winding.Count(pair => pair.Value != 0), Is.Zero, "Adjacent faces must wind in opposite directions");
                Assert.That(mesh.bounds.min.y, Is.EqualTo(-2 * config.sphereRadius).Within(.001f));
                var path = mesh.GetTriangles(1);
                Assert.That(path.Length, Is.GreaterThan(0));
                foreach (int index in path.Distinct())
                {
                    var point = new Vector2(vertices[index].x, vertices[index].z);
                    float distance = float.MaxValue;
                    Vector2 previous = config.pathStart;
                    for (int segment = 1; segment <= 16; segment++)
                    {
                        float t = segment / 16f;
                        Vector2 next = (1-t)*(1-t)*config.pathStart + 2*(1-t)*t*config.pathMid + t*t*config.pathEnd;
                        Vector2 edge = next - previous;
                        float u = Mathf.Clamp01(Vector2.Dot(point - previous, edge) / edge.sqrMagnitude);
                        distance = Mathf.Min(distance, Vector2.Distance(point, previous + edge*u));
                        previous = next;
                    }
                    Assert.That(distance, Is.LessThanOrEqualTo(config.pathWidth * .5f + .015f), "Path mesh must end at the actual path boundary");
                }
            }
            finally { Object.DestroyImmediate(mesh); }
        }

        [Test]
        public void Lake_RimIsBuriedInActualTerrainAndCenterShowsWater()
        {
            var config = new WorldEnvironmentConfig();
            var terrain = SphericalWorldTerrainBuilder.BuildTerrainMesh(config);
            var water = SphericalWorldTerrainBuilder.BuildLakeWaterMesh(config);
            var go = new GameObject("ReviewTerrain", typeof(MeshCollider));
            try
            {
                var collider = go.GetComponent<MeshCollider>(); collider.sharedMesh = terrain;
                Physics.SyncTransforms();
                var vertices = water.vertices;
                for (int i = 0; i < vertices.Length; i++)
                {
                    var vertex = vertices[i];
                    Assert.That(collider.Raycast(new Ray(vertex + Vector3.up * 2, Vector3.down), out var hit, 4), Is.True);
                    if (i == 0) Assert.That(vertex.y, Is.GreaterThan(hit.point.y + .1f));
                    else Assert.That(vertex.y, Is.LessThanOrEqualTo(hit.point.y + .001f), "Water rim must not hang above the actual terrain mesh");
                }
            }
            finally { Object.DestroyImmediate(go); Object.DestroyImmediate(terrain); Object.DestroyImmediate(water); }
        }

        [Test]
        public void Waterfall_FacesUpAndReachesLakeWithoutSinkingIntoTerrain()
        {
            var config = new WorldEnvironmentConfig();
            var ground = SphericalWorldTerrainBuilder.BuildTerrainMesh(config);
            var water = SphericalWorldTerrainBuilder.BuildLakeWaterMesh(config);
            var waterfall = SphericalWorldTerrainBuilder.BuildWaterfallMesh(config);
            var go = new GameObject("ReviewGround", typeof(MeshCollider));
            var lakeGo = new GameObject("ReviewWater", typeof(MeshCollider));
            try
            {
                var collider = go.GetComponent<MeshCollider>(); collider.sharedMesh = ground;
                var lakeCollider = lakeGo.GetComponent<MeshCollider>(); lakeCollider.sharedMesh = water;
                Physics.SyncTransforms();
                var vertices = waterfall.vertices;
                foreach (var vertex in vertices)
                {
                    Assert.That(collider.Raycast(new Ray(vertex + Vector3.up * 2, Vector3.down), out var hit, 4), Is.True);
                    Assert.That(vertex.y, Is.GreaterThanOrEqualTo(hit.point.y), "Waterfall vertex is buried in terrain");
                }
                var triangles = waterfall.triangles;
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    Vector3 a = vertices[triangles[i]], b = vertices[triangles[i+1]], c = vertices[triangles[i+2]];
                    Assert.That(Vector3.Cross(b-a, c-a).y, Is.GreaterThan(0));
                    var center = (a+b+c)/3;
                    Assert.That(collider.Raycast(new Ray(center + Vector3.up * 2, Vector3.down), out var hit, 4), Is.True);
                    Assert.That(center.y, Is.GreaterThanOrEqualTo(hit.point.y-.001f), "Waterfall face is buried between vertices");
                }
                foreach (var end in vertices.Skip(vertices.Length-2))
                {
                    Assert.That(lakeCollider.Raycast(new Ray(end + Vector3.up, Vector3.down), out var hit, 2), Is.True);
                    Assert.That(end.y-hit.point.y, Is.InRange(0, .005f), "Waterfall mouth must meet the actual lake mesh");
                }
            }
            finally
            {
                Object.DestroyImmediate(go); Object.DestroyImmediate(lakeGo);
                Object.DestroyImmediate(ground); Object.DestroyImmediate(water); Object.DestroyImmediate(waterfall);
            }
        }

        [Test]
        public void RockPebbleAndMushroomCapFacesPointOutward()
        {
            var meshes = new[] { TropicalPropsBuilder.BuildRockMesh(100, Vector3.one), TropicalPropsBuilder.BuildPebbleMesh(), TropicalPropsBuilder.BuildMushroomMesh() };
            try
            {
                foreach (var mesh in meshes)
                {
                    var vertices = mesh.vertices; var normals = mesh.normals;
                    var triangles = mesh == meshes[2] ? mesh.GetTriangles(1) : mesh.triangles;
                    for (int i = 0; i < triangles.Length; i += 3)
                    {
                        int a = triangles[i], b = triangles[i+1], c = triangles[i+2];
                        var face = Vector3.Cross(vertices[b]-vertices[a], vertices[c]-vertices[a]);
                        Assert.That(Vector3.Dot(face, normals[a]+normals[b]+normals[c]), Is.GreaterThan(0), mesh.name + " face winding contradicts outward normals");
                    }
                    if (mesh != meshes[2])
                    {
                        float volume = 0;
                        for (int i = 0; i < triangles.Length; i += 3) volume += Vector3.Dot(vertices[triangles[i]], Vector3.Cross(vertices[triangles[i+1]], vertices[triangles[i+2]])) / 6;
                        Assert.That(volume, Is.GreaterThan(0), mesh.name + " is turned inside out");
                    }
                }
            }
            finally { foreach (var mesh in meshes) Object.DestroyImmediate(mesh); }
        }

        [Test]
        public void SavedPropsKeepTheirOwnMeshesAndFirefliesHaveTransparentCorners()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Art/Environment/Prefabs/WorldEnvironment.prefab");
            var filters = prefab.GetComponentsInChildren<MeshFilter>();
            foreach (var group in new[] { ("GrassTuft", 12), ("Pebble", 10) })
            {
                var paths = filters.Where(filter => filter.name == group.Item1).Select(filter => AssetDatabase.GetAssetPath(filter.sharedMesh)).ToArray();
                Assert.That(paths.Length, Is.EqualTo(group.Item2));
                Assert.That(paths.Distinct().Count(), Is.EqualTo(group.Item2), "Variable-size instances must not overwrite one mesh asset");
            }
            Assert.That(filters.Count(filter => filter.name == "MiniPalm"), Is.EqualTo(8));
            var renderer = prefab.GetComponentInChildren<ParticleSystemRenderer>();
            var texture = renderer.sharedMaterial.mainTexture as Texture2D;
            Assert.That(texture, Is.Not.Null);
            Assert.That(texture.GetPixel(0, 0).a, Is.Zero);
            Assert.That(texture.GetPixel(16, 16).a, Is.GreaterThan(.9f));
            Assert.That(renderer.sharedMaterial.renderQueue, Is.EqualTo(3000));
        }
    }
}
