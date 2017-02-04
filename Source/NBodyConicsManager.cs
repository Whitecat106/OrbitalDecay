/*

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
/*
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;

namespace WhitecatIndustries
{
    [KSPAddon(KSPAddon.Startup.EveryScene, false)]
    public class NBodyConicsManager : MonoBehaviour
    {

        public static Dictionary<Vector3d, double> VesselPointsAtTime = new Dictionary<Vector3d, double>();
        public static Dictionary<Vector3d, double> VesselVelocitiesAtTime = new Dictionary<Vector3d, double>();

        public static Dictionary<Vessel, Orbit> VesselOrbits = new Dictionary<Vessel, Orbit>();
        public static Dictionary<Vector3d, double> BodyPointsAtTime = new Dictionary<Vector3d, double>();

        public float UpdateInterval = 0.2f;
        public float  LastUpdate = 0f;

        public static int MaxSamplesPerInterval = 5;
        public static int MaxTimeInFuture = 10000000;
        public static int ConicsLerpCount = 3; // Could cause massive lag

        public static Vessel vessel; // Assigned to by UI
        public static CelestialBody body; // Assigned to by UI

        public static LineRenderer lineRenderer = null;
        public static MeshRenderer meshRenderer = null;
        public static MeshFilter meshFilter = null;
        public static GameObject obj = null;
        public static List<GameObject> meshes = new List<GameObject>();


        public void OnDestroy()
        {
            Destroy(lineRenderer);
        }

        public void FixedUpdate()
        {
            if (HighLogic.LoadedSceneHasPlanetarium && Planetarium.fetch.enabled)
            {
                if (Time.timeSinceLevelLoad > 3) // Check everythings ready!
                {
                    if ((Time.time - LastUpdate) > UpdateInterval)
                    {
                        LastUpdate = Time.time;

                        vessel = FlightGlobals.ActiveVessel; // Add UI option for this

                        if (vessel != null)
                        {
                            print("ManagePredictions");
                            ManagePredictions(vessel); 
                        }
                    }
                }
            }
        }

        public static void ManagePredictions(Vessel vessel)
        {
            Orbit orbitHolder = new Orbit();

            if (VesselOrbits.Keys.Contains(vessel))
            {
                VesselOrbits.TryGetValue(vessel, out orbitHolder);
            }
            else
            {
                orbitHolder = vessel.orbitDriver.orbit;
                VesselOrbits.Add(vessel, vessel.orbitDriver.orbit);
            }

            Vector3d InitialPosition = orbitHolder.getRelativePositionAtUT(HighLogic.CurrentGame.UniversalTime);
            Vector3d InitialVelocity = orbitHolder.getRelativePositionAtUT(HighLogic.CurrentGame.UniversalTime);
            double SampleInterval = orbitHolder.period / 360;

            Vector3d FinalVelocity = new Vector3d(0,0,0);
            Vector3d FinalPosition = new Vector3d(0,0,0);

            double TimeAtSample = 0;

            if (VesselPointsAtTime.Count > 1)
            {
                InitialPosition = VesselPointsAtTime.ElementAt(VesselPointsAtTime.Count-1).Key;
                VesselPointsAtTime.TryGetValue(InitialPosition, out TimeAtSample);
            }
            else
            {
                TimeAtSample = HighLogic.CurrentGame.UniversalTime;
                VesselPointsAtTime.Add(OrbitalDecayUtilities.FlipYZ(InitialPosition), TimeAtSample);
            }

            if (VesselVelocitiesAtTime.Count > 1)
            {
                InitialVelocity = VesselVelocitiesAtTime.ElementAt(VesselVelocitiesAtTime.Count-1).Key;
                VesselVelocitiesAtTime.Add(InitialVelocity, TimeAtSample);
            }

            double mass = vessel.totalMass;
            print("Mass:");

            FinalVelocity = InitialVelocity;

                for (int i = 0; i < MaxSamplesPerInterval; i++) // Add to the list 10 times per update interval until MaxTimeInFutureReached
                {
                    if (TimeAtSample + (i * SampleInterval) < HighLogic.CurrentGame.UniversalTime + MaxTimeInFuture)
                    {
                        InitialVelocity = FinalVelocity;
                        FinalVelocity = FinalVelocity + NBodyManager.GetMomentaryDeltaVOrbit(orbitHolder, TimeAtSample + (i * SampleInterval), mass) * SampleInterval;
                        orbitHolder = NBodyManager.BuildFromStateVectors(orbitHolder.getRelativePositionAtUT(TimeAtSample + (i * SampleInterval)), FinalVelocity, vessel.orbitDriver.orbit.referenceBody, TimeAtSample + (i * SampleInterval), vessel, InitialVelocity, orbitHolder);
                        VesselPointsAtTime.Add(OrbitalDecayUtilities.FlipYZ(orbitHolder.getRelativePositionAtUT(TimeAtSample + (i * SampleInterval))), TimeAtSample + (i * SampleInterval));
                        VesselVelocitiesAtTime.Add(FinalVelocity, TimeAtSample + (i * SampleInterval));
                        
                        // Get Acceleration at time * by period/360
                        // Set final vel to that
                        // Final Pos = pos at time from orbit
                        // Add each point and vel to the dictionary
                    }
            }

                if (VesselOrbits.ContainsKey(vessel))
                {
                    VesselOrbits.Remove(vessel);
                }

            VesselOrbits.Add(vessel, orbitHolder);
            RenderTrajectory();

        }

        public static void RenderTrajectory()
        {
  
                lineRenderer = MapView.MapCamera.gameObject.AddOrGetComponent<LineRenderer>();

                lineRenderer.gameObject.layer = 31;
                lineRenderer.transform.localPosition = Vector3d.zero;
                lineRenderer.material = MapView.OrbitLinesMaterial;
                lineRenderer.SetColors(Color.blue, Color.blue);
                
                float lineWidth = 1f;


                lineRenderer.SetWidth(1, 1);
                lineRenderer.SetVertexCount(VesselPointsAtTime.Count * ConicsLerpCount); // Uses LerpValue for Lerping between points to make nice curves
                
                int SkipRuleCount = 0;

                for (int i = 0; i < VesselPointsAtTime.Count; i++, i++, i++) // For every three (for every lerp count)
                {
                    if (i != 0) // If not 0 do smoothing from point before.
                    {
                        //Start Point 
                        Vector3d StartPoint = VesselPointsAtTime.ElementAt(i-1).Key;
                        // End Point
                        Vector3d EndPoint = VesselPointsAtTime.ElementAt(i).Key;

                        for (int j = 1; j < ConicsLerpCount ; j++) // Think about it...
                        {
                            lineRenderer.SetPosition((i - j), PlanetariumCamera.Camera.WorldToScreenPoint((ScaledSpace.LocalToScaledSpace(Vector3d.Lerp(StartPoint, EndPoint, 1 / j)))));
                        }

                        lineRenderer.SetPosition((i), PlanetariumCamera.Camera.WorldToScreenPoint((ScaledSpace.LocalToScaledSpace(VesselPointsAtTime.ElementAt(i).Key))));
                        
                    }
                    else
                    {
                        lineRenderer.SetPosition((i), PlanetariumCamera.Camera.WorldToScreenPoint((ScaledSpace.LocalToScaledSpace(VesselPointsAtTime.ElementAt(i).Key))));
                    }
                }

                lineRenderer.enabled = true;

                ManageList();
             

        }

        public static void ManageList()
        {
            try
            {
                List<Vector3d> DeletablePoints = new List<Vector3d>();

                foreach (Vector3d point in VesselPointsAtTime.Keys)
                {
                    double time = 0;
                    VesselPointsAtTime.TryGetValue(point, out time);
                    if (time < HighLogic.CurrentGame.UniversalTime)
                    {
                        DeletablePoints.Add(point);
                    }
                }

                if (DeletablePoints.Count > 0)
                {
                    foreach (Vector3d point in DeletablePoints)
                    {
                        VesselPointsAtTime.Remove(point);
                    }
                }

                VesselVelocitiesAtTime.Clear();
            }
            catch (ArgumentOutOfRangeException)
            {
                print("List Management Exception");
            }
        }

        public static void DrawHillSphere(CelestialBody body)
        {


        }

        public static void DrawSphereOfInfluence(CelestialBody body)
        {


        }
    }
}
*/