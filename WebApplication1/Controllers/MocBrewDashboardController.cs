
using System.Web.Mvc;

namespace WebApplication1.Controllers
{
    public class MocBrewDashboardController : Controller
    {
        [HttpGet]
        public ActionResult Index()
        {
            // Kiểm tra tài khoản đã đăng nhập MỘC BREW chưa
            if (Session["MOCBREW_AccountId"] == null)
            {
                return RedirectToAction("Login", "MocBrewAuth");
            }

            // Lấy thông tin người dùng từ Session
            ViewBag.AccountId = Session["MOCBREW_AccountId"];
            ViewBag.Username = Session["MOCBREW_Username"];
            ViewBag.FullName = Session["MOCBREW_FullName"];
            ViewBag.RoleId = Session["MOCBREW_RoleId"];
            ViewBag.RoleName = Session["MOCBREW_RoleName"];

            return View();
        }
    }
}
