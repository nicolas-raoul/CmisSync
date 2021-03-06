//-----------------------------------------------------------------------
// <copyright file="DescendantsCrawler.cs" company="GRAU DATA AG">
//
//   This program is free software: you can redistribute it and/or modify
//   it under the terms of the GNU General private License as published by
//   the Free Software Foundation, either version 3 of the License, or
//   (at your option) any later version.
//
//   This program is distributed in the hope that it will be useful,
//   but WITHOUT ANY WARRANTY; without even the implied warranty of
//   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//   GNU General private License for more details.
//
//   You should have received a copy of the GNU General private License
//   along with this program. If not, see http://www.gnu.org/licenses/.
//
// </copyright>
//-----------------------------------------------------------------------

namespace CmisSync.Lib.Producer.Crawler {
    using System;

    using CmisSync.Lib.Consumer;
    using CmisSync.Lib.Cmis.ConvenienceExtenders;
    using CmisSync.Lib.Events;
    using CmisSync.Lib.Exceptions;
    using CmisSync.Lib.Filter;
    using CmisSync.Lib.Queueing;
    using CmisSync.Lib.SelectiveIgnore;
    using CmisSync.Lib.Storage.Database;
    using CmisSync.Lib.Storage.Database.Entities;
    using CmisSync.Lib.Storage.FileSystem;

    using DotCMIS.Client;
    using DotCMIS.Exceptions;

    using log4net;

    /// <summary>
    /// Decendants crawler.
    /// </summary>
    public class DescendantsCrawler : ReportingSyncEventHandler {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(DescendantsCrawler));
        private IActivityListener activityListener;
        private IDescendantsTreeBuilder treebuilder;
        private CrawlEventGenerator eventGenerator;
        private CrawlEventNotifier notifier;

        /// <summary>
        /// Initializes a new instance of the <see cref="DescendantsCrawler"/> class.
        /// </summary>
        /// <param name="queue">Sync Event Queue.</param>
        /// <param name="remoteFolder">Remote folder.</param>
        /// <param name="localFolder">Local folder.</param>
        /// <param name="storage">Meta data storage.</param>
        /// <param name="filter">Aggregated filter.</param>
        /// <param name="activityListener">Activity listner.</param>
        /// <param name="ignoredStorage">Ignored entities storage.</param>
        public DescendantsCrawler(
            ISyncEventQueue queue,
            IFolder remoteFolder,
            IDirectoryInfo localFolder,
            IMetaDataStorage storage,
            IFilterAggregator filter,
            IActivityListener activityListener,
            IIgnoredEntitiesStorage ignoredStorage)
            : base(queue)
        {
            if (remoteFolder == null) {
                throw new ArgumentNullException("remoteFolder");
            }

            if (localFolder == null) {
                throw new ArgumentNullException("localFolder");
            }

            if (storage == null) {
                throw new ArgumentNullException("storage");
            }

            if (filter == null) {
                throw new ArgumentNullException("filter");
            }

            if (activityListener == null) {
                throw new ArgumentNullException("activityListener");
            }

            this.activityListener = activityListener;
            this.treebuilder = new DescendantsTreeBuilder(storage, remoteFolder, localFolder, filter, ignoredStorage);
            this.eventGenerator = new CrawlEventGenerator(storage);
            this.notifier = new CrawlEventNotifier(queue);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CmisSync.Lib.Producer.Crawler.DescendantsCrawler"/> class based on its internal classes.
        /// This is mostly usefull for Unit Testing
        /// </summary>
        /// <param name='queue'>
        /// The event queue.
        /// </param>
        /// <param name='builder'>
        /// The DescendantsTreeBuilder.
        /// </param>
        /// <param name='generator'>
        /// The CrawlEventGenerator.
        /// </param>
        /// <param name="notifier">
        /// Event Notifier.
        /// </param>
        /// <param name='activityListener'>
        /// Activity listener.
        /// </param>
        /// <exception cref='ArgumentNullException'>
        /// <attribution license="cc4" from="Microsoft" modified="false" /><para>The exception that is thrown when a
        /// null reference (Nothing in Visual Basic) is passed to a method that does not accept it as a valid argument. </para>
        /// </exception>
        public DescendantsCrawler(
            ISyncEventQueue queue,
            IDescendantsTreeBuilder builder,
            CrawlEventGenerator generator,
            CrawlEventNotifier notifier,
            IActivityListener activityListener)
            : base(queue)
        {
            if (activityListener == null) {
                throw new ArgumentNullException("activityListener");
            }

            this.activityListener = activityListener;
            this.treebuilder = builder;
            this.eventGenerator = generator;
            this.notifier = notifier;
        }

        /// <summary>
        /// Handles StartNextSync events.
        /// </summary>
        /// <param name="e">The event to handle.</param>
        /// <returns>true if handled</returns>
        public override bool Handle(ISyncEvent e) {
            var startNextSync = e as StartNextSyncEvent;
            if (startNextSync != null) {
                try {
                    Logger.Debug("Starting DecendantsCrawlSync upon " + e);
                    using (var activity = new ActivityListenerResource(this.activityListener)) {
                        this.CrawlDescendants();
                    }

                    this.Queue.AddEvent(new FullSyncCompletedEvent(e as StartNextSyncEvent));
                    return true;
                } catch (InteractionNeededException interaction) {
                    this.Queue.AddEvent(new InteractionNeededEvent(interaction));
                    throw;
                } catch (CmisConnectionException connectionInterrupted) {
                    Logger.Debug("Connection lost on descendants crawl.", connectionInterrupted);
                    throw;
                } catch (Exception retryException) {
                    Logger.Info("Failed to crawl descendants (trying again):", retryException);
                    this.Queue.AddEvent(new StartNextSyncEvent(fullSyncRequested: startNextSync.FullSyncRequested) {
                        LastTokenOnServer = startNextSync.LastTokenOnServer
                    });
                    return false;
                }
            }

            return false;
        }

        private void CrawlDescendants() {
            try {
                DescendantsTreeCollection trees = this.treebuilder.BuildTrees();
                if (Logger.IsDebugEnabled) {
                    Logger.Debug(string.Format("LocalTree:  {0} Elements", trees.LocalTree.ToList().Count));
                    Logger.Debug(string.Format("RemoteTree: {0} Elements", trees.RemoteTree.ToList().Count));
                    Logger.Debug(string.Format("StoredTree: {0} Elements", trees.StoredObjects.Count));
                }

                Logger.Debug("Create events");
                CrawlEventCollection events = this.eventGenerator.GenerateEvents(trees);
                Logger.Debug("Events created");
                this.notifier.MergeEventsAndAddToQueue(events);
                Logger.Debug("Events merged and added to queue");
                var localRoot = trees.LocalTree.Item;
                var remoteRoot = trees.RemoteTree.Item;
                bool localIsReadOnly = localRoot.ReadOnly;
                bool remoteIsReadOnly = remoteRoot.IsReadOnly();
                if (localIsReadOnly != remoteIsReadOnly) {
                    localRoot.ReadOnly = remoteIsReadOnly;
                }
            } catch (System.IO.PathTooLongException e) {
                string msg = "Crawl Sync aborted because a local path is too long. Please take a look into the log to figure out the reason.";
                throw new InteractionNeededException(msg, e) { Title = "Local path is too long", Description = msg };
            }
        }
    }
}