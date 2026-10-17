# GitHub接入与剩余发布验收

日期：2026-10-17，Asia/Taipei。状态：本地准备中，未创建仓库、推送或执行远程CI；REL-03/04保持未完成，总计35/37。

## 已确定与待补信息

- 用户指定仓库名为`VMIL`；作者/版权人为Daniel Waltz。公开用户接口和已连接GitHub插件分别核实登录名`Aleka579`、用户ID260032387；拟用目标为`https://github.com/Aleka579/VMIL`。
- 用户已完成GitHub连接。插件实际登录账号为Aleka579，已安装账号列表包含该账号；查询目标仓库返回404，可访问仓库列表为空。这只能说明当前连接无法访问目标，不能断言私有仓库一定不存在。本轮起点`5b7f60f`，当前本地分支`codex/v02-config-management-20261013`，没有remote。
- 当前插件支持源码提交/分支及Actions结果读取，但未提供创建仓库、启用私密漏洞报告的操作。本机未发现GitHub CLI，Git Credential Manager账号查询为空。若仓库未创建，用户需在GitHub原生页面创建并授予连接访问；若已经存在，则核对准确URL与插件授权。不读取其他程序凭据或要求在聊天中提供令牌。
- 本地Git历史包含内部WORK_LOG中的本机路径，公开前使用经过审阅的源码快照；原本地历史保留。公开快照将AGENTS中的本机事件记录改为通用停止规则，并以脱敏交接摘要代替内部WORK_LOG，其余源码、测试与公开文档保持原始内容。实际清单/哈希随快照保存，不直接把内部历史推到公开仓库。
- 拟用渠道为GitHub Private Vulnerability Reporting，维护者和最终链接待用户确认；noreply提交邮箱不作报告邮箱。

## 本地准备

1. 工作流只需Windows托管Runner及.NET，不连接用户VM，不需要VMware、客户机凭据或本机报告。
2. `main`及`codex/**`推送、pull request、手动触发可执行；权限为`contents: read`，checkout不持久化凭据。当前工作分支推送后可以直接取得CI结果，不必先合并。
3. 固定现有官方稳定Action：checkout v7.0.1、setup-dotnet v6.0.0、upload-artifact v7.0.2。2026-10-17复核官方Release与windows-2025清单，无本轮依赖升级；此复核不是远程执行证据。
4. 工作流依次Release构建、两个合成测试项目、自包含publish，保存TRX和包含许可/实际构建元数据的应用目录。CI目录是验证产物，不自动创建Release或变更VM配置。
5. 安全政策、贡献指南、包说明、依赖说明及新版发布文案按已验收的有限API范围修正；旧9cfa28a Alpha及原发布草稿保留原来源。

## 远程执行方案

目标为`Aleka579/VMIL`。公开内容快照完成后，给出实际文件和清单并请求一次明确授权。当前连接仅核实身份和访问状态，不构成公开、上传、设置变更或Release授权。

- 若是新仓库，先准备经审阅源码快照的首次提交；若是已有仓库，先只读检查默认分支、已有文件和权限，再准备独立工作分支的差异，不覆盖原有历史。
- 只上传审阅后的源码、合成测试、文档、工作流和许可证。真实VM、备份、私有证据、二进制缓存、内部工作日志历史及任何凭据不上传。快照与本地Git源提交/清单的对应关系保留，不把快照当原历史的改写。
- 推送具体分支后记录远程提交、Actions运行URL和最终结论。失败先读日志、修正对应问题再复验；推送成功、静态语法检查或下载artifact均不等于CI通过。
- 拟在明确同意的公开仓库启用Private Vulnerability Reporting；该功能不对私有仓库开放，不将私有仓库改为公开作为隐含操作。若用户选择保持私有，则需另行确认可收信的私密邮箱等渠道。
- 原生页面入口：[创建VMIL](https://github.com/new?owner=Aleka579&name=VMIL)；新建时保持空仓库，不额外生成README或许可证。公开后，在仓库Settings → Security → Advanced Security → Private vulnerability reporting启用，并将VMIL加入GitHub连接的仓库访问范围；已存在仓库先核实，不删除重建。维护者拟为Daniel Waltz，仍需明确同意。
- 启用后回读仓库设置并核实“Report a vulnerability”入口。只验证设置和入口，不发送模拟漏洞、邮件或通知其他人；若接入权限不包含仓库管理，由用户在原生GitHub设置中完成，不擅自扩大插件权限。
- 确认维护者和实际私密链接后更新SECURITY.md/文档，再取得最终提交的成功CI；完成REL-03/04后才记37/37。创建GitHub Release仍单独授权，当前没有发布、打tag或上传二进制的指令。

## 完成证据

| 项目 | 必要证据 | 当前状态 |
| --- | --- | --- |
| 目标/权限 | 账号、仓库URL/可见性、授权范围 | 连接账号Aleka579已核实；VMIL查询404/访问列表为空，仓库可见性和具体远程授权未确认 |
| REL-03 | 最终远程提交及对应成功Actions运行URL，实际TRX/产物 | 未执行 |
| REL-04 | 维护者确认、可访问的私密报告入口/配置回读、正确安全政策 | 文案已准备，渠道未启用 |
| 公开Release | 独立发布授权及具体资产/标签 | 未授权/未执行 |

## 官方依据

- [私密漏洞报告配置](https://docs.github.com/en/code-security/how-tos/report-and-fix-vulnerabilities/configure-vulnerability-reporting/configure-for-a-repository)：公开仓库管理员启用后才有私密入口，与SECURITY.md独立。
- [仓库REST接口](https://docs.github.com/en/rest/repos/repos#enable-private-vulnerability-reporting-for-a-repository)：启用需要仓库管理写权限；可回读实际启用状态。
- [checkout v7.0.1](https://github.com/actions/checkout/releases/tag/v7.0.1)、[setup-dotnet v6.0.0](https://github.com/actions/setup-dotnet/releases/tag/v6.0.0)、[upload-artifact v7.0.2](https://github.com/actions/upload-artifact/releases/tag/v7.0.2)、[windows-2025清单](https://github.com/actions/runner-images/blob/main/images/windows/Windows2025-Readme.md)。
