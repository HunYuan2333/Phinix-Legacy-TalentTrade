using System;
using System.Collections.Generic;
using Verse;
using UnityEngine;
namespace Phinix.LegacyTalentTradeExtension.Client
{
    // Shared session lifecycle for this plugin. All models/drawing/action collection use public Host APIs.
    internal sealed class TalentPresentationView : IDisposable
    {
        private readonly TalentHostPresentation host; private readonly string id;
        internal Dictionary<string,string> DraftValues;
        internal bool UseNavigation; internal int PrimaryHeaders;
        private object session, result; private long revision, theme=-1, points=-1; private string shownStamp;
        private object language, game, map; private bool dirty=true, disposed;
        private static object CurrentMap => Current.Game==null?null:Find.CurrentMap;
        private IEnumerable<HostAction> definitions=Array.Empty<HostAction>();
        internal TalentPresentationView(TalentHostPresentation host,string id)
        {this.host=host;this.id=id;host.Sources.Add(id,context=>definitions);try{session=host.Open(id);}catch{host.Sources.Remove(id);throw;}}
        internal void Invalidate(){dirty=true;}
        internal bool Input(out HostInput input)=>host.Input(session,out input);
        internal bool IsClosed=>disposed||host.Closed(session);
        internal void Draw(Rect rect){if(!IsClosed)host.Draw(session,rect);}
        internal void Refresh(Func<string> stamp,string entity,string state,string selection,string silver,
            Func<IEnumerable<HostAction>> actions,Func<HostBatch,IEnumerable<object>> sections,int primary=0)
        {
            if(IsClosed)return;
            string next=stamp();object lang=TalentTradeLocalization.LanguageToken,currentGame=Current.Game,currentMap=CurrentMap;
            long tv=host.ThemeVersion,pv=host.PointVersion;object nextResult=host.LastResult(session);
            if(!dirty&&shownStamp==next&&theme==tv&&points==pv&&ReferenceEquals(language,lang)&&ReferenceEquals(game,currentGame)&&ReferenceEquals(map,currentMap)&&ReferenceEquals(result,nextResult))return;
            long rev=++revision;
            Func<bool> valid=()=>!IsClosed&&!dirty&&revision==rev&&stamp()==next&&host.ThemeVersion==tv&&host.PointVersion==pv&&
                ReferenceEquals(TalentTradeLocalization.LanguageToken,lang)&&ReferenceEquals(Current.Game,currentGame)&&ReferenceEquals(CurrentMap,currentMap);
            var official=new List<HostAction>(actions());
            if(UseNavigation&&host.Navigation!=null)official.AddRange(host.Navigation());
            definitions=official;
            var context=new TalentPresentationActionContext(id,rev,entity,state,selection,silver,LegacyTalentTradeRuntime.IsOnline).WithDraftValues(DraftValues);
            var batch=host.Collect(context,valid,definitions);
            try
            {
                dirty=false;var nav=new List<HostBinding>();
                if(UseNavigation) {nav=batch.Bindings.FindAll(b=>b.OwnerId==host.OwnerId&&b.LocalId.StartsWith("nav-",StringComparison.Ordinal));batch.Bindings.RemoveAll(b=>nav.Contains(b));}
                var content=new List<object>(sections(batch));
                if(nav.Count>0)content.Insert(0,host.Section("navigation","","",nav));
                host.Publish(session,id,"",rev,content,batch,valid,primary>0?primary+(nav.Count>0?1:0):0,null,null,0,nav.Count>0?1:0,PrimaryHeaders);
            }
            catch{dirty=true;batch.Dispose();throw;}
            shownStamp=next;theme=tv;points=pv;language=lang;game=currentGame;map=currentMap;result=nextResult;
        }
        public void Dispose(){if(disposed)return;disposed=true;host.Sources.Remove(id);(session as IDisposable)?.Dispose();session=null;game=null;map=null;language=null;definitions=Array.Empty<HostAction>();}
    }
}
