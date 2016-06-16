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

        public void FixedUpdate()
        {
            if (CurrentProcess == false)
            {
                if (HighLogic.LoadedSceneIsGame && (HighLogic.LoadedScene != GameScenes.LOADING && HighLogic.LoadedScene != GameScenes.LOADINGBUFFER && HighLogic.LoadedScene != GameScenes.MAINMENU))
                {
                    VariableUpdateInterval = 1.0f / TimeWarp.CurrentRate;

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

        public void ManageBody(CelestialBody body)
        {
            // Work out this!


        }

        public void ManageVessel(Vessel vessel)
        {
            CurrentProcess = true;

            CelestialBody ReferenceBody = vessel.orbitDriver.referenceBody;
            double VesselMass = VesselData.FetchMass(vessel);
            Vector3d VesselVelocity = vessel.orbitDriver.orbit.vel; //vessel.orbitDriver.orbit.getOrbitalVelocityAtUT(HighLogic.CurrentGame.UniversalTime).magnitude; // Possibly Change This
            double VesselAltitude = vessel.altitude + ReferenceBody.Radius;
            double VesselMNA = UtilMath.RadiansToDegrees(vessel.orbitDriver.orbit.GetMeanAnomaly(vessel.orbitDriver.orbit.E, HighLogic.CurrentGame.UniversalTime));
            List<CelestialBody> InfluencingBodies = new List<CelestialBody>();
            List<Vector3d> InfluencingAccelerationVectors = new List<Vector3d>();

            foreach (CelestialBody Body in FlightGlobals.Bodies)
            {
                if (ReferenceBody.HasChild(Body) || ReferenceBody.HasParent(Body))
                {
                    InfluencingBodies.Add(Body);
                    //print("Influencing Body: " + Body.name + " added.");
                }
            }

            foreach (CelestialBody Body in InfluencingBodies)
            {
                double InfluencingForce = 0;
                double BodyMass = Body.Mass;
                double DistanceToVessel = Vector3d.Distance(Body.position, vessel.GetWorldPos3D());
                print("Body " + Body.name + " distance to " + vessel.name + " : "+ DistanceToVessel);
                double BodyMNA = 0;

                try
                {
                     BodyMNA = UtilMath.RadiansToDegrees(Body.orbitDriver.orbit.GetMeanAnomaly(Body.orbitDriver.orbit.E, HighLogic.CurrentGame.UniversalTime));
                }
                catch (NullReferenceException)
                {
                     BodyMNA = 0;
                }

                double MNADifference = DifferenceBetweenMNA(VesselMNA, BodyMNA);

                InfluencingForce = - (GravitationalConstant * BodyMass * VesselMass) / (DistanceToVessel * DistanceToVessel);
                InfluencingForce = InfluencingForce * Math.Sin(MNADifference - 90.0);

                //print("Influencing Force from " + Body.name + " : " + (InfluencingForce) + "N.");

                Vector3d InfluencingAccelerationBodyDirectionVector = Body.position;
                Vector3d VesselPositionVector = vessel.GetWorldPos3D();
                Vector3d InfluencingAccelerationVector = new Vector3d(InfluencingAccelerationBodyDirectionVector.x - VesselPositionVector.x, InfluencingAccelerationBodyDirectionVector.y - VesselPositionVector.y, InfluencingAccelerationBodyDirectionVector.z - VesselPositionVector.z) * ((InfluencingForce / VesselMass) / TimeWarp.CurrentRate);


                InfluencingAccelerationVectors.Add(InfluencingAccelerationVector);
            }

            Vector3d FinalVelocityVector = Vector3d.zero;

            foreach (Vector3d Acceleration in InfluencingAccelerationVectors)
            {
                FinalVelocityVector = FinalVelocityVector + (Acceleration);
            }

            SetOrbit(vessel, FinalVelocityVector);
        }

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
       
        public void SetOrbit(Vessel vessel, Vector3d FinalVelocity)
        {
            
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
            CurrentProcess = false;
            

            /*
            double NewOrbitalEnergy = - (Math.Pow((vessel.orbitDriver.orbit.vel.x + FinalVelocity.x + vessel.orbitDriver.orbit.vel.y + FinalVelocity.y), 2.0) / 2.0) - (vessel.orbitDriver.orbit.referenceBody.gravParameter / (vessel.orbitDriver.orbit.altitude + vessel.orbitDriver.orbit.referenceBody.Radius));
            double NewEccentricity = Math.Sqrt(1.0 + ((2.0 * Math.Pow((vessel.orbitDriver.orbit.altitude), 2.0) * NewOrbitalEnergy) / vessel.orbitDriver.orbit.referenceBody.gravParameter)); // Change Grav Param?
            double NewSMA = - (vessel.orbitDriver.orbit.referenceBody.gravParameter / NewOrbitalEnergy) / 2.0;
            double NewLPE = 0;
            double NewLAN = 0;
           */
            /*

                Orbit orbit = vessel.orbitDriver.orbit;
                orbit.UpdateFromStateVectors(vessel.orbitDriver.orbit.pos, vessel.orbitDriver.orbit.vel + FinalVelocity, vessel.orbitDriver.orbit.referenceBody, HighLogic.CurrentGame.UniversalTime);
                orbit.inclination = vessel.orbitDriver.orbit.inclination;
                orbit.semiMajorAxis = vessel.orbitDriver.orbit.semiMajorAxis;
                orbit.eccentricity = vessel.orbit.eccentricity;
                orbit.LAN = vessel.orbit.LAN;
                orbit.argumentOfPeriapsis = vessel.orbit.argumentOfPeriapsis;
                orbit.meanAnomalyAtEpoch = vessel.orbit.meanAnomalyAtEpoch;
                orbit.epoch = vessel.orbit.epoch;
                orbit.referenceBody = vessel.orbit.referenceBody;
                orbit.Init();
                orbit.UpdateFromUT(HighLogic.CurrentGame.UniversalTime);

                vessel.orbitDriver.UpdateOrbit();
                VesselData.UpdateVesselSMA(vessel, vessel.orbitDriver.orbit.semiMajorAxis);
                VesselData.UpdateVesselLPE(vessel, vessel.orbitDriver.orbit.argumentOfPeriapsis);
                VesselData.UpdateVesselLAN(vessel, vessel.orbitDriver.orbit.meanAnomaly);
                VesselData.UpdateVesselECC(vessel, vessel.orbitDriver.orbit.eccentricity);
                VesselData.UpdateVesselINC(vessel, vessel.orbitDriver.orbit.inclination);
             */
        }
    }
}
