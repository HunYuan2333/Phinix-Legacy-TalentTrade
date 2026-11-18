using System;
using System.Reflection;
using System.Threading;
using HarmonyLib;
using PhinixClient;
using PhinixClient.Framework;
using Utils;
using Utils.Framework;

namespace Phinix.LegacyTalentTradeExtension.Client
{
    /// <summary>
    /// 人才贸易插件入口：通过 ClientExtensionModule.Compose 注册公开能力。
    ///
    /// 设计哲学 §1.1 插件平权；人才贸易独立的中继协议由自身 transport 负责。
    /// Harmony 补丁挂载约束（与禁用机制联动）：PatchAll 在 Activate，UnpatchAll 在 Shutdown；
    /// 禁止静态构造器挂补丁（程序集被扩展扫描加载时会绕过禁用）。
    /// </summary>
    [PhinixExtension("builtin.legacy-talent-trade")]
    public sealed class BuiltInLegacyTalentTradeClientExtension : ClientExtensionModule, IActivatablePhinixExtensionModule
    {
        private const string HarmonyId = "iniad.legacytalenttrade";

        private Harmony harmony;
        private LegacyTalentTradeSettingsPanel settingsPanel;
        internal static TalentHostPresentation Presentation { get; private set; }
        private System.Timers.Timer driveTimer;
        private bool activated;
        private TalentUpdateDriver updateDriver;

        public override string ExtensionId => "builtin.legacy-talent-trade";

        public override void Compose(IExtensionBuilder builder)
        {
            builder.RegisterApi<IMainTabProvider>(TalentPresentationAdapter.Wrap(TalentTradeTab.Instance));
            settingsPanel=new LegacyTalentTradeSettingsPanel();
            builder.RegisterApi<IClientSettingsPanelProvider>(settingsPanel);
        }

        public void Activate(ExtensionHostContext hostContext)
        {
            if (activated) return;
            updateDriver = new TalentUpdateDriver(hostContext.GetRequiredService<IClientTaskService>().ForModule(this), TalentTradeManager.Update);
            TalentTradeLocalization.Bind(hostContext.GetRequiredService<IClientLocalizationService>().ForModule(this));
            TalentTradeTheme.Start(hostContext,this);
            Presentation = TalentPresentationAdapter.Create(hostContext,this);

            LegacyTalentTradeRuntime.Session = hostContext.GetRequiredService<IClientSessionContext>();
            LegacyTalentTradeRuntime.Users = hostContext.GetRequiredService<IClientUserDirectory>();
            LegacyTalentTradeRuntime.Dispatcher = hostContext.GetRequiredService<IClientMainThreadDispatcher>();
            LegacyTalentTradeRuntime.SettingsContext = hostContext.GetRequiredService<IClientSettingsContext>();
            LegacyTalentTradeRuntime.Settings.Load(LegacyTalentTradeRuntime.SettingsContext);
            LegacyTalentTradeRuntime.Log = hostContext.Log;

            IClientUserEventStream userEvents = hostContext.GetRequiredService<IClientUserEventStream>();
            TalentTradeManager.Initialize(userEvents);
            TalentTradeTab.Instance.BindUserEvents(userEvents);

            activated = true;
            LegacyTalentTradeRuntime.IsActive = true;

            // 游戏级补丁（GenScene 退出下架 / PawnTextureAtlasGC 修复）——禁用时不会执行 Activate，补丁零挂载
            harmony = new Harmony(HarmonyId);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            driveTimer = new System.Timers.Timer
            {
                AutoReset = true,
                Interval = 200
            };
            driveTimer.Elapsed += OnDriveTick;
            driveTimer.Start();

            hostContext.Log?.Invoke("[TalentTrade] Legacy talent trade extension activated.", LogLevel.INFO);
        }

        private void OnDriveTick(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (!activated || !LegacyTalentTradeRuntime.IsActive) return;
            Volatile.Read(ref updateDriver)?.Tick();
        }

        private void StopDrive()
        {
            var driver=Interlocked.Exchange(ref updateDriver,null);
            var timer=Interlocked.Exchange(ref driveTimer,null);
            try
            {
                if(timer!=null) {timer.Stop();timer.Elapsed-=OnDriveTick;timer.Dispose();}
            }
            finally {driver?.Dispose();}
        }

        public void Shutdown(ExtensionHostContext hostContext)
        {
            StopDrive();
            TalentTradeTab.Instance.StopPresentation();
            TalentPawnIcons.Stop();
            settingsPanel?.StopPresentation();
            var oldPresentation=Presentation; Presentation=null; oldPresentation?.Dispose();
            TalentTradeLocalization.Bind(null);
            TalentTradeTheme.Stop();
            if (!activated) return;
            activated = false;
            LegacyTalentTradeRuntime.IsActive = false;

            TalentTradeManager.Shutdown();
            TalentTradeTab.Instance.BindUserEvents(null);
            harmony?.UnpatchAll(HarmonyId);
            harmony = null;

            LegacyTalentTradeRuntime.Session = null;
            LegacyTalentTradeRuntime.Users = null;
            LegacyTalentTradeRuntime.Dispatcher = null;
            LegacyTalentTradeRuntime.SettingsContext = null;
            LegacyTalentTradeRuntime.Log = null;

            hostContext.Log?.Invoke("[TalentTrade] Legacy talent trade extension shut down.", LogLevel.INFO);
        }
    }
}
