using RemoteQueue.Handling;

using SKBKontur.Catalogue.AccessControl;
using SKBKontur.Catalogue.Core.CommonBusinessObjects;
using SKBKontur.Catalogue.Core.Web.Controllers;
using SKBKontur.Catalogue.Core.Web.Models.ModelConfigurations;
using SKBKontur.Catalogue.ObjectManipulation.Extender;
using SKBKontur.Catalogue.RemoteTaskQueue.MonitoringServiceClient;
using SKBKontur.Catalogue.RemoteTaskQueue.TaskMonitoringViewer.ModelBuilders;
using SKBKontur.Catalogue.RemoteTaskQueue.TaskMonitoringViewer.ModelBuilders.TaskDetails;
using SKBKontur.Catalogue.RemoteTaskQueue.TaskMonitoringViewer.ModelBuilders.TaskList;
using SKBKontur.Catalogue.RemoteTaskQueue.TaskMonitoringViewer.Models.TaskList;

namespace SKBKontur.Catalogue.RemoteTaskQueue.TaskMonitoringViewer.Controllers
{
    public class RemoteTaskQueueControllerBaseParameters
    {
        public RemoteTaskQueueControllerBaseParameters(
            LoggedInControllerBaseParameters loggedInControllerBaseParameters,
            ITaskMetadataModelBuilder taskMetadataModelBuilder,
            ITaskDetailsModelBuilder taskDetailsModelBuilder,
            ITaskDetailsHtmlModelBuilder taskDetailsHtmlModelBuilder,
            IRemoteTaskQueueMonitoringServiceStorage remoteTaskQueueMonitoringServiceStorage,
            IBusinessObjectStorage businessObjectsStorage,
            ICatalogueExtender catalogueExtender,
            IMonitoringSearchRequestCriterionBuilder monitoringSearchRequestCriterionBuilder,
            IRemoteTaskQueue remoteTaskQueue,
            ITaskListModelBuilder taskListModelBuilder,
            ITaskListHtmlModelBuilder taskListHtmlModelBuilder,
            IAccessControlService accessControlService,
            IWebMutatorsTreeCollection<TaskListModelData> webMutatorsTreeCollection)
        {
            TaskListHtmlModelBuilder = taskListHtmlModelBuilder;
            TaskListModelBuilder = taskListModelBuilder;
            RemoteTaskQueue = remoteTaskQueue;
            BusinessObjectsStorage = businessObjectsStorage;
            CatalogueExtender = catalogueExtender;
            MonitoringSearchRequestCriterionBuilder = monitoringSearchRequestCriterionBuilder;
            TaskMetadataModelBuilder = taskMetadataModelBuilder;
            LoggedInControllerBaseParameters = loggedInControllerBaseParameters;
            TaskDetailsModelBuilder = taskDetailsModelBuilder;
            TaskDetailsHtmlModelBuilder = taskDetailsHtmlModelBuilder;
            RemoteTaskQueueMonitoringServiceStorage = remoteTaskQueueMonitoringServiceStorage;
            AccessControlService = accessControlService;
            WebMutatorsTreeCollection = webMutatorsTreeCollection;
        }

        public ITaskListModelBuilder TaskListModelBuilder { get; private set; }

        public IRemoteTaskQueue RemoteTaskQueue { get; private set; }
        public IBusinessObjectStorage BusinessObjectsStorage { get; private set; }
        public ICatalogueExtender CatalogueExtender { get; set; }
        public IMonitoringSearchRequestCriterionBuilder MonitoringSearchRequestCriterionBuilder { get; private set; }
        public LoggedInControllerBaseParameters LoggedInControllerBaseParameters { get; private set; }
        public ITaskMetadataModelBuilder TaskMetadataModelBuilder { get; private set; }
        public ITaskDetailsModelBuilder TaskDetailsModelBuilder { get; private set; }
        public ITaskDetailsHtmlModelBuilder TaskDetailsHtmlModelBuilder { get; private set; }
        public IRemoteTaskQueueMonitoringServiceStorage RemoteTaskQueueMonitoringServiceStorage { get; private set; }
        public ITaskListHtmlModelBuilder TaskListHtmlModelBuilder { get; private set; }
        public IAccessControlService AccessControlService { get; private set; }
        public IWebMutatorsTreeCollection<TaskListModelData> WebMutatorsTreeCollection { get; private set; }
    }
}