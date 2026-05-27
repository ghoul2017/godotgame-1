# 图标风格审查记录

## 本次问题

用户反馈图标仍受矢量图影响，角色和单位方向基本可用。复查缩略图和生成记录后，结论成立：问题主要出在道具、页签、分类、状态、交易和研发图标，而不是单位、建筑、背景或 UI 皮肤大截面。

## 原因

- 早期生成记录中存在 SVG 栅格化结果，虽然已被覆盖，但记录本身会误导后续工作。
- 后续原创图标仍大量使用几何图元、细线、统一卡框、扫描线底和切角终端面板，视觉语言接近矢量图标库。
- 执行时把统一库存容器的道具格、页签按钮和状态角标视觉错误地烘焙进图标本体。
- “清晰剪影”和“32px 可识别”被过度理解成线框符号；缺少“具体物件、体积、材质、透明背景、不要 UI 框”的硬约束。

## 修正规则

- 图标本体必须是透明背景上的具体物件、设备、矿石、工具、徽章或状态角标。
- 禁止把统一方框、道具格边框、终端卡片、蓝图网格底、扫描线底图烘焙进图标本体。
- 禁止细线线稿、纯几何符号、SVG 风格、app 图标式统一外框。
- 道具图标优先表现实物，例如金属锭、硅晶、矿石团、旧电池罐、磨损芯片、维修工具包、手持扫描器和平台机体。
- 交易和研发图标优先表现交易物件、补给箱、蓝图纸卷、协议板、制造夹具和空投舱结构，不做整张方形 UI 蓝图。
- 页签和分类图标可以更符号化，但必须是透明背景、粗实剪影、少量材质和厚度。
- 状态图标应是透明角标，例如锁、勾、警告和信用凭证，不做完整卡片。
- UI 框、槽位、按钮、列表行和面板由 UI 皮肤资源承担，不属于图标本体。

## 推荐生成提示词

```text
single PC RTS inventory icon asset, transparent background, no UI frame, no square card, no item slot border, no terminal panel, worn industrial object with physical volume, 3/4 top-down view, chunky readable silhouette, oxidized metal, scratches, chipped paint, grime, low-power cyan or amber accent lights, soft contact shadow, hand-painted raster game art, subject fills 75-85 percent of canvas, readable at 48px
```

## 推荐负面提示词

```text
vector icon, SVG, line art, outline-only, flat pictogram, app icon, logo, uniform square border, terminal card frame, blueprint grid background, thin strokes, perfect geometric primitives, UI badge, text, letters, watermark
```

## 当前处理

- 单位、建筑、背景和 UI 皮肤大截面暂时保留。
- 道具、页签、分类、状态、交易和研发图标已进入返工，目标是去掉统一卡框和线框符号。
- 后续生成记录必须说明是否使用透明背景、是否避免 UI 框、是否做过 48px 缩略检查。
- 运行时实际图标目录包括 `assets/ui/icons/`、`assets/ui/orbit/categories/`、`assets/ui/orbit/status/`、`assets/ui/orbit/trade/`、`assets/ui/orbit/research/` 和 `assets/data/*/`；当前没有使用 `assets/ui/orbit/icons/` 或 `assets/ui/surface/icons/` 作为正式图标目录。
- 同名旧 SVG 和 `.svg.import` 已删除，防止后续继续读取或误接回矢量源。

## 不合格图片处理规则

- 图片不达标时，不允许基于不合格图片继续抠图、修边、改色或局部迭代。
- 必须删除不合格输出，再用修正后的提示词从零重新生成。
- 重新生成提示词必须写明上一版失败原因，例如背景不透明、矢量感、线框符号、统一卡框或语义不清。
- 验证失败的图片不能进入资源引用，也不能用“后续再修”作为完成标准。
- 单位、建筑、道具和 UI 图标如果要求透明背景，必须验证四角 alpha 为 0，且主体外大部分像素透明。
