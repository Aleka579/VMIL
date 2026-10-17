# 第三方组件与许可

原创源码采用 [MIT](LICENSE)，Copyright (c) 2026 Daniel Waltz。第三方文件保留自己的版权、许可和声明，原创 LICENSE 不替代它们。

## 实际应用依赖

| 组件 | 实际版本 | 来源/许可证据 | 随包材料 |
| --- | --- | --- | --- |
| Microsoft.NETCore.App.Runtime.win-x64 | 10.0.12 | NuGet 官方包，LICENSE.TXT 为 MIT；.NET Foundation and Contributors | third-party/NETCore-LICENSE.txt、NETCore-THIRD-PARTY-NOTICES.txt |
| Microsoft.WindowsDesktop.App.Runtime.win-x64 | 10.0.12 | NuGet 官方 nuspec 为 MIT，包内 LICENSE 为 MIT | third-party/WindowsDesktop-LICENSE.txt |
| System.Management | 10.0.12 | Microsoft 官方包 nuspec 的 MIT 表达式与版权字段 | third-party/System.Management.nuspec；共享 third-party/NETCore-THIRD-PARTY-NOTICES.txt |
| System.CodeDom（传递依赖） | 10.0.12 | Microsoft 官方包 nuspec 的 MIT 表达式与版权字段 | third-party/System.CodeDom.nuspec；共享 third-party/NETCore-THIRD-PARTY-NOTICES.txt |

运行时第三方声明可能包含各自不同的许可；保留官方完整声明，不将它们一概改称原创 MIT。SDK 的开发工具许可不作为上述运行时包的许可依据，SDK 不随应用分发。

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

核查日期：2026-10-09。实际发布时重新核对版本、补丁与官方声明；当前材料属于本地 Alpha 准备，不表示远程发布已完成。
