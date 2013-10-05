using System;
using System.Collections.Generic;
using System.Linq;

using GroBuf;

using RemoteQueue.Cassandra.Repositories.GlobalTicksHolder;

using SKBKontur.Cassandra.CassandraClient.Abstractions;

using MoreLinq;

using SKBKontur.Cassandra.CassandraClient.Connections;

namespace RemoteQueue.Cassandra.Primitives
{
    public class BlobStorage<T> : ColumnFamilyRepositoryBase, IBlobStorage<T>
    {
        public BlobStorage(IColumnFamilyRepositoryParameters parameters, ISerializer serializer, IGlobalTime globalTime, string columnFamilyName)
            : base(parameters, columnFamilyName)
        {
            this.serializer = serializer;
            this.globalTime = globalTime;
        }

        public void Write(string id, T element)
        {
            var connection = RetrieveColumnFamilyConnection();
            connection.AddColumn(id, new Column
                {
                    Name = dataColumnName,
                    Timestamp = globalTime.UpdateNowTicks(),
                    Value = serializer.Serialize(element)
                });
        }

        public T Read(string id)
        {
            var connection = RetrieveColumnFamilyConnection();
            Column column;
            if(connection.TryGetColumn(id, dataColumnName, out column))
                return serializer.Deserialize<T>(column.Value);
            return default(T);
        }

        public T[] Read(string[] ids)
        {
            if (ids.Length == 0)
                return new T[0];
            return TryReadInternal(ids);
        }

        private T[] TryReadInternal(string[] ids)
        {
            if (ids == null) throw new ArgumentNullException("ids");
            if (ids.Length == 0) return new T[0];
            var rows = new List<KeyValuePair<string, Column[]>>();
            ids
                .Batch(1000, Enumerable.ToArray)
                .ForEach(batchIds => MakeInConnection(connection => rows.AddRange(connection.GetRowsExclusive(batchIds, null, 1000))));
            var rowsDict = rows.ToDictionary(row => row.Key);

            return ids.Where(rowsDict.ContainsKey)
                .Select(id => Read(rowsDict[id].Value))
                .Where(obj => obj != null).ToArray();
        }

        private T Read(IEnumerable<Column> columns)
        {
            return columns.Where(column => column.Name == dataColumnName).Select(column => serializer.Deserialize<T>(column.Value)).FirstOrDefault();
        }


        public IEnumerable<T> ReadAll(int batchSize = 1000)
        {
            var connection = RetrieveColumnFamilyConnection();
            var keys = connection.GetKeys(batchSize);
            return new SeparateOnBatchesEnumerable<string>(keys, batchSize).SelectMany(
                batch => connection.GetRows(batch, dataColumnName, 1)
                                   .Where(x => x.Value.Length > 0)
                                   .Select(x => serializer.Deserialize<T>(x.Value[0].Value)));
        }

        private void MakeInConnection(Action<IColumnFamilyConnection> action)
        {
            var connection = RetrieveColumnFamilyConnection();
            action(connection);
        }


        private readonly ISerializer serializer;
        private readonly IGlobalTime globalTime;
        private const string dataColumnName = "Data";
    }
}