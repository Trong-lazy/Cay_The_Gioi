# Prompt giao agent kiểm tra và tiếp tục T10

Ngày soạn: 07/10/2026. Dán nội dung dưới đây cho agent làm việc trong project.

---

Bạn tiếp quản project Unity **Cây Thế Giới** tại:

`C:\Users\Asus\foder game 3D\Cay_The_Gioi`

Hãy kiểm tra tiến độ thực tế và tiếp tục triển khai **T10 — Chuẩn bị pipeline đồ họa Blender và visual asset cây** tới khi đạt đầy đủ tiêu chí nghiệm thu, hoặc ghi rõ phần chưa hoàn thành và điều kiện đang ngăn kiểm chứng. Yêu cầu này cho phép bạn thực hiện các thay đổi asset, prefab, presentation, config và test cần thiết trong phạm vi T10; hãy làm việc thực tế, không dừng ở báo cáo hoặc kế hoạch. Một agent làm việc với state/scene; không tự giao agent khác hoặc nhắn chat khác.

## 1. Đọc nguồn sự thật và xác minh hiện trạng

Đọc đầy đủ `AGENTS.md`, `START_HERE.md`, `CURRENT_TASK.md`, `Docs/Tasks/10_Art_Pipeline_Blender.md`, `Docs/Tasks/09_Final_Integration.md`, `Docs/GDD_AI_Agent.md` và `Docs/Decisions.md`. Kiểm tra dependency T09 vẫn DONE và không có task nền tảng bị mở lại. Đọc các quyết định PENDING_USER; chỉ hỏi nếu chúng thực sự ngăn phần đang làm, tiếp tục các phần độc lập.

Đọc các tài liệu liên quan trước khi sửa:

- `Docs/Technical/Project_Setup.md`, `Architecture.md`, `Data_Model.md`, `Prototype_Config.md`, `Save_Data_Schema.md`, `Cross_Platform.md`.
- `Docs/Art/Tree_Model_Registry.md`, `Docs/Art/Tree_Materials_Manifest_20261006.md`.
- `Docs/Validation/Current_Models_GDD_Review_20261006.md`, `Material_Fixes_Antigravity_20261006.md`, `Mature_Independent_Acceptance_20261007.md`.
- `Tools/Docs/README.md` và cách dùng `Tools/Docs/update_progress.py`.

Thông tin bàn giao để bắt đầu, phải kiểm chứng lại trên đĩa và trong Editor:

- T00–T09 được ghi DONE; T10 IN_PROGRESS, checklist chưa tích. Đây không phải kết quả chạy test mới.
- Ba stage gameplay vẫn là `seedling`, `young_tree`, `large_tree`.
- Ba nguồn prototype có thật, nhưng lần kiểm tra gần nhất chưa được nhập vào `Assets/_Project/Art/Models/`; scene/prefab gameplay vẫn dùng primitive.
- Material Blender/FBX không chứng minh material URP đã hoạt động trong Unity.
- Review cũ ghi nhận mapping vỏ Cây Lớn khác nhau giữa Blender source và FBX reimport, 8/14 mặt diện tích 0 của Mầm/Non, cùng một số thông tin manifest chưa khớp. Xác minh lỗi nào còn tồn tại và lỗi nào ảnh hưởng trực tiếp việc import/hiển thị/kiểm thử T10; không tự coi mọi ghi chú lịch sử là lỗi hiện tại.

Đọc source thực tế: `TreeView`, `TreeInputAdapter`, `OrbitCameraRig`, `EvolutionTransition`, `WeatherView`, `CosmeticView`, `GameBootstrapper`, config visuals, ba prefab cây đang được config tham chiếu và các test liên quan. Hiện `TreeView` suy ra anchor/collider từ bounds tổng; đánh giá lại khi dùng model có rễ dưới mặt đất, tán rộng và tỷ lệ khác placeholder.

Trước khi sửa, kiểm tra scene/state/Console Unity thực tế và mọi thay đổi chưa lưu. Giữ T10 IN_PROGRESS, chạy updater nếu con trỏ lệch. Kiểm tra ranh giới repository trước khi dùng git; không quét hoặc thay đổi dữ liệu ngoài project nếu git root nằm ở thư mục cha.

## 2. Giữ đúng phạm vi

Giữ Unity **6000.6.1f1**, URP, Input System, một MainScene, gameplay offline và ba stage hiện có. Không nâng phiên bản, thêm package không cần thiết, đổi economy, save schema, stage ID hoặc thêm progression mới.

Theo D44, `Tree_Large` là **Cây Lớn**. Theo D43, Cổ Đại/Cây Thế Giới dùng chung base Cổ Thụ khi được triển khai sau này. `WT_MatureTree` là nguồn **Cây Trưởng Thành**, review hiện tại PARTIAL do độ phủ tán; nó không nằm trong ba stage acceptance T10. Ghi nhận tình trạng mature trong bàn giao/backlog, không tự thêm stage hoặc bắt T10 ba stage phải chờ polish mature.

Nguồn cần tích hợp:

| Gameplay stage | Blender source | FBX export |
| --- | --- | --- |
| `seedling` | `ArtSource/Models/Tree_Seedling_Simple_v01.blend` | `ArtSource/Exports/Tree_Seedling_Simple_v01.fbx` |
| `young_tree` | `ArtSource/Models/Tree_Young_Simple_v01.blend` | `ArtSource/Exports/Tree_Young_Simple_v01.fbx` |
| `large_tree` | `ArtSource/Models/Tree_Large.blend` | `ArtSource/Exports/Tree_Large.fbx` |

Ưu tiên giữ hình dáng đã có. Chỉ sửa model/UV/export khi lỗi được đo và trực tiếp cản T10; ghi nguyên nhân, số đo trước/sau, bảo toàn phần không liên quan và kiểm chứng reimport. Không sculpt/polish tổng thể hoặc dựng lại model. Không ghi đè mature, Cổ Thụ hay `Assets/_Project/Art/Bonsai_Tree.*` để làm ba stage.

Windows là nền tảng nghiệm thu trước. Mobile thiết bị thật vẫn DEFERRED theo D12 và không chặn DONE Windows; duy trì input cảm ứng, landscape và kiến trúc dùng chung, kiểm tra mô phỏng Editor phù hợp.

## 3. Triển khai pipeline và tích hợp Unity

1. Xác nhận hash/file nguồn, cấu trúc mesh, UV/material slots, scale, hướng trục, pivot và kích thước. Ghi pipeline tái lập Blender → FBX → Unity; phân biệt transform source, importer và prefab. Kiểm tra thực tế model đứng đúng hướng/đúng tỷ lệ trong Unity, không chỉ đọc thông số FBX.
2. Nhập ba FBX vào `Assets/_Project/Art/Models/` và các texture đang được chúng dùng vào thư mục art phù hợp. Unity tạo/import `.meta`; không chỉnh GUID hoặc `.meta` bằng tay. Khi di chuyển asset đã có, dùng Unity và giữ tham chiếu. Không sửa Library/Temp/project C# sinh tự động.
3. Tạo/remap material `Universal Render Pipeline/Lit` rõ ràng. BaseColor dùng sRGB; normal dùng Texture Type Normal map và cấu hình đúng hướng. Với texture đã bake màu, tint trắng; roughness chuyển thành smoothness theo dữ liệu đã xác minh. Đối chiếu manifest với source/Unity thực tế, đặc biệt UV/tiling vỏ Cây Lớn; không lấy shader Blender hoặc render FBX trong Blender làm nghiệm thu URP. Giữ nhiều material slot của vỏ hạt Mầm.
4. Dựng/cập nhật ba prefab và liên kết vào `PrototypeConfig.visuals`. Giữ pivot ground/root hợp lý, scale thống nhất và renderer bounds chính xác. Mỗi prefab có điểm neo chức năng `Anchor_LightningTarget`, `Anchor_LeavesCenter`, `Anchor_TrunkAura` và root anchor nếu hệ thống cần. Nối bằng component/reference có kiểu; tên object là quy ước authoring, không phải điều kiện gameplay.
5. Sửa presentation tối thiểu để TreeView, Weather/Burning và cosmetic sử dụng neo đúng của prefab đang hoạt động. Neo sét và tâm lá có thể ở vị trí khác nhau. Giữ API cũ tương thích khi cần; tránh bounds tổng tự đẩy aura vào tán hoặc neo rễ xuống điểm sâu nhất.
6. Chọn collider phù hợp để tap thân/lá/rễ nhìn thấy ở cả ba stage; kiểm tra raycast và vùng trống quanh tán, mặt đất/UI, trước/sau Evolution/Rebirth. Không nhận đôi touch/mouse, không tính kéo camera thành tap. Nếu collider cầu cũ bắt khoảng trống quá rộng, điều chỉnh có bằng chứng; không mặc định dùng MeshCollider cho mọi mesh.
7. Cập nhật camera pose/pivot/distance trong config theo từng model. Kiểm tra góc xoay, clipping, cây/rễ/tán trong khung và blend Evolution; không nhét công thức gameplay vào Bootstrapper.
8. Lưu scene/prefab/config bằng Unity, giữ MainScene là scene gameplay/build. Ghi config/value thay đổi và lý do; cập nhật Decisions cho quyết định mới nếu có.

Đọc skill Unity MCP của môi trường trước khi dùng và kiểm tra đúng instance/project/editor state trước thao tác. Dùng MCP để kiểm tra Editor/scene khi khả dụng. Nếu mất kết nối, tiếp tục phần độc lập và thử phương án kiểm chứng phù hợp có sẵn; không tuyên bố đã thử trong Editor khi chưa thử được. Tránh chạy setup tái tạo toàn bộ scene làm mất các hệ thống T01–T09.

## 4. Kiểm chứng trực tiếp và hồi quy

Dùng save/directory kiểm thử riêng để bảo toàn save người dùng. Ghi rõ Unity version, scene, config version, nền tảng, thời điểm và cách tạo từng trạng thái test. Không gọi API debug rồi mô tả thành thao tác click/touch thực.

- Compile và Console: ghi log đầu/cuối, xử lý lỗi liên quan; phân biệt lỗi gameplay với lỗi công cụ và lịch sử. Không clear log để che lỗi.
- Play Mode trực tiếp cả ba stage: model/material/transform đúng, anchor theo đúng vùng, tap chuột và touch mô phỏng, tap bị UI chặn, kéo camera không cộng tap, không double input.
- Evolution Mầm → Non → Lớn: model đổi đúng, camera blend mượt, locks được trả, tap tiếp tục hoạt động. Rebirth/Forced Rebirth trả về Mầm cùng model/neo/camera/cosmetic hợp lệ.
- Weather: sét tới neo phù hợp; Burning trên các vùng mà luật/config hiện tại cho phép. Không ép Giông vào stage bị khóa chỉ để có ảnh. Có thể dùng hook Editor với trạng thái thử được ghi rõ.
- Cosmetic `glow_placeholder`: preview/equip/restore gắn đúng thân ở ba stage và sau thay model, không chặn raycast.
- Save/load: ba stable stage ID và dữ liệu tài nguyên/level/permanent vẫn tương thích; load state cũ hiển thị đúng prefab/camera. Kiểm tra lifecycle liên quan sau sửa.
- Chạy toàn bộ EditMode và PlayMode theo task. Bổ sung hoặc điều chỉnh test có ý nghĩa cho anchor/collider/stage transitions; thay assertion phụ thuộc placeholder bằng assertion hành vi tương đương, không nới test chỉ để PASS. Lưu XML/log thực tế và số pass/fail; không dùng kết quả T09 cũ làm test T10.
- Theo nghiệm thu chung của T10, build và smoke test Windows Standalone sau tích hợp. Không ghi đè build/bằng chứng T09; ghi đường dẫn build/config và kết quả. Kiểm tra art, input, chuyển stage và lỗi Console/player log liên quan. Mobile máy thật vẫn hoãn.
- Chụp ảnh trước/sau trong Unity với điều kiện có thể so sánh; ảnh đủ ba stage, anchor/VFX khi phù hợp, lưu tại `Evidence/T10/` hoặc thư mục phiên có ngày. Không dùng ảnh Blender thay ảnh nghiệm thu Unity.

Chỉ chạy lại hoặc mở rộng test khi thay đổi/lỗi mới cần kiểm chứng. Nếu phát hiện lỗi task DONE làm sai acceptance, xử lý mở lại/đánh giá dependency đúng AGENTS.md; không bỏ qua hồi quy để đóng T10.

## 5. Ghi bàn giao và điều kiện dừng

Tạo `Docs/Validation/T10_<ngày thực hiện>.md` với `Task: T10`, `Date:` và `Result:` phản ánh đúng kết quả. Đối chiếu từng checklist thực hiện/acceptance/nghiệm thu với bằng chứng cụ thể, ghi file/config thay đổi, test commands/jobs, pass/fail, hạn chế và lỗi còn lại. Liên kết Evidence thật. Cập nhật metadata `validation` của T10 tới report nghiệm thu tổng phù hợp, không giữ report mature làm đại diện cho toàn bộ gameplay T10.

Chỉ tích những mục đã kiểm chứng. Chỉ đặt `status: DONE` khi mọi điều kiện trong T10 và AGENTS.md đạt: Unity compile/Console, MainScene Play Mode, full regression/save compatibility, Standalone Windows, Validation PASS, evidence và bàn giao đầy đủ. Nếu code/asset đã xong nhưng chưa kiểm chứng Unity trực tiếp, trạng thái tối đa WAITING_VALIDATION; nếu công cụ/thông tin ngăn phần bắt buộc, ghi BLOCKED với nguyên nhân và cách tháo gỡ. Nếu vẫn còn việc triển khai làm được, tiếp tục IN_PROGRESS.

Cuối phiên chạy `Tools/Docs/update_progress.py` bằng runtime Python sẵn có và kiểm tra Progress/CURRENT_TASK khớp. Không chỉnh tay hai con trỏ này. Chỉ xuất lại GDD Word khi đã thay GDD Markdown, rồi kiểm tra bố cục theo quy định; không đổi GDD chỉ để kể lại lịch sử kỹ thuật.

Trả lời cuối bằng tiếng Việt: trạng thái T10 thực tế; phần đã triển khai; bằng chứng kiểm thử; file/config chính đã đổi; lỗi/hạn chế còn lại và bước tiếp theo nếu chưa DONE. Không tự chuyển sang task ngoài phạm vi.
