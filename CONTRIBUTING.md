# 贡献指南

项目使用C#14/.NET10、WPF及xUnit，版本由global.json与Directory.Build.props统一。Core负责报告/隐私/比较，Windows负责采集，VMware负责VMX/状态/配置，App负责交互；保持四层边界，不引入与当前需求无关的抽象或依赖。

在Windows上使用匹配SDK，先restore，再运行：

```powershell
dotnet build VMIdentityLab.sln -c Release -m:1 --disable-build-servers
dotnet test --project tests/VMIdentityLab.UnitTests/VMIdentityLab.UnitTests.csproj -c Release --no-build --parallel none
dotnet test --project tests/VMIdentityLab.IntegrationTests/VMIdentityLab.IntegrationTests.csproj -c Release --no-build --parallel none
```

单元与临时文件测试使用合成数据，不需要运行VM。GUI变更还须实际启动/操作或渲染查看；编译成功不能代替交互验证。记录实际执行命令、结果及未验证项。CI不连接个人VM或读取私有报告。

只提交原创代码、必要文档和合成样例。不要提交真实VMX、磁盘/快照、个人报告、备份/恢复历史、路径标识、凭据、令牌、缓存或生成产物。不要自动关闭VM、删除锁文件、改变安全设置或放行未验证的配置；遵守AGENTS.md的授权和Defender停止规则。

配置候选需要目标版本官方证据与独立副本实测后才能进入发布策略；测试审批策略只能留在测试项目，不能加产品开关。当前仅批准17.5.2.23775571/hardware19/每插槽2核、4096↔6144MiB和2↔4vCPU；其他范围及旧文件写入保持拒绝。不能为了演示放宽策略或把单次竞争/本地租约当全局互斥证明。

描述具体问题、最终行为与验证，指出真实环境验证的范围。开源原创代码采用MIT；贡献应具有授予该许可的权利，新增依赖注明维护依据与许可。安全问题按SECURITY.md处理；私密渠道尚未配置时不要把敏感材料发到公开Issue。
