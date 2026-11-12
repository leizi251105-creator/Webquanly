
using System.ComponentModel.DataAnnotations;
using WebApplication1.Models;

namespace WebApplication1.Models.ViewModels
{
    public class TraCuuDonHangViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập mã đơn hàng.")]
        public string MaDonHang { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
        [StringLength(15)]
        public string SoDienThoai { get; set; }

        public DonHang KetQua { get; set; }
    }
}
