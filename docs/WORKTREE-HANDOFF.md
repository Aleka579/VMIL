# 主项目合并与 worktree 释放准备

准备日期：2026-10-11；实际释放记录：2026-10-12，Asia/Taipei。成果已合并main，旧worktree已解除Git登记并清空内容；被其他进程占用的空目录按用户选择保留，清理结束。

## 已完成

- [x] 核实main与工作分支初始均干净；main是成果分支的祖先，没有独立新提交。
- [x] 使用git merge --ff-only快进main到7fd29ad，包含574a8aa及此前环境、宿主/客户机验证成果；最终准备文档随后保存并同步main。
- [x] 保全artifacts与local-data：8658文件、1114888796字节，逐文件SHA256核对，未覆盖主项目原有文件。旧MCP虚拟环境仅保存在私有归档，未作为新运行环境直接复制使用。
- [x] 在main用现有CPython3.14.8离线创建venv，恢复原37项锁定依赖及MCP0.1.0；38份实际安装元数据完全一致，pip check成功，原生扩展导入成功。
- [x] 新环境sys.prefix、base_prefix和sys.path不依赖旧worktree；MCP握手与原7个读取方法目录通过，实际运行列表与Tools两次读取成功，目标仍运行、Tools为running。
- [x] 仅迁移用户级、旧项目级和main项目级该MCP的command/cwd；7个读取白名单、参数、环境及其他设置保持。原配置备份在用户Codex backups目录，凭据/完整配置不进入Git。
- [x] 新完整验证ZIP在main的SHA256仍为674BEA31BD97DB00C1F2D19B55AEA32A2884F47E5E191E20EBFB0C67A32AE12B；原Alpha ZIP未覆盖。

没有改应用源码或依赖版本，81单元/10文件与v0.1 25/25复用已执行的结果；没有将本次复制或MCP迁移冒充新的应用/客户机功能测试。正式发布仍需远程CI、私密渠道与正式新版包，v0.2未启动。

## 主项目入口

在git worktree list列出的main目录继续工作；机器实际路径记录于私有交接结果。源码、唯一WORK_LOG.md、tasks/PROGRESS.md和验证文档已合并。

| 内容 | main内入口 |
| --- | --- |
| 最新完整自包含验证程序 | artifacts/publish/readiness-20261011/VMIdentityLab.App.exe |
| 最新验证ZIP | artifacts/test-packages/VMIdentityLab-validation-20261011.zip |
| 桌面驱动 | artifacts/publish/guest-desktop-typed/ |
| Python解释器/新环境 | artifacts/tooling/python-3.14.8/tools/ 与 artifacts/tooling/mcp-venv/ |
| 离线wheel库 | artifacts/tooling/handoff-wheels-20261011/，原固定版本，未联网下载 |
| 原始客户机/宿主证据 | local-data/test-results/，原路径字节保留 |
| 初始复制清单与旧venv | local-data/handoff/20261011-145129/ 与 original-mcp-venv/ |
| 最终迁移结果 | local-data/handoff/20261011-145129/final/：copy-result、wheelhouse-result、main-mcp-check、config-migration-check及最终检查 |

上述目录均为私有/生成内容，Git忽略。源码项目下bin/obj和工具的编译中间文件可重建，没有当作独有数据归档。历史证据中的旧绝对路径保持原样；它们是当时执行记录，不是当前运行配置。私有辅助脚本是当次操作记录，不从main盲目重跑复制或配置迁移脚本。

## 2026-10-12 实际释放结果

- [x] 本轮从main执行操作；现有两组MCP启动器/解释器均在main，旧路径运行程序为0，昨日六个旧PID记录不再代表当前状态。
- [x] main包含释放前35ac593及全部成果；两个checkout初始干净、差异0/0，8665文件/1118095432字节保全复核通过，新增1份复核脚本补存到main。
- [x] 精确Git登记路径、非重解析点与祖先关系核实后，执行非强制git worktree remove。Git登记和目录内容已移除，仅剩main工作区；命令因空目录删除失败返回255，该失败保留记录。
- [x] 拒绝访问后先查Defender1116/1117（无匹配事件）、目录内容和ACL；一次非递归空目录删除明确报告被其他进程占用，未改变安全设置或终止进程。
- [x] 用户明确选择“本次保留空目录，结束清理”，因此空目录保留，不再尝试或安排后台删除。成果分支仍保留，源码/验证产物/证据均在main。

最初的“本轮保留worktree”描述属于2026-10-11准备阶段；现已由上述实际结果替代。空目录不是Git登记的worktree。后续在main继续开发，当前清理任务已按用户修订范围结束。

释放证据：local-data/worktree-release/20261012-140138/preservation-check.json、supplement/与release-result.json。原有移除前复核要求保留在WORK_LOG历史中；没有删除父目录、使用--force/git clean或影响VM。

## 配置依据与证据

使用OpenAI Docs核对[官方MCP配置说明](https://learn.chatgpt.com/docs/extend/mcp?surface=cli)：用户/项目配置分别保存，command与cwd描述启动路径，enabled_tools限定可调用方法。本轮仅迁移已验证路径，未增加方法或改变审批规则。

15:03:46复制校验、15:06:46离线wheel补全、15:11:42新环境/读取验证、15:13:03三处注册迁移均有私有结果。首次wheel盘点缺pip；核实同一CPython内置26.2.1后补齐，保留该失败/修复记录。实际测试与版本状态见[就绪表](../RELEASE-READINESS.md)、[v0.1结果](READINESS-TEST-RESULTS-2026-10-11.md)及唯一工作日志。
