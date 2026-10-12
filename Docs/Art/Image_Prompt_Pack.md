# Bộ prompt hình ảnh — Cây Thế Giới

> Cập nhật theo ảnh người dùng ngày 04/10/2026: hướng cây mới là bonsai cổ thụ với thân uốn, rễ nổi và tán trải ngang nhiều tầng, thể hiện chân thực hoặc anime bán chân thực. Dùng [bộ prompt theo ảnh bonsai](Bonsai_Reference_Prompts.md) cho hình dáng cây và rendering. Các đoạn low-poly/cozy/khối tán đơn giản dưới đây là đề xuất cũ, đã được thay về mỹ thuật; P01–P13 vẫn dùng tham khảo nội dung và bố cục UI, nhưng áp style UI mới trong tài liệu bonsai. Chưa chọn độc quyền giữa chân thực và anime; chưa tích hợp art vào Unity.

Ngày: 04/10/2026. Dùng để tạo ảnh với Google Flow hoặc công cụ tạo ảnh trong ChatGPT. Đây là đề xuất mỹ thuật để chọn hướng, không thay đổi GDD, luật gameplay, config hoặc trạng thái T10. Chưa tạo hoặc tích hợp asset trong Unity.

## Cách sử dụng

1. Tạo P00 trước, chọn một ảnh làm chuẩn màu/chất liệu. Khi tạo ảnh tiếp theo, đính kèm ảnh chuẩn và dùng câu giữ phong cách ở cuối tài liệu. Không dựa vào tên game để công cụ tự nhớ phong cách.
2. Trong Flow, chọn chế độ Image và tỷ lệ phù hợp; thêm ảnh tham chiếu vào prompt. Hướng dẫn chính thức: https://support.google.com/flow/answer/16729550?hl=en.
3. Mỗi lượt dùng một prompt. Với ChatGPT, mở đầu bằng “Tạo một ảnh theo prompt sau:” rồi dán prompt. Prompt có nhắc reference chỉ dùng sau khi đính kèm ảnh tương ứng.
4. UI concept: tỷ lệ 16:9; có thể yêu cầu 1920×1080 nhưng kiểm tra kích thước đầu ra thực tế. Icon/asset đơn: 1:1. Model turnaround: 16:9 hoặc 3:2. Prompt không đảm bảo file có đúng độ phân giải, alpha hoặc cấu trúc yêu cầu.
5. Ảnh màn hình UI dùng để tham khảo bố cục. Asset UI rời dùng P10/P11, icon dùng P12/P13. Chữ, số, thanh tiến độ, trạng thái nút được dựng trong Unity, không cắt nguyên ảnh mockup để làm UI chức năng.
6. Với sprite, yêu cầu transparent background nếu công cụ hỗ trợ alpha. Nếu không, tạo nền phẳng tương phản rồi xử lý tách nền; bàn cờ vẽ trong ảnh không phải alpha thật. VFX concept không phải shader, particle prefab hay flipbook đã được kiểm chứng.
7. Ảnh model vẫn là ảnh 2D. Muốn cây xoay camera được: dùng ảnh làm concept để dựng Blender hoặc chuyển ảnh sang mesh bằng công cụ 3D, rồi kiểm tra geometry, pivot, scale, materials, collider và anchors trong Unity. Ảnh nhiều góc có thể sai lệch, cần sửa cho nhất quán trước khi dựng.

## Phong cách đề xuất

- Fantasy thiên nhiên ấm áp, low-poly stylized, hình khối rõ, chi tiết vừa phải, không photorealistic.
- Lá xanh ngọc #4FAD72, xanh đậm #173D32, nền kem #F3ECD9, gỗ #805A3C, vàng #D8B66A, Water xanh #61B8D8. Mã màu là mục tiêu tham khảo, cần chuẩn hóa lại trong asset thực.
- Cosmetic: ngọc bích dịu. Burning: cam/đỏ cảnh báo. Hai hiệu ứng có hình và màu khác nhau.
- Một cây và một bệ đất trong scene; ba stage Mầm/Cây Non/Cây Lớn. Không thêm nhân vật, công trình, bản đồ, nhiệm vụ, quảng cáo hoặc tiền tệ mới.
- Prompt tiếng Anh để tiện dùng lại giữa các công cụ; nhãn UI tiếng Việt. Những số ghi trong prompt UI chỉ là dữ liệu minh họa đúng prototype hiện tại.

## P00 — Bảng phong cách tổng thể

```text
Create one cohesive visual style board for an original offline idle tree game called “Cây Thế Giới”. Cozy nature fantasy, stylized low-poly 3D, readable silhouettes, restrained magical details, simple matte surfaces, jade-green foliage, warm brown bark, ivory UI panels, forest-green typography and muted gold accents. Show a tiny two-leaf seedling, a young tree and a larger layered-canopy tree from the same fictional species, one modest circular soil platform, one sample HUD panel, three simple resource icons, and a small jade cosmetic aura. Organize them as a clean art-direction board on a neutral ivory background. The trees must feel suitable for a small Unity game with a freely orbiting camera, rather than a painted cinematic forest. Avoid photorealism, intricate textures, gigantic world trees, characters, buildings, neon colors, excessive bloom and ornate unreadable UI. Use no text, logos or watermark. Landscape 16:9.
```

## P01 — HUD màn hình chơi chính

```text
Design a polished landscape gameplay UI concept for “Cây Thế Giới”, an offline idle tree game. Use cozy low-poly nature fantasy, jade leaves, brown wood, ivory panels, dark forest-green text and muted gold details. One young tree stands on a circular soil platform in the center; keep its silhouette and tap area unobstructed. Respect a 5 percent screen-edge safe margin. Top left: compact “Cài đặt” and “Thành tựu” buttons, then a tidy resource panel for Water, Growth, HP and Coin. Top center: “Cây Non” and a separate “Tái sinh” button. Top right: one current-weather card with an icon and timer. Bottom left: “Cửa hàng”. Bottom right: the prominent “Nâng cấp” button. Bottom center: a compact “Sức sống” time bank showing “20s / 3600s”, a separate “0 / 50” tap counter and “+5%”. HP is health; vitality is a time bank, so visually separate them. Use large legible Vietnamese labels, generous spacing, short rows and touch-friendly controls. Reserve an empty notification lane below the top HUD. No overlaps between panels, buttons, tutorial or toast. Flat screen composition, no phone frame. No joystick, quest list, ads, premium gems or extra currencies. Landscape 16:9.
```

## P02 — Panel Upgrade

```text
Create a landscape in-game upgrade panel concept for a cozy low-poly fantasy tree game. Use ivory panels, forest-green typography, jade primary buttons, small wood accents and muted gold borders. Keep the tree visible in the left half. Place a spacious upgrade panel on the right, below the persistent top HUD, with a clear “Nâng cấp” heading and a large close button. Three consistent rows: “Rễ” with a roots-and-water icon, “Lá” with a leaf-and-sun icon, and “Thân” with a trunk-and-shield icon. Each row has separate level/cap, current output, next output, Water cost, and a prominent “+1” button. Show the “Cây Non” state with levels Rễ 1/10, Lá 1/10, Thân 0/5. Costs shown are 8, 8 and 10 Water respectively. Below the rows, show “Cây Non → Cây Lớn”, a cost of “200 Water + 100 Growth”, and a separate “Tiến hóa” button. Keep labels and values aligned, avoid decorative clutter. No skill tree, random loot or new resources. No panel extending beneath top buttons. Landscape 16:9.
```

## P03 — Shop và cosmetic

```text
Design a landscape cosmetic shop screen for a cozy fantasy idle tree game with jade, ivory, wood and muted gold colors. The title is “Cửa hàng”. Show the current Coin balance in the header and a large close button. Left: a live-preview area with the same low-poly tree on its soil platform. Right: one spacious cosmetic card called “Hào Quang Sinh Mệnh”, illustrated as a gentle jade ring and a few floating motes around the lower trunk. Price: “20 Coin”. Show a small “Xem thử” control and one primary “Mua” button; include a separate small state-design strip illustrating how that button becomes “Trang bị” or “Đang trang bị”. These state samples belong outside the actual playable screen area. Cosmetic is purely visual: no HP, production or protection bonus, no flames and no paid currency. Use clear hierarchy and touch-friendly controls. Do not invent additional shop inventory. Landscape 16:9.
```

## P04 — Achievement/Coin

```text
Create a clean achievement panel UI concept for a cozy fantasy idle tree game. Ivory surface, forest-green text, jade actions, subtle gold reward accents. Title “Thành tựu”, Coin balance in the header, large close button. Exactly six readable rows: “Nâng cấp Rễ lần đầu”, “Nâng cấp Lá lần đầu”, “Tiến hóa Cây Non”, “Tiến hóa Cây Lớn”, “Tái sinh lần đầu”, “Nhận thưởng Sức sống”. Each row has a distinct simple emblem, a short progress indicator and a reward of “10 Coin”. Show three sample states across the rows: an incomplete row with a muted “Chưa đạt”, a completed row with “Nhận”, and a claimed row with “Đã nhận”. Do not rely on color alone to communicate state. No daily quests, streak calendar, battle pass or achievements beyond these six. Large legible Vietnamese labels and clear vertical spacing. Landscape 16:9.
```

## P05 — Settings

```text
Create a centered settings modal over a softly dimmed tree gameplay scene, cozy nature fantasy UI, ivory card, forest-green text, jade controls and restrained wood accents. Heading “Cài đặt”. Exactly two setting rows: “Nhạc nền” with a large on/off toggle, and “Khóa xoay camera” with another on/off toggle. A small noninteractive note “Zoom: Sắp có”. Below: primary button “Tiếp tục” and secondary button “Thoát game”. Clear keyboard and touch readability, no tiny controls, no overlap with background HUD. Do not add account, language selector, cloud save, sound volume sliders, graphics settings or features not shown here. Landscape 16:9.
```

## P06 — Xác nhận Rebirth tự nguyện

```text
Design a clear voluntary rebirth confirmation modal for a cozy fantasy idle tree game. Dim the background, show the large tree faintly behind an ivory modal with jade and muted gold accents. Title “Tái sinh”. Show a simple visual sequence from Cây Lớn back to a two-leaf Mầm, and one distinct large-tree point emblem with “+1 điểm Cây Lớn”. Use two balanced information groups: “Đặt lại” lists stage, Water, Growth, upgrade levels and HP/status; “Giữ lại” lists Coin, points by stage, achievements, cosmetic, settings, tutorial and the vitality time bank. Two clear buttons: secondary “Hủy”, primary “Xác nhận tái sinh”. No Coin penalty, no sum of points from lower stages, no payment to continue. Keep the text brief and readable, no skull decoration or sinister atmosphere. Landscape 16:9.
```

## P07 — DEAD/Forced Rebirth

```text
Create a mandatory rebirth modal for the DEAD state of a stylized low-poly tree game. Use the same ivory and forest-green UI system, with a restrained amber warning border. The background tree is visibly dry and motionless, the background HUD is dimmed and inactive, HP shows 0. Title “Cây đã chết”. A short explanation says “Tái sinh để bắt đầu vòng đời mới”. Show a Cây Lớn point emblem with “+1 điểm Cây Lớn” and a seedling preview. Include only one prominent action: “Xác nhận tái sinh”. There is no cancel button, close X, paid revive, advertisement revive or countdown. Clarify visually that Coin and permanent progress are retained. Calm and understandable rather than horror-themed. Landscape 16:9.
```

## P08 — Thông báo offline

```text
Design an informational offline reward modal for a cozy fantasy idle tree game. Ivory card, jade leaf motif, forest-green text, blue Water icon and green Growth icon. Heading “Chào mừng trở lại”. Show “Vắng mặt: 1 giờ”, “Hiệu suất ngoại tuyến: 50%”, “Water +1.350”, “Growth +1.800”. A small footnote says “Tối đa 24 giờ”. The rewards have already been applied, so the only button is “Tiếp tục”, not “Nhận thưởng”. No double-reward button, no ad multiplier, no vitality bonus, no storm damage simulation or automatic evolution. Show a relaxed tree scene dimmed behind the modal. Clear hierarchy with generous whitespace. Landscape 16:9.
```

## P09 — Tutorial và toast không che UI

```text
Create a landscape gameplay tutorial overlay concept for a cozy low-poly tree game. Highlight exactly one target: the Rễ purchase button inside the Upgrade panel. Dim other areas gently. Place a compact ivory callout immediately beside the target, outside the tree silhouette and outside all other buttons. Vietnamese instruction: “Nâng cấp Rễ để tạo Water”. Use a thin jade pointer and a clear step indicator. Reserve a separate narrow toast lane below the top HUD with the sample message “Đã nâng cấp Rễ”. The toast must not cover panel headings or buttons. Keep tutorial, toast and vitality help in separate readable regions. Do not show several tutorial steps at once, obstruct the highlighted button or introduce new gameplay. Landscape 16:9.
```

## P10 — Asset panel rời để dựng UI

```text
Create one standalone blank UI panel background asset for a cozy nature fantasy game. A clean front-facing rounded rectangle, warm ivory fill, thin dark forest-green outline, tiny restrained leaf accents confined to the four corners, subtle muted gold inner border. The middle 80 percent is completely plain for dynamic text and controls. All edges are straight-on with no perspective distortion. Shading is contained within the silhouette, no large external drop shadow. Transparent background with real alpha if supported, no drawn checkerboard. No text, icons, numbers, buttons, scene or watermark. Wide 3:2 canvas, panel fully inside the image with padding. Designed as a source image for slicing into a scalable UI panel; no claim that it is already a configured nine-slice sprite.
```

## P11 — Asset button rời và trạng thái

Tạo từng trạng thái riêng; thay `[STATE]` bằng `normal`, `pressed`, `disabled` hoặc `selected`. Kích thước và biên phải giống nhau; không yêu cầu hover để dùng chức năng thiết yếu.

```text
Create one standalone blank rectangular UI button asset, state: [STATE], for a cozy nature fantasy game. Rounded corners, broad flat interior for a dynamic label, restrained leaf ornament at the ends, forest-green outline, jade fill. Normal: calm jade. Pressed: darker jade with reduced inner highlight. Disabled: muted neutral fill and weaker outline, still recognizable. Selected: thin muted-gold inner outline without changing the silhouette. Render only the requested state. Front view, no perspective, identical outline geometry and padding across states. Transparent background with real alpha if supported; otherwise a plain contrasting background. No text, icons, scene or outer glow. Landscape 3:1 canvas, fully contained shape. This is a button surface asset, not a complete UI screenshot.
```

## P12 — Icon đơn: tài nguyên/chức năng/thời tiết

Mỗi lượt thay `[SYMBOL]` bằng một mục trong bảng. Tạo riêng từng icon để giảm lỗi sheet và dễ xuất PNG.

| Tên file gợi ý | Nội dung thay cho SYMBOL |
| --- | --- |
| icon_water | a single blue water droplet |
| icon_growth | one green leaf with a small upward growth arrow |
| icon_hp | a small healthy tree trunk paired with a heart |
| icon_vitality | one ivory clock face surrounded by two jade leaves, representing stored buff time |
| icon_coin | one muted-gold coin embossed with a leaf |
| icon_root | a compact branching brown root with one blue droplet |
| icon_leaves | two green leaves with a small sun |
| icon_trunk | a short brown tree trunk paired with a protective shield |
| icon_evolution | a small sprout transforming into a fuller tree with one upward arrow |
| icon_rebirth | one circular arrow surrounding a two-leaf sprout |
| icon_shop | a small wooden display tray holding a jade aura ring |
| icon_achievement | a gold leaf-shaped medal |
| icon_settings | a simple rounded dark-green cog |
| icon_orbit | a circular orbit arrow around a tiny tree |
| icon_music | one musical note with a tiny leaf accent |
| icon_calm | two soft pale clouds with a small neutral sky gap |
| icon_sunny | a warm gold sun |
| icon_rainy | a soft blue cloud with three blue raindrops |
| icon_thunderstorm | a slate-blue cloud with one amber lightning bolt |
| icon_burning | a small orange flame, clearly a warning rather than a green cosmetic |
| point_seedling | a two-leaf sprout inside a simple round permanent-point emblem |
| point_young_tree | a small young tree inside the same permanent-point emblem frame |
| point_large_tree | a layered-canopy tree inside the same permanent-point emblem frame |

```text
Create a single isolated game UI icon depicting [SYMBOL]. Cozy nature fantasy, stylized simple forms, jade green, warm brown, ivory, muted gold and clear Water blue when applicable. One dominant silhouette, minimal interior detail, strong contrast and consistent medium outline thickness. It must remain recognizable when reduced to 48 pixels. Center it in a square canvas with 15 percent padding. Front view or very shallow depth, no dramatic perspective. Transparent background with real alpha if supported. No text, letters, numbers, extra symbols, enclosing button, scenery or watermark. Do not merge this icon with other UI controls. Square 1:1.
```

## P13 — Sáu badge Achievement

```text
Create a visual design sheet with exactly six separate achievement badges in a 3 by 2 grid for one cozy nature fantasy game. All badges share a simple muted-gold rounded medal frame, jade accents, readable medium outlines and ivory highlights. The six interior symbols in reading order are: first root upgrade, first leaf upgrade, evolution to a young tree, evolution to a large layered-canopy tree, rebirth represented by a circular arrow around a sprout, and first vitality reward represented by a leaf-wrapped clock. Equal sizes, generous gaps, no overlapping emblems. No text, numbers, stars indicating rarity, loot boxes or extra achievements. Plain ivory background. This is a design reference sheet; keep each badge simple enough to regenerate as an individual square icon.
```

## P14 — Mầm / seedling

```text
Create a 3D modeling reference sheet of one original low-poly stylized seedling for a cozy nature fantasy idle game. A very short warm-brown stem with exactly two broad jade-green cotyledon leaves on opposite sides, slightly curved and formed from a small number of broad facets. Tiny visible root tips at the stem base. Simple matte material colors, no leaf veins, no bark texture, no flowers, fruit, face or magic particles. The silhouette is clear and compact. The entire tree is above a consistent ground baseline; roots remain visible. Show front, right side and back orthographic views, plus one larger three-quarter perspective view. Every view depicts the exact same geometry, leaf count and leaf attachment heights. Neutral light-gray background, even studio light, no depth of field, no cast shadows hiding the roots. No soil platform, labels, dimension text, UI or watermark. The source concept must be practical to build in Blender with simple meshes. Landscape 16:9.
```

## P15 — Cây Non / young_tree

Đính kèm ảnh Mầm đã chọn để giữ cùng loài cây.

```text
Using the attached seedling reference as the species and material guide, create its young-tree stage as a low-poly 3D modeling reference sheet. Preserve jade-green foliage, warm-brown bark, broad faceted surfaces and the same friendly nature-fantasy shape language. A slim upright trunk, three readable short branches, an open canopy made from five solid foliage clusters, and a small exposed root flare. The trunk and main branching must remain visible between foliage clusters. No individual thin leaves, dense foliage cards, flowers, fruit, face, giant roots or built-in glow. Show front, right side and back orthographic views, plus one three-quarter perspective view. Match branch topology, canopy cluster count and root placement across all views. Ground baseline aligned, full tree visible, neutral gray background and flat studio light. Do not include soil, platform, UI, labels, measurement text or watermark. Landscape 16:9.
```

## P16 — Cây Lớn / large_tree

Đính kèm ảnh Cây Non đã chọn.

```text
Using the attached young-tree reference as the species and style guide, create its large-tree stage for the same low-poly idle game. The tree is mature within this three-stage prototype: a sturdy warm-brown trunk, four main readable branches, a broad canopy of seven solid jade foliage clusters arranged across three staggered tiers, and five substantial root flares resting at ground level. Broad faceted matte surfaces, simplified branching, some open gaps so the trunk can be seen and tapped. Make it recognizably larger and fuller than the young tree, while keeping the same species. No colossal world tree, hanging villages, detailed bark, millions of leaves, face, vines, flowers, fruit or magic effects. Show consistent front, right side and back orthographic views and one three-quarter view. Same topology in every view, aligned ground baseline, no hidden roots, neutral gray background, even studio lighting, no soil platform, UI, labels or watermark. Landscape 16:9.
```

## P17 — Một góc riêng để chuyển ảnh sang 3D

Đính kèm một sheet cây đã chọn; thay `[STAGE]` bằng seedling/young tree/large tree. Nếu dùng công cụ ảnh-sang-3D, dùng ảnh đơn này thay cho sheet nhiều cây.

```text
Use the attached reference sheet to render only the [STAGE] tree as one isolated modeling reference image. Preserve the approved shape, branch layout, root structure, foliage cluster count and materials exactly. Three-quarter view at tree mid-height, mild perspective, entire tree and root base visible, centered with generous margin. Neutral light-gray background, even illumination, no strong cast shadow or camera blur. Solid foliage masses, clear gaps between branches, no added details. No multiple views, duplicate trees, platform, scene, UI, labels or watermark. The image is intended as a single-object reference for later mesh reconstruction; do not pretend the image itself is a 3D asset. Square 1:1.
```

## P18 — Bệ đất cho scene

```text
Create a 3D modeling reference sheet for one circular soil platform used under a single low-poly tree in a cozy nature fantasy idle game. A shallow disk of warm-brown earth with a gently irregular edge, a thin jade-green grass rim and three very small stones placed away from the center. The center is flat and clear for the tree pivot and exposed roots. Broad simple facets, plain matte colors, no fine textures. The platform rests on the ground, it is not a flying island and has no floating rocks or dangling roots. Show consistent top, side and three-quarter views, neutral gray background, soft even lighting. No tree, flowers, buildings, labels, UI or watermark. Compact and practical to model with a few simple meshes. Landscape 16:9.
```

## P19 — Scene tổng thể không UI

```text
Create a concept render of one playable scene for a cozy low-poly nature fantasy idle tree game. A single young tree with jade foliage and brown bark stands on one modest circular soil platform. The camera sees the tree from a three-quarter view with shallow downward pitch; the base, trunk and canopy are fully visible. Soft pale-sage atmospheric background with very faint distant silhouettes, not a detailed forest level. Gentle warm light and restrained shadows. Keep the center visually clean and the outer screen regions quiet for an eventual UI overlay. No existing UI, text, characters, houses, paths, portals, animals or additional interactive objects. Simple forms and matte materials suitable as a Blender-to-Unity concept. Landscape 16:9.
```

## P20 — Bốn trạng thái thời tiết cùng scene

Tạo riêng từng ảnh, đính kèm P19 và thay `[WEATHER]`. Đây là keyframe tham chiếu, không thay geometry hoặc camera giữa các ảnh.

| WEATHER | Chỉ dẫn thêm |
| --- | --- |
| calm | soft neutral daylight, pale-sage sky, no environmental buff cue |
| sunny | warm golden daylight and subtle sun rays, clearly readable green foliage |
| rainy | gentle blue-gray daylight, fine rain and soft clouds, no lightning |
| thunderstorm | slate-blue clouds, restrained rain and one distant lightning flash that does not hit the tree; no fire |

```text
Edit the attached approved tree scene to show [WEATHER]. Preserve exactly the tree geometry, platform, camera position, framing, scale and object placements. Change only sky atmosphere, light color and the requested weather appearance. Keep the trunk and canopy readable and leave quiet space at screen edges for UI. Cozy stylized low-poly nature fantasy, no photorealism or excessive bloom. For thunderstorm, show storm weather but no impact strike and no burning: a storm does not guarantee that the tree is hit. No HUD, text, people or new objects. Landscape 16:9.
```

## P21 — Cosmetic Hào Quang Sinh Mệnh

```text
Create a visual effect concept for “Hào Quang Sinh Mệnh”, a purely cosmetic jade aura on a stylized low-poly tree. One slim soft jade ring wraps around the lower-middle trunk, with a few small upward-drifting leaf-shaped motes and a restrained translucent glow. Keep the tree trunk, roots and canopy clearly visible. The effect feels peaceful and decorative, not a shield, health bar, production buff or fire. No orange flames, lightning, giant magic circle, dense particles, text or UI. Show the effect around one young tree from a three-quarter angle on a neutral background. A visual reference for a separate Unity effect; the glow should not be permanently baked into the tree mesh or material. Square 1:1.
```

## P22 — Tap/Sức sống

```text
Create a restrained visual effect concept for tapping a tree in a cozy low-poly fantasy game. One small ivory-jade ripple at the tap point on the trunk and three tiny leaf-shaped particles drifting upward. Clearly visible but brief-looking, local to the tree, never covering the whole screen. The effect represents an accepted tap toward a vitality time reward; it does not visually add Water, Coin or HP. Keep the tree silhouette readable. No floating numbers, text, explosive flash, dense glow, weapon impact or extra UI. Neutral background, one tree, square 1:1. This is a static keyframe reference for later particle implementation.
```

## P23 — Evolution

```text
Create a three-keyframe visual storyboard for a tree evolution effect in a cozy low-poly fantasy idle game. The sequence is the same young tree becoming its larger layered-canopy stage. First keyframe: subtle jade motes gathering around the canopy. Middle: a narrow ivory-green ascending ring while the base remains clearly visible. Final: the approved large-tree silhouette with a small fading leaf burst. Use warm bark, jade foliage, matte low-poly shapes, restrained bloom. Keep the ground pivot and scene camera consistent; no teleport to another map. No rewards, coins, text, enormous explosion, portal or permanent aura. Arrange exactly three evenly spaced panels on a neutral background. This is an animation concept storyboard, not a sprite flipbook or final geometry specification. Landscape 16:9.
```

## P24 — Rebirth

```text
Create a three-keyframe visual storyboard for a rebirth effect in a cozy low-poly idle tree game. One large tree gently dissolves into a few muted-gold and jade leaf motes, then a small two-leaf seedling appears at exactly the same ground pivot. Gentle circular motion rather than a blast. Keep the same soil platform and consistent camera composition across all three panels. Distinguish rebirth from evolution through muted-gold circular motion instead of green upward growth. No extra currency icons, lower-stage point rewards, resurrection of the same large tree, portals, characters, flames or text. Exactly three panels, neutral background, clear silhouettes. Landscape 16:9. Static animation reference only.
```

## P25 — Sét trúng cây

```text
Create one readable static keyframe of a lightning impact effect on a low-poly jade-green tree during a thunderstorm. A single thin stylized amber-white bolt ends precisely on one branch near the outer canopy, with a tiny local spark at the contact point. The tree is still readable under slate-blue storm lighting. This depicts an actual strike, separate from the general thunderstorm weather concept. No blanket lightning across all branches, no giant explosion, no preexisting cosmetic aura, no text, damage number or UI. Keep the ground platform and camera simple. The bolt, contact flash and tree remain visually separable as implementation elements. Landscape 16:9.
```

## P26 — Burning theo vùng

Tạo ba ảnh riêng, thay `[REGION]` bằng `exposed root flare at ground level`, `one foliage cluster near a branch tip` hoặc `lower trunk`.

```text
Create a visual effect reference for localized burning on a stylized low-poly fantasy tree. The affected region is [REGION]. Small orange-red flames, a few amber embers and very thin gray smoke stay close to that region. The tree remains intact and recognizable; unburned regions remain unaffected. Jade foliage and brown bark keep the approved material style. This is a harmful status, visually distinct from the peaceful jade cosmetic aura. No green fire, giant inferno, destroyed island, gore, numbers, text or HUD. Clear three-quarter view, neutral background, square 1:1. The tree is a context reference; fire and smoke will be separate effects rather than baked model decoration.
```

## P27 — Cây DEAD

```text
Using the attached approved large-tree reference, create a visual state reference of the same tree after its health reaches zero. Preserve exactly its size, trunk structure, branch topology, root positions and ground pivot. Muted brown foliage, slightly desaturated bark and a still, subdued appearance. Keep the major foliage masses so the silhouette remains readable and geometry does not unexpectedly disappear. No ongoing fire, broken platform, falling tree, skeleton face, skulls or horror details. Neutral background, even light, three-quarter view, no UI, text or watermark. This is a presentation-state proposal for later evaluation, not permission to change gameplay or save data. Square 1:1.
```

## P28 — Sprite hạt rời cho VFX

Tạo từng ảnh riêng. Thay `[PARTICLE]` bằng `one simple jade leaf`, `one soft ivory four-point sparkle`, `one tiny amber ember` hoặc `one soft gray smoke puff`.

```text
Create one isolated particle texture source depicting [PARTICLE] for a cozy stylized nature game. Centered square image, simple readable silhouette, soft antialiased edges, generous transparent margin, no perspective scene, no cast shadow, no text. Transparent background with real alpha if supported. Leaf and ember have mostly solid interiors; sparkle and smoke may have soft translucent edges. No additional particles, no sheet, no tree, no baked rectangular background. This is an individual source sprite, not an animated effect or completed particle prefab. Square 1:1.
```

## P29 — Chuẩn hóa một ảnh UI đã chọn

Đính kèm ảnh HUD concept muốn sửa.

```text
Refine the attached tree-game UI image while preserving its approved nature-fantasy palette, icons, tree style and existing feature set. Improve spacing and hierarchy. Keep a 5 percent safe margin, reserve one notification lane below the top HUD, and place every open panel fully below the top buttons. Tutorial callouts must sit beside their target rather than on top of controls or bottom help text. Increase contrast of body text and distinguish HP from the vitality time bank. Keep the central tree silhouette tappable and visible. Use only Water, Growth, HP, Coin and vitality; do not invent menus, features or currencies. Preserve landscape 16:9. Output one clean revised screen, not before-and-after panels.
```

## P30 — Chỉnh cây để đồng nhất các góc

Đính kèm sheet cần chỉnh cùng ảnh góc ba phần tư được chọn làm chuẩn.

```text
Correct the attached tree turnaround sheet using the attached approved three-quarter tree as the geometry authority. Preserve the chosen trunk curvature, exact main-branch count, branch attachment heights, root flare count and foliage cluster count. Reconstruct front, right side and back orthographic views of that same tree; do not design new trees for each view. Align all ground baselines and use the same object height across orthographic panels. Plain gray background, even light, no cast shadows obscuring geometry. Keep broad low-poly matte surfaces and the approved jade/brown colors. No labels, UI, flowers, extra branches or decorative effects. Landscape 16:9. This is a reference-consistency pass, not a guarantee of physically valid 3D reconstruction.
```

## Câu bổ sung giữ phong cách

Chỉ thêm khi đã đính kèm ảnh mẫu:

```text
Use the attached approved style image as the visual authority. Match its palette, broad faceted forms, material softness, ornament density, icon outline thickness and UI corner shapes. Change only the subject or screen requested in this prompt. Do not redesign the visual identity.
```

## Câu sửa kết quả quá rối

```text
Simplify the attached result. Remove small decorative details, reduce glow and particle density, enlarge the main readable shapes, restore quiet negative space and keep only the requested game elements. Preserve the approved palette and subject identity. No new features, currencies or additional scenery.
```

## Brief bàn giao cho bước dựng 3D sau

Phần này dành cho Blender/Unity, không phải điều mà prompt ảnh tự tạo được:

- Giữ ID stage `seedling`, `young_tree`, `large_tree` và cosmetic `glow_placeholder`.
- Dựng model bằng khối tán đặc, thân/nhánh đơn giản; ưu tiên vật liệu phẳng trước texture hoặc shader phức tạp.
- Bệ đất là model riêng, không gộp vào cả ba cây; cây thay stage tại cùng gốc mặt đất.
- Xác định kích thước theo mét khi dựng, apply transforms, pivot ở gốc thân; kiểm tra Unity scale=1 và hướng sau import. Không coi hình orthographic là bằng chứng scale/rotation đúng.
- T10 cần anchors `Anchor_LightningTarget`, `Anchor_LeavesCenter`, `Anchor_TrunkAura`. Chúng là Transform trong prefab, không phải chữ hoặc mesh trang trí sinh trong ảnh. Giữ tương thích các anchor Root/Leaves/Trunk đang có trong view cho Burning.
- Kiểm tra raycast/collider/camera của từng stage. Tán cây và gốc dễ nhìn trong ảnh không đảm bảo collider tự đúng.
- UI mockup là mẫu thiết kế; panel, label, cost, state, safe area và input phải tiếp tục do hệ thống UI điều khiển.
- Kiểm tra ảnh rời trước khi dùng: alpha thật, không watermark/chữ thừa, không cắt mất viền, cùng silhouette giữa các state. Ghi nguồn công cụ/ngày/prompt và kiểm tra điều khoản sử dụng của công cụ ở bước chọn asset.

## Thứ tự tạo để có một bộ đồng nhất

1. P00 phong cách; P14–P16 chọn ba cây cùng loài.
2. P18 bệ đất, P19 scene, P01 HUD để chốt tổng thể.
3. P02–P09 các panel theo cùng hệ UI; P29 sửa chồng lớp nếu có.
4. P10–P13 asset UI/icon/badge rời sau khi chọn mockup.
5. P17/P30 ảnh model rõ và nhất quán để dựng mesh.
6. P20–P28 thời tiết và VFX; tạo riêng từng trạng thái/vùng/sprite.

T10 vẫn cần dựng model và nghiệm thu Unity sau khi chọn concept. Các prompt mở rộng UI/VFX ở đây phục vụ lựa chọn hình ảnh theo yêu cầu, không tự bắt đầu triển khai toàn bộ backlog.
