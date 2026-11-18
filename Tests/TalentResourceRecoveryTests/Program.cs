using System;
using Phinix.LegacyTalentTradeExtension.Client;

internal static class Program
{
    private static int checks;
    private static void Check(bool ok,string reason){if(!ok)throw new Exception(reason);checks++;}
    private static void Main()
    {
        long now=0;var buffer=new TalentBlobBuffer(()=>now);string result;
        for(int i=0;i<TalentTradeInputLimits.MaxPendingBlobs;i++)
            Check(buffer.TryAdd("partial-"+i,0,2,"a",out result)&&result==null,"Partial fragment admitted");
        Check(!buffer.TryAdd("overflow",0,2,"b",out result),"Live fragments are not evicted at capacity");
        now=299999;
        Check(buffer.TryAdd("partial-0",0,2,"a",out result),"Identical duplicate remains valid");
        now=300000;
        Check(buffer.TryAdd("fresh",0,2,"new",out result),"Abandoned fragments release all slots after timeout");
        Check(buffer.TryAdd("fresh",1,2,"value",out result)&&result=="newvalue","Fresh fragments assemble in order");
        Check(buffer.TryAdd("conflict",0,2,"first",out result),"Conflict fixture starts");
        Check(!buffer.TryAdd("conflict",0,2,"other",out result),"Conflicting duplicate cannot replace accepted data");
        Check(buffer.TryAdd("conflict",1,2,"last",out result)&&result=="firstlast","Rejected fragment leaves valid assembly intact");
        buffer.Clear();
        var full=new string('x',TalentTradeInputLimits.MaxProtocolCharacters);
        Check(buffer.TryAdd("large-a",0,2,full,out result)&&buffer.TryAdd("large-b",0,2,full,out result),"Global byte budget fixture");
        for(int i=0;i<40;i++)Check(!buffer.TryAdd("refused-"+i,0,2,"x",out result),"Byte refusals do not admit data");
        Check(buffer.TryAdd("large-a",1,2,"",out result)==false,"Empty fragment cannot bypass validation");
        now+=300000;
        Check(buffer.TryAdd("after-budget",0,1,"ok",out result)&&result=="ok","Expired byte budget and refused slots release together");
        Console.WriteLine("TALENT RESOURCE PASS "+checks+" checks; production fragment buffer, no pawn state");
    }
}
