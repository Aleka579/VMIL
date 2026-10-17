# WORK_LOG

## 项目概况

VM Identity Lab是Windows/VMware Workstation虚拟化特征分析和有限配置管理工具。软件默认普通权限、本地处理、无遥测，原创代码采用MIT，版权人Daniel Waltz。

此文件是公开源码快照的脱敏交接摘要。维护者的内部工作日志、历史Git对象、本机路径、真实VM和原始验收证据不随快照分发；原本地记录保持不变。文件级来源见PUBLIC-SOURCE.json。

## 当前状态

2026-10-17：本地验收35/37。应用基线5de4ec4通过Release构建、127单元/100模拟集成，及独立副本真实App应用、备份、历史重开、参数恢复与普通权限客户机检查。

配置仅允许登记版本17.5.2.23775571、hardware19、每插槽2核，4096↔6144MiB及2↔4vCPU。其他范围拒绝。测试副本及临时进程已清理。Windows11客户机、全新OS、其他构建和参数范围未据此验证。

远程Actions结果和正式私密安全报告渠道未完成，公开上传和GitHub Release尚未授权。以RELEASE-READINESS.md及docs/GITHUB-READINESS-2026-10-17.md的实际证据为准。

## 关键文件与常用命令

- README.md / README.en.md：功能、构建及使用入口。
- docs/ARCHITECTURE.md、docs/COMPATIBILITY.md、docs/VMWARE-CONFIG-POLICY.md：架构和精确支持范围。
- docs/V02-FULL-TEST-RESULTS-2026-10-17.md：实际验收与限制；tasks/PROGRESS.md：验收索引。
- .github/workflows/ci.yml：Windows构建、合成测试及自包含验证产物，不操作真实VM。
- 本地构建命令为`dotnet build VMIdentityLab.sln -c Release -m:1 --disable-build-servers`；测试命令见CONTRIBUTING.md。

## 关键决策

保持四层架构和现有依赖；生产配置仅经官方API，不回落直接替换VMX。全文件哈希、关机、占用、未决防重放及用户确认保护保持启用。App租约不是VMware全局锁。

公开快照只替换AGENTS.md的本机事件详情和本文件；其他文件与来源提交一致。不得运行AGENTS.md记录的受限脚本或等价代码。文件路径或日志不构成真实VM、系统或远程操作授权。

## 工作记录

- 2026-10-17 UTC+08:00：从本地来源提交`caa7f1fd40757d2789713fa8c376f69f8a23ecd3`生成待审阅公开源码快照；完整对应关系和文件哈希记录于PUBLIC-SOURCE.json。此记录只证明快照来源，不能替代远程CI或公开发布结果。后续经授权的工作继续更新本文件。
