using System;
using System.Collections.Generic;
using System.Text;
using RimWorld;
using Verse;

namespace Phinix.LegacyTalentTradeExtension.Client
{
    /// <summary>
    /// Receiver-side compatibility gate. Metadata is checked before any Pawn
    /// XML is handed to Scribe, so missing Defs cannot produce load-time errors.
    /// </summary>
    internal static class TransferCompatibilityUi
    {
        public static void Confirm(
            string actionText,
            string label,
            string manifestData,
            Action onAccepted)
        {
            List<string> manifests = new List<string>();
            manifests.Add(manifestData);
            ConfirmMany(actionText, label, manifests, onAccepted);
        }

        public static void ConfirmMany(
            string actionText,
            string label,
            IEnumerable<string> manifestData,
            Action onAccepted)
        {
            if(!BuildConfirmation(actionText,label,manifestData,out string confirmText))
            {Find.WindowStack.Add(new Dialog_MessageBox(confirmText));return;}
            Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                confirmText,
                onAccepted,
                destructive: false));
        }

        internal static bool BuildConfirmation(string actionText,string label,IEnumerable<string> manifestData,out string confirmText)
        {
            bool hasMetadata = false;
            bool fatal = false;
            TransferReport combined = new TransferReport();

            if (manifestData != null)
            {
                foreach (string data in manifestData)
                {
                    DefManifest manifest;
                    if (!DefManifestHelper.TryDeserializeCompressed(data, out manifest))
                        continue;

                    hasMetadata = true;
                    TransferReport report = DefManifestHelper.CheckCompatibility(manifest);
                    fatal |= report.HasFatalMissing;
                    combined.Compatible.AddRange(report.Compatible);
                    combined.Missing.AddRange(report.Missing);
                    combined.FatalMissing.AddRange(report.FatalMissing);
                }
            }

            if (fatal)
            {
                string text = "Phinix_legacyTalentTrade_compatibilityFatal".Localize(label);
                if (combined.Missing.Count > 0)
                    text += "\n\n" + string.Join("\n", Unique(combined.Missing));
                confirmText = text;
                return false;
            }

            confirmText = actionText;
            if (!hasMetadata)
            {
                confirmText += "\n\n" + "Phinix_legacyTalentTrade_compatibilityUnknown".Localize();
            }
            else if (combined.Missing.Count > 0)
            {
                StringBuilder warning = new StringBuilder();
                warning.AppendLine("Phinix_legacyTalentTrade_compatibilityWarning".Localize());
                warning.AppendLine();
                warning.Append(string.Join("\n", Unique(combined.Missing)));
                confirmText += "\n\n" + warning;
            }

            return true;
        }

        private static IEnumerable<string> Unique(IEnumerable<string> values)
        {
            HashSet<string> seen = new HashSet<string>();
            foreach (string value in values)
            {
                if (!string.IsNullOrEmpty(value) && seen.Add(value))
                    yield return value;
            }
        }
    }
}
