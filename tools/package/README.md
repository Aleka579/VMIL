# 本地 Alpha 打包

不推送、不创建GitHub Release；只接受带BUILD-INFO.json的干净Alpha构建。沿用当前框架和运行时，正式包不含开发驱动、测试记录或真实VM数据。

1. 保存源码提交，从该干净提交以Release/win-x64/self-contained发布到新的空目录，关闭PDB输出。
2. 从实际程序集读取版本/提交，从runtimeconfig读取includedFrameworks；记录SDK、构建时间、sourceState=clean到BUILD-INFO.json。各应用程序集必须对应同一源码提交。
3. 使用已有Python 3.14工具环境执行：

```powershell
python tools/package/create_alpha.py --directory artifacts/publish/NEW-ALPHA --output artifacts/releases/VMIdentityLab-VERSION-win-x64.zip
```

工具复制正式许可、去重第三方许可和包说明，检查非产品内容，生成PACKAGE-FILES.json、ZIP和.sha256。拒绝覆盖已有包、校验文件或打包元数据；失败保留现场，使用新的发布目录处理，不自动清理。

验证ZIP的CRC、清单中每个文件的SHA256和BUILD-INFO来源；普通权限运行本地包并检查JSON/HTML版本。客户机/目标环境和远程CI另行记录，不能以本机运行冒充通过。
