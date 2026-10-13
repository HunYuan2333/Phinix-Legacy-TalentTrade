using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using PhinixClient;
using RimWorld;
using UnityEngine;
using Verse;
namespace Phinix.LegacyTalentTradeExtension.Client
{
    internal sealed class TalentCatalogDraft {internal string Mode="browse",Search="",Price,Days="15",Deposit="500";}
    internal static class TalentCatalogRules
    {
        internal static bool Number(string text,int minimum,out int value)=>int.TryParse(text,NumberStyles.Integer,CultureInfo.InvariantCulture,out value)&&value>=minimum;
        internal static bool MarketVisible(MarketListing listing,string user,bool mine)=>listing!=null&&listing.State==MarketListingState.Active&&(!mine||listing.SellerUuid==user);
        internal static bool RentalVisible(RentalContract contract,string user,string mode)=>contract!=null&&
            (!(contract.State==RentalContractState.Listed||contract.State==RentalContractState.Active)||TradeablePawnUtility.CanRentPawn(contract.Summary))&&
            (mode=="mine"?contract.OwnerUuid==user&&contract.State==RentalContractState.Listed:mode=="rented"?contract.RenterUuid==user&&contract.State==RentalContractState.Active:contract.State==RentalContractState.Listed);
        internal static int DaysLeft(RentalContract contract,int ticks)=>(int)Math.Min(int.MaxValue,Math.Max(0,((long)contract.ExpiryTick-ticks)/GenDate.TicksPerDay));
    }
    internal sealed class TalentCatalogPresentation : IDisposable
    {
        private readonly MarketPanel market; private readonly RentalPanel rental;
        private readonly TalentHostPresentation host; private readonly TalentPresentationView view;
        private string mode="browse",search="",selection,price,days="15",deposit="500",notice;
        private Pawn pawn;private object game,map;
        private int version=-1;private string factsStamp="";
        private void RefreshFacts(){if(version==TalentTradeManager.StateVersion)return;version=TalentTradeManager.StateVersion;listings=TalentTradeManager.GetMarketListingsSnapshot();contracts=TalentTradeManager.GetRentalContractsSnapshot();factsStamp=IsRental?TalentProjectionFingerprint.Rental(contracts):TalentProjectionFingerprint.Market(listings);}
        private MarketListing[] listings=Array.Empty<MarketListing>();private RentalContract[] contracts=Array.Empty<RentalContract>();
        private readonly Dictionary<string,Pawn> pawns=new Dictionary<string,Pawn>();
        private bool IsRental=>rental!=null;
        private TalentCatalogDraft Draft=>IsRental?rental.PresentationDraft:market.PresentationDraft;
        private static string L(string key)=>("Phinix_legacyTalentTrade_"+key).Localize().ToString();
        private static Map CurrentMap=>Current.Game==null?null:Find.CurrentMap;
        private static int Ticks=>Current.Game==null?0:Find.TickManager?.TicksGame??0;
        internal TalentCatalogPresentation(MarketPanel market,RentalPanel rental)
        {this.market=market;this.rental=rental;host=BuiltInLegacyTalentTradeClientExtension.Presentation;view=new TalentPresentationView(host,IsRental?"talent-rental":"talent-market"){UseNavigation=true,PrimaryHeaders=1};price=Draft.Price??(IsRental?"50":"100");mode=Draft.Mode;search=Draft.Search;days=Draft.Days;deposit=Draft.Deposit;game=Current.Game;map=CurrentMap;}
        private string Stamp()
        {
            RefreshFacts();string time="";
            if(IsRental&&mode=="rented")foreach(var c in contracts)if(c!=null&&c.RenterUuid==TalentTradeManager.GetLocalUuid()&&c.State==RentalContractState.Active)time+=c.Id+":"+TalentCatalogRules.DaysLeft(c,Ticks)+";";
            return factsStamp+":"+TalentTradeManager.GetLocalUuid()+":"+LegacyTalentTradeRuntime.IsOnline+":"+mode+":"+search+":"+selection+":"+price+":"+days+":"+deposit+":"+time;
        }
        internal void Draw(Rect rect)=>view.Draw(rect);
        private void SelectMode(string value){mode=value;selection=null;pawn=null;notice=null;pawns.Clear();view.Invalidate();}
        internal void Update()
        {
            if(!ReferenceEquals(game,Current.Game)||!ReferenceEquals(map,CurrentMap))
            {game=Current.Game;map=CurrentMap;pawn=null;selection=null;pawns.Clear();view.Invalidate();}
            RefreshFacts();
            while(view.Input(out var input))
            {
                if(input.Id=="search")search=input.Value;
                else if(input.Id=="price")price=input.Value;
                else if(input.Id=="days")days=input.Value;
                else if(input.Id=="deposit")deposit=input.Value;
                else if(input.Id=="catalog") {selection=input.Value;if(mode=="create"){pawn=null;if(!string.IsNullOrEmpty(selection))pawns.TryGetValue(selection,out pawn);}}
                view.Invalidate();
            }
            string user=TalentTradeManager.GetLocalUuid();
            var listing=listings.FirstOrDefault(l=>l!=null&&l.Id==selection&&TalentCatalogRules.MarketVisible(l,user,mode=="mine"));
            var contract=contracts.FirstOrDefault(c=>c!=null&&c.Id==selection&&TalentCatalogRules.RentalVisible(c,user,mode));
            view.DraftValues=new Dictionary<string,string>{{"price",price},{"days",days},{"deposit",deposit},{"search",search}};
            view.Refresh(Stamp,mode=="create"?null:selection,mode,selection,null,()=>BuildActions(listing,contract,user),batch=>BuildSections(batch,listing,contract,user),2);
        }
        private HostAction Action(string id,string key,Action execute,string disabled=null)=>new HostAction(id,L(key),()=>{execute();view.Invalidate();},disabled);
        private HostAction Confirm(HostAction action,string text)
        {action.ConfirmTitle=action.Label;action.ConfirmText=text;action.ConfirmAccept=action.Label;action.ConfirmCancel=L("uiBack");return action;}
        private IEnumerable<HostAction> BuildActions(MarketListing listing,RentalContract contract,string user)
        {
            string offline=LegacyTalentTradeRuntime.IsOnline?null:L("pleaseLogIn");var actions=new List<HostAction>();
            actions.Add(Action("browse","uiBrowse",()=>SelectMode("browse")));
            actions.Add(Action("mine",IsRental?"rentalMyListings":"marketMyListings",()=>SelectMode("mine")));
            if(IsRental)actions.Add(Action("rented","rentalMyRentals",()=>SelectMode("rented")));
            actions.Add(Action("create",IsRental?"rentalList":"marketSell",()=>SelectMode("create"),offline));
            actions.Add(Action("refresh","refresh",()=>{if(!string.IsNullOrEmpty(user)){TalentTradeManager.SendProtocol(TalentTradeProtocol.BuildMarketSync(user));notice=L("uiRequestPending");}},offline));
            if(!IsRental)actions.Add(Confirm(Action("cleanup","forceCleanup",()=>{market.CleanupModern();notice=L("uiCleanupRequested");},offline),L("uiCleanupWarning")));
            if(mode=="create")
            {
                bool validPrice=TalentCatalogRules.Number(price,1,out int p),validDays=TalentCatalogRules.Number(days,1,out int d),validDeposit=TalentCatalogRules.Number(deposit,0,out int dep);
                string invalid=offline??(pawn==null?L("uiSelectPawn"):!validPrice||IsRental&&(!validDays||!validDeposit)?L("uiNumberInvalid"):null);
                if(pawn!=null&&(pawn.Dead||!pawn.Spawned||!TradeablePawnUtility.GetTradeablePawns(CurrentMap).Contains(pawn)))invalid=L("uiSelectPawn");
                if(pawn!=null&&!IsRental&&MarketPanel.IsBlockedPawn(pawn))invalid=L("ocPawnBlocked");
                if(pawn!=null&&IsRental&&!TradeablePawnUtility.CanRentPawn(pawn))invalid=L("rentalColonistOnly");
                Pawn captured=pawn;
                actions.Add(Confirm(Action("publish",IsRental?"rentalList":"marketSell",()=>
                {
                    if(captured==null||!captured.Spawned||captured.Dead||!TradeablePawnUtility.GetTradeablePawns(CurrentMap).Contains(captured))return;
                    if(IsRental)rental.SubmitModern(captured,p,d,dep);else market.SubmitModern(captured,p);
                    if(!captured.Spawned){SelectMode("mine");notice=L("uiListingRequested");}else notice=L("uiOperationIncomplete");
                },invalid),L("uiPublishConfirm")));
            }
            else if(!IsRental&&listing!=null)
            {
                if(listing.SellerUuid==user)
                {var delist=Action("delist","marketDelist",()=>{market.DelistModern(listing);notice=L("uiRequestPending");},offline);actions.Add(Confirm(delist,L("uiDelistConfirm")));}
                else
                {
                    var buy=Action("buy","marketBuy",()=>{market.BuyModern(listing);notice=L("uiRequestPending");},offline);
                    string text="Phinix_legacyTalentTrade_marketBuyConfirm".Localize(listing.Summary?.GetDisplayLabel()??"???",listing.PriceSilver.ToString());
                    ApplyCompatibility(buy,listing.Summary,listing.DefManifestData,ref text);actions.Add(Confirm(buy,text));
                }
            }
            else if(IsRental&&contract!=null)
            {
                if(contract.RenterUuid==user&&contract.State==RentalContractState.Active)
                    actions.Add(Confirm(Action("return","rentalReturn",()=>{rental.ReturnModern(contract);notice=L("uiRequestPending");},offline),L("uiReturnConfirm")));
                else if(contract.OwnerUuid==user)
                    actions.Add(Confirm(Action("delist","rentalDelist",()=>{rental.DelistModern(contract);notice=L("uiRequestPending");},offline),L("uiDelistConfirm")));
                else
                {
                    var rent=Action("rent","rentalRent",()=>{rental.RentModern(contract);notice=L("uiRequestPending");},offline);
                    string text="Phinix_legacyTalentTrade_rentalRentConfirm".Localize(contract.Summary?.GetDisplayLabel()??"???",contract.PricePerDay.ToString(),contract.MaxDays.ToString(),contract.Deposit.ToString());
                    ApplyCompatibility(rent,contract.Summary,contract.DefManifestData,ref text);actions.Add(Confirm(rent,text));
                }
            }
            foreach(var action in actions){if(action.Id=="cleanup")action.Kind="Danger";else if(action.Id=="publish"||action.Id=="buy"||action.Id=="rent")action.Kind="Primary";action.Selected=action.Id==mode;}
            return actions;
        }
        private static void ApplyCompatibility(HostAction action,PawnSummary summary,string manifest,ref string text)
        {
            if(summary!=null&&!string.IsNullOrEmpty(summary.RaceDefName)&&DefDatabase<ThingDef>.GetNamedSilentFail(summary.RaceDefName)==null)
            {action.Disabled="Phinix_legacyTalentTrade_cannotBuyNoRace".Localize(summary.GetDisplayLabel(),summary.RaceDefName);return;}
            if(!TransferCompatibilityUi.BuildConfirmation(text,summary?.GetDisplayLabel()??"???",new[]{manifest},out string confirmation))action.Disabled=confirmation;
            text=confirmation;
        }
        private IEnumerable<object> BuildSections(HostBatch batch,MarketListing listing,RentalContract contract,string user)
        {
            var toolbar=batch.Bindings.Where(b=>b.OwnerId==host.OwnerId&&(b.LocalId=="browse"||b.LocalId=="mine"||b.LocalId=="rented"||b.LocalId=="create"||b.LocalId=="refresh")).ToArray();
            var dangerous=batch.Bindings.Where(b=>b.OwnerId==host.OwnerId&&b.LocalId=="cleanup").ToArray();
            var operations=batch.Bindings.Where(b=>!toolbar.Contains(b)&&!dangerous.Contains(b)).ToArray();var rows=new List<HostRow>();pawns.Clear();
            if(mode=="create")
            {foreach(var p in TradeablePawnUtility.GetTradeablePawns(CurrentMap)){if(IsRental&&!TradeablePawnUtility.CanRentPawn(p))continue;string label=TradeablePawnUtility.GetLabel(p);if(!Matches(label,null))continue;string id=p.GetUniqueLoadID();pawns[id]=p;rows.Add(TalentPawnIcons.Local(new HostRow(id,TradeablePawnUtility.GetLabel(p),TalentCard.Details(PawnSummary.FromPawn(p))),p));}}
            else if(IsRental)
            {foreach(var c in contracts){if(!TalentCatalogRules.RentalVisible(c,user,mode))continue;string label=c.Summary?.GetDisplayLabel()??"???";if(!Matches(label,c.OwnerName))continue;rows.Add(Row(c.Id,c.Summary,"Phinix_legacyTalentTrade_rentalOwner".Localize(c.OwnerName??"???")+" · "+c.State));}}
            else foreach(var l in listings)
            {if(!TalentCatalogRules.MarketVisible(l,user,mode=="mine"))continue;string label=l.Summary?.GetDisplayLabel()??"???";if(!Matches(label,l.SellerName))continue;rows.Add(Row(l.Id,l.Summary,"Phinix_legacyTalentTrade_marketPriceFormat".Localize(l.PriceSilver.ToString())+" · "+(l.SellerName??"???")));}
            var header=host.Section("filter",L(IsRental?"subTabRental":"subTabMarket"),LegacyTalentTradeRuntime.IsOnline?null:L("pleaseLogIn"),toolbar,
                new[]{host.Field("search",L("uiSearch"),search)});
            if(dangerous.Length>0)header=host.ActionMenu(host.Section("filter",L("subTabMarket"),LegacyTalentTradeRuntime.IsOnline?null:L("pleaseLogIn"),toolbar.Concat(dangerous),new[]{host.Field("search",L("uiSearch"),search)}),"dangerous",L("uiMaintenance"),dangerous);
            var list=host.Section("catalog",L(mode=="create"?"uiSelectPawn":"uiListings"),"",list:host.List("catalog",rows,L(IsRental?"rentalNoListings":"marketNoListings"),selection));
            var fields=new List<object>();string title=L("uiSelectListing"),summary="";
            if(mode=="create")
            {
                title=L(IsRental?"rentalList":"marketSell");summary=(pawn==null?L("uiSelectPawn"):TalentCard.Description(PawnSummary.FromPawn(pawn))+"\n"+TalentCard.Details(PawnSummary.FromPawn(pawn)));
                fields.Add(host.Field("price",L(IsRental?"rentalPricePerDay":"marketPrice"),price,true,TalentCatalogRules.Number(price,1,out _)?null:L("uiNumberInvalid")));
                if(IsRental){fields.Add(host.Field("days",L("rentalMaxDays"),days,true,TalentCatalogRules.Number(days,1,out _)?null:L("uiNumberInvalid")));fields.Add(host.Field("deposit",L("rentalDeposit"),deposit,true,TalentCatalogRules.Number(deposit,0,out _)?null:L("uiNumberInvalid")));}
            }
            else if(IsRental&&contract!=null)
            {
                title=contract.Summary?.GetDisplayLabel()??"???";summary=RaceLine(contract.Summary)+"\n"+TalentCard.Details(contract.Summary)+"\n"+L("rentalOwner")+": "+contract.OwnerName+"\n"+L("rentalPricePerDay")+": "+contract.PricePerDay+"\n"+L("rentalDeposit")+": "+contract.Deposit+"\n"+L("rentalMaxDays")+": "+contract.MaxDays+"\n"+contract.State;
                if(contract.RenterUuid==user&&contract.State==RentalContractState.Active&&contract.ExpiryTick>0)summary+="\n"+"Phinix_legacyTalentTrade_rentalDaysLeft".Localize(TalentCatalogRules.DaysLeft(contract,Ticks).ToString());
            }
            else if(listing!=null){title=listing.Summary?.GetDisplayLabel()??"???";summary=RaceLine(listing.Summary)+"\n"+TalentCard.Details(listing.Summary)+"\n"+"Phinix_legacyTalentTrade_marketSeller".Localize(listing.SellerName??"???")+"\n"+"Phinix_legacyTalentTrade_marketPriceFormat".Localize(listing.PriceSilver.ToString())+"\n"+listing.State;}
            return new[]{header,list,host.FieldColumns(host.Section("details",title,summary+"\n"+L("uiCloseKeepsTrade"),operations,fields,status:notice,tone:"Pending"))};
        }
        private bool Matches(string label,string owner)=>string.IsNullOrEmpty(search)||(label??"").IndexOf(search,StringComparison.InvariantCultureIgnoreCase)>=0||(owner??"").IndexOf(search,StringComparison.InvariantCultureIgnoreCase)>=0;
        private static string RaceLine(PawnSummary summary){if(summary==null)return "";string race=summary.RaceDefName??"Human";return (DefDatabase<ThingDef>.GetNamedSilentFail(race)!=null?"✓ ":"✗ ")+race;}
        private static HostRow Row(string id,PawnSummary summary,string detail)=>TalentPawnIcons.Remote(new HostRow(id,summary?.GetDisplayLabel()??"???",detail+"\n"+RaceLine(summary)),summary);
        public void Dispose(){Draft.Mode=mode;Draft.Search=search;Draft.Price=price;Draft.Days=days;Draft.Deposit=deposit;view.Dispose();pawns.Clear();pawn=null;game=null;map=null;}
    }
}
