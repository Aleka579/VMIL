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

`--user`仅为可选账户提示；未知用户名时省略，在同一个本机原生窗口填写用户名和密码，不猜测账号或另行索要聊天中的密码。

加密VM可显式增加`--prompt-vm-password`：先在“VM Identity Lab 虚拟机加密密码”原生窗口输入VM加密密码，再在原客户机窗口填写Windows账号/登录密码。前者通过本机vmrun帮助明确支持的`-vp`参数传递，仍只存在本次进程内存及短暂原生认证argv，不保存或从VMware/系统凭据库提取；stdout/stderr对两类密码都脱敏。未提供该开关时保持原未加密VM流程。此开关不解密、更改TPM或修改VM配置。

离线验证入口为项目根目录下的`python -B tools/mcp/test_guest_session.py`，使用模拟原生窗口和子进程验证两类认证流程、空值、输出脱敏、超时/异常及队列；不请求真实凭据、不调用VM，临时文件由测试自动清理。

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

客户机证据目录使用已验证的短交互式PowerShell `New-Item -ItemType Directory -ErrorAction Stop`，先确认目标不存在，不使用Force。`cmd /c mkdir`在本机旧验收及M11均返回1且未取得子程序stderr，原因未确认，不再把它当作可用步骤。客户机Restricted策略下不运行准备的ps1文件、不改策略；采用系统本来允许的短交互cmdlet或实际测试EXE。

`vmrun`不转发客户机子程序的标准输出，需写入新的私有证据文件再取回；退出0还须核实实际产物。助手恢复队列前先核对全部旧待执行项及前置文件；不要先发布resume、随后才发布它们需要的目录创建任务，避免旧任务被提前取出。

Tools running及用户已点击“已登录”回复不足以证明VIX已识别该账户的交互会话。新启动后先用相同凭据通过 `runProgramInGuest -interactive` 执行不写文件的 `whoami.exe` 就绪检查，再发布目录/采集步骤；明确返回“用户必须以交互方式登录”时停止依赖，不循环重试或改用管理员会话。

## 复用原CI发布包验收

`guest_acceptance.py --case CASE_DIRECTORY STAGE`复用原CI产品ZIP、同CI四入口驱动和已有合成夹具；不重新打包或构建产品。每轮`CASE.json`显式指定`version`、`productFiles`、`source`、`guestRoot`、`appArchive/appSha256`、`driverArchive/driverSha256`、`fixtures`和`session`。版本/文件数来自该原CI包，不沿用旧Alpha常量；当前环境断言为Win11 x64。`guestRoot`限本轮新建的`C:\Users\Public\Documents\VMIL-...`目录，不能使用已有客户机数据目录；案例和实际命令/结果只放私有路径。

阶段依次为`prepare`、`deploy`、`cli`、`gui`、`finalize`。`prepare`只创建小型路径化合成输入；`deploy`先验证交互登录/新目录/空间，复制原ZIP，再用同盘NTFS硬链接为驱动共享413份运行时及产品文件，避免整套复制。`context`是独立环境读取阶段，仅供已确认普通脚本故障后的继续，不重放部署；恢复队列仍须先核对全部待执行命令，不能用于Defender拦截。

`cli`核对普通权限、14/17项、CIM一致性、脱敏/拒绝覆盖及双语PNG；`gui`使用正常App与既有UIA驱动，核对双语五页、真实采集/导出、能力说明和两字段三阶段合成输入。运行时依据实际加载路径及原CI映像SHA，不依赖本地化原生版本字符串。异步App启动由`Start-Process -Wait`记录真实退出码，控制器另取唯一自有进程PID。

`finalize`仅在上述实际通过后核对413文件/原ZIP、归档最后GUI后的证据、验证取回文件字节一致，再删除本轮客户机新目录并结束凭据。宿主只保留最终归档和摘要/命令记录，移除经归档验证的本轮散件重复下载。任何阶段出现失败或未知结果均先保留并定位，不重复运行已产生独占输出的阶段，不以旧Win10结果代替新客户机验收。该流程不实施真实身份写入、电源或快照操作。

实际执行及失败证据索引见[客户机结果](../../docs/GUEST-TEST-RESULTS-2026-10-11.md)；完整密码/认证argv不得保存或打印。
