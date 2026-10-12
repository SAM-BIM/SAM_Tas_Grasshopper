// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;

namespace SAM.Analytical.Grasshopper.Tas
{
    /// <summary>
    /// The building-level outputs of <c>Tas.AddBuildingResults</c>, from an <see cref="AnalyticalModelSimulationResult"/>:
    /// consumption in kWh, kWh/m2 and kWh/m3, peak load in kW, W/m2 and W/m3, and the peak hours.
    /// <para>
    /// Consumption and peak loads are read through <see cref="Analytical.Query.TryGetConsumptionOrPeakLoad"/>, which
    /// returns kWh and kW whichever units the result was written in (SAM_Tas now writes kWh/kW and marks the result; a
    /// result written before the units correction holds Wh/W and is converted there, once), so nothing here divides by
    /// 1000. When the units are not known the values are NaN and <see cref="UnitsKnown"/> is false. Holds no Tas or
    /// Grasshopper type, so it is tested without either.
    /// </para>
    /// </summary>
    public sealed class BuildingResults
    {
        public BuildingResults(AnalyticalModelSimulationResult analyticalModelSimulationResult)
        {
            if (analyticalModelSimulationResult == null)
            {
                throw new ArgumentNullException(nameof(analyticalModelSimulationResult));
            }

            ResultUnitsState = analyticalModelSimulationResult.ResultUnitsState();

            ConsumptionHeating = analyticalModelSimulationResult.ConsumptionOrPeakLoad(AnalyticalModelSimulationResultParameter.ConsumptionHeating);
            PeakHeatingLoad = analyticalModelSimulationResult.ConsumptionOrPeakLoad(AnalyticalModelSimulationResultParameter.PeakHeatingLoad);
            PeakHeatingHour = analyticalModelSimulationResult.GetValue<int>(AnalyticalModelSimulationResultParameter.PeakHeatingHour);

            ConsumptionCooling = analyticalModelSimulationResult.ConsumptionOrPeakLoad(AnalyticalModelSimulationResultParameter.ConsumptionCooling);
            PeakCoolingLoad = analyticalModelSimulationResult.ConsumptionOrPeakLoad(AnalyticalModelSimulationResultParameter.PeakCoolingLoad);
            PeakCoolingHour = analyticalModelSimulationResult.GetValue<int>(AnalyticalModelSimulationResultParameter.PeakCoolingHour);

            double volume = analyticalModelSimulationResult.GetValue<double>(AnalyticalModelSimulationResultParameter.Volume);
            if (!double.IsNaN(volume))
            {
                if (!double.IsNaN(ConsumptionHeating))
                {
                    ConsumptionHeatingPerVolume = Core.Query.Round(volume == 0 ? 0 : ConsumptionHeating / volume, 0.01);
                }

                if (!double.IsNaN(PeakHeatingLoad))
                {
                    PeakHeatingLoadPerVolume = Core.Query.Round(volume == 0 ? 0 : PeakHeatingLoad / volume * 1000, 0.01);
                }

                if (!double.IsNaN(ConsumptionCooling))
                {
                    ConsumptionCoolingPerVolume = Core.Query.Round(volume == 0 ? 0 : ConsumptionCooling / volume, 0.01);
                }

                if (!double.IsNaN(PeakCoolingLoad))
                {
                    PeakCoolingLoadPerVolume = Core.Query.Round(volume == 0 ? 0 : PeakCoolingLoad / volume * 1000, 0.01);
                }
            }

            double floorArea = analyticalModelSimulationResult.GetValue<double>(AnalyticalModelSimulationResultParameter.FloorArea);
            if (!double.IsNaN(floorArea))
            {
                if (!double.IsNaN(ConsumptionHeating))
                {
                    ConsumptionHeatingPerArea = Core.Query.Round(floorArea == 0 ? 0 : ConsumptionHeating / floorArea, 0.01);
                }

                if (!double.IsNaN(PeakHeatingLoad))
                {
                    PeakHeatingLoadPerArea = Core.Query.Round(floorArea == 0 ? 0 : PeakHeatingLoad / floorArea * 1000, 0.01);
                }

                if (!double.IsNaN(ConsumptionCooling))
                {
                    ConsumptionCoolingPerArea = Core.Query.Round(floorArea == 0 ? 0 : ConsumptionCooling / floorArea, 0.01);
                }

                if (!double.IsNaN(PeakCoolingLoad))
                {
                    PeakCoolingLoadPerArea = Core.Query.Round(floorArea == 0 ? 0 : PeakCoolingLoad / floorArea * 1000, 0.01);
                }
            }
        }

        public ResultUnitsState ResultUnitsState { get; }

        public bool UnitsKnown => ResultUnitsState == ResultUnitsState.Marked || ResultUnitsState == ResultUnitsState.LegacyTas || ResultUnitsState == ResultUnitsState.LegacyOpenStudio;

        /// <summary>Consumption Heating [kWh].</summary>
        public double ConsumptionHeating { get; } = double.NaN;

        /// <summary>Consumption Heating Per Area [kWh/m2].</summary>
        public double ConsumptionHeatingPerArea { get; } = double.NaN;

        /// <summary>Consumption Heating Per Volume [kWh/m3].</summary>
        public double ConsumptionHeatingPerVolume { get; } = double.NaN;

        /// <summary>Peak Heating Load [kW].</summary>
        public double PeakHeatingLoad { get; } = double.NaN;

        /// <summary>Peak Heating Load Per Area [W/m2].</summary>
        public double PeakHeatingLoadPerArea { get; } = double.NaN;

        /// <summary>Peak Heating Load Per Volume [W/m3].</summary>
        public double PeakHeatingLoadPerVolume { get; } = double.NaN;

        /// <summary>Peak Heating Hour [0-8759].</summary>
        public int PeakHeatingHour { get; } = -1;

        /// <summary>Consumption Cooling [kWh].</summary>
        public double ConsumptionCooling { get; } = double.NaN;

        /// <summary>Consumption Cooling Per Area [kWh/m2].</summary>
        public double ConsumptionCoolingPerArea { get; } = double.NaN;

        /// <summary>Consumption Cooling Per Volume [kWh/m3].</summary>
        public double ConsumptionCoolingPerVolume { get; } = double.NaN;

        /// <summary>Peak Cooling Load [kW].</summary>
        public double PeakCoolingLoad { get; } = double.NaN;

        /// <summary>Peak Cooling Load Per Area [W/m2].</summary>
        public double PeakCoolingLoadPerArea { get; } = double.NaN;

        /// <summary>Peak Cooling Load Per Volume [W/m3].</summary>
        public double PeakCoolingLoadPerVolume { get; } = double.NaN;

        /// <summary>Peak Cooling Hour [0-8759].</summary>
        public int PeakCoolingHour { get; } = -1;
    }
}
