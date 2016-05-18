/*
 * Whitecat Industries Orbital Decay for Kerbal Space Program. for Kerbal Space Program. 
 * 
 * Written by Whitecat106 (Marcus Hehir).
 * 
 * Kerbal Space Program is Copyright (C) 2016 Squad. See http://kerbalspaceprogram.com/. This
 * project is in no way associated with nor endorsed by Squad.
 * 
 * This code is licensed under the Attribution-NonCommercial-ShareAlike 3.0 (CC BY-NC-SA 3.0)
 * creative commons license. See <http://creativecommons.org/licenses/by-nc-sa/3.0/legalcode>
 * for full details.
 * 
 * Attribution — You are free to modify this code, so long as you mention that the resulting
 * work is based upon or adapted from this code.
 * 
 * Non-commercial - You may not use this work for commercial purposes.
 * 
 * Share Alike — If you alter, transform, or build upon this work, you may distribute the
 * resulting work only under the same or similar license to the CC BY-NC-SA 3.0 license.
 * 
 * Note that Whitecat Industries is a ficticious entity created for entertainment
 * purposes. It is in no way meant to represent a real entity. Any similarity to a real entity
 * is purely coincidental.
 */

// NASA Calculations: http://ntrs.nasa.gov/archive/nasa/casi.ntrs.nasa.gov/19700019279.pdf

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;

namespace WhitecatIndustries
{
    [KSPAddon(KSPAddon.Startup.EveryScene, false)]
    public class MasConManager : MonoBehaviour
    {
        static double TimeInterval = 1.0; // Timewarp managed by HighLogic Current Time 

        public void Start()
        {
            MasConData.LoadData();
        }

        // If MasConData.CheckMasConProximity = true then do these

        public static double GetCalculatedSMAChange(Vessel vessel, double LAN, double MNA, double LPE, double e, double Inc, double SMA, double EPH)
        {
            #region GenericEquations

            double EquivalentAltitude = 0;
            double AltitudeAp = (SMA * (1 + e) - vessel.orbitDriver.orbit.referenceBody.Radius);
            double AltitudePe = (SMA * (1 - e) - vessel.orbitDriver.orbit.referenceBody.Radius);
            EquivalentAltitude = (AltitudePe) + 900.0 * Math.Pow(e, (double)0.6);

            double GalAtVesselDistance = 0;
            double CentreLat = 0;
            double CentreLong = 0;
            double CentreGal = 0;
            double GalToGFactor = 0.00101972;

            //Required for Integration 

            if (vessel.orbitDriver.orbit.referenceBody.GetName() == "Earth" || vessel.orbitDriver.orbit.referenceBody.GetName() == "Kerbin") // At the moment only these modeled
            {
                GalAtVesselDistance = MasConData.GalAtPosition(vessel);
                CentreLat = double.Parse(MasConData.LocalMasCon(vessel).GetValue("centreLat"));
                CentreGal = double.Parse(MasConData.LocalMasCon(vessel).GetValue("centreGal"));
                CentreLong = double.Parse(MasConData.LocalMasCon(vessel).GetValue("centreLong"));
            }
            else
            {
                GalAtVesselDistance = UnityEngine.Random.Range((float)(vessel.orbitDriver.orbit.referenceBody.GeeASL * GalToGFactor * 0.9995), (float)(vessel.orbitDriver.orbit.referenceBody.GeeASL * GalToGFactor * 1.0005));
                CentreLat = vessel.latitude;
                CentreLong = vessel.longitude;
                CentreGal = GalAtVesselDistance;
            }

            double GAtVesselDistance = GalAtVesselDistance * GalToGFactor;

            double RAANMascon = CentreLong; // [Degrees]
            double DECMascon = CentreLat; // [Degrees]
            double RAANVessel = vessel.longitude; // [Degrees]
            double DECVessel = vessel.latitude; // [Degrees]

            double TrueAnomaly = vessel.orbitDriver.orbit.trueAnomaly; //Math.Acos((((1.0 - (e * e)) / (SMA / EquivalentAltitude)) - 1.0) / e); // v [Degrees]
            double ArgumentOfLatitude = MasConData.ToRadians(LPE) + TrueAnomaly; // u [Degrees]

            ///
            ArgumentOfLatitude = MasConData.ToDegrees(ArgumentOfLatitude); // u [Radians]
            ///

            if (double.IsNaN(TrueAnomaly))
            {
                TrueAnomaly = 0.0;
            }

            //print("TrueAnomaly: " + TrueAnomaly);

            double SubvectorA = Math.Cos(DECMascon) * Math.Cos(RAANMascon - RAANVessel); // A [Vector]
            double SubvectorB = Math.Sin(Inc) * Math.Sin(DECMascon) + Math.Cos(Inc) * Math.Cos(DECMascon) * Math.Sin(RAANMascon - RAANVessel); // B [Vector]
            double SubvectorC = Math.Cos(Inc) * Math.Sin(DECMascon) - Math.Sin(Inc) * Math.Cos(DECMascon) * Math.Sin(RAANMascon - RAANVessel); // C [Vector]

            //print("SubvectA: " + SubvectorA);
            //print("SubvectB: " + SubvectorB);
            //print("SubvectC: " + SubvectorC);

            double GravitationalConstant = 6.67408 * Math.Pow(10.0, -11.0); // G [Newton Meter Squared per Square Kilograms] 
            double GravitationalParameter = vessel.orbitDriver.orbit.referenceBody.gravParameter; // Mu [Newton Meter Squared Per Kilogram] 
            double BodyMass = vessel.orbitDriver.orbit.referenceBody.Mass; // M [Kilograms]
            double BodyGASL = vessel.orbitDriver.orbit.referenceBody.GeeASL; // Fg [Gee ASL]
            double BodyRadius = vessel.orbitDriver.orbit.referenceBody.Radius; // R [Meters] 
            double VesselAltitude = vessel.orbitDriver.orbit.semiMajorAxis - BodyRadius; // r [Meters]
            double MasConMass = (((BodyRadius * BodyRadius)) * ((GalAtVesselDistance * GalToGFactor) + (BodyGASL)) * 9.81) / (GravitationalConstant * BodyMass); // m [Kilograms] (CentreGal * GalToGFactor)


            MasConMass = Math.Abs((1.0 - MasConMass) * BodyMass); // Work on this here to made Gal changes effect orbits

            print("MasConMass: " + MasConMass);

            double SubvectorR = ((GravitationalConstant * MasConMass) / (Math.Pow(VesselAltitude, 3.0))) * (((3.0 * Math.Pow(BodyRadius, 2.0)) / (2.0 * VesselAltitude))) *
                (1.0 - (3.0f / 2.0f) * (Math.Pow(SubvectorA, 2.0) + Math.Pow(SubvectorB, 2.0)) - 3.0 * SubvectorA * SubvectorB * Math.Sin(2.0 * ArgumentOfLatitude) - (3.0f / 2.0f) *
                (Math.Pow(SubvectorA, 2.0) - Math.Pow(SubvectorB, 2.0)) * Math.Cos(2.0 * ArgumentOfLatitude)); // R- [Vector]

            double SubvectorQ = ((GravitationalConstant * MasConMass) / (Math.Pow(VesselAltitude, 3.0))) * ((3.0 * Math.Pow(BodyRadius, 2.0)) / (VesselAltitude)) *
                (SubvectorA * SubvectorB * Math.Cos(2.0 * ArgumentOfLatitude) - (1.0 / 2.0) * (Math.Pow(SubvectorA, 2.0) - Math.Pow(SubvectorB, 2.0)) * Math.Sin(2.0 * ArgumentOfLatitude)); // Q- [Vector]

            double SubvectorS = ((GravitationalConstant * MasConMass) / (Math.Pow(VesselAltitude, 3.0))) * (((3.0 * Math.Pow(BodyRadius, 2.0)) / (VesselAltitude))) *
                SubvectorC * (SubvectorA * Math.Cos(ArgumentOfLatitude) + SubvectorB * Math.Sin(ArgumentOfLatitude)); // S- [Vector] 

            //print("SubVectR: " + SubvectorR);
            //print("SubVectQ: " + SubvectorQ);
            //print("SubVectS: " + SubvectorS);

            double MeanMotion = (360.0) / vessel.orbitDriver.orbit.period; //Math.Sqrt(GravitationalParameter / (Math.Pow(SMA, 3.0))); // n [Radians per Second] 
            double SemiLatusRectum = SMA * (1.0 - (Math.Pow(e, 2.0))); // p [Meters] 

            //print("MeanMotion: " + MeanMotion);
            //print("SemiLatusRectum: " + SemiLatusRectum);

            double EccentricAnomaly = vessel.orbitDriver.orbit.eccentricAnomaly;
            double InitialMeanAnomaly = vessel.orbitDriver.orbit.GetMeanAnomaly(EccentricAnomaly, HighLogic.CurrentGame.UniversalTime); //MeanMotion * (HighLogic.CurrentGame.UniversalTime - vessel.orbitDriver.orbit.epoch); // EPH = Epoch Time
            double MeanAnomalyAtTime = vessel.orbitDriver.orbit.GetMeanAnomaly(EccentricAnomaly, HighLogic.CurrentGame.UniversalTime + TimeInterval);//MeanMotion * ((HighLogic.CurrentGame.UniversalTime + 1.0) - HighLogic.CurrentGame.UniversalTime); // 1.0 = Time Interval of 1 second // Initial Mean Anomaly + 

            double ExactInitialEccentricAnomaly = 0; // E0 [Degrees]  
            ExactInitialEccentricAnomaly = vessel.orbitDriver.orbit.GetEccentricAnomaly(HighLogic.CurrentGame.UniversalTime); ;

            /// Find the Rate Of Change of True Anomaly /// 

            double InitialTrueAnomaly = vessel.orbitDriver.orbit.TrueAnomalyAtT(HighLogic.CurrentGame.UniversalTime); //Math.Acos((Math.Cos(ExactInitialEccentricAnomaly - e)) / (1.0 - e * Math.Cos(ExactInitialEccentricAnomaly)));
            double FinalTrueAnomaly = vessel.orbitDriver.orbit.TrueAnomalyAtT(HighLogic.CurrentGame.UniversalTime + TimeInterval); // Math.Acos((Math.Cos(ExactFinalEccentricAnomaly - e)) / (1.0 - e * Math.Cos(ExactFinalEccentricAnomaly)));
            double RateOfChangeOfTrueAnomaly = Math.Abs(FinalTrueAnomaly - InitialTrueAnomaly); // v-  [Degrees per Second]  // Possibly remove absolution. 

            //print("InitialTrueAnom: " + InitialTrueAnomaly);
            //print("FinalTrueAnom: " + FinalTrueAnomaly);

            if (double.IsNaN(RateOfChangeOfTrueAnomaly))
            {
                RateOfChangeOfTrueAnomaly = 0.0;
            }

            //print("RateOfChangeOfTrueAnomaly: " + RateOfChangeOfTrueAnomaly);

            ///// Generic Equations Finished ///// 

            #endregion

            double RateOfChangeOfSemiMajorAxisDeltaTheta = ((2.0 * Math.Pow(VesselAltitude, 2.0)) / (Math.Pow(MeanMotion, 2.0) * SMA * SemiLatusRectum)) - (SubvectorR * e *
                Math.Sin(InitialTrueAnomaly) + (SemiLatusRectum / VesselAltitude) * SubvectorQ); // da/dTheta [Meters per Degree] 

            print("ChangeInSMAbyTheta: " + RateOfChangeOfSemiMajorAxisDeltaTheta);

            double RateOfChangeOfSemiMajorAxisDeltaTime = RateOfChangeOfSemiMajorAxisDeltaTheta * RateOfChangeOfTrueAnomaly; // Change In SMA [Meters per time interval]
            double NewSemiMajorAxis = SMA + RateOfChangeOfSemiMajorAxisDeltaTime; // [Meters per time interval]
            return RateOfChangeOfSemiMajorAxisDeltaTime * 0.1; // [Change in Meters per time interval] 

        }

        public static double GetCalculatedINCChange(Vessel vessel, double LAN, double MNA, double LPE, double e, double Inc, double SMA, double EPH)
        {
            #region GenericEquations

            double EquivalentAltitude = 0;
            double AltitudeAp = (SMA * (1 + e) - vessel.orbitDriver.orbit.referenceBody.Radius);
            double AltitudePe = (SMA * (1 - e) - vessel.orbitDriver.orbit.referenceBody.Radius);
            EquivalentAltitude = (AltitudePe) + 900.0 * Math.Pow(e, (double)0.6);

            double GalAtVesselDistance = 0;
            double CentreLat = 0;
            double CentreLong = 0;
            double CentreGal = 0;
            double GalToGFactor = 0.00101972;

            //Required for Integration 

            if (vessel.orbitDriver.orbit.referenceBody.GetName() == "Earth" || vessel.orbitDriver.orbit.referenceBody.GetName() == "Kerbin") // At the moment only these modeled
            {
                GalAtVesselDistance = MasConData.GalAtPosition(vessel);
                CentreLat = double.Parse(MasConData.LocalMasCon(vessel).GetValue("centreLat"));
                CentreGal = double.Parse(MasConData.LocalMasCon(vessel).GetValue("centreGal"));
                CentreLong = double.Parse(MasConData.LocalMasCon(vessel).GetValue("centreLong"));
            }
            else
            {
                GalAtVesselDistance = UnityEngine.Random.Range((float)(vessel.orbitDriver.orbit.referenceBody.GeeASL * GalToGFactor * 0.9995), (float)(vessel.orbitDriver.orbit.referenceBody.GeeASL * GalToGFactor * 1.0005));
                CentreLat = vessel.latitude;
                CentreLong = vessel.longitude;
                CentreGal = GalAtVesselDistance;
            }

            double GAtVesselDistance = GalAtVesselDistance * GalToGFactor;

            double RAANMascon = CentreLong; // [Degrees]
            double DECMascon = CentreLat; // [Degrees]
            double RAANVessel = vessel.longitude; // [Degrees]
            double DECVessel = vessel.latitude; // [Degrees]

            double TrueAnomaly = vessel.orbitDriver.orbit.trueAnomaly; //Math.Acos((((1.0 - (e * e)) / (SMA / EquivalentAltitude)) - 1.0) / e); // v [Degrees]
            double ArgumentOfLatitude = MasConData.ToRadians(LPE) + TrueAnomaly; // u [Degrees]

            ///
            ArgumentOfLatitude = MasConData.ToDegrees(ArgumentOfLatitude); // u [Radians]
            ///

            if (double.IsNaN(TrueAnomaly))
            {
                TrueAnomaly = 0.0;
            }

            //print("TrueAnomaly: " + TrueAnomaly);

            double SubvectorA = Math.Cos(DECMascon) * Math.Cos(RAANMascon - RAANVessel); // A [Vector]
            double SubvectorB = Math.Sin(Inc) * Math.Sin(DECMascon) + Math.Cos(Inc) * Math.Cos(DECMascon) * Math.Sin(RAANMascon - RAANVessel); // B [Vector]
            double SubvectorC = Math.Cos(Inc) * Math.Sin(DECMascon) - Math.Sin(Inc) * Math.Cos(DECMascon) * Math.Sin(RAANMascon - RAANVessel); // C [Vector]

            //print("SubvectA: " + SubvectorA);
            //print("SubvectB: " + SubvectorB);
            //print("SubvectC: " + SubvectorC);

            double GravitationalConstant = 6.67408 * Math.Pow(10.0, -11.0); // G [Newton Meter Squared per Square Kilograms] 
            double GravitationalParameter = vessel.orbitDriver.orbit.referenceBody.gravParameter; // Mu [Newton Meter Squared Per Kilogram] 
            double BodyMass = vessel.orbitDriver.orbit.referenceBody.Mass; // M [Kilograms]
            double BodyGASL = vessel.orbitDriver.orbit.referenceBody.GeeASL; // Fg [Gee ASL]
            double BodyRadius = vessel.orbitDriver.orbit.referenceBody.Radius; // R [Meters] 
            double VesselAltitude = vessel.orbitDriver.orbit.semiMajorAxis - BodyRadius; // r [Meters]
            double MasConMass = (((BodyRadius * BodyRadius)) * vessel.orbitDriver.orbit.referenceBody.GeeASL * 9.81) / (GravitationalConstant * BodyMass); // m [Kilograms] (CentreGal * GalToGFactor)

            MasConMass = Math.Abs((1.0 - MasConMass) * BodyMass);

            //print("MasConMass: " + MasConMass);

            double SubvectorR = ((GravitationalConstant * MasConMass) / (Math.Pow(VesselAltitude, 3.0))) * (((3.0 * Math.Pow(BodyRadius, 2.0)) / (2.0 * VesselAltitude))) *
                (1.0 - (3.0f / 2.0f) * (Math.Pow(SubvectorA, 2.0) + Math.Pow(SubvectorB, 2.0)) - 3.0 * SubvectorA * SubvectorB * Math.Sin(2.0 * ArgumentOfLatitude) - (3.0f / 2.0f) *
                (Math.Pow(SubvectorA, 2.0) - Math.Pow(SubvectorB, 2.0)) * Math.Cos(2.0 * ArgumentOfLatitude)); // R- [Vector]

            double SubvectorQ = ((GravitationalConstant * MasConMass) / (Math.Pow(VesselAltitude, 3.0))) * ((3.0 * Math.Pow(BodyRadius, 2.0)) / (VesselAltitude)) *
                (SubvectorA * SubvectorB * Math.Cos(2.0 * ArgumentOfLatitude) - (1.0 / 2.0) * (Math.Pow(SubvectorA, 2.0) - Math.Pow(SubvectorB, 2.0)) * Math.Sin(2.0 * ArgumentOfLatitude)); // Q- [Vector]

            double SubvectorS = ((GravitationalConstant * MasConMass) / (Math.Pow(VesselAltitude, 3.0))) * (((3.0 * Math.Pow(BodyRadius, 2.0)) / (VesselAltitude))) *
                SubvectorC * (SubvectorA * Math.Cos(ArgumentOfLatitude) + SubvectorB * Math.Sin(ArgumentOfLatitude)); // S- [Vector] 

            //print("SubVectR: " + SubvectorR);
            //print("SubVectQ: " + SubvectorQ);
            //print("SubVectS: " + SubvectorS);

            double MeanMotion = (360.0) / vessel.orbitDriver.orbit.period; //Math.Sqrt(GravitationalParameter / (Math.Pow(SMA, 3.0))); // n [Radians per Second] 
            double SemiLatusRectum = SMA * (1.0 - (Math.Pow(e, 2.0))); // p [Meters] 

            //print("MeanMotion: " + MeanMotion);
            //print("SemiLatusRectum: " + SemiLatusRectum);

            double EccentricAnomaly = vessel.orbitDriver.orbit.eccentricAnomaly;
            double InitialMeanAnomaly = vessel.orbitDriver.orbit.GetMeanAnomaly(EccentricAnomaly, HighLogic.CurrentGame.UniversalTime); //MeanMotion * (HighLogic.CurrentGame.UniversalTime - vessel.orbitDriver.orbit.epoch); // EPH = Epoch Time
            double MeanAnomalyAtTime = vessel.orbitDriver.orbit.GetMeanAnomaly(EccentricAnomaly, HighLogic.CurrentGame.UniversalTime + TimeInterval);//MeanMotion * ((HighLogic.CurrentGame.UniversalTime + 1.0) - HighLogic.CurrentGame.UniversalTime); // 1.0 = Time Interval of 1 second // Initial Mean Anomaly + 

            double ExactInitialEccentricAnomaly = 0; // E0 [Degrees]  
            ExactInitialEccentricAnomaly = vessel.orbitDriver.orbit.GetEccentricAnomaly(HighLogic.CurrentGame.UniversalTime); ;

            /// Find the Rate Of Change of True Anomaly /// 

            double InitialTrueAnomaly = vessel.orbitDriver.orbit.TrueAnomalyAtT(HighLogic.CurrentGame.UniversalTime); //Math.Acos((Math.Cos(ExactInitialEccentricAnomaly - e)) / (1.0 - e * Math.Cos(ExactInitialEccentricAnomaly)));
            double FinalTrueAnomaly = vessel.orbitDriver.orbit.TrueAnomalyAtT(HighLogic.CurrentGame.UniversalTime + TimeInterval); // Math.Acos((Math.Cos(ExactFinalEccentricAnomaly - e)) / (1.0 - e * Math.Cos(ExactFinalEccentricAnomaly)));
            double RateOfChangeOfTrueAnomaly = Math.Abs(FinalTrueAnomaly - InitialTrueAnomaly); // v-  [Degrees per Second]  // Possibly remove absolution. 

            //print("InitialTrueAnom: " + InitialTrueAnomaly);
            //print("FinalTrueAnom: " + FinalTrueAnomaly);

            if (double.IsNaN(RateOfChangeOfTrueAnomaly))
            {
                RateOfChangeOfTrueAnomaly = 0.0;
            }

            //print("RateOfChangeOfTrueAnomaly: " + RateOfChangeOfTrueAnomaly);

            ///// Generic Equations Finished ///// 

            #endregion


            double RateOfChangeOfInclinationDeltaTheta = (SubvectorS * Math.Pow(VesselAltitude, 3.0) * Math.Cos(ArgumentOfLatitude)) / (Math.Pow(MeanMotion, 2.0) * Math.Pow(SMA, 3.0) *
                SemiLatusRectum); // di/dTheta [Degrees per Degree] 

            double RateOfChangeOfInclinationDeltaTime = RateOfChangeOfInclinationDeltaTheta * RateOfChangeOfTrueAnomaly; // Change in Inclination [Meters per time interval] 
            double NewInclination = Inc + RateOfChangeOfInclinationDeltaTime; // [Degrees per time interval]
            return RateOfChangeOfInclinationDeltaTime; // [Change in Degrees per time interval] 
        }

        public static double GetCalculatedECCChange(Vessel vessel, double LAN, double MNA, double LPE, double e, double Inc, double SMA, double EPH)
        {
            #region GenericEquations

            double EquivalentAltitude = 0;
            double AltitudeAp = (SMA * (1 + e) - vessel.orbitDriver.orbit.referenceBody.Radius);
            double AltitudePe = (SMA * (1 - e) - vessel.orbitDriver.orbit.referenceBody.Radius);
            EquivalentAltitude = (AltitudePe) + 900.0 * Math.Pow(e, (double)0.6);

            double GalAtVesselDistance = 0;
            double CentreLat = 0;
            double CentreLong = 0;
            double CentreGal = 0;
            double GalToGFactor = 0.00101972;

            //Required for Integration 

            if (vessel.orbitDriver.orbit.referenceBody.GetName() == "Earth" || vessel.orbitDriver.orbit.referenceBody.GetName() == "Kerbin") // At the moment only these modeled
            {
                GalAtVesselDistance = MasConData.GalAtPosition(vessel);
                CentreLat = double.Parse(MasConData.LocalMasCon(vessel).GetValue("centreLat"));
                CentreGal = double.Parse(MasConData.LocalMasCon(vessel).GetValue("centreGal"));
                CentreLong = double.Parse(MasConData.LocalMasCon(vessel).GetValue("centreLong"));
            }
            else
            {
                GalAtVesselDistance = UnityEngine.Random.Range((float)(vessel.orbitDriver.orbit.referenceBody.GeeASL * GalToGFactor * 0.9995), (float)(vessel.orbitDriver.orbit.referenceBody.GeeASL * GalToGFactor * 1.0005));
                CentreLat = vessel.latitude;
                CentreLong = vessel.longitude;
                CentreGal = GalAtVesselDistance;
            }

            double GAtVesselDistance = GalAtVesselDistance * GalToGFactor;

            double RAANMascon = CentreLong; // [Degrees]
            double DECMascon = CentreLat; // [Degrees]
            double RAANVessel = vessel.longitude; // [Degrees]
            double DECVessel = vessel.latitude; // [Degrees]

            double TrueAnomaly = vessel.orbitDriver.orbit.trueAnomaly; //Math.Acos((((1.0 - (e * e)) / (SMA / EquivalentAltitude)) - 1.0) / e); // v [Degrees]
            double ArgumentOfLatitude = MasConData.ToRadians(LPE) + TrueAnomaly; // u [Degrees]

            ///
            ArgumentOfLatitude = MasConData.ToDegrees(ArgumentOfLatitude); // u [Radians]
            ///

            if (double.IsNaN(TrueAnomaly))
            {
                TrueAnomaly = 0.0;
            }

            //print("TrueAnomaly: " + TrueAnomaly);

            double SubvectorA = Math.Cos(DECMascon) * Math.Cos(RAANMascon - RAANVessel); // A [Vector]
            double SubvectorB = Math.Sin(Inc) * Math.Sin(DECMascon) + Math.Cos(Inc) * Math.Cos(DECMascon) * Math.Sin(RAANMascon - RAANVessel); // B [Vector]
            double SubvectorC = Math.Cos(Inc) * Math.Sin(DECMascon) - Math.Sin(Inc) * Math.Cos(DECMascon) * Math.Sin(RAANMascon - RAANVessel); // C [Vector]

            //print("SubvectA: " + SubvectorA);
            //print("SubvectB: " + SubvectorB);
            //print("SubvectC: " + SubvectorC);

            double GravitationalConstant = 6.67408 * Math.Pow(10.0, -11.0); // G [Newton Meter Squared per Square Kilograms] 
            double GravitationalParameter = vessel.orbitDriver.orbit.referenceBody.gravParameter; // Mu [Newton Meter Squared Per Kilogram] 
            double BodyMass = vessel.orbitDriver.orbit.referenceBody.Mass; // M [Kilograms]
            double BodyGASL = vessel.orbitDriver.orbit.referenceBody.GeeASL; // Fg [Gee ASL]
            double BodyRadius = vessel.orbitDriver.orbit.referenceBody.Radius; // R [Meters] 
            double VesselAltitude = vessel.orbitDriver.orbit.semiMajorAxis - BodyRadius; // r [Meters]
            double MasConMass = (((BodyRadius * BodyRadius)) * vessel.orbitDriver.orbit.referenceBody.GeeASL * 9.81) / (GravitationalConstant * BodyMass); // m [Kilograms] (CentreGal * GalToGFactor)

            MasConMass = Math.Abs((1.0 - MasConMass) * BodyMass);

            //print("MasConMass: " + MasConMass);

            double SubvectorR = ((GravitationalConstant * MasConMass) / (Math.Pow(VesselAltitude, 3.0))) * (((3.0 * Math.Pow(BodyRadius, 2.0)) / (2.0 * VesselAltitude))) *
                (1.0 - (3.0f / 2.0f) * (Math.Pow(SubvectorA, 2.0) + Math.Pow(SubvectorB, 2.0)) - 3.0 * SubvectorA * SubvectorB * Math.Sin(2.0 * ArgumentOfLatitude) - (3.0f / 2.0f) *
                (Math.Pow(SubvectorA, 2.0) - Math.Pow(SubvectorB, 2.0)) * Math.Cos(2.0 * ArgumentOfLatitude)); // R- [Vector]

            double SubvectorQ = ((GravitationalConstant * MasConMass) / (Math.Pow(VesselAltitude, 3.0))) * ((3.0 * Math.Pow(BodyRadius, 2.0)) / (VesselAltitude)) *
                (SubvectorA * SubvectorB * Math.Cos(2.0 * ArgumentOfLatitude) - (1.0 / 2.0) * (Math.Pow(SubvectorA, 2.0) - Math.Pow(SubvectorB, 2.0)) * Math.Sin(2.0 * ArgumentOfLatitude)); // Q- [Vector]

            double SubvectorS = ((GravitationalConstant * MasConMass) / (Math.Pow(VesselAltitude, 3.0))) * (((3.0 * Math.Pow(BodyRadius, 2.0)) / (VesselAltitude))) *
                SubvectorC * (SubvectorA * Math.Cos(ArgumentOfLatitude) + SubvectorB * Math.Sin(ArgumentOfLatitude)); // S- [Vector] 

            //print("SubVectR: " + SubvectorR);
            //print("SubVectQ: " + SubvectorQ);
            //print("SubVectS: " + SubvectorS);

            double MeanMotion = (360.0) / vessel.orbitDriver.orbit.period; //Math.Sqrt(GravitationalParameter / (Math.Pow(SMA, 3.0))); // n [Radians per Second] 
            double SemiLatusRectum = SMA * (1.0 - (Math.Pow(e, 2.0))); // p [Meters] 

            //print("MeanMotion: " + MeanMotion);
            //print("SemiLatusRectum: " + SemiLatusRectum);

            double EccentricAnomaly = vessel.orbitDriver.orbit.eccentricAnomaly;
            double InitialMeanAnomaly = vessel.orbitDriver.orbit.GetMeanAnomaly(EccentricAnomaly, HighLogic.CurrentGame.UniversalTime); //MeanMotion * (HighLogic.CurrentGame.UniversalTime - vessel.orbitDriver.orbit.epoch); // EPH = Epoch Time
            double MeanAnomalyAtTime = vessel.orbitDriver.orbit.GetMeanAnomaly(EccentricAnomaly, HighLogic.CurrentGame.UniversalTime + TimeInterval);//MeanMotion * ((HighLogic.CurrentGame.UniversalTime + 1.0) - HighLogic.CurrentGame.UniversalTime); // 1.0 = Time Interval of 1 second // Initial Mean Anomaly + 

            double ExactInitialEccentricAnomaly = 0; // E0 [Degrees]  
            ExactInitialEccentricAnomaly = vessel.orbitDriver.orbit.GetEccentricAnomaly(HighLogic.CurrentGame.UniversalTime); ;

            /// Find the Rate Of Change of True Anomaly /// 

            double InitialTrueAnomaly = vessel.orbitDriver.orbit.TrueAnomalyAtT(HighLogic.CurrentGame.UniversalTime); //Math.Acos((Math.Cos(ExactInitialEccentricAnomaly - e)) / (1.0 - e * Math.Cos(ExactInitialEccentricAnomaly)));
            double FinalTrueAnomaly = vessel.orbitDriver.orbit.TrueAnomalyAtT(HighLogic.CurrentGame.UniversalTime + TimeInterval); // Math.Acos((Math.Cos(ExactFinalEccentricAnomaly - e)) / (1.0 - e * Math.Cos(ExactFinalEccentricAnomaly)));
            double RateOfChangeOfTrueAnomaly = Math.Abs(FinalTrueAnomaly - InitialTrueAnomaly); // v-  [Degrees per Second]  // Possibly remove absolution. 

            //print("InitialTrueAnom: " + InitialTrueAnomaly);
            //print("FinalTrueAnom: " + FinalTrueAnomaly);

            if (double.IsNaN(RateOfChangeOfTrueAnomaly))
            {
                RateOfChangeOfTrueAnomaly = 0.0;
            }

            //print("RateOfChangeOfTrueAnomaly: " + RateOfChangeOfTrueAnomaly);

            ///// Generic Equations Finished ///// 

            #endregion


            double RateOfChangeOfEccentricityDeltaTheta = (Math.Pow(VesselAltitude, 2.0) / (Math.Pow(MeanMotion, 2.0) * (Math.Pow(SMA, 3.0)))) * (SubvectorR * Math.Sin(InitialTrueAnomaly) +
                SubvectorQ * (Math.Cos(InitialTrueAnomaly) + Math.Cos(ExactInitialEccentricAnomaly))); // [e-unit per Degree] 

            double RateOfChangeOfEccentricityDeltaTime = RateOfChangeOfEccentricityDeltaTheta * RateOfChangeOfTrueAnomaly; // Change in Eccentricity 
            double NewEccentricity = e + RateOfChangeOfEccentricityDeltaTime; // [e-unit per time interval] 
            return RateOfChangeOfEccentricityDeltaTime; // [Change in e-unit per time interval]
        }

        public static double GetCalculatedLPEChange(Vessel vessel, double LAN, double MNA, double LPE, double e, double Inc, double SMA, double EPH)
        {
            #region GenericEquations

            double EquivalentAltitude = 0;
            double AltitudeAp = (SMA * (1 + e) - vessel.orbitDriver.orbit.referenceBody.Radius);
            double AltitudePe = (SMA * (1 - e) - vessel.orbitDriver.orbit.referenceBody.Radius);
            EquivalentAltitude = (AltitudePe) + 900.0 * Math.Pow(e, (double)0.6);

            double GalAtVesselDistance = 0;
            double CentreLat = 0;
            double CentreLong = 0;
            double CentreGal = 0;
            double GalToGFactor = 0.00101972;

            //Required for Integration 

            if (vessel.orbitDriver.orbit.referenceBody.GetName() == "Earth" || vessel.orbitDriver.orbit.referenceBody.GetName() == "Kerbin") // At the moment only these modeled
            {
                GalAtVesselDistance = MasConData.GalAtPosition(vessel);
                CentreLat = double.Parse(MasConData.LocalMasCon(vessel).GetValue("centreLat"));
                CentreGal = double.Parse(MasConData.LocalMasCon(vessel).GetValue("centreGal"));
                CentreLong = double.Parse(MasConData.LocalMasCon(vessel).GetValue("centreLong"));
            }
            else
            {
                GalAtVesselDistance = UnityEngine.Random.Range((float)(vessel.orbitDriver.orbit.referenceBody.GeeASL * GalToGFactor * 0.9995), (float)(vessel.orbitDriver.orbit.referenceBody.GeeASL * GalToGFactor * 1.0005));
                CentreLat = vessel.latitude;
                CentreLong = vessel.longitude;
                CentreGal = GalAtVesselDistance;
            }

            double GAtVesselDistance = GalAtVesselDistance * GalToGFactor;

            double RAANMascon = CentreLong; // [Degrees]
            double DECMascon = CentreLat; // [Degrees]
            double RAANVessel = vessel.longitude; // [Degrees]
            double DECVessel = vessel.latitude; // [Degrees]

            double TrueAnomaly = vessel.orbitDriver.orbit.trueAnomaly; //Math.Acos((((1.0 - (e * e)) / (SMA / EquivalentAltitude)) - 1.0) / e); // v [Degrees]
            double ArgumentOfLatitude = MasConData.ToRadians(LPE) + TrueAnomaly; // u [Degrees]

            ///
            ArgumentOfLatitude = MasConData.ToDegrees(ArgumentOfLatitude); // u [Radians]
            ///

            if (double.IsNaN(TrueAnomaly))
            {
                TrueAnomaly = 0.0;
            }

            //print("TrueAnomaly: " + TrueAnomaly);

            double SubvectorA = Math.Cos(DECMascon) * Math.Cos(RAANMascon - RAANVessel); // A [Vector]
            double SubvectorB = Math.Sin(Inc) * Math.Sin(DECMascon) + Math.Cos(Inc) * Math.Cos(DECMascon) * Math.Sin(RAANMascon - RAANVessel); // B [Vector]
            double SubvectorC = Math.Cos(Inc) * Math.Sin(DECMascon) - Math.Sin(Inc) * Math.Cos(DECMascon) * Math.Sin(RAANMascon - RAANVessel); // C [Vector]

            //print("SubvectA: " + SubvectorA);
            //print("SubvectB: " + SubvectorB);
            //print("SubvectC: " + SubvectorC);

            double GravitationalConstant = 6.67408 * Math.Pow(10.0, -11.0); // G [Newton Meter Squared per Square Kilograms] 
            double GravitationalParameter = vessel.orbitDriver.orbit.referenceBody.gravParameter; // Mu [Newton Meter Squared Per Kilogram] 
            double BodyMass = vessel.orbitDriver.orbit.referenceBody.Mass; // M [Kilograms]
            double BodyGASL = vessel.orbitDriver.orbit.referenceBody.GeeASL; // Fg [Gee ASL]
            double BodyRadius = vessel.orbitDriver.orbit.referenceBody.Radius; // R [Meters] 
            double VesselAltitude = vessel.orbitDriver.orbit.semiMajorAxis - BodyRadius; // r [Meters]
            double MasConMass = (((BodyRadius * BodyRadius)) * vessel.orbitDriver.orbit.referenceBody.GeeASL * 9.81) / (GravitationalConstant * BodyMass); // m [Kilograms] (CentreGal * GalToGFactor)

            MasConMass = Math.Abs((1.0 - MasConMass) * BodyMass);

            //print("MasConMass: " + MasConMass);

            double SubvectorR = ((GravitationalConstant * MasConMass) / (Math.Pow(VesselAltitude, 3.0))) * (((3.0 * Math.Pow(BodyRadius, 2.0)) / (2.0 * VesselAltitude))) *
                (1.0 - (3.0f / 2.0f) * (Math.Pow(SubvectorA, 2.0) + Math.Pow(SubvectorB, 2.0)) - 3.0 * SubvectorA * SubvectorB * Math.Sin(2.0 * ArgumentOfLatitude) - (3.0f / 2.0f) *
                (Math.Pow(SubvectorA, 2.0) - Math.Pow(SubvectorB, 2.0)) * Math.Cos(2.0 * ArgumentOfLatitude)); // R- [Vector]

            double SubvectorQ = ((GravitationalConstant * MasConMass) / (Math.Pow(VesselAltitude, 3.0))) * ((3.0 * Math.Pow(BodyRadius, 2.0)) / (VesselAltitude)) *
                (SubvectorA * SubvectorB * Math.Cos(2.0 * ArgumentOfLatitude) - (1.0 / 2.0) * (Math.Pow(SubvectorA, 2.0) - Math.Pow(SubvectorB, 2.0)) * Math.Sin(2.0 * ArgumentOfLatitude)); // Q- [Vector]

            double SubvectorS = ((GravitationalConstant * MasConMass) / (Math.Pow(VesselAltitude, 3.0))) * (((3.0 * Math.Pow(BodyRadius, 2.0)) / (VesselAltitude))) *
                SubvectorC * (SubvectorA * Math.Cos(ArgumentOfLatitude) + SubvectorB * Math.Sin(ArgumentOfLatitude)); // S- [Vector] 

            //print("SubVectR: " + SubvectorR);
            //print("SubVectQ: " + SubvectorQ);
            //print("SubVectS: " + SubvectorS);

            double MeanMotion = (360.0) / vessel.orbitDriver.orbit.period; //Math.Sqrt(GravitationalParameter / (Math.Pow(SMA, 3.0))); // n [Radians per Second] 
            double SemiLatusRectum = SMA * (1.0 - (Math.Pow(e, 2.0))); // p [Meters] 

            //print("MeanMotion: " + MeanMotion);
            //print("SemiLatusRectum: " + SemiLatusRectum);

            double EccentricAnomaly = vessel.orbitDriver.orbit.eccentricAnomaly;
            double InitialMeanAnomaly = vessel.orbitDriver.orbit.GetMeanAnomaly(EccentricAnomaly, HighLogic.CurrentGame.UniversalTime); //MeanMotion * (HighLogic.CurrentGame.UniversalTime - vessel.orbitDriver.orbit.epoch); // EPH = Epoch Time
            double MeanAnomalyAtTime = vessel.orbitDriver.orbit.GetMeanAnomaly(EccentricAnomaly, HighLogic.CurrentGame.UniversalTime + TimeInterval);//MeanMotion * ((HighLogic.CurrentGame.UniversalTime + 1.0) - HighLogic.CurrentGame.UniversalTime); // 1.0 = Time Interval of 1 second // Initial Mean Anomaly + 

            double ExactInitialEccentricAnomaly = 0; // E0 [Degrees]  
            ExactInitialEccentricAnomaly = vessel.orbitDriver.orbit.GetEccentricAnomaly(HighLogic.CurrentGame.UniversalTime); ;

            /// Find the Rate Of Change of True Anomaly /// 

            double InitialTrueAnomaly = vessel.orbitDriver.orbit.TrueAnomalyAtT(HighLogic.CurrentGame.UniversalTime); //Math.Acos((Math.Cos(ExactInitialEccentricAnomaly - e)) / (1.0 - e * Math.Cos(ExactInitialEccentricAnomaly)));
            double FinalTrueAnomaly = vessel.orbitDriver.orbit.TrueAnomalyAtT(HighLogic.CurrentGame.UniversalTime + TimeInterval); // Math.Acos((Math.Cos(ExactFinalEccentricAnomaly - e)) / (1.0 - e * Math.Cos(ExactFinalEccentricAnomaly)));
            double RateOfChangeOfTrueAnomaly = Math.Abs(FinalTrueAnomaly - InitialTrueAnomaly); // v-  [Degrees per Second]  // Possibly remove absolution. 

            //print("InitialTrueAnom: " + InitialTrueAnomaly);
            //print("FinalTrueAnom: " + FinalTrueAnomaly);

            if (double.IsNaN(RateOfChangeOfTrueAnomaly))
            {
                RateOfChangeOfTrueAnomaly = 0.0;
            }

            //print("RateOfChangeOfTrueAnomaly: " + RateOfChangeOfTrueAnomaly);

            ///// Generic Equations Finished ///// 

            #endregion


            double RateOfChangeOfLPEDeltaTheta = (Math.Pow(VesselAltitude, 2.0) / (Math.Pow(MeanMotion, 2.0) * Math.Pow(SMA, 3.0) * e)) * (-SubvectorR * Math.Cos(InitialTrueAnomaly) +
                (1.0 + (VesselAltitude / SemiLatusRectum)) * SubvectorQ * Math.Sin(InitialTrueAnomaly)) - GetCalculatedLANChange(vessel, LAN, MNA, LPE, e, Inc, SMA, EPH) * Math.Cos(Inc);

            double RateOfChangeOfLPEDeltaTime = RateOfChangeOfLPEDeltaTheta * RateOfChangeOfTrueAnomaly;
            double NewLPE = LPE + RateOfChangeOfLPEDeltaTime;
            return RateOfChangeOfLPEDeltaTime;
        }

        public static double GetCalculatedLANChange(Vessel vessel, double LAN, double MNA, double LPE, double e, double Inc, double SMA, double EPH)
        {
            #region GenericEquations

            double EquivalentAltitude = 0;
            double AltitudeAp = (SMA * (1 + e) - vessel.orbitDriver.orbit.referenceBody.Radius);
            double AltitudePe = (SMA * (1 - e) - vessel.orbitDriver.orbit.referenceBody.Radius);
            EquivalentAltitude = (AltitudePe) + 900.0 * Math.Pow(e, (double)0.6);

            double GalAtVesselDistance = 0;
            double CentreLat = 0;
            double CentreLong = 0;
            double CentreGal = 0;
            double GalToGFactor = 0.00101972;

            //Required for Integration 

            if (vessel.orbitDriver.orbit.referenceBody.GetName() == "Earth" || vessel.orbitDriver.orbit.referenceBody.GetName() == "Kerbin") // At the moment only these modeled
            {
                GalAtVesselDistance = MasConData.GalAtPosition(vessel);
                CentreLat = double.Parse(MasConData.LocalMasCon(vessel).GetValue("centreLat"));
                CentreGal = double.Parse(MasConData.LocalMasCon(vessel).GetValue("centreGal"));
                CentreLong = double.Parse(MasConData.LocalMasCon(vessel).GetValue("centreLong"));
            }
            else
            {
                GalAtVesselDistance = UnityEngine.Random.Range((float)(vessel.orbitDriver.orbit.referenceBody.GeeASL * GalToGFactor * 0.9995), (float)(vessel.orbitDriver.orbit.referenceBody.GeeASL * GalToGFactor * 1.0005));
                CentreLat = vessel.latitude;
                CentreLong = vessel.longitude;
                CentreGal = GalAtVesselDistance;
            }

            double GAtVesselDistance = GalAtVesselDistance * GalToGFactor;

            double RAANMascon = CentreLong; // [Degrees]
            double DECMascon = CentreLat; // [Degrees]
            double RAANVessel = vessel.longitude; // [Degrees]
            double DECVessel = vessel.latitude; // [Degrees]

            double TrueAnomaly = vessel.orbitDriver.orbit.trueAnomaly; //Math.Acos((((1.0 - (e * e)) / (SMA / EquivalentAltitude)) - 1.0) / e); // v [Degrees]
            double ArgumentOfLatitude = MasConData.ToRadians(LPE) + TrueAnomaly; // u [Degrees]

            ///
            ArgumentOfLatitude = MasConData.ToDegrees(ArgumentOfLatitude); // u [Radians]
            ///

            if (double.IsNaN(TrueAnomaly))
            {
                TrueAnomaly = 0.0;
            }

            //print("TrueAnomaly: " + TrueAnomaly);

            double SubvectorA = Math.Cos(DECMascon) * Math.Cos(RAANMascon - RAANVessel); // A [Vector]
            double SubvectorB = Math.Sin(Inc) * Math.Sin(DECMascon) + Math.Cos(Inc) * Math.Cos(DECMascon) * Math.Sin(RAANMascon - RAANVessel); // B [Vector]
            double SubvectorC = Math.Cos(Inc) * Math.Sin(DECMascon) - Math.Sin(Inc) * Math.Cos(DECMascon) * Math.Sin(RAANMascon - RAANVessel); // C [Vector]

            //print("SubvectA: " + SubvectorA);
            //print("SubvectB: " + SubvectorB);
            //print("SubvectC: " + SubvectorC);

            double GravitationalConstant = 6.67408 * Math.Pow(10.0, -11.0); // G [Newton Meter Squared per Square Kilograms] 
            double GravitationalParameter = vessel.orbitDriver.orbit.referenceBody.gravParameter; // Mu [Newton Meter Squared Per Kilogram] 
            double BodyMass = vessel.orbitDriver.orbit.referenceBody.Mass; // M [Kilograms]
            double BodyGASL = vessel.orbitDriver.orbit.referenceBody.GeeASL; // Fg [Gee ASL]
            double BodyRadius = vessel.orbitDriver.orbit.referenceBody.Radius; // R [Meters] 
            double VesselAltitude = vessel.orbitDriver.orbit.semiMajorAxis - BodyRadius; // r [Meters]
            double MasConMass = (((BodyRadius * BodyRadius)) * vessel.orbitDriver.orbit.referenceBody.GeeASL * 9.81) / (GravitationalConstant * BodyMass); // m [Kilograms] (CentreGal * GalToGFactor)

            MasConMass = Math.Abs((1.0 - MasConMass) * BodyMass);

            //print("MasConMass: " + MasConMass);

            double SubvectorR = ((GravitationalConstant * MasConMass) / (Math.Pow(VesselAltitude, 3.0))) * (((3.0 * Math.Pow(BodyRadius, 2.0)) / (2.0 * VesselAltitude))) *
                (1.0 - (3.0f / 2.0f) * (Math.Pow(SubvectorA, 2.0) + Math.Pow(SubvectorB, 2.0)) - 3.0 * SubvectorA * SubvectorB * Math.Sin(2.0 * ArgumentOfLatitude) - (3.0f / 2.0f) *
                (Math.Pow(SubvectorA, 2.0) - Math.Pow(SubvectorB, 2.0)) * Math.Cos(2.0 * ArgumentOfLatitude)); // R- [Vector]

            double SubvectorQ = ((GravitationalConstant * MasConMass) / (Math.Pow(VesselAltitude, 3.0))) * ((3.0 * Math.Pow(BodyRadius, 2.0)) / (VesselAltitude)) *
                (SubvectorA * SubvectorB * Math.Cos(2.0 * ArgumentOfLatitude) - (1.0 / 2.0) * (Math.Pow(SubvectorA, 2.0) - Math.Pow(SubvectorB, 2.0)) * Math.Sin(2.0 * ArgumentOfLatitude)); // Q- [Vector]

            double SubvectorS = ((GravitationalConstant * MasConMass) / (Math.Pow(VesselAltitude, 3.0))) * (((3.0 * Math.Pow(BodyRadius, 2.0)) / (VesselAltitude))) *
                SubvectorC * (SubvectorA * Math.Cos(ArgumentOfLatitude) + SubvectorB * Math.Sin(ArgumentOfLatitude)); // S- [Vector] 

            //print("SubVectR: " + SubvectorR);
            //print("SubVectQ: " + SubvectorQ);
            //print("SubVectS: " + SubvectorS);

            double MeanMotion = (360.0) / vessel.orbitDriver.orbit.period; //Math.Sqrt(GravitationalParameter / (Math.Pow(SMA, 3.0))); // n [Radians per Second] 
            double SemiLatusRectum = SMA * (1.0 - (Math.Pow(e, 2.0))); // p [Meters] 

            //print("MeanMotion: " + MeanMotion);
            //print("SemiLatusRectum: " + SemiLatusRectum);

            double EccentricAnomaly = vessel.orbitDriver.orbit.eccentricAnomaly;
            double InitialMeanAnomaly = vessel.orbitDriver.orbit.GetMeanAnomaly(EccentricAnomaly, HighLogic.CurrentGame.UniversalTime); //MeanMotion * (HighLogic.CurrentGame.UniversalTime - vessel.orbitDriver.orbit.epoch); // EPH = Epoch Time
            double MeanAnomalyAtTime = vessel.orbitDriver.orbit.GetMeanAnomaly(EccentricAnomaly, HighLogic.CurrentGame.UniversalTime + TimeInterval);//MeanMotion * ((HighLogic.CurrentGame.UniversalTime + 1.0) - HighLogic.CurrentGame.UniversalTime); // 1.0 = Time Interval of 1 second // Initial Mean Anomaly + 

            double ExactInitialEccentricAnomaly = 0; // E0 [Degrees]  
            ExactInitialEccentricAnomaly = vessel.orbitDriver.orbit.GetEccentricAnomaly(HighLogic.CurrentGame.UniversalTime); ;

            /// Find the Rate Of Change of True Anomaly /// 

            double InitialTrueAnomaly = vessel.orbitDriver.orbit.TrueAnomalyAtT(HighLogic.CurrentGame.UniversalTime); //Math.Acos((Math.Cos(ExactInitialEccentricAnomaly - e)) / (1.0 - e * Math.Cos(ExactInitialEccentricAnomaly)));
            double FinalTrueAnomaly = vessel.orbitDriver.orbit.TrueAnomalyAtT(HighLogic.CurrentGame.UniversalTime + TimeInterval); // Math.Acos((Math.Cos(ExactFinalEccentricAnomaly - e)) / (1.0 - e * Math.Cos(ExactFinalEccentricAnomaly)));
            double RateOfChangeOfTrueAnomaly = Math.Abs(FinalTrueAnomaly - InitialTrueAnomaly); // v-  [Degrees per Second]  // Possibly remove absolution. 

            //print("InitialTrueAnom: " + InitialTrueAnomaly);
            //print("FinalTrueAnom: " + FinalTrueAnomaly);

            if (double.IsNaN(RateOfChangeOfTrueAnomaly))
            {
                RateOfChangeOfTrueAnomaly = 0.0;
            }

            //print("RateOfChangeOfTrueAnomaly: " + RateOfChangeOfTrueAnomaly);

            ///// Generic Equations Finished ///// 

            #endregion


            double RateOfChangeOfLANDeltaTheta = (SubvectorS * Math.Pow(VesselAltitude, 3.0) * Math.Sin(ArgumentOfLatitude)) / (Math.Pow(MeanMotion, 2.0) * 
                Math.Pow(SMA, 3.0) * SemiLatusRectum * Math.Sin(Inc)); // [Degrees per Degree] 

            double RateOfChangeOfLANDeltaTime = RateOfChangeOfLANDeltaTheta * RateOfChangeOfTrueAnomaly;
            double NewLAN = LAN + RateOfChangeOfLANDeltaTime;
            return RateOfChangeOfLANDeltaTime;
        }

        public static double GetCalculatedMNAChange(Vessel vessel, double LAN, double MNA, double LPE, double e, double Inc, double SMA, double EPH)
        {
            #region GenericEquations

            double EquivalentAltitude = 0;
            double AltitudeAp = (SMA * (1 + e) - vessel.orbitDriver.orbit.referenceBody.Radius);
            double AltitudePe = (SMA * (1 - e) - vessel.orbitDriver.orbit.referenceBody.Radius);
            EquivalentAltitude = (AltitudePe) + 900.0 * Math.Pow(e, (double)0.6);

            double GalAtVesselDistance = 0;
            double CentreLat = 0;
            double CentreLong = 0;
            double CentreGal = 0;
            double GalToGFactor = 0.00101972;

            //Required for Integration 

            if (vessel.orbitDriver.orbit.referenceBody.GetName() == "Earth" || vessel.orbitDriver.orbit.referenceBody.GetName() == "Kerbin") // At the moment only these modeled
            {
                GalAtVesselDistance = MasConData.GalAtPosition(vessel);
                CentreLat = double.Parse(MasConData.LocalMasCon(vessel).GetValue("centreLat"));
                CentreGal = double.Parse(MasConData.LocalMasCon(vessel).GetValue("centreGal"));
                CentreLong = double.Parse(MasConData.LocalMasCon(vessel).GetValue("centreLong"));
            }
            else
            {
                GalAtVesselDistance = UnityEngine.Random.Range((float)(vessel.orbitDriver.orbit.referenceBody.GeeASL * GalToGFactor * 0.9995), (float)(vessel.orbitDriver.orbit.referenceBody.GeeASL * GalToGFactor * 1.0005));
                CentreLat = vessel.latitude;
                CentreLong = vessel.longitude;
                CentreGal = GalAtVesselDistance;
            }

            double GAtVesselDistance = GalAtVesselDistance * GalToGFactor;

            double RAANMascon = CentreLong; // [Degrees]
            double DECMascon = CentreLat; // [Degrees]
            double RAANVessel = vessel.longitude; // [Degrees]
            double DECVessel = vessel.latitude; // [Degrees]

            double TrueAnomaly = vessel.orbitDriver.orbit.trueAnomaly; //Math.Acos((((1.0 - (e * e)) / (SMA / EquivalentAltitude)) - 1.0) / e); // v [Degrees]
            double ArgumentOfLatitude = MasConData.ToRadians(LPE) + TrueAnomaly; // u [Degrees]

            ///
            ArgumentOfLatitude = MasConData.ToDegrees(ArgumentOfLatitude); // u [Radians]
            ///

            if (double.IsNaN(TrueAnomaly))
            {
                TrueAnomaly = 0.0;
            }

            //print("TrueAnomaly: " + TrueAnomaly);

            double SubvectorA = Math.Cos(DECMascon) * Math.Cos(RAANMascon - RAANVessel); // A [Vector]
            double SubvectorB = Math.Sin(Inc) * Math.Sin(DECMascon) + Math.Cos(Inc) * Math.Cos(DECMascon) * Math.Sin(RAANMascon - RAANVessel); // B [Vector]
            double SubvectorC = Math.Cos(Inc) * Math.Sin(DECMascon) - Math.Sin(Inc) * Math.Cos(DECMascon) * Math.Sin(RAANMascon - RAANVessel); // C [Vector]

            //print("SubvectA: " + SubvectorA);
            //print("SubvectB: " + SubvectorB);
            //print("SubvectC: " + SubvectorC);

            double GravitationalConstant = 6.67408 * Math.Pow(10.0, -11.0); // G [Newton Meter Squared per Square Kilograms] 
            double GravitationalParameter = vessel.orbitDriver.orbit.referenceBody.gravParameter; // Mu [Newton Meter Squared Per Kilogram] 
            double BodyMass = vessel.orbitDriver.orbit.referenceBody.Mass; // M [Kilograms]
            double BodyGASL = vessel.orbitDriver.orbit.referenceBody.GeeASL; // Fg [Gee ASL]
            double BodyRadius = vessel.orbitDriver.orbit.referenceBody.Radius; // R [Meters] 
            double VesselAltitude = vessel.orbitDriver.orbit.semiMajorAxis - BodyRadius; // r [Meters]
            double MasConMass = (((BodyRadius * BodyRadius)) * vessel.orbitDriver.orbit.referenceBody.GeeASL * 9.81) / (GravitationalConstant * BodyMass); // m [Kilograms] (CentreGal * GalToGFactor)

            MasConMass = Math.Abs((1.0 - MasConMass) * BodyMass);

            //print("MasConMass: " + MasConMass);

            double SubvectorR = ((GravitationalConstant * MasConMass) / (Math.Pow(VesselAltitude, 3.0))) * (((3.0 * Math.Pow(BodyRadius, 2.0)) / (2.0 * VesselAltitude))) *
                (1.0 - (3.0f / 2.0f) * (Math.Pow(SubvectorA, 2.0) + Math.Pow(SubvectorB, 2.0)) - 3.0 * SubvectorA * SubvectorB * Math.Sin(2.0 * ArgumentOfLatitude) - (3.0f / 2.0f) *
                (Math.Pow(SubvectorA, 2.0) - Math.Pow(SubvectorB, 2.0)) * Math.Cos(2.0 * ArgumentOfLatitude)); // R- [Vector]

            double SubvectorQ = ((GravitationalConstant * MasConMass) / (Math.Pow(VesselAltitude, 3.0))) * ((3.0 * Math.Pow(BodyRadius, 2.0)) / (VesselAltitude)) *
                (SubvectorA * SubvectorB * Math.Cos(2.0 * ArgumentOfLatitude) - (1.0 / 2.0) * (Math.Pow(SubvectorA, 2.0) - Math.Pow(SubvectorB, 2.0)) * Math.Sin(2.0 * ArgumentOfLatitude)); // Q- [Vector]

            double SubvectorS = ((GravitationalConstant * MasConMass) / (Math.Pow(VesselAltitude, 3.0))) * (((3.0 * Math.Pow(BodyRadius, 2.0)) / (VesselAltitude))) *
                SubvectorC * (SubvectorA * Math.Cos(ArgumentOfLatitude) + SubvectorB * Math.Sin(ArgumentOfLatitude)); // S- [Vector] 

            //print("SubVectR: " + SubvectorR);
            //print("SubVectQ: " + SubvectorQ);
            //print("SubVectS: " + SubvectorS);

            double MeanMotion = (360.0) / vessel.orbitDriver.orbit.period; //Math.Sqrt(GravitationalParameter / (Math.Pow(SMA, 3.0))); // n [Radians per Second] 
            double SemiLatusRectum = SMA * (1.0 - (Math.Pow(e, 2.0))); // p [Meters] 

            //print("MeanMotion: " + MeanMotion);
            //print("SemiLatusRectum: " + SemiLatusRectum);

            double EccentricAnomaly = vessel.orbitDriver.orbit.eccentricAnomaly;
            double InitialMeanAnomaly = vessel.orbitDriver.orbit.GetMeanAnomaly(EccentricAnomaly, HighLogic.CurrentGame.UniversalTime); //MeanMotion * (HighLogic.CurrentGame.UniversalTime - vessel.orbitDriver.orbit.epoch); // EPH = Epoch Time
            double MeanAnomalyAtTime = vessel.orbitDriver.orbit.GetMeanAnomaly(EccentricAnomaly, HighLogic.CurrentGame.UniversalTime + TimeInterval);//MeanMotion * ((HighLogic.CurrentGame.UniversalTime + 1.0) - HighLogic.CurrentGame.UniversalTime); // 1.0 = Time Interval of 1 second // Initial Mean Anomaly + 

            double ExactInitialEccentricAnomaly = 0; // E0 [Degrees]  
            ExactInitialEccentricAnomaly = vessel.orbitDriver.orbit.GetEccentricAnomaly(HighLogic.CurrentGame.UniversalTime); ;

            /// Find the Rate Of Change of True Anomaly /// 

            double InitialTrueAnomaly = vessel.orbitDriver.orbit.TrueAnomalyAtT(HighLogic.CurrentGame.UniversalTime); //Math.Acos((Math.Cos(ExactInitialEccentricAnomaly - e)) / (1.0 - e * Math.Cos(ExactInitialEccentricAnomaly)));
            double FinalTrueAnomaly = vessel.orbitDriver.orbit.TrueAnomalyAtT(HighLogic.CurrentGame.UniversalTime + TimeInterval); // Math.Acos((Math.Cos(ExactFinalEccentricAnomaly - e)) / (1.0 - e * Math.Cos(ExactFinalEccentricAnomaly)));
            double RateOfChangeOfTrueAnomaly = Math.Abs(FinalTrueAnomaly - InitialTrueAnomaly); // v-  [Degrees per Second]  // Possibly remove absolution. 

            //print("InitialTrueAnom: " + InitialTrueAnomaly);
            //print("FinalTrueAnom: " + FinalTrueAnomaly);

            if (double.IsNaN(RateOfChangeOfTrueAnomaly))
            {
                RateOfChangeOfTrueAnomaly = 0.0;
            }

            //print("RateOfChangeOfTrueAnomaly: " + RateOfChangeOfTrueAnomaly);

            ///// Generic Equations Finished ///// 

            #endregion


            double RateOfChangeOfRhoIntegralDeltaTheta = -((2.0 * Math.Pow(VesselAltitude, 3.0) * SubvectorR) / (Math.Pow(MeanMotion, 2.0) * Math.Pow(SMA, 4.0) * Math.Pow((1.0 - Math.Pow(e, 2.0)), 0.5))) -
                Math.Pow((1.0 - Math.Pow(e, 2.0)), 0.5) * (GetCalculatedLPEChange(vessel, LAN, MNA, LPE, e, Inc, SMA, EPH) + Math.Cos(Inc) * GetCalculatedLANChange(vessel, LAN, MNA, LPE, e, Inc, SMA, EPH)); // Possibly change Math.Cos(inc) to Math.Cos(inc * GetCalculatedLanChange)

            double RateOfChangeOfRhoIntegralDeltaTime = RateOfChangeOfRhoIntegralDeltaTheta * RateOfChangeOfTrueAnomaly;

            double RhoInitial = InitialMeanAnomaly - MeanMotion;
            double FinalRho = RhoInitial + RateOfChangeOfRhoIntegralDeltaTime;
            double NewMeanMotion = Math.Sqrt(GravitationalParameter / (Math.Pow(SMA + GetCalculatedSMAChange(vessel, LAN, MNA, LPE, e, Inc, SMA, EPH), 3.0)));
            double NewMeanAnomaly = FinalRho + NewMeanMotion;

            double NewMeanAnomalyAtEpoch = MNA; // Work this one out later
            return NewMeanAnomalyAtEpoch;
        }
    }
}
