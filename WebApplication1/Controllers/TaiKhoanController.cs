using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using WebApplication1.Models;
using WebApplication1.Models.ViewModels;

namespace WebApplication1.Controllers
{
    public class TaiKhoanController : Controller
    {
        // =========================================================
        // KIỂM TRA ADMIN
        // =========================================================
        private bool IsAdmin()
        {
            if (Session["userID"] == null)
                return false;

            if (Session["role"] == null)
                return false;

            return Session["role"].ToString() == "Admin";
        }


        // =========================================================
        // DANH SÁCH TÀI KHOẢN
        // =========================================================
        public ActionResult Index(string search)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Index", "Login");
            }

            using (QUANLYEntities1 db = new QUANLYEntities1())
            {
                var danhSach = db.TaiKhoans.AsQueryable();

                if (!string.IsNullOrWhiteSpace(search))
                {
                    danhSach = danhSach.Where(x =>
                        x.TenDangNhap.Contains(search) ||
                        x.HoTen.Contains(search) ||
                        x.Email.Contains(search));
                }

                var result = danhSach
                    .OrderBy(x => x.MaTaiKhoan)
                    .ToList();

                // Lấy vai trò thực tế
                var danhSachVaiTro = db.Database.SqlQuery<RoleAccountViewModel>(
                    @"
                    SELECT
                        tkv.MaTaiKhoan,
                        vt.TenVaiTro
                    FROM TaiKhoan_VaiTro tkv
                    INNER JOIN VaiTro vt
                        ON tkv.MaVaiTro = vt.MaVaiTro
                    WHERE vt.TrangThai = 1
                    "
                ).ToList();

                var vaiTroTheoTaiKhoan = danhSachVaiTro
                    .GroupBy(x => x.MaTaiKhoan)
                    .ToDictionary(
                        g => g.Key,
                        g => g.First().TenVaiTro
                    );

                ViewBag.Search = search;
                ViewBag.VaiTroTheoTaiKhoan = vaiTroTheoTaiKhoan;

                return View(result);
            }
        }


        // =========================================================
        // GET: MÀN HÌNH CHỈNH SỬA TÀI KHOẢN
        // /TaiKhoan/Edit/4
        // =========================================================
        [HttpGet]
        public ActionResult Edit(int? id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Index", "Login");
            }

            if (id == null)
            {
                return RedirectToAction("Index");
            }

            using (QUANLYEntities1 db = new QUANLYEntities1())
            {
                var taiKhoan = db.TaiKhoans
                    .FirstOrDefault(x => x.MaTaiKhoan == id.Value);

                if (taiKhoan == null)
                {
                    return HttpNotFound();
                }

                return View(taiKhoan);
            }
        }


        // =========================================================
        // POST: LƯU THÔNG TIN TÀI KHOẢN
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(
            int MaTaiKhoan,
            string HoTen,
            string Email,
            string SoDienThoai,
            bool TrangThai)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Index", "Login");
            }

            using (QUANLYEntities1 db = new QUANLYEntities1())
            {
                var taiKhoan = db.TaiKhoans
                    .FirstOrDefault(x => x.MaTaiKhoan == MaTaiKhoan);

                if (taiKhoan == null)
                {
                    return HttpNotFound();
                }

                // Chỉ cập nhật các thông tin được phép sửa
                taiKhoan.HoTen = HoTen;
                taiKhoan.Email = Email;
                taiKhoan.SoDienThoai = SoDienThoai;
                taiKhoan.TrangThai = TrangThai;

                // Không thay đổi tên đăng nhập và mật khẩu
                db.SaveChanges();

                TempData["Success"] =
                    "Cập nhật thông tin tài khoản thành công.";

                return RedirectToAction("Index");
            }
        }

        // =========================================================
        // GET: MÀN HÌNH PHÂN QUYỀN
        // /TaiKhoan/PhanQuyen/4
        // =========================================================
        public ActionResult PhanQuyen(int id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Index", "Login");
            }

            using (QUANLYEntities1 db = new QUANLYEntities1())
            {
                // -------------------------------------------------
                // 1. Lấy tài khoản
                // -------------------------------------------------
                var taiKhoan = db.TaiKhoans
                    .FirstOrDefault(x => x.MaTaiKhoan == id);

                if (taiKhoan == null)
                {
                    return HttpNotFound();
                }


                // -------------------------------------------------
                // 2. Lấy danh sách vai trò
                // -------------------------------------------------
                var vaiTros = db.Database.SqlQuery<VaiTroItemViewModel>(
                    @"
                    SELECT
                        MaVaiTro,
                        TenVaiTro,
                        MoTa
                    FROM VaiTro
                    WHERE TrangThai = 1
                    ORDER BY MaVaiTro
                    "
                ).ToList();


                // -------------------------------------------------
                // 3. Lấy vai trò hiện tại của tài khoản
                // -------------------------------------------------
                var maVaiTroDangChon = db.Database.SqlQuery<int>(
                    @"
                    SELECT TOP 1 MaVaiTro
                    FROM TaiKhoan_VaiTro
                    WHERE MaTaiKhoan = @p0
                   ORDER BY MaTaiKhoanVaiTro
                    ",
                    id
                ).FirstOrDefault();


                // -------------------------------------------------
                // 4. Nếu chưa có vai trò thì = 0
                // -------------------------------------------------
                if (maVaiTroDangChon == 0)
                {
                    maVaiTroDangChon = 0;
                }


                // -------------------------------------------------
                // 5. Lấy danh sách quyền
                // -------------------------------------------------
                var quyens = db.Database.SqlQuery<QuyenItemViewModel>(
                    @"
                    SELECT
                        q.MaQuyen,
                        q.MaQuyenCode,
                        q.TenQuyen,
                        q.MoTa,

                        CAST(
                            CASE
                                WHEN EXISTS
                                (
                                    SELECT 1
                                    FROM TaiKhoan_VaiTro tkv
                                    INNER JOIN VaiTro_Quyen vtq
                                        ON tkv.MaVaiTro = vtq.MaVaiTro
                                    WHERE tkv.MaTaiKhoan = @p0
                                      AND vtq.MaQuyen = q.MaQuyen
                                )
                                THEN 1
                                ELSE 0
                            END
                            AS bit
                        ) AS DaDuocCap

                    FROM Quyen q
                    WHERE q.TrangThai = 1
                    ORDER BY q.MaQuyen
                    ",
                    id
                ).ToList();


                // -------------------------------------------------
                // 6. Lấy các quyền hiện tại
                // -------------------------------------------------
                var maQuyenDangChon = quyens
                    .Where(x => x.DaDuocCap)
                    .Select(x => x.MaQuyen)
                    .ToList();


                // -------------------------------------------------
                // 7. Tạo ViewModel
                // -------------------------------------------------
                var model = new PhanQuyenViewModel
                {
                    TaiKhoan = taiKhoan,

                    VaiTros = vaiTros,

                    Quyens = quyens,

                    MaVaiTroDangChon = maVaiTroDangChon,

                    MaQuyenDangChon = maQuyenDangChon
                };


                return View(model);
            }
        }


        // =========================================================
        // POST: LƯU PHÂN QUYỀN
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LuuPhanQuyen(
            int MaTaiKhoan,
            int MaVaiTro,
            int[] MaQuyen)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Index", "Login");
            }

            using (QUANLYEntities1 db = new QUANLYEntities1())
            {
                // -------------------------------------------------
                // 1. Kiểm tra tài khoản
                // -------------------------------------------------
                var taiKhoan = db.TaiKhoans
                    .FirstOrDefault(x => x.MaTaiKhoan == MaTaiKhoan);

                if (taiKhoan == null)
                {
                    return HttpNotFound();
                }


                // -------------------------------------------------
                // 2. Kiểm tra vai trò
                // -------------------------------------------------
                var vaiTroTonTai = db.Database.SqlQuery<int>(
                    @"
                    SELECT COUNT(*)
                    FROM VaiTro
                    WHERE MaVaiTro = @p0
                      AND TrangThai = 1
                    ",
                    MaVaiTro
                ).FirstOrDefault();

                if (vaiTroTonTai == 0)
                {
                    TempData["Error"] = "Vai trò không tồn tại.";
                    return RedirectToAction(
                        "PhanQuyen",
                        new { id = MaTaiKhoan }
                    );
                }


                // -------------------------------------------------
                // 3. Xóa vai trò cũ
                // -------------------------------------------------
                db.Database.ExecuteSqlCommand(
                    @"
                    DELETE FROM TaiKhoan_VaiTro
                    WHERE MaTaiKhoan = @p0
                    ",
                    MaTaiKhoan
                );


                // -------------------------------------------------
                // 4. Thêm vai trò mới
                // -------------------------------------------------
                db.Database.ExecuteSqlCommand(
                    @"
                    INSERT INTO TaiKhoan_VaiTro
                    (
                        MaTaiKhoan,
                        MaVaiTro,
                        NgayTao
                    )
                    VALUES
                    (
                        @p0,
                        @p1,
                        GETDATE()
                    )
                    ",
                    MaTaiKhoan,
                    MaVaiTro
                );


                // -------------------------------------------------
                // 5. Kiểm tra quyền được gửi lên
                // -------------------------------------------------
                if (MaQuyen != null && MaQuyen.Length > 0)
                {
                    foreach (int maQuyen in MaQuyen)
                    {
                        // Kiểm tra quyền có tồn tại không
                        var quyenTonTai = db.Database.SqlQuery<int>(
                            @"
                            SELECT COUNT(*)
                            FROM Quyen
                            WHERE MaQuyen = @p0
                              AND TrangThai = 1
                            ",
                            maQuyen
                        ).FirstOrDefault();

                        if (quyenTonTai > 0)
                        {
                            // Kiểm tra quyền có thuộc vai trò không
                            var daCo = db.Database.SqlQuery<int>(
                                @"
                                SELECT COUNT(*)
                                FROM VaiTro_Quyen
                                WHERE MaVaiTro = @p0
                                  AND MaQuyen = @p1
                                ",
                                MaVaiTro,
                                maQuyen
                            ).FirstOrDefault();

                            // Nếu chưa có thì thêm
                            if (daCo == 0)
                            {
                                db.Database.ExecuteSqlCommand(
                                    @"
                                    INSERT INTO VaiTro_Quyen
                                    (
                                        MaVaiTro,
                                        MaQuyen
                                    )
                                    VALUES
                                    (
                                        @p0,
                                        @p1
                                    )
                                    ",
                                    MaVaiTro,
                                    maQuyen
                                );
                            }
                        }
                    }
                }


                TempData["Success"] =
                    "Phân quyền cho tài khoản đã được lưu thành công.";

                return RedirectToAction(
                    "PhanQuyen",
                    new { id = MaTaiKhoan }
                );
            }
        }


        // =========================================================
        // CLASS NHẬN KẾT QUẢ SQL
        // =========================================================
        public class RoleAccountViewModel
        {
            public int MaTaiKhoan { get; set; }

            public string TenVaiTro { get; set; }
        }
    }
}