# Báo cáo Kiểm chứng Toàn diện Candidate Thân v01 và Xuất FBX (Prompt 3) — 06/10/2026

Scope: Nghiệm thu kỹ thuật độc lập cho bản chỉnh sửa thân candidate theo đúng 7 yêu cầu tại **Prompt 3** (`Docs/Art/Trunk_Only_Agent_Prompts_20261006.md`). Kiểm chứng số học từng đỉnh, topology, UV, normals, hiện tượng suy biến, xuất FBX và reimport đối chiếu. **Không tích hợp candidate vào gameplay Unity trong lượt này.**

---

## 1. So sánh chi tiết Roots / Foliage và phần khóa Trunk_Branches với nguồn

Kiểm tra số học từng đỉnh (float tolerance giải thích chi tiết) giữa nguồn gốc (`Assets/_Project/Art/Bonsai_Tree.blend`) và bản lưu candidate trên đĩa (`ArtSource/Models/Bonsai_Trunk_Candidate_v01.blend`):

| Đối tượng | Tiêu chí đối chiếu | Nguồn Baseline | Candidate v01 | Chênh lệch (Delta) | Trạng thái |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **`Roots`** | Vị trí đỉnh (Vertex Coords) | 9.120 đỉnh | 9.120 đỉnh | `0.0000000000 m` | **TRÙNG KHỚP 100%** |
| | Thứ tự cạnh & mặt (Topology) | 8.928 faces, 18.048 edges | 8.928 faces, 18.048 edges | `0` sai khác | **TRÙNG KHỚP 100%** |
| | Tọa độ UV (`UVMap`) | 35.712 loops | 35.712 loops | `0.0000000000` | **TRÙNG KHỚP 100%** |
| | Vector Corner Normals | 35.712 loops | 35.712 loops | `0.0000000000` | **TRÙNG KHỚP 100%** |
| | Ma trận World Transform | Identity (Scale 1.0) | Identity (Scale 1.0) | `0` sai khác | **TRÙNG KHỚP 100%** |
| | Modifiers & Material Slots | Không có modifier, slots [] | Không có modifier, slots [] | `0` sai khác | **TRÙNG KHỚP 100%** |
| **`Foliage`** | Vị trí đỉnh (Vertex Coords) | 22.880 đỉnh | 22.880 đỉnh | `0.0000000000 m` | **TRÙNG KHỚP 100%** |
| | Thứ tự cạnh & mặt (Topology) | 18.304 faces, 36.608 edges | 18.304 faces, 36.608 edges | `0` sai khác | **TRÙNG KHỚP 100%** |
| | Tọa độ UV (`UVMap`) | 54.912 loops | 54.912 loops | `0.0000000000` | **TRÙNG KHỚP 100%** |
| | Vector Corner Normals | 54.912 loops | 54.912 loops | `0.0000000000` | **TRÙNG KHỚP 100%** |
| | Ma trận World Transform | Identity (Scale 1.0) | Identity (Scale 1.0) | `0` sai khác | **TRÙNG KHỚP 100%** |
| | Modifiers & Material Slots | Không có modifier, slots [] | Không có modifier, slots [] | `0` sai khác | **TRÙNG KHỚP 100%** |
| **`Trunk_Branches` (Phần khóa)** | 12.257 đỉnh bị khóa (`weight == 0`) | Vị trí gốc | Giữ nguyên | `max delta = 0.0000000000 m` | **BẢO TOÀN TUYỆT ĐỐI** |
| | 936 đỉnh vành biên (boundary verts) | Vị trí gốc | Giữ nguyên | `max delta = 0.0000000000 m` | **BẢO TOÀN TUYỆT ĐỐI** |
| | Tọa độ UV tại các đỉnh bị khóa | Vị trí gốc | Giữ nguyên | `max delta = 0.0000000000` | **BẢO TOÀN TUYỆT ĐỐI** |
| | 750 cạnh biên chuyển tiếp (boundary) | Đầu khóa: delta = 0.0m | Đầu sửa: avg delta = 0.046m | Chuyển tiếp mượt, không đứt | **ĐẠT CHUẨN MỸ THUẬT** |

---

## 2. Kiểm tra mặt suy biến, cạnh biên, đỉnh rời và hiện tượng hình học

- **Mặt suy biến (Degenerate faces, diện tích < 10⁻⁸ m²)**: **0** trên cả 3 mesh.
- **Đỉnh rời (Loose vertices, không gắn cạnh)**: **0** trên cả 3 mesh.
- **Cạnh rời (Loose edges, không gắn mặt)**: **0** trên cả 3 mesh.
- **Cạnh biên hở (Boundary edges)**:
  - `Roots`: 384 cạnh (các đầu rễ hở cắm xuống đất).
  - `Foliage`: 18.304 cạnh (các phiến lá rời low-poly).
  - `Trunk_Branches`: 936 cạnh (đúng 39 vành ống của 39 component ban đầu, không phát sinh thêm bất kỳ cạnh biên mới nào).
- **Hiện tượng tự giao / lỗ lộ**: Kiểm tra qua 8 góc chụp clay và cận cảnh xác nhận không có lỗ thủng mới, không hở khe ở chân thân tiếp giáp bệ rễ, không đứt gãy tại cổ cành.

---

## 3. Thống kê số lượng đa giác (Triangles) trước và sau

| Mesh | Vertices | Faces | Triangles Baseline | Triangles Candidate | Trạng thái bảo toàn |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **`Roots`** | 9.120 | 8.928 | 17.856 | 17.856 | Giữ nguyên 100% |
| **`Trunk_Branches`** | 19.332 | 18.864 | 37.728 | 37.728 | Giữ nguyên 100% |
| **`Foliage`** | 22.880 | 18.304 | 18.304 | 18.304 | Giữ nguyên 100% |
| **TỔNG CỘNG** | **51.332** | **46.096** | **73.888** | **73.888** | **BẢO TOÀN 100% TRIANGLES** |

*Ghi chú*: Con số 73.888 tam giác là thống kê hình học baseline của mô hình gốc, chưa phải bằng chứng đã tối ưu hiệu năng trên Windows/mobile; không tự tuyên bố số draw calls/FPS khi chưa đo trực tiếp trong Unity.

---

## 4. Xuất Candidate FBX riêng biệt

- **Đường dẫn file**: [ArtSource/Exports/Bonsai_Trunk_Candidate_v01.fbx](file:///c:/Users/Asus/foder%20game%203D/Cay_The_Gioi/ArtSource/Exports/Bonsai_Trunk_Candidate_v01.fbx)
- **SHA256**: `802610722100C52F810E198C9DCF43A23267D31D30CF7AF38747ED6A3BF68644`
- **Thiết lập xuất**:
  - Chỉ xuất đúng 3 mesh đã chọn: `Roots`, `Trunk_Branches`, `Foliage`.
  - Loại trừ toàn bộ Camera, Light và các scene phụ.
  - Quy ước chuẩn Unity: `axis_forward='-Z'`, `axis_up='Y'`, `apply_unit_scale=True`, `apply_scale_options='FBX_SCALE_ALL'`, `mesh_smooth_type='FACE'`.
  - Tuyệt đối không ghi đè `Assets/_Project/Art/Bonsai_Tree.fbx`.

---

## 5. Reimport FBX và đối chiếu độc lập trong Blender

Đã chạy phiên Blender độc lập đọc file `Bonsai_Trunk_Candidate_v01.fbx` trong scene trắng:
- **Objects nhận dạng**: Đủ 3 mesh `Roots`, `Trunk_Branches`, `Foliage`.
- **Scale**: `(1.0, 1.0, 1.0)` trên cả 3 mesh (không bị lỗi scale 100 hay 0.01).
- **Rotation**: `(0.0, 0.0, 0.0)` (sai số epsilon float `-1.6e-7 rad`, tương đương `0.000009°`, hoàn toàn không bị lỗi lật trục -90 độ trục X).
- **Dimensions**:
  - `Trunk_Branches`: 6.2131 × 3.6678 × 3.7320 m.
  - `Roots`: 6.8543 × 5.7043 × 1.2715 m.
  - `Foliage`: 8.2095 × 5.0540 × 3.2972 m.
- **Tổng số tam giác**: **73.888 triangles** (khớp chính xác baseline).
- **Ảnh render kiểm chứng reimport**: [reimport_fbx_perspective.png](file:///c:/Users/Asus/foder%20game%203D/Cay_The_Gioi/Evidence/Trunk_Candidate_v01_Audit/reimport_fbx_perspective.png).

---

## 6. Danh mục bằng chứng và Hash toàn vẹn

### Hash đối chiếu:
- `Assets/_Project/Art/Bonsai_Tree.blend`: `18BB67FC27BEA34137777EC00ACD0AD12B4FDF408537F3DA2E833CE48095BEF3` (**Bảo toàn 100%**)
- `Assets/_Project/Art/Bonsai_Tree.fbx`: `52B63D9F9F555BBBC85302DA9BA3AA0E6F1BF82F4E193E070F5B1B8981D0B7D5` (**Bảo toàn 100%**)
- `ArtSource/Models/Bonsai_Trunk_Candidate_v01.blend`: `B99A966A42BD2A44591C7BA007A3BB896482A013DC6A0F4C5F979A1A12C3FC66`
- `ArtSource/Exports/Bonsai_Trunk_Candidate_v01.fbx`: `802610722100C52F810E198C9DCF43A23267D31D30CF7AF38747ED6A3BF68644`

### Bộ ảnh render (Evidence/Trunk_Candidate_v01_Audit/):
- **Baseline (Cũ)**: [baseline_front.png](file:///c:/Users/Asus/foder%20game%203D/Cay_The_Gioi/Evidence/Trunk_Candidate_v01_Audit/baseline_front.png), [baseline_back.png](file:///c:/Users/Asus/foder%20game%203D/Cay_The_Gioi/Evidence/Trunk_Candidate_v01_Audit/baseline_back.png), [baseline_left.png](file:///c:/Users/Asus/foder%20game%203D/Cay_The_Gioi/Evidence/Trunk_Candidate_v01_Audit/baseline_left.png), [baseline_right.png](file:///c:/Users/Asus/foder%20game%203D/Cay_The_Gioi/Evidence/Trunk_Candidate_v01_Audit/baseline_right.png), [baseline_perspective.png](file:///c:/Users/Asus/foder%20game%203D/Cay_The_Gioi/Evidence/Trunk_Candidate_v01_Audit/baseline_perspective.png), [baseline_closeup_trunk.png](file:///c:/Users/Asus/foder%20game%203D/Cay_The_Gioi/Evidence/Trunk_Candidate_v01_Audit/baseline_closeup_trunk.png), [baseline_closeup_branch_collar.png](file:///c:/Users/Asus/foder%20game%203D/Cay_The_Gioi/Evidence/Trunk_Candidate_v01_Audit/baseline_closeup_branch_collar.png), [baseline_closeup_root_junction.png](file:///c:/Users/Asus/foder%20game%203D/Cay_The_Gioi/Evidence/Trunk_Candidate_v01_Audit/baseline_closeup_root_junction.png).
- **Candidate v01 (Mới)**: [candidate_front.png](file:///c:/Users/Asus/foder%20game%203D/Cay_The_Gioi/Evidence/Trunk_Candidate_v01_Audit/candidate_front.png), [candidate_back.png](file:///c:/Users/Asus/foder%20game%203D/Cay_The_Gioi/Evidence/Trunk_Candidate_v01_Audit/candidate_back.png), [candidate_left.png](file:///c:/Users/Asus/foder%20game%203D/Cay_The_Gioi/Evidence/Trunk_Candidate_v01_Audit/candidate_left.png), [candidate_right.png](file:///c:/Users/Asus/foder%20game%203D/Cay_The_Gioi/Evidence/Trunk_Candidate_v01_Audit/candidate_right.png), [candidate_perspective.png](file:///c:/Users/Asus/foder%20game%203D/Cay_The_Gioi/Evidence/Trunk_Candidate_v01_Audit/candidate_perspective.png), [candidate_closeup_trunk.png](file:///c:/Users/Asus/foder%20game%203D/Cay_The_Gioi/Evidence/Trunk_Candidate_v01_Audit/candidate_closeup_trunk.png), [candidate_closeup_branch_collar.png](file:///c:/Users/Asus/foder%20game%203D/Cay_The_Gioi/Evidence/Trunk_Candidate_v01_Audit/candidate_closeup_branch_collar.png), [candidate_closeup_root_junction.png](file:///c:/Users/Asus/foder%20game%203D/Cay_The_Gioi/Evidence/Trunk_Candidate_v01_Audit/candidate_closeup_root_junction.png).
- **Reimport FBX**: [reimport_fbx_perspective.png](file:///c:/Users/Asus/foder%20game%203D/Cay_The_Gioi/Evidence/Trunk_Candidate_v01_Audit/reimport_fbx_perspective.png).
- **Báo cáo dữ liệu JSON**: [prompt3_deep_verification_report.json](file:///c:/Users/Asus/foder%20game%203D/Cay_The_Gioi/Evidence/Trunk_Candidate_v01_Audit/prompt3_deep_verification_report.json).

---

## 7. Known Issues và Giới hạn kỹ thuật

1. **Phạm vi Candidate**: Mô hình candidate mới chỉ điều chỉnh hình khối thân cây lớn (`large_tree`). Chưa tạo 2 stage còn lại (Mầm `seedling` và Cây Non `young_tree`).
2. **Chưa tích hợp gameplay**: Chưa gán material texture hoàn chỉnh, chưa thiết lập Empty Anchor GameObjects (`Anchor_LightningTarget`, `Anchor_LeavesCenter`, `Anchor_TrunkAura`), chưa thay thế prefab gameplay hay đo đạc collider raycast tap cây trong Unity Play Mode.
3. **Trạng thái task**: Task T10 tiếp tục giữ `IN_PROGRESS`, không tích bất kỳ tiêu chí acceptance nào chưa thử nghiệm trực tiếp trong Editor.
