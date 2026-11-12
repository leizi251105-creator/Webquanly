
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    public class KhoController : Controller
    {
        public ActionResult Index()
        {
            // 1. Kiểm tra đăng nhập
            if (Session["userID"] == null)
            {
                return RedirectToAction("Index", "Login");
            }

            // 2. Kiểm tra quyền truy cập kho
            var roles = Session["roles"] as List<string>;

            if (roles == null ||
                (!roles.Contains("Admin") &&
                 !roles.Contains("NhanVienKho")))
            {
                return new HttpStatusCodeResult(
                    403, "Bạn không có quyền quản lý kho.");
            }

            // 3. Lấy số liệu thống kê và danh sách lô thuốc
            using (QUANLYEntities1 db = new QUANLYEntities1())
            {
                var danhSachThuoc = db.Thuocs.AsQueryable();

                // Tổng số loại thuốc đang kinh doanh
                ViewBag.TongSoMatHang = danhSachThuoc
                    .Count(t => t.TrangThai);

                // Số loại thuốc có tồn kho thấp
                ViewBag.ThuocTonKhoThap = danhSachThuoc
                    .Count(t => t.TrangThai &&
                                t.SoLuongTon <= t.MucTonToiThieu);
                // Danh sách thuốc tồn kho thấp
                var danhSachThuocTonKhoThap = db.Thuocs
                    .Where(t => t.TrangThai &&
                                t.SoLuongTon <= t.MucTonToiThieu)
                    .OrderBy(t => t.SoLuongTon)
                    .ToList();

                ViewBag.DanhSachThuocTonKhoThap = danhSachThuocTonKhoThap;
                // Thời gian cảnh báo
                DateTime homNay = DateTime.Today;
                DateTime mocCanhBao = homNay.AddDays(90);

                // Số lô còn hàng, chưa hết hạn và sẽ hết hạn
                // trong vòng 90 ngày tới
                ViewBag.ThuocSapHetHan = db.LoThuocs.Count(l =>
                    l.TrangThai == true &&
                    l.SoLuong > 0 &&
                    l.HanSuDung >= homNay &&
                    l.HanSuDung <= mocCanhBao
                );

                // Danh sách tất cả các lô thuốc để hiển thị
                // trong giao diện quản lý kho
                var danhSachLoThuoc = db.LoThuocs
                    .Include(l => l.Thuoc)
                    .Where(l => l.SoLuong > 0)
                    .OrderBy(l => l.HanSuDung)
                    .ToList();

                ViewBag.DanhSachLoThuoc = danhSachLoThuoc;

                // Tổng số lượng thuốc còn trong các lô
                ViewBag.TongSoLuongTheoLo = db.LoThuocs
                    .Where(l => l.SoLuong > 0 &&
                                l.TrangThai == true)
                    .Select(l => (int?)l.SoLuong)
                    .Sum() ?? 0;
            }

            return View();
        }

        public ActionResult CanhBaoHanSuDung(int ngayCanhBao = 30)
        {
            // 1. Kiểm tra đăng nhập
            if (Session["userID"] == null)
            {
                return RedirectToAction("Index", "Login");
            }

            // 2. Kiểm tra quyền quản lý kho
            var roles = Session["roles"] as List<string>;

            if (roles == null ||
                (!roles.Contains("Admin") &&
                 !roles.Contains("NhanVienKho")))
            {
                return new HttpStatusCodeResult(
                    403, "Bạn không có quyền quản lý kho.");
            }

            // 3. Giới hạn các mốc cảnh báo hợp lệ
            if (ngayCanhBao != 30 &&
                ngayCanhBao != 60 &&
                ngayCanhBao != 90)
            {
                ngayCanhBao = 30;
            }

            using (QUANLYEntities1 db = new QUANLYEntities1())
            {
                DateTime homNay = DateTime.Today;
                DateTime mocCanhBao = homNay.AddDays(ngayCanhBao);

                // Lấy các lô còn hàng, kể cả lô đã hết hạn
                var danhSachLo = db.LoThuocs
                    .Include(l => l.Thuoc)
                    .Where(l => l.SoLuong > 0)
                    .OrderBy(l => l.HanSuDung)
                    .ToList();

                ViewBag.NgayCanhBao = ngayCanhBao;
                ViewBag.SoLoDaHetHan = danhSachLo.Count(l =>
                    l.TrangThai == true &&
                    l.HanSuDung.Date < homNay);

                ViewBag.SoLoSapHetHan = danhSachLo.Count(l =>
                    l.TrangThai == true &&
                    l.HanSuDung.Date >= homNay &&
                    l.HanSuDung.Date <= mocCanhBao);

                ViewBag.SoLoNgungHoatDong = danhSachLo.Count(l =>
                    l.TrangThai != true);

                return View(danhSachLo);
            }
        }

    
            public ActionResult TheoDoiTonKho()
        {
            // 1. Kiểm tra đăng nhập
            if (Session["userID"] == null)
            {
                return RedirectToAction("Index", "Login");
            }

            // 2. Kiểm tra quyền truy cập kho
            var roles = Session["roles"] as List<string>;

            if (roles == null ||
                (!roles.Contains("Admin") &&
                 !roles.Contains("NhanVienKho")))
            {
                return new HttpStatusCodeResult(
                    403, "Bạn không có quyền theo dõi tồn kho.");
            }

            // 3. Lấy danh sách thuốc từ cơ sở dữ liệu
            using (QUANLYEntities1 db = new QUANLYEntities1())
            {
                var danhSachThuoc = db.Thuocs
                    .Where(t => t.TrangThai)
                    .OrderBy(t => t.SoLuongTon)
                    .ToList();

                // 4. Thống kê tồn kho
                ViewBag.TongSoMatHang = danhSachThuoc.Count;

                ViewBag.SoMatHangTonThap = danhSachThuoc.Count(t =>
                    t.SoLuongTon <= t.MucTonToiThieu);

                ViewBag.SoMatHangConHang = danhSachThuoc.Count(t =>
                    t.SoLuongTon > t.MucTonToiThieu);

                ViewBag.TongSoLuongTon = danhSachThuoc.Sum(t =>
                    t.SoLuongTon);

                return View(danhSachThuoc);
            }
        }
    }
}
