using System;
using System.Collections.Generic;
using System.Text;

namespace Saftbot.NET
{
    /// <summary>
    /// The 16 possible bools that are unique to each server as settings
    /// </summary>
    public enum ServerSettings
    {
        plebsCanDJ,
        useGoogle
    }

    /// <summary>
    /// The 8 bools that are unique settings for each user
    /// </summary>
    public enum UserSettings
    {
        isAdmin,
        isDJ
    }
}
