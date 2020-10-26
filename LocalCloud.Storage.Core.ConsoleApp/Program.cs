using System;

namespace LocalCloud.Storage.Core.ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var storage = new Storage(@"C:\Users\nikita.dermenzhi\Desktop\New folder (2)");
            storage.DateRange = DateRanges.Day;


            Console.ReadKey();
        }
    }
}
