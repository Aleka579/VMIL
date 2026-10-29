# VM Identity Lab — 1.0.0

Windows x64 本地诊断、VMware 配置管理与三阶段身份验证工具。准确来源、SDK及运行时见 `BUILD-INFO.json`，文件哈希见 `PACKAGE-FILES.json`。正式发布采用已验收的原CI ZIP；实际CI、客户机及发布回执在源码的 `docs/V1-RELEASE.md` 分别记录。

## 启动与检测

完整解压并运行 `VMIdentityLab.App.exe`，保留运行时文件。自包含包无需开发 SDK、系统 .NET、Python 或管理员权限。五页提供概览、检测/身份分析、VMX 配置、报告比较/身份验证、导出；侧栏可切换简体中文和英文。

```powershell
.\VMIdentityLab.App.exe --collect --output .\report.json --html .\report.html
.\VMIdentityLab.App.exe --collect --include-identifiers --output .\identity.json
```

默认14项；第二条显式选择 UUID/MAC/BIOS 序列号后为17项，诊断报告依然脱敏。CLI 拒绝覆盖已有输出。VMX 声明、Windows 实际观测和 VMIL 分类分别解释，脱敏标记不能证明原值相同。

## 有限配置与恢复

内存/CPU 仍仅允许 Workstation17.5.2.23775571、hardware19、每插槽2核的4096↔6144 MiB、2↔4 vCPU。新增身份操作只允许同一精确构建及 hardware19、已有有效 MAC 和 BIOS UUID、唯一静态 hostonly 网卡且启动断开，一次修改或恢复一个已有字段。

审阅差异及风险并明确确认，再在本机连接窗口输入 vmrest 专用账号。软件重新核实关机、目标、版本、文件占用及当前内容，先备份并保存持久记录，再经官方 API 应用并复核。不启动接口、不控制 VM 电源、不直接替换 VMX。重新打开后可通过“备份与恢复历史”生成恢复预览，只反向修改该次参数，保留其他内容。

配置备份不是整机恢复，不能恢复磁盘、快照、NVRAM 或客户机状态。结果不确定时保留 pending、备份和预期文件，先核实结果，不删除记录或盲目重试。App 租约只协调本应用实例，不是 VMware 全局锁。

自动地址、多网卡、联网、其他构建、缺失键、厂商生成/复制流程和多字段恢复未准入。实验计划仍是独立未执行预览，不能解锁写入。修改 MAC/UUID 后其他虚拟化证据仍在，不保证第三方软件认为环境是物理机。不得用于绕过反作弊、反欺诈、封禁、授权或安全保护。

## 私有快照与三阶段验证

在“对比 → 身份验证”选择字段和会话，在对应系统分别采集变更前、变更后、恢复后三份私有快照，必要时指定同一网络接口 GUID。输入配对的应用/恢复历史记录后，分别核对配置、Windows 观测及 VMIL 分类。导出的验证结论 JSON/HTML 会脱敏。

私有快照确实包含原始 MAC/UUID，历史记录含本地路径；仅放个人私有目录，勿提交 Git、公开 Issue 或公开附件。三个阶段需同会话、系统/接口范围及相符记录；范围不同、多地址或旧脱敏报告会明确不可比较。不要把宿主快照当成客户机观测。

## 验证范围与许可

M11/M12 原模块包曾在现有 Windows10 Education Build18363 x64 独立副本完成实际应用/恢复及三阶段验证；这不代表本包、其他构建、全新系统或联网已经验证。本包以独立v1.0记录为准，官方支持与项目兼容实测分列。

原创 MIT，Copyright (c) 2026 Daniel Waltz；保留 `THIRD-PARTY-NOTICES.md`、`third-party/`。不含 SDK、VMware、Windows 镜像、真实 VM、个人报告或测试工具。无遥测或自动上传。安全问题通过 [GitHub 私密报告](https://github.com/Aleka579/VMIL/security/advisories/new)交由 Daniel Waltz 处理，不在公开 Issue 提交敏感材料。

## English

This Windows x64 release offers local diagnostics, opt-in redacted identity analysis, VMX inspection, finite official-API changes, backup/history/parameter restoration and three-stage validation. Run the complete self-contained folder as an ordinary user; no SDK, system .NET or Python is required. Check `BUILD-INFO.json` for source/runtime/distribution and `PACKAGE-FILES.json` for hashes. Exact CI, guest and publication evidence are recorded separately in `docs/V1-RELEASE.md` in the source repository.

Numeric writes retain the exact 17.5.2.23775571/hardware19/two-cores-per-socket scope: 4096↔6144 MiB and 2↔4 vCPU. Identity writes admit one existing MAC or BIOS UUID at a time only on that build/hardware, with both valid identities and one static host-only NIC disconnected at startup. Other builds, automatic/multiple/connected NICs, absent keys, generated/copied identities and multi-field restoration are not admitted. A fresh preview, explicit confirmation, reliable power-off, verified backup, durable intent and API readback are required. The App does not control power or replace VMX files directly. Backups do not restore disks, snapshots, NVRAM or entire machines; preserve unresolved records and do not blindly retry. The App lease is not a global VMware lock.

Capture private before/after/restored snapshots in the intended Windows system using one session and consistent adapter scope, then import matching apply/restore records in Comparison → Identity validation. Private snapshots contain raw MAC/UUID and must stay private. Public conclusions are redacted; mismatched scopes and old redacted identifiers remain incomparable. VMware evidence remains; physical-machine classification is not guaranteed. Do not use this software to evade anti-cheat, anti-fraud, bans, licensing or security protections.

Earlier M11/M12 guest evidence does not certify this package or a fresh OS. Consult the source readiness record for this exact package. Original code is MIT, Copyright (c) 2026 Daniel Waltz; retain third-party licenses. No telemetry, automatic upload, VMware binaries, OS images or test tools are included. Report vulnerabilities through the [private form](https://github.com/Aleka579/VMIL/security/advisories/new), maintained by Daniel Waltz.
