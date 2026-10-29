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
| 版本/双语UI/正式包说明 | 源码1.0.0，移除当前UI Alpha标识；待最终CI包核对 |
| 本地Release | SDK10.0.401，零警告/零错误 |
| 单元测试 | 240/240，失败/跳过0 |
| 模拟集成 | 206/206，失败/跳过0 |
| 打包工具 | 15/15，包含稳定版往返、旧Alpha兼容、篡改/错误身份拒绝 |
| 原生凭据助手模拟 | 8/8，真实凭据只在本机窗口输入 |
| 官方许可 | runtime/WPF v10.0.14原件核对；两包nuspec10.0.14/MIT、共享声明换行归一后一致 |
| 新主线CI和原始ZIP/来源/许可/哈希 | 待执行 |
| 最终CI原包普通权限Win11 CLI/PNG/双语App | 待执行；旧v0.3结果不计为本版通过 |
| tag/三资产/服务器SHA/正式Release/latest | 待完成 |

首次本地还原因沙箱网络套接字限制失败（NU1301/NU1900）；同期只读检查Defender1116/1117计数0。通过平台审批使用原SDK联网还原后构建成功，没有关闭审计、防护或更换被拦截载荷入口。完整原失败与成功输出保存在本版私有证据，不隐去失败。

## 发布步骤和恢复

先审查当前源码差异与许可证，再以公开main为父上传限定文件覆盖层，保留公共协作摘要，不上传内部AGENTS/WORK_LOG/PROGRESS、真实数据或本地历史。普通PR合并到main后，验收该合并SHA的原CI产品ZIP和匹配驱动；发布tag绑定同一被验收来源，不使用分支旧包代替。

最终原CI ZIP仅下载保留一份，生成ZIP摘要及逐文件摘要。只读客户机测试使用新建的自有测试目录、NTFS硬链接共享驱动运行时，结束后归档并清理该目录和临时凭据。真实VM电源、配置、快照及整机副本不在本轮自动执行范围。

固定发布器先验证成功CI、来源、版本、运行时、文件数及三份本地冻结摘要，再创建草稿、上传并核对三个服务器资产，最后公开正式版并设为latest。任一失败保留现场/草稿，不覆盖旧tag或Release、不盲目重试。发布后独立核对tag、正文、资产及main CI并记录回执。回退使用新提交/撤下有问题的新发布，不改写旧历史或用户数据。

## English

Version1.0.0 promotes the existing bounded feature set to a stable release, with current UI/package wording and .NET10.0.14 servicing updates. Diagnostic and validation schemas, exact-build write admission and recovery boundaries are unchanged. A newly versioned main-branch CI artifact must receive independent exact-byte acceptance; previous alpha results do not certify the new package. Existing prereleases remain immutable. Publication stages a draft, verifies all three asset hashes, then publishes a stable latest release. Real VM power/configuration operations are outside this release task.
