// @flow
import React from 'react';
import $c from 'property-chain';
import { Loader } from 'ui';
import TaskDetailsPage from '../components/TaskDetailsPage/TaskDetailsPage';
import { SuperUserAccessLevels } from '../../Domain/Globals';
import type { RemoteTaskInfoModel, IRemoteTaskQueueApi } from '../api/RemoteTaskQueueApi';
import { withRemoteTaskQueueApi } from '../api/RemoteTaskQueueApiInjection';
import { takeLastAndRejectPrevious } from './PromiseUtils';
import { getCurrentUserInfo } from '../../Domain/Globals';
import type { RouterLocationDescriptor } from '../../Commons/DataTypes/Routing';


type TaskDetailsPageContainerProps = {
    id: string;
    remoteTaskQueueApi: IRemoteTaskQueueApi;
    parentLocation: ?RouterLocationDescriptor;
};

type TaskDetailsPageContainerState = {
    taskDetails: ?RemoteTaskInfoModel;
    loading: boolean;
};

class TaskDetailsPageContainer extends React.Component {
    props: TaskDetailsPageContainerProps;
    state: TaskDetailsPageContainerState = {
        loading: false,
        taskDetails: null,
    };
    getTaskDetails = takeLastAndRejectPrevious(
        this.props.remoteTaskQueueApi.getTaskDetails.bind(this.props.remoteTaskQueueApi)
    );

    componentWillMount() {
        this.loadData(this.props.id);
    }

    componentWillReceiveProps(nextProps: TaskDetailsPageContainerProps) {
        if (this.props.id !== nextProps.id) {
            this.loadData(nextProps.id);
        }
    }

    async loadData(id: string): Promise<void> {
        this.setState({ loading: true });
        try {
            const taskDetails = await this.getTaskDetails(id);
            this.setState({ taskDetails: taskDetails });
        }
        finally {
            this.setState({ loading: false });
        }
    }

    async handlerRerun(): Promise<void> {
        const { remoteTaskQueueApi, id } = this.props;
        this.setState({ loading: true });
        try {
            await remoteTaskQueueApi.rerunTasks([id]);
            const taskDetails = await this.getTaskDetails(id);
            this.setState({ taskDetails: taskDetails });
        }
        finally {
            this.setState({ loading: false });
        }
    }

    async handlerCancel(): Promise<void> {
        const { remoteTaskQueueApi, id } = this.props;
        this.setState({ loading: true });
        try {
            await remoteTaskQueueApi.cancelTasks([id]);
            const taskDetails = await this.getTaskDetails(id);
            this.setState({ taskDetails: taskDetails });
        }
        finally {
            this.setState({ loading: false });
        }
    }

    getDefaultParetnLocation(): RouterLocationDescriptor {
        return {
            pathname: '/AdminTools/Tasks',
        };
    }

    render(): React.Element<*> {
        const { taskDetails, loading } = this.state;
        const { parentLocation } = this.props;
        const currentUser = getCurrentUserInfo();

        return (
            <Loader active={loading} type='big'>
                {taskDetails && (
                    <TaskDetailsPage
                        parentLocation={parentLocation || this.getDefaultParetnLocation()}
                        allowRerunOrCancel={
                            $c(currentUser)
                                .with(x => x.superUserAccessLevel)
                                .with(x => [SuperUserAccessLevels.God, SuperUserAccessLevels.Developer].includes(x))
                                .return(false)}
                        taskDetails={taskDetails}
                        loading={false}
                        actionsOnTaskResult={null}
                        error={''}
                        onRerun={() => this.handlerRerun()}
                        onCancel={() => this.handlerCancel()}
                    />
                )}
            </Loader>
        );
    }
}

export default withRemoteTaskQueueApi(TaskDetailsPageContainer);
