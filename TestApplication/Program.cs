using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestApplication
{
    class Program
    {
        static void Main(string[] args)
        {
            var calculator = new EvapotranspirationCalculator.Calculator();

            var result = calculator.Calculate("7a907ac8bfd9058c", DateTime.Parse("5 Sep 2017"), "IWESTERN594", 1);

            var EvapotranspirationMM = result.EvapotranspirationMM;
        }
    }
}
