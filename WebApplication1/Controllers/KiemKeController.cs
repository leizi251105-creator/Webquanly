
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using ClosedXML.Excel;
using System.IO;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    public class KiemKeController : Controller
    {
        private bool CoQuyenKiemKe()
        {
            if (Session["userID"] == null)
                return false;

            var roles = Session["roles"] as List<string>;

            return roles != null &&
                (roles.Contains("Admin") ||
                 roles.Contains("NhanVienKho"));
        }


        public ActionResult Index(
            string tuKhoa,
            string trangThai,
            DateTime? tuNgay,
            DateTime? denNgay)
        {
            if (Session["userID"] == null)
                return RedirectToAction("Index", "Login");

            if (!CoQuyenKiemKe())
                return new HttpStatusCodeResult(
                    403, "Bạn không có quyền kiểm kê kho.");

            using (var db = new QUANLYEntities1())
            {
                var query = db.PhieuKiemKes
                    .Include(p => p.TaiKhoan)
                    .AsQueryable();

                // 1. Tìm theo mã phiếu hoặc tên người lập
                if (!string.IsNullOrWhiteSpace(tuKhoa))
                {
                    tuKhoa = tuKhoa.Trim();

                    int maPhieu;
                    bool laMaPhieu = int.TryParse(tuKhoa, out maPhieu);

                    if (laMaPhieu)
                    {
                        query = query.Where(p =>
                            p.MaPhieuKiemKe == maPhieu ||
                            (p.TaiKhoan != null &&
                             p.TaiKhoan.HoTen.Contains(tuKhoa)));
                    }
                    else
                    {
                        query = query.Where(p =>
                            p.TaiKhoan != null &&
                            p.TaiKhoan.HoTen.Contains(tuKhoa));
                    }
                }

                // 2. Lọc theo trạng thái
                if (!string.IsNullOrWhiteSpace(trangThai))
                {
                    query = query.Where(p =>
                        p.TrangThai == trangThai);
                }

                // 3. Lọc từ ngày
                if (tuNgay.HasValue)
                {
                    DateTime ngayBatDau = tuNgay.Value.Date;

                    query = query.Where(p =>
                        p.NgayKiemKe >= ngayBatDau);
                }

                // 4. Lọc đến ngày (bao gồm toàn bộ ngày được chọn)
                if (denNgay.HasValue)
                {
                    DateTime ngayKetThuc =
                        denNgay.Value.Date.AddDays(1);

                    query = query.Where(p =>
                        p.NgayKiemKe < ngayKetThuc);
                }

                var danhSach = query
                    .OrderByDescending(p => p.NgayKiemKe)
                    .ToList();

                // Giữ lại điều kiện đã nhập trên giao diện
                ViewBag.TuKhoa = tuKhoa;
                ViewBag.TrangThai = trangThai;
                ViewBag.TuNgay = tuNgay.HasValue
                    ? tuNgay.Value.ToString("yyyy-MM-dd")
                    : "";

                ViewBag.DenNgay = denNgay.HasValue
                    ? denNgay.Value.ToString("yyyy-MM-dd")
                    : "";

                return View(danhSach);
            }
        }


        // GET: KiemKe/Create
        [HttpGet]
        public ActionResult Create()
        {
            if (Session["userID"] == null)
                return RedirectToAction("Index", "Login");

            if (!CoQuyenKiemKe())
                return new HttpStatusCodeResult(
                    403, "Bạn không có quyền kiểm kê kho.");

            using (var db = new QUANLYEntities1())
            {
                var danhSachLo = db.LoThuocs
                    .Include(l => l.Thuoc)
                    .Where(l => l.SoLuong > 0 &&
                                l.TrangThai == true)
                    .OrderBy(l => l.HanSuDung)
                    .ToList();

                var model = new KiemKeViewModel();

                foreach (var lo in danhSachLo)
                {
                    model.ChiTiet.Add(
                        new KiemKeChiTietViewModel
                        {
                            MaLo = lo.MaLo,
                            TenThuoc = lo.Thuoc.TenThuoc,
                            SoLo = lo.SoLo,
                            SoLuongHeThong = lo.SoLuong,
                            SoLuongThucTe = lo.SoLuong,
                            HanSuDung =
                                lo.HanSuDung.ToString("dd/MM/yyyy")
                        });
                }

                return View(model);
            }
        }

        // POST: KiemKe/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(KiemKeViewModel model)
        {
            if (Session["userID"] == null)
                return RedirectToAction("Index", "Login");

            if (!CoQuyenKiemKe())
                return new HttpStatusCodeResult(
                    403, "Bạn không có quyền kiểm kê kho.");

            if (model.ChiTiet == null || !model.ChiTiet.Any())
            {
                ModelState.AddModelError(
                    "",
                    "Không có dữ liệu kiểm kê.");
            }

            if (!ModelState.IsValid)
                return View(model);

            using (var db = new QUANLYEntities1())
            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    int maTaiKhoan =
                        Convert.ToInt32(Session["userID"]);

                    var phieu = new PhieuKiemKe
                    {
                        MaTaiKhoan = maTaiKhoan,
                        NgayKiemKe = DateTime.Now,
                        GhiChu = model.GhiChu,
                        TrangThai = "Chưa xác nhận"
                    };

                    db.PhieuKiemKes.Add(phieu);
                    db.SaveChanges();

                    foreach (var dong in model.ChiTiet)
                    {
                        if (dong.SoLuongThucTe < 0)
                        {
                            ModelState.AddModelError(
                                "",
                                "Số lượng thực tế không được âm.");

                            transaction.Rollback();
                            return View(model);
                        }

                        // Lấy số lượng hiện tại trực tiếp từ database.
                        var lo = db.LoThuocs.FirstOrDefault(l =>
                            l.MaLo == dong.MaLo &&
                            l.TrangThai == true &&
                            l.SoLuong > 0);

                        if (lo == null)
                        {
                            ModelState.AddModelError(
                                "",
                                "Có lô thuốc không còn hợp lệ. " +
                                "Vui lòng tải lại trang.");

                            transaction.Rollback();
                            return View(model);
                        }

                        var chiTiet = new ChiTietKiemKe
                        {
                            MaPhieuKiemKe = phieu.MaPhieuKiemKe,
                            MaLo = lo.MaLo,
                            SoLuongHeThong = lo.SoLuong,
                            SoLuongThucTe = dong.SoLuongThucTe
                        };

                        db.ChiTietKiemKes.Add(chiTiet);
                    }

                    db.SaveChanges();
                    transaction.Commit();

                    TempData["ThongBao"] =
                        "Đã tạo phiếu kiểm kê ở trạng thái Chưa xác nhận.";

                    return RedirectToAction("Index");
                }
                catch (Exception)
                {
                    transaction.Rollback();

                    ModelState.AddModelError(
                        "",
                        "Không thể lưu phiếu kiểm kê. " +
                        "Vui lòng kiểm tra dữ liệu và thử lại.");

                    return View(model);
                }
            }
        }


        public ActionResult Details(int? id)
        {
            if (Session["userID"] == null)
                return RedirectToAction("Index", "Login");

            if (!CoQuyenKiemKe())
                return new HttpStatusCodeResult(403);

            if (!id.HasValue || id.Value <= 0)
                return RedirectToAction("Index");

            using (var db = new QUANLYEntities1())
            {
                var phieu = db.PhieuKiemKes
                    .Include("TaiKhoan")
                    .Include("ChiTietKiemKes.LoThuoc")
                    .Include("ChiTietKiemKes.LoThuoc.Thuoc")
                    .FirstOrDefault(p => p.MaPhieuKiemKe == id.Value);

                if (phieu == null)
                    return HttpNotFound();

                return View(phieu);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult XacNhan(int id)
        {
            if (Session["userID"] == null)
                return RedirectToAction("Index", "Login");

            if (!CoQuyenKiemKe())
                return new HttpStatusCodeResult(403);

            using (var db = new QUANLYEntities1())
            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    var phieu = db.PhieuKiemKes
                        .FirstOrDefault(p => p.MaPhieuKiemKe == id);

                    if (phieu == null)
                        return HttpNotFound();

                    if (phieu.TrangThai != "Chưa xác nhận")
                    {
                        TempData["ThongBao"] =
                            "Phiếu không ở trạng thái có thể xác nhận.";

                        return RedirectToAction("Details", new { id = id });
                    }

                    var chiTiet = db.ChiTietKiemKes
                        .Where(c => c.MaPhieuKiemKe == id)
                        .ToList();

                    if (!chiTiet.Any())
                    {
                        TempData["ThongBao"] = "Phiếu không có chi tiết kiểm kê.";
                        return RedirectToAction("Details", new { id = id });
                    }

                    // Kiểm tra toàn bộ lô trước khi thay đổi tồn kho.
                    foreach (var dong in chiTiet)
                    {
                        var lo = db.LoThuocs
                            .FirstOrDefault(l => l.MaLo == dong.MaLo);

                        if (lo == null || lo.SoLuong != dong.SoLuongHeThong)
                        {
                            TempData["ThongBao"] =
                                "Tồn kho đã thay đổi sau khi lập phiếu. " +
                                "Vui lòng kiểm kê lại trước khi xác nhận.";

                            return RedirectToAction("Details", new { id = id });
                        }
                    }

                    int maTaiKhoan =
                        System.Convert.ToInt32(Session["userID"]);

                    foreach (var dong in chiTiet)
                    {
                        var lo = db.LoThuocs
                            .First(l => l.MaLo == dong.MaLo);

                        var thuoc = db.Thuocs
                            .FirstOrDefault(t => t.MaThuoc == lo.MaThuoc);

                        if (thuoc == null)
                            throw new System.Exception("Không tìm thấy thuốc.");

                        int chenhlech = dong.SoLuongThucTe - lo.SoLuong;

                        // Cập nhật số lượng thực tế của lô.
                        lo.SoLuong = dong.SoLuongThucTe;

                        // Điều chỉnh tổng tồn kho của thuốc theo chênh lệch.
                        thuoc.SoLuongTon += chenhlech;

                        if (thuoc.SoLuongTon < 0)
                            throw new System.Exception(
                                "Tồn kho tổng không hợp lệ.");
                    }

                    phieu.TrangThai = "Đã xác nhận";

                    db.LichSuHoatDongs.Add(new LichSuHoatDong
                    {
                        MaTaiKhoan = maTaiKhoan,
                        HanhDong = "Xác nhận kiểm kê",
                        DoiTuong = "PhieuKiemKe",
                        MaDoiTuong = id.ToString(),
                        ThoiGian = System.DateTime.Now,
                        NoiDung = "Xác nhận phiếu kiểm kê mã " + id,
                        DiaChiIP = Request.UserHostAddress
                    });

                    db.SaveChanges();
                    transaction.Commit();

                    TempData["ThongBao"] =
                        "Xác nhận kiểm kê thành công. Tồn kho đã được cập nhật.";

                    return RedirectToAction("Details", new { id = id });
                }
                catch
                {
                    transaction.Rollback();

                    TempData["ThongBao"] =
                        "Không thể xác nhận phiếu. Dữ liệu tồn kho chưa được cập nhật.";

                    return RedirectToAction("Details", new { id = id });
                }
            }
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Huy(int id)
        {
            if (Session["userID"] == null)
                return RedirectToAction("Index", "Login");

            if (!CoQuyenKiemKe())
                return new HttpStatusCodeResult(403,
                    "Bạn không có quyền hủy phiếu kiểm kê.");

            using (var db = new QUANLYEntities1())
            {
                var phieu = db.PhieuKiemKes
                    .FirstOrDefault(p => p.MaPhieuKiemKe == id);

                if (phieu == null)
                    return HttpNotFound();

                if (phieu.TrangThai != "Chưa xác nhận")
                {
                    TempData["ThongBao"] =
                        "Chỉ được hủy phiếu kiểm kê chưa xác nhận.";

                    return RedirectToAction("Details", new { id = id });
                }

                phieu.TrangThai = "Đã hủy";

                int maTaiKhoan =
                    Convert.ToInt32(Session["userID"]);

                db.LichSuHoatDongs.Add(new LichSuHoatDong
                {
                    MaTaiKhoan = maTaiKhoan,
                    HanhDong = "Hủy phiếu kiểm kê",
                    DoiTuong = "PhieuKiemKe",
                    MaDoiTuong = id.ToString(),
                    ThoiGian = DateTime.Now,
                    NoiDung = "Hủy phiếu kiểm kê mã " + id,
                    DiaChiIP = Request.UserHostAddress
                });

                db.SaveChanges();

                TempData["ThongBao"] =
                    "Đã hủy phiếu kiểm kê. Tồn kho không thay đổi.";

                return RedirectToAction("Details", new { id = id });
            }
        }



        [HttpGet]
        public ActionResult BaoCao(
            DateTime? tuNgay,
            DateTime? denNgay,
            string trangThai)
        {
            if (Session["userID"] == null)
                return RedirectToAction("Index", "Login");

            if (!CoQuyenKiemKe())
                return new HttpStatusCodeResult(
                    403, "Bạn không có quyền xem báo cáo kiểm kê.");

            using (var db = new QUANLYEntities1())
            {
                // Khởi tạo dữ liệu báo cáo
                var model = new KiemKeBaoCaoViewModel
                {
                    TuNgay = tuNgay,
                    DenNgay = denNgay,
                    TrangThai = trangThai
                };

                // Truy vấn phiếu kiểm kê
                var query = db.PhieuKiemKes.AsQueryable();

                if (tuNgay.HasValue)
                {
                    DateTime ngayBatDau = tuNgay.Value.Date;

                    query = query.Where(p =>
                        p.NgayKiemKe >= ngayBatDau);
                }

                if (denNgay.HasValue)
                {
                    DateTime ngayKetThuc =
                        denNgay.Value.Date.AddDays(1);

                    query = query.Where(p =>
                        p.NgayKiemKe < ngayKetThuc);
                }

                if (!string.IsNullOrWhiteSpace(trangThai))
                {
                    query = query.Where(p =>
                        p.TrangThai == trangThai);
                }

                var danhSachPhieu = query.ToList();

                // Thống kê số lượng phiếu
                model.TongSoPhieu = danhSachPhieu.Count;

                model.SoPhieuDaXacNhan = danhSachPhieu.Count(p =>
                    p.TrangThai == "Đã xác nhận");

                model.SoPhieuChuaXacNhan = danhSachPhieu.Count(p =>
                    p.TrangThai == "Chưa xác nhận");

                model.SoPhieuDaHuy = danhSachPhieu.Count(p =>
                    p.TrangThai == "Đã hủy");

                // Chỉ lấy chi tiết của phiếu đã xác nhận
                var maPhieuDaXacNhan = danhSachPhieu
                    .Where(p => p.TrangThai == "Đã xác nhận")
                    .Select(p => p.MaPhieuKiemKe)
                    .ToList();

                var danhSachChiTiet = db.ChiTietKiemKes
                    .Include(c => c.LoThuoc)
                    .Include(c => c.LoThuoc.Thuoc)
                    .Include(c => c.PhieuKiemKe)
                    .Where(c =>
                        maPhieuDaXacNhan.Contains(c.MaPhieuKiemKe))
                    .ToList();

                foreach (var dong in danhSachChiTiet)
                {
                    int chenhlech =
                        dong.SoLuongThucTe - dong.SoLuongHeThong;

                    // Chỉ hiển thị những lô có chênh lệch
                    if (chenhlech == 0)
                        continue;

                    model.DanhSachChiTiet.Add(
                        new KiemKeBaoCaoChiTietViewModel
                        {
                            MaPhieuKiemKe = dong.MaPhieuKiemKe,

                            NgayKiemKe = dong.PhieuKiemKe.NgayKiemKe,

                            TenThuoc = dong.LoThuoc != null &&
                                       dong.LoThuoc.Thuoc != null
                                ? dong.LoThuoc.Thuoc.TenThuoc
                                : "Không xác định",

                            SoLo = dong.LoThuoc != null
                                ? dong.LoThuoc.SoLo
                                : "Không xác định",

                            SoLuongHeThong = dong.SoLuongHeThong,

                            SoLuongThucTe = dong.SoLuongThucTe,

                            ChenhLech = chenhlech,

                            TenNguoiLap =
                                dong.PhieuKiemKe.TaiKhoan != null
                                    ? dong.PhieuKiemKe.TaiKhoan.HoTen
                                    : "Không xác định"
                        });
                }

                // Tổng hợp số lô thừa và thiếu
                model.SoLoThua = model.DanhSachChiTiet.Count(c =>
                    c.ChenhLech > 0);

                model.SoLoThieu = model.DanhSachChiTiet.Count(c =>
                    c.ChenhLech < 0);

                model.TongSoLuongThua = model.DanhSachChiTiet
                    .Where(c => c.ChenhLech > 0)
                    .Sum(c => c.ChenhLech);

                model.TongSoLuongThieu = model.DanhSachChiTiet
                    .Where(c => c.ChenhLech < 0)
                    .Sum(c => Math.Abs(c.ChenhLech));

                return View(model);
            }
        }



        [HttpGet]
        public ActionResult XuatExcel(
            DateTime? tuNgay,
            DateTime? denNgay,
            string trangThai)
        {
            if (Session["userID"] == null)
                return RedirectToAction("Index", "Login");

            if (!CoQuyenKiemKe())
                return new HttpStatusCodeResult(
                    403, "Bạn không có quyền xuất báo cáo kiểm kê.");

            using (var db = new QUANLYEntities1())
            {
                // Lọc phiếu theo điều kiện người dùng chọn
                var query = db.PhieuKiemKes.AsQueryable();

                if (tuNgay.HasValue)
                {
                    DateTime ngayBatDau = tuNgay.Value.Date;

                    query = query.Where(p =>
                        p.NgayKiemKe >= ngayBatDau);
                }

                if (denNgay.HasValue)
                {
                    DateTime ngayKetThuc =
                        denNgay.Value.Date.AddDays(1);

                    query = query.Where(p =>
                        p.NgayKiemKe < ngayKetThuc);
                }

                if (!string.IsNullOrWhiteSpace(trangThai))
                {
                    query = query.Where(p =>
                        p.TrangThai == trangThai);
                }

                var danhSachPhieu = query
                    .OrderByDescending(p => p.NgayKiemKe)
                    .ToList();

                // Tạo workbook Excel
                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add(
                        "Bao cao kiem ke");

                    // Tiêu đề
                    worksheet.Range("A1:H1").Merge();
                    worksheet.Cell("A1").Value =
                        "BÁO CÁO KIỂM KÊ TỒN KHO";

                    worksheet.Cell("A1").Style.Font.Bold = true;
                    worksheet.Cell("A1").Style.Font.FontSize = 16;
                    worksheet.Cell("A1").Style.Alignment.Horizontal =
                        XLAlignmentHorizontalValues.Center;

                    // Thời gian xuất báo cáo
                    worksheet.Range("A2:H2").Merge();
                    worksheet.Cell("A2").Value =
                        "Ngày xuất: " + DateTime.Now.ToString("dd/MM/yyyy HH:mm");

                    worksheet.Cell("A2").Style.Alignment.Horizontal =
                        XLAlignmentHorizontalValues.Center;

                    // Thông tin bộ lọc
                    worksheet.Cell("A4").Value = "Từ ngày";
                    worksheet.Cell("B4").Value = tuNgay.HasValue
                        ? tuNgay.Value.ToString("dd/MM/yyyy")
                        : "Tất cả";

                    worksheet.Cell("C4").Value = "Đến ngày";
                    worksheet.Cell("D4").Value = denNgay.HasValue
                        ? denNgay.Value.ToString("dd/MM/yyyy")
                        : "Tất cả";

                    worksheet.Cell("E4").Value = "Trạng thái";
                    worksheet.Cell("F4").Value =
                        string.IsNullOrWhiteSpace(trangThai)
                            ? "Tất cả"
                            : trangThai;

                    // Thống kê phiếu
                    worksheet.Cell("A6").Value = "TỔNG QUAN PHIẾU KIỂM KÊ";
                    worksheet.Range("A6:B6").Merge();

                    worksheet.Cell("A7").Value = "Tổng số phiếu";
                    worksheet.Cell("B7").Value = danhSachPhieu.Count;

                    worksheet.Cell("A8").Value = "Đã xác nhận";
                    worksheet.Cell("B8").Value = danhSachPhieu.Count(
                        p => p.TrangThai == "Đã xác nhận");

                    worksheet.Cell("A9").Value = "Chưa xác nhận";
                    worksheet.Cell("B9").Value = danhSachPhieu.Count(
                        p => p.TrangThai == "Chưa xác nhận");

                    worksheet.Cell("A10").Value = "Đã hủy";
                    worksheet.Cell("B10").Value = danhSachPhieu.Count(
                        p => p.TrangThai == "Đã hủy");

                    // Chỉ lấy chi tiết thuộc phiếu đã xác nhận
                    var maPhieuDaXacNhan = danhSachPhieu
                        .Where(p => p.TrangThai == "Đã xác nhận")
                        .Select(p => p.MaPhieuKiemKe)
                        .ToList();

                    var danhSachChiTiet = db.ChiTietKiemKes
                        .Include(c => c.LoThuoc)
                        .Include(c => c.LoThuoc.Thuoc)
                        .Include(c => c.PhieuKiemKe)
                        .Include(c => c.PhieuKiemKe.TaiKhoan)
                        .Where(c =>
                            maPhieuDaXacNhan.Contains(c.MaPhieuKiemKe))
                        .ToList();

                    // Lọc những dòng có chênh lệch
                    var chiTietChenhLech = danhSachChiTiet
                        .Where(c =>
                            c.SoLuongThucTe != c.SoLuongHeThong)
                        .ToList();

                    int soLoThua = chiTietChenhLech.Count(c =>
                        c.SoLuongThucTe > c.SoLuongHeThong);

                    int soLoThieu = chiTietChenhLech.Count(c =>
                        c.SoLuongThucTe < c.SoLuongHeThong);

                    int tongThua = chiTietChenhLech
                        .Where(c => c.SoLuongThucTe > c.SoLuongHeThong)
                        .Sum(c => c.SoLuongThucTe - c.SoLuongHeThong);

                    int tongThieu = chiTietChenhLech
                        .Where(c => c.SoLuongThucTe < c.SoLuongHeThong)
                        .Sum(c => c.SoLuongHeThong - c.SoLuongThucTe);

                    // Thống kê chênh lệch
                    worksheet.Cell("D6").Value =
                        "TỔNG HỢP CHÊNH LỆCH";
                    worksheet.Range("D6:E6").Merge();

                    worksheet.Cell("D7").Value = "Số lô thừa";
                    worksheet.Cell("E7").Value = soLoThua;

                    worksheet.Cell("D8").Value = "Số lô thiếu";
                    worksheet.Cell("E8").Value = soLoThieu;

                    worksheet.Cell("D9").Value = "Tổng số lượng thừa";
                    worksheet.Cell("E9").Value = tongThua;

                    worksheet.Cell("D10").Value = "Tổng số lượng thiếu";
                    worksheet.Cell("E10").Value = tongThieu;

                    // Tiêu đề bảng chi tiết
                    worksheet.Cell("A12").Value = "Mã phiếu";
                    worksheet.Cell("B12").Value = "Ngày kiểm kê";
                    worksheet.Cell("C12").Value = "Tên thuốc";
                    worksheet.Cell("D12").Value = "Số lô";
                    worksheet.Cell("E12").Value = "SL hệ thống";
                    worksheet.Cell("F12").Value = "SL thực tế";
                    worksheet.Cell("G12").Value = "Chênh lệch";
                    worksheet.Cell("H12").Value = "Người lập";

                    // Ghi dữ liệu
                    int row = 13;

                    foreach (var dong in chiTietChenhLech)
                    {
                        int chenhlech =
                            dong.SoLuongThucTe - dong.SoLuongHeThong;

                        worksheet.Cell(row, 1).Value =
                            dong.MaPhieuKiemKe;

                        worksheet.Cell(row, 2).Value =
                            dong.PhieuKiemKe.NgayKiemKe;

                        worksheet.Cell(row, 2).Style.DateFormat.Format =
                            "dd/MM/yyyy HH:mm";

                        worksheet.Cell(row, 3).Value =
                            dong.LoThuoc != null &&
                            dong.LoThuoc.Thuoc != null
                                ? dong.LoThuoc.Thuoc.TenThuoc
                                : "Không xác định";

                        worksheet.Cell(row, 4).Value =
                            dong.LoThuoc != null
                                ? dong.LoThuoc.SoLo
                                : "Không xác định";

                        worksheet.Cell(row, 5).Value =
                            dong.SoLuongHeThong;

                        worksheet.Cell(row, 6).Value =
                            dong.SoLuongThucTe;

                        worksheet.Cell(row, 7).Value = chenhlech;

                        worksheet.Cell(row, 8).Value =
                            dong.PhieuKiemKe.TaiKhoan != null
                                ? dong.PhieuKiemKe.TaiKhoan.HoTen
                                : "Không xác định";

                        row++;
                    }

                    // Định dạng tiêu đề
                    var header = worksheet.Range("A12:H12");

                    header.Style.Font.Bold = true;
                    header.Style.Fill.BackgroundColor =
                        XLColor.LightBlue;

                    header.Style.Alignment.Horizontal =
                        XLAlignmentHorizontalValues.Center;

                    header.Style.Alignment.Vertical =
                        XLAlignmentVerticalValues.Center;

                    // Định dạng các phần thống kê
                    worksheet.Range("A6:B6").Style.Font.Bold = true;
                    worksheet.Range("D6:E6").Style.Font.Bold = true;

                    worksheet.Range("A6:B6").Style.Fill.BackgroundColor =
                        XLColor.LightGray;

                    worksheet.Range("D6:E6").Style.Fill.BackgroundColor =
                        XLColor.LightGray;

                    // Kẻ viền bảng
                    if (row > 13)
                    {
                        worksheet.Range("A12:H" + (row - 1))
                            .Style.Border.OutsideBorder =
                            XLBorderStyleValues.Thin;

                        worksheet.Range("A12:H" + (row - 1))
                            .Style.Border.InsideBorder =
                            XLBorderStyleValues.Thin;
                    }

                    worksheet.Columns().AdjustToContents();

                    // Lưu file vào bộ nhớ
                    using (var stream = new MemoryStream())
                    {
                        workbook.SaveAs(stream);

                        string tenFile =
                            "BaoCaoKiemKe_" +
                            DateTime.Now.ToString("yyyyMMdd_HHmmss") +
                            ".xlsx";

                        return File(
                            stream.ToArray(),
                            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                            tenFile);
                    }
                }
            }
        }

    }
}
