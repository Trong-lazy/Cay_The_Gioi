# Kiểm chứng P03: Vỏ Thân Cây Lớn (Trưởng thành nhưng chưa Cổ thụ)

Task: T10  
Date: 2026-10-06  
Result: PARTIAL — Hoàn thành P03 (Sinh bộ texture vỏ thân Cây Lớn 1024×1024 Large_Bark_BaseColor và Large_Bark_Normal, unwrap UV layer Float2 cho Trunk_Branches và Roots, liên kết shader Principled BSDF với normal map trên ArtSource/Models/Tree_Large.blend); T10 giữ IN_PROGRESS chờ các bước tiếp theo (vỏ Cổ Thụ P04, bake/export P05) và nghiệm thu Unity.

## Yêu cầu và phạm vi

Thực hiện Prompt P03 của bộ `Docs/Art/Tree_Materials_Surface_Prompts_20261006.md`, tuân thủ quy tắc chung:
- Chỉ chỉnh sửa trực tiếp `ArtSource/Models/Tree_Large.blend`; bảo toàn 100% nguyên vẹn 3 nguồn khác:
  - Mầm: `Tree_Seedling_Simple_v01.blend`
  - Non: `Tree_Young_Simple_v01.blend`
  - Cổ Thụ: `Bonsai_Trunk_Candidate_v01.blend`
- Bảo toàn nguyên vẹn `Assets/_Project/Art/Bonsai_Tree.blend` và toàn bộ thư mục `Assets/`. Không tự đồng bộ sang Assets hoặc dựng thêm stage trong gameplay.
- Khóa tuyệt đối hình học (vertex positions, topology, corner normals, silhouette, mật độ/vị trí lá, cụm tán, thân/cành/rễ, pivot, unit scale, transforms). Không remesh, subdivision, decimate, sculpt hoặc displacement geometry.
- Audit dữ liệu lưu trước khi làm. Lưu trực tiếp với `save_version = 0` (không tạo `.blend1` dư).
- Tạo bộ texture vỏ thân chuyên biệt lưu ổn định tại `ArtSource/Textures/Tree_Common/`:
  - `Large_Bark_BaseColor.png` (1024×1024)
  - `Large_Bark_Normal.png` (1024×1024)
- Bảng màu vỏ nâu ấm P01: Nền `#8A6848`, sống vỏ sáng `#AD8860`, khe rãnh nhẹ `#594A39`. Bề mặt matte, Metallic = 0.0, Roughness = 0.85, Emission = 0.0. Không vẽ bóng nắng cố định hay đường viền đen.
- Thớ dọc chạy theo trục sinh trưởng địa phương của thân/cành, rãnh nông, đứt đoạn tự nhiên, sống vỏ mềm, vài vết khía lenticel nhỏ. Không nhăn da, không nứt toác sâu như Cổ Thụ.
- Unwrap UV thân/cành (`Trunk_Branches`) và rễ (`Roots`) vào kênh `Float2` (kênh UV0 mặc định cho export) mà không thay đổi bất kỳ đỉnh hay normal nào; giữ nguyên UV lá (`Foliage`) từ D34.
- Kết xuất đầy đủ 8 góc nhìn chuẩn: perspective (3/4), front, back, left, right, closeup_fork (cận chạc cành), oblique_light (ánh sáng xiên gắt soi rõ rãnh vỏ), distance_view (nhìn xa kiểm tra noise).

## Đặc tả Bộ Texture Vỏ Thân Cây Lớn

Bộ texture được sinh tự động bởi `Tools/Blender/generate_bark_textures.py` đạt chuẩn đồ họa realtime:

1. **`Large_Bark_BaseColor.png` (1024×1024, sRGB PNG):**
   - Nền màu vỏ nâu ấm `#8A6848` (`RGB(138, 104, 72)`).
   - Sống vỏ sáng tự nhiên `#AD8860` (`RGB(173, 136, 96)`) tại các vùng gờ nổi.
   - Khe rãnh nông nhẹ nhàng `#594A39` (`RGB(89, 74, 57)`) ở các khe thớ gỗ.
   - Thớ gỗ dọc uốn lượn hữu cơ (domain warping $U_{warp}, V_{warp}$), vân mạng Voronoi bất đối xứng kéo giãn dọc theo chiều cao ($N_x=8, N_y=4$, anisotropic ratio $0.22$).
   - Bổ sung 48 vi vết khía ngang (lenticels) đặc trưng của cây gỗ trưởng thành.
   - Tiling lặp tuần hoàn 100% liền mạch (seamlessly tileable) cả hai chiều U và V. Không bake ánh sáng định hướng.

2. **`Large_Bark_Normal.png` (1024×1024, Tangent Space OpenGL +Y, Non-Color PNG):**
   - Tính toán trực tiếp bằng đạo hàm sai phân trung tâm tuần hoàn (periodic central differences) từ trường độ cao $H(u, v)$ đã lọc mịn box-blur.
   - Rãnh nông mềm mại, sống vỏ uốn lượn tự nhiên, độ sâu thị giác vừa phải (strength 14.0 trong map, 0.85 trong shader node).
   - Chuẩn tangent-space OpenGL: $+Y$ up (kênh Green hướng lên), $+X$ right (kênh Red hướng phải), flat $N_z \approx 1.0$ (kênh Blue).
   - Liền mạch 100% ở mép nối, không có đường nối gắt hay kéo vệt khi áp lên thân trụ.

## Cấu hình UV & Shader trên Tree_Large.blend

- **UV Unwrapping**:
  - `Foliage`: Giữ nguyên layer `Float2` chuẩn D34 (U/V $[0.0, 1.0]$).
  - `Trunk_Branches` & `Roots`: Unwrap vào layer `Float2` với thuật toán `smart_project(angle_limit=66.0, island_margin=0.01)`.
  - Toàn bộ mesh dùng chung tên layer `Float2` làm kênh UV0 active render duy nhất, bảo đảm tương thích hoàn toàn với Unity URP Lit UV0 khi xuất FBX.
- **Shader `M_Bark`**:
  - `ShaderNodeTexCoord` (UV) $\to$ `ShaderNodeMapping` (Scale: `(2.5, 2.5, 1.0)`)
  - Image Texture `Large_Bark_BaseColor.png` (sRGB) $\to$ `Base Color`
  - Image Texture `Large_Bark_Normal.png` (Non-Color) $\to$ `ShaderNodeNormalMap` (Tangent Space, Strength `0.85`) $\to$ `Normal`
  - `Roughness`: 0.85 (matte). `Metallic`: 0.0. `Emission`: 0.0.

## Kết quả kiểm chứng thực tế và bảo toàn hình học

Blender CLI 5.2.2 LTS (`--background --factory-startup --disable-autoexec`).  
Áp dụng trực tiếp bằng `Tools/Blender/apply_p03_bark_materials.py`, lưu với `save_version = 0`, sau đó nạp lại độc lập từ đĩa và đối chiếu toàn bộ thông số:

| Object | Vertices (Trước/Sau) | Triangles (Trước/Sau) | Polygons (Trước/Sau) | Edges (Trước/Sau) | Geometry Fingerprint | Trạng thái bảo toàn |
| --- | --- | --- | --- | --- | --- | --- |
| `Foliage` | 15.400 / 15.400 | 24.640 / 24.640 | 18.480 / 18.480 | 30.800 / 30.800 | 100% trùng khớp | ĐẠT |
| `Roots` | 374 / 374 | 634 / 634 | 290 / 290 | 654 / 654 | 100% trùng khớp | ĐẠT |
| `Trunk_Branches` | 858 / 858 | 1.408 / 1.408 | 656 / 656 | 1.482 / 1.482 | 100% trùng khớp | ĐẠT |
| **Tổng cộng** | **16.632 / 16.632** | **26.682 / 26.682** | **19.426 / 19.426** | **32.936 / 32.936** | **100% trùng khớp** | **ĐẠT (Khớp mốc D34)** |

- **Hash SHA-256 file `Tree_Large.blend`:**
  - Trước P03: `0ee025bad5b29b82fe928791132ce7f6c2a65369c00aedbac8f5952abae89d19`
  - Sau P03: `e19a5c06c7d4a8ece000ee1ada30d01e5d6a40c7468cec7597767a3d59d914e1`
- **Tài sản bảo vệ (Khẳng định 100% không bị thay đổi):**
  - `Assets/_Project/Art/Bonsai_Tree.blend`: `b99a966a42bd2a44591c7ba007a3bb896482a013dc6a0f4c5f979a1a12c3fc66`
  - `Assets/_Project/Art/Bonsai_Tree.fbx`: `802610722100c52f810e198c9dcf43a23267d31d30cf7af38747ed6a3bf68644`
  - `ArtSource/Models/Tree_Seedling_Simple_v01.blend`: `0497709bb53f906e7aa2abedf832d59d1f3b0c57c4144cdc3aa96e74f4f461ff`
  - `ArtSource/Models/Tree_Young_Simple_v01.blend`: `004480f34facc7077b1e7b24671cb414faf3a7c76c1af9d6f8b024eb319f4a9a`
- **Phiên Blender GUI của người dùng:** Người dùng đang mở `Bonsai_Trunk_Candidate_v01.blend` trong GUI; quy trình P03 chạy hoàn toàn độc lập qua background CLI trên `Tree_Large.blend`, không can thiệp hay làm mất state chưa lưu của GUI.

## Bằng chứng

Toàn bộ script, texture và 8 ảnh kết xuất lưu tại `Evidence/Tree_Materials_P03_20261006/final/`:
- Texture vỏ cây:
  - `ArtSource/Textures/Tree_Common/Large_Bark_BaseColor.png`
  - `ArtSource/Textures/Tree_Common/Large_Bark_Normal.png`
- Script thực hiện:
  - `Tools/Blender/generate_bark_textures.py`: Sinh 2 file texture bằng NumPy & Blender Image.
  - `Tools/Blender/apply_p03_bark_materials.py`: Unwrap UV, gán shader, lưu và kết xuất final.
- Báo cáo số liệu:
  - `Evidence/Tree_Materials_P03_20261006/final/apply_p03_report.json`
- Ảnh kết xuất hoàn thiện:
  - `perspective.png` (Phối cảnh 3/4)
  - `front.png` (Mặt trước)
  - `back.png` (Mặt sau)
  - `left.png` (Mặt trái)
  - `right.png` (Mặt phải)
  - `closeup_fork.png` (Cận cảnh thân và chạc cành)
  - `oblique_light.png` (Ánh sáng xiên gắt soi rõ rãnh lồi lõm của vỏ cây)
  - `distance_view.png` (Góc nhìn xa kiểm tra độ mịn và khử nhiễu ở kích thước thumbnail)

## Phần chưa nghiệm thu / Dừng sau P03

- Đây là bước P03 (vỏ thân Cây Lớn), chưa tạo vỏ thân Cổ Thụ (P04), chưa bake/xuất đè FBX (P05).
- Chưa tích hợp vào Assets, chưa tạo Unity URP Lit Material, chưa nghiệm thu Play Mode hay chỉnh sửa logic gameplay. T10 duy trì trạng thái `IN_PROGRESS`.
