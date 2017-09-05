using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JoyOI.ManagementService.SDK;
using JoyOI.OnlineJudge.Models;
using JoyOI.OnlineJudge.WebApi.Lib;
using JoyOI.OnlineJudge.WebApi.Models;
using Newtonsoft.Json;

namespace JoyOI.OnlineJudge.WebApi.Controllers
{
    public class BaseController : BaseController<OnlineJudgeContext, User, Guid>
    {
        public virtual bool IsRoot
        {
            get
            {
                if (!_isRoot.HasValue)
                {
                    if (User.Current == null)
                    {
                        _isRoot = false;
                    }
                    else
                    {
                        _isRoot = User.Manager.IsInRoleAsync(User.Current, "Root").Result;
                    }
                }

                return _isRoot.Value;
            }
        }
        
        // [Inject]
        public IcM IcM { get; set; }

        public virtual bool HasOwnership { get; set; }

        private bool? _isRoot;

        [Inject]
        public ManagementServiceClient ManagementService { get; set; }

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
            var type = typeof(T);
            var webapiAttributedProperty = type.GetProperties().Where(x => x.GetCustomAttribute<WebApiAttribute>() != null);
            foreach (var x in result)
            {
                foreach (var y in webapiAttributedProperty)
                {
                    var level = y.GetCustomAttribute<WebApiAttribute>().Level;
                    if (level.HasFlag(FilterLevel.GetListDisabled))
                    {
                        y.SetValue(x, y.PropertyType.IsValueType ? Activator.CreateInstance(y.PropertyType) : null);
                    }
                    else if (level.HasFlag(FilterLevel.GetNeedRoot) && !IsRoot)
                    {
                        y.SetValue(x, y.PropertyType.IsValueType ? Activator.CreateInstance(y.PropertyType) : null);
                    }
                    else if (level.HasFlag(FilterLevel.GetNeedOwner) && !HasOwnership)
                    {
                        y.SetValue(x, y.PropertyType.IsValueType ? Activator.CreateInstance(y.PropertyType) : null);
                    }
                }
            }
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

        public ApiResult<T> Result<T>(T result, int code=  200)
        {
            Response.StatusCode = code;
            var type = typeof(T);
            var webapiAttributedProperties = type.GetProperties().Where(x => x.GetCustomAttribute<WebApiAttribute>() != null);
            foreach (var x in webapiAttributedProperties)
            {
                var level = x.GetCustomAttribute<WebApiAttribute>().Level;
                if (level.HasFlag(FilterLevel.GetSingleDisabled))
                {
                    x.SetValue(result, x.PropertyType.IsValueType ? Activator.CreateInstance(x.PropertyType) : null);
                }
                else if (level.HasFlag(FilterLevel.GetNeedRoot) && !IsRoot)
                {
                    x.SetValue(result, x.PropertyType.IsValueType ? Activator.CreateInstance(x.PropertyType) : null);
                }
            }
            return new ApiResult<T> { code = code, data = result };
        }

        public ApiResult<T> Result<T>(int code, string msg)
        {
            Response.StatusCode = code;
            return new ApiResult<T> { code = code, msg = msg };
        }

        public ApiResult Result(int code, string msg)
        {
            Response.StatusCode = code;
            return new ApiResult { code = code, msg = msg };
        }

        public IEnumerable<string> PatchEntity<T>(T entity, string json)
        {
            var type = entity.GetType();
            var properties = type.GetProperties();
            var jsonToObject = JsonConvert.DeserializeObject<T>(json);
            var jsonToDictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            var ret = new List<string>();
            foreach (var x in jsonToDictionary.Keys)
            {
                var property = properties.SingleOrDefault(y => y.Name.ToLower() == x.ToLower());

                if (property == null)
                    continue;

                var webapiAttribute = property.GetCustomAttribute<WebApiAttribute>();
                if (webapiAttribute != null && webapiAttribute.Level.HasFlag(FilterLevel.PatchDisabled))
                    continue;

                if (!property.GetValue(jsonToObject).Equals(property.GetValue(entity)))
                {
                    property.SetValue(entity, property.GetValue(jsonToObject));
                    ret.Add(property.Name);
                }
            }

            return ret;
        }

        public (T Entity, IEnumerable<string> Fields) PutEntity<T>(string json)
        {
            var entity = Activator.CreateInstance<T>();
            var type = typeof(T);
            var properties = type.GetProperties();
            var jsonToObject = JsonConvert.DeserializeObject<T>(json);
            var jsonToDictionary= JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            var ret = new List<string>();
            foreach (var x in jsonToDictionary.Keys)
            {
                var property = properties.SingleOrDefault(y => y.Name.ToLower() == x.ToLower());

                if (property == null)
                    continue;

                var webapiAttribute = property.GetCustomAttribute<WebApiAttribute>();
                if (webapiAttribute != null && webapiAttribute.Level == FilterLevel.PutDisabled)
                    continue;

                property.SetValue(entity, property.GetValue(jsonToObject));
                ret.Add(property.Name);
            }
            return (entity, ret);
        }

        public void FilterEntity<T>(T entity)
        {
            var type = typeof(T);
            foreach (var x in type.GetProperties().Where(x => x.GetCustomAttribute<WebApiAttribute>() != null))
            {
                var level = x.GetCustomAttribute<WebApiAttribute>().Level;
                if (level.HasFlag(FilterLevel.GetSingleDisabled))
                {
                    x.SetValue(entity, x.PropertyType.IsValueType ? Activator.CreateInstance(x.PropertyType) : null);
                }
                else if (level.HasFlag(FilterLevel.GetNeedRoot) && !IsRoot)
                {
                    x.SetValue(entity, x.PropertyType.IsValueType ? Activator.CreateInstance(x.PropertyType) : null);
                }
                else if (level.HasFlag(FilterLevel.GetNeedOwner) && !HasOwnership)
                {
                    x.SetValue(entity, x.PropertyType.IsValueType ? Activator.CreateInstance(x.PropertyType) : null);
                }
            }
        }
    }
}
