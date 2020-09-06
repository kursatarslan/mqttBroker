namespace MqttBroker.Enums
{
    public enum Maneuver
    {
        Unavailable = 0,
        JoinRequest,
        JoinAccepted,
        JoinRehected,
        SplitRequest,
        MergeRequest,
        ManueverCompleted,
        CreatePlatoon
    }
}