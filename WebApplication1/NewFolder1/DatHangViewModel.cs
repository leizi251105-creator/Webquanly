
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models.ViewModels
{
    public class DatHangViewModel
    {
        // Thông tin khách hàng
        [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
        [StringLength(150)]
        public string HoTen { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
        [StringLength(15)]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
        public string SoDienThoai { get; set; }

        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        [StringLength(100)]
        public string Email { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ nhận hàng.")]
        [StringLength(255)]
        public string DiaChi { get; set; }

        [StringLength(500)]
        public string GhiChu { get; set; }

        // Thông tin giỏ hàng
        public List<GioHangItemViewModel> DanhSachSanPham { get; set; }

        public int TongSoLuong { get; set; }

        public decimal TongTien { get; set; }
    }
}
