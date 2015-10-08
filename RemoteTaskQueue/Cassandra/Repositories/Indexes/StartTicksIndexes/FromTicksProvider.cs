using System;
using System.Collections.Concurrent;

using JetBrains.Annotations;

using RemoteQueue.Cassandra.Entities;
using RemoteQueue.Cassandra.Repositories.GlobalTicksHolder;

namespace RemoteQueue.Cassandra.Repositories.Indexes.StartTicksIndexes
{
    public class FromTicksProvider : IFromTicksProvider
    {
        public FromTicksProvider(ITicksHolder ticksHolder)
        {
            this.ticksHolder = ticksHolder;
            minTicksCache = new MinTicksCache(this.ticksHolder);
        }

        public long? TryGetFromTicks(TaskState taskState)
        {
            var firstTicks = minTicksCache.GetMinTicks(taskState);
            if(firstTicks == 0)
                return null;
            var diff = GetDiff(taskState).Ticks;
            var firstTicksWithDiff = firstTicks - diff;
            var twoDaysEarlier = (DateTime.UtcNow - TimeSpan.FromDays(2)).Ticks;
            return Math.Max(twoDaysEarlier, firstTicksWithDiff);
        }

        private TimeSpan GetDiff(TaskState taskState)
        {
            var lastBigDiffTime = lastBigDiffTimes.GetOrAdd(taskState, t => DateTime.MinValue);
            var now = DateTime.UtcNow;
            if(now - lastBigDiffTime > TimeSpan.FromMinutes(1))
            {
                lastBigDiffTimes.AddOrUpdate(taskState, DateTime.MinValue, (t, p) => now);
                //������ ���������� ������������� ���������� ������ ������, � ��� ���������� ����� ����� ����������,
                //��� ��������� ��������� ����� ������. ������� �������, ��� �������, � �������
                return TimeSpan.FromMinutes(8); // ������ ������ ������� ���������
            }
            return TimeSpan.FromMinutes(1); // ������� ���� ��������������
        }

        public void HandleTaskStateChange([NotNull] TaskMetaInformation taskMeta)
        {
            var taskState = taskMeta.State.GetCassandraName();
            var ticks = taskMeta.MinimalStartTicks;
            ticksHolder.UpdateMinTicks(taskState, ticks);
            // todo: ����� ����� ��������� ����� ������ � MinTicksCache ��� �������������
        }

        public void UpdateMinTicks(TaskState taskState, long ticks)
        {
            minTicksCache.UpdateMinTicks(taskState, ticks);
        }

        private readonly ITicksHolder ticksHolder;
        private readonly MinTicksCache minTicksCache;
        private readonly ConcurrentDictionary<TaskState, DateTime> lastBigDiffTimes = new ConcurrentDictionary<TaskState, DateTime>();
    }
}