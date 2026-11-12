using System.Collections.Generic;
using WebApplication1.Models;

namespace WebApplication1.Models.ViewModels
{
    public class PhanQuyenViewModel
    {
        public TaiKhoan TaiKhoan { get; set; }

        public List<VaiTroItemViewModel> VaiTros { get; set; }

        public List<QuyenItemViewModel> Quyens { get; set; }

        public int MaVaiTroDangChon { get; set; }

        public List<int> MaQuyenDangChon { get; set; }
    }

    public class VaiTroItemViewModel
    {
        public int MaVaiTro { get; set; }

        public string TenVaiTro { get; set; }

        public string MoTa { get; set; }
    }

    public class QuyenItemViewModel
    {
        public int MaQuyen { get; set; }

        public string MaQuyenCode { get; set; }

        public string TenQuyen { get; set; }

        public string MoTa { get; set; }

        public bool DaDuocCap { get; set; }
    }
}