# Nghiệm Thu Tăng Chi Tiết Bề Mặt Gỗ Cổ Thụ (Roots, Trunk & Branches Surface Detail)

- **Ngày thực hiện:** 05/10/2026
- **Công cụ:** Blender MCP (Blender 5.2.2 LTS)
- **Asset files:** `Assets/_Project/Art/Bonsai_Tree.blend`, `Assets/_Project/Art/Bonsai_Tree.fbx`
- **Kết quả:** PASS (Đạt toàn bộ tiêu chí về vân rãnh, xoắn thân, biến dạng rễ, không tăng polycount và giữ nguyên silhouette)

---

## 1. Mục tiêu và phạm vi

Tăng chi tiết bề mặt cấp trung/lớn (medium/large surface detail) cho hệ thống gỗ: **Roots, Trunk và Branches**, tạo cảm giác cây cổ thụ ngàn năm:
1. Thân hơi xoắn (helical twist) theo chiều cao.
2. Các rãnh và gân múi lớn chạy dọc thân cây (longitudinal fluting & furrows).
3. Bề mặt gồ ghề hữu cơ (uneven bark waviness & burls).
4. Gốc và rễ có sống lưng nổi (dorsal crests), mắt u bướu (root knuckles) và biến dạng organic.
5. Giữ nguyên silhouette chính, không làm vỡ topology hay điểm nối Roots → Trunk → Branches.
6. Không tăng polycount cực nhỏ (dành micro detail cho normal map).

---

## 2. Thống kê Mesh & Đảm bảo Hiệu năng (Game-Ready)

| Object | Polycount trước | Polycount sau | Biến thiên | Kích thước Bounds (W x D x H) | Ghi chú kỹ thuật |
| :--- | :---: | :---: | :---: | :---: | :--- |
| `Roots` | 8,928 faces | 8,928 faces | **0%** | 6.85m x 5.70m x 1.27m | Thêm sống lưng nổi, nốt rễ organic, tiếp đất Z=0.00m |
| `Trunk_Branches` | 18,864 faces | 18,864 faces | **0%** | 6.21m x 3.67m x 3.73m | Thân xoắn 55°, 5 bó múi cơ + rãnh sâu, u bướu chạc cành |
| `Foliage` | 3,520 faces | 3,520 faces | **0%** | 8.09m x 5.16m x 3.33m | Giữ nguyên 22 khối tán mây kín |
| **Tổng thể cây** | **31,312 faces** | **31,312 faces** | **0%** | **8.09m x 5.70m x 4.32m** | **Tối ưu 100% chuẩn Unity Mobile & PC** |

---

## 3. Danh sách ảnh chụp kiểm tra (Evidence Screenshots)

Đã lưu trữ toàn bộ ảnh kiểm tra cận cảnh và toàn cảnh tại `Evidence/Model_Detail_Surface/`:

1. `detailed_closeup_roots_trunk.png`: Cận cảnh gốc thân và bệ rễ — thấy rõ các múi cơ xoắn, rãnh xẻ dọc thân và sống rễ nổi cuồn cuộn.
2. `detailed_closeup_branches.png`: Cận cảnh thân trên và chạc cành — thấy thớ gỗ hữu cơ và các múi cơ uốn lượn nối vào cành.
3. `detailed_full_front.png`: Toàn cảnh chính diện — giữ nguyên silhouette Bonsai tam giác hài hòa.
4. `detailed_full_back.png`: Toàn cảnh phía sau — vân rãnh phủ đều 360°.
5. `detailed_full_left.png`: Toàn cảnh mạn trái — chiều sâu Y đầy đặn.
6. `detailed_full_right.png`: Toàn cảnh mạn phải — bệ rễ gân guốc bám chặt nền.
7. `detailed_full_persp34.png`: Toàn cảnh phối cảnh 3/4 — độ sâu và dáng vươn cổ thụ hùng vĩ.

---

## 4. Kết quả đối chiếu Acceptance Criteria

| Tiêu chí | Kết quả | Chi tiết kiểm chứng |
| :--- | :---: | :--- |
| **Thân hơi xoắn (Helical Twist)?** | **ĐẠT** | Bó cơ thân xoắn góc ~55° từ Z=0.2m đến Z=2.6m, tạo lực căng vặn xoắn tự nhiên của cây cổ thụ. |
| **Các rãnh lớn chạy dọc thân (Fluting Grooves)?** | **ĐẠT** | 5 rãnh cơ chính (amplitude 6.5cm) cùng hệ rãnh phụ đan xen, tạo khe rãnh vỏ cây sâu rõ nét. |
| **Bề mặt không đều (Uneven organic surface)?** | **ĐẠT** | Sử dụng anisotropic multi-fractal noise mô phỏng thớ gỗ dọc và các u bướu (burls) lồi lõm 2-3cm. |
| **Gốc và rễ biến dạng organic?** | **ĐẠT** | Sống lưng rễ nổi (dorsal crests), mắt rễ u nần, mặt đáy rễ vẫn áp sát mặt phẳng đất Z=0.00m. |
| **Bark detail nhìn thấy nhưng polycount không tăng quá cao?** | **ĐẠT** | Toàn bộ chi tiết được biến dạng trực tiếp trên lưới ~5cm có sẵn; số polycount giữ nguyên đúng 31,312 faces. |
| **Giữ nguyên silhouette chính?** | **ĐẠT** | Bounds toàn cây thay đổi < 1cm; silhouette Bonsai kinh điển không bị biến dạng. |
| **Không phá topology hay điểm nối Roots → Trunk → Branches?** | **ĐẠT** | Topology không sinh thêm lỗ hở; 42 đầu cành vẫn nằm ẩn kín trong lòng cụm tán; cổ rễ ôm khít gốc thân. |
