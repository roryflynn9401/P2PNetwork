using P2PProject.Client;
using System.Timers;

namespace P2PProject.Data
{
    public abstract class SyncService
    {
        public SyncService(Node node) 
        { 
            _localNode = node;
        }

        protected Node _localNode;
        public System.Timers.Timer? Timer;

        public virtual async Task InitaliseSync()
        {
            Timer = new System.Timers.Timer(15000);
            Timer.Elapsed += OnSyncEvent;
            Timer.Enabled = true;
        }
        protected abstract void OnSyncEvent(object source, ElapsedEventArgs e);
    }
}
