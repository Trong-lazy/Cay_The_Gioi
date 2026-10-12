# Kiểm chứng P02: Vân Lá Dùng Chung Cho Bốn Nguồn Cây

Task: T10  
Date: 2026-10-06  
Result: PARTIAL — Hoàn thành P02 (Tạo bộ texture gân lá dùng chung 512×512, chuẩn hóa UV lá Mầm/Non khớp chuẩn D34, liên kết shader Principled BSDF với normal map trên cả 4 file nguồn .blend); T10 giữ IN_PROGRESS chờ các bước tiếp theo (vỏ thân P03/P04, bake/export P05) và nghiệm thu Unity.

## Yêu cầu và phạm vi

Thực hiện Prompt P02 của bộ `Docs/Art/Tree_Materials_Surface_Prompts_20261006.md`, tuân thủ quy tắc chung:
- Tạo bộ texture gân lá dùng chung tại `ArtSource/Textures/Tree_Common/`:
  - `Leaf_Common_BaseColor.png` (512×512)
  - `Leaf_Common_Normal.png` (512×512)
- Sửa trực tiếp 4 file nguồn trong `ArtSource/Models/`:
  1. Mầm: `Tree_Seedling_Simple_v01.blend`
  2. Non: `Tree_Young_Simple_v01.blend`
  3. Lớn: `Tree_Large.blend`
  4. Cổ Thụ: `Bonsai_Trunk_Candidate_v01.blend`
- Bảo toàn tuyệt đối hình học (vertex positions, topology, corner normals, silhouette, mật độ/vị trí lá, cụm tán, thân/cành/rễ, pivot, unit scale, transforms).
- Bảo toàn nguyên vẹn `Assets/_Project/Art/Bonsai_Tree.blend` và toàn bộ thư mục `Assets/`. Không tự đồng bộ sang Assets hoặc dựng thêm stage trong gameplay.
- Audit dữ liệu lưu và live Blender GUI trước khi làm (`bpy.data.is_dirty: False`). Không tạo file candidate mới, không tạo file `.blend1` dư (`save_version = 0`).
- Giữ UV lá đã đồng bộ giữa Lớn và Cổ Thụ (D34); chuẩn hóa UV lá Mầm và Non khớp cùng hệ tọa độ mà không thay đổi bất kỳ đỉnh/normal nào.
- Liên kết node shader Principled BSDF với texture gân lá và normal map: gân lá tinh tế, sần mịn matte, không lốm đốm hay biến thành xương cá trắng khi camera xa. Normal map tangent-space OpenGL (+Y up, Non-Color), BaseColor sRGB kết hợp hòa trộn nhân (Multiply) với màu nền P01 để tránh bị tối gấp đôi.
- Kết xuất đầy đủ 5 góc nhìn chuẩn (perspective, back, top, low, closeup foliage) cho cả 4 cây dưới cùng điều kiện chiếu sáng và phơi sáng trung tính, cùng ảnh so sánh Cây Lớn cạnh Cổ Thụ (`large_ancient_comparison.png`).
- Tái nạp/kiểm chứng độc lập sau khi lưu.

## Đặc tả Bộ Texture Gân Lá Dùng Chung

Bộ texture được tạo tự động bởi `Tools/Blender/generate_leaf_textures.py` đạt tiêu chuẩn đồ họa realtime:

1. **`Leaf_Common_BaseColor.png` (512×512, sRGB PNG):**
   - Màu nền trung tính sáng ấm (`#DCDCD0` / `RGB(220, 220, 208)`).
   - Sống lá chính (midrib) thon chạy dọc từ cuống $(0.5, 0.0)$ đến ngọn $(0.5, 1.0)$.
   - 4 cặp gân phụ (lateral veins) uốn cong mềm mại hướng về mép lá, hơi bất đối xứng tự nhiên, độ sáng tăng nhẹ (+12% luminance).
   - Vùng viền ngoài lá được lấp đầy màu trung tính bằng phẳng, triệt tiêu hoàn toàn viền sáng hay lỗi kéo vệt (bleeding) khi nén/filtering mipmap.

2. **`Leaf_Common_Normal.png` (512×512, Tangent Space OpenGL +Y, Non-Color PNG):**
   - Nền phẳng: màu tím trung tính `RGB(128, 128, 255)` ($N_x=0, N_y=0, N_z=1$).
   - Sống gân giữa hình vòm cong mượt mà, độ sâu chuyển tiếp mềm mại qua bộ lọc box-blur đa tầng (multi-pass separable blur).
   - 4 cặp gân phụ phân nhánh chéo hình bán nguyệt thoải dần về mép lá.
   - Bổ sung vi cấu trúc tế bào lá (isotropic cellular micro-grain) mềm mại, loại bỏ triệt để hiện tượng sóng vằn chéo (directional banding), tạo độ sần matte tự nhiên khi ánh sáng quét qua.
   - Vùng đệm biên ngoài hoàn toàn phẳng để mép lá khép kín tiếp giáp trơn tru.

## Tọa độ UV Lá & Cấu hình Shader trên Bốn Nguồn Cây

Quy chuẩn UV lá chung cho cả 4 mốc:
- Cuống lá (Petiole/Base): $(0.5, 0.0)$
- Ngọn lá (Tip/Apex): $(0.5, 1.0)$
- Mép lá trái (Left edge): $(0.0, 0.5)$
- Mép lá phải (Right edge): $(1.0, 0.5)$

| Model | UV Layer Lá | Thao tác UV | Shader Node Cấu hình | Normal Strength | Tint Factor | Roughness (P01) |
| --- | --- | --- | --- | --- | --- | --- |
| **Mầm** (`Tree_Seedling_Simple_v01.blend`) | `UVMap` | Unwrap chuẩn hóa theo từng lá mầm (0 vertices/normals đổi) | Image Tex (sRGB) $\times$ P01 linear tint (`#9BCB55`) $\to$ Base Color; Image Tex (Non-Color) $\to$ Normal Map | **0.08** (nhẹ nhàng, mềm non) | **0.70** | 0.65 |
| **Non** (`Tree_Young_Simple_v01.blend`) | `UVMap` | Unwrap chuẩn hóa theo từng lá non (0 vertices/normals đổi) | Image Tex (sRGB) $\times$ P01 linear tint (`#75AA43`) $\to$ Base Color; Image Tex (Non-Color) $\to$ Normal Map | **0.14** (gợi gân mảnh) | **0.85** | 0.70 |
| **Lớn** (`Tree_Large.blend`) | `Float2` | Giữ nguyên UV D34 đồng bộ từ Cổ Thụ | Image Tex (sRGB) $\times$ P01 linear tint (`#5F963E`) $\to$ Base Color; Image Tex (Non-Color) $\to$ Normal Map | **0.20** (độ sâu chuẩn) | **1.00** | 0.72 |
| **Cổ Thụ** (`Bonsai_Trunk_Candidate_v01.blend`) | `UVMap` | Giữ nguyên UV gốc chuẩn D34 | Image Tex (sRGB) $\times$ P01 linear tint (`#507C38`) $\to$ Base Color; Image Tex (Non-Color) $\to$ Normal Map | **0.22** (độ sâu đậm nét già dặn) | **1.00** | 0.75 |

- Toàn bộ 4 cây: Metallic = 0.0, Emission = 0.0. Material thân gỗ/rễ (`M_Stem`, `M_Bark`, `M_Roots`, `M_SeedShell`) giữ nguyên cấu hình P01.

## Kết quả kiểm chứng thực tế và bảo toàn hình học

Blender CLI 5.2.2 LTS (`--background --factory-startup --disable-autoexec`).  
Áp dụng trực tiếp vào 4 file nguồn bằng `Tools/Blender/apply_p02_leaf_textures.py`, lưu với `save_version = 0`, sau đó nạp lại độc lập từ đĩa và đối chiếu toàn bộ thông số:

| Model | Vertices (Trước/Sau) | Triangles (Trước/Sau) | Polygons (Trước/Sau) | Edges (Trước/Sau) | Geometry Fingerprint | Trạng thái bảo toàn |
| --- | --- | --- | --- | --- | --- | --- |
| **Mầm** (`Tree_Seedling_Simple_v01.blend`) | 1.436 / 1.436 | 2.778 / 2.778 | 1.346 / 1.346 | 2.778 / 2.778 | 100% khớp (4/4 mesh) | ĐẠT (khớp mốc 2.778 tris) |
| **Non** (`Tree_Young_Simple_v01.blend`) | 2.420 / 2.420 | 4.574 / 4.574 | 2.243 / 2.243 | 4.659 / 4.659 | 100% khớp (3/3 mesh) | ĐẠT (khớp mốc 4.574 tris) |
| **Lớn** (`Tree_Large.blend`) | 16.632 / 16.632 | 26.682 / 26.682 | 19.426 / 19.426 | 36.054 / 36.054 | 100% khớp (3/3 mesh) | ĐẠT (khớp mốc 26.682 tris D34) |
| **Cổ Thụ** (`Bonsai_Trunk_Candidate_v01.blend`) | 51.332 / 51.332 | 73.888 / 73.888 | 51.996 / 51.996 | 103.324 / 103.324 | 100% khớp (3/3 mesh) | ĐẠT (khớp mốc 73.888 tris) |

- **Hash file SHA-256 trước/sau lưu P02:**
  - `Tree_Seedling_Simple_v01.blend`: `cdbdd09dcf57071eb4052fd286653cd671afc241722c2357e4a970bf3340b141` $\to$ `0497709bb53f906e7aa2abedf832d59d1f3b0c57c4144cdc3aa96e74f4f461ff`
  - `Tree_Young_Simple_v01.blend`: `2aa6df9dd3a81c0ebf78945a537e5bbc27d776a94db812c1895cb8cefaebe454` $\to$ `004480f34facc7077b1e7b24671cb414faf3a7c76c1af9d6f8b024eb319f4a9a`
  - `Tree_Large.blend`: `7a8ac8ffabaa7b296f4207602f0a12b4951ef52ab037a915812283563fa50e3e` $\to$ `c3b088967b65e6174a45a487e857fbac8d19bf63599300c3c210022cd884cd0c`
  - `Bonsai_Trunk_Candidate_v01.blend`: `16c043bbc936504a19e0c5d2f5a87204bc6a57e78dea87da5d512aad7f386219` $\to$ `e354d2f2f018eda32145cffcb43c1a43929edcbf7a3c3c09980a1ae518a9ccef`
- **Tài sản bảo vệ trong `Assets/`:**
  - `Assets/_Project/Art/Bonsai_Tree.blend`: `b99a966a42bd2a44591c7ba007a3bb896482a013dc6a0f4c5f979a1a12c3fc66` (giữ nguyên 100%)
  - `Assets/_Project/Art/Bonsai_Tree.fbx`: `802610722100c52f810e198c9dcf43a23267d31d30cf7af38747ed6a3bf68644` (giữ nguyên 100%)
- **Trạng thái phiên Blender GUI của người dùng:**
  - Đã gọi lệnh `revert_mainfile()` qua Blender MCP, nạp lại trực tiếp `ArtSource/Models/Tree_Large.blend` vừa lưu, hiển thị tức thì shader node `M_Foliage` với texture gân lá và normal map trên viewport của người dùng mà không có xung đột unsaved.

## Bằng chứng

Toàn bộ script, texture và 21 ảnh kết xuất lưu tại `Evidence/Tree_Materials_P02_20261006/`:
- Texture chung:
  - `ArtSource/Textures/Tree_Common/Leaf_Common_BaseColor.png`
  - `ArtSource/Textures/Tree_Common/Leaf_Common_Normal.png`
- Script thực hiện:
  - `Tools/Blender/generate_leaf_textures.py`: Sinh 2 file texture bằng NumPy.
  - `Tools/Blender/preview_p02_leaf_textures.py`: Preview và kết xuất thử nghiệm.
  - `Tools/Blender/apply_p02_leaf_textures.py`: Gán trực tiếp, lưu file và kết xuất final.
- Báo cáo số liệu:
  - `Evidence/Tree_Materials_P02_20261006/preview/preview_p02_report.json`
  - `Evidence/Tree_Materials_P02_20261006/final/apply_p02_report.json`
- Ảnh kết xuất hoàn thiện:
  - `final/seedling/` (perspective, back, top, low, closeup_foliage)
  - `final/young/` (perspective, back, top, low, closeup_foliage)
  - `final/large/` (perspective, back, top, low, closeup_foliage)
  - `final/ancient/` (perspective, back, top, low, closeup_foliage)
  - `final/large_ancient_comparison.png`: Ảnh so sánh Cây Lớn cạnh Cổ Thụ, khẳng định tính nhất quán tuyệt đối về kiểu dáng và vân lá giữa 2 cây.

## Phần chưa nghiệm thu / Dừng sau P02

- Đây là bước P02 (vân lá dùng chung), chưa tạo rãnh khía vỏ thân cho Cây Lớn (P03), chưa tạo rãnh vỏ Cổ Thụ (P04), chưa bake texture hoặc xuất đè FBX (P05).
- Chưa tích hợp vào Assets, chưa tạo Unity URP Lit Material, chưa nghiệm thu Play Mode hay chỉnh sửa logic gameplay. T10 duy trì trạng thái `IN_PROGRESS`.
