using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web.Helpers;
using RB = Microsoft.CSharp.RuntimeBinder;

namespace MvcHelper
{
    public static class QueryHelper
    {
        static public IQueryable<T> Page<T>(this IQueryable<T> sorce, int pageIndex, int rowsPerPage = 10)
        {
            if (pageIndex == -1)
                return sorce;

            return sorce.Skip(pageIndex * rowsPerPage).Take(rowsPerPage);
        }

        static public IQueryable<T> Sort<T>(this IQueryable<T> data, string sortColumn, SortDirection sortDirection)
        {
            var elementType = typeof(T);
            if (typeof(IDynamicMetaObjectProvider).IsAssignableFrom(elementType))
            {
                // IDynamicMetaObjectProvider properties are only available through a runtime binder, so we
                // must build a custom LINQ expression for getting the dynamic property value.
                // Lambda: o => o.Property (where Property is obtained by runtime binder)
                // NOTE: lambda must not use internals otherwise this will fail in partial trust when Helpers assembly is in GAC
                var binder = RB.Binder.GetMember(RB.CSharpBinderFlags.None, sortColumn, typeof(object), new RB.CSharpArgumentInfo[] {
                        RB.CSharpArgumentInfo.Create(RB.CSharpArgumentInfoFlags.None, null) });
                var param = System.Linq.Expressions.Expression.Parameter(typeof(IDynamicMetaObjectProvider), "o");
                var getter = System.Linq.Expressions.Expression.Dynamic(binder, typeof(object), param);
                return SortGenericExpression<T, object>((IQueryable<T>)data, getter, param, sortDirection);
            }
            else
            {
                // The IQueryable<dynamic> data source is cast as IQueryable<object> at runtime. We must call
                // SortGenericExpression using reflection so that the LINQ expressions use the actual element type.
                // Lambda: o => o.Property[.NavigationProperty,etc]
                var param = System.Linq.Expressions.Expression.Parameter(elementType, "o");
                System.Linq.Expressions.Expression member = param;
                var type = elementType;
                var sorts = sortColumn.Split('.');
                foreach (var name in sorts)
                {
                    PropertyInfo prop = type.GetProperty(name);
                    if (prop == null)
                    {
                        // no-op in case navigation property came from querystring (falls back to default sort)
                        return data;
                    }
                    member = Expression.Property(member, prop);
                    type = prop.PropertyType;
                }
                MethodInfo m = typeof(QueryHelper).GetMethod("SortGenericExpression", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
                m = m.MakeGenericMethod(typeof(T), member.Type);
                return (IQueryable<T>)m.Invoke(null, new object[] { data, member, param, sortDirection });
            }
        }

        static IQueryable<T> SortGenericExpression<T, TProperty>(IQueryable<T> data, System.Linq.Expressions.Expression body,
        ParameterExpression param, System.Web.Helpers.SortDirection sortDirection)
        {

            Debug.Assert(data != null);
            Debug.Assert(body != null);
            Debug.Assert(param != null);

            // The IQueryable<dynamic> data source is cast as an IQueryable<object> at runtime.  We must cast
            // this to an IQueryable<TElement> so that the reflection done by the LINQ expressions will work.
            //IQueryable<T_> data2 = data.Cast<T_>();
            Expression<Func<T, TProperty>> lambda = Expression.Lambda<Func<T, TProperty>>(body, param);
            if (sortDirection == System.Web.Helpers.SortDirection.Descending)
            {
                return data.OrderByDescending(lambda);
            }
            else
            {
                return data.OrderBy(lambda);
            }
        }
    }
}
