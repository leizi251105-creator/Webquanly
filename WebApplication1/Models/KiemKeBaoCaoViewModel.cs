
using System;
using System.Collections.Generic;

namespace WebApplication1.Models
{
    public class KiemKeBaoCaoViewModel
    {
        // Bộ lọc báo cáo
        public DateTime? TuNgay { get; set; }
        public DateTime? DenNgay { get; set; }
        public string TrangThai { get; set; }

        // Thống kê số lượng phiếu
        public int TongSoPhieu { get; set; }
        public int SoPhieuDaXacNhan { get; set; }
        public int SoPhieuChuaXacNhan { get; set; }
        public int SoPhieuDaHuy { get; set; }

        // Thống kê chênh lệch tồn kho
        public int SoLoThua { get; set; }
        public int SoLoThieu { get; set; }
        public int TongSoLuongThua { get; set; }
        public int TongSoLuongThieu { get; set; }

        // Danh sách chi tiết chênh lệch
        public List<KiemKeBaoCaoChiTietViewModel> DanhSachChiTiet
        {
            get;
            set;
        } = new List<KiemKeBaoCaoChiTietViewModel>();
    }

    public class KiemKeBaoCaoChiTietViewModel
    {
        public int MaPhieuKiemKe { get; set; }
        public DateTime NgayKiemKe { get; set; }

        public string TenThuoc { get; set; }
        public string SoLo { get; set; }

        public int SoLuongHeThong { get; set; }
        public int SoLuongThucTe { get; set; }
        public int ChenhLech { get; set; }

        public string TenNguoiLap { get; set; }
    }
}
