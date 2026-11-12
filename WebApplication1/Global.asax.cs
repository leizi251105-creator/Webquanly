
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace WebApplication1
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            // Đặt tên cookie Anti-Forgery riêng cho dự án
            System.Web.Helpers.AntiForgeryConfig.CookieName =
                "__RequestVerificationToken_WebApplication1";

            AreaRegistration.RegisterAllAreas();
            RouteConfig.RegisterRoutes(RouteTable.Routes);
        }
    }
}
