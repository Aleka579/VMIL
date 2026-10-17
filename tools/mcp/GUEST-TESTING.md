# 临时客户机测试接入

这是独立开发测试工具，不是App功能，也不随正式Release发布。适用于已获授权的现有VM客户机采集、GUI验证和报告传输；不管理VM电源、配置或快照。优先复用既有MCP读取工具；读取过滤没有提供复制/执行能力时，使用同一安装的vmrun。

依赖为本机Windows、vmrun>=17.5.2、Python>=3.14及现有pywin32312（见requirements.lock.txt），客户机须有运行的VMware Tools和有效非空Windows登录密码。当前Windows10客户机使用完整自包含App/驱动目录，无客户机Python/SDK安装要求。

## 准备测试包

用相同Runtime版本自包含发布tools/desktop/VMIdentityLab.DesktopTest，随后执行：

```powershell
python tools/mcp/prepare_guest_package.py --application artifacts/publish/ui-gbk-fix --driver artifacts/publish/guest-desktop-typed --output artifacts/test-packages/guest-new-run.zip --source-baseline SOURCE_COMMIT
```

SOURCE_COMMIT应对应实际编译的应用源码，不能仅因HEAD存在就当作产物来源。工具核对includedFrameworks相同，复制四个驱动入口文件、生成源码基线/DLL哈希清单，拒绝覆盖已有ZIP。完整应用运行时和许可目录保留；开发驱动不进入正式产品打包流程。

## 本机认证会话

```powershell
python tools/mcp/guest_session.py --vmrun VERIFIED_VMRUN_PATH --vmx VERIFIED_TARGET_VMX --user GUEST_ACCOUNT --evidence local-data/test-results/04-v01-validation/NEW_RUN
```

启动时使用Windows CredUI显示本机密码窗口，DO_NOT_PERSIST，不保存凭据。后台启动必须隐藏控制台；密码仅由用户在该本机窗口输入，不能写在命令、队列、聊天或文件中。脚本复用参数数组调用vmrun，不经过宿主shell；密码仍会短暂出现在vmrun原生认证参数中，不能称为无参数暴露或保证Python字符串完全擦除。

每轮使用新的私有evidence目录。session.json保存状态，commands/存放不含凭据的JSON，results/以同名JSON保存操作、参数、开始后耗时、完成时间与退出码。成功进程列表仅保存VMIdentityLab.App.exe行；认证过程不持久化完整客户机进程列表。

| operation | 原生vmrun操作 / arguments |
| --- | --- |
| processes | listProcessesInGuest / [] |
| run | runProgramInGuest / 选项、客户机程序绝对路径、参数 |
| copy_to | CopyFileFromHostToGuest / 宿主源路径、客户机新目标路径 |
| copy_from | CopyFileFromGuestToHost / 客户机源路径、宿主新证据路径 |
| file_exists / dir_exists | 对应存在性查询 / 客户机绝对路径 |
| resume | 普通失败排查完成后的显式恢复 / 无参数 |
| stop | 结束当前临时认证会话 / 无参数 |

队列文件用递增唯一编号，写完后再让会话读取。逐项检查结果，再发送依赖步骤。单次原生命令超时45秒；失败暂停run/copy_to，但独立只读查询/取回证据仍可进行。恢复时已有结果的命令不会重复执行；完成后发送stop并核实Python进程退出，闲置30分钟自动结束。此前Windows10保存对话框需要typed-value交互，见[UIA驱动说明](../desktop/README.md)。

这个助手接受测试程序参数，不是安全沙箱；调用方仍须核实授权与具体目标。Defender提示或涉及安全阻止的错误必须遵守项目AGENTS.md停止规则，不能用resume重试被禁止的操作。VMX、电源、快照、客户机安全策略和用户已有数据的覆盖不因启动本工具而获得授权。

实际执行及失败证据索引见[客户机结果](../../docs/GUEST-TEST-RESULTS-2026-10-11.md)；完整密码/认证argv不得保存或打印。
