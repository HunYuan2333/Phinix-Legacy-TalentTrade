using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using Verse;
namespace Phinix.LegacyTalentTradeExtension.Client
{
    public partial class DirectTradeWindow
    {
        private TalentPresentationView modernView;
        private bool picking;
        private string selection, modernNotice;
        private readonly Dictionary<string,Pawn> availablePawns=new Dictionary<string,Pawn>();
        private readonly Dictionary<string,PawnSummary> offeredPawns=new Dictionary<string,PawnSummary>();
        private string shownRowsStamp;
        private object presentationGame;
        private static string L(string key)=>("Phinix_legacyTalentTrade_"+key).Localize().ToString();
        private void StartPresentation()
        {presentationGame=Current.Game;var host=BuiltInLegacyTalentTradeClientExtension.Presentation;if(host!=null)modernView=new TalentPresentationView(host,"talent-direct."+trade.Id);}
        private string cachedFactsKey,cachedFacts;
        private string ViewStamp()
        {
            var t=TalentTradeManager.GetTrade(trade.Id);string key=TalentTradeManager.StateVersion+":"+(t?.InitiatorOffer?.Pawns.Count??0)+":"+(t?.TargetOffer?.Pawns.Count??0)+":"+(t?.InitiatorOffer?.SilverAmount??0)+":"+(t?.TargetOffer?.SilverAmount??0);
            if(key!=cachedFactsKey){cachedFactsKey=key;cachedFacts=TalentProjectionFingerprint.Direct(new[]{t},true);}
            return cachedFacts+":"+TalentTradeManager.GetLocalUuid()+":"+LegacyTalentTradeRuntime.IsOnline+":"+picking;
        }

        private static bool Editable(DirectTradeState state)=>state==DirectTradeState.Pending||state==DirectTradeState.Accepted||state==DirectTradeState.Negotiating||state==DirectTradeState.Confirmed;
        private void UpdatePresentation()
        {
            if(modernView==null)return;
            if(!ReferenceEquals(presentationGame,Current.Game)){Close();return;}
            DirectTrade current=TalentTradeManager.GetTrade(trade.Id);
            if(current==null||current.State==DirectTradeState.Completed||current.State==DirectTradeState.Cancelled){Close();return;}
            trade=current;string initialStamp=ViewStamp();
            while(modernView.Input(out var input))
            {
                if(input.Id=="silver")silverBuffer=input.Value;
                else if(input.Id=="my-pawns"&&shownRowsStamp==initialStamp)selection=input.Value;
                modernView.Invalidate();
            }
            string local=TalentTradeManager.GetLocalUuid();bool initiator=trade.InitiatorUuid==local;
            bool participant=initiator||trade.TargetUuid==local;
            var my=initiator?trade.InitiatorOffer:trade.TargetOffer;var their=initiator?trade.TargetOffer:trade.InitiatorOffer;
            bool mineLocked=initiator?trade.InitiatorConfirmed:trade.TargetConfirmed;
            bool theirLocked=initiator?trade.TargetConfirmed:trade.InitiatorConfirmed;
            string other=initiator?trade.TargetName:trade.InitiatorName;
            if(string.IsNullOrEmpty(other))other=initiator?trade.TargetUuid:trade.InitiatorUuid;
            var host=BuiltInLegacyTalentTradeClientExtension.Presentation;
            modernView.Refresh(ViewStamp,trade.Id,trade.State.ToString(),selection,silverBuffer,()=>
            {
                var actions=new List<HostAction>();
                string disabled=!participant||!LegacyTalentTradeRuntime.IsOnline||!Editable(trade.State)?L("uiNotEditable"):null;
                bool validSilver=int.TryParse(silverBuffer,NumberStyles.Integer,CultureInfo.InvariantCulture,out int silver)&&silver>=0;
                string numeric=disabled??(validSilver?null:L("uiSilverInvalid"));
                actions.Add(new HostAction("pick",L(picking?"uiBack":"tradeAddUnit"),()=>{picking=!picking;selection=null;modernView.Invalidate();},disabled));
                if(picking)
                {
                    availablePawns.TryGetValue(selection??"",out Pawn pawn);
                    actions.Add(new HostAction("add",L("tradeAddUnit"),()=>{if(pawn==null||!pawn.Spawned||pawn.Dead||!TradeablePawnUtility.GetTradeablePawns(Find.CurrentMap).Contains(pawn))return;TryAddPawnToOffer(pawn,initiator);picking=false;selection=null;modernView.Invalidate();},disabled??(pawn==null?L("uiSelectPawn"):null)));
                }
                else
                {
                    offeredPawns.TryGetValue(selection??"",out PawnSummary summary);
                    int index=my?.Pawns.IndexOf(summary)??-1;
                    actions.Add(new HostAction("remove",L("tradeRemovePawn"),()=>{if(index<0||my==null||index>=my.Pawns.Count||!ReferenceEquals(my.Pawns[index],summary))return;RemovePawnFromOffer(index);selection=null;modernView.Invalidate();},disabled??(index<0?L("uiSelectPawn"):null)));
                }
                actions.Add(new HostAction("send",L("tradeConfirmSend"),()=>{silverValue=silver;SendCurrentOffer(initiator);modernView.Invalidate();},numeric){Kind="Primary"});
                var lockAction=new HostAction("lock",L("tradeLock"),()=>{silverValue=silver;SendCurrentOffer(initiator);TalentTradeManager.LockTrade(trade.Id);modernView?.Invalidate();},numeric??(mineLocked?L("tradeWaitingLock"):null));
                string confirmation=L("tradeLockConfirm");bool compatible=true;
                if(their?.PawnData!=null&&their.PawnData.Count>0) compatible=TransferCompatibilityUi.BuildConfirmation(confirmation,L("tradeTheirOffer"),their.PawnManifestData,out confirmation);
                if(!compatible)lockAction.Disabled=confirmation;
                lockAction.ConfirmTitle=L("tradeLock");lockAction.ConfirmText=confirmation;lockAction.ConfirmAccept=L("tradeLock");lockAction.ConfirmCancel=L("uiBack");actions.Add(lockAction);
                actions.Add(new HostAction("cancel",L("cancel"),()=>{TalentTradeManager.CancelTrade(trade.Id);Close();}){Kind="Danger",ConfirmTitle=L("cancel"),ConfirmText=L("uiCancelTrade"),ConfirmAccept=L("cancel"),ConfirmCancel=L("uiBack")});
                return actions;
            },batch=>
            {
                availablePawns.Clear();offeredPawns.Clear();var ours=new List<HostRow>();var theirs=new List<HostRow>();
                if(picking)
                {foreach(var pawn in TradeablePawnUtility.GetTradeablePawns(Find.CurrentMap)){string id=pawn.GetUniqueLoadID();availablePawns[id]=pawn;ours.Add(TalentPawnIcons.Local(new HostRow(id,TradeablePawnUtility.GetLabel(pawn),TalentCard.Details(PawnSummary.FromPawn(pawn))),pawn));}}
                else if(my!=null)
                {for(int i=0;i<my.Pawns.Count;i++){var summary=my.Pawns[i];if(summary==null)continue;string id="mine."+i+"."+System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(summary);offeredPawns[id]=summary;ours.Add(SummaryRow(id,summary));}}
                if(their!=null)for(int i=0;i<their.Pawns.Count;i++){var summary=their.Pawns[i];if(summary!=null)theirs.Add(SummaryRow("theirs."+i,summary));}
                shownRowsStamp=ViewStamp();
                string status=L("tradeStatus")+": "+trade.State+"\n"+L("tradeMyOffer")+": "+(mineLocked?L("tradeLock"):L("uiNotLocked"))+" · "+L("tradeTheirOffer")+": "+(theirLocked?L("tradeLock"):L("uiNotLocked"));
                return new[]{host.Section("ours",L("tradeWith")+" "+other,status+"\n"+L("uiCloseKeepsTrade")+"\n"+L("uiPublishedSilver")+": "+(my?.SilverAmount??0),batch.Bindings,
                    new[]{host.Field("silver",L("tradeSilverAmount"),silverBuffer,true,int.TryParse(silverBuffer,out int v)&&v>=0?null:L("uiSilverInvalid"),Editable(trade.State)?null:L("uiNotEditable"))},
                    host.List("my-pawns",ours,L(picking?"noTradeablePawnsAvailable":"uiEmptyOffer"),selection), status:modernNotice, tone:"Warning"),
                    host.Section("theirs",L("tradeTheirOffer"),L("tradeSilverAmount")+": "+(their?.SilverAmount??0)+" "+L("silver"),list:host.List("their-pawns",theirs,L("uiEmptyOffer"),readOnly:true))};
            },1);
        }
        private static HostRow SummaryRow(string id,PawnSummary summary)
        {return TalentPawnIcons.Remote(new HostRow(id,summary.GetDisplayLabel(),TalentCard.Details(summary)),summary);}
    }
}
