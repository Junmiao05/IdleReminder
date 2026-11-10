# IdleReminder｜Windows 闲置提醒器

一个轻量的Windows 11后台闲置提醒工具。离开键盘和鼠标达到设定时间后，在本地播放提示音或自行导入的录音，帮助你记起电脑上原本正在做的事。

画画、玩游戏途中拿起手机，随后忘记电脑上正在做什么？IdleReminder 在你长时间未操作键鼠时播放提醒，帮助你回到电脑。

Ever picked up your phone while drawing or gaming, only to forget what you were doing on your computer?
IdleReminder plays a reminder sound when your keyboard and mouse have been inactive for a while, helping you get back to what you were doing.

本项目完全免费，当前为试用版0.0.1.28。

## 下载与运行

请到本仓库Releases页面下载IdleReminder-v0.0.1.28-win-x64.zip，解压到固定、可写的目录，运行IdleReminder.exe。请保留整个目录，无需额外安装.NET Runtime。
源码附件是IdleReminder-v0.0.1.28-source.zip，供开发和自行编译使用。

软件常驻系统托盘，关闭设置窗口不等于退出。修改选项后请点击“保存设置”。
当前未进行代码签名。遇到安全提示时请先核对下载来源和SHA256，不要关闭系统安全防护。

## 功能

- 默认闲置20分钟提醒，可配置阈值、重复间隔、本地提示音和人声组合。
- 支持托盘暂停、1分钟到7天自定义暂停及恢复询问。程序重启后自动启用提醒。
- 按应用设置播放中或运行中不提醒；播放规则需读取Windows媒体标识，不支持按网址区分。
- 扬声器按设备设置目标音量，主动试听确认后启用；支持名单内音乐压低及音量恢复。
- 预设皮肤切换。手机通知通过另行安装并配对的KDE Connect实现，属实验性功能。
- 默认Windows输入计时；可选设备键鼠事件计时及过滤，用于兼容性排查。设备模式可能请求正常UAC，拒绝后仍可普通运行。

详细使用方法见使用指南.txt；测试证据范围和限制见测试与兼容性说明.md。

## 已知限制

- 天猫精灵方糖R的音量提高/恢复不稳定，建议关闭该设备自动提高；其他型号未验证。
- PowerPoint窗口化放映抑制不可靠；WPS未完成实机验收。
- 本机S0现代待机期间仍可能播报，睡眠行为不能保证一致；锁屏不播报已有实测。
- 手机后台及弱Wi-Fi连接不稳定。
- 未适配手柄；设备事件过滤可能漏掉触控板或数位笔。不能保证兼容所有反作弊系统。
- Windows勿扰可能隐藏通知；不保证所有应用都提供可识别、可暂停的媒体会话。

主音量提高会影响同设备其他声音，请从较低目标开始试听。强杀、断电、音频服务异常时不保证还原。

## 隐私与存储

不读取按键内容、不截图、不录屏、不监听麦克风、不读取游戏内存、不注入、不安装驱动，无账号或遥测。
默认本地使用无需联网；启用手机功能时通过KDE Connect与指定授权手机通信。
解压版配置和导入录音位于程序目录的data，皮肤位于themes。不要把私人data提交到公开仓库。

## 更新

先从托盘退出旧程序，备份data和自定义themes，再更新完整发布文件，避免覆盖个人配置。
换位置后请重新检查自启动路径、版本、录音和设备设置。不要让早期版本读写新配置。

## 从源码构建

需要Windows及.NET 10 SDK。首次还原依赖需联网。在项目根目录运行：

    .\build.ps1

生成文件位于publish/current。如脚本受系统策略限制，可手动执行其中命令，无需永久修改执行策略。
本公开源码基线对应0.0.1.28，不含下一版resx重构或独立SkinDesigner代码。包内完整构建说明见SOURCE-README.md。

## 许可

IR本体：GPL-3.0-only，见LICENSE及LICENSE_SCOPE.md。
随包预设皮肤JSON及勾号PNG：GPL-3.0-only，见themes/LICENSE.md。
官方应用图标：单独的使用许可，见ICON-LICENSE.md。
.NET运行时及第三方组件保留原许可，见licenses。

## 反馈

提交Issue时请注明版本、Windows版本、相关设备/软件、复现步骤和预期/实际结果。分享日志或截图前请移除私人信息。
