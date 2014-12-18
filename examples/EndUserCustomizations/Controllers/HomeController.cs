using System.Web;
using System.Web.Mvc;
using System.IO;
using Newtonsoft.Json;

namespace EndUserCustomizations.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            ViewBag.DefaultInvoice = HttpUtility.JavaScriptStringEncode(JsonConvert.SerializeObject(new
                {
                    content = System.IO.File.ReadAllText(Server.MapPath("~/Examples/Invoice.html")),
                    helpers = System.IO.File.ReadAllText(Server.MapPath("~/Examples/Invoice.js")),
                    recipe = "phantom-pdf",
                    engine = "jsrender",
                    data = new { dataJson = System.IO.File.ReadAllText(Server.MapPath("~/Examples/Invoice.json")) }
                }));

            ViewBag.ClientTable = HttpUtility.JavaScriptStringEncode(JsonConvert.SerializeObject(new
            {
                content = System.IO.File.ReadAllText(Server.MapPath("~/Examples/ClientTable.html")),
                recipe = "client-html",
                engine = "jsrender"
            }));

            ViewBag.ClientChart = HttpUtility.JavaScriptStringEncode(JsonConvert.SerializeObject(new
            {
                content = System.IO.File.ReadAllText(Server.MapPath("~/Examples/ClientChart.html")),
                recipe = "client-html",
                engine = "jsrender"
            }));

            return View();
        }
    }
}