using System;

namespace TestApplication
{
    class Program
    {
        static void Main(string[] args)
        {
            var calculator = new EvapotranspirationCalculator.Calculator();

            var weatherUndergroundKey = "";         // Weather Underground api key
            var date = DateTime.Today;              // Requested Date
            var pws = "";                           // Personal weather station code
            var canopyReflectionCoefficient = 1;    // Canopy Reflection Coefficient

            var result = calculator.Calculate(weatherUndergroundKey, date, pws, canopyReflectionCoefficient);

            var EvapotranspirationMM = result.EvapotranspirationMM;
        }
    }
}
