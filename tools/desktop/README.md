# 本地桌面集成测试驱动

此开发工具复用.NET10/WPF中的Windows UI Automation及user32标准原生对话框通知，不包含第三方包，不进入应用解决方案和发布包。需要在当前用户交互桌面执行；沙箱账户能读取部分文件不代表拥有该桌面或CIM权限。

```powershell
dotnet build tools/desktop/VMIdentityLab.DesktopTest/VMIdentityLab.DesktopTest.csproj -c Release -m:1 --disable-build-servers
```

命令形式：VMIdentityLab.DesktopTest.exe action processId [argument ...]。调用前设置VMIDENTITYLAB_TEST_LOG为私有jsonl路径可保存操作记录。程序不读取密码字段，不管理VM电源/配置，也不请求管理员权限。

| action | 参数与用途 |
| --- | --- |
| inspect | 新JSON路径；保存指定进程的控件树/值、表格行数及Toggle状态，拒绝覆盖已有证据 |
| invoke | 可见控件名称；调用InvokePattern，拒绝禁用或歧义控件 |
| context-menu | 唯一可见TreeItem名称；选择并回读，核实SysTreeView32或实测ATL:SysTreeView32/进程后PostMessage WM_CONTEXTMENU，使用控件的选择位置、不模拟键盘/鼠标；只展开菜单，随后必须inspect核实动作 |
| check | 确认框AutomationId；仅将唯一可见、启用的CheckBox设为选中并回读状态，执行者仍须事先核实具体授权及差异 |
| focus-window | 指定窗口标题；仅对给定进程的唯一匹配窗口请求焦点，不读取或填写密码 |
| show-window | 用户看不到已存在窗口时，核实唯一标题/进程归属，恢复并移到主屏前方；只对已请求的本轮交互窗口使用，不填写密码 |
| select | 条目名称；选择页面/语言/历史等SelectionItem，唯一屏外条目先通过ScrollItemPattern滚入视图，再选择并回读 |
| scroll | AutomationId、垂直百分比0–100；通过标准ScrollPattern滚动唯一可见控件，补取虚拟化表格未实现行的证据 |
| select-option | 下拉框AutomationId、条目名称；一次调用完成展开、刷新可见选项、选择并验证，避免分次启动客户机驱动时弹出层消失 |
| expand | AutomationId；按已有ExpandCollapsePattern展开语言下拉框或已核实的菜单子项；不以InvokePattern代替子菜单展开 |
| value | 编辑框AutomationId、新值；限定Edit类型并回读验证 |
| typed-value | 原生文件名编辑框ID、新值；核实Edit窗口类/进程归属，用WM_CHAR逐字符输入并回读；Win10实际保存已验证 |
| native-command | 原生控件数字ID；核实Button窗口类和所属进程，再向实际父对话框发送BN_CLICKED |
| wait-dialog | 文件名框ID；默认1001，本机打开框使用1148 |
| wait-ready | 检测按钮当前名称；等待它恢复可用 |
| wait-id | AutomationId；等待确认按钮等控件可用 |
| cancel-collection | 检测按钮名、取消按钮名；实际开始并取消采集 |
| close | 关闭指定测试进程的窗口 |

原生对话框示例：保存/打开ID1、取消ID2、消息框“否”ID7，使用前先inspect核实本机实际控件，不能假定所有Windows版本相同。每个步骤检查退出码，等运行会话结束后再做依赖的检查；UI命令完成还须核实文件/界面状态。

WPF 未选中的页面或页签可能不出现在 UI Automation 树中。先选择对应页面/页签，再检查复选框或 DataGrid；不能在概览页断言检测页控件，或在身份页查找系统观测表。表格行仍需按虚拟化状态滚动取证。

合成 PNG 回归另用 `VMIdentityLab.DesktopTest.exe verify-preview <PNG路径> [identity]`，不传进程ID、不访问桌面或硬件。检查固定1220×800、无透明内容、侧栏/底部背景及标题；`identity` 还检查身份表前三行观测区域确有文字像素。只接受本项目标准预览布局，不能用于推断任意图片或所有DPI下的视觉质量。App用 `--render-preview --output <新PNG> --language en-US --page 1 --preview-tab identity` 生成身份页；省略最后两项仍按原页面参数生成。结果见[M09 PNG验收](../../docs/V03-M09-PREVIEW-ACCEPTANCE.md)。

真实控件树/报告/VMX只存放local-data/，不提交。这里只维护已用于实际验证的操作；本机失败的VMware键盘、扫描码和前台焦点探测不作为可用能力。

M11清理已实际验证Workstation17.5.2的ATL:SysTreeView32库菜单：context-menu仅展开唯一选中探针的菜单，随后inspect核实“移除(R)”及确认框确切对象，不调用“从磁盘中删除”。它不承担VM电源或配置写入。标准消息依据[WM_CONTEXTMENU](https://learn.microsoft.com/en-us/windows/win32/menurc/wm-contextmenu)和[PostMessageW](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-postmessagew)；消息投递成功后仍须检查实际菜单/结果。

Win10 Build18363的保存框显示值与实际采用文件名曾不一致，单独SetFocus未解决；typed-value输入后实际JSON/HTML保存成功。Windows11既有value/native-command流程仍通过。按实际控件选择，不将超时返回等同于操作没有发生，先检查对话框和目标文件再决定下一步。原生字符消息依据[微软WM_CHAR文档](https://learn.microsoft.com/en-us/windows/win32/inputdev/wm-char)。

客户机驱动可用相同.NET运行时自包含发布，与App共享运行时文件；开发驱动不进入正式应用包。临时本机认证和复制入口见[客户机测试工具说明](../mcp/GUEST-TESTING.md)。实际验证见[宿主机结果](../../docs/HOST-TEST-RESULTS-2026-10-11.md)和[客户机结果](../../docs/GUEST-TEST-RESULTS-2026-10-11.md)。
