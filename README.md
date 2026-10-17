# VM Identity Lab

[English](README.en.md)

VMware Workstation 虚拟化特征分析、兼容性诊断与配置管理工具。客户机测试基线为 Windows 10/11 x64、VMware Workstation Pro >=17.5.2；采用 .NET 10 LTS 与 PowerShell 7.6 LTS，MCP测试工具Python >=3.14。完整要求见[依赖清单](docs/DEPENDENCIES.md)。

## 当前开发

当前源码为 **0.2.0-alpha.1**：官方API配置管理及实际App应用、退出重开历史、参数恢复闭环已通过，配置验收6/6，总计35/37。2026-10-17 Release零警告/错误、127单元/100模拟集成，以及最新包客户机普通权限/双语/报告/包内运行时十六项检查通过。配置仅支持登记版本17.5.2.23775571、hardware19、每插槽2核及4096↔6144MiB、2↔4vCPU；状态/内容冲突和未决结果保全已验证，不宣称全局VMware锁。远程CI和私密安全渠道尚未完成，正式发布未就绪，见[全量测试与限制](docs/V02-FULL-TEST-RESULTS-2026-10-17.md)。

既有Alpha快照为 `artifacts/releases/VMIdentityLab-0.2.0-alpha.1-win-x64.zip`，来源9cfa28a，保留其只读范围及[原包验收](docs/V02-ACCEPTANCE-RESULTS-2026-10-14.md)，不包含最新后端。已验收的5de4ec4新本地验证包另存`artifacts/test-packages/VMIdentityLab-5de4ec4-validation-20261017.zip`及校验文件；未创建公开Release，历史包不覆盖。

## v0.1 已有交付

本项目按八个功能模块开发。M00–M05的四层工程、报告、VMX只读解析、对比、Windows检测和五页WPF已实现；Release构建、81项单元/10项文件与流程、真实宿主/客户机报告闭环、17.5.2支持分类与电源四态验证通过。Win10教育版18363客户机按用户确认的无需开发SDK/系统.NET口径通过普通权限完整验证包验收；v0.1为25/25，总体29/37。该v0.1交付版本为 **0.1.0-alpha.2**，包含GBK修复和可选本机API状态查询，仍为本地预览。旧0.1.0-alpha.1 ZIP保留修复前构建；2026-10-11验证包保留原构建标识，不追改历史版本。版本记录见[CHANGELOG.md](CHANGELOG.md)；实际环境验证范围见[宿主结果](docs/HOST-TEST-RESULTS-2026-10-11.md)、[客户机结果](docs/GUEST-TEST-RESULTS-2026-10-11.md)和[本轮结果](docs/READINESS-TEST-RESULTS-2026-10-11.md)。

已有v0.1本地包：`artifacts/releases/VMIdentityLab-0.1.0-alpha.2-win-x64.zip`，配套`.zip.sha256`；完整目录为`artifacts/publish/0.1.0-alpha.2-win-x64/`。包内`BUILD-INFO.json`记录源码提交ab177a7，保留v0.1标识；不能将该ZIP重标为v0.2。

## 启动与采集

运行完整自包含目录内的 VMIdentityLab.App.exe。五页提供概览、检测、配置声明查看、报告对比和导出；左下角可切换中英文。软件用户无需开发SDK或系统.NET，已在声明的Win10目标实测。VMX页可显式连接已配置的本机官方API查询电源状态；不连接时为未知，不自动操作VM。

```powershell
.\VMIdentityLab.App.exe --collect --output .\guest-report.json --html .\guest-report.html
```

同一引擎在宿主机/客户机分别采集，手动传回 JSON 再导入比较。CLI 拒绝覆盖已有输出；GUI 明确确认后才替换已有报告。详见 [使用指南](docs/USER-GUIDE.md)。

## 构建与测试

使用 global.json 指定的 .NET 10 稳定 SDK；本项目当前本机工具位于忽略的 artifacts/tooling/dotnet/。

```powershell
dotnet build VMIdentityLab.sln -c Release -m:1 --disable-build-servers
dotnet test --project tests/VMIdentityLab.UnitTests/VMIdentityLab.UnitTests.csproj -c Release --parallel none
dotnet test --project tests/VMIdentityLab.IntegrationTests/VMIdentityLab.IntegrationTests.csproj -c Release --parallel none
dotnet publish src/VMIdentityLab.App/VMIdentityLab.App.csproj -c Release -r win-x64 --self-contained true -p:DebugType=None -p:DebugSymbols=false -o artifacts/publish/win-x64
```

开发验证设置 DOTNET_CLI_TELEMETRY_OPTOUT=1、TESTINGPLATFORM_TELEMETRY_OPTOUT=1。测试只使用合成数据和独立临时文件；真实 VM 动作单独确认。

## 从这里开始

- [完整计划](PLAN.md)：目标、版本、技术路线、阶段、授权与交付。
- [模块开发顺序](docs/MODULES.md)：M00–M07 的职责、接口、依赖与验收归属。
- [Codex 总控指令](CODEX_START.md)：复制到新的开发任务时使用。
- [阶段验收索引](tasks/PROGRESS.md)：37 条验收与证据入口。
- [项目工作日志](WORK_LOG.md)：实际状态和本轮文件生成记录。
- [项目指令](AGENTS.md)：恢复工作、测试、隐私和授权规则。
- [发布就绪表](RELEASE-READINESS.md)：构建、测试和发布状态。

## 两个版本

v0.1 完成只读检测、VMX 查看、宿主机/客户机报告对比、脱敏 JSON/HTML 和 WPF 界面。v0.2 再加入版本明确、经过验证的有限配置管理。

客户机采集采用用户手动运行和报告导入；配置管理要求可靠确认完全关机，并先在独立副本测试。

## 文档入口

- [需求与验收映射](docs/REQUIREMENTS.md)
- [架构与数据流程](docs/ARCHITECTURE.md)
- [兼容性矩阵](docs/COMPATIBILITY.md)
- [全部依赖与版本要求](docs/DEPENDENCIES.md)
- [本地环境配置与接入状态](docs/ENVIRONMENT-SETUP.md)
- [报告格式与隐私](docs/REPORT-SCHEMA.md)
- [VMware 配置规则](docs/VMWARE-CONFIG-POLICY.md)
- [17.5.2只读配置与状态依据](docs/VMWARE-17.5.2-EVIDENCE.md)
- [安全设计](docs/SECURITY.md)
- [测试计划](docs/TEST-PLAN.md)

## 使用范围

面向 Windows 开发者、测试人员和 VMware 用户，用于诊断、比较与兼容性配置管理。产品不以绕过第三方反作弊、反欺诈、授权或设备封禁为目标。

客户机允许Windows 10/11 x64，VMware最低17.5.2，属于用户指定的测试兼容基线；官方支持与实测兼容分别记录。其他SDK、脚本工具、依赖和CI继续核查维护期，临期版本不作为新采用方案。

宿主机OS版本不设门槛。SDK10.0.401、Runtime10.0.12、VMware17.5.2与项目内Python3.14.8/MCP SDK1.30.0保留；7个读取工具已接入。最新5de验证包客户机和无需SDK/系统.NET目标、精确有限配置范围均已验收；该客户机已有其他软件，非全新OS。旧包与各日期证据保留原范围，远程CI及私密安全渠道仍待完成。

## 许可与开源准备

原创源码采用 [MIT](LICENSE)，Copyright (c) 2026 Daniel Waltz，已由用户确认。第三方声明见 [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md)。安全报告渠道仍待配置；正式就绪程度以 [RELEASE-READINESS.md](RELEASE-READINESS.md) 为准。

