namespace jsreport.Client.Entities
{
    /// <summary>
    /// Complex object used to configure phantom-pdf rendering. It's embedded in Template
    /// </summary>
    public class Phantom
    {
        /// <summary>
        /// Paper margin like 2cm, see phantom doc for details http://phantomjs.org/
        /// </summary>
        public string margin { get; set; }

        /// <summary>
        /// Html used for page header, you can use special tags to insert page numbers: {#pageNum}/{#numPages}
        /// </summary>
        public string header { get; set; }

        /// <summary>
        /// Height of header like 2cm
        /// </summary>
        public string headerHeight { get; set; }

        /// <summary>
        /// Html used for page footer, you can use special tags to insert page numbers: {#pageNum}/{#numPages}
        /// </summary> 
        public string footer { get; set; }

        /// <summary>
        /// Height of footer like 2cm
        /// </summary>
        public string footerHeight { get; set; }

        /// <summary>
        /// Paper orientation, possible values "landscape" and "portrait"
        /// </summary>
        public string orientation { get; set; }

        /// <summary>
        /// Paper format, possible values "A5", "A4", "A3", "Letter", "Tabloid", "Legal"
        /// width or height specification takes precedence
        /// </summary>
        public string format { get; set; }

        /// <summary>
        /// Paper width like 2cm
        /// </summary>
        public string width { get; set; }

        /// <summary>
        /// Paper height like 2cm
        /// </summary>
        public string height { get; set; }
    }
}