using System;

namespace jsreport.MVC
{
    public class EnableJsReportAttribute : Attribute
    {
        public EnableJsReportAttribute(string headerHeight, string headerPartialView, string footerHeight, string footerPartialView, string margin,
            string orientation, string width, string height, string format, string recipe, string contentDisposition, string engine, bool waitForJS,
            bool blockJavaScript, int printDelay, int resourceTimeout)
        {
            HeaderHeight = headerHeight;
            HeaderPartialView = headerPartialView;
            FooterHeight = footerHeight;
            FooterPartialView = footerPartialView;
            Margin = margin;
            Orientation = orientation;
            Width = width;
            Height = height;
            Format = format;
            Recipe = recipe;
            ContentDisposition = contentDisposition;
            Engine = engine;
            PrintDelay = printDelay;
            BlockJavaScript = blockJavaScript;
            WaitForJS = waitForJS;
            ResourceTimeout = resourceTimeout;
        }

        public EnableJsReportAttribute()
        {
        }

        public object RenderingRequest { get; set; }
        public string Engine { get; set; }
        public string HeaderHeight { get; set; }
        public string HeaderPartialView { get; set; }
        public string FooterHeight { get; set; }
        public string FooterPartialView { get; set; }
        public string Margin { get; set; }
        public string Orientation { get; set; }
        public string Format { get; set; }
        public string Width { get; set; }
        public string Height { get; set; }
        public string Recipe { get; set; }
        public bool WaitForJS { get; set; }
        public bool BlockJavaScript { get; set; }
        public int PrintDelay { get; set; }
        public int ResourceTimeout { get; set; }
        public string ContentDisposition { get; set; }

    }
}
