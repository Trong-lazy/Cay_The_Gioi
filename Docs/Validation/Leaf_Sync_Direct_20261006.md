# Đồng bộ lá Cây Lớn từ hình học thật của Cổ Thụ

Task: T10
Date: 2026-10-06
Result: PARTIAL — chỉnh asset và kiểm tra Blender background/FBX đạt; chưa nghiệm thu T10 trong Unity.

## Yêu cầu và phạm vi

Người dùng yêu cầu trực tiếp sửa để đồng bộ lá Cây Lớn và Cổ Thụ trước khi thêm màu. Sửa duy nhất mesh `Foliage` trong `ArtSource/Models/Tree_Large.blend`, xuất đè `ArtSource/Exports/Tree_Large.fbx`. Không tạo candidate hoặc bản sao model. Dùng `ArtSource/Models/Bonsai_Trunk_Candidate_v01.blend` làm nguồn chỉ đọc. Không đổi thân, cành, rễ, transform/pivot, material hiện có, scene Unity, gameplay hoặc các model khác.

## Nguyên nhân và cách sửa

- Lượt D33 lấy thông số tỷ lệ/gập lá rồi tái tạo lá đơn bằng công thức. Đây chưa phải sao chép hình học/cách ghép cụm của model tham chiếu, nên nhận xét hai cây vẫn khác lá là có cơ sở. Ảnh đối chiếu cũ trong script `generate_tree_large_v3.py` cũng dùng công thức dựng lại, không chứng minh trùng hình học nguồn.
- Audit thật: Cổ Thụ có 4.576 phiến lá rời, mỗi phiến 5 đỉnh/4 tam giác; các nhóm 4 phiến liên tiếp tạo cấu trúc cụm của nguồn. Cây Lớn trước sửa có 385 lá đơn, mỗi lá 10 đỉnh/16 tam giác tính cả mặt dưới/mép.
- Trích trực tiếp 8 mẫu cụm, giữ tọa độ tương đối, hình lá, UV và corner normals của nguồn. Thay mỗi lá cũ bằng một cụm 4 phiến nhỏ hơn, giữ 385 vị trí neo cũ và phân bố 7 vùng tán theo XYZ. Điều chỉnh bằng tỷ lệ đồng nhất, định hướng theo lá cũ và dao động góc tối đa 6 độ, không dựng lại đường viền bằng công thức khác.
- Thêm mặt dưới/mép mỏng khép kín cho từng phiến để có hình học ở cả hai mặt. Độ dày bằng `0.0015 × scale` mét; mặt giữa của phiến vẫn trùng nguồn sau đảo biến đổi. Không khẳng định đã kiểm thử backface/material URP trong Unity.
- Vật liệu xám chỉ dùng trong scene render tạm, không lưu vào model. Vật liệu `M_Bark` và `M_Foliage` hiện có được giữ; chưa áp bảng màu mới.

## Kết quả kiểm chứng thực tế

Blender CLI 5.2.2 LTS, `--background --factory-startup --disable-autoexec`. Không có Unity/Blender MCP trong phiên; không thao tác hoặc nghiệm thu Blender GUI/Unity Editor.

| Mesh | Trước (verts/tris) | Sau (verts/tris) |
| --- | --- | --- |
| Roots | 374 / 634 | 374 / 634 |
| Trunk_Branches | 858 / 1.408 | 858 / 1.408 |
| Foliage | 3.850 / 6.160 | 15.400 / 24.640 |
| Tổng | 5.082 / 8.202 | 16.632 / 26.682 |

- 385 cụm × 4 = 1.540 phiến lá thật lấy từ nguồn. Sai số tối đa mặt giữa sau đảo biến đổi: `1.9588793553612986e-7` (đơn vị tọa độ nguồn).
- Foliage mới: 0 mặt suy biến, 0 đỉnh rời, 0 cạnh non-manifold. Kiểm tra này không phải kiểm tra giao nhau giữa các phiến/cụm; vẫn có xếp chồng theo thiết kế lá nguồn.
- Fingerprint Roots/Trunk_Branches trước và sau lưu/mở lại trùng nhau: tọa độ đỉnh, edges, faces, UV, corner normals, world matrix và material slots. Fingerprint Foliage đã tạo và sau mở lại cũng trùng.
- Material slots Foliage và trạng thái node/default values của material lá sau mở lại trùng trước sửa. Không chạy bước tô màu.
- SHA256 Cổ Thụ nguồn, `Assets/_Project/Art/Bonsai_Tree.blend`, Cây Mầm và Cây Non giữ nguyên. Hash cụ thể trong `apply_report.json`.
- Tán trước: X 3.315967 m, Y 2.104346 m; sau: X 3.321965 m, Y 2.109498 m. Đỉnh cây sau Z 2.352890 m; đáy rễ giữ Z -0.264316 m. Giữ vị trí neo không đồng nghĩa bounds tuyệt đối không đổi.
- Đọc lại file FBX trong Blender factory scene độc lập: đủ 3 mesh, tổng 26.682 tris, số đỉnh/UV layer/material slots trùng file blend. Sai số tối đa world vertex `4.2981520209650625e-7` m; scale `(1,1,1)`, rotation gần 0 (sai số float `1.63e-7` rad), không lệch 90 độ.
- Rễ/thân có cạnh hở/non-manifold từ trước (94/244 cạnh); bảo toàn theo phạm vi, không tuyên bố toàn cây là mesh kín và không sửa phần này trong lượt đồng bộ lá.

## Bằng chứng

Thư mục `Evidence/Leaf_Sync_Direct_20261006/`:

- `inspect_geometry.py`: audit lá nguồn/đích chỉ đọc.
- `sync_actual_sprigs.py`: preview trong bộ nhớ, áp vào file gốc có kiểm tra hash và mở lại; không dùng chạy lặp trên model đã sửa.
- `preview_report.json`, `apply_report.json`: số liệu, fingerprints, hashes và bảo toàn nguồn.
- `before/`, `after/`: 8 góc trước/sau (front, back, left, right, top, perspective, back_perspective, low). Đã xem trực tiếp toàn bộ 8 ảnh sau sửa.
- `reference_sprig/`, `transferred_sprig/`: render cụm nguồn và cụm lấy lại từ đỉnh thực của mesh đích, chuẩn hóa cùng kích thước để đối chiếu; không phải hai ảnh đều dựng từ công thức.
- `reference_tree/perspective.png`: tán thật của model Cổ Thụ, chỉ foliage để so phong cách lá.
- `verify_roundtrip.py`, `roundtrip_report.json`: kiểm tra độc lập file đã lưu và FBX.
- `saved_blend_perspective.png`, `reimport_fbx_perspective.png`: đã xem và đối chiếu hai bản thực tế.

Hash blend trước: `690424d073b5b1c68fa19dc1e2aa1af9dbf2098e720dd19d827f6ac685771adf`.

Hash blend sau: `a01e1328f3ad95df3e238057050f06d9e247e321ede9853f7d3601e28b49f723`.

Hash FBX sau: `1beee5cbade81a2abdd9b73ffb94e6de281a3d8974dfa7b28cd3a0f051d54c95`.

## Phần chưa nghiệm thu

Chưa kiểm thử import URP, Console/compile, prefab/collider/anchors, camera, effects, Unity Play Mode, hồi quy gameplay hoặc thiết bị thật trong phiên này. Số tam giác tăng để dùng cụm nguồn, không có đo FPS/draw calls nên không tuyên bố tối ưu mobile. T10 giữ `IN_PROGRESS`, checklist gameplay chưa tích. Bản sửa asset sẵn để người dùng xem trước khi yêu cầu tô màu/tích hợp tiếp.
