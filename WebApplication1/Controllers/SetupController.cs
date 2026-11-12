
using System;
using System.Data;
using System.Linq;
using System.Security.Cryptography;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace WebApplication1.Controllers
{
    public class SetupController : Controller
    {
        public class SetupViewModel
        {
            [Required]
            [StringLength(50)]
            public string Username { get; set; }

            [Required]
            [StringLength(150)]
            public string FullName { get; set; }

            [Required]
            [StringLength(100, MinimumLength = 12)]
            public string Password { get; set; }

            [Required]
            [System.ComponentModel.DataAnnotations.Compare(
    "Password",
    ErrorMessage = "Mật khẩu xác nhận không khớp."
)]
            public string ConfirmPassword { get; set; }
        }

        [HttpGet]
        public ActionResult Index()
        {
            using (var db = new MOCBREWEntities())
            {
                if (db.Accounts.Any())
                    return new HttpStatusCodeResult(404);

                return View(new SetupViewModel());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Index(SetupViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            using (var db = new MOCBREWEntities())
            using (var transaction = db.Database.BeginTransaction(
                IsolationLevel.Serializable))
            {
                if (db.Accounts.Any())
                    return new HttpStatusCodeResult(404);

                var managerRole = db.Roles.SingleOrDefault(
                    r => r.RoleId == 2 && r.IsActive);

                if (managerRole == null)
                {
                    ModelState.AddModelError("",
                        "Không tìm thấy vai trò Manager đang hoạt động.");

                    return View(model);
                }

                string username = model.Username.Trim();

                if (db.Accounts.Any(a => a.Username == username))
                {
                    ModelState.AddModelError("Username",
                        "Tên đăng nhập đã tồn tại.");

                    return View(model);
                }

                var account = new Account
                {
                    RoleId = 2,
                    Username = username,
                    PasswordHash = HashPassword(model.Password),
                    FullName = model.FullName.Trim(),
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };

                db.Accounts.Add(account);
                db.SaveChanges();

                transaction.Commit();

                return Content(
                    "TẠO TÀI KHOẢN MANAGER THÀNH CÔNG! " +
                    "Hãy chuyển sang chức năng đăng nhập MỘC BREW.");
            }
        }

        private static string HashPassword(string password)
        {
            byte[] salt = new byte[16];

            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(salt);
            }

            const int iterations = 100000;

            using (var pbkdf2 = new Rfc2898DeriveBytes(
                password, salt, iterations))
            {
                byte[] hash = pbkdf2.GetBytes(32);

                return "PBKDF2$" + iterations + "$" +
                    Convert.ToBase64String(salt) + "$" +
                    Convert.ToBase64String(hash);
            }
        }
    }
}
