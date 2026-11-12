using System.Web.Mvc;

namespace WebApplication1.Controllers
{
    public class BaoCaoController : Controller
    {
        public ActionResult Index()
        {
            if (Session["userID"] == null)
            {
                return RedirectToAction("Index", "Login");
            }

            return View();
        }
    }
}