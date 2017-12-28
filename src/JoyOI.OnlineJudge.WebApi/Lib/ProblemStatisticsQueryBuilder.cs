using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using JoyOI.OnlineJudge.Models;
using JoyOI.OnlineJudge.WebApi.Models;
using MySql.Data.MySqlClient;

namespace JoyOI.OnlineJudge.WebApi.Lib
{
    public static class ProblemStatisticsQueryBuilder
    {
        public static async Task<IDictionary<string, ProblemStatisticsValue>> GenerateAsync(OnlineJudgeContext db, string groupId, IEnumerable<string> problemIds, CancellationToken token)
        {
            var ret = new Dictionary<string, ProblemStatisticsValue>();
            var pid = $"({string.Join(',', problemIds.Select(x => $"'{x}'"))})";
            var sql = $"SELECT `ProblemId`, `Result` = 0 AS `IsAccepted`, Sum(1) AS `Count` FROM `JudgeStatuses` WHERE `ProblemId` IN {pid} AND `GroupId` = '{groupId}' GROUP BY `ProblemId`, `Result` = 0";
            using (MySqlConnection conn = (MySqlConnection)db.Database.GetDbConnection())
            {
                try { await conn.OpenAsync(token); }
                catch { }
                using (var cmd = new MySqlCommand(sql, conn))
                using (var reader = await cmd.ExecuteReaderAsync(token))
                {
                    while (await reader.ReadAsync(token))
                    {
                        var problemId = reader.GetString(0);
                        if (!ret.ContainsKey(problemId))
                        {
                            ret.Add(problemId, new ProblemStatisticsValue());
                        }

                        if (reader.GetInt32(1) == 1)
                        {
                            ret[problemId].Accepted = reader.GetInt32(2);
                        }
                        else
                        {
                            ret[problemId].NonAccepted = reader.GetInt32(2);
                        }
                    }
                }
            }
            return ret;
        }
    }
}
