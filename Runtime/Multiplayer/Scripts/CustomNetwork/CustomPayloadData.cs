using System;

namespace HVAC
{
    [Serializable]
    public class CustomPayloadData
    {
        public string playerName;
        public string className;
        public string regionName;
        public string avatarURL;
        public PlayerMode playerMode;
        public bool isCreateRoom;
    }
}