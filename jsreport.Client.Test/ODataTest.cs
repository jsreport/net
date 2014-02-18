using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Simple.OData.Client;

namespace jsreport.Client.Test
{
    [TestFixture]
    public class ODataTest
    {
        [Test]
        public void Foo()
        {
            //var client = new ODataClient("http://localhost:3000/odata");

            //Parallel.ForEach(Enumerable.Range(0, 1000), (i) =>
            //    {           
            //        Console.WriteLine(i);
            //        var template = client
            //                .For("templates")
            //                .Set(new { shortid = "TPqHz27dAi", name = i.ToString(), html = i.ToString(), recipe = "html", engine = "jsrender"  })
            //                .InsertEntry();

            //    });

            var client = new ODataClient("http://localhost:1337/odata");

            Parallel.ForEach(Enumerable.Range(0, 1000000), (i) =>
                {           
                    Console.WriteLine(i);
                    var product = client
                            .For("Products")
                            .Set(new { Name = i.ToString()})
                            .InsertEntry();

                });

            
          //  var packages = client.FindEntries("templates").ToList();
        }
    }
}
