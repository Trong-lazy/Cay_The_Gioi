# Kiểm tra model hiện hành và đối chiếu GDD

Task: T10
Date: 2026-10-06
Result: PARTIAL — màu lá FBX đã sửa; còn vấn đề mapping vỏ nguồn Tree_Large, dữ liệu bàn giao, polish hình học và chưa tích hợp art vào Unity. Sau phân loại D42, prototype còn thiếu nguồn mỹ thuật Cây Lớn.

Cập nhật phân loại sau review, theo yêu cầu trực tiếp người dùng/D42 ngày 06/10/2026: `Tree_Large` chuyển từ Cây Lớn sang **Cây Trưởng Thành**. Bảng mốc hiện hành ở mục 2/5 dưới đây đã cập nhật; số đo/ảnh và các nhãn Lớn/large trong chẩn đoán vẫn chỉ cùng file đã audit, không chạy audit mới hoặc sửa asset vì đổi mốc. Danh mục nguồn đang dùng: `Docs/Art/Tree_Model_Registry.md`.

## 1. Phạm vi, phương pháp và giới hạn

Người dùng yêu cầu kiểm tra model hiện tại, những điểm cần chỉnh và model còn thiếu so với GDD. Chỉ review; không sửa/lưu blend, export FBX, sửa texture/manifest, nhập asset vào Unity hoặc gửi việc cho agent khác.

- Đọc START_HERE, con trỏ/T10, GDD Markdown chính, Backlog, quyết định D01–D12 liên quan phạm vi và D34–D41 liên quan art; đối chiếu config/prefab/TreeView thực.
- Blender 5.2.2 LTS background mở lại cả bốn nguồn trên đĩa, kiểm tra mesh/material/texture, render mới nhiều góc và nhập lại cả bốn FBX trong scene trắng riêng.
- Dùng helper đã được review từ lượt kiểm tra trước nhưng chỉ nạp định nghĩa hàm, không chạy hoặc ghi đè review cũ. Bằng chứng mới tại `Evidence/Current_Model_GDD_Review_20261006/` gồm `audit_report.json`, `surface_details.json`, hai script đọc-only và các render thật.
- So geometry với dữ liệu review độc lập trước Antigravity, không chỉ với baseline do agent tự lập. Cả 18 file art đầu vào (4 blend, 4 FBX, 10 PNG) và 2 file Bonsai được bảo vệ, tổng 20 file, giữ SHA256 suốt lượt review; kiểm tra lại cuối phiên không có file đổi.
- Unity MCP hiện kết nối đúng `Cay_The_Gioi`, Unity 6000.6.1f1, MainScene, không Play/compile. Dùng skill Unity và resource-first; query mesh/prefab bằng C# chỉ đọc trong bộ nhớ sau khi resource component bị lỗi. Scene `isDirty=False`; không sửa Assets, scene hoặc config.
- Không vào Play Mode hoặc thử URP với model mới. Không kiểm tra Blender GUI chưa lưu. Review này không phải nghiệm thu gameplay T10 hoặc hiệu năng mobile.

## 2. Bộ nguồn cây hiện tại

| Mốc đang dùng trong bàn giao | Nguồn tại ArtSource/Models | Tris thực đo | Nhận xét |
| --- | --- | ---: | --- |
| Mầm | Tree_Seedling_Simple_v01.blend | 2.778 | Có thân/rễ, hai lá lớn, chồi nhỏ, hai nửa vỏ hạt; màu và gân lá cơ bản ổn. |
| Cây Non | Tree_Young_Simple_v01.blend | 4.574 | Có thân hóa gỗ, lá nhiều hướng và rễ 3D; màu FBX giữ đúng. |
| Cây Trưởng Thành | Tree_Large.blend | 26.682 | Chuyển mốc theo D42, giữ tên file; lá/cụm tán giữ D34, mapping vỏ qua FBX còn lệch. |
| Cổ Thụ | Bonsai_Trunk_Candidate_v01.blend | 73.888 | Vân vỏ đã đổi sang hướng dọc hợp lý hơn; mối nối thân–rễ vẫn lộ khi cận. |

Bản Bonsai trong Assets, các `.blend1`, Backup và Recovery không được tính là mốc cây mới. Tên kỹ thuật `ancient` trong script/texture không tự đổi mốc Cổ Thụ thành Cổ Đại; bảng dùng cách xếp mốc của bàn giao hiện hành.

## 3. Những phần đã đạt sau Antigravity

- Đủ 10 PNG thực và đường dẫn resolve được. Hash texture khớp bộ Antigravity bàn giao. Lá màu sRGB, normal Non-Color/Tangent; cả bốn cây vẫn cùng mẫu gân nguồn và cùng normal lá.
- R1 đã giải quyết trong roundtrip Blender: cả bốn FBX tự nối đúng map lá màu cuối vào BaseColor; tán không còn trắng/xám. Các map màu cuối không bị nhân tint xanh lần hai trong graph hiện hành.
- Geometry gốc, topology, corner normals, transforms và modifiers của bốn nguồn khớp review độc lập trước sửa. UV lá bốn cây và UV vỏ Mầm/Non/Lớn không đổi; UV vỏ Cổ Thụ đổi như bước R2.
- FBX giữ số đỉnh/tris, UV values và slots; world vertex error lớn nhất khoảng `7.153e-7 m`, không lệch dáng/tỷ lệ đáng kể. Không có subdivision/displacement geometry mới.
- R2 hướng thớ vỏ đã cải thiện rõ: trên thân/cành/rễ Cổ Thụ, texture không còn các vòng sóng ngang toàn cục của bản cũ. Render nguồn và FBX Cổ Thụ tương ứng. Không đánh đồng “hướng UV đúng” với “mọi mối nối hình học đã mượt”.
- Không phát hiện loose edges hoặc cạnh có hơn hai mặt trong kiểm tra mesh nguồn. Các boundary ở cành/rễ và lá card được ghi nhận, không tự coi toàn bộ là lỗi cần đóng kín.

## 4. Thiếu sót và mức ưu tiên

### A. Cần xử lý trước khi chốt bộ art/pipeline

**A1 — Vỏ Cây Lớn không tương đương sau FBX import trong Blender.**

Nguồn `M_Bark` dùng Mapping `vector_type=POINT`, scale `(2.5,2.5,1)`; FBX reimport tạo Mapping `vector_type=TEXTURE` với cùng con số. Với location/rotation bằng 0, hai chế độ này có phép biến đổi scale ngược nhau: nguồn nhân UV 2.5, bản reimport chia UV 2.5. Vì vậy giữ đúng số scale chưa đủ để giữ mật độ vân.

Render nguồn có chi tiết thân/rễ rõ hơn; bản FBX thô nhìn phẳng và vân bị phóng lớn. Chỉ đổi mode Mapping trong bộ nhớ của bản import về POINT, không lưu asset, đã đưa ảnh gần như trở lại nguồn: RMSE RGB toàn ảnh từ `0.0074333` giảm còn `0.0000771` (thang 0..1). Corner normals world của thân có min dot `0.99999983`, nên đây không phải bằng chứng normals hình học bị mất.

Phần cần sửa sau khi được giao: chuẩn hóa mapping đầu ra hoặc quy trình remap và kiểm chứng mật độ vân thực; không chỉ đọc lại ba số Scale. URP phải có bước xác minh riêng, không suy ra Unity cũng mắc chính xác hành vi importer Blender này. Tài liệu mapping hãng hỗ trợ giải thích POINT/TEXTURE: [Blender 5.2 Mapping Node](https://docs.blender.org/manual/id/5.2/render/shader_nodes/utilities/vector/mapping.html).

Bằng chứng: `large/perspective.png` (nguồn), `large_fbx/perspective.png` (RAW import), `large_fbx_mapping_checked/perspective.png` (chẩn đoán sửa mode trong RAM, không phải file đã khắc phục).

**A2 — Manifest và bảng nghiệm thu chưa khớp nguồn hiện tại.**

- Source Non và Lớn đã được lưu sau lần hash agent ghi: hash blend hiện tại khác report/manifest, trong khi hash bốn FBX vẫn khớp agent. Fingerprint geometry vẫn giữ nguyên. Không quy kết ai làm thay đổi; cần cập nhật bảng nguồn sau khi xác định bản bàn giao cuối và kiểm chứng lại.
- Manifest còn nói Cây Lớn có nhánh `Noise -> Bump`; graph đã lưu hiện tại không có nhánh này. Không dùng lý do procedural mất khi xuất để giải thích sai lệch mapping A1.
- Manifest ghi world error `<5e-8 m` cho toàn bộ, nhưng dữ liệu thật lớn nhất khoảng `7.153e-7 m`. Sai số vẫn rất nhỏ, chỉ là lời mô tả chưa đúng.
- Bảng mục 5 báo cáo Antigravity liệt kê thân Lớn 2.868 tris/rễ 1.286 tris; nguồn hiện hành thực tế là 1.408/634 tris, tán 24.640, tổng 26.682. Bảng SeedShell Mầm cũng ghi 918 tris, trong khi hiện hành là 1.024. Không dùng bảng copy làm bằng chứng thay dữ liệu.
- Các phần R3 đã sửa đúng gồm danh sách 10 texture, map lá bake/tint trắng, UV/tiling và cấu hình Both của card Cổ Thụ; cần giữ và chỉnh nốt thông tin lệch, không làm lại toàn bộ material.

**A3 — Chưa đưa các nguồn đã làm vào game.**

Unity query chỉ đọc xác nhận MainScene đang dùng Stem=Cylinder, LeafLeft/LeafRight=Sphere. Cả ba prefab `seedling`, `young_tree`, `large_tree` đang được config trỏ tới cũng chỉ có Cylinder/Sphere của `Library/unity default resources`. Có RootAnchor/LeavesAnchor/TrunkAnchor và collider placeholder; không phải thiếu hoàn toàn hệ anchor, nhưng chưa nghiệm thu chúng với art mới.

Cần lượt tích hợp T10 riêng: nhập đúng FBX/texture, tạo/gán URP material, thay phần view trong prefab, kiểm tra tap collider, camera Evolution, các anchor sét/cháy/aura, save/load và hồi quy. Với root có phần dưới mặt đất, cần kiểm tra vị trí hiệu ứng root theo cách hiển thị trong game, không tự lấy điểm thấp nhất làm vị trí đẹp. Lá card Cổ Thụ khi được tích hợp cần Both; không cần dựng lại hoặc nhân đôi geometry lá chỉ để nhìn mặt sau.

### B. Nên làm sạch trước bàn giao art cuối, không phải yêu cầu dựng lại dáng cây

**B1 — Mặt lá diện tích 0 ở hai cây nhỏ.**

Mầm có 8 polygon diện tích đúng 0, Non có 14; nếu tính cả mặt gần 0 (`area < 1e-12 m²`) thì lần lượt 19 và 33. Đây là mặt hình học, khác vấn đề UV mép lá Lớn. Do fingerprint geometry khớp bản trước, không phải lỗi mới do Antigravity đổi màu. Hình render chưa cho thấy lỗi nghiêm trọng ở khoảng nhìn hiện tại, nhưng nên dọn các mặt suy biến bằng một lượt được phép chỉnh dữ liệu mesh nhỏ, bảo toàn silhouette/UV/normal cần thiết và cập nhật số tris sau đó; không tự merge/remesh toàn cây.

**B2 — Cổ Thụ lộ mối nối khi nhìn cận.**

`ancient/bark.png` và `ancient/roots.png` cho thấy các mép/đầu đoạn thân–rễ chồng nhau khá rõ, như ghép các ống. Tắt normal trong RAM vẫn thấy các mép này: không giải quyết chỉ bằng giảm độ sần. Geometry giữ nguyên dáng đã duyệt, nên đây là vấn đề hình khối sẵn có, không phải R2 làm hỏng geometry. Nếu camera game nhìn đủ gần, nên chỉnh tiếp giáp cục bộ trong một lượt được giao riêng, giữ dáng tổng thể; không cần đổi hệ rễ hoàn toàn.

**B3 — Polish tùy chọn.**

Vỏ Cổ Thụ hiện hơi sần mạnh khi cận và một số đường gân/mép lá Lớn quá đều hoặc sáng. Có thể tinh chỉnh nhẹ contrast/normal sau khi xem trong URP. Đây là đánh giá thẩm mỹ, không coi bắt buộc đổi palette hoặc tán đã được người dùng thích. Không tự thêm rig, gió, LOD hoặc tối ưu Cổ Thụ chỉ từ số tris; hiệu năng chưa được đo.

## 5. Model còn thiếu so với GDD

Nguồn luật: `Docs/GDD_AI_Agent.md` mục 2–3 chỉ yêu cầu ba stage đại diện cho prototype; `Docs/Backlog.md` ghi bốn mốc mở rộng Trưởng Thành, Cổ Thụ, Cổ Đại, Cây Thế Giới. Không lấy ý tưởng tương lai thành acceptance T10 hiện tại.

| Chuỗi mốc concept | Có nguồn model riêng hiện hành? | Thuộc prototype đang triển khai? |
| --- | --- | --- |
| Mầm | Có: Tree_Seedling_Simple_v01 | Có |
| Cây Non | Có: Tree_Young_Simple_v01 | Có |
| Cây Lớn | Chưa có nguồn phù hợp được chọn sau D42 | Có; vẫn dùng placeholder trong gameplay |
| Cây Trưởng Thành | Có: Tree_Large (tên kỹ thuật giữ nguyên) | Không, mở rộng |
| Cổ Thụ | Có: Bonsai_Trunk_Candidate_v01 | Không, đã chuẩn bị art trước |
| Cổ Đại | Chưa có | Không, mở rộng |
| Cây Thế Giới | Chưa có | Không, mở rộng |

Kết luận số lượng:

- Prototype ba mốc: sau D42 **thiếu 1 nguồn model cây — Cây Lớn**; placeholder gameplay vẫn tồn tại và chưa thay bằng art mới. Kết luận “thiếu 0” ở thời điểm trước phân loại không còn áp dụng.
- Nếu mỗi mốc concept có một model riêng, đủ chuỗi bảy mốc: **cần thêm 3 model — Cây Lớn, Cổ Đại, Cây Thế Giới**. Người dùng sẽ tìm/chọn sau; không tự dựng/tải hoặc thêm config/stage/save.
- Giữ Bonsai ở mốc Cổ Thụ theo bàn giao hiện hành; D42 chỉ chuyển Tree_Large sang Trưởng Thành, không đổi mốc Bonsai hoặc dùng một nguồn cho hai mốc riêng.

Ngoài cây: GDD hiện có Ground placeholder; có thể thay bằng một nền/bệ đất dùng chung ở bước mỹ thuật. Sét, mưa, Burning, aura và Evolution là asset hiệu ứng/VFX/material hoặc animation view, không mặc định mỗi hiệu ứng cần một model Blender mới. GDD không bắt buộc model hạt chưa nảy riêng, cây chết riêng, chậu, nhà hoặc nhiều map cho prototype; không cộng chúng vào số cây còn thiếu. Bộ rễ/thân/lá đã tách mesh, không cần tạo ba model cây riêng cho ba nhánh Upgrade.

## 6. Kiểm tra Unity và lỗi công cụ

Trước query, Console đọc được 0 Error/Warning; MainScene sạch, Play stopped, không compilation pending. Hai resource component của MCP lỗi `TargetInvocationException -> NotImplementedException` và sinh 2 Error log thuộc package MCP. Không xóa log, không sửa package, không coi chúng là lỗi material/gameplay. Query C# chỉ đọc mesh/prefab thay thế thành công; scene vẫn sạch/không Play. Chi tiết kết quả lưu tại `unity_readonly_snapshot.json`.

Không có Play Mode/test suite/build mới; không đánh dấu acceptance T10 hoặc Unity shader PASS. Hạn chế công cụ không ngăn kết luận scene đang dùng placeholder vì đã có query trực tiếp và đối chiếu YAML/config.

## 7. Bàn giao

Ưu tiên đề xuất: xử lý A1/A2 → dọn B1 và quyết định có cần B2 ở khoảng camera mục tiêu → tích hợp/kiểm chứng ba cây prototype (A3) → sau đó mới giao ba mốc mở rộng nếu người dùng muốn đủ concept.

Chỉ thêm script đọc-only, JSON/render bằng chứng, report này, ghi chú dẫn chiếu report Antigravity và bàn giao T10; không sửa model, texture, FBX, manifest, GDD, Decisions, Assets hoặc package. T10 vẫn đIN_PROGRESS; chạy công cụ tổng hợp tiến độ, không tự DONE.
