/*
 * Whitecat Industries Orbital Decay for Kerbal Space Program. 
 * 
 * Written by Whitecat106 (Marcus Hehir).
 * 
 * Kerbal Space Program is Copyright (C) 2013 Squad. See http://kerbalspaceprogram.com/. This
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;

namespace WhitecatIndustries
{
    [KSPAddon(KSPAddon.Startup.EveryScene, false)]
    public class DecayManager : MonoBehaviour
    {
        private float UPTInterval = 1.0f;
        private float lastUpdate = 1.0f;

        public static double DecayValue;
        public static double MaxDecayValue;
        public static bool VesselDied = false;
        public static float EstimatedTimeUntilDeorbit;

        public static Dictionary<Vessel, bool> MessageDisplayed = new Dictionary<Vessel, bool>();
        public static double DecayRateModifier = 0.0;
        public static double VesselCount = 0;
        public static Vessel ActiveVessel = new Vessel();
        public static bool ActiveVesselOnOrbit = false;

        public static bool CatchupResourceMassAreaDataComplete = false;

        public void Start()
        {
            if (HighLogic.LoadedSceneIsGame && (HighLogic.LoadedScene != GameScenes.LOADING || HighLogic.LoadedScene != GameScenes.LOADINGBUFFER))
            {
                DecayRateModifier = Settings.ReadDecayDifficulty();

                GameEvents.onVesselWillDestroy.Add(ClearVesselOnDestroy);
                GameEvents.onVesselWasModified.Add(UpdateActiveVesselInformation); // Resource level change 1.3.0
                GameEvents.onStageSeparation.Add(UpdateActiveVesselInformationEventReport); // Resource level change 1.3.0
                GameEvents.onPartActionUIDismiss.Add(UpdateActiveVesselInformationPart); // Resource level change 1.3.0
                GameEvents.onPartActionUICreate.Add(UpdateActiveVesselInformationPart);

                DecayRateModifier = Settings.ReadDecayDifficulty();
                Vessel vessel = new Vessel();
                VesselCount = FlightGlobals.Vessels.Count;
                for (int i = 0; i < FlightGlobals.Vessels.Count; i++)
                {
                    vessel = FlightGlobals.Vessels.ElementAt(i);
                    if ((vessel.vesselType != VesselType.SpaceObject && vessel.vesselType != VesselType.Unknown))
                    {
                        if (vessel.situation == Vessel.Situations.ORBITING)
                        {
                            CatchUpOrbit(vessel);
                        }
                    }
                }
                if (HighLogic.LoadedScene == GameScenes.FLIGHT && FlightGlobals.ActiveVessel.situation == Vessel.Situations.ORBITING) // Active Fuel Manage
                {
                   ActiveVesselFuelManage(FlightGlobals.ActiveVessel);
                }
            }
        }

        public void UpdateActiveVesselInformationEventReport(EventReport report) // 1.3.0
        {
            VesselData.UpdateActiveVesselData(FlightGlobals.ActiveVessel);
        }

        public void UpdateActiveVesselInformationPart(Part part) // Until eventdata OnPartResourceFlowState works! // 1.3.0
        {
            if (part.vessel == FlightGlobals.ActiveVessel && TimeWarp.CurrentRate == 1)
            {
                VesselData.UpdateActiveVesselData(FlightGlobals.ActiveVessel);
            }

        }

        public void UpdateActiveVesselInformation(Vessel vessel)
        {
            if (vessel == FlightGlobals.ActiveVessel)
            {
                VesselData.UpdateActiveVesselData(vessel);
            }
        }

        public void ClearVesselOnDestroy(Vessel vessel)
        {
            VesselData.ClearVesselData(vessel);
        }

        public void FixedUpdate()
        {
            if (Time.timeSinceLevelLoad > 1.0 && HighLogic.LoadedSceneIsFlight && CatchupResourceMassAreaDataComplete == false && FlightGlobals.ActiveVessel.situation == Vessel.Situations.ORBITING)
            {
                if (!FlightGlobals.ActiveVessel.packed && FlightGlobals.ActiveVessel.isActiveAndEnabled) // Vessel is ready
                {
                    VesselData.UpdateActiveVesselData(FlightGlobals.ActiveVessel);
                    print("WhitecatIndustries - Orbital Decay - Updating Fuel Levels for: " + FlightGlobals.ActiveVessel.GetName());
                    CatchupResourceMassAreaDataComplete = true;
                }
            }

            if (Time.timeSinceLevelLoad > 0.6)
            {
            if (HighLogic.LoadedSceneIsGame && (HighLogic.LoadedScene != GameScenes.LOADING && HighLogic.LoadedScene != GameScenes.LOADINGBUFFER && HighLogic.LoadedScene != GameScenes.MAINMENU))
            {
                if ((Time.time - lastUpdate) > UPTInterval)
                {
                    lastUpdate = Time.time;
                    double ObjectCount = FlightGlobals.Vessels.Count;
                    if (ObjectCount != VesselCount)
                    {
                        VesselCount = ObjectCount;
                        for (int h = 0; h < FlightGlobals.Vessels.Count; h++) // Debris write (frequent due to staging)
                        {
                            Vessel Debris = FlightGlobals.Vessels.ElementAt(h);
                            if (Debris.FindDefaultVesselType() == VesselType.Debris)
                            {
                                VesselData.WriteVesselData(Debris);
                            }
                        }
                    }

                    if (HighLogic.LoadedScene == GameScenes.SPACECENTER || HighLogic.LoadedScene == GameScenes.TRACKSTATION || HighLogic.LoadedScene == GameScenes.FLIGHT)
                    {
                        for (int i = 0; i < FlightGlobals.Vessels.Count; i++)
                        {
                            Vessel vessel = FlightGlobals.Vessels.ElementAt(i);

                            if (vessel.situation == Vessel.Situations.ORBITING || (vessel.situation == Vessel.Situations.SUB_ORBITAL && vessel != FlightGlobals.ActiveVessel && vessel == vessel.packed)) // Fixes teleporting debris
                            {
                                if ((vessel.vesselType != VesselType.SpaceObject && vessel.vesselType != VesselType.Unknown))
                                {
                                    if (VesselData.FetchStationKeeping(vessel) == false)
                                    {
                                        if (VesselData.FetchSMA(vessel) != 0)
                                        {
                                            if (Settings.ReadRD() == true)
                                            {
                                                RealisticDecaySimulator(vessel);  // 1.1.0 Realistic Decay Formula
                                            }
                                            else
                                            {
                                                StockDecaySimulator(vessel);
                                            }

                                            if (vessel == !vessel.packed || (TimeWarp.CurrentRate != 0 && HighLogic.LoadedScene == GameScenes.FLIGHT && vessel.situation == Vessel.Situations.ORBITING))
                                            {

                                                if (Settings.ReadRD() == true)
                                                {
                                                    ActiveDecayRealistic(vessel); // 1.2.0 Realistic Active Decay fixes
                                                }
                                                else
                                                {
                                                    ActiveDecayStock(vessel);
                                                }
                                            }
                                        }

                                        if (HighLogic.LoadedScene == GameScenes.TRACKSTATION && Settings.ReadPT() == true)
                                        {
                                            if (Settings.ReadDT() == true)
                                            {
                                                CatchUpOrbit(vessel);
                                            }
                                            else if (Settings.ReadDT() == false && vessel.vesselType != VesselType.Debris)
                                            {
                                                CatchUpOrbit(vessel);
                                            }
                                            else
                                            {
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                }
            }
        }

        public void Save()
        {
            if (HighLogic.LoadedSceneIsGame && HighLogic.LoadedScene != GameScenes.FLIGHT && (HighLogic.LoadedScene != GameScenes.LOADING || HighLogic.LoadedScene != GameScenes.LOADINGBUFFER && HighLogic.LoadedScene != GameScenes.MAINMENU)) // No Saving badly!
            {
                Vessel vessel = new Vessel();  // Set Vessel Orbits
                for (int i = 0; i < FlightGlobals.Vessels.Count; i++)
                {
                    vessel = FlightGlobals.Vessels.ElementAt(i);
                    if ((vessel.vesselType != VesselType.SpaceObject && vessel.vesselType != VesselType.Unknown))
                    {
                        CatchUpOrbit(vessel);
                    }
                }
            }
        }

        public void OnDestroy()
        {
            if (HighLogic.LoadedSceneIsGame && (HighLogic.LoadedScene != GameScenes.LOADING && HighLogic.LoadedScene != GameScenes.LOADINGBUFFER && HighLogic.LoadedScene != GameScenes.MAINMENU))
            {
                GameEvents.onVesselWillDestroy.Remove(ClearVesselOnDestroy);
                GameEvents.onVesselWasModified.Remove(UpdateActiveVesselInformation); // 1.3.0 Resource Change
                GameEvents.onStageSeparation.Remove(UpdateActiveVesselInformationEventReport); // 1.3.0
                GameEvents.onPartActionUIDismiss.Remove(UpdateActiveVesselInformationPart); // 1.3.0
                GameEvents.onPartActionUICreate.Remove(UpdateActiveVesselInformationPart);

                DecayRateModifier = Settings.ReadDecayDifficulty();
                Vessel vessel = new Vessel();  // Set Vessel Orbits
                VesselCount = FlightGlobals.Vessels.Count;
                for (int i = 0; i < FlightGlobals.Vessels.Count; i++)
                {
                    vessel = FlightGlobals.Vessels.ElementAt(i);
                    if ((vessel.vesselType != VesselType.SpaceObject && vessel.vesselType != VesselType.Unknown))
                    {
                        if (vessel.situation == Vessel.Situations.ORBITING)
                        {
                            CatchUpOrbit(vessel);
                        }
                    }
                }
            }
        }

        public void ActiveVesselOrbitManage()
        {
             // Redundant in 1.1.0
            
            if (ActiveVesselOnOrbit == false)
            {
                if (FlightGlobals.ActiveVessel.situation == Vessel.Situations.ORBITING)
                {
                    ActiveVesselOnOrbit = true;
                    VesselData.WriteVesselData(FlightGlobals.ActiveVessel);
                }
            }
             
        }

        public void ActiveVesselFuelManage(Vessel vessel)
        {
            bool StationKeep = VesselData.FetchStationKeeping(vessel);
            if (StationKeep == true)
            {
                StationKeepingManager.FuelManager(vessel);
            }
        }

        /*public void ActiveVesselEngineManage(Vessel vessel)
        {
            if (vessel.FindPartModulesImplementing<ModuleEngines>().Count == 0)
            {
                for (int i = 0; i < vessel.FindPartModulesImplementing<ModuleEngines>().Count; i++)
                {
                    PartModule enginemodule = vessel.FindPartModulesImplementing<ModuleEngines>().ElementAt(i);
                    Part engine = enginemodule.part;
                    VesselData.UpdateEngineInfo(vessel, engine.partName, engine.GetInstanceID(), , true);

                }
            }
        }
       */

        public static void CatchUpOrbit(Vessel vessel)
        {
            if (vessel.situation != Vessel.Situations.PRELAUNCH && vessel.situation != Vessel.Situations.LANDED)
            {
                if (VesselData.FetchSMA(vessel) != vessel.orbitDriver.orbit.semiMajorAxis && VesselData.FetchSMA(vessel) < vessel.orbitDriver.orbit.semiMajorAxis)
                {
                    try
                    {
                        OrbitPhysicsManager.HoldVesselUnpack(60);
                    }
                    catch (NullReferenceException)
                    {
                    }

                    for (int i = 0; i < FlightGlobals.Vessels.Count; i++)
                    {
                        Vessel ship = FlightGlobals.Vessels.ElementAt(i);
                        if (ship.packed)
                        {
                            ship.GoOnRails();
                        }
                    }
                    if (HighLogic.LoadedScene == GameScenes.FLIGHT && vessel.situation != Vessel.Situations.PRELAUNCH)
                    {
                        if (vessel = FlightGlobals.ActiveVessel)
                        {
                            vessel.GoOnRails();
                        }
                    }
                    var oldBody = vessel.orbitDriver.orbit.referenceBody;
                    var orbit = vessel.orbitDriver.orbit;
                    orbit.inclination = vessel.orbit.inclination;
                    if (VesselData.FetchSMA(vessel) == 0)
                    {
                        orbit.semiMajorAxis = vessel.orbit.semiMajorAxis;
                        orbit.eccentricity = vessel.orbit.eccentricity;
                    }
                    else
                    {
                        
                        double TempEccentricity = orbit.eccentricity; // Issues with this
                        double NewEccentricity = orbit.eccentricity;
                        if (orbit.eccentricity > 0.005)
                        {
                        //    NewEccentricity = CalculateNewEccentricity(TempEccentricity, orbit.semiMajorAxis, VesselData.FetchSMA(vessel));
                        }
                        
                        orbit.semiMajorAxis = VesselData.FetchSMA(vessel);
                        orbit.eccentricity = NewEccentricity;
                    }
                    orbit.LAN = vessel.orbit.LAN;
                    orbit.argumentOfPeriapsis = vessel.orbit.argumentOfPeriapsis;
                    orbit.meanAnomalyAtEpoch = vessel.orbit.meanAnomalyAtEpoch;
                    orbit.epoch = vessel.orbit.epoch;
                    orbit.referenceBody = vessel.orbit.referenceBody;
                    orbit.Init();

                    if (TimeWarp.CurrentRate == 1)
                    {
                       // print("KSP - Orbital Decay - Updating Orbit of: " + vessel.vesselName + ". New Semi Major Axis: " + orbit.semiMajorAxis + "m.");
                    }

                    orbit.UpdateFromUT(HighLogic.CurrentGame.UniversalTime); // Changed from Planetarium.GetUniversalTime()
                    vessel.orbitDriver.pos = vessel.orbit.pos.xzy;
                    vessel.orbitDriver.vel = vessel.orbit.vel;

                    var newBody = vessel.orbitDriver.orbit.referenceBody;
                    if (newBody != oldBody)
                    {
                        var evnt = new GameEvents.HostedFromToAction<Vessel, CelestialBody>(vessel, oldBody, newBody);
                        GameEvents.onVesselSOIChanged.Fire(evnt);
                        VesselData.UpdateBody(vessel, newBody);
                    }
                }
            }
        }

        public static void RealisticDecaySimulator(Vessel vessel) // 1.1.0
        {
            Orbit orbit = vessel.orbitDriver.orbit;
            CelestialBody body = orbit.referenceBody;

            if (body.atmosphere)  // Atmospheric Drag
            {
                double InitialSemiMajorAxis = VesselData.FetchSMA(vessel);
                double MaxInfluence = body.Radius * 1.5;

                if (InitialSemiMajorAxis < MaxInfluence)
                {
                    double StandardGravitationalParameter = body.gravParameter;
                    double CartesianPositionVectorMagnitude = orbit.getRelativePositionAtT(HighLogic.CurrentGame.UniversalTime).magnitude; // Planetarium to HighLogic
                    double EquivalentAltitude = (InitialSemiMajorAxis - body.Radius);
                    if (orbit.eccentricity > 0.085)
                    {
                        double AltitudeAp = (InitialSemiMajorAxis * (1 - orbit.eccentricity) - body.Radius);
                        double AltitudePe = (InitialSemiMajorAxis * (1 + orbit.eccentricity) - body.Radius);
                        EquivalentAltitude = (AltitudePe) + 900.0 * Math.Pow(orbit.eccentricity, (double)0.6);
                    }
                     
                    double InitialOrbitalVelocity = orbit.vel.magnitude;
                    double InitialDensity = body.atmDensityASL;
                    double BoltzmannConstant = Math.Pow(1.380 * 10, -23);
                    double Altitude = vessel.altitude;
                    double GravityASL = body.GeeASL;
                    double AtmosphericMolarMass = body.atmosphereMolarMass;

                    double VesselArea = VesselData.FetchArea(vessel);
                    if (VesselArea == 0)
                    {
                        VesselArea = 1.0;
                    }

                    double DistanceTravelled = InitialOrbitalVelocity; // Meters
                    double VesselMass = VesselData.FetchMass(vessel);   // Kg
                    if (VesselMass == 0)
                    {
                        VesselMass = 100.0; // Default is 100kg
                    }

                    EquivalentAltitude = (EquivalentAltitude / 1000.0);

                    //float AtmosphericDensity = (Mathf.Pow(1.020f * 10, 7) * Mathf.Pow((float)((EquivalentAltitude)), -7.172f)); // Kg/m^3 // *10
       
                    double MolecularMass = 27.0 - (0.0012 * ((EquivalentAltitude) - 200.0));
                    double F107Flux = SCSManager.FetchCurrentF107();
                    double GeomagneticIndex = SCSManager.FetchCurrentAp();

                    double ExothericTemperature = 900.0 + (2.5 *(F107Flux - 70.0)) + (1.5 * GeomagneticIndex);
                    double ScaleHeight = ExothericTemperature / MolecularMass;
                    float AtmosphericDensity = (6*(Mathf.Pow((10), -10)) * Mathf.Pow((float)Math.E, -(((float)(EquivalentAltitude) - 175.0f) / (float)ScaleHeight)));

                    double DeltaPeriod = ((3 * Math.PI) * (InitialSemiMajorAxis * AtmosphericDensity) * ((VesselArea * 2.2) / VesselMass)); // Unitless
                    double InitialPeriod = (2 * Math.PI * Math.Sqrt((Math.Pow(InitialSemiMajorAxis, (double)3)) / StandardGravitationalParameter));
                    double FinalPeriod = InitialPeriod - DeltaPeriod;
                    double FinalSemiMajorAxis = Math.Pow(((Math.Pow(((double)FinalPeriod / (double)(2 * Math.PI)), (double)2)) * (double)StandardGravitationalParameter), ((double)1.0 / (double)3.0));
                    double DecayValue = InitialSemiMajorAxis - FinalSemiMajorAxis;

                    //print("Drag Decay Value: " + ((InitialSemiMajorAxis - FinalSemiMajorAxis) * TimeWarp.CurrentRate * Settings.ReadDecayDifficulty()) + "m");

                    VesselData.UpdateVesselSMA(vessel, (float)(InitialSemiMajorAxis - (DecayValue * TimeWarp.CurrentRate * Settings.ReadDecayDifficulty())));
                    CheckVesselSurvival(vessel);
                }
            }
            else // Radiation Pressure 
            {
                double SolarEnergy = (double)Math.Pow(((double)3.86 * (double)10.0), (double)26); // W
                double SolarDistance = body.orbitDriver.orbit.altitude; // m
                double SolarConstant = SolarEnergy / ((double)4 * (double)Math.PI * Math.Pow((double)SolarDistance, (double)2.0)); // W/m^2
                double InitialSemiMajorAxis = VesselData.FetchSMA(vessel);
                double StandardGravitationalParameter = body.gravParameter;
                double MeanAngularVelocity = (double)Math.Sqrt((double)StandardGravitationalParameter / ((double)Math.Pow((double)InitialSemiMajorAxis, (double)3.0)));
                double SpeedOfLight = Math.Pow((double)3.0 * (double)10.0, (double)8.0);

                double VesselArea = VesselData.FetchArea(vessel);
                if (VesselArea == 0)
                {
                    VesselArea = 1.0;
                }

                double VesselMass = VesselData.FetchMass(vessel);   // Kg
                if (VesselMass == 0)
                {
                    VesselMass = 1.0;
                }

                double VesselRadius = Math.Sqrt((double)VesselArea / (double)Math.PI);
                double ImmobileAccelleration = (Math.PI * (VesselRadius * VesselRadius) * SolarConstant) / (VesselMass * SpeedOfLight * (SolarDistance * SolarDistance));
                double ChangeInSemiMajorAxis = -(6.0 * Math.PI * ImmobileAccelleration * (InitialSemiMajorAxis)) / (MeanAngularVelocity * SpeedOfLight);
                double FinalSemiMajorAxis = InitialSemiMajorAxis + ChangeInSemiMajorAxis;

                //print("SRP Decay Value: " + ((InitialSemiMajorAxis - FinalSemiMajorAxis) * TimeWarp.CurrentRate * Settings.ReadDecayDifficulty()) + "m");

                VesselData.UpdateVesselSMA(vessel, (float)(InitialSemiMajorAxis + (ChangeInSemiMajorAxis * TimeWarp.CurrentRate * Settings.ReadDecayDifficulty())));
                CheckVesselSurvival(vessel);
            }
        }

        public static void StockDecaySimulator(Vessel vessel)
        {
            double BodyGravityConstant = vessel.orbitDriver.orbit.referenceBody.GeeASL;
            double AtmosphereMultiplier;
            double MaxDecayInfluence = vessel.orbitDriver.orbit.referenceBody.Radius * 10;
            var oldBody = vessel.orbitDriver.orbit.referenceBody;

            if (vessel.orbitDriver.orbit.referenceBody.atmosphere)
            {
                AtmosphereMultiplier = vessel.orbitDriver.orbit.referenceBody.atmospherePressureSeaLevel / 101.325;
            }
            else
            {
                AtmosphereMultiplier = 0.5;
            }

            if (vessel.orbitDriver.orbit.semiMajorAxis + 50 < MaxDecayInfluence)
            {
                double Lambda = 0.000000000133913;
                double Sigma = MaxDecayInfluence - vessel.orbitDriver.orbit.altitude;
                double Area = VesselData.FetchArea(vessel);
                if (Area == 0)
                {
                    Area = 1.0;
                }
                double Mass = VesselData.FetchMass(vessel);
                if (Mass == 0)
                {
                    Mass = 100.0; // Default 100Kg
                }

                DecayValue = (double)TimeWarp.CurrentRate * Sigma * BodyGravityConstant * AtmosphereMultiplier * Lambda * Area * (Mass/1000.0); // 1.0.9 Update

                if (vessel.orbitDriver.orbit.referenceBody.atmosphere)
                {
                    if (vessel.orbitDriver.orbit.PeA < vessel.orbitDriver.orbit.referenceBody.atmosphereDepth)
                    {
                       // DecayValue = DecayValue * (Math.Pow(Math.E, vessel.orbitDriver.orbit.referenceBody.atmosphereDepth - vessel.orbitDriver.orbit.PeA)); // Have it increase alot more as we enter the hard atmosphere
                        //DecayValue = DecayValue * 1.5; // Stops sqiffyness! 
                    }

                    MaxDecayValue = ((vessel.orbitDriver.orbit.referenceBody.Radius + vessel.orbitDriver.orbit.referenceBody.atmosphereDepth) * BodyGravityConstant * AtmosphereMultiplier * Lambda);
                    EstimatedTimeUntilDeorbit = ((float)(vessel.orbitDriver.orbit.semiMajorAxis - (float)vessel.orbitDriver.orbit.referenceBody.atmosphereDepth)) / (float)MaxDecayValue;
                }
                else
                {
                    MaxDecayValue = ((vessel.orbitDriver.orbit.referenceBody.Radius + 100) * BodyGravityConstant * AtmosphereMultiplier * Lambda);
                    EstimatedTimeUntilDeorbit = ((float)(vessel.orbitDriver.orbit.semiMajorAxis - (float)vessel.orbitDriver.orbit.referenceBody.atmosphereDepth)) / (float)MaxDecayValue;
                }

            }
            else
            {
                DecayValue = 0.0;
            }

            DecayValue = DecayValue * DecayRateModifier;// Decay Rate Modifier from Settings 
            VesselDied = false;
            CheckVesselSurvival(vessel);

            if (VesselDied == false)         // Just Incase the vessel is destroyed part way though the check.
            {
                if (vessel.orbitDriver.orbit.referenceBody.GetInstanceID() != 0 || vessel.orbitDriver.orbit.semiMajorAxis > vessel.orbitDriver.orbit.referenceBody.Radius + 5)
                {
                    VesselData.UpdateVesselSMA(vessel, ((float)VesselData.FetchSMA(vessel) - (float)DecayValue));
                }
            }
            CheckVesselSurvival(vessel);
        }

        public static void CheckVesselSurvival(Vessel vessel)
        {
            VesselDied = false;
            if (vessel.situation != Vessel.Situations.SUB_ORBITAL) // Prevents debris from dissapearing
            {

                if (vessel.orbitDriver.orbit.referenceBody.atmosphere) // Big problem ( Jool, Eve, Duna, Kerbin, Laythe)
                {
                    if (!MessageDisplayed.Keys.Contains(vessel))
                    {
                        if (VesselData.FetchSMA(vessel) < vessel.orbitDriver.orbit.referenceBody.Radius + vessel.orbitDriver.referenceBody.atmosphereDepth)
                        {
                            TimeWarp.SetRate(0, true);
                            print("Warning: " + vessel.name + " is approaching " + vessel.orbitDriver.referenceBody.name + "'s hard atmosphere");
                            ScreenMessages.PostScreenMessage("Warning: " + vessel.name + " is approaching " + vessel.orbitDriver.referenceBody.name + "'s hard atmosphere");
                            MessageDisplayed.Add(vessel, true);
                        }
                    }

                    if (VesselData.FetchSMA(vessel) < vessel.orbitDriver.orbit.referenceBody.Radius + (vessel.orbitDriver.referenceBody.atmosphereDepth/(double)1.5)) // 1.3.0 Increased Tolerance
                    {
                        VesselDied = true;
                    }
                }
                else // Moon Smaller Problem
                {
                    if (MessageDisplayed.Keys.Contains(vessel))
                    {
                        if (VesselData.FetchSMA(vessel) < vessel.orbitDriver.orbit.referenceBody.Radius + 5000)
                        {
                            TimeWarp.SetRate(0, true);
                            print("Warning: " + vessel.name + " is approaching " + vessel.orbitDriver.referenceBody.name + "'s surface");
                            ScreenMessages.PostScreenMessage("Warning: " + vessel.name + " is approaching " + vessel.orbitDriver.referenceBody.name + "'s surface");
                            MessageDisplayed.Add(vessel, true);
                        }
                    }

                    if (VesselData.FetchSMA(vessel) < vessel.orbitDriver.orbit.referenceBody.Radius + 100)
                    {
                        VesselDied = true;
                    }
                }

                if (VesselDied == true)
                {
                    if (vessel != FlightGlobals.ActiveVessel)
                    {
                        print(vessel.name + " entered " + vessel.orbitDriver.referenceBody.name + "'s atmosphere and was destroyed");
                        ScreenMessages.PostScreenMessage(vessel.name + " entered " + vessel.orbitDriver.referenceBody.name + "'s atmosphere and was destroyed");
                        if (MessageDisplayed.ContainsKey(vessel))
                        {
                            MessageDisplayed.Remove(vessel);
                        }
                        VesselData.ClearVesselData(vessel);
                        vessel.Die();
                    }
                    VesselDied = false;
                }
            }
        }

        public static void ActiveDecayRealistic(Vessel vessel)            // 1.2.0
        {  
            double ReadTime = HighLogic.CurrentGame.UniversalTime;     
            double DecayValue = DecayRateRealistic(vessel);
            double InitialVelocity = vessel.orbitDriver.orbit.getOrbitalVelocityAtUT(ReadTime).magnitude;
            double CalculatedFinalVelocty = 0.0;
            Orbit newOrbit = vessel.orbitDriver.orbit;
            newOrbit.semiMajorAxis = (VesselData.FetchSMA(vessel) - DecayValue);
            CalculatedFinalVelocty = newOrbit.getOrbitalVelocityAtUT(ReadTime).magnitude;

            if (TimeWarp.CurrentRate == 1)
            {
                //vessel.ChangeWorldVelocity(-((vessel.orbitDriver.orbit.getOrbitalVelocityAtUT(ReadTime) * (InitialVelocity - CalculatedFinalVelocty)) / 10000000)); // REMOVED FOR NOW (BACK IN 1.3.0)
            }

            else if (TimeWarp.CurrentRate > 1 && TimeWarp.WarpMode != TimeWarp.Modes.LOW) // 1.3.0 Timewarp Fix
            {
                VesselData.UpdateVesselSMA(vessel, ((float)VesselData.FetchSMA(vessel) - (float)DecayValue));
                CatchUpOrbit(vessel);
            }
      
        }

        public static void ActiveDecayStock(Vessel vessel)
        {
            OrbitDriver driver;
            Orbit orbit;
            Vector3d decayVelVector;

            double MaxDecayInfluence = vessel.orbitDriver.orbit.referenceBody.Radius * 10;

            if (vessel.orbitDriver.orbit.PeA < MaxDecayInfluence)
            {
                double DecayValue = DecayRateStock(vessel);
                double MaxDecayValue;
                orbit = vessel.orbitDriver.orbit;
                driver = vessel.orbitDriver;
                double BodyGravityConstant = vessel.orbitDriver.orbit.referenceBody.GeeASL;
                double AtmosphereMultiplier;
                double Lambda = 0.000000000133913;

                if (vessel.orbitDriver.orbit.referenceBody.atmosphere)
                {
                    AtmosphereMultiplier = vessel.orbitDriver.orbit.referenceBody.atmospherePressureSeaLevel / 101.325;
                }
                else
                {
                    AtmosphereMultiplier = 0.5;
                }

                if (orbit.referenceBody.atmosphere)
                {
                    MaxDecayValue = ((vessel.orbitDriver.orbit.referenceBody.Radius + vessel.orbitDriver.orbit.referenceBody.atmosphereDepth) * BodyGravityConstant * AtmosphereMultiplier * Lambda);
                    EstimatedTimeUntilDeorbit = ((float)(vessel.orbitDriver.orbit.semiMajorAxis - (float)vessel.orbitDriver.orbit.referenceBody.atmosphereDepth)) / (float)MaxDecayValue;
                }
                else
                {
                    MaxDecayValue = ((vessel.orbitDriver.orbit.referenceBody.Radius + 100) * BodyGravityConstant * AtmosphereMultiplier * Lambda);
                    EstimatedTimeUntilDeorbit = ((float)(vessel.orbitDriver.orbit.semiMajorAxis - (float)vessel.orbitDriver.orbit.referenceBody.atmosphereDepth)) / (float)MaxDecayValue;
                }

                DecayValue = DecayValue * DecayRateModifier;// Decay Rate Modifier from Settings 

                double DeltaS = DecayValue;
                decayVelVector = orbit.vel * ((DecayValue) / (447041.9058));

                if (TimeWarp.CurrentRate == 1)
                {
                    //vessel.ChangeWorldVelocity(-decayVelVector); // REMOVED FOR NOW (BACK IN 1.3.0)
                }

                else if (TimeWarp.CurrentRate > 1 && TimeWarp.WarpMode != TimeWarp.Modes.LOW) // 1.3.0 Timewarp Fix
                {
                    VesselData.UpdateVesselSMA(vessel, ((float)VesselData.FetchSMA(vessel) - (float)DecayValue));
                    CatchUpOrbit(vessel);
                }
            }
        }

        public static double DecayRateRealistic(Vessel vessel)
        {
            double DecayRate = 0.0;
            Orbit orbit = vessel.orbitDriver.orbit;
            CelestialBody body = orbit.referenceBody;

            if (body.atmosphere == true)  // Atmospheric Drag // Disused for 1.1.0
            {
                double InitialSemiMajorAxis = VesselData.FetchSMA(vessel);
                double MaxInfluence = body.Radius * 1.5;

                if (InitialSemiMajorAxis < MaxInfluence)
                {
                    double StandardGravitationalParameter = body.gravParameter;
                    double CartesianPositionVectorMagnitude = orbit.getRelativePositionAtT(HighLogic.CurrentGame.UniversalTime).magnitude; // Planetarium
                    double EquivalentAltitude = (InitialSemiMajorAxis - body.Radius);
                    if (orbit.eccentricity > 0.085)
                    {
                        double AltitudeAp = (InitialSemiMajorAxis * (1 - orbit.eccentricity) - body.Radius);
                        double AltitudePe = (InitialSemiMajorAxis * (1 + orbit.eccentricity) - body.Radius);
                        EquivalentAltitude = (AltitudePe) + 900 * Math.Pow(orbit.eccentricity, (double)0.6);
                    }
                    double InitialOrbitalVelocity = orbit.vel.magnitude;
                    double InitialDensity = body.atmDensityASL;
                    double BoltzmannConstant = Math.Pow(1.380 * 10, -23);
                    double Altitude = vessel.altitude;
                    double GravityASL = body.GeeASL;
                    double AtmosphericMolarMass = body.atmosphereMolarMass;
                    double VesselArea = VesselData.FetchArea(vessel);
                    if (VesselArea == 0)
                    {
                        VesselArea = 1.0;
                    }

                    double DistanceTravelled = InitialOrbitalVelocity; // Meters
                    double VesselMass = VesselData.FetchMass(vessel);   // Kg
                    if (VesselMass == 0)
                    {
                        VesselMass = 100.0; // Default is 100kg
                    }

                    EquivalentAltitude = EquivalentAltitude / 1000.0;

                    //float AtmosphericDensity = (Mathf.Pow(1.020f * 10, 7) * Mathf.Pow((float)((EquivalentAltitude)), -7.172f)); // Kg/m^3 // *1

                    double MolecularMass = 27 - 0.0012 * ((EquivalentAltitude) - 200);    
                    double F107Flux = SCSManager.FetchCurrentF107();
                    double GeomagneticIndex = SCSManager.FetchCurrentAp();

                    double ExothericTemperature = 900.0 + (2.5 * (F107Flux - 70)) + (1.5 * GeomagneticIndex);
                    double ScaleHeight = ExothericTemperature / MolecularMass;
                    float AtmosphericDensity = (6 * (Mathf.Pow((10), -10)) * Mathf.Pow((float)Math.E, -(((float)(EquivalentAltitude) - 175.0f) / (float)ScaleHeight)));

                    double DeltaPeriod = ((3 * Math.PI) * (InitialSemiMajorAxis * AtmosphericDensity) * ((VesselArea * 2.2) / VesselMass)); // Unitless
                    double InitialPeriod = (2 * Math.PI * Math.Sqrt((Math.Pow(InitialSemiMajorAxis, (double)3)) / StandardGravitationalParameter));
                    double FinalPeriod = InitialPeriod - DeltaPeriod;
                    double FinalSemiMajorAxis = Math.Pow(((Math.Pow(((double)FinalPeriod / (double)(2 * Math.PI)), (double)2)) * (double)StandardGravitationalParameter), ((double)1.0 / (double)3.0));
                    DecayRate = (InitialSemiMajorAxis - FinalSemiMajorAxis) * TimeWarp.CurrentRate * DecayRateModifier;
                }
            }
            else // Radiation Pressure 
            {
                double SolarEnergy = (double)Math.Pow(((double)3.86 * (double)10.0), (double)26.0); // W
                double SolarDistance = body.orbitDriver.orbit.altitude; // m
                double SolarConstant = 0.0;
                SolarConstant = SolarEnergy / ((double)4.0 * (double)Math.PI * (double)Math.Pow((double)SolarDistance, (double)2.0)); // W/m^2
                double InitialSemiMajorAxis = VesselData.FetchSMA(vessel);
                double StandardGravitationalParameter = body.gravParameter;
                double MeanAngularVelocity = (double)Math.Sqrt((double)StandardGravitationalParameter / ((double)Math.Pow((double)InitialSemiMajorAxis, (double)3.0)));
                double SpeedOfLight = Math.Pow((double)3 * (double)10, (double)8);

                double VesselArea = VesselData.FetchArea(vessel);
                if (VesselArea == 0)
                {
                    VesselArea = 1.0;
                }

                double VesselMass = VesselData.FetchMass(vessel);   // Kg
                if (VesselMass == 0)
                {
                    VesselMass = 100.0;
                }

                double VesselRadius = (double)Math.Sqrt((double)VesselArea / (double)Math.PI);
                double ImmobileAccelleration = (Math.PI * (VesselRadius * VesselRadius) * SolarConstant) / (VesselMass * SpeedOfLight * (SolarDistance * SolarDistance));
                double ChangeInSemiMajorAxis = -(6.0 * Math.PI * ImmobileAccelleration * (InitialSemiMajorAxis)) / (MeanAngularVelocity * SpeedOfLight);

                DecayRate = ((ChangeInSemiMajorAxis) * TimeWarp.CurrentRate * DecayRateModifier);
            }
            return DecayRate;
        }

        public static double DecayRateStock(Vessel vessel)
        {
            double MaxDecayInfluence = vessel.orbitDriver.orbit.referenceBody.Radius * 10;
            double AtmosphereMultiplier;

            if (vessel.orbitDriver.orbit.referenceBody.atmosphere)
            {
                //if (vessel.orbitDriver.orbit.altitude < vessel.orbitDriver.orbit.referenceBody.atmosphereDepth + (vessel.orbitDriver.orbit.referenceBody.atmosphereDepth * 0.075))
                {
                   // AtmosphereMultiplier = (vessel.orbitDriver.orbit.referenceBody.atmospherePressureSeaLevel / 101.325) * (10 - (VesselData.FetchSMA(vessel) - (vessel.orbitDriver.orbit.referenceBody.Radius + vessel.orbitDriver.orbit.referenceBody.atmosphereDepth) / 10000));
                }
                //else
                {
                    AtmosphereMultiplier = vessel.orbitDriver.orbit.referenceBody.atmospherePressureSeaLevel / 101.325;
                }

            }
            else
            {
                AtmosphereMultiplier = 0.5;
            }

            if (vessel.orbitDriver.orbit.semiMajorAxis + 50 < MaxDecayInfluence)
            {
                double Lambda = 0.000000000133913;
                double Sigma = 0;

                if (vessel.orbitDriver.orbit.referenceBody.atmosphere)
                {
                    Sigma = MaxDecayInfluence - VesselData.FetchSMA(vessel) - (vessel.orbitDriver.orbit.referenceBody.Radius + vessel.orbitDriver.orbit.referenceBody.atmosphereDepth);
                }
                else if (!vessel.orbitDriver.orbit.referenceBody.atmosphere)
                {
                    Sigma = MaxDecayInfluence - VesselData.FetchSMA(vessel) - vessel.orbitDriver.orbit.referenceBody.Radius;
                }

                double Area = VesselData.FetchArea(vessel);
                if (Area == 0)
                {
                    Area = 1.0;
                }
                double Mass = VesselData.FetchMass(vessel);
                if (Mass == 0)
                {
                    Mass = 100.0;
                }

                DecayValue = (double)TimeWarp.CurrentRate * Sigma * vessel.orbitDriver.orbit.referenceBody.GeeASL * AtmosphereMultiplier * Lambda * (Mass/1000) * Area;
            }
            else
            {
                DecayValue = 0;
            }

            DecayValue = DecayValue * DecayRateModifier;// Decay Rate Modifier from Settings 

            return DecayValue;
        }

        public static double StockDecayTimePrediction(Vessel vessel)
        {
            double DaysUntilDecay = 0;
            OrbitDriver Driver = vessel.orbitDriver;
            double PeA = Driver.orbit.PeA;
            double DecayRatePeA = DecayRateStock(vessel) / 2;
            double HardAtmosphereOrSurface = 0;

            if (Driver.orbit.referenceBody.atmosphere)
            {
                HardAtmosphereOrSurface = Driver.orbit.referenceBody.atmosphereDepth;
            }

            else
            {
                HardAtmosphereOrSurface = 0;
            }

            double hoursInDay = 6;
            bool hr = Settings.Read24Hr();
            if (hr == true)
            {
                hoursInDay = 24;
            }

            double Distance = PeA - HardAtmosphereOrSurface;
            double TimeInSeconds = Distance / (DecayRatePeA / TimeWarp.CurrentRate);
            DaysUntilDecay = TimeInSeconds / (60 * 60 * hoursInDay);
            return DaysUntilDecay;
        }


        public static double RealisticDecayTimePrediction(Vessel vessel)
        {
            double DaysUntilDecay = 0;
            double InitialSemiMajorAxis = VesselData.FetchSMA(vessel);
            Orbit orbit = vessel.orbitDriver.orbit;
            CelestialBody body = vessel.orbitDriver.orbit.referenceBody;
            double InitialPeriod = Math.PI * 2.0 * (Math.Sqrt((InitialSemiMajorAxis * InitialSemiMajorAxis * InitialSemiMajorAxis) / body.gravParameter));

            double EquivalentAltitude = (InitialSemiMajorAxis - body.Radius);
            if (orbit.eccentricity > 0.085)
            {
                double AltitudeAp = (InitialSemiMajorAxis * (1 - orbit.eccentricity) - body.Radius);
                double AltitudePe = (InitialSemiMajorAxis * (1 + orbit.eccentricity) - body.Radius);
                EquivalentAltitude = (AltitudePe) + 900 * Math.Pow(orbit.eccentricity, (double)0.6);
            }

            double BaseAltitude = body.atmosphereDepth/1000;

            double MolecularMass = 27 - 0.0012 * ((EquivalentAltitude/1000) - 200);
            double F107Flux = SCSManager.FetchAverageF107();
            double GeomagneticIndex = SCSManager.FetchAverageAp();

            double ExothericTemperature = 900.0 + (2.5 * (F107Flux - 70)) + (1.5 * GeomagneticIndex);
            double ScaleHeight = ExothericTemperature / MolecularMass;
            float AtmosphericDensity = (6 * (Mathf.Pow((10), -10)) * Mathf.Pow((float)Math.E, -(((float)(EquivalentAltitude/1000) - 175.0f) / (float)ScaleHeight)));
            double Beta = 1.0 / ScaleHeight;

            double VesselArea = VesselData.FetchArea(vessel);
            if (VesselArea == 0)
            {
                VesselArea = 1.0;
            }

            double VesselMass = VesselData.FetchMass(vessel);   // Kg
            if (VesselMass == 0)
            {
                VesselMass = 100.0;
            }

            EquivalentAltitude = EquivalentAltitude + body.Radius;


            double Time1 = ((InitialPeriod / (60.0*60.0))/4.0*Math.PI) * (((2.0 * Beta * EquivalentAltitude) + 1.0)/(AtmosphericDensity * (Beta * Beta) * (EquivalentAltitude * EquivalentAltitude * EquivalentAltitude)));
            double Time2 = Time1 * (VesselMass / (2.2 * VesselArea)) * (1 - Math.Pow(Math.E, (Beta * (BaseAltitude-((EquivalentAltitude-body.Radius)/1000)))));

            DaysUntilDecay = Time2;

            return DaysUntilDecay;
        }


        public static double CalculateNewEccentricity(double OldEccentricity, double OldSMA, double NewSMA) // Really think about this!
        {
            double NewEccentricity = 0.0;
            double FixedSemiMinorAxis = Math.Sqrt((OldSMA * OldSMA) - ((OldEccentricity * OldEccentricity) * (OldSMA * OldSMA)));
            NewEccentricity = (Math.Sqrt(1 - ((FixedSemiMinorAxis * FixedSemiMinorAxis) / (NewSMA * NewSMA))));
            return NewEccentricity;
        }
    } 
}
