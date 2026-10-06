using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace Phinix.LegacyTalentTradeExtension.Client
{
    internal static class PendingPawnReturnAudit
    {
        [DataContract]
        private sealed class Entry
        {
            [DataMember(Order = 0)] internal int schemaVersion = 1;
            [DataMember(Order = 1)] internal string @event = "talent.pending_return";
            [DataMember(Order = 2)] internal string time;
            [DataMember(Order = 3)] internal string code;
            [DataMember(Order = 4)] internal string saveToken;
            [DataMember(Order = 5)] internal string listingId;
        }

        private static string Bounded(string value)
        { return value == null || value.Length <= 256 ? value : value.Substring(0, 256); }

        internal static string Json(string code, string saveToken, string listingId)
        { return Serialize("talent.pending_return", code, saveToken, listingId); }

        internal static string Purchase(string code, string saveToken, string listingId)
        { return Serialize("talent.purchase", code, saveToken, listingId); }

        private static string Serialize(string eventName, string code, string saveToken, string listingId)
        {
            var entry = new Entry { @event = eventName, time = DateTime.UtcNow.ToString("o"), code = Bounded(code),
                saveToken = Bounded(saveToken), listingId = Bounded(listingId) };
            using (var stream = new MemoryStream())
            {
                new DataContractJsonSerializer(typeof(Entry)).WriteObject(stream, entry);
                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }
    }
}
