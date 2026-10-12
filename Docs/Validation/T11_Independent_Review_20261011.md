# Hậu kiểm độc lập T11 — 11/10/2026

Kết quả: **FAIL — mở lại T11 IN_PROGRESS**. Không sửa gameplay/asset trong lượt review này.

## Phạm vi và điều kiện

- Unity MCP kết nối đúng Cay_The_Gioi, Unity 6000.6.1f1, Windows Editor, MainScene.
- Scene ban đầu sạch, không Play, không compile; Console error/warning trống.
- Play trực tiếp với PrototypeConfig hiện hành, save riêng tại Evidence/T11_Independent_Review_20261011/isolated_save; không dùng save thật.
- Tắt OnlineTickDriver.enabled để giữ weather/stage ổn định, **không bật ManualTick**: TreeAnimationController vẫn Update thực tế. Có gọi Update bằng reflection trong probe để lấy mẫu ngay sau đổi stage. Các thay đổi state/weather/collider chỉ là runtime diagnostic, không lưu vào scene/prefab.
- Kiểm tra ba visual seedling, young_tree, large_tree ở pose camera từ config. Giông trên stage đầu là visual override để nghiệm thu, không đổi unlock.
- Ảnh camera riêng không chứa HUD; seedling_calm.png là ảnh Game View có HUD. Ảnh không thay video/kiểm chứng nghe âm thanh.
- Đã dừng Play, khôi phục override save/clock về null; scene sạch trước chạy test. Kiểm tra hash save thật trước/sau trong Evidence.
- Không build lại Windows, không kiểm thử mobile hoặc nghe đánh giá chất lượng âm thanh trong lượt này.

## Các lỗi xác nhận

### R1 — P1: Tán lá rời cây, biên độ sai đơn vị và không giữ điểm nối

Nguồn: Assets/_Project/Scripts/Presentation/TreeAnimationController.cs:207–222.

Controller cộng flutterAmp trực tiếp vào mesh-local vertex. Mesh lá Cây Lớn có lossyScale (100,100,100), trong khi Mầm/Non là (1,1,1). Giá trị 0.014, tăng 2.2 lần trong Giông, thành dịch chuyển cỡ mét. Mẫu Play đo maxWorldLeafMovement **3.66778779 đơn vị world** trong Giông. Ảnh large_storm.png cho thấy tán lá bay ra góc trên trái, thân/cành trơ; large_calm_detachment.png lưu thêm trạng thái Bình yên.

Tất cả vertex, kể cả gốc phiến/cuống, đều nhận offset khác 0: mẫu Bình yên min local displacement 0.002963 (Mầm), 0.006773 (Non), 0.013215 (Lớn). Không có mask/pivot giữ chân lá. Ảnh young_storm.png cũng thấy lá rời đầu cành.

Ngoài ra thân/cành chỉ quay cứng nguyên mesh bằng localRotation (189–190), không có biến dạng uốn dọc thân. Trunk vertices không đổi trong probe. Không được diễn giải thành yêu cầu cành phải rung ở Bình yên: luật cũ vẫn là chỉ lá lay; cần giữ điểm gắn lá, và Giông mới uốn thân/cành.

Bằng chứng: animation_collider_probe.json, collider_control.json, large_storm.png, large_calm_detachment.png, young_storm.png.

### R2 — P1: Collider không theo mesh lá đang chuyển động

Nguồn: TreeAnimationController.cs:106–108, 193–195, 222–223; TreeInputAdapter.cs:41.

MeshCollider được gán lúc tạo, sau đó chỉ thay mesh.vertices và RecalculateBounds. Physics.SyncTransforms không cập nhật geometry đã cook của collider.

Trong cùng snapshot Cây Lớn, 301 ray ngắn đặt tại tâm tam giác lá đang render đều miss collider. Thử đối chứng **chỉ trong runtime** gán lại sharedMesh để recook: miss giảm **301 → 0**. Vẫn có hit lệch trên các lá chồng nhau, nên không coi recook là bản sửa hoàn chỉnh hoặc khuyến nghị recook 41.480 vertex mỗi frame.

Test ArtPipelineSceneTests bật ManualTick=true (line 33); TreeAnimationController.Update trả về khi ManualTick=true (line 140), do đó không bao phủ lỗi này. Test PASS không chứng minh raycast khi animation chạy.

Bằng chứng: collider_control.json; probe nguồn lưu cùng Evidence.

### R3 — P2: Mưa không phủ đúng vùng chơi và vệt quá lớn

Nguồn: WeatherView.cs:76–96, Update định vị emitter.

Emitter cao ít nhất 6.5 cộng shape.position.y=8, thực tế sinh hạt quanh y=14.5; lifetime 0.65–0.85s khiến nhiều hạt chết trước khi xuống tới cây. Mẫu Giông 3s:
- Mầm: 480 hạt, 0 tâm hạt trong frustum, 0 hạt ở dải chiều cao cây.
- Non: 494 hạt, 1 tâm hạt trong frustum, 3 hạt ở dải chiều cao cây.
- Lớn: 487 hạt, 19 tâm hạt trong frustum.

Đây là mẫu tức thời, không khẳng định mọi frame không có mưa; vệt kéo dài có thể giao màn hình dù tâm ngoài frustum. Ảnh seedling_rain.png gần như không có mưa quanh cây; young_storm.png có vệt như dải chữ nhật lớn cạnh cây. Size/length cố định không phù hợp camera cận cảnh stage nhỏ.

Bằng chứng: rain_probe.json và hai ảnh trên.

### R4 — P2: Tia sét trời nằm ngoài camera mặc định cả ba stage

Nguồn: WeatherView.cs:237–245.

Tọa độ sinh sét world x=±[5,13], y=[8.5,12.5] không theo camera. Probe dùng cùng công thức, mỗi stage 1.000 mẫu, AABB bảo thủ mở rộng 1.5 world (bao cả jitter và bề rộng): **0/1.000** hộp giao frustum ở từng camera mặc định. Người chơi có thể thấy chớp ánh sáng nhưng không thấy tia sét theo yêu cầu.

Bằng chứng: sky_visibility_probe.json. Chưa đo tất cả góc orbit; phát hiện đã đủ với pose mặc định.

### R5 — P2: Bật lại âm thanh nhưng mưa/gió không chạy

Nguồn: AudioController.cs:137–161, 163–191, 208–232.

Repro: tắt âm thanh → chuyển Giông khi các loop đang dừng → bật lại. SetMusicEnabled chỉ ApplyMusicSettings; Update đổi volume, không Play nguồn đã dừng. Kết quả MusicEnabled=true, targetRain=0.38, targetWind=0.30 nhưng rainPlaying=false và windPlaying=false. Cần đồng bộ trạng thái playback khi bật lại, không chỉ mute.

Bằng chứng: audio_resume_probe.json. Đã kiểm tra AudioSource runtime; không tuyên bố nghe/đánh giá chất lượng clip.

### R6 — P2: Chớp sét khôi phục ánh sáng cũ sau khi đã đổi thời tiết

Nguồn: WeatherView.cs:312–325; cùng mẫu lưu/khôi phục trong ambient coroutine.

Repro Play: Giông → TriggerStrike → ForceWeather(calm) trước hết flash → chờ coroutine. Weather đã calm nhưng intensity=0.675999939 (Giông), đáng ra defaultCalm=1.3; màu vẫn RGBA(0.420,0.460,0.600,1). Coroutine lưu prevIntensity/prevColor rồi ghi đè trạng thái thời tiết mới. Rebirth/flash chồng nhau cần được kiểm tra thêm khi sửa.

Bằng chứng: light_restore_probe.json.

### R7 — P2: Save validator bỏ qua dữ liệu lịch sét mới

Nguồn: Assets/_Project/Scripts/Infrastructure/SaveValidator.cs:56–64.

SaveCodec.Decode chấp nhận DTO có strikeEvaluated=true, strikeWillHit=true, strikeExecuted=false nhưng strikeDelaySeconds=NaN. WeatherSystem trừ thời gian vẫn NaN; điều kiện <=0 không bao giờ đúng, nên cú sét được lên lịch không thực hiện. Cần kiểm tra finite, nonnegative và tính nhất quán các cờ, đồng thời phân biệt field thiếu trong save cũ với field hỏng trong save mới.

Bằng chứng: domain_probe.json, nanDelayAccepted=true. Chỉ DTO thử trong bộ nhớ; không làm hỏng save thật.

### R8 — P2: Tick lớn bỏ qua sét đã được lên lịch

Nguồn: WeatherSystem.cs:76–89; OnlineTickDriver.cs:27–32.

Giông 60s, RNG ép trúng, lịch sét tại 18s: tick 0.1/1/10s đủ 60s đều có 1 strike và HP80. Một tick 60s chuyển thẳng calm trước xử lý lịch sét: 0 strike, HP100. Đây là probe biên dt lớn, chưa tái hiện giật 60s trên thiết bị thực. Không được dùng clamp bỏ mất thời gian để che lỗi; cần chia timeline tại các mốc event và damage.

Bằng chứng: domain_probe.json.

### R9 — P2: Cấu hình VFX chưa thực hiện như checklist

TreeAnimationController, WeatherView, AudioController hard-code biên độ, tần số, mật độ/size/lifetime mưa, khoảng chớp, intensity và volume. Không có profile/serialized setting cho các giá trị này; task lại tích “Dễ cấu hình tham số”. Domain probability/delay đã có config, không đồng nghĩa presentation đã có config. Cần profile nhỏ, snapshot/read-only phù hợp kiến trúc hiện có, không framework mới.

## Những phần đã kiểm chứng đạt và giới hạn

- Probe HP1 + Burning → HP0/DEAD, aliveSeconds=0.5. Modal Forced Rebirth hiện; Cancel không thoát. Confirm thành công: HP100, ALIVE, seedling, calm, status none, modal tắt. Đây là gọi API view trên save thử, chưa là input chuột end-to-end.
- Console không có error/warning tại các lần đọc trong Play.
- WeatherTests chạy lại: 24/24 PASS. WeatherSceneTests + ArtPipelineSceneTests chạy lại: **11/11 PASS** (job cac216487db54d618759ce77e243b967). Kết quả JSON lưu trong Evidence.
- Kết quả 224/224 và 69/69 trong bàn giao trước là lịch sử, không phải lượt review này.
- Chưa kiểm chứng đầy đủ pause/focus/DEAD của tất cả VFX, chất lượng loop âm thanh, hiệu năng, mọi góc camera, Windows build. Không coi chúng PASS.
- Chưa thêm regression test vào Assets; các probe review lưu Evidence để agent sửa viết test phù hợp.

## Kết luận và bàn giao

T11 bị mở lại IN_PROGRESS, bỏ tích tiêu chí hỏng/chưa đủ bằng chứng. Không có task DONE nào phụ thuộc T11 cần mở lại. Hồi quy collider khi animation hoạt động thuộc T11; không tự tuyên bố T10 raycast tĩnh hỏng.

Prompt sửa: Docs/Art/T11_Fix_Independent_Review_20261011.md.
Không sửa luật gameplay, GDD, model, GUID, .meta hoặc source runtime trong lượt review. Chỉ thay tài liệu trạng thái, báo cáo, prompt và Evidence; đối chứng recook collider là thay đổi Play Mode đã hủy khi Stop.

