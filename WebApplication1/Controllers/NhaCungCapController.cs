
using System;
using System.Linq;
using System.Data.Entity;
using System.Net;
using System.Web.Mvc;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    public class NhaCungCapController : Controller
    {
        // =========================================
        // KIỂM TRA QUYỀN QUẢN LÝ NHÀ CUNG CẤP
        // =========================================
        private bool CoQuyenNhaCungCap()
        {
            if (Session["userID"] == null)
                return false;

            // Tạm thời kiểm tra đăng nhập.
            // Sẽ bổ sung kiểm tra quyền chi tiết ở bước phân quyền.
            return true;
        }

        // =========================================
        // 1. DANH SÁCH VÀ TÌM KIẾM NHÀ CUNG CẤP
        // =========================================
        [HttpGet]
        public ActionResult Index(string tuKhoa, string trangThai)
        {
            if (Session["userID"] == null)
                return RedirectToAction("Index", "Login");

            if (!CoQuyenNhaCungCap())
                return new HttpStatusCodeResult(403);

            using (QUANLYEntities1 db = new QUANLYEntities1())
            {
                var query = db.NhaCungCaps.AsQueryable();

                if (!string.IsNullOrWhiteSpace(tuKhoa))
                {
                    string keyword = tuKhoa.Trim();

                    query = query.Where(n =>
                        n.TenNhaCungCap.Contains(keyword) ||
                        (n.SoDienThoai != null &&
                         n.SoDienThoai.Contains(keyword)) ||
                        (n.Email != null &&
                         n.Email.Contains(keyword)) ||
                        (n.MaSoThue != null &&
                         n.MaSoThue.Contains(keyword)) ||
                        (n.DiaChi != null &&
                         n.DiaChi.Contains(keyword))
                    );
                }

                if (trangThai == "1")
                    query = query.Where(n => n.TrangThai == true);
                else if (trangThai == "0")
                    query = query.Where(n => n.TrangThai == false);

                var danhSach = query
                    .OrderBy(n => n.MaNhaCungCap)
                    .ToList();

                ViewBag.TuKhoa = tuKhoa;
                ViewBag.TrangThai = trangThai;

                return View(danhSach);
            }
        }

        // =========================================
        // 2. THÊM NHÀ CUNG CẤP - GET
        // =========================================
        [HttpGet]
        public ActionResult Create()
        {
            if (Session["userID"] == null)
                return RedirectToAction("Index", "Login");

            return View();
        }

        // =========================================
        // 3. THÊM NHÀ CUNG CẤP - POST
        // =========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(NhaCungCap model)
        {
            if (Session["userID"] == null)
                return RedirectToAction("Index", "Login");

            if (string.IsNullOrWhiteSpace(model.TenNhaCungCap))
            {
                ModelState.AddModelError(
                    "TenNhaCungCap",
                    "Vui lòng nhập tên nhà cung cấp."
                );
            }

            if (ModelState.IsValid)
            {
                using (QUANLYEntities1 db = new QUANLYEntities1())
                {
                    model.TenNhaCungCap = model.TenNhaCungCap.Trim();

                    if (db.NhaCungCaps.Any(n =>
                        n.TenNhaCungCap == model.TenNhaCungCap))
                    {
                        ModelState.AddModelError(
                            "TenNhaCungCap",
                            "Tên nhà cung cấp đã tồn tại."
                        );
                    }
                    else
                    {
                        if (model.TrangThai == null)
                            model.TrangThai = true;

                        db.NhaCungCaps.Add(model);
                        db.SaveChanges();

                        TempData["ThongBao"] =
                            "Thêm nhà cung cấp thành công.";

                        return RedirectToAction("Index");
                    }
                }
            }

            return View(model);
        }

        // =========================================
        // 4. XEM CHI TIẾT NHÀ CUNG CẤP
        // =========================================
        [HttpGet]
        public ActionResult Details(int id)
        {
            if (Session["userID"] == null)
                return RedirectToAction("Index", "Login");

            using (QUANLYEntities1 db = new QUANLYEntities1())
            {
                var nhaCungCap = db.NhaCungCaps
                    .FirstOrDefault(n => n.MaNhaCungCap == id);

                if (nhaCungCap == null)
                    return HttpNotFound("Không tìm thấy nhà cung cấp.");

                return View(nhaCungCap);
            }
        }

        [HttpGet]
        public ActionResult LichSuNhapHang(
            int id,
            string tuKhoa,
            DateTime? tuNgay,
            DateTime? denNgay)
        {
            if (Session["userID"] == null)
                return RedirectToAction("Index", "Login");

            if (!CoQuyenNhaCungCap())
            {
                return new HttpStatusCodeResult(
                    403,
                    "Bạn không có quyền xem lịch sử nhập hàng.");
            }

            using (QUANLYEntities1 db = new QUANLYEntities1())
            {
                // 1. Kiểm tra nhà cung cấp
                var nhaCungCap = db.NhaCungCaps
                    .FirstOrDefault(n => n.MaNhaCungCap == id);

                if (nhaCungCap == null)
                    return HttpNotFound("Không tìm thấy nhà cung cấp.");

                // 2. Lấy các phiếu nhập của nhà cung cấp
                var query = db.PhieuNhaps
                    .Include("TaiKhoan")
                    .Where(p => p.MaNhaCungCap == id);

                // 3. Tìm kiếm theo mã phiếu nhập
                if (!string.IsNullOrWhiteSpace(tuKhoa))
                {
                    tuKhoa = tuKhoa.Trim();

                    if (int.TryParse(tuKhoa, out int maPhieu))
                    {
                        query = query.Where(p =>
                            p.MaPhieuNhap == maPhieu);
                    }
                    else
                    {
                        query = query.Where(p =>
                            p.GhiChu.Contains(tuKhoa));
                    }
                }

                // 4. Lọc từ ngày
                if (tuNgay.HasValue)
                {
                    query = query.Where(p =>
                        p.NgayNhap >= tuNgay.Value.Date);
                }

                // 5. Lọc đến ngày
                if (denNgay.HasValue)
                {
                    DateTime ngayKetThuc =
                        denNgay.Value.Date.AddDays(1);

                    query = query.Where(p =>
                        p.NgayNhap < ngayKetThuc);
                }

                // 6. Sắp xếp phiếu mới nhất lên đầu
                var danhSach = query
                    .OrderByDescending(p => p.NgayNhap)
                    .ThenByDescending(p => p.MaPhieuNhap)
                    .ToList();

                // 7. Thông tin nhà cung cấp
                ViewBag.MaNhaCungCap = nhaCungCap.MaNhaCungCap;
                ViewBag.TenNhaCungCap = nhaCungCap.TenNhaCungCap;

                // 8. Thông tin tìm kiếm
                ViewBag.TuKhoa = tuKhoa;
                ViewBag.TuNgay = tuNgay?.ToString("yyyy-MM-dd");
                ViewBag.DenNgay = denNgay?.ToString("yyyy-MM-dd");

                // 9. Thống kê theo kết quả đang hiển thị
                ViewBag.TongSoPhieu = danhSach.Count;

                ViewBag.TongGiaTri = danhSach.Sum(p =>
                    p.TongTien ?? 0m);

                ViewBag.PhieuGanNhat = danhSach
                    .Select(p => (DateTime?)p.NgayNhap)
                    .FirstOrDefault();

                return View(danhSach);
            }
        }


        // =========================================
        // 5. SỬA NHÀ CUNG CẤP - GET
        // =========================================
        [HttpGet]
        public ActionResult Edit(int id)
        {
            if (Session["userID"] == null)
                return RedirectToAction("Index", "Login");

            using (QUANLYEntities1 db = new QUANLYEntities1())
            {
                var nhaCungCap = db.NhaCungCaps
                    .FirstOrDefault(n => n.MaNhaCungCap == id);

                if (nhaCungCap == null)
                    return HttpNotFound("Không tìm thấy nhà cung cấp.");

                return View(nhaCungCap);
            }
        }

        // =========================================
        // 6. SỬA NHÀ CUNG CẤP - POST
        // =========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(NhaCungCap model)
        {
            if (Session["userID"] == null)
                return RedirectToAction("Index", "Login");

            if (string.IsNullOrWhiteSpace(model.TenNhaCungCap))
            {
                ModelState.AddModelError(
                    "TenNhaCungCap",
                    "Vui lòng nhập tên nhà cung cấp."
                );
            }

            if (ModelState.IsValid)
            {
                using (QUANLYEntities1 db = new QUANLYEntities1())
                {
                    var nhaCungCap = db.NhaCungCaps
                        .FirstOrDefault(n =>
                            n.MaNhaCungCap == model.MaNhaCungCap);

                    if (nhaCungCap == null)
                        return HttpNotFound();

                    bool trungTen = db.NhaCungCaps.Any(n =>
                        n.MaNhaCungCap != model.MaNhaCungCap &&
                        n.TenNhaCungCap == model.TenNhaCungCap.Trim());

                    if (trungTen)
                    {
                        ModelState.AddModelError(
                            "TenNhaCungCap",
                            "Tên nhà cung cấp đã tồn tại."
                        );
                    }
                    else
                    {
                        nhaCungCap.TenNhaCungCap =
                            model.TenNhaCungCap.Trim();

                        nhaCungCap.DiaChi = model.DiaChi;
                        nhaCungCap.SoDienThoai = model.SoDienThoai;
                        nhaCungCap.Email = model.Email;
                        nhaCungCap.MaSoThue = model.MaSoThue;
                        nhaCungCap.TrangThai = model.TrangThai;

                        db.SaveChanges();

                        TempData["ThongBao"] =
                            "Cập nhật nhà cung cấp thành công.";

                        return RedirectToAction("Index");
                    }
                }
            }

            return View(model);
        }

        // =========================================
        // 7. XÓA NHÀ CUNG CẤP
        // =========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            if (Session["userID"] == null)
                return RedirectToAction("Index", "Login");

            using (QUANLYEntities1 db = new QUANLYEntities1())
            {
                var nhaCungCap = db.NhaCungCaps
                    .FirstOrDefault(n => n.MaNhaCungCap == id);

                if (nhaCungCap == null)
                    return HttpNotFound();

                bool daCoPhieuNhap = db.PhieuNhaps
                    .Any(p => p.MaNhaCungCap == id);

                if (daCoPhieuNhap)
                {
                    TempData["Loi"] =
                        "Không thể xóa nhà cung cấp đã có phiếu nhập. " +
                        "Bạn có thể chuyển sang trạng thái ngừng hoạt động.";

                    return RedirectToAction("Index");
                }

                db.NhaCungCaps.Remove(nhaCungCap);
                db.SaveChanges();

                TempData["ThongBao"] =
                    "Xóa nhà cung cấp thành công.";

                return RedirectToAction("Index");
            }
        }
    }
}
