# M01 报告与隐私

- 版本：v0.1。
- 源码归属：Core/Reports。
- 当前状态：实现与 14 项合成测试已通过；跨模块验收待联调。
- 入口：[模块架构](../../docs/MODULES.md)、[验收索引](../PROGRESS.md)、[工作日志](../../WORK_LOG.md)。

## 目标

形成唯一的报告导入、默认脱敏和 JSON/HTML 导出流程。

## 前置条件

M00 模型契约可用。

## 具体子任务与实现步骤

- [ ] 实现 JSON 版本、大小、深度、结构与类型校验，拒绝不支持输入。
- [ ] 按字段可见性和隐私策略处理敏感值及诊断信息。
- [ ] 渲染正确编码、不加载远程资源的静态 HTML。
- [ ] 导出错误可识别；实际报告保存在私有位置，公开样例仅合成。

## 自动化测试与实际验证

格式往返、不支持版本、损坏输入、敏感字段和恶意 HTML 字符串。

执行记录包含命令、目录、时间及时区、结果、失败原因与修复情况；未执行不标通过。证据分类沿用 [测试计划](../../docs/TEST-PLAN.md)。

## 验收标准

DET-03；VMX-05 与 UI-05 的隐私/导出部分和后续模块共同验收。

## 风险与回滚方案

不执行导入内容；保留已有用户输出，不未经确认覆盖。 发生 Defender 拦截立即停止对应操作，遵守项目禁令。

## 完成状态与下一步

- 已完成：ReportJson、FindingCatalog、ReportPrivacy、HtmlReportRenderer；类型化值、5 MiB/深度 32 限制和敏感字段策略。
- 验证：项目内 dotnet test --project tests/VMIdentityLab.UnitTests/VMIdentityLab.UnitTests.csproj -c Release --report-xunit-trx --report-xunit-trx-filename m01.trx --results-directory artifacts/test-results/m01 --parallel none；14 成功、0 失败、0 跳过。证据：artifacts/test-results/m01/m01.trx。
- 待补齐：VMX 摘要与界面导出联调、实际报告脱敏；本结果不表示 UI-05 整项完成。
- 下一步：M02 复用报告隐私契约，M03 复用类型化值与状态。
