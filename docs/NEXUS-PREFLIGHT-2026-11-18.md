# 0.2.1 Nexus 更新前复核

日期：2026-11-18（Asia/Hong_Kong）。结论：**可按现有 Windows 单人 Beta 页面更新到 0.2.1**。这不是新的玩法验收，也不是全敌人、全部模组组合、多人或长期稳定性验收。

## 改动范围

- 只改控制器发放／去重、换层锁定和玩家说明；`DroneGuid`、控制器 ID `guardian_drone_controller`、存档键 `local.cu.guardiandrone.state` 和 0.2.0 玩法参数未改。
- 源码提交：`6c90cb1b5c8fe008934ba06ac5614bf11361d22a`。
- 实测并冻结的 DLL：`51f49c000a57c6b4ef25960ce877a9543bb5a164f8403d1e4531f7637d33a380`。
- 玩家 ZIP：`GuardianDrone-0.2.1.zip`，`15b7214b73d351ea16556d6766dd4d0dd8a47ec1b9be5d0b24db22e6cf3c0fc7`。
- Nexus ZIP：`GuardianDrone-0.2.1-Nexus.zip`，`c22db749d46f237d82e95834934d663162c85b7b8e251374d659df6d6647d1c1`，DLL 与玩家 ZIP 字节一致。
- ZIP 内只有 `BepInEx/plugins/GuardianDrone/GuardianDrone.dll`、两份玩家说明和 NOTICE；不含游戏程序集、依赖、配置、探针、存档、日志或本机路径。
- C# 构建不是逐字节可复现的；复核和打包使用冻结 ZIP 里的 `51f49c…` DLL，不用重新编译的 `bin` 产物替换。

## 环境与证据

隔离副本：`furry/.local/cu-test/drone-game`，BepInEx 5.4.23.6、CUCoreLib 1.1；DroneProbe 只在该副本启用。正式游戏目录没有安装 BepInEx，正式用户目录 `%USERPROFILE%\AppData\LocalLow\Orsoniks\CasualtiesUnknown` 的 `Player.log`、`Player-prev.log`、`settings.json` 哈希在测试前后一致。

保存模组复测使用真实 **Save Anywhere 1.5.0**（Nexus 885）和报告链接的 **QoL Unknown 7 v1.1.4**（Nexus 7）。两者的 `Application.persistentDataPath` 和 PlayerPrefs 写入只在本副本做了测试专用隔离；原始 ZIP 和分发包没有改。

| 层 | 结果与范围 |
|---|---|
| STATIC | PASS：工程版本、插件版本、README、存档 GUID／物品 ID 一致；ZIP 文件集合和哈希核对 |
| BUILD | PASS：locked restore + Release，0 警告 0 错误；冻结 DLL 与实测 DLL 相同 |
| BOOT | PASS：0.2.1 在隔离游戏中加载，保存模组组合也正常启动 |
| GAMEPLAY | PASS（0.2.0 记录仍适用）：部署、停火、医疗优先、挡枪和巡航逻辑未改；本版只重跑了相关生命周期 |
| SAVE_RELOAD | PASS：0.2.0 旧存档、普通保存／退出／读档、Save Anywhere 快捷栏／随身背包、QoL 快捷栏／地面控制器 |
| 控制器重复修复 | PASS：真实 QoL 存档在 0.2.0 复现两个控制器对象（一个未激活），0.2.1 激活原控制器且不补发；14–15 秒后仍 1/1 |
| 换层锁定 | PASS：原生 `RegenerateWorld`、真实确认面板、Save Anywhere 和 QoL 在场换层；加载期间 `droneCount=0`，完成后 1 台，无卡死；QoL 换层时状态文件最大间隔 0.23 秒 |
| 正式数据保护 | PASS：正式游戏目录和正式用户目录未改 |

原始证据留在工具工作区，不随仓库分发：`furry/runs/guardian-v021-bugfix2`、`guardian-v021-descent`、`guardian-v021-saveanywhere3/4/7`、`guardian-v020-qol1/2`、`guardian-v021-qol3/5/6`。

## 上架必须写清楚的范围

- 单人 Windows x64 Beta；只核对 Steam build 26666273 和上述依赖。
- 0.2.1 是 Bug 修复版，不扩大索敌、医疗或兼容性范围；0.2.0 的敌人、地形、机甲和音频限制仍适用。
- QoL Unknown 7 和 Save Anywhere 1.5.0 只在隔离副本复测；它们的槽位／自动存档 UI 没有逐个点击，未声明对所有存档模组组合通用。
- 多人、守卫／炮塔类敌人、机甲同场和长时性能未重跑。
- 公开源码不等于授予开源许可证；Nexus 权限和 AI 披露沿用 0.2.0 的实际选择。

更新材料见 [release/nexus-0.2.1/NEXUS.md](../release/nexus-0.2.1/NEXUS.md)。
