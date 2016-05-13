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

    public class StationKeepingManager : MonoBehaviour
    {
      
        public static bool EngineCheck(Vessel vessel) // 1.3.0
        {
            bool HasEngine = false;

                if (vessel != FlightGlobals.ActiveVessel)
                {
                    ProtoVessel protovessel = vessel.protoVessel;
                    List<ProtoPartSnapshot> PPSL = protovessel.protoPartSnapshots;

                    foreach (ProtoPartSnapshot PPS in PPSL)
                    {
                        List<ProtoPartModuleSnapshot> PPMSL = PPS.modules;
                        foreach (ProtoPartModuleSnapshot PPMS in PPMSL)
                        {
                            if (PPMS.moduleName == "ModuleEngines" || PPMS.moduleName == "ModuleRCS")
                            {
                                HasEngine = true;
                                break;
                            }
                        }

                        if (HasEngine == true)
                        {
                            break;
                        }
                    }
                }

                else if (vessel == FlightGlobals.ActiveVessel)
                {
                    if (vessel.FindPartModulesImplementing<ModuleEngines>().Count > 0 || vessel.FindPartModulesImplementing<ModuleRCS>().Count > 0)
                    {
                        HasEngine = true;
                    }
                    else
                    {
                        HasEngine = false;
                    }
                }

            return HasEngine;
        }

        public static void FuelManager(Vessel vessel)
        {
            string ResourceName = "";
            
            ResourceName = Settings.ReadStationKeepingResource();
            double CurrentFuel = 0;
            CurrentFuel = VesselData.FetchFuel(vessel);

            double ResourceEfficiency = ResourceManager.GetEfficiency(ResourceName);
            double LostFuel = 0.0;

            if (Settings.ReadRD() == true)
            {
                LostFuel = DecayManager.DecayRateRealistic(vessel) * Settings.ReadResourceRateDifficulty() * ResourceEfficiency; // * Fuel Multiplier
            }
            else
            {
                LostFuel = DecayManager.DecayRateStock(vessel) * Settings.ReadResourceRateDifficulty() * ResourceEfficiency; // * Fuel Multiplier
            }

            double FuelNew = CurrentFuel - LostFuel;

            if (EngineCheck(vessel) == false) // 1.3.0
            {
                ScreenMessages.PostScreenMessage("Warning: " + vessel.name + " has no operational Engines or RCS modules, Station Keeping disabled.");
                VesselData.UpdateStationKeeping(vessel, false);
            }

            else if (EngineCheck(vessel) == true)
            {

                if (CurrentFuel < LostFuel)
                {
                    ScreenMessages.PostScreenMessage("Warning: " + vessel.name + " has run out of " + ResourceName + ", Station Keeping disabled.");
                    VesselData.UpdateStationKeeping(vessel, false);
                    VesselData.UpdateVesselFuel(vessel, 0);
                }
                else
                {
                    if (vessel == FlightGlobals.ActiveVessel)
                    {
                        ResourceManager.CatchUp(vessel, ResourceName);
                        ResourceManager.RemoveResources(vessel, ResourceName, (LostFuel)); // Balancing required here
                    }
                    VesselData.UpdateVesselFuel(vessel, FuelNew);
                }
            }
        }
    }
}
