using System.Collections.Generic;
using UnityEngine;

namespace CayTheGioi.MapEnvironment
{
    public static class TropicalPropsBuilder
    {
        /// <summary>
        /// Tạo mesh đá khối low-poly stylized với các mặt phẳng vát cạnh tự nhiên.
        /// </summary>
        public static Mesh BuildRockMesh(int seed, Vector3 scale)
        {
            var mesh = new Mesh { name = "Stylized_Rock_Mesh" };
            var prng = new System.Random(seed);

            // Bắt đầu từ khối lăng trụ 8 cạnh hoặc icosahedron biến dạng
            int rings = 3;
            int segments = 6;
            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();

            // Đỉnh trên
            verts.Add(new Vector3(
                ((float)prng.NextDouble() - 0.5f) * 0.15f * scale.x,
                scale.y * (0.95f + (float)prng.NextDouble() * 0.1f),
                ((float)prng.NextDouble() - 0.5f) * 0.15f * scale.z
            ));
            uvs.Add(new Vector2(0.5f, 1f));

            for (int r = 1; r <= rings; r++)
            {
                float t = (float)r / (rings + 1);
                float y = Mathf.Lerp(scale.y, 0f, t);
                float rRadius = Mathf.Sin(t * Mathf.PI) * 0.55f;

                for (int s = 0; s < segments; s++)
                {
                    float angle = (float)s / segments * Mathf.PI * 2f;
                    float jitterRadius = rRadius * (0.8f + (float)prng.NextDouble() * 0.45f);

                    float x = Mathf.Cos(angle) * jitterRadius * scale.x;
                    float z = Mathf.Sin(angle) * jitterRadius * scale.z;

                    verts.Add(new Vector3(x, y, z));
                    uvs.Add(new Vector2((float)s / segments, 1f - t));
                }
            }

            // Đáy dưới (y = -0.1f để chìm chắc vào nền đất)
            verts.Add(new Vector3(0, -0.1f * scale.y, 0));
            uvs.Add(new Vector2(0.5f, 0f));

            var tris = new List<int>();

            // Nối đỉnh chóp với vòng 1
            for (int s = 0; s < segments; s++)
            {
                int next = (s + 1) % segments;
                tris.Add(0);
                tris.Add(1 + s);
                tris.Add(1 + next);
            }

            // Nối các vòng
            for (int r = 0; r < rings - 1; r++)
            {
                int rStart = 1 + r * segments;
                int nStart = 1 + (r + 1) * segments;

                for (int s = 0; s < segments; s++)
                {
                    int next = (s + 1) % segments;
                    int i0 = rStart + s;
                    int i1 = rStart + next;
                    int i2 = nStart + s;
                    int i3 = nStart + next;

                    tris.Add(i0); tris.Add(i2); tris.Add(i1);
                    tris.Add(i1); tris.Add(i2); tris.Add(i3);
                }
            }

            // Nối vòng cuối với đáy
            int bottomIdx = verts.Count - 1;
            int lastRingStart = 1 + (rings - 1) * segments;
            for (int s = 0; s < segments; s++)
            {
                int next = (s + 1) % segments;
                tris.Add(bottomIdx);
                tris.Add(lastRingStart + next);
                tris.Add(lastRingStart + s);
            }

            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            ReverseWinding(tris);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals(); // Flat / sharp faceted shading
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// Tạo bụi dương xỉ nhiệt đới (fern fronds) tỏa cánh cong.
        /// </summary>
        public static Mesh BuildFernMesh(int frondCount = 7, float radius = 0.7f, float height = 0.5f)
        {
            var mesh = new Mesh { name = "Tropical_Fern_Mesh" };
            var verts = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();

            for (int f = 0; f < frondCount; f++)
            {
                float baseAngle = (float)f / frondCount * Mathf.PI * 2f + (f * 0.15f);
                int segs = 4;
                int startVert = verts.Count;

                for (int i = 0; i <= segs; i++)
                {
                    float t = (float)i / segs;
                    // Đường cong uốn ra ngoài và chúc đầu xuống
                    float r = Mathf.Lerp(0.05f, radius, Mathf.Pow(t, 0.8f));
                    float y = Mathf.Sin(t * Mathf.PI * 0.65f) * height;

                    float width = Mathf.Sin(t * Mathf.PI) * 0.16f;
                    Vector3 center = new Vector3(Mathf.Cos(baseAngle) * r, y, Mathf.Sin(baseAngle) * r);
                    Vector3 side = new Vector3(-Mathf.Sin(baseAngle), 0, Mathf.Cos(baseAngle)) * width;

                    verts.Add(center - side);
                    verts.Add(center + side);
                    uvs.Add(new Vector2(0, t));
                    uvs.Add(new Vector2(1, t));
                    normals.Add(Vector3.up);
                    normals.Add(Vector3.up);
                }

                for (int i = 0; i < segs; i++)
                {
                    int i0 = startVert + i * 2;
                    int i1 = i0 + 1;
                    int i2 = i0 + 2;
                    int i3 = i0 + 3;

                    // 2 mặt (double-sided)
                    tris.Add(i0); tris.Add(i2); tris.Add(i1);
                    tris.Add(i1); tris.Add(i2); tris.Add(i3);
                    tris.Add(i0); tris.Add(i1); tris.Add(i2);
                    tris.Add(i1); tris.Add(i3); tris.Add(i2);
                }
            }

            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// Tạo cây lá lớn nhiệt đới (broadleaf plant / chuối cảnh nhiệt đới).
        /// </summary>
        public static Mesh BuildBroadleafMesh(int leafCount = 5, float size = 1.0f)
        {
            var mesh = new Mesh { name = "Tropical_Broadleaf_Mesh" };
            var verts = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();

            for (int l = 0; l < leafCount; l++)
            {
                float baseAngle = (float)l / leafCount * Mathf.PI * 2f + 0.3f;
                int segs = 5;
                int startVert = verts.Count;

                for (int i = 0; i <= segs; i++)
                {
                    float t = (float)i / segs;
                    float r = Mathf.Lerp(0.08f, size * 0.9f, t);
                    float y = (Mathf.Sin(t * Mathf.PI * 0.55f) * 0.85f - (t > 0.7f ? (t - 0.7f) * 0.6f : 0f)) * size;

                    float width = Mathf.Sin(t * Mathf.PI) * (size * 0.28f);
                    Vector3 center = new Vector3(Mathf.Cos(baseAngle) * r, y, Mathf.Sin(baseAngle) * r);
                    Vector3 side = new Vector3(-Mathf.Sin(baseAngle), 0, Mathf.Cos(baseAngle)) * width;

                    verts.Add(center - side);
                    verts.Add(center + side);
                    uvs.Add(new Vector2(0, t));
                    uvs.Add(new Vector2(1, t));
                    normals.Add(Vector3.up);
                    normals.Add(Vector3.up);
                }

                for (int i = 0; i < segs; i++)
                {
                    int i0 = startVert + i * 2;
                    int i1 = i0 + 1;
                    int i2 = i0 + 2;
                    int i3 = i0 + 3;

                    tris.Add(i0); tris.Add(i2); tris.Add(i1);
                    tris.Add(i1); tris.Add(i2); tris.Add(i3);
                    tris.Add(i0); tris.Add(i1); tris.Add(i2);
                    tris.Add(i1); tris.Add(i3); tris.Add(i2);
                }
            }

            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// B05: Tạo khóm hoa san hô nhiệt đới (coral flowers) có cuống hoa vững chắc nối từ mặt đất
        /// và các phiến lá đế tiếp xúc nền đất tự nhiên, loại bỏ cảm giác đĩa hoa lơ lửng.
        /// </summary>
        public static Mesh BuildCoralFlowerMesh(int blossomCount = 4)
        {
            var mesh = new Mesh { name = "Coral_Flower_Mesh" };
            var verts = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();

            // 1. Đế lá sát đất (Basal leaf rosette tiếp xúc mặt đất y = -0.05m đến 0.02m)
            int groundLeafCount = 6;
            Vector3 groundCenter = new Vector3(0, -0.04f, 0);
            int gCenterIdx = verts.Count;
            verts.Add(groundCenter);
            normals.Add(Vector3.up);
            uvs.Add(new Vector2(0.5f, 0.5f));

            for (int gl = 0; gl < groundLeafCount; gl++)
            {
                float glAngle = (float)gl / groundLeafCount * Mathf.PI * 2f;
                float glRadius = 0.28f;
                Vector3 leafTip = new Vector3(Mathf.Cos(glAngle) * glRadius, 0.015f, Mathf.Sin(glAngle) * glRadius);
                verts.Add(leafTip);
                normals.Add(new Vector3(Mathf.Cos(glAngle) * 0.3f, 0.95f, Mathf.Sin(glAngle) * 0.3f).normalized);
                uvs.Add(new Vector2(0.5f + 0.5f * Mathf.Cos(glAngle), 0.5f + 0.5f * Mathf.Sin(glAngle)));
            }

            for (int gl = 0; gl < groundLeafCount; gl++)
            {
                int next = (gl + 1) % groundLeafCount;
                tris.Add(gCenterIdx);
                tris.Add(gCenterIdx + 1 + gl);
                tris.Add(gCenterIdx + 1 + next);
            }

            // 2. Cuống hoa và bông hoa
            for (int b = 0; b < blossomCount; b++)
            {
                float blossomAngle = (float)b / blossomCount * Mathf.PI * 2f;
                Vector3 center = new Vector3(Mathf.Cos(blossomAngle) * 0.22f, 0.12f + (b % 2) * 0.05f, Mathf.Sin(blossomAngle) * 0.22f);
                Vector3 stemBase = new Vector3(Mathf.Cos(blossomAngle) * 0.06f, -0.05f, Mathf.Sin(blossomAngle) * 0.06f);

                // Cuống hoa nối từ gốc mặt đất (stemBase) lên bông hoa (center)
                int stemIdx = verts.Count;
                float stemR = 0.025f;
                for (int s = 0; s < 3; s++)
                {
                    float sa = (float)s / 3 * Mathf.PI * 2f;
                    Vector3 offset = new Vector3(Mathf.Cos(sa) * stemR, 0, Mathf.Sin(sa) * stemR);
                    verts.Add(stemBase + offset); // bottom
                    normals.Add(offset.normalized);
                    uvs.Add(new Vector2((float)s / 3, 0f));

                    verts.Add(center + offset * 0.7f); // top
                    normals.Add(offset.normalized);
                    uvs.Add(new Vector2((float)s / 3, 1f));
                }
                for (int s = 0; s < 3; s++)
                {
                    int next = (s + 1) % 3;
                    int b0 = stemIdx + s * 2;
                    int t0 = b0 + 1;
                    int b1 = stemIdx + next * 2;
                    int t1 = b1 + 1;

                    tris.Add(b0); tris.Add(t0); tris.Add(b1);
                    tris.Add(b1); tris.Add(t0); tris.Add(t1);
                }

                // Cánh hoa
                int petalCount = 5;
                int centerIdx = verts.Count;
                verts.Add(center);
                normals.Add(Vector3.up);
                uvs.Add(new Vector2(0.5f, 0.5f));

                for (int p = 0; p < petalCount; p++)
                {
                    float a = (float)p / petalCount * Mathf.PI * 2f;
                    float petalLen = 0.14f;
                    Vector3 tip = center + new Vector3(Mathf.Cos(a) * petalLen, 0.04f, Mathf.Sin(a) * petalLen);

                    verts.Add(tip);
                    normals.Add(Vector3.up);
                    uvs.Add(new Vector2(0.5f + 0.5f * Mathf.Cos(a), 0.5f + 0.5f * Mathf.Sin(a)));
                }

                for (int p = 0; p < petalCount; p++)
                {
                    int next = (p + 1) % petalCount;
                    tris.Add(centerIdx);
                    tris.Add(centerIdx + 1 + p);
                    tris.Add(centerIdx + 1 + next);
                }
            }

            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// Tạo nấm phát sáng dạ quang (Bioluminescent Mushroom).
        /// Submesh 0: Thân nấm (stem)
        /// Submesh 1: Nón nấm phát sáng (glowing cap)
        /// </summary>
        public static Mesh BuildMushroomMesh(float capRadius = 0.22f, float stemHeight = 0.35f)
        {
            var mesh = new Mesh { name = "Glow_Mushroom_Mesh" };
            var verts = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();

            var stemTris = new List<int>();
            var capTris = new List<int>();

            // 1. Thân nấm (Cylinder uốn cong nhẹ)
            int stemSegs = 8;
            int stemLevels = 3;
            for (int l = 0; l <= stemLevels; l++)
            {
                float t = (float)l / stemLevels;
                float y = t * stemHeight;
                float bendX = Mathf.Sin(t * Mathf.PI * 0.5f) * 0.04f;
                float r = Mathf.Lerp(capRadius * 0.32f, capRadius * 0.22f, t);

                for (int s = 0; s < stemSegs; s++)
                {
                    float angle = (float)s / stemSegs * Mathf.PI * 2f;
                    verts.Add(new Vector3(bendX + Mathf.Cos(angle) * r, y, Mathf.Sin(angle) * r));
                    normals.Add(new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)));
                    uvs.Add(new Vector2((float)s / stemSegs, t));
                }
            }

            for (int l = 0; l < stemLevels; l++)
            {
                int r0 = l * stemSegs;
                int r1 = (l + 1) * stemSegs;
                for (int s = 0; s < stemSegs; s++)
                {
                    int next = (s + 1) % stemSegs;
                    stemTris.Add(r0 + s); stemTris.Add(r1 + s); stemTris.Add(r0 + next);
                    stemTris.Add(r0 + next); stemTris.Add(r1 + s); stemTris.Add(r1 + next);
                }
            }

            // 2. Nón nấm (Dome/Hemisphere)
            int capStartIdx = verts.Count;
            int capSegs = 10;
            int capRings = 4;

            // Đỉnh chóp nón
            Vector3 capTop = new Vector3(0.04f, stemHeight + capRadius * 0.85f, 0);
            verts.Add(capTop);
            normals.Add(Vector3.up);
            uvs.Add(new Vector2(0.5f, 0.5f));

            for (int r = 1; r <= capRings; r++)
            {
                float t = (float)r / capRings;
                float phi = t * (Mathf.PI * 0.52f); // góc cực từ đỉnh xuống vành nón
                float ringR = Mathf.Sin(phi) * capRadius;
                float ringY = stemHeight + Mathf.Cos(phi) * (capRadius * 0.85f);

                for (int s = 0; s < capSegs; s++)
                {
                    float theta = (float)s / capSegs * Mathf.PI * 2f;
                    Vector3 pos = new Vector3(0.04f + Mathf.Cos(theta) * ringR, ringY, Mathf.Sin(theta) * ringR);
                    verts.Add(pos);
                    normals.Add((pos - new Vector3(0.04f, stemHeight, 0)).normalized);
                    uvs.Add(new Vector2(0.5f + 0.5f * Mathf.Cos(theta) * t, 0.5f + 0.5f * Mathf.Sin(theta) * t));
                }
            }

            // Nối chóp với ring 1
            for (int s = 0; s < capSegs; s++)
            {
                int next = (s + 1) % capSegs;
                capTris.Add(capStartIdx);
                capTris.Add(capStartIdx + 1 + s);
                capTris.Add(capStartIdx + 1 + next);
            }

            // Nối các ring nón
            for (int r = 1; r < capRings; r++)
            {
                int r0 = capStartIdx + 1 + (r - 1) * capSegs;
                int r1 = capStartIdx + 1 + r * capSegs;
                for (int s = 0; s < capSegs; s++)
                {
                    int next = (s + 1) % capSegs;
                    capTris.Add(r0 + s); capTris.Add(r1 + s); capTris.Add(r0 + next);
                    capTris.Add(r0 + next); capTris.Add(r1 + s); capTris.Add(r1 + next);
                }
            }

            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.subMeshCount = 2;
            mesh.SetTriangles(stemTris, 0);
            ReverseWinding(capTris);
            mesh.SetTriangles(capTris, 1);
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// Tạo cây cọ nhiệt đới mini (Mini palm tree) cho vùng viền chân trời.
        /// Submesh 0: Thân cọ (Trunk)
        /// Submesh 1: Tán lá cọ (Fronds)
        /// </summary>
        public static Mesh BuildMiniPalmMesh(float height = 2.2f)
        {
            var mesh = new Mesh { name = "Mini_Palm_Mesh" };
            var verts = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();

            var trunkTris = new List<int>();
            var leafTris = new List<int>();

            // 1. Thân cọ cong nhẹ
            int segs = 6;
            int levels = 5;
            for (int l = 0; l <= levels; l++)
            {
                float t = (float)l / levels;
                float y = t * height;
                float bend = Mathf.Sin(t * Mathf.PI * 0.45f) * 0.35f;
                float r = Mathf.Lerp(0.14f, 0.08f, t);

                for (int s = 0; s < segs; s++)
                {
                    float angle = (float)s / segs * Mathf.PI * 2f;
                    verts.Add(new Vector3(bend + Mathf.Cos(angle) * r, y, Mathf.Sin(angle) * r));
                    normals.Add(new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)));
                    uvs.Add(new Vector2((float)s / segs, t * 3f));
                }
            }

            for (int l = 0; l < levels; l++)
            {
                int r0 = l * segs;
                int r1 = (l + 1) * segs;
                for (int s = 0; s < segs; s++)
                {
                    int next = (s + 1) % segs;
                    trunkTris.Add(r0 + s); trunkTris.Add(r1 + s); trunkTris.Add(r0 + next);
                    trunkTris.Add(r0 + next); trunkTris.Add(r1 + s); trunkTris.Add(r1 + next);
                }
            }

            // 2. Tán lá cọ (6 tàu lá vươn cong từ đỉnh)
            int leafStart = verts.Count;
            int frondCount = 6;
            Vector3 topCenter = new Vector3(Mathf.Sin(Mathf.PI * 0.45f) * 0.35f, height, 0);

            for (int f = 0; f < frondCount; f++)
            {
                float baseAngle = (float)f / frondCount * Mathf.PI * 2f;
                int frondSegs = 4;
                int fVertStart = verts.Count;

                for (int i = 0; i <= frondSegs; i++)
                {
                    float t = (float)i / frondSegs;
                    float fLen = t * 1.15f;
                    float fDrop = Mathf.Pow(t, 1.8f) * 0.55f;
                    float w = Mathf.Sin(t * Mathf.PI) * 0.22f;

                    Vector3 pos = topCenter + new Vector3(Mathf.Cos(baseAngle) * fLen, 0.08f - fDrop, Mathf.Sin(baseAngle) * fLen);
                    Vector3 side = new Vector3(-Mathf.Sin(baseAngle), 0, Mathf.Cos(baseAngle)) * w;

                    verts.Add(pos - side);
                    verts.Add(pos + side);
                    normals.Add(Vector3.up);
                    normals.Add(Vector3.up);
                    uvs.Add(new Vector2(0, t));
                    uvs.Add(new Vector2(1, t));
                }

                for (int i = 0; i < frondSegs; i++)
                {
                    int i0 = fVertStart + i * 2;
                    int i1 = i0 + 1;
                    int i2 = i0 + 2;
                    int i3 = i0 + 3;

                    leafTris.Add(i0); leafTris.Add(i2); leafTris.Add(i1);
                    leafTris.Add(i1); leafTris.Add(i2); leafTris.Add(i3);
                    leafTris.Add(i0); leafTris.Add(i1); leafTris.Add(i2);
                    leafTris.Add(i1); leafTris.Add(i3); leafTris.Add(i2);
                }
            }

            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.subMeshCount = 2;
            mesh.SetTriangles(trunkTris, 0);
            mesh.SetTriangles(leafTris, 1);
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// Tạo khóm cỏ nhiệt đới thấp (Low Grass Tuft) bám sát nền đất (cao 0.12m - 0.16m).
        /// </summary>
        public static Mesh BuildGrassTuftMesh(float height = 0.15f)
        {
            var mesh = new Mesh { name = "Grass_Tuft_Mesh" };
            var verts = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();

            int bladeCount = 5;
            for (int b = 0; b < bladeCount; b++)
            {
                float angle = (float)b / bladeCount * Mathf.PI * 2f;
                float lean = 0.08f;
                Vector3 basePos = new Vector3(Mathf.Cos(angle) * 0.03f, -0.04f, Mathf.Sin(angle) * 0.03f);
                Vector3 tipPos = new Vector3(Mathf.Cos(angle) * lean, height * (0.8f + (b % 3) * 0.15f), Mathf.Sin(angle) * lean);
                Vector3 side = new Vector3(-Mathf.Sin(angle), 0, Mathf.Cos(angle)) * 0.025f;

                int vStart = verts.Count;
                verts.Add(basePos - side);
                verts.Add(basePos + side);
                verts.Add(tipPos);

                normals.Add(Vector3.up);
                normals.Add(Vector3.up);
                normals.Add(Vector3.up);

                uvs.Add(new Vector2(0, 0));
                uvs.Add(new Vector2(1, 0));
                uvs.Add(new Vector2(0.5f, 1));

                tris.Add(vStart);
                tris.Add(vStart + 2);
                tris.Add(vStart + 1);
            }

            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// Tạo viên sỏi nhỏ mịn nhẵn (Smooth Pebble) bám đất (cao 0.04m - 0.06m).
        /// </summary>
        public static Mesh BuildPebbleMesh(float radius = 0.08f)
        {
            var mesh = new Mesh { name = "Pebble_Mesh" };
            var verts = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();

            int segs = 6;
            // Đỉnh chóp sỏi
            verts.Add(new Vector3(0, radius * 0.55f, 0));
            normals.Add(Vector3.up);
            uvs.Add(new Vector2(0.5f, 0.5f));

            // Vành giữa
            for (int s = 0; s < segs; s++)
            {
                float angle = (float)s / segs * Mathf.PI * 2f;
                Vector3 p = new Vector3(Mathf.Cos(angle) * radius, radius * 0.15f, Mathf.Sin(angle) * (radius * 0.75f));
                verts.Add(p);
                normals.Add(p.normalized);
                uvs.Add(new Vector2(0.5f + 0.5f * Mathf.Cos(angle), 0.5f + 0.5f * Mathf.Sin(angle)));
            }

            // Đáy sỏi cắm xuống đất
            verts.Add(new Vector3(0, -0.04f, 0));
            normals.Add(Vector3.down);
            uvs.Add(new Vector2(0.5f, 0.5f));

            int bottomIdx = verts.Count - 1;
            for (int s = 0; s < segs; s++)
            {
                int next = (s + 1) % segs;
                // Mặt trên
                tris.Add(0);
                tris.Add(1 + s);
                tris.Add(1 + next);

                // Mặt dưới
                tris.Add(bottomIdx);
                tris.Add(1 + next);
                tris.Add(1 + s);
            }

            ReverseWinding(tris);
            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void ReverseWinding(List<int> triangles)
        {
            for (int i = 0; i < triangles.Count; i += 3)
            { int index = triangles[i + 1]; triangles[i + 1] = triangles[i + 2]; triangles[i + 2] = index; }
        }
    }
}
