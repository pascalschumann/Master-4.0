namespace Master40.DB.Enums
{
    public enum State
    {
        Created,
        Injected,
        ProviderExist,
        BackwardScheduleExists,
        ForwardScheduleExists,
        ExistsInCapacityPlan,
        Producing,
        Finished // 7
    }

    public enum ProducingState
    {
        Created,
        Waiting,
        Producing,
        Finished // 3
    }
}
