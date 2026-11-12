
using System;
using System.Linq;
using System.Collections.Generic;
using System.Web.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;

namespace WebApplication1.Controllers
{
    public class MocBrewStaffController : Controller
    {
        // ==========================================
        // 1. DANH SÁCH NHÂN VIÊN
        // ==========================================

        [HttpGet]
        public ActionResult Index()
        {
            // Kiểm tra đăng nhập
            if (Session["MOCBREW_AccountId"] == null)
            {
                return RedirectToAction("Login", "MocBrewAuth");
            }

            using (var db = new MOCBREWEntities())
            {
                // Kiểm tra quyền Manager
                if (!IsManager(db))
                {
                    return RedirectToAction("Index", "MocBrewDashboard");
                }

                // Lấy danh sách tài khoản Staff
                var staffList = db.Accounts
                    .Where(a => a.RoleId == 3)
                    .OrderBy(a => a.EmployeeCode)
                    .Select(a => new StaffListItem
                    {
                        AccountId = a.AccountId,
                        EmployeeCode = a.EmployeeCode,
                        FullName = a.FullName,
                        Username = a.Username,
                        Email = a.Email,
                        Phone = a.Phone,
                        IsActive = a.IsActive,
                        CreatedAt = a.CreatedAt
                    })
                    .ToList();

                return View(staffList);
            }
        }

        // ==========================================
        // 2. HIỂN THỊ FORM THÊM NHÂN VIÊN
        // ==========================================

        [HttpGet]
        public ActionResult Create()
        {
            if (Session["MOCBREW_AccountId"] == null)
                return RedirectToAction("Login", "MocBrewAuth");

            using (var db = new MOCBREWEntities())
            {
                if (!IsManager(db))
                    return RedirectToAction("Index", "MocBrewDashboard");
            }

            return View(new CreateStaffViewModel());
        }


        // ==========================================
        // 3. XỬ LÝ TẠO TÀI KHOẢN NHÂN VIÊN
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(CreateStaffViewModel model)
        {
            if (Session["MOCBREW_AccountId"] == null)
                return RedirectToAction("Login", "MocBrewAuth");

            using (var db = new MOCBREWEntities())
            {
                if (!IsManager(db))
                    return RedirectToAction("Index", "MocBrewDashboard");

                if (!ModelState.IsValid)
                    return View(model);

                string username = model.Username.Trim();
                string employeeCode = model.EmployeeCode.Trim();

                if (db.Accounts.Any(a => a.Username == username))
                {
                    ModelState.AddModelError(
                        "Username",
                        "Tên đăng nhập đã tồn tại.");

                    return View(model);
                }

                if (db.Accounts.Any(a => a.EmployeeCode == employeeCode))
                {
                    ModelState.AddModelError(
                        "EmployeeCode",
                        "Mã nhân viên đã tồn tại.");

                    return View(model);
                }

                bool staffRoleActive = db.Roles.Any(r =>
                    r.RoleId == 3 && r.IsActive);

                if (!staffRoleActive)
                {
                    ModelState.AddModelError(
                        "",
                        "Vai trò Staff chưa được kích hoạt.");

                    return View(model);
                }

                var account = new Account
                {
                    RoleId = 3,
                    EmployeeCode = employeeCode,
                    FullName = model.FullName.Trim(),
                    Username = username,
                    PasswordHash = HashPassword(model.Password),
                    Email = string.IsNullOrWhiteSpace(model.Email)
                        ? null : model.Email.Trim(),
                    Phone = string.IsNullOrWhiteSpace(model.Phone)
                        ? null : model.Phone.Trim(),
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };

                db.Accounts.Add(account);
                db.SaveChanges();

                TempData["StaffMessage"] =
                    "Tạo tài khoản nhân viên thành công!";

                return RedirectToAction("Index");
            }
        }


        // ==========================================
        // 4. MÃ HÓA MẬT KHẨU
        // ==========================================

        private string HashPassword(string password)
        {
            const int iterations = 100000;

            byte[] salt = new byte[16];

            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            using (var derive = new Rfc2898DeriveBytes(
                password, salt, iterations))
            {
                byte[] hash = derive.GetBytes(32);

                return "PBKDF2$" +
                       iterations + "$" +
                       Convert.ToBase64String(salt) + "$" +
                       Convert.ToBase64String(hash);
            }
        }


        // ==========================================
        // 5. MODEL NHẬP THÔNG TIN NHÂN VIÊN
        // ==========================================

        public class CreateStaffViewModel
        {
            [Required(ErrorMessage = "Vui lòng nhập mã nhân viên.")]
            [StringLength(20)]
            public string EmployeeCode { get; set; }

            [Required(ErrorMessage = "Vui lòng nhập họ và tên.")]
            [StringLength(150)]
            public string FullName { get; set; }

            [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập.")]
            [StringLength(50)]
            public string Username { get; set; }

            [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
            [StringLength(100, MinimumLength = 8,
                ErrorMessage = "Mật khẩu phải có ít nhất 8 ký tự.")]
            public string Password { get; set; }

            [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu.")]
            [System.ComponentModel.DataAnnotations.Compare(
                "Password",
                ErrorMessage = "Mật khẩu xác nhận không khớp.")]
            public string ConfirmPassword { get; set; }

            [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
            [StringLength(100)]
            public string Email { get; set; }

            [StringLength(15)]
            public string Phone { get; set; }
        }


        // ==========================================
        // 2. KIỂM TRA QUYỀN MANAGER
        // ==========================================

        private bool IsManager(MOCBREWEntities db)
        {
            int accountId;
            int roleId;

            if (!int.TryParse(
                Convert.ToString(Session["MOCBREW_AccountId"]),
                out accountId))
            {
                return false;
            }

            if (!int.TryParse(
                Convert.ToString(Session["MOCBREW_RoleId"]),
                out roleId))
            {
                return false;
            }

            // Chỉ chấp nhận RoleId = 2 (Manager)
            if (roleId != 2)
            {
                return false;
            }

            // Kiểm tra tài khoản còn hoạt động
            bool accountActive = db.Accounts.Any(a =>
                a.AccountId == accountId &&
                a.RoleId == 2 &&
                a.IsActive);

            // Kiểm tra vai trò Manager còn hoạt động
            bool roleActive = db.Roles.Any(r =>
                r.RoleId == 2 &&
                r.IsActive);

            // Kiểm tra quyền STAFF_MANAGE trực tiếp từ database
            int permissionCount = db.Database.SqlQuery<int>(
                @"SELECT COUNT(1)
      FROM dbo.RolePermissions rp
      INNER JOIN dbo.Permissions p
          ON p.PermissionId = rp.PermissionId
      WHERE rp.RoleId = @p0
        AND p.PermissionCode = @p1",
                2,
                "STAFF_MANAGE"
            ).SingleOrDefault();

            bool hasPermission = permissionCount > 0;

            return accountActive && roleActive && hasPermission;
        }

        // ==========================================
        // 3. MODEL HIỂN THỊ DANH SÁCH NHÂN VIÊN
        // ==========================================

        public class StaffListItem
        {
            public int AccountId { get; set; }
            public string EmployeeCode { get; set; }
            public string FullName { get; set; }
            public string Username { get; set; }
            public string Email { get; set; }
            public string Phone { get; set; }
            public bool IsActive { get; set; }
            public DateTime CreatedAt { get; set; }
        }
    }
}
