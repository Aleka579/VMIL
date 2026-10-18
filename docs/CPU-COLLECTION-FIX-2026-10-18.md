# CPU慢查询修复（2026-10-18）

旧公开3832a6a CI包的Win10客户机严格检查为20/23：两次普通权限CLI分别9/14与10/14成功，CPU型号WMI慢查询导致CPU四项整体超时，完整采集、CPU/CIM以及CLI/GUI全值对照三项未达。该包和失败证据保留，不因本次修改而改写其结果。

## 实现

- CPU型号改读标准CPUID80000002–80000004品牌字符串：先验证80000000支持上界，再按EAX/EBX/ECX/EDX小端顺序解码48字节，处理终止字符和空值。
- CPU四项使用后台本地指令/属性采集，不等待WMI单查询门；SMBIOS/PnP/系统WMI仍由原单工门约束。分类5秒、枚举2秒期限不变，不增加重试。
- 不支持品牌叶或空字符串时型号仍明确NotSupported；其他CPU观测保留，不编造值、不使用慢WMI回落。
- 新cpu.model来源为`CPUID.80000002..80000004`。旧报告确切`Win32_Processor.Name`来源仍保留；其他未审阅来源继续按目录规范化，不能将路径等原文带入导出。字段ID、类型与报告schema不变。

品牌叶语义依据[AMD CPUID Specification](https://www.amd.com/content/dam/amd/en/documents/archived-tech-docs/design-guides/25481.pdf)及[Intel架构手册](https://cdrdv2-public.intel.com/868136/252046-081-sdm-change-document.pdf)。CPUID提供处理器声明信息，不是可信身份认证；本改动只读，不更改宿主或VM设置。

## 验证范围

新增6例品牌字符串及3例报告回归，覆盖支持边界、全部寄存器字节序、48字节无终止符、空/填充/终止字符串，以及新旧报告来源与未审阅来源隐私。本地Release构建零警告/错误，130/130单元和106/106模拟集成通过，零失败/跳过；包括原有超时、取消与未知状态回归。TRX位于artifacts/test-results/20261018-cpu-fix/。

当前尚未据此宣布新CI包或三项真实客户机验收通过。后续使用新源码提交、同Runtime13的新完整包，在原运行VM的普通权限CLI/GUI下重新采集，并与独立CIM及新报告交叉核实；旧3832产物不继承通过结论。原VM电源、配置和快照不在操作范围。
