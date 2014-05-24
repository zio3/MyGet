using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Collections;
using System.Linq.Expressions;
using System.Globalization;

namespace System.Web.Mvc.Html
{

    public static class ListExtensions
    {
        public static MvcHtmlString ListFor<T, TProperty>(this HtmlHelper<T> html, Expression<Func<T, TProperty>> expression)
        {
            var genArgs = typeof(TProperty).GetGenericArguments();
            return ListForSub(html, genArgs, expression.Compile().Invoke(html.ViewData.Model));
        }

        private static MvcHtmlString ListForSub(HtmlHelper html, Type[] genArgs, object data)
        {
            if (genArgs.Length >= 1)
            {
                //var vt = html.ViewContext.View as IViewProxy;
                var fullViewName = "ListTamplates/" + genArgs[0].Name;
                ViewEngineResult viewEngineResult = ViewEngines.Engines.FindPartialView(html.ViewContext, fullViewName);

                if (viewEngineResult != null)
                {
                    using (StringWriter writer = new StringWriter(CultureInfo.InvariantCulture))
                    {
                        viewEngineResult.View.Render(html.ViewContext, writer);
                        return new MvcHtmlString(writer.ToString());
                    }
                }
                else
                {
                    return html.Partial("ListTamplates/CommonList");
                }
            }
            throw new ArgumentException("ModelにIQueryable型が必要です");
        }

        public static MvcHtmlString ListForModel(this HtmlHelper html)
        {
            var genArgs = html.ViewData.Model.GetType().GetGenericArguments();
            return ListForSub(html, genArgs, html.ViewData.Model);
        }
    }
}
