
using System.Linq;
using System.Web.Mvc;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    public class BanHangController : Controller
    {
        public ActionResult Index()
        {
            if (Session["userID"] == null)
                return RedirectToAction("Index", "Login");

            var danhSachVaiTro =
                Session["roles"] as System.Collections.Generic.List<string>;

            bool coQuyen = danhSachVaiTro != null &&
                (danhSachVaiTro.Contains("Admin") ||
                 danhSachVaiTro.Contains("NhanVienBanHang"));

            if (!coQuyen)
                return new HttpStatusCodeResult(403);

            using (QUANLYEntities1 db = new QUANLYEntities1())
            {
                ViewBag.TongDonHang = db.DonHangs.Count();

                ViewBag.DonChoXacNhan = db.DonHangs.Count(
                    x => x.TrangThai == "Chờ xác nhận");

                ViewBag.DonDaXacNhan = db.DonHangs.Count(
                    x => x.TrangThai == "Đã xác nhận");

                ViewBag.DonDaGiao = db.DonHangs.Count(
                    x => x.TrangThai == "Đã giao hàng");

                ViewBag.TenNhanVien = Session["fullName"];

                return View();
            }
        }
    }
}
