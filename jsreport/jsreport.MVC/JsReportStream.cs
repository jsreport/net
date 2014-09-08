using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Linq;
using jsreport.Client;
using System.Collections.Generic;

namespace jsreport.MVC
{
    public class JsReportStream : Stream
    {
        private readonly Stream _stream;        
        private readonly ActionExecutedContext _context;
        private readonly IList<byte[]> _htmlInputList = new List<byte[]>();
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
        
        public override void Flush() 
        {
            
        }

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
            var buffer = _htmlInputList.SelectMany(l => l).ToArray<byte>();
            string s = Encoding.UTF8.GetString(buffer);             

            var output = _renderReport(_context, _attr, s).Result;
            
            output.Content.CopyTo(_stream);
            _stream.Flush(); 
            _stream.Close();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            var data = new byte[count];
            Buffer.BlockCopy(buffer, offset, data, 0, count);
            _htmlInputList.Add(data);
        }
    }
}