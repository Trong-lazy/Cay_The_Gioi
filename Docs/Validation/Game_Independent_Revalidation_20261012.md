# Nghiệm thu độc lập bản game hiện tại — 12/10/2026

Task: T11
Result: FAIL

Phạm vi người dùng chốt: nghiệm thu cả bản game hiện tại, dò bug; chưa sửa gameplay hay nâng đồ họa. Bản bàn giao mới nhất là T11 lúc 10:52 ngày 12/10. T13 chưa có thay đổi sửa map mới so với review trước.

## Kiểm tra thực tế và giới hạn

- Unity 6000.6.1f1, URP, MainScene. Unity MCP không được cung cấp trong phiên này. Dùng skill Computer Use để chạy Unity Test Runner trực tiếp.
- EditMode chạy mới: **249/249 PASS**, XML `Evidence/Revalidation_20261012/EditModeResults.xml`, thời gian UTC 04:06:24–04:06:29 ngày 12/10. Đây là test hiện có, không chứng minh các ca chưa được viết đã đúng.
- PlayMode chạy mới: **69/69 PASS**, XML `Evidence/Revalidation_20261012/PlayModeResults.xml`, UTC04:08:33–04:09:42 ngày12/10. Trong đó UpgradeSceneTests **7/7 PASS**. Console sau suite có1Error: `Evolution view failed: TreeView stage visual or model slot is missing.` Đây là lỗi cố tình tạo trong EvolutionSceneTests:107 (`LogAssert.Expect`, test missing prefab), được XML ghi trong ca PASS; không phải phát hiện lỗi production mới. Không có compiler error ngăn suite. Không crash trong hai lần chạy kiểm thử này; không suy ra đã hết crash ở mọi thao tác.
- Probe bổ sung biên thời gian dùng **source Domain thật của project**, biên dịch .NET 10 trong `Evidence/Revalidation_20261012/DomainProbe`; không gọi đây là Unity PlayMode. Kết quả `domain-probe-results.json`; lệnh tái hiện: `dotnet run --project Evidence/Revalidation_20261012/DomainProbe/DomainProbe.csproj`.
- Đối chiếu code T11, runner mới, Validation, code/mesh/prefab môi trường và bằng chứng review T13 trước đó. Quan sát Game View Edit Mode vẫn có biên đất hình tia nhọn. Không chụp mới đủ 3 stage/4 weather/2 resolution trong lần này, không nghe trực tiếp chất lượng âm thanh và không build lại Standalone/mobile.
- Các probe animation/collider của 11/10 là bằng chứng **trước sửa T11**, không dùng số cũ để khẳng định lỗi đó vẫn tái hiện y hệt sau sửa. Cần đo lại actual model/component.

## Lỗi mới xác nhận

### N01 — P1: Sát thương và thời lượng Burning phụ thuộc kích thước tick

`OnlineTickDriver.cs:30–32,43–45` chạy toàn bộ Weather.Tick(dt) trước Health.Tick(dt). Weather chia bước nội bộ nhưng Health không nhận các đoạn trước/sau sét. Khi sét đến ở giây 18 của tick 20 giây, Health áp Burning lên cả tick, tiêu hết 10 giây cháy ngay.

Probe: cùng seed, stage `large_tree`, Thân L0, HP100, Giông60s, sét giây18. Tại giây20, tick0,05s còn **95,9 HP**, Burning còn7,95s; tick20s còn **80 HP**, Burning hết. Giá trị lý tưởng là96 HP; tick nhỏ hiện lệch một frame0,05s. Test runner chỉ so cuối giây60 nên bỏ lọt sai khác ở giữa timeline.

Yêu cầu: tiến weather/health/resources cùng các đoạn thời gian sự kiện; không sửa bằng cách chỉ ép HP cuối đợt cho giống nhau. Test cả trạng thái trung gian, aliveSeconds và output tài nguyên trước/sau strike/death/weather boundary.

### N02 — P1: Kết thúc thời tiết trừ trước toàn bộ Burning còn lại, có thể giết cây sai luật

`WeatherSystem.cs:96,118,126–143`: `FlushPendingBurnDamageBeforeCalm` trừ `remainingBurnSeconds * dps` ngay khi thời tiết hết, rồi xóa status. D06/D19/D49 quy định Bình yên xóa Burning; không quy định thu trước sát thương chưa xảy ra.

Probe Giông còn1s, Burning còn10s, HP100, ThânL0: sau1s HP xuống **80** thay vì chỉ chịu tối đa1s sát thương trước khi status bị xóa. Trường hợp HP5 bị ép **DEAD/HP0** ngay trong Weather.Tick. Có thể chết sớm dù phần Burning còn lại phải được hủy khi sang Bình yên. Giữ luật hiện tại, không sửa GDD để hợp thức hóa hành vi này.

### N03 — P1: Đường chết cây này bỏ qua sự kiện vòng đời

Trong cùng `FlushPendingBurnDamageBeforeCalm`, WeatherSystem ghi trực tiếp hp/life và clear status, không đi qua HealthStatusSystem. Probe HP5 ghi nhận `life=DEAD` nhưng **RunDied=0, HealthChanged=0**; Health.Tick sau đó trả aliveSeconds0 và không phát bù.

`AudioController` và `CosmeticView` đăng ký RunDied để dừng audio/hủy preview. Đây là đường không thực hiện callback dọn run. Không kết luận modal Forced Rebirth chắc chắn mất: RebirthView còn polling DEAD trong LateUpdate; cần test riêng UI và tất cả subscriber. Chết hợp lệ phải phát sự kiện đúng một lần, theo giao dịch health chuẩn.

### N04 — P2: Strike đến hạn ngay đầu tick làm mất phần thời gian tick

`WeatherSystem.cs:87–99` xử lý event `step <= 1e-6` rồi `break`. SaveValidator cho phép strikeDelaySeconds0. Probe Giông60s, strikeWillHit=true, chưa executed, delay0: `Weather.Tick(5)` phát1strike nhưng thời tiết vẫn còn **60s**, đúng phải55s. Cùng vấn đề có thể gặp khi weather remaining0. Cần xử lý sự kiện rồi tiêu thụ tiếp thời gian dư, với bảo vệ tiến triển vòng lặp.

### N05 — P2: Công thức lifetime mưa không dùng tốc độ cấu hình/range thực tế

`WeatherView.cs:93`: tốc độ hạt random `[0,85;1,15] * rainStartSpeed`. `WeatherView.cs:243` lại lấy lifetime `(emitterHeight+0,5)/14`, hardcode14. Với emitter6,3m và cấu hình14, hạt chậm11,9m/s chỉ đi5,78m, chưa tới Y0 ngay cả giả sử bay thẳng xuống từ tâm emitter. Shape còn có độ dày và terrain có cao độ thay đổi. Đổi rainStartSpeed cũng không đổi lifetime tương ứng.

Xác nhận lỗi công thức/cấu hình; chưa đo mới tỷ lệ hạt chạm terrain thực tế. Báo cáo agent nói dùng vfxConfig.rainStartSpeed và100% chạm đất không khớp code. Cần test particle thật ở 3 stage, không chỉ phép nhân tốc độ danh nghĩa với lifetime tự tính trong test.

### N06 — P1 về nghiệm thu: Runner không chứng minh các kết luận animation/VFX/audio

`Assets/_Project/Editor/T11RegressionRunner.cs`:

- R1 tự đặt scale100, `stemWeight=0`, tính worldAmp/scale*scale; không khởi tạo TreeAnimationController, không đọc cuống lá/mesh thật.
- R2 chỉ quay một Vector3 giả; không kiểm tra anchor, collider, Physics.Raycast hoặc input thật.
- R3 dùng treeTop giả và tốc độ danh nghĩa; không đọc ParticleSystem thật, bỏ range0,85–1,15 và khác công thức production.
- R4 sinh u/v trong giới hạn rồi kiểm lại giới hạn; không project cả polyline, không kiểm camera/occlusion.
- R5 kiểm target volume config dương, không đọc AudioSource.isPlaying sau mute/resume.
- R6 kiểm cường độ hardcode dương; không đổi weather trong lúc coroutine flash chạy.
- R7 dùng validator thật; R8 dùng Domain thật nhưng chỉ so cuối60s nên bỏ N01; R9 chỉ chứng minh config mặc định hợp lệ.

Vì vậy các kết luận “ghim cuống100%”, “raycast mọi pha đúng”, “1000/1000 tia sét nằm trọn viewport”, “audio resume/flash restore đã kiểm chứng” **chưa đủ bằng chứng để đóng nghiệm thu**. Đây không phải khẳng định toàn bộ chúng vẫn hỏng. Giải pháp chia scale đã có trong code; cần đo lại actual assets để xác nhận hiệu quả.

### N07 — P2: File kết quả runner không phải JSON hợp lệ dưới locale hiện tại

Runner nối chuỗi số bằng ToString không InvariantCulture. File thật có số `0,004000`/`0,38` dùng dấu phẩy thập phân; parser JSON thất bại tại results[0].maxWorldLeafMovement. Bản copy nguyên trạng: `Evidence/Revalidation_20261012/agent-runner-original.json`. Dùng serializer/InvariantCulture và test parse output; không sửa tay evidence cho trông như PASS. InitializeOnLoad còn ghi đè cùng file khi reload; kết quả hiện tại không phải bản lưu bất biến của lần bàn giao10:52.

## Map và UI hiện tại

Review trước vẫn áp dụng vì T13 chưa có bản sửa mới. Chi tiết và bằng chứng: `Docs/Validation/T13_Independent_Review_20261012.md`.

| ID | Mức | Vấn đề còn mở |
|---|---|---|
| B01 | P1 | Đường đất/bờ cát thành nêm/tia nhọn theo tam giác mesh; vẫn thấy trong Game View hiện tại. |
| B02 | P1 | Vành hồ lệch đất -0,522 đến+0,486m theo probe trước; bờ hở/treo/chôn. |
| B03 | P1 | Props che cây chính khi orbit; bố cục không đủ hành lang nhìn cho 3 stage. |
| B04 | P2 | Nền chỉ là chỏm cầu82°, còn96 cạnh hở; chưa hoàn thiện khối cầu kín. |
| B05 | P2 | Hoa là các mặt màu rời thiếu thân/đế tiếp đất. |
| B06 | P2 | Thác dùng tam giác trùng ngược chiều chung vertex rồi RecalculateNormals. |
| B07 | P2 | Lưu mesh nấm theo tên lặp giữa cluster ghi đè biến thể. |
| UI01 | P2 | Thẻ Linh khí cắt thông tin mở khóa; đã tồn tại ở ảnh baseline10/10. |

Nút Cài đặt/Thành tựu nền tối đã có trong ảnh baseline10/10. Không đủ bằng chứng gọi chúng là hồi quy do trang trí map. T12 chỉ đổi giá trên nút nâng cấp, phụ thuộc T09B, không phụ thuộc T11. Giữ nhãn chi phí và luật giao dịch; không trả lại +1. Test UpgradeSceneTests trong suite mới đánh giá hồi quy chức năng, không thay nghiệm thu mọi panel/độ phân giải.

## Trạng thái và bàn giao

- T11: mở lại IN_PROGRESS, bỏ tích tiêu chí ảnh hưởng và trỏ Validation tới báo cáo này. Báo cáo PASS cũ được giữ làm lịch sử.
- T13: BLOCKED theo phụ thuộc T11; các lỗi map vẫn mở, giữ nguyên phần đã triển khai.
- T12: giữ DONE vì dependency T09B không bị ảnh hưởng; kết quả test hồi quy mới ghi ở trên khi hoàn tất.
- Chỉ thêm probe/evidence/tài liệu; không sửa code game, prefab/model/scene/config/save. Công cụ tổng hợp tài liệu được chỉnh tối thiểu để cho phép BLOCKED chờ dependency và không đếm task BLOCKED là task đang thực thi; vẫn chặn IN_PROGRESS/DONE khi dependency chưa DONE.
- Save chính/backup: **hash SHA256 trước/sau đều không đổi**, evidence `save-before.json`/`save-after.json`. Unity cuối phiên ngoài PlayMode, MainScene không có dấu dirty. GPU Resident Drawer trên PC_RPAsset và Mobile_RPAsset đều0/Disabled; không sửa workaround.
- Prompt sửa theo thứ tự: `Docs/Art/Game_Bugfix_Prompt_20261012.md`. Sửa gameplay và bug môi trường trước, nâng đồ họa sau; skybox do người dùng lựa chọn sau.
