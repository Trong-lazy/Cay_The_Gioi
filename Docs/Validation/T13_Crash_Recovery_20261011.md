# T13 — Khôi phục Unity sau crash (11/10/2026)

Task: T13
Result: PASS — kiểm tra giới hạn của workaround, không phải nghiệm thu toàn bộ T13.

## Chẩn đoán

- Unity 6000.6.1f1, Windows Editor DX11, MainScene, PC_RPAsset.
- Crash 20:58:48 có native stack qua ParallelFilterChangedInstancesAndCreateScriptingArray -> ObjectDispatcher -> WorldProcessor.Update -> GPUResidentDrawer.PostPostLateUpdate. Ngay trước crash đang bắt đầu PlayMode tests. Chưa xác định thao tác cụ thể gây lỗi native.
- Crash 20:54:42 chỉ có Editor.log 4.178 byte dừng trong khởi động, không đủ stack để quy cùng nguyên nhân. Chưa phân tích minidump.
- PC_RPAsset bật GPU Resident Drawer; Mobile đã tắt.
- Báo cáo liên quan của Unity UUM-149114 có tình huống/stack khác, không coi là xác nhận cùng bug: https://issuetracker.unity.com/issues/23844/crash-on-prefixsumdrawsandinstances-and-other-gpu-resident-drawer-jobs-when-generating-a-3d-object-with-ai-assistant-and-performing-various-tasks-with-the-generated-model

## Thay đổi

- `Assets/Settings/PC_RPAsset.asset`: m_GPUResidentDrawerMode 1 -> 0. Workaround tránh đường render gây crash; có thể mất lợi ích tối ưu CPU của tính năng này, chưa đo hiệu năng.
- Không đổi Unity/package, model cây, script môi trường hoặc gameplay.
- Mở lại Editor, chọn Keep Backups; Unity giữ `Assets/_Recovery/0 (1).unity`. Không ghi đè MainScene từ recovery. Kết nối lại MCP thành công.

## Kiểm chứng

- MainScene mở được, scene sạch, pipeline PC_RPAsset báo GPU Resident Drawer Disabled.
- Play Mode với save riêng `Evidence/T13_Crash_Recovery_20261011/IsolatedSave`; gọi WorldEnvironmentController.BuildEnvironment để kiểm tra tạo/hủy renderer. Sau cập nhật có 67 renderer; chạy tới game time 29,56 giây và thoát bình thường. Không lưu thay đổi runtime.
- ArtPipelineSceneTests + WeatherSceneTests: 11/11 Passed, 0 fail/skip, 20,10 giây. Job 201f272de31946ae8250dd2c8f86c9f8 succeeded, XML lưu trong Evidence.
- Sau test: không compiling/playing, MainScene sạch, Console 0 error/warning, không có crash folder mới.
- SHA256 save chính và backup không đổi; khôi phục ValidationDirectory, ValidationUtc và runInBackground sau kiểm tra.
- Evidence: `Evidence/T13_Crash_Recovery_20261011/` gồm crash-stack.txt, PlayModeResults.xml, play-rebuild.json, play-stable.json, save-before.json và save-after.json.

## Bàn giao

Agent tiếp tục T13 với GPU Resident Drawer Disabled. T13 vẫn IN_PROGRESS; kết quả này không chứng minh mọi crash đã hết hay môi trường đạt toàn bộ acceptance. Nếu tái phát, giữ crash folder mới và thao tác trước crash để phân tích tiếp. Không xóa Library như bước sửa mặc định.
