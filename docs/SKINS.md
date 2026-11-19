# Guardian Drone 皮肤 / 0.4.0 候选

日期：2026-11-19。来源：Nexus 玩家反馈里的“more premade models”，以及作者后续要求“看看皮肤方面能不能搞”。本文件记录候选实现和实机验证。**当前状态：已存入 GitHub 分支 `feature/drone-skins`；`main` 仍是 0.3.0，未合并、未发布。** 作者认为“为一个下拉框单独开一个设置标签页”偏重，所以本分支只作为存档，暂不进入正式版。

## 结论

能做，而且不再占用 F9：

- 模型选择放在游戏设置菜单的 **Guardian Drone → Model** 下拉框（CUCoreLib 的 mod options 标签页）。
- 不需要第三个版本，也不需要额外按键；F6/F7/F8 保持原样。
- 三个模型：**Standard / Industrial / Stealth**。
- 纯装饰，不影响伤害、射程、治疗或优先级。

## 设置标签页是不是过重

一个下拉框单独占一个标签页确实偏重。对比过的做法：

| 做法 | 优点 | 缺点 |
|---|---|---|
| 独立设置标签页（本分支） | 游戏原生 UI、无新按键、可实时切换 | 只有一个下拉框，标签页显得空 |
| BepInEx 配置项 `Model=0/1/2` | 零 UI、零按键、实现最轻 | 不直观，需要编辑 cfg 或重启 |
| 游戏自带物品自定义（默认 N） | 零新按键、原生 UI、可改控制器颜色 | 8 个颜色槽和 3 个模型没有语义对应，玩家不容易发现 |
| 三个独立 DLL / ZIP | 下载哪个就是哪个皮肤，零设置 | 三份构建/测试/Nexus 文件；不能在游戏内切换 |
| 不做皮肤 | 没有额外维护 | 不回应玩家的“more premade models” |

如果以后正式做，建议优先考虑 **BepInEx 配置项 + 已部署时热切换**，或者在控制器物品描述里明确写“在设置里改”；独立标签页可以保留，但要有第二个选项（例如音量/特效）才值得占一页。

## 为什么不用单独版本 / F9

先看了游戏和现有 mod 的做法：

- 游戏自带 **物品自定义 UI**（默认 N 键）可以改物品名称和 8 个颜色槽；它更适合给单个物品调色，不适合当作明确的“模型选择器”。
- **CUCoreLib 1.1 提供 `ModOptionsRegistry` / `ModOptionDefinition.Dropdown`**，可以在游戏设置菜单里注册自定义分类和下拉框。Nexus 上的 **Player Sprite Replacer** 就是用这种方式加 “Skin Replacer” 标签页；**Dress My Scav** 则用独立皮肤菜单 + 控制台命令。
- Wayfarer Mech 用的是多个按键 + 自定义面板；Save Anywhere 用 BepInEx 配置 + F5。多按键是常见做法，但皮肤只有一个选项，没必要再占一个键。
- 单独发布 Standard/Industrial/Stealth 三个 DLL 可行，但会产生三份构建、三份测试、Nexus 多个文件和“装了两个版本”的支持问题；一个 DLL + 设置下拉框更省事。

## 实现

- `Plugin.cs`：注册 CUCoreLib 下拉框：
  - 分类：`Guardian Drone`
  - 选项：`Model` = Standard / Industrial / Stealth
  - `Id`：`local.cu.guardiandrone.model`
  - `apply` 回调更新 `Plugin.SelectedSkin` 并立即刷新已部署的无人机。
  - 启动时读取 `Settings.Get<SettingDropdown>(ModelOptionId).value`，没有则默认 Standard。
- `DroneVisual.cs`：按 `skin` 选择 hull／rotor0／rotor1／tool；`eye` 和控制器图标共用；`SetSkin` 只换 sprite，不重建无人机。
- `DroneController.cs`：部署时使用 `Plugin.SelectedSkin`；`Skin` 属性返回当前 visual 模型。
- `DroneSave.cs`：不新增存档字段；模型是游戏设置，跨角色/存档全局保存，旧 0.3.0 存档不受影响。
- 模型共用同一 64×40 骨架、同一枪口偏移和同一 eye 位置，避免光束／治疗落点错位。

## 美术

- `pixel-art/guardian-drone/build.py` 保留 Standard 资产不变，新增 `hull_1`、`rotor0_1`、`rotor1_1`、`tool_1` 和 `hull_2`、`rotor0_2`、`rotor1_2`、`tool_2`。
- 预览：`pixel-art/guardian-drone/skin-preview.png`；游戏内对比：`furry/runs/v040-settings1/settings-model-*.png`。

## 实测

隔离副本 `furry/.local/cu-test/drone-game`，真实游戏进程，可见窗口。

| 场景 | 结果 | 证据 |
|---|---|---|
| 设置项存在 | PASS | `v040-settings1`：`modelSetting` 非空 |
| Standard／Industrial／Stealth 切换 | PASS | `v040-settings1`：`model`、`modelSetting`、`drone.skin` 三者一致，截图 `settings-model-0/1/2.png` |
| 游戏设置菜单出现 Guardian Drone 标签 | PASS | `v040-settings-tab2`：标签页截图 `settings-tab-101.png`，显示 Model = Stealth |
| 设置跨进程保留 | PASS | `v040-settings-reload2`：重启后 `model=2`、`modelSetting=2`，重新部署的无人机为 Stealth |
| 0.3.0 战斗／治疗矩阵在最终设置 build 上重跑 | PASS | `v040-settings-smoke`：17 项全 PASS |
| 0.3.0 旧存档 | 未单独重跑 | 不新增存档字段；模型是全局设置，旧存档读入不依赖它 |

最终设置 build DLL SHA-256：`5341b236c209368c162c59d8c1264adc9ceb052096424ba32cb9d822271468ef`（测试 stage：`furry/artifacts/guardian-v040-settings-stage`）。

## 未做

- 设置标签页的分类名是英文 `Guardian Drone`；模型名是英文 Standard/Industrial/Stealth。
- 控制器物品图标不随模型变化。
- eye 和控制器图标共用；没有每模型独立 eye 动画。
- 没有把新模型加入 Nexus 图库或发布材料；候选只进入 `feature/drone-skins` 分支存档，未合并到 `main`。
