using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using jsreport.Client;

namespace Library
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var service = new ReportingService("https://playground.jsreport.net");

            using (var fileStream = File.Create("report.pdf"))
            {
                var report = service.RenderAsync("eyaNpy1ho", 11, new {
                        books = new[]
                            {
                                new Book() {name = "A Tale of Two Cities", author = "Charles Dickens", sales = 351},
                                new Book() {name = "The Lord of the Rings", author = "J. R. R. Tolkien", sales = 156},
                                new Book() {name = "The Da Vinci Code", author = "Dan Brown", sales = 280},
                                new Book() {name = "The Hobbit", author = "J. R. R. Tolkien", sales = 170}
                            }}).Result;

            report.Content.CopyTo(fileStream);
        }
    }

    internal class Book
    {
        public string name { get; set; }
        public string author { get; set; }
        public int sales { get; set; }
    }
}

}