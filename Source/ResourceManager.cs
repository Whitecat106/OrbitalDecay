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
    public class ResourceManager : MonoBehaviour
    {
        public static void RemoveResources(Vessel vessel, string resource, float quantity)
        {
            if (vessel = FlightGlobals.ActiveVessel)
            {
                int MonoPropId = PartResourceLibrary.Instance.GetDefinition(resource).id;
                vessel.rootPart.RequestResource(MonoPropId, quantity);
            }
        }

        public static void CatchUp(Vessel vessel, string resource)
        {
            int MonoPropId = PartResourceLibrary.Instance.GetDefinition(resource).id;
            vessel.rootPart.RequestResource(MonoPropId, (-VesselData.FetchFuel(vessel) + VesselData.FetchDryFuel(vessel)));
            VesselData.UpdateDryFuel(vessel,VesselData.FetchFuel(vessel));
        }

        public static float GetResources(Vessel vessel, string resource)
        {
            float quantity = 0.0f;
            int MonoPropId = PartResourceLibrary.Instance.GetDefinition(resource).id;
            bool Skip = false;
            List<PartResource> resources = new List<PartResource>();
            try
            {
                vessel.rootPart.GetConnectedResources(MonoPropId, ResourceFlowMode.STACK_PRIORITY_SEARCH, resources);
            }
            catch (NullReferenceException)
            {
                Skip = true;
            }
            if (resources.Count != 0 && Skip == false)
            {
                foreach (PartResource res in resources)
                {
                    if (res.info.id == MonoPropId)
                    {
                        quantity = quantity + (float)res.amount;
                    }
                }
            }
            return quantity;
        }

        public static float GetDryResources(Vessel vessel, string resource) // Fix this
        {
            float quantity = 0.0f;

            int MonoPropId = PartResourceLibrary.Instance.GetDefinition(resource).id;
            List<PartResource> resources = new List<PartResource>();
           foreach (Part part in vessel.parts) 
            {
                quantity = quantity + (float)part.Resources.Get(MonoPropId).maxAmount;
            }
            return quantity;
        }

        public static float GetEfficiency(string resource) // Eventually combine with engine ISP but quite nice like this!
        {
            float Efficiency = 0.0f;
            PartResourceDefinition resourceDef = PartResourceLibrary.Instance.GetDefinition(resource);
            if (Settings.ReadRD())
            {
                Efficiency = resourceDef.density * 50.0f;
            }
            else
            {
                Efficiency = resourceDef.density;
            }
            return Efficiency;
        }

        public static List<PartResource> GetVesselPartResources(Vessel vessel) //1.2.0
        {
            PartResourceList List;
            List<PartResource> UsableResources = new List<PartResource>();

            List = vessel.rootPart.Resources;

            foreach (PartResource Res in List)
            {
                double Id = Res.GetInstanceID();
                foreach (PartResourceDefinition Resource in PartResourceLibrary.Instance.resourceDefinitions)
                {
                    if (Resource.id == Id)
                    {
                        if (Resource.resourceFlowMode != ResourceFlowMode.ALL_VESSEL && Resource.resourceFlowMode != ResourceFlowMode.NO_FLOW && Resource.resourceTransferMode != ResourceTransferMode.NONE &&
                            Resource.name != "EVA Propellant" && Resource.name != "Ore" && Resource.name != "ElectricCharge" && Resource.name != "IntakeAir")
                        {
                            UsableResources.Add(Res);
                        }
                    }
                }
            }       
            return UsableResources;
        }

    }
}
