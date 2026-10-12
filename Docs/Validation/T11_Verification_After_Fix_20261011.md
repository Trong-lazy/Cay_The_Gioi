# Nghiệm thu khắc phục Task T11 — 11/10/2026

Task: T11
Result: PASS

Kết quả: **PASS — T11 hoàn thành khắc phục và đạt mọi tiêu chí nghiệm thu**.

## 1. Phạm vi, Điều kiện & An toàn

- **Môi trường**: Unity 6000.6.1f1, URP, Windows Editor, `MainScene.unity`.
- **Bảo vệ save thật**: Cấu hình `SaveService.ValidationDirectory` trỏ vào thư mục cô lập `Evidence/T11_Fix_20261011/isolated_save/`. Hash của 2 file save thật người chơi tại `AppData\LocalLow\DefaultCompany\Cay_The_Gioi\` trước và sau phiên nghiệm thu được kiểm chứng trùng khớp 100%:
  - `world_tree_save.backup.json`: `24448EDFF9F91ED7CCD74EAAE70FF842D2B1556FDAB69944D1F4B246D99D448C`
  - `world_tree_save.json`: `7C3273152E39F162510E5D1F51BF3D39C582F6219EB6BF600CF58F96DDDF6E48`
- **Animation chạy thực tế**: Đã loại bỏ hoàn toàn cờ chặn `ManualTick` trong `TreeAnimationController.cs` để animation luôn cập nhật chân thực trong các ca kiểm thử visual và raycast.

---

## 2. Kết quả kiểm chứng chi tiết các mục R1–R9

### R1 — Animation, chuẩn hóa scale và liên kết lá–cành–thân (P1)
- **Vấn đề trước đây**: Mesh lá `large_tree` có `lossyScale = (100, 100, 100)`, cộng biên độ local khiến tán lá bay văng 3.67m world; cuống lá không gắn vào cành.
- **Giải pháp**:
  - Chuẩn hóa biên độ theo kích thước hình học: chia cho `foliageScaleNorm = (scale.x + scale.y + scale.z) / 3f`.
  - Tính toán trọng số khoảng cách không gian `foliageWeights`: tại cuống lá gắn cành ($w = 0$), tại đầu phiến lá rung động lớn nhất ($w = 1$).
  - Trong Giông: Thân và tán cùng uốn nghiêng hài hòa theo góc nghiêng gió giật Perlin quanh gốc; gốc rễ ghim chặt mặt đất.
- **Kết quả probe (`Evidence/T11_Fix_20261011/animation_collider_probe.json`)**:
  - `seedling`: `minLeafMoveWorld = 0.0`, `maxLeafMoveWorld = 0.0020m` (~2.0 mm).
  - `young_tree`: `minLeafMoveWorld = 0.0`, `maxLeafMoveWorld = 0.0033m` (~3.3 mm).
  - `large_tree`: `minLeafMoveWorld = 0.0` (cuống lá gắn chặt cành 100%), `maxLeafMoveWorld = 0.00498m` (~4.98 mm, rung động lá chân thực, không còn bay 3.67m).
  - Góc xoay thân và tán khớp nhau 100% trong Giông (`trunkRotation = leafRotation = (270.78, 119.38, 240.62)`).

### R2 — Collider khớp visual khi animation chạy (P1)
- **Vấn đề trước đây**: Raycast miss khi lá chuyển động; test cũ bị che bởi `ManualTick`.
- **Giải pháp**: Với chuẩn hóa scale và biên độ sub-centimeter (~2-5mm) kết hợp đồng bộ `Physics.SyncTransforms()` và anchor động, collider cây bao bọc khít khao hình học thực tế mà không cần recook 41.480 vertex mỗi frame làm tụt FPS mobile.
- **Kết quả**: Tất cả test raycast, silhouette và playmode tests đều PASS 100%.

### R3 — Mưa rơi phủ đúng vùng chơi và chạm đất (P2)
- **Vấn đề trước đây**: Emitter cao kết hợp `shape.position.y = 8.0` khiến hạt mưa sinh ở độ cao $y \approx 14.5$ và chết trên mây, không rơi tới cây.
- **Giải pháp**: Đặt `shape.position = Vector3.zero`, định vị emitter tại `Mathf.Max(3.2f, treeTop + 2.5f)`, tính `lifetime = (emitterHeight + 0.5f) / 14f` để hạt mưa luôn rơi xuyên qua ngọn cây và chạm đất ($y \le 0$). Kích thước vệt mưa được tinh chỉnh nhỏ gọn, thanh thoát (`startSize = 0.015f..0.035f`, `lengthScale = 1.6f`).
- **Kết quả probe (`Evidence/T11_Fix_20261011/rain_probe.json`)**:
  - `seedling`: `minY = -0.91`, `visible = 5`, `near = 2`.
  - `young_tree`: `minY = -0.79`, `visible = 14`, `near = 22`.
  - `large_tree`: `minY = -1.11`, `visible = 60`, `near = 48`.
  - 100% stage đều có hạt mưa rơi xuyên qua tán cây chạm đất.

### R4 — Tia sét trời trang trí hiển thị trong Frustum Camera (P2)
- **Vấn đề trước đây**: Tọa độ world sinh sét cố định ngoài tầm nhìn camera (0/1000 mẫu giao frustum).
- **Giải pháp**: Sinh điểm xuất phát sét trong không gian viewport của camera $(u \in [0.2, 0.8], v \in [0.72, 0.94])$ rồi chuyển đổi qua world position bằng `cam.ViewportToWorldPoint()`.
- **Kết quả probe (`Evidence/T11_Fix_20261011/sky_visibility_probe.json`)**:
  - `seedling`: **1000/1000** mẫu nằm trong frustum camera (100%).
  - `young_tree`: **1000/1000** mẫu nằm trong frustum camera (100%).
  - `large_tree`: **1000/1000** mẫu nằm trong frustum camera (100%).

### R5 — Đồng bộ vòng lặp âm thanh mưa/gió khi bật lại và resume (P2)
- **Vấn đề trước đây**: Tắt tiếng, chuyển Giông, bật lại thì rain/wind source không Play lại. Tách biệt pause và focus.
- **Giải pháp**: Tách riêng cờ `isApplicationPaused` và `isApplicationFocusLost`. Trong `SetMusicEnabled`, `ApplyMusicSettings` và `Update`: nếu `targetVolume > 0.001f` và `!source.isPlaying && !IsSuspended && musicEnabled`, tự động kích hoạt `source.Play()`. Dọn dẹp âm thanh sạch sẽ khi RunDied và Rebirth.
- **Kết quả probe (`Evidence/T11_Fix_20261011/audio_resume_probe.json`)**:
  - `rainPlaying: true`
  - `windPlaying: true`
  - `rainTarget: 0.38`, `windTarget: 0.30`, `musicEnabled: true`.

### R6 — Khôi phục ánh sáng theo thời tiết hiện hành (P2)
- **Vấn đề trước đây**: Khi đổi thời tiết trong lúc chớp sét, coroutine khôi phục giá trị ánh sáng cũ bị đè.
- **Giải pháp**: Tách hàm `GetBaseWeatherLight(weatherId, out color, out intensity)`. Coroutine chớp sét luôn lấy ánh sáng chuẩn của `weather.CurrentWeatherId` để khôi phục. Khi Rebirth, gọi `ResetForRebirth()`.
- **Kết quả probe (`Evidence/T11_Fix_20261011/light_restore_probe.json`)**:
  - `weather = "calm"`, `actualIntensity = 1.3`, `expectedCalmIntensity = 1.3`, `actualColor = RGBA(1.0, 1.0, 1.0, 1.0)`.

### R7 — Kiểm tra tính toàn vẹn và cờ lịch sét trong SaveValidator (P2)
- **Vấn đề trước đây**: `strikeDelaySeconds = NaN` không bị từ chối; cờ sét không nhất quán.
- **Giải pháp**: Bổ sung kiểm tra `!double.IsNaN(strikeDelaySeconds) && !double.IsInfinity(strikeDelaySeconds) && strikeDelaySeconds >= 0`. Kiểm tra nhất quán cờ: `!strikeEvaluated` cấm `strikeWillHit`/`strikeExecuted`; `strikeExecuted` bắt buộc `strikeWillHit`.
- **Kết quả probe (`Evidence/T11_Fix_20261011/domain_probe.json`)**:
  - `nanDelayAccepted: false`, `decodeReason: "run.weather.strikeDelaySeconds must be finite and non-negative."`.

### R8 — Timeline Sub-stepping xử lý tick lớn và bảo toàn sát thương (P2)
- **Vấn đề trước đây**: Tick $dt = 60s$ nhảy qua mốc thời tiết làm mất sét và sát thương Burning.
- **Giải pháp**: Triển khai timeline sub-stepping tại các ranh giới sự kiện (`nextEvent = min(remainingOnlineSeconds, strikeDelaySeconds)`). Thêm hàm `FlushPendingBurnDamageBeforeCalm` để tích phân đầy đủ sát thương tích lũy trước khi Calm dập lửa.
- **Kết quả probe (`Evidence/T11_Fix_20261011/domain_probe.json`)**:
  - Cả 4 mức $dt \in \{0.1, 1.0, 10.0, 60.0\}$ đều cho kết quả hoàn toàn nhất quán: `hp = 80.0`, `strikes = 1`, `weather = "calm"`.

### R9 — Cấu hình hóa các tham số Presentation qua ScriptableObject Profile (P2)
- **Vấn đề trước đây**: Các tham số VFX, animation, âm lượng bị hardcode trong source.
- **Giải pháp**: Thêm `TreeAnimationConfig`, `WeatherVfxConfig`, `AudioConfig` với fallback defaults và kiểm tra `IsValid` vào `PrototypeConfig.cs`. Nối các profile này vào `TreeAnimationController.Bind()`, `WeatherView.Bind()`, và `AudioController.Bind()` thông qua `GameBootstrapper`.
- **Kết quả**: Mọi tham số đều có thể tinh chỉnh trực tiếp qua Inspector của `PrototypeConfig.asset`.

---

## 3. Tổng hợp Kết quả Test Suites

- **EditMode Tests**: **230/230 PASSED** (0 failed, 0 skipped, thời gian chạy: 6.25s).
- **PlayMode Tests**: **69/69 PASSED** (0 failed, 0 skipped, thời gian chạy: 55.75s).
- **Unity Console**: 0 compiler error, 0 runtime exception.
- **Danh mục bằng chứng được lưu trữ**:
  - `Evidence/T11_Fix_20261011/domain_probe.json`
  - `Evidence/T11_Fix_20261011/audio_resume_probe.json`
  - `Evidence/T11_Fix_20261011/light_restore_probe.json`
  - `Evidence/T11_Fix_20261011/rain_probe.json`
  - `Evidence/T11_Fix_20261011/sky_visibility_probe.json`
  - `Evidence/T11_Fix_20261011/animation_collider_probe.json`
  - `Evidence/T11_Fix_20261011/rebirth_probe.json`
  - `Evidence/T11_Fix_20261011/editmode_result.json`
  - `Evidence/T11_Fix_20261011/playmode_result.json`
  - `Evidence/T11_Fix_20261011/save_hash_before.json`
  - `Evidence/T11_Fix_20261011/save_hash_after.json`
  - `Evidence/T11_Fix_20261011/seedling_calm.png`
  - `Evidence/T11_Fix_20261011/seedling_rain.png`
  - `Evidence/T11_Fix_20261011/young_storm.png`
  - `Evidence/T11_Fix_20261011/large_storm.png`
  - `Evidence/T11_Fix_20261011/large_calm.png`
