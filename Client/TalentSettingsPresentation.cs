using System;
using System.Collections.Generic;
using PhinixClient;
using PhinixClient.Framework;
using UnityEngine;
using Verse;
namespace Phinix.LegacyTalentTradeExtension.Client
{
    internal sealed class TalentSettingsWindow : Window
    {
        private readonly TalentPresentationView view;private readonly TalentHostPresentation host;private readonly IClientSettingsContext settings;
        public override Vector2 InitialSize=>new Vector2(Mathf.Min(680f,UiScreenSafeArea.Current.width),Mathf.Min(430f,UiScreenSafeArea.Current.height));
        internal TalentSettingsWindow(IClientSettingsContext settings)
        {this.settings=settings;host=BuiltInLegacyTalentTradeClientExtension.Presentation;view=new TalentPresentationView(host,"talent-settings");doCloseX=true;draggable=true;resizeable=true;forcePause=false;try{UpdateView();}catch{view.Dispose();throw;}}
        private static string L(string key)=>("Phinix_legacyTalentTrade_"+key).Localize().ToString();
        private string Stamp()=>LegacyTalentTradeRuntime.Settings.EnableNotifications+":"+LegacyTalentTradeRuntime.Settings.EnableDebugLog;
        private void UpdateView()
        {
            var current=LegacyTalentTradeRuntime.Settings;
            bool notifications=current.EnableNotifications,debug=current.EnableDebugLog;
            view.DraftValues=new Dictionary<string,string>{{"notifications",notifications.ToString()},{"debug",debug.ToString()}};
            view.Refresh(Stamp,null,"settings",null,null,()=>new[]{
                new HostAction("notifications",(notifications?"✓ ":"□ ")+L("settingNotifications"),()=>{current.Save(settings,LegacyTalentTradeSettings.KeyNotifications,!notifications);view.Invalidate();}){Selected=notifications},
                new HostAction("debug",(debug?"✓ ":"□ ")+L("settingDebugLog"),()=>{current.Save(settings,LegacyTalentTradeSettings.KeyDebugLog,!debug);view.Invalidate();}){Selected=debug}},
                batch=>new[]{host.Section("settings",L("tab"),L("settingNotificationsDesc")+"\n\n"+L("settingDebugLogDesc"),batch.Bindings)});
        }
        public override void WindowUpdate(){base.WindowUpdate();UpdateView();windowRect=UiScreenSafeArea.ClampWindow(windowRect,new Vector2(360f,280f));}
        public override void DoWindowContents(Rect rect)=>view.Draw(rect);
        public override void PostClose(){view.Dispose();base.PostClose();}
    }
}
