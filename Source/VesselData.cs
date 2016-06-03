
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
    public class VesselData : MonoBehaviour
    {
        public static ConfigNode VesselInformation = new ConfigNode();
        public static string FilePath = KSPUtil.ApplicationRootPath + "GameData/WhitecatIndustries/Orbital Decay/PluginData/VesselData.cfg";
        public static ConfigNode File = ConfigNode.Load(FilePath);

        public static double EndSceneWaitTime = 0;
        public static double StartSceneWaitTime = 0;
        public static bool VesselMovementUpdate = false;
        public static bool VesselMoving = false;
        public static double TimeOfLastMovement = 0.0;
        public static bool ClearedOld = false;
        private float UPTInterval = 1.0f;
        private float lastUpdate = 0.0f;

        public void Awake()
        {
            VesselInformation.ClearNodes();

            if (HighLogic.LoadedSceneIsGame && (HighLogic.LoadedScene != GameScenes.LOADING || HighLogic.LoadedScene != GameScenes.LOADINGBUFFER))
            {
                if (System.IO.File.ReadAllLines(FilePath).Length == 0)
                {
                    ConfigNode FileM = new ConfigNode();
                    ConfigNode FileN = new ConfigNode("VESSEL");
                    FileN.AddValue("name", "WhitecatsDummyVessel");
                    FileN.AddValue("id", "000");
                    FileN.AddValue("persistence", "WhitecatsDummySaveFileThatNoOneShouldNameTheirSave");
                    FileM.AddNode(FileN);
                    FileM.Save(FilePath);
                }

                File = ConfigNode.Load(FilePath);

                if (File.nodes.Count > 0)
                {
                    foreach (ConfigNode vessel in File.GetNodes("VESSEL"))
                    {
                        string Persistence = vessel.GetValue("persistence");
                        if (Persistence == HighLogic.SaveFolder.ToString() || Persistence == "WhitecatsDummySaveFileThatNoOneShouldNameTheirSave")
                        {
                            VesselInformation.AddNode(vessel);
                        }
                    }
                }
            }
            print("WhitecatIndustries - Orbital Decay - Loaded vessel data.");
        }

        public void FixedUpdate()
        {
            if (Time.timeSinceLevelLoad > 0.3)
            {
                if ((Time.time - lastUpdate) > UPTInterval) // 1.4.0 Lag Busting
                {
                    lastUpdate = Time.time;

                    if (HighLogic.LoadedSceneIsGame && (HighLogic.LoadedScene != GameScenes.LOADING && HighLogic.LoadedScene != GameScenes.LOADINGBUFFER && HighLogic.LoadedScene != GameScenes.MAINMENU))
                    {
                        Vessel vessel = new Vessel();
                        for (int i = 0; i < FlightGlobals.Vessels.Count; i++)
                        {
                            vessel = FlightGlobals.Vessels.ElementAt(i);
                            {
                                if (CheckIfContained(vessel) == true)
                                {
                                    if (vessel.situation != Vessel.Situations.SUB_ORBITAL && vessel.situation != Vessel.Situations.ORBITING)
                                    {
                                        ClearVesselData(vessel);
                                    }

                                    if (vessel.situation == Vessel.Situations.ORBITING || vessel.situation == Vessel.Situations.SUB_ORBITAL)
                                    {
                                        WriteVesselData(vessel);
                                    }
                                }
                                else if (CheckIfContained(vessel) == false)
                                {
                                    if (vessel.situation == Vessel.Situations.ORBITING || vessel.situation == Vessel.Situations.SUB_ORBITAL) // 1.4.2
                                    {
                                        WriteVesselData(vessel);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public void OnDestroy()
        {
            if (DecayManager.CheckSceneStateMain(HighLogic.LoadedScene))
            {
                if ((Planetarium.GetUniversalTime() == HighLogic.CurrentGame.UniversalTime) || HighLogic.LoadedScene == GameScenes.FLIGHT)
                {
                    print("WhitecatIndustries - Orbital Decay - Vessel Information saved. Ondestroy");
                    File.ClearNodes();
                    VesselInformation.Save(FilePath);
                   // VesselInformation.ClearNodes();
                }
            }
        }

        public static void OnQuickSave()
        {
            if (DecayManager.CheckSceneStateMain(HighLogic.LoadedScene))
            {
                if ((Planetarium.GetUniversalTime() == HighLogic.CurrentGame.UniversalTime) || HighLogic.LoadedScene == GameScenes.FLIGHT)
                {
                    print("WhitecatIndustries - Orbital Decay - Vessel Information saved.");
                    File.ClearNodes();
                    VesselInformation.Save(FilePath);
                    VesselInformation.ClearNodes();
                }
            }
        }

        public static bool CheckIfContained(Vessel vessel)
        {
            bool Contained = false;

            foreach (ConfigNode node in VesselInformation.GetNodes("VESSEL"))
            {
                if (node.GetValue("id") == vessel.id.ToString())
                {
                    Contained = true;
                }
            }
            return Contained;
        }

        public static void WriteVesselData(Vessel vessel)
        {
            if (CheckIfContained(vessel) == false)
            {
                ConfigNode vesselData = BuildConfigNode(vessel);
                VesselInformation.AddNode(vesselData);
            }

            if (CheckIfContained(vessel) == true)
            {
                if (vessel == FlightGlobals.ActiveVessel)
                {
                    if (FlightGlobals.ActiveVessel.geeForce > 0.01) // Checks if a vessel is still moving between orbits (Average GForce around 0.0001)
                    {
                        VesselMovementUpdate = false;
                        VesselMoving = true;
                        TimeOfLastMovement = HighLogic.CurrentGame.UniversalTime;
                    }
                    else
                    {
                        VesselMoving = false;
                    }

                    if (VesselMoving == false && (HighLogic.CurrentGame.UniversalTime - TimeOfLastMovement) < 1 && VesselMovementUpdate == false)
                    {
                        UpdateVesselSMA(vessel, vessel.orbitDriver.orbit.semiMajorAxis);
                        UpdateVesselECC(vessel, vessel.orbitDriver.orbit.eccentricity);
                        UpdateVesselINC(vessel, vessel.orbitDriver.orbit.inclination);
                        UpdateVesselEPH(vessel, vessel.orbitDriver.orbit.epoch);
                        UpdateVesselLAN(vessel, vessel.orbitDriver.orbit.LAN);
                        UpdateVesselMNA(vessel, vessel.orbitDriver.orbit.meanAnomalyAtEpoch);
                        UpdateVesselLPE(vessel, vessel.orbitDriver.orbit.argumentOfPeriapsis);

                        VesselMovementUpdate = true;
                    }
                }
            }

        }

        public static void UpdateActiveVesselData(Vessel vessel)
        {
            ConfigNode VesselNode = new ConfigNode("VESSEL");
            bool found = false;

            foreach (ConfigNode node in VesselInformation.GetNodes("VESSEL"))
            {
                if (node.GetValue("id") == vessel.id.ToString())
                {
                    VesselNode = node;
                    found = true;
                    break;
                }
            }

            if (found == true)
            {
                VesselNode.SetValue("Mass", (vessel.GetTotalMass() * 1000).ToString());
                VesselNode.SetValue("Area", (CalculateVesselArea(vessel)).ToString());
            }
        }

        public static void ClearVesselData(Vessel vessel)
        {
            ConfigNode VesselNode = new ConfigNode("VESSEL");
            bool found = false;

            foreach (ConfigNode node in VesselInformation.GetNodes("VESSEL"))
            {
                if (node.GetValue("id") == vessel.id.ToString())
                {
                    VesselNode = node;
                    found = true;
                    break;
                }
            }

            if (found == true)
            {
                VesselInformation.RemoveNode(VesselNode);
            }
        }

        public static ConfigNode BuildConfigNode(Vessel vessel)
        {
            ConfigNode newVessel = new ConfigNode("VESSEL");
            newVessel.AddValue("name", vessel.GetName());
            newVessel.AddValue("id", vessel.id.ToString());
            newVessel.AddValue("persistence", HighLogic.SaveFolder.ToString());
            string CatalogueCode = vessel.vesselType.ToString().Substring(0, 1) + vessel.GetInstanceID().ToString();
            newVessel.AddValue("code", CatalogueCode);
            if (vessel == FlightGlobals.ActiveVessel)
            {
                newVessel.AddValue("Mass", vessel.GetTotalMass() * 1000); // 1.1.0 in kilograms!
                newVessel.AddValue("Area", CalculateVesselArea(vessel)); // Try?
            }
            else
            {
                newVessel.AddValue("Mass", vessel.GetTotalMass() * 1000); // Try "1"
                newVessel.AddValue("Area", CalculateVesselArea(vessel)); // Still getting bugs here
            }
            newVessel.AddValue("ReferenceBody", vessel.orbitDriver.orbit.referenceBody.GetName());
            newVessel.AddValue("SMA", vessel.GetOrbitDriver().orbit.semiMajorAxis);

            newVessel.AddValue("ECC", vessel.GetOrbitDriver().orbit.eccentricity);       // 1.4.0 greater information.
            newVessel.AddValue("INC", vessel.GetOrbitDriver().orbit.inclination);
            newVessel.AddValue("LPE", vessel.GetOrbitDriver().orbit.argumentOfPeriapsis);
            newVessel.AddValue("LAN", vessel.GetOrbitDriver().orbit.LAN);
            newVessel.AddValue("MNA", vessel.GetOrbitDriver().orbit.meanAnomalyAtEpoch);
            newVessel.AddValue("EPH", vessel.GetOrbitDriver().orbit.epoch);

           // newVessel.AddValue("StationKeeping", false.ToString());
            //151newVessel.AddValue("Fuel", ResourceManager.GetResources(vessel, ResourceName));
        //    newVessel.AddValue("Fuel", ResourceManager.GetResources(vessel));//151
        //    newVessel.AddValue("Resource", ResourceManager.GetResourceNames(vessel));//151

            return newVessel;
        }

        public static bool FetchStationKeeping(Vessel vessel)
        {
            bool StationKeeping = false;
            if (vessel == FlightGlobals.ActiveVessel)
            {
                List<ModuleOrbitalDecay> modlist = vessel.FindPartModulesImplementing<ModuleOrbitalDecay>();
                foreach (ModuleOrbitalDecay module in modlist)
                {
                    StationKeeping = module.stationKeepData.IsStationKeeping;
                    break;
                }

            }
            else
            {
                ProtoVessel proto = vessel.protoVessel;

                foreach (ProtoPartSnapshot protopart in proto.protoPartSnapshots)
                {
                    foreach (ProtoPartModuleSnapshot protopartmodulesnapshot in protopart.modules)
                    {
                        if (protopartmodulesnapshot.moduleName == "ModuleOrbitalDecay")
                        {
                            ConfigNode node = protopartmodulesnapshot.moduleValues.GetNode("stationKeepData");
                            StationKeeping = bool.Parse(node.GetValue("IsStationKeeping"));
                            break;
                        }
                    }
                }
            }

            return StationKeeping;
        }


        public static double FetchFuelLost()
        {
            List<ModuleOrbitalDecay> modlist = FlightGlobals.ActiveVessel.FindPartModulesImplementing<ModuleOrbitalDecay>();
            double FuelLost = modlist.ElementAt(0).stationKeepData.fuelLost;
            return FuelLost;
        }


        public static void SetFuelLost(double FuelLost)
        {

            List<ModuleOrbitalDecay> modlist = FlightGlobals.ActiveVessel.FindPartModulesImplementing<ModuleOrbitalDecay>();
            foreach(ModuleOrbitalDecay module in modlist )
            {
                module.stationKeepData.fuelLost = FuelLost;
            }
        }


        public static double FetchMass(Vessel vessel)
        {
            ConfigNode Data = VesselInformation;
            bool Vesselfound = false;
            double Mass = 0.0;

            foreach (ConfigNode Vessel in Data.GetNodes("VESSEL"))
            {
                string id = Vessel.GetValue("id");
                if (id == vessel.id.ToString())
                {
                    Vesselfound = true;
                }

                if (Vesselfound == true)
                {
                    Mass = double.Parse(Vessel.GetValue("Mass"));
                    break;
                }
            }
            return Mass;
        }

        public static double FetchArea(Vessel vessel)
        {
            ConfigNode Data = VesselInformation;
            bool Vesselfound = false;
            double Area = 0.0;

            foreach (ConfigNode Vessel in Data.GetNodes("VESSEL"))
            {
                string id = Vessel.GetValue("id");
                if (id == vessel.id.ToString())
                {
                    Vesselfound = true;
                }

                if (Vesselfound == true)
                {
                    Area = double.Parse(Vessel.GetValue("Area"));
                    break;
                }
            }
            return Area;
        }

        public static void UpdateStationKeeping(Vessel vessel, bool StationKeeping)
        {
            if (vessel == FlightGlobals.ActiveVessel)
            {
                List<ModuleOrbitalDecay> modlist = vessel.FindPartModulesImplementing<ModuleOrbitalDecay>();
                foreach (ModuleOrbitalDecay module in modlist)
                {
                    module.stationKeepData.IsStationKeeping = StationKeeping;
                }
            }
            else
            {
                ProtoVessel proto = vessel.protoVessel;
                foreach (ProtoPartSnapshot protopart in proto.protoPartSnapshots)
                {
                    foreach (ProtoPartModuleSnapshot protopartmodulesnapshot in protopart.modules)
                    {
                        if (protopartmodulesnapshot.moduleName == "ModuleOrbitalDecay")
                        {
                            ConfigNode node = protopartmodulesnapshot.moduleValues.GetNode("stationKeepData");
                            node.SetValue("IsStationKeeping", StationKeeping.ToString());
                            break;
                        }
                    }
                }
            }
        }

        public static void UpdateVesselSMA(Vessel vessel, double SMA)
        {
            ConfigNode Data = VesselInformation;
            bool Vesselfound = false;

            foreach (ConfigNode Vessel in Data.GetNodes("VESSEL"))
            {
                string id = Vessel.GetValue("id");
                if (id == vessel.id.ToString())
                {
                    Vesselfound = true;
                }

                if (Vesselfound == true)
                {
                    Vessel.SetValue("SMA", SMA.ToString());
                    break;
                }
            }
        }

        public static double FetchSMA(Vessel vessel)
        {
            ConfigNode Data = VesselInformation;
            bool Vesselfound = false;
            double SMA = 0.0;

            foreach (ConfigNode Vessel in Data.GetNodes("VESSEL"))
            {
                string id = Vessel.GetValue("id");
                if (id == vessel.id.ToString())
                {
                    Vesselfound = true;
                }

                if (Vesselfound == true)
                {
                    SMA = double.Parse(Vessel.GetValue("SMA"));
                    break;
                }
            }

            return SMA;
        }

        public static void UpdateVesselECC(Vessel vessel, double ECC)
        {
            ConfigNode Data = VesselInformation;
            bool Vesselfound = false;

            if (double.IsNaN(ECC)) // No NANs here please!
            {
                ECC = 0.0;
            }

            foreach (ConfigNode Vessel in Data.GetNodes("VESSEL"))
            {
                string id = Vessel.GetValue("id");
                if (id == vessel.id.ToString())
                {
                    Vesselfound = true;
                }

                if (Vesselfound == true)
                {
                    Vessel.SetValue("ECC", ECC.ToString());
                    break;
                }
            }
        }

        public static double FetchECC(Vessel vessel)
        {
            ConfigNode Data = VesselInformation;
            bool Vesselfound = false;
            double ECC = 0.0;

            foreach (ConfigNode Vessel in Data.GetNodes("VESSEL"))
            {
                string id = Vessel.GetValue("id");
                if (id == vessel.id.ToString())
                {
                    Vesselfound = true;
                }

                if (Vesselfound == true)
                {
                    ECC = double.Parse(Vessel.GetValue("ECC"));
                    break;
                }
            }
            if (double.IsNaN(ECC)) // No NANs here please!
            {
                ECC = 0.0;
            }
            return ECC;
        }

        public static void UpdateVesselINC(Vessel vessel, double INC)
        {
            ConfigNode Data = VesselInformation;
            bool Vesselfound = false;

            foreach (ConfigNode Vessel in Data.GetNodes("VESSEL"))
            {
                string id = Vessel.GetValue("id");
                if (id == vessel.id.ToString())
                {
                    Vesselfound = true;
                }

                if (Vesselfound == true)
                {
                    Vessel.SetValue("INC", INC.ToString());
                    break;
                }
            }
        }

        public static double FetchINC(Vessel vessel)
        {
            ConfigNode Data = VesselInformation;
            bool Vesselfound = false;
            double INC = 0.0;

            foreach (ConfigNode Vessel in Data.GetNodes("VESSEL"))
            {
                string id = Vessel.GetValue("id");
                if (id == vessel.id.ToString())
                {
                    Vesselfound = true;
                }

                if (Vesselfound == true)
                {
                    INC = double.Parse(Vessel.GetValue("INC"));
                    break;
                }
            }

            return INC;
        }

        public static void UpdateVesselLPE(Vessel vessel, double LPE)
        {
            ConfigNode Data = VesselInformation;
            bool Vesselfound = false;

            foreach (ConfigNode Vessel in Data.GetNodes("VESSEL"))
            {
                string id = Vessel.GetValue("id");
                if (id == vessel.id.ToString())
                {
                    Vesselfound = true;
                }

                if (Vesselfound == true)
                {
                    Vessel.SetValue("LPE", LPE.ToString());
                    break;
                }
            }
        }

        public static double FetchLPE(Vessel vessel)
        {
            ConfigNode Data = VesselInformation;
            bool Vesselfound = false;
            double LPE = 0.0;

            foreach (ConfigNode Vessel in Data.GetNodes("VESSEL"))
            {
                string id = Vessel.GetValue("id");
                if (id == vessel.id.ToString())
                {
                    Vesselfound = true;
                }

                if (Vesselfound == true)
                {
                    LPE = double.Parse(Vessel.GetValue("LPE"));
                    break;
                }
            }

            return LPE;
        }

        public static void UpdateVesselLAN(Vessel vessel, double LAN)
        {
            ConfigNode Data = VesselInformation;
            bool Vesselfound = false;

            foreach (ConfigNode Vessel in Data.GetNodes("VESSEL"))
            {
                string id = Vessel.GetValue("id");
                if (id == vessel.id.ToString())
                {
                    Vesselfound = true;
                }

                if (Vesselfound == true)
                {
                    Vessel.SetValue("LAN", LAN.ToString());
                    break;
                }
            }
        }

        public static double FetchLAN(Vessel vessel)
        {
            ConfigNode Data = VesselInformation;
            bool Vesselfound = false;
            double LAN = 0.0;

            foreach (ConfigNode Vessel in Data.GetNodes("VESSEL"))
            {
                string id = Vessel.GetValue("id");
                if (id == vessel.id.ToString())
                {
                    Vesselfound = true;
                }

                if (Vesselfound == true)
                {
                    LAN = double.Parse(Vessel.GetValue("LAN"));
                    break;
                }
            }

            return LAN;
        }

        public static void UpdateVesselMNA(Vessel vessel, double MNA)
        {
            ConfigNode Data = VesselInformation;
            bool Vesselfound = false;

            foreach (ConfigNode Vessel in Data.GetNodes("VESSEL"))
            {
                string id = Vessel.GetValue("id");
                if (id == vessel.id.ToString())
                {
                    Vesselfound = true;
                }

                if (Vesselfound == true)
                {
                    Vessel.SetValue("MNA", MNA.ToString());
                    break;
                }
            }
        }

        public static double FetchMNA(Vessel vessel)
        {
            ConfigNode Data = VesselInformation;
            bool Vesselfound = false;
            double MNA = 0.0;

            foreach (ConfigNode Vessel in Data.GetNodes("VESSEL"))
            {
                string id = Vessel.GetValue("id");
                if (id == vessel.id.ToString())
                {
                    Vesselfound = true;
                }

                if (Vesselfound == true)
                {
                    MNA = double.Parse(Vessel.GetValue("MNA"));
                    break;
                }
            }

            return MNA;
        }

        public static void UpdateVesselEPH(Vessel vessel, double EPH)
        {
            ConfigNode Data = VesselInformation;
            bool Vesselfound = false;

            foreach (ConfigNode Vessel in Data.GetNodes("VESSEL"))
            {
                string id = Vessel.GetValue("id");
                if (id == vessel.id.ToString())
                {
                    Vesselfound = true;
                }

                if (Vesselfound == true)
                {
                    Vessel.SetValue("EPH", EPH.ToString());
                    break;
                }
            }
        }

        public static double FetchEPH(Vessel vessel)
        {
            ConfigNode Data = VesselInformation;
            bool Vesselfound = false;
            double EPH = 0.0;

            foreach (ConfigNode Vessel in Data.GetNodes("VESSEL"))
            {
                string id = Vessel.GetValue("id");
                if (id == vessel.id.ToString())
                {
                    Vesselfound = true;
                }

                if (Vesselfound == true)
                {
                    EPH = double.Parse(Vessel.GetValue("EPH"));
                    break;
                }
            }

            return EPH;
        }


        /* simple ISP dependant effi calculation.
         * NEEDS ballancing
         * removing fuel based on dV would make more sense,
         * and more thinkering to firgure it out
         * 1.60 milestone maybe?
         */
        public static float FetchEfficiency(Vessel vessel)
        {
            float Efficiency = 0;
            if (vessel == FlightGlobals.ActiveVessel)
            {
                List<ModuleOrbitalDecay> modlist  = vessel.FindPartModulesImplementing<ModuleOrbitalDecay>();
                Efficiency = modlist.ElementAt(0).stationKeepData.ISP;
               
            }
            else
            {
                ProtoVessel proto = vessel.protoVessel;

                foreach (ProtoPartSnapshot protopart in proto.protoPartSnapshots)
                {
                    foreach (ProtoPartModuleSnapshot protopartmodulesnapshot in protopart.modules)
                    {
                        if (protopartmodulesnapshot.moduleName == "ModuleOrbitalDecay")
                        {
                            ConfigNode node = protopartmodulesnapshot.moduleValues.GetNode("stationKeepData");
                            Efficiency = float.Parse(node.GetValue("ISP"));
                            break;
                        }
                    }
                }
            }
            if (Settings.ReadRD())
            {
                Efficiency *= 0.5f; // Balance here!
            }
            

            return 1/Efficiency;
        }

/* unused in 1.5.0
        public static double FetchFuel(Vessel vessel)
        {
                                                                           
           ConfigNode Data = VesselInformation;
            bool Vesselfound = false;
            double Fuel = 0.0;

            foreach (ConfigNode Vessel in Data.GetNodes("VESSEL"))
            {
                string id = Vessel.GetValue("id");
                if (id == vessel.id.ToString())
                {
                    Vesselfound = true;
                }

                if (Vesselfound == true)
                {
                    Fuel = double.Parse(Vessel.GetValue("Fuel").ToString());
                    break;
                }
            }

            return Fuel;
        }
        */

        public static void UpdateBody(Vessel vessel, CelestialBody body)
        {
            ConfigNode Data = VesselInformation;
            bool Vesselfound = false;

            foreach (ConfigNode Vessel in Data.GetNodes("VESSEL"))
            {
                string id = Vessel.GetValue("id");
                if (id == vessel.id.ToString())
                {
                    Vesselfound = true;
                }

                if (Vesselfound == true)
                {
                    Vessel.SetValue("ReferenceBody", body.GetName());
                    break;
                }
            }
        }

        public static void UpdateVesselFuel(Vessel vessel, double Fuel)
        {
            ConfigNode Data = VesselInformation;
            bool Vesselfound = false;

            foreach (ConfigNode Vessel in Data.GetNodes("VESSEL"))
            {
                string id = Vessel.GetValue("id");
                if (id == vessel.id.ToString())
                {
                    Vesselfound = true;
                }

                if (Vesselfound == true)
                {
                    Vessel.SetValue("Fuel", Fuel.ToString());
                    break;
                }
            }
        }
/* unused in 1.5.0
        public static void UpdateVesselResource(Vessel vessel, string Resource)
        {
            ConfigNode Data = VesselInformation;
            bool Vesselfound = false;

            foreach (ConfigNode Vessel in Data.GetNodes("VESSEL"))
            {
                string id = Vessel.GetValue("id");
                if (id == vessel.id.ToString())
                {
                    Vesselfound = true;
                }

                if (Vesselfound == true)
                {
                    Vessel.SetValue("Resource", Resource);
                    break;
                }
            }
        }
        */

        public static double CalculateVesselArea(Vessel vessel)
        {
            double Area = 0;
            Area = FindVesselArea(vessel);
            return Area;
        }

        public static double FindVesselArea(Vessel vessel)
        {
            double Area = 0.0;
            ProtoVessel vesselImage = vessel.protoVessel;
            List<ProtoPartSnapshot> PartSnapshots = vesselImage.protoPartSnapshots;
            foreach (ProtoPartSnapshot part in PartSnapshots)
            {
                if (vessel == FlightGlobals.ActiveVessel)
                {
                    Area = Area + part.partRef.radiativeArea;
                }
                else
                {
                    Area = Area + (part.partInfo.partSize * 2.0 * Math.PI);
                }
            }

            return Area/4.0; // only one side facing prograde
        }
    }
}
