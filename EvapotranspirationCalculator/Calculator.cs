using EvapotranspirationCalculator.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Web.Script.Serialization;

namespace EvapotranspirationCalculator
{
    public class Calculator
    {
        // calculation constants
        const double vaporRate = 237.3;
        const double enthalpy = 17.27;
        const double kelvin = 273.15;
        const double solarConstant = 0.0820;


        //public Evapotranspiration Calculate(string wundergroundKey, DateTime inputDate, string pws, double canopyReflectionCoefficient)
        public Evapotranspiration Calculate(string wundergroundKey, DateTime inputDate, string pws, double canopyReflectionCoefficient)
        {
            var url = "http://api.wunderground.com/api/" + wundergroundKey + "/conditions/history_" + inputDate.ToString("yyyyMMdd") + "/q/pws:" + pws + ".json";

            var wc = new WebClient();
            string json = wc.DownloadString(url);

            var serializer = new JavaScriptSerializer();

            dynamic obj = serializer.DeserializeObject(json);

            var maxTempF = ParseDouble(obj["history"]["dailysummary"][0]["maxtempi"]);
            var minTempF = ParseDouble(obj["history"]["dailysummary"][0]["mintempi"]);

            var solarRadiationReadings = new List<double>();
            var observationsArray = (object[])obj["history"]["observations"];

            for (var i = observationsArray.Length - 1; i >= 0; i--)
            {
                var observations = (Dictionary<string, object>)observationsArray[i];
                var solarRadiationReading = ParseDouble(observations["solarradiation"].ToString());
                solarRadiationReadings.Add(solarRadiationReading);
            }

            var meanSolarRadiationW = solarRadiationReadings.Average();
            var avgWindSpeedMPH = ParseDouble(obj["history"]["dailysummary"][0]["meanwindspdi"]);
            var elevationFt = ParseDouble(obj["current_observation"]["display_location"]["elevation"]) * 3.28084;
            var maxHumidity = ParseDouble(obj["history"]["dailysummary"][0]["maxhumidity"]);
            var minHumidity = ParseDouble(obj["history"]["dailysummary"][0]["minhumidity"]);
            var latitudeDegrees = ParseDouble(obj["current_observation"]["display_location"]["latitude"]);
            var longitudeDegrees = ParseDouble(obj["current_observation"]["display_location"]["longitude"]);

            var maxTempC = FtoC(maxTempF);
            var minTempC = FtoC(minTempF);
            var elevationM = FttoM(elevationFt);
            var julianDay = inputDate.DayOfYear;


            // step 1, mean daily air temperature C
            var meanDailyAirTemperatureC = new List<double> { maxTempC, minTempC }.Average();

            // step 2, mean solar radiation MJ
            var meanSolarRadiationMJ = MJtoW(meanSolarRadiationW);

            // step 3, avg wind speed Ms at 2m
            var avgWindSpeedMs = MPHtoMs(avgWindSpeedMPH);

            // step 4, slope of saturation vapor pressure curve
            var saturationVaporPressureCurveSlope = saturationVaporPressureCurveSlopeFn(meanDailyAirTemperatureC);

            // step 5, atmospheric pressure
            var atmosphericPressue = atmosphericPressueFn(elevationM);

            // step 6, psycometric constant
            var psychrometricConstant = psychrometricConstantFn(atmosphericPressue);

            // step 7, delta term (DT)
            var deltaTerm = deltaTermFn(saturationVaporPressureCurveSlope, psychrometricConstant, avgWindSpeedMs);

            // step 8, psi term (PT)
            var psiTerm = psiTermFn(saturationVaporPressureCurveSlope, psychrometricConstant, avgWindSpeedMs);

            // step 9, temperature term (TT)
            var temperatureTerm = temperatureTermFn(meanDailyAirTemperatureC, avgWindSpeedMs);

            // step 10, mean saturation vapor pressure curve
            var saturationVaporPressureMean = new List<double> { saturationVaporFn(maxTempC), saturationVaporFn(minTempC) }.Average();

            // step 11, actual vapor pressure
            var saturationVaporPressureActual = saturationVaporPressureActualFn(minTempC, maxTempC, minHumidity, maxHumidity);

            // step 11.1, vapor pressure deficit
            var saturationVaporPressureDeficit = saturationVaporPressureMean - saturationVaporPressureActual;

            // step 12.1 relative sun earth difference
            var relativeEarthSunDifference = relativeEarthSunDifferenceFn(julianDay);

            // step 12.2 relative sun earth difference
            var solarDeclination = solarDeclinationFn(julianDay);

            // step 13 latitude radians
            var latitudeRadians = DEGtoRAD(latitudeDegrees);

            // step 14 sunset hour angle
            var sunsetHourAngle = sunsetHourAngleFn(latitudeRadians, solarDeclination);

            // step 15 extraterrestrial radiation
            var extraterrestrialRadiation = extraterrestrialRadiationFn(relativeEarthSunDifference, sunsetHourAngle, latitudeRadians, solarDeclination);

            // step 16 clear sky solar radiation
            var clearSkySolarRadiation = clearSkySolarRadiationFn(elevationM, extraterrestrialRadiation);

            // step 17 clear sky solar radiation
            var netSolarRadiation = (1 - canopyReflectionCoefficient) * meanSolarRadiationMJ;

            // step 18  Net outgoing long wave solar radiation
            var netOutgoingLongWaveSolarRadiation = netOutgoingLongWaveSolarRadiationFn(minTempC, maxTempC, saturationVaporPressureActual, meanSolarRadiationMJ, clearSkySolarRadiation);

            // step 19 net radiation
            var netRadiation = netSolarRadiation - netOutgoingLongWaveSolarRadiation;

            // step 19.1 net radiation ng in mm
            var netRadiationMM = netRadiation * 0.408;

            // step FS1 radiation term
            var radiationTerm = deltaTerm * netRadiationMM;

            // step FS2 wind term
            var windTerm = psiTerm * temperatureTerm * (saturationVaporPressureMean - saturationVaporPressureActual);

            // step Final, evapotranspiration value
            var evapotranspirationMM = radiationTerm + windTerm;
            var evapotranspirationIN = evapotranspirationMM * 0.0393701;

            var evapoObject = new Evapotranspiration
            {
                AvgDailyAirTemperatureC = meanDailyAirTemperatureC,
                AvgSolarRadiationMJ = meanSolarRadiationMJ,
                AvgWindSpeedMs = avgWindSpeedMs,
                SaturationVaporPressureCurveSlope = saturationVaporPressureCurveSlope,
                AtmosphericPressue = atmosphericPressue,
                PsychrometricConstant = psychrometricConstant,
                DeltaTerm = deltaTerm,
                PsiTerm = psiTerm,
                TemperatureTerm = temperatureTerm,
                SaturationVaporPressureAvg = saturationVaporPressureMean,
                SaturationVaporPressureActual = saturationVaporPressureActual,
                SaturationVaporPressureDeficit = saturationVaporPressureDeficit,
                RelativeEarthSunDifference = relativeEarthSunDifference,
                SolarDeclination = solarDeclination,
                LatitudeRadians = latitudeRadians,
                SunsetHourAngle = sunsetHourAngle,
                ExtraterrestrialRadiation = extraterrestrialRadiation,
                ClearSkySolarRadiation = clearSkySolarRadiation,
                NetSolarRadiation = netSolarRadiation,
                NetOutgoingLongWaveSolarRadiation = netOutgoingLongWaveSolarRadiation,
                NetRadiation = netRadiation,
                NetRadiationMM = netRadiationMM,
                RadiationTerm = radiationTerm,
                WindTerm = windTerm,
                EvapotranspirationIN = evapotranspirationIN,
                EvapotranspirationMM = evapotranspirationMM
            };

            return evapoObject;
        }


        private double ParseDouble(string value)
        {
            return double.Parse(value, NumberStyles.Any, CultureInfo.InvariantCulture);
        }

        private double FtoC(double F)
        {
            return (F - 32) / 1.8;
        }

        private double FttoM(double Ft)
        {
            return Ft * 0.3048;
        }

        private double MJtoW(double MJ)
        {
            return MJ * 0.0864;
        }

        private double MPHtoMs(double MPH)
        {
            return MPH * 0.477;
        }

        private double DEGtoRAD(double DEG)
        {
            return (double)Math.PI / 180 * DEG;
        }

        private double saturationVaporFn(double temperature)
        {
            return (0.6108 * Math.Exp((enthalpy * temperature) / (temperature + vaporRate)));
        }

        private double saturationVaporPressureCurveSlopeFn(double temperature)
        {
            var top = 4098 * (saturationVaporFn(temperature));
            var bottom = Math.Pow((temperature + vaporRate), 2);
            return top / bottom;
        }

        private double atmosphericPressueFn(double elevationM)
        {
            var inner = ((293 - 0.0065 * elevationM) / 293);
            return 101.3 * Math.Pow(inner, 5.26);
        }

        private double psychrometricConstantFn(double atmosphericPressue)
        {
            return 0.000665 * atmosphericPressue;
        }

        private double deltaTermFn(double saturationVaporPressureCurveSlope, double psychrometricConstant, double avgWindSpeedMs)
        {
            var top = saturationVaporPressureCurveSlope;
            var bottom = saturationVaporPressureCurveSlope + psychrometricConstant * (1 + 0.34 * avgWindSpeedMs);
            return top / bottom;
        }

        private double psiTermFn(double saturationVaporPressureCurveSlope, double psychrometricConstant, double avgWindSpeedMs)
        {
            var top = psychrometricConstant;
            var bottom = saturationVaporPressureCurveSlope + psychrometricConstant * (1 + 0.34 * avgWindSpeedMs);
            return top / bottom;
        }

        private double temperatureTermFn(double meanDailyAirTemperatureC, double avgWindSpeedMs)
        {
            return ((900) / (meanDailyAirTemperatureC + kelvin) * avgWindSpeedMs);
        }

        private double saturationVaporPressureActualFn(double minTempC, double maxTempC, double minHumidity, double maxHumidity)
        {
            var rel1 = saturationVaporFn(minTempC) * (maxHumidity / 100);
            var rel2 = saturationVaporFn(maxTempC) * (minHumidity / 100);
            return new List<double> { rel1, rel2 }.Average();
        }

        private double relativeEarthSunDifferenceFn(double julianDay)
        {
            return 1 + 0.033 * Math.Cos(((2 * Math.PI) / 365) * julianDay);
        }

        private double solarDeclinationFn(double julianDay)
        {
            return 0.409 * Math.Sin(((2 * Math.PI) / 365) * julianDay - 1.39);
        }

        private double sunsetHourAngleFn(double latitudeRadians, double solarDeclination)
        {
            return Math.Acos(-1 * Math.Tan(latitudeRadians) * Math.Tan(solarDeclination));
        }

        private double extraterrestrialRadiationFn(double relativeEarthSunDifference, double sunsetHourAngle, double latitudeRadians, double solarDeclination)
        {
            var rel1 = 24 * 60 / Math.PI;
            var rel2 = solarConstant * relativeEarthSunDifference;
            var rel3 = ((sunsetHourAngle * Math.Sin(latitudeRadians) * Math.Sin(solarDeclination)) + (Math.Cos(latitudeRadians) * Math.Cos(solarDeclination) * Math.Sin(sunsetHourAngle)));
            return rel1 * rel2 * rel3;
        }

        private double clearSkySolarRadiationFn(double elevationM, double extraterrestrialRadiation)
        {
            return (0.75 + (2 * Math.Pow(10, -5)) * elevationM) * extraterrestrialRadiation;
        }

        private double netOutgoingLongWaveSolarRadiationFn(double minTempC, double maxTempC, double saturationVaporPressureActual, double meanSolarRadiationMJ, double clearSkySolarRadiation)
        {
            var rel1 = 4.903 * Math.Pow(10, -9);
            var rel2 = new List<double> { Math.Pow((maxTempC + kelvin), 4), Math.Pow((minTempC + kelvin), 4) }.Average();
            var rel3 = (0.34 - 0.14 * Math.Sqrt(saturationVaporPressureActual));
            var rel4 = 1.35 * meanSolarRadiationMJ / clearSkySolarRadiation - 0.35;
            return rel1 * rel2 * rel3 * rel4;
        }
    }
}
