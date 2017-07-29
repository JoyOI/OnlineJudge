using System.Collections;

namespace JoyOI.OnlineJudge.WebApi.Models
{
    public class PagedResult<T>
        where T : IEnumerable
    {
        public int current { get; set; }

        public int total { get; set; }

        public int size { get; set; }

        public T result { get; set; }

        public int count { get; set; }
    }
}
