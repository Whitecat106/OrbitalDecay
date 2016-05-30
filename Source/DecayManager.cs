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
        #region Declared Variables

        private float UPTInterval = 1.0f;
        private float lastUpdate = 0.0f;

        public static double DecayValue;
        public static double MaxDecayValue;
        public static bool VesselDied = false;
        public static float EstimatedTimeUntilDeorbit;
        public static bool GUIToggled = false;
        public static Dictionary<Vessel, bool> MessageDisplayed = new Dictionary<Vessel, bool>();
        public static double VesselCount = 0;
        public static Vessel ActiveVessel = new Vessel();
        public static bool ActiveVesselOnOrbit = false;
        public static bool EVAActive = false;

        public static bool CatchupResourceMassAreaDataComplete = false;

        #endregion

        #region Unity Scene Subroutines

        public void Start()
        {
            if (HighLogic.LoadedSceneIsGame && (HighLogic.LoadedScene != GameScenes.LOADING && HighLogic.LoadedScene != GameScenes.LOADINGBUFFER && HighLogic.LoadedScene != GameScenes.MAINMENU))
            {
                CatchupResourceMassAreaDataComplete = false;
                // GameEvents -- //

                GameEvents.onVesselWillDestroy.Add(ClearVesselOnDestroy); // Vessel destroy checks 1.1.0
                GameEvents.onVesselWasModified.Add(UpdateActiveVesselInformation); // Resource level change 1.3.0
                GameEvents.onStageSeparation.Add(UpdateActiveVesselInformationEventReport); // Resource level change 1.3.0
                GameEvents.onNewVesselCreated.Add(UpdateVesselSpawned); // New Vessel Checks 1.4.2

                if (HighLogic.LoadedScene == GameScenes.FLIGHT)//CheckSceneStateFlight(HighLogic.LoadedScene)) // 1.3.1
                {
                    GameEvents.onPartActionUIDismiss.Add(UpdateActiveVesselInformationPart); // Resource level change 1.3.0
                    GameEvents.onPartActionUIDismiss.Add(SetGUIToggledFalse);
                    GameEvents.onPartActionUICreate.Add(UpdateActiveVesselInformationPart);

                    GameEvents.onGameStateSave.Add(QuickLoadUpdate); // Quicksave Checks 1.5.0
                    GameEvents.onGameStatePostLoad.Add(QuickSaveUpdate); // Quickload Checks 1.5.0 
                }

                // -- GameEvents //

                Vessel vessel = new Vessel();
                VesselCount = FlightGlobals.Vessels.Count;
                for (int i = 0; i < FlightGlobals.Vessels.Count; i++)
                {
                    vessel = FlightGlobals.Vessels.ElementAt(i);

                        if (vessel.situation == Vessel.Situations.ORBITING || vessel.situation == Vessel.Situations.SUB_ORBITAL) // 1.4.2
                        {
                                CatchUpOrbit(vessel);
                        }
                }
            }
        }

        #region Update Subroutines

        public void UpdateActiveVesselInformationEventReport(EventReport report) // 1.3.0
        {
            if (HighLogic.LoadedScene == GameScenes.FLIGHT) // 1.3.1
            {
                VesselData.UpdateActiveVesselData(FlightGlobals.ActiveVessel);
            }
        }

        public void UpdateActiveVesselInformationPart(Part part) // Until eventdata OnPartResourceFlowState works! // 1.3.0
        {
            if (part.vessel == FlightGlobals.ActiveVessel && TimeWarp.CurrentRate == 0) // 1.4.2 
            {
                if (HighLogic.LoadedScene == GameScenes.FLIGHT && GUIToggled == false) // 1.3.1
                {
                    VesselData.UpdateActiveVesselData(FlightGlobals.ActiveVessel);
                    GUIToggled = true;
                }
            }
        }

        public void SetGUIToggledFalse(Part part)
        {
            GUIToggled = false;
        }

        public void UpdateActiveVesselInformation(Vessel vessel)
        {
            if (vessel == FlightGlobals.ActiveVessel)
            {
                VesselData.UpdateActiveVesselData(vessel);
            }
        }

        public void UpdateVesselSpawned(Vessel vessel)
        {
            if (vessel.situation == Vessel.Situations.ORBITING)
            {
                VesselData.WriteVesselData(vessel);
            }
        }

        public void QuickSaveUpdate(ConfigNode node)
        {
          
            VesselData.OnQuickSave();
        } // 1.5.0 QuickSave functionality // Thanks zajc3w!

        public void QuickLoadUpdate(ConfigNode node)
        {
            
            VesselData.OnQuickSave();
            VesselData.VesselInformation.ClearNodes();
            if (HighLogic.LoadedSceneIsFlight)
            {
                VesselData.UpdateActiveVesselData(FlightGlobals.ActiveVessel);
            }
        } // 1.5.0 Quickload functionality // Thanks zajc3w!
            
        public void ClearVesselOnDestroy(Vessel vessel)
        {
            VesselData.ClearVesselData(vessel);
        }

        public void UpdateVesselDataResources(Vessel vessel)
        {
            //151 VesselData.UpdateVesselFuel(vessel, ResourceManager.GetResources(vessel, Settings.ReadStationKeepingResource()));
            VesselData.UpdateVesselFuel(vessel, ResourceManager.GetResources2(vessel));//151
            VesselData.UpdateVesselResource(vessel, ResourceManager.GetResourceNames(vessel));//151
        }

        #endregion

        #region Check Subroutines 

        public static bool CheckSceneStateMain(GameScenes scene)
        {
            if (scene != GameScenes.LOADING && scene != GameScenes.LOADINGBUFFER && HighLogic.LoadedSceneIsGame)
            {
                return true;
            }

            else
            {
                return false;
            }
        }

        public static bool CheckSceneStateMainNotSpaceCentre(GameScenes scene)
        {
            if (scene != GameScenes.LOADING && scene != GameScenes.LOADINGBUFFER && HighLogic.LoadedSceneIsGame && scene != GameScenes.SPACECENTER)
            {
                return true;
            }

            else
            {
                return false;
            }
        }

        public static bool CheckSceneStateFlight(GameScenes scene)
        {
            if (scene == GameScenes.FLIGHT && (scene != GameScenes.LOADING && scene != GameScenes.LOADINGBUFFER && HighLogic.LoadedSceneIsGame))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool CheckVesselState(Vessel vessel)
        {
            if (vessel.situation == Vessel.Situations.ORBITING)
            {
                return true;
            }

            else
            {
                return false;
            }
        }

        public static bool CheckVesselStateActive(Vessel vessel)
        {
            if (vessel.situation == Vessel.Situations.ORBITING && vessel == FlightGlobals.ActiveVessel)
            {
                return true;
            }

            else
            {
                return false;
            }
        }

        public static bool CheckVesselActiveInScene(Vessel vessel)
        {
            if (vessel.situation == Vessel.Situations.ORBITING && vessel == FlightGlobals.ActiveVessel)
            {
                return true;
            }

            else
            {
                return false;
            }
        }

        public static bool CheckVesselProximity(Vessel vessel)
        {
            bool close = false;

            if (HighLogic.LoadedSceneIsFlight)
            {
                double Distance = Vector3d.Distance(vessel.GetWorldPos3D(), FlightGlobals.ActiveVessel.GetWorldPos3D()); // Fixes Rendezvous Issues Hopefully
                if (Distance < 100000)
                {
                    close = true;
                }

                if (vessel == FlightGlobals.ActiveVessel)
                {
                    foreach (Vessel v in FlightGlobals.Vessels)
                    {
                        if (v.loaded && v != vessel) // && (v.vesselType == VesselType.EVA || v.vesselType == VesselType.Debris))
                        {
                            close = true;
                            break;
                        }
                    }
                }      
            }

            return close;
        }

        #endregion

        public void FixedUpdate()
        {
            if (Time.timeSinceLevelLoad > 0.4 && HighLogic.LoadedSceneIsFlight && CatchupResourceMassAreaDataComplete == false && (FlightGlobals.ActiveVessel.situation == Vessel.Situations.ORBITING || FlightGlobals.ActiveVessel.situation == Vessel.Situations.SUB_ORBITAL))
            {
                if (FlightGlobals.ActiveVessel.isActiveAndEnabled) // Vessel is ready
                {
                    /*if (VesselData.FetchFuel(FlightGlobals.ActiveVessel) < ResourceManager.GetResources(FlightGlobals.ActiveVessel, Settings.ReadStationKeepingResource()))
                    { 
                        ResourceManager.CatchUp(FlightGlobals.ActiveVessel, Settings.ReadStationKeepingResource());
                    }*/
                    if (VesselData.FetchFuelLost() > 0 )
                    {
                        ResourceManager.RemoveResources(FlightGlobals.ActiveVessel, VesselData.FetchFuelLost());
                        VesselData.SetFuelLost(0);

                    }
                        
                    VesselData.UpdateActiveVesselData(FlightGlobals.ActiveVessel);
                    print("WhitecatIndustries - Orbital Decay - Updating Fuel Levels for: " + FlightGlobals.ActiveVessel.GetName());
                    CatchupResourceMassAreaDataComplete = true;
                }
            }

            if (Time.timeSinceLevelLoad > 0.5)
            {
                if (HighLogic.LoadedSceneIsGame && (HighLogic.LoadedScene != GameScenes.LOADING && HighLogic.LoadedScene != GameScenes.LOADINGBUFFER && HighLogic.LoadedScene != GameScenes.MAINMENU))
                {
                    if ((Time.time - lastUpdate) > UPTInterval)
                    {
                        lastUpdate = Time.time;

                        if (HighLogic.LoadedScene == GameScenes.SPACECENTER || HighLogic.LoadedScene == GameScenes.TRACKSTATION || HighLogic.LoadedScene == GameScenes.FLIGHT)
                        {
                            for (int i = 0; i < FlightGlobals.Vessels.Count; i++)
                            {
                                Vessel vessel = FlightGlobals.Vessels.ElementAt(i);

                                if ((vessel.situation == Vessel.Situations.ORBITING) || (vessel.situation == Vessel.Situations.SUB_ORBITAL && vessel != FlightGlobals.ActiveVessel && vessel == vessel.packed)) // Fixes teleporting debris
                                {
                                    if (VesselData.FetchStationKeeping(vessel) == false)
                                    {
                                        if (VesselData.FetchSMA(vessel) > 0)
                                        {
                                            if (!vessel.packed)
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
                                            else
                                            {
                                                RealisticDecaySimulator(vessel);
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
                                    else
                                    {
                                        StationKeepingManager.FuelManager(vessel);
                                    }

                                    if (HighLogic.LoadedSceneIsFlight) // UI Resource Updating 1.4.2
                                    {
                                        if (vessel == FlightGlobals.ActiveVessel)
                                        {
                                            UpdateVesselDataResources(vessel);
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
                    //if ((vessel.vesselType != VesselType.SpaceObject && vessel.vesselType != VesselType.Unknown)) // 1.4.2 fixes
                    if (vessel.situation == Vessel.Situations.ORBITING)
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
                GameEvents.onNewVesselCreated.Remove(UpdateVesselSpawned); // 1.4.2 

                if (HighLogic.LoadedScene == GameScenes.FLIGHT) // 1.3.1
                {
                    GameEvents.onPartActionUIDismiss.Remove(UpdateActiveVesselInformationPart); // 1.3.0
                    GameEvents.onPartActionUIDismiss.Remove(SetGUIToggledFalse);
                    GameEvents.onPartActionUICreate.Remove(UpdateActiveVesselInformationPart);

                    GameEvents.onGameStateSave.Remove(QuickSaveUpdate); 
                    GameEvents.onGameStatePostLoad.Remove(QuickLoadUpdate); // Quickload Checks 1.5.0 
                }

                Vessel vessel = new Vessel();  // Set Vessel Orbits
                VesselCount = FlightGlobals.Vessels.Count;
                for (int i = 0; i < FlightGlobals.Vessels.Count; i++)
                {
                    vessel = FlightGlobals.Vessels.ElementAt(i);
                        if (vessel.situation == Vessel.Situations.ORBITING)
                        {
                            CatchUpOrbit(vessel);
                        }
                }
            }
        }

        #endregion

        #region Active Specific Subroutines

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
             
        } // Redundant in 1.1.0

        public static void CatchUpOrbit(Vessel vessel)
        {
            if (vessel.situation != Vessel.Situations.PRELAUNCH && vessel.situation != Vessel.Situations.LANDED)
            {
                if (VesselData.FetchSMA(vessel) < vessel.GetOrbitDriver().orbit.semiMajorAxis && CheckVesselProximity(vessel) == false)
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

                    if (VesselData.FetchSMA(vessel) != 0)
                    {
                        /*
                        var oldBody = vessel.orbitDriver.orbit.referenceBody;
                        var orbit = vessel.orbitDriver.orbit;
                        if (vessel.orbitDriver.orbit.eccentricity > 0.001)
                        {
                            SetOrbitEccentricity(vessel);
                        }
                         */

                        SetOrbitEccentricity(vessel);
                        SetOrbitINC(vessel);
                        //SetOrbitLAN(vessel);
                        SetOrbitSMA(vessel);
                    }
                }
            }
        } // Main Orbit Set

        #endregion 

        #region Set Orbit Subroutines

        public static void SetOrbitSMA(Vessel vessel) // 1.4.0 
        {
            var oldBody = vessel.orbitDriver.orbit.referenceBody;
            var orbit = vessel.orbitDriver.orbit;
            orbit.inclination = vessel.orbit.inclination;
            orbit.eccentricity = vessel.orbit.eccentricity;
            orbit.semiMajorAxis = VesselData.FetchSMA(vessel);
            orbit.LAN = vessel.orbit.LAN;
            orbit.argumentOfPeriapsis = vessel.orbit.argumentOfPeriapsis;
            orbit.meanAnomalyAtEpoch = vessel.orbit.meanAnomalyAtEpoch;
            orbit.epoch = vessel.orbit.epoch;
            orbit.referenceBody = vessel.orbit.referenceBody;
            orbit.Init();

            orbit.UpdateFromUT(HighLogic.CurrentGame.UniversalTime);
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

        public static void SetOrbitEccentricity(Vessel vessel) // 1.4.0 
        {
                var oldBody = vessel.orbitDriver.orbit.referenceBody;
                var orbit = vessel.orbitDriver.orbit;
                orbit.inclination = vessel.orbit.inclination;
                double oldEccentricity = orbit.eccentricity;
                orbit.semiMajorAxis = vessel.GetOrbitDriver().orbit.semiMajorAxis;
                orbit.eccentricity = VesselData.FetchECC(vessel);  //CalculateNewEccentricity(oldEccentricity, orbit.semiMajorAxis, VesselData.FetchSMA(vessel));                
                orbit.LAN = vessel.orbit.LAN;
                orbit.argumentOfPeriapsis = vessel.orbit.argumentOfPeriapsis;
                orbit.meanAnomalyAtEpoch = vessel.orbit.meanAnomalyAtEpoch;
                orbit.epoch = vessel.orbit.epoch;
                orbit.referenceBody = vessel.orbit.referenceBody;
                orbit.Init();
                orbit.UpdateFromUT(HighLogic.CurrentGame.UniversalTime);
                vessel.orbitDriver.pos = vessel.orbit.pos.xzy;
                vessel.orbitDriver.vel = vessel.orbit.vel;
        }

        public static void SetOrbitINC(Vessel vessel) // 1.5.0 
        {
            var oldBody = vessel.orbitDriver.orbit.referenceBody;
            var orbit = vessel.orbitDriver.orbit;
            orbit.inclination = VesselData.FetchINC(vessel);
            orbit.semiMajorAxis = vessel.orbit.semiMajorAxis;
            orbit.eccentricity = vessel.orbit.eccentricity;               
            orbit.LAN = vessel.orbit.LAN;
            orbit.argumentOfPeriapsis = vessel.orbit.argumentOfPeriapsis;
            orbit.meanAnomalyAtEpoch = vessel.orbit.meanAnomalyAtEpoch;
            orbit.epoch = vessel.orbit.epoch;
            orbit.referenceBody = vessel.orbit.referenceBody;
            orbit.Init();
            orbit.UpdateFromUT(HighLogic.CurrentGame.UniversalTime);
            vessel.orbitDriver.pos = vessel.orbit.pos.xzy;
            vessel.orbitDriver.vel = vessel.orbit.vel;
        }

        public static void SetOrbitLAN(Vessel vessel) // 1.5.0 
        {
            var oldBody = vessel.orbitDriver.orbit.referenceBody;
            var orbit = vessel.orbitDriver.orbit;
            orbit.inclination = vessel.orbit.inclination;
            orbit.semiMajorAxis = vessel.orbit.semiMajorAxis;
            orbit.eccentricity = vessel.orbit.eccentricity;
            orbit.LAN = VesselData.FetchLAN(vessel);
            orbit.argumentOfPeriapsis = vessel.orbit.argumentOfPeriapsis;
            orbit.meanAnomalyAtEpoch = vessel.orbit.meanAnomalyAtEpoch;
            orbit.epoch = vessel.orbit.epoch;
            orbit.referenceBody = vessel.orbit.referenceBody;
            orbit.Init();
            orbit.UpdateFromUT(HighLogic.CurrentGame.UniversalTime);
            vessel.orbitDriver.pos = vessel.orbit.pos.xzy;
            vessel.orbitDriver.vel = vessel.orbit.vel;
        }

        #endregion

        #region Misc Calculation Subroutines 

        public static double CalculateNewEccentricity(double OldEccentricity, double OldSMA, double NewSMA) // 1.4.0 needs balancing maybe
        {
            double NewEccentricity = 0.0;
            double FixedSemiMinorAxis = OldSMA * Math.Sqrt(1.0 - (Math.Pow(OldEccentricity, 2.0)));
            NewEccentricity = Math.Sqrt(1.0 - ((Math.Pow(FixedSemiMinorAxis, 2.0)) / Math.Pow((NewSMA), 2.0))); //
            return NewEccentricity;
        }

        #endregion

        #region Decay Simulator

        public static void RealisticDecaySimulator(Vessel vessel) // 1.4.0 Cleanup
        {
            Orbit orbit = vessel.orbitDriver.orbit;
            CelestialBody body = orbit.referenceBody;

            RealisticGravitationalPertubationDecay(vessel); // 1.5.0
            RealisticRadiationDragDecay(vessel); // 1.5.0 Happens everywhere now
            RealisticYarkovskyEffectDecay(vessel); // 1.6.0

            if (body.atmosphere)  
            {
                if (Settings.ReadRD() == true)
                {
                    RealisticAtmosphericDragDecay(vessel);
                }
                else
                {
                    StockAtmosphericDragDecay(vessel);
                }
            }
            
            CheckVesselSurvival(vessel);
        }
        #endregion

        #region Decay Simulator Subroutines

        public static void RealisticAtmosphericDragDecay(Vessel vessel)
        {
            Orbit orbit = vessel.orbitDriver.orbit;
            CelestialBody body = orbit.referenceBody;

                double InitialSemiMajorAxis = VesselData.FetchSMA(vessel);
                double MaxInfluence = body.Radius * 1.5;

                if (InitialSemiMajorAxis < MaxInfluence)
                {
                    double StandardGravitationalParameter = body.gravParameter;
                    double CartesianPositionVectorMagnitude = orbit.getRelativePositionAtT(HighLogic.CurrentGame.UniversalTime).magnitude; // Planetarium to HighLogic
                    double EquivalentAltitude = (InitialSemiMajorAxis - body.Radius);

                    // Eccentricity updating
                    double NewEccentricity = VesselData.FetchECC(vessel);
                    // Still having problems here!
                    // NewEccentricity = CalculateNewEccentricity(VesselData.FetchECC(vessel), InitialSemiMajorAxis, (InitialSemiMajorAxis - (DecayRateRealistic(vessel) / 10)));
                    VesselData.UpdateVesselECC(vessel, NewEccentricity);

                    double Eccentricity = NewEccentricity;

                    if (Eccentricity > 0.085)
                    {
                        double AltitudeAp = (InitialSemiMajorAxis * (1 + Eccentricity) - body.Radius);
                        double AltitudePe = (InitialSemiMajorAxis * (1 - Eccentricity) - body.Radius);
                        EquivalentAltitude = (AltitudePe) + 900.0 * Math.Pow(Eccentricity, (double)0.6);
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

                    double MolecularMass = 27.0 - (0.0012 * ((EquivalentAltitude) - 200.0));
                    double F107Flux = SCSManager.FetchCurrentF107();
                    double GeomagneticIndex = SCSManager.FetchCurrentAp();

                    double ExothericTemperature = 900.0 + (2.5 * (F107Flux - 70.0)) + (1.5 * GeomagneticIndex);
                    double ScaleHeight = ExothericTemperature / MolecularMass;
                    double AtmosphericDensity = (6.0 * (Math.Pow((10.0), -10.0)) * Math.Pow((double)Math.E, -(((double)(EquivalentAltitude) - 175.0f) / (double)ScaleHeight)));

                    double DeltaPeriod = ((3 * Math.PI) * (InitialSemiMajorAxis * AtmosphericDensity) * ((VesselArea * 2.2) / VesselMass)); // Unitless
                    double InitialPeriod = (2 * Math.PI * Math.Sqrt((Math.Pow(InitialSemiMajorAxis, (double)3.0)) / StandardGravitationalParameter));
                    double FinalPeriod = InitialPeriod - DeltaPeriod;
                    double FinalSemiMajorAxis = Math.Pow(((Math.Pow(((double)FinalPeriod / (double)(2 * Math.PI)), (double)2.0)) * (double)StandardGravitationalParameter), ((double)1.0 / (double)3.0));
                    double DecayValue = InitialSemiMajorAxis - (double)FinalSemiMajorAxis;

                    double Multipliers = (double.Parse(TimeWarp.CurrentRate.ToString("F5")) * (double)Settings.ReadDecayDifficulty());

                    VesselData.UpdateVesselSMA(vessel, (InitialSemiMajorAxis - (DecayValue * Multipliers)));
                     
                    // Possibly update vessel LAN too? - 1.5.0
                }
        } // Requires SCS

        public static void StockAtmosphericDragDecay(Vessel vessel)
        {
            Orbit orbit = vessel.orbitDriver.orbit;
            CelestialBody body = orbit.referenceBody;

                double InitialSemiMajorAxis = VesselData.FetchSMA(vessel);
                double MaxInfluence = body.Radius * 1.5;

                if (InitialSemiMajorAxis < MaxInfluence)
                {
                    double StandardGravitationalParameter = body.gravParameter;
                    double CartesianPositionVectorMagnitude = orbit.getRelativePositionAtT(HighLogic.CurrentGame.UniversalTime).magnitude; // Planetarium to HighLogic
                    double EquivalentAltitude = (InitialSemiMajorAxis - body.Radius);

                    // Eccentricity updating
                    double NewEccentricity = VesselData.FetchECC(vessel);
                    // Still having problems here!
                    // NewEccentricity = CalculateNewEccentricity(VesselData.FetchECC(vessel), InitialSemiMajorAxis, (InitialSemiMajorAxis - (DecayRateRealistic(vessel) / 10)));
                    VesselData.UpdateVesselECC(vessel, NewEccentricity);

                    double Eccentricity = NewEccentricity;

                    if (Eccentricity > 0.085)
                    {
                        double AltitudeAp = (InitialSemiMajorAxis * (1 + Eccentricity) - body.Radius);
                        double AltitudePe = (InitialSemiMajorAxis * (1 - Eccentricity) - body.Radius);
                        EquivalentAltitude = (AltitudePe) + 900.0 * Math.Pow(Eccentricity, (double)0.6);
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
                        VesselArea = 5.0;
                    }

                    double DistanceTravelled = InitialOrbitalVelocity; // Meters
                    double VesselMass = VesselData.FetchMass(vessel);   // Kg
                    if (VesselMass == 0)
                    {
                        VesselMass = 1000.0; // Default is 100kg
                    }

                    EquivalentAltitude = (EquivalentAltitude / 1000.0);

                    double AtmosphericDensity = 1.020 * Math.Pow(10.0, 7.0) * Math.Pow(EquivalentAltitude + 70.0, -7.172);
                    double DeltaPeriod = ((3 * Math.PI) * (InitialSemiMajorAxis * AtmosphericDensity) * ((VesselArea * 2.2) / VesselMass)); // Unitless
                    double InitialPeriod = (2 * Math.PI * Math.Sqrt((Math.Pow(InitialSemiMajorAxis, (double)3.0)) / StandardGravitationalParameter));
                    double FinalPeriod = InitialPeriod - DeltaPeriod;
                    double FinalSemiMajorAxis = Math.Pow(((Math.Pow(((double)FinalPeriod / (double)(2 * Math.PI)), (double)2.0)) * (double)StandardGravitationalParameter), ((double)1.0 / (double)3.0));
                    double DecayValue = InitialSemiMajorAxis - (double)FinalSemiMajorAxis;
                    double Multipliers = (double.Parse(TimeWarp.CurrentRate.ToString("F5")) * (double)Settings.ReadDecayDifficulty());

                    VesselData.UpdateVesselSMA(vessel, (InitialSemiMajorAxis - (DecayValue * Multipliers)));
                }
        } // 1.4.0

        public static void RealisticRadiationDragDecay(Vessel vessel)
        {
            Orbit orbit = vessel.orbitDriver.orbit;
            CelestialBody body = orbit.referenceBody;

            double SolarEnergy = (double)Math.Pow(((double)3.86 * (double)10.0), (double)26); // W
            double SolarDistance = 0.0;
            if (vessel.orbitDriver.orbit.referenceBody.GetName() != "Sun")
            {
                SolarDistance = body.orbitDriver.orbit.altitude; // m
            }

            else
            {
                SolarDistance = vessel.orbitDriver.orbit.altitude;
            }

            double SolarConstant = SolarEnergy / ((double)4.0 * (double)Math.PI * Math.Pow((double)SolarDistance, (double)2.0)); // W/m^2
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
                VesselMass = 100.0;
            }

            double VesselRadius = Math.Sqrt((double)VesselArea / (double)Math.PI);
            double ImmobileAccelleration = (Math.PI * (VesselRadius * VesselRadius) * SolarConstant) / (VesselMass * SpeedOfLight * (SolarDistance * SolarDistance));
            double ChangeInSemiMajorAxis = -(6.0 * Math.PI * ImmobileAccelleration * (InitialSemiMajorAxis)) / (MeanAngularVelocity * SpeedOfLight);
            double FinalSemiMajorAxis = InitialSemiMajorAxis + ChangeInSemiMajorAxis;

            VesselData.UpdateVesselSMA(vessel, (InitialSemiMajorAxis + (ChangeInSemiMajorAxis * TimeWarp.CurrentRate * Settings.ReadDecayDifficulty())));
        }

        public static void RealisticGravitationalPertubationDecay(Vessel vessel) // 1.5.0 
        {
            Orbit orbit = vessel.orbitDriver.orbit;
            CelestialBody body = orbit.referenceBody;

                double GravitationalConstant = 6.67408 * Math.Pow(10.0, -11.0); // G [Newton Meter Squared per Square Kilogram]
                double ForceAtSurface = (GravitationalConstant * vessel.GetTotalMass() * vessel.orbitDriver.orbit.referenceBody.Mass);
                double ForceAtDistance = (GravitationalConstant * vessel.GetTotalMass() * vessel.orbitDriver.orbit.referenceBody.Mass) / (Math.Pow(vessel.orbitDriver.orbit.altitude, 2.0));
                if (ForceAtDistance > (0.0000000000001 * ForceAtSurface)) // Stops distant laggy pertubations
                {
                    if (TimeWarp.CurrentRate < 100)
                    {
                        if (MasConData.CheckMasConProximity(vessel))
                        {
                            VesselData.UpdateVesselSMA(vessel, VesselData.FetchSMA(vessel) + MasConManager.GetCalculatedSMAChange(vessel, orbit.LAN, orbit.meanAnomalyAtEpoch, orbit.argumentOfPeriapsis, orbit.eccentricity, orbit.inclination, orbit.semiMajorAxis, orbit.epoch) * TimeWarp.CurrentRate);
                            VesselData.UpdateVesselINC(vessel, VesselData.FetchINC(vessel) + MasConManager.GetCalculatedINCChange(vessel, orbit.LAN, orbit.meanAnomalyAtEpoch, orbit.argumentOfPeriapsis, orbit.eccentricity, orbit.inclination, orbit.semiMajorAxis, orbit.epoch));
                            VesselData.UpdateVesselECC(vessel, VesselData.FetchECC(vessel) + MasConManager.GetCalculatedINCChange(vessel, orbit.LAN, orbit.meanAnomalyAtEpoch, orbit.argumentOfPeriapsis, orbit.eccentricity, orbit.inclination, orbit.semiMajorAxis, orbit.epoch));
                            VesselData.UpdateVesselLAN(vessel, VesselData.FetchLAN(vessel) + MasConManager.GetCalculatedLANChange(vessel, orbit.LAN, orbit.meanAnomalyAtEpoch, orbit.argumentOfPeriapsis, orbit.eccentricity, orbit.inclination, orbit.semiMajorAxis, orbit.epoch));
                            //print("Change In MNA from Mascon: " + MasConManager.GetCalculatedMNAChange(vessel, orbit.LAN, orbit.meanAnomalyAtEpoch, orbit.argumentOfPeriapsis, orbit.eccentricity, orbit.inclination, orbit.semiMajorAxis, orbit.epoch));
                        }
                    }

                    else
                    {
                        if (MasConData.CheckMasConProximity(vessel))
                        {
                            VesselData.UpdateVesselSMA(vessel, VesselData.FetchSMA(vessel) + MasConManager.GetSecularSMAChange(vessel, orbit.LAN, orbit.meanAnomalyAtEpoch, orbit.argumentOfPeriapsis, orbit.eccentricity, orbit.inclination, orbit.semiMajorAxis, orbit.epoch));
                            VesselData.UpdateVesselINC(vessel, VesselData.FetchINC(vessel) + MasConManager.GetSecularIncChange(vessel, orbit.LAN, orbit.meanAnomalyAtEpoch, orbit.argumentOfPeriapsis, orbit.eccentricity, orbit.inclination, orbit.semiMajorAxis, orbit.epoch));
                            VesselData.UpdateVesselECC(vessel, VesselData.FetchECC(vessel) + MasConManager.GetSecularECCChange(vessel, orbit.LAN, orbit.meanAnomalyAtEpoch, orbit.argumentOfPeriapsis, orbit.eccentricity, orbit.inclination, orbit.semiMajorAxis, orbit.epoch));
                            VesselData.UpdateVesselLAN(vessel, MasConManager.GetSecularLANChange(vessel, orbit.LAN, orbit.meanAnomalyAtEpoch, orbit.argumentOfPeriapsis, orbit.eccentricity, orbit.inclination, orbit.semiMajorAxis, orbit.epoch));
                        }
                }
            }
        }

        public static void RealisticYarkovskyEffectDecay(Vessel vessel) // 1.5.0 
        {

        }

        #endregion

        #region Old Stock 

        /*
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

            if (vessel.GetOrbitDriver().orbit.semiMajorAxis + 50 < MaxDecayInfluence)
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

                double DistanceMultiplier = Math.Pow(Math.E, ((vessel.orbitDriver.orbit.referenceBody.atmosphereDepth/1000) / ((VesselData.FetchSMA(vessel) - vessel.orbitDriver.orbit.referenceBody.Radius) / 1000)));

                DecayValue = TimeWarp.CurrentRate * AtmosphereMultiplier * vessel.orbitDriver.orbit.referenceBody.GeeASL * 0.5 * (1.0 / (Mass / 1000.0)) * Area * DistanceMultiplier;
                //DecayValue = (double)TimeWarp.CurrentRate * Sigma * BodyGravityConstant * AtmosphereMultiplier * Lambda * Area * (Mass) * (2.509 * Math.Pow(10.0, -4.0)) * DistanceMultiplier; // 1.0.9 Update
            }
            else
            {
                DecayValue = 0.0;
            }

            double DecayRateModifier = 0.0;
            DecayRateModifier = Settings.ReadDecayDifficulty();

            DecayValue = DecayValue * DecayRateModifier;// Decay Rate Modifier from Settings 
            VesselDied = false;
            CheckVesselSurvival(vessel);

            if (VesselDied == false)         // Just Incase the vessel is destroyed part way though the check.
            {
                if (vessel.orbitDriver.orbit.referenceBody.GetInstanceID() != 0 || vessel.GetOrbitDriver().orbit.semiMajorAxis > vessel.orbitDriver.orbit.referenceBody.Radius + 5)
                {
                    VesselData.UpdateVesselSMA(vessel, ((float)VesselData.FetchSMA(vessel) - (float)DecayValue));
                }
            }
            CheckVesselSurvival(vessel);
        }
        */

        #endregion  

        #region Survival Checks 
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

                    if (VesselData.FetchSMA(vessel) < vessel.orbitDriver.orbit.referenceBody.Radius + (vessel.orbitDriver.referenceBody.atmosphereDepth / (double)2.0)) // 1.5.0 Increased Tolerance
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
        #endregion

        #region Active Decay Subroutines

        public static void ActiveDecayRealistic(Vessel vessel)            // 1.4.0 Use Rigidbody.addForce
        { 
            double ReadTime = HighLogic.CurrentGame.UniversalTime;
            double DecayValue = DecayRateAtmosphericDrag(vessel);
            double InitialVelocity = vessel.orbitDriver.orbit.getOrbitalVelocityAtUT(ReadTime).magnitude;
            double CalculatedFinalVelocity = 0.0;
            Orbit newOrbit = vessel.orbitDriver.orbit;
            newOrbit.semiMajorAxis = (VesselData.FetchSMA(vessel) - DecayValue);
            CalculatedFinalVelocity = newOrbit.getOrbitalVelocityAtUT(ReadTime).magnitude;

            double DeltaVelocity = InitialVelocity - CalculatedFinalVelocity;
            double decayForce = DeltaVelocity * (vessel.GetTotalMass() * 1000);
            GameObject thisVessel = new GameObject();

            if (TimeWarp.CurrentRate == 0 || (TimeWarp.CurrentRate > 0  && TimeWarp.WarpMode == TimeWarp.Modes.LOW))
            {
                if (vessel.vesselType != VesselType.EVA)
                {
                    foreach (Part p in vessel.parts)
                    {
                        if ((p.physicalSignificance == Part.PhysicalSignificance.FULL) &&
                            (p.Rigidbody != null))
                        {
                           // p.Rigidbody.AddForce(Vector3d.back * (decayForce)); // 1.5.0
                        }
                    }

                    VesselData.UpdateVesselSMA(vessel, newOrbit.semiMajorAxis);
                }
            }

            else if (TimeWarp.CurrentRate > 0 && TimeWarp.WarpMode == TimeWarp.Modes.HIGH) // 1.3.0 Timewarp Fix
            {
                bool MultipleLoadedSceneVessels = false; // 1.4.0 Debris warp fix
                MultipleLoadedSceneVessels = CheckVesselProximity(vessel);

                if (MultipleLoadedSceneVessels == false)
                {
                    if (vessel.vesselType != VesselType.EVA)
                    {
                        VesselData.UpdateVesselSMA(vessel, (VesselData.FetchSMA(vessel) - DecayValue));
                        CatchUpOrbit(vessel); 
                    }
                }
            }
        }

        public static void ActiveDecayStock(Vessel vessel)
        {
            double ReadTime = HighLogic.CurrentGame.UniversalTime;
            double DecayValue = DecayRateAtmosphericDrag(vessel);
            double InitialVelocity = vessel.orbitDriver.orbit.getOrbitalVelocityAtUT(ReadTime).magnitude;
            double CalculatedFinalVelocity = 0.0;
            Orbit newOrbit = vessel.orbitDriver.orbit;
            newOrbit.semiMajorAxis = (VesselData.FetchSMA(vessel) - DecayValue);
            CalculatedFinalVelocity = newOrbit.getOrbitalVelocityAtUT(ReadTime).magnitude;
            double DeltaVelocity = InitialVelocity - CalculatedFinalVelocity;
            double decayForce = DeltaVelocity * (vessel.GetTotalMass() /1000.0);
            GameObject thisVessel = new GameObject();

            if (TimeWarp.CurrentRate == 0 || (TimeWarp.CurrentRate > 0 && TimeWarp.WarpMode == TimeWarp.Modes.LOW))
            {
                if (vessel.vesselType != VesselType.EVA)
                {
                    foreach (Part p in vessel.parts)
                    {
                        if ((p.physicalSignificance == Part.PhysicalSignificance.FULL) &&
                            (p.Rigidbody != null))
                        {
                           // p.Rigidbody.AddForce((Vector3d.back * (decayForce))); // 1.5.0 Too Fast Still
                        }
                    }
                    VesselData.UpdateVesselSMA(vessel, newOrbit.semiMajorAxis);
                }

            }

            else if (TimeWarp.CurrentRate > 0 && TimeWarp.WarpMode == TimeWarp.Modes.HIGH) // 1.3.0 Timewarp Fix
            {
                    bool MultipleLoadedSceneVessels = false; // 1.4.0 Debris warp fix
                    MultipleLoadedSceneVessels = CheckVesselProximity(vessel);

                    if (MultipleLoadedSceneVessels == false)
                    {
                        if (vessel.vesselType != VesselType.EVA)
                        {
                            VesselData.UpdateVesselSMA(vessel, (VesselData.FetchSMA(vessel) - DecayValue));
                            CatchUpOrbit(vessel);
                        }
                    }
                }
        }

        #endregion

        #region Decay Rate Subroutines

        public static double DecayRateRadiationPressure(Vessel vessel)
        {
            double DecayRate = 0.0;
            CelestialBody body = vessel.orbitDriver.orbit.referenceBody;
            double SolarEnergy = (double)Math.Pow(((double)3.86 * (double)10.0), (double)26.0); // W
            double SolarDistance = body.orbitDriver.orbit.altitude; // m
            double SolarConstant = 0.0;
            SolarConstant = SolarEnergy / ((double)4.0 * (double)Math.PI * (double)Math.Pow((double)SolarDistance, (double)2.0)); // W/m^2
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
                VesselMass = 100.0;
            }

            double VesselRadius = (double)Math.Sqrt((double)VesselArea / (double)Math.PI);
            double ImmobileAccelleration = (Math.PI * (VesselRadius * VesselRadius) * SolarConstant) / (VesselMass * SpeedOfLight * (SolarDistance * SolarDistance));
            double ChangeInSemiMajorAxis = -(6.0 * Math.PI * ImmobileAccelleration * (InitialSemiMajorAxis)) / (MeanAngularVelocity * SpeedOfLight);

            double DecayRateModifier = 0.0;
            DecayRateModifier = Settings.ReadDecayDifficulty();
            DecayRate = ((ChangeInSemiMajorAxis) * TimeWarp.CurrentRate * DecayRateModifier);

            return DecayRate;
        }

        public static double DecayRateAtmosphericDrag(Vessel vessel) // Removed floats 
        {
            double DecayRate = 0.0;
            Orbit orbit = vessel.orbitDriver.orbit;
            CelestialBody body = orbit.referenceBody;

            if (Settings.ReadRD() == true)
            {
                if (body.atmosphere == true)  // Atmospheric Drag // Disused for 1.1.0
                {
                    double InitialSemiMajorAxis = VesselData.FetchSMA(vessel);
                    double MaxInfluence = body.Radius * 1.5;

                    if (InitialSemiMajorAxis < MaxInfluence)
                    {
                        double StandardGravitationalParameter = body.gravParameter;
                        double CartesianPositionVectorMagnitude = orbit.getRelativePositionAtT(HighLogic.CurrentGame.UniversalTime).magnitude; // Planetarium
                        double EquivalentAltitude = (InitialSemiMajorAxis - body.Radius);
                        double Eccentricity = VesselData.FetchECC(vessel);

                        if (Eccentricity > 0.085)
                        {
                            double AltitudeAp = (InitialSemiMajorAxis * (1 + Eccentricity) - body.Radius);
                            double AltitudePe = (InitialSemiMajorAxis * (1 - Eccentricity) - body.Radius);
                            EquivalentAltitude = (AltitudePe) + 900 * Math.Pow(Eccentricity, (double)0.6);
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

                        double MolecularMass = 27 - 0.0012 * ((EquivalentAltitude) - 200);
                        double F107Flux = SCSManager.FetchCurrentF107();
                        double GeomagneticIndex = SCSManager.FetchCurrentAp();

                        double ExothericTemperature = 900.0 + (2.5 * (F107Flux - 70.0)) + (1.5 * GeomagneticIndex);
                        double ScaleHeight = ExothericTemperature / MolecularMass;
                        double AtmosphericDensity = (6.0 * (Math.Pow((10.0), -10.0)) * Math.Pow(Math.E, -(((EquivalentAltitude) - 175.0f) / ScaleHeight)));

                        double DeltaPeriod = ((3 * Math.PI) * (InitialSemiMajorAxis * AtmosphericDensity) * ((VesselArea * 2.2) / VesselMass)); // Unitless
                        double InitialPeriod = (2 * Math.PI * Math.Sqrt((Math.Pow(InitialSemiMajorAxis, (double)3)) / StandardGravitationalParameter));
                        double FinalPeriod = InitialPeriod - DeltaPeriod;
                        double FinalSemiMajorAxis = Math.Pow(((Math.Pow(((double)FinalPeriod / (double)(2.0 * Math.PI)), (double)2.0)) * (double)StandardGravitationalParameter), ((double)1.0 / (double)3.0));

                        double DecayRateModifier = 0.0;
                        DecayRateModifier = Settings.ReadDecayDifficulty();

                        DecayRate = (InitialSemiMajorAxis - FinalSemiMajorAxis) * TimeWarp.CurrentRate * DecayRateModifier;
                    }
                }
            }
            else
            {
                if (body.atmosphere == true)  // Atmospheric Drag // Disused for 1.1.0
                {
                    double InitialSemiMajorAxis = VesselData.FetchSMA(vessel);
                    double MaxInfluence = body.Radius * 1.5;

                    if (InitialSemiMajorAxis < MaxInfluence)
                    {
                        double StandardGravitationalParameter = body.gravParameter;
                        double CartesianPositionVectorMagnitude = orbit.getRelativePositionAtT(HighLogic.CurrentGame.UniversalTime).magnitude; // Planetarium
                        double EquivalentAltitude = (InitialSemiMajorAxis - body.Radius);
                        double Eccentricity = VesselData.FetchECC(vessel);

                        if (Eccentricity > 0.085)
                        {
                            double AltitudeAp = (InitialSemiMajorAxis * (1 + Eccentricity) - body.Radius);
                            double AltitudePe = (InitialSemiMajorAxis * (1 - Eccentricity) - body.Radius);
                            EquivalentAltitude = (AltitudePe) + 900 * Math.Pow(Eccentricity, (double)0.6);
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
                            VesselArea = 5.0;
                        }

                        double DistanceTravelled = InitialOrbitalVelocity; // Meters
                        double VesselMass = VesselData.FetchMass(vessel);   // Kg
                        if (VesselMass == 0)
                        {
                            VesselMass = 1000.0; // Default is 100kg
                        }

                        EquivalentAltitude = EquivalentAltitude / 1000.0;

                        double AtmosphericDensity = 1.020 * (Math.Pow(10, 7.0) * Math.Pow(((EquivalentAltitude + 70.0)), -7.172)); // Kg/m^3 // *1
                        double DeltaPeriod = ((3 * Math.PI) * (InitialSemiMajorAxis * AtmosphericDensity) * ((VesselArea * 2.2) / VesselMass)); // Unitless
                        double InitialPeriod = (2 * Math.PI * Math.Sqrt((Math.Pow(InitialSemiMajorAxis, (double)3)) / StandardGravitationalParameter));
                        double FinalPeriod = InitialPeriod - DeltaPeriod;
                        double FinalSemiMajorAxis = Math.Pow(((Math.Pow(((double)FinalPeriod / (double)(2.0 * Math.PI)), (double)2.0)) * (double)StandardGravitationalParameter), ((double)1.0 / (double)3.0));

                        double DecayRateModifier = 0.0;
                        DecayRateModifier = Settings.ReadDecayDifficulty();

                        DecayRate = (InitialSemiMajorAxis - FinalSemiMajorAxis) * TimeWarp.CurrentRate * DecayRateModifier;
                    }
                }
            }

            return DecayRate;
        }

        public static double DecayRateGravitationalPertubation(Vessel vessel)
        {
            Orbit orbit = vessel.orbitDriver.orbit;
            CelestialBody body = orbit.referenceBody;
            double DecayRate = 0.0;

            double GravitationalConstant = 6.67408 * Math.Pow(10.0, -11.0); // G [Newton Meter Squared per Square Kilogram]
            double ForceAtSurface = (GravitationalConstant * vessel.GetTotalMass() * vessel.orbitDriver.orbit.referenceBody.Mass);
            double ForceAtDistance = (GravitationalConstant * vessel.GetTotalMass() * vessel.orbitDriver.orbit.referenceBody.Mass) / (Math.Pow(vessel.orbitDriver.orbit.altitude, 2.0));
            if (ForceAtDistance > (0.0000000000001 * ForceAtSurface)) // Stops distant laggy pertubations
            {
                if (vessel.isActiveVessel)
                {
                    if (MasConData.CheckMasConProximity(vessel))
                    {
                        DecayRate = MasConManager.GetCalculatedSMAChange(vessel, orbit.LAN, orbit.meanAnomalyAtEpoch, orbit.argumentOfPeriapsis, orbit.eccentricity, orbit.inclination, orbit.semiMajorAxis, orbit.epoch) * TimeWarp.CurrentRate;
                    }
                }

                else
                {
                    if (MasConData.CheckMasConProximity(vessel))
                    {
                        DecayRate = MasConManager.GetSecularSMAChange(vessel, orbit.LAN, orbit.meanAnomalyAtEpoch, orbit.argumentOfPeriapsis, orbit.eccentricity, orbit.inclination, orbit.semiMajorAxis, orbit.epoch);
                    }
                }
            }

            return DecayRate;
        }

        public static double DecayRateYarkovskyEffect(Vessel vessel)
        {
            double DecayRate = 0.0;
            return DecayRate;
        }

#endregion 

        #region Timing Subroutines

        public static double DecayTimePredictionExponentialsVariables(Vessel vessel)
        {
            double DaysUntilDecay = 0;
            double InitialSemiMajorAxis = VesselData.FetchSMA(vessel);
            Orbit orbit = vessel.GetOrbitDriver().orbit;
            CelestialBody body = vessel.orbitDriver.orbit.referenceBody;
            double InitialPeriod = Math.PI * 2.0 * (Math.Sqrt((InitialSemiMajorAxis * InitialSemiMajorAxis * InitialSemiMajorAxis) / body.gravParameter));

            double EquivalentAltitude = (InitialSemiMajorAxis - body.Radius);
            if (orbit.eccentricity > 0.085)
            {
                double AltitudeAp = (InitialSemiMajorAxis * (1 - orbit.eccentricity) - body.Radius);
                double AltitudePe = (InitialSemiMajorAxis * (1 + orbit.eccentricity) - body.Radius);
                EquivalentAltitude = (AltitudePe) + 900 * Math.Pow(orbit.eccentricity, (double)0.6);
            }

            double BaseAltitude = body.atmosphereDepth / 1000;

            double MolecularMass = 27 - 0.0012 * ((EquivalentAltitude / 1000) - 200);
            double F107Flux = SCSManager.FetchAverageF107();
            double GeomagneticIndex = SCSManager.FetchAverageAp();

            double ExothericTemperature = 900.0 + (2.5 * (F107Flux - 70)) + (1.5 * GeomagneticIndex);
            double ScaleHeight = ExothericTemperature / MolecularMass;
            double AtmosphericDensity =0;

            if (Settings.ReadRD() == false)
            {
                AtmosphericDensity = 1.020 * Math.Pow(10.0, 7.0) * Math.Pow(EquivalentAltitude / 1000.0 + 70.0, -7.172); // 1.4.2
            }
            else if (Settings.ReadRD() == true)
            {
                AtmosphericDensity = (6 * (Math.Pow((10), -10)) * Math.Pow(Math.E, -(((EquivalentAltitude / 1000) - 175.0f) / ScaleHeight)));
            }

            double Beta = 1.0 / ScaleHeight;

            double VesselArea = VesselData.FetchArea(vessel);
            if (VesselArea == 0)
            {
                VesselArea = 5.0;
            }

            double VesselMass = VesselData.FetchMass(vessel);   // Kg
            if (VesselMass == 0)
            {
                VesselMass = 1000.0;
            }

            EquivalentAltitude = EquivalentAltitude + body.Radius;


            double Time1 = ((InitialPeriod / (60.0 * 60.0)) / 4.0 * Math.PI) * (((2.0 * Beta * EquivalentAltitude) + 1.0) / (AtmosphericDensity * (Beta * Beta) * (EquivalentAltitude * EquivalentAltitude * EquivalentAltitude)));
            double Time2 = Time1 * (VesselMass / (2.2 * VesselArea)) * (1 - Math.Pow(Math.E, (Beta * (BaseAltitude - ((EquivalentAltitude - body.Radius) / 1000)))));

            DaysUntilDecay = Time2;

            return DaysUntilDecay;
        } // 1.4.0

        public static double DecayTimePredictionLinearVariables(Vessel vessel)
        {
            double DecayTimeInSeconds = 0.0;

            double DecayRateVariables = Math.Abs(DecayManager.DecayRateRadiationPressure(vessel)) + Math.Abs(DecayManager.DecayRateYarkovskyEffect(vessel)); //+ Math.Abs(DecayRateGravitationalPertubation(vessel));
            double TimewarpRate = 0;
            if (TimeWarp.CurrentRate == 0)
            {
                TimewarpRate = 1;
            }
            else
            {
                TimewarpRate = TimeWarp.CurrentRate;
            }
            double TimeUntilImpact = Math.Abs((VesselData.FetchSMA(vessel) - vessel.orbitDriver.orbit.referenceBody.Radius) / (DecayRateVariables/TimewarpRate));
            DecayTimeInSeconds = TimeUntilImpact;
            return DecayTimeInSeconds;
        }

        #endregion
    }
}