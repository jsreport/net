using System;
using System.IO;

namespace jsreport.Local
{
    public class Program
    {
        public static void Main()
        {
            Console.WriteLine("Starging");

            var rs = new LocalReportingService();
            rs.Initialize();

            var result = rs.Render(new
                {
                    template = new
                        {
                            content = "foo",
                            engine = "none",
                            recipe = "html"
                        }
                });

            
            /*using (var writer = File.OpenWrite("out.pdf"))
            {
                result.Content.CopyTo(writer);
            }*/

            using (StreamReader reader = new StreamReader(result.Content))
            {
                Console.WriteLine(reader.ReadToEnd());
            }


            Console.WriteLine("done");
            Console.ReadKey();
        }
    }
}
