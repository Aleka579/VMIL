# 本地环境配置

日期：2026-10-10。用户只要求配置环境，不开始v0.1测试。应用验收仍为19/37，本文件的环境检查不能计作宿主/客户机功能验收。

后续更新（2026-10-11）：宿主/客户机真实报告闭环、17.5.2版本分类、本机官方API四态及声明的无SDK/系统.NET目标通过；v0.1 25/25、总体29/37。原生vmrun临时认证完成客体执行/复制，MCP仍只开放7个读取方法；App采用.NET客户端，不将其计作MCP REST方法通过。用户授权的软电源序列结束后原目标已恢复运行，测试进程/认证会话/临时API均已结束。以下“未调用/未开始”是环境配置当天记录，最新事实见[宿主](HOST-TEST-RESULTS-2026-10-11.md)、[客户机](GUEST-TEST-RESULTS-2026-10-11.md)、[本轮](READINESS-TEST-RESULTS-2026-10-11.md)与[验收索引](../tasks/PROGRESS.md)。

迁移更新（2026-10-11）：成果已快进main，产物/私有证据已保全；main内CPython3.14.8与新venv用本地原固定wheel离线重建，38项版本、pip check、原生导入、握手和两个读取调用通过。用户与项目注册的command/cwd已迁到main，其余配置和7工具白名单保持；现有旧连接仍须重载。实际路径、备份及最终移除条件见[交接清单](WORKTREE-HANDOFF.md)。

释放更新（2026-10-12）：旧MCP连接已改为main运行；worktree Git登记与内容已移除。Windows占用的空目录按用户选择保留并结束清理，成果分支和已保全数据仍在。实际失败/诊断记录见[交接清单](WORKTREE-HANDOFF.md)。

## 已部署环境

| 项目 | 实际版本/入口 | 状态 |
| --- | --- | --- |
| CPython x64 | 3.14.8，artifacts/tooling/python-3.14.8/tools/python.exe | 官方NuGet项目内发行，PSF签名Valid，版本已检查 |
| 隔离Python环境 | artifacts/tooling/mcp-venv/Scripts/python.exe | 创建成功，pip26.2.1 |
| Python MCP SDK | 1.30.0 | 安装/依赖检查通过 |
| httpx | 0.28.1 | 安装/依赖检查通过 |
| hatchling | 1.32.4 | 本地构建后端已安装 |
| 本地VMware MCP包 | vmware-mcp0.1.0 | 从用户下载源码构建wheel并安装，运行代码未修改 |
| VMware/vmrun | 17.5.2 build-23775571 | 文件版本已核实，工具未调用 |
| .NET SDK/Runtime | 既有SDK10.0.401、Runtime10.0.12 | 原工具保留，本轮不构建或测试应用 |
| PowerShell | 7.6.6，artifacts/tooling/powershell-7.6.6/pwsh.dll | 官方框架依赖ZIP哈希匹配，复用.NET10.0.12，实际版本检查通过 |

具体Python依赖版本见[requirements.lock.txt](../tools/mcp/requirements.lock.txt)；该锁定结果包含pip/构建工具，不含单独构建的本地MCP包。wheel和安装报告位于忽略的artifacts/tooling/，机器路径、私有证据不提交。

PowerShell框架依赖ZIP为23012318字节，SHA256 EA3C73AC3BF7AFA07432C65B8D9F16B8945BEFA216CEC38A51B6E213DC8FA709，与微软发布匹配。runtimeconfig要求.NET10，实际复用10.0.12；以既有dotnet执行该pwsh.dll并仅查询版本，结果7.6.6。项目工具已准备，Codex自带7.6.5解释器保持其运行时原设置。PowerShell进程设置POWERSHELL_TELEMETRY_OPTOUT=1；没有系统安装、PATH更改或安全配置修改。

## Codex接入

- 注册名称：vm_identity_lab_vmware，stdio方式运行隔离环境中的vmware_mcp.server。
- 用户config.toml已增加该条目；原配置先备份到用户Codex backups目录，其他条目保留。项目.codex/config.toml保存相同配置且已gitignore，无个人路径模板见[codex.config.toml.example](../tools/mcp/codex.config.toml.example)。
- 使用实际VMRUN_PATH，REST主机仅配置为127.0.0.1及端口8697；未配置任何密码，也未启动vmrest。
- 当前只开放vmrun_list、vmrun_tools_state、vmrun_guest_ip、vmrun_file_exists、vmrun_dir_exists、vmrun_ls、vmrun_ps共7个读取工具。没有启用电源、配置、快照、删除、写入或客体程序执行工具。
- 当前会话工具目录已实际出现这7个MCP方法，不把配置文件存在当成已加载证据。

## 实际环境检查

- pip check退出0，无冲突。
- [check_connection.py](../tools/mcp/check_connection.py)只执行initialize和tools/list，检查配置的读取工具存在；没有tools/call入口或应用测试。
- 结果：SDK报告serverInfo版本1.30.0，分发包版本仍0.1.0；协议2025-11-25，实际目录130个工具。上游README声明117，不作为实际数量或能力证明。
- 私有结果：local-data/environment/mcp-handshake.json，工具调用数0，v0.1测试未开始。保留read-only过滤后只开放7个工具，不把130个声明方法写成全部兼容。

## 重建入口

1. 按Python官方Windows文档下载固定版本NuGet发行到项目工具目录，核对来源/签名；不依赖系统PATH。
2. 用该解释器创建artifacts/tooling/mcp-venv。
3. 从[requirements.lock.txt](../tools/mcp/requirements.lock.txt)安装锁定版本；[requirements.in](../tools/mcp/requirements.in)记录兼容API系列。MCP调用v1 SDK接口，不无条件采用未来主版本。
4. 从下载源构建本地vmware-mcp wheel，安装到隔离环境；解析/构建只涉及MCP环境。
5. 将模板占位符替换为核实后的本机路径，再登记受限MCP配置；更换或释放worktree前先更新注册路径。

[Python官方NuGet说明](https://docs.python.org/3.14/using/windows.html#the-nuget-org-packages)、[PowerShell7.6.6官方发布](https://github.com/PowerShell/PowerShell/releases/tag/v7.6.6)、[OpenAI MCP配置](https://learn.chatgpt.com/docs/extend/mcp?surface=cli)。

## 待用户明确开始测试后的工作

客户机Build、VMware Tools服务、客体认证、真实vmrun能力、可靠电源状态、UI交互和干净环境仍未验证。读取工具的存在不代表已取得客体登录权限。后续测试所需复制/执行工具再按具体任务启用；本轮不启动程序、查询真实VM或改变VM电源/配置。
