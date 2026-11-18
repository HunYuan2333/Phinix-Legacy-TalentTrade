using System;
using PhinixClient.Framework;

namespace Phinix.LegacyTalentTradeExtension.Client
{
    // Owns a module task scope. Only queued/running work coalesces later ticks;
    // rejection/cancellation/failure releases the slot on the next tick.
    internal sealed class TalentUpdateDriver : IDisposable
    {
        private readonly object gate = new object();
        private readonly IClientTaskScope scope;
        private readonly Action update;
        private IClientTaskHandle pending;
        private bool stopped;
        internal TalentUpdateDriver(IClientTaskScope scope, Action update)
        { this.scope=scope??throw new ArgumentNullException(nameof(scope)); this.update=update??throw new ArgumentNullException(nameof(update)); }
        internal void Tick()
        {
            lock(gate)
            {
                if(stopped || (pending!=null && !pending.Capture().Terminal)) return;
                pending=null;
                pending=scope.Submit(Run);
            }
        }
        private void Run()
        {
            lock(gate) { if(stopped) return; }
            // Host executes this on the game thread; shutdown uses that same boundary.
            update();
        }
        public void Dispose()
        {
            lock(gate) { if(stopped) return; stopped=true; pending=null; }
            scope.Dispose();
        }
    }
}
