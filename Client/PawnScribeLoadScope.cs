using System;
using Verse;
namespace Phinix.LegacyTalentTradeExtension.Client
{
    // Standalone pawn loading uses the same initialization context as a game load.
    // In Playing, initial age-cache evaluation would dispatch a live birthday transition with no previous stage.
    internal sealed class PawnScribeLoadScope : IDisposable
    {
        private readonly ProgramState previous;
        private bool disposed;
        internal PawnScribeLoadScope()
        {
            if (Scribe.mode != LoadSaveMode.Inactive) throw new InvalidOperationException("Pawn Scribe load is already busy.");
            previous = Current.ProgramState;
            Current.ProgramState = ProgramState.MapInitializing;
        }
        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            try { Scribe.loader.ForceStop(); }
            finally { Current.ProgramState = previous; }
        }
    }
}
