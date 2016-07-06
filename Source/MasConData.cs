/*
 * Whitecat Industries Orbital Decay for Kerbal Space Program. for Kerbal Space Program. 
 * 
 * Written by Whitecat106 (Marcus Hehir).
 * 
 * Kerbal Space Program is Copyright (C) 2016 Squad. See http://kerbalspaceprogram.com/. This
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
    public class MasConData : MonoBehaviour
    {
        public static string FilePath = KSPUtil.ApplicationRootPath + "GameData/WhitecatIndustries/Orbital Decay/Plugins/PluginData/MasConData.cfg";
        public static ConfigNode GMSData;

        public static void LoadData()
        {
            GMSData = ConfigNode.Load(FilePath);
        }

        private static ConfigNode ThisGravityMap(string Body)
        {
            ConfigNode returnNode = new ConfigNode("GRAVITYMAP");

            foreach (ConfigNode GravityMap in GMSData.GetNodes("GRAVITYMAP"))
            {
                if (GravityMap.GetValue("body") == Body)
                {
                    returnNode = GravityMap;
                    break;
                }
            }
            return returnNode;
        }

        
        public static bool IsBetween(double item, double min, double max) 
        {
            return (Enumerable.Range(Math.Abs((int)min), Math.Abs((int)max)).Contains(Math.Abs((int)item)));
        }
        

        public static ConfigNode LocalMasCon(Vessel vessel)
        {
            ConfigNode LocalGravityMap = ThisGravityMap(vessel.orbitDriver.orbit.referenceBody.GetName());
            ConfigNode Local = new ConfigNode();

            if (LocalGravityMap.nodes.Count > 0)
            {
                double VesselLat = vessel.latitude;
                double VesselLong = vessel.longitude;

                bool LatitudeWithin = false;
                bool LongitudeWithin = false;

                foreach (ConfigNode MasCon in LocalGravityMap.GetNodes("MASCON"))
                {
                    double CentreGal = double.Parse(MasCon.GetValue("centreGal"));
                    double CentreLat = double.Parse(MasCon.GetValue("centreLat"));
                    double CentreLong = double.Parse(MasCon.GetValue("centreLong"));
                    double DegDiam = double.Parse(MasCon.GetValue("diamdeg"));
                    double DegRad = DegDiam / 2.0;

                    double UpperBoundLat = CentreLat + DegRad;
                    double LowerBoundLat = CentreLat - DegRad;
                    double UpperBoundLong = CentreLong + DegRad;
                    double LowerBoundLong = CentreLong - DegRad;

                    if (UpperBoundLat > 90)
                    {
                        UpperBoundLat = Math.Abs(UpperBoundLat - 180);
                    }

                    if (LowerBoundLat < 90)
                    {
                        LowerBoundLat = -1 * (UpperBoundLat + 180);
                    }

                    if (UpperBoundLong > 180)
                    {
                        UpperBoundLong = UpperBoundLong - 360;
                    }

                    if (UpperBoundLong < -180)
                    {
                        UpperBoundLong = UpperBoundLong + 360;
                    }

                    if (IsBetween(VesselLat, UpperBoundLat, LowerBoundLat))
                    {
                        LatitudeWithin = true;
                    }

                    if (IsBetween(VesselLong, UpperBoundLong, LowerBoundLong))
                    {
                        LongitudeWithin = true;
                    }

                    if (LatitudeWithin == true && LongitudeWithin == true)
                    {
                        Local = MasCon;
                        break;
                    }
                }
            }

            return Local;
        }

        public static bool CheckMasConProximity(Vessel vessel)
        {
            bool WithinEffectRange = false;

            if (vessel.orbitDriver.orbit.referenceBody.GetName() == "Earth" || vessel.orbitDriver.orbit.referenceBody.GetName() == "Kerbin" || vessel.orbitDriver.orbit.referenceBody.GetName() == "Moon") // 
            {
                ConfigNode LocalGravityMap = ThisGravityMap(vessel.orbitDriver.orbit.referenceBody.GetName());

                if (LocalGravityMap.nodes.Count > 0)
                {
                    double VesselLat = vessel.latitude;
                    double VesselLong = vessel.longitude;

                    bool LatitudeWithin = false;
                    bool LongitudeWithin = false;

                    foreach (ConfigNode MasCon in LocalGravityMap.GetNodes("MASCON"))
                    {
                        double CentreGal = double.Parse(MasCon.GetValue("centreGal"));
                        double CentreLat = double.Parse(MasCon.GetValue("centreLat"));
                        double CentreLong = double.Parse(MasCon.GetValue("centreLong"));
                        double DegDiam = double.Parse(MasCon.GetValue("diamdeg"));
                        double DegRad = DegDiam / 2.0;

                        double UpperBoundLat = CentreLat + DegRad;
                        double LowerBoundLat = CentreLat - DegRad;
                        double UpperBoundLong = CentreLong + DegRad;
                        double LowerBoundLong = CentreLong - DegRad;

                        if (UpperBoundLat > 90)
                        {
                            UpperBoundLat = Math.Abs(UpperBoundLat - 180);
                        }

                        if (LowerBoundLat < 90)
                        {
                            LowerBoundLat = -1 * (UpperBoundLat + 180);
                        }

                        if (UpperBoundLong > 180)
                        {
                            UpperBoundLong = UpperBoundLong - 360;
                        }

                        if (UpperBoundLong < -180)
                        {
                            UpperBoundLong = UpperBoundLong + 360;
                        }

                        if (IsBetween(VesselLat, UpperBoundLat, LowerBoundLat))
                        {
                            LatitudeWithin = true;
                        }

                        if (IsBetween(VesselLong, UpperBoundLong, LowerBoundLong))
                        {
                            LongitudeWithin = true;
                        }
                        

                        if (LatitudeWithin == true && LongitudeWithin == true)
                        {
                            break;
                        }
                    }

                    if (LatitudeWithin == true && LongitudeWithin == true)
                    {
                        WithinEffectRange = true;
                    }
                }
            }
            return WithinEffectRange;
        }

        public static double LocalGal(Vessel vessel)
        {
            return double.Parse(LocalMasCon(vessel).GetValue("centreGal"));
        }

        public static double GalAtPosition(Vessel vessel)
        {
            ConfigNode LocalMap = ThisGravityMap(vessel.orbitDriver.orbit.referenceBody.GetName());
            double meanGal = double.Parse(LocalMap.GetValue("meanGal"));

            ConfigNode MasCon = LocalMasCon(vessel);
            double CentreGal = double.Parse(MasCon.GetValue("centreGal"));
            double CentreLat = double.Parse(MasCon.GetValue("centreLat"));
            double CentreLong = double.Parse(MasCon.GetValue("centreLong"));
            double radiusdeg = double.Parse(MasCon.GetValue("diamdeg")) / 2;
            double EdgeLat = CentreLat + radiusdeg;
            double EdgeLong = CentreLong + radiusdeg;

            double GalAtDistance = 0.0;

            var R = vessel.orbitDriver.orbit.referenceBody.Radius;
            var A = ToRadians(CentreLat);
            var B = ToRadians(EdgeLat);
            var C = (ToRadians(EdgeLat) - ToRadians(CentreLat));
            var D = (ToRadians(EdgeLong) - ToRadians(CentreLong));
            var E = Math.Sin(C / 2) * Math.Sin(C / 2) +
                    Math.Cos(A) * Math.Cos(B) *
                    Math.Sin(D / 2) * Math.Sin(D / 2);
            var F = 2 * Math.Atan2(Math.Sqrt(E), Math.Sqrt(1 - E));
            var Edgedistance = R * F;

            var φ1 = ToRadians(CentreLat);
            var φ2 = ToRadians(vessel.latitude);
            var Δφ = (ToRadians(vessel.latitude) - ToRadians(CentreLat));
            var Δλ = (ToRadians(vessel.longitude) - ToRadians(CentreLong));
            var a = Math.Sin(Δφ / 2) * Math.Sin(Δφ / 2) +
                    Math.Cos(φ1) * Math.Cos(φ2) *
                    Math.Sin(Δλ / 2) * Math.Sin(Δλ / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            var Vesseldistance = R * c;

            GalAtDistance = (Math.Abs(CentreGal) / Edgedistance) * Vesseldistance; // Work out negative push mascons for 1.6.0 removed absolute CentreGal

            /*
            double GravitationalConstant = 6.67408 * Math.Pow(10.0, -11.0); // G [Newton Meter Squared per Square Kilograms] 
            double AccelerationToGal = 100.0;
            double GalAtSurface = ((GravitationalConstant * vessel.orbitDriver.orbit.referenceBody.Mass * vessel.GetTotalMass()));
            double GalAtVerticalDistance = ((GravitationalConstant * vessel.orbitDriver.orbit.referenceBody.Mass * vessel.GetTotalMass()) / (Math.Pow(vessel.orbitDriver.orbit.altitude,2.0))) * AccelerationToGal;
            GalAtDistance = GalAtDistance + GalAtVerticalDistance;
            */

            return GalAtDistance;
        }

        public static double ToRadians(double val)
        {
            return (Math.PI / 180.0) * val;
        }

        public static double ToDegrees(double val)
        {
            return 57.3 * val;
        }

        #region Old Subs

        public static double CalculateRightAscension(Vector3d direction)
        {
            double RAAN = 0.0;
            var NormalisedVector = Vector3d.Normalize(direction);
            var l = direction.x / NormalisedVector.magnitude;
            var m = direction.y / NormalisedVector.magnitude;
            var n = direction.z / NormalisedVector.magnitude;
            var alpha = 0.0;
            var delta = 0.0;
            delta = Math.Asin(n) * 180.0 / Math.PI;

            if (m > 0)
            {
                alpha = Math.Acos(l / Math.Cos(delta)) * 180.0 / Math.PI;
            }
            else
            {
                alpha = 360 - Math.Acos(l / Math.Cos(delta)) * 180.0 / Math.PI;
            }

            RAAN = alpha;

            return RAAN;
        }

        public static double CalculateDeclination(Vector3d direction)
        {
            double DEC = 0.0;

            var NormalisedVector = Vector3d.Normalize(direction);
            var l = direction.x / NormalisedVector.magnitude;
            var m = direction.y / NormalisedVector.magnitude;
            var n = direction.z / NormalisedVector.magnitude;

            var delta = 0.0;
            delta = Math.Asin(n) * 180.0 / Math.PI;

            DEC = delta;

            return DEC;
        }

        #endregion
    }
}