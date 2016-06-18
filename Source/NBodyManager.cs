using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;

namespace WhitecatIndustries 
{
    [KSPAddon(KSPAddon.Startup.EveryScene, false)]
    public class NBodyManager : MonoBehaviour // 1.6.0 NBody simulator
    {
        private float VariableUpdateInterval = 1.0f;
        private float lastUpdate = 0.0f;
        public double GravitationalConstant = Math.Pow(6.67408 * 10, -11);
        private bool CurrentProcess = false; 


        List<Orbit> FutureRenderOrbits = new List<Orbit>();
        List<MeshRenderer> CurrentMeshRenderers = new List<MeshRenderer>();
        List<GameObject> CurrentLineGameObjects = new List<GameObject>();
        Material lineMaterial;


        public bool ToggleHillSpheres = false;
        public bool ToggleSphereOfInfluences = true;

        public void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneHasPlanetarium)
            {
                ClearOrbitLines();
            }

            if (CurrentProcess == false)
            {
                if (HighLogic.LoadedSceneIsGame && (HighLogic.LoadedScene != GameScenes.LOADING && HighLogic.LoadedScene != GameScenes.LOADINGBUFFER && HighLogic.LoadedScene != GameScenes.MAINMENU))
                {
                    VariableUpdateInterval = 1.0f;

                    if (Time.timeSinceLevelLoad > 0.7) // Fit in here
                    {
                        if ((Time.time - lastUpdate) > VariableUpdateInterval)
                        {
                            lastUpdate = Time.time;

                            foreach (CelestialBody body in FlightGlobals.Bodies)
                            {
                                if (body != Sun.Instance.sun)
                                {
                                    ManageBody(body);
                                }
                            }

                            foreach (Vessel vessel in FlightGlobals.Vessels)
                            {
                                if (vessel.vesselType != VesselType.Unknown && vessel.vesselType != VesselType.SpaceObject) // 
                                {
                                    if (VesselData.FetchStationKeeping(vessel) == false)
                                    {
                                        if (vessel == FlightGlobals.ActiveVessel)
                                        {
                                            if (DecayManager.CheckVesselStateOrbEsc(vessel))
                                            {
                                                ManageVessel(vessel);
                                            }
                                        }
                                        else
                                        {
                                            if (DecayManager.CheckVesselStateOrbEsc(vessel))
                                            {
                                                ManageVessel(vessel);
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

        #region InfluencingBodyLists
        public List<CelestialBody> InfluencingBodiesV(Vessel vessel)
        {
            List<CelestialBody> InfluencingBodies = new List<CelestialBody>();
            CelestialBody ReferenceBody = vessel.orbitDriver.orbit.referenceBody;

            foreach (CelestialBody Body in FlightGlobals.Bodies)
            {
                if (ReferenceBody.HasChild(Body) || ReferenceBody.HasParent(Body))
                {
                    InfluencingBodies.Add(Body);
                }
            }

            return InfluencingBodies;
        }

        public List<CelestialBody> InfluencingBodiesB(CelestialBody body)
        {
            List<CelestialBody> InfluencingBodies = new List<CelestialBody>();
            CelestialBody ReferenceBody = body;

            foreach (CelestialBody Body in FlightGlobals.Bodies)
            {
                if (ReferenceBody.HasChild(Body) || ReferenceBody.HasParent(Body))
                {
                    InfluencingBodies.Add(Body);
                }
            }

            return InfluencingBodies;
        }
        #endregion 

        #region InfluencingAccelerationLists

        public List<Vector3d> InfluencingAccelerationsB(Vessel vessel, double time)
        {
            List<Vector3d> InfluencingAccelerations = new List<Vector3d>();


            return InfluencingAccelerations;
        }

        public List<Vector3d> InfluencingAccelerationsV(Vessel vessel, double time)
        {
            List<Vector3d> InfluencingAccelerations = new List<Vector3d>();

            foreach (CelestialBody Body in InfluencingBodiesV(vessel))
            {
                double VesselMass = vessel.GetTotalMass() * 1000.0;
                double VesselMNA = vessel.orbitDriver.orbit.GetMeanAnomaly(vessel.orbitDriver.orbit.E, time);
                double InfluencingForce = 0;
                double BodyMass = Body.Mass;
                double DistanceToVessel = Vector3d.Distance(Body.position, vessel.GetWorldPos3D());
                //print("Body " + Body.name + " distance to " + vessel.name + " : " + DistanceToVessel);
                double BodyMNA = 0;

                try
                {
                    BodyMNA = (Body.orbitDriver.orbit.GetMeanAnomaly(Body.orbitDriver.orbit.E, time));
                }
                catch (NullReferenceException)
                {
                    BodyMNA = 0;
                }

                double MNADifference = UtilMath.RadiansToDegrees(DifferenceBetweenMNA(VesselMNA, BodyMNA)); // Try this!

                if (Body == Sun.Instance.sun)
                {
                    MNADifference = 90.0;
                }

                InfluencingForce = (GravitationalConstant * BodyMass * VesselMass) / (DistanceToVessel * DistanceToVessel);
                InfluencingForce = InfluencingForce * Math.Sin(MNADifference - 90.0);

                Vector3d InfluencingAccelerationBodyDirectionVector = Body.position;
                Vector3d VesselPositionVector = vessel.GetWorldPos3D();
                Vector3d InfluencingAccelerationVector = (new Vector3d(-InfluencingAccelerationBodyDirectionVector.x + VesselPositionVector.x, -InfluencingAccelerationBodyDirectionVector.y + VesselPositionVector.y, -InfluencingAccelerationBodyDirectionVector.z + VesselPositionVector.z)) * ((InfluencingForce / VesselMass));

                InfluencingAccelerations.Add(InfluencingAccelerationVector);

            }
            return InfluencingAccelerations;
        }
        

        #endregion 

        #region Calculations

        public static double CalculateHillSphere(Vessel vessel)
        {
            double HillSphereRadius = 0;




            return HillSphereRadius;
        }

        public static double DifferenceBetweenMNA(double VesselMNA, double BodyMNA)
        {
            double Difference = 0;
            if (VesselMNA > BodyMNA)
            {
                Difference = VesselMNA - BodyMNA;
                if (Difference < 0)
                {
                    Difference = 360 - (Math.Abs(Difference));
                }
            }

            else
            {
                Difference = BodyMNA - VesselMNA;
                if (Difference < 0)
                {
                    Difference = 360 - (Math.Abs(Difference));
                }
            }


            return Difference;
        }

        public double GetMomentaryDeltaV(Vessel vessel, double time)
        {
            List<Vector3d> InfluencingAccelerationVectors = InfluencingAccelerationsV(vessel, time);
            Vector3d FinalVelocityVector = new Vector3d();

            foreach (Vector3d Acceleration in InfluencingAccelerationVectors)
            {
                FinalVelocityVector = FinalVelocityVector + (Acceleration);
            }

            return FinalVelocityVector.magnitude;
        }

#endregion 

        #region ObjectManagement

        public void ManageBody(CelestialBody body)
        {
            // Work out this!
        }

        public void ManageVessel(Vessel vessel)
        {
            CurrentProcess = true;

            List<Vector3d> InfluencingAccelerationVectors = InfluencingAccelerationsV(vessel, HighLogic.CurrentGame.UniversalTime);

            Vector3d FinalVelocityVector = new Vector3d();

            foreach (Vector3d Acceleration in InfluencingAccelerationVectors)
            {
                FinalVelocityVector = FinalVelocityVector + (Acceleration * TimeWarp.CurrentRate);
            }

            SetOrbit(vessel, FinalVelocityVector);
        }

        #endregion 

        #region PlanetariumManagement

        public void ClearOrbitLines()
        {
            if (CurrentMeshRenderers.Count > 0)
            {
                foreach (MeshRenderer MeshRenderer in CurrentMeshRenderers)
                {
                    MeshRenderer.enabled = false;
                    Destroy(MeshRenderer);
                    CurrentMeshRenderers.Remove(MeshRenderer);
                }
            }
            if (CurrentLineGameObjects.Count > 0)
            {
                foreach (GameObject obj in CurrentLineGameObjects)
                {
                    obj.SetActive(false);
                    Destroy(obj);
                    CurrentLineGameObjects.Remove(obj);
                }
            }
        }

        public void ManageConics(Vessel vessel, double time)
        {
            FutureRenderOrbits.Clear();

            Orbit InitialOrbit = vessel.orbitDriver.orbit;
            double InitialTime = time;
            double OrbitalPeriod = vessel.orbitDriver.orbit.period; //Not used currently
            double TimewarpRate = TimeWarp.CurrentRate;
            double NoOfSteps = Settings.ReadNBCC();
            double TimeSnapshots = TimewarpRate / NoOfSteps;
            

            for (int i = 0; i < NoOfSteps; i++)
            {
                List<Vector3d> InfluencingAccelerationVectors = InfluencingAccelerationsV(vessel, time + (TimeSnapshots * i));

                Vector3d FinalVelocityVector = new Vector3d();

                foreach (Vector3d Acceleration in InfluencingAccelerationVectors)
                {
                    FinalVelocityVector = FinalVelocityVector + (Acceleration);
                }

                FutureRenderOrbits.Add(NewCalculatedOrbit(vessel, FinalVelocityVector, (time + (TimeSnapshots * i))));
            }

            foreach (Orbit orbit in FutureRenderOrbits)
            {
                Vector3 PositionAtStart = orbit.getTruePositionAtUT(time + (TimeSnapshots * FutureRenderOrbits.IndexOf(orbit)));
                Vector3 PositionAt1stDegree = orbit.getTruePositionAtUT(time + (TimeSnapshots * (FutureRenderOrbits.IndexOf(orbit) + (1.0 / 6.0))));
                Vector3 PositionAt2ndDegree = orbit.getTruePositionAtUT(time + (TimeSnapshots * (FutureRenderOrbits.IndexOf(orbit) + (2.0 / 6.0))));
                Vector3 PositionAt3rdDegree = orbit.getTruePositionAtUT(time + (TimeSnapshots * (FutureRenderOrbits.IndexOf(orbit) + (3.0 / 6.0))));
                Vector3 PositionAt4thDegree = orbit.getTruePositionAtUT(time + (TimeSnapshots * (FutureRenderOrbits.IndexOf(orbit) + (4.0 / 6.0))));
                Vector3 PositionAt5thDegree = orbit.getTruePositionAtUT(time + (TimeSnapshots * (FutureRenderOrbits.IndexOf(orbit) + (5.0 / 6.0))));
                Vector3 PositionAtEnd = orbit.getTruePositionAtUT(time + (TimeSnapshots * (FutureRenderOrbits.IndexOf(orbit) + 1)));

                // --- Dot to dot between each orbital segment to make a smooth curve --- //


                //LineRenderer OrbitBezierRenderer = new LineRenderer();

                MeshRenderer OrbitBezierRenderer;
                MeshFilter OrbitBezierFilter;
                
                GameObject OrbitBezier = new GameObject("OrbitBezierLine");


                if (OrbitBezier.GetComponent<MeshRenderer>() == null)
                {
                     OrbitBezierRenderer = OrbitBezier.AddComponent<MeshRenderer>();
                }
                if (OrbitBezier.GetComponent<MeshFilter>() == null)
                {
                     OrbitBezierFilter = OrbitBezier.AddComponent<MeshFilter>();
                }

                OrbitBezierFilter = OrbitBezier.GetComponent<MeshFilter>();
                OrbitBezierRenderer = OrbitBezier.GetComponent<MeshRenderer>();

                OrbitBezierFilter.mesh = new Mesh();
                OrbitBezierFilter.mesh.name = "OrbitBezierLine";
                OrbitBezierFilter.mesh.vertices = new Vector3[5];
                OrbitBezierFilter.mesh.uv = new Vector2[5] { new Vector2(0, 1), new Vector2(0,1), new Vector2(0, 0), new Vector2(1, 1), new Vector2(1, 0) };
                OrbitBezierFilter.mesh.SetIndices(new int[] { 0, 2, 1, 2, 3, 1 }, MeshTopology.Triangles, 0);
                OrbitBezierFilter.mesh.colors = Enumerable.Repeat(Color.red, 5).ToArray();
                
                lineMaterial = MapView.fetch.orbitLinesMaterial;

                OrbitBezierRenderer.material = lineMaterial;

                Vector3[] Points2D = new Vector3[4];
                Vector3[] Points3D = new Vector3[5];

                float LineWidth = 1.0f;

                var camera = PlanetariumCamera.Camera;
                var start = camera.WorldToScreenPoint(ScaledSpace.LocalToScaledSpace(PositionAtStart));
                var end = camera.WorldToScreenPoint(ScaledSpace.LocalToScaledSpace(PositionAtEnd));
                var segment = new Vector3(end.y - start.y, start.x - end.x, 0).normalized * (LineWidth / 2);

                 if (!MapView.Draw3DLines)
                 {
                    var dist = Screen.height / 2 + 0.01f;
                    start.z = start.z >= 0.15f ? dist : -dist;
                    end.z = end.z >= 0.15f ? dist : -dist;
                 }
                 OrbitBezier.layer = 31;

                 Points3D[0] = camera.ScreenToWorldPoint(PositionAt1stDegree);
                 Points3D[1] = camera.ScreenToWorldPoint(PositionAt2ndDegree);
                 Points3D[2] = camera.ScreenToWorldPoint(PositionAt3rdDegree);
                 Points3D[3] = camera.ScreenToWorldPoint(PositionAt4thDegree);
                 Points3D[4] = camera.ScreenToWorldPoint(PositionAt5thDegree);

                 OrbitBezierFilter.mesh.vertices = MapView.Draw3DLines ? Points3D : Points2D;
                 OrbitBezierFilter.mesh.RecalculateBounds();
                 OrbitBezierFilter.mesh.MarkDynamic();

                CurrentMeshRenderers.Add(OrbitBezierRenderer);
                CurrentLineGameObjects.Add(OrbitBezier);
                

                /*
                GameObject obj = null;

                var newMesh = new GameObject();
                newMesh.AddComponent<MeshFilter>();
                var renderer = newMesh.AddComponent<MeshRenderer>();
                renderer.enabled = true;

                renderer.receiveShadows = false;
                newMesh.layer = 31;
                obj.GetComponent<Renderer>().sharedMaterial = material;

                var mesh = obj.GetComponent<MeshFilter>().mesh;


                int steps = 128;
                double duration = 1000;
                double prevTA = orbit.TrueAnomalyAtUT(time + (TimeSnapshots * FutureRenderOrbits.IndexOf(orbit)));
                double prevTime = time + (TimeSnapshots * FutureRenderOrbits.IndexOf(orbit));

                double[] stepUT = new double[steps * 4];
                int utIdx = 0;
                double maxDT = Math.Max(1.0, duration / (double)steps);
                double maxDTA = 2.0 * Math.PI / (double)steps;
                stepUT[utIdx++] = time + (TimeSnapshots * FutureRenderOrbits.IndexOf(orbit));
                while (true)
                {
                    double t = prevTime + maxDT;
                    for (int count = 0; count < 100; ++count)
                    {
                        double ta = orbit.TrueAnomalyAtUT(t);
                        while (ta < prevTA)
                            ta += 2.0 * Math.PI;
                        if (ta - prevTA <= maxDTA)
                        {
                            prevTA = ta;
                            break;
                        }
                        t = (prevTime + t) * 0.5;
                    }

                    if (t > (time + (TimeSnapshots * FutureRenderOrbits.IndexOf(orbit))) + duration - (t - prevTime) * 0.5)
                        break;

                    prevTime = t;

                    stepUT[utIdx++] = t;
                    if (utIdx >= stepUT.Length - 1)
                    {
                        //Util.PostSingleScreenMessage("ut overflow", "ut overflow");
                        break; // this should never happen, but better stop than overflow if it does
                    }
                }
                stepUT[utIdx++] = (time + (TimeSnapshots * FutureRenderOrbits.IndexOf(orbit))) + duration;

                var vertices = new Vector3[utIdx * 2 + 2];
                var uvs = new Vector2[utIdx * 2 + 2];
                var triangles = new int[utIdx * 6];

                Vector3 prevMeshPos = PositionAtStart;
                for (int i = 0; i < utIdx; ++i)
                {
                    double t = stepUT[i];

                    Vector3 curMeshPos = PositionAtStart;

                    curMeshPos += PositionAt1stDegree;

                    
                    uvs[i * 2 + 0] = new Vector2(0.8f, 0);
                    uvs[i * 2 + 1] = new Vector2(0.8f, 1);

                    if (i > 0)
                    {
                        int idx = (i - 1) * 6;
                        triangles[idx + 0] = (i - 1) * 2 + 0;
                        triangles[idx + 1] = (i - 1) * 2 + 1;
                        triangles[idx + 2] = i * 2 + 1;

                        triangles[idx + 3] = (i - 1) * 2 + 0;
                        triangles[idx + 4] = i * 2 + 1;
                        triangles[idx + 5] = i * 2 + 0;
                    }

                    prevMeshPos = curMeshPos;
                }

                mesh.Clear();
                mesh.vertices = vertices;
                mesh.uv = uvs;
                mesh.colors = 
                mesh.triangles = triangles;
                mesh.RecalculateBounds();
                mesh.MarkDynamic();

                */


                /*
                OrbitDriver TempDriver = new OrbitDriver();
                TempDriver.orbit = orbit;
                Planetarium.Orbits.Add(TempDriver);
                TempDriver.Renderer.SetColor(Color.red);
                TempDriver.Renderer.DrawOrbit(OrbitRenderer.DrawMode.OFF);
                */





            }

        }

        public void PlanetariumManager(Vessel vessel, double time)
        {
            #region Spheres
            if (ToggleSphereOfInfluences)
                {
                    if (HighLogic.LoadedSceneHasPlanetarium)
                    {
                        foreach (CelestialBody body in FlightGlobals.Bodies)
                        {

                        }
                    }
                }

                else
                {
                    foreach (CelestialBody body in FlightGlobals.Bodies)
                    {
                        // Remove Lines from view
                    }
                }

                if (ToggleHillSpheres)
                {
                    if (HighLogic.LoadedSceneHasPlanetarium)
                    {
                        foreach (CelestialBody body in FlightGlobals.Bodies)
                        {

                        }
                    }
                }

                else
                {
                    foreach (CelestialBody body in FlightGlobals.Bodies)
                    {
                        // Remove Lines from view
                    }
                }
            #endregion 

                if (HighLogic.LoadedSceneHasPlanetarium)
                {

                if (TimeWarp.CurrentRate > 1)
                {
                    ManageConics(vessel, time);

                    Planetarium.fetch.UpdateCBs();
                    vessel.orbitDriver.CancelInvoke("drawOrbit");
                    vessel.orbitDriver.SetOrbitMode(OrbitDriver.UpdateMode.IDLE);
                    Planetarium.Orbits.Remove(vessel.orbitDriver);
                }
                else
                {
                    if (!Planetarium.Orbits.Contains(vessel.orbitDriver))
                    {
                        Planetarium.Orbits.Add(vessel.orbitDriver);
                    }
                    Planetarium.fetch.UpdateCBs();
                    vessel.orbitDriver.SetOrbitMode(OrbitDriver.UpdateMode.UPDATE);
                    vessel.orbitDriver.orbitColor = Color.red;
                }
            }
        }

        public Orbit NewCalculatedOrbit(Vessel vessel, Vector3d FinalVelocity, double time)
        {
            
            Orbit orbit = vessel.orbitDriver.orbit;
            CelestialBody oldBody = orbit.referenceBody;
            // vessel.orbitDriver.orbit.pos needs a fix!!!! 

            orbit.UpdateFromStateVectors(vessel.orbitDriver.orbit.pos, vessel.orbitDriver.orbit.vel + FinalVelocity, vessel.orbitDriver.orbit.referenceBody, time);
            orbit.inclination = vessel.orbitDriver.orbit.inclination;
            orbit.semiMajorAxis = vessel.orbitDriver.orbit.semiMajorAxis;
            orbit.eccentricity = vessel.orbit.eccentricity;
            orbit.LAN = vessel.orbit.LAN;
            orbit.argumentOfPeriapsis = vessel.orbit.argumentOfPeriapsis;
            orbit.meanAnomalyAtEpoch = vessel.orbit.meanAnomalyAtEpoch;
            orbit.epoch = vessel.orbit.epoch;
            orbit.referenceBody = vessel.orbit.referenceBody;
            var newBody = vessel.orbitDriver.orbit.referenceBody;

            if (newBody != oldBody)
            {
                var evnt = new GameEvents.HostedFromToAction<Vessel, CelestialBody>(vessel, oldBody, newBody);
                GameEvents.onVesselSOIChanged.Fire(evnt);
                VesselData.UpdateBody(vessel, newBody);
            }

            return orbit;
        }

        #endregion

        public void SetOrbit(Vessel vessel, Vector3d FinalVelocity)
        {
            PlanetariumManager(vessel, HighLogic.CurrentGame.UniversalTime);

            #region DepreciatedMethods
            /*
            if (TimeWarp.CurrentRate > 1.0)
            {
                Orbit PredictedOrbit = new Orbit();

                if (PredictedFutureOrbits.Keys.Count > 0)
                {
                    if (PredictedFutureOrbits.ContainsKey(vessel))
                    {
                        PredictedFutureOrbits.TryGetValue(vessel, out PredictedOrbit);
                    }

                    else
                    {
                        PredictedFutureOrbits.Add(vessel, vessel.orbitDriver.orbit);
                        PredictedOrbit = vessel.orbitDriver.orbit;
                    }
                }
                else
                {
                    PredictedFutureOrbits.Add(vessel, vessel.orbitDriver.orbit);
                    PredictedOrbit = vessel.orbitDriver.orbit;
                }




                // Map New Orbit at new time
 

            }
            else
            {
                /*
                double NewSemiMajorAxis = 1.0 / (-(Math.Pow(vessel.orbitDriver.orbit.vel.magnitude + (FinalVelocity.magnitude), 2.0) / vessel.orbitDriver.orbit.referenceBody.gravParameter) + (2.0 / (vessel.altitude + vessel.orbitDriver.orbit.referenceBody.Radius)));
                double MeanMotion = (360.0) / vessel.orbitDriver.orbit.period;
                double LANRecession = (((-(0.00338 * Math.Cos((vessel.orbitDriver.orbit.inclination))) / (MeanMotion * 24 * 60 * 60)))); // Manage these
                double LPEReccession = ((-(0.00169 * (4.0 - 5.0 * (Math.Pow(Math.Sin(vessel.orbitDriver.orbit.inclination), 2.0))))/ (MeanMotion * 24 * 60 * 60)));

                var orbit = vessel.orbitDriver.orbit;
                orbit.inclination = vessel.orbitDriver.orbit.inclination;
                orbit.semiMajorAxis = NewSemiMajorAxis;
                orbit.eccentricity = vessel.orbit.eccentricity;      
                orbit.LAN = vessel.orbit.LAN + LANRecession;
                orbit.argumentOfPeriapsis = vessel.orbit.argumentOfPeriapsis + LPEReccession;
                orbit.meanAnomalyAtEpoch = vessel.orbit.meanAnomalyAtEpoch;
                orbit.epoch = vessel.orbit.epoch;
                orbit.referenceBody = vessel.orbit.referenceBody;
                orbit.Init();
                orbit.UpdateFromUT(HighLogic.CurrentGame.UniversalTime);
                vessel.orbitDriver.pos = vessel.orbit.pos.xzy;
                vessel.orbitDriver.vel = vessel.orbit.vel;

                VesselData.UpdateVesselSMA(vessel, vessel.orbitDriver.orbit.semiMajorAxis);
                VesselData.UpdateVesselLPE(vessel, vessel.orbitDriver.orbit.argumentOfPeriapsis);
                VesselData.UpdateVesselLAN(vessel, vessel.orbitDriver.orbit.meanAnomaly);
                VesselData.UpdateVesselECC(vessel, vessel.orbitDriver.orbit.eccentricity);
                VesselData.UpdateVesselINC(vessel, vessel.orbitDriver.orbit.inclination);
                vessel.orbitDriver.UpdateOrbit();
                */

                // Priority Method: 

            /*
                double DeltaOrbitalEnergy = -(Math.Pow((FinalVelocity.magnitude), 2.0) / 2.0) - (vessel.orbitDriver.orbit.referenceBody.gravParameter / (vessel.orbitDriver.orbit.altitude));
                double NewEccentricity = Math.Sqrt(1.0 + ((2.0 * Math.Pow((vessel.orbitDriver.orbit.altitude), 2.0) * (vessel.orbitDriver.orbit.orbitalEnergy + DeltaOrbitalEnergy)) / vessel.orbitDriver.orbit.referenceBody.gravParameter)); // Change Grav Param?
                double NewSMA = -(vessel.orbitDriver.orbit.referenceBody.gravParameter / (vessel.orbitDriver.orbit.orbitalEnergy + DeltaOrbitalEnergy) * 2.0);
                double NewLPE = 0;
                double NewLAN = 0;
            

                print("New SemiMajor Axis: " + NewSMA);
                print("Old SemiMajor Axis: " + vessel.orbitDriver.orbit.semiMajorAxis);
                print("New Eccentricity: " + NewEccentricity);
                print("Old Eccentricity: " + vessel.orbitDriver.orbit.eccentricity);
            */
            #endregion

            print("Delta V: " + FinalVelocity.magnitude);
                Orbit orbit = NewCalculatedOrbit(vessel, FinalVelocity, HighLogic.CurrentGame.UniversalTime);

                orbit.Init();
                orbit.UpdateFromUT(HighLogic.CurrentGame.UniversalTime);

                vessel.orbitDriver.UpdateOrbit();
                VesselData.UpdateVesselSMA(vessel, vessel.orbitDriver.orbit.semiMajorAxis);
                VesselData.UpdateVesselLPE(vessel, vessel.orbitDriver.orbit.argumentOfPeriapsis);
                VesselData.UpdateVesselLAN(vessel, vessel.orbitDriver.orbit.meanAnomaly);
                VesselData.UpdateVesselECC(vessel, vessel.orbitDriver.orbit.eccentricity);
                VesselData.UpdateVesselINC(vessel, vessel.orbitDriver.orbit.inclination);

                CurrentProcess = false;

        }
    }
}
