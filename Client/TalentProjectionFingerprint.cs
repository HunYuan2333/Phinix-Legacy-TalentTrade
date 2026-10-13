using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
namespace Phinix.LegacyTalentTradeExtension.Client
{
    // View facts only. Heartbeat timestamps and unrelated message versions are not page changes.
    internal static class TalentProjectionFingerprint
    {
        private sealed class Facts : IDisposable
        {
            private readonly SHA256 sha=SHA256.Create();private readonly byte[] bytes=new byte[4096];private readonly char[] chunk=new char[1024];private readonly Encoder encoder=Encoding.Unicode.GetEncoder();
            internal void Add(object value)
            {
                string text=value==null?"":Convert.ToString(value,System.Globalization.CultureInfo.InvariantCulture);
                byte[] size=BitConverter.GetBytes(text.Length);sha.TransformBlock(size,0,size.Length,null,0);
                int offset=0;
                do {int count=Math.Min(chunk.Length,text.Length-offset);text.CopyTo(offset,chunk,0,count);offset+=count;int used=encoder.GetBytes(chunk,0,count,bytes,0,offset==text.Length);sha.TransformBlock(bytes,0,used,null,0);}while(offset<text.Length);
            }
            internal string Finish(){sha.TransformFinalBlock(Array.Empty<byte>(),0,0);return BitConverter.ToString(sha.Hash);}
            public void Dispose(){sha.Dispose();}
        }
        private static void Summary(Facts f,PawnSummary s)
        {f.Add(s!=null);if(s==null)return;f.Add(s.Name);f.Add(s.Gender);f.Add(s.BiologicalAge);f.Add(s.RaceDefName);f.Add(s.PawnKind);f.Add(s.SkillsSummary);f.Add(s.TraitsSummary);f.Add(s.HealthSummary);}
        internal static string Market(IEnumerable<MarketListing> items)
        {using(var f=new Facts()){foreach(var x in items){if(x==null)continue;f.Add(x.Id);f.Add(x.SellerUuid);f.Add(x.SellerName);f.Add(x.PriceSilver);f.Add(x.State);Summary(f,x.Summary);f.Add(x.DefManifestData);}return f.Finish();}}
        internal static string Rental(IEnumerable<RentalContract> items)
        {using(var f=new Facts()){foreach(var x in items){if(x==null)continue;f.Add(x.Id);f.Add(x.OwnerUuid);f.Add(x.RenterUuid);f.Add(x.OwnerName);f.Add(x.RenterName);f.Add(x.PricePerDay);f.Add(x.Deposit);f.Add(x.MaxDays);f.Add(x.RentedDays);f.Add(x.ExpiryTick);f.Add(x.State);Summary(f,x.Summary);f.Add(x.DefManifestData);}return f.Finish();}}
        internal static string Direct(IEnumerable<DirectTrade> items,bool includeOffers=false)
        {using(var f=new Facts()){foreach(var x in items){if(x==null)continue;f.Add(x.Id);f.Add(x.InitiatorUuid);f.Add(x.TargetUuid);f.Add(x.InitiatorName);f.Add(x.TargetName);f.Add(x.State);f.Add(x.InitiatorConfirmed);f.Add(x.TargetConfirmed);if(includeOffers){Offer(f,x.InitiatorOffer);Offer(f,x.TargetOffer);}}return f.Finish();}}
        private static void Offer(Facts f,TradeOffer offer)
        {f.Add(offer!=null);if(offer==null)return;f.Add(offer.SilverAmount);f.Add(offer.Pawns.Count);foreach(var s in offer.Pawns)Summary(f,s);f.Add(offer.PawnData.Count);foreach(var data in offer.PawnData)f.Add(data);f.Add(offer.PawnManifestData.Count);foreach(var data in offer.PawnManifestData)f.Add(data);foreach(var item in offer.ItemSummaries)f.Add(item);}
    }
}
