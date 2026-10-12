# Prompt sửa phần chưa đạt và nghiệm thu lại T10

Ngày soạn: 07/10/2026. Gửi nội dung dưới đây cho agent làm việc trong project.

---

Bạn làm việc trong project **Cây Thế Giới** tại:

`C:\Users\Asus\foder game 3D\Cay_The_Gioi`

Hãy sửa các phần T10 chưa đạt được xác nhận trong `Docs/Validation/T10_Independent_Review_20261007.md`, kiểm thử trực tiếp và hoàn chỉnh hồ sơ nghiệm thu. Yêu cầu này cho phép thực hiện các thay đổi presentation/input/collider/prefab/test và tài liệu cần thiết để sửa lỗi. Tiếp tục tới khi đạt tiêu chí hoặc ghi rõ blocker thực tế; không dừng ở đề xuất. Một agent thao tác scene/state; không tự giao agent khác hoặc nhắn chat khác.

## 1. Tiếp quản đúng trạng thái

Đọc `AGENTS.md`, `START_HERE.md`, `CURRENT_TASK.md`, `Docs/Tasks/10_Art_Pipeline_Blender.md`, `Docs/Decisions.md`, `Docs/Art/Tree_Model_Registry.md`, `Docs/Technical/Architecture.md`, `Cross_Platform.md`, `Project_Setup.md`, `Prototype_Config.md` và `Save_Data_Schema.md`. Đọc đầy đủ report độc lập và các bằng chứng trước khi sửa:

- `Evidence/T10_Independent_Review_20261007/raycast_probe.json` và `raycast_probe.cs.txt`.
- `off_model_click.json`, `off_model_click.cs.txt`.
- `visible_model_miss.json`, `visible_model_miss.cs.txt`.
- `vfx_result.json`, `editor_final.json`, `editmode_result.json`, `playmode_result.json`.
- `standalone_20261007_203714/summary.json` và `run_standalone.ps1` trong cùng thư mục Evidence.
- Báo cáo bàn giao ban đầu `Docs/Validation/T10_20261007.md` và `Evidence/T10/`.

T10 hiện **IN_PROGRESS**, đã bỏ tích implementation collider, acceptance raycast và mục hoàn tất checklist. T09 vẫn DONE. Kiểm tra dependency/con trỏ trước khi sửa; không đặt lại DONE từ kết quả lịch sử.

Kiểm thử độc lập gần nhất: EditMode 191/191, PlayMode 62/62, Windows QA 5/5 PASS. VFX/cosmetic neo đúng ở Cây Lớn. Tuy nhiên các test cũ chưa bao phủ collider vùng trống gần cây/rìa tán, nên những kết quả này không chứng minh acceptance raycast đạt.

Kiểm tra actual Unity instance, version, Console, MainScene, scene dirty/Play state và các thay đổi người dùng trước thao tác. Đọc skill Unity MCP của môi trường trước khi dùng MCP. Không chạy setup tái tạo toàn bộ scene. Giữ Unity 6000.6.1f1, URP/Input System và các package hiện có.

## 2. Tái hiện và xác định nguyên nhân trước khi sửa

Đọc `TreeInputAdapter.cs`, `TreeView.cs`, `TreeStageAnchors.cs`, ba prefab `seedling/young_tree/large_tree`, config visuals, `ArtPipelineSceneTests.cs`, các test input/Evolution/save/cosmetic liên quan. Đối chiếu prefab/scene thực tế: Physics raycast đang nhận SphereCollider của Tree và CapsuleCollider trong prefab chỉ vì hit có TreeView trong parent.

Vào MainScene Play với save thử riêng, camera pose chuẩn Cây Lớn: distance 6.2 m, pitch 20°, targetHeight 1.18 m, yaw như evidence; Game View 1920×1080. Tọa độ screen dùng gốc dưới trái:

| Trường hợp | Điểm screen | Lỗi trước sửa | Kết quả phải đạt sau sửa |
| --- | --- | --- | --- |
| Khoảng trống cạnh tán Cây Lớn | `(611.49, 629.6163)` | Ray không giao mesh nhưng hit CanopyTapCollider, click tăng tap 0→1 | Không cộng tap |
| Điểm trên model tán bên trái | `(573.2952, 412.3293)` | Ray giao mesh khoảng 6.1323 m nhưng collider chỉ hit Ground khoảng 8.0961 m, click giữ tap 1→1 | Nhận đúng một tap nếu điểm vẫn là bề mặt nhìn thấy trong cấu hình tái hiện |

Tái hiện qua MouseState press/release nhiều frame, sau đó TouchState; không dùng `Vitality.TryTap` làm bằng chứng click. Đảm bảo UI không chặn, ALIVE, input không bị pause/focus/offline/transition gate khóa và thời gian press dưới maximumTapSeconds. Đối chiếu ảnh thực với các điểm, không chỉ kết luận từ phép ray hai mặt.

Probe lưới trước sửa ghi Mầm/Non/Lớn lần lượt 168/162/84 điểm collider hit không giao mesh; 3/0/17 điểm mesh có giao nhưng collider bỏ sót. Đây là số mẫu một góc camera, không phải tỷ lệ pixel lỗi toàn game. Probe ray–triangle hai mặt có thể tính mặt bị cull; một số điểm Mầm rất sát Ground. Phân biệt render culling, occlusion và epsilon khi chọn điểm test, không ép mọi số đếm probe thành tiêu chí zero hoặc bỏ qua lỗi cụ thể đã tái hiện.

## 3. Sửa hit detection có phạm vi và cấu hình rõ ràng

Sửa để click/chạm bề mặt cây đang nhìn thấy được nhận, khoảng trống rõ ràng quanh cây không cộng tap, và vật che phía trước/UI vẫn ngăn input đúng. Áp dụng cho **cả ba stage**, không vá hai tọa độ screen hoặc tên `CanopyTapCollider` riêng lẻ.

- Chọn collider/hit validation phù hợp hình model và tỷ lệ stage. Có thể dùng collider proxy theo vùng, MeshCollider tĩnh, hoặc broad-phase kết hợp phép xác minh bề mặt; giải thích lựa chọn, phạm vi chính xác và chi phí. Không coi SphereCollider/CapsuleCollider hiện có hoặc assertion cấm MeshCollider của test cũ là luật thiết kế bất biến.
- Nếu dùng narrow-phase, broad-phase phải bao phủ các phần cây thật để không loại điểm lá/rễ trước bước xác minh. So sánh độ sâu hit cây thật với occluder thật; không tiếp tục nhận Ground thay cho cây chỉ vì proxy collider hụt. Không click xuyên vật che.
- Nếu đọc mesh runtime, bảo đảm importer/readability/cache hoạt động trong Windows player. Không dựa vào UnityEditor API trong player. Tránh quét toàn bộ tam giác mỗi Update, cấp phát mesh.vertices/triangles hoặc bộ nhớ lớn theo frame; tái dùng dữ liệu/cache theo vòng đời stage. Đo chi phí có ý nghĩa cho model thật, không tự tuyên bố tối ưu mobile.
- Nếu dùng MeshCollider, kiểm tra cooking/transforms/culling và raycast thực tế; không tự bật convex làm lấp toàn bộ khoảng trống tán. Nếu dùng proxy, không chỉ tăng radius tới khi bắt được mép lá khiến vùng trống tiếp tục nhận tap.
- Nếu cần dung sai tap cho màn hình nhỏ, đặt trong config presentation/input, giải thích/đo rõ. Dung sai nhỏ không được dùng để hợp thức hóa vùng trống rộng đã tái hiện. Không đổi acceptance hoặc luật thưởng tap để né lỗi.
- Nối presentation bằng component/reference có kiểu, không phụ thuộc tên mesh/anchor trong gameplay. Domain vẫn xử lý command/tài nguyên như cũ. Giữ stage IDs/save schema/economy.
- Giữ UI blocking, phân biệt drag/tap, multitouch, suppress touch/mouse mô phỏng và lifecycle. VFX/cosmetic không trở thành collider chặn input.
- Khi đổi stage, loại collider/cache stage cũ khỏi hit detection; cập nhật/khôi phục đúng sau Evolution, Rebirth, Forced Rebirth và load save. Giữ fallback cho prefab thử cũ khi cần, nhưng fallback không được ghi đè cơ chế của prefab production.

Giữ model, texture, material URP và anchor đã đạt. Không sửa geometry hoặc dựng lại cây để khớp collider. Không mở rộng sang mature/Cổ Thụ hoặc thêm stage. Không nâng Unity, thêm package, sửa GUID/.meta bằng tay, Library/Temp hoặc project C# sinh tự động. Giữ các sửa trên đĩa chưa thuộc phiên của bạn.

## 4. Bổ sung kiểm thử bắt đúng lỗi

Trước fix, thêm regression test hoặc probe tái lập hai trường hợp và chứng minh chúng fail vì lỗi hit detection. Sau fix chúng phải PASS qua device events thật của Input System. Lưu cả kết quả trước/sau; không tạo fixture tưởng tượng đã fail.

- Tất cả ba stage: chọn điểm bề mặt nhìn thấy ở thân/lá/rễ/rìa tán và điểm trống gần silhouette. Thử trước/sau/trái/phải hoặc ít nhất bốn góc yaw, cùng một góc pitch cao hợp lệ. Có ảnh/geometry reference xác định điểm hợp lệ, không chỉ chọn TrunkAnchor/LeavesCenter.
- Dùng camera projection/world điểm để test chạy ở nhiều resolution; giữ thêm hai điểm vàng ở 1920×1080 làm regression rõ ràng. Không hard-code toàn bộ acceptance vào một resolution. Kiểm tra landscape 16:9 và một tỷ lệ landscape rộng hơn cho input/UI dùng chung.
- Điểm hợp lệ nhận một tap qua chuột và một tap qua touch. Điểm trống không cộng; tap trên UI không cộng; drag không cộng; touch+mouse mô phỏng không cộng đôi; nhiều ngón hủy đúng.
- Kiểm tra Ground/vật che thật trước cây, bề mặt cây trước Ground, collider/cache stage cũ và các VFX đang hiện. Không dùng ray hai mặt làm oracle cho mặt đang bị material cull/occlude.
- Giao dịch Evolution thật Mầm → Non → Lớn: model/neo/camera/locks và hit detection sau blend. Rebirth/Forced Rebirth, load save từng stage: trả/khôi phục model và hit detection đúng.
- Nếu điều chỉnh test cũ phụ thuộc primitive hoặc kiểu collider, thay bằng assertion hành vi tương đương và giải thích. Không xóa/nới assertion để che lỗi raycast; không làm test chỉ lặp lại cùng thuật toán hit validation production.

Compile/Console sạch lỗi liên quan, chạy toàn bộ EditMode/PlayMode với kết quả và số test thực tế sau bổ sung. Dùng save/clock thử riêng, snapshot SHA256 save thật hiện hành trước/sau, không dùng hash lịch sử agent làm baseline mới. Khôi phục input settings/native devices, Enter Play Mode Options và override save/clock cuối phiên.

Thử trực tiếp MainScene Play Mode và lưu ảnh/bằng chứng sau fix. Build Windows mới từ code/prefab đã sửa, dùng output mới có ngày/phiên để giữ build T09/T10 cũ. Chạy lại 5 QA scenario với save riêng và chứng minh **raycast sau fix trong Windows player**, vì 5 QA scenario cũ chủ yếu dùng hook domain/UI và không tự chứng minh tap trên silhouette. Có thể bổ sung scenario device-event/probe player có assertion rõ ràng và được opt-in QA; không biến cơ chế QA thành hành vi launch bình thường.

Tham khảo `run_standalone.ps1`: hidden/windowed từng gây focus loss/SUSPENDED; batchmode có rendering/1920×1080 đạt 5/5. Ghi đúng chế độ kiểm thử và hạn chế. Không sửa lifecycle/save chỉ để chạy hidden được, không tự gọi batchmode là playtest tay. Mobile thiết bị thật tiếp tục DEFERRED, không chặn nghiệm thu Windows.

## 5. Sửa hồ sơ nghiệm thu chưa chính xác

1. Bằng chứng `Evidence/T10/blender_audit_step1.json` đang được dẫn nhưng thiếu: tìm log/file thật nếu có. Nếu không có, chạy lại audit nguồn/export đọc-only và lưu script/JSON mới với thời gian/hash thật; cập nhật link tới kết quả mới. Nếu audit không thể chạy, sửa mô tả cho đúng bằng chứng đã có và ghi giới hạn, không tạo báo cáo hồi tố giả. Không sửa model hoặc overwrite evidence cũ để che khoảng trống.
2. Xuất bảng anchor/collider sau fix từ prefab/config đã lưu và đối chiếu Unity live, cập nhật tài liệu đúng theo giá trị mới. Không sao chép bảng cũ vốn đã sai. Giữ kiểm tra scale/pivot/material slots, đặc biệt SeedShell hai slot.
3. Mỗi claim camera framing/Evolution/VFX phải chỉ đúng test/Play/ảnh đã kiểm chứng; phân biệt `ShowStage` trực tiếp với Evolution transaction, listener/API hook với device event, camera-render ảnh với ảnh có HUD.
4. Phân loại chính xác log shader postprocessing bị strip/unsupported, lỗi công cụ khởi tạo Test Runner và lỗi gameplay. Ghi cảnh báo còn lại cùng tác động xác minh; không tuyên bố mọi player log sạch nếu có diagnostic. Không nâng/sửa package hoặc làm shader polish không cần cho T10.

Giữ report review độc lập PARTIAL như lịch sử lỗi đã phát hiện; tạo report nghiệm thu sửa mới `Docs/Validation/T10_Raycast_Fix_<ngày thực hiện>.md` và Evidence phiên mới. Ghi nguyên nhân, file/config sửa, lựa chọn kỹ thuật, test trước/sau, editor/player paths, kết quả runtime, hạn chế và lỗi còn lại. Cập nhật các mô tả hiện hành trong task/START_HERE/registry/Decisions cho nhất quán; không xóa lịch sử failed runs.

## 6. Khi được đóng T10

T10 giữ IN_PROGRESS trong lúc sửa. Chỉ tích lại collider/raycast khi các repro và kiểm thử trực tiếp đạt; chỉ đặt DONE khi **toàn bộ checklist T10 và DoD trong AGENTS.md** vẫn đạt trên phiên bản mới: compile/Console, MainScene Play, acceptance, full regression/save, Windows build mới, Evidence thật và Validation PASS. Các mục đã tích nhưng bị thay đổi ảnh hưởng phải được kiểm lại, không thừa hưởng PASS cũ một cách tự động.

Cập nhật metadata `validation` tới report tổng nghiệm thu sửa phù hợp. Chạy `Tools/Docs/update_progress.py` bằng Python sẵn có, kiểm tra Progress/CURRENT_TASK; không sửa tay con trỏ. Nếu chưa kiểm chứng Unity trực tiếp, trạng thái tối đa WAITING_VALIDATION; nếu thiếu công cụ/thông tin ngăn phần bắt buộc, ghi BLOCKED với lý do cụ thể. Không chạy task tiếp theo. Nếu GDD Markdown đổi, xuất Word và kiểm tra bố cục theo quy định; không cần đổi GDD chỉ để ghi bugfix kỹ thuật.

Trả lời cuối bằng tiếng Việt: đã sửa nguyên nhân gì; hai repro và các stage/góc/input đạt ra sao; số test EditMode/PlayMode; build Windows và bằng chứng raycast player; tài liệu đã sửa; save có bảo toàn không; trạng thái T10 và lỗi/hạn chế còn lại. Không đánh dấu DONE chỉ vì full test cũ hoặc 5 smoke scenario PASS.
