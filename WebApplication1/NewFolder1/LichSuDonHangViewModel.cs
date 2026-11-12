using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using WebApplication1.Models;

namespace WebApplication1.Models.ViewModels
{
    public class LichSuDonHangViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
        [StringLength(15)]
        public string SoDienThoai { get; set; }

        public List<DonHang> DanhSachDonHang { get; set; }
    }
}