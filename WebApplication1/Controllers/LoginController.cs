
using System;
using System.Linq;
using System.Web.Mvc;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    public class LoginController : Controller
    {
        // GET: Login
        public ActionResult Index()
        {
            return View();
        }

        // POST: Login/Autherize
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Autherize(TaiKhoan taiKhoanModel)
        {
            // Kiểm tra dữ liệu đầu vào
            if (taiKhoanModel == null ||
                string.IsNullOrWhiteSpace(taiKhoanModel.TenDangNhap) ||
                string.IsNullOrWhiteSpace(taiKhoanModel.MatKhau))
            {
                ViewBag.LoginErrorMessage =
                    "Vui lòng nhập đầy đủ tên đăng nhập và mật khẩu.";

                return View("Index", taiKhoanModel);
            }

            using (QUANLYEntities1 db = new QUANLYEntities1())
            {
                // 1. Kiểm tra tài khoản và mật khẩu
                var taiKhoanDetails = db.TaiKhoans
                    .FirstOrDefault(x =>
                        x.TenDangNhap == taiKhoanModel.TenDangNhap &&
                        x.MatKhau == taiKhoanModel.MatKhau &&
                        x.TrangThai == true);

                // 2. Tài khoản không hợp lệ
                if (taiKhoanDetails == null)
                {
                    ViewBag.LoginErrorMessage =
                        "Tên đăng nhập hoặc mật khẩu không đúng.";

                    return View("Index", taiKhoanModel);
                }

                // 3. Lấy danh sách vai trò của tài khoản
                var roleNames = db.Database.SqlQuery<string>(
                    @"
                    SELECT vt.TenVaiTro
                    FROM TaiKhoan_VaiTro tkv
                    INNER JOIN VaiTro vt
                        ON tkv.MaVaiTro = vt.MaVaiTro
                    WHERE tkv.MaTaiKhoan = @p0
                      AND vt.TrangThai = 1
                    ",
                    taiKhoanDetails.MaTaiKhoan
                ).ToList();

                // 4. Kiểm tra tài khoản đã được phân quyền chưa
                if (roleNames == null || roleNames.Count == 0)
                {
                    ViewBag.LoginErrorMessage =
                        "Tài khoản chưa được phân quyền. Vui lòng liên hệ quản trị viên.";

                    return View("Index", taiKhoanModel);
                }

                // 5. Lưu thông tin tài khoản vào Session
                Session["userID"] = taiKhoanDetails.MaTaiKhoan;
                Session["username"] = taiKhoanDetails.TenDangNhap;
                Session["userName"] = taiKhoanDetails.TenDangNhap;
                Session["fullName"] = taiKhoanDetails.HoTen;

                // Lưu danh sách vai trò
                Session["roles"] = roleNames;

                // Lưu vai trò chính
                Session["role"] = roleNames.First();

                // 6. Điều hướng theo vai trò

                // Quản trị viên
                if (roleNames.Contains("Admin"))
                {
                    return RedirectToAction("Index", "Admin");
                }

                // Nhân viên kho
                if (roleNames.Contains("NhanVienKho"))
                {
                    return RedirectToAction("Index", "Kho");
                }

                // Nhân viên bán hàng
                if (roleNames.Contains("NhanVienBanHang"))
                {
                    return RedirectToAction("Index", "BanHang");
                }

                // Nhân viên nhập hàng
                if (roleNames.Contains("NhanVienNhapHang"))
                {
                    return RedirectToAction("Index", "NhapHang");
                }

                // Nhân viên báo cáo
                if (roleNames.Contains("NhanVienBaoCao"))
                {
                    return RedirectToAction("Index", "BaoCao");
                }

                // Vai trò chưa được cấu hình
                Session.Clear();

                ViewBag.LoginErrorMessage =
                    "Vai trò của tài khoản chưa được cấu hình.";

                return View("Index", taiKhoanModel);
            }
        }

        // Đăng xuất
        public ActionResult LogOut()
        {
            Session.Clear();
            Session.Abandon();

            return RedirectToAction("Index", "Login");
        }
    }
}
