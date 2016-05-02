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

        public void Awake()
        {
            VesselInformation.ClearNodes();

            if (HighLogic.LoadedSceneIsGame && (HighLogic.LoadedScene != GameScenes.LOADING || HighLogic.LoadedScene != GameScenes.LOADINGBUFFER))
            {
                File = ConfigNode.Load(FilePath);

                if (File.nodes.Count > 0)
                {
                    foreach (ConfigNode vessel in File.GetNodes("VESSEL"))
                    {
                        VesselInformation.AddNode(vessel);
                    }
                }
            }
            print("WhitecatIndustries - Orbital Decay - Loaded vessel data.");
        }

        public void FixedUpdate()
        {
            if (Time.timeSinceLevelLoad > 0.5)
            {
                if (HighLogic.LoadedSceneIsGame && (HighLogic.LoadedScene != GameScenes.LOADING || HighLogic.LoadedScene != GameScenes.LOADINGBUFFER))
                {
                    Vessel vessel = new Vessel();
                    for (int i = 0; i < FlightGlobals.Vessels.Count; i++)
                    {
                        vessel = FlightGlobals.Vessels.ElementAt(i);
                        if ((vessel.vesselType != VesselType.SpaceObject && vessel.vesselType != VesselType.Unknown))
                        {
                            if (CheckIfContained(vessel) == true)
                            {
                                if (vessel.situation != Vessel.Situations.ORBITING)
                                {
                                    ClearVesselData(vessel);
                                }

                                if (vessel.situation == Vessel.Situations.ORBITING)
                                {
                                    WriteVesselData(vessel);
                                }
                            }
                            else if (CheckIfContained(vessel) == false)
                            {
                                if (vessel.situation == Vessel.Situations.ORBITING)
                                {
                                    WriteVesselData(vessel);
                                }
                            }
                        }
                    }
                }
            }
        }

        public void OnDestroy()
        {
            if (HighLogic.LoadedSceneIsGame && (HighLogic.LoadedScene != GameScenes.LOADING && HighLogic.LoadedScene != GameScenes.LOADINGBUFFER && HighLogic.LoadedScene != GameScenes.MAINMENU))
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
                if (vessel = FlightGlobals.ActiveVessel)
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
                        UpdateVesselSMA(vessel, (float)vessel.orbitDriver.orbit.semiMajorAxis);
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
                string ResourceName = "";
                ResourceName = Settings.ReadStationKeepingResource();

                VesselNode.SetValue("Mass", (vessel.GetTotalMass() * 1000).ToString());
                VesselNode.SetValue("Area", (CalculateVesselArea(vessel)).ToString());
                VesselNode.SetValue("Fuel", (ResourceManager.GetResources(vessel, ResourceName)).ToString());
                VesselNode.SetValue("DryFuel", (ResourceManager.GetResources(vessel,ResourceName)).ToString()); // Dry Resources broken?
            }
        }


        public static void ClearVesselData(Vessel vessel)
        {
            ConfigNode VesselNode = new ConfigNode("VESSEL");
            bool found = false; 

            foreach(ConfigNode node in VesselInformation.GetNodes("VESSEL"))
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

            string ResourceName = "";
            ResourceName = Settings.ReadStationKeepingResource();

            newVessel.AddValue("name", vessel.GetName());
            newVessel.AddValue("id", vessel.id.ToString());
            string CatalogueCode = vessel.vesselType.ToString().Substring(0,1) + vessel.GetInstanceID().ToString();
            newVessel.AddValue("code", CatalogueCode);
            if (vessel == FlightGlobals.ActiveVessel)
            {
                newVessel.AddValue("Mass", vessel.GetTotalMass() * 1000); // 1.1.0 in kilograms!
                newVessel.AddValue("Area", CalculateVesselArea(vessel)); // Try?
            }
            else
            {
                newVessel.AddValue("Mass", vessel.GetTotalMass() * 1000); // Try "1"
                newVessel.AddValue("Area", "2.0"); // Still getting bugs here
            }
            newVessel.AddValue("ReferenceBody", vessel.orbitDriver.orbit.referenceBody.GetName());
            newVessel.AddValue("SMA", vessel.orbitDriver.orbit.semiMajorAxis.ToString());
            newVessel.AddValue("StationKeeping", false.ToString());
            newVessel.AddValue("Fuel", ResourceManager.GetResources(vessel, ResourceName));
            newVessel.AddValue("DryFuel", ResourceManager.GetResources(vessel, ResourceName));

            return newVessel; 
        }

        public static bool FetchStationKeeping(Vessel vessel)
        {
            ConfigNode Data = VesselInformation;
            bool Vesselfound = false;
            bool StationKeeping = false;

            foreach (ConfigNode Vessel in Data.GetNodes("VESSEL"))
            {
                string id = Vessel.GetValue("id");
                if (id == vessel.id.ToString())
                {
                    Vesselfound = true;
                }

                if (Vesselfound == true)
                {
                    StationKeeping = bool.Parse(Vessel.GetValue("StationKeeping"));
                    break;
                }
            }
            return StationKeeping;
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
                    Vessel.SetValue("StationKeeping", StationKeeping.ToString());
                    break;
                }
            }

        }

        public static void UpdateVesselSMA(Vessel vessel, float SMA)
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
        public static float FetchSMA(Vessel vessel)
        {
            ConfigNode Data = VesselInformation;
            bool Vesselfound = false;
            float SMA = 0.0f;

            foreach (ConfigNode Vessel in Data.GetNodes("VESSEL"))
            {
                string id = Vessel.GetValue("id");
                if (id == vessel.id.ToString())
                {
                    Vesselfound = true;
                }

                if (Vesselfound == true)
                {
                    SMA = float.Parse(Vessel.GetValue("SMA").ToString());
                    break;
                }
            }
            return SMA;
        }

        public static float FetchFuel(Vessel vessel)
        {
            ConfigNode Data = VesselInformation;
            bool Vesselfound = false;
            float Fuel = 0.0f;

            foreach (ConfigNode Vessel in Data.GetNodes("VESSEL"))
            {
                string id = Vessel.GetValue("id");
                if (id == vessel.id.ToString())
                {
                    Vesselfound = true;
                }

                if (Vesselfound == true)
                {
                    Fuel = float.Parse(Vessel.GetValue("Fuel").ToString());
                    break;
                }
            }
            return Fuel;
        }

        public static float FetchDryFuel(Vessel vessel)
        {
            ConfigNode Data = VesselInformation;
            bool Vesselfound = false;
            float Fuel = 0.0f;

            foreach (ConfigNode Vessel in Data.GetNodes("VESSEL"))
            {
                string id = Vessel.GetValue("id");
                if (id == vessel.id.ToString())
                {
                    Vesselfound = true;
                }

                if (Vesselfound == true)
                {
                    Fuel = float.Parse(Vessel.GetValue("DryFuel").ToString());
                    break;
                }
            }
            return Fuel;
        }

        public static void UpdateDryFuel(Vessel vessel, float fuel)
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
                    Vessel.SetValue("DryFuel", fuel.ToString());
                    break;
                }
            }
        }

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


        public static void UpdateVesselFuel(Vessel vessel, float Fuel)
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

        public static double CalculateVesselArea(Vessel vessel)
        {
            double Area = 0;
            if (vessel.rootPart.radiativeArea != 0)
            {
                Area = vessel.rootPart.radiativeArea / 2;
            }
            else
            {
                if (Settings.ReadRD()) // Temporary Assumptions
                {
                    Area = 3.5;
                }
                else
                {
                    Area = 1.25; 
                }
            }
            print("Area: " + Area);
            return Area;
        }

       /* public static double FetchEngineISP(Vessel vessel) // Resource and Engine management 1.2.0
        {
            ConfigNode Data = VesselInformation;
            bool Vesselfound = false;
            float ISP = 1.0f;

            foreach (ConfigNode Vessel in Data.GetNodes("VESSEL"))
            {
                string id = Vessel.GetValue("id");
                if (id == vessel.id.ToString())
                {
                    Vesselfound = true;
                }

                if (Vesselfound == true)
                {
                    if (Vessel.nodes.Count != 0)
                    {
                        double MaxISP = 0.0;
                        foreach (ConfigNode Engine in Vessel.GetNodes("ENGINE"))
                        {
                            if (double.Parse(Engine.GetValue("ISP")) > MaxISP)
                            {
                                MaxISP = double.Parse(Engine.GetValue("ISP"));
                            }
                        }
                        ISP = (float)MaxISP;
                    }
                }
            }
            return ISP;
        }

        public static void UpdateEngineInfo(Vessel vessel, string Name, string EngineId, float ISP, bool Operational)
        {
            ConfigNode Data = VesselInformation;
            bool Vesselfound = false;
            bool Enginefound = false;

            foreach (ConfigNode Vessel in Data.GetNodes("VESSEL"))
            {
                string id = Vessel.GetValue("id");
                if (id == vessel.id.ToString())
                {
                    Vesselfound = true;
                }

                if (Vesselfound == true)
                {
                    if (Vessel.nodes.Count != 0)
                    {
                        foreach (ConfigNode Engine in Vessel.GetNodes("ENGINE"))
                        {
                            if (Engine.GetValue("ID") == EngineId)
                            {
                                Enginefound = true;
                            }

                            if (Enginefound == true)
                            {
                                Engine.SetValue("ISP", ISP.ToString());
                                break;
                            }
                        }
                    }

                    if (Enginefound == false)
                    {
                        AddEngineInfo(vessel, Name, EngineId, ISP, Operational);
                    }
                }
            }
        }

        public static void AddEngineInfo(Vessel vessel, string Name, string EngineId, float ISP, bool Operational)
        {
            ConfigNode Data = VesselInformation;
            bool Vesselfound = false;
            bool Enginefound = false;

            foreach (ConfigNode Vessel in Data.GetNodes("VESSEL"))
            {
                string id = Vessel.GetValue("id");
                if (id == vessel.id.ToString())
                {
                    Vesselfound = true;
                }

                if (Vesselfound == true)
                {
                    if (Vessel.nodes.Count != 0)
                    {
                        foreach (ConfigNode Engine in Vessel.GetNodes("ENGINE"))
                        {
                            if (Engine.GetValue("ID") == EngineId)
                            {
                                Enginefound = true;
                                break;
                            }
                        }
                    }

                    if (Enginefound == false)
                    {
                        ConfigNode NewEngine = new ConfigNode("ENGINE");
                        NewEngine.AddValue("Name", Name);
                        NewEngine.AddValue("ID", EngineId);
                        NewEngine.AddValue("ISP", ISP);
                        Vessel.AddNode(NewEngine);
                    }
                }
            }
        }
       */

        // 1.2.0 new area and mass functions 

        public static List<ProtoPartSnapshot> VesselPartsSS(Vessel vessel)
        {
            List<ProtoPartSnapshot> SnapShot;
            SnapShot = vessel.protoVessel.protoPartSnapshots;
            return SnapShot;
        }

        public static double FetchPPSSArea(Vessel vessel)
        {
            double Area = 0;
            double TotalSize = 0;
            List<ProtoPartSnapshot> Parts = VesselPartsSS(vessel);
            foreach (ProtoPartSnapshot SS in Parts)
            {
                TotalSize = TotalSize + SS.partInfo.partSize;
            }

            Area = TotalSize / 2.0;
            return Area;
        }
    }
}
