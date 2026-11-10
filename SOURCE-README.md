# IdleReminder 0.0.1.28 源码

本包为0.0.1.28发布版对应的IR本体源码，不包含0.0.1.29的resx重构、独立SkinDesigner、Git历史、私人配置或用户录音。

## 构建

需要Windows 11 x64及.NET 10 SDK（只安装Runtime不能编译）；首次还原需要访问NuGet。
在解压目录打开PowerShell，运行：

    .\build.ps1

如系统阻止脚本，可在本目录手动执行同样的dotnet publish命令，再复制themes和许可文件；无需永久放宽系统执行策略。
构建产物位于publish/current，程序为自包含发布，最终用户无需安装.NET Runtime。
本包不包含SDK或NuGet缓存。请保留项目目录结构，不要只移动cs文件或EXE。

## 源码与资源

- src：IR本体完整源码、项目和manifest，包含程序内诊断入口。
- assets/icons：官方图标原始PNG及ICO；assets/tone0～4.wav：五种合成提示音。
- generate-tones.ps1：提示音生成脚本。
- themes：与本次二进制包相同的正式皮肤及引用资源。
- licenses：随附运行时的原始许可材料。
- 使用指南.txt、测试与兼容性说明.md：用法和已知限制。

构建已提供的PNG/ICO/WAV无需另装图像或音频工具。单元测试及本机专用诊断工具工程未包含在此最小本体源码包中；它们不是构建程序所必需的文件。
不要将新旧程序同时运行；复制源码不会迁移个人配置。实际使用时程序按所在目录保存data；在本项目开发根目录的特殊标记行为见src/Store.cs。

## 版本来源

本体代码以本地提交acc1dda（0.0.1.28自定义暂停）为基线，附加版权/SPDX注释；许可文档、使用说明和正式皮肤按交付时内容更新。
未使用当前主分支的0.0.1.29界面资源化代码。包中不含resx重构。
隔离构建验证确认可生成0.0.1.28；未承诺二进制逐字节可复现。

## 许可

IR本体采用GPL-3.0-only，全文见LICENSE。
官方应用图标单独适用ICON-LICENSE.md；正式皮肤及配套勾号PNG适用themes/LICENSE.md。
第三方运行时许可见licenses。独立皮肤编辑器代码不在本包范围。
本包中的素材许可不延伸至用户自行导入录音或第三方素材。

项目地址：https://github.com/Junmiao05/IdleReminder
本项目完全免费。