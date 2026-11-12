
using System.Linq;
using System.Web.Mvc;
using WebApplication1.Models;
using WebApplication1.Models.ViewModels;

namespace WebApplication1.Controllers
{
    public class CustomerController : Controller
    {
        public ActionResult Index()
        {
            using (QUANLYEntities1 db = new QUANLYEntities1())
            {
                var model = new CustomerHomeViewModel
                {
                    DanhSachDanhMuc = db.DanhMucThuocs
                        .Where(x => x.TrangThai == true)
                        .OrderBy(x => x.MaDanhMuc)
                        .ToList(),

                    DanhSachThuoc = db.Thuocs
                        .Where(x => x.TrangThai == true)
                        .OrderByDescending(x => x.MaThuoc)
                        .ToList()
                };

                return View(model);
            }
        }
    }
}
