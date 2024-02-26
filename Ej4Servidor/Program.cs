using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ej4Servidor
{
    internal class Program
    {
        static void Main(string[] args)
        {
            ShiftServer server = new ShiftServer();
            server.Init();
        }
    }
}
