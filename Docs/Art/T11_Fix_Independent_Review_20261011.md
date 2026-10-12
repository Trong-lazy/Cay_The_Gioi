# Prompt sửa T11 sau hậu kiểm độc lập

Hãy thực hiện sửa lỗi và nghiệm thu lại T11 trong dự án:
C:\Users\Asus\foder game 3D\Cay_The_Gioi

Đọc AGENTS.md, START_HERE.md, CURRENT_TASK.md, Docs/Tasks/11_VFX_Tree_Animation_Weather_Audio.md và báo cáo bắt buộc:
Docs/Validation/T11_Independent_Review_20261011.md

Bằng chứng nằm tại Evidence/T11_Independent_Review_20261011/. Báo cáo DONE cũ đã bị hậu kiểm bác bỏ. T11 hiện IN_PROGRESS. Chỉ sửa T11 và hồi quy trực tiếp; không làm lại model, thêm thời tiết, thêm package hoặc nâng Unity. Không chạy agent khác sửa chung Editor/scene.

## 1. Tái hiện trước khi sửa

- Kiểm tra đúng phiên Unity, MainScene, config, Console và scene chưa lưu.
- Bảo vệ save thật bằng save directory riêng; lưu hash trước/sau.
- Dùng probe/ảnh trong Evidence làm hướng dẫn, tự tái hiện trên code hiện tại.
- Giữ animation hoạt động trong các ca visual/raycast: test cũ bật ManualTick, khiến TreeAnimationController.Update thoát ngay.
- Lưu bằng chứng trước/sau; không lấy test PASS cũ thay nghiệm thu.

## 2. Sửa animation và liên kết lá–cành–thân (R1, ưu tiên cao nhất)

Lỗi thực tế: mesh lá large_tree có lossyScale100, flutterAmp cộng trong mesh-local thành dịch chuyển vài mét. Mẫu đo 3.6678 world; tán bay khỏi thân. Tất cả vertex lá bị dịch, không giữ chân lá/cuống; Mầm/Non cũng có nguy cơ hở chỗ nối.

Yêu cầu:
- Chuẩn hóa không gian/đơn vị chuyển động theo kích thước cây thật, xét đầy đủ transform scale/rotation. Không chữa bằng hằng số chia100 riêng cho một model.
- Bình yên/Nắng/Mưa vẫn chỉ lá lay nhẹ; cành/thân đứng yên là đúng luật. Giữ điểm nối cuống/phiến lá với cành, chuyển động tăng dần ra đầu lá và lệch pha hợp lý giữa các cụm.
- Giông: gốc/rễ cố định, thân/cành uốn có phân bố theo chiều cao/nhánh; lá kế thừa cùng biến dạng thân/cành rồi mới thêm flutter cục bộ. Không để tán trượt độc lập hoặc chỉ quay cả thân như thanh cứng.
- Dùng giải pháp đơn giản tương thích URP/mobile, dễ thay model; không làm lại Blender nếu có thể xử lý trên dữ liệu hiện có.
- Tham chiếu phần cây/anchors rõ ràng trong prefab/config; tránh ràng buộc tên object để quyết định hành vi.
- Anchor sét, lá, thân và Burning phải theo đúng hình học đang chuyển động, kể cả Evolution/Rebirth.
- Chuyển weather mượt, không giật biên độ/tần số đột ngột.

## 3. Sửa raycast khi animation chạy (R2)

- Collider hiện không cập nhật theo vertex lá; probe 301 ray tại lá đang render miss301, đối chứng recook miss0.
- Physics.SyncTransforms chỉ đồng bộ Transform, không đủ chứng minh collider khớp mesh biến dạng.
- Chọn cách đồng bộ hit-test/geometry hợp lý; không tạo collider thô phủ cả khoảng trống, không mặc định recook mesh41.480 vertex mỗi frame mà chưa đánh giá chi phí.
- Test nhiều thời điểm animation, ba stage và nhiều góc: click lá đang thấy được, khoảng trống nơi lá vừa rời, silhouette, mặt bị cull, phía sau Ground, chuột và touch mô phỏng.
- Test phải thất bại trên bản lỗi trước sửa. Không tắt animation để đạt PASS.

## 4. Sửa mưa và sét trời (R3/R4)

- Bỏ sai lệch cộng độ cao emitter6.5 + shape offset8 làm phần lớn mưa chết trên cao.
- Mưa phải rơi xuyên vùng chơi nhìn thấy được ở cả ba camera và khoảng orbit/zoom cho phép.
- Điều chỉnh độ dài/bề rộng/alpha vệt theo scale hình ảnh; không để thành dải chữ nhật to cạnh Mầm/Cây Non.
- Tia sét trời hiện sinh tại x±5..13, y8.5..12.5; probe1.000 mẫu mỗi stage đều ngoài frustum camera mặc định. Sinh sét ở vùng trời camera nhìn thấy, vẫn hợp hướng nhìn khi orbit; không dùng tọa độ world cố định xa khung.
- Sét trang trí độc lập RNG gameplay, không gọi damage; Giông không trúng cây vẫn thấy tia sét, chớp và nghe sấm.
- Tia gây hại theo anchor động đúng thời điểm; kiểm chứng Burning ba vùng.
- Không thêm thời tiết hoặc đổi buff/unlock.

## 5. Sửa lifecycle audio/ánh sáng (R5/R6)

- Repro audio: tắt tiếng → chuyển Giông khi loop đang dừng → bật tiếng. Hiện target volume>0 nhưng rainSource/windSource không Play.
- Khi bật lại hoặc resume, đồng bộ cả mute/volume/playback theo weather, pause/focus và life hiện hành; không tạo loop trùng.
- Tách đúng trạng thái pause và focus để callback này không vô tình xóa trạng thái khóa của callback kia. Kiểm tra one-shot sấm, DEAD và Rebirth.
- Repro ánh sáng: Giông → strike → calm trước hết flash; coroutine trả lại màu/intensity Giông dù weather đã calm.
- Quản lý ánh sáng nền theo weather hiện tại và flash tạm riêng; hủy/chồng coroutine/Rebirth/đổi weather không ghi đè trạng thái mới.
- Kiểm tra không còn tia, flash, cháy hoặc âm thanh run cũ sau Rebirth; dọn runtime materials/clips/subscriptions đúng vòng đời.
- Đánh giá nghe thực tế loop mưa/gió/sấm; nếu thiếu khả năng nghe, ghi giới hạn, không tự tuyên bố âm thanh mượt.

## 6. Sửa lịch sét, save và biên thời gian (R7/R8)

Giữ nguyên luật người dùng:
- 10% mỗi đợt Giông đủ điều kiện, xét một lần, tối đa một cú gây hại.
- Protection chỉ giảm damage; không thay xác suất. Stage đầu vẫn được bảo vệ.
- Burning gốc10s, 2HP/s, công thức prototype đã chọn; HP0 mở Forced Rebirth, chờ xác nhận.
- Không damage/sét/chết offline; save-load không roll lại hoặc double strike.

Sửa:
- SaveValidator phải kiểm tra finite/nonnegative/nhất quán cho strikeDelaySeconds và các cờ mới. Decode hiện chấp nhận NaN khiến sét treo vĩnh viễn.
- Phân biệt save cũ thiếu field với dữ liệu hỏng; migrate an toàn, không reset tiến trình hay tạo sát thương hồi tố.
- Tick lớn vượt lịch sét/kết thúc weather không được bỏ qua strike hoặc tính Burning sai thời gian. Probe Giông60s, strike18s: dt60 bỏ toàn bộ sét trong khi dt0.1/1/10 có strike.
- Chia thời gian tại mốc weather/strike/Burning/death, đảm bảo tích phân nhất quán; không giải quyết bằng bỏ dt. Kiểm tra cả tick nhỏ qua đúng ranh giới gây sét, không chỉ tick60s.
- Thêm regression tests tương ứng, save-load trước/sau strike, pause/resume và offline.

## 7. Cấu hình và kiểm thử (R9)

- Đưa tuning animation, rain, flash/sky bolt, audio volume vào config/profile nhỏ dễ chỉnh. Hiện chúng hard-code dù checklist đã tích cấu hình.
- Không trộn config, state runtime và save DTO; không thêm framework.
- Chạy test logic cần thiết và hồi quy liên quan. Test visual/geometry phải chạy animation thật.
- Play Mode kiểm chứng đủ ba stage × bốn weather, nhiều thời điểm và góc camera; ghi rõ debug override ở stage bảo vệ.
- Lưu video ngắn hoặc chuỗi frame + số đo để chứng minh cuống lá gắn cành, uốn đồng bộ, collider khớp và mưa/sét rõ. Ảnh tĩnh đơn lẻ không đủ chứng minh chuyển động.
- Kiểm tra 1920×1080,1366×768; build/smoke Windows với save riêng sau khi sửa.
- Kiểm tra HP→DEAD→modal không hủy→Confirm→reset, không cấp point lặp và lỗi lưu xử lý đúng.
- Kiểm tra hiệu năng trên Windows; mobile thiết bị thật vẫn deferred, không ghi mobile PASS.

## 8. Tài liệu và điều kiện hoàn thành

- Bổ sung kết quả vào Validation mới, giữ báo cáo cũ là lịch sử bị hậu kiểm bác bỏ.
- Phân biệt CONFIRMED và PROTOTYPE_DEFAULT; không coi hard-code/chi tiết triển khai trong D49 là yêu cầu người dùng bắt buộc giữ khi nó gây lỗi.
- Đồng bộ tài liệu bị ảnh hưởng. Nếu sửa GDD Markdown, xuất lại Word và kiểm tra bố cục.
- Cập nhật task, Evidence và bàn giao; chạy Tools/Docs/update_progress.py.
- Không DONE nếu còn lỗi trên hoặc chưa có kiểm chứng trực tiếp Unity. Ghi WAITING_VALIDATION/BLOCKED đúng lý do khi cần.
- Báo cáo cuối: từng R1–R9 đã sửa thế nào, bằng chứng/test trước-sau, file/config thay đổi, lỗi còn lại, hash save thật và trạng thái task.

Tự xử lý các lựa chọn kỹ thuật trong phạm vi này. Không hỏi lại luật đã chốt và không kết thúc ở việc đưa kế hoạch.

