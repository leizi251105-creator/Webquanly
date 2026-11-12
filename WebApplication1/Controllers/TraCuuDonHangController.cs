
using System.Linq;
using System.Web.Mvc;
using System.Data.Entity;
using WebApplication1.Models;
using WebApplication1.Models.ViewModels;

namespace WebApplication1.Controllers
{
    public class TraCuuDonHangController : Controller
    {
        // Hiển thị trang tra cứu
        [HttpGet]
        public ActionResult Index()
        {
            return View(new TraCuuDonHangViewModel());
        }

        // Xử lý tra cứu đơn hàng
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Index(TraCuuDonHangViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Chuyển mã DH00001 thành số 1
            string maDon = model.MaDonHang.Trim().ToUpper();

            if (maDon.StartsWith("DH"))
                maDon = maDon.Substring(2);

            int maDonHang;

            if (!int.TryParse(maDon, out maDonHang))
            {
                ModelState.AddModelError(
                    "MaDonHang",
                    "Mã đơn hàng không đúng định dạng."
                );

                return View(model);
            }

            using (QUANLYEntities1 db = new QUANLYEntities1())
            {
                var donHang = db.DonHangs
                    .Include(x => x.KhachHang)
                    .Include(x => x.ChiTietDonHangs.Select(ct => ct.Thuoc))
                    .FirstOrDefault(x =>
                        x.MaDonHang == maDonHang &&
                        x.KhachHang.SoDienThoai == model.SoDienThoai.Trim()
                    );

                if (donHang == null)
                {
                    ModelState.AddModelError(
                        "",
                        "Không tìm thấy đơn hàng. Vui lòng kiểm tra lại mã đơn và số điện thoại."
                    );

                    return View(model);
                }

                model.KetQua = donHang;

                return View(model);
            }
        }
    }
}
