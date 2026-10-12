# Cây Thế Giới — prompt theo ảnh bonsai của người dùng

Ngày 04/10/2026. Hướng mỹ thuật mới theo yêu cầu trực tiếp của người dùng, thay đề xuất cây có tán khối tròn low-poly trong bộ prompt trước. Tài liệu này tạo concept và ảnh tham khảo, chưa thực hiện dựng model hoặc sửa Unity.

Ảnh gốc do người dùng cung cấp: [world_tree_bonsai_user_20261004.png](../../ArtSource/References/world_tree_bonsai_user_20261004.png). Không xác định loài thực vật hoặc tác giả từ ảnh. Những prompt dưới đây dùng dáng bonsai/tùng làm cảm hứng cho một loài cây hư cấu.

## Những đặc trưng cần giữ

- Thân già dày, uốn và nghiêng tự nhiên; vỏ nâu xám có rãnh, tránh thân thẳng hình trụ.
- Rễ nổi bám mặt đất, tỏa ra và đan xen quanh chân cây.
- Cành lớn uốn ngang, phân nhánh nhỏ dần; nhiều khoảng trống để nhìn rõ cấu trúc.
- Tán rộng hơn chiều cao, bất đối xứng cân bằng, xếp thành các lớp ngang như những đám mây lá; không đổi thành cây có tán cầu.
- Lá nhỏ, cụm lá gọn, xanh tự nhiên hơi trầm. Cành/rễ nhìn được khi orbit.
- Ảnh có kiến trúc và sân triển lãm phía sau: chỉ lấy cây làm tham chiếu. Không mang tòa nhà, biển chữ, người, xe hay chậu trưng bày vào concept game.
- Cây Lớn của prototype lấy dáng cổ thụ từ ảnh. “Cây Thế Giới” là tên game; không tự thêm stage mới hay biến stage cuối prototype thành cây khổng lồ có thành phố.

## Hai cách thể hiện để so sánh

**A — Chân thực:** tỷ lệ tự nhiên, vỏ sần và lá nhỏ có chiều sâu, ánh sáng và chất liệu tự nhiên. Concept có thể chi tiết; model game được dựng có chọn lọc và kiểm tra hiệu năng sau.

**B — Anime bán chân thực:** giữ cấu trúc cây và chi tiết chính, mảng sáng tối rõ, texture vẽ dịu, bóng mềm kiểu anime. Đây không phải chibi, tán cầu, lá phát sáng hoặc cây có khuôn mặt.

Người dùng chấp nhận hướng chân thực hoặc anime, chưa chọn một hướng độc quyền. Tạo cùng bố cục A/B để chọn. Không coi phong cách anime đồng nghĩa thêm nhân vật anime vào scene.

## Cách dùng

1. Đính kèm ảnh bonsai gốc khi dùng B01/B02. Tạo từng ảnh riêng cùng tỷ lệ 16:9 để so sánh.
2. Chọn một ảnh làm chuẩn; các prompt model/UI/scene tiếp theo đính kèm ảnh chuẩn ấy. Không trộn rendering chân thực và anime giữa từng asset nếu chưa chủ động muốn kết hợp.
3. Chạy từng prompt một. Với B04–B16, thay `[RENDER_STYLE]` bằng một đoạn dưới đây.
4. Ảnh model là tham khảo 2D, không tạo file Blender/FBX chỉ bằng prompt. Đối chiếu góc nhìn và sửa geometry khi dựng.
5. UI cần dựng chữ/số/nút bằng Unity. Với sprite rời, xác minh alpha thật, không coi nền bàn cờ vẽ là transparency.

Đoạn A:

```text
Naturalistic realistic game-art rendering: believable bark roughness, fine dark-green foliage, natural proportions, physically plausible soft lighting, restrained contrast, no plastic surfaces or exaggerated fantasy glow.
```

Đoạn B:

```text
Semi-realistic anime environment rendering: believable bonsai anatomy, softly painted bark detail, layered foliage with fine leafy edges, clear light-and-shadow shapes, subtle cel-shaded accents and gentle atmospheric color. Mature and serene, not chibi or cartoon toy art.
```

## B01 — Cây Lớn chân thực theo ảnh gốc

```text
Use the attached bonsai photograph as the structural reference for the main tree in an original game called “Cây Thế Giới”. Create a naturalistic realistic concept of this ancient bonsai-inspired tree. Preserve the thick twisting gray-brown trunk, exposed intertwining root flare, long sinuous horizontal branches, broad asymmetrical silhouette, and many flattened layered foliage pads separated by visible gaps. The crown is wider than the tree is tall. Small dark-green conifer-like foliage, deeply fissured bark, believable branch taper and gravity. A calm, venerable tree, with its trunk and roots clearly readable.

Remove the entire exhibition setting from the reference: no building, people, signs, cars, planter or text. Place the tree on a modest natural soil mound against a quiet pale background. Keep the whole tree inside the frame with generous margin, roots visible, three-quarter view, soft daylight and sharp structural detail. No spherical cartoon canopy, low-poly facets, straight cylindrical trunk, enormous fantasy scale, glowing leaves, faces or watermark. Landscape 16:9. This is a visual concept image, not a finished 3D mesh.
```

## B02 — Cùng cây theo anime bán chân thực

```text
Use the attached bonsai photograph as the structural reference for the main tree in “Cây Thế Giới”. Render the tree as a semi-realistic anime environment asset. Keep the thick twisting gray-brown trunk, exposed interwoven roots, graceful horizontal branches and wide asymmetrical crown made of flattened layered foliage pads. Maintain the reference tree’s anatomy and negative spaces; the crown is wider than its height.

Use softly painted fissured bark, olive-to-forest-green foliage, delicate leafy edges, clear controlled light-and-shadow shapes, subtle cel-shaded accents and warm gentle daylight. The tree feels ancient, serene and alive, with believable branching rather than a toy-like design.

Replace the exhibition background with a quiet pale-sage atmosphere and a modest natural soil mound. Full tree visible, roots visible, three-quarter view, generous margins. No people, anime characters, building, planter, signage, chibi proportions, spherical leaf blobs, low-poly faceting, neon colors, glowing leaves or watermark. Landscape 16:9. Static concept art only.
```

## B03 — So sánh hai phong cách từ cùng một concept

Đính kèm B01 hoặc B02 đã chọn làm chuẩn hình dáng; tạo hai lượt, chỉ thay phần style.

```text
Re-render the attached approved bonsai tree while preserving exactly its camera framing, silhouette, trunk curvature, branch positions, foliage pad distribution, root flare and soil mound. Change only the rendering style to [RENDER_STYLE]. Keep tree scale and lighting direction consistent so the image can be compared fairly with the other style version. No added branches, flowers, structures, characters, effects, text or UI. Landscape 16:9, one tree only.
```

## B04 — Mầm cùng loài cây

```text
Create the seedling stage of the fictional bonsai-inspired tree species shown in the attached approved mature-tree concept. A very short young brown stem, two small slender juvenile leaves opening to opposite sides, a slight natural bend and tiny visible roots at the base. Young healthy growth, not a miniature old tree: no ancient cracked trunk, large root knots or full mature canopy. Use natural subdued greens and brown, keeping the species family visually compatible with the later conifer-like tree.

[RENDER_STYLE]

Show one isolated seedling in a clear three-quarter view, fully visible against neutral light gray, even studio lighting and generous margin. No soil platform, pot, buildings, face, magic glow, UI, text or watermark. Square 1:1. This is a modeling reference.
```

## B05 — Cây Non cùng loài cây

```text
Create the young-tree stage of the fictional bonsai-inspired tree shown in the attached mature-tree concept. A slimmer slightly twisting brown trunk, a few graceful horizontal branches, three to five modest flattened foliage pads and a small exposed root flare. Early formation of the same wide asymmetric layered canopy, with open spaces between foliage pads. The tree has less bark cracking and simpler branching than the mature reference. Preserve dark natural greens and believable tapered branches.

[RENDER_STYLE]

One tree in a full three-quarter view on a plain gray background, roots visible, even light and generous margin. No spherical leaf blobs, dense rounded umbrella canopy, overly straight trunk, decorative pot, large scenery, face, UI or watermark. Square 1:1. An image reference for later 3D modeling.
```

## B06 — Cây Lớn tách nền để tham khảo dựng mesh

```text
Reproduce the attached approved mature bonsai-inspired tree as a single isolated 3D modeling reference. Preserve its thick curving trunk, exposed intertwined root flare, horizontal tapered branches, broad flattened tiers of fine foliage and asymmetric crown. Show natural empty gaps between branches and canopy layers. The trunk and major roots must remain clearly visible.

[RENDER_STYLE]

Neutral light-gray background, soft even illumination without dramatic shadows or depth of field. Three-quarter camera at mid-trunk height, entire tree visible with generous margin. Tree only: no soil mound, decorative pot, platform, extra trees, built-in VFX, UI, labels or watermark. Square 1:1. Do not change the approved tree proportions to fill the canvas.
```

## B07 — Turnaround nhiều góc cho Blender

Đính kèm một ảnh cây đã chọn, không yêu cầu công cụ tự đoán mặt sau là chính xác.

```text
Create a modeling turnaround reference sheet of the exact bonsai-inspired tree in the attached approved image. Four panels: front orthographic, right orthographic, back orthographic and a three-quarter perspective view. Preserve the same trunk bend, branch hierarchy, branch attachment heights, root positions and layered foliage silhouette in all panels. Use plausible inferred geometry where the original image hides the back; do not claim it is an exact scan. Align the ground baseline and use the same object height in the three orthographic panels.

[RENDER_STYLE]

Neutral gray background, even studio lighting, no dramatic shadow, perspective distortion in the orthographic panels, labels, dimensions, text, soil platform or extra scenery. Keep branch gaps visible. Landscape 16:9. This is an approximate reconstruction guide that requires consistency checks before modeling.
```

## B08 — Ba stage trong cùng một sheet

Đính kèm các ảnh Mầm/Cây Non/Cây Lớn đã chọn.

```text
Create a clean progression concept sheet for the same fictional bonsai-inspired tree species using the attached approved stage images. Show exactly three trees from left to right: seedling with two juvenile leaves, young tree with a slim curved trunk and a few horizontal foliage tiers, and mature tree with thick fissured bark, exposed interwoven roots and a broad layered canopy. The mature silhouette follows the original bonsai reference. Keep leaf color, bark family, branching language and rendering consistent across all three.

[RENDER_STYLE]

One shared ground baseline, equal soft studio lighting and plain gray background. Show growth through increased trunk thickness, root spread and branching, not different species. No fourth stage, giant world tree, houses, pot, UI, text or watermark. Landscape 16:9. Relative sizes are visual proposals, not finalized Unity meter dimensions.
```

## B09 — Scene chính theo cây mới

```text
Create one gameplay scene concept for “Cây Thế Giới” using the attached approved bonsai-inspired tree as the visual authority. A single ancient curved-trunk tree with exposed roots and broad horizontal foliage tiers stands on a low natural soil platform edged with a little moss and a few small stones. Quiet pale-sage atmosphere, minimal distant soft silhouettes, gentle daylight. Keep the tree central, with its whole canopy and roots inside the frame. Maintain generous empty regions around the edges for a landscape HUD.

[RENDER_STYLE]

The setting is serene and modest, allowing a camera to orbit the tree. No exhibition building, signs, decorative bonsai pot, shrine, floating island, characters, houses or additional gameplay objects. No giant scale, neon glow, text, UI or watermark. Landscape 16:9.
```

## B10 — HUD phù hợp chân thực/anime

```text
Design a landscape gameplay UI mockup for “Cây Thế Giới”, using the attached approved bonsai-inspired tree concept as the scene centerpiece. The tree has a thick curved trunk, visible roots and broad layered horizontal foliage; keep its silhouette and tap area clear.

Use restrained botanical-fantasy UI: warm parchment or softly translucent ivory panels, dark forest-green text, muted bronze outlines, subtle bark and branch motifs confined to corners. Clean flat controls with clear typography rather than cartoon leaf buttons or heavy carved-wood frames. UI remains crisp and readable regardless of the scene rendering style.

Top left: “Cài đặt”, “Thành tựu” and compact Water/Growth/HP/Coin information. Top center: stage “Cây Lớn” and “Tái sinh”. Top right: current weather icon and timer. Bottom left: “Cửa hàng”. Bottom right: “Nâng cấp”. Bottom center: a separate “Sức sống” time bank showing “20s / 3600s”, “0 / 50” taps and “+5%”. HP is distinct from vitality. Reserve one notification lane below the top HUD; keep every panel below top buttons. Large touch-friendly controls, 5 percent screen-edge safe margin.

Scene rendering: [RENDER_STYLE]

No extra currencies, ads, characters, overlapping toast, phone frame or new game features. Landscape 16:9. This is a layout concept; dynamic UI will be implemented separately.
```

## B11 — Chỉnh các panel cũ theo phong cách mới

Đính kèm mockup panel cũ và ảnh cây/style mới. Prompt này giữ tính năng đã chốt trong P02–P09 của bộ cũ.

```text
Restyle the attached game UI mockup to match the attached approved realistic or semi-realistic anime bonsai tree concept. Preserve every existing label, number, action, game rule and control state. Change the visual treatment to restrained botanical fantasy: parchment or translucent ivory surfaces, dark forest-green type, muted bronze borders and fine branch motifs at the corners. Remove toy-like shapes, exaggerated green bevels, bulky wood framing and overly cute decorations. Keep layout flat, text contrast strong and touch controls spacious. Place the panel below persistent top buttons, and keep tutorial/toast lanes separate.

Use the bonsai tree reference in background previews, retaining its curved trunk, exposed roots and horizontal layered canopy. Match the chosen scene rendering. No added features, currencies, revive ads, heroes or inventory. Landscape 16:9. Output one revised screen.
```

## B12 — UI panel asset rời theo hướng mới

```text
Create a single blank UI panel background asset for a refined botanical-fantasy game with realistic or anime bonsai scenery. Warm ivory-parchment surface, dark forest-green thin outline, subtle muted-bronze inner border, small elegant branching motifs restricted to the corners. A very large plain central area for dynamic labels and controls. Straight-on flat view, softly rounded corners, no perspective or heavy bevel. No cartoon leaf faces, bulky carved wood, glowing neon or elaborate filigree.

Transparent background with real alpha if supported, no drawn checkerboard. No text, numbers, buttons, scene or watermark. Fully contained edges with padding. Landscape 3:2. Source panel art for later UI slicing, not a configured Unity sprite.
```

## B13 — Icon theo cây mới

Thay `[SYMBOL]` theo bảng icon P12 trong bộ cũ: Water, Growth, HP, Sức sống, Coin, Root/Leaves/Trunk, thời tiết, Rebirth, point từng stage. Một lượt một icon.

```text
Create one isolated game UI icon of [SYMBOL] for a refined bonsai-themed botanical-fantasy game. A crisp illustrative symbol with believable organic shapes, restrained shading, dark forest-green outlines and a muted palette of natural green, gray-brown bark, ivory, blue for Water and bronze-gold for Coin. Tree and branch symbols use curved trunks and layered horizontal foliage, not round cartoon tree crowns. Clock imagery represents stored vitality time; a heart represents HP. Keep the symbol readable at 48 pixels through a strong simple silhouette and minimal detail.

Center on a square canvas with 15 percent padding. Transparent background with real alpha if supported. No text, tiny bark texture, photorealistic miniature clutter, extra icons, enclosing button, scene or watermark. Square 1:1.
```

## B14 — Cosmetic quanh thân cây mới

```text
Using the attached approved bonsai-inspired tree concept, add a separate visual reference for the cosmetic “Hào Quang Sinh Mệnh”. A restrained soft jade ribbon of light wraps loosely around the lower-middle curved trunk, accompanied by a few tiny floating motes. Follow the trunk curve and keep bark, exposed roots, branches and foliage visible. The aura is decorative and gentle, visually different from fire or lightning. Preserve the tree geometry and chosen realistic or anime rendering.

No HP bar, shield bubble, stats, glowing entire canopy, giant magic circle, orange fire, text or UI. One clear three-quarter view on a neutral background, square 1:1. Aura is an effect concept for later separate implementation, not part of the tree mesh.
```

## B15 — Thời tiết/Burning trên cây mới

Thay `[STATE]` bằng một mô tả bên dưới; tạo riêng từng ảnh:

- `calm neutral daylight, no buff cues`
- `sunny warm daylight with restrained sun rays`
- `gentle rain and cooler blue-gray ambient light, no lightning impact`
- `thunderstorm with slate clouds and distant lightning, no tree impact or fire`
- `one actual lightning bolt ending on one branch with a tiny local contact spark`
- `small orange flames and thin smoke restricted to one foliage pad`
- `small orange flames restricted to the lower trunk`
- `small orange flames restricted to one exposed root`

```text
Edit the attached approved bonsai tree scene to show [STATE]. Preserve the exact tree structure, wide layered canopy, curved trunk, root geometry, camera, scale and platform. Change only the requested weather or localized effect. Maintain the chosen realistic or semi-realistic anime rendering. Keep the tree readable and reserve quiet edges for UI. A storm without an impact does not set the tree on fire; localized burning affects only the specified region. Harmful fire is orange, distinct from the decorative jade aura.

No added branches, new scene, destroyed platform, entire-tree inferno, characters, text, floating damage numbers or HUD. Landscape 16:9. A static visual reference, not a completed particle system.
```

## B16 — Sửa ảnh sinh ra không giống tham chiếu

```text
Correct the generated tree using the attached original bonsai photograph and approved concept as the structural guide. Restore the thick twisting trunk, large visible root flare, long horizontal branches and flattened asymmetric tiers of fine green foliage. Widen the canopy and open the gaps between foliage layers so the branching is visible. Replace rounded spherical canopy blobs and straight cylindrical stems with believable organic forms. Keep the chosen realistic or semi-realistic anime rendering. Remove exhibition scenery, planter, neon glow and any extra fantasy structures. Preserve one single tree, full silhouette inside the frame, neutral background, no text or watermark.
```

## B17 — Ảnh cây trưởng thành mang cảm giác “Cây Thế Giới”

Đây chỉ là biến thể concept về khí chất, không thêm stage hoặc luật gameplay.

```text
Using the attached bonsai photograph and approved game-tree concept, make the mature tree feel venerable and quietly mythical while keeping the same grounded bonsai architecture. Emphasize the ancient winding bark, exposed gripping roots, elegant horizontal branches and expansive layered crown. Add only subtle atmosphere: soft morning mist, warm filtered daylight and a few distant natural motes. Keep foliage naturally green and the tree itself unlit by magic. No giant tower-tree, hanging city, ruins, rune-covered bark, portals, deity faces or new fantasy props.

[RENDER_STYLE]

One tree on its modest soil platform, generous empty space for an eventual UI, full canopy and roots visible. Landscape 16:9, no text, UI or watermark.
```

## Chuyển concept thành asset trong project

- T10 vẫn ưu tiên pipeline, scale/pivot, prefab/anchor/collider và ba stage; phong cách ảnh chân thực/anime không tự yêu cầu shader cao cấp hoặc mesh cực dày.
- Tạo cảm giác chân thực bằng silhouette, cấu trúc nhánh, chất liệu có chọn lọc và ánh sáng. Anime vẫn cần model 3D khi orbit, không thay bằng một ảnh billboard duy nhất.
- Giữ các ID `seedling`, `young_tree`, `large_tree`, `glow_placeholder`; không thay resource, save hoặc luật Rebirth.
- Bệ đất tách riêng; vị trí gốc thân nhất quán. Chốt kích thước thật khi dựng, không suy ra mét từ ảnh chưa có thước đo.
- Hình lá chi tiết trong concept không quyết định phương pháp mesh/cards. Chọn cách dựng sau khi kiểm tra pipeline hiện có, không tự thêm package.
- `Anchor_LightningTarget`, `Anchor_LeavesCenter`, `Anchor_TrunkAura` là Transform gắn trong prefab. Rễ/Lá/Thân dùng làm vùng tác động, không vẽ tên anchor lên texture.
- Kiểm tra camera để tán rộng không bị cắt, collider không click xuyên hoặc lệch và UI không phủ cây. Lưu ảnh model, prompt và nguồn tham chiếu cùng bàn giao T10 sau khi thực sự triển khai.

## Thứ tự tạo đề xuất

1. B01 và B02 từ cùng ảnh gốc để chọn rendering.
2. B04/B05/B06 để chọn ba stage cùng loài; B08 kiểm tra sự liên tục.
3. B07 cho tham khảo Blender; B09 chọn scene; B10 chọn HUD.
4. B11 áp lên panel chức năng cũ; B12/B13 xuất thiết kế asset rời.
5. B14/B15 và các prompt hiệu ứng P22–P28 của bộ cũ, nhưng thay phần rendering cũ bằng hướng mới.
