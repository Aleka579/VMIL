# M10：身份实验计划与预览验收

日期：2026-10-19，Asia/Taipei（UTC+08:00）。在 `codex/v03-m10-20261019` 独立 worktree 实施，基线 `a4e3a8b`；本轮源码属于该基线之上的 M10 开发改动。程序集版本仍为 `0.2.0-alpha.1`，没有发布新包。

**模块验收 5/5 完成。** Release 构建 0 警告/0 错误，164/164 单元测试、115/115 模拟/临时文件集成测试通过，无跳过；本机普通权限合成数据桌面检查 20/20。真实 VM、客户机、全新 OS 和本轮远程 CI 未执行。没有新增身份写入批准。

## 实际交付

| 部分 | 实现与范围 |
| --- | --- |
| 闭合候选目录 | MAC 唯一性管理、UUID 复制生命周期；不能传入自定义参数、脚本、MAC 或 UUID |
| 可审查信息 | 字段语义、目标完整版本、虚拟硬件版本、研究基线、官方链接/核查日期、值约束、风险、恢复范围及准入缺口 |
| 差异 | 声明状态与拟变更意图分列；原标识只显示脱敏/缺失/无效/歧义状态，不分配新地址或 UUID |
| 界面 | 配置页的“身份实验计划 · 仅预览”，中英文资源；报告页可导出已审阅的计划 |
| 失效 | 绑定目标路径、完整文件内容、Workstation 版本、已有状态观测及候选；切换目标/候选或读取新状态会清除审阅，导出前重读文件/版本 |
| 导出 | 独立 JSON 报告 `vmil.identity-plan/1`；固定 `status=notExecuted`、`canExecute=false`，拒绝覆盖已有文件和非 `.json` 目标 |

审阅复选框仅表示已读当前计划，用于导出，不是写入授权。系统诊断报告仍为 1.0；计划 JSON 不能由原报告入口导入，也没有配置计划导入或执行入口。

电源状态保留其原观测时间，界面标注“非实时”；文件重读不冒充电源查询。本模块不持续监控外部 VM 电源，未来 M11 的执行仍需重新核实状态及全部准入条件。

## 依据与准入

2026-10-19 已重新读取 [Broadcom KB324973](https://knowledge.broadcom.com/external/article/324973) 和 [KB320246 / legacy1541](https://knowledge.broadcom.com/external/article?legacyId=1541)。前者列举旧版 hosted 产品，后者混合 Workstation 描述与 ESXi 环境列表；均不足以批准目标 `17.5.2.23775571` 的本项目身份写入。沿用 [M08 结论](V03-M08-FEASIBILITY.md)。

即使版本与研究基线相同、输入语法正确且已有观测为可靠关机，两候选仍明确缺少：逐字段目标版本依据、允许值/关联约束、官方通道及并发语义、恢复验证、独立副本往返和生产写入批准。版本未知/错配、非可靠关机、重复/不支持配置、缺失/非法标识及多网卡声明另列缺口。基本语法检查不等于厂商值范围或有效身份验证。

## 架构决定

- `Core/Experiments/IdentityExperimentCatalog.cs` 维护登记候选、双语说明和缺口定义。
- `VMware/Configuration/IdentityExperimentPlan.cs` 生成不可执行计划、比较上下文并导出脱敏 JSON；不产生拟写 VMX 字节，不调用 VMware API。
- `App/ViewModels/MainViewModel.Experiments.cs` 组合现有 VMX 读取、审阅与文件选择；复用差异行和 `ReportFiles` 新文件保存。MAC 键识别复用 M09 读取器。
- M07 的 `VmxConfigPlan` 带有用于 CPU/内存事务的拟写字节，M10 的意图不能冒充该类型。生产事务、有限白名单、四个工程及依赖均未扩展。

## 实际验证

环境：宿主 Windows Build `26100.7623` / 24H2 / `CoreCountrySpecific`，普通用户；既有本地 SDK `10.0.401`、运行时 `10.0.12`。使用合成 VMX 和本轮新输出，不采集宿主硬件或连接 VMware 服务。

```powershell
dotnet build VMIdentityLab.sln -c Release -m:1 --disable-build-servers
dotnet test --project tests/VMIdentityLab.UnitTests/VMIdentityLab.UnitTests.csproj -c Release --no-build --parallel none --report-xunit-trx --report-xunit-trx-filename m10-unit.trx --results-directory artifacts/test-results/20261019-m10
dotnet test --project tests/VMIdentityLab.IntegrationTests/VMIdentityLab.IntegrationTests.csproj -c Release --no-build --parallel none --report-xunit-trx --report-xunit-trx-filename m10-files.trx --results-directory artifacts/test-results/20261019-m10
```

初次构建发现新 partial 文件的 `Localization` 与 WPF 同名，显式别名修正后以 `--no-restore` 重建通过；保留首轮失败日志，没有关闭警告或审计。新增 24 个单元用例、2 个文件集成测试；164/115 包含既有回归。TRX 位于 `artifacts/test-results/20261019-m10/`。

桌面验收包含 95 次驱动调用和 39 份 UIA 快照：93 次调用成功，另两次原生保存框文件名控件引用失效已调查并恢复。每次先重取窗口及文件状态，确认仍是原保存框、默认文件名且输出未生成后继续；没有重放已经完成的保存。原失败记录保留，未因此修改产品或放宽断言。

20 项汇总检查涵盖无目标禁用、双语候选/风险/恢复、审阅前后状态、M07 审阅按钮隔离、报告页导出、MAC/UUID 独立计划、已有输出拒绝、切换候选/目标清除审阅、外部注释变化拒绝旧计划导出，以及三份 JSON 的隐私和未执行状态。只有测试脚本明确追加的合成注释改变输入文件，App 未修改配置、创建备份或事务旁文件。App `21436` 已正常关闭并确认进程不存在。

私有证据：`local-data/m10-20261019/`，包括构建/测试日志、`ui-actions.jsonl`、原生控件失败与回读、39 份控件快照、三份计划 JSON 及 `ui-acceptance-check.json`（`passed=true`、20 项）。合成夹具、私有验收脚本和输出均未进入发布包；已结束脚本使用独占输出，不原样重放。

## 完成与后续

本模块五条验收见 [M10 任务](../tasks/modules/M10-identity-profiles.md)。M11–M13 未开始；身份候选真实写入准入仍不满足。M10 不要求启动客户机或创建测试副本，当前本机结果不能代表客户机、远程 CI 或 Release 验收。
