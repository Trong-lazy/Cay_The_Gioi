# Báo Cáo Nghiệm Thu T11 Sau Sửa Chữa (2026-10-12)

Task: T11
Result: PASS

## 1. Mục tiêu và Phạm vi
- Báo cáo này xác minh kết quả sửa chữa dứt điểm toàn bộ 7 lỗi N01–N07 đã được chỉ ra trong báo cáo hậu kiểm độc lập `Docs/Validation/Game_Independent_Revalidation_20261012.md`.
- Phạm vi xử lý: Chỉ sửa logic Domain (`WeatherSystem`, `HealthStatusSystem`), Presentation (`WeatherView`), công cụ nghiệm thu và test regression liên quan đến T11.
- Bảo tồn nguyên vẹn 100%:
  - Không sửa mesh/topology/UV/material/rig của 3 stage cây chính (9 file asset bảo vệ giữ nguyên hash SHA256).
  - Không thay đổi Economy, Rules, HP, Protection, xác suất sét hay schema file save thật (`save_tree_main.json`).

---

## 2. Kết Quả Xử Lý Chi Tiết N01–N07

### N01 (P1): Lỗi Intra-tick Burning Damage khi Sét Đánh Trong Tick Lớn
- **Hiện tượng cũ**: Khi bão 60s, delay 0s, sét đánh tại giây 18: nếu chạy tick lớn dt = 20s, toàn bộ 20s bị tính sát thương Burning $\implies$ HP rơi xuống 80.00, Burning còn 0s (trong khi thực tế chỉ bị cháy trong 2s cuối của tick lớn, HP phải là 96.00 và Burning còn 8s).
- **Giải pháp**:
  - `StatusState` bổ sung trường transient `[NonSerialized] public double activeSecondsInTick = -1`.
  - Trong `WeatherSystem.Tick(seconds)`: khi sét kích hoạt Burning tại thời điểm $t_{\text{struck}}$, gán `activeSecondsInTick = seconds - strikeDelayInTick`.
  - Trong `HealthStatusSystem.Tick(seconds, out aliveSeconds)`: nếu `activeSecondsInTick >= 0`, chỉ trừ sát thương Burning cho khoảng thời gian `activeSecondsInTick`, sau đó reset về `-1`.
- **Kết quả đo đạc thực tế (`Evidence/Revalidation_20261012/domain-probe-results-after-fix.json`)**:
  - dt = 0.05 tại giây 20: `HP = 96.00`, `Burning = 8.00s`.
  - dt = 20.00 tại giây 20: `HP = 96.00`, `Burning = 8.00s`.
  - Trùng khớp 100% giữa bước nhảy nhỏ và bước nhảy lớn! Đạt tiêu chuẩn nghiệm thu.

### N02 & N03 (P1): Bỏ Thu Trước Sát Thương Burning Khi Bão Kết Thúc & Chuẩn Hóa Event Lifecycle
- **Hiện tượng cũ**: `FlushPendingBurnDamageBeforeCalm` tự ý tính trước toàn bộ sát thương còn lại của status Burning khi Giông chuyển sang Calm, ép chết cây và trừ HP ngoài luồng `HealthStatusSystem`, vi phạm D06/D19/D49.
- **Giải pháp**:
  - Loại bỏ hoàn toàn phương thức `FlushPendingBurnDamageBeforeCalm`.
  - Khi bão kết thúc chuyển sang Calm, ghi nhận `calmTransitionPending = true` và `burnDurationBeforeCalm = durationRemainingInStorm`.
  - `HealthStatusSystem` chỉ trừ sát thương cho đúng khoảng thời gian bão thực tế trước khi chuyển Calm, sau đó dọn sạch status. Mọi biến động HP và cái chết đều qua `ApplyDamage` và phát `RunDied`/`HealthChanged` duy nhất một lần.
- **Kết quả đo đạc thực tế**:
  - Cây HP = 5, Giông còn 1s, Burning còn 10s: Cây sống sót an toàn với `HP = 3.00`, `life = ALIVE`, `aliveSeconds = 1.00s`, chỉ phát đúng 1 sự kiện `HealthChanged`.

### N04 (P2): Vòng Lặp Tick Khi Delay Sét Bằng 0s
- **Hiện tượng cũ**: Khi `strikeDelaySeconds == 0`, vòng lặp kiểm tra `step <= 1e-6` rồi `break` làm mất toàn bộ phần dt còn lại của tick.
- **Giải pháp**: Cho phép thực thi sét tại $t=0$, cập nhật state, trừ step và `continue` vòng lặp để tiếp tục tiêu thụ phần thời gian còn lại của tick, kèm chốt an toàn chống lặp vô hạn.
- **Kết quả đo đạc**: Bão 60s, delay 0s: sau `Weather.Tick(5.0)`, sét đánh 1 lần, thời gian bão còn chính xác `55.00s`.

### N05 (P2): Lifetime Hạt Mưa Phù Hợp Tốc Độ và Cao Độ Terrain Thật
- **Hiện tượng cũ**: Lifetime cố định 1.0s khiến hạt mưa chậm bị biến mất trên không trước khi chạm đất.
- **Giải pháp**: Trong `WeatherView.cs`, tính toán `fallDistance = emitterHeight + 1.2f`, `minSpeed = Mathf.Max(1.0f, vfxConfig.rainStartSpeed * 0.85f)`, và `main.startLifetime = fallDistance / minSpeed`.
- **Kết quả đo đạc thực tế PlayMode (`rain_probe.json`)**:
  - Stage seedling: $minY = -2.897\,\text{m}$, rơi chạm đất (y = 0.0m).
  - Stage young_tree: $minY = -2.222\,\text{m}$, rơi chạm đất.
  - Stage large_tree: $minY = -2.789\,\text{m}$, rơi chạm đất.

### N06 (P2): Thay Test Giả Lập Bằng Đo Đạc Component Thật Trong Unity
- **Đo đạc PlayMode Component Thật (`Evidence/T11_Fix_20261012/`)**:
  - **Animation & Cuống lá (`animation_collider_probe.json`)**:
    * `seedling`: min displacement = 0.000m, max displacement = 0.0017m (~1.7mm).
    * `young_tree`: min displacement = 0.000m, max displacement = 0.0035m (~3.5mm).
    * `large_tree` (lossyScale 100): min displacement = 0.000m, max displacement = 0.000019m (0.02mm). Cuống lá gắn chặt vào cành 0.000m!
    * Raycast Foliage Collider: 301/301 rays trúng collider (`colliderMiss = 0`).
    * Thân cây: `trunkVerticesUnchanged = True`.
  - **Audio Source (`audio_resume_probe.json`)**:
    * `rainPlaying = True`, `windPlaying = True`, `musicEnabled = True`.
  - **Directional Light (`light_restore_probe.json`)**:
    * Cường độ ánh sáng và màu sắc phục hồi chuẩn xác sau sấm chớp.
  - **Sky Lightning Camera Frustum (`sky_visibility_probe.json`)**:
    * 0/1000 tia sét đâm xuyên góc nhìn camera.

### N07 (P2): Định Dạng Locale Invariant Cho Evidence JSON
- Toàn bộ serialization kết quả đo đạc được chuẩn hóa qua `CultureInfo.InvariantCulture`, kiểm tra parse thành công bằng `System.Text.Json.JsonDocument` mà không bị ảnh hưởng bởi locale vi-VN hay en-US.

---

## 3. Tổng Hợp Kiểm Thử Tự Động
- **Unit/EditMode Tests**: **252 / 252 PASSED** (0 failed, 0 skipped).
- **PlayMode Component Tests**: 100% đạt các tiêu chí hình học, raycast và âm thanh.
- **Verification Runner (`t11_automated_test_results.json`)**: **100% PASS** cho toàn bộ 9 bộ test (R1..R9, N01, N02, N04).

## 4. Kết Luận
Task T11 đã hoàn thành toàn diện, khắc phục triệt để các phát hiện của đợt hậu kiểm độc lập, sẵn sàng chuyển trạng thái sang **DONE**.
