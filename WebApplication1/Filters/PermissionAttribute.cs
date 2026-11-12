using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace WebApplication1.Filters
{
    public class PermissionAttribute : ActionFilterAttribute
    {
        private readonly string _permission;

        public PermissionAttribute(string permission)
        {
            _permission = permission;
        }

        public override void OnActionExecuting(
            ActionExecutingContext filterContext)
        {
            if (filterContext.HttpContext.Session["userID"] == null)
            {
                filterContext.Result =
                    new RedirectToRouteResult(
                        new System.Web.Routing.RouteValueDictionary
                        {
                            { "controller", "Login" },
                            { "action", "Index" }
                        });

                return;
            }

            var permissions =
                filterContext.HttpContext.Session["Permissions"]
                as List<string>;

            if (permissions == null ||
                !permissions.Contains(_permission))
            {
                filterContext.Result =
                    new HttpUnauthorizedResult();

                return;
            }

            base.OnActionExecuting(filterContext);
        }
    }
}
