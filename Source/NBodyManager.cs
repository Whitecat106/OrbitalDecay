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
        public static double GravitationalConstant = Math.Pow(6.67408 * 10, -11);
        public static bool CurrentProcess = false;
        public static bool PostTimewarpUpdateRequired = false;

        public static double TimeAtTimewarpStart = 0;

        public static Dictionary<Vessel, Orbit> VesselOrbitalPredictions = new Dictionary<Vessel, Orbit>();
        public static Dictionary<CelestialBody, Orbit> BodyOrbitalPredictions = new Dictionary<CelestialBody, Orbit>();

        public static List<Orbit> VesselFutureRenderOrbits = new List<Orbit>();
        public static List<MeshRenderer> CurrentMeshRenderers = new List<MeshRenderer>();
        public static List<GameObject> CurrentLineGameObjects = new List<GameObject>();
        public static Material lineMaterial;


        public static bool ToggleHillSpheres = false;
        public static bool ToggleSphereOfInfluences = true;

        public void Start()
        {
            GameEvents.onTimeWarpRateChanged.Add(TimewarpShift);
        }

        public void TimewarpShift()
        {
            if (TimeWarp.CurrentRate < 2)
            {
                TimeAtTimewarpStart = 0;
            }

            if (TimeWarp.CurrentRate > 2 && TimeAtTimewarpStart == 0)
            {
                TimeAtTimewarpStart = HighLogic.CurrentGame.UniversalTime;
            }
        }

        public void OnDestroy()
        {
            GameEvents.onTimeWarpRateChanged.Remove(TimewarpShift);
        }

        public void FixedUpdate()//
        {

            if (Settings.ReadNB() == true)
            {
                if (Time.timeSinceLevelLoad > 0.3)
                {
                    if (!HighLogic.LoadedSceneHasPlanetarium)
                    {
                        ClearOrbitLines();
                    }

                    if (VesselOrbitalPredictions.Keys.Count > 0)
                    {
                        foreach (Vessel v in VesselOrbitalPredictions.Keys)
                        {
                            if (!FlightGlobals.Vessels.Contains(v))
                            {
                                VesselOrbitalPredictions.Remove(v);
                            }
                        }
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

                                    foreach (Vessel v in FlightGlobals.Vessels)
                                    {
                                        if (DecayManager.CheckNBodyAltitude(v))
                                        {
                                            ManageOrbitPredictionsVessel(v);
                                        }
                                    }

                                    foreach (CelestialBody b in FlightGlobals.Bodies)
                                    {
                                        if (b != Sun.Instance.sun)
                                        {
                                            ManageOrbitalPredictionsBody(b);
                                        }
                                    }

                                    foreach (CelestialBody body in FlightGlobals.Bodies)
                                    {
                                        if (body != Sun.Instance.sun)
                                        {
                                            ManageBody(body);
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
        public static List<CelestialBody> InfluencingBodiesV(Vessel vessel)
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

        public static List<CelestialBody> InfluencingBodiesB(CelestialBody body)
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

        public static List<Vector3d> InfluencingAccelerationsB(Vessel vessel, double time)
        {
            List<Vector3d> InfluencingAccelerations = new List<Vector3d>();


            return InfluencingAccelerations;
        }

        public static List<Vector3d> InfluencingAccelerationsV(Vessel vessel, double time)
        {
            List<Vector3d> InfluencingAccelerations = new List<Vector3d>(); // Position at time

            foreach (CelestialBody Body in InfluencingBodiesV(vessel))
            {
                double VesselMass = VesselData.FetchMass(vessel);
                double VesselMNA = vessel.orbitDriver.orbit.GetMeanAnomaly(vessel.orbitDriver.orbit.E, time);
                double InfluencingForce = 0;
                double BodyMass = Body.Mass;
                Vector3d BodyPosition = new Vector3d();
                if (vessel.orbitDriver.orbit.referenceBody == Body || Body == Sun.Instance.sun)
                {
                    BodyPosition = new Vector3d(0, 0, 0);
                }
                else
                {
                    BodyPosition = Body.orbit.getRelativePositionAtUT(time);
                }
                double DistanceToVessel = Vector3d.Distance(BodyPosition, vessel.orbitDriver.orbit.getRelativePositionAtUT(time)); //
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

                Vector3d InfluencingAccelerationBodyDirectionVector = new Vector3d();
                if (vessel.orbitDriver.orbit.referenceBody == Body || Body == Sun.Instance.sun)
                {
                    InfluencingAccelerationBodyDirectionVector = new Vector3d(0, 0, 0);
                }
                else
                {
                    InfluencingAccelerationBodyDirectionVector = Body.orbit.getRelativePositionAtUT(time);
                }
                Vector3d VesselPositionVector = vessel.orbitDriver.orbit.getRelativePositionAtUT(time);
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


            // to do


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

        public static Vector3d GetMomentaryDeltaV(Vessel vessel, double time)
        {
            List<Vector3d> InfluencingAccelerationVectors = InfluencingAccelerationsV(vessel, time);
            Vector3d FinalVelocityVector = new Vector3d();

            foreach (Vector3d Acceleration in InfluencingAccelerationVectors)
            {
                FinalVelocityVector = FinalVelocityVector + (Acceleration);
            }

            return FinalVelocityVector;
        }

        #endregion

        #region ObjectManagement

        public static void ManageBody(CelestialBody body)
        {
            // Work out this!
        }

        public static void ManageVessel(Vessel vessel)
        {
            CurrentProcess = true;

            List<Vector3d> InfluencingAccelerationVectors = InfluencingAccelerationsV(vessel, HighLogic.CurrentGame.UniversalTime);

            Vector3d FinalVelocityVector = new Vector3d();

            foreach (Vector3d Acceleration in InfluencingAccelerationVectors)
            {
                FinalVelocityVector = FinalVelocityVector + (Acceleration);
            }

            SetOrbit(vessel, FinalVelocityVector);
        }

        #endregion

        #region PlanetariumManagement

        public static void ClearOrbitLines()
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

        public static void ManageVesselConics(Vessel vessel, double time)
        {

            VesselFutureRenderOrbits.Clear();

            Orbit InitialOrbit = vessel.orbitDriver.orbit;
            double InitialTime = time;
            double OrbitalPeriod = vessel.orbitDriver.orbit.period; //Not used currently
            double TimewarpRate = TimeWarp.CurrentRate;
            double NoOfSteps = Settings.ReadNBCC();
            double TimeSnapshots = TimewarpRate / NoOfSteps;

            /*
            for (int i = 0; i < NoOfSteps; i++)
            {
                List<Vector3d> InfluencingAccelerationVectors = InfluencingAccelerationsV(vessel, time + (TimeSnapshots * i));

                Vector3d FinalVelocityVector = new Vector3d();

                foreach (Vector3d Acceleration in InfluencingAccelerationVectors)
                {
                    FinalVelocityVector = FinalVelocityVector + (Acceleration);
                }

                VesselFutureRenderOrbits.Add(NewCalculatedOrbit(vessel, FinalVelocityVector, (time + (TimeSnapshots * i))));

                if (VesselOrbitalPredictions.ContainsKey(vessel))
                {
                    VesselOrbitalPredictions.Remove(vessel);
                    VesselOrbitalPredictions.Add(vessel, NewCalculatedOrbit(vessel, FinalVelocityVector, (time + (TimeSnapshots * i))));
                }
                else
                {
                    VesselOrbitalPredictions.Add(vessel, NewCalculatedOrbit(vessel, FinalVelocityVector, (time + (TimeSnapshots * i))));
                }
            }
             */

            foreach (Orbit orbit in VesselFutureRenderOrbits)
            {
                /*
                List<Vector3> PositionIncrements = new List<Vector3>();
                for (int i = 1; i < Math.Sqrt(TimewarpRate); i++ )
                {

                }
                 */
                LineRenderer line = null;
                GameObject obj = new GameObject("Line");

                line = obj.AddComponent<LineRenderer>();
                line.transform.parent = vessel.transform;
                line.useWorldSpace = false; // ...and moving along with it (rather 
                // than staying in fixed world coordinates)
                line.transform.localPosition = Vector3.zero;
                line.transform.localEulerAngles = Vector3.zero;

                Vector3 PositionAtStart = orbit.getTruePositionAtUT(time + (TimeSnapshots * VesselFutureRenderOrbits.IndexOf(orbit)));
                Vector3 PositionAtEnd = orbit.getTruePositionAtUT(time + (TimeSnapshots * (VesselFutureRenderOrbits.IndexOf(orbit) + 1)));
                print(PositionAtStart);
                print(PositionAtEnd);

                // Make it render a red to yellow triangle, 1 meter wide and 2 meters long
                line.material = MapView.fetch.dottedLineMaterial;
                line.SetColors(Color.red, Color.yellow);
                line.SetWidth(100, 0);
                line.SetVertexCount(50);
                line.SetPosition(0, ScaledSpace.LocalToScaledSpace(PositionAtStart));
                line.SetPosition(1, ScaledSpace.LocalToScaledSpace(PositionAtEnd));











                /*
                Vector3 PositionAtStart = orbit.getTruePositionAtUT(time + (TimeSnapshots * VesselFutureRenderOrbits.IndexOf(orbit)));
                Vector3 PositionAt1stDegree = orbit.getTruePositionAtUT(time + (TimeSnapshots * (VesselFutureRenderOrbits.IndexOf(orbit) + (1.0 / 6.0))));
                Vector3 PositionAt2ndDegree = orbit.getTruePositionAtUT(time + (TimeSnapshots * (VesselFutureRenderOrbits.IndexOf(orbit) + (2.0 / 6.0))));
                Vector3 PositionAt3rdDegree = orbit.getTruePositionAtUT(time + (TimeSnapshots * (VesselFutureRenderOrbits.IndexOf(orbit) + (3.0 / 6.0))));
                Vector3 PositionAt4thDegree = orbit.getTruePositionAtUT(time + (TimeSnapshots * (VesselFutureRenderOrbits.IndexOf(orbit) + (4.0 / 6.0))));
                Vector3 PositionAt5thDegree = orbit.getTruePositionAtUT(time + (TimeSnapshots * (VesselFutureRenderOrbits.IndexOf(orbit) + (5.0 / 6.0))));
                Vector3 PositionAtEnd = orbit.getTruePositionAtUT(time + (TimeSnapshots * (VesselFutureRenderOrbits.IndexOf(orbit) + 1)));

                // --- Dot to dot between each orbital segment to make a smooth curve --- //
                
                // Convert to scaled space 

                PositionAtStart = ScaledSpace.LocalToScaledSpace(PositionAtStart);
                PositionAt1stDegree = ScaledSpace.LocalToScaledSpace(PositionAt1stDegree);
                PositionAt2ndDegree = ScaledSpace.LocalToScaledSpace(PositionAt2ndDegree);
                PositionAt3rdDegree = ScaledSpace.LocalToScaledSpace(PositionAt3rdDegree);
                PositionAt4thDegree = ScaledSpace.LocalToScaledSpace(PositionAt4thDegree);
                PositionAt5thDegree = ScaledSpace.LocalToScaledSpace(PositionAt5thDegree);
                PositionAtEnd = ScaledSpace.LocalToScaledSpace(PositionAtEnd);

                //


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
                var start = camera.WorldToScreenPoint((PositionAtStart));
                var end = camera.WorldToScreenPoint((PositionAtEnd));
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

        public static void PlanetariumManager(Vessel vessel, double time)
        {
            #region Spheres
            if (ToggleSphereOfInfluences)
            {
                if (HighLogic.LoadedSceneHasPlanetarium)
                {
                    foreach (CelestialBody body in FlightGlobals.Bodies)
                    {
                        // Add lines
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
                        // Add lines
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
                    // ManageVesselConics(vessel, time);

                    Planetarium.fetch.UpdateCBs();
                    vessel.orbitDriver.CancelInvoke("drawOrbit");
                    //vessel.orbitDriver.SetOrbitMode(OrbitDriver.UpdateMode.IDLE);
                    //Planetarium.Orbits.Remove(vessel.orbitDriver);
                }
                else
                {
                    if (!Planetarium.Orbits.Contains(vessel.orbitDriver))
                    {
                        Planetarium.Orbits.Add(vessel.orbitDriver);
                    }
                    //Planetarium.fetch.UpdateCBs();
                    //vessel.orbitDriver.SetOrbitMode(OrbitDriver.UpdateMode.UPDATE);
                }
            }
        }
        #endregion


        #region OrbitManagement

        public static Orbit NewCalculatedOrbit(Vessel vessel, Orbit oldOrbit, Vector3d FinalVelocity, double time)
        {
            Orbit orbit = new Orbit();
            CelestialBody oldBody = orbit.referenceBody;

            if (VesselOrbitalPredictions.ContainsKey(vessel))
            {
                VesselOrbitalPredictions.TryGetValue(vessel, out orbit);
            }
            else
            {
                orbit = oldOrbit;
                VesselOrbitalPredictions.Add(vessel, oldOrbit);
            }

            //orbit.UpdateFromStateVectors(orbit.getRelativePositionAtUT(time), orbit.vel, vessel.orbitDriver.orbit.referenceBody, time);
            //orbit.UpdateFromStateVectors(oldOrbit.getRelativePositionAtUT(time), oldOrbit.vel + (FinalVelocity), vessel.orbitDriver.orbit.referenceBody, time);

            // Debuging // 
            #region Debug Values
            print("DeltaV Vector: " + FinalVelocity);
            print("DeltaV Scalar: " + FinalVelocity.magnitude);
            print("OLDSMA: " + vessel.orbitDriver.orbit.semiMajorAxis);
            print("NEWSMA: " + orbit.semiMajorAxis); //CalculateSMA(vessel, oldOrbit.getOrbitalSpeedAtRelativePos(oldOrbit.getRelativePositionAtUT(time)) * Vector3d.one + FinalVelocity, time, 1.0));
            print("OLDEcc: " + vessel.orbitDriver.orbit.eccentricity);
            print("NEWEcc: " + orbit.eccentricity); //CalculateEccentricity(vessel, oldOrbit.getOrbitalSpeedAtRelativePos(oldOrbit.getRelativePositionAtUT(time)) * Vector3d.one + FinalVelocity, time, 1.0));
            print("OLDLAN: " + vessel.orbitDriver.orbit.LAN);
            print("NEWLAN: " + orbit.LAN);//CalculateLPE(vessel, oldOrbit.getOrbitalSpeedAtRelativePos(oldOrbit.getRelativePositionAtUT(time)) * Vector3d.one + FinalVelocity, time, 1.0));
            print("OLDLpe: " + vessel.orbitDriver.orbit.argumentOfPeriapsis);
            print("NEWLpe: " + orbit.argumentOfPeriapsis); // CalculateLPE(vessel, oldOrbit.getOrbitalSpeedAtRelativePos(oldOrbit.getRelativePositionAtUT(time)) * Vector3d.one + FinalVelocity, time, 1.0));
            # endregion

            var newBody = vessel.orbitDriver.orbit.referenceBody;

            if (newBody != oldBody)
            {
                var evnt = new GameEvents.HostedFromToAction<Vessel, CelestialBody>(vessel, oldBody, newBody);
                GameEvents.onVesselSOIChanged.Fire(evnt);
                VesselData.UpdateBody(vessel, newBody);
            }

            VesselOrbitalPredictions.Remove(vessel);
            VesselOrbitalPredictions.Add(vessel, orbit);

            return orbit;
        }

        #region depreciated non state vectors
        public static double CalculateInclination(Vessel vessel, Vector3d FinalVelocity, double time, double timeinterval)
        {
            double Inc = 0;
            Orbit NextOrbit = new Orbit();
            NextOrbit.UpdateFromStateVectors(vessel.orbitDriver.orbit.getRelativePositionAtUT(time), FinalVelocity, vessel.orbitDriver.orbit.referenceBody, time);



            // Do this

            return Inc;
        }

        public static double CalculateSMA(Vessel vessel, Vector3d FinalVelocity, double time, double timeinterval)
        {
            double SMA = 0;
            double Altitude = vessel.orbitDriver.orbit.altitude + vessel.orbitDriver.orbit.referenceBody.Radius;
            double CalculationEnergyError = (((Math.Pow(vessel.orbitDriver.orbit.vel.magnitude, 2.0)) / 2.0) - (vessel.orbitDriver.orbit.referenceBody.gravParameter / Altitude)) - vessel.orbitDriver.orbit.orbitalEnergy;

            double ForwardVelocity = FinalVelocity.magnitude;
            double NewEnergy = (((Math.Pow(ForwardVelocity, 2.0)) / 2.0) - (vessel.orbitDriver.orbit.referenceBody.gravParameter / Altitude)) - CalculationEnergyError;

            SMA = ((-(vessel.orbitDriver.orbit.referenceBody.gravParameter) / NewEnergy) / 2.0);

            return Math.Abs(SMA);
        }

        public static double CalculateEccentricity(Vessel vessel, Vector3d FinalVelocity, double time, double timeinterval)
        {
            double Eccentricity = 0;

            double ForwardVelocity = FinalVelocity.magnitude;
            double Altitude = vessel.orbitDriver.orbit.altitude + vessel.orbitDriver.orbit.referenceBody.Radius;
            double NewEnergy = ((Math.Pow(ForwardVelocity, 2.0)) / 2.0) - (vessel.orbitDriver.orbit.referenceBody.gravParameter / Altitude);
            double CalculationEnergyError = (((Math.Pow(vessel.orbitDriver.orbit.vel.magnitude, 2.0)) / 2.0) - (vessel.orbitDriver.orbit.referenceBody.gravParameter / Altitude)) + vessel.orbitDriver.orbit.orbitalEnergy;


            Orbit NextOrbit = new Orbit();
            NextOrbit.UpdateFromStateVectors(vessel.orbitDriver.orbit.getRelativePositionAtUT(time), FinalVelocity, vessel.orbitDriver.orbit.referenceBody, time);

            double MomentOfInertia = Math.Pow(vessel.orbitDriver.orbit.pos.magnitude, 2.0) * VesselData.FetchMass(vessel);
            Vector3d AngularVelocity = Vector3d.Cross(vessel.orbitDriver.orbit.getRelativePositionAtUT(time), FinalVelocity) / (Math.Pow(vessel.orbitDriver.orbit.getRelativePositionAtUT(time).magnitude, 2.0));
            Vector3d AngularMomentumVector = AngularVelocity * MomentOfInertia; // Maybe this if not then calculate


            Eccentricity = Math.Sqrt((1.0 + ((2.0 * (NewEnergy - CalculationEnergyError) * Math.Pow(AngularMomentumVector.magnitude, 2.0)) / (Math.Pow(vessel.orbitDriver.orbit.referenceBody.gravParameter, 2.0)))));

            return Eccentricity;
        }

        public static double CalculateLAN(Vessel vessel, Vector3d FinalVelocity, double time, double timeinterval)
        {
            double LAN = 0;

            Orbit NextOrbit = new Orbit();
            NextOrbit.UpdateFromStateVectors(vessel.orbitDriver.orbit.pos, FinalVelocity, vessel.orbitDriver.orbit.referenceBody, time);

            double MomentOfInertia = Math.Pow(vessel.orbitDriver.orbit.getRelativePositionAtUT(time).magnitude, 2.0) * VesselData.FetchMass(vessel);
            Vector3d AngularVelocity = Vector3d.Cross(vessel.orbitDriver.orbit.getRelativePositionAtUT(time), FinalVelocity) / (Math.Pow(vessel.orbitDriver.orbit.getRelativePositionAtUT(time).magnitude, 2.0));

            Vector3d AngularMomentumVector = AngularVelocity * MomentOfInertia; // Maybe this if not then calculate

            Vector3d NeutralVector = new Vector3d(0, 0, 1);

            Vector3d AscendingNodeVector = Vector3d.Cross(AngularMomentumVector, NeutralVector); // Maybe this

            if (AscendingNodeVector.y < 0)
            {
                LAN = (2.0 * Math.PI) - Math.Acos(AscendingNodeVector.x / AscendingNodeVector.magnitude);
            }

            else
            {
                LAN = Math.Acos(AscendingNodeVector.x / AscendingNodeVector.magnitude);
            }

            LAN = UtilMath.RadiansToDegrees(LAN);
            return LAN;
        }

        public static double CalculateLPE(Vessel vessel, Vector3d FinalVelocity, double time, double timeinterval)
        {
            double LPE = 0;

            Orbit NextOrbit = new Orbit();
            NextOrbit.UpdateFromStateVectors(vessel.orbitDriver.orbit.getRelativePositionAtUT(time), FinalVelocity, vessel.orbitDriver.orbit.referenceBody, time);


            double MomentOfInertia = Math.Pow(vessel.orbitDriver.orbit.getRelativePositionAtUT(time).magnitude, 2.0) * VesselData.FetchMass(vessel);
            Vector3d AngularVelocity = Vector3d.Cross(vessel.orbitDriver.orbit.getRelativePositionAtUT(time), FinalVelocity) / (Math.Pow(vessel.orbitDriver.orbit.getRelativePositionAtUT(time).magnitude, 2.0));
            Vector3d AngularMomentumVector = AngularVelocity * MomentOfInertia; // Maybe this if not then calculate

            Vector3d NeutralVector = new Vector3d(0, 0, 1);
            Vector3d AscendingNodeVector = Vector3d.Cross(AngularMomentumVector, NeutralVector); // Maybe this 
            Vector3d EccentricVector = NextOrbit.eccVec;

            LPE = Math.Acos((Vector3d.Cross(AscendingNodeVector, EccentricVector).magnitude) / (AscendingNodeVector.magnitude * EccentricVector.magnitude));

            LPE = UtilMath.RadiansToDegrees(LPE);
            return LPE;
        }
        #endregion

        #endregion

        public static void ManageOrbitalPredictionsBody(CelestialBody body)
        {
            Orbit orbit = new Orbit();

            if (BodyOrbitalPredictions.ContainsKey(body))
            {
                BodyOrbitalPredictions.TryGetValue(body, out orbit);
            }

            else
            {
                BodyOrbitalPredictions.Add(body, body.orbitDriver.orbit);
                orbit = body.orbitDriver.orbit;
            }





        }

        public static void ManageOrbitPredictionsVessel(Vessel vessel) // Called per second
        {
            Orbit orbit = new Orbit();

            if (VesselOrbitalPredictions.ContainsKey(vessel))
            {
                VesselOrbitalPredictions.TryGetValue(vessel, out orbit);
            }

            else
            {
                VesselOrbitalPredictions.Add(vessel, vessel.orbitDriver.orbit);
                orbit = vessel.orbitDriver.orbit;
            }


            Vector3d FinalVelVector = new Vector3d();
            Vector3d InitialVelVector = GetMomentaryDeltaV(vessel, HighLogic.CurrentGame.UniversalTime - TimeWarp.CurrentRate);


            if (TimeWarp.CurrentRate > 100 && TimeWarp.CurrentRate <= 1000) // Anti Lag Method
            {
                for (int i = 0; i < TimeWarp.CurrentRate / 10.0; i++)
                {
                    List<Vector3d> InfluencingAccelerationVectors = InfluencingAccelerationsV(vessel, TimeAtTimewarpStart + i);

                    Vector3d FinalVelocityVector = new Vector3d();

                    foreach (Vector3d Acceleration in InfluencingAccelerationVectors)
                    {
                        FinalVelocityVector = FinalVelocityVector + (Acceleration);
                    }

                    FinalVelVector = FinalVelVector + FinalVelocityVector;

                    // orbit = NewCalculatedOrbit(vessel, orbit, FinalVelocityVector , TimeAtTimewarpStart + i);
                }
            }
            if (TimeWarp.CurrentRate > 1000 && TimeWarp.CurrentRate <= 10000) // Anti Lag Method
            {
                for (int i = 0; i < TimeWarp.CurrentRate / 100.0; i++)
                {
                    List<Vector3d> InfluencingAccelerationVectors = InfluencingAccelerationsV(vessel, TimeAtTimewarpStart + i);

                    Vector3d FinalVelocityVector = new Vector3d();

                    foreach (Vector3d Acceleration in InfluencingAccelerationVectors)
                    {
                        FinalVelocityVector = FinalVelocityVector + (Acceleration);
                    }

                    FinalVelVector = FinalVelVector + FinalVelocityVector;

                    // orbit = NewCalculatedOrbit(vessel, orbit, FinalVelocityVector, TimeAtTimewarpStart + i);
                }
            }

            if (TimeWarp.CurrentRate > 10000 && TimeWarp.CurrentRate <= 100000)
            {
                for (int i = 0; i < TimeWarp.CurrentRate / 1000.0; i++)
                {
                    List<Vector3d> InfluencingAccelerationVectors = InfluencingAccelerationsV(vessel, TimeAtTimewarpStart + i);

                    Vector3d FinalVelocityVector = new Vector3d();

                    foreach (Vector3d Acceleration in InfluencingAccelerationVectors)
                    {
                        FinalVelocityVector = FinalVelocityVector + (Acceleration);
                    }

                    FinalVelVector = FinalVelVector + FinalVelocityVector;

                    // orbit = NewCalculatedOrbit(vessel, orbit, FinalVelocityVector, TimeAtTimewarpStart + i);
                }
            }

            if (TimeWarp.CurrentRate > 100000)
            {
                for (int i = 0; i < TimeWarp.CurrentRate / 10000.0; i++)
                {
                    List<Vector3d> InfluencingAccelerationVectors = InfluencingAccelerationsV(vessel, TimeAtTimewarpStart + i);

                    Vector3d FinalVelocityVector = new Vector3d();

                    foreach (Vector3d Acceleration in InfluencingAccelerationVectors)
                    {
                        FinalVelocityVector = FinalVelocityVector + (Acceleration);
                    }

                    FinalVelVector = FinalVelVector + FinalVelocityVector;

                    // orbit = NewCalculatedOrbit(vessel, orbit, FinalVelocityVector, TimeAtTimewarpStart + i);
                }
            }

            else
            {
                for (int i = 0; i < TimeWarp.CurrentRate; i++)
                {
                    List<Vector3d> InfluencingAccelerationVectors = InfluencingAccelerationsV(vessel, TimeAtTimewarpStart + i);

                    Vector3d FinalVelocityVector = new Vector3d();

                    foreach (Vector3d Acceleration in InfluencingAccelerationVectors)
                    {
                        FinalVelocityVector = FinalVelocityVector + (Acceleration);
                    }


                    FinalVelVector = FinalVelVector + (FinalVelocityVector);

                    //orbit = NewCalculatedOrbit(vessel, orbit, FinalVelocityVector, TimeAtTimewarpStart + i);
                }
            }

            FinalVelVector = InitialVelVector - FinalVelVector;
            print("Change in delta V across timewarp duration in one second: " + FinalVelVector.magnitude);

            orbit = BuildOrbitFromStateVectors.BuildFromStateVectors(orbit.getRelativePositionAtUT(HighLogic.CurrentGame.UniversalTime), orbit.vel + FinalVelVector, orbit.referenceBody, HighLogic.CurrentGame.UniversalTime);

            // Is this actively updating the orbit? If So... why the bloody hell so?!!?!? 
            //orbit.UpdateFromStateVectors(orbit.getRelativePositionAtUT(HighLogic.CurrentGame.UniversalTime), orbit.vel + FinalVelVector, orbit.referenceBody, HighLogic.CurrentGame.UniversalTime);

            VesselOrbitalPredictions.Remove(vessel);
            VesselOrbitalPredictions.Add(vessel, orbit);

            TimeAtTimewarpStart = HighLogic.CurrentGame.UniversalTime;

        }

        public static void SetOrbit(Vessel vessel, Vector3d FinalVelocity)
        {
            //PlanetariumManager(vessel, HighLogic.CurrentGame.UniversalTime);

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

            Orbit orbit = new Orbit();
            orbit = NewCalculatedOrbit(vessel, vessel.orbitDriver.orbit, FinalVelocity, HighLogic.CurrentGame.UniversalTime);

            if (TimeWarp.CurrentRate < 2)
            {

                orbit.Init();

                VesselData.UpdateVesselSMA(vessel, vessel.orbitDriver.orbit.semiMajorAxis);
                VesselData.UpdateVesselLPE(vessel, vessel.orbitDriver.orbit.argumentOfPeriapsis);
                VesselData.UpdateVesselLAN(vessel, vessel.orbitDriver.orbit.meanAnomaly);
                VesselData.UpdateVesselECC(vessel, vessel.orbitDriver.orbit.eccentricity);
                VesselData.UpdateVesselINC(vessel, vessel.orbitDriver.orbit.inclination);
            }
            //orbit.UpdateFromUT(HighLogic.CurrentGame.UniversalTime);
            CurrentProcess = false;
        }
    }




    public static class BuildOrbitFromStateVectors
    {
        public static Orbit BuildFromStateVectors(Vector3d position, Vector3d velocity, CelestialBody body, double UniversalTime)
        {
            Orbit StateVectorBuiltOrbit = new Orbit();

            

            
            double NewSemiMajorAxis = 0;
            double NewInclination = 0;
            double NewEccentricity = 0;
            double NewLAN = 0;
            double NewLPE = 0;
            double NewEPH = 0;
            double NewMNA = 0; 
            

            return StateVectorBuiltOrbit;
        }
    }


}