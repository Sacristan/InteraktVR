namespace VRInteraction
{
    public static class GlobalKeys
    {
        public const string KEY_NONE = "NONE";

        //ACTIONS
        public const string KEY_ACTION = "ACTION";
        public const string KEY_TELEPORT = "TELEPORT";

        //CAUSES
        public const string KEY_PICKUP = "PICKUP";
        public const string KEY_PICKUP_DROP = "PICKUP_DROP";
        public const string KEY_INPUT_RECEIVED = "InputReceived";
        public const string KEY_DROP = "Drop";

        public static readonly string[] VR_ACTIONS_ARRAY = new string[] { KEY_NONE, KEY_ACTION, KEY_PICKUP_DROP, KEY_TELEPORT };
    }

}