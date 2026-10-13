using System;
using System.Collections.Generic;
using System.Linq;
namespace Phinix.LegacyTalentTradeExtension.Client
{
    public partial class DirectTradePanel
    {
        private TalentPresentationView presentationView;
        private string selectedUser, selectedTrade;
        private static string L(string key)=> ("Phinix_legacyTalentTrade_"+key).Localize().ToString();
        internal void ViewOpened()
        {if(presentationView==null&&BuiltInLegacyTalentTradeClientExtension.Presentation!=null)presentationView=new TalentPresentationView(BuiltInLegacyTalentTradeClientExtension.Presentation,"talent-direct-list"){UseNavigation=true};}
        internal void ViewClosed(){var old=presentationView;presentationView=null;old?.Dispose();}
        internal void StopPresentation(){ViewClosed();openTradeWindow?.Close();openTradeWindow=null;}
        private int factsVersion=-1;private string factsStamp="";
        private string PresentationStamp()
        {if(factsVersion!=TalentTradeManager.StateVersion){factsVersion=TalentTradeManager.StateVersion;factsStamp=TalentProjectionFingerprint.Direct(TalentTradeManager.GetActiveTradesSnapshot());}
         return factsStamp+":"+System.Threading.Volatile.Read(ref usersVersion)+":"+TalentTradeManager.GetLocalUuid()+":"+LegacyTalentTradeRuntime.IsOnline;}

        internal void ViewUpdate()
        {
            if(presentationView==null)return;
            while(presentationView.Input(out var input))
            {if(input.Id=="direct-users")selectedUser=input.Value;else if(input.Id=="direct-trades")selectedTrade=input.Value;presentationView.Invalidate();}
            var host=BuiltInLegacyTalentTradeClientExtension.Presentation;
            presentationView.Refresh(PresentationStamp,null,"direct-list",selectedTrade??selectedUser,null,()=>
            {
                string local=TalentTradeManager.GetLocalUuid(); var actions=new List<HostAction>();
                bool online=LegacyTalentTradeRuntime.IsOnline;
                bool available=online&&selectedUser!=null&&selectedUser!=local&&TalentTradeManager.GetOnlineUserUuids().Contains(selectedUser);
                string user=selectedUser;
                actions.Add(new HostAction("start",L("tradeWith"),()=>{var id=TalentTradeManager.InitiateDirectTrade(user);if(id!=null)OpenTradeWindow(id);},available?null:L("uiSelectUser")));
                var trade=TalentTradeManager.GetTrade(selectedTrade);string id2=trade?.Id;
                if(trade!=null&&online)
                {
                    if(trade.State==DirectTradeState.Pending&&trade.TargetUuid==local)
                    {actions.Add(new HostAction("accept",L("tradeAcceptRequest"),()=>{TalentTradeManager.AcceptTrade(id2);OpenTradeWindow(id2);}));actions.Add(new HostAction("reject",L("tradeRejectRequest"),()=>TalentTradeManager.RejectTrade(id2)));}
                    else
                    {actions.Add(new HostAction("open",L("tradeOpen"),()=>OpenTradeWindow(id2)));var cancel=new HostAction("cancel",L("cancel"),()=>TalentTradeManager.CancelTrade(id2)){Kind="Danger",ConfirmTitle=L("cancel"),ConfirmText=L("uiCancelTrade"),ConfirmAccept=L("cancel"),ConfirmCancel=L("uiBack")};actions.Add(cancel);}
                }
                return actions;
            },batch=>
            {
                string local=TalentTradeManager.GetLocalUuid();var users=new List<HostRow>();
                foreach(string uuid in TalentTradeManager.GetOnlineUserUuids())
                {if(uuid==local)continue;string name;users.Add(new HostRow(uuid,LegacyTalentTradeRuntime.TryGetDisplayName(uuid,out name)?name:uuid));}
                var trades=new List<HostRow>();foreach(var trade in TalentTradeManager.GetActiveTradesSnapshot())
                {if(trade==null)continue;bool mine=trade.InitiatorUuid==local;string name=mine?trade.TargetName:trade.InitiatorName;
                 trades.Add(new HostRow(trade.Id,string.IsNullOrEmpty(name)?mine?trade.TargetUuid:trade.InitiatorUuid:name,L("tradeStatus")+": "+trade.State));}
                return new[]{host.Section("members",L("tradeOnlineUsers"),LegacyTalentTradeRuntime.IsOnline?L("uiSelectUser"):L("pleaseLogIn"),batch.Bindings.Where(b=>b.OwnerId==host.OwnerId&&b.LocalId=="start"),list:host.List("direct-users",users,L("uiNoUsers"),selectedUser)),
                    host.Section("trades",L("tradeActiveTrades"),L("uiCloseKeepsTrade"),batch.Bindings.Where(b=>b.LocalId!="start"||b.OwnerId!=host.OwnerId),list:host.List("direct-trades",trades,L("tradeNoActive"),selectedTrade))};
            },1);
        }
    }
}
