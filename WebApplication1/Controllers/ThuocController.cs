using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.IO;
using System.Web;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    public class ThuocController : Controller
    {
        // KIỂM TRA QUYỀN TRUY CẬP
        private bool CoQuyenTruyCap()
        {
            if (Session["userID"] == null)
                return false;

            var roles = Session["roles"] as List<string>;

            return roles != null &&
                   (roles.Contains("Admin") ||
                    roles.Contains("NhanVienKho"));
        }

        // NẠP DANH SÁCH DANH MỤC THUỐC
        private void NapDanhMuc(int? maDanhMuc = null)
        {
            using (QUANLYEntities1 db = new QUANLYEntities1())
            {
                ViewBag.MaDanhMuc = new SelectList(
                    db.DanhMucThuocs
                        .Where(x => x.TrangThai == true)
                        .OrderBy(x => x.TenDanhMuc)
                        .ToList(),
                    "MaDanhMuc",
                    "TenDanhMuc",
                    maDanhMuc
                );
            }
        }

        // ==========================================
        // 1. DANH SÁCH THUỐC
        // ==========================================
        [HttpGet]
        public ActionResult Index(string search)
        {
            if (Session["userID"] == null)
                return RedirectToAction("Index", "Login");

            if (!CoQuyenTruyCap())
            {
                return new HttpStatusCodeResult(
                    403,
                    "Bạn không có quyền quản lý thuốc."
                );
            }

            using (QUANLYEntities1 db = new QUANLYEntities1())
            {
                var danhSach = db.Thuocs
       .Include("LoThuocs")
       .AsQueryable();
                if (!string.IsNullOrWhiteSpace(search))
                {
                    search = search.Trim();

                    int maThuoc;
                    bool laMaThuoc = int.TryParse(search, out maThuoc);

                    danhSach = danhSach.Where(t =>
                        t.TenThuoc.Contains(search) ||
                        (laMaThuoc && t.MaThuoc == maThuoc)
                    );
                }

                ViewBag.Search = search;

                return View(
                    danhSach
                        .OrderBy(t => t.TenThuoc)
                        .ToList()
                );
            }
        }

        // ==========================================
        // 2. HIỂN THỊ FORM THÊM THUỐC
        // ==========================================
        [HttpGet]
        public ActionResult Create()
        {
            if (Session["userID"] == null)
                return RedirectToAction("Index", "Login");

            if (!CoQuyenTruyCap())
                return new HttpStatusCodeResult(403);

            NapDanhMuc();

            return View();
        }

        // ==========================================
        // 3. LƯU THUỐC MỚI
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Thuoc model, HttpPostedFileBase anhFile)
        {
            if (Session["userID"] == null)
                return RedirectToAction("Index", "Login");

            if (!CoQuyenTruyCap())
                return new HttpStatusCodeResult(403);

            if (model == null)
                return new HttpStatusCodeResult(
                    400,
                    "Dữ liệu thuốc không hợp lệ."
                );

            if (string.IsNullOrWhiteSpace(model.TenThuoc))
            {
                ModelState.AddModelError(
                    "TenThuoc",
                    "Vui lòng nhập tên thuốc."
                );
            }

            if (string.IsNullOrWhiteSpace(model.DonViTinh))
            {
                ModelState.AddModelError(
                    "DonViTinh",
                    "Vui lòng nhập đơn vị tính."
                );
            }

            if (model.GiaNhap < 0)
            {
                ModelState.AddModelError(
                    "GiaNhap",
                    "Giá nhập không được âm."
                );
            }

            if (model.GiaBan < 0)
            {
                ModelState.AddModelError(
                    "GiaBan",
                    "Giá bán không được âm."
                );
            }

            if (model.MucTonToiThieu < 0)
            {
                ModelState.AddModelError(
                    "MucTonToiThieu",
                    "Mức tồn tối thiểu không được âm."
                );
            }
            // KIỂM TRA HÌNH ẢNH THUỐC
            if (anhFile != null && anhFile.ContentLength > 0)
            {
                string duoiFile = Path.GetExtension(anhFile.FileName)
                    .ToLowerInvariant();

                string[] dinhDangChoPhep = { ".jpg", ".jpeg", ".png" };

                if (!dinhDangChoPhep.Contains(duoiFile))
                {
                    ModelState.AddModelError(
                        "AnhThuoc",
                        "Chỉ chấp nhận hình ảnh JPG, JPEG hoặc PNG."
                    );
                }

                if (anhFile.ContentLength > 2 * 1024 * 1024)
                {
                    ModelState.AddModelError(
                        "AnhThuoc",
                        "Dung lượng hình ảnh không được vượt quá 2MB."
                    );
                }

                if (anhFile.ContentType != "image/jpeg" &&
                    anhFile.ContentType != "image/png")
                {
                    ModelState.AddModelError(
                        "AnhThuoc",
                        "File tải lên không phải định dạng hình ảnh hợp lệ."
                    );
                }
            }

            using (QUANLYEntities1 db = new QUANLYEntities1())
            {
                bool danhMucTonTai = db.DanhMucThuocs.Any(
                    x => x.MaDanhMuc == model.MaDanhMuc &&
                         x.TrangThai == true
                );

                if (!danhMucTonTai)
                {
                    ModelState.AddModelError(
                        "MaDanhMuc",
                        "Vui lòng chọn danh mục đang hoạt động."
                    );
                }

                if (!ModelState.IsValid)
                {
                    NapDanhMuc(model.MaDanhMuc);
                    return View(model);
                }

                var thuocMoi = new Thuoc
                {
                    TenThuoc = model.TenThuoc.Trim(),
                    MaDanhMuc = model.MaDanhMuc,
                    DonViTinh = model.DonViTinh.Trim(),
                    HoatChat = model.HoatChat,
                    HamLuong = model.HamLuong,
                    GiaNhap = model.GiaNhap,
                    GiaBan = model.GiaBan,
                    SoLuongTon = 0,
                    MucTonToiThieu = model.MucTonToiThieu,
                    TrangThai = true,
                    NgayTao = DateTime.Now
                };

                // LƯU HÌNH ẢNH NẾU NGƯỜI DÙNG CÓ CHỌN
                if (anhFile != null && anhFile.ContentLength > 0)
                {
                    string thuMucAnh = Server.MapPath("~/Include/Images/Thuoc/");

                    if (!Directory.Exists(thuMucAnh))
                    {
                        Directory.CreateDirectory(thuMucAnh);
                    }

                    string duoiFile = Path.GetExtension(anhFile.FileName)
                        .ToLowerInvariant();

                    string tenFile = Guid.NewGuid().ToString("N") + duoiFile;

                    string duongDanLuu = Path.Combine(thuMucAnh, tenFile);

                    anhFile.SaveAs(duongDanLuu);

                    thuocMoi.AnhThuoc = "~/Include/Images/Thuoc/" + tenFile;
                }

                db.Thuocs.Add(thuocMoi);
                db.SaveChanges();
            }

            TempData["ThongBao"] = "Thêm thuốc thành công.";

            return RedirectToAction("Index");
        }

        // ==========================================
        // 4. HIỂN THỊ FORM SỬA THUỐC
        // CHỈ GIỮ MỘT ACTION GET EDIT
        // ==========================================
        [HttpGet]
        public ActionResult Edit(int? id)
        {
            if (Session["userID"] == null)
                return RedirectToAction("Index", "Login");

            if (!CoQuyenTruyCap())
                return new HttpStatusCodeResult(403);

            if (id == null)
                return RedirectToAction("Index");

            using (QUANLYEntities1 db = new QUANLYEntities1())
            {
                var thuoc = db.Thuocs.Find(id.Value);

                if (thuoc == null)
                    return HttpNotFound("Không tìm thấy thuốc.");

                NapDanhMuc(thuoc.MaDanhMuc);

                return View(thuoc);
            }
        }

        // ==========================================
        // 5. LƯU THAY ĐỔI THUỐC
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Thuoc model, HttpPostedFileBase anhFile)
        {
            if (Session["userID"] == null)
                return RedirectToAction("Index", "Login");

            if (!CoQuyenTruyCap())
                return new HttpStatusCodeResult(403);

            if (model == null)
                return new HttpStatusCodeResult(
                    400,
                    "Dữ liệu thuốc không hợp lệ."
                );

            if (string.IsNullOrWhiteSpace(model.TenThuoc))
            {
                ModelState.AddModelError(
                    "TenThuoc",
                    "Vui lòng nhập tên thuốc."
                );
            }

            if (string.IsNullOrWhiteSpace(model.DonViTinh))
            {
                ModelState.AddModelError(
                    "DonViTinh",
                    "Vui lòng nhập đơn vị tính."
                );
            }

            if (model.GiaNhap < 0)
            {
                ModelState.AddModelError(
                    "GiaNhap",
                    "Giá nhập không được âm."
                );
            }

            if (model.GiaBan < 0)
            {
                ModelState.AddModelError(
                    "GiaBan",
                    "Giá bán không được âm."
                );
            }

            if (model.MucTonToiThieu < 0)
            {
                ModelState.AddModelError(
                    "MucTonToiThieu",
                    "Mức tồn tối thiểu không được âm."
                );
            }
            // KIỂM TRA HÌNH ẢNH MỚI
            if (anhFile != null && anhFile.ContentLength > 0)
            {
                string duoiFile = Path.GetExtension(anhFile.FileName)
                    .ToLowerInvariant();

                string[] dinhDangChoPhep = { ".jpg", ".jpeg", ".png" };

                if (!dinhDangChoPhep.Contains(duoiFile))
                {
                    ModelState.AddModelError(
                        "AnhThuoc",
                        "Chỉ chấp nhận hình ảnh JPG, JPEG hoặc PNG."
                    );
                }

                if (anhFile.ContentLength > 2 * 1024 * 1024)
                {
                    ModelState.AddModelError(
                        "AnhThuoc",
                        "Dung lượng hình ảnh không được vượt quá 2MB."
                    );
                }

                if (anhFile.ContentType != "image/jpeg" &&
                    anhFile.ContentType != "image/png")
                {
                    ModelState.AddModelError(
                        "AnhThuoc",
                        "File tải lên không phải định dạng hình ảnh hợp lệ."
                    );
                }
            }

            using (QUANLYEntities1 db = new QUANLYEntities1())
            {
                var thuoc = db.Thuocs.Find(model.MaThuoc);

                if (thuoc == null)
                    return HttpNotFound("Không tìm thấy thuốc.");

                bool danhMucTonTai = db.DanhMucThuocs.Any(
                    x => x.MaDanhMuc == model.MaDanhMuc &&
                         x.TrangThai == true
                );

                if (!danhMucTonTai)
                {
                    ModelState.AddModelError(
                        "MaDanhMuc",
                        "Vui lòng chọn danh mục đang hoạt động."
                    );
                }

                if (!ModelState.IsValid)
                {
                    model.AnhThuoc = thuoc.AnhThuoc;

                    NapDanhMuc(model.MaDanhMuc);

                    return View(model);
                }

                // Chỉ cập nhật thông tin được phép chỉnh sửa.
                // Không thay đổi mã thuốc, tồn kho, trạng thái
                // hoặc ngày tạo.
                thuoc.TenThuoc = model.TenThuoc.Trim();
                thuoc.MaDanhMuc = model.MaDanhMuc;
                thuoc.DonViTinh = model.DonViTinh.Trim();
                thuoc.HoatChat = model.HoatChat;
                thuoc.HamLuong = model.HamLuong;
                thuoc.GiaNhap = model.GiaNhap;
                thuoc.GiaBan = model.GiaBan;
                thuoc.MucTonToiThieu = model.MucTonToiThieu;
                // LƯU ẢNH MỚI NẾU NGƯỜI DÙNG CÓ CHỌN
                if (anhFile != null && anhFile.ContentLength > 0)
                {
                    string thuMucAnh = Server.MapPath("~/Include/Images/Thuoc/");

                    if (!Directory.Exists(thuMucAnh))
                    {
                        Directory.CreateDirectory(thuMucAnh);
                    }

                    string duoiFile = Path.GetExtension(anhFile.FileName)
                        .ToLowerInvariant();

                    string tenFile = Guid.NewGuid().ToString("N") + duoiFile;

                    string duongDanLuu = Path.Combine(thuMucAnh, tenFile);

                    anhFile.SaveAs(duongDanLuu);

                    thuoc.AnhThuoc = "~/Include/Images/Thuoc/" + tenFile;
                }
                db.SaveChanges();
            }

            TempData["ThongBao"] = "Cập nhật thuốc thành công.";

            return RedirectToAction("Index");
        }

        // ==========================================
        // 6. NGỪNG KINH DOANH THUỐC
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult NgungKinhDoanh(int? id)
        {
            if (Session["userID"] == null)
                return RedirectToAction("Index", "Login");

            if (!CoQuyenTruyCap())
                return new HttpStatusCodeResult(403);

            if (id == null)
                return RedirectToAction("Index");

            using (QUANLYEntities1 db = new QUANLYEntities1())
            {
                var thuoc = db.Thuocs.Find(id.Value);

                if (thuoc == null)
                    return HttpNotFound("Không tìm thấy thuốc.");

                if (!thuoc.TrangThai)
                {
                    TempData["ThongBao"] =
                        "Thuốc đã ngừng kinh doanh trước đó.";

                    return RedirectToAction("Index");
                }

                thuoc.TrangThai = false;

                db.SaveChanges();
            }

            TempData["ThongBao"] =
                "Đã ngừng kinh doanh thuốc thành công.";

            return RedirectToAction("Index");
        }
    
    
[HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult KinhDoanhLai(int? id)
        {
            if (Session["userID"] == null)
                return RedirectToAction("Index", "Login");

            if (!CoQuyenTruyCap())
                return new HttpStatusCodeResult(403);

            if (id == null)
                return RedirectToAction("Index");

            using (QUANLYEntities1 db = new QUANLYEntities1())
            {
                var thuoc = db.Thuocs.Find(id.Value);

                if (thuoc == null)
                    return HttpNotFound("Không tìm thấy thuốc.");

                if (thuoc.TrangThai)
                {
                    TempData["ThongBao"] =
                        "Thuốc này đang được kinh doanh.";

                    return RedirectToAction("Index");
                }

                bool danhMucHoatDong = db.DanhMucThuocs.Any(
                    x => x.MaDanhMuc == thuoc.MaDanhMuc
                         && x.TrangThai == true
                );

                if (!danhMucHoatDong)
                {
                    TempData["ThongBao"] =
                        "Không thể kinh doanh lại vì danh mục thuốc đã ngừng hoạt động.";

                    return RedirectToAction("Index");
                }

                thuoc.TrangThai = true;

                db.SaveChanges();
            }

            TempData["ThongBao"] =
                "Đã kích hoạt kinh doanh lại thuốc thành công.";

            return RedirectToAction("Index");
        

        }

    }
}



