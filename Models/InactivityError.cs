using System.ComponentModel;

namespace InactivityBot.Models
{
    public enum InactivityError
    {
        [Description("The channel to post the Inactivity Message was not found")]
        MissingChannel = 0,

        [Description("I have no idea what the error is/the error is not defined in this enum")]
        Misc = 9999
    }
}
