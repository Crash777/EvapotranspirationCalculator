namespace EvapotranspirationCalculator.Models
{
    public class Evapotranspiration
    {
        public double AvgDailyAirTemperatureC { get; set; }
        public double AvgSolarRadiationMJ { get; set; }
        public double AvgWindSpeedMs { get; set; }
        public double SaturationVaporPressureCurveSlope { get; set; }
        public double AtmosphericPressue { get; set; }
        public double PsychrometricConstant { get; set; }
        public double DeltaTerm { get; set; }
        public double PsiTerm { get; set; }
        public double TemperatureTerm { get; set; }
        public double SaturationVaporPressureAvg { get; set; }
        public double SaturationVaporPressureActual { get; set; }
        public double SaturationVaporPressureDeficit { get; set; }
        public double RelativeEarthSunDifference { get; set; }
        public double SolarDeclination { get; set; }
        public double LatitudeRadians { get; set; }
        public double SunsetHourAngle { get; set; }
        public double ExtraterrestrialRadiation { get; set; }
        public double ClearSkySolarRadiation { get; set; }
        public double NetSolarRadiation { get; set; }
        public double NetOutgoingLongWaveSolarRadiation { get; set; }
        public double NetRadiation { get; set; }
        public double NetRadiationMM { get; set; }
        public double RadiationTerm { get; set; }
        public double WindTerm { get; set; }
        public double EvapotranspirationMM { get; set; }
        public double EvapotranspirationIN { get; set; }
    }
}
