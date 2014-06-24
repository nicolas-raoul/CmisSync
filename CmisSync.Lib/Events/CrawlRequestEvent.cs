using System;
using System.IO;
using DotCMIS.Client;

namespace CmisSync.Lib.Events
{
    public class CrawlRequestEvent : ISyncEvent
    {
        public IFolder RemoteFolder { get; private set; }

        public DirectoryInfo LocalFolder { get; private set; }

        public CrawlRequestEvent (DirectoryInfo localFolder, IFolder remoteFolder)
        {
            if(localFolder == null)
                throw new ArgumentNullException("Given path is null");
            if(remoteFolder == null)
                throw new ArgumentNullException("Given remote folder is null");
            RemoteFolder = remoteFolder;
            LocalFolder = localFolder;
        }
    }
}
