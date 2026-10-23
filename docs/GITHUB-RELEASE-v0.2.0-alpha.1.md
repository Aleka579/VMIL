# v0.2 GitHub Release准备

当前结论：**已于2026-10-18 16:55:04（UTC+08:00）上传并公开预发布**。[v0.2.0-alpha.1 Release](https://github.com/Aleka579/VMIL/releases/tag/v0.2.0-alpha.1)包含三个已核对的资产；[新CI包客户机23/23](CPU-FIX-ACCEPTANCE-2026-10-18.md)，原三项全部通过。

| 设置 | 本候选的具体值 |
| --- | --- |
| 仓库 | [Aleka579/VMIL](https://github.com/Aleka579/VMIL) |
| 拟用tag | v0.2.0-alpha.1 |
| 精确目标提交 | e27543fb5a8519b9333b57af26f95dc3873a732c |
| 标题 | v0.2.0-alpha.1 — VMware API配置管理预览 |
| 类型 | Pre-release；已公开，draft=false、prerelease=true |
| 发布说明 | [中英文草稿](RELEASE-NOTES-v0.2.0-alpha.1.md) |
| 原CI | [39109350478](https://github.com/Aleka579/VMIL/actions/runs/39109350478)，130单元/106模拟集成通过 |
| 审阅目录 | artifacts/releases/github-v0.2.0-alpha.1-e27543f/ |

审阅目录保存原CI ZIP逐字节副本、ZIP的.sha256、包内412文件清单、中英文说明，以及发布前的RELEASE-CANDIDATE.json和release-request.json。旧样稿保留当时未发布状态；新增PUBLISHED-RECEIPT.json记录实际发布ID416338180、精确tag和三份服务器大小/SHA。原包内BUILD-INFO的ci-artifact来源保持真实。

ZIP为65,197,403字节，SHA256 `77ab023491fb677eeaeff2d9860c9c771b718c762444eccd8d4e498d42de27b7`。当前程序集是0.2.0-alpha.1，按该版预发布，不能将相同二进制重标为稳定版0.2.0。

用户本轮明确授权准备完成后上传。[发布任务39113269967](https://github.com/Aleka579/VMIL/actions/runs/39113269967)实际成功，先创建草稿、上传并验证三份资产，再公开。tag及main均回读e27543f，未改变原产品来源。专用release/v0.2.0-alpha.1分支只增加工作流、固定来源脚本、公开发布说明和脱敏验收摘要；原152个源码blob不变，内部历史、工作日志和原始客户机证据未上传。

旧3832草稿及20/23失败证据保留在独立目录，不覆盖历史Alpha包。此次新包客户机只读验证不扩展此前真实配置的有限范围，也不等于全新OS/Windows11验证。

前两次发布任务因CRLF文本格式及空REST数组处理错误，在创建Release前停止；错误已修复，针对性核对通过，失败记录保留。第三次任务全部步骤成功，三个资产的服务器大小与SHA256均与本地审阅稿一致，发布说明正文匹配。

本次上传已完成。后续版本发布或支持范围变化按新任务处理，不自动重建副本、重跑已通过测试或移动当前tag。
