// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using NUnit.Framework;

namespace SAM.Analytical.Grasshopper.Tas.Tests
{
    /// <summary>
    /// The outputs of <c>Tas.AddBuildingResults</c> after the units correction (SAM_Tas#92, option A). The regression that
    /// matters: a Tas result written before the correction (Wh/W, unmarked) and one written after it (kWh/kW, marked)
    /// give the component the same numbers, and the same numbers it output before, with no second ÷ 1000. The numbers are
    /// the licensed SAM_Tas#92 measurements.
    /// </summary>
    [TestFixture]
    public class BuildingResultsTests
    {
        private const string TasSource = "SAM.Analytical.Tas";
        private const string OpenStudioSource = "SAM.Analytical.OpenStudio";

        // SAM_Tas#92: Systems Demo TSD copy, as SAM_Tas stored it before the correction.
        private const double DemoHeatingWh = 15_227_840.66370964;
        private const double DemoPeakHeatingW = 38_727.40625;

        // SAM_Tas#92: the SAM-generated Part O model's cooling (PR7a measured 2 523.75 kWh).
        private const double PartOCoolingWh = 2_523_751.8729965687;

        private const double PeakCoolingW = 21_456.875;
        private const double FloorArea = 1_234.5;
        private const double Volume = 3_703.5;

        [Test]
        public void LegacyAndMarkedTasResults_GiveIdenticalOutputs_EqualToThePreCorrectionComponent()
        {
            BuildingResults legacy = new BuildingResults(Legacy(DemoHeatingWh, PartOCoolingWh, DemoPeakHeatingW, PeakCoolingW, FloorArea, Volume));
            BuildingResults marked = new BuildingResults(Marked(DemoHeatingWh / 1000, PartOCoolingWh / 1000, DemoPeakHeatingW / 1000, PeakCoolingW / 1000, FloorArea, Volume));
            double[] expected = PreCorrectionOutputs(DemoHeatingWh, PartOCoolingWh, DemoPeakHeatingW, PeakCoolingW, FloorArea, Volume);

            AssertBitIdentical(expected, Outputs(legacy));
            AssertBitIdentical(expected, Outputs(marked));
            Assert.That(legacy.UnitsKnown, Is.True);
            Assert.That(marked.UnitsKnown, Is.True);
            Assert.That(legacy.ResultUnitsState, Is.EqualTo(ResultUnitsState.LegacyTas));
            Assert.That(marked.ResultUnitsState, Is.EqualTo(ResultUnitsState.Marked));
        }

        [Test]
        public void RecordedNumbers_InKilowattHoursAndKilowatts_PeaksPerAreaAndVolumeInWatts()
        {
            BuildingResults marked = new BuildingResults(Marked(DemoHeatingWh / 1000, PartOCoolingWh / 1000, DemoPeakHeatingW / 1000, PeakCoolingW / 1000, FloorArea, Volume));

            Assert.That(marked.ConsumptionHeating, Is.EqualTo(15_227.84066370964).Within(1e-9));
            Assert.That(marked.ConsumptionCooling, Is.EqualTo(2_523.7518729965687).Within(1e-9));
            Assert.That(marked.PeakHeatingLoad, Is.EqualTo(38.72740625).Within(1e-12));

            // W/m2 and W/m3: the peak in W over the area or volume, not kW and not kWh.
            Assert.That(marked.PeakHeatingLoadPerArea, Is.EqualTo(Core.Query.Round(DemoPeakHeatingW / FloorArea, 0.01)));
            Assert.That(marked.PeakHeatingLoadPerVolume, Is.EqualTo(Core.Query.Round(DemoPeakHeatingW / Volume, 0.01)));
            Assert.That(marked.PeakHeatingLoadPerArea, Is.EqualTo(31.37));

            // kWh/m2 and kWh/m3.
            Assert.That(marked.ConsumptionHeatingPerArea, Is.EqualTo(Core.Query.Round(15_227.84066370964 / FloorArea, 0.01)));
            Assert.That(marked.ConsumptionHeatingPerVolume, Is.EqualTo(Core.Query.Round(15_227.84066370964 / Volume, 0.01)));

            Assert.That(marked.PeakHeatingHour, Is.EqualTo(205));
            Assert.That(marked.PeakCoolingHour, Is.EqualTo(4602));
        }

        [Test]
        public void LegacyAndMarked_MatchThePreCorrectionComponent_AcrossASweep()
        {
            Random random = new Random(20261012);
            for (int i = 0; i < 2000; i++)
            {
                double heatingWh = random.NextDouble() * System.Math.Pow(10, random.Next(0, 10));
                double coolingWh = random.NextDouble() * System.Math.Pow(10, random.Next(0, 10));
                double peakHeatingW = random.NextDouble() * System.Math.Pow(10, random.Next(0, 7));
                double peakCoolingW = random.NextDouble() * System.Math.Pow(10, random.Next(0, 7));
                double floorArea = random.Next(0, 10) == 0 ? 0 : 1 + random.NextDouble() * System.Math.Pow(10, random.Next(0, 5));
                double volume = floorArea * (2 + random.NextDouble() * 4);

                double[] expected = PreCorrectionOutputs(heatingWh, coolingWh, peakHeatingW, peakCoolingW, floorArea, volume);
                AssertBitIdentical(expected, Outputs(new BuildingResults(Legacy(heatingWh, coolingWh, peakHeatingW, peakCoolingW, floorArea, volume))));
                AssertBitIdentical(expected, Outputs(new BuildingResults(Marked(heatingWh / 1000, coolingWh / 1000, peakHeatingW / 1000, peakCoolingW / 1000, floorArea, volume))));
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void OpenStudioResult_IsReadAsKilo_NotDividedAgain(bool marked)
        {
            AnalyticalModelSimulationResult result = Result(OpenStudioSource, DemoHeatingWh / 1000, PartOCoolingWh / 1000, DemoPeakHeatingW / 1000, PeakCoolingW / 1000, FloorArea, Volume);
            if (marked)
            {
                result.SetResultUnitsVersion();
            }

            AssertBitIdentical(PreCorrectionOutputs(DemoHeatingWh, PartOCoolingWh, DemoPeakHeatingW, PeakCoolingW, FloorArea, Volume), Outputs(new BuildingResults(result)));
        }

        [TestCase("Tas", 0)]
        [TestCase(TasSource, 2)]
        public void UnknownUnits_OutputNoConsumptionOrPeak_ButKeepThePeakHours(string source, int marker)
        {
            AnalyticalModelSimulationResult result = Result(source, DemoHeatingWh, PartOCoolingWh, DemoPeakHeatingW, PeakCoolingW, FloorArea, Volume);
            if (marker != 0)
            {
                result.SetValue(AnalyticalModelSimulationResultParameter.ResultUnitsVersion, marker);
            }

            BuildingResults buildingResults = new BuildingResults(result);

            Assert.That(buildingResults.UnitsKnown, Is.False);
            Assert.That(buildingResults.ConsumptionHeating, Is.NaN);
            Assert.That(buildingResults.ConsumptionHeatingPerArea, Is.NaN);
            Assert.That(buildingResults.PeakHeatingLoad, Is.NaN);
            Assert.That(buildingResults.PeakHeatingLoadPerVolume, Is.NaN);
            Assert.That(buildingResults.ConsumptionCooling, Is.NaN);
            Assert.That(buildingResults.PeakCoolingLoadPerArea, Is.NaN);
            Assert.That(buildingResults.PeakHeatingHour, Is.EqualTo(205));
            Assert.That(buildingResults.PeakCoolingHour, Is.EqualTo(4602));
        }

        [Test]
        public void NoResult_IsRefused()
        {
            Assert.Throws<ArgumentNullException>(() => new BuildingResults(null));
        }

        // ---- helpers --------------------------------------------------------------------------------------------

        private static AnalyticalModelSimulationResult Legacy(double heatingWh, double coolingWh, double peakHeatingW, double peakCoolingW, double floorArea, double volume)
        {
            return Result(TasSource, heatingWh, coolingWh, peakHeatingW, peakCoolingW, floorArea, volume);
        }

        private static AnalyticalModelSimulationResult Marked(double heatingKWh, double coolingKWh, double peakHeatingKW, double peakCoolingKW, double floorArea, double volume)
        {
            AnalyticalModelSimulationResult result = Result(TasSource, heatingKWh, coolingKWh, peakHeatingKW, peakCoolingKW, floorArea, volume);
            Assert.That(result.SetResultUnitsVersion(), Is.True);
            return result;
        }

        private static AnalyticalModelSimulationResult Result(string source, double consumptionHeating, double consumptionCooling, double peakHeatingLoad, double peakCoolingLoad, double floorArea, double volume)
        {
            AnalyticalModelSimulationResult result = new AnalyticalModelSimulationResult("Model", source, "model.tsd");
            result.SetValue(AnalyticalModelSimulationResultParameter.ConsumptionHeating, consumptionHeating);
            result.SetValue(AnalyticalModelSimulationResultParameter.PeakHeatingLoad, peakHeatingLoad);
            result.SetValue(AnalyticalModelSimulationResultParameter.PeakHeatingHour, 205);
            result.SetValue(AnalyticalModelSimulationResultParameter.ConsumptionCooling, consumptionCooling);
            result.SetValue(AnalyticalModelSimulationResultParameter.PeakCoolingLoad, peakCoolingLoad);
            result.SetValue(AnalyticalModelSimulationResultParameter.PeakCoolingHour, 4602);
            result.SetValue(AnalyticalModelSimulationResultParameter.FloorArea, floorArea);
            result.SetValue(AnalyticalModelSimulationResultParameter.Volume, volume);
            return result;
        }

        private static double[] Outputs(BuildingResults buildingResults)
        {
            return
            [
                buildingResults.ConsumptionHeating, buildingResults.ConsumptionHeatingPerArea, buildingResults.ConsumptionHeatingPerVolume,
                buildingResults.PeakHeatingLoad, buildingResults.PeakHeatingLoadPerArea, buildingResults.PeakHeatingLoadPerVolume, buildingResults.PeakHeatingHour,
                buildingResults.ConsumptionCooling, buildingResults.ConsumptionCoolingPerArea, buildingResults.ConsumptionCoolingPerVolume,
                buildingResults.PeakCoolingLoad, buildingResults.PeakCoolingLoadPerArea, buildingResults.PeakCoolingLoadPerVolume, buildingResults.PeakCoolingHour,
            ];
        }

        // The component's outputs exactly as TasTSDAddBuildingResults computed them before the units correction, from
        // SAM_Tas's Wh and W (it divided by 1000 itself).
        private static double[] PreCorrectionOutputs(double heatingWh, double coolingWh, double peakHeatingW, double peakCoolingW, double floorArea, double volume)
        {
            double consumptionHeating = heatingWh / 1000;
            double peakHeatingLoad = peakHeatingW / 1000;
            double consumptionCooling = coolingWh / 1000;
            double peakCoolingLoad = peakCoolingW / 1000;

            return
            [
                consumptionHeating,
                Core.Query.Round(floorArea == 0 ? 0 : consumptionHeating / floorArea, 0.01),
                Core.Query.Round(volume == 0 ? 0 : consumptionHeating / volume, 0.01),
                peakHeatingLoad,
                Core.Query.Round(floorArea == 0 ? 0 : peakHeatingLoad / floorArea * 1000, 0.01),
                Core.Query.Round(volume == 0 ? 0 : peakHeatingLoad / volume * 1000, 0.01),
                205,
                consumptionCooling,
                Core.Query.Round(floorArea == 0 ? 0 : consumptionCooling / floorArea, 0.01),
                Core.Query.Round(volume == 0 ? 0 : consumptionCooling / volume, 0.01),
                peakCoolingLoad,
                Core.Query.Round(floorArea == 0 ? 0 : peakCoolingLoad / floorArea * 1000, 0.01),
                Core.Query.Round(volume == 0 ? 0 : peakCoolingLoad / volume * 1000, 0.01),
                4602,
            ];
        }

        private static void AssertBitIdentical(double[] expected, double[] actual)
        {
            Assert.That(actual.Length, Is.EqualTo(expected.Length));
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.That(BitConverter.DoubleToInt64Bits(actual[i]), Is.EqualTo(BitConverter.DoubleToInt64Bits(expected[i])), string.Format("output {0}: expected {1}, got {2}", i, expected[i], actual[i]));
            }
        }
    }
}
