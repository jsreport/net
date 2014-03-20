using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using jsreport.Client;

namespace jsreport.MVC
{
    public class JsReportStream : Stream
    {
        private readonly Stream _stream;
        private readonly ActionExecutedContext _context;
        private readonly EnableJsReportAttribute _attr;
        private readonly Func<ActionExecutedContext, EnableJsReportAttribute, string, Task<Report>> _renderReport;

        public JsReportStream(ActionExecutedContext context, EnableJsReportAttribute attr, Func<ActionExecutedContext, EnableJsReportAttribute, string, Task<Report>> renderReport)
        {
            _stream = context.HttpContext.Response.Filter;
            _context = context;
            _attr = attr;
            _renderReport = renderReport;
        }


        public override bool CanRead { get { return true; } }
        public override bool CanSeek { get { return true; } }
        public override bool CanWrite { get { return true; } }
        public override void Flush() { _stream.Flush(); }
        public override long Length { get { return 0; } }
        public override long Position { get; set; }
        public override int Read(byte[] buffer, int offset, int count)
        {
            return _stream.Read(buffer, offset, count);
        }
        public override long Seek(long offset, SeekOrigin origin)
        {
            return _stream.Seek(offset, origin);
        }
        public override void SetLength(long value)
        {
            _stream.SetLength(value);
        }
        public override void Close()
        {
            _stream.Close();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            byte[] data = new byte[count];
            Buffer.BlockCopy(buffer, offset, data, 0, count);
            string s = Encoding.Default.GetString(buffer);

            var output = _renderReport(_context, _attr, s).Result;

            output.Content.CopyTo(_stream);
        }
    }
}