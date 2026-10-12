# Báo cáo Khắc phục Lỗi Cổ Rễ & Điểm Nối Rễ Cây Non (Tree_Young_Simple_v01)

- **Ngày thực hiện**: 06/10/2026
- **Trạng thái**: Hoàn tất sửa chữa & kiểm chứng (T10 vẫn `IN_PROGRESS`)
- **Quyết định liên quan**: D30 (`Docs/Decisions.md`)
- **Nguồn yêu cầu**: Ảnh người dùng cung cấp (`media_1791245376098.png`) phản ánh 3 lỗi ở vùng cổ rễ và điểm nối rễ.

---

## 1. Nguyên nhân & Giải pháp Khắc phục

### Lỗi 1: Nhánh rễ trôi nổi trong không khí (Floating Root Segment)
- **Hiện tượng**: Một ống rễ phía bên trái rễ cọc bị cắt cụt và trôi nổi cách xa rễ cọc ~2.7 cm.
- **Nguyên nhân**: Điểm gốc của nhánh `root_mid_left_b` được đặt cố định tại `(-0.004, -0.006, -0.100)`, nhưng rễ cọc ở độ sâu $z = -0.100\text{ m}$ đã uốn lượn hữu cơ về phía trước tới `(0.008, 0.018, -0.100)`. Do đó, điểm gốc nằm trôi nổi ngoài vỏ rễ cọc.
- **Giải pháp**:
  - Dùng hàm nội suy trục rễ cọc `interp_spine` để lấy chính xác tọa độ tâm rễ cọc tại độ sâu $z = -0.100\text{ m}$.
  - Đặt điểm xuất phát $p_0$ cắm sâu vào tâm rễ cọc.
  - Thiết lập `cap_start=False` để ống rễ mở nắp bên trong lõi, vươn ra ngoài mượt mà 100%.

### Lỗi 2: Nếp thắt / vết cắn trên vỏ rễ cọc (Pinched Notch / Crease)
- **Hiện tượng**: Nhánh rễ bên phải `root_mid_right_f` ở độ sâu $z = -0.135\text{ m}$ xuất hiện một nếp gấp nhăn/vết khoét sắc nhọn trên bề mặt rễ cọc.
- **Nguyên nhân**: Vòng đỉnh đầu của nhánh cắt chéo qua mặt vỏ rễ cọc ở góc nghiêng hẹp, tạo giao cắt mặt không đều và shading crease.
- **Giải pháp**:
  - Đặt điểm gốc $p_0$ nằm sâu trong tâm trục rễ cọc tại $z = -0.135\text{ m}$.
  - Thiết lập vector thoát nhánh theo hướng mượt dần ra ngoài, loại bỏ nắp đỉnh `cap_start=False`.

### Lỗi 3: Gờ vành lồi ở cổ rễ (Stepped Collar Ring / Lip)
- **Hiện tượng**: Tại điểm giao giữa chân thân xanh và rễ (vị trí con trỏ 3D), xuất hiện một chiếc gờ vành hình tròn lồi ra như một chiếc vòng đệm / khớp nối ống nước.
- **Nguyên nhân**: Đỉnh mesh `Roots` cao hơn $z=0$ (lên tới $z=0.012\text{ m}$) và có bán kính $0.0160\text{ m}$ với nắp phẳng (`cap_start=True`), trong khi chân thân `Trunk_Branches` có bán kính chỉ $0.0145\text{ m}$ tại $z=0.000\text{ m}$, tạo ra một bậc thang $1.5\text{ mm}$ nhô ra xung quanh thân.
- **Giải pháp**:
  - Đồng bộ bán kính thân và rễ tại $z = 0.000\text{ m}$ đúng bằng $0.0145\text{ m}$.
  - Cho chân thân lồng nhẹ xuống dưới mặt đất $z = -0.008\text{ m}$ (`cap_start=False`).
  - Cho đỉnh rễ bắt đầu từ $z = 0.002\text{ m}$ (`cap_start=False`) và loe dần xuống $z = -0.022\text{ m}$ ($r = 0.0162\text{ m}$).
  - Chuyển tiếp giữa thân xanh và rễ kem trở nên hoàn toàn liền mạch, không còn bất kỳ gờ hay bậc nào.

---

## 2. Thống kê Đa giác

- `Foliage`: 1.638 vertices, 3.224 triangles.
- `Trunk_Branches`: 346 vertices, 590 triangles.
- `Roots`: 436 vertices, 760 triangles.
- **Tổng đa giác**: **4.574 triangles** (đạt ngân sách `< 5.000` tris).

---

## 3. Ảnh Minh chứng Kiểm tra

- **Ảnh cận cảnh góc chụp đối chiếu**: [`Evidence/YoungTree_v01_Audit/closeup_root_junction_fixed.png`](file:///c:/Users/Asus/foder%20game%203D/Cay_The_Gioi/Evidence/YoungTree_v01_Audit/closeup_root_junction_fixed.png) (xác nhận cả 3 lỗi đã được xử lý triệt để).
- **Kiểm chứng FBX Re-import**: [`Evidence/YoungTree_v01_Audit/reimport_fbx_perspective.png`](file:///c:/Users/Asus/foder%20game%203D/Cay_The_Gioi/Evidence/YoungTree_v01_Audit/reimport_fbx_perspective.png).
- Toàn bộ 7 góc nhìn đã được kết xuất và cập nhật lại đồng bộ trong thư mục `Evidence/YoungTree_v01_Audit/`.
