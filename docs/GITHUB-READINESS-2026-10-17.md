# GitHub开源与CI验收

日期：2026-10-17，Asia/Taipei。源码已公开，REL-03/04已有实际通过证据，总计37/37；尚未创建GitHub Release。后续提交由对应Actions继续验证，不把历史运行当作未来提交的结果。

## 仓库、授权与安全渠道

- 仓库：[Aleka579/VMIL](https://github.com/Aleka579/VMIL)，公开，默认分支main。插件实际登录Aleka579/用户ID260032387，目标仓库ID1423187964；导入前原生Git读取确认没有任何分支。
- 用户明确确认公开仓库及私密报告已创建，授权上传已审阅源码、运行CI及必要修复，并由Daniel Waltz维护报告渠道。本次按方案导入空main，不覆盖其他仓库或已有用户内容。
- 私密渠道：[Report a vulnerability](https://github.com/Aleka579/VMIL/security/advisories/new)。官方GET `/repos/Aleka579/VMIL/private-vulnerability-reporting`实际返回`enabled: true`，公开Advisories页面按钮和对应链接可见。
- SECURITY.md和中英文使用说明记录实际入口与维护者。没有发送模拟漏洞、邮件或通知；noreply提交邮箱不作为报告邮箱。

## 源码来源

- 已审阅源码包：`VMIL-caa7f1f-source-review.zip`，149文件/333973字节，SHA256 `3896119f4921ce3e49d86f9367b70299f1d811bc6fd555193aefdd4ce2ec71a1`。本地来源提交`caa7f1fd40757d2789713fa8c376f69f8a23ecd3`；应用和测试与已验收`5de4ec4`基线无差异。
- 仅公开AGENTS本机事件详情和WORK_LOG改为脱敏文本，停止规则保留；原本地记录和Git历史保持不变。其他内容与来源提交逐字节一致，不包含真实VM、内部历史、凭据、报告、缓存或二进制产物。
- 先以PUBLIC-SOURCE.json建立无父初始化提交`2a8a5c9`，再导入[完整源码0c471bfd](https://github.com/Aleka579/VMIL/commit/0c471bfdf85ed492921fa43a3d393a6cd5edb19c)。完整tree为`75ffe7cacf4628903aa93ac9e24e4f4eefd9fb4b`；官方递归tree回读149个blob，Git SHA全部等于审阅包预期值。
- PUBLIC-SOURCE.json描述首次导入快照的148个内容文件及两份脱敏替换，不是后续提交的动态文件清单。后续修改由公开Git提交追踪；内部维护分支不直接推送到公开main。

## 实际GitHub Actions

[首次运行39024064418](https://github.com/Aleka579/VMIL/actions/runs/39024064418)，event=push，源码`0c471bfdf85ed492921fa43a3d393a6cd5edb19c`，2026-10-17 18:13:15–18:15:25 UTC+08:00，最终`completed / success`。Windows Server 2025托管Runner执行，不连接任何真实VM。

| 检查 | 实际结果 |
| --- | --- |
| 固定官方Action / .NET安装 | checkout v7.0.1、setup-dotnet v6.0.0、upload-artifact v7.0.2成功；完整SHA固定 |
| Release构建 | 0警告、0错误 |
| 单元测试 | 127/127，0失败、0未执行 |
| 模拟集成/临时文件测试 | 100/100，0失败、0未执行 |
| 自包含publish、许可/指南、BUILD-INFO | 全部步骤成功 |
| TRX产物 | 35083字节，artifact ID12097702746；下载后SHA/CRC及两份实际XML计数核对通过 |
| 应用CI产物 | GitHub报告65196230字节，artifact ID12097767604；成功上传，未将其当作客户机验收包 |

TRX ZIP SHA256：`847cbc1c33fdd6639ebaa6766cdde676d0952acd2e10bbb478c7ee504e50881f`。GitHub公布的首轮应用产物ZIP SHA256：`2299f9f97acee5987a58aafbd55d1778a8bc7d767a1e370691a8b32dc939ace8`。不同CI运行的产物有自己的提交/时间/哈希，不能沿用此处数据。

工作流使用`contents: read`、checkout不持久化凭据、单并行构建/测试，无VMware、客户机或私有报告依赖。它保留CI验证产物，不自动创建Release。最终文档同步后的运行也须核实，结果由对应[Actions记录](https://github.com/Aleka579/VMIL/actions)及维护日志追踪。

## 验收与交付范围

| 项目 | 结果与证据 |
| --- | --- |
| REL-03 | 通过；实际成功运行、job日志、两份下载TRX和CI产物元数据 |
| REL-04 | 通过；MIT/Daniel Waltz、中英说明/第三方许可、安全政策、实际私密入口与维护者确认 |
| 本地/真实VM验收 | 保留原5de验证包身份、127/100及客户机十六项证据，见[FULL17](V02-FULL-TEST-RESULTS-2026-10-17.md) |
| GitHub Release / tag | 未授权、未创建；当前仍为0.2.0-alpha.1 |
| CI产物的客户机运行 | 未执行；不能由本地5de包实测自动推定 |
| 全新OS、Windows11客户机、其他VMware构建/拓扑/数值 | 未据此验证；保持原兼容性和有限写入范围 |

公开源码、CI验证产物和正式Release分别记录。将来发布仍需确定具体版本/标签/资产及支持范围；不因37/37自动发布、改版本或启动真实VM。

## 证据入口

私有目录`local-data/release-prep-20261017/`保存`source-review-check.json`、`github-import-check.json`、`github-import-run-final.json`、`github-import-job.log`、`initial-ci-test-results.zip`及其`-check.json`。原始路径/日志不进入公开源码。公开可复核证据为上述提交、运行、合成TRX和报告入口。

## 官方依据

- [私密漏洞报告配置](https://docs.github.com/en/code-security/how-tos/report-and-fix-vulnerabilities/configure-vulnerability-reporting/configure-for-a-repository)：公开仓库管理员实际启用，独立于SECURITY.md。
- [仓库REST接口](https://docs.github.com/en/rest/repos/repos#check-if-private-vulnerability-reporting-is-enabled-for-a-repository)：实际开关回读。
- Action完整标签SHA与windows-2025维护依据见[依赖清单](DEPENDENCIES.md)；本轮没有升级依赖。
