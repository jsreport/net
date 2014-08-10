using System.Xml.Serialization;

namespace jsreport.Embedded
{
    public class ReportDefinition
    {
        public string Timeout { get; set; }
        public string Schema { get; set; }
        public string Engine { get; set; }

        public string Recipe { get; set; }

        public PhantomDefinition Phantom { get; set; }
    }

    public class PhantomDefinition
    {
        public string Margin { get; set; }
        public string Header { get; set; }
        public string HeaderHeight { get; set; }
        public string Footer { get; set; }
        public string FooterHeight { get; set; }
        public string Orientation { get; set; }
        public string Format { get; set; }
        public string Width { get; set; }
        public string Height { get; set; }

        [XmlIgnore]
        public bool IsDirty {
            get
            {
                return Margin != null || Header != null || HeaderHeight != null || Footer != null ||
                       FooterHeight != null || Orientation != null ||
                       Format != null || Width != null || Height != null;
            }
        }
    }
}