# M09：只读身份分析实现与验收

- 日期：2026-10-18，Asia/Taipei（UTC+08:00）。
- 起点：`08c540d`，分支 `codex/v03-m08-20261018`，沿用 M08 worktree。
- 状态：M09 开发与本机普通权限功能验收完成，模块验收 5/5；2026-10-19 的 [CI 原包与客户机补充验收](V03-M09-CI-GUEST-ACCEPTANCE.md)另列实际来源和结果。全新 OS 与 v0.3 发布仍未完成。
- 下方为 2026-10-18 原实现验收：当时未读取真实 VMX、未调用 VMware API/客户机命令、未操作 VM 配置/电源/快照/完整副本；只使用合成 VMX 和合成旧格式报告。
- 原本机实际目标：Windows 11 家庭版中文版，Build26100，x64；不将其结果外推到客户机。

## 实际功能

检测详情现在包含“系统观测”和“身份分析”两个页签。身份分析固定列出 17 个已审查字段：原有 14 项和 3 项可选标识。每行分别呈现 VMX 声明、当前报告观测、采集状态、项目修改支持；选中行可查看来源、观测时间和依据。

没有采集的项显示“未采集”，预期接口注明尚未执行；不能将其当作读取成功、标识不存在或物理机证明。失败、超时、拒绝访问仍沿用现有明确状态。修改支持仅描述项目已有能力，不判断当前目标已获准写入。

| 可选标识 | 只读来源 | 返回规则 |
| --- | --- | --- |
| UUID | `Win32_ComputerSystemProduct.UUID` | 空、无法解析、全零/全一 UUID 为不可用；成功值在返回报告前脱敏 |
| BIOS 序列号 | `Win32_BIOS.SerialNumber` | 与系统厂商/型号分开；空值为不可用，不推断硬件唯一性 |
| MAC 地址集合 | `.NET NetworkInterface.GetAllNetworkInterfaces/GetPhysicalAddress` | 非回环/隧道接口的非全零六字节地址，排序去重；可能包含多张实体或虚拟网卡，不绑定某张 VM 网卡 |

新增原始标识默认不读取。GUI 必须勾选“本地读取 UUID、MAC 和 BIOS 序列号”，再点击采集；CLI 必须显式使用 `--include-identifiers`。此选项仅保留在当前 App 会话，不保存到设置。界面和 JSON/HTML 均保持脱敏，当前功能不提供原始标识导出或跨报告的原值匹配。

原始标识只在采集过程中暂存，不写入日志、报告或界面；这不承诺托管内存的安全擦除。对比器继续把脱敏值判为不可比较，采集状态变化单列，不能把相同掩码解释成相同硬件。

## 配置声明的处理

- 复用 VMX 解析器的只读结果；`numvcpus` 对应处理器声明，`memsize` 保留 MiB 单位，`guestOS` 仅是客户机类型声明，不冒充实际 OS 名称或 Build。
- 对已记录的 UUID/MAC 声明只返回“存在声明，已脱敏”；不返回原值。重复键、空值或无法解释的内容保持歧义/无法确认状态。
- MAC 声明可能涉及多张网卡及非当前使用地址，不从字段存在推导当前生效地址。
- 普通报告的 VMX 摘要仍只导出原来的公开键；重新导入时，没有敏感声明信息就显示“摘要未包含声明”。它不表示原 VMX 一定没有该配置。
- 所选 VMX 与当前系统报告没有建立目标身份对应，界面与 HTML 明确提示分别观察，不自动判定匹配或不匹配。

写入策略没有变化：仅保留原有精确内存/vCPU范围；MAC/UUID 仍是研究候选，新增获准身份写入为 0。

## 报告兼容和隐私

报告仍使用 **schema 1.0**，未新增顶层属性或枚举值。未选择敏感采集时保持原 14 项；选择后报告增加三个已有目录 ID 的脱敏结果。身份分析表是由报告/所选声明推导的视图，不把“未采集”写成成功结果。

新报告为三个标识记录真实的已审查接口来源。旧报告中的通用 `identifier` 来源原样保留并提示无法确认具体接口；不重标为新的 WMI/.NET 数据。未审查来源变为 `unverified`，不泄漏输入路径。旧 CPU 的 `Win32_Processor.Name` 来源继续保留。

JSON 导出、导入和 HTML 均通过现有隐私层。HTML 增加身份能力分析，编码不可信字符串，不加载远程资源。能力依据为 M08 的版本化结论，不从检测成功推导修改支持。

## 实际验证结果

| 检查 | 实际结果 | 证据与范围 |
| --- | --- | --- |
| 完整 Release 构建 | 通过，0 警告/0 错误 | SDK10.0.401；正常还原及 NuGet 审计，未改依赖或检查规则 |
| 单元测试 | **140/140**，无跳过 | `artifacts/test-results/20261018-m09/m09-unit-initial.trx` |
| 模拟/文件集成测试 | **113/113**，无跳过 | 同目录 `m09-files-initial.trx`；无真实 VM 操作 |
| 默认 CLI | 退出0，14项，`elevated=false` | 私有 `local-data/m09-20261018/host-default.*` |
| 显式选择 CLI | 退出0，17项，三个新来源均 success 且仅 Redacted | 私有 `host-opt-in.*`、`cli-check.json` |
| 真实本机 GUI | 38步核心操作检查通过，11份功能快照 | 私有 `gui-check.json`、`ui-actions.jsonl`；默认Off、显式读取、17行、双语、取消、合成VMX和旧格式导入 |
| 最终 App 构建 | 通过，0 警告/0 错误 | 撤回未证实的预览试修后重新构建；采集/模型/测试代码与TRX对应版本一致 |
| 客户机 / 干净 OS / 新远程 CI | 原 2026-10-18 本机验收未执行 | 后续见 [CI 原包补充记录](V03-M09-CI-GUEST-ACCEPTANCE.md)，不引用旧 v0.2 的23/23填补 |

使用的命令在本 worktree 执行，复用已有项目内 SDK 和包缓存，限制并行：

```powershell
dotnet build VMIdentityLab.sln -c Release -m:1 --disable-build-servers --tl:off
dotnet test --project tests/VMIdentityLab.UnitTests/VMIdentityLab.UnitTests.csproj -c Release --no-build --no-restore --parallel none --report-xunit-trx --report-xunit-trx-filename m09-unit-initial.trx --results-directory artifacts/test-results/20261018-m09
dotnet test --project tests/VMIdentityLab.IntegrationTests/VMIdentityLab.IntegrationTests.csproj -c Release --no-build --no-restore --parallel none --report-xunit-trx --report-xunit-trx-filename m09-files-initial.trx --results-directory artifacts/test-results/20261018-m09
```

首次沙箱构建在 NuGet 漏洞数据网络读取处以 NU1900 停止；正常联网后通过，没有禁用审计或降低警告级别。所有真实报告、控件树和临时取证工具均在忽略目录，不提交。

## 验证中发现的问题

1. **UI 取证遗漏已解决。** DataGrid 保持行虚拟化，首轮控件快照只实现了前8行，不能据此断言 UUID 已显示。测试驱动新增标准 ScrollPattern 操作和 Toggle 状态记录，滚动后补查三个敏感字段，再完成检查；原快照与失败记录保留。
2. **离屏 PNG 后续修复。** 上轮仅依据查看工具的缺字/透明显示，未完成定位；本轮独立解码与原生像素检查纠正了部分显示判断，并修复真实存在的内容尺寸和透明边界问题。24张双语/六视图独立进程回归、自包含包本机检查已通过；详见[PNG修复与补充验收](V03-M09-PREVIEW-ACCEPTANCE.md)。正常GUI渲染设置不变；原失败记录保留，客户机及新CI结果另列。

## 主要文件与下一步

- Core：[IdentityAnalysis.cs](../src/VMIdentityLab.Core/Reports/IdentityAnalysis.cs)、字段目录与隐私来源处理。
- Windows：可选 `Identifiers` 类别及三个实际来源，复用原超时、串行门和取消管线。
- VMware：[VmxIdentityReader.cs](../src/VMIdentityLab.VMware/VmxIdentityReader.cs)，只读、仅返回敏感声明存在性。
- App：既有检测页、双语资源、CLI 参数及相同采集引擎；未新增项目、驱动、服务或第三方依赖。
- 测试：[IdentityAnalysisTests.cs](../tests/VMIdentityLab.UnitTests/IdentityAnalysisTests.cs)、[IdentityCollectionTests.cs](../tests/VMIdentityLab.IntegrationTests/IdentityCollectionTests.cs)。

应用版本入口仍为 `0.2.0-alpha.1`，此 worktree 是未发布的 M09 开发构建，不能与已发布 e27543f 原包混同；版本升级、最终包和发布属于 M13。后续可以按新请求开启 M10。M11 新字段写入仍需补足 M08 记录的目标版本依据、授权副本与恢复证据，本轮未推进 M10–M13。

## 只读来源依据

- [Microsoft Win32_ComputerSystemProduct](https://learn.microsoft.com/en-us/windows/win32/cimwin32prov/win32-computersystemproduct)：UUID 属性及 SMBIOS 来源。
- [Microsoft Win32_BIOS](https://learn.microsoft.com/en-us/windows/win32/cimwin32prov/win32-bios)：BIOS 序列号属性。
- [Microsoft NetworkInterface.GetPhysicalAddress](https://learn.microsoft.com/en-us/dotnet/api/system.net.networkinformation.networkinterface.getphysicaladdress?view=net-10.0)：网络适配器地址读取。
- [M08 修改能力依据与限制](V03-M08-FEASIBILITY.md)：只读接口说明不构成 VMware 修改能力证明。
