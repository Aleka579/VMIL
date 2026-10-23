# 本机候选 App 验收宿主

M12另提供`VMIdentityLab.Acceptance.exe --validation-fixture <新的私有目录>`，只生成两类字段的三阶段合成快照、旧报告和配对记录，并核对真实双语资源/空输入状态；不构造原生采集器或调用VMware。普通App的实际导入/导出/错误失效另由现有桌面驱动验收，见[M12结果](../../docs/V03-M12-RESULTS.md)。夹具不是实机证据，不进入发布包。

开发测试工具，不属于解决方案或正式发布包。它引用实际App的MainWindow、MainViewModel、确认/凭据窗口、官方API事务和私有历史代码；正常App无候选命令行、环境或导入开关，本工具不改变发布策略。

2026-10-20夜[实际验收](../../docs/V03-M11-APP-ACCEPTANCE.md)完成后，正常App策略已按独立证据登记有限已有字段；该决定不由本工具或清单触发。当前自检15项验证正常策略有限批准、专用宿主目标隔离、版本拒绝、双语资源与无旁文件/历史，均仅使用合成文件。已结束的真实清单及副本不能重放。

仅本工具程序集通过 internal 构造函数注入候选策略、一个确切VMX路径和本轮私有历史目录。其他目标仍不可提交；数值字段不由此测试策略批准。内存中的预览确认ID、版本/状态/占用、备份/intent、独立复核及未决防重放保持原流程。单一目标绑定不是用户授权的替代。

```powershell
dotnet build tools/acceptance/VMIdentityLab.Acceptance/VMIdentityLab.Acceptance.csproj -c Release -m:1 --disable-build-servers
VMIdentityLab.Acceptance.exe --self-test <新的私有证据目录>
VMIdentityLab.Acceptance.exe <本轮私有manifest.json>
```

自检只创建合成文件，不连接VMware、不请求凭据或修改VM。清单包含targetPath、historyDirectory、installationDirectory、vmwareSha256和vmrestSha256，拒绝未知属性；历史目录限定在清单目录之下。实际写入前仍须独立副本/字段证据及本轮具体方案授权。工具拒绝管理员运行，不执行VM电源或快照操作；连接凭据只通过原App本机窗口输入。

本工具采用自己的Application宿主，复用实际主题/语言资源，避免将另一x:Class的App.xaml加载到派生应用。App语言字典使用[微软文档定义的引用程序集pack URI](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/app-development/pack-uris-in-wpf#referenced-assembly-resource-file)，既支持正常入口，也支持独立验收宿主；异常记录为失败退出，不产生未处理异常弹窗。

本轮具体目标、基线准备、源VM电源恢复和清理步骤只保存在忽略的local-data验收方案，不把真实路径、VMX、标识或凭据写入本说明。模拟/本机审阅不等于真实App或客户机验收。
