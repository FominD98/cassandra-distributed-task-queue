using System.Collections.Generic;

using SKBKontur.Catalogue.ClientLib.Domains;
using SKBKontur.Catalogue.ClientLib.Topology;
using SKBKontur.Catalogue.Expressions.ExpressionTrees;
using SKBKontur.Catalogue.RemoteTaskQueue.MonitoringDataTypes.MonitoringEntities;
using SKBKontur.Catalogue.RemoteTaskQueue.MonitoringDataTypes.Queries;

namespace SKBKontur.Catalogue.RemoteTaskQueue.MonitoringServiceClient
{
    public class RemoteTaskQueueMonitoringServiceClient : IRemoteTaskQueueMonitoringServiceClient
    {
        public RemoteTaskQueueMonitoringServiceClient(IDomainTopologyFactory domainTopologyFactory, IMethodDomainFactory methodDomainFactory)
        {
            this.methodDomainFactory = methodDomainFactory;
            domainTopology = domainTopologyFactory.Create("remoteTaskQueueMonitoringServiceTopology");
        }

        public void ActualizeDatabaseScheme()
        {
            var domain = methodDomainFactory.Create("ActualizeDatabaseScheme", domainTopology, timeout, clientName);
            domain.SendToEachReplica(DomainConsistencyLevel.All);
        }

        public void DropLocalStorage()
        {
            var domain = methodDomainFactory.Create("DropLocalStorage", domainTopology, timeout, clientName);
            domain.SendToEachReplica(DomainConsistencyLevel.All);
        }

        public MonitoringTaskMetadata[] Search(ExpressionTree criterion, ExpressionTree sortRules, int count = 1000, int rangeFrom = 0)
        {
            var domain = methodDomainFactory.Create("Search", domainTopology, timeout, clientName);
            var searchQuery = new MonitoringSearchQuery
                {
                    Criterion = criterion,
                    SortRules = sortRules,
                    RangeFrom = rangeFrom,
                    Count = count,
                };
            return domain.QueryFromRandomReplica<MonitoringTaskMetadata[], MonitoringSearchQuery>(searchQuery);
        }

        public object[] GetDistinctValues(ExpressionTree criterion, ExpressionTree columnPath)
        {
            var domain = methodDomainFactory.Create("GetDistinctValues", domainTopology, timeout, clientName);
            var monitoringGetDistinctValuesQuery = new MonitoringGetDistinctValuesQuery
                {
                    Criterion = criterion,
                    ColumnPath = columnPath,
                };
            return domain.QueryFromRandomReplica<object[], MonitoringGetDistinctValuesQuery>(monitoringGetDistinctValuesQuery);
        }

        public int GetCount(ExpressionTree criterion)
        {
            var domain = methodDomainFactory.Create("GetCount", domainTopology, timeout, clientName);
            var query = new MonitoringGetCountQuery
                {
                    Criterion = criterion
                };
            return domain.QueryFromRandomReplica<int, MonitoringGetCountQuery>(query);
        }

        private readonly IMethodDomainFactory methodDomainFactory;
        private readonly IDomainTopology domainTopology;
        private const int timeout = 30 * 1000;
        private const string clientName = "RemoteTaskQueueMonitoringServiceClient";
    }
}