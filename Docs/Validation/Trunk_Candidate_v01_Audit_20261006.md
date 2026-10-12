# Báo cáo Audit Nguồn, Khởi tạo Candidate v01 và Khóa Vùng Sửa Thân — 06/10/2026

Scope: Thực hiện Prompt 1 trong quy trình chỉnh sửa thân cây (`Docs/Art/Trunk_Only_Agent_Prompts_20261006.md`). Phiên hiện tại chỉ kiểm tra nguồn, bảo toàn dữ liệu, tạo candidate riêng, thiết lập vertex mask và kết xuất baseline/mask. **Chưa thay đổi hình học cây trong phiên này.**

---

## 1. Môi trường công cụ và phiên Blender

- **Blender**: Phiên bản 5.2.2 LTS (`C:\Program Files\Blender Foundation\Blender 5.2\blender.exe`).
- **Blender MCP**: Kết nối hoạt động (`mcp-for-blender`, addon version 1.8).
- **Trạng thái phiên Blender GUI**: Process PID 17620 mở phiên trống chưa lưu (`(Unsaved) - Blender 5.2.2 LTS`, `bpy.data.filepath == ''`, `is_dirty == False`). Để bảo toàn tuyệt đối phiên làm việc của người dùng, toàn bộ tác vụ audit, nhân bản candidate, tính toán mask và render được thực hiện hoàn toàn độc lập qua tiến trình Blender background (`--background --factory-startup --disable-autoexec`).
- **Unity**: Phiên bản trên đĩa 6000.6.1f1, URP và Input System nguyên vẹn. Không có Unity process nào đang chạy; không gọi audit này là nghiệm thu T10 hay nghiệm thu Editor/Play Mode.

---

## 2. Kiểm tra nguồn gốc và bảo toàn tài sản

- **Nguồn mặc định**: `Assets/_Project/Art/Bonsai_Tree.blend`
  - SHA256 trước khi audit: `18BB67FC27BEA34137777EC00ACD0AD12B4FDF408537F3DA2E833CE48095BEF3` (Khớp 100% hash kiểm tra tại D25).
  - SHA256 sau khi audit: `18BB67FC27BEA34137777EC00ACD0AD12B4FDF408537F3DA2E833CE48095BEF3` (**Bảo toàn 100%, không bị sửa đổi**).
- **Source FBX**: `Assets/_Project/Art/Bonsai_Tree.fbx`
  - SHA256: `52B63D9F9F555BBBC85302DA9BA3AA0E6F1BF82F4E193E070F5B1B8981D0B7D5` (**Bảo toàn 100%**).
- Các file `.meta`, GUID, thư mục `Library/Temp`, prefabs và gameplay scenes giữ nguyên trạng, không bị chỉnh sửa.

---

## 3. Tạo bản sao riêng Candidate v01

- **Đường dẫn**: `ArtSource/Models/Bonsai_Trunk_Candidate_v01.blend`
- **SHA256 candidate**: `39DEDA1C6A7483CCFE54C2F1352B554E578DAC5F3AD9501B5D255179321FBB64`
- **Đối chiếu Fingerprint giữa Candidate và Baseline**:
  - `Roots`: Vertex count (9.120), Triangle count (17.856), Positions digest, Topology digest, UV digest, Corner normals digest, Matrix world, Modifiers, Materials đều **trùng khớp 100%**.
  - `Foliage`: Vertex count (22.880), Triangle count (18.304), Positions digest, Topology digest, UV digest, Corner normals digest, Matrix world, Modifiers, Materials đều **trùng khớp 100%**.
  - `Trunk_Branches`: Vị trí đỉnh (Positions digest), Topology digest, UV digest, Corner normals digest, Matrix world, Modifiers đều **trùng khớp 100%** (chưa thay đổi hình học). Chỉ bổ sung duy nhất Vertex Group `Trunk_Editable_Mask`.

---

## 4. Xác định cấu trúc hình học và thiết lập Mask khóa vùng

Mesh `Trunk_Branches` gồm 19.332 đỉnh, 18.864 mặt (37.728 triangles), chia thành 39 connected components dạng ống lồng:
- **Comp 0..5 (Trunk Core)**: Thân chính trụ giữa (Comp 0) và 5 gờ thớ dọc (Comp 1..5) quấn quanh trục, Z từ 0.088m đến 3.431m (tổng 6.264 đỉnh).
- **Comp 6..38 (Branches)**: Các nhánh cành cấp 1, cành phụ cấp 2 và ngọn tán (13.068 đỉnh).

### Quy tắc phân vùng Mask (`Trunk_Editable_Mask`)
1. **Khóa tuyệt đối chân thân tiếp giáp rễ (`LOCKED_ROOT_JUNCTION`, weight = 0.0)**:
   - Các đỉnh có Z ≤ 0.25m hoặc khoảng cách tới mesh `Roots` ≤ 0.03m (111 đỉnh).
   - Vùng chuyển tiếp bệ rễ (`TRANSITION_ROOT_FLARE`, 0.0 < weight < 1.0): Z từ 0.25m đến 0.60m với falloff smoothstep (118 đỉnh).
2. **Vùng thân chính được phép sửa (`EDITABLE_TRUNK`, weight = 1.0)**:
   - Toàn bộ thân chính và các sống dọc (Comp 0..5) từ Z = 0.60m đến Z = 3.30m (5.852 đỉnh, chiếm 30.3% mesh).
   - Vùng chuyển tiếp chạc ngọn (`TRANSITION_APEX_SPLIT`, 0.0 < weight < 1.0): Z từ 3.30m đến 3.43m (51 đỉnh).
3. **Vùng chuyển tiếp cổ cành (`TRANSITION_BRANCH_COLLAR`, 0.0 < weight < 1.0)**:
   - Chỉ cho phép chuyển tiếp trong phạm vi khoảng cách ≤ 0.20m từ điểm cắm cành vào thân chính (1.054 đỉnh). Trọng số giảm dần theo smoothstep từ 1.0 về 0.0 tại biên.
4. **Khóa tuyệt đối toàn bộ cành phụ, đầu cành và tiếp giáp tán lá (weight = 0.0)**:
   - Cành phụ xa thân (`LOCKED_DISTAL_BRANCH`): 2.164 đỉnh.
   - Đầu cành / vành biên hở (`LOCKED_BRANCH_TIP`): 924 đỉnh.
   - Vùng tiếp xúc sát tán lá `Foliage` (khoảng cách < 0.15m, `LOCKED_FOLIAGE_PROXIMITY`): 9.058 đỉnh.

### Thống kê tổng hợp Mask trên `Trunk_Branches`:
- **Đỉnh bị khóa hoàn toàn (Weight = 0.0)**: **12.257 đỉnh (63.4%)**
- **Đỉnh vùng chuyển tiếp (0.0 < Weight < 1.0)**: **1.223 đỉnh (6.3%)**
- **Đỉnh thân chính được phép chỉnh sửa (Weight = 1.0)**: **5.852 đỉnh (30.3%)**

Dữ liệu chi tiết từng đỉnh lưu tại: `Evidence/Trunk_Candidate_v01_Audit/mask_definition.json`.

---

## 5. Kết xuất Baseline và Mask Visualization

Sử dụng ánh sáng trung tính 3 điểm (Key Area 1200W, Fill Area 650W, Rim Area 800W) kết hợp Ambient World (0.8), không fog/volumetric. Toàn bộ thông số camera và ánh sáng được lưu cố định tại `Evidence/Trunk_Candidate_v01_Audit/render_config.json` để bảo đảm điều kiện so sánh đồng nhất ở Prompt 2 và 3.

Đã kết xuất đủ 16 ảnh độ phân giải cao (1280 × 960) tại `Evidence/Trunk_Candidate_v01_Audit/`:
1. **Bộ ảnh Baseline Clay (Chất liệu đất sét trung tính, không sửa material candidate)**:
   - `baseline_front.png`
   - `baseline_back.png`
   - `baseline_left.png`
   - `baseline_right.png`
   - `baseline_perspective.png` (Góc nhìn 3/4)
   - `baseline_closeup_trunk.png` (Cận cảnh thân chính)
   - `baseline_closeup_branch_collar.png` (Cận cảnh cổ cành)
   - `baseline_closeup_root_junction.png` (Cận cảnh tiếp giáp bệ rễ)
2. **Bộ ảnh Mask Visualization (Mã màu trực quan)**:
   - Xanh ngọc lục bảo (Emerald Green): Vùng thân chính được phép sửa (`Weight = 1.0`).
   - Vàng hổ phách (Amber Gold): Vùng chuyển tiếp cổ cành / chân rễ (`0.0 < Weight < 1.0`).
   - Xanh xám đậm (Slate Blue): Vùng cành xa, đầu cành, điểm gắn lá và chân rễ bị khóa (`Weight = 0.0`).
   - Xám than / Xanh rêu nhạt: Bối cảnh cố định của `Roots` và `Foliage`.
   - Các file: `mask_front.png`, `mask_back.png`, `mask_left.png`, `mask_right.png`, `mask_perspective.png`, `mask_closeup_trunk.png`, `mask_closeup_branch_collar.png`, `mask_closeup_root_junction.png`.

---

## 6. Đọc và phân tích ảnh tham chiếu mỹ thuật

- **Đường dẫn**: `C:\Users\Asus\Downloads\pexels-quang-nguyen-vinh-222549-14771841.jpg` (Kích thước 5.5 MB, đọc thành công).
- **Nhận xét mỹ thuật phục vụ Prompt 2**:
  - **Bệ gốc vững chãi (Stable Base)**: Chân thân cây bonsai cổ thụ nở bạnh dày, ôm liền mạch xuống dàn rễ nổi, tạo cảm giác đầm chắc và chống đỡ vững vàng.
  - **Sống gân và rãnh dọc (Longitudinal Grooves & Flutes)**: Thân cây không tròn trơn tuột như ống cao su mà gồm nhiều thớ gỗ cổ thụ bện xoắn hữu cơ, có các rãnh dọc nông sâu xen kẽ tạo chất già cỗi.
  - **Độ thuôn nhịp nhàng (Taper)**: Thân thuôn dần từ bệ gốc lên chạc chia cành, các chỗ đâm cành (branch collars) có vai nở đỡ cành ngang tự nhiên.
  - **Ràng buộc tuân thủ**: Không sao chép chậu đá, ghế đá, mặt sân, dây điện hay cảnh quan phía sau; giữ nguyên bố cục silhouette và dàn tán lá/rễ hiện có.

---

## 7. Bàn giao và trạng thái chuyển tiếp

- **T09**: DONE.
- **T10**: Tiếp tục `IN_PROGRESS` (chưa nghiệm thu Unity, chưa tích checklist acceptance).
- **Sẵn sàng cho Prompt 2**: File candidate `ArtSource/Models/Bonsai_Trunk_Candidate_v01.blend` đã có sẵn vertex group `Trunk_Editable_Mask`. Sẵn sàng để agent thực hiện Prompt 2 (chỉnh hình khối thân có kiểm soát theo mask).
