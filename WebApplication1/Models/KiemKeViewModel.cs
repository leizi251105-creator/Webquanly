
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models
{
    public class KiemKeViewModel
    {
        [StringLength(500)]
        public string GhiChu { get; set; }

        public List<KiemKeChiTietViewModel> ChiTiet { get; set; }
            = new List<KiemKeChiTietViewModel>();
    }

    public class KiemKeChiTietViewModel
    {
        public int MaLo { get; set; }

        public string TenThuoc { get; set; }

        public string SoLo { get; set; }

        public int SoLuongHeThong { get; set; }

        [Range(0, int.MaxValue,
            ErrorMessage = "Số lượng thực tế phải lớn hơn hoặc bằng 0.")]
        public int SoLuongThucTe { get; set; }

        public string HanSuDung { get; set; }
    }
}
