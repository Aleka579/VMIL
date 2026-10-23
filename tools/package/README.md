# 本地 Alpha 打包

不推送、不创建GitHub Release；只接受带BUILD-INFO.json的干净Alpha构建。沿用当前框架和运行时，正式包不含开发驱动、测试记录或真实VM数据。

1. 保存源码提交，从该干净提交以Release/win-x64/self-contained发布到新的空目录，关闭PDB输出。自包含发布固定.NET10.0.13，默认诊断/模拟测试不强制安装新系统运行时。
2. 从实际程序集读取版本/提交，从runtimeconfig读取includedFrameworks；记录SDK、构建时间、sourceState=clean到BUILD-INFO.json。各应用程序集必须对应同一源码提交。
3. 使用已有Python 3.14工具环境执行：

```powershell
python tools/package/create_alpha.py --directory artifacts/publish/NEW-ALPHA --output artifacts/releases/VMIdentityLab-VERSION-win-x64.zip
```

工具复制正式许可、去重第三方许可和包说明，检查非产品内容，生成PACKAGE-FILES.json、ZIP和.sha256。拒绝覆盖已有包、校验文件或打包元数据；失败保留现场，使用新的发布目录处理，不自动清理。

打包后使用已有Python3.14验证（不推送、不修改VM、不创建tag）：

```powershell
python -I tools/package/verify_alpha.py --package artifacts/releases/VMIdentityLab-VERSION-win-x64.zip --expected-version VERSION --expected-source FULL-COMMIT
python -I tools/package/test_alpha.py -v
```

验证脚本检查ZIP CRC、完整清单、逐文件哈希、运行时/来源和许可、非产品及重复/越界路径，不解压或执行ZIP输入；独立离线测试使用合成文件。CI保留原产品artifact名称并新增同格式清单，未来下载到的原始ZIP须独立验收，不能用本地包替代。只使用已允许的原生构建/打包流程；遇到脚本执行策略限制不降低策略或换入口执行受阻脚本。

验证ZIP的CRC、清单中每个文件的SHA256和BUILD-INFO来源；普通权限运行本地包并检查JSON/HTML版本。客户机/目标环境和远程CI另行记录，不能以本机运行冒充通过。
