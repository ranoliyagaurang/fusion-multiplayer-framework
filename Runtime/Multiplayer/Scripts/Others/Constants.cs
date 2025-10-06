using System.Globalization;

namespace PTTI_Multiplayer
{
    public class Constants
    {
        public static string ToKMB(double coins)
        {
            if (coins > 999999999999)
                return coins.ToString("0,,,,.##T", CultureInfo.InvariantCulture);
            else if (coins > 999999999)
                return coins.ToString("0,,,.##B", CultureInfo.InvariantCulture);
            else if (coins > 999999)
                return coins.ToString("0,,.##M", CultureInfo.InvariantCulture);
            else if (coins > 9999)
                return coins.ToString("0,.#K", CultureInfo.InvariantCulture);
            else
                return coins.ToString("N0", CultureInfo.InvariantCulture);
        }

        public class PlayerPrefs
        {
            public const string welcome = "Welcome";
            public const string rememberMe = "RememberMe";
            public const string email = "Email";
            public const string password = "Password";
            public const string stepsCompleted = "StepsCompleted";
        }

        public class StringFormat
        {
            public const string windSpeed = "{0}<sup>mph</sup>";
            public const string distance = "{0}.<sup>ft</sup><size=45>{1}</size>";
            public const string weight = "{0}<sup>klb</sup>.<size=45>{1}</size>";
            public const string angle = "{0}<sup>'</sup>.<size=75>{1}</size>";
        }

        public class PlayerKey
        {
            public const string playerItems = "playerItems";
            public const string profile = "profile";
        }

        public class Network
        {
            public const int pingDelay = 2;
            public const int studentsCount = 99;
        }
    }
}