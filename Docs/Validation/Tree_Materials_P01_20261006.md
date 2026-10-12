# Kiểm chứng P01: Audit và Áp dụng Màu cơ bản cho Bốn Nguồn Cây

Task: T10
Date: 2026-10-06
Result: PARTIAL — Hoàn thành P01 (Audit & gán material màu cơ bản trên 4 nguồn .blend); T10 giữ IN_PROGRESS chờ các bước tiếp theo (vân lá P02, vỏ P03/P04, bake/export P05) và nghiệm thu Unity.

## Yêu cầu và phạm vi

Thực hiện Prompt P01 của bộ `Docs/Art/Tree_Materials_Surface_Prompts_20261006.md`, tuân thủ quy tắc chung:
- Sửa trực tiếp 4 file nguồn trong `ArtSource/Models/`:
  1. Mầm: `Tree_Seedling_Simple_v01.blend`
  2. Non: `Tree_Young_Simple_v01.blend`
  3. Lớn: `Tree_Large.blend`
  4. Cổ Thụ: `Bonsai_Trunk_Candidate_v01.blend`
- Bảo toàn tuyệt đối hình học (vertex positions, topology, corner normals, silhouette, mật độ/vị trí lá, cụm tán, thân/cành/rễ, pivot, unit scale, transforms).
- Bảo toàn nguyên vẹn `Assets/_Project/Art/Bonsai_Tree.blend` và toàn bộ thư mục `Assets/`. Không tự đồng bộ sang Assets hoặc dựng thêm stage trong gameplay.
- Audit dữ liệu lưu và live Blender GUI trước khi làm (`bpy.data.is_dirty: False`). Không tạo file candidate mới, không tạo file `.blend1` dư (`save_version = 0`).
- Gán Principled BSDF với bảng màu đề xuất, chuyển đổi chính xác sRGB sang linear bằng công thức chuẩn. Roughness matte (0.65–0.90), Metallic = 0.0, Emission = 0.0.
- Kết xuất đầy đủ 5 góc nhìn chuẩn (front, back, left, right, perspective 3/4) cho cả 4 cây dưới cùng điều kiện chiếu sáng và phơi sáng trung tính, cùng ảnh so sánh đội hình (lineup progression).
- Tái nạp/kiểm chứng độc lập sau khi lưu.

## Bảng màu và Thông số Material P01

HEX sRGB được chuyển sang linear bằng công thức:
$C_{linear} = C_{srgb} / 12.92$ nếu $C_{srgb} \le 0.04045$, ngược lại $((C_{srgb} + 0.055) / 1.055)^{2.4}$.

| Mốc | Bộ phận | Material | HEX (sRGB) | Linear RGBA | Roughness | Metallic | Emission |
| --- | --- | --- | --- | --- | --- | --- | --- |
| **Mầm** | Lá mầm & chồi | `M_Leaves` | `#9BCB55` | `[0.3278, 0.5972, 0.0908, 1.0]` | 0.65 | 0.0 | 0.0 |
| | Thân non | `M_Stem` | `#8BB75A` | `[0.2582, 0.4735, 0.1022, 1.0]` | 0.80 | 0.0 | 0.0 |
| | Rễ cọc/nhánh | `M_Roots` | `#C9AE82` | `[0.5841, 0.4233, 0.2232, 1.0]` | 0.88 | 0.0 | 0.0 |
| | Vỏ hạt ngoài | `M_SeedShell_Outer` | `#765038` | `[0.1812, 0.0802, 0.0395, 1.0]` | 0.90 | 0.0 | 0.0 |
| | Vỏ hạt trong | `M_SeedShell_Inner` | `#8E6347` | `[0.2705, 0.1248, 0.0630, 1.0]` | 0.90 | 0.0 | 0.0 |
| **Non** | Tán 8 lá & chồi | `M_Leaves` | `#75AA43` | `[0.1779, 0.4020, 0.0561, 1.0]` | 0.70 | 0.0 | 0.0 |
| | Thân cành | `M_Stem` | `#A18153` | `[0.3564, 0.2195, 0.0865, 1.0]` | 0.82 | 0.0 | 0.0 |
| | Rễ 3D | `M_Roots` | `#A98B62` | `[0.3968, 0.2582, 0.1221, 1.0]` | 0.88 | 0.0 | 0.0 |
| **Lớn** | 1.540 phiến lá | `M_Foliage` | `#5F963E` | `[0.1144, 0.3050, 0.0482, 1.0]` | 0.72 | 0.0 | 0.0 |
| | Thân & Cành | `M_Bark` | `#8A6848` | `[0.2542, 0.1384, 0.0648, 1.0]` | 0.85 | 0.0 | 0.0 |
| | 5 Rễ bạnh | `M_Bark` | `#8A6848` | `[0.2542, 0.1384, 0.0648, 1.0]` | 0.85 | 0.0 | 0.0 |
| **Cổ Thụ** | 4.576 phiến lá | `M_Foliage` | `#507C38` | `[0.0802, 0.2016, 0.0395, 1.0]` | 0.75 | 0.0 | 0.0 |
| | Thân cổ thụ | `M_Bark` | `#70634F` | `[0.1620, 0.1248, 0.0782, 1.0]` | 0.88 | 0.0 | 0.0 |
| | Bộ rễ già | `M_Bark` | `#70634F` | `[0.1620, 0.1248, 0.0782, 1.0]` | 0.88 | 0.0 | 0.0 |

## Kết quả kiểm chứng thực tế và bảo toàn hình học

Blender CLI 5.2.2 LTS (`--background --factory-startup --disable-autoexec`).
Mỗi file được nạp, ghi fingerprint trước sửa, áp material, lưu trực tiếp với `save_version = 0`, sau đó mở lại độc lập từ đĩa và đối chiếu toàn bộ thông số:

| Model | Vertices (Trước/Sau) | Triangles (Trước/Sau) | Polygons (Trước/Sau) | Edges (Trước/Sau) | Geometry Fingerprint | Trạng thái bảo toàn |
| --- | --- | --- | --- | --- | --- | --- |
| **Mầm** (`Tree_Seedling_Simple_v01.blend`) | 1.436 / 1.436 | 2.778 / 2.778 | 1.346 / 1.346 | 2.778 / 2.778 | 100% khớp (4/4 mesh) | ĐẠT (khớp mốc 2.778 tris) |
| **Non** (`Tree_Young_Simple_v01.blend`) | 2.420 / 2.420 | 4.574 / 4.574 | 2.243 / 2.243 | 4.659 / 4.659 | 100% khớp (3/3 mesh) | ĐẠT (khớp mốc 4.574 tris) |
| **Lớn** (`Tree_Large.blend`) | 16.632 / 16.632 | 26.682 / 26.682 | 19.426 / 19.426 | 36.054 / 36.054 | 100% khớp (3/3 mesh) | ĐẠT (khớp mốc 26.682 tris D34) |
| **Cổ Thụ** (`Bonsai_Trunk_Candidate_v01.blend`) | 51.332 / 51.332 | 73.888 / 73.888 | 51.996 / 51.996 | 103.324 / 103.324 | 100% khớp (3/3 mesh) | ĐẠT (khớp mốc 73.888 tris) |

- **Hash file trước/sau lưu:**
  - `Tree_Seedling_Simple_v01.blend`: `5c575b1e...` -> `cdbdd09dcf57071eb4052fd286653cd671afc241722c2357e4a970bf3340b141`
  - `Tree_Young_Simple_v01.blend`: `53aee33d...` -> `2aa6df9dd3a81c0ebf78945a537e5bbc27d776a94db812c1895cb8cefaebe454`
  - `Tree_Large.blend`: `a01e1328...` -> `7a8ac8ffabaa7b296f4207602f0a12b4951ef52ab037a915812283563fa50e3e`
  - `Bonsai_Trunk_Candidate_v01.blend`: `a7af560a...` -> `16c043bbc936504a19e0c5d2f5a87204bc6a57e78dea87da5d512aad7f386219`
- **Tài sản bảo vệ trong `Assets/`:**
  - `Assets/_Project/Art/Bonsai_Tree.blend`: `b99a966a42bd2a44591c7ba007a3bb896482a013dc6a0f4c5f979a1a12c3fc66` (giữ nguyên 100%)
  - `Assets/_Project/Art/Bonsai_Tree.fbx`: `802610722100c52f810e198c9dcf43a23267d31d30cf7af38747ed6a3bf68644` (giữ nguyên 100%)
- **Trạng thái phiên Blender GUI của người dùng:**
  - Đã gọi lệnh `revert_mainfile()` qua Blender MCP, nạp lại trực tiếp `ArtSource/Models/Tree_Large.blend` vừa lưu, hiển thị tức thì material `M_Foliage` và `M_Bark` trên viewport của người dùng mà không có xung đột unsaved.

## Audit UV hiện có

- `Seedling` & `Young`: Chưa có UV layer trên các mesh (`uv_layers: []`). Cần unwrap ở các bước tiếp theo khi làm texture/normal map.
- `Large`:
  - `Foliage`: Đã có UV layer `Float2` (U: [0.0, 1.0], V: [0.0, 1.0]) được đồng bộ từ cụm lá thật của Cổ Thụ (D34).
  - `Roots` & `Trunk_Branches`: Có layer `Float2` nhưng tọa độ phẳng `(0, 0)` (placeholder). Sẽ unwrap trong P03 khi tạo vỏ thân.
- `Ancient`:
  - Cả 3 mesh (`Foliage`, `Roots`, `Trunk_Branches`) đều có UV layer `UVMap` (U: [0.0, 1.0], V: [0.0, 1.0]).

## Bằng chứng

Toàn bộ script, báo cáo số liệu và 21 ảnh kết xuất lưu tại `Evidence/Tree_Materials_P01_20261006/`:
- Script audit & áp dụng:
  - `Tools/Blender/audit_p01_sources.py`
  - `Tools/Blender/preview_p01_materials.py`
  - `Tools/Blender/apply_p01_materials.py`
- Báo cáo số liệu:
  - `Evidence/P01_Audit_Initial.json`
  - `Evidence/Tree_Materials_P01_20261006/preview/preview_report.json`
  - `Evidence/Tree_Materials_P01_20261006/final/apply_report.json`
- Ảnh kết xuất:
  - `final/seedling/` (front, back, left, right, perspective)
  - `final/young/` (front, back, left, right, perspective)
  - `final/large/` (front, back, left, right, perspective)
  - `final/ancient/` (front, back, left, right, perspective)
  - `final/lineup_progression.png`: Toàn cảnh 4 cây xếp ngang theo đúng tỷ lệ kích thước thực (Seedling 0.26m, Young 0.84m, Large 2.35m, Ancient 2.4m rộng 8.2m).

## Phần chưa nghiệm thu / Dừng sau P01

- Đây là bước P01 (màu cơ bản), chưa tạo texture gân lá (P02), chưa tạo vỏ khía rãnh/normal map (P03/P04), chưa bake texture hoặc xuất đè FBX (P05).
- Chưa tích hợp vào Assets, chưa tạo Unity URP Lit Material, chưa nghiệm thu Play Mode hay chỉnh sửa logic gameplay. T10 duy trì trạng thái `IN_PROGRESS`.
