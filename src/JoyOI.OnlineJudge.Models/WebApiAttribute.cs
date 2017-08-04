using System;

namespace JoyOI.OnlineJudge.Models
{
    public enum FilterLevel
    {
        ReadOnly,
        CouldNotPatch,
        GetHidden,
        GetEnumerateHidden,
        GetSingleHidden
    }

    public class WebApiAttribute : Attribute
    {
        public FilterLevel Level { get; private set; }

        public WebApiAttribute(FilterLevel level)
        {
            Level = level;
        }
    }
}
