# Schema lưu và xử lý offline

## Thiết kế sớm triển khai từng bước

Schema v1 dự kiến ngay từ T00 có vùng dành cho Rebirth, Weather, meta và Sức sống. T04 triển khai save/load run, settings và giá trị mặc định; T05 offline; T06 Rebirth; T07 Weather; T08 meta. Có vùng dữ liệu chưa được gameplay sử dụng không có nghĩa hệ thống đã hoàn thành.

JSON đặt ở Application.persistentDataPath; ví dụ world_tree_save.json và world_tree_save.backup.json. Không đưa save thực của người chơi vào Assets hoặc repository.

## Envelope dự kiến

```json
{
  "schemaVersion": 1,
  "configVersion": "prototype-1",
  "saveId": "stable-save-uuid",
  "revision": 1,
  "savedAtUtcSeconds": 0,
  "offlineCheckpointUtcSeconds": 0,
  "run": {
    "runId": "unique-run-uuid",
    "life": "ALIVE",
    "stageId": "seedling",
    "water": 10.0,
    "growth": 0.0,
    "levels": {"root": 0, "leaves": 0, "trunk": 0},
    "hp": 100.0,
    "weather": {
      "weatherId": "calm",
      "remainingOnlineSeconds": 120.0,
      "badStreak": 0,
      "recentWeatherIds": [],
      "incidentCooldownOnlineSeconds": 0.0
    },
    "status": {
      "statusId": "none",
      "remainingOnlineSeconds": 0.0,
      "impactRegionId": "none"
    },
    "runCounters": []
  },
  "permanent": {
    "stagePoints": [],
    "coin": 0,
    "achievements": [],
    "ownedCosmeticIds": [],
    "equippedCosmeticId": "none",
    "tutorialCompletedStepIds": [],
    "rebirthCount": 0,
    "highestStageEverId": "seedling",
    "permanentCounters": []
  },
  "vitality": {"tapProgress": 0, "bankSeconds": 0.0},
  "settings": {"musicEnabled": true, "orbitLocked": false}
}
```

Ví dụ là hình dạng dữ liệu, không dùng timestamp 0/UUID mẫu làm save thực. Dùng DTO có field public/properties hợp với serializer đã chọn; nếu dùng JsonUtility, các ví point/counter dùng list entry với ID, không Dictionary không serialize được. Validate ID trùng và số trước restore. Kiểm tra HP/life nhất quán, level trong cap stage, cosmetic equip đã sở hữu, bank trong 0–3600 và tapProgress nhỏ hơn tapsPerReward; không chỉ kiểm tra JSON parse được.

## Version và lỗi

- File không có: tạo run mới, không thưởng offline từ epoch.
- JSON hỏng, dữ liệu âm/NaN, ID lạ hoặc enum sai: giữ file lỗi, thử backup hợp lệ; nếu không có thì thông báo và chờ người chơi chọn retry/reset. Không âm thầm xóa dữ liệu.
- schemaVersion thấp: migration tuần tự có test/backup; thiếu field mới có default rõ.
- schemaVersion cao hơn hỗ trợ: không ghi đè; thông báo save chưa tương thích.
- configVersion khác: kiểm tra ID/cap và chính sách migration đã ghi Decisions; không tự mất point hoặc ownership.
- I/O thất bại: giữ bản chính và state trước giao dịch persistent quan trọng, báo lỗi/retry. Không báo hoàn thành offline/Rebirth/claim khi commit lưu cần thiết thất bại.

## Ghi an toàn

Tạo snapshot nhất quán → serialize → ghi file .tmp cùng thư mục persistentDataPath → flush/đóng → đọc xác minh DTO → thay file chính bằng cơ chế atomic phù hợp nền tảng PC/điện thoại và giữ backup. Lần ghi đầu dùng rename trong cùng filesystem; kiểm thử đường lỗi, không giả định API replacement luôn thành công. Save/checkpoint khi pause/focus loss; resume catch-up một lần, không phụ thuộc OnApplicationQuit. Xem Cross_Platform.md.

Gameplay online có thể tiếp tục với thông báo dirty save sau autosave lỗi, nhưng giao dịch persistent quan trọng (offline, Rebirth, claim/mua) phải có snapshot rollback hoặc chặn retry đúng quy tắc để không nhân đôi. Triển khai từng bước, không tạo distributed transaction framework.

Autosave theo config khoảng 30 giây, sau giao dịch quan trọng, khi focus/pause phù hợp và quit. Không ghi mỗi frame. Ghi checkpoint UTC tương ứng state đã mô phỏng online để không tính lại online time thành offline.

## Offline đúng một lần

1. Sau load lấy now UTC qua clock; elapsed là max(0, now - checkpoint). Với thời gian âm, không cấp thưởng và không lùi checkpoint để tạo thưởng về sau.
2. Thời gian được xét production là min(elapsed, 86400). Run DEAD cho delta bằng 0.
3. Evaluate ở neutral context: weather/status/vitality/click bị loại khỏi bonus; giữ effect vĩnh viễn đúng phạm vi. OfflineRateFactor 0.5 áp một lần cho toàn mô phỏng economy như Prototype_Config.
4. Tạo candidate state với Water/Growth delta; Weather/status giữ remainingOnlineSeconds cũ. bankSeconds và tapProgress của Sức sống giữ nguyên; không cộng thưởng từ tap hoặc hưởng buff +5% offline.
5. Candidate checkpoint tiến tới max(checkpoint, now), bao gồm cả thời gian vượt cap; tăng revision. Lưu candidate trước khi cho input hoặc hiện thưởng.
6. Nếu lưu thành công, commit runtime candidate và thông báo số được nhận. Nếu thất bại, không commit delta; retry cùng candidate hoặc reload trạng thái gốc, không chạy tính thưởng cộng vào runtime nhiều lần.

Mở lại ngay sau lưu không nhận lần hai. Nếu crash trước commit file, lần mở sau tính lại từ bản cũ nhưng lần trước chưa commit thưởng; nếu crash sau commit file thì checkpoint đã mới. Không có cơ chế hoàn toàn chống chỉnh đồng hồ/save cho game offline; chỉ xử lý elapsed âm, cap và idempotency, không thêm backend.

Backup có thể cũ hơn file chính và mất tiến độ gần nhất. Khi recovery từ backup, đánh dấu recovery; không tự trả catch-up từ timestamp cũ nếu không biết lần trước đã trả thưởng. Cho hiển thị cảnh báo recovery, đặt checkpoint recovery theo now bằng giao dịch lưu xác nhận trước tiếp tục; test chính sách này. Không tuyên bố backup ngăn được mọi rollback.

## Giao dịch Rebirth và meta

Rebirth candidate có point stage cũ +1, runId mới, reset run và permanent được giữ; save candidate một lần trước commit runtime/event. Yêu cầu runId nguồn và guard in-flight; command cũ không còn hợp lệ sau reset.

Claim candidate có Coin tăng và claimed true; buy candidate có Coin giảm và ownership thêm; equip candidate đổi ID đã sở hữu. Không trao thưởng qua event subscriber. Preview không đi vào candidate.

## Ma trận kiểm tra bắt buộc

- New game, save/load roundtrip, field mặc định, backup/hỏng file, schema cũ/mới và I/O lỗi.
- Offline 0 giây, 1 giờ, 24 giờ, 30 giờ, clock âm, DEAD, thiếu Water, effect point riêng và reopen ngay.
- Clock giả, crash/lỗi trước-sau commit, không nhân đôi Water/Growth, point hoặc Coin.
- Weather/status đã lưu chỉ tiếp tục online; không damage offline, không mất trạng thái DEAD.
- Tutorial/settings/equip/ownership giữ đúng; preview không lưu; Rebirth giữ Sức sống; offline không tiêu quỹ và không dùng buff Sức sống.

## Chính sách triển khai T04

D16 quy định schema1, default field tùy chọn, prototype-1→prototype-2 và chặn version/config khác; SaveCodec dùng Newtonsoft đã có. File.Replace không thành công thì báo lỗi và giữ bản chính, không xóa trước rename. Phục hồi backup yêu cầu xác nhận và commit checkpoint mới; reset lưu trữ file cũ trước. Không trao thưởng offline trong T04.


## Thực hiện T05

Giữ schema v1/config prototype-2; reward và offlineCheckpointUtcSeconds nằm trong cùng atomic snapshot, không thêm cờ pending vào save. OfflineCoordinator chụp clock một lần, evaluate từ source chưa thưởng, lưu candidate rồi mới expose Water/Growth. Load thường luôn qua offline commit (kể cả zero elapsed); không ghi checkpoint mới trước evaluate. Backup recovery có xác nhận vẫn chốt now và không award từ backup cũ. Runtime pending resume chỉ là state transient; I/O fail chặn command/tick/autosave/quit chưa thưởng. Retry/reopen đọc baseline/checkpoint đã commit, không nhân reward. UTC lùi không hạ checkpoint. Test cả before/after replacement và locked temp thật trong Unity Editor; không suy rộng sang mobile API chưa thử.

## T06 commit Rebirth / tương thích prototype-3

SchemaVersion vẫn1, DTO không thêm field. Rebirth candidate chứa runId mới, toàn bộ run reset, +1 point stage cũ/rebirthCount, permanent/vitality/settings giữ. SaveController.CommitCandidate kiểm tra current runId, initialized/enabled, suspended/pending offline/inflight; persist candidate atomic trước domain adopt/event. Không ForceSave source cũ trong transaction. BeforeReplace fail giữ revision/file/source; retry cùng run được một lần. AfterReplace đã ghi thì acknowledged, reopen expectedRunId cũ không thể thưởng lại. DEAD persist/reopen giữ forced modal, không tự reset.

Config hiện hành prototype-3 thêm effect rules/reset calm; prototype-1/2 được whitelist sang3 sau full validate, giữ tài nguyên/point/checkpoint/meta. Config lạ/schema cao hơn vẫn blocked và giữ file. Backup recovery vẫn yêu cầu xác nhận/checkpoint mới theo T04; không đoán migration lạ. Editor tests dùng temp directory/clock; native mobile atomic file API chưa nghiệm thu. Xem D18/Validation/T06_20261003.md.

## T09A Config prototype-5

Schema vẫn 1. SaveCodec chấp nhận prototype-1/2/3/4 khi runtime prototype-5, sau khi validate state. Không chuyển đổi, hoàn tiền hoặc reset số dư/level/Coin/point; loại tiền nâng cấp mới chỉ áp cho giao dịch tiếp theo. SaveService ghi configVersion mới ở lần save sau. Cấu hình chưa biết vẫn CONFIG_INCOMPATIBLE.


## T09A prototype-6 hiện hành

Schema1 bổ sung run.photosynthesis, run.spirit, run.spiritAbsorptionUnlocked, run.levels.spiritAbsorption. Save config6 bắt buộc các field mới; kiểm tra hữu hạn/không âm, khóa/cap/stage nhất quán. Không suy đoán version không biết.

Migration prototype1–5 cấp ví mới Quang hợp=initialPhotosynthesis5; khi ThânL0 bù Growth tối thiểu trunkBaseCost10 để mua nguồn sản xuất. Giữ Water, level, số Growth đã có (trừ phần bù bootstrap), point/Coin/meta/checkpoint; Linh khí0/khóa. Candidate ghi config6 cùng checkpoint trước expose offline nên grant chỉ một lần. Config6 load không bù lại bất kỳ ví nào. Offline candidate/commit gồm cả bốn ví; lỗi ghi không expose delta, retry không nhân đôi. Rebirth candidate giữ schema1/config6, reset run theo factory.

Các ca prototype5 cũ là lịch sử; validation T09A mới phải kiểm tra migration, vốn mới, save/load hấp thụ, offline và Rebirth.
