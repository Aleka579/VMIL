# v0.3.0-alpha.1 上传与发布审阅方案

本文件仅准备可审阅范围，不构成推送/CI触发/发布授权。2026-10-21最初仅做本地准备；复审后用户要求完成未完成部分，已完成修复/本地重验，新的具体远程内容仍待确认，当前未远程写入。

## 源码与远程准备

目标仓库Aleka579/VMIL，候选版本0.3.0-alpha.1，预发布，不改旧v0.2tag。当前开发位于codex/v03-m10-20261019，包含内部日志和开发历史，不能直接推该分支历史。固定当前本地源码提交后，准备逐文件SHA256及差异清单。

拟上传生产源码src/、合成测试tests/、解决方案/构建设置/global.json、正式许可证/去重第三方许可、双语README/指南及经过审阅的功能/兼容/安全/报告文档、正常CI和独立开发工具。补齐文档所引用的六份M08–M13公开模块定义及四份v0.2发布/验收摘要，避免公开模块入口引用未上传文件；它们只含可公开的范围/验收定义或摘要。AGENTS.md、WORK_LOG.md、tasks/PROGRESS.md等内部历史/授权与环境调查、真实证据、artifacts/、local-data/、个人路径/标识/凭据均不上传。源码中的合成输入可保留，但真实VMX/磁盘/快照/备份不纳入。

已只读核实main=e27543f、M09公开分支=676d0ec。拟以676d0ec公开历史创建新的codex/v03-m13-ci-20261021分支和审阅覆盖层提交，只运行CI，main/旧tag/Release不变；本地bdec910只作为已验证产品来源。具体逐文件差异/摘要与私有执行方案准备后再请求确认，不强推或上传内部历史。CI的sourceCommit必须对应新的公开提交，其包与本地候选字节及提交不同。

## CI与最终包验收

保留windows-2025及固定官方Actions，普通contents:read；产品artifact继续VMIdentityLab-win-x64-alpha，开发驱动独立artifact。工作流新增EXE/四DLL提交/版本核对和PACKAGE-FILES.json，不新增v0.3发布触发器。旧publish-v02-alpha1.yml及发布脚本固定旧已验收包，本轮不执行或改作v0.3。

授权运行CI后核对job、TRX、包内Runtime10.0.13、System.Management/System.CodeDom10.0.13、BUILD-INFO及清单，固定原始下载ZIP的SHA256/大小。将原包直接用于普通权限目标验收，开发驱动另外准备，不把驱动加进Release产品ZIP；原ZIP不重打包。

在授权的具体目标核对默认14/显式17项、来源/版本、双语五页、诊断导出/拒绝覆盖、身份计划禁用范围、私有三阶段合成输入/脱敏导出。M11/M12旧实机往返保留原范围；是否需要新副本写入复验、目标电源操作及用户输入凭据在具体验收方案中单独说明，不沿用已结束授权。全新OS/其他Build/联网未做则继续明确未验证。

## Release资产与发布

待最终CI及目标验收通过后准备三资产：

- VMIdentityLab-0.3.0-alpha.1-win-x64.zip：已验收的原CI字节。
- 同名.zip.sha256：该原ZIP摘要。
- VMIdentityLab-0.3.0-alpha.1-win-x64.files.sha256：原ZIP每文件摘要；包内已有PACKAGE-FILES.json。

说明草稿见[双语发布文案](../.github/release-notes/v0.3.0-alpha.1.md)，不把本地候选哈希填成最终CI哈希。补齐公开来源、run/artifact、Runtime、目标验收及明确限制后，向用户请求针对确切提交、tag、资产和说明的最终发布授权。获授权后创建新预发布并回读tag目标、三个资产大小/SHA，保存回执；失败保留证据，不覆盖旧tag或假报完成。
