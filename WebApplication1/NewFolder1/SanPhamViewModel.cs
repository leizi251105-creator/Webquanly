
using System.Collections.Generic;
using WebApplication1.Models;

namespace WebApplication1.Models.ViewModels
{
    public class SanPhamViewModel
    {
        // Danh sách thuốc hiển thị
        public List<Thuoc> DanhSachThuoc { get; set; }

        // Danh sách danh mục thuốc
        public List<DanhMucThuoc> DanhSachDanhMuc { get; set; }

        // Từ khóa tìm kiếm
        public string TuKhoa { get; set; }

        // Mã danh mục được chọn
        public int? MaDanhMuc { get; set; }
    }
}
