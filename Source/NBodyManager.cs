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

        public void FixedUpdate()
        {
            if (HighLogic.LoadedSceneIsGame && (HighLogic.LoadedScene != GameScenes.LOADING && HighLogic.LoadedScene != GameScenes.LOADINGBUFFER && HighLogic.LoadedScene != GameScenes.MAINMENU))
            {
                VariableUpdateInterval = 1.0f;

                if (Time.timeSinceLevelLoad > 1)
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
                            if (vessel.vesselType != VesselType.SpaceObject && vessel.vesselType != VesselType.Unknown) // 
                            {
                                if (VesselData.FetchStationKeeping(vessel) == false)
                                {
                                    ManageVessel(vessel);
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
            CelestialBody ReferenceBody = vessel.orbitDriver.referenceBody;
            double VesselMass = VesselData.FetchMass(vessel);
            double VesselVelocity = vessel.orbitDriver.orbit.getOrbitalVelocityAtUT(HighLogic.CurrentGame.UniversalTime).magnitude; // Possibly Change This
            double VesselAltitude = vessel.altitude + ReferenceBody.Radius;
            double VesselMNA = UtilMath.RadiansToDegrees(vessel.orbitDriver.orbit.meanAnomaly);
            List<CelestialBody> InfluencingBodies = new List<CelestialBody>();
            List<double> InfluencingAccelerations = new List<double>();

            foreach (CelestialBody Body in FlightGlobals.Bodies)
            {
                if (ReferenceBody.HasChild(Body) || ReferenceBody.HasParent(Body))
                {
                    InfluencingBodies.Add(Body);
                    print("Influencing Body: " + Body.name + " added.");
                }
            }

            foreach (CelestialBody Body in InfluencingBodies)
            {
                double InfluencingForce = 0;
                double BodyMass = Body.Mass;
                double DistanceToVessel = Vector3d.Distance(Body.getPositionAtUT(HighLogic.CurrentGame.UniversalTime), vessel.GetWorldPos3D());
                print("Body " + Body.name + " distance to " + vessel.name + " : "+ DistanceToVessel);
                double BodyMNA = 0;

                try
                {
                     BodyMNA = UtilMath.RadiansToDegrees(Body.orbitDriver.orbit.meanAnomaly);
                }
                catch (NullReferenceException)
                {
                     BodyMNA = 0;
                }

                double MNADifference = DifferenceBetweenMNA(VesselMNA, BodyMNA);

                InfluencingForce = -(GravitationalConstant * BodyMass * VesselMass) / (DistanceToVessel * DistanceToVessel);
                print("Influencing Force from " + Body.name + " : " + (InfluencingForce * TimeWarp.CurrentRate) + "N.");

                InfluencingForce = InfluencingForce * Math.Sin(MNADifference - 90.0);

                double InfluencingAcceleration = InfluencingForce / VesselMass;
                InfluencingAccelerations.Add(InfluencingAcceleration);
            }

            double FinalVelocity = VesselVelocity;
            foreach (double Acceleration in InfluencingAccelerations)
            {
                print("Acceleration Change: " + Acceleration);
                FinalVelocity = FinalVelocity + (Acceleration * TimeWarp.CurrentRate);
                print("Final Velocity of Vessel: " + FinalVelocity);
            }

            SetOrbit(vessel, FinalVelocity);
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
       
        public void SetOrbit(Vessel vessel, double FinalVelocity)
        {
            
            double NewSemiMajorAxis = 1.0 / (-(Math.Pow(FinalVelocity, 2.0) / vessel.orbitDriver.orbit.referenceBody.gravParameter) + (2.0 / (vessel.altitude + vessel.orbitDriver.orbit.referenceBody.Radius)));

            var orbit = vessel.orbitDriver.orbit;
            orbit.inclination = vessel.orbitDriver.orbit.inclination;
            orbit.semiMajorAxis = NewSemiMajorAxis;
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
             
            /*
            Orbit tempOrbit = vessel.orbitDriver.orbit;
            tempOrbit.UpdateFromStateVectors(vessel.orbitDriver.orbit.pos, vessel.orbitDriver.orbit.vel + (Vector3d.one * FinalVelocity), vessel.orbitDriver.orbit.referenceBody, Planetarium.GetUniversalTime());
            var orbit = vessel.orbitDriver.orbit;
            orbit.inclination = vessel.orbitDriver.orbit.inclination;
            orbit.semiMajorAxis = vessel.orbitDriver;
            orbit.eccentricity = vessel.orbit.eccentricity;
            orbit.LAN = vessel.orbit.LAN;
            orbit.argumentOfPeriapsis = vessel.orbit.argumentOfPeriapsis;
            orbit.meanAnomalyAtEpoch = vessel.orbit.meanAnomalyAtEpoch;
            orbit.epoch = vessel.orbit.epoch;
            orbit.referenceBody = vessel.orbit.referenceBody;
            orbit.Init();
            orbit.UpdateFromUT(HighLogic.CurrentGame.UniversalTime);
            */
        }

        public void OnDestroy()
        {


        }
    }
}
