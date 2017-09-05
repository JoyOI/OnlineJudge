using System;

namespace JoyOI.OnlineJudge.Models
{
    [Flags]
    public enum FilterLevel
    {
        GetListDisabled = 1,
        GetSingleDisabled = 2,
        GetNeedRoot = 4,
        PutDisabled = 8,
        PatchDisabled = 16,
        GetNeedOwner = 32
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
