using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace WebApplication1.Models
{
    public class TaiKhoanLogin
    {
       
            public int MaTaiKhoan { get; set; }

            [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập")]
            public string TenDangNhap { get; set; }

            [DataType(DataType.Password)]
            [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
            public string MatKhau { get; set; }

            public string LoginErrorMessage { get; set; }
        
    }
}