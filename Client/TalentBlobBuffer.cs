using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Phinix.LegacyTalentTradeExtension.Client
{
    // Incomplete wire fragments own no pawns or purchase outcomes. The caller serializes access.
    internal sealed class TalentBlobBuffer
    {
        private sealed class Pending
        {
            internal string[] Parts;
            internal long CreatedAt;
        }
        private readonly Dictionary<string,Pending> pending=new Dictionary<string,Pending>(StringComparer.Ordinal);
        private readonly Func<long> clock;
        internal TalentBlobBuffer(Func<long> clock=null)
        {this.clock=clock??(()=>(long)(Stopwatch.GetTimestamp()*(1000.0/Stopwatch.Frequency)));}
        internal void Clear()=>pending.Clear();
        internal bool TryAdd(string id,int index,int count,string data,out string complete)
        {
            complete=null;
            long now=clock();
            foreach(string stale in pending.Where(p=>now-p.Value.CreatedAt>=5*60*1000).Select(p=>p.Key).ToArray())pending.Remove(stale);
            if(string.IsNullOrEmpty(id)||id.Length>TalentTradeInputLimits.MaxIdCharacters||count<1||count>TalentTradeInputLimits.MaxBlobParts||
                index<0||index>=count||string.IsNullOrEmpty(data)||data.Length>TalentTradeInputLimits.MaxProtocolCharacters)return false;
            bool exists=pending.TryGetValue(id,out var blob);
            if(!exists && pending.Count>=TalentTradeInputLimits.MaxPendingBlobs)return false;
            if(exists && (blob.Parts.Length!=count || blob.Parts[index]!=null && blob.Parts[index]!=data))return false;
            long size=exists?blob.Parts.Sum(p=>(long)(p?.Length??0)):0;
            long retained=pending.Values.Sum(b=>b.Parts.Sum(p=>(long)(p?.Length??0)));
            int added=exists&&blob.Parts[index]!=null?0:data.Length;
            if(size+added>TalentTradeInputLimits.MaxProtocolCharacters||retained+added>TalentTradeInputLimits.MaxQueuedCharacters)return false;
            // Refused input must not create an empty entry that consumes a slot.
            if(!exists){blob=new Pending{Parts=new string[count],CreatedAt=now};pending.Add(id,blob);}
            blob.Parts[index]=data;
            if(blob.Parts.All(p=>p!=null)){complete=string.Concat(blob.Parts);pending.Remove(id);}
            return true;
        }
    }
}
