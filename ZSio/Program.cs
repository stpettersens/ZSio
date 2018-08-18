using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZSio
{
    class Program
    {
        static void Main(string[] args)
        {
            ZSio.MakeZSFile("example.zs", "Hello, World!");
			Console.WriteLine("Wrote ZS file: 'example.zs'... Done.");
        }
    }
}
