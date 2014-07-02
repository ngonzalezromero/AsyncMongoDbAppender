using System;
using log4net.Config;
using log4net;

namespace test
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            XmlConfigurator.Configure();

            //Stopwatch watch = new Stopwatch();
            //watch.Start();
            for (int i = 0; i < 50; i++)
            {
                //  int i = 0;
                LogManager.GetLogger(typeof(MainClass)).Error("Test Ado Net Logger at: " + DateTime.Now + "cont " + i);
            }
            //watch.Stop();

            //Console.WriteLine(watch.ElapsedMilliseconds);
          
            //    int y = 1;
            /*
            while (y < 100000)
            {
                Console.WriteLine(y);
                y++;
            }
                */      
            Console.ReadKey();
        }
    }
}
