# Danh mục model cây và mốc hình ảnh hiện hành

Cập nhật hậu kiểm: 07/10/2026 — D44/D45/D46 giữ phân loại và asset tích hợp, nhưng **T10 IN_PROGRESS** do hai lỗi hit-test trên seedling. Xem `Docs/Validation/T10_Independent_Recheck_20261007.md`. Các nhãn nghiệm thu DONE trong bảng dưới là lịch sử của agent, chưa phải trạng thái nghiệm thu hiện hành.

Đây là bảng đối chiếu tên mốc mỹ thuật với file nguồn và asset trong Unity. Đọc bảng này trước khi dựng, sửa, tìm hoặc tích hợp model. Tên file kỹ thuật không phải stage ID của gameplay và không tự quyết định mốc tuổi của cây.

## Phân loại đã chốt

Model đang lưu tại `ArtSource/Models/Tree_Large.blend` đại diện cho **Cây Lớn**. Quyết định D42 từng xếp nó là Cây Trưởng Thành; D44 thay thế phân loại đó theo yêu cầu mới nhất của người dùng. Ảnh tham chiếu ngày 06/10/2026 được dành cho mốc Cây Trưởng Thành (`WT_MatureTree.blend`); không tính cùng một model thành hai mốc riêng.

| Mốc cây | Stage ID Gameplay | Nguồn hiện hành tại `ArtSource/Models` | FBX tại `ArtSource/Exports` & `Assets/_Project/Art/Models` | Prefab & Trạng thái tích hợp Unity |
| --- | --- | --- | --- | --- |
| Mầm | `seedling` | `Tree_Seedling_Simple_v01.blend` | `Tree_Seedling_Simple_v01.fbx` | **Đã tích hợp & nghiệm thu (T10 DONE)**: `Assets/_Project/Prefabs/Trees/seedling.prefab` (5 URP Lit `.mat`, `TreeStageAnchors`, 4 `MeshCollider` non-convex khớp mesh con). Xem `Docs/Validation/T10_Raycast_Fix_20261007.md` |
| Cây Non | `young_tree` | `Tree_Young_Simple_v01.blend` | `Tree_Young_Simple_v01.fbx` | **Đã tích hợp & nghiệm thu (T10 DONE)**: `Assets/_Project/Prefabs/Trees/young_tree.prefab` (3 URP Lit `.mat`, `TreeStageAnchors`, 3 `MeshCollider` non-convex khớp mesh con). Xem `Docs/Validation/T10_Raycast_Fix_20261007.md` |
| Cây Lớn | `large_tree` | `Tree_Large.blend` | `Tree_Large.fbx` | **Đã tích hợp & nghiệm thu (T10 DONE)**: `Assets/_Project/Prefabs/Trees/large_tree.prefab` (2 URP Lit `.mat` `M_Large_Foliage` + `M_Large_Bark` Tiling `2.5×2.5`, `TreeStageAnchors`, 3 `MeshCollider` non-convex khớp mesh con). Xem `Docs/Validation/T10_Raycast_Fix_20261007.md` |
| Cây Trưởng Thành | *(Mốc mở rộng)* | `WT_MatureTree.blend` | `WT_MatureTree_LOD0.fbx`, `WT_MatureTree_LOD1.fbx`, `WT_MatureTree_LOD2.fbx` (tại `ArtSource/Exports/`) | Review độc lập `PARTIAL`: đồng bộ lá D34/M_Foliage, tách 3 vùng và material/FBX đạt; còn tán mỏng/lộ đầu cành một số góc; chưa tích hợp Unity. Tris 37.018 / 20.238 / 8.504. Xem `Docs/Validation/Mature_Independent_Acceptance_20261007.md` |
| Cổ Thụ | *(Mốc mở rộng)* | `Bonsai_Trunk_Candidate_v01.blend` | `Bonsai_Trunk_Candidate_v01.fbx` (tại `ArtSource/Exports/`) | Có nguồn đã chuẩn hóa UV vỏ Bishop 3D (D41); base model dùng chung cho hai mốc cuối theo D43; dành cho mốc mở rộng ngoài 3 stage prototype |
| Cổ Đại | *(Mốc mở rộng)* | Dùng chung `Bonsai_Trunk_Candidate_v01.blend` | Dùng chung `Bonsai_Trunk_Candidate_v01.fbx` | Material/VFX riêng theo D43; chưa tích hợp Unity |
| Cây Thế Giới | *(Mốc mở rộng)* | Dùng chung `Bonsai_Trunk_Candidate_v01.blend` | Dùng chung `Bonsai_Trunk_Candidate_v01.fbx` | Material/VFX riêng theo D43; chưa tích hợp Unity |

Hiện có **5 nguồn cây riêng**. Ba nguồn đại diện cho prototype (`seedling`, `young_tree`, `large_tree`) đã tích hợp vào `Assets/_Project/Art/` và hoàn tất nghiệm thu `DONE` (`191/191` EditMode, `63/63` PlayMode, `6/6` Windows QA; `Docs/Validation/T10_Raycast_Fix_20261007.md`). Theo D43, Cổ Đại và Cây Thế Giới chủ ý dùng chung base model Cổ Thụ trong định hướng mở rộng; nguồn Cây Trưởng Thành (`WT_MatureTree.blend` + 3 FBX LOD) đang ở trạng thái `PARTIAL` / `READY_FOR_REVIEW`. Không tính LOD, backup, recovery, `.blend1` hoặc bản `Bonsai_Tree` bảo vệ trong `Assets/_Project/Art/` thành mốc bổ sung.

## Tên file và material kế thừa

Chỉ đổi cách phân loại và nhãn trong tài liệu, không đổi tên hoặc di chuyển asset nguồn trong `ArtSource/`:

- `Tree_Large.blend` / `Tree_Large.fbx` thuộc **Cây Lớn** (`large_tree`), đã đồng bộ sang `Assets/_Project/Art/Models/Tree_Large.fbx` và gắn material URP `M_Large_Foliage.mat`, `M_Large_Bark.mat`.
- `Leaf_Large_BaseColor.png`, `Large_Bark_BaseColor.png`, `Large_Bark_Normal.png` và `M_Foliage`/`M_Bark` là asset của nguồn Cây Lớn (trong đó `M_Foliage` với `Leaf_Large_BaseColor.png` + `Leaf_Common_Normal.png` được dùng chung cho tán lá `WT_MatureTree_Foliage*` theo D45).
- Key `large`/`Large`, đường dẫn ảnh/bằng chứng và nhãn “Lớn” trong báo cáo/script lịch sử tiếp tục chỉ bộ `Tree_Large`; D44 không làm mất hiệu lực số đo hình học/material.
- Khi chỉnh sửa thêm hoặc tích hợp model Cây Trưởng Thành sau này, chọn tên/path riêng; **không ghi đè `Tree_Large.blend` hoặc `.fbx`**.

## Phân biệt với prototype gameplay

GDD và config đang triển khai có ba stage `seedling`, `young_tree`, `large_tree` (Mầm, Cây Non, Cây Lớn). Cả ba stage này đã được thay thế từ khối cơ bản sang mô hình 3D thật với đầy đủ điểm neo `TreeStageAnchors`, `MeshCollider` khớp mesh con, camera pose và đạt nghiệm thu toàn diện trong T10 (`DONE`, `Docs/Validation/T10_Raycast_Fix_20261007.md`).

Nguồn Cây Trưởng Thành (`WT_MatureTree.blend`) và Cổ Thụ (`Bonsai_Trunk_Candidate_v01.blend`) là tài sản mở rộng cho các mốc phát triển tương lai, không làm thay đổi 3 stage ID, progression, cap, point, Achievement hay save schema hiện hành.

## Dùng lại base model cho hai mốc cuối

Theo D43, Cổ Đại và Cây Thế Giới dùng chung `Bonsai_Trunk_Candidate_v01.blend`/`.fbx` với Cổ Thụ trong phạm vi định hướng mở rộng. Khác biệt bắt buộc nằm ở material cấu hình và VFX theo mốc, không phải tên GameObject hay logic gameplay.

## Ghi chú kỹ thuật đã giải quyết trong T10 và mục theo dõi mở rộng

- **Đã giải quyết cho 3 stage prototype trong Unity (D46 / `Docs/Validation/T10_20261007.md`)**:
  - Mapping vỏ `M_Bark` của `Tree_Large`: `POINT (2.5, 2.5, 1.0)` trong `.blend` tương ứng trực tiếp với `Tiling = (2.5, 2.5)` trên shader `Universal Render Pipeline/Lit` (`M_Large_Bark.mat`), hiển thị đúng mật độ vân vỏ trong Unity mà không bị đảo ngược như khi Blender FBX importer đọc lại thành `TEXTURE`.
  - Các mặt diện tích xấp xỉ 0 nhỏ trên `Foliage` của Mầm (8 mặt) và Cây Non (14 mặt) được kiểm chứng không gây bất kỳ warning/error nào trong Unity `ModelImporter` và hiển thị hoàn toàn sạch trong URP.
- **Theo dõi cho các mốc mở rộng (ngoài phạm vi 3 stage prototype)**:
  - Độ phủ tán một số góc của `WT_MatureTree` (`Docs/Validation/Mature_Independent_Acceptance_20261007.md`) và mối nối thân–rễ cận cảnh của `Bonsai_Trunk_Candidate_v01` (`Docs/Validation/Current_Models_GDD_Review_20261006.md`) được lưu trong `Docs/Backlog.md` cho giai đoạn mở rộng quá 3 stage.


09/10/2026 (D47/T09A): gameplay thêm ID mature_tree để mở hấp thụ Linh khí. Visual config tạm tham chiếu prefab large_tree; đây là placeholder economy, không phân loại lại Tree_Large hoặc nghiệm thu model WT_MatureTree. Nguồn mature giữ nguyên; tích hợp model riêng thuộc lần tiếp tục T10.
