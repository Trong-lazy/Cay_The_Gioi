# Báo Cáo Chuyển Model Sửa Sang Assets/_Project/Art/Bonsai_Tree.blend

- **Ngày thực hiện:** 06/10/2026
- **Yêu cầu trực tiếp:** "chuyển file model bạn sửa sang file ở trong ảnh" (ảnh kèm hộp thoại mở file `Assets/_Project/Art/Bonsai_Tree.blend`).
- **Quyết định liên quan:** D26 (Docs/Decisions.md), D23-D25.

---

## 1. Bản Sao Lưu An Toàn (Backup Baseline)
Trước khi ghi đè, hai file baseline nguyên bản đã được lưu trữ an toàn tại:
- Thư mục: `ArtSource/Backup_Before_Apply_20261006/`
- `Bonsai_Tree_baseline.blend` (SHA256: `18BB67FC27BEA34137777EC00ACD0AD12B4FDF408537F3DA2E833CE48095BEF3`)
- `Bonsai_Tree_baseline.fbx` (SHA256: `52B63D9F9F555BBBC85302DA9BA3AA0E6F1BF82F4E193E070F5B1B8981D0B7D5`)

Các file `.meta` trong `Assets/_Project/Art/` (`Bonsai_Tree.blend.meta` và `Bonsai_Tree.fbx.meta`) được giữ nguyên vẹn 100%, bảo toàn GUID không đổi (`2a95f5e80a8ceee4d94452fe729f6459` và `85fb7c1216c01d846be38836a02e7cc0`).

---

## 2. Nguồn Áp Dụng (Candidate)
Dữ liệu candidate từ `ArtSource/Models/Bonsai_Trunk_Candidate_v01.blend` và `ArtSource/Exports/Bonsai_Trunk_Candidate_v01.fbx` đã được chuyển vào:
- `Assets/_Project/Art/Bonsai_Tree.blend` (SHA256: `B99A966A42BD2A44591C7BA007A3BB896482A013DC6A0F4C5F979A1A12C3FC66`)
- `Assets/_Project/Art/Bonsai_Tree.fbx` (SHA256: `802610722100C52F810E198C9DCF43A23267D31D30CF7AF38747ED6A3BF68644`)

---

## 3. Xác Minh Trong Phiên Blender Đang Chạy
- Lệnh nạp `bpy.ops.wm.open_mainfile` đã chuyển trực tiếp phiên Blender GUI (PID 17620) sang mở file:
  `c:\Users\Asus\foder game 3D\Cay_The_Gioi\Assets\_Project\Art\Bonsai_Tree.blend`
- Object `Trunk_Branches` được chọn và căn giữa màn hình Viewport với Solid Cavity Shading.
- Đỉnh và mặt: `19.332` vertices, `18.864` polygons, giữ nguyên `73.888` triangles toàn cây.
- Nhóm đỉnh `Trunk_Editable_Mask` hiện diện đầy đủ.
