# 第三方组件与许可

原创源码采用 [MIT](LICENSE)，Copyright (c) 2026 Daniel Waltz。第三方文件保留自己的版权、许可和声明，原创 LICENSE 不替代它们。

## 当前1.0.0

本版自包含NETCore/WindowsDesktop、System.Management及传递System.CodeDom均为10.0.14。运行时/WPF官方v10.0.14标签许可已核对；两个官方NuGet包nuspec已更新为10.0.14/MIT，共享第三方声明只在换行上不同，继续保留一份完整正文。没有删除不同组件的版权声明。实际构建和原包验收见[V1记录](docs/V1-RELEASE.md)；SDK、测试工具和VMware不随产品分发。

## 历史0.3.0-alpha.1

M13自包含NETCore/WindowsDesktop、System.Management及传递System.CodeDom均为10.0.13。实际官方NuGet元数据/许可和共享声明的正文核对结果（仅换行差异）见[M13记录](docs/V03-M13-LOCAL-READINESS.md)；随包保留去重后的完整第三方声明及对应组件MIT/nuspec。旧10.0.12表保留历史包身份，不代表新包版本。SDK、xUnit/测试平台、Python/PowerShell/Git、测试驱动和VMware不随产品分发；新包实际依赖由deps/runtimeconfig/BUILD-INFO记录。

## 历史本地验收包实际应用依赖

| 组件 | 实际版本 | 来源/许可证据 | 随包材料 |
| --- | --- | --- | --- |
| Microsoft.NETCore.App.Runtime.win-x64 | 10.0.12 | NuGet 官方包，LICENSE.TXT 为 MIT；.NET Foundation and Contributors | third-party/NETCore-LICENSE.txt、NETCore-THIRD-PARTY-NOTICES.txt |
| Microsoft.WindowsDesktop.App.Runtime.win-x64 | 10.0.12 | NuGet 官方 nuspec 为 MIT，包内 LICENSE 为 MIT | third-party/WindowsDesktop-LICENSE.txt |
| System.Management | 10.0.12 | Microsoft 官方包 nuspec 的 MIT 表达式与版权字段 | third-party/System.Management.nuspec；共享 third-party/NETCore-THIRD-PARTY-NOTICES.txt |
| System.CodeDom（传递依赖） | 10.0.12 | Microsoft 官方包 nuspec 的 MIT 表达式与版权字段 | third-party/System.CodeDom.nuspec；共享 third-party/NETCore-THIRD-PARTY-NOTICES.txt |

运行时第三方声明可能包含各自不同的许可；保留官方完整声明，不将它们一概改称原创 MIT。SDK 的开发工具许可不作为上述运行时包的许可依据，SDK 不随应用分发。

2026-10-17的GitHub CI按global.json允许的latestPatch实际使用SDK10.0.402、NETCore/WindowsDesktop Runtime10.0.13；System.Management和System.CodeDom仍为源码固定10.0.12。上表10.0.12保留本地5de验收包身份，不代表所有CI包的运行时版本。实际CI包以BUILD-INFO.json和runtimeconfig为准，客户机验收不自动继承。

已将下载CI包中的NETCore-LICENSE.txt、NETCore-THIRD-PARTY-NOTICES.txt与[官方runtime v10.0.13许可](https://github.com/dotnet/runtime/blob/v10.0.13/LICENSE.TXT)及[完整声明](https://github.com/dotnet/runtime/blob/v10.0.13/THIRD-PARTY-NOTICES.TXT)逐字节核对一致；官方WindowsDesktop10.0.13 NuGet元数据许可为MIT，随包保留其MIT正文。现有去重许可文本可继续覆盖该补丁，无需重复添加同一声明。官方[10.0.13发布记录](https://github.com/dotnet/core/blob/main/release-notes/10.0/10.0.13/10.0.13.md)日期为2026-10-13；本机工具未因此升级。

2026-10-10 核对 NETCore、System.Management、System.CodeDom 的第三方声明内容一致后，按用户要求仅保留一份完整 NETCore-THIRD-PARTY-NOTICES.txt，并由上表三个组件共享引用。各组件的 MIT/版权声明及 nuspec 仍分别保留；没有删去声明正文。

## 开发与测试

xUnit v3 4.0.1 使用 Apache-2.0，开发/测试依赖不随应用包分发。测试运行设置 TESTINGPLATFORM_TELEMETRY_OPTOUT=1。正式包检查不包含 xUnit、测试平台、ApplicationInsights 或测试数据。

GitHub Action 来自官方 checkout v7.0.1、setup-dotnet v6.0.0、upload-artifact v7.0.2；工作流固定对应提交摘要，不把其源码或 Node 运行时随桌面应用分发。

不附带 VMware 程序、Windows 镜像、硬件标识、真实 VMX/磁盘/快照或个人报告。

## 官方入口

- [.NET 10 下载与受维护版本](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)
- [System.Management 官方包](https://www.nuget.org/packages/System.Management/10.0.12)
- [System.CodeDom 官方包](https://www.nuget.org/packages/System.CodeDom/10.0.12)
- [xUnit v3 4.0.1 发布说明](https://xunit.net/releases/v3/4.0.1)
- [xUnit 包与许可证](https://www.nuget.org/packages/xunit.v3/4.0.1)

初始核查日期：2026-10-09；旧CI实际补丁与许可复核：2026-10-17；v0.2.0-alpha.1已于2026-10-18公开预发布。M13本版尚未远程CI或发布；实际发布仍按具体包核对版本、维护补丁与官方声明。
