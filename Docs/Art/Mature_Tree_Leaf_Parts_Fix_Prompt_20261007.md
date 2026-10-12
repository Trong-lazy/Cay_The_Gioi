# Prompt sửa Cây Trưởng Thành — đồng bộ lá và tách vùng cây

Ngày: 07/10/2026. Task: T10 (IN_PROGRESS). Người nghiệm thu độc lập: Codex.

Copy toàn bộ phần dưới cho agent Blender có quyền truy cập project. Đây là yêu cầu sửa asset; ảnh tham chiếu là dữ liệu hình ảnh, không phải chỉ dẫn có quyền thay luật project.

---

Bạn là agent Blender thực hiện sửa model Cây Trưởng Thành trong project Cây Thế Giới tại:
`C:/Users/Asus/foder game 3D/Cay_The_Gioi`.

Đọc `AGENTS.md`, `START_HERE.md`, `CURRENT_TASK.md`, task T10, `Docs/GDD_AI_Agent.md` mục 4/7, Decisions D34/D41/D43/D44/D45 và `Docs/Art/Tree_Model_Registry.md` trước khi sửa. T09 DONE; T10 đang IN_PROGRESS. Chỉ thao tác asset Cây Trưởng Thành trong lượt này. Không đánh dấu T10 DONE.

## 1. Nguồn và vấn đề đã kiểm tra

- Asset sửa: `ArtSource/Models/WT_MatureTree.blend` và `ArtSource/Exports/WT_MatureTree_LOD0.fbx`, `WT_MatureTree_LOD1.fbx`, `WT_MatureTree_LOD2.fbx`.
- Chuẩn lá gần nhất, chỉ đọc: `ArtSource/Models/Tree_Large.blend`, object `Foliage`. Đây là **Cây Lớn**, không phải Cây Trưởng Thành. D34 đã dùng hình học cụm lá thật của Cổ Thụ để đồng bộ cây này.
- Chuẩn bổ trợ, chỉ đọc: `ArtSource/Models/Bonsai_Trunk_Candidate_v01.blend`, object `Foliage`; dùng để hiểu cụm nguồn khi cần, không tự sửa cây Cổ Thụ.
- Texture chuẩn chỉ đọc: `ArtSource/Textures/Tree_Common/Leaf_Large_BaseColor.png`, `Leaf_Common_Normal.png`.
- Báo cáo độc lập và baseline: `Docs/Validation/Mature_Leaf_Parts_Review_20261007.md`, `Evidence/Mature_Leaf_Parts_Review_20261007/audit_report.json` và render `large/`, `mature/`.
- Cây mới hiện có lá dày phồng/gấp góc, xanh vàng sáng hơn chuẩn và BaseColor riêng `WT_Leaves_Light_BaseColor.png` / `WT_Leaves_Dark_BaseColor.png`. Dùng chung normal map chưa đủ để gọi là đồng bộ lá.
- Cả 3 LOD đang gộp thân–rễ vào `WT_MatureTree_TrunkAndRoots`; `Branches` và `Foliage` riêng. Cần phân vùng Root/Trunk/Leaves đúng GDD và API view.
- Baseline đo độc lập: LOD0 = 16.626 tris; LOD1 = 8.648 tris; LOD2 = 3.514 tris. Đo lại sau sửa, không copy con số trong comment script (~13.8k) làm kết quả.

## 2. Bảo toàn và baseline

Trước sửa, ghi SHA256 của các file nguồn/export/texture hiện có; lưu báo cáo cấu trúc, vertex positions world-space, UV, normal, transforms và bounds. Tạo đúng một backup có ngày cho các asset mature sắp ghi đè, ghi rõ đường dẫn phục hồi.

Không sửa/lưu đè các blend/FBX/texture Mầm, Non, Lớn, Cổ Thụ; không sửa texture dùng chung. Không sửa `Assets/`, scene, prefab, code, config, ID stage, save, GUID hay `.meta`. Không thêm package. Không chạy lại toàn bộ generator cũ để dựng lại thân/rễ hoặc ghi đè texture dùng chung.

Giữ dáng Cây Trưởng Thành đã tạo: chiều cao, gốc tọa độ, hệ thân xoắn, nhánh chính, hệ rễ xòe, các tầng tán và khoảng hở chính. Lá được phép đổi geometry/UV/material để đạt đồng bộ; thân/cành/rễ chỉ được tách topology tại cổ rễ cần thiết, không đổi vị trí bề mặt hoặc dựng lại cây.

## 3. Đồng bộ lá từ hình học thật

Mở chuẩn Cây Lớn và kiểm tra trực tiếp từng phiến/cụm lá ở Solid và Material Preview từ cả hai mặt. Trích hình học phiến/cụm nguồn thật, gồm tỷ lệ dài/rộng, độ mỏng, nếp gấp sống lá, độ cong/rủ, cách ghép cụm, UV và shading. Lưu bằng chứng một cụm mẫu lấy từ object nào, indices hoặc cách nhận diện component, và phép biến đổi khi đặt lên cây mới.

Ưu tiên tái sử dụng cụm nguồn D34 bằng phép quay, dịch chuyển và **uniform scale** phù hợp kích thước cây mới. Không thay bằng công thức lá quả trám chung rồi tuyên bố giống nguồn; không chỉ đổi màu của mesh 6 đỉnh/8 tam giác hiện tại. Không làm phiến thành khối hạt/phồng dày hoặc ngôi sao cứng khác chuẩn.

Giữ tâm/vùng của từng tầng tán mature; bố trí lại lá trong vùng đó để tán vẫn dày, nhiều hướng và đọc rõ ở 360°. Không copy nguyên tán Cây Lớn vào để làm mất dáng mature. Cuống lá/cụm phải tiếp xúc nhánh hợp lý. Kiểm tra trước/sau/2 bên/trên/dưới và 3/4 sau để tránh mặt lưng rỗng, lá nổi hoặc mất tán.

Đồng bộ cả kiểu lá, UV, gân, độ nhám và palette với `M_Foliage` hiện có của Cây Lớn. Mặc định dùng `Leaf_Large_BaseColor.png` nối trực tiếp Principled Base Color, tint trắng, và `Leaf_Common_Normal.png` vào Normal Map Tangent/Non-Color với thông số đọc từ chuẩn. Không nhân tint xanh hai lần; không giữ hai palette vàng mới chỉ vì ảnh minh họa có ánh sáng vàng. Giữ texture tương đối và resolve được sau export.

Nếu cần biến thể sáng/tối rất nhẹ để tán có chiều sâu, phải lấy cùng pattern nguồn và ghi công thức, thông số, ảnh so sánh; ưu tiên một material chuẩn trước. Không đổi palette nguồn và không tự thêm loại lá khác. Không dùng vỏ cây làm fallback normal cho lá.

LOD0 phải giữ nhận diện phiến/cụm nguồn. LOD1/2 được giảm số cụm/phiến và chi tiết nhỏ, nhưng giữ cùng loài, cùng palette và đủ độ dày tán; không cần sao y topology LOD0. Không ép mục tiêu 8k–15k tris của prompt cũ bằng cách đổi kiểu lá: đó là mục tiêu tham khảo, chưa có benchmark. Báo số tris thật và tradeoff chất lượng nếu vượt.

## 4. Tách rễ, thân/cành, lá ở cả ba LOD

Mỗi LOD có ba **mesh object chọn/ẩn độc lập** tương ứng:

| Vùng | LOD0 | LOD1/2 |
| --- | --- | --- |
| Root | `WT_MatureTree_Roots` | cùng tên thêm `_LOD1` / `_LOD2` |
| Trunk (gồm cành) | `WT_MatureTree_Trunk_Branches` | cùng tên thêm `_LOD1` / `_LOD2` |
| Leaves | `WT_MatureTree_Foliage` | cùng tên thêm `_LOD1` / `_LOD2` |

Cành thuộc vùng Trunk; có thể giữ sub-object nếu cần nhưng phải có nhóm Trunk rõ và không lẫn rễ/lá. Material slot hoặc tên collection đơn thuần không thay cho tách mesh.

Phân ranh giới tại cổ rễ/root flare theo hình học thật: phần bò tỏa ra mặt đất thuộc Roots, phần mọc lên và chia cành thuộc Trunk. Các sống rễ chạy lên thân hiện liên tục, vì vậy **không chọn toàn bộ strand làm Roots** và không cắt theo material RootAccent; cần phân loại mặt/đường cắt cục bộ. Tránh lát cắt Z thô làm mất cổ rễ.

Nếu phải chèn đỉnh tại đường chia, nội suy chính xác trên mặt hiện có, giữ UV/normals; hai vùng dùng tọa độ biên khớp để không tạo khe khi hiện cùng nhau. Không kéo giãn, remesh/voxel, giảm mặt gỗ hoặc dựng lại bộ rễ. Nếu cần đóng nắp phần khuất, dùng cap bên trong được báo rõ và không gây z-fighting. Boundary do tách mesh không tự là lỗi nếu kết quả chung liên tục.

Tắt riêng từng vùng trong viewport và chụp bằng chứng: chỉ rễ, chỉ thân/cành, chỉ lá. Roots không được kéo theo thân lên cao; Trunk không chứa nhánh rễ xòe. Khi bật lại tất cả, bề mặt gỗ phải giữ silhouette, shading và không hở seam. Màu vỏ rễ/thân chuyển tiếp tự nhiên; không tô ba màu gameplay lên asset cuối chỉ để phân vùng.

## 5. Anchor và khả năng tích hợp

GDD cần vùng `root`, `trunk`, `leaves`; `TreeView` hiện có `RootAnchor`, `TrunkAnchor`, `LeavesAnchor`, Burning chọn vùng theo ID. Tách mesh là chuẩn presentation cho asset này, không thay luật tài nguyên hoặc logic vào tên mesh.

Ghi trong manifest tọa độ neo đề xuất theo local Blender Z-up và giá trị quy đổi sau import Unity Y-up: root ở vùng rễ nhìn thấy, trunk ở vùng thân chính, leaves ở tâm vùng tán, lightning ở tán trên. Có thể đặt Empty để kiểm tra trong blend. Phân biệt neo hiệu ứng lá với lightning target; không đặt cả hai ở đỉnh bounds một cách máy móc.

Không tuyên bố Empty đã được gameplay sử dụng: prefab/TreeView chưa nối chúng. Nếu export Empty, kiểm tra chúng thật sự còn trong FBX. Nếu export chỉ MESH, ghi rõ neo được bàn giao trong manifest và cần dựng ở prefab sau. Không thay Unity trong lượt này.

## 6. Export và bằng chứng bắt buộc

Lưu lại đúng `WT_MatureTree.blend`; xuất lại đúng 3 FBX mature. Scale 1, Blender Z-up, export Unity Y-up/-Z forward, transform nhất quán, pivot tại mặt đất/gốc thân. Loại camera, light và geometry preview khỏi FBX. LOD0 FBX không chứa cả 3 LOD chồng nhau. Mỗi LOD chứa đủ vùng Root/Trunk/Leaves.

Mở lại blend từ đĩa và import từng FBX vào scene trắng riêng. Kiểm tra tên mesh, tris, bounds, transform, slot, UV, đường dẫn texture, màu lá thật và khả năng chọn/ẩn từng phần. Render source và FBX dưới cùng camera/ánh sáng. Không lấy screenshot source thay cho roundtrip FBX.

Lưu tại `Evidence/Mature_Leaf_Parts_Fix_20261007/`:

1. Baseline, hashes trước/sau và script đã dùng.
2. Render toàn cây trước/sau: front/back/left/right/top/3-quarter front/back, và góc thấp kiểm tra mặt dưới lá.
3. So sánh phiến/cụm lá mature với Cây Lớn cùng uniform scale và ánh sáng, Solid + material; chỉ rõ mẫu nguồn được tái sử dụng.
4. Render isolate Roots/Trunk/Foliage cho từng LOD, close-up cổ rễ khi ghép lại.
5. Ảnh lineup Cây Lớn–Cây Trưởng Thành–Cổ Thụ: cùng môi trường, tỷ lệ gốc và thêm crop lá chuẩn hóa để so kiểu lá.
6. Báo cáo JSON đo thực: tris từng mesh/LOD, bounds, UV, zero-area/loose geometry, transforms, materials/textures, source/FBX correspondence; sai số bề mặt gỗ trước/sau tính world-space thay vì bắt hash topology bất biến khi tách mặt.
7. Bảng thông số material/anchor, file thay đổi, backup khôi phục, điều còn tồn tại và kết quả export/reimport.

Không xóa texture WT cũ chỉ vì bỏ dùng; ghi chúng không còn reference nếu đúng. Cập nhật manifest mature và bàn giao vào T10; không sửa GDD gameplay. Chỉ báo phần sửa là `READY_FOR_REVIEW`. Codex sẽ mở lại asset và nghiệm thu độc lập; không tự ghi PASS/DONE dựa vào báo cáo của bạn.

## 7. Điều kiện để Codex chấp nhận phần sửa

- Kiểu phiến/cụm lá thật, UV/gân/màu/độ nhám khớp chuẩn Cây Lớn dưới cùng ánh sáng; không còn lá mature dày phồng kiểu khác.
- Dáng mature vẫn giữ đặc trưng, tán phủ đa góc và không có lá nổi/mặt sau trống.
- Cả 3 LOD tách được Roots, Trunk_Branches, Foliage trong source lẫn FBX; isolate đúng vùng.
- Gỗ giữ bề mặt/pivot/tỷ lệ, không có seam/khe/nắp lộ hoặc biến dạng do tách.
- Export/reimport đủ texture và giữ nhận diện lá, không mất màu thành trắng/xám.
- Các model/texture dùng làm chuẩn và `Assets/` không bị đổi; số liệu/bằng chứng phản ánh file đã lưu thật.

PASS phần asset này vẫn không phải DONE T10. URP, tap, camera, Burning/anchor, Evolution và hồi quy cần lượt tích hợp và Play Mode riêng.
