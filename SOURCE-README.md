# IdleReminder v0.0.2.0 源码

本包对应 v0.0.2.0 Windows x64 发布版。包含 IR 本体、中英文资源与指南生成器、构建脚本、引擎检查、正式预设及所需素材。皮肤设计器独立发布。

## 构建

需要 Windows 和 .NET 10 SDK（仅安装 Runtime 不能编译）；首次还原需联网访问 NuGet。

在解压目录打开 PowerShell，运行：

    .\build.ps1

脚本先构建、运行引擎检查，再将自包含版本发布到 publish/current。无需启动生成的程序即可验证编译；不要和正在使用的 IR 同时运行。
源树自带 .idlereminder-root，因此从本源码目录内运行时数据/皮肤使用源码根目录。独立分发文件夹应包含发布程序、themes 和许可等完整内容。
build.ps1 不生成最终公开 ZIP；正式预设位于源码根目录 themes。package-release.ps1 用于对已经生成的版本 ZIP 刷新正式预设。

手动构建和检查：

    dotnet run --project tests/EngineTests.csproj -c Release
    dotnet run --project docs/GuideBuilder/GuideBuilder.csproj -c Release -- --self-test
    dotnet publish src/IdleReminder.csproj -c Release -r win-x64 --self-contained true -o publish/current

## 指南和资源

docs/UserGuide.zh-Hans.md、UserGuide.en.md 为维护源文件。构建自动从 Markdown 生成 HTML 和干净的 TXT，并附上 IR 版本号。手动生成说明见 docs/README.md。请勿只改生成文件。
src/Resources 包含 395 项中英文界面资源；缺少未来译文时按既有逻辑回退。
assets/tone0～4.wav 为合成提示音，generate-tones.ps1 为生成脚本。官方图标 PNG/ICO 位于 assets/icons。themes 与二进制包使用相同版本的正式预设及引用 PNG。
本包不含 Git 历史、备份、NuGet 缓存、私人配置、导入录音或设计器私人草稿。未承诺二进制逐字节可复现。

## 许可

代码及预设采用 GPLv3，仅第3版。官方应用图标适用 ICON-LICENSE.md；预设及其配套勾号图片适用 themes/LICENSE.md。运行时第三方许可见 licenses。
项目：https://github.com/Junmiao05/IdleReminder
开发：君喵Junmiao & Codex。
