# Bằng chứng T07 — 03/10/2026

Nguồn kết quả: Docs/Validation/T07_20261003.md. Kết quả test và direct Play Mode thật qua Unity MCP.

- `editmode-final.json`: 153/153 ca EditMode PASS 100% (20 ca Weather/Risk/Burning/Health mới + 133 ca cũ).
- `playmode-final.json`: 47/47 ca PlayMode PASS 100% (6 ca WeatherScene mới + 41 ca cũ).
- `direct-play-weather.json`: Kết quả thực tế kiểm tra trực tiếp trong Unity Play Mode:
  - Khởi tạo mặc định: Mầm, Weather "calm" 120s, HP 100/100, Status "none".
  - Nắng ("sunny"): Buff Growth 2.4/s (+20%), ánh sáng vàng ấm RGBA(1.000, 0.960, 0.850, 1.000), intensity 1.625.
  - Mưa ("rainy"): Buff Water 2.4/s (+20%), ánh sáng xanh xám RGBA(0.720, 0.820, 0.950, 1.000), intensity 1.105.
  - Giông ("thunderstorm"): Ánh sáng u ám, sét đánh trúng Lá -> Status "burning" (10s), khối lửa placeholder bám đúng LeavesAnchor, Growth giảm 30% (từ 2.0 xuống 1.4/s).
  - Burning damage: Trừ 2 HP/giây. Khi HP về 0 -> Life = DEAD, dừng toàn bộ production, modal Forced Rebirth tự động bật lên.
  - Forced Rebirth: Bấm xác nhận -> Cây reset về Mầm, HP hồi phục 100, Weather về "calm", Status về "none", lửa biến mất, nhận điểm Rebirth an toàn.
  - Save/Load: Lưu và khôi phục chính xác Weather, Status, Impact Region, HP.
- Console cuối: 0 errors, 0 warnings.
