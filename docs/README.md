# 使用指南的维护

维护 UserGuide.zh-Hans.md 与 UserGuide.en.md；页面样式位于 guide.css。每个自然段保留为一段，不按字符数硬换行。

正常构建或发布 IR 时，MSBuild 自动调用 GuideBuilder，分别生成中英文 HTML 和 TXT（共四份文件）。只需现有 .NET 10 SDK，不需 Python、Node、第三方 NuGet 包或联网下载转换器。

也可手动生成：

    dotnet run --project docs/GuideBuilder/GuideBuilder.csproj -- docs

生成的 HTML 将 CSS 内嵌，不加载脚本、字体、图片或 CDN；两种语言页面相互使用相对链接。外部的 KDE 与 GitHub 链接仅在读者主动点击时访问。

转换器支持本项目用到的 Markdown 子集：一级至三级标题、自然段、平铺的有序/无序列表、单段引用提示、加粗、行内代码及 HTTP(S) 链接。文本会被转义，不直接执行 HTML。不要在文档中加入未支持的表格、嵌套列表、脚本或代码块。

完整发布目录应包含 docs/UserGuide.zh-Hans.html、docs/UserGuide.en.html、docs/UserGuide.zh-Hans.txt 和 docs/UserGuide.en.txt。源文件和生成器由源码包保留，用户阅读不需运行生成器。

IR 的“使用指南”按生效语言打开本地 HTML，优先使用运行程序旁的文档。根目录旧 TXT 仅为历史兼容回退，不同步维护。docs 中的新 TXT 与 HTML 均从 Markdown 自动生成。当前 IR 入口仍优先打开 HTML，尚未增加纯文本入口。

验证页面时检查浅色/深色、窄窗口、浏览器放大、目录锚点、语言互链和离线资源。自动截图不代表所有浏览器与物理显示器均已人工验收。


## 纯文本输出

TXT 去掉标题井号、加粗星号、行内代码反引号和引用前缀；保留自然段、简单列表与编号。二级章节自动编号，三级标题独立成段。外部链接输出为“名称 (完整网址)”，同名网址不重复。自然段不硬换行，由记事本的自动换行控制阅读宽度。

输出为带 BOM 的 UTF-8 和 Windows CRLF 换行，方便记事本识别中英文；代码/路径中的字面字符不会为了清理 Markdown 而误删。

验证转换规则：

    dotnet run --project docs/GuideBuilder/GuideBuilder.csproj -- --self-test

HTML 和 TXT 标题下的适用版本由构建自动附加，不需要在 Markdown 正文手工维护。正常构建读取 MSBuild 的 Version；手动运行生成器默认读取 src/IdleReminder.csproj 的 Version。HTML 浏览器标题也包含版本号。原始 Markdown 保持你的编辑内容不变。

