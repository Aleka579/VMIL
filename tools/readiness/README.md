# v0.1 本地验证辅助工具

这些工具用于开发验证，不随应用发布。实际结果见[验证记录](../../docs/READINESS-TEST-RESULTS-2026-10-11.md)。

- package_validation.py：对已完成publish并加入完整许可的应用目录生成新的私有ZIP和文件哈希清单；输出存在时拒绝覆盖。清单注明源码基线与待提交修改，不冒充正式Release。
- configure-vmrest.cmd：调用用户指定的官方vmrest.exe -C，在窗口中保留退出码，输入由用户本地完成。它只检查已知USERPROFILE/.vmrest位置，不能保证所有版本的配置位置；实际存储路径未知时记录未知，不读取认证内容。

```powershell
python tools/readiness/package_validation.py --directory artifacts/publish/readiness-20261011 --output artifacts/test-packages/VMIdentityLab-validation-20261011.zip --baseline 015c387
.\tools\readiness\configure-vmrest.cmd "<已核实的官方vmrest.exe路径>"
```

打包先复核publish目录与许可来源。使用配置工具前取得账号创建范围授权，并在可交互的本机窗口输入；不要在聊天或命令参数中提供密码。临时启动API还须确认回环监听、输出仅保留私有目录，并在结束后停止本轮进程。工具不安装服务、不改安全设置、不启动或关闭VM，不授权后续配置操作。
