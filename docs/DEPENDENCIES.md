# 依赖与支持版本要求

## 2026-10-29 v1.0.0当前基线

源码版本1.0.0；C#14/.NET10 LTS/WPF和四层工程保持。自包含NETCore/WindowsDesktop与System.Management/传递System.CodeDom精确**10.0.14**，采用微软[2026-10-25正式安全补丁](https://github.com/dotnet/core/blob/main/release-notes/10.0/10.0.14/10.0.14.md)，不采用临期或预览框架。global.json继续10.0.401/latestPatch；本地复用10.0.401，CI实际SDK单独记录。xunit.v3仍4.0.1，解析测试依赖集合未改。

[.NET10支持至2028-11-14](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core)；PowerShell7.6 LTS继续7.6.6，[Python3.14仍bugfix且计划2030-10结束](https://devguide.python.org/versions/)。开发/测试工具不随产品分发；没有系统级安装。本版实际CI、许可和包验证见[V1发布记录](V1-RELEASE.md)。下方日期章节、10.0.12/13依赖表均为历史版本，不能替代当前基线或新包验收。

## 2026-10-21 M13当前候选更新

当前源码0.3.0-alpha.1，本地准备、尚未推送或发布。[M13维护期复核](V03-M13-LOCAL-READINESS.md)列出本轮官方依据：.NET10 LTS及PowerShell7.6 LTS维护至2028-11-14，Python3.14处于bugfix且计划2030-10结束；xUnit4.0.1/MTP2.4及固定官方Actions保持维护依据。没有新采用临期工具或替换框架。

本候选System.Management及传递System.CodeDom固定**10.0.13**，自包含NETCore/WindowsDesktop固定**10.0.13**，版本入口分别为Windows.csproj及Directory.Build.props。复用已有本地SDK10.0.401，不安装系统组件；CI按global.json的latestPatch选择，未来实际SDK另记，不将旧本地SDK称作最新补丁。原v0.2/M09/M11/M12包及下方旧依赖表保留当时版本，不能当成本候选依赖或验收结果。最终包以BUILD-INFO、deps、runtimeconfig和逐文件清单为准；新CI/目标验收仍待执行。

## 历史基线和环境依据

版本基线核对日期：2026-10-10；实际验证更新：2026-10-18，GitHub官方Action/Runner于2026-10-17复核。本轮CPU修复未升级依赖或改变最低版本要求。本文列出版本要求；[兼容性矩阵](COMPATIBILITY.md)记录具体环境和证据。声明范围、已解析版本、文件存在与测试通过分别标记。

## 统一基线

- 宿主机OS不设项目版本或剩余支持期门槛，只核对工具/API实际兼容性。
- 客户机和干净目标允许Windows 10/11 x64；记录版本类型、版本、Build、补丁和实测结果，不预先承诺所有版本兼容。
- VMware Workstation Pro最低 **17.5.2**，当前文件版本 **17.5.2 build-23775571**；配套工具按实际能力确认。
- 下载的VMware MCP测试工具使用 **Python >=3.14**，选择维护中的稳定版；本地README与requires-python已统一，后续Python分支仍需验证。
- 上述用户指定环境不再因旧的12个月维护期门槛阻止测试。官方支持另列；其他SDK、脚本工具、依赖和CI继续核查维护状态，临期工具不作为新采用方案。
- 数字下限不保证全部更高版本兼容；.NET SDK/库使用项目固定版本，升级后再验证。

## 操作系统、开发和运行工具

| 项目 | 支持版本要求 | 用途/必要性 | 当前事实 |
| --- | --- | --- | --- |
| 宿主机Windows | 不设版本门槛，工具实际兼容 | 开发及宿主检测 | 既有Win11 Home Chinese 24H2、26100.7623 x64记录，已实际构建/采集 |
| 客户机/干净目标Windows | Windows 10/11 x64，按具体版本/Build验证 | 客户机采集、发布包验收 | Win10教育版18363 x64普通权限实测；干净口径为无需SDK/系统.NET，已有VC++与其他软件 |
| VMware Workstation Pro | >=17.5.2，后续分支能力另核对 | VMware测试/管理 | 17.5.2 build-23775571；运行/Tools/客体与获授权电源测试通过 |
| vmrun.exe | 与所选Workstation配套，基线17.5.2 | MCP客体命令、文件与部分状态能力 | 17.5.2 build-23775571；认证、复制、执行、软挂起/关机/启动通过 |
| vmrest.exe | 随所选Workstation提供，实际REST API兼容后启用；有限配置操作需要 | 官方清单/电源/设置读取及有限PUT；普通采集和VMX解析不以REST为前提 | 1.3.1 build-23775571；真实App有限应用/恢复、状态拒绝及精确媒体类型通过，临时接口已停止 |
| VMware Tools | 与客户机OS及Workstation兼容且服务运行，具体版本先检查 | vmrun客体操作前置；应用只读采集不要求安装 | running，产品11.3.5.18557794、文件11.3.5.31214实测，不编造其他数值最低版 |
| vmcli.exe | 可选，需实际存在且工具命令兼容 | 下载MCP的43个vmcli声明工具 | 17.5.2安装目录未发现，不属于当前最低基线必需依赖 |
| .NET SDK | global.json起点10.0.401，latestPatch允许同特性带稳定补丁 | 宿主构建/测试；客户机运行自包含包不需要SDK | 本地10.0.401；新e27543f CI实际10.0.402，构建/130单元/106集成通过 |
| .NET Runtime | .NET 10 LTS，按具体包实际版本记录 | 应用运行 | 本地5de验收包10.0.12；新e27543f CI包NETCore/WindowsDesktop10.0.13，客户机严格23/23、CLI及GUI14/14，见[CPU18](CPU-FIX-ACCEPTANCE-2026-10-18.md)；旧3832的20/23保留历史 |
| C# / WPF / MVVM | C#14.0、.NET 10 WPF；MVVM为项目内实现 | 编译/界面 | Directory.Build.props与App项目固定，无额外MVVM框架 |
| PowerShell | 7.6 LTS，项目内准备最新补丁7.6.6 | 宿主脚本；客户机EXE不依赖 | 7.6.6官方框架依赖ZIP/哈希/版本通过，复用.NET10.0.12；Codex自带仍7.6.5 |
| Git for Windows | 已验证基线2.53.0.windows.3，采用维护中的安全补丁 | 源码版本管理 | 本次git --version核实；应用用户无需Git |
| Python | >=3.14，维护中的稳定版，当前3.14.8 | 宿主运行下载MCP，应用EXE不依赖 | 官方NuGet项目内部署及签名/版本检查通过 |
| pip / venv | 与所选Python配套，当前pip26.2.1 | MCP安装/隔离；venv为Python标准库 | 隔离环境创建成功，不是应用运行依赖 |
| Codex MCP客户端 | 支持stdio，实际CLI0.162.0-alpha.17.2 | 自动化测试客户端 | 已注册7个读取工具；运行列表/Tools实际调用通过，认证执行复用原生vmrun，没有开放其他MCP方法 |

Python3.14维护至2030年10月，.NET 10 LTS维护至2028年11月；具体补丁/OS按官方政策复核。[Python版本状态](https://devguide.python.org/versions/)、[.NET支持政策](https://dotnet.microsoft.com/en-us/platform/support/policy)、[PowerShell维护政策](https://learn.microsoft.com/en-us/powershell/scripting/install/powershell-support-lifecycle)。

微软.NET官方OS表只列部分Windows 10 LTSC/Enterprise环境。用户允许Windows 10测试，不代表所有版本得到微软支持；项目实测与官方支持分列。[Windows与.NET支持表](https://learn.microsoft.com/en-us/dotnet/core/install/windows)。

## 主项目直接依赖与运行框架

| 依赖 | 源码/发布版本要求 | 范围 | 依据 |
| --- | --- | --- | --- |
| System.Management | 精确10.0.12 | Windows采集/运行 | src/VMIdentityLab.Windows/VMIdentityLab.Windows.csproj |
| xunit.v3 | 精确4.0.1 | 两个测试项目，不随应用发布 | 两个测试csproj |
| Microsoft.NETCore.App.Runtime.win-x64 | 本地验收10.0.12 / CI实际10.0.13 | 自包含平台运行时 | 各包BUILD-INFO/deps/runtimeconfig，不跨包继承验收 |
| Microsoft.WindowsDesktop.App.Runtime.win-x64 | 本地验收10.0.12 / CI实际10.0.13 | 自包含WPF运行时 | 各包BUILD-INFO/deps/runtimeconfig，不跨包继承验收 |

四个源码工程是内部引用，当前v0.2源码版本随应用0.2.0-alpha.1；唯一版本定义在Directory.Build.props。v0.2未增加第三方依赖或升级工具链；本地0.1.0-alpha.2包保留v0.1版本。平台程序集由运行框架统一提供，不按DLL名字单独选版本。[体积清单](package-size-inventory.csv)记录旧alpha.1包，不代表新版包的逐文件清单。历史包与当前源码的区别见[版本记录](../CHANGELOG.md)。

CI同特性带补丁选择来自既有latestPatch规则，不改源码版本或本机环境。官方[10.0.13发布记录](https://github.com/dotnet/core/blob/main/release-notes/10.0/10.0.13/10.0.13.md)确认2026-10-13发布，SDK10.0.402包含10.0.13运行时；下载CI包的实际来源/运行时/许可已核实，许可文本与官方版本一致，见[第三方说明](../THIRD-PARTY-NOTICES.md)。CI与本地客户机证据仍分开。

## NuGet完整已解析集合

来源为既有App及两个测试项目的project.assets.json和发布文件。本轮CPU修复复用既有解析集合，最新130单元/106模拟集成通过；没有新增包或手工覆盖传递版本。状态及有限配置请求使用平台System.Net.Http。

| 包名 | 已解析版本 | 使用范围 |
| --- | --- | --- |
| System.Management | 10.0.12 | 直接应用依赖 |
| System.CodeDom | 10.0.12 | Windows项目传递依赖，发布中由平台资产提供对应程序集 |
| xunit.v3 | 4.0.1 | 直接测试依赖 |
| xunit.v3.assert | 4.0.1 | 测试 |
| xunit.v3.common | 4.0.1 | 测试 |
| xunit.v3.core.mtp-v2 | 4.0.1 | 测试 |
| xunit.v3.extensibility.core | 4.0.1 | 测试 |
| xunit.v3.mtp-v2 | 4.0.1 | 测试 |
| xunit.v3.runner.common | 4.0.1 | 测试 |
| xunit.v3.runner.inproc.console | 4.0.1 | 测试 |
| xunit.analyzers | 2.1.0 | 编译分析/测试 |
| Microsoft.Testing.Platform | 2.4.0 | 测试 |
| Microsoft.Testing.Platform.MSBuild | 2.4.0 | 测试 |
| Microsoft.Testing.Extensions.TrxReport.Abstractions | 2.4.0 | 测试报告 |
| Microsoft.Testing.Extensions.Telemetry | 2.4.0 | 测试传递包，遥测显式关闭，不随应用发布 |
| Microsoft.ApplicationInsights | 2.23.0 | 测试传递包，不随应用发布 |
| Microsoft.Bcl.AsyncInterfaces | 6.0.0 | 测试传递兼容包 |
| Microsoft.Win32.Registry | 5.0.0 | 测试传递兼容包 |
| System.Security.AccessControl | 6.0.1 | 测试传递兼容包 |

兼容包的5.x/6.x版本不是.NET 5/6运行时要求，不套用旧运行时寿命，也不为数字一致改上游依赖图。开发/CI设置DOTNET_CLI_TELEMETRY_OPTOUT=1与TESTINGPLATFORM_TELEMETRY_OPTOUT=1。

## 下载VMware MCP的全部声明依赖

来源：[ZacharyZcR/vmware-mcp](https://github.com/ZacharyZcR/vmware-mcp)，元数据0.1.0。本地只修改最低版本声明与说明，没有改运行代码。

| 依赖 | 本地版本要求 | 必需性/状态 |
| --- | --- | --- |
| CPython | >=3.14，项目内固定3.14.8 | 宿主测试解释器已使用；应用与客户机包不依赖Python |
| mcp（Python SDK） | 上游>=1.0.0，当前兼容系列<2.0；锁定1.30.0 | 安装/握手/7读取工具通过，运行列表与Tools实测 |
| httpx | 上游>=0.27.0，当前系列<1.0；锁定0.28.1 | MCP依赖已安装；本轮App REST采用.NET客户端，不把它计作MCP REST工具实测 |
| hatchling | 上游无数值下限，当前锁定1.32.4 | 本地MCP wheel构建通过 |
| asyncio、json、os、typing | 随Python提供 | 标准库，不额外pip安装 |
| Workstation/vmrun/vmrest/Tools/vmcli | 见前表 | 按功能组核实，不把所有工具组变成强制依赖 |

原下载包没有依赖锁文件；环境配置已生成[37项解析版本锁定清单](../tools/mcp/requirements.lock.txt)，另安装本地vmware-mcp0.1.0 wheel。安装/一致性和握手不替代功能实测；具体环境状态见[ENVIRONMENT-SETUP.md](ENVIRONMENT-SETUP.md)和[本轮结果](READINESS-TEST-RESULTS-2026-10-11.md)。

CursorTouch/Windows-MCP、WinProbe、Node.js、uv和SSH未作为此次下载MCP的依赖采用。后续若选用桌面MCP，单独记录其依赖与验收。

## GitHub Actions与Runner

| 项目 | 固定版本/要求 | 当前状态 |
| --- | --- | --- |
| actions/checkout | v7.0.1，3d3c42e5aac5ba805825da76410c181273ba90b1 | workflow固定 |
| actions/setup-dotnet | v6.0.0，a98b56852c35b8e3190ac28c8c2271da59106c68 | 读取global.json |
| actions/upload-artifact | v7.0.2，cf430e030ddbb5b0abf93d22962f4752f3646cd9 | workflow固定 |
| Windows Runner | windows-2025，平台提供满足固定Action要求的runner/Node运行时 | 2026-10-18实际Actions39109350478构建/130单元/106集成/publish通过，本机Node不是必装依赖 |

2026-10-17已只读核对三个官方Release及Git远程标签，完整SHA与workflow一致；官方windows-2025清单仍在更新，实际Actions已成功执行。依据为[当前workflow](../.github/workflows/ci.yml)、[官方Runner清单](https://github.com/actions/runner-images/blob/main/images/windows/Windows2025-Readme.md)和[本轮接入记录](GITHUB-READINESS-2026-10-17.md)。这些CI依赖不捆绑到客户机应用包。

## 本次验证边界

- 已完成：既有工具环境/7读取方法及客户机临时接入；最新Release、127单元/100模拟集成、真实App有限配置与完整副本应用/恢复、最新包客户机十六项及清理，见[FULL17](V02-FULL-TEST-RESULTS-2026-10-17.md)。PowerShell7.6.6只做部署/版本验证，不把它计作App运行依赖。
- GitHub已验证：公开源码导入、真实Actions构建/127单元/100模拟集成及CI产物，私密安全渠道开关/入口与维护者Daniel Waltz确认，见[结果](GITHUB-READINESS-2026-10-17.md)。
- 新增已完成：e27543f CI130/106及同Runtime13原包客户机严格23/23，原三项全通过；[CPU18](CPU-FIX-ACCEPTANCE-2026-10-18.md)保存来源、包哈希、清理及有限范围。未新增依赖或系统安装，旧失败和许可证据保留。
- 发布已完成：v0.2.0-alpha.1按用户授权公开预发布，专用Actions39113269967成功，tag及三个资产大小/SHA回读通过；复用原CI字节，不新增依赖或安装。
- 未执行：快照、其他MCP方法、Windows11客户机及全新OS；不保证其他版本/拓扑/数值兼容。
- 下一步：本次上传已完成，后续按维护任务处理；不为回写文档触发新CI，不重建已删除副本，系统安装和真实VM仍遵守授权规则。
- v0.1已有源码/本地预览包为0.1.0-alpha.2，来自干净提交ab177a7，BUILD-INFO.json记录SDK10.0.401与Runtime10.0.12；程序集与本机实际导出版本核对通过，ZIP附SHA256。此次不升级依赖，不代表完成新的在线维护期/漏洞核查。
- 原alpha.1与2026-10-11完整验证ZIP保留原构建快照；后者已在声明目标测试。alpha.2没有重做真实客户机或全新OS测试，正式发布前仍需完成其余发布条件。
