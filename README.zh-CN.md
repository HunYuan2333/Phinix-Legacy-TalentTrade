# Phinix 旧版人才贸易插件

<p align="center">
  <a href="./README.md">English</a> · 简体中文
</p>

Phinix 经典人才贸易模组的官方托管插件移植版本（包 ID: `phinix.legacy.talent-trade`），在 RimWorld 1.6 中实现殖民地间的殖民者交易、雇佣与租赁功能。

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
# 核验源码与引用归属
python3 check-source.py

# 打包候选 ZIP
python3 pack.py \
  --phinix-package <path-to-Phinix-Rework> \
  --game-references <path-to-RimWorld-Managed> \
  --harmony-references <path-to-Harmony-Assemblies> \
  --packager <path-to-ManagedPackageTool.dll> \
  --output <path-to-output>/phinix-legacy-talenttrade-1.0.1.zip
```

严禁将 RimWorld 游戏程序集、Harmony 或宿主程序集打包进分发 ZIP 中。

---

## 故障排查与求助

- **交易挂起**：检查与 Phinix 服务器的网络连通性；所有交易操作需等待服务器权威返回。
- **日志提报**：在 GitHub 提交反馈时，请提供脱敏后的游戏日志（`Player.log`）、RimWorld 确切版本、Phinix 版本及本插件版本。提报前请务必抹去个人凭证与私有密钥信息。
