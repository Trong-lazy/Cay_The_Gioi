# Báo cáo Chỉnh Thân Candidate v01 và Kiểm Chứng Export FBX — 06/10/2026

Scope: Thực hiện Prompt 2 (Chỉnh hình khối thân có kiểm soát) và Prompt 3 (Kiểm chứng, export FBX và đối chiếu reimport) theo quy trình tại `Docs/Art/Trunk_Only_Agent_Prompts_20261006.md`. Chỉ chỉnh sửa trong phạm vi mask trên candidate riêng; toàn bộ dữ liệu gốc và phần khóa được bảo toàn 100%.

---

## 1. Kết quả chỉnh sửa hình khối thân (Prompt 2)

Thực hiện chỉnh sửa hình học trên file candidate riêng [ArtSource/Models/Bonsai_Trunk_Candidate_v01.blend](file:///c:/Users/Asus/foder%20game%203D/Cay_The_Gioi/ArtSource/Models/Bonsai_Trunk_Candidate_v01.blend) tuân thủ chặt chẽ trọng số của `Trunk_Editable_Mask`:

1. **Khắc phục cảm giác thân mảnh và khối trụ ống cao su**:
   - Tăng độ dày thân theo trục uốn (spine) từ +24% tại bệ gốc lên cực đại +31% ở đoạn thân trung (Z = 0.8m – 1.8m), đưa đường kính thân trung bình từ ~52cm lên ~66cm – 74cm.
   - Thân thuôn dần lên chạc ngọn (+18% tại Z = 2.8m và giảm dần về 0 tại ngọn Z = 3.35m), loại bỏ hoàn toàn hiện tượng thắt eo cục bộ.
2. **Điêu khắc sống dọc và rãnh nông cổ thụ theo ảnh tham chiếu**:
   - Dựa trên ảnh [pexels-quang-nguyen-vinh-222549-14771841.jpg](file:///C:/Users/Asus/Downloads/pexels-quang-nguyen-vinh-222549-14771841.jpg), tạo 5 sống gân thớ xoắn hữu cơ chạy dọc các component thân, làm nổi bật đường nét vỏ cây già cỗi.
   - Thêm độ nở bạnh cơ bắp bất đối xứng ở chân thân phía trước-trái (`asym_flare`), tạo cảm giác bệ gốc bám chặt vững chãi.
3. **Tạo vai đỡ cổ cành tự nhiên (Branch Collar Shoulders)**:
   - Các điểm chia cành chính (Comp 6, 9, 11, 13, 16, 19, 22, 24, 26, 28, 30, 32) được nở nhẹ ở phần cổ cành (trong phạm vi ≤ 0.20m tính từ thân) tạo thành vai đỡ cành ngang chắc chắn.
   - Trọng số biến dạng triệt tiêu mượt mà về 0 tại biên mask ngoài của cổ cành, bảo đảm tuyệt đối không kéo lê hoặc làm lệch các cành vươn ra tán.
4. **Bảo toàn tiếp giáp rễ**:
   - Biến dạng giảm dần về 0 từ Z = 0.60m xuống Z = 0.25m và triệt tiêu hoàn toàn tại các đỉnh tiếp giáp rễ nổi, không làm hở khe hay xuyên thủng bệ rễ.

---

## 2. Kiểm chứng số học và bảo toàn dữ liệu (Prompt 3)

### Đối chiếu dữ liệu nguồn và candidate:
- **Nguồn gốc `.blend`**: `Assets/_Project/Art/Bonsai_Tree.blend`
  - SHA256 trước và sau: `18BB67FC27BEA34137777EC00ACD0AD12B4FDF408537F3DA2E833CE48095BEF3` (**Bảo toàn 100%**).
- **Nguồn gốc `.fbx`**: `Assets/_Project/Art/Bonsai_Tree.fbx`
  - SHA256: `52B63D9F9F555BBBC85302DA9BA3AA0E6F1BF82F4E193E070F5B1B8981D0B7D5` (**Bảo toàn 100%**).
- **Candidate `.blend`**: `ArtSource/Models/Bonsai_Trunk_Candidate_v01.blend`
  - SHA256: `2707CD92E1EE11996663D461FC457421814C2A1A208085CF965AB826F4C2848C`.
- **Candidate Export `.fbx`**: `ArtSource/Exports/Bonsai_Trunk_Candidate_v01.fbx`
  - SHA256: `802610722100C52F810E198C9DCF43A23267D31D30CF7AF38747ED6A3BF68644`.

### Kiểm chứng Topology và Fingerprint từng Mesh:
- **`Roots`**: 9.120 đỉnh, 17.856 tris, 384 boundary edges. Toàn bộ vị trí đỉnh, UV, normals, matrix_world, modifiers, materials **khớp 100% với baseline**.
- **`Foliage`**: 22.880 đỉnh, 18.304 tris, 18.304 boundary edges. Toàn bộ vị trí đỉnh, UV, normals, matrix_world, modifiers, materials **khớp 100% với baseline**.
- **`Trunk_Branches`**:
  - Số đỉnh: **19.332** (giữ nguyên).
  - Số mặt: **18.864** (giữ nguyên).
  - Số tam giác evaluated: **37.728** (giữ nguyên).
  - Cạnh biên (boundary edges): **936** (giữ nguyên).
  - Cạnh non-manifold: **936** (giữ nguyên).
  - Đỉnh rời (loose vertices): **0**; Mặt suy biến: **0**.
  - **Sai số vị trí trên toàn bộ 12.257 đỉnh bị khóa (Weight = 0.0)**: **0.0000000000 m (Không suy suyển dù chỉ 1 bit)**.
- **Tổng số tam giác toàn mô hình**: **73.888 triangles** (bảo toàn 100% baseline).

---

## 3. Xuất và Reimport FBX kiểm chứng độc lập

1. **Xuất FBX**:
   - Xuất riêng 3 mesh `Roots`, `Trunk_Branches`, `Foliage` sang `ArtSource/Exports/Bonsai_Trunk_Candidate_v01.fbx`.
   - Chuẩn trục Unity: Forward = -Z, Up = Y, Unit scale = 1.0, Scale = FBX_SCALE_ALL.
2. **Reimport trong phiên Blender độc lập**:
   - Đọc lại file FBX trong scene trắng, xác nhận:
     - 3 Mesh Object nhập thành công: `Roots`, `Trunk_Branches`, `Foliage`.
     - Transform Scale: `(1.0, 1.0, 1.0)` trên cả 3 object (không bị lỗi scale 100 hay 0.01).
     - Transform Rotation: `Euler(0, 0, 0)` (không bị lệch -90 độ trục X).
     - Tổng số tam giác: **73.888 triangles**.
   - Ảnh render kiểm chứng: [reimport_fbx_perspective.png](file:///c:/Users/Asus/foder game 3D/Cay_The_Gioi/Evidence/Trunk_Candidate_v01_Audit/reimport_fbx_perspective.png).

---

## 4. Bảng ảnh đối chiếu Baseline vs Candidate v01

Tất cả ảnh chụp dưới cùng góc máy và ánh sáng trung tính (đã lưu tại `Evidence/Trunk_Candidate_v01_Audit/render_config.json`):

| Góc nhìn | Ảnh Baseline (Cũ) | Ảnh Candidate v01 (Mới) | Đánh giá thay đổi |
| :--- | :--- | :--- | :--- |
| **Perspective (3/4)** | [baseline_perspective.png](file:///c:/Users/Asus/foder%20game%203D/Cay_The_Gioi/Evidence/Trunk_Candidate_v01_Audit/baseline_perspective.png) | [candidate_perspective.png](file:///c:/Users/Asus/foder%20game%203D/Cay_The_Gioi/Evidence/Trunk_Candidate_v01_Audit/candidate_perspective.png) | Thân đầm chắc, đường lượn hữu cơ vững chãi, đỡ trọn tán lá |
| **Front (Chính diện)** | [baseline_front.png](file:///c:/Users/Asus/foder%20game%203D/Cay_The_Gioi/Evidence/Trunk_Candidate_v01_Audit/baseline_front.png) | [candidate_front.png](file:///c:/Users/Asus/foder%20game%203D/Cay_The_Gioi/Evidence/Trunk_Candidate_v01_Audit/candidate_front.png) | Chân thân nở bạnh ôm rễ, thân trung đầy đặn không thắt eo |
| **Back (Phía sau)** | [baseline_back.png](file:///c:/Users/Asus/foder%20game%203D/Cay_The_Gioi/Evidence/Trunk_Candidate_v01_Audit/baseline_back.png) | [candidate_back.png](file:///c:/Users/Asus/foder%20game%203D/Cay_The_Gioi/Evidence/Trunk_Candidate_v01_Audit/candidate_back.png) | Sống gân phía sau liền mạch, thuôn tự nhiên lên chạc |
| **Left (Bên trái)** | [baseline_left.png](file:///c:/Users/Asus/foder%20game%203D/Cay_The_Gioi/Evidence/Trunk_Candidate_v01_Audit/baseline_left.png) | [candidate_left.png](file:///c:/Users/Asus/foder%20game%203D/Cay_The_Gioi/Evidence/Trunk_Candidate_v01_Audit/candidate_left.png) | Nhịp uốn S-curve giữ nguyên, độ dày thân cải thiện rõ rệt |
| **Right (Bên phải)** | [baseline_right.png](file:///c:/Users/Asus/foder%20game%203D/Cay_The_Gioi/Evidence/Trunk_Candidate_v01_Audit/baseline_right.png) | [candidate_right.png](file:///c:/Users/Asus/foder%20game%203D/Cay_The_Gioi/Evidence/Trunk_Candidate_v01_Audit/candidate_right.png) | Độ liên kết các thớ gỗ tốt hơn, không còn khe rỗng sâu |
| **Close-up Thân** | [baseline_closeup_trunk.png](file:///c:/Users/Asus/foder%20game%203D/Cay_The_Gioi/Evidence/Trunk_Candidate_v01_Audit/baseline_closeup_trunk.png) | [candidate_closeup_trunk.png](file:///c:/Users/Asus/foder%20game%203D/Cay_The_Gioi/Evidence/Trunk_Candidate_v01_Audit/candidate_closeup_trunk.png) | Các sống gân cổ thụ nổi khối cơ bắp, bề mặt mượt mà |
| **Close-up Cổ cành** | [baseline_closeup_branch_collar.png](file:///c:/Users/Asus/foder%20game%203D/Cay_The_Gioi/Evidence/Trunk_Candidate_v01_Audit/baseline_closeup_branch_collar.png) | [candidate_closeup_branch_collar.png](file:///c:/Users/Asus/foder%20game%203D/Cay_The_Gioi/Evidence/Trunk_Candidate_v01_Audit/candidate_closeup_branch_collar.png) | Vai cành nở đỡ cành ngang, chuyển tiếp mượt không gấp khúc |
| **Close-up Bệ rễ** | [baseline_closeup_root_junction.png](file:///c:/Users/Asus/foder%20game%203D/Cay_The_Gioi/Evidence/Trunk_Candidate_v01_Audit/baseline_closeup_root_junction.png) | [candidate_closeup_root_junction.png](file:///c:/Users/Asus/foder%20game%203D/Cay_The_Gioi/Evidence/Trunk_Candidate_v01_Audit/candidate_closeup_root_junction.png) | Ăn khớp hoàn hảo với dàn rễ nổi, không hở khe |

---

## 5. Trạng thái task và bàn giao

- **Task T10**: Giữ nguyên `IN_PROGRESS` (chưa nghiệm thu Unity Editor/Play Mode, chưa tích acceptance checklist).
- **Phạm vi bảo toàn**: Không chỉnh sửa file nguồn, không chỉnh sửa gameplay prefab/scene/config, không tạo Mầm/Cây Non trong lượt này.
- **Sản phẩm bàn giao**:
  - File nguồn candidate: `ArtSource/Models/Bonsai_Trunk_Candidate_v01.blend`.
  - File xuất candidate: `ArtSource/Exports/Bonsai_Trunk_Candidate_v01.fbx`.
  - Toàn bộ bằng chứng và log: `Evidence/Trunk_Candidate_v01_Audit/candidate_v01_report.json`.
