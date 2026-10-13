using System.Collections.Generic;
using System.Collections.ObjectModel;
namespace Phinix.LegacyTalentTradeExtension
{
    // Immutable projection facts only; no Pawn, mutable offer, ledger or execution capability.
    public sealed class TalentPresentationActionContext
    {
        public const string PointId = "builtin.legacy-talent-trade.presentation.actions";
        public TalentPresentationActionContext(string viewId, long revision, string entityId, string state, string selectionId, string silverText, bool online)
        { ViewId=viewId; Revision=revision; EntityId=entityId; State=state; SelectionId=selectionId; SilverText=silverText; Online=online; }
        public IReadOnlyDictionary<string,string> DraftValues { get; private set; } = new ReadOnlyDictionary<string,string>(new Dictionary<string,string>());
        public TalentPresentationActionContext WithDraftValues(IDictionary<string,string> values)
        {var copy=new TalentPresentationActionContext(ViewId,Revision,EntityId,State,SelectionId,SilverText,Online);copy.DraftValues=new ReadOnlyDictionary<string,string>(values==null?new Dictionary<string,string>():new Dictionary<string,string>(values));return copy;}
        public string ViewId { get; } public long Revision { get; } public string EntityId { get; }
        public string State { get; } public string SelectionId { get; } public string SilverText { get; } public bool Online { get; }
    }
}
