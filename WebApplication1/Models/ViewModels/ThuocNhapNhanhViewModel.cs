using System.ComponentModel.DataAnnotations;

namespace WebApplication1.ViewModels
{
    public class ThuocNhapNhanhViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập tên thuốc.")]
        [StringLength(200, ErrorMessage = "Tên thuốc không được vượt quá 200 ký tự.")]
        [Display(Name = "Tên thuốc")]
        public string TenThuoc { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn danh mục thuốc.")]
        [Display(Name = "Danh mục")]
        public int MaDanhMuc { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập đơn vị tính.")]
        [StringLength(50, ErrorMessage = "Đơn vị tính không được vượt quá 50 ký tự.")]
        [Display(Name = "Đơn vị tính")]
        public string DonViTinh { get; set; }

        [StringLength(255)]
        [Display(Name = "Hoạt chất")]
        public string HoatChat { get; set; }

        [StringLength(100)]
        [Display(Name = "Hàm lượng")]
        public string HamLuong { get; set; }

        [Range(typeof(decimal), "0", "9999999999999999",
            ErrorMessage = "Giá bán không được âm.")]
        [Display(Name = "Giá bán")]
        public decimal GiaBan { get; set; }

        [Range(0, int.MaxValue,
            ErrorMessage = "Mức tồn tối thiểu không được âm.")]
        [Display(Name = "Mức tồn tối thiểu")]
        public int MucTonToiThieu { get; set; }
    }
}