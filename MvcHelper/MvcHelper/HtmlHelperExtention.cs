using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Web.Mvc.Html;

namespace System.Web.Mvc.Html
{
    public enum DefaultAction
    {
        Other,
        Create,
        Edit,
        Details,
        Delete,
        Index,
    }

    public static class HtmlHelperExtention
    {
        public static MvcHtmlString ValidationSummaryForTbAlert(this HtmlHelper html)
        {
            var errors = html.ViewData.ModelState.SelectMany(c => c.Value.Errors).OfType<ModelError>();

            if(errors.Count() == 0)
                return MvcHtmlString.Empty;
            return html.Partial("Parts/ValidateAlerts",errors);
            throw new Exception();
        }

        public static string GetActionName(this HtmlHelper html)
        {
            return html.ViewContext.RouteData.Values["action"].ToString();
        }

        public static bool IsAction(this HtmlHelper html, DefaultAction action)
        {
            DefaultAction result = GetDefaultAction(html, action);
            return action == result;
        }
        public static bool IsAction(this HtmlHelper html, string name)
        {
            var actionName = html.GetActionName();
            return string.Compare(actionName, name,true) == 0;
        }

        private static DefaultAction GetDefaultAction(HtmlHelper html, DefaultAction action)
        {
            var actionName = html.GetActionName();
            DefaultAction result;
            if (!Enum.TryParse<DefaultAction>(actionName, true, out result))
            {
                result = DefaultAction.Other;
            }
            return result;
        }

    }
}
