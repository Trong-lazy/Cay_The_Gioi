# Hậu kiểm độc lập bản sửa T10 — 07/10/2026

Task: T10  
Result: FAIL  
Trạng thái: IN_PROGRESS. Chỉ kiểm thử và cập nhật tài liệu, chưa sửa gameplay/asset.

## Môi trường

Unity 6000.6.1f1, MainScene, PrototypeConfig prototype-4, Game View 1920×1080.
Build: Builds/Windows_T10_20261007/Cay_The_Gioi.exe.
Evidence mới: Evidence/T10_Independent_Recheck_20261007/.
Các tọa độ màn hình dùng gốc bottom-left; seedling pose distance=1.4, pitch=16, targetHeight=0.12, yaw=0.

## P2 — Click mặt sau vỏ hạt không được render vẫn cộng tap

Điểm (913.731262,453.8399) không cắt tam giác front-facing nào của cây, nhưng Physics.Raycast trúng MeshCollider SeedShell. Material M_SeedShell_Outer/Inner là URP/Lit, _Cull=2 (Back), trong khi Physics.queriesHitBackfaces=true.

MouseState press/release qua các frame thật: tapProgress 0→1, IsOverUi=false. Bằng chứng backfaceclick.json. Probe front-face 25×25 tìm 4 điểm kiểu này ở seedling; young_tree/large_tree không có ở pose mặc định đã thử.

Nguyên nhân: raycast hai mặt không đồng bộ với render culling. Không mặc định yêu cầu thay model/material; cần giữ tiêu chí click phần cây thực sự thấy được.

## P2 — Collider đất thấp hơn mesh đất gây click xuyên đất

Điểm (913.731262,407.544922): Physics hit SeedShell tại (-0.041918,-0.001081,-0.013088), dưới đất. Ground mesh Cylinder top Y=0; BoxCollider top Y=-0.0024. Tia từ camera phía trên gặp đất hiển thị trước phần vỏ âm Y.

MouseState thực tế: tapProgress 1→2, IsOverUi=false. Bằng chứng groundclick.json. Probe tìm 3 điểm trúng cây âm Y trong footprint đất ở seedling; 0 ở hai stage còn lại tại pose đã thử.

Nguyên nhân: hạ collider 2.4mm nhưng giữ mặt đất hiển thị. Không dùng chính collider đã hạ làm oracle để kết luận render occlusion đúng.

## Kiểm thử và giới hạn

- EditMode: 191/191 PASS, edit_test_job.json.
- Windows mới: 6/6 QA PASS, exitCode=0; smoke_fresh/offline/progression/death_forced_rebirth/ui_flow/smoke_raycast. Batchmode có rendering, không nographics; không điện thoại thật. Log không có mẫu NullReferenceException/MissingReferenceException/Unhandled Exception/Shader error/CHECK_FAIL đã quét.
- smoke_raycast xác nhận hai tọa độ lỗi cũ trên large_tree đã sửa cho Mouse/Touch. Probe hai phía cũ 0/0 cả ba stage; không bao gồm hai lỗi mới trên.
- Full PlayMode lần 1: 60/63 PASS, 3 FAIL (play_first_results.xml). Chạy đồng thời Windows QA nên có khả năng ảnh hưởng focus/timing; không quy cả 3 thành lỗi gameplay.
- Full PlayMode lần 2, không chạy Windows đồng thời: 61/63 PASS, 2 FAIL (play_second_results.xml): Touch visible yaw-step3 expected8/actual7; Evolution Finish TransitionLocked expectedfalse/actualtrue. MCP báo init timeout nhưng Unity thực sự chạy đủ 63 và lưu XML. Các test FAIL khác giữa hai lần, chưa xác định nguyên nhân focus/timing.
- ArtPipelineSceneTests chạy riêng: 4/4 PASS, art_test_job.json; chứng minh test hiện tại bỏ sót hai lỗi trực tiếp, và full suite chưa ổn định.
- Lượt gọi full cuối trả total=0 (test filter/runner), KHÔNG tính PASS hồi quy; final_empty_test_job.json.
- Probe front-facing vẫn 0 miss tại các điểm đã thử. Không suy ra toàn bộ mọi góc/camera đạt.

## Lỗ hổng oracle

RayIntersectsTriangles dùng Abs(det) nên coi mặt sau bị render cull là bề mặt nhìn thấy. Che khuất dùng Physics.RaycastAll với Ground đã hạ thay vì geometry Ground hiển thị. Vì vậy cả test trong Editor lẫn build có thể PASS và bỏ qua hai lỗi mới.
Test tap dùng yield-null, có kết quả phụ thuộc timing cần điều tra; chưa có bằng chứng để kết luận các FAIL biến động là hồi quy domain T03/T06.

## Bảo toàn và bàn giao

Save thật trước/sau trùng SHA256:
main 91B3491C4D3B1F989B2BD82C8040C055F04BF91CAF7EB082CB2B3B83401DBC2F;
backup 9242975C6C2CA998EC3A26046E060D30A3F527532FEE0975C18E62E7B686CE65.
Editor dùng editor_save riêng; Windows mỗi case dùng save riêng; đã tắt Play và xóa override thử nghiệm.

seedling_camera.png là camera render không overlay UI; probe và mouse event là bằng chứng chính. Không sửa Blender/FBX/prefab/script/scene/project settings.
T10 mở lại IN_PROGRESS, bỏ tích collider/raycast và hồi quy chưa ổn định. Không task DONE nào phụ thuộc T10. T00–T09 chưa mở lại vì chưa chứng minh lỗi tiêu chí domain từ các test biến động.
Sửa hit-test theo culling và mặt đất thực tế, thêm assertion hai tọa độ trên cho Mouse/Touch, revalidate Editor và build Windows mới. Không sửa oracle để hợp thức hóa hit hoặc đổi luật gameplay.

