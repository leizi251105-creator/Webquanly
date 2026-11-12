
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using WebApplication1.Models;
using WebApplication1.Models.ViewModels;

namespace WebApplication1.Controllers
{
    public class GioHangController : Controller
    {
        // Lấy giỏ hàng từ Session
        private List<GioHangItemViewModel> LayGioHang()
        {
            var gioHang = Session["GioHang"]
                as List<GioHangItemViewModel>;

            if (gioHang == null)
            {
                gioHang = new List<GioHangItemViewModel>();
                Session["GioHang"] = gioHang;
            }

            return gioHang;
        }

        // Hiển thị giỏ hàng
        public ActionResult Index()
        {
            var gioHang = LayGioHang();

            ViewBag.TongSoLuong = gioHang.Sum(x => x.SoLuong);
            ViewBag.TongTien = gioHang.Sum(x => x.ThanhTien);

            return View(gioHang);
        }

        // Thêm sản phẩm vào giỏ hàng
        [HttpPost]
        public ActionResult ThemVaoGio(int id, int soLuong = 1)
        {
            if (soLuong < 1)
                soLuong = 1;

            using (QUANLYEntities1 db = new QUANLYEntities1())
            {
                var thuoc = db.Thuocs.FirstOrDefault(x =>
                    x.MaThuoc == id && x.TrangThai == true);

                if (thuoc == null)
                {
                    TempData["ThongBaoLoi"] =
                        "Không tìm thấy sản phẩm.";

                    return RedirectToAction("Index", "SanPham");
                }

                if (thuoc.SoLuongTon <= 0)
                {
                    TempData["ThongBaoLoi"] =
                        "Sản phẩm hiện đã hết hàng.";

                    return RedirectToAction("Details", "SanPham",
                        new { id = id });
                }

                var gioHang = LayGioHang();

                var sanPham = gioHang.FirstOrDefault(x =>
                    x.MaThuoc == id);

                int soLuongHienTai = sanPham == null
                    ? 0
                    : sanPham.SoLuong;

                if (soLuongHienTai + soLuong > thuoc.SoLuongTon)
                {
                    TempData["ThongBaoLoi"] =
                        "Số lượng yêu cầu vượt quá số lượng tồn kho.";

                    return RedirectToAction("Details", "SanPham",
                        new { id = id });
                }

                if (sanPham == null)
                {
                    sanPham = new GioHangItemViewModel
                    {
                        MaThuoc = thuoc.MaThuoc,
                        TenThuoc = thuoc.TenThuoc,
                        AnhThuoc = thuoc.AnhThuoc,
                        DonGia = thuoc.GiaBan,
                        DonViTinh = thuoc.DonViTinh,
                        SoLuong = soLuong,
                        SoLuongTon = thuoc.SoLuongTon
                    };

                    gioHang.Add(sanPham);
                }
                else
                {
                    sanPham.SoLuong += soLuong;
                    sanPham.SoLuongTon = thuoc.SoLuongTon;
                }

                Session["GioHang"] = gioHang;

                TempData["ThongBao"] =
                    "Đã thêm sản phẩm vào giỏ hàng.";
            }

            return RedirectToAction("Index");
        }

        // Xóa sản phẩm khỏi giỏ hàng
        public ActionResult Xoa(int id)
        {
            var gioHang = LayGioHang();

            var sanPham = gioHang.FirstOrDefault(x =>
                x.MaThuoc == id);

            if (sanPham != null)
            {
                gioHang.Remove(sanPham);
                Session["GioHang"] = gioHang;
                TempData["ThongBao"] =
                    "Đã xóa sản phẩm khỏi giỏ hàng.";
            }

            return RedirectToAction("Index");
        }

        // Cập nhật số lượng sản phẩm
        [HttpPost]
        public ActionResult CapNhat(int id, int soLuong)
        {
            var gioHang = LayGioHang();

            var sanPham = gioHang.FirstOrDefault(x =>
                x.MaThuoc == id);

            if (sanPham == null)
                return RedirectToAction("Index");

            if (soLuong <= 0)
            {
                gioHang.Remove(sanPham);
                TempData["ThongBao"] =
                    "Đã xóa sản phẩm khỏi giỏ hàng.";
            }
            else
            {
                using (QUANLYEntities1 db = new QUANLYEntities1())
                {
                    var thuoc = db.Thuocs.FirstOrDefault(x =>
                        x.MaThuoc == id && x.TrangThai == true);

                    if (thuoc == null || soLuong > thuoc.SoLuongTon)
                    {
                        TempData["ThongBaoLoi"] =
                            "Số lượng yêu cầu không hợp lệ hoặc vượt tồn kho.";

                        return RedirectToAction("Index");
                    }

                    sanPham.SoLuong = soLuong;
                    sanPham.SoLuongTon = thuoc.SoLuongTon;
                    sanPham.DonGia = thuoc.GiaBan;
                }
            }

            Session["GioHang"] = gioHang;

            return RedirectToAction("Index");
        }
    }
}
