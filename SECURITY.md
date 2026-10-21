# 安全政策

当前源码为0.3.0-alpha.1本地候选，尚未上传或发布本版；已公开v0.2.0-alpha.1保持原内容。M11/M12原模块包完成有限应用/恢复及客户机核对，不替代新候选/CI原包验收。结果见[发布就绪表](RELEASE-READINESS.md)、[M13记录](docs/V03-M13-LOCAL-READINESS.md)及[配置政策](docs/VMWARE-CONFIG-POLICY.md)。私密安全渠道与维护者Daniel Waltz沿用已确认安排。

本工具用于Windows/VMware诊断与有限兼容性配置管理，默认普通权限、本地处理、无遥测、脱敏导出。配置操作须经新预览、明确确认、可靠关机核实、原字节备份和官方API复核；不自动操作电源。

## 当前评估范围

| 范围 | 状态 |
| --- | --- |
| 当前源码及明确来源的构建 | 受理诊断采集、报告输入/导出、配置完整性及本机凭据处理相关问题；仍为开发版 |
| 可写兼容范围 | Workstation登记版本17.5.2.23775571、hardware19、每插槽2核；内存4096↔6144MiB、vCPU2↔4 |
| 已有身份字段 | 同一精确构建/hardware19、有效已有MAC和BIOS UUID、唯一静态hostonly网卡且启动断开，一次一个字段；不含生成/复制/缺失键/多字段恢复 |
| 其他VMware构建/拓扑/数值 | 写入拒绝；最低版本声明不代表写入支持 |
| 旧Alpha快照 | 保留原只读范围，不视为包含最新后端或修复 |
| 不在本项目内 | VMware/Windows本身的漏洞、第三方反作弊或授权检测规避；向相应厂商报告 |

本地租约协调App实例，不是VMware全局锁。超时、提交期间状态/内容变化或响应不确定时保留pending及备份，阻止重放；不能删除记录后盲目重试。配置备份不能恢复虚拟磁盘或整机状态。

显式私有快照包含原始MAC/UUID，恢复历史包含本地路径；仅用户私下保管，默认诊断及验证结论仍脱敏。三阶段结果分别核对配置、Windows观测及VMIL分类，不能用于保证物理机身份。

## 报告安全问题

维护者：Daniel Waltz（GitHub：[Aleka579](https://github.com/Aleka579)）。请通过[GitHub私密安全报告入口](https://github.com/Aleka579/VMIL/security/advisories/new)提交问题。2026-10-17已核实仓库为公开、官方接口enabled=true，页面“Report a vulnerability”按钮可见；维护者与渠道已经用户确认。Git提交邮箱为noreply地址，不能作为报告邮箱。

也可在仓库Security → Advisories页面选择“Report a vulnerability”私下提交。请勿在公开Issue中提交原始VMX、完整报告、用户名路径、硬件标识符、密码或令牌。原始虚拟机、磁盘、快照和账号凭据不作为报告附件；优先提供最小合成样例。

报告应包含应用版本/构建来源、系统与VMware脱敏版本、预期与实际行为、影响及可复现步骤。维护者先私下确认和复现，修复验证后协调公开说明；未经安排不承诺响应时限或漏洞奖励，不要求报告者执行破坏性验证。

私密报告功能独立于本文件，已核实实际开关和入口；没有发送模拟报告或通知。配置说明见[GitHub官方文档](https://docs.github.com/en/code-security/how-tos/report-and-fix-vulnerabilities/configure-vulnerability-reporting/configure-for-a-repository)，实际接入结果见[GitHub验收记录](docs/GITHUB-READINESS-2026-10-17.md)。

内部设计和测试要求见 [docs/SECURITY.md](docs/SECURITY.md)。

