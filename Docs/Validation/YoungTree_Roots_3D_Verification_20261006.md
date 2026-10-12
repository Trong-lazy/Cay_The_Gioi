# Báo cáo Kiểm chứng Nâng cấp Hệ thống Rễ 3D Trục Y - Cây Non (Tree_Young_Simple_v01)

- **Ngày thực hiện**: 06/10/2026
- **Trạng thái**: Hoàn tất kiểm chứng hình học & xuất FBX (T10 vẫn `IN_PROGRESS`)
- **Quyết định liên quan**: D28, D29 (`Docs/Decisions.md`)
- **Yêu cầu trực tiếp**: Người dùng phản hồi "phần rễ của cây giờ vẫn quá thẳng ( chỉ ở trục z x tôi muốn một vài chiếc rễ phải hướng về phía trục y"

---

## 1. Đối chiếu Kích thước và Độ sâu Không gian 3D của Rễ (Roots Bounding Box)

| Trục tọa độ | Trước khi sửa (chủ yếu trục X-Z) | Sau khi sửa (Nâng cấp đa hướng 3D) | Chênh lệch / Đánh giá |
| :--- | :--- | :--- | :--- |
| **Trục X (Ngang)** | `[-0.255m, 0.245m]` (Rộng: `0.501m`) | `[-0.245m, 0.248m]` (Rộng: `0.494m`) | Tương đương, giữ độ vươn tỏa ngang |
| **Trục Y (Sâu Trước-Sau)** | `[-0.023m, 0.023m]` (Sâu: `0.045m`) | `[-0.226m, 0.222m]` (Sâu: `0.449m`) | **Tăng gấp 10 lần**, rễ có thể tích tròn 3D hoàn chỉnh |
| **Trục Z (Chiều sâu đất)** | `[-0.480m, 0.012m]` (Sâu: `0.492m`) | `[-0.480m, 0.013m]` (Sâu: `0.493m`) | Chuẩn xác, rễ cọc cắm sâu ~0.48m |

---

## 2. Chi tiết Cấu trúc Rễ mới

1. **Rễ cọc chính (Taproot)**:
   - Xuất phát từ cổ rễ $z = 0.012\text{ m}$ cắm thẳng xuống $z = -0.480\text{ m}$.
   - Có đường lượn chữ S hữu cơ trên cả trục X và trục Y (uốn nhẹ về phía $+Y$ ở đoạn trên và trôi nhẹ về $-Y$ ở đoạn dưới) trước khi thuôn nhọn về đầu rễ.
2. **Rễ vươn thẳng phía trước (+Y Axis)**:
   - Tách từ cổ rễ vươn mạnh ra phía trước tới $y = +0.222\text{ m}$, uốn cong cắm xuống đất tại $z = -0.175\text{ m}$.
   - Nhánh phụ rẽ sang hướng trước-phải (+X, +Y) đạt $y = +0.192\text{ m}$.
3. **Rễ vươn thẳng phía sau (-Y Axis)**:
   - Tách từ cổ rễ vươn mạnh ra phía sau tới $y = -0.226\text{ m}$, uốn cong cắm xuống đất tại $z = -0.180\text{ m}$.
   - Nhánh phụ rẽ sang hướng sau-trái (-X, -Y) đạt $y = -0.196\text{ m}$.
4. **Các rễ chéo bên (Diagonal 3D Lateral Roots)**:
   - Rễ chéo trước trái: $(-X, +Y)$ vươn ra $x = -0.245\text{ m}, y = +0.105\text{ m}$.
   - Rễ chéo sau phải: $(+X, -Y)$ vươn ra $x = +0.248\text{ m}, y = -0.108\text{ m}$.
   - Rễ tầng trung $(-X, -Y)$ và $(+X, +Y)$ xuất phát từ thân rễ ở độ sâu $z = -0.10\text{ m}$ đến $-0.13\text{ m}$.

---

## 3. Thống kê Đa giác (Triangles Count)

- **`Foliage`**: 1.638 vertices, 3.224 triangles (giữ nguyên hình học lá dày 3D từ Prompt 9).
- **`Trunk_Branches`**: 336 vertices, 578 triangles (giữ nguyên thân chữ S và cuống lá).
- **`Roots`**: 416 vertices, 728 triangles (tăng từ 564 tris lên 728 tris để mô tả các nhánh rễ 3D).
- **Tổng đa giác toàn cây**: **4.530 triangles** (đạt tiêu chuẩn `< 5.000` triangles).

---

## 4. Kiểm chứng Tái nạp FBX (FBX Reimport Audit)

- File kiểm chứng: `ArtSource/Exports/Tree_Young_Simple_v01.fbx`
- Scale: `(1.0, 1.0, 1.0)`
- Rotation: `(0.0, 0.0, 0.0)`
- Trục tọa độ: Unity chuẩn (`Y-up`, `-Z forward`)
- Tổng số đối tượng: 3 mesh objects (`Roots`, `Trunk_Branches`, `Foliage`).
- Chiều cao phần chồi trên mặt đất ($z > 0$): $0.838\text{ m}$.
- Chiều sâu rễ dưới mặt đất ($z < 0$): $0.480\text{ m}$.

---

## 5. Bằng chứng Hình ảnh Đa góc (Evidence)

Toàn bộ ảnh kết xuất kiểm tra được cập nhật tại `Evidence/YoungTree_v01_Audit/`:
- `preview_front.png`: Góc nhìn chính diện giữ vững bố cục 8 lá so le theo ảnh tham chiếu, rễ tỏa đều 2 bên có lớp trước và lớp sau.
- `preview_right.png` & `preview_left.png`: Nhìn từ cạnh bên thấy rõ các nhánh rễ vươn rộng về phía trước (+Y) và phía sau (-Y), giải quyết triệt để khuyết điểm "rễ thẳng/dẹt".
- `preview_top_down.png`: Tán rễ tỏa đều 360 độ hình ngôi sao bao quanh gốc cây.
- `preview_perspective_front_3_4.png` & `preview_perspective_back_3_4.png`: Thể hiện phối cảnh 3D vững chãi, chân thực.
- `reimport_fbx_perspective.png`: Kiểm chứng trực tiếp file FBX sau khi export.
