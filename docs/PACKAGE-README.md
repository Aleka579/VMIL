# VM Identity Lab · 0.2.0-alpha.1

Copyright (c) 2026 Daniel Waltz · 原创源码 MIT。

## 状态与范围

本开发版提供Windows虚拟化特征采集、VMX查看、报告对比及脱敏JSON/HTML导出，并通过VMware官方API管理少量已验证配置。实际App应用、原字节备份、退出重开历史、参数恢复及客户机资源核对已验收；127单元/100模拟集成通过。

配置只允许Workstation登记版本17.5.2.23775571、hardware19、每插槽2核，内存4096↔6144MiB及vCPU2↔4。其他构建/拓扑/数值拒绝。旧Alpha快照不含最新后端；以本包BUILD-INFO.json中的实际源码与运行时为准，不能只按文件名推断内容。

## 使用

完整解压并保留全部文件，运行VMIdentityLab.App.exe。自包含包包含.NET运行时，无需开发SDK或系统.NET。五页提供概览、检测、配置、报告对比与导出；侧栏切换中英文。

```powershell
.\VMIdentityLab.App.exe --collect --output .\report.json --html .\report.html
```

CLI拒绝覆盖已有输出；GUI替换报告前确认。采集与VMX读取不需要API账号。可在VMX页连接已配置的本机官方API查询状态；App不自动启动接口或操作虚拟机电源。

配置操作先查看当前目标、逐项差异和影响，勾选确认后在本机窗口输入API专用账号。程序重新核实完全关机、唯一目标、版本、文件占用和当前内容，先备份，再通过官方API更新和复核；不会直接替换VMX。

在“备份与恢复历史”选择实际记录可生成新恢复预览；恢复只反向修改本次参数，保留其他内容。历史及备份路径仅本地保存，不进入公开报告。参数或拓扑后续变化会拒绝恢复；配置备份不能恢复虚拟磁盘、快照或整机状态。

出现“API操作结果尚未确认”时，保留pending、备份和预期文件，先核实服务端执行结果，不要删除记录或盲目重试。App租约只协调本应用实例，不是VMware全局锁；没有未决操作自动核销功能。

## 实测与发布状态

最新5de验证包在Win10教育版18363 x64普通权限通过14/14采集，以及中英界面、实际报告对比/导出、脱敏和包内.NET加载等十六项检查。标准目录/PATH/登记未发现系统dotnet；该环境已有VC++及其他软件，不是全新OS。Windows11客户机、其他VMware构建/拓扑/数值不外推。

远程CI与私密安全报告渠道以实际仓库启用状态为准。本说明不表示已发布GitHub Release，也不把旧包的实测结果自动赋予未来包。源码中的RELEASE-READINESS.md记录当前门槛，SECURITY.md说明报告方式；不要将敏感材料发到公开Issue，noreply提交邮箱不能接收报告。

Hypervisor或Tools痕迹不能单独证明客户机身份；VMX声明与客户机观测分开显示。

## 许可与隐私

原创许可见LICENSE，第三方声明见THIRD-PARTY-NOTICES.md和third-party/。不捆绑SDK、VMware、Windows镜像、真实VM、个人报告或测试驱动；无遥测，不自动上传报告。保留完整许可和运行时文件。

## English

This alpha provides Windows diagnostics, VMX inspection, report comparison, redacted JSON/HTML export and limited configuration changes through the official VMware API. Actual apply, backup, history after restart and parameter-only restore passed on an independent copy. Only registered version 17.5.2.23775571, hardware 19, two cores per socket, and 4096↔6144 MiB / 2↔4 vCPU are allowed.

Keep the complete self-contained folder together. The App does not control VM power. Changes require a fresh preview, explicit confirmation, a powered-off VM and a request-scoped API connection. Restore preserves other contents; configuration backups are not full VM backups. Preserve unresolved-operation records and do not blindly retry. The App lease is not a global VMware lock.

The latest 5de package passed ordinary-user CLI/GUI and report/runtime checks on Windows 10 Education build 18363, without a development SDK or system .NET. This was not a fresh OS. Consult BUILD-INFO.json for this package's identity and RELEASE-READINESS.md in the source for remote CI and release status. Original code is MIT; retain the included third-party licenses. There is no telemetry or automatic upload.
