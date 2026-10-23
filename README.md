# VM Identity Lab

[English](README.en.md)

VMware Workstation 虚拟化特征分析、兼容性诊断与配置管理工具。客户机测试基线为 Windows 10/11 x64、VMware Workstation Pro >=17.5.2；采用 .NET 10 LTS 与 PowerShell 7.6 LTS，MCP测试工具Python >=3.14。完整要求见[依赖清单](docs/DEPENDENCIES.md)。

## 当前版本

**0.3.0-alpha.1 已于2026-10-23公开预发布**：[下载](https://github.com/Aleka579/VMIL/releases/tag/v0.3.0-alpha.1)。tag固定指向[26656db](https://github.com/Aleka579/VMIL/tree/v0.3.0-alpha.1)，其[真实CI](https://github.com/Aleka579/VMIL/actions/runs/39616530060)通过240单元/206模拟集成，Release零警告/错误。确切原CI包通过Win11 Build26100普通权限宿主14项CLI/PNG和43项双语GUI，以及[Win10教育版Build18363/x64客户机28/50项](docs/V03-M13-CI-GUEST-ACCEPTANCE.md)；413文件/原ZIP测试后不变，临时目录和凭据已清理。[M13](tasks/modules/M13-v03-delivery.md)完成5/5，三份服务器资产大小/SHA及标签已核对，见[发布回执](docs/GITHUB-RELEASE-v0.3.0-alpha.1.md)。旧[0.2.0-alpha.1](https://github.com/Aleka579/VMIL/releases/tag/v0.2.0-alpha.1)保留原范围；全新OS、其他Build/拓扑及联网不因本次验收扩大声明。

v0.3 的[完整计划](docs/V03-PLAN.md)与[M08 静态可行性调研](docs/V03-M08-FEASIBILITY.md)已完成；[M09 只读身份分析](docs/V03-M09-RESULTS.md)功能及声明环境补充验收完成：普通权限CLI默认14项/显式17项、双语GUI、[实际CI](https://github.com/Aleka579/VMIL/actions/runs/39196591655)140单元/113模拟集成、新原包宿主9/9和现有Win10客户机28/28通过。[验收记录](docs/V03-M09-CI-GUEST-ACCEPTANCE.md)分别保留首次PNG失败、局部修复、最终原包与清理。可选UUID/MAC/BIOS序列号默认不读取，选择后仍仅输出脱敏结果，报告保持1.0。M09功能已随本次v0.3预发布交付，旧e27543f包不包含该功能；全新OS未验证，M09阶段没有新增身份写入。

[M10 实验计划与预览](docs/V03-M10-RESULTS.md)原阶段完成5/5及本机合成验收：Release零警告/错误、164单元/115模拟集成、普通权限双语GUI20项通过。配置页可审阅MAC唯一性、UUID复制生命周期的意图、风险、恢复和准入缺口，导出脱敏计划JSON；候选不可执行，固定标记“计划未执行”，内容变化清除旧审阅。现已随26656db公开源码纳入新CI，仍不在e27543f旧资产中；原阶段未执行客户机或远程CI的记录保持其历史含义。

[M11实际App验收与有限准入](docs/V03-M11-APP-ACCEPTANCE.md)完成5/5：已有静态MAC/BIOS UUID各经App应用、退出重开历史、客户机核对、受控恢复和原值核对，客户机75/75、收据36/36通过。默认策略仅允许Workstation17.5.2.23775571/hardware19、已有有效两标识及单个静态hostonly且启动断开的网卡，一次一个字段；仍须可靠关机、明确确认、备份、intent和独立复核。该轮199单元/192集成、正常App双语确认8/8通过，副本删除、原VM恢复挂起；这是M11阶段验收，最终产品CI和发布见上方。

[M12三阶段身份验证](docs/V03-M12-REAL-ACCEPTANCE.md)已完成有限范围验收5/5，副本已删除、原VM恢复挂起。可显式保存私有MAC/UUID快照、选择网络接口GUID，导入前/后/恢复后三组输入及配对配置记录，分别查看配置、Windows观测与VMIL分类，导出脱敏JSON/HTML。237单元/200集成、六份真实客户机快照150/150、四收据36/36、宿主及Win10客户机双语/脱敏各34/34通过。分类仍保留VMware证据，联网未测试；私有快照不能公开上传，旧报告标识仍不可比较，M11写入范围不变；最终CI和公开预发布见上方。

[MAC/UUID官方补证](docs/V03-M11-MAC-UUID-EVIDENCE.md)、[精确构建研究](docs/V03-M11-REAL-API-RETEST.md)与上述App/客户机实测分别提供依据；[配置政策](docs/VMWARE-CONFIG-POLICY.md)明确值格式及恢复边界。其他构建、自动地址、联网/多网卡、缺失键、厂商生成/复制仍未准入。MAC/UUID改变后其他虚拟化证据仍在，不保证第三方程序将环境识别为物理机。

历史本地 Alpha 快照 `artifacts/releases/VMIdentityLab-0.2.0-alpha.1-win-x64.zip` 来源9cfa28a，保留其只读范围及[原包验收](docs/V02-ACCEPTANCE-RESULTS-2026-10-14.md)；5de4ec4 本地验证包另存 `artifacts/test-packages/VMIdentityLab-5de4ec4-validation-20261017.zip`。它们与已公开的 e27543f 原 CI 包分别记录，不覆盖或重标历史包。

## v0.1 已有交付

本项目按八个功能模块开发。M00–M05的四层工程、报告、VMX只读解析、对比、Windows检测和五页WPF已实现；Release构建、81项单元/10项文件与流程、真实宿主/客户机报告闭环、17.5.2支持分类与电源四态验证通过。Win10教育版18363客户机按用户确认的无需开发SDK/系统.NET口径通过普通权限完整验证包验收；v0.1为25/25，总体29/37。该v0.1交付版本为 **0.1.0-alpha.2**，包含GBK修复和可选本机API状态查询，仍为本地预览。旧0.1.0-alpha.1 ZIP保留修复前构建；2026-10-11验证包保留原构建标识，不追改历史版本。版本记录见[CHANGELOG.md](CHANGELOG.md)；实际环境验证范围见[宿主结果](docs/HOST-TEST-RESULTS-2026-10-11.md)、[客户机结果](docs/GUEST-TEST-RESULTS-2026-10-11.md)和[本轮结果](docs/READINESS-TEST-RESULTS-2026-10-11.md)。

已有v0.1本地包：`artifacts/releases/VMIdentityLab-0.1.0-alpha.2-win-x64.zip`，配套`.zip.sha256`；完整目录为`artifacts/publish/0.1.0-alpha.2-win-x64/`。包内`BUILD-INFO.json`记录源码提交ab177a7，保留v0.1标识；不能将该ZIP重标为v0.2。

## 启动与采集

运行完整自包含目录内的 VMIdentityLab.App.exe。五页提供概览、检测、配置声明查看、报告对比和导出；左下角可切换中英文。软件用户无需开发SDK或系统.NET，已在声明的Win10目标实测。VMX页可显式连接已配置的本机官方API查询电源状态；不连接时为未知，不自动操作VM。

```powershell
.\VMIdentityLab.App.exe --collect --output .\guest-report.json --html .\guest-report.html
```

同一引擎在宿主机/客户机分别采集，手动传回 JSON 再导入比较。CLI 拒绝覆盖已有输出；GUI 明确确认后才替换已有报告。详见 [中文指南](docs/USER-GUIDE.md)与[英文指南](docs/USER-GUIDE.en.md)。

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

- [完整计划](PLAN.md)：原 v0.1/v0.2 路线；[v0.3 计划](docs/V03-PLAN.md)与[M08 结论](docs/V03-M08-FEASIBILITY.md)单列。
- [模块开发顺序](docs/MODULES.md)：既有 M00–M07 及规划中 M08–M13 的职责、依赖与验收归属。
- [Codex 总控指令](CODEX_START.md)：复制到新的开发任务时使用。
- [历史验收索引](tasks/PROGRESS.md)：早期37条验收与证据入口；本版状态见上方M13。
- [公开交接摘要](WORK_LOG.md)：早期脱敏历史摘要，当前验收见本版结果文档。
- [公共协作指引](AGENTS.md)：已公开的脱敏指引；本机内部日志和授权记录不随发布上传。
- [发布就绪表](RELEASE-READINESS.md)：构建、测试和发布状态。

## 版本路线

v0.1 完成只读检测、VMX 查看、宿主机/客户机报告对比、脱敏 JSON/HTML 和 WPF 界面。v0.2 再加入版本明确、经过验证的有限配置管理。

v0.3 已完成M08–M12对应范围验收：身份分析、未执行实验计划、已有MAC/UUID有限应用恢复及三阶段验证。M13先完成本地候选，实际CI、最终CI原包目标验收及发布仍待后续授权；新字段/版本仍须独立证据和恢复验证。

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

宿主机OS版本不设门槛。最终 e27543f CI 包使用 SDK10.0.402/Runtime10.0.13；VMware17.5.2、项目内Python3.14.8/MCP SDK1.30.0及7个读取工具的既有记录保留。当前包在 Win10 教育版18363 x64 的无需SDK/系统.NET目标通过；该客户机已有其他软件，非全新OS，Win11及其他构建尚未验证。精确有限配置实测与旧包保留各自范围，见[兼容性](docs/COMPATIBILITY.md)。安全问题请使用[私密报告入口](https://github.com/Aleka579/VMIL/security/advisories/new)，维护者Daniel Waltz。

## 许可与开源准备

原创源码采用 [MIT](LICENSE)，Copyright (c) 2026 Daniel Waltz，已由用户确认。第三方声明见 [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md)。[私密安全报告渠道](https://github.com/Aleka579/VMIL/security/advisories/new)已开启，由Daniel Waltz维护；正式就绪程度以 [RELEASE-READINESS.md](RELEASE-READINESS.md) 为准。

