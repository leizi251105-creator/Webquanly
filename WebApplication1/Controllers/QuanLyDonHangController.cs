
using System;
using System.Linq;
using System.Web.Mvc;
using System.Data.Entity;
using WebApplication1.Models;
using WebApplication1.Models.ViewModels;

namespace WebApplication1.Controllers
{
    public class QuanLyDonHangController : Controller
    {
        private bool CoQuyenQuanLyDonHang()
        {
            if (Session["userID"] == null)
                return false;

            var danhSachVaiTro = Session["roles"] as System.Collections.Generic.List<string>;

            if (danhSachVaiTro != null)
            {
                return danhSachVaiTro.Contains("Admin")
                    || danhSachVaiTro.Contains("NhanVienBanHang");
            }

            string vaiTro = Session["role"] as string;

            return vaiTro == "Admin"
                || vaiTro == "NhanVienBanHang";
        }
        // Danh sách và tìm kiếm đơn hàng
        public ActionResult Index(
            string tuKhoa,
            string trangThai,
            DateTime? tuNgay,
            DateTime? denNgay)
        {
            // Kiểm tra đăng nhập
            if (Session["userID"] == null)
                return RedirectToAction("Index", "Login");

            using (QUANLYEntities1 db = new QUANLYEntities1())
            {
                var query = db.DonHangs
                    .Include(x => x.KhachHang)
                    .AsQueryable();

                // Tìm kiếm theo mã đơn, tên khách hàng hoặc số điện thoại
                if (!string.IsNullOrWhiteSpace(tuKhoa))
                {
                    tuKhoa = tuKhoa.Trim();

                    int maDonHang;
                    bool laMaDon = int.TryParse(tuKhoa, out maDonHang);

                    query = query.Where(x =>
                        (laMaDon && x.MaDonHang == maDonHang) ||
                        (x.KhachHang != null &&
                         (x.KhachHang.HoTen.Contains(tuKhoa) ||
                          x.KhachHang.SoDienThoai.Contains(tuKhoa)))
                    );
                }

                // Lọc trạng thái
                if (!string.IsNullOrWhiteSpace(trangThai))
                {
                    query = query.Where(x => x.TrangThai == trangThai);
                }

                // Lọc từ ngày
                if (tuNgay.HasValue)
                {
                    query = query.Where(x => x.NgayDat >= tuNgay.Value);
                }

                // Lọc đến ngày (bao gồm toàn bộ ngày được chọn)
                if (denNgay.HasValue)
                {
                    DateTime ngaySauDo = denNgay.Value.Date.AddDays(1);
                    query = query.Where(x => x.NgayDat < ngaySauDo);
                }

                // Lấy danh sách đơn hàng
                var danhSach = query
                    .OrderByDescending(x => x.NgayDat)
                    .ToList();

                // Thống kê đơn hàng
                var tatCaDonHang = db.DonHangs.ToList();

                var model = new QuanLyDonHangViewModel
                {
                    DanhSachDonHang = danhSach,

                    TuKhoa = tuKhoa,
                    TrangThai = trangThai,
                    TuNgay = tuNgay,
                    DenNgay = denNgay,

                    TongDonHang = tatCaDonHang.Count,

                    DonChoXacNhan = tatCaDonHang.Count(
                        x => x.TrangThai == "Chờ xác nhận"),

                    DonDaXacNhan = tatCaDonHang.Count(
                        x => x.TrangThai == "Đã xác nhận"),

                    DonDangGiao = tatCaDonHang.Count(
                        x => x.TrangThai == "Đang giao hàng"),

                    DonDaGiao = tatCaDonHang.Count(
                        x => x.TrangThai == "Đã giao hàng"),

                    TongDoanhThu = tatCaDonHang
                        .Where(x => x.TrangThai == "Đã giao hàng")
                        .Sum(x => (decimal?)x.TongTien) ?? 0
                };

                return View(model);
            }
        }

        [HttpGet]
        public ActionResult Details(int? id)
        {
            if (Session["userID"] == null)
                return RedirectToAction("Index", "Login");

            if (!CoQuyenQuanLyDonHang())
                return new HttpStatusCodeResult(403);

            if (id == null)
                return RedirectToAction("Index");

            using (QUANLYEntities1 db = new QUANLYEntities1())
            {
                var donHang = db.DonHangs
                    .Include(x => x.KhachHang)
                    .Include(x => x.ChiTietDonHangs.Select(ct => ct.Thuoc))
                    .FirstOrDefault(x => x.MaDonHang == id.Value);

                if (donHang == null)
                    return HttpNotFound("Không tìm thấy đơn hàng.");

                return View(donHang);
            }
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult XacNhan(int id)
        {
            if (Session["userID"] == null)
                return RedirectToAction("Index", "Login");

            if (!CoQuyenQuanLyDonHang())
                return new HttpStatusCodeResult(403);

            using (QUANLYEntities1 db = new QUANLYEntities1())
            {
                var donHang = db.DonHangs.Find(id);

                if (donHang == null)
                    return HttpNotFound("Không tìm thấy đơn hàng.");

                if (donHang.TrangThai != "Chờ xác nhận")
                {
                    TempData["ThongBaoLoi"] =
                        "Đơn hàng không ở trạng thái chờ xác nhận.";

                    return RedirectToAction("Details", new { id = id });
                }

                donHang.TrangThai = "Đã xác nhận";

                db.SaveChanges();

                TempData["ThongBao"] =
                    "Xác nhận đơn hàng thành công. Đơn hàng đang chờ đơn vị vận chuyển.";

                return RedirectToAction("Details", new { id = id });
            }
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CapNhatTrangThai(int id, string trangThaiMoi)
        {
            if (Session["userID"] == null)
                return RedirectToAction("Index", "Login");

            if (!CoQuyenQuanLyDonHang())
                return new HttpStatusCodeResult(403);

            string[] trangThaiHopLe =
            {
        "Đang giao hàng",
        "Đã giao hàng"
    };

            if (!trangThaiHopLe.Contains(trangThaiMoi))
            {
                TempData["ThongBaoLoi"] = "Trạng thái không hợp lệ.";
                return RedirectToAction("Details", new { id = id });
            }

            using (QUANLYEntities1 db = new QUANLYEntities1())
            {
                var donHang = db.DonHangs.Find(id);

                if (donHang == null)
                    return HttpNotFound("Không tìm thấy đơn hàng.");

                if (trangThaiMoi == "Đang giao hàng"
                    && donHang.TrangThai != "Đã xác nhận")
                {
                    TempData["ThongBaoLoi"] =
                        "Chỉ có thể chuyển đơn đã xác nhận sang đang giao hàng.";

                    return RedirectToAction("Details", new { id = id });
                }

                if (trangThaiMoi == "Đã giao hàng"
                    && donHang.TrangThai != "Đang giao hàng")
                {
                    TempData["ThongBaoLoi"] =
                        "Chỉ có thể xác nhận đã giao hàng khi đơn đang giao.";

                    return RedirectToAction("Details", new { id = id });
                }

                donHang.TrangThai = trangThaiMoi;

                db.SaveChanges();

                TempData["ThongBao"] =
                    "Cập nhật trạng thái đơn hàng thành công.";

                return RedirectToAction("Details", new { id = id });
            }
        }
    }
}
