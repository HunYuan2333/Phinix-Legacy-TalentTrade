# Phinix 旧版人才贸易插件

> **当前迁移构建 / Current migration build:** API3 单包兼容 1.6/1.7；命令和上线缺项以 [BUILDING-DUAL.md](BUILDING-DUAL.md) 为准。未部署线上流程；下文早期发行记录不代表当前迁移包的兼容范围。

<p align="center">
  <a href="./README.md">English</a> · 简体中文
</p>

Phinix 经典人才贸易模组的官方托管插件移植版本（包 ID: `phinix.legacy.talent-trade`），在 RimWorld 1.6 中实现殖民地间的殖民者交易、雇佣与租赁功能。

> [!NOTE]
> - **技术指南**：关于公开 API、生命周期、线程与资源所有权规范，详见 [Phinix 插件开发指南](https://github.com/HunYuan2333/Phinix-Rework/blob/dev/docs/%E6%8F%92%E4%BB%B6%E5%BC%80%E5%8F%91%E6%8C%87%E5%8D%97.md)。
> - **架构与生命周期状态**：当前源码已改用 `ClientExtensionModule.Compose`，删除插件自身旧 `Register` 入口，打包要求客户端抽象 `>=1.9.0 <3.0.0`。完整 DI/静态上下文与回调生命周期整治仍待完成；本地编译和包预检通过不代表已发布或游戏验收。
> - **Harmony 补丁约束**：所有补丁均在 `Activate` 中挂载并在 `Shutdown` 中卸载；严禁在静态构造函数中挂补丁，以确保插件在被扩展管理器停用时实现真正的零补丁挂载。
> - **存档持久化限制**：租借记录与市场挂牌数据保存在存档的专属 `GameComponent` 中。存档内仍有活跃交易或外出租赁人员时，严禁卸载本插件，否则会导致反序列化错误坏档。
> - **CI 与自动构建**：GitHub Actions 自动构建工作流（main 提交自动编译打包，dev 不编译）正在 F7 统一规划实施中；目前通过本地 `pack.py` 完成打包。
> - **Skill 说明**：Phinix 插件开发 skill 正在准备中，后续将补充链接。

---

## 概述与原作者署名

- **功能定位**：通过 Phinix 联网基础设施，提供跨殖民地的人才市场、点对点交易及借用租赁。
- **原作者署名**：本项目基于社区原作者 **iniad** 编写的经典 Talent Trade 模组进行现代重构。原作者权利及许可条款完整保留；本仓库提供适配 Phinix Rework 的独立托管插件版本。
- **分发模式**：已彻底从 Phinix 主模组包剥离。作为独立托管 DLL 插件运行，遵循标准 Phinix 扩展生命周期。

---

## 获取与安装

### 游戏内商店安装（推荐）

1. 在 RimWorld 游戏内打开 Phinix 窗口，切换至 **商店**（Store）Tab。
2. 找到 **Talent trade**（版本 1.0.1），点击 **安装**（Install）。
3. **完全退出并重启 RimWorld** 以加载新安装的程序集。

### 前置运行要求

- **RimWorld 1.6**
- **Phinix Rework**（提供宿主扩展运行时与客户端抽象支持）
- **Harmony 2.3.6+**（用于殖民者序列化与数据传输补丁）

---

## 核心功能与使用

1. **人才市场（Market）**：
   - 浏览其他在线玩家挂牌出售的殖民者与奴隶。
   - 购买前支持查看目标人物的特性、技能、身体状况及装备概览。
2. **直接交易（Direct Trade）**：
   - 与指定的在线殖民地发起点对点交易谈判。
   - 支持殖民者互换或以白银购买。
3. **人才租赁（Rental）**：
   - 将自有殖民者短期出租给其他殖民地，赚取租金。
   - 租期到期后，被租借的人才将自动返还至所属母殖民地。
4. **输入安全保护**：
   - 严格拦截负数金额、非法字符及越界合约参数，防止恶意或异常数据提交。

---

## 存档机制与持久化限制

- **GameComponent 存储**：活跃挂牌与租赁回队数据保存在存档文件（`.rws`）的专属 `GameComponent` 中。
- **服务器权威确认**：所有人才转移均需经过专用服务器权威确认并返回凭证；本地点击发送不代表交易已成功落地。
- **内存态购买意图**：进行中的购买请求在游戏会话中暂存于内存；若在服务器结算前游戏异常闪退或重启，无法自动恢复未结算的事务。

---

## 停用与卸载风险警示

> [!CAUTION]
> 若当前存档中存在**正在挂牌出售的殖民者、外出租赁未归的人员或等待结算的返还记录**，**请勿直接停用或卸载本插件**。缺少本插件加载可能导致反序列化失败，甚至导致相关殖民者永久遗失。

### 安全卸载流程

1. 撤回人才市场上所有正在出售的殖民者并接回地图。
2. 等待外出租赁的所有殖民者全员归队。
3. 保存游戏，并确认人才贸易账本中已无未完成的在途合约。
4. 在 **扩展管理**（Extension Manager）中停用或卸载 **Talent trade**，并重启游戏。

---

## 构建与候选包验证

需要 .NET 10 SDK、本地 Phinix-Rework 源码及 RimWorld 1.6 依赖：

```bash
python3 pack.py \
  --phinix-root /path/to/Phinix-Rework \
  --game-references /path/to/rw16-references \
  --game17-references /path/to/rw17-references \
  --harmony17 /path/to/rw17-references/0Harmony.dll \
  --version 0.0.1 \
  --output /tmp/phinix-talenttrade-dual-local.zip
```

严禁将 RimWorld 游戏程序集、Harmony 或宿主程序集打包进分发 ZIP 中。

---

## 故障排查与求助

- **交易挂起**：检查与 Phinix 服务器的网络连通性；所有交易操作需等待服务器权威返回。
- **日志提报**：在 GitHub 提交反馈时，请提供脱敏后的游戏日志（`Player.log`）、RimWorld 确切版本、Phinix 版本及本插件版本。提报前请务必抹去个人凭证与私有密钥信息。


2026-10-10：按用户确认新增客户端抽象 1.9 维护线，和 2.0 分别编译；默认新版本 1.1.0。范围、版本号及命令参数见 [客户端兼容构建](Client-Compatibility.md)。原先“仅 2.0”说明由本节更新。本地包已完成最终静态和宿主引用校验，游戏内验收及正式发布仍待完成。


**当前方案已修正为单包内部适配**：版本 1.1.1 的同一 ZIP 支持 API 1.9/2.0；之前两条发行线说明已撤销。旧接口和运行时识别仅在内部 adapter。构建基线、宿主绑定过渡和验证限制见 [客户端兼容说明](Client-Compatibility.md)。

## T4 本地主题接入

1.1.2 本地候选新增内部主题 adapter，单包继续支持 1.9/当前客户端。当前客户端可编辑模块配色，1.9 使用默认样式。配色与业务状态分开，未修改在线发布版本；详见 Client-Compatibility.md。

正规发布需要固定源码提交、不可变 ZIP、索引申请 Issue 和维护者亲自添加 `plugin-approved`。红包未决发送及重启核对、人才晚加载组件及缺包重新保存仍是发行门槛；通过前，候选包不上正式目录，主体保留内置 DLL。

## main/dev 与正式发行

在 dev 开发，该分支不触发 Actions。每次确认后的 main 提交，会在配置的主/次版本系列中预留下一个补丁版本，使用固定客户端/Common 源码与私有编译引用构建，然后发布含插件 ZIP、SHA256SUMS 和源码/构建摘要的正式 GitHub Release。同一提交重跑复用已预留版本；构建失败留下未公开的草稿供重试，不替换已发布字节。并行提交预留不同版本，不取消等待中的构建。

维护者在 Repository secrets 配置 BUILD_REFERENCES_TOKEN，仅授予 ci/config.json 指定的私有引用仓库 Contents:Read 权限。这是维护者的 CI 配置；第三方作者应提供自己的合法编译引用。公开产物不含游戏、宿主或 Harmony DLL。程序集身份版本由源码独立控制；自动分配的包发行版本通过 pack.py --version 传入。

合入 main 表示作者确认发布。GitHub Release 不跳过 Index 准入/来源更新策略，也不代表游戏验收通过；修改应先在 dev 测试。宿主和引用的固定输入通过 ci/config.json 显式维护。不要覆盖已发布 ZIP 或暴露引用 token。
