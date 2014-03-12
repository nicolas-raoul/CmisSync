using System;
using System.Timers;

namespace CmisSync.Lib.Events
{
    /// <summary>
    /// Sync scheduler. Inserts every pollInterval a new StartNextSyncEvent into the Queue
    /// </summary>
    public class SyncScheduler : SyncEventHandler, IDisposable
    {
        /// <summary>
        /// The default Queue Event Handler PRIORITY.
        /// </summary>
        private double interval;
        private ISyncEventQueue queue;
        private Timer timer;
        /// <summary>
        /// Initializes a new instance of the <see cref="CmisSync.Lib.Events.SyncScheduler"/> class.
        /// Starts adding events automatically after successful creation.
        /// </summary>
        /// <param name="queue">Queue.</param>
        /// <param name="pollInterval">Poll interval.</param>
        public SyncScheduler (ISyncEventQueue queue, double pollInterval = 5000)
        {
            if(queue == null)
                throw new ArgumentNullException("Given queue must not be null");
            if(pollInterval <= 0)
                throw new ArgumentException("pollinterval must be greater than zero");

            this.interval = pollInterval;
            this.queue = queue;
            this.timer = new Timer(this.interval);
            this.timer.Elapsed += delegate(object sender, ElapsedEventArgs e) {
                this.queue.AddEvent(new StartNextSyncEvent());
            };
        }

        /// <summary>
        /// Starts adding events into the Queue, if it has been stopped before.
        /// </summary>
        public void Start() {
            this.timer.Start();
        }

        /// <summary>
        /// Stops adding event into the Queue
        /// </summary>
        public void Stop() {
            this.timer.Stop();
        }

        /// <summary>
        /// Handles Config changes if the poll interval has been changed.
        /// Resets also the timer if a full sync event has been recognized.
        /// </summary>
        /// <param name="e">E.</param>
        public override bool Handle (ISyncEvent e)
        {
            RepoConfigChangedEvent config = e as RepoConfigChangedEvent;
            if(config!=null)
            {
                double newInterval = config.RepoInfo.PollInterval;
                if( newInterval> 0 && this.interval != newInterval)
                {
                    this.interval = newInterval;
                    Stop ();
                    timer.Interval = this.interval;
                    Start ();
                }
                return false;
            }
            StartNextSyncEvent start = e as StartNextSyncEvent;
            if(start != null && start.FullSyncRequested)
            {
                Stop ();
                Start();
            }
            return false;
        }

        /// <summary>
        /// Releases all resource used by the <see cref="CmisSync.Lib.Events.SyncScheduler"/> object.
        /// </summary>
        /// <remarks>Call <see cref="Dispose"/> when you are finished using the <see cref="CmisSync.Lib.Events.SyncScheduler"/>.
        /// The <see cref="Dispose"/> method leaves the <see cref="CmisSync.Lib.Events.SyncScheduler"/> in an unusable
        /// state. After calling <see cref="Dispose"/>, you must release all references to the
        /// <see cref="CmisSync.Lib.Events.SyncScheduler"/> so the garbage collector can reclaim the memory that the
        /// <see cref="CmisSync.Lib.Events.SyncScheduler"/> was occupying.</remarks>
        public void Dispose() {
            timer.Stop();
            timer.Dispose();
        }
    }
}

