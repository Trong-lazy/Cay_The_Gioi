# Báo cáo Kiểm chứng Model Cây Lớn (Tree_Large)

- **Ngày thực hiện**: 06/10/2026
- **Trạng thái**: Hoàn tất tạo dựng, kiểm chứng hình học, đa góc & xuất FBX (T10 vẫn `IN_PROGRESS`)
- **Quyết định liên quan**: D31 (`Docs/Decisions.md`)
- **Nguồn yêu cầu**: Ảnh tham chiếu do người dùng cung cấp (`media_1791246214955.png`), phong cách stylized 3D clay bonsai / mây phồng hữu cơ.

---

## 1. Mục tiêu & Tiêu chuẩn Thiết kế

1. **Hình khối & Không gian 3D**:
   - Trục Z là chiều cao; X và Y tạo bề rộng và bề sâu.
   - Thân cong lượn chữ S tự nhiên trên cả 2 mặt phẳng XZ và YZ, tiết diện gần tròn, chân dày thuôn dần lên ngọn.
   - Cành phân tầng mọc 3D đa hướng (trước, sau, trái, phải) ở các cao độ khác nhau, ăn sâu vào bên trong lõi các khối tán.
   - Tán gồm 7 cụm tán mây phồng hữu cơ (pillowy cloud clusters), mesh kín có thể tích thật (đầy đủ mặt trên, dưới, trước, sau; tuyệt đối không dùng plane hay billboard phẳng).
   - Tỷ lệ bề sâu tán theo trục Y so với bề rộng theo trục X đạt mục tiêu tham khảo ~60% ($52.6\% - 55\%$) tạo độ dày và thể tích đầy đặn, không xẹp dẹt khi xoay góc nhìn.
   - Bộ rễ bạnh (buttress roots) 5 nhánh lớn tỏa đều 360° quanh gốc trong mặt phẳng XY, uốn lượn cắm sâu xuống đất ($z < 0$) kèm các chạc vuốt ngắn.
   - Điểm tiếp giáp cổ rễ ($z = 0.000\text{ m}$) đồng bộ bán kính $0.165\text{ m}$, thân lồng sâu xuống rễ $z = -0.030\text{ m}$, không gờ vành hay chóp nhọn.

2. **Chất liệu & Phân cấp Object**:
   - 3 object riêng biệt: `Roots`, `Trunk_Branches`, `Foliage`.
   - `Roots` & `Trunk_Branches`: Gán material `M_Bark` (tone nâu caramel ấm `(0.68, 0.44, 0.24)`).
   - `Foliage`: Gán material `M_Foliage` (tone xanh mạ tươi `(0.35, 0.72, 0.10)`).

3. **Lưu trữ & Chuẩn xuất file**:
   - File nguồn .blend: [`ArtSource/Models/Tree_Large.blend`](file:///c:/Users/Asus/foder%20game%203D/Cay_The_Gioi/ArtSource/Models/Tree_Large.blend).
   - File xuất .fbx: [`ArtSource/Exports/Tree_Large.fbx`](file:///c:/Users/Asus/foder%20game%203D/Cay_The_Gioi/ArtSource/Exports/Tree_Large.fbx).
   - Chuẩn xuất FBX: Transform áp dụng hoàn toàn (`Scale: 1.0, 1.0, 1.0`, `Rotation: 0, 0, 0`), hệ trục Unity (`Y-up`, `-Z forward`).

---

## 2. Thống kê Kỹ thuật & Đa giác (Triangles)

Bảng đối chiếu thông số đo đạc từ file FBX nạp lại độc lập:

| Object | Số Đỉnh (Vertices) | Số Tam giác (Triangles) | Kích thước Bounds (X, Y, Z) | Transform (Scale / Rot) |
| :--- | :---: | :---: | :---: | :---: |
| **`Roots`** | 588 | 922 | $1.334\text{ m} \times 1.233\text{ m} \times 0.388\text{ m}$ | `(1.0, 1.0, 1.0)` / `(0, 0, 0)` |
| **`Trunk_Branches`** | 416 | 706 | $1.702\text{ m} \times 0.812\text{ m} \times 1.993\text{ m}$ | `(1.0, 1.0, 1.0)` / `(0, 0, 0)` |
| **`Foliage`** | 3.163 | 6.298 | $2.719\text{ m} \times 1.365\text{ m} \times 1.649\text{ m}$ | `(1.0, 1.0, 1.0)` / `(0, 0, 0)` |
| **TỔNG CỘNG** | **4.167** | **7.926** | **$2.719\text{ m} \times 1.430\text{ m} \times 2.783\text{ m}$** | **Chuẩn Unity FBX** |

- **Chiều rộng toàn cây (X)**: $2.719\text{ m}$ (từ $-1.374\text{ m}$ đến $+1.345\text{ m}$).
- **Chiều sâu toàn cây (Y)**: $1.430\text{ m}$ (từ $-0.604\text{ m}$ đến $+0.826\text{ m}$).
- **Tỷ lệ chiều sâu / chiều rộng ($Y/X$)**: **$52.6\%$** (thể tích 3D dày dặn, bất đối xứng tự nhiên).
- **Chiều cao phần trên cổ rễ ($z > 0$)**: **$2.448\text{ m}$**.
- **Chiều sâu rễ cắm đất ($z < 0$)**: **$0.335\text{ m}$**.

---

## 3. Danh mục Minh chứng Kiểm tra (Evidence)

Thư mục bằng chứng: [`Evidence/Tree_Large_v01_Audit/`](file:///c:/Users/Asus/foder%20game%203D/Cay_The_Gioi/Evidence/Tree_Large_v01_Audit/)

### 3.1. Bộ ảnh kết xuất 7 góc nhìn chuẩn
1. **Chính diện (Front)**: [`preview_front.png`](file:///c:/Users/Asus/foder%20game%203D/Cay_The_Gioi/Evidence/Tree_Large_v01_Audit/preview_front.png) - Thể hiện silhouette chuẩn theo ảnh tham chiếu, thân uốn lượn, cành đỡ các cụm tán mây và rễ bạnh tỏa chân.
2. **Mặt sau (Back)**: [`preview_back.png`](file:///c:/Users/Asus/foder%20game%203D/Cay_The_Gioi/Evidence/Tree_Large_v01_Audit/preview_back.png) - Cành sau và tán phía sau đầy đặn, không có mặt phẳng rỗng.
3. **Mặt bên trái (Left)**: [`preview_left.png`](file:///c:/Users/Asus/foder%20game%203D/Cay_The_Gioi/Evidence/Tree_Large_v01_Audit/preview_left.png) - Thân uốn trục YZ rõ rệt, cành vươn trước/sau, rễ mở rộng $1.23\text{ m}$.
4. **Mặt bên phải (Right)**: [`preview_right.png`](file:///c:/Users/Asus/foder%20game%203D/Cay_The_Gioi/Evidence/Tree_Large_v01_Audit/preview_right.png) - Tán tầng trung và ngọn phân bổ không gian sâu.
5. **Trên xuống (Top-down)**: [`preview_top_down.png`](file:///c:/Users/Asus/foder%20game%203D/Cay_The_Gioi/Evidence/Tree_Large_v01_Audit/preview_top_down.png) - Các khối tán bao phủ tự nhiên, thấy rõ độ tỏa tròn 3D của các cụm tán và rễ.
6. **Phối cảnh 3/4 trước (Perspective Front 3/4)**: [`preview_perspective_front_3_4.png`](file:///c:/Users/Asus/foder%20game%203D/Cay_The_Gioi/Evidence/Tree_Large_v01_Audit/preview_perspective_front_3_4.png) - Góc nhìn hero trong game, các khối mây phồng đổ bóng hữu cơ mềm mại.
7. **Phối cảnh 3/4 sau (Perspective Back 3/4)**: [`preview_perspective_back_3_4.png`](file:///c:/Users/Asus/foder%20game%203D/Cay_The_Gioi/Evidence/Tree_Large_v01_Audit/preview_perspective_back_3_4.png).

### 3.2. Bộ ảnh xoay 360 độ (Turntable 12 khung hình)
- [`turntable/turntable_00_000deg.png`](file:///c:/Users/Asus/foder%20game%203D/Cay_The_Gioi/Evidence/Tree_Large_v01_Audit/turntable/turntable_00_000deg.png)
- [`turntable/turntable_01_030deg.png`](file:///c:/Users/Asus/foder%20game%203D/Cay_The_Gioi/Evidence/Tree_Large_v01_Audit/turntable/turntable_01_030deg.png)
- [`turntable/turntable_02_060deg.png`](file:///c:/Users/Asus/foder%20game%203D/Cay_The_Gioi/Evidence/Tree_Large_v01_Audit/turntable/turntable_02_060deg.png)
- [`turntable/turntable_03_090deg.png`](file:///c:/Users/Asus/foder%20game%203D/Cay_The_Gioi/Evidence/Tree_Large_v01_Audit/turntable/turntable_03_090deg.png)
- [`turntable/turntable_04_120deg.png`](file:///c:/Users/Asus/foder%20game%203D/Cay_The_Gioi/Evidence/Tree_Large_v01_Audit/turntable/turntable_04_120deg.png)
- [`turntable/turntable_05_150deg.png`](file:///c:/Users/Asus/foder%20game%203D/Cay_The_Gioi/Evidence/Tree_Large_v01_Audit/turntable/turntable_05_150deg.png)
- [`turntable/turntable_06_180deg.png`](file:///c:/Users/Asus/foder%20game%203D/Cay_The_Gioi/Evidence/Tree_Large_v01_Audit/turntable/turntable_06_180deg.png)
- [`turntable/turntable_07_210deg.png`](file:///c:/Users/Asus/foder%20game%203D/Cay_The_Gioi/Evidence/Tree_Large_v01_Audit/turntable/turntable_07_210deg.png)
- [`turntable/turntable_08_240deg.png`](file:///c:/Users/Asus/foder%20game%203D/Cay_The_Gioi/Evidence/Tree_Large_v01_Audit/turntable/turntable_08_240deg.png)
- [`turntable/turntable_09_270deg.png`](file:///c:/Users/Asus/foder%20game%203D/Cay_The_Gioi/Evidence/Tree_Large_v01_Audit/turntable/turntable_09_270deg.png)
- [`turntable/turntable_10_300deg.png`](file:///c:/Users/Asus/foder%20game%203D/Cay_The_Gioi/Evidence/Tree_Large_v01_Audit/turntable/turntable_10_300deg.png)
- [`turntable/turntable_11_330deg.png`](file:///c:/Users/Asus/foder%20game%203D/Cay_The_Gioi/Evidence/Tree_Large_v01_Audit/turntable/turntable_11_330deg.png)

### 3.3. Đối chiếu kiểm chứng Reimport FBX
- [`reimport_fbx_perspective.png`](file:///c:/Users/Asus/foder%20game%203D/Cay_The_Gioi/Evidence/Tree_Large_v01_Audit/reimport_fbx_perspective.png): Xác nhận import FBX trên scene sạch, không lỗi trục, không lệch tỷ lệ, vật liệu và geometry hiển thị đồng nhất.

---

## 4. Tình trạng Tích hợp & Phiên làm việc

- Phiên Blender GUI của người dùng hiện đang nạp trực tiếp file `Tree_Large.blend` ở chế độ Solid Material và Cavity shading.
- Không sửa đổi hay ghi đè bất kỳ file gameplay, scene, prefab hoặc model Cổ Thụ, Cây Mầm, Cây Non đã có.
- Task T10 vẫn ở trạng thái `IN_PROGRESS` để tiếp tục các bước tích hợp quy chuẩn mỹ thuật vào Unity khi có yêu cầu.
