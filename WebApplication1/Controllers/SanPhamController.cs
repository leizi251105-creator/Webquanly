
using System.Linq;
using System.Web.Mvc;
using WebApplication1.Models;
using WebApplication1.Models.ViewModels;

namespace WebApplication1.Controllers
{
    public class SanPhamController : Controller
    {
        // Trang danh sách sản phẩm
        public ActionResult Index(string tuKhoa, int? maDanhMuc)
        {
            using (QUANLYEntities1 db = new QUANLYEntities1())
            {
                var query = db.Thuocs
                    .Where(x => x.TrangThai == true);

                // Tìm kiếm theo tên thuốc
                if (!string.IsNullOrWhiteSpace(tuKhoa))
                {
                    tuKhoa = tuKhoa.Trim();

                    query = query.Where(x =>
                        x.TenThuoc.Contains(tuKhoa));
                }

                // Lọc theo danh mục
                if (maDanhMuc.HasValue)
                {
                    query = query.Where(x =>
                        x.MaDanhMuc == maDanhMuc.Value);
                }

                var model = new SanPhamViewModel
                {
                    DanhSachThuoc = query
                        .OrderBy(x => x.TenThuoc)
                        .ToList(),

                    DanhSachDanhMuc = db.DanhMucThuocs
                        .Where(x => x.TrangThai == true)
                        .OrderBy(x => x.MaDanhMuc)
                        .ToList(),

                    TuKhoa = tuKhoa,
                    MaDanhMuc = maDanhMuc
                };

                return View(model);
            }
        }

        // Trang chi tiết sản phẩm
        public ActionResult Details(int? id)
        {
            if (id == null)
                return RedirectToAction("Index");

            using (QUANLYEntities1 db = new QUANLYEntities1())
            {
                var thuoc = db.Thuocs.FirstOrDefault(x =>
                    x.MaThuoc == id.Value && x.TrangThai == true);

                if (thuoc == null)
                    return HttpNotFound("Không tìm thấy sản phẩm.");

                // Lấy tên danh mục thuốc
                ViewBag.TenDanhMuc = db.DanhMucThuocs
                    .Where(x => x.MaDanhMuc == thuoc.MaDanhMuc)
                    .Select(x => x.TenDanhMuc)
                    .FirstOrDefault();

                return View(thuoc);
            }
        }

    }
}
