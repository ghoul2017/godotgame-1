# 建筑 PNG 生成记录

日期：2026-05-27

## 生成范围

本次只生成以下建筑 PNG：

- `assets/data/buildings/storage_box.png`
- `assets/data/buildings/repair_station.png`
- `assets/data/buildings/rocket_pad.png`
- `assets/data/buildings/solar_panel.png`
- `assets/data/buildings/assembler_basic.png`
- `assets/data/buildings/fluid_tank.png`

## 依据

- 已读取 `docs/UI设计文档.md`：地表 UI 需要旧工业、废土机械、低功耗终端感；建筑图标应表现用途，并在 32 px / 48 px 下保持清晰剪影。
- 已读取 `docs/6. 地表矿产、物品物流、建造、生产和电力系统.md`：第六步首批正式建筑包括仓库、维修站、火箭组装坪、太阳能板、基础组装机和储罐；建筑资源服务地表建造、生产、电力和物流系统。

## 方法

- 使用内置 `image_gen` 生成栅格源图，附图仅作为旧工业废土地表基地的风格参考。
- 每个建筑单独生成，提示词统一要求：三分之四俯视、旧工业废土机械、低饱和金属材质、清晰剪影、无文字、无水印、非矢量图、非 UI 面板。
- 生成源图使用纯色 `#00ff00` 色键背景；最终项目 PNG 使用透明背景。
- 未读取现有 `.svg` 作为图像源。
- 未使用 SVG 栅格化。

## 资产区分

- `storage_box.png`：加固金属仓储箱，橙锈色面板、边角护甲和锁扣，表达地表仓库。
- `repair_station.png`：低矮维修舱、维修机械臂、工具舱和服务灯，表达维修入口。
- `rocket_pad.png`：大型组装平台、龙门支架、模块夹具和轨道，表达火箭部件组装入口，不表现发射流程。
- `solar_panel.png`：倾斜太阳能阵列、金属支架、控制箱和线缆，表达稳定发电。
- `assembler_basic.png`：基础组装机、传送带、压装臂和输出托盘，表达早期生产。
- `fluid_tank.png`：双圆柱储罐、阀门、管线和底座，表达能源介质或后续液体接口存储。

## 本地处理记录

- 本机无 `python` 命令，使用 `python3`。
- `remove_chroma_key.py` 因当前 Python 环境缺少 Pillow 无法执行；本机也没有 `uv` 或 ImageMagick。
- 处理改为：用 macOS `sips` 缩放到 256x256，再用 Python3 标准库解析 PNG，把 `#00ff00` 色键转换为 alpha，并清理低 alpha 残边。
- 只保留最终 256x256 RGBA PNG 作为项目资产。

## 验证

验证命令：

```bash
file assets/data/buildings/storage_box.png assets/data/buildings/repair_station.png assets/data/buildings/rocket_pad.png assets/data/buildings/solar_panel.png assets/data/buildings/assembler_basic.png assets/data/buildings/fluid_tank.png
sips -g pixelWidth -g pixelHeight -g hasAlpha assets/data/buildings/storage_box.png assets/data/buildings/repair_station.png assets/data/buildings/rocket_pad.png assets/data/buildings/solar_panel.png assets/data/buildings/assembler_basic.png assets/data/buildings/fluid_tank.png
```

验证结果：

- 6 个文件均为 `256 x 256`。
- 6 个文件均为 8-bit RGBA PNG。
- 6 个文件均 `hasAlpha: yes`。
- 四角 alpha 均为 0。
- 48 px 缩略检查中，仓库箱、维修站、火箭组装坪、太阳能板、基础组装机、储罐的轮廓和功能语义均可区分。
