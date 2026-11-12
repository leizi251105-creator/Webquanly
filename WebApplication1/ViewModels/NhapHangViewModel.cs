
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WebApplication1.ViewModels
{
    public class NhapHangViewModel
    {
        [Required(ErrorMessage = "Vui lòng chọn nhà cung cấp.")]
        [Display(Name = "Nhà cung cấp")]
        public int MaNhaCungCap { get; set; }

        [Display(Name = "Ngày nhập")]
        public DateTime NgayNhap { get; set; } = DateTime.Now;

        [Display(Name = "Ghi chú")]
        public string GhiChu { get; set; }

        public List<ChiTietNhapHangViewModel> ChiTiet { get; set; }
            = new List<ChiTietNhapHangViewModel>();
    }

    public class ChiTietNhapHangViewModel
    {
        [Required(ErrorMessage = "Vui lòng chọn thuốc.")]
        [Display(Name = "Thuốc")]
        public int MaThuoc { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số lượng.")]
        [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0.")]
        [Display(Name = "Số lượng")]
        public int SoLuong { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập đơn giá.")]
        [Range(typeof(decimal), "0.01", "999999999",
            ErrorMessage = "Đơn giá phải lớn hơn 0.")]
        [Display(Name = "Đơn giá nhập")]
        public decimal DonGia { get; set; }

        [Display(Name = "Số lô")]
        public string SoLo { get; set; }

        [Display(Name = "Ngày sản xuất")]
        public DateTime? NgaySanXuat { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập hạn sử dụng.")]
        [Display(Name = "Hạn sử dụng")]
        public DateTime HanSuDung { get; set; }
    }
}
