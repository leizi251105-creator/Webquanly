
using System.Collections.Generic;
using WebApplication1.Models;

namespace WebApplication1.Models.ViewModels
{
    public class CustomerHomeViewModel
    {
        public List<Thuoc> DanhSachThuoc { get; set; }

        public List<DanhMucThuoc> DanhSachDanhMuc { get; set; }
    }
}
