# 架构设计

状态：按 [MODULES.md](MODULES.md) 的八个功能模块在四层项目内实现；基础工程与报告模块已开始实际验证。阶段结果见 [PROGRESS.md](../tasks/PROGRESS.md)，需求入口为 [REQUIREMENTS.md](REQUIREMENTS.md)。

## 分层

| 模块 | 职责 | 依赖 |
| --- | --- | --- |
| VMIdentityLab.Core | 报告、采集状态、证据解释、比较、脱敏、配置变更模型 | .NET 标准平台功能，不依赖 WPF 或 VMware 安装 |
| VMIdentityLab.Windows | SMBIOS、CPU、PnP、Tools、系统信息采集 | Core、必要 Windows API |
| VMIdentityLab.VMware | VMX 保留式解析、状态探测、支持规则、受控写入和恢复 | Core、实际验证的 VMware 工具 |
| VMIdentityLab.App | WPF/MVVM、后台任务、文件选择、查看、导入、导出、采集入口 | Core、Windows、VMware |

依赖方向固定，Core 不调用 App。构造和替换依赖保持直接，仅在采集接口、状态探测和文件操作边界保留可测试替身。

## 主要职责接口（计划）

- WindowsCollector：返回五类结构化观测，每项包含状态、时间和来源。
- VmxReader：读取原始字节及保留式文档，提供已解释项和未知项。
- VmPowerStateProbe：提供状态、来源、采集时间、失败原因和可靠性说明。
- ReportComparer：按格式版本、类型、单位和可见性比较两份报告。
- ReportExporter：输出脱敏 JSON 和经编码的静态 HTML。
- ConfigManager：验证规则、生成预览、备份、应用和受控恢复。

以上是实现职责，不要求引入插件系统、服务端或额外框架。

## 宿主机/客户机流程

宿主机采集 A；客户机内采集 B；用户复制 B 到宿主机；App 导入 A/B 并对比。客户机入口与 GUI 共享同一采集引擎，仅生成本地报告，不建立长期服务或网络连接。

VMX 反映配置声明，客户机报告反映实际观测。报告与 UI 明确显示两类来源，不将 guestOS 或内存设置当作实际 Windows Build 或已观测内存。

## 数据状态

采集状态使用 success、accessDenied、notSupported、timeout、failed。接口未执行或环境未确定的记录为未验证/未知，不伪造成功状态。

电源状态为 PoweredOn、PoweredOff、Suspended、Unknown。工具不存在、查询失败或证据不足时返回 Unknown。

v0.1的可选WorkstationRestPowerStateProbe由App提供请求级认证HttpClient，只向本机回环地址发送清单/电源GET；先将选定VMX核对到唯一注册ID。App禁用代理/重定向，限制响应与超时；暂停、无效响应和歧义保持Unknown。密码不保存到App设置，官方vmrest配置和启动不由App自动执行。WorkstationEnvironment只读安装元数据；VmxSupportCatalog的目标版本依据和未核实项独立于可写白名单。实际范围见[17.5.2依据](VMWARE-17.5.2-EVIDENCE.md)。

报告模型见 [REPORT-SCHEMA.md](REPORT-SCHEMA.md)。用户敏感值只在必要本地处理范围存在，默认日志和导出不含原值。

## VMX 保留式处理

解析模型保留原始字节、编码、换行、顺序、注释和未知行。零修改时字节完全相同。可写键重复、语法无法确认、加密输入或版本不支持时配置管理禁止写入。

写入以预览时原内容为基线；用户确认后再次核实。v0.2的`IVmxConfigTransaction`由`VmxApiConfigManager`实现生产路径，经官方API更新有限字段；原字节备份和预期配置先新建并验证，不回落直接替换VMX。恢复依据收据仅反向修改本次参数，保留当前其他内容。

同目标文件租约协调App实例，持久化intent阻止未决操作重放；两者不构成VMware全局锁。成功收据经原工作流保存到版本化私有历史，保存失败只重试历史。2026-10-17真实provider及产品副本闭环通过后，仅批准登记版本17.5.2.23775571/hardware19/每插槽2核、4096↔6144MiB和2↔4vCPU；未评审范围及旧文件写入仍拒绝，详见[提交设计](CONFIGURATION-WRITE-DESIGN.md)与[实际验收](V02-FULL-TEST-RESULTS-2026-10-17.md)。

参见 [VMWARE-CONFIG-POLICY.md](VMWARE-CONFIG-POLICY.md)。

## 响应与权限

采集与文件操作不运行在 WPF UI 线程；展示进度、取消和分项失败。超时不能被隐藏成健康结论。普通权限为默认，特定采集不足时显示具体原因，不整体自动提权。

## 边界与构建

宿主OS不设版本门槛；客户机及干净目标允许Windows 10/11 x64，VMware最低17.5.2，独立MCP测试工具Python最低3.14。完整依赖见[DEPENDENCIES.md](DEPENDENCIES.md)。自包含发布不自动证明兼容性；构建使用global.json固定SDK，Core/Windows/VMware自动化测试与UI/真实VM测试分别记录。

不打包 VMware 程序或 Windows 镜像；真实环境资料留在私有位置。

