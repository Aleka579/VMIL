# 模块职责与开发顺序

既有 M00–M07 分布在四个源码项目内；M00–M06 组成 v0.1 只读流程，v0.2/M07 提供精确范围的官方 API 配置。v0.2 既有验收 37/37，最终 CPU 修复 CI 为 130 单元/106 模拟集成、原包客户机 23/23，已于 2026-10-18 公开 alpha.1 预发布；见 [CPU18](CPU-FIX-ACCEPTANCE-2026-10-18.md) 和 [发布记录](GITHUB-RELEASE-v0.2.0-alpha.1.md)。历史配置实测范围不扩大。

v0.3沿用四个工程：M08–M12原声明范围已验收，旧副本/临时会话已清理。M13完整5/5：[真实CI/宿主](V03-M13-CI-RESULTS.md)、[最终原包客户机验收](V03-M13-CI-GUEST-ACCEPTANCE.md)及2026-10-23[公开预发布/服务器回执](GITHUB-RELEASE-v0.3.0-alpha.1.md)均通过。产品26656db/CI39616530060、TRX240/206，Win11普通权限14/43、Win10客户机28/50，测试后413文件/原ZIP未变且清理完成。三资产大小/SHA和tag精确目标一致，main/旧发布保留。全部附加worktree已释放，全新OS等范围不扩大。

## 模块与依赖

| 顺序 | 模块 | 源码归属 | 依赖 | 任务入口 |
| --- | --- | --- | --- | --- |
| M00 | 环境与基础契约 | Core、解决方案与测试基础 | 无 | [任务](../tasks/modules/M00-foundation.md) |
| M01 | 报告与隐私 | Core/Reports | M00 | [任务](../tasks/modules/M01-reports.md) |
| M02 | VMX 只读分析 | VMware | M00、M01 | [任务](../tasks/modules/M02-vmx.md) |
| M03 | 报告对比 | Core/Comparison | M00、M01 | [任务](../tasks/modules/M03-comparison.md) |
| M04 | Windows 检测 | Windows、Core/Evidence | M00、M01 | [任务](../tasks/modules/M04-detection.md) |
| M05 | WPF 应用 | App | M01–M04 | [任务](../tasks/modules/M05-app.md) |
| M06 | v0.1 验收与交付 | 测试、构建、文档、CI | M00–M05 | [任务](../tasks/modules/M06-delivery.md) |
| M07 | v0.2 配置管理 | VMware、App | M02、已核实的 v0.1 | [任务](../tasks/modules/M07-config.md) |
| M08 | v0.3 可行性调研 | docs、tasks | M07、目标版本资料；静态调研完成 | [任务](../tasks/modules/M08-feasibility.md) |
| M09 | 只读身份分析 | Core、Windows、VMware、App | M08；功能5/5及声明环境原包补充验收完成 | [任务](../tasks/modules/M09-identity-analysis.md) |
| M10 | 实验计划与预览 | Core、VMware、App | M09；5/5及本机合成验收完成 | [任务](../tasks/modules/M10-identity-profiles.md) |
| M11 | 受控身份配置事务 | VMware、App，复用M07 | 有限已有MAC/UUID范围5/5，精确版本和隔离配置；其他范围拒绝 | [任务](../tasks/modules/M11-identity-transactions.md) |
| M12 | 身份验证闭环 | 测试、既有四层功能 | 有限范围验收5/5，原VM恢复挂起、副本已删除 | [任务](../tasks/modules/M12-identity-validation.md) |
| M13 | v0.3 验收交付 | 测试、构建、文档、CI | 5/5；真实CI、原包宿主/客户机、公开预发布及服务器回执通过 | [任务](../tasks/modules/M13-v03-delivery.md) |

按 M00 → M01 → M02 → M03 → M04 → M05 → M06 顺序开发。M02、M03 和 M04 均依赖基础报告契约；顺序执行便于单代理维护与控制内存，不要求同时启动多个构建或 VM。

v0.3按M08→M09→M10推进，再对获准字段开展M11/M12，最后M13。M11有限已有字段已通过，未覆盖的生成/复制及其他版本不能降低门槛；M12本轮真实方案已执行结束，证据归档及副本清理完成，不重放已完成操作或重建旧副本。[M13本地候选](V03-M13-LOCAL-READINESS.md)版本0.3.0-alpha.1已构建验证，不挪用旧包或旧CI替代最终CI原包目标验收。

## 项目边界

- Core：平台无关的报告模型、隐私规则、证据解释和对比；不引用 Windows API、WPF 或 VMware 程序。
- Windows：本机只读采集，返回 Core 的结构化观测；不负责 UI 或报告文件保存。
- VMware：VMX 原始内容保留、声明解释和状态证据；v0.1 不提供配置写入。
- App：组合引擎、后台任务、页面和文件选择；客户机采集入口共享同一引擎。

测试分为合成数据单元测试与独立临时文件集成测试；真实 VM、UI、干净环境和远程 CI 的实际结果另列，不从自动化测试推导。

## 输入输出契约

| 模块 | 输入 | 输出与接口职责 |
| --- | --- | --- |
| M00 | 字段定义和真实环境记录 | DiagnosticReport、Finding、类型化 FindingValue、CollectionStatus；单位和来源明确 |
| M01 | 报告对象或 JSON 流 | ReportJson 序列化/校验；ReportPrivacy 脱敏；HtmlReportRenderer 静态 HTML |
| M02 | 用户选定的 VMX 字节/文件 | VmxDocument 保留原字节；VmxReader 读取；声明摘要、重复项与状态证据 |
| M03 | 两份受支持格式的报告 | ReportComparer 返回相同、不同、状态差异或不可比较，保留理由 |
| M04 | 当前 Windows 接口与取消信号 | WindowsCollector 返回五类观测；EvidenceEvaluator 解释证据，不把失败当作无信号 |
| M05 | 用户操作及前述输出 | 五个页面、采集进度、取消、导入/对比/导出和同引擎采集模式 |
| M06 | 源码、版本、测试环境 | 构建产物、实际测试证据、兼容性矩阵和发布就绪表 |
| M07 | 已确认版本、白名单、预览与授权 | 受控备份、应用和恢复；关机、占用或外部变更保护不成立时拒绝 |

接口名称是职责约定，具体签名以经过构建和测试的源码为准；不预先增加插件系统、网络服务或通用工作流框架。

## 原验收项的责任归属

| 模块 | 主责验收项 | 跨模块完成条件 |
| --- | --- | --- |
| M00 | ENV-01–ENV-04 | 实际环境记录与生命周期核查，不因已创建工程全部勾选 |
| M01 | DET-03、VMX-05、UI-05 的报告部分 | VMX 脱敏需 M02；UI-05 语言资源需 M05 |
| M02 | VMX-01–VMX-04 | 目标版本状态探测实测不足时 VMX-04 保持未验证 |
| M03 | UI-04 | 合成测试与 App 导入流程分别验证 |
| M04 | DET-01、DET-02、DET-04、DET-05 | 五类实际采集、故障与误判场景均有证据 |
| M05 | UI-01–UI-03；UI-05 的语言部分 | 编译通过不代表交互和普通权限实测通过 |
| M06 | V01-01–V01-06、REL-01–REL-06 | 真实 VM、干净环境、许可和远程 CI 缺口分别列出 |
| M07 | CFG-01–CFG-06 | 每项发布配置须有官方依据和独立副本证据 |

上述跨模块关系不增加验收总数，当前公开交付状态以[发布就绪表](../RELEASE-READINESS.md)及对应模块验收为准；公开历史进度索引保留原范围。

## 每个模块的开发流程

1. 读取对应任务、日志和依赖模块的实际状态。
2. 实现当前需求与最小直接接口，先用合成数据验证规则。
3. 运行受影响的构建和有意义的测试；失败修复后重跑相关检查。
4. 更新模块状态、对应版本的验收证据和唯一 WORK_LOG.md，再保存本地提交；M08 研究验收不并入旧 37 条。
5. 环境受阻时继续独立工作；不以未编译源码、模拟测试或文档保存冒充功能完成。

宿主机版本不设门槛，客户机/目标允许Win10/11 x64，VMware最低17.5.2；用户指定环境例外与其他工具维护期规则见[依赖清单](DEPENDENCIES.md)。真实VM动作、系统安装和远程发布按项目授权规则确认。既有Defender受限代码继续禁用。
