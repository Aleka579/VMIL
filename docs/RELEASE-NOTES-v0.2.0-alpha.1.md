# v0.2.0-alpha.1 — VMware API配置管理预览

本预发布版CI通过，客户机严格检查23/23通过；已验证环境和有限配置范围见下文。

VM Identity Lab提供Windows虚拟化特征分析、VMX只读查看、真实报告对比和默认脱敏的JSON/HTML导出，支持中英文界面。自包含Windows x64包无需开发SDK或系统.NET，无遥测或自动上传。

v0.2新增有限的官方VMware API配置管理：差异预览、逐项确认、原始字节备份、结果复核、持久化历史和参数恢复。未知、运行、挂起、占用、旧预览及未决操作保持拒绝；App不控制VM电源，不直接替换VMX。

可写范围仅为实测Workstation17.5.2.23775571、hardware19、每插槽2核，内存4096↔6144MiB及vCPU2↔4。其他构建、拓扑和值不开放。配置备份不是整机备份，本地租约不保证VMware全局互斥。

## 来源与验证

- 源提交：`e27543fb5a8519b9333b57af26f95dc3873a732c`；版本0.2.0-alpha.1，tag `v0.2.0-alpha.1`，标记Pre-release。
- [对应CI](https://github.com/Aleka579/VMIL/actions/runs/39109350478)：Release零警告/错误，130单元/106模拟集成通过；SDK10.0.402，NETCore/WindowsDesktop10.0.13。
- 新CI原包在Win10教育版18363 x64普通权限严格23/23：两次CLI及GUI14/14、独立事实及全值一致；双语五页、真实报告14行对比、六份脱敏导出、包内运行时、拒绝覆盖与正常退出通过。
- 原CI ZIP及412文件测试后哈希完全相符；没有重建或重压缩。测试目录、开发驱动和凭据会话已清理，原VM保持运行。
- 真实配置应用/恢复来自此前5de本地包的独立副本测试；本次CPU修复未改配置模块，新CI字节包本轮仅作只读客户机验收。

## CPU采集修复与验证范围

CPU型号使用标准CPUID品牌字符串，其他CPU字段不等待慢WMI查询；支持边界和失败状态明确，旧报告仍保留实际WMI来源。本轮两次CLI及GUI均14/14成功，全部观测值相同，CPU与独立CIM一致。分类5秒/枚举2秒限制不变，没有增加重试；旧3832候选20/23失败记录保留。

目标是已有Win10教育版18363 x64，标准目录/PATH/登记未发现SDK或系统.NET，已有其他软件；不是全新OS。Windows11客户机及其他VMware构建/数值/拓扑未由本次验证。项目实测不等于微软或VMware官方支持承诺。

## 下载文件

| 文件 | 用途 |
| --- | --- |
| VMIdentityLab-0.2.0-alpha.1-win-x64.zip | 原始CI ZIP逐字节复制，65,197,403字节；没有重打包 |
| VMIdentityLab-0.2.0-alpha.1-win-x64.zip.sha256 | 下载包SHA256 |
| VMIdentityLab-0.2.0-alpha.1-win-x64.files.sha256 | 包内412文件SHA256清单 |

ZIP SHA256：`77ab023491fb677eeaeff2d9860c9c771b718c762444eccd8d4e498d42de27b7`。

MIT，Copyright (c) 2026 Daniel Waltz。第三方声明随包提供，不包含VMware程序、Windows镜像、真实VM数据、测试驱动或个人报告。安全问题请使用[私密报告渠道](https://github.com/Aleka579/VMIL/security/advisories/new)，由Daniel Waltz维护。

## English

Prerelease for commit `e27543fb5a8519b9333b57af26f95dc3873a732c`. The original CI ZIP is preserved byte for byte, built with SDK10.0.402 and .NET10.0.13. CI passed130 unit and106 simulated integration tests. Strict ordinary-user Win10 guest acceptance passed23/23, resolving all three prior failures.

CPU brand collection now uses standard CPUID leaves without waiting for WMI. Both CLI runs and the GUI collected14/14 successful observations with identical values and independent CIM agreement. Existing deadlines remain unchanged. Bilingual navigation, real report comparison, redacted exports, bundled runtime loading, overwrite refusal and normal exit passed. The old3832 failed candidate remains recorded separately.

Configuration writes remain limited to the exact validated Workstation17.5.2 build23775571/hardware19/two cores per socket,4096↔6144MiB and2↔4vCPU. Earlier real configuration tests used the5de local build; this CI artifact received read-only guest tests. The guest is an existing OS, not a fresh installation. Other VMware builds/topologies and Windows11 guests remain unverified.
