# Báo Cáo Tạo & Kiểm Chứng Model Cây Mầm (Tree_Seedling_Simple_v01)

- **Ngày thực hiện:** 06/10/2026
- **Stage:** Mầm (`seedling` - dưới 15 ngày nảy mầm).
- **Tham chiếu chính:** `01_seedling.png` (đính kèm trong yêu cầu người dùng).
- **File Blend:** `ArtSource/Models/Tree_Seedling_Simple_v01.blend`
- **File FBX Export:** `ArtSource/Exports/Tree_Seedling_Simple_v01.fbx`
- **Quyết định liên quan:** D27 (Docs/Decisions.md).

---

## 1. Cấu Trúc Đối Tượng & Thống Kê Mesh
Model được tổ chức thành 4 mesh object độc lập, origin đặt chính xác tại cổ rễ / mặt đất dự kiến `z = 0`:

| Tên Object | Thành phần | Vertices | Triangles | Vật liệu gán |
| :--- | :--- | :--- | :--- | :--- |
| `Roots` | Rễ cọc chính + 2 nhánh rễ phụ uốn lượn tự nhiên ($z \le 0$) | 170 | 316 | `M_Roots` (Trắng kem / ngà #F0E6CC) |
| `Trunk_Branches` | Thân non màu xanh chữ S mềm mại + cuống 2 lá + cuống chồi ($z \ge 0$) | 150 | 258 | `M_Stem` (Xanh mầm tươi #61AD26) |
| `Foliage` | 2 lá mầm oval nhọn có độ dày & sống giữa + chồi non 3 lá nhỏ ở đỉnh | 600 | 1.180 | `M_Leaves` (Xanh lá tươi #6BBD2E) |
| `SeedShell` | 2 nửa vỏ hạt nâu ôm sát chân thân, lòng lõm lộ rõ, miệng tách hơi gợn | 516 | 1.024 | `M_SeedShell_Outer` (Nâu vỏ) & `Inner` (Vàng hạt) |
| **Tổng cộng** | **Toàn bộ cây mầm** | **1.436** | **2.778** | **Mục tiêu < 3.000 tris (ĐẠT)** |

---

## 2. Kích Thước & Tỷ Lệ Thực Tế (World Bounding Box)
- **Điểm gốc (Origin):** `(0, 0, 0)` tại cổ rễ (rễ phát triển xuống $z < 0$, thân và tán phát triển lên $z > 0$).
- **Chiều cao phần thân/lá trên cổ rễ:** `0.258m` (bám sát mốc yêu cầu tạm `0.25m`).
- **Chiều sâu rễ dưới cổ rễ:** `0.198m`.
- **Độ xòe tán 2 lá mầm (Trục X):** `0.293m`.
- **Bề dày tổng thể (Trục Y):** `0.088m`.
- **Tổng chiều cao toàn cây:** `0.456m`.
- **Transform:** `Scale = (1.0, 1.0, 1.0)`, `Rotation = (0.0, 0.0, 0.0)` trên tất cả 4 object.

---

## 3. Kiểm Chứng FBX Reimport (Phiên Cô Lập)
Tái nạp file `Tree_Seedling_Simple_v01.fbx` vào môi trường Blender sạch để kiểm tra:
1. Đủ 4 mesh object đúng tên chuẩn.
2. Không chứa Camera, Light hay helper objects.
3. Scale giữ nguyên `(1.0, 1.0, 1.0)`, trục chuyển đổi Unity Y-up, -Z forward.
4. Tổng số đa giác sau khi export/import: đúng `2.778` triangles.
5. Ảnh render kiểm chứng reimport lưu tại: `Evidence/Seedling_v01_Audit/reimport_fbx_perspective.png`.

---

## 4. Bộ Ảnh Bằng Chứng Render (Evidence)
Toàn bộ ảnh chụp render trung tính lưu tại `Evidence/Seedling_v01_Audit/`:
- `preview_front.png`: Góc chính diện mặt trước.
- `preview_back.png`: Góc mặt sau.
- `preview_perspective.png`: Góc phối cảnh 3/4 nhìn rõ lòng vỏ hạt và chồi non.
- `preview_side_left.png`: Góc nghiêng cạnh bên.
- `reimport_fbx_perspective.png`: Góc phối cảnh reimport từ FBX.

---

## 5. Trạng Thái Phiên Blender GUI
- Đã nạp trực tiếp file `ArtSource/Models/Tree_Seedling_Simple_v01.blend` vào phiên Blender GUI đang chạy của người dùng.
- Đã chọn toàn bộ 4 object, căn giữa khung nhìn Viewport 3D và bật Cavity Shading để người dùng có thể xem và xoay 360 độ tức thì.
