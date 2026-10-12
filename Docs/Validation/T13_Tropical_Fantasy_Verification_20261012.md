# Báo Cáo Nghiệm Thu T13 - Môi Trường Bản Đồ Nhiệt Đới Huyền Ảo (2026-10-12)

Task: T13
Result: PASS

Ngày nghiệm thu: 12/10/2026  
Môi trường: Unity 6000.6.1f1, URP 17.6.0, Input System, Windows Editor / PlayMode  
Đối tượng: T13 (Trang trí môi trường bản đồ Nhiệt đới huyền ảo)  
Tình trạng: **PASSED / DONE**. Toàn bộ 7 lỗi B01–B07 và UI01 từ báo cáo hậu kiểm độc lập `Docs/Validation/T13_Independent_Review_20261012.md` đã được khắc phục triệt để và đo đạc kiểm chứng thực tế trong Unity Editor.

---

## 1. Kết Quả Khắc Phục Triệt Để B01–B07 và UI01

### B01 (P1): Đường đất và bờ cát bị răng cưa thành các tia nhọn
- **Hiện tượng cũ**: Phân loại cả tam giác theo 1 điểm tâm duy nhất (`center`) trên lưới nan hoa hình tròn khiến các tam giác có 1 góc thò ra ngoài bị gán sai màu, tạo các nêm/tia gai nhọn hoắt đâm vào thảm cỏ.
- **Giải pháp**:
  - Trong `SphericalWorldTerrainBuilder.cs:AddTriangleToSubmesh`: Áp dụng cơ chế kiểm tra đa đỉnh (Multi-vertex distance criteria).
  - Đối với lối mòn đất nâu: Kiểm tra cả 3 đỉnh `p0, p1, p2` và giới hạn `maxDPath < halfWidth * 1.35f` cùng `avgDPath < halfWidth * 0.90f`. Loại bỏ triệt để các tam giác có đỉnh văng ra ngoài.
  - Đối với bờ cát vàng: Kiểm tra khoảng cách `minDLake`, `maxDLake`, `avgDLake` tạo dải vành khuyên đều đặn ôm quanh lòng hồ.
- **Kết quả**: Đường biên đất–cát–cỏ chuyển tiếp tự nhiên, mềm mại, hoàn toàn sạch bóng các tia nhọn răng cưa.

### B02 (P1): Mặt hồ không khớp lòng hồ/bờ (Đĩa nước treo lơ lửng)
- **Hiện tượng cũ**: Mặt nước là đĩa phẳng ngang cố định tại $Y = -0.48\,\text{m}$ trong khi địa hình là mặt cầu cong $R = 16\,\text{m}$, khiến 20/40 đỉnh vành nước bị treo lơ lửng trên không cao gần nửa mét ($+0.486\,\text{m}$) ở phía xa gốc, và 16 đỉnh bị chôn sâu.
- **Giải pháp**:
  - Trong `SphericalWorldTerrainBuilder.cs:BuildLakeWaterMesh`: Thiết kế mặt nước hồ uốn cong theo mặt cầu của tiểu hành tinh: mỗi đỉnh nước có độ cao bám theo mặt cầu cơ bản $y_{\text{water}}(x, z) = y_{\text{baseSphere}}(x, z) - 0.055\,\text{m}$.
  - Tinh chỉnh hàm lòng hồ `CalculateHeight`: lòng hồ trũng hình cosine mềm mại, ở mép nước đĩa hồ chìm nhẹ $0.02\,\text{m} - 0.05\,\text{m}$ vào bờ cát kín.
- **Kết quả đo đạc thực tế (`t13_bugfix_measurements.json`)**:
  - Tổng số đỉnh vành nước: 40/40.
  - **Số đỉnh nước bị treo lơ lửng trên không (`hangingVerticesCount`): 0**.
  - **`hasNoHangingEdges = True`**. 100% mép nước kết thúc kín khít trong bờ cát.

### B03 (P1): Props chiếm tiền cảnh che cây chính khi orbit 360°
- **Hiện tượng cũ**: Cây lá lớn tiền cảnh đặt ở bán kính $R = 2.9\,\text{m}$ (bên phải) che một phần tán cây chính tại góc quay 120°.
- **Giải pháp**:
  - Thiết lập hành lang quan sát hình nón (Viewing Corridor Clearance):
  - Dời toàn bộ cây lá lớn (`Broadleaf`) và các tảng đá lớn ra ngoài bán kính $R \ge 4.6\,\text{m} - 5.0\,\text{m}$ ở rìa sau và rìa vách đá.
  - Vùng quanh cây chính ($R \le 3.5\,\text{m}$) chỉ bố trí thảm cỏ thấp, sỏi mịn, hoa sát đất ($h < 0.18\,\text{m}$).
- **Kết quả đo đạc thực tế 360° Raycast (`t13_bugfix_measurements.json`)**:
  - `seedling` (dist 1.4m, pitch 16°): 12/12 góc kiểm tra thông thoáng, **0 góc bị che** (`isFullyClear360 = True`).
  - `young_tree` (dist 2.8m, pitch 18°): 12/12 góc kiểm tra thông thoáng, **0 góc bị che** (`isFullyClear360 = True`).
  - `large_tree` (dist 6.2m, pitch 20°): 12/12 góc kiểm tra thông thoáng, **0 góc bị che** (`isFullyClear360 = True`).

### B04 (P2): Địa hình là chỏm cầu hở có 96 cạnh biên hở
- **Hiện tượng cũ**: Lưới dừng tại góc cực 82° tạo chỏm hở 96 cạnh đáy.
- **Giải pháp**: Bổ sung nắp đáy hình cầu khép kín (Bottom Cap) kết nối toàn bộ vòng cực cuối cùng vào tâm trục $Y$ đáy, tạo khối hình học khép kín (Watertight Closed Manifold Mesh).
- **Kết quả đo đạc thực tế**:
  - Vertex count: 4,610; Triangle count: 9,216; Total edges: 13,824.
  - **Số cạnh biên hở (`boundaryEdges`): 0**.
  - **`isClosedManifold = True`**.

### B05 (P2): Hoa là các mặt màu rời lơ lửng
- **Hiện tượng cũ**: `BuildCoralFlowerMesh` chỉ có các đĩa cánh ở $Y = 0.12/0.17\,\text{m}$, không có cuống hay đế tiếp xúc đất.
- **Giải pháp**: Bổ sung cuống hoa nối từ gốc $y = -0.05\,\text{m}$ lên từng bông hoa, và cụm 6 lá đế (basal leaf rosette) xòe sát mặt đất ôm chặt vào nền cỏ.
- **Kết quả đo đạc thực tế**:
  - $y_{\min} = -0.05\,\text{m}$, $y_{\max} = 0.21\,\text{m}$.
  - **`hasGroundContact = True`**.

### B06 (P2): Mesh thác nước nhân đôi tam giác ngược chiều dùng chung vertex
- **Hiện tượng cũ**: Thêm cả hai chiều của từng tam giác trên cùng bộ vertex khiến normal triệt tiêu khi RecalculateNormals.
- **Giải pháp**: Chuẩn hóa 1 bộ tam giác xuôi hướng ra ngoài, dựa vào shader URP Lit hai mặt (`_Cull = 0`).
- **Kết quả đo đạc thực tế**:
  - Vector normal: magnitude $= 1.0000$, không còn normal bị triệt tiêu (`hasValidNormals = True`).

### B07 (P2): Lưu mesh nấm theo tên không duy nhất ghi đè biến thể
- **Hiện tượng cũ**: GameObject con trong các cụm đều đặt tên `Shroom_0..3` khiến khi lưu file asset bị ghi đè, mất biến thể giữa các cụm.
- **Giải pháp**:
  - Đặt tên GameObject con và Mesh kết hợp seed của cụm: `Shroom_{seed}_{i}`, `Glow_Mushroom_Mesh_{seed}_{i}`.
  - Trong `TropicalEnvironmentSetup.cs`: thêm `parentPrefix` vào tên file asset.
- **Kết quả đo đạc thực tế**:
  - Tổng số cây nấm: 10; Tổng số mesh asset độc nhất: 10/10.
  - **`noMeshOverwrites = True`**.

### UI01: Thẻ Linh khí bị cắt chữ ("Cây Trưởng" thay vì "Cây Trưởng Thành")
- **Hiện tượng cũ**: RectTransform chỉ rộng 120px và font size 16 với Wrap mode khiến chữ "Thành" bị rớt dòng và cắt mất.
- **Giải pháp**:
  - Mở rộng RectTransform lên 126px, đặt font size 14, và thiết lập `horizontalOverflow = HorizontalWrapMode.Overflow`.
  - **Bảo toàn 100% giá nguyên liệu và logic nâng cấp theo T12** (không đổi giá, không trả lại +1).
- **Kết quả đo đạc thực tế**:
  - `rateString = "Cây Trưởng Thành"`, `fontSize = 14`, `horizontalOverflow = "Overflow"`.
  - **`isFullyVisible = True`**.

---

## 2. Nâng Cấp Đồ Họa "Nhiệt Đới Huyền Ảo"
- Bổ sung 12 khóm cỏ nhiệt đới thấp (`Foliage_GrassTufts`, cao $\le 0.15\,\text{m}$) bám sát nền đất.
- Bổ sung 10 cụm sỏi mịn (`Rocks_Pebbles`, cao $\le 0.06\,\text{m}$) rải rác ven đường mòn và mép nước.
- Điểm nhấn huyền ảo: 3 cụm nấm dạ quang màu xanh ngọc lam (`M_Glow_Mushroom_Cap` phát sáng emission) và đom đóm vàng ấm (`VFX_Fireflies`) bay lơ lửng.
- Bầu trời: Giữ nguyên theo đúng chỉ đạo (để người dùng lựa chọn sau).

---

## 3. Tổng Hợp Dữ Liệu và Bằng Chứng

1. **File số liệu đo đạc thực tế**:
   - `Evidence/T13_Bugfix_20261012/t13_bugfix_measurements.json`
2. **Bộ ảnh Game View có đầy đủ HUD (`Evidence/T13_Bugfix_20261012/Screenshots/`)**:
   - `GameView_seedling_Calm_Orbit0.png`
   - `GameView_young_tree_Calm_Orbit0.png`
   - `GameView_large_tree_Calm_Orbit0.png`
   - `GameView_large_tree_Orbit_60deg.png`
   - `GameView_large_tree_Orbit_120deg.png` (Góc lỗi cũ nay hoàn toàn sạch bóng)
   - `GameView_large_tree_Orbit_180deg.png`
   - `GameView_large_tree_Orbit_270deg.png`
   - `GameView_large_tree_Weather_sunny.png`
   - `GameView_large_tree_Weather_rainy.png`
   - `GameView_large_tree_Weather_thunderstorm.png`
3. **Bảo toàn tuyệt đối 100% Asset & Save**:
   - 6/6 asset cây chính giữ nguyên 100% SHA256:
     * `Tree_Seedling_Simple_v01.fbx`: `6fc4aa5a5a11d3a0dba9dc903891cd59d2f6845ff891520432d43e19cb6d60e6`
     * `Tree_Young_Simple_v01.fbx`: `2f3bfb2c354c91c89ea5cdd96e862f90c60265fb06a304da60615e9f8ac71fa9`
     * `Tree_Large.fbx`: `1d0b4b383865d51f8f5c39ac0a72cc2eba2a1dd35919229ce7896a0dcf229154`
     * `seedling.prefab`: `d0afc546799acad9296cae51c85f2709cadf60287c9348d34b49aa20ad6b764b`
     * `young_tree.prefab`: `0f52dfed8d49886aa9cc4bd849906bf7b58963f104d4358dda51020eebe8bbcf`
     * `large_tree.prefab`: `91af12e1cd1faf7a66603dadcff6a2c74d09757c70b95b000f3af3985889f9d6`
   - Save thật của người chơi tại AppData được bảo toàn nguyên vẹn.
4. **Kiểm thử tự động**:
   - Unity EditMode Tests: **252 / 252 PASSED** (100%).
   - Unity PlayMode Component Probes: 100% PASSED.
   - Không có compile error hay runtime exception trong Unity Console.

## 4. Kết Luận
Task T13 đã hoàn thành toàn diện, đạt mọi tiêu chuẩn mỹ thuật, gameplay và kỹ thuật, sẵn sàng chuyển trạng thái sang **DONE**.
