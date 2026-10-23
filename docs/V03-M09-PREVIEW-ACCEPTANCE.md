# M09：PNG 预览修复与补充验收

日期：2026-10-18，Asia/Taipei（UTC+08:00）。起点 `4f4c4b9`，修复时使用 `codex/v03-m08-20261018` worktree。本文补充 [M09 原验收记录](V03-M09-RESULTS.md)，不覆盖原始失败证据。

2026-10-19 本地整合后，源码与下述私有证据均已回到主项目，worktree 已释放。证据保持原相对路径及逐文件 SHA256，原绝对路径仅作历史记录；原测试队列和独占输出脚本不重放。

## 定位结论

原代码把有边框的 1220×800 窗口内的内容，重新 Measure/Arrange 后直接捕获为同尺寸 PNG。窗口客户区实际更小，后续窗口布局又恢复了客户区约束，原图片底部与边缘含透明区域。旧 `identity-en-v2.png` 实际存在 55,822 个 alpha=0 像素，底部背景为透明，能够复现并由像素检查拒绝。

本次还纠正了此前仅凭查看工具判断“缺字/整图透明”的结论。三个在查看工具中表现不同的英语试验文件，其文件字节完全相同，SHA256 均为 `670d22d6b09994e121802242e783a7e0f1e5203c4abb36ac0c7a5b414f1b2d16`；WPF 和独立 PNG 解码器均能读到 `true`、`Microsoft Hv`、`16` 对应区域的实际文字像素。不能把这些显示差异归因为 WPF 字体、显卡或某个透明度设置；查看链路显示异常的根因尚未确定，未修改 Codex 或宿主机图形设置。

## 实际修复

- 仅在开发预览窗口使用无边框、固定内容尺寸，移到虚拟桌面范围外，不激活、不加入任务栏；正常 GUI 的窗口和表格虚拟化不变。
- 等待窗口 `ContentRendered`，不对已归窗口管理的子树强制执行另一套 Measure/Arrange；等待上限为 10 秒。
- 捕获后输出 24 位 RGB PNG，预览不包含透明通道；不保留文字抗锯齿产生的一位 alpha 舍入误差。
- 在 `finally` 中关闭预览窗口，保存失败也退出。复用现有禁止覆盖输出的文件保存行为。
- 增加开发参数 `--preview-tab identity`；只允许与 `--render-preview --page 1` 组合，原页面参数保持原含义。
- 使用原有 WPF API，无新框架、驱动、服务或图像处理依赖。临时 VisualBrush/HwndSource/软件渲染等试验均未保留；本次不能由它们推导独立根因。

## 实际检查

| 项目 | 结果与范围 |
| --- | --- |
| Release 构建 | 0 警告、0 错误；SDK 10.0.401 |
| 单元 / 模拟集成 | 140/140、113/113，无跳过；本轮重新执行 |
| 开发 PNG 回归 | 24/24：中英文 × 五个原页面及身份页 × 两次独立进程 |
| 像素回归 | 固定 1220×800、完整背景、alpha=255；身份页三个观测区域有实际文字像素 |
| 独立解码 | 中英文 RGB PNG（colorType=2）、976,000 像素无透明；三个观测区域像素计数分别为65、181、36，与原生解码一致 |
| 旧失败图片 | 原透明边界 PNG 被拒绝，保留原文件 |
| 自包含包本机检查 | 8/8：14/17项普通权限采集、双语 PNG、旧失败图片拒绝、禁止覆盖、两种非法参数；Runtime 10.0.12 |
| 客户机 | 2026-10-19现有Win10教育版18363通过；首次小桌面PNG失败及追加修复见下节 |
| 新远程 CI / CI 原包客户机 | 676d0ec实际CI140/113、宿主9/9、客户机28/28及清理通过；见[原包记录](V03-M09-CI-GUEST-ACCEPTANCE.md) |
| 全新 OS | 未执行；不创建或覆盖系统来假称干净环境通过 |

自包含包在工具沙箱中采集 WMI 时返回 `0x80041003`；同期 Defender 1116/1117 定向读取为 `NoMatchingEventsFound`。正常普通用户会话复核后，三个可选来源均 success 且仍脱敏；本轮没有提升 App 管理员权限或修改安全设置。保留沙箱下 accessDenied 报告，不把它当采集成功。

## 复现入口

在项目根目录使用已有 SDK；下面的 `dotnet` 指项目本地 SDK。输出必须是新文件。

```powershell
dotnet build VMIdentityLab.sln -c Release -m:1 --disable-build-servers
dotnet src/VMIdentityLab.App/bin/Release/net10.0-windows/VMIdentityLab.App.dll --render-preview --output local-data/new-identity.png --language en-US --page 1 --preview-tab identity
dotnet tools/desktop/VMIdentityLab.DesktopTest/bin/Release/net10.0-windows/VMIdentityLab.DesktopTest.dll verify-preview local-data/new-identity.png identity
```

测试驱动仍不随正式软件分发。像素检查用于拒绝空白、透明和关键字段缺失，不能替代所有操作系统、DPI、字体和主题组合的人工视觉验收。

私有证据目录 `local-data/m09-preview-fix-20261018/`：`acceptance-rgb/results.json`、`independent-png-check.jsonl`、`package-host-native/results.json`、`GUEST-ACCEPTANCE-PLAN.md`。TRX 位于 `artifacts/test-results/20261018-m09-preview/`。真实报告、凭据、控件树及失败 PNG 不进入公开源码。

本轮修复不改变 M09 的只读身份分析边界，不批准 MAC/UUID 写入。版本仍为未发布的 0.2.0-alpha.1 开发构建，未创建 v0.3 Release。

平台依据：[Window.ContentRendered](https://learn.microsoft.com/en-us/dotnet/api/system.windows.window.contentrendered?view=windowsdesktop-10.0)、[RenderTargetBitmap](https://learn.microsoft.com/en-us/dotnet/api/system.windows.media.imaging.rendertargetbitmap?view=windowsdesktop-10.0)。具体故障结论以上述本机对照为依据。

## 2026-10-19：客户机小桌面的追加缺陷

首个 M09 CI 来源 `22ec00a` 的构建与 140/113 自动化通过，原包在客户机的采集、GUI、旧格式、脱敏及真实对比均取得独立证据；中文身份 PNG 的严格像素检查却失败，不能将这个包的客户机补充验收标为全部通过。原文件及错误保留在 `local-data/m09-acceptance-20261019/guest-session-interactive/`。

客户机当时虚拟桌面为 1289×731，Win32 最大跟踪高度为 751；生成的 RGB PNG 虽然有 1220×800、不透明背景和完整前三观测字形，底部约 50 像素却为黑色。WPF 的 [Window 源码（10.0.13）](https://github.com/dotnet/wpf/blob/v10.0.13/src/Microsoft.DotNet.Wpf/src/PresentationFramework/System/Windows/Window.cs)在 `GetWindowMinMax` 和 `ArrangeOverride` 中同时使用 Win32 跟踪限制与窗口最小值约束内容，`Height=800` 本身不保证内容仍为 800。

追加修复仅在预览窗口设置 `MinWidth=1220`、`MinHeight=800`，使预览画布在较小桌面下仍完整布局；捕获前核对实际内容尺寸，不匹配时拒绝生成局部图片。普通交互窗口的最小尺寸、布局、采集及配置功能均未改变。

实际验证分为两步：先以本地修复通过 App Release 0 警告/0 错误、宿主两语言六视图12/12及独立客户机开发副本中的双语 PNG；随后按具体授权上传676d0ec，实际CI140/113和原始产物校验通过，新原包宿主9/9、客户机28/28通过，同一小桌面下的中英文PNG均完整。开发副本与CI原包分别取证，原失败PNG不覆盖；详见[原包与清理记录](V03-M09-CI-GUEST-ACCEPTANCE.md)。
