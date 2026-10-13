# 当前正式包支持范围（2026-10-13）

按用户最新决定，1.2 系列正式包仅面向本次重构后的 Host API >=2.0.0 <3.0.0，不再承诺旧 API 1.9 客户端组合。打包直接使用固定当前 Host，compile-client-api 为 2.0.0。历史 adapter 暂留，旧协议和保存键未因此改动。下文为历史测试与兼容决策记录，其发行范围已由本节取代。

# 单包内部 adapter（当前方案）

每个插件发布同一个 1.1.1 包，支持客户端 API >=1.9.0 <3.0.0。原分成 1.9/2.0 两个包的方案已撤销，pack.py 不再有 --client-api 参数，也不存在独立维护线版本号。

--phinix-package 必须提供 1.9 基线公开 DLL，--game-references 提供 RimWorld 1.6 编译引用；人才贸易另提供 Harmony 引用。默认 version 从 publication.json 读取。打包工具 --compile-client-api 1.9.0 校验引用来源，插件 DLL 不引用仅新 SDK 存在的类型。

当前宿主 API/AssemblyFileVersion 仍为 2.0.0；过渡期 ClientExtensionAbstractions 的 CLR AssemblyVersion 冻结 1.9.0.0，避免在业务 adapter 运行前被加载器拒绝。新核心没有恢复已删除接口。更新前一轮 CLR 2.0 开发宿主后须重启；旧客户端 1.9 无需改动。

红包的发送识别、当前 Chat 反射及旧库存 Deliver 仅位于 Client/Compatibility/ClientAdapters.cs。读取运行时常量使用反射，避免 1.9 编译时常量内联误判。当前 Chat 停用不选择旧接口重发；业务/UI 只消费中性公告端口。人才贸易当前使用的 SDK 接口在两端一致，同一二进制直接复用这些稳定契约，无额外版本分支。

单份 ZIP 对两端的最终静态包、宿主引用与实际类型加载检查通过，尚未游戏内验收或上线。客户更新完成后再删除 adapter 旧分支并结束绑定过渡；不为此保留两份业务或混用 UI。

## T4 本地候选 1.1.2

新增 Compatibility/ThemeAdapter.cs，通过反射绑定当前 IClientTheme/IClientThemeRegistry，使用实际激活模块注册和 Commit，持有并释放 owner lease。编译引用仍为 1.9，新接口不进入二进制外部类型引用。1.9 使用插件默认配色；当前宿主的缺键用默认值补齐。UI 只消费 PluginPalette，不包含版本/schema 判断。当前 API 类型形状损坏会明确失败，不回退旧主题接口；可选主题服务缺失可以使用默认样式继续业务。

配色作用域固定当前 palette，快照版本变化时更新。注册的 talent-trade.panel / .highlight / .accent 分别有真实面板、悬停与强调消费点，缺值按宿主角色回退。元数据采用中英双语字面标签，不注入宿主专属翻译入口。UI 重构之后可继续替换具体控件，adapter 边界不变。

1.1.2 候选独立提供，publication.json 默认 1.1.1 及线上 Index 未改。编译与 1.9/当前宿主静态校验通过；尚未游戏验收或执行运行时反射绑定，不能视为实际两端功能已验证。

## B38 本地候选1.1.3：直接交易公共呈现切片

同一SDK1.9编译二进制。Compatibility/PresentationAdapter.cs 是唯一新API类型名、反射形状和provider生命周期绑定边界：现代Host绑定同一个公开session/point/theme；旧1.9仍用原Draw。业务/新投影不判断客户端版本，当前API形状缺损明确失败。普通模块身份注册 `builtin.legacy-talent-trade.presentation.actions`；Contracts 的 TalentPresentationActionContext 只读view/entity/state/selection/silverText/online/revision，不含Pawn/offer/ledger权威。

直接交易成员与活动交易列表、报价窗口的白银草稿、双方PawnSummary与本地Def图标、选人、移除、提交、锁定/取消确认迁入Host。Host不deserialize/materialize/hold Pawn。兼容reader抽出BuildConfirmation，缺失种族阻止锁定，未知manifest仍警告；1.9原Dialog路径复用相同reader。现代锁定接受确认后才依次SendCurrentOffer→LockTrade，取消确认不发草稿；业务协议/成交/托管/归还规则不改。关闭窗口不取消交易；窗口Game变化只关view，迟到race probe不向关闭/原Game过期/已结束trade加Pawn。

主Tab的三个分支导航、Market/Rental与settings仍原Draw可达，下一批迁移。此批不是R46全部完成。Pawn是现有summary/只读字段+借用Def图标，没有新增真实portrait或图案虚构数据。manager跨存档ownership、交付/归还与业务持久恢复仍需专项证据，不以UIguard宣称完整安全。

SDK1.9编译0errors/0warnings；当前实际DLL加载、公开模型/bridge+fake session/registry34检查，旧1.9实际DLL加载/旧provider3检查通过。它们不调用真实GUI/成交/落地。1.9/当前静态候选引用与CRC检查通过，游戏待测。候选talent-trade1.1.3，配HostB37与Example1.0.34，红包1.1.11保留；不需要新Host公共API、不重包Example。publication默认1.1.1、旧候选与adapters保留，未部署/发布。

## B39 本地候选1.1.5：人才全部既有呈现入口迁移完成

当前主Tab不再调用TalentTabs/MarketPanel.Draw/RentalPanel.Draw旧绘制路径；三分支导航为同一Host固定header动作，直接交易、市场、租赁都使用普通公开session/point/field/list/status/confirmation/theme。Market/Rental为左固定筛选/滚动列表、右固定起点的详情/表单独立滚动；窄窗由相同公共split回退。强制清理保留，归独立公共action menu和Danger确认，明确不会归还Pawn。第三方贡献保留在操作区；没有第二套action registry/renderer/theme。

市场：列表/搜索/我的→详情→兼容性确认购买；或选实际地图Pawn→价格字段/预览→公共发布确认→原序列化/托管/发布；下架及强制清理仍调用原业务。租赁：全部/我的出租/我租用→详情→原兼容性确认租用、下架、归还；发布表单使用原colonist-only筛选，正整数租金/天数、零或非负押金。所有回调文案不宣称成交、交付、归还或退款成功；以原业务状态/真实物品为准。没有改价格、租期、OC检查、持久化、协议或结算权威。

通知/debug设置通过原设置panel的公共Host按钮打开同一公共page，原setting keys和Save/Set路径保持。每份owner view释放session、scope contribution source、临时Pawn引用；模式/搜索/数字文本在同一运行期切分支保留，具体Pawn选择关闭后必须重选，切Game/map清除借用选择。新增readonly防御复制DraftValues供普通贡献读取字段，Host/SDK/Example不需变更。

全局StateVersion对无关入站及心跳也会变化；本批只在插件投影层算有界流式事实指纹（不重建/序列化Pawn），排除LastRefreshUtc等不影响显示的心跳字段和无关消息。内容/价格/参与人/manifest/报价真实变化仍失效旧动作；租赁剩余天数按实际游戏tick变化，而非每秒刷新。业务manager版本/心跳/协议不改。指纹只作本地view revision判定，不是物品相等、授权或业务确认。复杂大payload的游戏性能仍待测。

SDK1.9编译0errors/0warnings；当前actual DLL/models + fake session/registry57检查、旧1.9类型/provider3检查通过。验证分组主体数量包含globalheader，固定导航/筛选split有效；public危险菜单、draft防御复制、筛选/数字/缺种族gate、心跳不变/价格变化、设置原键、failed Open清理均覆盖。静态1.9/current参考和CRC通过；不是真实GUI/Scribe/成交/租赁游戏通过。

候选talent-trade1.1.5，HostB37、红包1.1.11、Example1.0.34维持。人才呈现源码完成，用户要求现在集中测试，不逐批要求游戏验收。旧1.9内部adapter/原Draw可达路径保留，publication默认版本不改、不部署发布。后续R44业务补全增强与UI收尾分开，1.7仍需独立考评。

最终信息保留检查补回目录/详情的原种族Def名与✓/✗证据，空RaceDefName采用原Human fallback（图标/详情不抛空键异常）。1.1.4初始候选与报告保留，集中测试仅用最终1.1.5。

## B40 本地候选1.1.6：直接交易空选择与分支恢复

首次进入直接交易未选择活动交易时，GetTrade(null)抛出ArgumentNullException，异常越过插件主Tab使Host标记整个provider失败。现GetTrade对null/empty返回无交易；未知非空ID仍无交易，不改变任何业务状态。主Tab隔离分支open/update/draw投影异常：关闭故障分支session，通过同一公开Host页面保留导航和显式重试；错误记录一次，失败期间不自动重试，不取消/返还/重发交易。恢复页自身失败仍交给Host通用故障边界。

实际DLL白盒先复现原异常，再验证72项current公共模型/session/registry及3项旧1.9绑定通过。新增实际主Tab三分支往返、故障open与publish注入、返回、显式重试、稳定页面/无自动重试、旧动作拒绝与业务StateVersion不变。编译通过，游戏内复测仍待验收。既有失效provider需安装新版并重启游戏后重新打开；HostB37/Example/红包候选无需替换。

## B41 本地候选1.1.7：人物图标与下架加载修复

当前Host补公共ClientPresentationRow.WithIconSource(Func<Texture>)：仅可见行Repaint主线程解析借用Texture，支持RenderTexture；measurement/Layout不调用，不接管Pawn/资源创建/释放。Talent本地humanlike使用PortraitsCache64x64；远端summary使用模块自有中性人物图标（不可据summary虚构真人头像），动物原Def.uiIcon保持。模块stop释放占位纹理，游戏portrait由游戏cache所有；关闭session清除借用Pawn closure。当前Host至少需B41（adapter明确检查公共成员），SDK1.9原Draw仍保留。

用户下架日志：旧XmlToPawn手工调用crossRefs和postload，finally先Inactive再FinalizeLoading；当前1.6 DLL的FinalizeLoading本身包含两个阶段。现LoadingVars下仅调用一次标准FinalizeLoading，scope以MapInitializing加载并恢复原ProgramState，ForceStop清理属于本次的loader；拒绝busy外层Scribe。1.6 LifeStageWorker_HumanlikeAdult在Playing对初次cache重算的previousLifeStage=null访问developmentalStage导致StyleTracker NRE；加载scope避开运行期生日事件，结束前prime年龄cache，结构缺失拒绝，不再宣称所有警告无害。没有全局Harmony/no-op抑制。

下架原实现在排队之前删除LocalPawnData并Untrack，即使加载失败也丢掉可恢复模板。本批复用原pendingReturnIds/Data/UncertainIds与PendingPawnReturnLedger：先登记原payload，再移交原return delivery；拒绝/queue-full/加载失败保留payload，handoff结果未知保留uncertain不自动重试，真实ownership transfer成功才移除。无component/原Game不匹配时原数据不删。无新保存schema、网络协议、成交规则；这是下架本地返还缺陷修复，并非R44新业务增强。

编译通过；当前actual DLL白盒81项、旧1.9绑定3项、Host stub228项、Example生命周期45断言通过。真实Pawn XML/Scribe postload/头像渲染与落地仍须游戏验证；原本已被旧版删除的payload无法由本修复凭空重建。联合候选HostB41/人才1.1.7/Example1.0.35，红包1.1.11不变；旧候选保留，未部署/发布。
