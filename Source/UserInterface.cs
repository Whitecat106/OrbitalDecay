// 1.2.0 Issues:

// Stock decay - Eccentricity problem -- Remove for now? -- No lets work this one out! -- Or maybe not... 1.3.0
// add RSS realistic active decay -- 1.3.0
// Multiple resources active at the same time? -- 1.3.0

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
using KSP.UI.Screens;

namespace WhitecatIndustries
{
    [KSPAddon(KSPAddon.Startup.EveryScene, false)]
    class UserInterface : MonoBehaviour
    {
        private static int currentTab = 0;
        private static string[] tabs = { "Vessels", "Settings" }; 
        private static Rect windowPosition = new Rect(0, 0, 300, 500);
        private static GUIStyle windowStyle = new GUIStyle(HighLogic.Skin.window);
        private static Color tabUnselectedColor = new Color(0.0f, 0.0f, 0.0f);
        private static Color tabSelectedColor = new Color(0.0f, 0.0f, 0.0f);
        private static Color tabUnselectedTextColor = new Color(0.0f, 0.0f, 0.0f);
        private static Color tabSelectedTextColor = new Color(0.0f, 0.0f, 0.0f);
        private GUISkin skins = HighLogic.Skin;
        private int id = Guid.NewGuid().GetHashCode();
        public static ApplicationLauncherButton ToolbarButton = null;

        public static bool Visible = false;
        public static Texture launcher_icon = null;

        Vector2 scrollPosition1 = Vector2.zero;
        Vector2 scrollPosition2 = Vector2.zero;
        Vector2 scrollPosition3 = Vector2.zero;
        float MultiplierValue = 5.0f;
        float MultiplierValue2 = 5.0f;

        void Awake()
        {
            GameEvents.onGUIApplicationLauncherReady.Remove(ReadyEvent);
            GameEvents.onGUIApplicationLauncherReady.Add(ReadyEvent);
            GameEvents.onGUIApplicationLauncherDestroyed.Remove(DestroyEvent);
            GameEvents.onGUIApplicationLauncherDestroyed.Add(DestroyEvent);
            GameEvents.onHideUI.Remove(GuiOff);
            GameEvents.onHideUI.Add(GuiOff);
        }

        public void ReadyEvent()
        {
            if (ApplicationLauncher.Ready && ToolbarButton == null)
            {
                var Scenes = ApplicationLauncher.AppScenes.TRACKSTATION | ApplicationLauncher.AppScenes.SPACECENTER | ApplicationLauncher.AppScenes.FLIGHT | ApplicationLauncher.AppScenes.MAPVIEW;
                launcher_icon = GameDatabase.Instance.GetTexture("WhitecatIndustries/Orbital Decay/Icon/IconToolbar", false);
                ToolbarButton = ApplicationLauncher.Instance.AddModApplication(GuiOn, GuiOff, null, null, null, null, Scenes, launcher_icon);
            }
        }

        public void OnDestroy()
        {
            DestroyEvent();
        }

        public void DestroyEvent()
        {
            if (ToolbarButton == null) return;
            ApplicationLauncher.Instance.RemoveModApplication(ToolbarButton);
            ToolbarButton = null;
            Visible = false;
        }

        private void GuiOn()
        {
            Visible = true;
        }

        private void GuiOff()
        {
            Visible = false;
        }

        public void OnGUI()
        {
            if (Visible)
            {
                windowPosition = GUILayout.Window(id, windowPosition, MainWindow, "Orbital Decay Manager", windowStyle);
            }
        }

        public void MainWindow(int windowID)
        {      
                if (GUI.Button(new Rect(windowPosition.width - 22, 3, 19, 19), "x"))
                {
                    if (ToolbarButton != null)
                        ToolbarButton.toggleButton.Value = false;
                }
                GUILayout.BeginVertical();
                GUILayout.Space(10);
                GUILayout.BeginHorizontal();

                for (int i = 0; i < tabs.Length; ++i)
                {
                    if (GUILayout.Button(tabs[i]))
                    {
                        currentTab = i;
                    }
                }

                GUILayout.EndHorizontal();
                GUILayout.Space(10);
                switch (currentTab)
                {
                    case 0:
                        InformationTab();
                        break;
                    case 1:
                        SettingsTab();
                        break;

                    default:
                        break;
                }
                GUILayout.EndVertical();
                GUI.DragWindow();
                windowPosition.x = Mathf.Clamp(windowPosition.x, 0f, Screen.width - windowPosition.width);
                windowPosition.y = Mathf.Clamp(windowPosition.y, 0f, Screen.height - windowPosition.height);
            }

        public void InformationTab()
        {
            GUILayout.BeginHorizontal();
            GUI.skin.label.fontSize = 16;
            GUI.skin.label.alignment = TextAnchor.MiddleCenter;
            GUILayout.Label("Vessel Information", GUILayout.Width(290));
            GUI.skin.label.alignment = TextAnchor.MiddleLeft;
            GUI.skin.label.fontSize = 12;
            GUILayout.EndHorizontal();

            GUILayout.BeginVertical();
            GUILayout.Space(3);
            GUILayout.Label("____________________________________");
            GUILayout.EndVertical();

            scrollPosition1 = GUILayout.BeginScrollView(scrollPosition1, GUILayout.Width(290), GUILayout.Height(480));
            bool Realistic = Settings.ReadRD();
            var ClockType = Settings.Read24Hr();
            //151var Resource = Settings.ReadStationKeepingResource();
            

            foreach (Vessel vessel in FlightGlobals.Vessels)
            {
                if (vessel.situation == Vessel.Situations.ORBITING && vessel.vesselType != VesselType.SpaceObject && vessel.vesselType != VesselType.Unknown && vessel.vesselType != VesselType.Debris)
                {
                    var StationKeeping = VesselData.FetchStationKeeping(vessel).ToString();
                 //   var StationKeepingFuelRemaining = VesselData.FetchFuel(vessel).ToString("F3");
                    var StationKeepingFuelRemaining = ResourceManager.GetResources(vessel).ToString("F3");
                //    var Resource = VesselData.FetchResource(vessel);//151
                    var Resource = ResourceManager.GetResourceNames(vessel);
                    var ButtonText = "";
                    var HoursInDay = 6.0;

                    double DaysInYear = 0;
                    bool KerbinTime = GameSettings.KERBIN_TIME;

                    if (KerbinTime == true)
                    {
                        DaysInYear = 9203545 / (60 * 60 * HoursInDay);
                    }
                    else
                    {
                        DaysInYear = 31557600 / (60 * 60 * HoursInDay);
                    }


                    if (StationKeeping == "True")
                    {
                        ButtonText = "Disable Station Keeping";
                    }
                    else
                    {
                        ButtonText = "Enable Station Keeping";
                    }

                    if (ClockType == true)
                    {
                        HoursInDay = 24.0;
                    }
                    else
                    {
                        HoursInDay = 6.0;
                    }

                    GUILayout.BeginVertical();
                    GUILayout.Label("Vessel Name: " + vessel.vesselName);
                    GUILayout.Space(2);
                    GUILayout.Label("Orbiting Body: " + vessel.orbitDriver.orbit.referenceBody.GetName());
                    GUILayout.Space(2);

                    if (StationKeeping == "True")
                    {
                        GUILayout.Label("Current Total Decay Rate: Vessel is Station Keeping");
                        GUILayout.Space(2);
                    }
                    else
                    {
                        double TotalDecayRatePerSecond = Math.Abs(DecayManager.DecayRateAtmosphericDrag(vessel)) + Math.Abs(DecayManager.DecayRateRadiationPressure(vessel)) + Math.Abs(DecayManager.DecayRateYarkovskyEffect(vessel)); //+ Math.Abs(DecayManager.DecayRateGravitationalPertubation(vessel));
                        double ADDR = DecayManager.DecayRateAtmosphericDrag(vessel);
                        double GPDR = DecayManager.DecayRateGravitationalPertubation(vessel);
                        double PRDR = DecayManager.DecayRateRadiationPressure(vessel);
                        double YEDR = DecayManager.DecayRateYarkovskyEffect(vessel);

                        GUILayout.Label("Current Total Decay Rate: " + FormatDecayRateToString(TotalDecayRatePerSecond));
                        //GUILayout.Space(2);

                        //if (GUILayout.Button(ButtonText2)) // Display a new window here?
                        //{
                            //GUILayout.Space(2);
                            //GUILayout.Label("Current Atmospheric Drag Decay Rate: " + FormatDecayRateToString(ADDR));
                            //GUILayout.Space(2);
                            //GUILayout.Label("Current Radiation Pressure Decay Rate: " + FormatDecayRateToString(PRDR));
                            //GUILayout.Space(2);
                            //GUILayout.Label("Current Gravitational Effect Decay Rate: " + FormatDecayRateToString(GPDR));
                            //GUILayout.Space(2);
                            //GUILayout.Label("Current Yarkovsky Effect Decay Rate: " + FormatDecayRateToString(YEDR)); // 1.6.0
                        //}

                        GUILayout.Space(2);

                        double TimeUntilDecayInUnits = 0.0;
                        string TimeUntilDecayInDays = "";

                        if (ADDR != 0)
                        {
                            TimeUntilDecayInUnits = DecayManager.DecayTimePredictionExponentialsVariables(vessel);
                            TimeUntilDecayInDays = FormatTimeUntilDecayInDaysToString(TimeUntilDecayInUnits);
                        }
                        else
                        {
                            TimeUntilDecayInUnits = DecayManager.DecayTimePredictionLinearVariables(vessel);
                            TimeUntilDecayInDays = FormatTimeUntilDecayInSecondsToString(TimeUntilDecayInUnits);
                        }

                        GUILayout.Label("Approximate Time Until Decay: " + TimeUntilDecayInDays);
                        GUILayout.Space(2);
                    }

                    GUILayout.Label("Station Keeping: " + StationKeeping);
                    GUILayout.Space(2);
                    GUILayout.Label("Station Keeping Fuel Remaining: " + StationKeepingFuelRemaining);
                    GUILayout.Space(2);
                    GUILayout.Label("Using Fuel Type: " + Resource);//151
                    GUILayout.Space(2); //151

                    if (StationKeeping == "True")
                    {
                        double DecayRateSKL = 0;

                        DecayRateSKL = DecayManager.DecayRateAtmosphericDrag(vessel) + DecayManager.DecayRateRadiationPressure(vessel) + DecayManager.DecayRateYarkovskyEffect(vessel);


                        double StationKeepingLifetime = (double.Parse(StationKeepingFuelRemaining) / ((DecayRateSKL / TimeWarp.CurrentRate) * VesselData.FetchEfficiency(vessel) /*ResourceManager.GetEfficiency(Resource)*/ * Settings.ReadResourceRateDifficulty())) / (60 * 60 * HoursInDay);

                        if (StationKeepingLifetime < -5) // SRP Fixes
                        {
                            GUILayout.Label("Station Keeping Fuel Lifetime: > 1000 years.");
                        }

                        else
                        {
                            if (StationKeepingLifetime > 365000 && HoursInDay == 24)
                            {
                                GUILayout.Label("Station Keeping Fuel Lifetime: > 1000 years.");
                            }

                            else if (StationKeepingLifetime > 425000 && HoursInDay == 6)
                            {
                                GUILayout.Label("Station Keeping Fuel Lifetime: > 1000 years.");
                            }

                            else
                            {
                                if (StationKeepingLifetime > 425 && HoursInDay == 6)
                                {
                                    GUILayout.Label("Station Keeping Fuel Lifetime: " + (StationKeepingLifetime / 425).ToString("F1") + " years.");
                                }

                                else if (StationKeepingLifetime > 365 && HoursInDay == 24)
                                {
                                    GUILayout.Label("Station Keeping Fuel Lifetime: " + (StationKeepingLifetime / 365).ToString("F1") + " years.");
                                }

                                else
                                {
                                    GUILayout.Label("Station Keeping Fuel Lifetime: " + StationKeepingLifetime.ToString("F1") + " days.");
                                }
                            }
                        }
                        GUILayout.Space(3);
                    }

                    if (GUILayout.Button(ButtonText))
                    {
                        if (StationKeeping == "True")
                        {
                            VesselData.UpdateStationKeeping(vessel, false);
                            ScreenMessages.PostScreenMessage("Vessel: " + vessel.vesselName + (": Station Keeping Disabled"));

                        }

                        if (StationKeeping == "False")
                        {
                            if (StationKeepingManager.EngineCheck(vessel) == true)
                            {
                                if ((double.Parse(StationKeepingFuelRemaining) > 0.01)) // Good enough...
                                {
                                    
                                    VesselData.UpdateStationKeeping(vessel, true);
                                    ScreenMessages.PostScreenMessage("Vessel: " + vessel.vesselName + (": Station Keeping Enabled"));
                                }
                                else
                                {
                                    ScreenMessages.PostScreenMessage("Vessel: " + vessel.vesselName + (" has no fuel to Station Keep!"));
                                }
                            }
                            else
                            {
                                ScreenMessages.PostScreenMessage("Vessel: " + vessel.vesselName + (" has no Engines or RCS modules on board!"));
                            }
                        }
                    }
                    GUILayout.Space(2);
                    GUILayout.Label("____________________________________");
                    GUILayout.Space(3);
                    GUILayout.EndVertical();
                }

            }
            GUILayout.EndScrollView();

        }

        public void SettingsTab()
        {
            
            GUILayout.BeginHorizontal();
            GUI.skin.label.fontSize = 16;
            GUI.skin.label.alignment = TextAnchor.MiddleCenter;
            GUILayout.Label("Control Panel", GUILayout.Width(290));
            GUI.skin.label.alignment = TextAnchor.MiddleLeft;
            GUI.skin.label.fontSize = 12;
            GUILayout.EndHorizontal();

            GUILayout.BeginVertical();
            GUILayout.Label("____________________________________");
            GUILayout.Space(3);

            var DecayDifficulty = Settings.ReadDecayDifficulty();
            var ResourceDifficulty = Settings.ReadResourceRateDifficulty();

            GUILayout.Space(2);
            if (GUILayout.Button("Toggle Kerbin Day (6 hour) / Earth Day (24 hour)"))
            {
                Settings.Write24H(!Settings.Read24Hr());
                if (Settings.Read24Hr() == true)
                {
                    ScreenMessages.PostScreenMessage("Earth Day (24 hour) set.");
                }
                else
                {
                    ScreenMessages.PostScreenMessage("Kerbin Day (6 hour) set.");
                }
                
            }
            GUILayout.Space(2);
            if (GUILayout.Button("Toggle Planetarium Updating"))
            {
                Settings.WritePlanetariumTracking(!Settings.ReadPT());
                if (Settings.ReadPT() == true)
                {
                    ScreenMessages.PostScreenMessage("Planetarium Updating Active - Warning this may cause tracking station lag with many vessels.");
                }
                else
                {
                    ScreenMessages.PostScreenMessage("Planetarium Updating Disabled.");
                }
            }

            if (Settings.ReadPT() == true)
            {
                GUILayout.Space(2);
                if (GUILayout.Button("Toggle Debris Updating"))
                {
                    Settings.WritePDebrisTracking(!Settings.ReadDT());
                    if (Settings.ReadDT() == true)
                    {
                        ScreenMessages.PostScreenMessage("Debris Tracking Active - Warning Planetarium Lag Increased.");
                    }
                    else
                    {
                        ScreenMessages.PostScreenMessage("Debris Tracking Disabled - Planetarium Lag Decreased.");
                    }
                }

            }

            GUILayout.Space(3);
            GUI.skin.label.alignment = TextAnchor.MiddleCenter;
            MultiplierValue = GUILayout.HorizontalSlider(MultiplierValue, 0.5f, 50.0f);
            GUILayout.Space(2);
            GUILayout.Label("Current Decay multiplier: " + (DecayDifficulty).ToString("F1"));
            GUILayout.Space(2); 
            GUILayout.Label("New Decay multiplier: " + (MultiplierValue/5).ToString("F1"));
            GUILayout.Space(2); 

            if (GUILayout.Button("Set Multiplier"))
            {
                Settings.WriteDifficulty(MultiplierValue/5);
                ScreenMessages.PostScreenMessage("Decay Multiplier set to: " + ((MultiplierValue/5).ToString("F2")));
            }

            GUILayout.Space(2);
            GUILayout.Label("____________________________________");
            GUILayout.Space(3);

            /*scrollPosition3 = GUILayout.BeginScrollView(scrollPosition3, GUILayout.Width(290), GUILayout.Height(100));
            for (int i = 0; i < PartResourceLibrary.Instance.resourceDefinitions.ToList().Count; i++)
            {
                string Resource = PartResourceLibrary.Instance.resourceDefinitions.ToList().ElementAt(i).name;
                if (PartResourceLibrary.Instance.resourceDefinitions.ToList().ElementAt(i).resourceTransferMode != ResourceTransferMode.NONE &&
                    PartResourceLibrary.Instance.resourceDefinitions.ToList().ElementAt(i).resourceFlowMode != ResourceFlowMode.ALL_VESSEL &&
                    PartResourceLibrary.Instance.resourceDefinitions.ToList().ElementAt(i).resourceFlowMode != ResourceFlowMode.NO_FLOW &&
                    (Resource != "EVA Propellant" && Resource != "Ore" && Resource != "ElectricCharge" && Resource != "IntakeAir"))
                {
                    GUILayout.Label("Resource Name: " + Resource);
                    GUILayout.Space(3);

                    if (GUILayout.Button("Set as Station Keeping Resource"))
                    {
                        Settings.WriteStatKeepResource(Resource);
                        for (int j = 0; j < FlightGlobals.Vessels.Count; j++)
                        {
                            Vessel vessel = FlightGlobals.Vessels.ElementAt(j);
                            if (vessel.situation == Vessel.Situations.ORBITING && vessel.vesselType != VesselType.SpaceObject && vessel.vesselType != VesselType.Unknown)
                            {
                                //VesselData.UpdateVesselFuel(vessel, ResourceManager.GetResources(vessel, Resource));
                            }
                        }
                        ScreenMessages.PostScreenMessage("Station Keeping Resource set to: " + Resource);
                    }
                    GUILayout.Space(3);
                }
            }
            GUILayout.EndScrollView();
            GUILayout.Label("Note: Changing resources requires switching to each Station Keeping vessel");
            GUILayout.Space(2);
            GUILayout.Label("____________________________________");
            GUILayout.Space(3);
            */
            GUI.skin.label.alignment = TextAnchor.MiddleCenter;
            MultiplierValue2 = GUILayout.HorizontalSlider(MultiplierValue2, 0.5f, 50.0f);
            GUILayout.Space(2);
            GUILayout.Label("Resource drain rate multiplier: " + (ResourceDifficulty.ToString("F1")));
            GUILayout.Space(2);
            GUILayout.Label("New Resource drain rate multiplier: " + (MultiplierValue2 / 5).ToString("F1"));
            GUILayout.Space(2);

            if (GUILayout.Button("Set Multiplier"))
            {
                Settings.WriteResourceRateDifficulty(MultiplierValue2 / 5);
                ScreenMessages.PostScreenMessage("Resource drain rate multiplier: " + ((MultiplierValue2 / 5).ToString("F1")));
            }

            GUILayout.EndVertical();
        }

        public string FormatDecayRateToString(double DecayRate)
        {
            double TimewarpRate = 0;

            DecayRate = Math.Abs(DecayRate);

            if (TimeWarp.CurrentRate == 0)
            {
                TimewarpRate = 1;
            }
            else
            {
                TimewarpRate = TimeWarp.CurrentRate;
            }

            DecayRate = DecayRate / TimewarpRate;
            string DecayRateString = "";
            double SecondsInYear = 0.0;
            double HoursInDay = 0.0;

            bool KerbinTime = GameSettings.KERBIN_TIME;

            if (KerbinTime)
            {
                SecondsInYear = 9203545;
                HoursInDay = 6;
            }
            else
            {
                SecondsInYear = 31557600;
                HoursInDay = 24;
            }

            double DecayRatePerDay = DecayRate * 60 * 60 * HoursInDay;
            double DecayRatePerYear = DecayRate * SecondsInYear;

            // Daily Rates //

            if (DecayRatePerDay > 1000.0)
            {
                DecayRateString = (DecayRatePerDay / 1000.0).ToString("F1") + "Km per day.";
            }

            else if (DecayRatePerDay <= 1000.0 && DecayRatePerDay >= 1.0)
            {
                DecayRateString = (DecayRatePerDay).ToString("F1") + "m per day.";
            }

            else if (DecayRatePerDay < 1.0 && DecayRatePerDay >= 0.01)
            {
                DecayRateString = (DecayRatePerDay * 10).ToString("F1") + "cm per day.";
            }

            else if (DecayRatePerDay < 0.01 && DecayRatePerDay >= 0.001)
            {
                DecayRateString = (DecayRatePerDay * 100).ToString("F1") + "mm per day.";
            }

            else if (DecayRatePerDay < 0.001)
            {
                if (DecayRatePerYear > 1000.0)
                {
                    DecayRateString = (DecayRatePerYear / 1000.0).ToString("F1") + "Km per year.";
                }

                else if (DecayRatePerYear <= 1000.0 && DecayRatePerYear >= 1.0)
                {
                    DecayRateString = (DecayRatePerYear).ToString("F1") + "m per year.";
                }

                else if (DecayRatePerYear < 1.0 && DecayRatePerYear >= 0.01)
                {
                    DecayRateString = (DecayRatePerYear * 10).ToString("F1") + "cm per year.";
                }

                else if (DecayRatePerYear < 0.01 && DecayRatePerYear >= 0.001)
                {
                    DecayRateString = (DecayRatePerYear * 100).ToString("F1") + "mm per year.";
                }

                else
                {
                    DecayRateString = "Negligible.";
                }
            }


            return DecayRateString;
        }

        public string FormatTimeUntilDecayInDaysToString(double TimeUntilDecayInDays)
        {
            TimeUntilDecayInDays = Math.Abs(TimeUntilDecayInDays);

            string DecayTimeString = "";
            double SecondsInYear = 0.0;
            double HoursInDay = 0.0;
            double DaysInYear = 0.0;

            bool KerbinTime = GameSettings.KERBIN_TIME;

            if (KerbinTime)
            {
                SecondsInYear = 9203545;
                HoursInDay = 6;
            }
            else
            {
                SecondsInYear = 31557600;
                HoursInDay = 24;
            }

            DaysInYear = SecondsInYear / (HoursInDay * 60 * 60);

            if (TimeUntilDecayInDays > DaysInYear)
            {
                if ((TimeUntilDecayInDays / DaysInYear) > 1000)
                {
                    DecayTimeString = "> 1000 years.";
                }
                else
                {
                    DecayTimeString = (TimeUntilDecayInDays/DaysInYear).ToString("F1") + " years.";
                }
            }

            else
            {
                if (TimeUntilDecayInDays > 1.0)
                {
                    DecayTimeString = TimeUntilDecayInDays.ToString("F1") + " days.";
                }

                else 
                {
                    if ((TimeUntilDecayInDays * HoursInDay) > 1.0)
                    {
                        DecayTimeString = (TimeUntilDecayInDays * HoursInDay).ToString("F1") + " hours.";
                    }

                    else
                    {
                        DecayTimeString = "Decay Imminent.";
                    }
                }
            }


            return DecayTimeString;
        }

        public string FormatTimeUntilDecayInSecondsToString(double TimeUntilDecayInSeconds)
        {
            TimeUntilDecayInSeconds = Math.Abs(TimeUntilDecayInSeconds);

            string DecayTimeString = "";
            double SecondsInYear = 0.0;

            bool KerbinTime = GameSettings.KERBIN_TIME;

            if (KerbinTime)
            {
                SecondsInYear = 9203545;
            }
            else
            {
                SecondsInYear = 31557600;
            }

            try
            {
                DecayTimeString = KSPUtil.dateTimeFormatter.PrintTime(Math.Abs(TimeUntilDecayInSeconds), 2, false);
            }
            catch (ArgumentOutOfRangeException)
            {
                DecayTimeString = "Error!";
            }
            
            if (TimeUntilDecayInSeconds > 1000 * SecondsInYear)
            {
                DecayTimeString = "> 1000 years.";
            }

            return DecayTimeString;
        }
    }
}
