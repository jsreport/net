using System;

namespace jsreport.MVC
{
    public class EnableJsReportAttribute : Attribute
    {
        public EnableJsReportAttribute(string headerHeight, string headerPartialView, string footerHeight, string footerPartialView, string margin)
        {
            HeaderHeight = headerHeight;
            HeaderPartialView = headerPartialView;
            FooterHeight = footerHeight;
            FooterPartialView = footerPartialView;
            Margin = margin;
        }

        public EnableJsReportAttribute()
        {
        }

        public string HeaderHeight { get; set; }
        public string HeaderPartialView { get; set; }
        public string FooterHeight { get; set; }
        public string FooterPartialView { get; set; }
        public string Margin { get; set; }
    }
}