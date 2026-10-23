# v0.3.0-alpha.1 发布审阅方案

2026-10-23执行完成：用户明确允许发布后，已按本方案公开预发布并核对tag/三资产，M13为5/5，见[实际发布回执](GITHUB-RELEASE-v0.3.0-alpha.1.md)。下方保留2026-10-22准备的具体范围；文档本身不构成后续新发布授权。

## 固定来源和资产

- 仓库：Aleka579/VMIL；新tag：`v0.3.0-alpha.1`，Pre-release，`make_latest=false`。
- 精确tag目标：`26656db690d594bebcf2d69d273bfcd5dfc376bc`，保持产品原来源；发布分支提交不作为产品来源。
- 原CI：[39616530060](https://github.com/Aleka579/VMIL/actions/runs/39616530060)，产品artifact12354145051；240单元/206模拟集成、Release零警告/错误。
- 原包：65,286,300字节/413文件，SDK10.0.402、两运行时10.0.13；不重新构建、重压缩或混入开发驱动。
- 目标验收：Win11 Build26100普通权限14/43；[Win10教育版Build18363/x64客户机28/50](V03-M13-CI-GUEST-ACCEPTANCE.md)，测试后413文件/原ZIP未变。临时目录、凭据已清理；VM保持运行。

| 资产 | 字节 | SHA256 |
| --- | ---: | --- |
| VMIdentityLab-0.3.0-alpha.1-win-x64.zip | 65286300 | `b607f1cb61b16c19e2fd788a5dfa3f45f0ef74ca3117bc8d170e9072e73ef779` |
| VMIdentityLab-0.3.0-alpha.1-win-x64.zip.sha256 | 107 | `857c29b1d977348a503199a9f625fed347c9f21b6b1bb70fb7c9840cd7cc728b` |
| VMIdentityLab-0.3.0-alpha.1-win-x64.files.sha256 | 41128 | `f1d82dc4257605a1f766f0e66d021de3b56ad7f998472929a5588411c6677de4` |

三资产已在本地审阅目录保存。逐文件清单保留原CI ZIP条目顺序和CRLF，复现结果与已审阅文本逐字节相同。说明为[中英文发布文案](../.github/release-notes/v0.3.0-alpha.1.md)。

## 具体远程操作

1. 从公开26656db创建新的`codex/release-v03-alpha1-20261022`分支，只增加新固定发布工作流/脚本、公开验收摘要及更新的发布文档；保留所有生产源码和测试blob，不上传内部Git历史、AGENTS、WORK_LOG、进度日志、真实VM数据或原始证据。
2. 该分支的push触发新的`publish-v03-alpha1.yml`。沿用windows-2025和固定官方checkout，顶层只读，发布job使用仓库范围的actions:read/contents:write临时令牌；不保存令牌。现有codex分支CI也可能按既有规则运行，但Release固定使用39616530060原包。
3. `Publish-VerifiedV03Alpha.ps1`重新核对成功CI、来源、artifact未过期、大小/SHA、BUILD-INFO、运行时及413条摘要。目标tag或Release已存在时停止，不覆盖。
4. 创建新草稿，上传并核对三份服务器资产大小/digest后才公开为Pre-release；回读tag精确目标和发布状态，保留回执。
5. 发布后仅同步公开文档的实际状态和脱敏回执，不更改发布脚本/说明触发路径或产品原包；最后更新本地日志/进度并提交。

不修改公开main、旧tag、旧v0.2 Release或本机VM；该范围不含新的配置写入/电源/副本测试。源代码归属MIT / Daniel Waltz，私密安全渠道保持原确认。

## 失败与恢复

本地脚本只静态解析，不携带实际令牌或执行远程请求。发布器复用已成功的v0.2实现，新版本常量/摘要及原ZIP顺序已离线核对；静态检查不冒充实际发布通过。

上传失败则保留草稿和回执，停止后续公开动作；出现未知结果先读取实际状态，不重放发布或强移tag。公开后的撤回、删除或更换资产须按新的具体问题处理，不能覆盖原包来伪装成功。全新OS、其他Build/VMware版本/拓扑及联网未验证，身份准入保持原有限范围。
