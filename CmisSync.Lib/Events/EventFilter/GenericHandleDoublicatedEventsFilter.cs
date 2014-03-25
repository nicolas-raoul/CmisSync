using System;

using CmisSync.Lib.Events;

namespace CmisSync.Lib.Events.Filter
{
    public class GenericHandleDoublicatedEventsFilter<TFilter, TReset> : SyncEventHandler
        where TFilter: ISyncEvent
        where TReset : ISyncEvent
    {
        private bool firstOccurence = true;

        public override bool Handle (ISyncEvent e)
        {
            if(e is TFilter)
            {
                if(firstOccurence)
                {
                    firstOccurence = false;
                    return false;
                }
                else
                {
                    return true;
                }
            }
            if(e is TReset)
            {
                firstOccurence = true;
            }
            return false;
        }

        public override int Priority {
            get {
                return EventHandlerPriorities.GetPriority(typeof(GenericHandleDoublicatedEventsFilter<,>));
            }
        }
    }
}

