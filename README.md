# Guardian Drone

Casualties: Unknown 的独立同伴模组，当前 GitHub 玩家包是 0.2.1（[Release v0.2.1](https://github.com/dumplingsup/casualties-unknown-guardian-drone/releases/tag/v0.2.1)）；Nexus 页面仍由作者手动更新，材料在 `release/nexus-0.2.1/`。免费、无敌的四旋翼在开阔处大约 6–9 格外巡航；主角挡住弹道时会停火并换位，已经飞出的弹丸仍可能命中。治疗时靠近，止住外伤出血并修复皮肤和肌肉。

0.1.0 的可玩性没有通过，那个 ZIP 和标签保留作历史记录。[0.2.0 验收](docs/V0.2.0.md) 记录了这一版实测过和还没测的项目。0.2.1 修复了随时存档模组造成的重复控制器和换层时的无人机卡死：[0.2.1 修复记录](docs/V0.2.1.md)。0.2.1 已在隔离副本里与真实的 Save Anywhere 1.5.0 和报告链接的 QoL Unknown 7 v1.1.4 同测：控制器在快捷栏或随身背包中读档后仍只有一只；QoL 还原成未激活状态的自定义控制器会被本模组激活并保留，不再补发；确认面板换层不卡死。

[Guardian Drone 0.2.0 已在 Nexus 发布](https://www.nexusmods.com/scavprototype/mods/985)，定位为 Windows 单人 Beta。最终 DLL 的隔离回归见 [2026-11-16 发布前复核](docs/NEXUS-PREFLIGHT-2026-11-16.md)，页面、文件和扫描结果见 [发布记录](release/nexus-0.2.0/PUBLISH-STATUS.md)。

![四旋翼外形](pixel-art/guardian-drone/preview.png)

A free quadcopter companion for Casualties: Unknown. It holds fire when you are in the lane, patrols several tiles away, and moves in to treat external wounds.

**F6** 部署／收回，**F7** 停火，**F8** 优先治疗。需要 BepInEx 5 和 CUCoreLib 1.1。插件 GUID 是 `local.cu.guardiandrone`，这是存档契约，不要随手改名。

玩家安装说明：[中文](README.zh-CN.md) · [English](README.en.md)。0.1.0 记录：[docs/V0.1.0.md](docs/V0.1.0.md)。

## 这个仓库包含什么

包含模组源码、原创像素、可重跑的绘图脚本、构建说明和 0.1.0 / 0.2.0 验收记录。

不包含游戏程序集、BepInEx、CUCoreLib、测试存档、隔离运行日志、机甲源码或其他模组的素材。参考用的第三方无人机像素只记录了链接和观察，没有把原图放进来。见 [NOTICE.txt](NOTICE.txt) 和 [pixel-art/guardian-drone/REFERENCES.md](pixel-art/guardian-drone/REFERENCES.md)。

玩家包在 GitHub Release 里。0.2.0 是 `GuardianDrone-0.2.0.zip`，0.1.0 的 ZIP 仍然留着。安装包没有写入正式游戏目录。多人、守卫和炮塔类敌人、机甲同场仍未验证。

## 构建

构建只写本目录的编译输出，不会复制到游戏。

```powershell
Copy-Item local.props.example local.props
# 编辑 local.props，填入你自己的游戏目录和 CUCoreLib 1.1 DLL。

dotnet build src/GuardianDrone/GuardianDrone.csproj -c Release -p:RestoreLockedMode=true
python tools/package_release.py
```

`local.props` 已被忽略。NuGet 版本由 `packages.lock.json` 锁定。发布时使用 [docs/V0.2.0.md](docs/V0.2.0.md) 里的哈希，不用一次随意重编译替换已测 ZIP。

重画像素：

```powershell
python pixel-art/guardian-drone/build.py
```

脚本使用 Pillow，并从本机 `pixelstudio` 模块创建画布。已提交的 PNG 就是 0.2.0 使用的原创图层，不运行脚本也能编译。

## 许可

这个仓库公开，便于查看源码和下载测试版。目前没有选择开源许可证。Casualties: Unknown、BepInEx、Harmony、CUCoreLib、Unity 以及参考像素包仍归各自权利人，不随本仓库再分发。
