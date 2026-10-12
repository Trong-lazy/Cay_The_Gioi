---
id: T09
title: Tích hợp và nghiệm thu khung Windows
status: DONE
depends_on: ["T08C"]
validation: "Docs/Validation/T09_20261003.md"
---

# T09 Tích hợp và nghiệm thu khung Windows

## Mục tiêu

Vòng chơi khung hoàn chỉnh và build offline PC Windows được nghiệm thu, sẵn thay đồ họa Blender sau. Theo yêu cầu ngày 04/10/2026 và D12, build/test điện thoại thật tạm hoãn sang Docs/Deferred/Mobile_Validation.md; không chặn DONE của T09 Windows. Giữ code/input/UI dùng chung.

## Thực hiện

- [x] Chạy mọi acceptance quan trọng/save matrix trên MainScene, sửa lỗi integration/hồi quy.
- [x] Tuning config dựa trên đo thực: Evolution, Upgrade wait, Protection, death; giữ luật đã chốt.
- [x] Kiểm tra UI 1920×1080, particle/text/SFX giới hạn và không lỗi tích lũy trong phiên dài.
- [x] Xác minh nghiệm thu build Windows từ MainScene bằng report/log thật; bổ sung bằng chứng save/load/catch-up và forced/voluntary loop còn thiếu tại đường dẫn đã ghi.
- [x] Ghi build/version/config và minh chứng thực có tồn tại; cập nhật GDD/Decisions nếu tuning ảnh hưởng hợp đồng.

## Acceptance

- [x] Hoàn tất vòng Mầm→Cây Non→Cây Lớn→Rebirth, point/cap/resource đúng, tutorial không kẹt.
- [x] Sức sống nhận theo nhóm/cap/+5%/không stack, cạn đúng, giữ Rebirth/freeze offline; offline 24h/50% không dùng buff.
- [x] Giông/Burning/DEAD/Forced Rebirth tái hiện được; bình yên neutral, không lỗi modal/preview.
- [x] Claim/mua/equip/save/load không lặp thưởng; JSON hỏng/I/O lỗi có hành vi được ghi.
- [x] Xác minh build mở trên Windows không cần mạng/Blender/Unity Editor, không lỗi chặn loop; có bằng chứng Standalone lưu lại được.
- [x] Có số đo thực và kế hoạch mời 3–5 người chơi; nếu chưa playtest thì ghi chưa thử, không bịa kết quả.

## Nghiệm thu chung

- [x] Compile/tests liên quan và full integration Play Mode đạt; không chỉ kiểm tra Editor compile.
- [x] Build smoke test và hồi quy toàn chuỗi có log/report/screenshot thực, lỗi acceptance được xử lý.
- [x] Validation PASS trong phạm vi Windows/Evidence, metadata validation và bàn giao đường dẫn build/known nonblocking issues; mobile ghi DEFERRED riêng.
- [x] DONE rồi tổng hợp; task Blender chỉ tạo sau khi khung đạt hoặc người dùng đổi ưu tiên.

## Bàn giao

Phiên 04/10/2026:
1. Đã hoàn thiện toàn diện suite kiểm thử tự động Standalone cho Windows độc lập:
   - Sửa lỗi đường dẫn có khoảng trắng khi truyền tham số dòng lệnh cho player độc lập.
   - Thêm kịch bản kiểm thử tương tác uGUI EventSystem (`ui_flow`) kiểm tra trực tiếp luồng click các panel: Settings (toggle BGM), Upgrade (mua Root, Leaves, Trunk), Evolution (tiến hóa lên Cây Non và Cây Lớn qua nút Evolve trên panel), Thành tựu (claim thưởng Coin), Cửa hàng (mua và trang bị cosmetic), Rebirth (mở modal và xác nhận Rebirth từ Cây Lớn), ForceSave.
   - Tối ưu cơ chế chụp màn hình trực tiếp từ khung hình hoàn chỉnh (bao gồm cả overlay UI 1920×1080) và tạo chuỗi 12 ảnh minh chứng theo từng bước.
2. Bản build Standalone Windows mới nhất tại `Builds/Windows/CayTheGioi.exe` (101.88 MB, 0 error).
3. Chạy thực tế 5/5 kịch bản tự động trên bản build độc lập với thư mục save thử nghiệm riêng (`Evidence/T09/StandaloneQA/qa_save`), đạt 100% ExitCode = 0:
   - `smoke_fresh`: ExitCode 0, tạo run mới, tap 50 lần nhận Sức sống, mua Root 1, Leaves 1.
   - `smoke_offline`: ExitCode 0, tua 3600s, nhận thưởng ngoại tuyến 50% = 1800s hiệu lực (+1,350 Water, +1,800 Growth), bảo toàn quỹ Sức sống.
   - `smoke_progression`: ExitCode 0, full loop tiến hóa và Rebirth qua hook domain.
   - `smoke_death_forced_rebirth`: ExitCode 0, kiểm tra trạng thái DEAD và modal Forced Rebirth bắt buộc.
   - `ui_flow`: ExitCode 0, toàn bộ chuỗi bấm nút giao diện người dùng thực tế.
4. Bảo toàn dữ liệu người chơi thật 100%: hash SHA256 của `world_tree_save.json` và `world_tree_save.backup.json` tại `AppData/LocalLow/DefaultCompany/Cay_The_Gioi` trước và sau quá trình kiểm thử không đổi (`D9013370...` và `3A3C9995...`).
5. Toàn bộ báo cáo JSON, logs, save snapshots và 16 screenshots thực tế được lưu tại `Evidence/T09/StandaloneQA/`.
6. Hồi quy hoàn chỉnh: 190/190 EditMode tests PASS, 59/59 PlayMode tests PASS. Console 0 lỗi.
7. Mobile DEFERRED tại `Docs/Deferred/Mobile_Validation.md` theo D12, không chặn nghiệm thu Windows.
8. T09 hoàn tất nghiệm thu, chuyển trạng thái `DONE`. Chuẩn bị task T10 cho giai đoạn đồ họa Blender.
