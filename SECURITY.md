# 安全政策

当前为0.2.0-alpha.1开发版，没有已公开发布的维护版本。本地最新应用来源5de4ec4，127单元/100模拟集成、真实App应用/备份/历史重开/恢复以及客户机验收已通过；配置仅允许下表范围。远程CI与私密安全报告渠道仍未完成，不因本文件存在而宣称渠道已启用。实际结果见[发布就绪表](RELEASE-READINESS.md)与[完整验收](docs/V02-FULL-TEST-RESULTS-2026-10-17.md)。

本工具用于Windows/VMware诊断与有限兼容性配置管理，默认普通权限、本地处理、无遥测、脱敏导出。配置操作须经新预览、明确确认、可靠关机核实、原字节备份和官方API复核；不自动操作电源。

## 当前评估范围

| 范围 | 状态 |
| --- | --- |
| 最新源码与5de验证包 | 受理诊断采集、报告输入/导出、配置完整性及本机凭据处理相关问题；仍为开发版 |
| 可写兼容范围 | Workstation登记版本17.5.2.23775571、hardware19、每插槽2核；内存4096↔6144MiB、vCPU2↔4 |
| 其他VMware构建/拓扑/数值 | 写入拒绝；最低版本声明不代表写入支持 |
| 旧Alpha快照 | 保留原只读范围，不视为包含最新后端或修复 |
| 不在本项目内 | VMware/Windows本身的漏洞、第三方反作弊或授权检测规避；向相应厂商报告 |

本地租约协调App实例，不是VMware全局锁。超时、提交期间状态/内容变化或响应不确定时保留pending及备份，阻止重放；不能删除记录后盲目重试。配置备份不能恢复虚拟磁盘或整机状态。

## 报告安全问题

目前尚无已验证的私密报告入口。拟采用GitHub Private Vulnerability Reporting，待目标公开仓库及维护者确认、实际启用并核实入口后，在此填写正式链接。Git提交邮箱为noreply地址，不能作为报告邮箱。

渠道启用后，在目标仓库的Security页面选择“Report a vulnerability”私下提交。请勿在公开Issue中提交原始VMX、完整报告、用户名路径、硬件标识符、密码或令牌。原始虚拟机、磁盘、快照和账号凭据不作为报告附件；优先提供最小合成样例。

报告应包含应用版本/构建来源、系统与VMware脱敏版本、预期与实际行为、影响及可复现步骤。维护者先私下确认和复现，修复验证后协调公开说明；未经安排不承诺响应时限或漏洞奖励，不要求报告者执行破坏性验证。

GitHub的私密报告功能需要公开仓库实际启用，独立于本文件；配置步骤见[GitHub官方说明](https://docs.github.com/en/code-security/how-tos/report-and-fix-vulnerabilities/configure-vulnerability-reporting/configure-for-a-repository)。当前接入进度见[剩余发布步骤](docs/GITHUB-READINESS-2026-10-17.md)。

内部设计和测试要求见 [docs/SECURITY.md](docs/SECURITY.md)。

