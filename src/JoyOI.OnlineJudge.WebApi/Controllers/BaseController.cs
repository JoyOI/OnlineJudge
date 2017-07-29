using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JoyOI.OnlineJudge.Models;
using JoyOI.OnlineJudge.WebApi.Models;
using Newtonsoft.Json;

namespace JoyOI.OnlineJudge.WebApi.Controllers
{
    public class BaseController : BaseController<OnlineJudgeContext, User, Guid>
    {
        public static Expression<Func<T, bool>> ContainsWhere<T>(Expression<Func<T, string>> propSelector, IEnumerable<string> matches)
        {
            if (matches == null || matches.Count() < 1)
            {
                throw new ArgumentException();
            }
            Expression body = MakeContains(propSelector.Body, Expression.Constant(matches.ElementAt(0)));
            for (var i = 1; i < matches.Count(); i++)
            {
                body = Expression.Or(body, MakeContains(propSelector.Body, Expression.Constant(matches.ElementAt(i))));
            }
            return Expression.Lambda<Func<T, bool>>(body, propSelector.Parameters);
        }

        public static MethodCallExpression MakeContains(Expression expression, Expression parameter)
        {
            var containsMethod = typeof(string).GetTypeInfo().GetMethod("Contains", new Type[] { typeof(string) });
            return Expression.Call(expression, containsMethod, parameter);
        }

        public async Task<ApiResult<PagedResult<IEnumerable<T>>>> Paged<T>(IQueryable<T> src, int currentPage, int size = 100, CancellationToken token = default(CancellationToken))
        {
            var total = src.Count();
            var result = await src.Skip((currentPage - 1) * size).Take(size).ToListAsync(token);
            return new ApiResult<PagedResult<IEnumerable<T>>>
            {
                code = 200,
                msg = "",
                data = new PagedResult<IEnumerable<T>>
                {
                    count = result.Count,
                    current = currentPage,
                    size = size,
                    result = result,
                    total = total
                }
            };
        }

        public Task<ApiResult<T>> Result<T>(T result, int code=  200)
        {
            return Task.FromResult(new ApiResult<T> { code = code, data = result });
        }

        public Task<ApiResult<T>> Result<T>(int code, string msg)
        {
            return Task.FromResult(new ApiResult<T> { code = code, msg = msg });
        }

        public Task<ApiResult> Result(int code, string msg)
        {
            return Task.FromResult(new ApiResult { code = code, msg = msg });
        }

        public void PatchEntity<T>(string json, T entity)
        {
            var type = typeof(T);
            var jsonToObject = JsonConvert.DeserializeObject<T>(json);
            var jsonToDictionary = JsonConvert.DeserializeObject<IDictionary<string, object>>(json);
            var keys = jsonToDictionary.Keys.Where(x => x.ToLower() != "id");
            foreach (var x in keys)
            {
                var propertyInfo = type.GetProperties().SingleOrDefault(y => y.Name.ToLower() == x.ToLower());
                if (propertyInfo == null)
                {
                    continue;
                }

                var value = propertyInfo.GetValue(jsonToObject);
                propertyInfo.SetValue(entity, value);
            }
        }
    }
}
