using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Phinix.LegacyTalentTradeExtension.Client
{
    /// <summary>
    /// Per-save persistence for market listings.
    /// On save: backup listing pawn data (no side effects).
    /// On load: delist old listings, return pawns via drop pod.
    /// </summary>
    public class TalentTradeGameComponent : GameComponent
    {
        // Persisted: listing IDs + pawn data from last save
        private List<string> pendingReturnIds = new List<string>();
        private List<string> pendingReturnData = new List<string>();

        // Persisted: unique identity for this save lineage
        private string saveToken;

        // Persisted: last mod version the player has seen the notification for
        private string lastSeenVersion;

        private List<string> pendingReturnUncertainIds = new List<string>();
        private PendingPawnReturnLedger returnLedger;
        private readonly Game owningGame;

        // Runtime only: track which listings belong to THIS save session
        private HashSet<string> activeListingIds = new HashSet<string>();
        private readonly Dictionary<string, string> activeListingBackups = new Dictionary<string, string>();
        private readonly HashSet<string> activeBackupUncertainIds = new HashSet<string>();

        private static TalentTradeGameComponent current;
        public static TalentTradeGameComponent Current { get { return current; } }
        public string SaveToken { get { return saveToken; } }

        public TalentTradeGameComponent(Game game) : base()
        {
            owningGame = game;
            current = this;
        }

        public override void FinalizeInit()
        {
            // 插件禁用（StaticActivationPolicy 跳过）时，GameComponent 仍会被 RimWorld
            // 自动实例化（GenTypes.AllSubclassesNonAbstract）。此处必须守卫，禁用即不做事。
            if (!LegacyTalentTradeRuntime.IsActive) return;

            current = this;
            // Generate token for new games or saves that predate this feature
            if (string.IsNullOrEmpty(saveToken))
                saveToken = Guid.NewGuid().ToString("N");
            // Detect save-switch and clean up orphaned listings
            TalentTradeManager.OnSaveSessionStart(saveToken);
            // Return any pawns that were listed in a previous session
            ReturnPendingPawns();
            // Show version letter on first load or after mod update
            VersionNotifier.TryNotify(lastSeenVersion);
            lastSeenVersion = VersionNotifier.ModVersion;
        }

        public override void ExposeData()
        {
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                // Just backup — no delist, no clearing
                BackupActiveListings();
            }

            Scribe_Collections.Look(ref pendingReturnIds, "pendingReturnIds", LookMode.Value);
            Scribe_Collections.Look(ref pendingReturnData, "pendingReturnData", LookMode.Value);
            Scribe_Collections.Look(ref pendingReturnUncertainIds, "pendingReturnUncertainIds", LookMode.Value);
            Scribe_Values.Look(ref saveToken, "saveToken", null);
            Scribe_Values.Look(ref lastSeenVersion, "lastSeenVersion", null);

            if (pendingReturnIds == null) pendingReturnIds = new List<string>();
            if (pendingReturnData == null) pendingReturnData = new List<string>();
            if (pendingReturnUncertainIds == null) pendingReturnUncertainIds = new List<string>();
        }

        public void TrackListing(string listingId)
        {
            activeListingIds.Add(listingId);
        }

        public void UntrackListing(string listingId)
        {
            activeListingIds.Remove(listingId);
            activeListingBackups.Remove(listingId);
            activeBackupUncertainIds.Remove(listingId);
        }

        public bool OwnsListing(string listingId)
        {
            return activeListingIds.Contains(listingId);
        }

        public HashSet<string> GetActiveListingIdsSnapshot()
        {
            return new HashSet<string>(activeListingIds);
        }

        /// <summary>
        /// Save only: backup active listing data into the save file.
        /// Does NOT delist or clear anything — game continues normally after save.
        /// </summary>
        private void BackupActiveListings()
        {
            // Disabled modules still have a RimWorld GameComponent. Preserve its stored payload.
            if (!LegacyTalentTradeRuntime.IsActive) return;
            EnsureReturnLedger();
            var active = new List<KeyValuePair<string, string>>();
            foreach (string listingId in activeListingIds)
            {
                string data = TalentTradeManager.GetLocalPawnData(listingId);
                if (!string.IsNullOrEmpty(data)) activeListingBackups[listingId] = data;
                else if (activeListingBackups.TryGetValue(listingId, out data))
                {
                    // Cache absence does not prove ownership. It may follow disconnect OR delivery.
                    activeBackupUncertainIds.Add(listingId);
                    ReturnAudit("active-backup-uncertain", listingId);
                }
                else ReturnAudit("active-payload-unavailable", listingId);
                // Cache loss/temporary disconnect must not erase a previously captured active payload.
                active.Add(new KeyValuePair<string, string>(listingId, data));
            }
            returnLedger.Snapshot(active, out pendingReturnIds, out pendingReturnData, out pendingReturnUncertainIds);
            foreach (string listingId in activeBackupUncertainIds)
                if (activeListingIds.Contains(listingId) && !pendingReturnUncertainIds.Contains(listingId))
                    pendingReturnUncertainIds.Add(listingId);
        }

        private void EnsureReturnLedger()
        {
            if (returnLedger == null)
                returnLedger = new PendingPawnReturnLedger(pendingReturnIds, pendingReturnData, pendingReturnUncertainIds);
        }

        private void ReturnAudit(string code, string listingId)
        {
            // Do not log serialized pawn data. These IDs identify the save and recovery record.
            string message = "[TalentTrade] " + PendingPawnReturnAudit.Json(code, saveToken, listingId);
            if (code == "queued" || code == "Returned" || code == "stale-session-skipped")
                LegacyTalentTradeRuntime.LogMessage(message);
            else LegacyTalentTradeRuntime.LogWarning(message);
        }

        /// <summary>Records are removed only after local world ownership transfer succeeds.</summary>
        private void ReturnPendingPawns()
        {
            EnsureReturnLedger();
            foreach (var record in returnLedger.Records)
            {
                if (!returnLedger.TryQueue(record))
                {
                    ReturnAudit(record.Uncertain ? "uncertain-retained" : "invalid-or-duplicate-retained", record.Id);
                    continue;
                }
                bool accepted = TalentTradeManager.TryEnqueueMainThread(() =>
                {
                    // A same-token reload is still a different Game/component instance.
                    if (!LegacyTalentTradeRuntime.IsActive || current != this || Verse.Current.Game != owningGame)
                    {
                        returnLedger.Finish(record, PawnReturnOutcome.Deferred);
                        ReturnAudit("stale-session-skipped", record.Id);
                        return;
                    }
                    Pawn pawn = null;
                    PawnReturnOutcome outcome = PawnReturnOutcome.Deferred;
                    try
                    {
                        string localUuid = TalentTradeManager.GetLocalUuid();
                        if (!string.IsNullOrEmpty(localUuid))
                            TalentTradeManager.SendProtocol(TalentTradeProtocol.BuildMarketDelist(record.Id, localUuid));
                        TalentTradeManager.RemoveListingLocally(record.Id);
                        outcome = PawnDeserializer.RestorePendingPawn(record.Data, () =>
                        {
                            returnLedger.BeginHandoff(record);
                            BackupActiveListings();
                        }, out pawn);
                    }
                    catch (Exception ex)
                    {
                        outcome = record.Uncertain ? PawnReturnOutcome.Uncertain : PawnReturnOutcome.Deferred;
                        LegacyTalentTradeRuntime.LogError("[TalentTrade] Pending return failed: " + ex);
                    }
                    finally
                    {
                        returnLedger.Finish(record, outcome);
                        BackupActiveListings();
                        ReturnAudit(outcome.ToString(), record.Id);
                    }
                    if (outcome == PawnReturnOutcome.Returned && pawn != null)
                        Messages.Message("Phinix_legacyTalentTrade_pawnRestored".Localize(pawn.LabelShortCap),
                            new LookTargets(pawn), MessageTypeDefOf.NeutralEvent, false);
                });
                if (!accepted)
                {
                    returnLedger.Finish(record, PawnReturnOutcome.Deferred);
                    ReturnAudit("queue-full-retained", record.Id);
                }
                else ReturnAudit("queued", record.Id);
            }
        }
    }
}
