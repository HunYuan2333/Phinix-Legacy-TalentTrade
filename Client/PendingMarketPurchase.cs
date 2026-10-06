using System;

namespace Phinix.LegacyTalentTradeExtension.Client
{
    // This record correlates local intent; it is not remote sender authentication.
    // Payload retention here is process-local, not a durable transaction journal.
    internal sealed class PendingMarketPurchase
    {
        internal readonly string SellerUuid, BuyerUuid, SaveToken;
        internal readonly object Game;
        internal readonly int StartedTick;
        internal string Payload;
        internal bool Queued, Uncertain, TimedOut;

        internal PendingMarketPurchase(string seller, string buyer, string token, object game, int tick)
        { SellerUuid = seller; BuyerUuid = buyer; SaveToken = token; Game = game; StartedTick = tick; }

        internal bool Matches(string seller, string buyer, string token, object game)
        {
            return Game != null && ReferenceEquals(Game, game) && !string.IsNullOrEmpty(SaveToken) &&
                SaveToken == token && !string.IsNullOrEmpty(BuyerUuid) && BuyerUuid == buyer &&
                !string.IsNullOrEmpty(SellerUuid) && SellerUuid == seller;
        }

        internal bool TryReceive(string data)
        {
            if (Queued || Uncertain || string.IsNullOrEmpty(data) ||
                data.Length > 4L * ((TalentTradeInputLimits.MaxCompressedBytes + 2L) / 3L) ||
                (Payload != null && Payload != data)) return false;
            Payload = data;
            Queued = true;
            return true;
        }

        internal void BeginHandoff() { Uncertain = true; }
        internal void Finish(PawnReturnOutcome outcome)
        {
            Queued = false;
            Uncertain = outcome != PawnReturnOutcome.Deferred;
        }
    }
}
