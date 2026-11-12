
using System.Linq;
using System.Web.Mvc;
using System.Data.Entity;
using WebApplication1.Models;
using WebApplication1.Models.ViewModels;

namespace WebApplication1.Controllers
{
    public class DonHangController : Controller
    {
        // Hiển thị trang lịch sử đơn hàng
        [HttpGet]
        public ActionResult LichSu()
        {
            return View(new LichSuDonHangViewModel());
        }

        // Tìm kiếm đơn hàng theo số điện thoại
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LichSu(LichSuDonHangViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            using (QUANLYEntities1 db = new QUANLYEntities1())
            {
                string soDienThoai = model.SoDienThoai.Trim();

                model.DanhSachDonHang = db.DonHangs
                    .Include(x => x.KhachHang)
                    .Where(x =>
                        x.KhachHang != null &&
                        x.KhachHang.SoDienThoai == soDienThoai
                    )
                    .OrderByDescending(x => x.NgayDat)
                    .ToList();

                if (model.DanhSachDonHang.Count == 0)
                {
                    ModelState.AddModelError(
                        "",
                        "Không tìm thấy đơn hàng nào với số điện thoại này."
                    );
                }

                return View(model);
            }
        }


        // Xem chi tiết đơn hàng
        [HttpGet]
        public ActionResult Details(int? id, string soDienThoai)
        {
            if (id == null || string.IsNullOrWhiteSpace(soDienThoai))
                return RedirectToAction("LichSu");

            using (QUANLYEntities1 db = new QUANLYEntities1())
            {
                string sdt = soDienThoai.Trim();

                var donHang = db.DonHangs
                    .Include(x => x.KhachHang)
                    .Include(x => x.ChiTietDonHangs.Select(ct => ct.Thuoc))
                    .FirstOrDefault(x =>
                        x.MaDonHang == id.Value &&
                        x.KhachHang != null &&
                        x.KhachHang.SoDienThoai == sdt
                    );

                if (donHang == null)
                    return HttpNotFound("Không tìm thấy đơn hàng.");

                return View(donHang);
            }
        }

    }
}
