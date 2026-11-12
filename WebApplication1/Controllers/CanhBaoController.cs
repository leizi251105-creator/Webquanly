
using System;
using System.Linq;
using System.Web.Mvc;
using System.Data.Entity;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    public class CanhBaoController : Controller
    {
        private QUANLYEntities1 db = new QUANLYEntities1();

        // Kiểm tra quyền truy cập
        private bool CoQuyenCanhBao()
        {
            if (Session["userID"] == null)
                return false;

            var roles = Session["roles"] as
                System.Collections.Generic.List<string>;

            return roles != null &&
                (roles.Contains("Admin") ||
                 roles.Contains("NhanVienKho"));
        }

        // Trang tổng hợp cảnh báo
        public ActionResult Index(int soNgay = 30)
        {
            if (Session["userID"] == null)
                return RedirectToAction("Index", "Login");

            if (!CoQuyenCanhBao())
                return new HttpStatusCodeResult(403);

            if (soNgay != 30 && soNgay != 60 && soNgay != 90)
                soNgay = 30;

            DateTime homNay = DateTime.Today;
            DateTime mocCanhBao = homNay.AddDays(soNgay);

            // 1. Thuốc tồn kho thấp
            var thuocTonThap = db.Thuocs
                .Where(t => t.TrangThai &&
                    t.SoLuongTon <= t.MucTonToiThieu)
                .OrderBy(t => t.SoLuongTon)
                .ToList();

            // 2. Lô thuốc sắp hết hạn
            var loSapHetHan = db.LoThuocs
                .Include(l => l.Thuoc)
                .Where(l =>
                    l.TrangThai == true &&
                    l.SoLuong > 0 &&
                    l.HanSuDung >= homNay &&
                    l.HanSuDung <= mocCanhBao)
                .OrderBy(l => l.HanSuDung)
                .ToList();

            // 3. Lô thuốc đã hết hạn
            var loDaHetHan = db.LoThuocs
                .Include(l => l.Thuoc)
                .Where(l =>
                    l.HanSuDung < homNay &&
                    l.SoLuong > 0)
                .OrderBy(l => l.HanSuDung)
                .ToList();

            ViewBag.SoNgay = soNgay;
            ViewBag.SoThuocTonThap = thuocTonThap.Count;
            ViewBag.SoLoSapHetHan = loSapHetHan.Count;
            ViewBag.SoLoDaHetHan = loDaHetHan.Count;

            ViewBag.ThuocTonThap = thuocTonThap;
            ViewBag.LoSapHetHan = loSapHetHan;
            ViewBag.LoDaHetHan = loDaHetHan;

            return View();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                db.Dispose();

            base.Dispose(disposing);
        }
    }
}
