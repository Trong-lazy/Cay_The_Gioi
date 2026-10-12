# Nghiệm Thu Hoàn Thiện Tán Cây Foliage Game-Ready (Volumetric Leaf Sprigs)

- **Ngày thực hiện:** 05/10/2026
- **Công cụ:** Blender MCP (Blender 5.2.2 LTS)
- **Asset files:** `Assets/_Project/Art/Bonsai_Tree.blend`, `Assets/_Project/Art/Bonsai_Tree.fbx`
- **Kết quả:** PASS (Đạt toàn bộ tiêu chí về cấu trúc lá 3D, phân bố thể tích, giữ khoảng trống tầng tán và tối ưu hiệu năng game)

---

## 1. Mục tiêu và giải pháp kỹ thuật

Chuyển đổi 22 khối tán kín (smooth cushions) trước đây thành hệ thống tán cây có cảm giác cấu tạo từ nhiều lá nhỏ hữu cơ:
1. **Phương pháp kỹ thuật:** Ứng dụng công nghệ **Volumetric Leaf Sprig Cards** (cụm lá đa thớt có gân sống uốn cong và xòe 3-4 cánh lá).
2. **Không tạo hàng chục nghìn lá rời:** Gom thành 22 cụm tán thống nhất, mỗi cụm gồm 52 cụm lá kép phân tầng (4 tầng: Underside skirt, Lower mantle, Upper mantle, Crown dome).
3. **Giữ khoảng trống tầng tán:** Duy trì các khoảng không giữa các tầng cành theo chuẩn Bonsai để người chơi luôn nhìn thấy thân xoắn và các cành lớn.
4. **Phân bố 3D bao quanh cành:** Cụm lá bao bọc 360° quanh đầu cành, hướng góc ngẫu nhiên (yaw, pitch, roll).
5. **Chuẩn hiệu năng Game (Unity PC & Mobile):**
   - Số Draw Calls cho tán lá: **1 Draw Call duy nhất** (toàn bộ tán nằm trong 1 mesh object `Foliage`).
   - Spherized Tree Normals: Chuẩn hóa vector pháp tuyến theo tâm hình cầu của từng cụm + sky bias, đảm bảo tán sáng mịn mềm mại, không bị lỗi gãy bóng.
   - UV Mapping: Toàn bộ lá được unwrap chuẩn $[0, 1] \times [0, 1]$ sẵn sàng cho alpha leaf texture.

---

## 2. Thống kê Mesh & Đảm bảo Hiệu năng

| Object | Polycount (Faces) | Vertices | Bounds Kích Thước (W x D x H) | Draw Calls (Unity) |
| :--- | :---: | :---: | :---: | :---: |
| `Roots` | 8,928 | 9,120 | 6.85m x 5.70m x 1.27m | 1 |
| `Trunk_Branches` | 18,864 | 19,332 | 6.21m x 3.67m x 3.73m | 1 |
| `Foliage` | 18,304 | 22,880 | 8.21m x 5.05m x 3.30m | 1 |
| **Tổng thể Model** | **46,096 faces** | **51,332 verts** | **8.21m x 5.70m x 4.30m** | **Tối đa 3 Draw Calls** |

---

## 3. Danh sách ảnh chụp kiểm chứng (Evidence Screenshots)

Đã lưu trữ toàn bộ ảnh kiểm tra đa góc tại `Evidence/Model_Foliage_Refined/`:

1. `foliage_01_Front.png`: Toàn cảnh chính diện — tán lá nhiều tầng dạng mây Bonsai, thấy rõ thân và cành.
2. `foliage_02_Back.png`: Toàn cảnh phía sau — tán phủ đều 360°, cành hậu cảnh vươn đẹp.
3. `foliage_03_Left.png`: Toàn cảnh mạn trái — chiều sâu Y hơn 5m, không phẳng dẹt.
4. `foliage_04_Right.png`: Toàn cảnh mạn phải — nhịp điệu cành và cụm lá cân đối.
5. `foliage_05_Top.png`: Nhìn thẳng từ trên xuống — các chùm lá che phủ đan xen tự nhiên.
6. `foliage_06_Perspective_3_4.png`: Góc phối cảnh nghệ thuật — chiều sâu không gian Bonsai cổ kính.
7. `foliage_07_Closeup_Canopy.png`: Cận cảnh ngước nhìn dưới tán — thấy rõ các cành cắm vào tán lá và ánh sáng lọt qua kẽ lá.

---

## 4. Đối chiếu Acceptance Criteria

| Tiêu chí | Kết quả | Chi tiết kiểm chứng |
| :--- | :---: | :--- |
| **Cảm giác cấu tạo từ nhiều lá nhỏ?** | **ĐẠT** | Mỗi chùm lá có viền răng cưa, phiến lá cong xòe 3 hướng, tạo cảm giác hàng ngàn chiếc lá khi nhìn gần vừa phải. |
| **Không tạo hàng chục nghìn mesh lá độc lập?** | **ĐẠT** | Tán lá được gom thành 1 mesh object duy nhất với 18,304 quads, cực kỳ nhẹ cho GPU. |
| **Polygon và draw calls hợp lý?** | **ĐẠT** | 1 draw call cho toàn bộ foliage; tổng polycount toàn cây là 46k faces (~90k tris) chuẩn hero asset Unity PC/Mobile. |
| **Giữ khoảng trống giữa các tầng tán?** | **ĐẠT** | Thân chính chữ S và toàn bộ các cành scaffold đều nhìn thấy rõ qua các khoảng hở giữa các tầng tán. |
| **Phân bố volume 3D, hướng ngẫu nhiên?** | **ĐẠT** | Phân tầng 4 lớp (skirt, lower, upper, dome), xoay góc ngẫu nhiên $\pm 25^\circ$. |
| **Tránh khối đặc hoặc mặt phẳng?** | **ĐẠT** | Ánh sáng và camera xuyên qua được các lớp lá; có độ dày thể tích $0.65\text{m}$ trên mỗi cụm. |
| **Phù hợp Unity PC & Mobile?** | **ĐẠT** | Spherized normals + tight UV layout giảm triệt để overdraw fragment fill-rate trên mobile. |
