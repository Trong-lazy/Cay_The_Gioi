# Kiểm chứng P04: Vỏ Thân Cổ Thụ (Già hơn nhưng cùng loài)

Task: T10  
Date: 2026-10-06  
Result: PARTIAL — Hoàn thành P04 (Sinh bộ texture vỏ thân Cổ Thụ 1024×1024 Ancient_Bark_BaseColor và Ancient_Bark_Normal, liên kết shader Principled BSDF với normal map trên ArtSource/Models/Bonsai_Trunk_Candidate_v01.blend, kết xuất 8 ảnh kiểm chứng và ảnh so sánh trực tiếp cặp Cây Lớn - Cổ Thụ); T10 giữ IN_PROGRESS chờ bước đóng gói bake/export FBX (P05) và nghiệm thu Unity.

## Yêu cầu và phạm vi

Thực hiện Prompt P04 của bộ `Docs/Art/Tree_Materials_Surface_Prompts_20261006.md`, tuân thủ quy tắc chung:
- Sửa trực tiếp file nguồn `ArtSource/Models/Bonsai_Trunk_Candidate_v01.blend`; bảo toàn 100% nguyên vẹn 3 nguồn khác:
  - Mầm: `Tree_Seedling_Simple_v01.blend`
  - Non: `Tree_Young_Simple_v01.blend`
  - Lớn: `Tree_Large.blend`
- Bảo toàn nguyên vẹn `Assets/_Project/Art/Bonsai_Tree.blend` và toàn bộ thư mục `Assets/`. Không tự đồng bộ sang Assets hoặc dựng thêm stage trong gameplay.
- Khóa tuyệt đối hình học (vertex positions, topology, corner normals, silhouette, mật độ/vị trí lá, cụm tán, thân/cành/rễ, pivot, unit scale, transforms). Không remesh, subdivision, decimate, sculpt hoặc displacement geometry.
- Audit dữ liệu lưu trước khi làm. Lưu trực tiếp với `save_version = 0` (không tạo `.blend1` dư).
- Tạo bộ texture vỏ thân chuyên biệt lưu ổn định tại `ArtSource/Textures/Tree_Common/`:
  - `Ancient_Bark_BaseColor.png` (1024×1024)
  - `Ancient_Bark_Normal.png` (1024×1024)
- Bảng màu vỏ nâu xám già dặn P01: Nền nâu xám `#70634F`, sống vỏ nhạt `#9B896C`, khe nứt `#463F34`. Không xám trắng chết cây, không đen cháy, không hóa đá. Bề mặt matte, Metallic = 0.0, Roughness = 0.88, Emission = 0.0.
- Ngôn ngữ hình ảnh: Thớ gỗ uốn quanh thân/cành theo hướng sinh trưởng; sống vỏ rộng và đứt đoạn hơn Cây Lớn, khe nứt phân nhánh vừa phải, vài vùng chai vỏ quanh cổ cành. Chênh độ sâu thị giác khoảng 1.6× so với Cây Lớn. Không tạo u mới, hang hốc, mặt người hay dây leo.
- Tọa độ UV: Giữ nguyên layer `UVMap` đã có sẵn trên cả 3 mesh (`Trunk_Branches`, `Roots`, `Foliage`). Lá tiếp tục dùng bộ vân lá dùng chung P02 (`Leaf_Common_BaseColor.png` và `Leaf_Common_Normal.png`) với tint già trầm `#507C38`.
- Kết xuất đầy đủ 8 góc nhìn chuẩn: perspective (3/4), front, back, left, right, closeup_fork (cận chạc cành), closeup_roots (cận cảnh bộ rễ già) và ảnh so sánh cặp Cây Lớn cạnh Cổ Thụ (`large_ancient_bark_comparison.png`).

## Đặc tả Bộ Texture Vỏ Thân Cổ Thụ

Bộ texture được sinh tự động bởi `Tools/Blender/generate_ancient_bark_textures.py` đạt chuẩn đồ họa realtime:

1. **`Ancient_Bark_BaseColor.png` (1024×1024, sRGB PNG):**
   - Nền màu vỏ nâu xám già `#70634F` (`RGB(112, 99, 79)`).
   - Sống vỏ nhạt thời gian `#9B896C` (`RGB(155, 137, 108)`) tại các gờ bản vỏ rộng.
   - Khe rãnh nứt phân nhánh `#463F34` (`RGB(70, 63, 52)`).
   - Bản vỏ rộng hơn Cây Lớn ($N_x=6$ cột thay vì 8), thớ gỗ xoắn uốn lượn mạnh mẽ (domain warping đa tầng với 4 hài ba), bổ sung 64 vi vết khía và nốt chai vỏ (lenticels/callouses).
   - Lặp tuần hoàn liền mạch 100% (seamlessly tileable), **không bake bóng nắng định hướng hay viền đen**.

2. **`Ancient_Bark_Normal.png` (1024×1024, Tangent Space OpenGL +Y, Non-Color PNG):**
   - Đạo hàm sai phân trung tâm tuần hoàn từ trường độ cao đã qua lọc box-blur mịn.
   - Sống vỏ gồ ghề dứt khoát, rãnh nứt sâu hơn (~1.6× so với Large: strength 22.0 trong map và 1.05 trong shader node), thể hiện rõ độ già cỗi nhưng không làm rách bề mặt.
   - Chuẩn tangent-space OpenGL: $+Y$ up (kênh Green hướng lên), $+X$ right (kênh Red hướng phải), flat $N_z \approx 1.0$ (kênh Blue).
   - Liền mạch 100% không để lại đường nối hay lỗi gãy góc trên thân.

## Cấu hình UV & Shader trên Bonsai_Trunk_Candidate_v01.blend

- **UV Mapping**:
  - Toàn bộ 3 mesh (`Trunk_Branches`, `Roots`, `Foliage`) giữ nguyên layer `UVMap` chuẩn từ baseline D34/P01/P02.
  - `UVMap` là kênh UV0 active render duy nhất cho toàn bộ model Cổ Thụ.
- **Shader `M_Bark`**:
  - `ShaderNodeTexCoord` (UV) $\to$ `ShaderNodeMapping` (Scale: `(2.5, 2.5, 1.0)`).
  - Image Texture `Ancient_Bark_BaseColor.png` (sRGB) $\to$ `Base Color`.
  - Image Texture `Ancient_Bark_Normal.png` (Non-Color) $\to$ `ShaderNodeNormalMap` (Tangent Space, Strength `1.05`) $\to$ `Normal`.
  - `Roughness`: 0.88 (matte), `Metallic`: 0.0, `Emission`: 0.0.
- **Shader `M_Foliage`**:
  - Duy trì 100% cấu hình P02: Image Texture `Leaf_Common_BaseColor.png` $\times$ linear tint `#507C38` (Factor 1.0) và Normal Map `Leaf_Common_Normal.png` (Strength 0.22, Tangent Space).

## Kết quả kiểm chứng thực tế và bảo toàn hình học

Blender CLI 5.2.2 LTS (`--background --factory-startup --disable-autoexec`).  
Áp dụng trực tiếp bằng `Tools/Blender/apply_p04_ancient_bark.py`, lưu với `save_version = 0`, sau đó nạp lại độc lập từ đĩa và đối chiếu toàn bộ thông số:

| Object | Vertices (Trước/Sau) | Triangles (Trước/Sau) | Polygons (Trước/Sau) | Edges (Trước/Sau) | Geometry Fingerprint | Trạng thái bảo toàn |
| --- | --- | --- | --- | --- | --- | --- |
| `Foliage` | 22.880 / 22.880 | 18.304 / 18.304 | 18.304 / 18.304 | 36.608 / 36.608 | 100% trùng khớp | ĐẠT |
| `Roots` | 9.120 / 9.120 | 17.856 / 17.856 | 8.928 / 8.928 | 18.048 / 18.048 | 100% trùng khớp | ĐẠT |
| `Trunk_Branches` | 19.332 / 19.332 | 37.728 / 37.728 | 18.864 / 18.864 | 38.196 / 38.196 | 100% trùng khớp | ĐẠT |
| **Tổng cộng** | **51.332 / 51.332** | **73.888 / 73.888** | **46.096 / 46.096** | **92.852 / 92.852** | **100% trùng khớp** | **ĐẠT (Khớp mốc baseline)** |

- **Hash SHA-256 file `Bonsai_Trunk_Candidate_v01.blend`:**
  - Trước P04: `e1e9227534be3c281b4bb0870c043f963827a277f533b955bbb93771e9e4961e`
  - Sau P04: `4233aecf6225130b638905fc2e2d6866c5f02c4933b013ffdcab4b2cc4850224`
- **Tài sản bảo vệ (Khẳng định 100% không bị thay đổi):**
  - `Assets/_Project/Art/Bonsai_Tree.blend`: `b99a966a42bd2a44591c7ba007a3bb896482a013dc6a0f4c5f979a1a12c3fc66`
  - `Assets/_Project/Art/Bonsai_Tree.fbx`: `802610722100c52f810e198c9dcf43a23267d31d30cf7af38747ed6a3bf68644`
  - `ArtSource/Models/Tree_Seedling_Simple_v01.blend`: `0497709bb53f906e7aa2abedf832d59d1f3b0c57c4144cdc3aa96e74f4f461ff`
  - `ArtSource/Models/Tree_Young_Simple_v01.blend`: `004480f34facc7077b1e7b24671cb414faf3a7c76c1af9d6f8b024eb319f4a9a`
  - `ArtSource/Models/Tree_Large.blend`: `e19a5c06c7d4a8ece000ee1ada30d01e5d6a40c7468cec7597767a3d59d914e1`

## Bằng chứng

Toàn bộ script, texture và 8 ảnh kết xuất lưu tại `Evidence/Tree_Materials_P04_20261006/final/`:
- Texture vỏ cây Cổ Thụ:
  - `ArtSource/Textures/Tree_Common/Ancient_Bark_BaseColor.png`
  - `ArtSource/Textures/Tree_Common/Ancient_Bark_Normal.png`
- Script thực hiện:
  - `Tools/Blender/generate_ancient_bark_textures.py`: Sinh 2 file texture bằng NumPy & Blender Image.
  - `Tools/Blender/apply_p04_ancient_bark.py`: Gán shader, lưu và kết xuất final.
- Báo cáo số liệu:
  - `Evidence/Tree_Materials_P04_20261006/final/apply_p04_report.json`
- Ảnh kết xuất hoàn thiện:
  - `perspective.png` (Phối cảnh 3/4 Cổ Thụ)
  - `front.png` (Mặt trước)
  - `back.png` (Mặt sau)
  - `left.png` (Mặt trái)
  - `right.png` (Mặt phải)
  - `closeup_fork.png` (Cận cảnh thân xoắn và chạc cành Cổ Thụ)
  - `closeup_roots.png` (Cận cảnh bộ rễ già bạnh tỏa 3D)
  - `large_ancient_bark_comparison.png`: Ảnh so sánh trực quan Cây Lớn (nâu ấm, rãnh nông) đặt cạnh Cổ Thụ (nâu xám, rãnh rõ nét) dưới cùng điều kiện chiếu sáng, thể hiện rõ tính nhất quán về loài thực vật và sự chuyển biến tự nhiên theo tuổi thọ cây.

## Phần chưa nghiệm thu / Dừng sau P04

- Đây là bước P04 (vỏ thân Cổ Thụ), chưa thực hiện đóng gói bake/xuất đè FBX (P05).
- Chưa tích hợp vào Assets, chưa tạo Unity URP Lit Material, chưa nghiệm thu Play Mode hay chỉnh sửa logic gameplay. T10 duy trì trạng thái `IN_PROGRESS`.
