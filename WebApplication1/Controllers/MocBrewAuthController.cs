
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Web.Mvc;

namespace WebApplication1.Controllers
{
    public class MocBrewAuthController : Controller
    {
        // =========================================
        // 1. HIỂN THỊ TRANG ĐĂNG NHẬP
        // =========================================
        [HttpGet]
        public ActionResult Login()
        {
            // Nếu đã đăng nhập MỘC BREW thì chuyển đến Dashboard
            if (Session["MOCBREW_AccountId"] != null)
            {
                return RedirectToAction("Index", "MocBrewDashboard");
            }

            return View();
        }

        // =========================================
        // 2. XỬ LÝ ĐĂNG NHẬP
        // =========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(string username, string password)
        {
            username = (username ?? "").Trim();

            if (string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "Vui lòng nhập đầy đủ tên đăng nhập và mật khẩu.";
                return View();
            }

            using (var db = new MOCBREWEntities())
            {
                var account = db.Accounts
                    .Include("Role")
                    .SingleOrDefault(a =>
                        a.Username == username &&
                        a.IsActive);

                if (account == null ||
                    account.Role == null ||
                    !account.Role.IsActive ||
                    !VerifyPassword(password, account.PasswordHash))
                {
                    ViewBag.Error = "Tên đăng nhập hoặc mật khẩu không chính xác.";
                    return View();
                }

                // Xóa thông tin đăng nhập MỘC BREW cũ nếu có
                Session.Remove("MOCBREW_AccountId");
                Session.Remove("MOCBREW_Username");
                Session.Remove("MOCBREW_FullName");
                Session.Remove("MOCBREW_RoleId");
                Session.Remove("MOCBREW_RoleName");

                // Lưu thông tin đăng nhập riêng cho MỘC BREW
                Session["MOCBREW_AccountId"] = account.AccountId;
                Session["MOCBREW_Username"] = account.Username;
                Session["MOCBREW_FullName"] = account.FullName;
                Session["MOCBREW_RoleId"] = account.RoleId;
                Session["MOCBREW_RoleName"] = account.Role.RoleName;

                return RedirectToAction("Index", "MocBrewDashboard");
            }
        }

        // =========================================
        // 3. ĐĂNG XUẤT
        // =========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
            Session.Remove("MOCBREW_AccountId");
            Session.Remove("MOCBREW_Username");
            Session.Remove("MOCBREW_FullName");
            Session.Remove("MOCBREW_RoleId");
            Session.Remove("MOCBREW_RoleName");

            return RedirectToAction("Login", "MocBrewAuth");
        }

        // =========================================
        // 4. KIỂM TRA MẬT KHẨU PBKDF2
        // =========================================
        private static bool VerifyPassword(
            string password,
            string storedHash)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(storedHash))
                    return false;

                string[] parts = storedHash.Split('$');

                if (parts.Length != 4 || parts[0] != "PBKDF2")
                    return false;

                int iterations;

                if (!int.TryParse(parts[1], out iterations) ||
                    iterations < 10000 ||
                    iterations > 1000000)
                {
                    return false;
                }

                byte[] salt = Convert.FromBase64String(parts[2]);
                byte[] expectedHash = Convert.FromBase64String(parts[3]);

                if (salt.Length != 16 || expectedHash.Length != 32)
                    return false;

                using (var pbkdf2 = new Rfc2898DeriveBytes(
                    password,
                    salt,
                    iterations))
                {
                    byte[] actualHash = pbkdf2.GetBytes(32);

                    // So sánh hai chuỗi byte
                    // Không dừng ngay khi gặp byte khác nhau
                    int difference = 0;

                    for (int i = 0; i < expectedHash.Length; i++)
                    {
                        difference |= actualHash[i] ^ expectedHash[i];
                    }

                    return difference == 0;
                }
            }
            catch
            {
                // Hash không hợp lệ thì từ chối đăng nhập
                return false;
            }
        }
    }
}
