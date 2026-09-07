# CeleTech Arsenal - Shuttle Extension

## 源码仓库

这是 RimWorld 1.6 模组 **CeleTech Arsenal - Shuttle Extension** 的源码仓库。项目作者为 LongRange、灵能罐头及其他贡献者。

本仓库只公开软件实现，不是完整、可直接游玩的模组发行包。创意工坊版本所需的美术、声音、AssetBundle、预览图和编译后程序集没有包含在这里，其中部分素材没有获得开源授权。

### 包含内容

- `Source/`：C# 源码与项目文件
- `Defs/`：RimWorld 软件定义
- `Patches/`：兼容性 XML 补丁
- `Samples/`：第三方扩展 SDK 示例

### 未包含且未授权的内容

以下内容不受本仓库的 MPL-2.0 许可证授权，也不能因为本仓库公开而推定获得任何使用许可：

- `Textures/` 中的美术与 AI 辅助图像
- `Sounds/` 中的声音录音
- `Assets/` 中的 AssetBundle 和着色器资产
- `About/` 中的预览图、图标和创意工坊素材
- `Assemblies/` 中的编译产物
- CeleTech、RimWorld 及其他项目的名称、Logo、商标和第三方内容

详细边界见 [`NOTICE.md`](NOTICE.md)。需要安装或游玩模组时，请使用正式发布渠道；不要把这个源码仓库当作完整运行包。

## License

除 `NOTICE.md` 明确排除或另行说明的内容外，本仓库中的源码和软件定义文件采用 [Mozilla Public License 2.0](LICENSE) 授权。

MPL-2.0 是文件级 copyleft：发布对现有 MPL 文件的修改时，相关修改文件仍须按 MPL-2.0 提供源码；独立的新文件和较大作品可以采用其他兼容条款，具体以许可证正文为准。

## Build context

项目使用 .NET Framework 4.7.2，并依赖合法安装的 RimWorld 1.6、Odyssey 和 Harmony。部分独立兼容程序集还会引用相应第三方模组。依赖程序集不包含在本仓库中，所有依赖均受各自条款约束。

仓库放在 RimWorld 的 `Mods` 目录布局下并满足本地依赖路径后，可执行：

```powershell
dotnet build Source\CMC_Shuttle_Extension.csproj -p:Configuration=Release -p:Platform=AnyCPU
```

构建只生成程序集；完整运行仍需要本源码仓库未提供的正式模组素材。

## English summary

This repository contains only the software source and data definitions for CeleTech Arsenal - Shuttle Extension. It is not a complete playable distribution. Artwork, AI-assisted images, sounds, AssetBundles, preview media, compiled assemblies, trademarks, and other omitted assets are not licensed by this repository. Except where `NOTICE.md` says otherwise, the files present here are licensed under MPL-2.0.

## RimWorld notice

Portions of the materials used to create this content/mod are trademarks and/or copyrighted works of Ludeon Studios Inc. All rights reserved by Ludeon. This content/mod is not official and is not endorsed by Ludeon.
