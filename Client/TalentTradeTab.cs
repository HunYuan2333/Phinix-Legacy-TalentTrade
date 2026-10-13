using PhinixClient;
using UnityEngine;
using Verse;

namespace Phinix.LegacyTalentTradeExtension.Client
{
    public class TalentTradeTab : IMainTabProvider, IResponsiveMainTabProvider
    {
        public static readonly TalentTradeTab Instance = new TalentTradeTab();
        private static readonly UiLayoutHints Hints = new UiLayoutHints(new Vector2(320f, 260f), new Vector2(900f, 600f), true);
        public string TabLabel => "Phinix_legacyTalentTrade_tab".Localize();
        public float TabOrder => 1200f;
        public UiLayoutHints LayoutHints => Hints;
        private readonly TalentTabs tabs = new TalentTabs(
            "Phinix_legacyTalentTrade_subTabDirectTrade", "Phinix_legacyTalentTrade_subTabMarket", "Phinix_legacyTalentTrade_subTabRental") { Selected = 1 };
        private readonly MarketPanel marketPanel = new MarketPanel();
        private readonly RentalPanel rentalPanel = new RentalPanel();
        private readonly DirectTradePanel directTradePanel = new DirectTradePanel();

        internal void BindUserEvents(PhinixClient.Framework.IClientUserEventStream events)
        {
            directTradePanel.BindUserEvents(events);
        }

        private bool presentationOpened;
        private TalentPresentationView failedBranchView;
        private System.Collections.Generic.IEnumerable<HostAction> Navigation()
        {
            string[] labels={"subTabDirectTrade","subTabMarket","subTabRental"};
            for(int i=0;i<3;i++){int chosen=i;yield return new HostAction("nav-"+i,("Phinix_legacyTalentTrade_"+labels[i]).Localize().ToString(),()=>SelectPresentation(chosen)){Selected=tabs.Selected==i};}
        }
        private void SelectPresentation(int index)
        {
            if (tabs.Selected == index && failedBranchView == null) return;
            ViewClosed();
            tabs.Selected = index;
            ViewOpened();
        }
        public void ViewOpened()
        {
            var host = BuiltInLegacyTalentTradeClientExtension.Presentation;
            if (host == null || presentationOpened) return;
            presentationOpened = true;
            host.Navigation = Navigation;
            RunBranch(() =>
            {
                if (tabs.Selected == 0) directTradePanel.ViewOpened();
                else if (tabs.Selected == 1) marketPanel.ViewOpened();
                else rentalPanel.ViewOpened();
            });
        }
        public void ViewUpdate()
        {
            if (!presentationOpened) return;
            if (failedBranchView != null) { UpdateFailure(); return; }
            RunBranch(() =>
            {
                if (tabs.Selected == 0) directTradePanel.ViewUpdate();
                else if (tabs.Selected == 1) marketPanel.ViewUpdate();
                else rentalPanel.ViewUpdate();
            });
        }
        private void CloseBranches()
        {
            directTradePanel.ViewClosed();
            marketPanel.ViewClosed();
            rentalPanel.ViewClosed();
        }
        // A projection failure releases only its view. Business operations remain owned by the manager.
        private void RunBranch(System.Action action)
        {
            try { action(); }
            catch (System.Exception ex)
            {
                CloseBranches();
                LegacyTalentTradeRuntime.LogError("[Talent] Presentation branch " + tabs.Selected + " failed: " + ex);
                failedBranchView = new TalentPresentationView(BuiltInLegacyTalentTradeClientExtension.Presentation,
                    "talent-branch-error") { UseNavigation = true };
                UpdateFailure();
            }
        }
        private void UpdateFailure()
        {
            var host = BuiltInLegacyTalentTradeClientExtension.Presentation;
            failedBranchView.Refresh(() => tabs.Selected.ToString(), null, "view-error", null, null,
                () => new[] { new HostAction("retry-view", "Phinix_legacyTalentTrade_uiRetryView".Localize().ToString(),
                    () => SelectPresentation(tabs.Selected)) },
                batch => new[] { host.Section("view-error", "Phinix_legacyTalentTrade_uiViewError".Localize().ToString(),
                    "Phinix_legacyTalentTrade_uiViewErrorHelp".Localize().ToString(), batch.Bindings) });
        }
        public void ViewClosed()
        {
            presentationOpened = false;
            CloseBranches();
            var old = failedBranchView;
            failedBranchView = null;
            old?.Dispose();
        }
        internal void StopPresentation()
        {
            ViewClosed();
            directTradePanel.StopPresentation();
            if (BuiltInLegacyTalentTradeClientExtension.Presentation != null)
                BuiltInLegacyTalentTradeClientExtension.Presentation.Navigation = null;
        }
        public void Draw(Rect inRect)
        {
            if (BuiltInLegacyTalentTradeClientExtension.Presentation != null)
            {
                if (failedBranchView == null) RunBranch(() =>
                {
                    if (tabs.Selected == 0) directTradePanel.Draw(inRect);
                    else if (tabs.Selected == 1) marketPanel.DrawPresentation(inRect);
                    else rentalPanel.DrawPresentation(inRect);
                });
                failedBranchView?.Draw(inRect);
                return;
            }
            using (TalentTradeTheme.BeginDraw()) DrawContents(inRect);
        }

        private void DrawContents(Rect inRect)
        {
            if (inRect.width <= 0f || inRect.height <= 0f) return;
            if (!LegacyTalentTradeRuntime.IsOnline)
            {
                TalentTradeTheme.Surface(inRect);
                Widgets.NoneLabelCenteredVertically(inRect, "Phinix_legacyTalentTrade_pleaseLogIn".Localize());
                return;
            }
            GameFont font = Text.Font;
            bool wrap = Text.WordWrap;
            Text.Font = GameFont.Small;
            Text.WordWrap = true;
            try
            {
                Rect content = TalentTradeUi.Inset(tabs.Draw(inRect), 4f);
                if (content.width <= 0f || content.height <= 0f) return;
                switch (tabs.Selected)
                {
                    case 0: directTradePanel.Draw(content); break;
                    case 1: marketPanel.Draw(content); break;
                    case 2: rentalPanel.Draw(content); break;
                }
            }
            finally { Text.Font = font; Text.WordWrap = wrap; }
        }
    }
}
