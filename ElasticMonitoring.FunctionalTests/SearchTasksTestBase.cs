﻿using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using NUnit.Framework;

using RemoteTaskQueue.Monitoring.Storage.Client;

using SKBKontur.Catalogue.NUnit.Extensions.EdiTestMachinery;
using SKBKontur.Catalogue.Objects;

using TestCommon;

#pragma warning disable 649

namespace SKBKontur.Catalogue.RemoteTaskQueue.ElasticMonitoring.FunctionalTests
{
    [SuppressMessage("ReSharper", "UnassignedReadonlyField")]
    [EdiTestSuite("SearchTasksTests"), WithColumnFamilies, WithExchangeServices, AndResetMonitoringServiceState]
    public abstract class SearchTasksTestBase
    {
        protected void CheckSearch(string q, Timestamp from, Timestamp to, params string[] expectedIds)
        {
            var actualIds = Search(taskSearchClient, q, from, to);
            CollectionAssert.AreEquivalent(expectedIds, actualIds, "q=" + q);
        }

        private static string[] Search(ITaskSearchClient taskSearchClient, string q, Timestamp from, Timestamp to)
        {
            var taskSearchResponse = taskSearchClient.SearchFirst(new TaskSearchRequest
                {
                    FromTicksUtc = from.Ticks,
                    ToTicksUtc = to.Ticks,
                    QueryString = q
                });
            var result = new List<string>();
            if(taskSearchResponse.NextScrollId != null)
            {
                do
                {
                    foreach(var id in taskSearchResponse.Ids)
                        result.Add(id);
                    taskSearchResponse = taskSearchClient.SearchNext(taskSearchResponse.NextScrollId);
                } while(taskSearchResponse.Ids != null && taskSearchResponse.Ids.Length > 0);
            }
            return result.ToArray();
        }

        [Injected]
        private readonly TaskSearchClient taskSearchClient;

        [Injected]
        protected readonly ElasticMonitoringServiceClient elasticMonitoringServiceClient;

        [Injected]
        protected readonly RemoteQueue.Handling.RemoteTaskQueue remoteTaskQueue;
    }
}