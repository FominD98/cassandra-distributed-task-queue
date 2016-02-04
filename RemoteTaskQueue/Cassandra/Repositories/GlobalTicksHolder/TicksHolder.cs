using System;
using System.Collections.Concurrent;

using GroBuf;

using JetBrains.Annotations;

using RemoteQueue.Settings;

using SKBKontur.Cassandra.CassandraClient.Abstractions;
using SKBKontur.Cassandra.CassandraClient.Clusters;
using SKBKontur.Cassandra.CassandraClient.Connections;

namespace RemoteQueue.Cassandra.Repositories.GlobalTicksHolder
{
    // todo (maybe-optimize): ��-�������� ���� ��������� �� ��� CF
    // ����� ����������� �� ����� ������� ������� - � ����� CF ����� ������� ��� ������ ���������� ������� ���������: ticks � long.MaxValue - ticks
    public class TicksHolder : ITicksHolder
    {
        public TicksHolder(ICassandraCluster cassandraCluster, ISerializer serializer, ICassandraSettings cassandraSettings)
        {
            this.cassandraCluster = cassandraCluster;
            this.serializer = serializer;
            keyspaceName = cassandraSettings.QueueKeyspace;
        }

        public void UpdateMaxTicks([NotNull] string name, long ticks)
        {
            //lock(locker)
            {
                /*long? maxTicks;
                if(persistedMaxTicks.TryGetValue(name, out maxTicks) && maxTicks.HasValue && ticks <= maxTicks.Value)
                    return;*/
                RetrieveColumnFamilyConnection().AddColumn(name, new Column
                    {
                        Name = maxTicksColumnName,
                        Timestamp = ticks,
                        Value = serializer.Serialize(ticks),
                        TTL = null,
                    });
                //persistedMaxTicks.AddOrUpdate(name, ticks, (key, oldMaxTicks) => oldMaxTicks.HasValue ? Math.Max(ticks, oldMaxTicks.Value) : ticks);
            }
        }

        public long GetMaxTicks([NotNull] string name)
        {
            Column column;
            if(!RetrieveColumnFamilyConnection().TryGetColumn(name, maxTicksColumnName, out column))
                return 0;
            return serializer.Deserialize<long>(column.Value);
        }

        public void UpdateMinTicks([NotNull] string name, long ticks)
        {
            //lock(locker)
            {
                /*long? minTicks;
                if(persistedMinTicks.TryGetValue(name, out minTicks) && minTicks.HasValue && ticks >= minTicks.Value)
                    return;*/
                RetrieveColumnFamilyConnection().AddColumn(name, new Column
                    {
                        Name = minTicksColumnName,
                        Timestamp = long.MaxValue - ticks,
                        Value = serializer.Serialize(long.MaxValue - ticks),
                        TTL = null,
                    });
                //persistedMinTicks.AddOrUpdate(name, ticks, (key, oldMinTicks) => oldMinTicks.HasValue ? Math.Min(ticks, oldMinTicks.Value) : ticks);
            }
        }

        public long GetMinTicks([NotNull] string name)
        {
            Column column;
            if(!RetrieveColumnFamilyConnection().TryGetColumn(name, minTicksColumnName, out column))
                return 0;
            return long.MaxValue - serializer.Deserialize<long>(column.Value);
        }

        [NotNull]
        private IColumnFamilyConnection RetrieveColumnFamilyConnection()
        {
            return cassandraCluster.RetrieveColumnFamilyConnection(keyspaceName, ColumnFamilyName);
        }

        public void ResetInMemoryState()
        {
            persistedMaxTicks.Clear();
            persistedMinTicks.Clear();
        }

        public const string ColumnFamilyName = "ticksHolder";
        private const string maxTicksColumnName = "MaxTicks";
        private const string minTicksColumnName = "MinTicks";
        private readonly ICassandraCluster cassandraCluster;
        private readonly ISerializer serializer;
        private readonly string keyspaceName;
        private readonly object locker = new object();
        private readonly ConcurrentDictionary<string, long?> persistedMaxTicks = new ConcurrentDictionary<string, long?>();
        private readonly ConcurrentDictionary<string, long?> persistedMinTicks = new ConcurrentDictionary<string, long?>();
    }
}