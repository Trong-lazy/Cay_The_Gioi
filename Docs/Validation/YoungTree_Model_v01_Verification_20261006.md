# Báo Cáo Tạo & Kiểm Chứng Model Cây Non (Tree_Young_Simple_v01)

- **Ngày thực hiện:** 06/10/2026
- **Stage:** Cây Non (`young_tree` - cây gieo từ hạt dưới 3 tháng).
- **Tham chiếu chính:** `02_young_tree.png` (đính kèm trong yêu cầu người dùng).
- **File Blend:** `ArtSource/Models/Tree_Young_Simple_v01.blend`
- **File FBX Export:** `ArtSource/Exports/Tree_Young_Simple_v01.fbx`
- **Quyết định liên quan:** D28 (Docs/Decisions.md).

---

## 1. Cấu Trúc Đối Tượng & Thống Kê Mesh
Model được tổ chức thành 3 mesh object độc lập, điểm gốc (Origin) đặt chính xác tại cổ rễ / mặt đất dự kiến `z = 0`:

| Tên Object | Thành phần | Vertices | Triangles | Material & Màu sắc |
| :--- | :--- | :---: | :---: | :--- |
| **`Roots`** | Rễ cọc chính uốn lượn tự nhiên ($z \le 0$) + 4 rễ bên chính tỏa rộng và các nhánh phụ. | 314 | 564 | `M_Roots` (Trắng kem / ngà `#F0E6CC`) |
| **`Trunk_Branches`** | Thân xanh mảnh uốn cong chữ S mềm mại ($z \ge 0$) + 8 cuống lá sole + 1 nhánh phụ ngắn và cuống chồi đỉnh. | 308 | 514 | `M_Stem` (Xanh mầm tươi `#61AD26`) |
| **`Foliage`** | 8 lá chính oval nhọn xếp so le có độ dày và sống giữa + 1 lá nhánh con + cụm chồi nhỏ nhánh bên và chồi đỉnh 3 lá non. | 1.638 | 3.224 | `M_Leaves` (Xanh lá tươi `#5DAE22`) |
| **Tổng cộng** | **Toàn bộ cây non** | **2.260** | **4.302** | **Mục tiêu < 5.000 tris (ĐẠT)** |

---

## 2. Kích Thước & Tỷ Lệ Thực Tế (World Bounding Box)
- **Điểm gốc (Origin):** `(0, 0, 0)` tại cổ rễ (rễ phát triển xuống $z < 0$, thân và tán phát triển lên $z > 0$).
- **Chiều cao phần thân/lá trên cổ rễ:** `0.838m` (bám sát mốc yêu cầu tạm `0.8m`).
- **Chiều sâu rễ dưới cổ rễ:** `0.480m`.
- **Độ xòe tán (Trục X):** `0.501m`.
- **Bề dày tổng thể (Trục Y):** `0.126m`.
- **Tổng chiều cao toàn cây:** `1.318m`.
- **Transform:** `Scale = (1.0, 1.0, 1.0)`, `Rotation = (0.0, 0.0, 0.0)` trên tất cả 3 object.

---

## 3. Kiểm Chứng FBX Reimport (Phiên Cô Lập)
Tái nạp file `Tree_Young_Simple_v01.fbx` vào môi trường Blender sạch để kiểm tra:
1. Đủ 3 mesh object đúng tên chuẩn (`Roots`, `Trunk_Branches`, `Foliage`).
2. Không chứa Camera, Light hay helper objects.
3. Scale giữ nguyên `(1.0, 1.0, 1.0)`, trục chuyển đổi Unity Y-up, -Z forward.
4. Tổng số đa giác sau khi export/import: đúng `4.302` triangles (< 5.000 triangles).
5. Ảnh render kiểm chứng reimport lưu tại: `Evidence/YoungTree_v01_Audit/reimport_fbx_perspective.png`.

---

## 4. Bộ Ảnh Bằng Chứng Render (Evidence)
Toàn bộ ảnh chụp render trung tính lưu tại `Evidence/YoungTree_v01_Audit/`:
- `preview_front.png`: Góc chính diện mặt trước.
- `preview_back.png`: Góc mặt sau.
- `preview_perspective.png`: Góc phối cảnh 3/4 nhìn rõ bố cục cành lá và bộ rễ.
- `preview_side_left.png`: Góc nghiêng cạnh bên.
- `reimport_fbx_perspective.png`: Góc phối cảnh reimport từ FBX.

---

## 5. Trạng Thái Phiên Blender GUI
- Đã nạp trực tiếp file `ArtSource/Models/Tree_Young_Simple_v01.blend` vào phiên Blender GUI đang chạy của người dùng.
- Đã chọn toàn bộ 3 object, căn giữa khung nhìn Viewport 3D và bật Cavity Shading để người dùng có thể xem và xoay 360 độ tức thì.
