
namespace WebApplication1.Models.ViewModels
{
    public class GioHangItemViewModel
    {
        // Mã thuốc
        public int MaThuoc { get; set; }

        // Tên thuốc
        public string TenThuoc { get; set; }

        // Hình ảnh thuốc
        public string AnhThuoc { get; set; }

        // Giá bán
        public decimal DonGia { get; set; }

        // Đơn vị tính
        public string DonViTinh { get; set; }

        // Số lượng khách hàng chọn
        public int SoLuong { get; set; }

        // Số lượng hiện có trong kho
        public int SoLuongTon { get; set; }

        // Thành tiền
        public decimal ThanhTien
        {
            get
            {
                return DonGia * SoLuong;
            }
        }
    }
}
