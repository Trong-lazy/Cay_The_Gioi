# Kiểm tra độc lập T10 sau báo cáo hoàn thành

Task: T10
Date: 2026-10-07
Result: PARTIAL

Kết luận: tích hợp model/material/anchor và các bộ test hiện có hoạt động, nhưng acceptance raycast chưa đạt. T10 mở lại `IN_PROGRESS` theo AGENTS.md. Phiên này kiểm tra và ghi trạng thái; chưa sửa code gameplay, collider, prefab, config hoặc model.

## Môi trường và cách kiểm tra

Unity MCP đúng `Cay_The_Gioi@c16bfba113f4554d`, Unity 6000.6.1f1, WindowsEditor/StandaloneWindows64. Scene `Assets/_Project/Scenes/MainScene.unity`, `PrototypeConfig.asset` prototype-4; camera ba stage 1.4/2.8/6.2 m, pitch 16/18/20, target height 0.12/0.42/1.18 m.

Đọc báo cáo agent `T10_20261007.md`, task, source presentation/input/test, prefab/config hiện hành, ảnh và JSON/log đã bàn giao. Chạy lại toàn bộ test qua Unity Test Runner; vào MainScene Play trực tiếp với save riêng `Evidence/T10_Independent_Review_20261007/editor_save` và clock 2000000000. Fund Water/Growth thử riêng và gọi API Evolution tới Non rồi Lớn. Các phép đo ba visual bằng `TreeView.ShowStage` được ghi nhận là kiểm tra presentation, không phải ba giao dịch Evolution. Queue MouseState qua Input System để kiểm tra click; không gọi TryTap làm bằng chứng click.

Evidence phiên tại `Evidence/T10_Independent_Review_20261007/`.

## Kết quả kiểm thử lại

| Kiểm tra | Kết quả | Bằng chứng |
| --- | --- | --- |
| EditMode toàn bộ | 191/191 PASS, 0 skipped | `editmode_result.json`, job `101e859b33a3422c9aa6675c01a15529`, 1.468 s |
| PlayMode toàn bộ | 62/62 PASS, 0 skipped | `playmode_result.json`, job `79e2df6327f046318537a34784b04062`, 34.703 s |
| Windows build hiện có, 5 kịch bản QA | 5/5 PASS, ExitCode 0 | `standalone_20261007_203714/summary.json`, mỗi case có report/player.log/save/ảnh |
| Offline trong Windows | 3600 s, +1350 Water/+1800 Growth | `standalone_20261007_203714/smoke_offline/report.json`; fixture Root1/Leaves1, clock checkpoint +3600 |
| Lightning/Burning/cosmetic trong Play trực tiếp | PASS về điểm neo tại Cây Lớn | `vfx_result.json`: sét endpoint delta 0; Burning root/trunk/leaves đúng parent/delta 0; glow đúng TrunkAnchor/delta 0 |
| Raycast bổ sung và click device events | FAIL | `raycast_probe.json`, `off_model_click.json`, `visible_model_miss.json` và ba script `.cs.txt` |
| Console cuối phiên | 0 error/warning được query | `editor_final.json`; không sửa package hoặc clear Console |

Lần khởi chạy EditMode đầu (`279eb1f8406d49b4afdc61c7e07b0f83`) timeout khởi tạo 15 s, chưa chạy test; retry với init_timeout 120000 đạt 191/191. Đây là lỗi khởi tạo công cụ, không phải một test logic fail.

Lần Standalone đầu mở hidden/windowed ở 1280×720 bị focus loss, gây SaveController SUSPENDED và các command/UI không chạy đủ (2/5 PASS). Giữ nguyên toàn bộ bằng chứng tại `standalone_20261007_203549/`. Chạy lại executable không đổi bằng `-batchmode`, có rendering, 1920×1080 và save cô lập đạt 5/5. Không quy lỗi lần hidden thành hồi quy gameplay đã xác nhận, cũng không coi batchmode là playtest tương tác tay/window focus. Không build lại trong review này. Script tái lập: `run_standalone.ps1`.

## Lỗi trực tiếp ngăn DONE: collider lệch vùng model

`TreeInputAdapter.OnTree` nhận mọi Physics.Raycast hit có TreeView trong parent. TreeView giữ SphereCollider cộng các CapsuleCollider trên prefab; không kiểm tra ray có chạm hình model thật. Tại Cây Lớn, capsule `CanopyTapCollider` phủ cả khoảng trống và vẫn bỏ sót phần tán bên trái.

Tái hiện ở camera pose chuẩn Cây Lớn, Game View 1920×1080, screen coordinate gốc dưới trái:

1. Click trái `(611.49, 629.6163)`: ray không giao bất kỳ tam giác model nào, UI không chặn, nhưng Physics hit `CanopyTapCollider`. Queue press/release qua các frame thực: tapProgress **0 → 1**. Đây là lỗi nhận tap ở vùng trống, xác nhận bằng `off_model_click.json`.
2. Click trái `(573.2952, 412.3293)`: ray giao tam giác model ở khoảng **6.1323 m**, collider chỉ hit Ground ở **8.0961 m**. UI không chặn, tapProgress **1 → 1**. Đây là điểm trên vùng model bị bỏ sót; `visible_model_miss.json`.

Phép đo bổ sung: lấy world vertices/triangles mesh đang hiển thị, dùng giao ray–tam giác hai mặt (Möller–Trumbore), so với Physics.Raycast trên lưới 25×25 phủ projected model bounds +40 px. Bỏ điểm bị UI chặn. Không tạo MeshCollider hoặc sửa mesh. Số điểm là mẫu trong góc hiện tại, không phải tỷ lệ lỗi tất cả pixel/góc nhìn:

| Stage | Điểm được đo | Collider hit nhưng ray không giao mesh | Ray giao mesh nhưng collider bỏ sót theo ngưỡng probe |
| --- | --- | --- | --- |
| Mầm | 625 | 168 | 3 |
| Non | 613 | 162 | 0 |
| Lớn | 580 | 84 | 17 |

Các mẫu bỏ sót ở Mầm có hai điểm gần mặt đất với chênh lệch dưới 2 mm; cần phân biệt occlusion/epsilon khi sửa, không coi tất cả số đếm là lỗi nhìn thấy độc lập đã chứng minh. Phép ray hai mặt cũng có thể gồm mặt bị cull, nên số mẫu là chẩn đoán. Hai điểm click Cây Lớn ở trên là tái hiện cụ thể để agent xử lý và kiểm tra hình ảnh. Ảnh Play mới: `large_direct_play.png` (camera render, không có overlay UI).

Test `RaycastTapHitsTrunkAndCanopyAndMissesEmptySpaceAcrossAllThreeStages` chỉ thử TrunkAnchor/LeavesAnchor và điểm xa `(0.15*width, 0.45*height)`, không thử rìa tán, phần rễ hoặc khoảng trống gần cây. Vì vậy full test PASS chưa bao phủ acceptance raycast.

## Sai lệch và thiếu bằng chứng trong báo cáo agent

- `Evidence/T10/blender_audit_step1.json` được dẫn ở report/task nhưng không có trên đĩa tại review; chưa chạy audit Blender mới trong phiên này. Cần khôi phục bằng chứng thật hoặc sửa mô tả cho đúng nguồn kiểm chứng hiện có.
- Bảng anchor/collider mục 3 của report không khớp prefab đã lưu trước giờ báo cáo: radius Sphere hiện tại Mầm/Non/Lớn = **0.14/0.24/1.02**, không phải 0.22/0.52/1.35. Cây Lớn có hai capsule: thân center `(0,0.55,-0.02)`, r0.28/h1.2; canopy center `(0.02,1.35,-0.08)`, r0.52/h2.9. LeavesCenter Cây Lớn `(0.02,1.52,-0.10)`, lightning `(0.02,2.22,-0.08)`. Cần cập nhật bảng theo asset hiện hành, không lấy thông số cũ làm kết quả test.
- Report mô tả ArtPipelineSceneTests kiểm định camera framing/Evolution ba stage rộng hơn assertion thực tế: test VFX Evolution dừng tại Non; test visual dùng ShowStage/SetStagePose trực tiếp, không assert framing. Các test Evolution khác/full-loop QA bổ sung phần progression, nhưng cần dẫn đúng test và giới hạn.
- Player log agent có thông báo shader postprocessing không được hỗ trợ/bị strip (Gaussian/Bokeh DOF, Panini). Chưa thấy exception/runtime error liên quan gameplay từ các dòng này; phân biệt cảnh báo render khỏi tuyên bố log hoàn toàn sạch.
- 13 asset được kiểm lại từ `protected_files_verification.json` khớp expected. Hai save thật hiện khác hash lịch sử agent trước khi review; last-write 20:31:54/20:32:09, trước các test phiên này. Không đủ chứng cứ quy kết agent làm đổi save. Hash hiện hành giữ nguyên xuyên qua các lần test/review.

## Bảo toàn và bàn giao

Save main trước/sau review: `91B3491C4D3B1F989B2BD82C8040C055F04BF91CAF7EB082CB2B3B83401DBC2F`.
Save backup trước/sau review: `9242975C6C2CA998EC3A26046E060D30A3F527532FEE0975C18E62E7B686CE65`.

Cuối phiên Play stopped, MainScene clean, override directory/clock null, input settings về ResetAndDisableNonBackgroundDevices/PointersAndKeyboardsRespectGameViewFocus, Enter Play Mode Options disabled như trước. Các device giả được gỡ và native Pointer được bật lại. Không chỉnh scene/prefab/config/model/.meta. Chỉ thêm Evidence, report và cập nhật trạng thái/tài liệu bị ảnh hưởng; GDD không đổi nên không xuất Word.

T10 mở lại IN_PROGRESS; bỏ tích implementation collider, acceptance raycast và mục hoàn tất checklist. Không có task đã DONE nào phụ thuộc T10 trong danh mục hiện tại; T00–T09 giữ DONE, full suites vẫn PASS.

Bước sửa tiếp theo: điều chỉnh collider/hit validation có cấu hình cho ba stage, thêm kiểm thử click gần silhouette/rìa tán/rễ ở nhiều góc và bằng chuột/touch; giữ UI/drag/mouse-touch arbitration. Không bắt buộc một kỹ thuật collider cụ thể. Sau sửa, nghiệm thu lại raycast trực tiếp, hồi quy và build Windows với code/prefab mới; bổ sung bằng chứng thiếu, cập nhật bảng báo cáo rồi mới cân nhắc DONE. Phiên kiểm tra này không tự thực hiện fix ngoài yêu cầu review.
