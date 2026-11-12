using System.Web.Mvc;

namespace WebApplication1.Controllers
{
    public class AdminController : Controller
    {
        private bool IsAdmin()
        {
            if (Session["userID"] == null)
                return false;

            if (Session["role"] == null)
                return false;

            return Session["role"].ToString() == "Admin";
        }

        // GET: Admin
        public ActionResult Index()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Index", "Login");
            }

            return View();
        }
    }
}