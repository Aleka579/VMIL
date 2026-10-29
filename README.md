# VM Identity Lab

[English](README.en.md)

Windows 虚拟化特征分析、VMware Workstation 配置管理与诊断报告工具。提供简体中文和英文界面，默认普通用户权限、本地处理、脱敏导出，无遥测。

当前源码版本 **1.0.0**。正式包、真实 CI 和验收进度见[发布记录](docs/V1-RELEASE.md)。[历史 v0.3 预发布](https://github.com/Aleka579/VMIL/releases/tag/v0.3.0-alpha.1)及其资产保持原样。

## 主要功能

- 检测 SMBIOS、CPU、PnP、VMware Tools 和系统信息；默认 14 项，显式选择身份信息后 17 项。
- 只读解析 VMX、查看兼容性声明，分别解释配置、Windows 观测和虚拟化证据。
- 对比宿主机与客户机报告，导出默认脱敏的 JSON 和静态 HTML。
- 在有限准入范围内，经官方 VMware API 应用配置，提供差异预览、确认、备份、持久历史及参数恢复。
- 导入私有的变更前、变更后和恢复后快照，分别核对配置、客户机观测和 VMIL 分类。

MAC/UUID 改变后其他虚拟化证据仍然存在，软件不保证第三方程序将环境识别为物理机。不用于绕过反作弊、反欺诈、封禁、授权或安全保护。

## 下载和使用

发布资产位于 [GitHub Releases](https://github.com/Aleka579/VMIL/releases)。下载 Windows x64 ZIP 及校验文件，完整解压后运行 `VMIdentityLab.App.exe`。保留全部运行时文件，无需安装开发 SDK、系统 .NET 或 Python。

`BUILD-INFO.json` 记录版本、来源提交、SDK 和运行时；`PACKAGE-FILES.json` 记录逐文件哈希。

```powershell
.\VMIdentityLab.App.exe --collect --output .\report.json --html .\report.html
.\VMIdentityLab.App.exe --collect --include-identifiers --output .\identity.json
```

CLI 拒绝覆盖已有输出。GUI 替换已有报告需要确认。客户机和宿主机分别采集，再手动导入报告比较。私有身份快照含原始 MAC/UUID，恢复历史含本地路径，请勿上传公开仓库或 Issue。

## 兼容范围

只读检测面向 Windows 10/11 x64；项目实测、上游官方支持与未知环境分开记录。v1.0 精确包的结果以[本版记录](docs/V1-RELEASE.md)为准，旧包验收不自动转为本版通过。历史 Win10、加密 Win11 客户机结果见[兼容性矩阵](docs/COMPATIBILITY.md)。

配置写入范围保持不变：

| 功能 | 已准入范围 |
| --- | --- |
| 内存 / vCPU | Workstation 17.5.2 build23775571、hardware19、每插槽2核；4096↔6144 MiB、2↔4 vCPU |
| 已有静态 MAC / BIOS UUID | 同一精确 Workstation/vmrest 构建、hardware19、已有有效两标识、唯一静态 hostonly 网卡且启动断开；一次一个已有字段 |
| 其他版本、值或拓扑 | 只读展示，禁止写入 |

应用和恢复均须完全关机、新预览、明确确认、原字节备份、持久记录和 API 复核。App 不控制 VM 电源，也不直接替换 VMX。备份只恢复配置参数；本地租约不是 VMware 全局锁。未决操作保留记录并阻止重放。具体依据和恢复边界见[配置政策](docs/VMWARE-CONFIG-POLICY.md)。

## 构建与测试

C# 14 / .NET 10 LTS / WPF / MVVM。SDK 由 `global.json` 选择稳定补丁；自包含运行时及采集依赖为 10.0.14。既有开发工具环境可复用，应用用户无需这些工具。

```powershell
dotnet build VMIdentityLab.sln -c Release -m:1 --disable-build-servers
dotnet test --project tests/VMIdentityLab.UnitTests/VMIdentityLab.UnitTests.csproj -c Release --no-build --parallel none
dotnet test --project tests/VMIdentityLab.IntegrationTests/VMIdentityLab.IntegrationTests.csproj -c Release --no-build --parallel none
```

设置 `DOTNET_CLI_TELEMETRY_OPTOUT=1` 和 `TESTINGPLATFORM_TELEMETRY_OPTOUT=1`。默认测试使用合成数据和独立临时文件，不操作真实 VM。CI 构建、模拟测试和真实客户机验收分别记录。

## 文档和许可

[中文指南](docs/USER-GUIDE.md) · [英文指南](docs/USER-GUIDE.en.md) · [配置政策](docs/VMWARE-CONFIG-POLICY.md) · [兼容性](docs/COMPATIBILITY.md) · [依赖](docs/DEPENDENCIES.md) · [架构](docs/ARCHITECTURE.md) · [发布记录](docs/V1-RELEASE.md) · [版本历史](CHANGELOG.md)

原创源码采用 [MIT](LICENSE)，Copyright (c) 2026 Daniel Waltz。保留[第三方许可](THIRD-PARTY-NOTICES.md)。漏洞请通过 [GitHub 私密报告](https://github.com/Aleka579/VMIL/security/advisories/new)提交，由 Daniel Waltz 维护；勿在公开 Issue 上传凭据、真实 VMX 或个人报告。
