using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BoomboxSynthesizer;

public class Program
{
    public static BoomboxServer Server;
    public static void Main()
    {
        Server = new BoomboxServer();
        Server.Initialize();
    }
}
