
using System;
using System.Linq;
using System.Text;
using System.Web.Mvc;

namespace WebApplication1.Controllers
{
    public class TestConnectionController : Controller
    {
        public ActionResult Index()
        {
            var assembly = typeof(MOCBREWEntities).Assembly;

            var resources = assembly.GetManifestResourceNames();

            var result = new StringBuilder();

            result.AppendLine("DANH SACH RESOURCE TRONG DLL:");
            result.AppendLine();

            foreach (var resource in resources)
            {
                if (resource.EndsWith(".csdl") ||
                    resource.EndsWith(".ssdl") ||
                    resource.EndsWith(".msl"))
                {
                    result.AppendLine(resource);
                }
            }

            return Content(result.ToString(), "text/plain");
        }
    }
}
