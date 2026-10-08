# Phinix 旧版插件

[English](README.md)

维护者选择正常的 **public 源码与 GitHub Release** 路线。索引准入先做静态检查，再由维护者亲自添加 `plugin-approved`。不要给游戏客户端放 GitHub 访问 token；按维护者明确要求保留旧版连接参数。

本仓库只包含一个旧版插件自己的 Contracts、Client 和语言资源，身份见 `publication.json`。它通过与第三方相同的 Phinix 扩展生命周期加载，是托管 DLL 包，不是工坊 Mod。

## 构建候选包

安装 Python 3.10+ 和 .NET SDK 10，提供已构建或安装的 Phinix Mod，以及自己合法安装的 RimWorld 1.6 程序集。人才插件还需要 Harmony 2.3.6。这些文件只用于编译，不得提交或随包发行；本地构建无需 Git 或 GitHub CLI。

```sh
python check-source.py
python pack.py --phinix-package /path/to/phinix-rework \
  --game-references /path/to/RimWorldLinux_Data/Managed \
  --harmony-references /path/to/Harmony/Assemblies \
  --packager /path/to/ManagedPackageTool.dll \
  --output /tmp/plugin-candidate.zip \
  --bundle-output /tmp/plugin-candidate \
  --display-output /tmp/plugin-display.json
```

打包器须从可信的 Phinix 源码 `Extensions/PluginStore/Tools/ManagedPackageTool` 构建。ZIP 仅包含 manifest、自己的两个 DLL 和语言文件；不包含主体、交易、库存、Harmony 或游戏 DLL。程序集、模块、类型、设置、codec 和存储身份保持原样。

## 发布边界

源码 CI 只核对文件归属、编译引用、语言声明和固定快照，不上传游戏程序集，也不调用线上业务服务。编译和静态包校验通过不代表游戏验收通过。

独立版游戏验收前，两插件继续随主体发行。不要把候选包与内置版本同时加载，避免重复程序集和模块。红包依赖现有交易、库存模块和其他人维护的旧中继；按维护者明确要求保留原有客户端访问参数。人才保留旧服务、GameComponent 类型和存档字段。拆仓不新增授权或擅自指定 MIT，原作者权利继续保留。

正规发布需要固定源码提交、不可变 ZIP、索引申请 Issue 和维护者亲自添加 `plugin-approved`。红包未决发送及重启核对、人才晚加载组件及缺包重新保存仍是发行门槛；通过前，候选包不上正式目录，主体保留内置 DLL。

## main/dev 与正式发行

在 dev 开发，该分支不触发 Actions。每次确认后的 main 提交，会在配置的主/次版本系列中预留下一个补丁版本，使用固定客户端/Common 源码与私有编译引用构建，然后发布含插件 ZIP、SHA256SUMS 和源码/构建摘要的正式 GitHub Release。同一提交重跑复用已预留版本；构建失败留下未公开的草稿供重试，不替换已发布字节。并行提交预留不同版本，不取消等待中的构建。

维护者在 Repository secrets 配置 BUILD_REFERENCES_TOKEN，仅授予 ci/config.json 指定的私有引用仓库 Contents:Read 权限。这是维护者的 CI 配置；第三方作者应提供自己的合法编译引用。公开产物不含游戏、宿主或 Harmony DLL。程序集身份版本由源码独立控制；自动分配的包发行版本通过 pack.py --version 传入。

合入 main 表示作者确认发布。GitHub Release 不跳过 Index 准入/来源更新策略，也不代表游戏验收通过；修改应先在 dev 测试。宿主和引用的固定输入通过 ci/config.json 显式维护。不要覆盖已发布 ZIP 或暴露引用 token。
