# Báo cáo Kiểm chứng Model Cây Lớn v02 (Tán lá Đa tầng Shingled & Thân Khía Rãnh)

- **Ngày thực hiện**: 06/10/2026
- **Trạng thái**: Hoàn tất nâng cấp hình học, tán lá 3D, khía rãnh vỏ cây, kiểm chứng FBX & đa góc (T10 vẫn `IN_PROGRESS`)
- **Quyết định liên quan**: D32 (`Docs/Decisions.md`)
- **Nguồn yêu cầu**: Ảnh tham chiếu do người dùng cung cấp (`media_1791250903616.png`), hoàn thiện tán lá riêng biệt có chiều sâu và thân có nét vỏ cây nhẹ.

---

## 1. Mục tiêu & Chi tiết Nâng cấp Hình học

1. **Chuyển đổi toàn diện Tán lá (Foliage)**:
   - Loại bỏ hoàn toàn 7 khối guide xanh đặc ban đầu, thay thế bằng **483 lá 3D riêng biệt**.
   - Mỗi chiếc lá là một mesh khối khép kín có độ dày 3.5mm:
     - 3 thùy viền lượn sóng (scalloped lobes) tạo nét vẽ hoạt hình mềm mại.
     - Đỉnh vuốt nhọn, rãnh gân chính chữ V nâng mép lá.
     - Vòm cong và độ rủ nhẹ tự nhiên về phía đầu lá.
     - Cuống lá (petiole) dạng hình trụ kết nối trực tiếp vào cành phụ.
   - Cấu trúc ngói lợp (shingled multi-tier pads):
     - Lá được bố trí thành 7 tầng trên từng cụm tán (Chóp đỉnh, Tầng trên, Tầng vành ngang, Tầng skirt rủ, và Tầng lót che đáy).
     - Các tầng lá gối đầu lên nhau từ trên xuống dưới, che kín mặt trên và mặt bên của tán, tạo cảm giác mướt mát, dày dặn và bắt sáng từng mảng sống động theo reference.

2. **Khía rãnh Vỏ cây & Cành phụ (Trunk & Branches)**:
   - Thân chính và các cành lớn được khía 5 sống/rãnh nông (longitudinal flutes) xoắn nhẹ theo chiều uốn lượn chữ S của thân, tạo các thớ gỗ hữu cơ bắt sáng và đổ bóng sắc sảo dưới Cavity shading.
   - Bổ sung hệ thống cành phụ / cuống cành (secondary twigs) vươn vào lõi các cụm tán, kết nối vật lý 100% cuống lá vào thân gỗ; hoàn toàn không có lá bay lơ lửng.

3. **Bộ rễ bạnh (Roots)**:
   - Bổ sung khía rãnh thớ gỗ chạy dọc sống rễ bạnh.
   - 5 nhánh rễ bạnh tỏa đều 360° quanh gốc kèm các chạc vuốt ngắn, cắm sâu xuống đất.
   - Chuyển tiếp cổ rễ tại $z = 0.000\text{ m}$ trơn tru, liền mạch với chân thân.

---

## 2. Bảng Đối chiếu Đa giác & Kích thước (Trước và Sau Nâng cấp)

| Thông số / Đối tượng | Trước nâng cấp (Prompt 1) | Sau nâng cấp (Prompt 2) | Biến thiên / Đánh giá |
| :--- | :---: | :---: | :---: |
| **`Roots`** | 922 tris (588 verts) | **958 tris** (608 verts) | +36 tris (bổ sung thớ rãnh sống rễ) |
| **`Trunk_Branches`** | 706 tris (416 verts) | **1.408 tris** (858 verts) | +702 tris (5 rãnh thân xoắn + cành phụ) |
| **`Foliage`** | 6.298 tris (3.163 verts) | **44.436 tris** (27.048 verts) | 483 lá 3D riêng biệt, có thùy & độ dày |
| **TỔNG CỘNG** | **7.926 tris** | **46.802 tris** | **Nhẹ hơn 37% so với Cổ Thụ (73.888 tris)** |
| **Bề rộng trục X** | $2.719\text{ m}$ | **$3.177\text{ m}$** | Tán lá xòe rộng tự nhiên theo reference |
| **Bề sâu trục Y** | $1.430\text{ m}$ | **$1.967\text{ m}$** | Chiều sâu không gian 3D mở rộng |
| **Tỷ lệ Chiều sâu / Bề rộng ($Y/X$)** | $52.6\%$ | **$61.9\%$** | **ĐẠT mục tiêu tham khảo $60\% - 80\%$** |
| **Chiều cao trên cổ rễ ($z > 0$)** | $2.448\text{ m}$ | **$2.370\text{ m}$** | Tương đương, tán ôm gọn tự nhiên |
| **Chiều sâu rễ ($z < 0$)** | $0.335\text{ m}$ | **$0.335\text{ m}$** | Giữ nguyên gốc rễ |

---

## 3. Danh mục Minh chứng Kiểm tra (Evidence)

Thư mục lưu trữ: [`Evidence/Tree_Large_v01_Audit/`](file:///c:/Users/Asus/foder%20game%203D/Cay_The_Gioi/Evidence/Tree_Large_v01_Audit/)

### 3.1. Bộ ảnh kết xuất 7 góc nhìn chuẩn
- [Chính diện (Front)](file:///c:/Users/Asus/foder%20game%203D/Cay_The_Gioi/Evidence/Tree_Large_v01_Audit/preview_front.png): Tán lá dày dặn, các cụm lá xòe tầng đẹp mắt, thân vỏ khía rãnh tự nhiên.
- [Mặt sau (Back)](file:///c:/Users/Asus/foder%20game%203D/Cay_The_Gioi/Evidence/Tree_Large_v01_Audit/preview_back.png): Tán sau đầy đặn, không có khoảng trống trơ trọi.
- [Bên trái (Left)](file:///c:/Users/Asus/foder%20game%203D/Cay_The_Gioi/Evidence/Tree_Large_v01_Audit/preview_left.png): Thân uốn trục YZ rõ rệt, tán vươn trước/sau đạt chiều sâu $1.97\text{ m}$.
- [Bên phải (Right)](file:///c:/Users/Asus/foder%20game%203D/Cay_The_Gioi/Evidence/Tree_Large_v01_Audit/preview_right.png): Cụm tán tầng trung và ngọn phân bổ không gian sâu.
- [Trên xuống (Top-down)](file:///c:/Users/Asus/foder%20game%203D/Cay_The_Gioi/Evidence/Tree_Large_v01_Audit/preview_top_down.png): Các tầng lá lợp kín tán theo hình vòm tròn đầy, thấy rõ bố cục shingled tự nhiên.
- [Phối cảnh 3/4 trước (Perspective Front 3/4)](file:///c:/Users/Asus/foder%20game%203D/Cay_The_Gioi/Evidence/Tree_Large_v01_Audit/preview_perspective_front_3_4.png): Góc nhìn hero trong game, lá bắt sáng và đổ bóng tầng lớp sắc nét.
- [Phối cảnh 3/4 sau (Perspective Back 3/4)](file:///c:/Users/Asus/foder%20game%203D/Cay_The_Gioi/Evidence/Tree_Large_v01_Audit/preview_perspective_back_3_4.png).

### 3.2. Bộ ảnh xoay 360 độ Turntable (12 khung hình)
- Các khung hình từ [`turntable_00_000deg.png`](file:///c:/Users/Asus/foder%20game%203D/Cay_The_Gioi/Evidence/Tree_Large_v01_Audit/turntable/turntable_00_000deg.png) đến [`turntable_11_330deg.png`](file:///c:/Users/Asus/foder%20game%203D/Cay_The_Gioi/Evidence/Tree_Large_v01_Audit/turntable/turntable_11_330deg.png).

### 3.3. Kiểm chứng Reimport FBX
- [`reimport_fbx_perspective.png`](file:///c:/Users/Asus/foder%20game%203D/Cay_The_Gioi/Evidence/Tree_Large_v01_Audit/reimport_fbx_perspective.png): Xác nhận FBX nạp lại trên scene sạch hoàn toàn chuẩn xác, scale `(1,1,1)`, rotation `(0,0,0)`, hệ trục Unity `Y-up`, `-Z forward`.

### 3.4. Ảnh chụp thực tế phiên Blender GUI đang mở của người dùng
- [`gui_viewport_live_v2.png`](file:///c:/Users/Asus/foder%20game%203D/Cay_The_Gioi/Evidence/Tree_Large_v01_Audit/gui_viewport_live_v2.png): Cửa sổ Blender của người dùng đã nạp trực tiếp model mới nhất, sẵn sàng để xoay ngắm 360 độ.

---

## 4. Tình trạng Tích hợp & Quy trình

- Model được lưu đè trực tiếp tại đúng 2 file gốc: [`ArtSource/Models/Tree_Large.blend`](file:///c:/Users/Asus/foder%20game%203D/Cay_The_Gioi/ArtSource/Models/Tree_Large.blend) và [`ArtSource/Exports/Tree_Large.fbx`](file:///c:/Users/Asus/foder%20game%203D/Cay_The_Gioi/ArtSource/Exports/Tree_Large.fbx).
- Không tạo candidate, không tạo file copy có hậu tố.
- Giữ nguyên toàn bộ các file gameplay, prefab, scene và các stage khác.
- Task T10 vẫn ở trạng thái `IN_PROGRESS`.
