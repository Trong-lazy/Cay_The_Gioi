using System.Collections.Generic;
using UnityEngine;

namespace CayTheGioi.MapEnvironment
{
    public static class SphericalWorldTerrainBuilder
    {
        public static Mesh BuildTerrainMesh(WorldEnvironmentConfig config)
        {
            var mesh = new Mesh { name = "Spherical_Planet_Terrain_Mesh" };

            int rings = Mathf.Clamp(config.gridResolution, 32, 64);
            int segments = Mathf.Clamp(config.gridResolution * 2, 64, 128);

            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();

            // Vertex 0: Đỉnh tại tâm (0, 0, 0)
            vertices.Add(Vector3.zero);
            uvs.Add(new Vector2(0.5f, 0.5f));

            float maxPhi = 82f * Mathf.Deg2Rad; // Góc cực phủ sâu xuống gần xích đạo quả cầu
            float R = config.sphereRadius;

            // Các vòng phân bố theo góc cực phi
            for (int r = 1; r <= rings; r++)
            {
                float t = (float)r / rings;
                // Phân bố phi tuyến để vùng quanh cây và hồ có mật độ lưới cao
                float phi = maxPhi * Mathf.Pow(t, 1.15f);

                float currentRadius = R * Mathf.Sin(phi);
                float baseSphereY = -R + R * Mathf.Cos(phi);

                for (int s = 0; s < segments; s++)
                {
                    float theta = (float)s / segments * Mathf.PI * 2f;
                    float x = currentRadius * Mathf.Cos(theta);
                    float z = currentRadius * Mathf.Sin(theta);

                    float y = CalculateHeight(x, z, config, baseSphereY);

                    vertices.Add(new Vector3(x, y, z));

                    // UV trải phẳng có tỷ lệ lặp lại đẹp
                    float u = 0.5f + (x / (R * 2f)) * 10f;
                    float v = 0.5f + (z / (R * 2f)) * 10f;
                    uvs.Add(new Vector2(u, v));
                }
            }

            // 4 danh sách tam giác tương ứng 4 submesh (với CLOCKWISE winding nhìn từ ngoài):
            // 0: Thảm cỏ nhiệt đới (Tropical Grass)
            // 1: Lối mòn đất nâu ấm (Warm Dirt Path)
            // 2: Bờ cát vàng sáng (Sandy Shore)
            // 3: Đáy hồ / đá ngầm (Lake Bed)
            var grassTriangles = new List<int>();
            var pathTriangles = new List<int>();
            var sandTriangles = new List<int>();
            var lakeBedTriangles = new List<int>();
            var cuts = new Dictionary<(int, int, int), int>();

            // Vòng đầu tiên: nối từ tâm (index 0) tới vòng 1 (clockwise)
            for (int s = 0; s < segments; s++)
            {
                int nextS = (s + 1) % segments;
                int i0 = 0;
                int i1 = 1 + s;
                int i2 = 1 + nextS;

                // Clockwise: 0 -> nextS -> s
                AddTriangleToSubmesh(i0, i2, i1, vertices, config, grassTriangles, pathTriangles, sandTriangles, lakeBedTriangles, cuts);
            }

            // Các vòng kế tiếp: quad chia 2 tam giác (clockwise)
            for (int r = 1; r < rings; r++)
            {
                int innerStart = 1 + (r - 1) * segments;
                int outerStart = 1 + r * segments;

                for (int s = 0; s < segments; s++)
                {
                    int nextS = (s + 1) % segments;

                    int innerA = innerStart + s;
                    int innerB = innerStart + nextS;
                    int outerA = outerStart + s;
                    int outerB = outerStart + nextS;

                    // Clockwise: innerA -> innerB -> outerA và innerB -> outerB -> outerA
                    AddTriangleToSubmesh(innerA, innerB, outerA, vertices, config, grassTriangles, pathTriangles, sandTriangles, lakeBedTriangles, cuts);
                    AddTriangleToSubmesh(innerB, outerB, outerA, vertices, config, grassTriangles, pathTriangles, sandTriangles, lakeBedTriangles, cuts);
                }
            }

            // Continue the existing upper terrain into a complete sphere, with outward winding.
            int lastRingStart = 1 + (rings - 1) * segments;
            const int lowerRings = 24;
            for (int ring = 1; ring <= lowerRings; ring++)
            {
                float phi = Mathf.Lerp(maxPhi, Mathf.PI, (float)ring / (lowerRings + 1));
                int nextRingStart = vertices.Count;
                for (int s = 0; s < segments; s++)
                {
                    float theta = (float)s / segments * Mathf.PI * 2;
                    vertices.Add(new Vector3(R * Mathf.Sin(phi) * Mathf.Cos(theta), -R + R * Mathf.Cos(phi), R * Mathf.Sin(phi) * Mathf.Sin(theta)));
                }
                for (int s = 0; s < segments; s++)
                {
                    int next = (s + 1) % segments;
                    grassTriangles.AddRange(new[] { lastRingStart + s, lastRingStart + next, nextRingStart + s,
                        lastRingStart + next, nextRingStart + next, nextRingStart + s });
                }
                lastRingStart = nextRingStart;
            }
            int bottomCenterIdx = vertices.Count;
            vertices.Add(new Vector3(0, -2 * R, 0));

            for (int s = 0; s < segments; s++)
            {
                int nextS = (s + 1) % segments;
                int iA = lastRingStart + s;
                int iB = lastRingStart + nextS;

                // Tam giác đáy khép kín khối cầu nhìn từ ngoài: iA -> bottomCenterIdx -> iB
                grassTriangles.Add(iA);
                grassTriangles.Add(iB);
                grassTriangles.Add(bottomCenterIdx);
            }

            // Boundary clipping added vertices; build UVs and vertex colors for natural biome tinting
            uvs.Clear();
            var colors = new List<Color>(vertices.Count);
            foreach (var vertex in vertices)
            {
                uvs.Add(new Vector2(.5f + vertex.x / (R * 2) * 10, .5f + vertex.z / (R * 2) * 10));

                var p = new Vector2(vertex.x, vertex.z);
                float distLake = Vector2.Distance(p, config.lakeCenter);
                float distPath = DistanceToPath(p, config);

                Color c;
                if (distLake < config.lakeRadius * 1.35f)
                {
                    float t = Mathf.Clamp01(distLake / (config.lakeRadius * 1.35f));
                    c = Color.Lerp(new Color(0.82f, 0.95f, 0.98f, 1.0f), new Color(1.0f, 0.95f, 0.86f, 1.0f), t);
                }
                else if (distPath < config.pathWidth * 0.95f)
                {
                    float t = Mathf.Clamp01(distPath / (config.pathWidth * 0.95f));
                    c = Color.Lerp(new Color(1.0f, 0.90f, 0.82f, 1.0f), new Color(0.92f, 1.0f, 0.88f, 1.0f), t);
                }
                else
                {
                    float noise = Mathf.PerlinNoise(vertex.x * 0.45f + 12.3f, vertex.z * 0.45f + 45.6f);
                    c = Color.Lerp(new Color(0.88f, 1.0f, 0.84f, 1.0f), new Color(0.98f, 1.0f, 0.92f, 1.0f), noise);
                }
                colors.Add(c);
            }

            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetColors(colors);
            mesh.subMeshCount = 4;
            mesh.SetTriangles(grassTriangles, 0);
            mesh.SetTriangles(pathTriangles, 1);
            mesh.SetTriangles(sandTriangles, 2);
            mesh.SetTriangles(lakeBedTriangles, 3);

            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();

            return mesh;
        }

        public static float CalculateHeight(float x, float z, WorldEnvironmentConfig config, float baseSphereY = float.NaN)
        {
            float R = config.sphereRadius;
            float ySphere;

            if (float.IsNaN(baseSphereY))
            {
                float distSq = x * x + z * z;
                if (distSq < R * R)
                    ySphere = -R + Mathf.Sqrt(R * R - distSq);
                else
                    ySphere = -R;
            }
            else
            {
                ySphere = baseSphereY;
            }

            // Lòng hồ trũng bám theo độ cong tự nhiên của mặt cầu
            float dLake = Vector2.Distance(new Vector2(x, z), config.lakeCenter);
            if (dLake < config.lakeRadius)
            {
                float t = dLake / config.lakeRadius;
                // Hàm trũng cosine mềm mại: sâu nhất ở tâm và tiếp xúc trơn tru với mặt cầu ở mép
                float falloff = Mathf.Cos(t * Mathf.PI * 0.5f);
                ySphere -= config.lakeDepth * falloff * falloff;
            }

            // Vách đá cạnh thác
            float dCliff = Vector2.Distance(new Vector2(x, z), config.waterfallOrigin);
            if (dCliff < 1.35f)
            {
                float t = dCliff / 1.35f;
                float bump = (1f - t * t) * 0.28f;
                ySphere += bump;
            }

            return ySphere;
        }

        public static float CalculateBaseSphereY(float x, float z, WorldEnvironmentConfig config)
        {
            float R = config.sphereRadius;
            float distSq = x * x + z * z;
            if (distSq < R * R)
                return -R + Mathf.Sqrt(R * R - distSq);
            return -R;
        }

        private static void AddTriangleToSubmesh(
            int i0, int i1, int i2, List<Vector3> vertices, WorldEnvironmentConfig config,
            List<int> grassTris, List<int> pathTris, List<int> sandTris, List<int> lakeTris,
            Dictionary<(int, int, int), int> cuts)
        {
            var polygons = new List<(List<int> Points, int Mask)> { (new List<int> { i0, i1, i2 }, 0) };
            // Split BOTH sides at every boundary. Skipping an already classified side
            // would create T-junctions where a later boundary intersects their shared edge.
            for (int boundary = 0; boundary < 3; boundary++)
            {
                var split = new List<(List<int> Points, int Mask)>();
                foreach (var polygon in polygons)
                {
                    var inside = ClipPolygon(polygon.Points, boundary, true, vertices, config, cuts);
                    var outside = ClipPolygon(polygon.Points, boundary, false, vertices, config, cuts);
                    if (inside.Count >= 3) split.Add((inside, polygon.Mask | (1 << boundary)));
                    if (outside.Count >= 3) split.Add((outside, polygon.Mask));
                }
                polygons = split;
            }
            foreach (var polygon in polygons)
            {
                // Preserve which side of each cut this piece belongs to. A centroid
                // can lie inside a curved region even when this is its outside piece.
                var target = (polygon.Mask & 1) != 0 ? lakeTris
                    : (polygon.Mask & 2) != 0 ? sandTris
                    : (polygon.Mask & 4) != 0 ? pathTris : grassTris;
                var points = polygon.Points;
                for (int i = 1; i + 1 < points.Count; i++)
                {
                    if (points[0] == points[i] || points[i] == points[i + 1] || points[i + 1] == points[0]) continue;
                    target.Add(points[0]); target.Add(points[i]); target.Add(points[i + 1]);
                }
            }
        }

        private static List<int> ClipPolygon(List<int> polygon, int boundary, bool inside,
            List<Vector3> vertices, WorldEnvironmentConfig config, Dictionary<(int, int, int), int> cuts)
        {
            var result = new List<int>();
            int previous = polygon[polygon.Count - 1];
            float previousDistance = BiomeBoundary(vertices[previous], boundary, config);
            foreach (int current in polygon)
            {
                float distance = BiomeBoundary(vertices[current], boundary, config);
                bool currentIn = inside ? distance <= 0 : distance >= 0;
                bool previousIn = inside ? previousDistance <= 0 : previousDistance >= 0;
                if (currentIn != previousIn)
                {
                    int cut;
                    if (Mathf.Abs(previousDistance) < 1e-7f) cut = previous;
                    else if (Mathf.Abs(distance) < 1e-7f) cut = current;
                    else
                    {
                        var key = (Mathf.Min(previous, current), Mathf.Max(previous, current), boundary);
                        if (!cuts.TryGetValue(key, out cut))
                        {
                            cut = vertices.Count;
                            // Distance fields are nonlinear at bends/end caps. Locate the real
                            // crossing instead of extrapolating from endpoint distances.
                            float low = 0, high = 1;
                            bool lowInside = previousDistance < 0;
                            for (int iteration = 0; iteration < 24; iteration++)
                            {
                                float mid = (low + high) * .5f;
                                float value = BiomeBoundary(Vector3.Lerp(vertices[previous], vertices[current], mid), boundary, config);
                                if ((value < 0) == lowInside) low = mid; else high = mid;
                            }
                            vertices.Add(Vector3.Lerp(vertices[previous], vertices[current], (low + high) * .5f));
                            cuts.Add(key, cut);
                        }
                    }
                    if (result.Count == 0 || result[result.Count - 1] != cut) result.Add(cut);
                }
                if (currentIn && (result.Count == 0 || result[result.Count - 1] != current)) result.Add(current);
                previous = current; previousDistance = distance;
            }
            if (result.Count > 1 && result[0] == result[result.Count - 1]) result.RemoveAt(result.Count - 1);
            return result;
        }

        private static float BiomeBoundary(Vector3 vertex, int boundary, WorldEnvironmentConfig config)
        {
            var point = new Vector2(vertex.x, vertex.z);
            if (boundary == 0) return Vector2.Distance(point, config.lakeCenter) - config.lakeRadius * .75f;
            if (boundary == 1) return Vector2.Distance(point, config.lakeCenter) - config.lakeRadius * 1.22f;
            return Mathf.Max(DistanceToPath(point, config) - config.pathWidth * .5f, .45f - point.magnitude, point.magnitude - 5.2f);
        }

        private static float DistanceToPath(Vector2 p, WorldEnvironmentConfig config)
        {
            float minD = float.MaxValue;
            const int steps = 16;
            Vector2 prev = config.pathStart;
            for (int i = 1; i <= steps; i++)
            {
                float t = (float)i / steps;
                Vector2 curr = Mathf.Pow(1f - t, 2) * config.pathStart +
                               2f * (1f - t) * t * config.pathMid +
                               Mathf.Pow(t, 2) * config.pathEnd;

                float d = DistancePointToSegment(p, prev, curr);
                if (d < minD) minD = d;
                prev = curr;
            }
            return minD;
        }

        private static float DistancePointToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float sqrLen = ab.sqrMagnitude;
            if (sqrLen < 1e-6f) return Vector2.Distance(p, a);

            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / sqrLen);
            Vector2 proj = a + t * ab;
            return Vector2.Distance(p, proj);
        }

        /// <summary>
        /// B02: Xây dựng mặt nước hồ uốn cong theo mặt cầu của tiểu hành tinh,
        /// bảo đảm mọi đỉnh của vành nước đều nằm chìm nhẹ vào bờ cát kín, không lộ mép treo lơ lửng.
        /// </summary>
        public static Mesh BuildLakeWaterMesh(WorldEnvironmentConfig config)
        {
            var mesh = new Mesh { name = "Lake_Water_Mesh" };
            int segments = 40;
            var verts = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();

            // Độ sâu của mực nước so với mặt cầu cơ bản tại tâm hồ
            float waterOffset = 0.055f;

            // Tâm mặt hồ bám theo mặt cầu
            float centerBaseY = CalculateBaseSphereY(config.lakeCenter.x, config.lakeCenter.y, config);
            Vector3 center = new Vector3(config.lakeCenter.x, centerBaseY - waterOffset, config.lakeCenter.y);
            verts.Add(center);
            normals.Add(new Vector3(config.lakeCenter.x / config.sphereRadius, 1f, config.lakeCenter.y / config.sphereRadius).normalized);
            uvs.Add(new Vector2(0.5f, 0.5f));

            // Bán kính mặt nước được thiết kế gọn gàng trong lòng hồ (0.88 bán kính hồ)
            float waterRadius = config.lakeRadius * 0.88f;
            for (int i = 0; i < segments; i++)
            {
                float angle = (float)i / segments * Mathf.PI * 2f;
                // Biến điệu nhẹ nhàng bờ nước tự nhiên
                float r = waterRadius * (1f + 0.04f * Mathf.Sin(angle * 3f));
                float x = config.lakeCenter.x + r * Mathf.Cos(angle);
                float z = config.lakeCenter.y + r * Mathf.Sin(angle);

                // Cao độ nước bám theo mặt cầu cơ bản: mép nước tiếp xúc chuẩn xác vào bờ cát
                float edgeBaseY = CalculateBaseSphereY(x, z, config);
                float y = edgeBaseY - waterOffset;

                Vector3 pos = new Vector3(x, y, z);
                verts.Add(pos);
                normals.Add(new Vector3(x / config.sphereRadius, 1f, z / config.sphereRadius).normalized);
                uvs.Add(new Vector2(0.5f + 0.5f * Mathf.Cos(angle), 0.5f + 0.5f * Mathf.Sin(angle)));
            }

            // Clockwise winding nhìn từ trên xuống: 0 -> next -> i
            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                tris.Add(0);
                tris.Add(1 + next);
                tris.Add(1 + i);
            }

            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// B06: Chuẩn hóa winding và normal thác nước, loại bỏ các tam giác ngược chiều dùng chung vertex.
        /// </summary>
        public static Mesh BuildWaterfallMesh(WorldEnvironmentConfig config)
        {
            var mesh = new Mesh { name = "Waterfall_Mesh" };

            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();

            const int points = 17;
            Vector2 flow = (config.lakeCenter - config.waterfallOrigin).normalized;
            Vector3 side = new Vector3(-flow.y, 0, flow.x);
            var water = BuildLakeWaterMesh(config);
            var waterVertices = water.vertices;
            var waterTriangles = water.triangles;
            for (int i = 0; i < points; i++)
            {
                float t = (float)i / (points - 1);
                Vector2 position = Vector2.Lerp(config.waterfallOrigin, config.lakeCenter, t);
                float width = Mathf.Lerp(.32f, .50f, t);
                Vector3 center = new Vector3(position.x, 0, position.y);
                Vector3 left = center - side * (width * .5f);
                Vector3 right = center + side * (width * .5f);
                left.y = WaterfallSurfaceY(left, config, waterVertices, waterTriangles);
                right.y = WaterfallSurfaceY(right, config, waterVertices, waterTriangles);

                verts.Add(left);
                verts.Add(right);

                uvs.Add(new Vector2(0f, t));
                uvs.Add(new Vector2(1f, t));
            }
            if (Application.isPlaying) Object.Destroy(water); else Object.DestroyImmediate(water);

            for (int i = 0; i < points - 1; i++)
            {
                int r0 = i * 2;
                int r1 = r0 + 1;
                int r2 = r0 + 2;
                int r3 = r0 + 3;

                // Chuẩn hóa 1 bộ tam giác xuôi hướng ra ngoài, không nhân đôi ngược chiều làm triệt tiêu normal
                tris.Add(r0); tris.Add(r1); tris.Add(r2);
                tris.Add(r1); tris.Add(r3); tris.Add(r2);
            }

            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static float WaterfallSurfaceY(Vector3 point, WorldEnvironmentConfig config, Vector3[] water, int[] triangles)
        {
            float height = CalculateHeight(point.x, point.z, config) + .02f;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                Vector3 a = water[triangles[i]], b = water[triangles[i + 1]], c = water[triangles[i + 2]];
                float denominator = (b.z - c.z) * (a.x - c.x) + (c.x - b.x) * (a.z - c.z);
                float u = ((b.z - c.z) * (point.x - c.x) + (c.x - b.x) * (point.z - c.z)) / denominator;
                float v = ((c.z - a.z) * (point.x - c.x) + (a.x - c.x) * (point.z - c.z)) / denominator;
                if (u >= -.00001f && v >= -.00001f && u + v <= 1.00001f)
                    height = Mathf.Max(height, u * a.y + v * b.y + (1 - u - v) * c.y + .003f);
            }
            return height;
        }
    }
}
