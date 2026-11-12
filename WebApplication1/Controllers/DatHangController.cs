
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using WebApplication1.Models;
using WebApplication1.Models.ViewModels;

namespace WebApplication1.Controllers
{
    public class DatHangController : Controller
    {
        // Hiển thị trang nhập thông tin đặt hàng
        [HttpGet]
        public ActionResult Index()
        {
            var gioHang = Session["GioHang"]
                as List<GioHangItemViewModel>;

            // Kiểm tra giỏ hàng
            if (gioHang == null || !gioHang.Any())
            {
                TempData["ThongBaoLoi"] =
                    "Giỏ hàng đang trống. Vui lòng chọn sản phẩm trước khi đặt hàng.";

                return RedirectToAction("Index", "GioHang");
            }

            // Tạo dữ liệu hiển thị trang đặt hàng
            var model = new DatHangViewModel
            {
                DanhSachSanPham = gioHang,
                TongSoLuong = gioHang.Sum(x => x.SoLuong),
                TongTien = gioHang.Sum(x => x.ThanhTien)
            };

            return View(model);
        }


        // Xử lý xác nhận đặt hàng
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult XacNhan(DatHangViewModel model)
        {
            var gioHang = Session["GioHang"]
                as List<GioHangItemViewModel>;

            // Kiểm tra giỏ hàng
            if (gioHang == null || !gioHang.Any())
            {
                TempData["ThongBaoLoi"] = "Giỏ hàng đang trống.";
                return RedirectToAction("Index", "GioHang");
            }

            // Nếu thông tin nhập chưa hợp lệ
            if (!ModelState.IsValid)
            {
                model.DanhSachSanPham = gioHang;
                model.TongSoLuong = gioHang.Sum(x => x.SoLuong);
                model.TongTien = gioHang.Sum(x => x.ThanhTien);

                return View("Index", model);
            }

            using (QUANLYEntities1 db = new QUANLYEntities1())
            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    // 1. Kiểm tra lại sản phẩm và tồn kho thực tế
                    var danhSachThuoc = new List<Thuoc>();

                    foreach (var item in gioHang)
                    {
                        if (item.SoLuong <= 0)
                        {
                            transaction.Rollback();
                            TempData["ThongBaoLoi"] = "Số lượng sản phẩm không hợp lệ.";
                            return RedirectToAction("Index", "GioHang");
                        }

                        var thuoc = db.Thuocs.FirstOrDefault(x =>
                            x.MaThuoc == item.MaThuoc &&
                            x.TrangThai == true);

                        if (thuoc == null || thuoc.SoLuongTon < item.SoLuong)
                        {
                            transaction.Rollback();
                            TempData["ThongBaoLoi"] =
                                "Sản phẩm " + item.TenThuoc +
                                " không đủ số lượng tồn kho. Vui lòng kiểm tra lại giỏ hàng.";

                            return RedirectToAction("Index", "GioHang");
                        }

                        danhSachThuoc.Add(thuoc);
                    }

                    // 2. Tìm khách hàng theo số điện thoại
                    var khachHang = db.KhachHangs.FirstOrDefault(x =>
                        x.SoDienThoai == model.SoDienThoai);

                    if (khachHang == null)
                    {
                        khachHang = new KhachHang
                        {
                            HoTen = model.HoTen,
                            SoDienThoai = model.SoDienThoai,
                            Email = model.Email,
                            DiaChi = model.DiaChi,
                            NgayTao = DateTime.Now,
                            TrangThai = true
                        };

                        db.KhachHangs.Add(khachHang);
                    }
                    else
                    {
                        khachHang.HoTen = model.HoTen;
                        khachHang.Email = model.Email;
                        khachHang.DiaChi = model.DiaChi;
                    }

                    db.SaveChanges();

                    // 3. Tính tổng tiền theo giá bán hiện tại trong database
                    decimal tongTien = 0;

                    for (int i = 0; i < gioHang.Count; i++)
                    {
                        tongTien += danhSachThuoc[i].GiaBan
                                    * gioHang[i].SoLuong;
                    }

                    // 4. Tạo đơn hàng
                    var donHang = new DonHang
                    {
                        MaKhachHang = khachHang.MaKhachHang,
                        MaTaiKhoan = null,
                        NgayDat = DateTime.Now,
                        TongTien = tongTien,
                        TrangThai = "Chờ xác nhận",
                        GhiChu = model.GhiChu
                    };

                    db.DonHangs.Add(donHang);
                    db.SaveChanges();

                    // 5. Lưu chi tiết đơn hàng và trừ tồn kho
                    for (int i = 0; i < gioHang.Count; i++)
                    {
                        var item = gioHang[i];
                        var thuoc = danhSachThuoc[i];

                        var chiTiet = new ChiTietDonHang
                        {
                            MaDonHang = donHang.MaDonHang,
                            MaThuoc = thuoc.MaThuoc,
                            SoLuong = item.SoLuong,
                            DonGia = thuoc.GiaBan,
                            ThanhTien = thuoc.GiaBan * item.SoLuong
                        };

                        db.ChiTietDonHangs.Add(chiTiet);

                        thuoc.SoLuongTon -= item.SoLuong;
                    }

                    db.SaveChanges();

                    // 6. Hoàn tất giao dịch
                    transaction.Commit();

                    // Xóa giỏ hàng sau khi đặt hàng thành công
                    Session["GioHang"] = null;

                    TempData["ThongBaoDatHang"] =
                        "Đặt hàng thành công! Mã đơn hàng: DH" +
                        donHang.MaDonHang.ToString("D5");

                    return RedirectToAction(
                        "ThanhCong",
                        new { id = donHang.MaDonHang }
                    );
                }
                catch (Exception)
                {
                    transaction.Rollback();

                    TempData["ThongBaoLoi"] =
                        "Đã xảy ra lỗi khi đặt hàng. Vui lòng thử lại.";

                    return RedirectToAction("Index", "GioHang");
                }
            }
        }



        // Trang thông báo đặt hàng thành công
        [HttpGet]
        public ActionResult ThanhCong(int? id)
        {
            if (id == null)
                return RedirectToAction("Index", "Customer");

            using (QUANLYEntities1 db = new QUANLYEntities1())
            {
                var donHang = db.DonHangs
                    .Include("KhachHang")
                    .Include("ChiTietDonHangs.Thuoc")
                    .FirstOrDefault(x => x.MaDonHang == id.Value);

                if (donHang == null)
                    return HttpNotFound("Không tìm thấy đơn hàng.");

                return View(donHang);
            }
        }

    }
}
