using UnityEngine;
using Verse;
namespace Phinix.LegacyTalentTradeExtension.Client
{
    public partial class MarketPanel
    {
        internal readonly TalentCatalogDraft PresentationDraft=new TalentCatalogDraft();
        private TalentCatalogPresentation modern;
        internal void ViewOpened(){if(modern==null&&BuiltInLegacyTalentTradeClientExtension.Presentation!=null)modern=new TalentCatalogPresentation(this,null);}
        internal void ViewUpdate(){modern?.Update();}
        internal void DrawPresentation(Rect rect){modern?.Draw(rect);}
        internal void ViewClosed(){var old=modern;modern=null;old?.Dispose();}
        internal void SubmitModern(Pawn pawn,int price){selectedPawn=pawn;priceValue=price;try{DoListForSale();}finally{selectedPawn=null;}}
        internal void BuyModern(MarketListing listing){DoBuy(listing);}
        internal void DelistModern(MarketListing listing){DelistListing(listing);}
        internal void CleanupModern(){ForceCleanupAllMyListings();}
        internal static bool IsBlockedPawn(Pawn pawn)=>IsAriandelOCPawn(pawn);
    }
    public partial class RentalPanel
    {
        internal readonly TalentCatalogDraft PresentationDraft=new TalentCatalogDraft();
        private TalentCatalogPresentation modern;
        internal void ViewOpened(){if(modern==null&&BuiltInLegacyTalentTradeClientExtension.Presentation!=null)modern=new TalentCatalogPresentation(null,this);}
        internal void ViewUpdate(){modern?.Update();}
        internal void DrawPresentation(Rect rect){modern?.Draw(rect);}
        internal void ViewClosed(){var old=modern;modern=null;old?.Dispose();}
        internal void SubmitModern(Pawn pawn,int price,int days,int deposit){selectedPawn=pawn;pricePerDayValue=price;maxDaysValue=days;depositValue=deposit;try{DoListForRent();}finally{selectedPawn=null;}}
        internal void RentModern(RentalContract contract){DoRent(contract);}
        internal void ReturnModern(RentalContract contract){DoReturn(contract);}
        internal void DelistModern(RentalContract contract){DoDelist(contract);}
    }
}
