# Prompt Antigravity — sửa các lỗi material còn lại

Ghi chú D42 ngày 06/10/2026: bộ `Tree_Large` được chuyển sang mốc **Cây Trưởng Thành**, giữ nguyên đường dẫn source/FBX/texture. Mọi nhãn “Cây Lớn/Lớn” chỉ bộ `Tree_Large` trong prompt cũ này được hiểu là Trưởng Thành; không dùng nó làm nguồn cho mốc Cây Lớn mới, không đổi ID gameplay `large_tree`. Đọc `Docs/Art/Tree_Model_Registry.md` trước khi giao tiếp việc. Đây không phải lệnh chạy lại bước sửa đã làm hoặc dựng các model còn thiếu.

Ngày soạn: 06/10/2026. Dùng bằng cách gửi toàn bộ file này cho Antigravity trong project bên dưới.

Đây là lệnh triển khai dành cho agent nhận việc, không phải báo cáo đã sửa. Phiên Codex soạn file chỉ thay tài liệu, chưa thay model, FBX, texture hoặc Unity.

## 1. Yêu cầu thực hiện

Bạn đang làm việc tại:

`C:\Users\Asus\foder game 3D\Cay_The_Gioi`

Hãy khắc phục R1, R2, R3 trong `Docs/Validation/Materials_Independent_Review_20261006.md`, kiểm tra lại bằng dữ liệu và render thực. Thực hiện tuần tự trong một lượt nếu công cụ cho phép. Chỉ xử lý material/texture/UV vỏ cần thiết và xuất lại bộ art hiện có; không mở rộng thành tích hợp gameplay.

Đích bàn giao: bốn model giữ nguyên hình khối đã duyệt, vỏ Cổ Thụ đọc đúng thớ cây, màu lá không mất khi xuất, bảng tái tạo material URP đủ và đúng. Không được chỉ sửa báo cáo để biến kết quả PARTIAL thành PASS.

## 2. Đọc và kiểm tra trước khi sửa

Đọc `AGENTS.md`, `START_HERE.md`, `CURRENT_TASK.md`, task T10 được con trỏ dẫn tới, các quyết định D34–D40 và những quyết định chưa chốt có liên quan. T10 là task đang hoạt động; T09 đã DONE. Không mở task song song hoặc cho nhiều agent sửa chung file/scene.

Đọc tiếp:

- `Docs/Validation/Materials_Independent_Review_20261006.md`: nguồn lỗi hiện hành, ưu tiên hơn kết luận PASSED cũ khi có mâu thuẫn.
- `Evidence/Material_Independent_Review_20261006/audit_report.json`: số liệu nguồn/FBX/nodes/UV/hashes của lượt kiểm tra độc lập.
- `Docs/Art/Tree_Materials_Surface_Prompts_20261006.md` và `Docs/Art/Tree_Materials_Manifest_20261006.md`.
- `Docs/Validation/Tree_Materials_P05_20261006.md` và script triển khai P02/P04/P05 liên quan trong `Tools/Blender/`.

Xác nhận file hiện hành và tool Blender thật. Nếu dùng Unity MCP để kiểm tra chỉ đọc, đọc skill Unity của môi trường trước; giữ Unity 6000.6.1f1, URP 17.6.0 và Input System hiện có. Nếu không kết nối được Editor, ghi rõ, không nói đã kiểm tra Console/URP.

Nếu Blender GUI có thay đổi chưa lưu, không tự save/revert/đóng phiên người dùng. Làm với file trên đĩa bằng phiên background riêng; nếu việc đó có nguy cơ ghi đè chỉnh sửa mới chưa thống nhất, hỏi người dùng trước khi lưu file bị ảnh hưởng. Phần độc lập có thể tiếp tục.

## 3. File được phép sửa và những phần phải giữ

Sửa trực tiếp bốn nguồn hiện có, không tạo model mới:

| Mốc | File nguồn | FBX xuất đè tương ứng | Tris hiện hành |
| --- | --- | --- | ---: |
| Mầm | `ArtSource/Models/Tree_Seedling_Simple_v01.blend` | `ArtSource/Exports/Tree_Seedling_Simple_v01.fbx` | 2.778 |
| Non | `ArtSource/Models/Tree_Young_Simple_v01.blend` | `ArtSource/Exports/Tree_Young_Simple_v01.fbx` | 4.574 |
| Lớn | `ArtSource/Models/Tree_Large.blend` | `ArtSource/Exports/Tree_Large.fbx` | 26.682 |
| Cổ Thụ | `ArtSource/Models/Bonsai_Trunk_Candidate_v01.blend` | `ArtSource/Exports/Bonsai_Trunk_Candidate_v01.fbx` | 73.888 |

Texture nằm tại `ArtSource/Textures/Tree_Common/`. Được sửa hai map vỏ Cổ Thụ hiện có và thêm bốn PNG màu lá đầu ra được nêu ở bước R1. Không tạo thêm bản `.blend`/FBX candidate, final, copy, v02 hoặc backup model dư. Tắt tạo backup tự động ở thao tác lưu của lượt này; không xóa các `.blend1` đã có từ trước.

Khóa tuyệt đối tọa độ đỉnh, topology, corner normals, silhouette, số/vị trí/hình lá, bố cục tán XYZ, thân/cành/rễ, pivot, units, transforms và mesh/material slot assignments. Không sculpt, remesh, decimate, thêm subdivision, displacement geometry, đổi tên object hoặc vật liệu để né lỗi.

Khóa UV lá của cả bốn cây. Rìa lá Cây Lớn có UV cố định/suy biến đã tồn tại từ D34, không tự unwrap lại toàn bộ lá để xóa thống kê này. Đây không phải hình học suy biến mới do material.

Chỉ được thay UV/mapping trên `Trunk_Branches` và `Roots` của Cổ Thụ để sửa R2; không thay geometry hoặc corner normals. Giữ tên/thứ tự UV layer, kênh UV0 và tránh tạo UV layer rác. UV vỏ của ba cây còn lại giữ nguyên trong lượt này. Nếu cần vượt phạm vi đó, báo lý do và xin hướng dẫn.

Không sửa bất kỳ file nào trong `Assets/`, đặc biệt `Assets/_Project/Art/Bonsai_Tree.blend` và `.fbx`. Không sửa scene/prefab, gameplay, camera, collider/anchor, config, stage/save ID, package, ProjectSettings, `.meta`, Library hoặc Temp. Cổ Thụ vẫn là art source, không tự thêm thành stage gameplay thứ tư.

Trước khi sửa, ghi SHA256 và fingerprint từng mesh (vị trí, topology, normals, transforms, UV values, slot assignments), texture và hai file Bonsai được bảo vệ. Hash cả file blend được phép đổi sau sửa material, nhưng fingerprint geometry phải giữ. Dùng baseline vừa ghi cho lượt này; không giả định `.blend1` luôn là bản trước material đúng nhất.

## 4. R1 — giữ màu lá khi xuất, không phụ thuộc Mix node

Lỗi đã xác nhận: bốn source có Image Texture → Mix Multiply → Principled Base Color; reimport FBX mất BaseColor/tint lá và trả về màu mặc định. Normal texture vẫn còn. Giữ material slot không đồng nghĩa giữ màu.

Phương án triển khai của lượt này: tạo bốn map BaseColor đã chứa màu cuối của lá, cùng xuất phát từ một mẫu gân lá chung. Đây là bốn texture đầu ra khác màu, không phải bốn model hoặc bốn thiết kế lá khác nhau.

1. Đọc graph đang dùng trước khi sửa; chụp render và lưu giá trị tint/factor thực. Với graph đã audit, màu linear là `C = T × ((1 - f) + f × B)`, trong đó T là tint, B là texture chung sau giải mã sRGB và f lần lượt 0.70/0.85/1.00/1.00. Nếu graph hiện hành khác, xác định công thức thực, không dùng con số lịch sử một cách mù quáng.
2. Bake hoặc tính đúng BaseColor của graph, không bake ánh sáng, AO, shadow, normal, exposure hoặc AgX vào ảnh. Tính trong linear rồi encode sRGB đúng một lần; kiểm tra tránh gamma sai hoặc nhân tint hai lần.
3. Lưu PNG 512×512 tại thư mục texture chung:
   - `Leaf_Seedling_BaseColor.png`
   - `Leaf_Young_BaseColor.png`
   - `Leaf_Large_BaseColor.png`
   - `Leaf_Ancient_BaseColor.png`
4. Giữ `Leaf_Common_BaseColor.png` làm nguồn pattern trung tính và giữ nguyên `Leaf_Common_Normal.png` dùng chung. Không tự thiết kế lại vân/hình lá hoặc đổi palette. Bốn map đầu ra phải giữ màu và mức blend đã nhìn thấy trong source trước sửa.
5. Trong mỗi source, nối trực tiếp Image Texture đầu ra tương ứng vào Principled Base Color bằng UV0. Không để Mix/Multiply làm điều kiện cho màu xuất. Ghi rõ màu đã baked; Base Map tint của đầu ra URP là trắng `#FFFFFF`, không dùng lại tint xanh cũ. Kiểm tra cả thiết lập màu material/exporter để không nhân tint lần hai.
6. Giữ roughness/normal strength theo source hiện hành và normal-map Tangent/Non-Color. Đường dẫn ảnh tương đối, tồn tại thật; không chỉ packed trong blend. Không thêm shader tùy chỉnh để xử lý lỗi này.
7. So render trước/sau source dưới cùng camera, đèn, color management để kiểm tra màu lá không đổi đáng kể do chuyển graph. Đồng thời đo sai số BaseColor tại các điểm UV đại diện so công thức nguồn; ghi số đo và dung sai PNG thực, không chỉ nói “trông giống”.

Mục tiêu là shader lá cơ bản có texture BaseColor thật để FBX exporter nhận được, đồng thời có đầu ra dễ dựng với URP Lit. Nếu exporter vẫn không giữ được liên kết, sửa cấu hình export/path và kiểm tra lại trong phạm vi. Không lách bằng cách tô xanh phẳng hoặc chỉ render lại source rồi gắn nhãn FBX. Nếu vẫn không giải quyết được, ghi R1 chưa PASS và bàn giao remap đầy đủ, không tuyên bố hoàn tất màu sau import.

## 5. R2 — sửa hướng vân vỏ Cổ Thụ, giữ dáng cây

Xem ảnh lỗi thật `Evidence/Material_Independent_Review_20261006/ancient/bark.png`, cả bản `bark_normal_disabled.png` và ảnh P05. Lỗi có trong BaseColor, không chỉ Normal; giảm Normal Scale một mình không giải quyết được.

- Chỉnh UV vỏ thân/cành/rễ Cổ Thụ để hướng thớ chạy dọc đường phát triển địa phương của từng đoạn. Không chỉ xoay một Mapping node toàn cây: thân, cành ngang và rễ tỏa có hướng khác nhau.
- Ưu tiên tổ chức các island theo chiều dài đoạn thân/cành/rễ; giữ mật độ texel hợp lý, seams tại vùng ít lộ, tránh lằn đổi hướng đột ngột ở cổ rễ và chạc cành. Có thể thay UV seams cần thiết, không thay đỉnh/mặt/normals hình học.
- Nếu vẫn có dải cuộn giống vân đá, sửa `Ancient_Bark_BaseColor.png` và `Ancient_Bark_Normal.png` đồng bộ theo cùng cấu trúc độ cao: rãnh dọc chính không đều, bản vỏ già, khía ngang ngắn là chi tiết phụ. Không tạo các vòng ngang lớn đều như dây quấn.
- Giữ nhóm nâu xám già, độ nhám cao; không làm bóng như gỗ sơn hoặc vỏ đá. Vỏ Cổ Thụ già và sâu hơn Cây Lớn nhưng vẫn cùng họ nghệ thuật; không đổi màu thân Cây Lớn để che sự lệch.
- Chỉ dùng normal map cho chi tiết nổi/chìm, không thêm displacement/sculpt. Map màu sRGB, normal Tangent Non-Color, 1024×1024. BaseColor và Normal phải dùng đúng cùng UV/mapping, không lệch thớ.
- Mapping bark ban đầu là `(2.5,2.5,1)` ở cả Lớn/Cổ Thụ. Nếu Cổ Thụ đổi tiling/offset/rotation để đạt hình ảnh, ghi giá trị cuối thực; không bắt buộc giữ 2.5 nếu nó làm vân sai kích cỡ. Cây Lớn giữ nguyên và ghi đủ tiling hiện có.
- Xem ít nhất trước/sau/trái/phải, hai góc 3/4 và cận thân chính, chạc cành, cổ rễ. Thớ ở cành/rễ phải theo chiều của cành/rễ, không áp hướng dọc Z cho mọi mặt.
- Render cận bật/tắt Normal chỉ trong bộ nhớ để phân biệt lỗi BaseColor với lỗi normal; bản lưu cuối phải bật normal đúng. Đọc lại file đã lưu và kiểm tra lại, không chỉ xem trạng thái chưa lưu trong GUI.

## 6. R3 — sửa manifest theo dữ liệu đầu ra thật

Cập nhật `Docs/Art/Tree_Materials_Manifest_20261006.md`, không tạo một manifest cạnh tranh với bản hiện hành. Với mỗi mesh/material, ghi đủ:

- File source/FBX, tên slot và assignment thực; Mầm có 4 mesh và 5 material dùng, SeedShell có 2 slots. Kiểm đếm lại, phân biệt mesh/material/slot.
- Texture đầu ra, kích thước, định dạng, color space, UV0, wrap/filter đề xuất và đường dẫn tương đối.
- BaseColor đã baked hay cần tint; với bốn map lá màu cuối là tint trắng. Có thể giữ tint/factor cũ ở cột nguồn lịch sử, không hướng dẫn nhân lại chúng.
- Roughness và smoothness tương ứng; Metallic=0, Emission=0; normal file/space/scale, tiling/offset và các bước remap còn cần.
- Tiling vỏ Lớn thực tế 2.5×2.5; Cổ Thụ ghi giá trị cuối sau sửa. Giải thích mapping áp vào cả BaseColor và Normal. Nếu bake mapping vào UV thì chỉ dùng tiling 1 ở material, không áp hai lần. Không để node Mapping phức tạp không thể tái tạo bằng URP Lit mà bỏ qua trong bảng.
- Sửa mô tả nền map `Leaf_Common_BaseColor.png` dựa trên PNG thật (audit cũ ghi góc `#D1D1C8`, không phải `#C4D8A0`); nếu số đo mới khác, ghi dữ liệu mới và cách đo. Phân biệt map trung tính nguồn với bốn map lá màu đầu ra.
- Hướng dẫn Unity URP Lit: Base Map đúng file, tint đúng, normal import là Normal Map/không tạo từ grayscale, smoothness, Opaque, UV0 và Render Face Both cho lá card Cổ Thụ. Không dùng Alpha Clipping/Transparent để che lỗi mesh không yêu cầu.
- Phân biệt rõ “FBX tự giữ được”, “cần remap thủ công”, “chưa thử trong Unity”. Các normal-scale/roughness giữa engine cần kiểm chứng; không khẳng định pixel-perfect Blender=URP từ việc điền cùng số.

## 7. Xuất và kiểm chứng độc lập — bắt buộc

Xuất đè đúng bốn FBX đã nêu; giữ quy chuẩn scale 1, đơn vị mét, forward -Z/up Y, chỉ mesh được duyệt, không camera/đèn test, không animation dư. Không apply transform theo cách đổi geometry gốc; kiểm tra world geometry/pivot sau export.

Mở lại từng blend từ đĩa và reimport từng FBX vào scene trắng trong phiên độc lập. Không dùng scene source đang giữ node để giả lập kết quả import. Kiểm tra:

1. Geometry fingerprint trước/sau khớp; tris bốn cây không đổi. FBX không lệch hình/trục/tỷ lệ; đối chiếu world positions và UV corner values bằng dữ liệu, không chỉ đếm UV layers/slots.
2. UV lá bốn cây và UV vỏ ba cây ngoài Cổ Thụ không đổi. UV vỏ Cổ Thụ chỉ đổi như đã ghi; sau FBX vẫn khớp nguồn cuối. Corner normals nguồn không đổi; tangents cần phù hợp UV mới.
3. BaseColor lá trong cả bốn FBX reimport thực có liên kết tới đúng map màu đầu ra, không trở về `(0.8,0.8,0.8,1)` hoặc tint sai. Kiểm tra file ảnh resolve được khi mở độc lập.
4. Material slot assignments còn đúng, không slot rỗng, không đổi tên để che mất vật liệu; normal map còn hoạt động.
5. Render đối chiếu source cuối và FBX import cho từng cây dưới cùng ánh sáng/camera/color management. Nếu cần remap tham số chưa được exporter giữ, chụp và ghi riêng bản RAW import và bản REMAPPED; tuyệt đối không đặt bản đã remap thành “import tự đúng”.
6. Lineup đủ cả bốn cây không cắt tán/rễ; có ảnh riêng từng cây và cận vỏ Cổ Thụ. Ảnh phải xuất từ model thật, không ảnh AI thay bằng chứng.
7. Hash các asset được bảo vệ và file ngoài phạm vi không đổi; kiểm tra texture paths tương đối, không ảnh missing, không procedural-only chưa có đầu ra.

Không đưa model vào Assets/scene để thử Unity trong lượt này. Việc kiểm tra URP trong Editor/tích hợp gameplay cần lượt được giao riêng. Nếu chưa thực hiện, phần Unity ghi `NOT_TESTED` hoặc `WAITING_VALIDATION`, không PASS và không DONE T10.

## 8. Bằng chứng, bàn giao và điều kiện dừng

Ghi bằng chứng mới, không ghi đè ảnh/reports review gốc:

- `Evidence/Material_Fixes_Antigravity_20261006/`: ảnh before/after, source-vs-FBX, lineup, số liệu kiểm chứng dạng JSON, logs và danh sách hash trước/sau.
- `Docs/Validation/Material_Fixes_Antigravity_20261006.md`: kết quả riêng R1/R2/R3, phương pháp thật, file sửa, số đo, ảnh minh chứng và lỗi còn lại. R4 chỉ ghi nhận, không tự mở rộng thành sửa UV lá hay dọn backup.

Bảo tồn lịch sử P05 và review độc lập; thêm ghi chú superseded bằng báo cáo mới sau khi thật sự sửa và kiểm tra. Ghi bàn giao vào task T10. Nếu đổi quy chuẩn đầu ra texture/UV so D37/D39, ghi bổ sung quyết định triển khai có nguồn là lượt người dùng giao prompt này, không sửa lịch sử thành như đã đúng từ đầu và không đổi luật gameplay/GDD.

Chạy `Tools/Docs/update_progress.py` bằng Python phù hợp để cập nhật con trỏ/Progress. T10 vẫn `IN_PROGRESS`, không tích acceptance gameplay chưa thử. Nếu thiếu công cụ ngăn kiểm tra cần thiết, ghi rõ phần bị chặn; không bịa log, ảnh hoặc test.

Chỉ kết luận phần sửa material đạt khi R1 có màu/texture đầu ra được kiểm chứng, R2 có hình ảnh thớ vỏ hợp lý từ nhiều góc, R3 khớp file thật và geometry được bảo toàn. Nếu chưa đạt mục nào, báo PARTIAL và nêu mục đó thay vì “đã hoàn thành tất cả”.

Thông báo cuối ngắn gọn bằng tiếng Việt: đã sửa gì; giữ nguyên gì; file nguồn/texture/FBX/tài liệu đã đổi; kết quả từng R1–R3 kèm đường dẫn bằng chứng; việc nào chưa thử trong Unity. Dừng tại đó, không tự bắt đầu tích hợp scene hoặc task tiếp theo.
