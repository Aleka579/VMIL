# v1.0.0 发布记录

## 范围与来源

用户于2026-10-29要求发布v1.0。本版采用`1.0.0` / tag `v1.0.0`，作为正式Release发布；v0.2/v0.3预发布及其资产保留不变。不是将旧Alpha ZIP重标为正式版：版本、界面、包说明和构建来源均由新CI生成。

本版整理既有诊断、VMX只读解析、报告对比、有限官方API事务/历史/恢复及三阶段身份验证。诊断schema1.0、私有快照和验证schema保持兼容。除版本资源及维护补丁外，不改配置事务逻辑，不扩大写入范围。

内存/CPU准入仍为精确Workstation17.5.2 build23775571/hardware19/每插槽2核、4096↔6144MiB及2↔4vCPU。身份准入仍为同一精确Workstation/vmrest构建、hardware19、有效已有MAC及BIOS UUID、单个静态hostonly且启动断开网卡，一次一个已有字段。版本下限不构成写入批准；Win11 hardware21的只读测试也不扩大该政策。见[配置政策](VMWARE-CONFIG-POLICY.md)。

全新OS、其他VMware构建/拓扑和联网未验证；厂商生成/复制、自动地址、缺失键和多字段恢复不开放。其他虚拟化证据仍在，不保证物理机分类，不用于绕过第三方保护。

## 依赖与维护期

- C#14 / .NET10 LTS / WPF / MVVM和四层工程保持；不更换框架或新增产品依赖。
- 自包含NETCore/WindowsDesktop及System.Management/传递System.CodeDom更新到10.0.14。微软[10.0.14发布记录](https://github.com/dotnet/core/blob/main/release-notes/10.0/10.0.14/10.0.14.md)确认2026-10-25安全补丁及SDK10.0.403；最终实际SDK/运行时由新CI的BUILD-INFO记录，不把本地10.0.401称作最新补丁。
- [.NET10支持至2028-11-14](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core)，[PowerShell7.6 LTS](https://learn.microsoft.com/en-us/powershell/scripting/install/powershell-support-lifecycle)沿用维护中的7.6.6；[Python3.14仍处于bugfix](https://devguide.python.org/versions/)，计划2030-10结束。不采用临期.NET8/9、预览框架或新系统级工具。
- Python/PowerShell/SDK、测试驱动及VMware不随产品分发；第三方许可与实际包核对，MIT/Daniel Waltz和已开启私密报告渠道沿用确认结果。

## 验证门槛

| 项目 | 本轮实际结果 |
| --- | --- |
| 版本/双语UI/正式包说明 | 1.0.0原CI包来源/版本已核对；双语正常App侧栏/报告版本及实际加载映像通过 |
| 本地Release | SDK10.0.401，零警告/零错误 |
| 单元测试 | 240/240，失败/跳过0 |
| 模拟集成 | 206/206，失败/跳过0 |
| 打包工具 | 15/15，包含稳定版往返、旧Alpha兼容、篡改/错误身份拒绝 |
| 原生凭据助手模拟 | 8/8，真实凭据只在本机窗口输入 |
| 官方许可 | runtime/WPF v10.0.14原件核对；两包nuspec10.0.14/MIT、共享声明换行归一后一致 |
| 合成三阶段输入及资源 | 原验收工具零警告/错误；77/77通过 |
| 公开源码/PR CI | 26文件覆盖层，226文件树040c3708；分支CI40893859028、PR CI40893926270均success，PR2已普通合并 |
| 新主线CI | main/8c30801ca4974910d513bb0a454733f9b452fd8f，[CI40894459119](https://github.com/Aleka579/VMIL/actions/runs/40894459119) completed/success；17步success，零警告/错误，原TRX240/206失败跳过0 |
| 原始ZIP/来源/许可/哈希 | artifact12907280066，413文件/65,286,472字节/解压147,354,326字节，SDK10.0.403/两运行时10.0.14，CRC/全SHA/许可/依赖及匹配驱动通过 |
| 最终CI原包普通权限Win11 | 部署6/6、CLI/PNG28/28、正常双语App50/50通过；实际退出0，413文件/原ZIP未变，最终证据归档及目录/凭据清理通过 |
| tag/三资产/服务器SHA/正式Release/latest | 待完成 |

首次本地还原因沙箱网络套接字限制失败（NU1301/NU1900）；同期只读检查Defender1116/1117计数0。通过平台审批使用原SDK联网还原后构建成功，没有关闭审计、防护或更换被拦截载荷入口。完整原失败与成功输出保存在本版私有证据，不隐去失败。

本版包核对脚本首次误将CodeDom视为独立deps库；实际由`runtimepack.Microsoft.WindowsDesktop.App.Runtime.win-x64/10.0.14`提供。按实际提供者修正检查后通过，原失败保留，未改产品包或放宽版本。前置失败期间未创建CASE、未发送客户机命令；最终通过后才开始新案例部署。

## 冻结资产

tag拟绑定已通过主线CI的`8c30801ca4974910d513bb0a454733f9b452fd8f`。原ZIP不重压缩；两份摘要使用CRLF，逐文件摘要保留原ZIP顺序。

| 文件 | 字节数 | SHA256 |
| --- | --- | --- |
| VMIdentityLab-1.0.0-win-x64.zip | 65286472 | c1f7ac2fee3d3bb10093374f5d495379f3deae58dde3be83554bcadd67a15779 |
| VMIdentityLab-1.0.0-win-x64.zip.sha256 | 99 | e6c6d3fa7cdf7f974975fa7c3eb3e4afa93a209c5fee8b5ae02409346be478a4 |
| VMIdentityLab-1.0.0-win-x64.files.sha256 | 41128 | f5e6dd2db495930bf32a032947dc9d2c2d462ab2af5c00b700c9a795bbc7717e |

## 最终原包Win11客户机验收

Windows11 Pro Build26100/x64、HW21/EFI/vTPM、4逻辑处理器、6GiB内存，普通权限。标准三个.NET目录及命令未发现系统SDK/.NET；实际四个加载运行时映像路径和SHA与原包一致。目标是现有系统，不是全新OS。加密VM密码及客户机Windows凭据仅在本机原生窗口输入，不保存或公开；本轮不改VM配置、不操作电源或快照，也没有整机副本。

部署6/6、CLI/PNG28/28和正常App50/50实际通过：默认14/显式17项采集、独立CIM事实、脱敏JSON/HTML、拒绝覆盖、中英文PNG及五页、v1.0侧栏、身份支持/依据说明、两字段三阶段合成输入/配对历史、23项配置/客户机结论及3项程序分类、网络未测标记、脱敏导出及UI隐藏原值。App真实退出0；中文PNG已视觉核对，原M09离屏空白未出现。

最终413文件及原ZIP不变；最后GUI后的93份证据已归档，85份先前取回文件与归档逐字节一致，随后删除本轮重复散件及自有客户机目录。凭据会话于2026-10-29T23:13:38+08:00停止，23:14:39实际确认进程退出，VM只读列表仍运行。最终私有证据归档SHA256为`552b56256565f529c0eddef504575a6d1ec9d0759e9611c3b85eb8b542c2066f`；不公开原始证据或客户机路径。

本版原包只读验收不重标原Win10结果，也不替代M11/M12既有版本的真实身份写入/恢复证据；写入事务逻辑未改、准入不扩大。

## 发布步骤和恢复

先审查当前源码差异与许可证，再以公开main为父上传限定文件覆盖层，保留公共协作摘要，不上传内部AGENTS/WORK_LOG/PROGRESS、真实数据或本地历史。普通PR合并到main后，验收该合并SHA的原CI产品ZIP和匹配驱动；发布tag绑定同一被验收来源，不使用分支旧包代替。

最终原CI ZIP仅下载保留一份，生成ZIP摘要及逐文件摘要。只读客户机测试使用新建的自有测试目录、NTFS硬链接共享驱动运行时，结束后归档并清理该目录和临时凭据。真实VM电源、配置、快照及整机副本不在本轮自动执行范围。

固定发布器先验证成功CI、来源、版本、运行时、文件数及三份本地冻结摘要，再创建草稿、上传并核对三个服务器资产，最后公开正式版并设为latest。任一失败保留现场/草稿，不覆盖旧tag或Release、不盲目重试。发布后独立核对tag、正文、资产及main CI并记录回执。回退使用新提交/撤下有问题的新发布，不改写旧历史或用户数据。

## English

Version1.0.0 promotes the existing bounded feature set to a stable release, with current UI/package wording and .NET10.0.14 servicing updates. Diagnostic and validation schemas, exact-build write admission and recovery boundaries are unchanged. A newly versioned main-branch CI artifact must receive independent exact-byte acceptance; previous alpha results do not certify the new package. Existing prereleases remain immutable. Publication stages a draft, verifies all three asset hashes, then publishes a stable latest release. Real VM power/configuration operations are outside this release task.
