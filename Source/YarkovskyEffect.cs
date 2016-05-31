using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;

namespace WhitecatIndustries
{
    class YarkovskyEffect : MonoBehaviour
    {
        public static double FetchDeltaSMA(Vessel vessel)
        {
            if (TimeWarp.CurrentRate > 1.0)
            {
                return SeasonalSMAChange(vessel);
            }

            else
            {
                return DiuralSMAChange(vessel);
            }
        }


        public static double DiuralSMAChange(Vessel vessel)
        {
            double RateOfChangeOfSMA = 0;
            double InitialSMA = vessel.orbitDriver.orbit.semiMajorAxis;
            double Albedo = 0.12; // Work on something better for this! Currently using Worn Asphalt as a guide... 
            double MeanMotion = (360.0) / vessel.orbitDriver.orbit.period;
            double Area = VesselData.FetchArea(vessel);
            double Mass = VesselData.FetchMass(vessel);
            double Radius = Math.Sqrt(Area / Math.PI);
            double Volume = (4.0 / 3.0) * Math.PI * Math.Pow(Radius, 3.0);
            double Density = Mass / Volume;
            double c = Math.Pow(8.0 * 10, 8);
            double Altitude = 0;
            if (vessel.orbitDriver.orbit.referenceBody == Sun.Instance.sun) // Checks for the sun
            {
                Altitude = vessel.orbitDriver.orbit.altitude;
            }
            else
            {
                Altitude = vessel.orbitDriver.orbit.referenceBody.orbit.altitude;
            }

            double SolarEnergy = (double)Math.Pow(((double)3.86 * (double)10.0), (double)26.0);
            double SolarConstant = 0.0;
            SolarConstant = SolarEnergy / ((double)4.0 * (double)Math.PI * (double)Math.Pow(Altitude, (double)2.0)); // W/m^2

            double BoltzmannConstant = Math.Pow(6.68 * 10.0, -11.0);
            double SolarTemperature = vessel.externalTemperature; // try this!
            double Epsilon = UnityEngine.Random.Range(0.75f, 0.9f);
            double LocalForce = 0;
            LocalForce = (Epsilon * BoltzmannConstant * Math.Pow(SolarTemperature, 4.0)) / Albedo;

            double Iota = (3.0 * LocalForce) / (4.0 * Radius * Density * c);
            Vector3d RotationAxis = vessel.upAxis;

            float ObliquityOfSpinAxis = 0;
            double AmplitudeOfRotation = 0;
            double RotationPhase = 0;

            if (LoadingCheck.PersistentRotationInstalled)
            {
                // Add Persistent Rotation Compatibility here! //1.6.0
            }

            else
            {
                if (vessel.isActiveVessel)
                {
                    Quaternion VesselRotation = vessel.srfRelRotation;
                    Vector3 TemporaryAxis;
                    VesselRotation.ToAngleAxis(out ObliquityOfSpinAxis, out TemporaryAxis);
                    Vector3 AngularSpeed = (TemporaryAxis * ObliquityOfSpinAxis); // Rotation Per Second?
                    AmplitudeOfRotation = AngularSpeed.magnitude;
                    RotationPhase = Vector3.Angle(RotationAxis, TemporaryAxis);
                }

                else
                {
                    Quaternion VesselRotation = new Quaternion().Inverse(); // Issues here work out a background rotational calculation?
                    Vector3 TemporaryAxis;
                    VesselRotation.ToAngleAxis(out ObliquityOfSpinAxis, out TemporaryAxis);
                    Vector3 AngularSpeed = (TemporaryAxis * ObliquityOfSpinAxis); // Rotation Per Second?
                    AmplitudeOfRotation = AngularSpeed.magnitude;
                    RotationPhase = Vector3.Angle(RotationAxis, TemporaryAxis);
                }
            }
            
            double RotationFrequency = AmplitudeOfRotation;
            double DiuralThermalParameter = 0;
            double Gamma = 0;
            double SpecificHeatCapacity = 670; // Lets not model this too far yet... maybe for 1.6.0 using c of Regolith for now
            double ThermalConductivity = (Math.Pow(100.0, 2.0)/ (Density * SpecificHeatCapacity));
            double PenetrationDepth = 0;
            double X = 0;

            DiuralThermalParameter = (Math.Sqrt(ThermalConductivity * Density * SpecificHeatCapacity * RotationFrequency) / (Epsilon * BoltzmannConstant * Math.Pow(SolarTemperature, 3.0) * Altitude));
            PenetrationDepth = Math.Sqrt(ThermalConductivity/(Density * SpecificHeatCapacity * RotationFrequency));
            X = (Radius * Math.Sqrt(2)) / PenetrationDepth;
            Gamma = DiuralThermalParameter / X;

            double BigOFunctionOfEccentricity = Math.Pow(vessel.orbitDriver.orbit.eccentricity, 0.0);
            RateOfChangeOfSMA = ((-8.0 * Albedo) / (9.0 * MeanMotion)) * Iota * ((AmplitudeOfRotation * Math.Sin(RotationPhase)) / 1.0 + Gamma) * Math.Cos(ObliquityOfSpinAxis) + 0;

            if (double.IsNaN(RateOfChangeOfSMA))
            {
                RateOfChangeOfSMA = 0;
            }

            //print("RateOfChangeOfSMAYarkovsky: " + RateOfChangeOfSMA);

            return RateOfChangeOfSMA;
        }

        public static double SeasonalSMAChange(Vessel vessel)
        {
            double RateOfChangeOfSMA = 0;

            // Look up seasonal changes in SMA, for now: (1.6.0)
            RateOfChangeOfSMA = DiuralSMAChange(vessel) * TimeWarp.CurrentRate;

            return RateOfChangeOfSMA;
        }
    }
}
