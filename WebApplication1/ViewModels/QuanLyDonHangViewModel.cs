
using System;
using System.Collections.Generic;
using WebApplication1.Models;

namespace WebApplication1.Models.ViewModels
{
    public class QuanLyDonHangViewModel
    {
        public List<DonHang> DanhSachDonHang { get; set; }

        public string TuKhoa { get; set; }

        public string TrangThai { get; set; }

        public DateTime? TuNgay { get; set; }

        public DateTime? DenNgay { get; set; }

        public int TongDonHang { get; set; }

        public int DonChoXacNhan { get; set; }

        public int DonDaXacNhan { get; set; }

        public int DonDangGiao { get; set; }

        public int DonDaGiao { get; set; }

        public decimal TongDoanhThu { get; set; }
    }
}
