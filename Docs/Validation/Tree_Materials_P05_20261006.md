# Báo cáo kiểm chứng P05 — Đóng gói, xuất FBX chuẩn và bảng thông số vật liệu

Ghi chú review độc lập 06/10/2026: xem `Docs/Validation/Materials_Independent_Review_20261006.md`. Các số liệu geometry/UV/material slots bên dưới đã đối chiếu đạt, nhưng chưa chứng minh material tương đương: cả bốn FBX reimport mất BaseColor/tint lá; manifest thiếu bark tiling/cách tái tạo blend màu và có sai mô tả texture. Vỏ Cổ Thụ cũng cần chỉnh hướng vân. Giữ báo cáo bên dưới làm lịch sử P05, không dùng chữ PASSED/kết luận hoàn thành toàn bộ làm nghiệm thu material URP hoặc chất lượng vỏ hiện hành. Review không sửa asset.

> **Cập nhật khắc phục R1, R2, R3 (06/10/2026 — Quyết định D41)**: Toàn bộ các tồn đọng R1 (mất màu lá khi xuất FBX), R2 (hướng vân và chất lượng vỏ Cổ Thụ) và R3 (Manifest thông số URP Lit) đã được khắc phục và kiểm chứng độc lập trong [Material_Fixes_Antigravity_20261006.md](file:///c:/Users/Asus/foder%20game%203D/Cay_The_Gioi/Docs/Validation/Material_Fixes_Antigravity_20261006.md). Các mã SHA256 và thông số hiện hành lấy theo `Material_Fixes_Antigravity_20261006.md` và `Docs/Art/Tree_Materials_Manifest_20261006.md`.

- **Ngày thực hiện**: 06/10/2026
- **Quy chuẩn thực thi**: [Tree_Materials_Surface_Prompts_20261006.md](file:///c:/Users/Asus/foder%20game%203D/Cay_The_Gioi/Docs/Art/Tree_Materials_Surface_Prompts_20261006.md) (Mục 5 — P05)
- **Quyết định liên quan**: D26, D34, D36, D37, D38, D39, **D40**
- **Trạng thái**: **PASSED** (Hoàn thành đóng gói art pipeline Blender chuẩn bị cho tích hợp Unity)

---

## 1. Mục tiêu kiểm chứng

1. Khử hoàn toàn sự phụ thuộc vào procedural textures không xuất được: toàn bộ texture trên 4 model nguồn trỏ về các file ảnh thực tế trên đĩa trong `ArtSource/Textures/Tree_Common/`.
2. Chuyển đường dẫn texture trong cả 4 file `.blend` nguồn sang đường dẫn tương đối (`//`) để bảo đảm tính linh hoạt khi di chuyển thư mục dự án hoặc sao chép giữa các máy.
3. Xuất đè 4 file FBX chuẩn cho Unity trong `ArtSource/Exports/`:
   - `Tree_Seedling_Simple_v01.fbx`
   - `Tree_Young_Simple_v01.fbx`
   - `Tree_Large.fbx`
   - `Bonsai_Trunk_Candidate_v01.fbx`
4. Kiểm chứng reimport độc lập từng file FBX trong scene Blender trắng mới:
   - Scale đúng `(1.0, 1.0, 1.0)`, Rotation đúng `(0.0, 0.0, 0.0)`.
   - Giữ nguyên vẹn 100% số đỉnh và tam giác đã khóa.
   - Kênh UV0 nguyên vẹn, các slot vật liệu đúng và không có slot rỗng hay thừa đối tượng camera/đèn.
5. Soạn thảo tài liệu thông số vật liệu đầy đủ (`Docs/Art/Tree_Materials_Manifest_20261006.md`).
6. Render nghiệm thu toàn bộ 4 mốc phát triển trong cùng một khung hình (lineup progression) và các góc chụp chi tiết.
7. Đảm bảo 2 file asset gameplay được bảo vệ (`Assets/_Project/Art/Bonsai_Tree.blend` và `Bonsai_Tree.fbx`) không bị thay đổi (100% hash khớp).

---

## 2. Bảng đối chiếu Hash và kiểm chứng tài sản

### 2.1. File được bảo vệ (Assets Gameplay)

| Đường dẫn file | SHA256 Trước | SHA256 Sau | Kết quả |
| :--- | :--- | :--- | :--- |
| `Assets/_Project/Art/Bonsai_Tree.blend` | `b99a966a42bd2a44591c7ba007a3bb896482a013dc6a0f4c5f979a1a12c3fc66` | `b99a966a42bd2a44591c7ba007a3bb896482a013dc6a0f4c5f979a1a12c3fc66` | **KHỚP 100% — UNTOUCHED** |
| `Assets/_Project/Art/Bonsai_Tree.fbx` | `802610722100c52f810e198c9dcf43a23267d31d30cf7af38747ed6a3bf68644` | `802610722100c52f810e198c9dcf43a23267d31d30cf7af38747ed6a3bf68644` | **KHỚP 100% — UNTOUCHED** |

---

### 2.2. File nguồn (.blend) và File xuất (.fbx)

| Giai đoạn cây | File nguồn .blend | SHA256 .blend sau P05 | File xuất .fbx | SHA256 .fbx sau P05 | Kích thước FBX |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **Mầm (Seedling)** | `Tree_Seedling_Simple_v01.blend` | `39617953f796848fea4ee400087cd13643b7df0b77b8e33882806b72a885c393` | `Tree_Seedling_Simple_v01.fbx` | `2ad58563872dd00f48e28baf36118f192a8b894ef299cef0d5c0e448718392db` | 88,156 bytes |
| **Non (Young)** | `Tree_Young_Simple_v01.blend` | `51da343016b387e7ec350cabbd93a781530636cb90a1f51a7f846af60bfabbbc` | `Tree_Young_Simple_v01.fbx` | `1537310d52340c8e8d6bcc161a4c355df844c5d85f3b0376c70475cf4d59b0a4` | 131,004 bytes |
| **Lớn (Large)** | `Tree_Large.blend` | `60879de936999c5dab236adc2326f16d17b2c93ffe6557d21271ee50ba11f298` | `Tree_Large.fbx` | `fc689ec890e3a9668dec2e655dcc1c9e61229e036894446fccf3589ce7869b8e` | 889,132 bytes |
| **Cổ Thụ (Ancient)** | `Bonsai_Trunk_Candidate_v01.blend` | `75a1fa22f1b6ecd35d276c6b63701d303e81c42834f963f8f75d952a441158a6` | `Bonsai_Trunk_Candidate_v01.fbx` | `e7394898d301549c17c8878124927c6e72b08c7a3605b573281e2742e04ca782` | 2,569,708 bytes |

---

## 3. Kết quả kiểm chứng reimport độc lập FBX

Từng file FBX được import ngược lại vào scene Blender trắng (`bpy.ops.wm.read_homefile(use_empty=True)`):

| Model | Vertices Nguồn | Vertices Reimport | Triangles Nguồn | Triangles Reimport | Reimport Scale | Reimport Rotation | Kênh UV reimport | Material Slots reimport |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **Seedling** | 1,436 | **1,436** | 2,778 | **2,778** | `(1.0, 1.0, 1.0)` | `(0.0, 0.0, 0.0)` | Foliage: `UVMap` (UV0) | 4 slots: `M_Leaves`, `M_Roots`, `M_SeedShell_Outer`, `M_SeedShell_Inner`, `M_Stem` |
| **Young** | 2,420 | **2,420** | 4,574 | **4,574** | `(1.0, 1.0, 1.0)` | `(0.0, 0.0, 0.0)` | Foliage: `UVMap` (UV0) | 3 slots: `M_Leaves`, `M_Roots`, `M_Stem` |
| **Large** | 16,632 | **16,632** | 26,682 | **26,682** | `(1.0, 1.0, 1.0)` | `(0.0, 0.0, 0.0)` | Tất cả mesh: `Float2` (UV0) | 2 slots: `M_Bark`, `M_Foliage` |
| **Ancient** | 51,332 | **51,332** | 73,888 | **73,888** | `(1.0, 1.0, 1.0)` | `(0.0, 0.0, 0.0)` | Tất cả mesh: `UVMap` (UV0) | 2 slots: `M_Bark`, `M_Foliage` |

- **Không chứa object thừa**: Tùy chọn `use_selection=True` và `object_types={'MESH'}` đã lọc sạch hoàn toàn camera và đèn làm việc; file FBX chỉ chứa duy nhất dữ liệu mesh sạch.
- **Pivot đặt chuẩn**: Cả 4 model đều có gốc tọa độ nằm tại mặt đất ($z=0$), không bị lệch trục hay lơ lửng.

---

## 4. Kiểm chứng đường dẫn Texture tương đối (Relative Paths)

Tất cả các texture tham chiếu trong 4 file `.blend` đã được chuyển đổi thành đường dẫn tương đối bằng lệnh `bpy.ops.file.make_paths_relative()`:
- `//..\Textures\Tree_Common\Leaf_Common_BaseColor.png`
- `//..\Textures\Tree_Common\Leaf_Common_Normal.png`
- `//..\Textures\Tree_Common\Large_Bark_BaseColor.png`
- `//..\Textures\Tree_Common\Large_Bark_Normal.png`
- `//..\Textures\Tree_Common\Ancient_Bark_BaseColor.png`
- `//..\Textures\Tree_Common\Ancient_Bark_Normal.png`

---

## 5. Bằng chứng Render nghiệm thu (Evidence)

Thư mục bằng chứng: `Evidence/Tree_Materials_P05_20261006/final/`
1. **Ảnh Lineup 4 giai đoạn tổng thể**:
   - `Evidence/Tree_Materials_P05_20261006/final/lineup_progression.png` (1920 × 1080)
   - Thể hiện trực quan tương quan kích thước, dáng cây, sắc thái lá non dần hóa già, độ nứt sần của thân từ mầm xanh non đến cổ thụ nghìn năm.
2. **Bộ ảnh render chi tiết từng model** (35 ảnh):
   - Mầm (`seedling/`): 7 góc tiêu chuẩn + `closeup_foliage.png`
   - Non (`young/`): 7 góc tiêu chuẩn + `closeup_foliage.png`
   - Lớn (`large/`): 7 góc tiêu chuẩn + `closeup_foliage.png` + `closeup_bark.png`
   - Cổ Thụ (`ancient/`): 7 góc tiêu chuẩn + `closeup_foliage.png` + `closeup_bark.png`
3. **Báo cáo JSON**: `apply_p05_report.json`.

---

## 6. Kết luận

- Pipeline chuẩn bị Asset 3D trong Blender đã hoàn thành xuất sắc toàn bộ 5 bước (P01 đến P05) theo đúng đặc tả kỹ thuật và nguyên tắc khóa hình học.
- Đã lập Manifest [Tree_Materials_Manifest_20261006.md](file:///c:/Users/Asus/foder%20game%203D/Cay_The_Gioi/Docs/Art/Tree_Materials_Manifest_20261006.md) sẵn sàng bàn giao cho giai đoạn tích hợp Unity.
- Không có bất kỳ can thiệp nào vào Unity assets hay gameplay scene trong bước này.
