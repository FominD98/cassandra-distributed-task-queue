﻿using GroBuf;

using GroboContainer.Core;


using RemoteQueue.Cassandra.Primitives;

using SKBKontur.Cassandra.CassandraClient.Clusters;
using SKBKontur.Catalogue.CassandraPrimitives.RemoteLock;
using SKBKontur.Catalogue.CassandraPrimitives.RemoteLock.RemoteLocker;
using SKBKontur.Catalogue.CassandraPrimitives.Storages.Primitives;
using SKBKontur.Catalogue.RemoteTaskQueue.Common.RemoteTaskQueue;

namespace SKBKontur.Catalogue.RemoteTaskQueue.Common
{
    public static class LockRepositoryContainerConfigurator
    {
        public static void ConfigureLockRepository(this IContainer container, string remoteLockColumnFamily = null)
        {
            var serializer = container.Get<ISerializer>();
            container.Configurator.ForAbstraction<ICassandraClusterSettings>().UseType<RemoteQueueTestsCassandraSettings>();
            var columnFamilyRepositoryParameters = container.Get<IColumnFamilyRepositoryParameters>();
            remoteLockColumnFamily = remoteLockColumnFamily ?? columnFamilyRepositoryParameters.LockColumnFamilyName;
            var cassandraRemoteLockImplementationSettings = CassandraRemoteLockImplementationSettings.Default(new ColumnFamilyFullName(columnFamilyRepositoryParameters.Settings.QueueKeyspaceForLock, remoteLockColumnFamily));
            var remoteLockImplementation = new CassandraRemoteLockImplementation(columnFamilyRepositoryParameters.CassandraCluster, serializer, cassandraRemoteLockImplementationSettings);
            container.Configurator.ForAbstraction<IRemoteLockCreator>().UseInstances(new RemoteLocker(remoteLockImplementation, new RemoteLockerMetrics(columnFamilyRepositoryParameters.Settings.QueueKeyspace)));
        }
    }
}