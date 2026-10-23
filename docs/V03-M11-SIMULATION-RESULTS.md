# M11：事务保护与模拟验证

后续补证入口：[MAC/UUID官方支持与恢复](V03-M11-MAC-UUID-EVIDENCE.md)。已取得17系列手册正文并更新不可执行候选的说明，精确构建自动化/恢复和真实闭环仍未验证；下文保留本阶段当时的源码、失败和测试范围。

日期：2026-10-19，Asia/Taipei（UTC+08:00）。按用户请求继续当前 `codex/v03-m10-20261019` worktree，基线 `5d4fefa`（M10 已完成 5/5）。本轮不修改真实虚拟机，不连接 VMware 服务或请求凭据。

**当前可实施的开发和模拟验证已完成，M11 整体仍未完成。** Release 构建 0 警告/0 错误，168/168 单元测试、139/139 模拟和临时文件集成测试通过，无跳过。新增 4 个单元、24 个集成用例。新身份字段获准数仍为 **0**；没有实现或验证 MAC/UUID 写入适配器。

## 实际修复

原 `VmxApiConfigManager` 在写入、回读持久化操作记录（intent）之前做最终检查。新增故障注入在 `AfterIntent` 修改合成 MAC 或 UUID，原代码两例均实际发出 1 次 PUT，而预期为 0：首轮集成 131 项中 129 通过、2 失败。原失败保留，没有改低断言。

修正将原有最终检查移到 intent 写入并验证之后、PUT 之前：重新确认唯一注册映射、可靠关机、API 设置、完整 Workstation 版本、路径、独占读取及完整配置哈希，再检查取消状态。不增加重复查询、通用事务框架、参数写入接口或 VMX 文件替换回落。

若本次拥有已验证的 intent，且尚未发出 PUT，检查失败时将它归档为 `not-sent`，保留备份、预期配置和原因异常。`not-sent` 只说明本次未发送请求，不表示外部环境没有变化，也不触发重试。归档失败仍保留 pending，重开管理器后在任何 API 请求之前阻止再执行。intent 本身写入/校验失败时不自动归档；已经发出请求的未知结果仍保留 pending。

这项排序修复收窄已复现的检查间隔；App 租约和短暂独占读取仍不能提供 VMware 全局锁或原子比较更新保证。

## 新增模拟范围

| 范围 | 实际验证 |
| --- | --- |
| 未准入字段 | MAC 声明、生成地址和两种 UUID 键单独或与内存混合时，预览拒绝且无事务旁文件；HTTP 客户端对不同字段顺序同样在发送前拒绝 |
| 计划与确认隔离 | M10 计划 ID 不能确认数值预览；计划不可执行，失败不生成历史或 API 请求 |
| 数值往返与历史重开 | 使用现有精确生产策略在内存中的模拟 API 应用 CPU/内存，保留原身份字节；历史重开后的数值恢复保留后来改变的 MAC/UUID 声明 |
| 预览失效 | 身份声明在预览后改变，数值写入在备份前拒绝；恢复需新确认，恢复预览之后改变 UUID 同样拒绝 |
| 最后本地写入期间变化 | MAC/UUID、运行/挂起/未知状态、注册映射、API 设置、安装版本或文件占用变化，均在 PUT 前拒绝并保留 not-sent 证据 |
| 归档失败 | 用本轮临时目录模拟目标冲突；pending 原字节保留，重开不能绕过 |
| 请求后的变化和响应丢失 | PUT 后改变身份声明或丢失响应时保留 pending、备份及后来的配置；新计划/导出不能清除 pending，重开不能重放 |

新增 `ApiIdentityBoundaryTests.cs` 通过同一测试类的 partial 文件复用原模拟 HTTP、临时文件及故障替身。没有为未验证身份字段编造模拟写入通道。身份变更是测试注入，用于验证保留/拒绝规则，不是产品写入功能。

既有全部测试同时回归，覆盖版本/值限制、状态拒绝、备份/暂存/intent 故障、取消、跨实例及子进程租约、延迟服务端结果、防重放、历史兼容及恢复冲突。

## 命令与证据

使用已有共享 SDK `10.0.401` / .NET `10.0.12` 和单并发构建。没有安装依赖、修改版本或关闭审计。

```powershell
dotnet build VMIdentityLab.sln -c Release --no-restore -m:1 --disable-build-servers --tl:off
dotnet test --project tests/VMIdentityLab.UnitTests/VMIdentityLab.UnitTests.csproj -c Release --no-build --parallel none --report-xunit-trx --report-xunit-trx-filename m11-unit.trx --results-directory artifacts/test-results/20261019-m11
dotnet test --project tests/VMIdentityLab.IntegrationTests/VMIdentityLab.IntegrationTests.csproj -c Release --no-build --parallel none --report-xunit-trx --report-xunit-trx-filename m11-files.trx --results-directory artifacts/test-results/20261019-m11
```

实际调用使用主项目 `artifacts/tooling/dotnet/dotnet.exe` 及共享 `cli-home`、`packages`。成功日志在当前 worktree `local-data/m11-20261019/release-build.log`、`unit-tests.log`、`integration-tests.log`；TRX 在 `artifacts/test-results/20261019-m11/`。首次复现保留 `reproduction-build.log`、`reproduction-tests.log` 和 `artifacts/test-results/20261019-m11-red/m11-reproduction.trx`，不与最终通过混同。日志、TRX 和临时产物不提交。

本轮没有新增界面，未重跑 M10 GUI。未执行真实 API、客户机、独立副本、全新 OS、远程 CI、自包含发行包或 Release 验收；旧 M07/M09/M10 实测保持原来源，不能代替本轮实机结果。

## 仍未完成

[M11 任务](../tasks/modules/M11-identity-transactions.md)五项中，仅现有 M07 拒绝保护与未决/恢复保留两项在当前范围验证通过。MAC/UUID 的逐字段目标版本依据、允许值、官方读写通道、完整恢复范围及独立副本往返仍缺失；因此不实现猜测的后端映射，不批准新生产字段，不宣称 M11 完整验收。

后续先补足候选字段的支持与恢复证据，再设计有限适配及模拟；确需真实实验时另行准备具体方案和授权。M12/M13 未启动，不合并、释放当前 worktree、推送或发布。
