# Setup dự án trước gameplay

Phạm vi T00 là cấu hình dự án, dữ liệu nền tảng, MainScene và kiểm tra khởi tạo. Resource production, tap thưởng, Upgrade và các hệ thống khác bắt đầu ở các task sau. Đó là phạm vi nghiệm thu T00; T01–T05 hiện đã bổ sung gameplay/save/offline theo các Validation tương ứng.

Mục tiêu theo D12 gồm PC Windows, Android và iOS; điện thoại Landscape cố định; ưu tiên hoàn thiện PC Windows trước. Bảng dưới ghi settings PC đã kiểm tra ở T00; T01–T08C đã bổ sung gameplay/save/offline; T09 đã có bản build độc lập Windows tại Builds/Windows. Input cảm ứng, UI SafeArea và lifecycle dùng chung cho cả hai nền tảng; kiểm thử thiết bị mobile thật thực hiện khi có thiết bị.

## Thiết lập mục tiêu

| Thiết lập | Giá trị |
| --- | --- |
| Unity | 6000.6.1f1 theo ProjectVersion.txt |
| Target | StandaloneWindows64 |
| Rendering | URP hiện có, PC_RPAsset cho pipeline nền và quality PC |
| Color space | Linear hiện có |
| Input | Input System mới, ProcessEventsInDynamicUpdate, UI InputSystemUIInputModule |
| UI | uGUI overlay, CanvasScaler 1920×1080 |
| Build resolution | 1920×1080, windowed, runInBackground bật |
| Serialization | Force Text, Visible Meta Files |
| Enter Play Mode | Reload domain và scene theo mặc định |
| Namespace | CayTheGioi |
| Build entry và scene làm việc | Assets/_Project/Scenes/MainScene.unity |
| Play Mode start override | Không ép scene, để Test Runner dùng scene tạm |

Giữ nguyên company/product hiện có để đường dẫn persistentDataPath không tự thay đổi. Không nâng Unity hoặc package. Package lock được giữ trong Packages; không tạo remote Git hay thay repository cha trên máy.

## Cấu trúc dùng thực tế

- Assets/_Project/Scenes: MainScene.
- Scripts/Domain: config rules, runtime value state, factory và save DTO v1; assembly không reference UnityEngine.
- Scripts/Configuration: PrototypeConfig ScriptableObject và stage view.
- Scripts/Bootstrap: khởi tạo/validate/wiring; không chứa công thức gameplay.
- Scripts/Presentation: TreeView, OrbitCameraRig, HUD khởi đầu.
- Scripts/Infrastructure: SaveService/SaveSession/OfflineCoordinator và codec/serializer/validator; RNG Weather chưa triển khai.
- Configs: PrototypeConfig.asset.
- Prefabs/Trees: seedling, young_tree, large_tree placeholder.
- Art/Placeholders/Materials: màu Bark/Leaf/Soil; Unity tạo GUID/meta.
- UI, Audio: nơi cho asset khi cần.
- Editor: công cụ setup scene/settings dùng lại.
- Tests/EditMode và Tests/PlayMode: kiểm tra config, factory, schema và khởi tạo scene.

## Cách chuẩn bị hoặc mở lại

1. Mở project với Unity đã ghi ở ProjectVersion.txt, chờ import/compile hoàn tất.
2. Kiểm tra Console và kết nối đúng project trong Unity MCP nếu dùng agent.
3. Chạy menu Cây Thế Giới → Setup → Prepare Project. Lần đầu tạo asset/scene; nếu MainScene đã có thì giữ scene và config/prefab đã tồn tại, chỉ xác minh thư mục và áp settings.
4. Menu Cây Thế Giới → Setup → Open MainScene mở scene làm việc. Trình setup không được bỏ mất scene đang sửa chưa lưu.
5. Vào Play: thấy cây Mầm, Water10, Growth0, HP100, Sức sống0 và tap0/50. T02: mở Upgrade, mua Root+1 rồi Leaves+1; Water còn0 rồi tăng0.75/s, Growth1/s. Click/chạm cây50 lần thêm20s/+5%; chuột phải hoặc kéo một ngón để orbit. Trunk khóa Mầm; menu `Cây Thế Giới → T02 → Debug → Young Tree State (no Evolution)` thử unlock/Protection, `Seedling Reset` trả run mới; không đổi model. Menu T01 vẫn có production/bank/dead scenario. Debug chỉ sửa runtime/config không đổi, nhưng autosave/quit hiện có thể lưu state debug; dùng dữ liệu thử riêng. Save người chơi load lại khi Play, không reset mặc định.
6. Chạy Test Runner EditMode và PlayMode cho assemblies CayTheGioi; ghi Validation/Evidence thật trước DONE.

Nếu dùng MCP để chạy setup, sau khi compile thành công gọi `CayTheGioi.Editor.ProjectSetup.PrepareProject()` qua execute_code hoặc menu item đã tra trong resource. Không sinh YAML scene/prefab hoặc GUID bằng tay.

## Hợp đồng nền tảng

- PrototypeRules chứa số tạm và các luật đã chốt: offline 86400s/0.5; bank Sức sống 3600s; output multiplier 1.05. T00 chỉ lưu cấu hình, chưa chạy cơ chế production/offline.
- Stage ID ổn định; config có view cho cả ba stage. Factory tạo state riêng mỗi lần, không chia sẻ mutable list và không trao point.
- Tree có ModelSlot, RootAnchor, LeavesAnchor, TrunkAnchor và collider; thay đồ họa sau giữ hợp đồng này.
- GameBootstrapper khởi tạo một lần mỗi instance; gọi lại không sinh state/model mới. Chưa có singleton/static event hoặc DontDestroyOnLoad để gây nhân đôi khi reload.
- SaveData v1 chỉ là DTO định nghĩa schema. Snapshot cần deep-copy plain value data khi làm T04; không ghi live reference vào file.
- `.gitignore`, `.gitattributes` và `.editorconfig` chuẩn bị lưu source, loại file Unity sinh tự động, giữ meta/scene/config. Chưa tạo commit hoặc remote.

## Khi setup lỗi

Config không hợp lệ: dừng khởi tạo và ghi Console, không tiếp tục với state âm/ID thiếu. Compile lỗi: sửa trước khi chạy setup. MCP mất kết nối khi domain reload: đợi Editor ổn định, kiểm tra instance rồi tiếp tục. Scene đang sửa chưa lưu: lưu trước khi chạy Prepare Project; không tự bỏ thay đổi.

Không ép EditorSceneManager.playModeStartScene sang MainScene: Unity Test Runner cần scene tạm riêng khi vào Play. Dự án vẫn chỉ build MainScene; mở MainScene trước khi thử thủ công.

T00–T05 đã PASS trong Editor, CURRENT_TASK chuyển T06 NOT_STARTED sau tổng hợp tiến độ. Không tự bắt đầu task tiếp theo chỉ vì đọc setup; kiểm tra bàn giao và phạm vi được giao trước.

T03: Upgrade panel có current→next stage, chi phí/reason, nút Evolution và Orbit: Tự do/Đã khóa. Đủ tài nguyên vẫn giữ stage đến khi bấm. Mầm→Cây Non cần50 Water/25 Growth; Cây Non→Cây Lớn cần200/100. Dùng `Cây Thế Giới → T03 → Debug → Fund Next Evolution` trong Play để thử nhanh rồi tự bấm Evolution. Không dùng debug stage T02 để kiểm tra chuyển model. Blend0.8s chặn Upgrade/Evolution/tap/orbit, hoàn tất mở lại; production/Sức sống tiếp tục. Config prototype-2 thêm target height camera, không đổi economy. Ở T03 chưa save; từ T04 tiến độ được lưu, T05 tính offline khi mở lại.

T00 đã PASS ngày 02 tháng 10 năm 2026, xem `Docs/Validation/T00_20261002.md`. Nếu gặp lỗi MainToolbarWindow/UIElements layout của Editor khi cửa sổ quá hẹp, nới cửa sổ rồi đọc lại Console; không bỏ qua lỗi để đánh dấu test đạt. Phiên nghiệm thu dùng cửa sổ Editor 1280×800 logical.

Phiên Console 03/10/2026: nới cửa sổ chưa đủ khi thu hẹp lại. Workaround đã thử là ẩn các overlay toolbar Store, Unity Version Control, Collab và Multiplayer Role, rồi kiểm tra ở 875,2 logical px; để 1280×800 cuối phiên. Có thể hiện lại qua tùy biến toolbar nhưng phải kiểm tra Console. Không sửa Unity/package hoặc tắt log lỗi. Xem Docs/Validation/Console_20261003.md.

SaveTestScope dùng policy input tạm trong test để các device events giả vẫn tới UI khi Codex/Console có focus; teardown trả BackgroundBehavior/EditorInputBehavior về giá trị cũ. Đây không phải policy nhận input trong nền của game. Sau test kiểm tra override save/clock đã bỏ và native Pointer đã bật lại. Khi MCP mất callback hoặc báo 0 test, đối chiếu XML Unity xuất với số test/timestamp; không lấy status của job thay kết quả thực.


## Thử T05 offline đã nghiệm thu

Mở MainScene và Play dùng save mặc định ở persistentDataPath. Dùng Upgrade tạo Root để sản xuất Water; Stop, đợi vài giây rồi Play lại. Popup hiển thị thời gian xét, delta Water/Growth, cap24h/tốc độ50% từ config; bấm Tiếp tục. Sức sống/tap không bị tiêu trong offline; production online và +5% tiếp tục sau commit, kể cả popup đang mở. Leaves vẫn cần Water, report có thể giảm Water vì tiêu thụ. File lỗi thưởng chặn gameplay và có Thử lại; không báo nhận thưởng trước commit.

Để tái hiện chính xác1h/24h/30h mà không chờ, chạy OfflineTests/EditMode và OfflineSceneTests/PlayMode: các test dùng clock/thư mục temp riêng, không sửa save người chơi. Validation/T05_20261003.md và Evidence/T05 có Play→Stop→Play clock thử, ảnh ngang/dọc/inset, lỗi lưu và retry. Validation override đã trả null cuối phiên; không để clock giả hoạt động với save người chơi. Không dùng các scenario debug trên save thật nếu muốn bảo toàn tiến độ.

## Thử T06 Rebirth trong MainScene

Config hiện hành prototype-3. Mở Assets/_Project/Scenes/MainScene.unity, Play, đóng popup offline nếu có; Upgrade Root/Leaves rồi Evolution tới Cây Non. Bấm Rebirth để xem +1 point Cây Non/effect/reset/giữ; Hủy giữ nguyên, Xác nhận lưu transaction rồi trở về Mầm Water10/Growth0/HP100, giữ quỹ Sức sống/meta/settings. Stop/Play lại kiểm tra run/point được load. Mầm ALIVE chưa mở Rebirth tự nguyện. DEAD có modal Forced không Cancel; RebirthSceneTests kiểm tra bằng save thử riêng, Editor menu T06/Debug/Force DEAD chỉ cho phép Play có ValidationDirectory, không dùng save người chơi.

Test Runner: toàn bộ EditMode133 ca, PlayMode41 ca (03/10/2026), thử Game view1920×1080 và custom540×960. Dùng temp SaveTestScope/clock, cách ly native Pointer khi phát device events rồi restore. Cần để Enter Play Mode Options disabled (reload domain+scene) như T00; TestRunner có thể để enabled/DisableDomainReload, nên kiểm tra/khôi phục setting sau test. Cuối T06 override directory/clock đã bỏ, Mouse enabled, Play stopped/MainScene clean; player save hashes không đổi. Touch/inset/lifecycle mới mô phỏng Editor; chưa thiết bị thật/build riêng. Xem Validation/T06_20261003.md.
