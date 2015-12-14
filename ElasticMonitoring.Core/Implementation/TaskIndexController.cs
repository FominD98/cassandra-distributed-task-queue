﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using log4net;

using MoreLinq;

using Newtonsoft.Json;

using RemoteQueue.Cassandra.Entities;
using RemoteQueue.Cassandra.Repositories;
using RemoteQueue.Cassandra.Repositories.GlobalTicksHolder;

using SKBKontur.Catalogue.CassandraPrimitives.RemoteLock;
using SKBKontur.Catalogue.Core.Graphite.Client.Relay;
using SKBKontur.Catalogue.Core.Graphite.Client.StatsD;
using SKBKontur.Catalogue.RemoteTaskQueue.ElasticMonitoring.Core.Implementation.Utils;
using SKBKontur.Catalogue.RemoteTaskQueue.ElasticMonitoring.TaskIndexedStorage.Types;
using SKBKontur.Catalogue.RemoteTaskQueue.ElasticMonitoring.TaskIndexedStorage.Writing;

namespace SKBKontur.Catalogue.RemoteTaskQueue.ElasticMonitoring.Core.Implementation
{
    public class TaskIndexController : ITaskIndexController
    {
        public TaskIndexController(
            IEventLogRepository eventLogRepository,
            IMetaCachedReader reader,
            ITaskMetaProcessor taskMetaProcessor,
            LastReadTicksStorage lastReadTicksStorage,
            IGlobalTime globalTime,
            IRemoteLockCreator remoteLockCreator,
            ICatalogueStatsDClient statsDClient,
            ICatalogueGraphiteClient graphiteClient,
            ITaskWriteDynamicSettings dynamicSettings)
        {
            this.eventLogRepository = eventLogRepository;
            this.reader = reader;
            this.taskMetaProcessor = taskMetaProcessor;
            this.globalTime = globalTime;
            this.remoteLockCreator = remoteLockCreator;
            this.lastReadTicksStorage = lastReadTicksStorage;
            maxBatch = dynamicSettings.MaxBatch;
            unstableZoneTicks = eventLogRepository.UnstableZoneLength.Ticks;
            unprocessedEventsMap = new EventsMap(unstableZoneTicks * 2);
            processedEventsMap = new EventsMap(unstableZoneTicks * 2);
            Interlocked.Exchange(ref lastTicks, unknownTicks);
            Interlocked.Exchange(ref snapshotTicks, unknownTicks);

            graphitePrefix = dynamicSettings.GraphitePrefixOrNull;
            if(graphitePrefix != null)
            {
                logger.LogInfoFormat("Graphite is ON. Prefix={0}", graphitePrefix);
                this.graphiteClient = graphiteClient;
                this.statsDClient = statsDClient.WithScope(string.Format("{0}.Actualization", graphitePrefix));
            }
            else
                this.statsDClient = EmptyStatsDClient.Instance;

            maxTicks = dynamicSettings.MaxTicks;
            remoteLockId = dynamicSettings.RemoteLockId;

            if(maxTicks.HasValue)
                logger.LogInfoFormat("MaxTicks is {0}", DateTimeFormatter.FormatWithMsAndTicks(maxTicks.Value));
            logger.LogInfoFormat("RemoteLockId: '{0}'", remoteLockId);
        }

        private const long unknownTicks = long.MinValue;

        private long GetNow()
        {
            return globalTime.GetNowTicks();
        }

        public void Dispose()
        {
            if(distributedLock != null)
            {
                //NOTE close lock if shutdown by container
                distributedLock.Dispose();
                distributedLock = null;
                logger.InfoFormat("Distributed lock released");
            }
        }

        private TaskMetaInformation[] CutMetas(TaskMetaInformation[] metas)
        {
            //NOTE hack code for tests
            var ticks = MinTicksHack;
            if(ticks <= 0)
                return metas;
            var list = new List<TaskMetaInformation>();
            foreach(var taskMetaInformation in metas)
            {
                if(taskMetaInformation.Ticks > ticks)
                    list.Add(taskMetaInformation);
            }
            return list.ToArray();
        }

        public void ProcessNewEvents()
        {
            lock(lockObject)
            {
                if(!DistributedLockAcquired())
                    return;
                var now = GetNow();
                var fromTicks = Interlocked.Read(ref lastTicks);

                if(maxTicks.HasValue && fromTicks > maxTicks) //NOTE hack
                {
                    logger.LogInfoFormat("MaxTicks is reached.");
                    return;
                }

                if(fromTicks == unknownTicks)
                {
                    fromTicks = GetLastTicks();
                    Interlocked.Exchange(ref lastTicks, fromTicks);
                    Interlocked.Exchange(ref snapshotTicks, fromTicks);
                }

                logger.LogInfoFormat("Processing new events from {0} to {1}", DateTimeFormatter.FormatWithMsAndTicks(fromTicks), DateTimeFormatter.FormatWithMsAndTicks(now));

                var hasEvents = false;

                unprocessedEventsMap.CollectGarbage(now);
                //NOTE collectGarbage before unprocessedEventsMap.GetEvents() to kill trash events that has no meta
                var unprocessedEvents = unprocessedEventsMap.GetEvents();

                processedEventsMap.CollectGarbage(now);
                reader.CollectGarbage(now);
                var newEvents = GetEvents(fromTicks);

                unprocessedEvents.Concat(newEvents)
                                 .Batch(maxBatch, Enumerable.ToArray)
                                 .ForEach(events =>
                                     {
                                         hasEvents = true;
                                         ProcessEventsBatch(events, now);
                                     });

                if(!hasEvents)
                    ProcessEventsBatch(new TaskMetaUpdatedEvent[0], now);
            }
        }

        private long GetLastTicks()
        {
            logger.LogInfoFormat("Loading Last ticks");
            var lastReadTicks = lastReadTicksStorage.GetLastReadTicks();
            logger.InfoFormat("Last ticks loaded. Value={0}", DateTimeFormatter.FormatWithMsAndTicks(lastReadTicks));
            return lastReadTicks;
        }

        private void ProcessEventsBatch(TaskMetaUpdatedEvent[] taskMetaUpdatedEvents, long now)
        {
            if(maxTicks.HasValue && Interlocked.Read(ref lastTicks) > maxTicks) //NOTE hack
                return;
            var actualMetas = statsDClient.Timing("ReadMetas", () => ReadActualMetas(taskMetaUpdatedEvents, now));

            actualMetas = CutMetas(actualMetas);

            taskMetaProcessor.IndexMetas(actualMetas);

            var ticks = GetSafeTimeForSnapshot(now, actualMetas);
            Interlocked.Exchange(ref snapshotTicks, ticks);
            SaveSnapshot(ticks);
            unprocessedEventsMap.CollectGarbage(now);
            processedEventsMap.CollectGarbage(now);
            reader.CollectGarbage(now);

            var lt = Interlocked.Read(ref lastTicks);
            if(lt < ticks)
                Interlocked.Exchange(ref lastTicks, ticks);
        }

        private TaskMetaInformation[] ReadActualMetas(TaskMetaUpdatedEvent[] taskMetaUpdatedEvents, long now)
        {
            var taskMetaInformations = reader.ReadActualMetasQuiet(taskMetaUpdatedEvents, now);
            var actualMetas = new List<TaskMetaInformation>();
            for(var i = 0; i < taskMetaInformations.Length; i++)
            {
                var taskMetaInformation = taskMetaInformations[i];
                var taskMetaUpdatedEvent = taskMetaUpdatedEvents[i];
                if(taskMetaInformation != null)
                {
                    actualMetas.Add(taskMetaInformation);
                    processedEventsMap.AddEvent(taskMetaUpdatedEvent);
                    unprocessedEventsMap.RemoveEvent(taskMetaUpdatedEvent);
                }
                else
                    unprocessedEventsMap.AddEvent(taskMetaUpdatedEvent);
            }

            var actualMetasArray = actualMetas.ToArray();
            return actualMetasArray;
        }

        public long MinTicksHack { get { return Interlocked.Read(ref minTicksHack); } }

        public void SendActualizationLagToGraphite()
        {
            if(graphitePrefix == null)
                return;
            var lag = GetActualizationLag();
            if(lag != null)
                graphiteClient.Send(string.Format("{0}.Actualization.Lag", graphitePrefix), lag.Value, DateTime.UtcNow);
        }

        private long? GetActualizationLag()
        {
            var ticks = Interlocked.Read(ref snapshotTicks);
            if(ticks != unknownTicks)
                return (long)TimeSpan.FromTicks(DateTime.UtcNow.Ticks - ticks).TotalMilliseconds;
            return null;
        }

        public void SetMinTicksHack(long minTicks)
        {
            Interlocked.Exchange(ref minTicksHack, minTicks);
        }

        private long GetSafeTimeForSnapshot(long now, TaskMetaInformation[] taskMetaInformations)
        {
            var lastTicksEstimation = GetMinTicks(taskMetaInformations, now);
            var oldestEventTime = unprocessedEventsMap.GetOldestEventTime();
            if(oldestEventTime != null)
                lastTicksEstimation = Math.Min(lastTicksEstimation, Math.Max(oldestEventTime.Value, (DateTime.UtcNow - TimeSpan.FromMinutes(15)).Ticks));
            return lastTicksEstimation;
        }

        private void SaveSnapshot(long ticks)
        {
            if(ticks != unknownTicks)
            {
                logger.LogInfoFormat("Snapshot moved to {0}", DateTimeFormatter.FormatWithMsAndTicks(ticks));
                lastReadTicksStorage.SetLastReadTicks(ticks);
            }
        }

        private static long GetMinTicks(TaskMetaInformation[] taskMetaInformations, long now)
        {
            if(taskMetaInformations.Length <= 0)
                return now;
            var minTicks = taskMetaInformations.Min(x => x.LastModificationTicks.Value);
            return minTicks;
        }

        private IEnumerable<TaskMetaUpdatedEvent> GetEvents(long fromTicks)
        {
            return eventLogRepository.GetEvents(fromTicks - unstableZoneTicks, maxBatch).Where(processedEventsMap.NotContains);
        }

        public void LogStatus()
        {
            logger.InfoFormat("Status: {0}", JsonConvert.SerializeObject(GetStatus(), Formatting.Indented));
        }

        public ElasticMonitoringStatus GetStatus()
        {
            return new ElasticMonitoringStatus()
                {
                    DistributedLockAcquired = IsDistributedLockAcquired(),
                    MinTicksHack = MinTicksHack,
                    UnprocessedMapLength = unprocessedEventsMap.GetUnsafeCount(),
                    ProcessedMapLength = processedEventsMap.GetUnsafeCount(),
                    ActualizationLagMs = GetActualizationLag(),
                    LastTicks = Interlocked.Read(ref lastTicks),
                    SnapshotTicks = Interlocked.Read(ref snapshotTicks),
                    NowTicks = DateTime.UtcNow.Ticks,
                    MetaCacheSize = reader.UnsafeGetCount(),
                };
        }

        public bool IsDistributedLockAcquired()
        {
            return distributedLock != null;
        }

        private bool DistributedLockAcquired()
        {
            if(IsDistributedLockAcquired())
                return true;
            IRemoteLock @lock;
            if(remoteLockCreator.TryGetLock(remoteLockId, out @lock))
            {
                distributedLock = @lock;
                logger.InfoFormat("Distributed lock acquired.");
                return true;
            }
            return false;
        }

        private readonly long? maxTicks;

        private static readonly ILog logger = LogManager.GetLogger("TaskIndexController");

        private long minTicksHack;
        private long lastTicks;
        private long snapshotTicks;
        private volatile IRemoteLock distributedLock;

        private readonly ITaskMetaProcessor taskMetaProcessor;
        private readonly LastReadTicksStorage lastReadTicksStorage;
        private readonly IRemoteLockCreator remoteLockCreator;
        private readonly ICatalogueGraphiteClient graphiteClient;
        private readonly ICatalogueStatsDClient statsDClient;
        private readonly IGlobalTime globalTime;
        private readonly IEventLogRepository eventLogRepository;
        private readonly IMetaCachedReader reader;
        private readonly EventsMap unprocessedEventsMap;
        private readonly EventsMap processedEventsMap;

        private readonly object lockObject = new object();

        private readonly long unstableZoneTicks;
        private readonly int maxBatch;
        private readonly string graphitePrefix;
        private readonly string remoteLockId;
    }
}