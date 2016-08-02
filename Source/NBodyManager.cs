
// REBUILD THIS FOR 1.6.0 USING #77 Comments


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

        public static bool ToggleHillSpheres = false;
        public static bool ToggleSphereOfInfluences = true;


        public static void TimewarpShift()
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

        #region InfluencingBodyLists
        public static List<CelestialBody> InfluencingBodiesV(Vessel vessel)
        {
            List<CelestialBody> InfluencingBodies = new List<CelestialBody>();
            CelestialBody ReferenceBody = vessel.orbitDriver.orbit.referenceBody;

            foreach (CelestialBody Body in FlightGlobals.Bodies)
            {
                if ((ReferenceBody.HasChild(Body) ))//|| ReferenceBody.HasParent(Body)) && ReferenceBody != Sun.Instance.sun && Body != Sun.Instance) // No sun for now
                {
                    InfluencingBodies.Add(Body);
                }
            }

            return InfluencingBodies;
        }

        public static List<CelestialBody> InfluencingBodiesV(Orbit orbit)
        {
            List<CelestialBody> InfluencingBodies = new List<CelestialBody>();
            CelestialBody ReferenceBody = orbit.referenceBody;

            foreach (CelestialBody Body in FlightGlobals.Bodies)
            {
                if ((ReferenceBody.HasChild(Body)))//|| ReferenceBody.HasParent(Body)) && ReferenceBody != Sun.Instance.sun && Body != Sun.Instance) // No sun for now
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
                if (VesselMass == 0) // Incase vesselData hasnt caught up! 
                {
                    VesselMass = vessel.GetTotalMass() * 1000;
                }
 
                double PhaseAngle = FindPhaseAngleBetweenObjects(vessel.orbit, Body.orbit);

                double InfluencingForce = 0;
                double BodyMass = Body.Mass;
                Vector3d BodyPosition = new Vector3d();
                if (vessel.orbitDriver.orbit.referenceBody == Body || (vessel.orbitDriver.orbit.referenceBody == Body && Body == Sun.Instance.sun)) // Work out sun position later...
                {
                    BodyPosition = new Vector3d(0, 0, 0);
                }
                else
                {
                    if (Body == Sun.Instance.sun)
                    {
                        BodyPosition = new Vector3d(0, 0, 0) ;
                    }
                    else
                    {
                        BodyPosition = Body.orbit.getRelativePositionAtUT(time);
                    }
                }
                double DistanceToVessel = Vector3d.Distance(Body.bodyTransform.position, vessel.vesselTransform.position); // Maybe but no time aspect
                    //Vector3d.Distance(BodyPosition, vessel.orbitDriver.orbit.getRelativePositionAtUT(time)); //

                print("Body " + Body.name + " distance to " + vessel.name + " : " + DistanceToVessel);

                double BodyMNA = 0;

                try
                {
                    BodyMNA = (Body.orbitDriver.orbit.GetMeanAnomaly(Body.orbitDriver.orbit.E, time));
                }
                catch (NullReferenceException)
                {
                     BodyMNA = 0;
                }

                double MNADifference = UtilMath.DegreesToRadians(PhaseAngle); // Fixed the phasing issues 1.6.0

                //print("MNA Difference = " + MNADifference);

                //print("Distance to Vessel: " + vessel.name + "   " + DistanceToVessel);

                InfluencingForce = (GravitationalConstant * BodyMass * VesselMass) / (DistanceToVessel * DistanceToVessel);
                InfluencingForce = InfluencingForce * Math.Cos(MNADifference); 

                Vector3d InfluencingAccelerationBodyDirectionVector = new Vector3d();


                if (vessel.orbitDriver.orbit.referenceBody == Body || (vessel.orbitDriver.orbit.referenceBody == Body && Body == Sun.Instance.sun))
                {
                    InfluencingAccelerationBodyDirectionVector = new Vector3d(0, 0, 0);
                }
                else
                {
                    if (Body == Sun.Instance.sun)
                    {
                        InfluencingAccelerationBodyDirectionVector = Body.position;
                    }
                    else
                    {
                        InfluencingAccelerationBodyDirectionVector = Body.orbit.getRelativePositionAtUT(time);
                    }
                }

                InfluencingAccelerationBodyDirectionVector = Body.transform.position; //


                Vector3d VesselRelativePositionVector = vessel.orbitDriver.orbit.getRelativePositionAtUT(time);

                VesselRelativePositionVector = vessel.transform.position; //

                Vector3d InfluencingAccelerationVector = (new Vector3d(-InfluencingAccelerationBodyDirectionVector.x + VesselRelativePositionVector.x, -InfluencingAccelerationBodyDirectionVector.y + VesselRelativePositionVector.y, -InfluencingAccelerationBodyDirectionVector.z + VesselRelativePositionVector.z)) * ((InfluencingForce / VesselMass)); // Always positive?

                InfluencingAccelerationVector = (new Vector3d(InfluencingAccelerationBodyDirectionVector.x - VesselRelativePositionVector.x, InfluencingAccelerationBodyDirectionVector.y - VesselRelativePositionVector.y, InfluencingAccelerationBodyDirectionVector.z - VesselRelativePositionVector.z)) * ((InfluencingForce / VesselMass)); // Better?

                //print("Accel Vector Magnitude: " + InfluencingAccelerationVector.magnitude);

                InfluencingAccelerations.Add(InfluencingAccelerationVector);

            }
            return InfluencingAccelerations;
        }

        public static List<Vector3d> InfluencingAccelerationsVOrbit(Orbit orbit, double time, double mass)
        {
            List<Vector3d> InfluencingAccelerations = new List<Vector3d>(); // Position at time

            foreach (CelestialBody Body in InfluencingBodiesV(orbit))
            {
                double VesselMass = mass;
                if (VesselMass == 0) // Incase vesselData hasnt caught up! 
                {
                    VesselMass = mass * 1000;
                }

                double PhaseAngle = FindPhaseAngleBetweenObjects(orbit, Body.orbit);

                double InfluencingForce = 0;
                double BodyMass = Body.Mass;
                Vector3d BodyPosition = new Vector3d();
                if (orbit.referenceBody == Body || (orbit.referenceBody == Body && Body == Sun.Instance.sun)) // Work out sun position later...
                {
                    BodyPosition = new Vector3d(0, 0, 0);
                }
                else
                {
                    if (Body == Sun.Instance.sun)
                    {
                        BodyPosition = new Vector3d(0, 0, 0);
                    }
                    else
                    {
                        BodyPosition = Body.orbit.getRelativePositionAtUT(time);
                    }
                }
                double DistanceToVessel = Vector3d.Distance(Body.bodyTransform.position, orbit.pos); // Maybe but no time aspect
                //Vector3d.Distance(BodyPosition, vessel.orbitDriver.orbit.getRelativePositionAtUT(time)); //

                double BodyMNA = 0;

                try
                {
                    BodyMNA = (Body.orbitDriver.orbit.GetMeanAnomaly(Body.orbitDriver.orbit.E, time));
                }
                catch (NullReferenceException)
                {
                    BodyMNA = 0;
                }

                double MNADifference = UtilMath.DegreesToRadians(PhaseAngle); // Fixed the phasing issues 1.6.0

                //print("MNA Difference = " + MNADifference);

                //print("Distance to Vessel: " + vessel.name + "   " + DistanceToVessel);

                InfluencingForce = (GravitationalConstant * BodyMass * VesselMass) / (DistanceToVessel * DistanceToVessel);
                InfluencingForce = InfluencingForce * Math.Cos(MNADifference);

                Vector3d InfluencingAccelerationBodyDirectionVector = new Vector3d();


                if (orbit.referenceBody == Body || (orbit.referenceBody == Body && Body == Sun.Instance.sun))
                {
                    InfluencingAccelerationBodyDirectionVector = new Vector3d(0, 0, 0);
                }
                else
                {
                    if (Body == Sun.Instance.sun)
                    {
                        InfluencingAccelerationBodyDirectionVector = Body.position;
                    }
                    else
                    {
                        InfluencingAccelerationBodyDirectionVector = Body.orbit.getRelativePositionAtUT(time);
                    }
                }

                InfluencingAccelerationBodyDirectionVector = Body.transform.position; //


                Vector3d VesselRelativePositionVector = orbit.getRelativePositionAtUT(time);

                VesselRelativePositionVector = orbit.pos; //

                Vector3d InfluencingAccelerationVector = (new Vector3d(-InfluencingAccelerationBodyDirectionVector.x + VesselRelativePositionVector.x, -InfluencingAccelerationBodyDirectionVector.y + VesselRelativePositionVector.y, -InfluencingAccelerationBodyDirectionVector.z + VesselRelativePositionVector.z)) * ((InfluencingForce / VesselMass)); // Always positive?

                InfluencingAccelerationVector = (new Vector3d(InfluencingAccelerationBodyDirectionVector.x - VesselRelativePositionVector.x, InfluencingAccelerationBodyDirectionVector.y - VesselRelativePositionVector.y, InfluencingAccelerationBodyDirectionVector.z - VesselRelativePositionVector.z)) * ((InfluencingForce / VesselMass)); // Better?

                //print("Accel Vector Magnitude: " + InfluencingAccelerationVector.magnitude);

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

        public static Vector3d GetMomentaryDeltaVOrbit(Orbit orbit, double time, double mass)
        {
            List<Vector3d> InfluencingAccelerationVectors = InfluencingAccelerationsVOrbit(orbit, time, mass);
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

            if (vessel.vesselType != VesselType.SpaceObject || vessel.vesselType != VesselType.Unknown) // For the moment
            {
                SetVesselDataOrbit(vessel, FinalVelocityVector);
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

            return orbit;
        }

        #region non state vector components
        public static double CalculateDeltaInclination(Vector3d position, Vector3d DeltaV, double time, double timeinterval, Vessel vessel, CelestialBody body)
        {
            double Inc = 0;
            double Altitude = 0;
            double Mass = VesselData.FetchMass(vessel);

            if (Mass == 0)
            {
                Mass = vessel.GetTotalMass() * 1000;
            }


            Vector3d BodyPosition = new Vector3d();

            if (vessel.orbitDriver.orbit.referenceBody == body || (vessel.orbit.referenceBody== body && body == Sun.Instance.sun))
            {
                BodyPosition = new Vector3d(0, 0, 0);
            }
            else
            {
                BodyPosition = body.orbit.getRelativePositionAtUT(time);
            }

            Altitude = Vector3d.Distance(position, body.transform.position);

            Vector3d AngularMomentum = Vector3d.Cross(position, DeltaV);
            Vector3d NodeVector = Vector3d.Cross(new Vector3d(0, 1, 0), AngularMomentum);
            double GravitationalParameter = body.gravParameter;
            Vector3d EccentricityVector = ((((Math.Pow(DeltaV.magnitude, 2.0) - GravitationalParameter) / position.magnitude) * position) - (Vector3d.Dot(position, DeltaV) * DeltaV)) /
                GravitationalParameter;

            Inc = Math.Acos(AngularMomentum.y / AngularMomentum.magnitude); // Remember y & z flipped Fine here!

            return Inc;
        }

        public static double CalculateDeltaSMA(Vector3d position, Vector3d DeltaV, double time, double timeinterval, CelestialBody body, Vessel vessel)
        {
            double SMA = 0;
            double Altitude = 0;
            Vector3d BodyPosition = new Vector3d();

            if (vessel.orbitDriver.orbit.referenceBody == body || body == Sun.Instance.sun)
            {
                BodyPosition = new Vector3d(0, 0, 0);
            }
            else
            {
                BodyPosition = body.orbit.getRelativePositionAtUT(time);
            }
            
            Altitude = Vector3d.Distance(BodyPosition, position);

            double ForwardVelocity = DeltaV.magnitude;
            double NewEnergy = (((Math.Pow(ForwardVelocity, 2.0)) / 2.0) - (body.gravParameter / Altitude));

            SMA = ((-(body.gravParameter) / NewEnergy) / 2.0);

            return Math.Abs(SMA);
        }

        public static double CalculateDeltaEccentricity(Vector3d position, Vector3d DeltaVelocity, double time, double timeinterval, CelestialBody body, Vessel vessel)
        {
            double Eccentricity = 0;

            double ForwardVelocity = DeltaVelocity.magnitude;
            double Altitude = 0;
            Vector3d BodyPosition = new Vector3d();

            if (vessel.orbitDriver.orbit.referenceBody == body && body == Sun.Instance.sun) // work out the sun effect 
            {
                BodyPosition = new Vector3d(0, 0, 0); // Fix this for eccentricity
            }
            else
            {
                BodyPosition = body.orbit.getRelativePositionAtUT(time);
            }

            Altitude = Vector3d.Distance(body.transform.position, position);

            Vector3d AngularMomentum = Vector3d.Cross(position, DeltaVelocity);
            Vector3d NodeVector = Vector3d.Cross(new Vector3d(0, 0, 1), AngularMomentum); // Y & Z Flipping issues here?
            double GravitationalParameter = body.gravParameter;
            //Vector3d EccentricityVector = ((((Math.Pow(FinalVelocity.magnitude, 2.0) - GravitationalParameter) / position.magnitude) * position) - (Vector3d.Dot(position, FinalVelocity) * FinalVelocity)) /
                //GravitationalParameter;

            Vector3d EccentricityVector = (Vector3d.Cross(DeltaVelocity, position) / GravitationalParameter) - (position / position.magnitude);

            Eccentricity = EccentricityVector.magnitude;

            if (Eccentricity == 1)
            {
                Eccentricity = vessel.orbitDriver.orbit.eccentricity;
            }

            return Eccentricity;
        }

        public static double CalculateDeltaLAN(Vector3d position, Vector3d DeltaV, double time, double timeinterval, Vessel vessel)
        {
            double LAN = 0;

            Vector3d AngularMomentum = Vector3d.Cross(position, DeltaV);
            Vector3d AscendingNodeVector = Vector3d.Cross(new Vector3d(0, 1, 0), AngularMomentum); // Y & Z Flipping issues here?
            double GravitationalParameter = vessel.orbitDriver.orbit.referenceBody.gravParameter;
            Vector3d EccentricityVector = ((((Math.Pow(DeltaV.magnitude, 2.0) - GravitationalParameter) / position.magnitude) * position) - (Vector3d.Dot(position, DeltaV) * DeltaV)) /
                GravitationalParameter;

            Vector3d NVector = new Vector3d(-AngularMomentum.y, AngularMomentum.x, 0);
            if (NVector.y >= 0)
            {
                LAN = Math.Acos(NVector.x / NVector.magnitude);
            }

            else
            {
                LAN = 2 * Math.PI - Math.Acos(NVector.x / NVector.magnitude);
            }

            if (vessel.orbitDriver.orbit.inclination < 0.01)
            {
                LAN = 0;
            }
            /*Vector3d AscendingNodeVectorNoHat = Vector3d.Cross(AscendingNodeVector, AscendingNodeVector.xzy);

            LAN = Math.Acos(AscendingNodeVectorNoHat.x / AscendingNodeVectorNoHat.magnitude);
            if (AscendingNodeVectorNoHat.y < 0)
            {
                LAN = 360 - LAN;
            }
             */

            return UtilMath.RadiansToDegrees(LAN);
        }

        public static double CalculateDeltaLPE(Vector3d position, Vector3d DeltaV, double time, double timeinterval, CelestialBody body, Vessel vessel)
        {
            double LPE = 0;

            Vector3d AngularMomentum = Vector3d.Cross(position, DeltaV);
            Vector3d AscendingNodeVector = Vector3d.Cross(new Vector3d(0, 1, 0), AngularMomentum); // Y & Z Flipping issues here?
            double GravitationalParameter = vessel.orbitDriver.orbit.referenceBody.gravParameter;
            Vector3d EccentricityVector = ((((Math.Pow(DeltaV.magnitude, 2.0) - GravitationalParameter) / position.magnitude) * position) - (Vector3d.Dot(position, DeltaV) * DeltaV)) /
                GravitationalParameter;

            Vector3d AscendingNodeVectorNoHat = Vector3d.Cross(AscendingNodeVector, AscendingNodeVector.xzy);

            LPE = Math.Acos( Vector3d.Dot(AscendingNodeVectorNoHat, EccentricityVector) / AscendingNodeVectorNoHat.magnitude * EccentricityVector.magnitude);

            if (EccentricityVector.y < 0)
            {
                LPE = 360 - LPE;
            }

            return UtilMath.RadiansToDegrees(LPE);
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






        } // Called per second of real (non UT) time

        public static void ManageOrbitPredictionsVessel(Vessel vessel) // Called per second
        {
            Orbit orbit = new Orbit();

            if (TimeWarp.CurrentRate < 2)
            {
                TimeAtTimewarpStart = HighLogic.CurrentGame.UniversalTime;
            }

            if (VesselOrbitalPredictions.ContainsKey(vessel))
            {
                VesselOrbitalPredictions.TryGetValue(vessel, out orbit);
            }

            else
            {
                VesselOrbitalPredictions.Add(vessel, vessel.orbitDriver.orbit);
                orbit = vessel.orbitDriver.orbit;
            }

            var NumberOfSteps = Settings.ReadNBCC();

            Vector3d InitialPosition = orbit.getRelativePositionAtUT(TimeAtTimewarpStart);
            Vector3d KeplerianFinalPosition = orbit.getRelativePositionAtUT(TimeAtTimewarpStart + 1.0 * TimeWarp.CurrentRate);


            Vector3d FinalVelVector = new Vector3d();
            Vector3d InitialVelVector = GetMomentaryDeltaV(vessel, TimeAtTimewarpStart); 
            Vector3d FinalVelInitialVector = new Vector3d();
            
                for (int i = 0; i < NumberOfSteps; i++)
                {
                    List<Vector3d> InfluencingAccelerationVectors = InfluencingAccelerationsV(vessel, TimeAtTimewarpStart + (TimeWarp.CurrentRate/NumberOfSteps) + i); // needs work here to adjust LAN 

                    Vector3d FinalVelocityVector = new Vector3d();

                    foreach (Vector3d Acceleration in InfluencingAccelerationVectors)
                    {
                        FinalVelocityVector = FinalVelocityVector + (Acceleration);
                    }

                    FinalVelVector = FinalVelVector + FinalVelocityVector;

                    if (i == 0)
                    {
                        FinalVelInitialVector = FinalVelVector;
                    }
                }

                #region Depreciated Timewarp Calculations
                /*
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
             */
                #endregion

                print("Change in delta V across timewarp duration in one second: " + FinalVelVector.magnitude);

                FinalVelVector = InitialVelVector + (FinalVelVector - FinalVelInitialVector) ; // Get change across 50 not sum of 50
                print("Final vel Vector Magnitude: " + (FinalVelVector + orbit.vel).magnitude);

            orbit = BuildFromStateVectors(orbit.getRelativePositionAtUT(HighLogic.CurrentGame.UniversalTime), FinalVelVector, orbit.referenceBody, HighLogic.CurrentGame.UniversalTime, vessel, InitialVelVector , orbit);

            // Issues with timewarp here

            VesselOrbitalPredictions.Remove(vessel);
            VesselOrbitalPredictions.Add(vessel, orbit);

            TimeAtTimewarpStart = HighLogic.CurrentGame.UniversalTime + 1.0;
        }

        public static void SetVesselDataOrbit(Vessel vessel, Vector3d FinalVelocity)
        {
            Orbit orbit = new Orbit();
            orbit = NewCalculatedOrbit(vessel, vessel.orbitDriver.orbit, FinalVelocity, HighLogic.CurrentGame.UniversalTime);
            //orbit.UpdateFromUT(HighLogic.CurrentGame.UniversalTime);

            VesselData.UpdateVesselSMA(vessel, orbit.semiMajorAxis);
            VesselData.UpdateVesselLPE(vessel, orbit.argumentOfPeriapsis);
            VesselData.UpdateVesselLAN(vessel, orbit.meanAnomaly);
            VesselData.UpdateVesselECC(vessel, orbit.eccentricity);
            VesselData.UpdateVesselINC(vessel, orbit.inclination);
        }
    
        public static Orbit BuildFromStateVectors(Vector3d position, Vector3d FinalVelocity, CelestialBody body, double UniversalTime, Vessel vessel, Vector3d InitialVelocity, Orbit initialOrbit)
        {
            Orbit StateVectorBuiltOrbit = vessel.orbitDriver.orbit;
            Vector3d deltaV = FinalVelocity; //-InitialVelocity + FinalVelocity; // This Maybe?

            double NewSemiMajorAxis = NBodyManager.CalculateDeltaSMA(position, deltaV, UniversalTime, 1.0, body, vessel);
            double NewInclination = NBodyManager.CalculateDeltaInclination(position, deltaV, UniversalTime, 1.0, vessel, body); // 0
            double NewEccentricity = NBodyManager.CalculateDeltaEccentricity(position, deltaV, UniversalTime, 1.0, body, vessel); // NaN
            double NewLAN = NBodyManager.CalculateDeltaLAN(position, deltaV, UniversalTime, 1.0, vessel); // 200 out?
            //double NewLPE = NBodyManager.CalculateDeltaLPE(position, deltaV, UniversalTime, 1.0, body, vessel); // 230 out?
            double NewEPH = 0;
            double NewMNA = 0;
            bool NaNFound = false;

            if (Math.Abs(NewLAN - StateVectorBuiltOrbit.LAN) > 5)
            {
                NewLAN = StateVectorBuiltOrbit.LAN; // Incase cycled past 360 Needs more work here!
            }

            if (NewSemiMajorAxis < body.Radius || double.IsNaN(NewSemiMajorAxis))
            {
                NewSemiMajorAxis = vessel.orbitDriver.orbit.semiMajorAxis;
                NaNFound = true;
            }

            if (NewEccentricity > 0.99 || double.IsNaN(NewEccentricity))
            {
                NewEccentricity = vessel.orbitDriver.orbit.eccentricity;
                NaNFound = true;
            }

            if (double.IsNaN(NewInclination))
            {
                NewInclination = vessel.orbitDriver.orbit.inclination; 
                NaNFound = true;
            }

            StateVectorBuiltOrbit.semiMajorAxis =  NewSemiMajorAxis; 
            StateVectorBuiltOrbit.inclination =  NewInclination;
            StateVectorBuiltOrbit.eccentricity =  NewEccentricity;
            StateVectorBuiltOrbit.LAN = NewLAN;
            StateVectorBuiltOrbit.epoch = vessel.orbitDriver.orbit.epoch;
           // StateVectorBuiltOrbit.argumentOfPeriapsis = NewLPE;
            StateVectorBuiltOrbit.meanAnomaly = vessel.orbitDriver.orbit.GetMeanAnomaly(vessel.orbitDriver.orbit.E, HighLogic.CurrentGame.UniversalTime);
            StateVectorBuiltOrbit.referenceBody = body;
            StateVectorBuiltOrbit.meanAnomalyAtEpoch = vessel.orbitDriver.orbit.meanAnomalyAtEpoch;

            if (double.IsNaN(deltaV.magnitude) || deltaV.magnitude == 0 || NaNFound == true) // Catches vessel data lag
            {
                StateVectorBuiltOrbit = vessel.orbitDriver.orbit;
            }

            else
            {
                print("NewSMA: " + NewSemiMajorAxis);
                print("NewINC: " + NewInclination);
                print("NewEcc: " + NewEccentricity);
                print("NewLAN: " + NewLAN);
                //print("NewLpe: " + NewLPE);
            }

            return StateVectorBuiltOrbit;
        }

        public static void ManageOrbitalPredictons()
        {

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


            for (int i = 0; i < FlightGlobals.Bodies.Count; i++)
            {
                if (Settings.ReadNBB()) // If body mangement is enabled
                {
                    if (FlightGlobals.Bodies.ElementAt(i) != Sun.Instance.sun)
                    {
                        ManageOrbitalPredictionsBody(FlightGlobals.Bodies.ElementAt(i));
                        ManageBody(FlightGlobals.Bodies.ElementAt(i));
                    }
                }
            }

            for (int j = 0; j < FlightGlobals.Vessels.Count; j++)
            {
                if (!VesselData.FetchStationKeeping(FlightGlobals.Vessels.ElementAt(j)) && FlightGlobals.Vessels.ElementAt(j).vesselType == VesselType.Station) // debugging
                {
                   // ManageOrbitPredictionsVessel(FlightGlobals.Vessels.ElementAt(j)); For the moment
                }
            }
        }

        public static double FindPhaseAngleBetweenObjects( Orbit orbit, Orbit target)
        {
            var angle = Vector3d.Angle(Vector3d.Exclude(orbit.GetOrbitNormal(), target.pos), orbit.pos);
            return angle;//(orbit.semiMajorAxis < target.semiMajorAxis) ? angle : angle - 360.0;
        }

    }
}