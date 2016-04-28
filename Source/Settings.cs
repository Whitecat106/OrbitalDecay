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
    [KSPAddon(KSPAddon.Startup.EveryScene,false)]
    public class Settings : MonoBehaviour
    {
        public static string FilePath = KSPUtil.ApplicationRootPath + "GameData/WhitecatIndustries/Orbital Decay/PluginData/Settings.cfg";
        public static ConfigNode SettingData = new ConfigNode();
        public static ConfigNode settings = ConfigNode.Load(FilePath);

        public void Start()
        {
            SettingData.ClearData();
            settings = ConfigNode.Load(FilePath);
            foreach (ConfigNode item in settings.nodes)
            {
                SettingData.AddNode(item);
            }
        }

        public void OnDestroy()
        {
            settings.ClearData();
            SettingData.Save(FilePath);
        }

        public static void WriteRD(bool RD)
        {
            ConfigNode Data = SettingData;
            ConfigNode SimSet = Data.GetNode("SIMULATION");
            SimSet.SetValue("RealisticDecay", RD.ToString());   
        }
        public static void Write24H(bool H24)
        {
            ConfigNode Data = SettingData;
            ConfigNode SimSet = Data.GetNode("SIMULATION");
            SimSet.SetValue("24HourClock", H24.ToString());  
        }
        public static void WritePlanetariumTracking(bool PT)
        {
            ConfigNode Data = SettingData;
            ConfigNode SimSet = Data.GetNode("SIMULATION");
            SimSet.SetValue("PlanetariumTracking", PT.ToString()); 
        }

        public static void WritePDebrisTracking(bool DT)
        {
            ConfigNode Data = SettingData;
            ConfigNode SimSet = Data.GetNode("SIMULATION");
            SimSet.SetValue("PlanetariumDebrisTracking", DT.ToString());
        }

        public static void WriteDifficulty(double Difficulty)
        {
            ConfigNode Data = SettingData;
            ConfigNode SimSet = Data.GetNode("SIMULATION");
            SimSet.SetValue("DecayDifficulty", Difficulty.ToString());
        }
        public static void WriteResourceRateDifficulty(double Difficulty)
        {
            ConfigNode Data = SettingData;
            ConfigNode Resources = Data.GetNode("RESOURCES");
            Resources.SetValue("ResourceRateDifficulty", Difficulty.ToString());
        }
        public static void WriteStatKeepResource(string Resource)
        {
            ConfigNode Data = SettingData;
            ConfigNode Resources = Data.GetNode("RESOURCES");
            Resources.SetValue("StatKeepResource", Resource);
        }
        public static bool ReadRD()
        {
            ConfigNode Data = SettingData;
            ConfigNode SimSet = Data.GetNode("SIMULATION");
            bool RD = bool.Parse(SimSet.GetValue("RealisticDecay"));
            return RD;
        }

        public static bool Read24Hr()
        {
            ConfigNode Data = SettingData;
            ConfigNode SimSet = Data.GetNode("SIMULATION");
            bool R24H = bool.Parse(SimSet.GetValue("24HourClock"));
            return R24H;
        }

        public static bool ReadPT()
        {
            ConfigNode Data = SettingData;
            ConfigNode SimSet = Data.GetNode("SIMULATION");
            bool PT = bool.Parse(SimSet.GetValue("PlanetariumTracking"));
            return PT;
        }

        public static bool ReadDT()
        {
            ConfigNode Data = SettingData;
            ConfigNode SimSet = Data.GetNode("SIMULATION");
            bool DT = bool.Parse(SimSet.GetValue("PlanetariumDebrisTracking"));
            return DT;
        }

        public static double ReadDecayDifficulty()
        {
            ConfigNode Data = SettingData;
            ConfigNode SimSet = Data.GetNode("SIMULATION");
            double Difficulty = double.Parse(SimSet.GetValue("DecayDifficulty"));
            return Difficulty;

        }

        public static string ReadStationKeepingResource()
        {
            ConfigNode Data = SettingData;
            ConfigNode Resources = Data.GetNode("RESOURCES");
            string FavouredResource = Resources.GetValue("StatKeepResource");
            return FavouredResource;
        }

        public static double ReadResourceRateDifficulty()
        {
            ConfigNode Data = SettingData;
            ConfigNode Resources = Data.GetNode("RESOURCES");
            double FavouredResource = double.Parse(Resources.GetValue("ResourceRateDifficulty"));
            return FavouredResource;
        }

    }
}
