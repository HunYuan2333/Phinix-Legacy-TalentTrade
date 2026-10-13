using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using PhinixClient;
using PhinixClient.Framework;
using Phinix.LegacyTalentTradeExtension;
using UnityEngine;
using Utils.Framework;
internal static class CurrentProbe
{
    private const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    private static object Call(object obj,string name,params object[] args)=>obj.GetType().GetMethod(name,Private).Invoke(obj,args);
    private static object Get(object obj,string name)=>obj.GetType().GetField(name,Private).GetValue(obj);
    private static void Set(object obj,string name,object value)=>obj.GetType().GetField(name,Private).SetValue(obj,value);
    public static void Run(Assembly candidate)
    {

        var provider=new Provider();int opens=0,updates=0,closes=0;
        var wrapper=new ClientPresentationMainTab(provider,()=>opens++,()=>updates++,()=>closes++,"packets");
        wrapper.ViewUpdate();Program.Check(updates==0,"Hidden provider receives no owner update");
        wrapper.ViewOpened();wrapper.ViewOpened();wrapper.ViewUpdate();wrapper.ViewClosed();wrapper.ViewClosed();
        Program.Check(opens==1&&updates==1&&closes==1,"Provider lifecycle open/close is paired and idempotent");
        wrapper.ViewOpened();wrapper.ViewClosed();Program.Check(opens==2&&closes==2,"Provider can reopen");
        Program.Check(wrapper.TabLabel==provider.TabLabel&&wrapper.TabOrder==provider.TabOrder&&wrapper.LayoutHints.MinimumContentSize.x==provider.LayoutHints.MinimumContentSize.x&&wrapper.NavigationId=="packets","Public wrapper preserves navigation and responsive hints");
        int failedCleanup=0;var bad=new ClientPresentationMainTab(provider,()=>throw new InvalidOperationException(),()=>{},()=>failedCleanup++,"failed");
        try{bad.ViewOpened();}catch(InvalidOperationException){}bad.ViewClosed();Program.Check(failedCleanup==1,"Failed open releases the partially opened owner exactly once");
        var host=new ExtensionHostContext();var services=new Services();var module=new Module();
        host.AddService<IClientPresentationService>(services);host.AddService<IClientExtensionPointService>(services);host.AddService<IClientTheme>(services);
        Type bridgeType=candidate.GetType("Phinix.LegacyTalentTradeExtension.Client.TalentHostPresentation",true);
        var bridge=Activator.CreateInstance(bridgeType,Private,null,new object[]{host,module,typeof(IClientSessionContext).Assembly},null);
        Program.Check(services.Definitions==1&&services.PointId==TalentPresentationActionContext.PointId,"Adapter defines the ordinary public point once");
        Program.Check((long)bridgeType.GetProperty("ThemeVersion",Private).GetValue(bridge)==7,"Adapter reads borrowed Host theme version");
        var actionType=candidate.GetType("Phinix.LegacyTalentTradeExtension.Client.HostAction",true);int invoked=0;
        var action=Activator.CreateInstance(actionType,Private,null,new object[]{"send","Send",(Action)(()=>invoked++),null},null);
        Set(action,"Kind","Primary");Set(action,"Selected",true);Set(action,"ConfirmTitle","Confirm");Set(action,"ConfirmText","Submission is not business confirmation");Set(action,"ConfirmAccept","Send");Set(action,"ConfirmCancel","Keep");
        var actionArray=Array.CreateInstance(actionType,1);actionArray.SetValue(action,0);
        Type factoryType=typeof(Func<,>).MakeGenericType(typeof(TalentPresentationActionContext),typeof(IEnumerable<>).MakeGenericType(actionType));
        var parameter=Expression.Parameter(typeof(TalentPresentationActionContext));
        var callback=Expression.Lambda(factoryType,Expression.Convert(Expression.Constant(actionArray),factoryType.GetMethod("Invoke").ReturnType),parameter).Compile();
        ((IDictionary)Get(bridge,"Sources"))["view"]=callback;
        bool current=true;var context=new TalentPresentationActionContext("view",1,"trade","Negotiating","pawn","100",true);
        object batch=Call(bridge,"Collect",context,(Func<bool>)(()=>current),actionArray);
        var bindings=(IList)Get(batch,"Bindings");Program.Check(bindings.Count==1,"Real Host action binding is collected");
        var binding=(ClientPresentationAction)Get(bindings[0],"Model");
        Program.Check(binding.Kind==ClientButtonKind.Primary&&binding.IsSelected&&binding.Confirmation.Text.Contains("not business"),"Public selected/primary/confirmation metadata is retained");
        binding.Action.TryExecute();Program.Check(invoked==1,"Invocation uses the one public registry action");current=false;binding.Action.TryExecute();Program.Check(invoked==1,"Registry receives the exact stale-context guard");current=true;
        var field=(ClientPresentationTextField)Call(bridge,"Field","quantity","Quantity","200",true,"0–100",null);
        Program.Check(field.InputKind==ClientPresentationTextInputKind.Integer&&field.ValidationText=="0–100","Adapter constructs the public integer field with owner validation");
        Type rowType=candidate.GetType("Phinix.LegacyTalentTradeExtension.Client.HostRow",true);
        object row=Activator.CreateInstance(rowType,Private,null,new object[]{"one","Claim","100"},null);Set(row,"Badge","Best");Set(row,"BadgeColor","talent-trade.accent");
        var rowArray=Array.CreateInstance(rowType,1);rowArray.SetValue(row,0);
        var list=(ClientPresentationList)Call(bridge,"List","claims",rowArray,"No claims",null,true);
        Program.Check(list.IsReadOnly&&list.Rows[0].Badges[0].ColorKey=="talent-trade.accent","Readonly claim projection uses real Host list and theme badge");
        Program.Check(list.HoverColorKey=="talent-trade.highlight"&&list.SelectionColorKey=="talent-trade.accent","Registered plugin list theme roles survive readonly conversion");
        int textureCalls=0;object dynamicRow=Activator.CreateInstance(rowType,Private,null,new object[]{"portrait","Portrait","Borrowed"},null);
        Set(dynamicRow,"IconSource",(Func<Texture>)(()=>{textureCalls++;return null;}));var portraitRows=Array.CreateInstance(rowType,1);portraitRows.SetValue(dynamicRow,0);
        var portraitList=(ClientPresentationList)Call(bridge,"List","portraits",portraitRows,"",null,true);
        Program.Check(portraitList.Rows[0].IconSource!=null&&textureCalls==0,"Actual plugin adapter passes a passive texture source to public Host without resolving it");
        object section=Call(bridge,"Section","draft","Draft","Projection only",bindings,new object[]{field},list,"Waiting","Pending",true,null);
        section=Call(bridge,"FieldColumns",section);
        Program.Check(((ClientPresentationSection)section).FieldColumns==2,"Adapter binds the same public responsive field-column hint");
        object view=Call(bridge,"Open","view");
        Call(bridge,"Publish",view,"view","Red packets",1L,new[]{section},batch,(Func<bool>)(()=>current),0,null,null,0,0,0);
        Program.Check(services.View.Page.Sections[0].Status.Tone==ClientPresentationTone.Pending&&Get(batch,"Owned")==null,"Publication transfers the one owned action set and the actual public page");
        services.View.Inputs.Enqueue(new ClientPresentationInput(1,"quantity",ClientPresentationInputKind.TextChanged,"100"));
        var method=bridgeType.GetMethod("Input",Private);object[] inputArgs={view,null};
        Program.Check((bool)method.Invoke(bridge,inputArgs)&&(string)Get(inputArgs[1],"Value")=="100"&&(long)Get(inputArgs[1],"Revision")==1,"Revisioned Host input is preserved by the adapter");
        ((IDisposable)view).Dispose();Program.Check((bool)Call(bridge,"Closed",view)&&services.SetsDisposed==1,"Closing the view releases its transferred set");
        var orphan=new TalentPresentationActionContext("gone",2,null,null,null,null,false);
        object empty=Call(bridge,"Collect",orphan,(Func<bool>)(()=>true),Array.CreateInstance(actionType,0));Program.Check(((IList)Get(empty,"Bindings")).Count==0,"Closed/foreign view identity grants no official operation");((IDisposable)empty).Dispose();
        var viewType=candidate.GetType("Phinix.LegacyTalentTradeExtension.Client.TalentPresentationView",true);
        var lifecycleView=Activator.CreateInstance(viewType,Private,null,new object[]{bridge,"lifecycle"},null);
        Type sectionFactory=typeof(Func<,>).MakeGenericType(candidate.GetType("Phinix.LegacyTalentTradeExtension.Client.HostBatch"),typeof(IEnumerable<object>));
        object body=Call(bridge,"Section","body","Facts","Readonly projection",null,null,null,null,"Neutral",true,null);
        var bp=Expression.Parameter(sectionFactory.GetGenericArguments()[0]);
        var sectionCallback=Expression.Lambda(sectionFactory,Expression.Convert(Expression.Constant(new[]{body}),typeof(IEnumerable<object>)),bp).Compile();
        int stamp=0; Func<string> snapshot=()=>stamp.ToString();
        Call(lifecycleView,"Refresh",snapshot,"trade","Pending",null,"0",Expression.Lambda(typeof(Func<>).MakeGenericType(factoryType.GetGenericArguments()[1]),Expression.Convert(Expression.Constant(actionArray),factoryType.GetGenericArguments()[1])).Compile(),sectionCallback,0);
        var oldPage=services.View.Page;
        stamp++; var bound=services.View.Set.Actions[0]; int oldInvoked=invoked;
        bound.TryExecute();Program.Check(invoked==oldInvoked,"Changed projection guard rejects old owner intent");
        ((IDisposable)lifecycleView).Dispose();
        Program.Check(services.View.IsClosed,"Plugin view closes its public session");
        object afterClose=Call(bridge,"Collect",new TalentPresentationActionContext("lifecycle",3,"trade","Pending",null,"0",true),(Func<bool>)(()=>true),actionArray);
        Program.Check(((IList)Get(afterClose,"Bindings")).Count==0,"Closed view removes the owner contribution source");((IDisposable)afterClose).Dispose();
        CheckCatalogFacts(candidate,bridge,services,actionArray,factoryType);
        CheckWindowFacts(candidate);
        CheckPawnRecovery(candidate);
        ((IDisposable)bridge).Dispose();((IDisposable)bridge).Dispose();Program.Check(services.ScopeDisposals==1,"Module-owned ordinary contribution scope is released once");
    }
    private static void CheckCatalogFacts(Assembly candidate,object bridge,Services services,Array actionArray,Type factoryType)
    {
        var values=new Dictionary<string,string>{{"price","10"}};
        var context=new TalentPresentationActionContext("test",1,"id","create",null,null,true).WithDraftValues(values);
        values["price"]="999";Program.Check(context.DraftValues["price"]=="10","Contribution draft facts are defensive copies");
        bool readOnly=false;try{((IDictionary<string,string>)context.DraftValues)["price"]="0";}catch(NotSupportedException){readOnly=true;}
        Program.Check(readOnly,"Third parties cannot mutate owner drafts");
        var rules=candidate.GetType("Phinix.LegacyTalentTradeExtension.Client.TalentCatalogRules",true);
        var number=rules.GetMethod("Number",BindingFlags.Static|BindingFlags.NonPublic);
        foreach(string invalid in new[]{"", "-1", "1.5", "2147483648", "0"})
        {object[] args={invalid,1,0};Program.Check(!(bool)number.Invoke(null,args),"Invalid positive input rejected: "+invalid);}
        object[] deposit={"0",0,0};Program.Check((bool)number.Invoke(null,deposit),"Zero deposit retained");
        var market=new Phinix.LegacyTalentTradeExtension.Client.MarketListing{Id="listing",SellerUuid="owner",PriceSilver=10,State=Phinix.LegacyTalentTradeExtension.Client.MarketListingState.Active};
        var marketVisible=rules.GetMethod("MarketVisible",BindingFlags.Static|BindingFlags.NonPublic);
        Program.Check((bool)marketVisible.Invoke(null,new object[]{market,"other",false})&&!(bool)marketVisible.Invoke(null,new object[]{market,"other",true}),"Market own filter follows actual publisher");
        var rental=new Phinix.LegacyTalentTradeExtension.Client.RentalContract{Id="rent",OwnerUuid="owner",RenterUuid="renter",State=Phinix.LegacyTalentTradeExtension.Client.RentalContractState.Active,Summary=new Phinix.LegacyTalentTradeExtension.Client.PawnSummary{PawnKind="Colonist"}};
        var rentalVisible=rules.GetMethod("RentalVisible",BindingFlags.Static|BindingFlags.NonPublic);
        Program.Check((bool)rentalVisible.Invoke(null,new object[]{rental,"renter","rented"})&&!(bool)rentalVisible.Invoke(null,new object[]{rental,"other","rented"}),"Rental view uses actual active renter");
        rental.State=Phinix.LegacyTalentTradeExtension.Client.RentalContractState.Listed;rental.Summary.PawnKind="Animal";
        Program.Check(!(bool)rentalVisible.Invoke(null,new object[]{rental,"other","browse"}),"Existing colonist-only rental rule retained");
        var fingerprint=candidate.GetType("Phinix.LegacyTalentTradeExtension.Client.TalentProjectionFingerprint",true).GetMethod("Market",BindingFlags.Static|BindingFlags.NonPublic);
        string before=(string)fingerprint.Invoke(null,new object[]{new[]{market}});market.LastRefreshUtc=DateTime.UtcNow;
        Program.Check(before==(string)fingerprint.Invoke(null,new object[]{new[]{market}}),"Heartbeat timestamp does not change presentation revision");
        market.PriceSilver++;Program.Check(before!=(string)fingerprint.Invoke(null,new object[]{new[]{market}}),"Changed price invalidates prior confirmation");
        var rowFactory=candidate.GetType("Phinix.LegacyTalentTradeExtension.Client.TalentCatalogPresentation",true).GetMethod("Row",BindingFlags.Static|BindingFlags.NonPublic);
        object missingRace=rowFactory.Invoke(null,new object[]{"missing",new Phinix.LegacyTalentTradeExtension.Client.PawnSummary{RaceDefName=null},"Listing"});
        Program.Check(((string)Get(missingRace,"Detail")).Contains("Human"),"Null race uses original Human fallback and keeps race evidence visible");
        var module=candidate.GetType("Phinix.LegacyTalentTradeExtension.Client.BuiltInLegacyTalentTradeClientExtension",true);
        var presentation=module.GetField("<Presentation>k__BackingField",BindingFlags.Static|BindingFlags.NonPublic);
        presentation.SetValue(null,bridge);
        var navFactory=typeof(Func<>).MakeGenericType(factoryType.GetGenericArguments()[1]);
        Set(bridge,"Navigation",Expression.Lambda(navFactory,Expression.Convert(Expression.Constant(Array.CreateInstance(actionArray.GetType().GetElementType(),0)),factoryType.GetGenericArguments()[1])).Compile());
        // One actual owner navigation definition; no native drawing or business command.
        var nav=Activator.CreateInstance(actionArray.GetType().GetElementType(),Private,null,new object[]{"nav-market","Market",(Action)(()=>{}),null},null);
        var navArray=Array.CreateInstance(actionArray.GetType().GetElementType(),1);navArray.SetValue(nav,0);
        Set(bridge,"Navigation",Expression.Lambda(navFactory,Expression.Convert(Expression.Constant(navArray),factoryType.GetGenericArguments()[1])).Compile());
        foreach(bool renting in new[]{false,true})
        {
            string panelName=renting?"RentalPanel":"MarketPanel";object panel=Activator.CreateInstance(candidate.GetType("Phinix.LegacyTalentTradeExtension.Client."+panelName,true));
            var catalogType=candidate.GetType("Phinix.LegacyTalentTradeExtension.Client.TalentCatalogPresentation",true);
            var catalog=Activator.CreateInstance(catalogType,Private,null,renting?new object[]{null,panel}:new object[]{panel,null},null);
            Call(catalog,"Update");var page=services.View.Page;
            Program.Check(page.Sections.Count==4&&page.SplitView.GlobalHeaderSections==1&&page.SplitView.PrimaryHeaderSections==1&&page.SplitView.PrimarySectionCount==3,"Navigation + fixed filter + list/detail form has valid public split accounting");
            if(!renting)Program.Check(page.Sections[1].ActionMenus.Count==1&&page.Sections[1].ActionMenus[0].ActionIds.Count==1,"Force cleanup is isolated in a public action menu");
            ((IDisposable)catalog).Dispose();Program.Check(services.View.IsClosed,"Catalog close disposes the public session");
        }
        var settingsType=candidate.GetType("Phinix.LegacyTalentTradeExtension.Client.TalentSettingsWindow",true);
        var settingsWindow=System.Runtime.Serialization.FormatterServices.GetUninitializedObject(settingsType);
        var settingView=Activator.CreateInstance(candidate.GetType("Phinix.LegacyTalentTradeExtension.Client.TalentPresentationView",true),Private,null,new object[]{bridge,"talent-settings"},null);
        var saved=new Settings();Set(settingsWindow,"settings",saved);Set(settingsWindow,"host",bridge);Set(settingsWindow,"view",settingView);
        Call(settingsWindow,"UpdateView");Program.Check(services.View.Page.Sections.Count==1&&services.View.Page.Sections[0].Actions.Count==2,"Settings use the same public action page");
        services.View.Set.Actions.Single(a=>a.Id=="notifications").TryExecute();Program.Check(saved.Values.ContainsKey("legacyTalentTrade.enableNotifications"),"Notification toggle writes the original setting key");
        Call(settingsWindow,"UpdateView");services.View.Set.Actions.Single(a=>a.Id=="debug").TryExecute();Program.Check(saved.Values.ContainsKey("legacyTalentTrade.enableDebugLog"),"Debug toggle writes the original setting key");
        ((IDisposable)settingView).Dispose();
        services.FailOpen=true;bool failed=false;
        try{Activator.CreateInstance(candidate.GetType("Phinix.LegacyTalentTradeExtension.Client.TalentPresentationView",true),Private,null,new object[]{bridge,"failed-open"},null);}catch(TargetInvocationException){failed=true;}
        services.FailOpen=false;Program.Check(failed&&!((IDictionary)Get(bridge,"Sources")).Contains("failed-open"),"Failed session open releases its owner source registration");
        var tab=new Phinix.LegacyTalentTradeExtension.Client.TalentTradeTab();
        tab.ViewOpened();tab.ViewUpdate();
        services.View.Set.Actions.Single(a=>a.Id=="nav-0").TryExecute();
        tab.ViewUpdate();
        Program.Check(services.View.PageId=="talent-direct-list"&&services.View.Page.Sections.Count==3,"First direct branch with no selected trade renders an empty list and navigation");
        services.View.Set.Actions.Single(a=>a.Id=="nav-1").TryExecute();tab.ViewUpdate();
        Program.Check(services.View.PageId=="talent-market","Direct branch can return to market through its public navigation");
        Program.Check(Phinix.LegacyTalentTradeExtension.Client.TalentTradeManager.GetTrade(null)==null&&
            Phinix.LegacyTalentTradeExtension.Client.TalentTradeManager.GetTrade("")==null&&
            Phinix.LegacyTalentTradeExtension.Client.TalentTradeManager.GetTrade("missing")==null,"Absent or unknown selection returns no trade without changing business facts");
        for(int i=0;i<3;i++)
        { services.View.Set.Actions.Single(a=>a.Id=="nav-"+i).TryExecute();tab.ViewUpdate();
          Program.Check(services.View.Page.Sections[0].Actions.Count==3,"Actual branch navigation remains reachable: "+i); }
        var stale=services.View.Set.Actions.Single(a=>a.Id=="nav-0");
        services.FailOpenId="talent-direct-list";
        stale.TryExecute();tab.ViewUpdate();
        Program.Check(services.View.PageId=="talent-branch-error"&&services.View.Page.Sections[0].Actions.Count==3,"Branch open failure renders recovery and navigation through public Host");
        var faultPage=services.View.Page;int attempts=services.OpenAttempts;
        tab.ViewUpdate();tab.ViewUpdate();
        Program.Check(ReferenceEquals(faultPage,services.View.Page)&&attempts==services.OpenAttempts,"Failed branch does not automatically retry or repeatedly replace its page");
        services.View.Set.Actions.Single(a=>a.Id=="nav-1").TryExecute();tab.ViewUpdate();
        Program.Check(services.View.PageId=="talent-market"&& !((IDictionary)Get(bridge,"Sources")).Contains("talent-branch-error"),"Failed branch can return to market and releases recovery source");
        services.View.Set.Actions.Single(a=>a.Id=="nav-0").TryExecute();tab.ViewUpdate();
        services.FailOpenId=null;
        services.View.Set.Actions.Single(a=>a.Id=="retry-view").TryExecute();tab.ViewUpdate();
        Program.Check(services.View.PageId=="talent-direct-list"&&!((IDictionary)Get(bridge,"Sources")).Contains("talent-branch-error"),"Explicit retry reopens only the branch view and releases failure session");
        stale.TryExecute();Program.Check(services.View.PageId=="talent-direct-list","Disposed prior navigation remains rejected after recovery");
        services.View.Set.Actions.Single(a=>a.Id=="nav-1").TryExecute();tab.ViewUpdate();
        int businessVersion=Phinix.LegacyTalentTradeExtension.Client.TalentTradeManager.StateVersion;
        services.View.Set.Actions.Single(a=>a.Id=="nav-0").TryExecute();
        services.FailPublishId="talent-direct-list";tab.ViewUpdate();
        Program.Check(services.View.PageId=="talent-branch-error","Branch projection publication failure also retains recovery navigation");
        services.FailPublishId=null;
        services.View.Set.Actions.Single(a=>a.Id=="nav-2").TryExecute();tab.ViewUpdate();
        Program.Check(services.View.PageId=="talent-rental","Update failure can return to another branch");
        Program.Check(Phinix.LegacyTalentTradeExtension.Client.TalentTradeManager.StateVersion==businessVersion,"View failure, navigation and retry do not mutate business state");
        tab.ViewClosed();Program.Check(services.View.IsClosed,"Actual tab close releases its branch session");
        Set(bridge,"Navigation",null);presentation.SetValue(null,null);
    }
    private static void CheckWindowFacts(Assembly candidate)
    {
        var window=candidate.GetType("Phinix.LegacyTalentTradeExtension.Client.DirectTradeWindow",true);
        var editable=window.GetMethod("Editable",BindingFlags.Static|BindingFlags.NonPublic);
        foreach(var state in new[]{"Pending","Accepted","Negotiating","Confirmed"})
            Program.Check((bool)editable.Invoke(null,new[]{Enum.Parse(candidate.GetType("Phinix.LegacyTalentTradeExtension.Client.DirectTradeState"),state)}),"Original editable state retained: "+state);
        foreach(var state in new[]{"Executing","Completed","Cancelled"})
            Program.Check(!(bool)editable.Invoke(null,new[]{Enum.Parse(candidate.GetType("Phinix.LegacyTalentTradeExtension.Client.DirectTradeState"),state)}),"Terminal/executing is read only: "+state);
        var compatibility=candidate.GetType("Phinix.LegacyTalentTradeExtension.Client.TransferCompatibilityUi",true).GetMethod("BuildConfirmation",BindingFlags.Static|BindingFlags.NonPublic);
        object[] args={"Confirm","Their offer",null,null};
        Program.Check((bool)compatibility.Invoke(null,args)&&((string)args[3]).Contains("compatibilityUnknown"),"Absent metadata retains explicit unknown warning");
        args=new object[]{"Confirm","Their offer",new[]{"malformed"},null};
        Program.Check((bool)compatibility.Invoke(null,args)&&((string)args[3]).Contains("compatibilityUnknown"),"Unreadable metadata does not pretend compatibility success");
        var manifest=new Phinix.LegacyTalentTradeExtension.Client.DefManifest();manifest.RaceDefs.Add("phinix.runtime.test.unknown-race");
        args=new object[]{"Confirm","Their offer",new[]{Phinix.LegacyTalentTradeExtension.Client.DefManifestHelper.SerializeCompressed(manifest)},null};
        Program.Check(!(bool)compatibility.Invoke(null,args)&&((string)args[3]).Contains("compatibilityFatal"),"Missing race blocks confirmation before any Pawn deserialization");
    }
    private static void CheckPawnRecovery(Assembly candidate)
    {
        var icons=candidate.GetType("Phinix.LegacyTalentTradeExtension.Client.TalentPawnIcons",true);
        var rule=icons.GetMethod("UsesPortrait",BindingFlags.Static|BindingFlags.NonPublic);
        var human=(Verse.ThingDef)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(Verse.ThingDef));
        human.race=new Verse.RaceProperties { intelligence=Verse.Intelligence.Humanlike };
        var animal=(Verse.ThingDef)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(Verse.ThingDef));
        animal.race=new Verse.RaceProperties { intelligence=Verse.Intelligence.Animal };
        Program.Check((bool)rule.Invoke(null,new[]{human})&&!(bool)rule.Invoke(null,new[]{animal}),"Humanlike uses portraits; animals retain the original Def icon path");
        var scopeType=candidate.GetType("Phinix.LegacyTalentTradeExtension.Client.PawnScribeLoadScope",true);
        var previous=Verse.Current.ProgramState;
        Verse.Current.ProgramState=Verse.ProgramState.Playing;
        Verse.Scribe.mode=Verse.LoadSaveMode.Inactive;
        try
        {
            var scope=(IDisposable)Activator.CreateInstance(scopeType,Private,null,Array.Empty<object>(),null);
            Program.Check(Verse.Current.ProgramState==Verse.ProgramState.MapInitializing,"Standalone Pawn load runs in initialization context, not live birthday context");
            scope.Dispose();scope.Dispose();
            Program.Check(Verse.Current.ProgramState==Verse.ProgramState.Playing&&Verse.Scribe.mode==Verse.LoadSaveMode.Inactive,"Load scope restores live context and cleans Scribe idempotently");
            Verse.Scribe.mode=Verse.LoadSaveMode.LoadingVars;bool rejected=false;
            try{Activator.CreateInstance(scopeType,Private,null,Array.Empty<object>(),null);}catch(TargetInvocationException ex){rejected=ex.InnerException is InvalidOperationException;}
            Program.Check(rejected&&Verse.Current.ProgramState==Verse.ProgramState.Playing&&Verse.Scribe.mode==Verse.LoadSaveMode.LoadingVars,"Busy outer Scribe load is rejected without changing its state");
        }
        finally { Verse.Scribe.mode=Verse.LoadSaveMode.Inactive;Verse.Current.ProgramState=previous; }
        var ledgerType=candidate.GetType("Phinix.LegacyTalentTradeExtension.Client.PendingPawnReturnLedger",true);
        object ledger=Activator.CreateInstance(ledgerType,Private,null,new object[]{new List<string>(),new List<string>(),Array.Empty<string>()},null);
        Program.Check((bool)Call(ledger,"TryAdd","delist","original-payload")&&(bool)Call(ledger,"TryAdd","delist","original-payload")&&!(bool)Call(ledger,"TryAdd","delist","different-payload"),"Delist ledger preserves exact original data, deduplicates and rejects conflicting payload");
        var records=(Array)ledgerType.GetProperty("Records",Private).GetValue(ledger);object record=records.GetValue(0);
        Program.Check((bool)Call(ledger,"TryQueue",record)&&!(bool)Call(ledger,"TryQueue",record),"Delist record admits only one queued delivery");
        var outcome=candidate.GetType("Phinix.LegacyTalentTradeExtension.Client.PawnReturnOutcome",true);
        Call(ledger,"Finish",record,Enum.Parse(outcome,"Deferred"));
        Program.Check((string)Get(record,"Data")=="original-payload"&&(bool)Call(ledger,"TryQueue",record),"Load failure retains raw data in existing ledger and allows later explicit recovery");
        Call(ledger,"BeginHandoff",record);Call(ledger,"Finish",record,Enum.Parse(outcome,"Uncertain"));
        Program.Check(!(bool)Call(ledger,"TryQueue",record)&&(string)Get(record,"Data")=="original-payload","Ambiguous handoff is retained without automatic redelivery");
    }
    private sealed class Settings : IClientSettingsContext
    {
        public readonly Dictionary<string,object> Values=new Dictionary<string,object>();
        public T Get<T>(string key,T fallback=default(T))=>Values.TryGetValue(key,out object value)?(T)value:fallback;
        public void Set<T>(string key,T value){Values[key]=value;}
        public IEnumerable<string> BlockedUsers=>Array.Empty<string>();public bool CollapseBlockedUsers {get;set;}
        public void BlockUser(string uuid){}public void UnBlockUser(string uuid){}public event Action<string,object> OnSettingChanged {add{}remove{}}
    }
    private sealed class Token : IDisposable
    {private readonly bool fail;public int Calls;public Token(bool fail){this.fail=fail;}public void Dispose(){Calls++;if(fail)throw new InvalidOperationException("release");}}
    private sealed class Module : IPhinixExtensionModule
    {public string ExtensionId=>"builtin.legacy-talent-trade";public void Register(IExtensionBuilder b){} }
    private sealed class Provider : IMainTabProvider,IResponsiveMainTabProvider
    {public string TabLabel=>"Packets";public float TabOrder=>1100;public UiLayoutHints LayoutHints {get;}=new UiLayoutHints(new Vector2(320,300),new Vector2(900,600),true);public void Draw(Rect rect){} }
    private sealed class Services : IClientPresentationService,IClientExtensionPointService,IClientTheme
    {
        public bool FailOpen;public string FailOpenId,FailPublishId;public int OpenAttempts;public int Definitions,ScopeDisposals,SetsDisposed;public string PointId;public View View;
        private Delegate factory;
        public long Revision=>3;public long Version=>7;public event Action Changed {add{}remove{}}
        public IClientPresentationSession Open(IPhinixExtensionModule owner,string id){OpenAttempts++;if(FailOpen||id==FailOpenId)throw new InvalidOperationException("failed open");return View=new View(this,id);}
        public IClientExtensionPointScope ForModule(IPhinixExtensionModule module)=>new Scope(this);
        public IReadOnlyList<ClientExtensionPointInfo> CapturePoints()=>Array.Empty<ClientExtensionPointInfo>();
        private sealed class Scope : IClientExtensionPointScope
        {private readonly Services s;private bool disposed;public Scope(Services s){this.s=s;}public IClientActionPoint<T> DefineActions<T>(string id)where T:class{s.PointId=id;s.Definitions++;return new Point<T>(s);}public void ContributeActions<T>(string id,Func<T,IEnumerable<ClientActionDefinition>> f,float order=0)where T:class{s.factory=f;}public void Dispose(){if(!disposed){disposed=true;s.ScopeDisposals++;}}}
        private sealed class Point<T> : IClientActionPoint<T>where T:class
        {private readonly Services s;public Point(Services s){this.s=s;}public IClientActionSet Collect(T context,Func<bool> current=null)=>new Set(s,((Func<T,IEnumerable<ClientActionDefinition>>)s.factory)(context).Select(d=>(IClientContributedAction)new ActionBinding(d,current)).ToArray());}
        private sealed class Set : IClientActionSet
        {private readonly Services s;private bool disposed;public Set(Services s,IClientContributedAction[] actions){this.s=s;Actions=actions;}public IReadOnlyList<IClientContributedAction> Actions {get;}public void Dispose(){if(!disposed){disposed=true;s.SetsDisposed++;}}}
        private sealed class ActionBinding : IClientContributedAction
        {private readonly ClientActionDefinition d;private readonly Func<bool> current;public ActionBinding(ClientActionDefinition d,Func<bool> current){this.d=d;this.current=current;}public string OwnerId=>"builtin.legacy-talent-trade";public string Id=>d.Id;public string Label=>d.Label;public string DisabledReason=>d.DisabledReason;public ClientActionExecutionResult TryExecute(){if(!current()||DisabledReason!=null)return new ClientActionExecutionResult(false,false,"stale");d.Execute();return new ClientActionExecutionResult(true,true,null);}}
        public Color PrimaryText=>Color.white;public Color SecondaryText=>Color.white;public Color Background=>Color.clear;public Color Surface=>Color.clear;public Color Separator=>Color.clear;public Color HoverHighlight=>Color.clear;public Color Pending=>Color.white;public Color Error=>Color.white;public Color Success=>Color.white;public Color Warning=>Color.white;
        public Color GetColor(string key)=>Color.white;public bool TryGetColor(string key,out Color c){c=Color.white;return true;}public float GetFloat(string key,float fallback=0)=>fallback;
        public ClientThemeSnapshot Capture()=>new ClientThemeSnapshot(Version,new Dictionary<string,Color>(),new Dictionary<string,float>());
    }
    private sealed class View : IClientPresentationSession
    {
        private readonly Services s;internal IClientActionSet Set;public ClientPresentationPage Page;public readonly Queue<ClientPresentationInput> Inputs=new Queue<ClientPresentationInput>();
        public View(Services s,string id){this.s=s;PageId=id;}public string OwnerId=>"builtin.legacy-talent-trade";public string PageId {get;}public bool IsClosed {get;private set;}public string FailureCode=>null;public ClientActionExecutionResult LastActionResult=>null;
        public void Publish(ClientPresentationPage page,IClientActionSet owned,Func<bool> current){if(PageId==s.FailPublishId)throw new InvalidOperationException("failed publish");Set?.Dispose();Set=owned;Page=page;}
        public void Draw(Rect rect){}public float MeasureComposerHeight(float width)=>0;
        public bool TryDequeueInput(out ClientPresentationInput input){input=Inputs.Count==0?null:Inputs.Dequeue();return input!=null;}
        public string ComposerDraft=>"";public string ComposerControlName=>"";public bool ComposerFocused=>false;public void SetComposerDraft(string value){}public void RequestComposerFocus(){}public void InvokeAction(string id){}public List<Verse.FloatMenuOption> CreateActionMenuOptions()=>new List<Verse.FloatMenuOption>();public void ScrollToRow(string feed,string row){}public void ScrollToEnd(){}
        public void Dispose(){if(IsClosed)return;IsClosed=true;Set?.Dispose();Set=null;}
    }
}
