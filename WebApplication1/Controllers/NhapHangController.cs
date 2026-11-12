
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    public class NhapHangController : Controller
    {
        private bool CoQuyenNhapHang()
        {
            if (Session["userID"] == null)
                return false;

            var roles = Session["roles"] as List<string>;

            return roles != null &&
                   (roles.Contains("Admin") ||
                    roles.Contains("NhanVienKho") ||
                    roles.Contains("NhanVienNhapHang"));
        }

        [HttpGet]
        public ActionResult Index(
     string tuKhoa,
     DateTime? tuNgay,
     DateTime? denNgay)
        {
            if (Session["userID"] == null)
                return RedirectToAction("Index", "Login");

            if (!CoQuyenNhapHang())
                return new HttpStatusCodeResult(
                    403, "Bạn không có quyền truy cập chức năng nhập hàng.");

            using (QUANLYEntities1 db = new QUANLYEntities1())
            {
                var query = db.PhieuNhaps
                    .Include("NhaCungCap")
                    .Include("TaiKhoan")
                    .AsQueryable();

                // Tìm kiếm theo mã phiếu hoặc tên nhà cung cấp
                if (!string.IsNullOrWhiteSpace(tuKhoa))
                {
                    string tuKhoaTim = tuKhoa.Trim();

                    int maPhieu;
                    bool laMaPhieu = int.TryParse(tuKhoaTim, out maPhieu);

                    query = query.Where(p =>
                        (laMaPhieu && p.MaPhieuNhap == maPhieu) ||
                        (p.NhaCungCap != null &&
                         p.NhaCungCap.TenNhaCungCap.Contains(tuKhoaTim))
                    );
                }

                // Lọc từ ngày
                if (tuNgay.HasValue)
                {
                    DateTime ngayBatDau = tuNgay.Value.Date;
                    query = query.Where(p => p.NgayNhap >= ngayBatDau);
                }

                // Lọc đến hết ngày
                if (denNgay.HasValue)
                {
                    DateTime ngayKetThuc = denNgay.Value.Date.AddDays(1);
                    query = query.Where(p => p.NgayNhap < ngayKetThuc);
                }

                var danhSach = query
                    .OrderByDescending(p => p.NgayNhap)
                    .ToList();

                // Giữ điều kiện tìm kiếm trên giao diện
                ViewBag.TuKhoa = tuKhoa;
                ViewBag.TuNgay = tuNgay?.ToString("yyyy-MM-dd");
                ViewBag.DenNgay = denNgay?.ToString("yyyy-MM-dd");

                // Thống kê trên toàn bộ phiếu nhập
                var tatCaPhieu = db.PhieuNhaps.ToList();

                ViewBag.TongSoPhieu = tatCaPhieu.Count;

                DateTime dauThang = new DateTime(
                    DateTime.Now.Year,
                    DateTime.Now.Month,
                    1
                );

                DateTime dauThangSau = dauThang.AddMonths(1);

                ViewBag.SoPhieuTrongThang = tatCaPhieu.Count(p =>
                    p.NgayNhap >= dauThang &&
                    p.NgayNhap < dauThangSau
                );

                ViewBag.TongGiaTriNhap = tatCaPhieu.Sum(p =>
                    p.TongTien ?? 0
                );

                return View(danhSach);
            }
        }

        [HttpGet]
        public ActionResult Create()
        {
            if (Session["userID"] == null)
                return RedirectToAction("Index", "Login");

            var roles = Session["roles"] as List<string>;

            if (roles == null ||
                !(roles.Contains("Admin") ||
                  roles.Contains("NhanVienKho") ||
                  roles.Contains("NhanVienNhapHang")))
            {
                return new HttpStatusCodeResult(
                    403, "Bạn không có quyền tạo phiếu nhập.");
            }

            using (QUANLYEntities1 db = new QUANLYEntities1())
            {
                ViewBag.NhaCungCap = new SelectList(
                    db.NhaCungCaps
                        .Where(n => n.TrangThai == true)
                        .OrderBy(n => n.TenNhaCungCap)
                        .ToList(),
                    "MaNhaCungCap",
                    "TenNhaCungCap"
                );

                ViewBag.Thuoc = new SelectList(
                    db.Thuocs
                        .Where(t => t.TrangThai)
                        .OrderBy(t => t.TenThuoc)
                        .ToList(),
                    "MaThuoc",
                    "TenThuoc"
                );

                ViewBag.DanhMucThuoc = new SelectList(
                db.DanhMucThuocs
                .Where(d => d.TrangThai == true)
                .OrderBy(d => d.TenDanhMuc)
                .ToList(),
                "MaDanhMuc",
                "TenDanhMuc"
                );
            }

            var model = new WebApplication1.ViewModels.NhapHangViewModel
            {
                NgayNhap = System.DateTime.Now
            };

            model.ChiTiet.Add(
                new WebApplication1.ViewModels.ChiTietNhapHangViewModel
                {
                    SoLuong = 1,
                    HanSuDung = System.DateTime.Today.AddMonths(12)
                });

            return View(model);

        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(
            WebApplication1.ViewModels.NhapHangViewModel model)
        {
            if (Session["userID"] == null)
                return RedirectToAction("Index", "Login");

            var roles = Session["roles"] as List<string>;

            if (roles == null ||
                !(roles.Contains("Admin") ||
                  roles.Contains("NhanVienKho") ||
                  roles.Contains("NhanVienNhapHang")))
            {
                return new HttpStatusCodeResult(
                    403, "Bạn không có quyền tạo phiếu nhập.");
            }

            // Kiểm tra danh sách chi tiết
            if (model.ChiTiet == null || model.ChiTiet.Count == 0)
            {
                ModelState.AddModelError("", "Phiếu nhập phải có ít nhất một thuốc.");
            }

            // Kiểm tra dữ liệu từng dòng
            if (model.ChiTiet != null)
            {
                foreach (var ct in model.ChiTiet)
                {
                    if (ct.SoLuong <= 0)
                        ModelState.AddModelError("", "Số lượng phải lớn hơn 0.");

                    if (ct.DonGia <= 0)
                        ModelState.AddModelError("", "Đơn giá nhập phải lớn hơn 0.");

                    if (ct.HanSuDung.Date <= DateTime.Today)
                        ModelState.AddModelError("", "Hạn sử dụng phải sau ngày hôm nay.");

                    if (ct.NgaySanXuat.HasValue &&
                        ct.NgaySanXuat.Value.Date > ct.HanSuDung.Date)
                    {
                        ModelState.AddModelError(
                            "", "Ngày sản xuất không được sau hạn sử dụng.");
                    }

                    if (string.IsNullOrWhiteSpace(ct.SoLo))
                        ModelState.AddModelError("", "Vui lòng nhập số lô thuốc.");
                }
            }

            int maTaiKhoan;

            try
            {
                maTaiKhoan = Convert.ToInt32(Session["userID"]);
            }
            catch
            {
                return new HttpStatusCodeResult(
                    401, "Không xác định được tài khoản đăng nhập.");
            }

            using (QUANLYEntities1 db = new QUANLYEntities1())
            {
                // Nạp lại danh sách để hiển thị nếu dữ liệu không hợp lệ
                Action napDanhSach = () =>
                {
                    ViewBag.NhaCungCap = new SelectList(
                        db.NhaCungCaps
                            .Where(n => n.TrangThai == true)
                            .OrderBy(n => n.TenNhaCungCap)
                            .ToList(),
                        "MaNhaCungCap", "TenNhaCungCap",
                        model.MaNhaCungCap);

                    ViewBag.Thuoc = new SelectList(
                        db.Thuocs
                            .Where(t => t.TrangThai)
                            .OrderBy(t => t.TenThuoc)
                            .ToList(),
                        "MaThuoc", "TenThuoc");

                    ViewBag.DanhMucThuoc = new SelectList(
                    db.DanhMucThuocs
                    .Where(d => d.TrangThai == true)
                    .OrderBy(d => d.TenDanhMuc)
                    .ToList(),
                    "MaDanhMuc",
                    "TenDanhMuc"
);
                };

                if (!ModelState.IsValid)
                {
                    napDanhSach();
                    return View(model);
                }

                // Kiểm tra nhà cung cấp và tài khoản
                var nhaCungCap = db.NhaCungCaps
                    .FirstOrDefault(n =>
                        n.MaNhaCungCap == model.MaNhaCungCap &&
                        n.TrangThai == true);

                var taiKhoan = db.TaiKhoans.Find(maTaiKhoan);

                if (nhaCungCap == null || taiKhoan == null)
                {
                    ModelState.AddModelError(
                        "", "Nhà cung cấp hoặc tài khoản không hợp lệ.");
                    napDanhSach();
                    return View(model);
                }

                // Kiểm tra các thuốc được chọn có đang hoạt động không
                var maThuocDuocChon = model.ChiTiet
                    .Select(ct => ct.MaThuoc)
                    .Distinct()
                    .ToList();

                var soThuocHopLe = db.Thuocs.Count(t =>
                    maThuocDuocChon.Contains(t.MaThuoc) &&
                    t.TrangThai);

                if (soThuocHopLe != maThuocDuocChon.Count)
                {
                    ModelState.AddModelError(
                        "", "Có thuốc không tồn tại hoặc đã ngừng kinh doanh.");
                    napDanhSach();
                    return View(model);
                }

                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        decimal tongTien = model.ChiTiet.Sum(
                            ct => ct.SoLuong * ct.DonGia);

                        // 1. Tạo phiếu nhập
                        var phieu = new PhieuNhap
                        {
                            MaNhaCungCap = model.MaNhaCungCap,
                            MaTaiKhoan = maTaiKhoan,
                            NgayNhap = model.NgayNhap,
                            TongTien = tongTien,
                            GhiChu = model.GhiChu
                        };

                        db.PhieuNhaps.Add(phieu);
                        db.SaveChanges();

                        // 2. Tạo từng lô và chi tiết phiếu
                        foreach (var ct in model.ChiTiet)
                        {
                            var thuoc = db.Thuocs.Find(ct.MaThuoc);

                            if (thuoc == null || !thuoc.TrangThai)
                            {
                                throw new Exception(
                                    "Thuốc không còn hợp lệ. Vui lòng thử lại.");
                            }

                            var lo = new LoThuoc
                            {
                                MaThuoc = ct.MaThuoc,
                                SoLo = ct.SoLo.Trim(),
                                NgaySanXuat = ct.NgaySanXuat,
                                HanSuDung = ct.HanSuDung,
                                SoLuong = ct.SoLuong,
                                GiaNhap = ct.DonGia,
                                TrangThai = true
                            };

                            db.LoThuocs.Add(lo);
                            db.SaveChanges();

                            var chiTiet = new ChiTietPhieuNhap
                            {
                                MaPhieuNhap = phieu.MaPhieuNhap,
                                MaThuoc = ct.MaThuoc,
                                MaLo = lo.MaLo,
                                SoLuong = ct.SoLuong,
                                DonGia = ct.DonGia,
                                ThanhTien = ct.SoLuong * ct.DonGia
                            };

                            db.ChiTietPhieuNhaps.Add(chiTiet);

                            // Cập nhật tồn kho tổng của thuốc
                            thuoc.SoLuongTon += ct.SoLuong;
                            thuoc.GiaNhap = ct.DonGia;
                        }

                        db.SaveChanges();
                        transaction.Commit();

                        TempData["ThongBaoNhapHang"] =
                            "Tạo phiếu nhập PN" +
                            phieu.MaPhieuNhap.ToString("D5") +
                            " thành công.";

                        return RedirectToAction("Index");
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();

                        ModelState.AddModelError(
                            "",
                            "Không thể lưu phiếu nhập. Dữ liệu đã được hoàn tác. " +
                            "Vui lòng kiểm tra thông tin và cấu trúc cơ sở dữ liệu.");

                        napDanhSach();
                        return View(model);
                    }
                }
            }
        }
        [HttpGet]
        public ActionResult Details(int id)
        {
            if (Session["userID"] == null)
                return RedirectToAction("Index", "Login");

            if (!CoQuyenNhapHang())
            {
                return new HttpStatusCodeResult(
                    403,
                    "Bạn không có quyền xem chi tiết phiếu nhập.");
            }

            using (QUANLYEntities1 db = new QUANLYEntities1())
            {
                var phieu = db.PhieuNhaps
                    .Include("NhaCungCap")
                    .Include("TaiKhoan")
                    .Include("ChiTietPhieuNhaps.Thuoc")
                    .Include("ChiTietPhieuNhaps.LoThuoc")
                    .FirstOrDefault(p => p.MaPhieuNhap == id);

                if (phieu == null)
                {
                    return HttpNotFound("Không tìm thấy phiếu nhập.");
                }

                return View(phieu);
            }
        }

    
            [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult ThemThuocNhanh(
            WebApplication1.ViewModels.ThuocNhapNhanhViewModel model)
        {
            if (Session["userID"] == null)
            {
                Response.StatusCode = 401;
                return Json(new
                {
                    success = false,
                    message = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại."
                });
            }

            if (!CoQuyenNhapHang())
            {
                Response.StatusCode = 403;
                return Json(new
                {
                    success = false,
                    message = "Bạn không có quyền thêm thuốc."
                });
            }

            if (model == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Dữ liệu thuốc không hợp lệ."
                });
            }

            if (!ModelState.IsValid)
            {
                var loi = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .FirstOrDefault();

                return Json(new
                {
                    success = false,
                    message = loi ?? "Vui lòng kiểm tra lại thông tin thuốc."
                });
            }

            using (QUANLYEntities1 db = new QUANLYEntities1())
            {
                var danhMuc = db.DanhMucThuocs.FirstOrDefault(d =>
                    d.MaDanhMuc == model.MaDanhMuc &&
                    d.TrangThai == true);

                if (danhMuc == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Danh mục thuốc không tồn tại hoặc đã ngừng hoạt động."
                    });
                }

                string tenThuoc = model.TenThuoc.Trim();

                bool daTonTai = db.Thuocs.Any(t =>
                    t.TenThuoc == tenThuoc);

                if (daTonTai)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Tên thuốc đã tồn tại trong danh mục. Vui lòng chọn thuốc có sẵn."
                    });
                }

                var thuoc = new Thuoc
                {
                    TenThuoc = tenThuoc,
                    MaDanhMuc = model.MaDanhMuc,
                    DonViTinh = model.DonViTinh.Trim(),
                    HoatChat = string.IsNullOrWhiteSpace(model.HoatChat)
                        ? null : model.HoatChat.Trim(),
                    HamLuong = string.IsNullOrWhiteSpace(model.HamLuong)
                        ? null : model.HamLuong.Trim(),
                    GiaNhap = 0,
                    GiaBan = model.GiaBan,
                    SoLuongTon = 0,
                    MucTonToiThieu = model.MucTonToiThieu,
                    TrangThai = true,
                    NgayTao = DateTime.Now
                };

                db.Thuocs.Add(thuoc);
                db.SaveChanges();

                return Json(new
                {
                    success = true,
                    message = "Thêm thuốc mới thành công.",
                    maThuoc = thuoc.MaThuoc,
                    tenThuoc = thuoc.TenThuoc
                });
            }
        }
    }
}
