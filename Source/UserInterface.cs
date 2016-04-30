// 1.2.0 Issues:

// Stock decay - Eccentricity problem -- Remove for now? -- No lets work this one out! -- Or maybe not... 1.3.0
// add RSS realistic active decay -- 1.3.0
// add protopartsnapshot information 1.3.0 
// Multiple resources active at the same time? -- 1.3.0
// Planetarium tracking by default? -- In RSS
// Vessel update on save load or scene swtich -- Maybe fixed

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
                GUILayout.Space(15);
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
            GUI.skin.label.alignment = TextAnchor.MiddleLeft;
            GUILayout.Label("Information", GUILayout.Width(290));
            GUILayout.EndHorizontal();
            GUILayout.Space(5);

            GUILayout.BeginVertical();
            GUILayout.Space(3);
            GUILayout.EndVertical();

            scrollPosition1 = GUILayout.BeginScrollView(scrollPosition1, GUILayout.Width(290), GUILayout.Height(480));
            bool Realistic = Settings.ReadRD();
            var ClockType = Settings.Read24Hr();
            var Resource = Settings.ReadStationKeepingResource();
        
            foreach (Vessel vessel in FlightGlobals.Vessels)
            {
                if (vessel.situation == Vessel.Situations.ORBITING && vessel.vesselType != VesselType.SpaceObject && vessel.vesselType != VesselType.Unknown && vessel.vesselType != VesselType.Debris)
                {
                    string StationKeeping = VesselData.FetchStationKeeping(vessel).ToString();
                    string StationKeepingFuelRemaining = VesselData.FetchFuel(vessel).ToString("F3");
                    var ButtonText = "";
                    var HoursInDay = 6.0;

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
                    GUILayout.Space(3);
                    GUILayout.Label("Vessel Name: " + vessel.vesselName);
                    GUILayout.Space(3);
                    GUILayout.Label("Orbiting Body: " + vessel.orbitDriver.orbit.referenceBody.GetName());
                    GUILayout.Space(3);

                    if (StationKeeping == "True")
                    {
                        GUILayout.Label("Current Decay Rate: Vessel is Station Keeping");
                        GUILayout.Space(3);
                        GUILayout.Label("Approximate Time Until Decay: Vessel is Station Keeping");
                        GUILayout.Space(3);
                    }
                    else
                    {
                        double DecayRate = 0.0;
                        double DecayNumber = 0.0;
                        if (Realistic == true)
                        {
                            DecayNumber = (((DecayManager.DecayRateRealistic(vessel) * 60.0 * 60.0 * HoursInDay) / TimeWarp.CurrentRate));
                        }
                        else
                        {
                            DecayNumber = (((DecayManager.DecayRateStock(vessel) * 60.0 * 60.0 * HoursInDay) / TimeWarp.CurrentRate));
                        }

                        DecayRate = DecayNumber;

                        if (DecayRate > 500000)
                        {
                            GUILayout.Label("Current Decay Rate: Vessel Periapsis too close to body atmosphere");
                        }

                        else if (DecayRate <= 0.000000001 && Math.Sign(DecayRate) == 1 || (DecayRate <= 0.000000001 && Math.Sign(DecayRate) == 0))
                        {
                            GUILayout.Label("Current Decay Rate: Vessel is in a stable orbit");
                        }

                        else if (DecayRate <= 0.000000001 && Math.Sign(DecayRate) == -1)
                        {
                            GUILayout.Label("Current Decay Rate: Vessel Periapsis too close to body atmosphere");
                        }

                        else if (DecayRate > 0.000000001 && DecayRate < 1000)
                        {
                            GUILayout.Label("Current Decay Rate: " + DecayRate.ToString("F1") + "m per day");
                        }

                        else
                        {
                            GUILayout.Label("Current Decay Rate: " + (DecayRate/1000).ToString("F1") + "Km per day");
                        }
                       
                        GUILayout.Space(3);

                        if (vessel.orbitDriver.orbit.referenceBody.atmosphere)
                        {
                            if (Realistic == true)
                            {
                                double RealisticDecayTime = DecayManager.RealisticDecayTimePrediction(vessel);
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

                                if (RealisticDecayTime < 0)
                                {
                                    GUILayout.Label("Approximate Time Until Decay: Decay Imminent");
                                }
                                else
                                {
                                    if (RealisticDecayTime > DaysInYear)
                                    {
                                        GUILayout.Label("Approximate Time Until Decay: " + (RealisticDecayTime / DaysInYear).ToString("F1") + " years.");
                                    }
                                    else
                                    {
                                        GUILayout.Label("Approximate Time Until Decay: " + RealisticDecayTime.ToString("F1") + " days.");
                                    }
                                }
                            }
                            else
                            {
                                double StockDecayTime = DecayManager.StockDecayTimePrediction(vessel);
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

                                if (StockDecayTime < 0)
                                {
                                    GUILayout.Label("Approximate Time Until Decay: Decay Imminent");
                                }

                                else
                                {
                                    if (StockDecayTime > DaysInYear)
                                    {
                                        GUILayout.Label("Approximate Time Until Decay: " + (StockDecayTime / DaysInYear).ToString("F1") + " years.");
                                    }
                                    else
                                    {
                                        GUILayout.Label("Approximate Time Until Decay: " + StockDecayTime.ToString("F1") + " days.");
                                    }
                                }

                                /*
                                DecayNumberX = (((DecayManager.DecayRateStock(vessel) * 60 * 60 * HoursInDay) / TimeWarp.CurrentRate));


                                DecayRateX = DecayNumberX;
                                double DecayTime = (((VesselData.FetchSMA(vessel)) - (vessel.orbitDriver.orbit.referenceBody.Radius + vessel.orbitDriver.orbit.referenceBody.atmosphereDepth)) / ((DecayRateX)));

                                if (DecayTime < 0)
                                {
                                    GUILayout.Label("Approximate Time Until Decay: Decay Imminent");
                                }

                                if (DecayTime >= 0 && DecayTime <= 365 && HoursInDay == 24)
                                {
                                    GUILayout.Label("Approximate Time Until Decay: " + (DecayTime).ToString("F1") + " days.");
                                }
                                else if (DecayTime > 365 && HoursInDay == 24)
                                {
                                    GUILayout.Label("Approximate Time Until Decay: " + (DecayTime / 365).ToString("F1") + " years.");
                                }
                                if (DecayTime >= 0 && DecayTime <= 425 && HoursInDay == 6)
                                {
                                    GUILayout.Label("Approximate Time Until Decay: " + (DecayTime).ToString("F1") + " days.");
                                }
                                else if (DecayTime > 425 && HoursInDay == 6)
                                {
                                    GUILayout.Label("Approximate Time Until Decay: " + (DecayTime / 425).ToString("F1") + " years.");
                                }
                                 * */
                            }
                        }
                        else
                        {
                            double DecayRateX = 0.0;
                            if (Realistic == true)
                            {
                                DecayRateX = (((DecayManager.DecayRateRealistic(vessel) * 60 * 60 * HoursInDay) / TimeWarp.CurrentRate));

                                double DecayTime = (((VesselData.FetchSMA(vessel)) - (vessel.orbitDriver.orbit.referenceBody.Radius)) / ((DecayRateX)));
                                if (DecayTime < 0)
                                {
                                    GUILayout.Label("Approximate Time Until Decay: Decay Imminent");
                                }
                                if (DecayTime >= 0 && DecayTime <= 365 && HoursInDay == 24)
                                {
                                    GUILayout.Label("Approximate Time Until Decay: " + (DecayTime).ToString("F1") + " days.");
                                }
                                else if (DecayTime > 365 && HoursInDay == 24)
                                {
                                    GUILayout.Label("Approximate Time Until Decay: " + (DecayTime / 365).ToString("F1") + " years.");
                                }
                                if (DecayTime >= 0 && DecayTime <= 425 && HoursInDay == 6)
                                {
                                    GUILayout.Label("Approximate Time Until Decay: " + (DecayTime).ToString("F1") + " days.");
                                }
                                else if (DecayTime > 425 && HoursInDay == 6)
                                {
                                    GUILayout.Label("Approximate Time Until Decay: " + (DecayTime / 425).ToString("F1") + " years.");
                                }
                            }
                            else
                            {
                                double StockDecayTime = DecayManager.StockDecayTimePrediction(vessel);
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

                                if (StockDecayTime > DaysInYear)
                                {
                                    GUILayout.Label("Approximate Time Until Decay: " + (StockDecayTime / DaysInYear).ToString("F1") + " years.");
                                }
                                else
                                {
                                    GUILayout.Label("Approximate Time Until Decay: " + StockDecayTime.ToString("F1") + " days.");
                                }
                            }
                        }
                        GUILayout.Space(3);
                    }

                    GUILayout.Label("Station Keeping: " + StationKeeping);
                    GUILayout.Space(3);
                    GUILayout.Label("Station Keeping Fuel Remaining: " + StationKeepingFuelRemaining);
                    GUILayout.Space(3);
                    if (StationKeeping == "True")
                    {
                        double DecayRateSKL = 0;
                        if (Realistic == true)
                        {
                            DecayRateSKL = (DecayManager.DecayRateRealistic(vessel));
                        }
                        else
                        {
                            DecayRateSKL = (DecayManager.DecayRateStock(vessel));
                        }

                        double StationKeepingLifetime = (double.Parse(StationKeepingFuelRemaining) / ((DecayRateSKL / TimeWarp.CurrentRate) * ResourceManager.GetEfficiency(Resource))) / (60 * 60 * HoursInDay);
                        GUILayout.Label("Station Keeping Fuel Lifetime: " + StationKeepingLifetime.ToString("F1") + " days.");
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
                            if (double.Parse(StationKeepingFuelRemaining) > 0.01  && (VesselData.FetchDryFuel(vessel)) > 0.01) // Good enough...
                            {
                                VesselData.UpdateStationKeeping(vessel, true);
                                ScreenMessages.PostScreenMessage("Vessel: " + vessel.vesselName + (": Station Keeping Enabled"));

                            }
                            else
                            {
                                ScreenMessages.PostScreenMessage("Vessel: " + vessel.vesselName + (" has no fuel to Station Keep!"));
                            }
                        }
                    }

                    GUILayout.Space(3);
                    GUILayout.EndVertical();
                }
                
            }
            GUILayout.EndScrollView();
        }

        public void SettingsTab()
        {
            
            GUILayout.BeginHorizontal();
            GUI.skin.label.alignment = TextAnchor.MiddleLeft;
            GUILayout.Label("Settings", GUILayout.Width(290));
            GUILayout.EndHorizontal();
            GUILayout.Space(5);
            GUILayout.BeginVertical();
            GUILayout.Space(10);

            var DecayDifficulty = Settings.ReadDecayDifficulty();
            var ResourceDifficulty = Settings.ReadResourceRateDifficulty();

          /*  if (GUILayout.Button("Toggle Realistic Decay")) // Realistic decay useless on a Stock Game
            {
                Settings.WriteRD(!Settings.ReadRD());
                if (Settings.ReadRD() == true)
                {
                    ScreenMessages.PostScreenMessage("Realistic Decay Enabled - Stock model disabled");
                }
                else
                {
                    ScreenMessages.PostScreenMessage("Realistic Decay Disabled - Stock model in use");
                }
            }
           */
            GUILayout.Space(3);
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
            GUILayout.Space(3);
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
                GUILayout.Space(3);
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

            GUILayout.Space(6);
            GUI.skin.label.alignment = TextAnchor.MiddleCenter;
            MultiplierValue = GUILayout.HorizontalSlider(MultiplierValue, 0.5f, 50.0f);
            GUILayout.Space(3);
            GUILayout.Label("Current Decay multiplier: " + (DecayDifficulty).ToString("F1"));
            GUILayout.Space(3); 
            GUILayout.Label("New Decay multiplier: " + (MultiplierValue/5).ToString("F1"));
            GUILayout.Space(3); 

            if (GUILayout.Button("Set Multiplier"))
            {
                Settings.WriteDifficulty(MultiplierValue/5);
                ScreenMessages.PostScreenMessage("Decay Multiplier set to: " + (MultiplierValue/5));
            }

            GUILayout.Space(3);

            scrollPosition3 = GUILayout.BeginScrollView(scrollPosition3, GUILayout.Width(290), GUILayout.Height(80));
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
                                VesselData.UpdateDryFuel(vessel, ResourceManager.GetDryResources(vessel, Resource));
                            }
                        }
                        ScreenMessages.PostScreenMessage("Station Keeping Resource set to: " + Resource);
                    }
                    GUILayout.Space(3);
                }
            }
            GUILayout.EndScrollView();
            GUILayout.Label("Note: Changing resources requires switching to each Station Keeping vessel");
            GUILayout.Space(3);
            GUI.skin.label.alignment = TextAnchor.MiddleCenter;
            MultiplierValue2 = GUILayout.HorizontalSlider(MultiplierValue2, 0.5f, 50.0f);
            GUILayout.Space(3);
            GUILayout.Label("Resource drain rate multiplier: " + ResourceDifficulty);
            GUILayout.Space(3);
            GUILayout.Label("New Resource drain rate multiplier: " + (MultiplierValue2 / 5).ToString("F1"));
            GUILayout.Space(3);

            if (GUILayout.Button("Set Multiplier"))
            {
                Settings.WriteResourceRateDifficulty(MultiplierValue2 / 5);
                ScreenMessages.PostScreenMessage("Resource drain rate multiplier: " + (MultiplierValue2 / 5));
            }

            GUILayout.EndVertical();
        }
    }
}
