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
    class VABUserInterface : MonoBehaviour
    {
        private static int currentTab = 0;
        private static string[] tabs = { "Orbital Decay Utilities" };
        private static Rect MainwindowPosition = new Rect(0, 0, 300, 400);
        private static Rect DecayBreakdownwindowPosition = new Rect(0, 0, 450, 150);
        private static Rect NBodyManagerwindowPosition = new Rect(0, 0, 450, 150);
        private static GUIStyle windowStyle = new GUIStyle(HighLogic.Skin.window);
        private static Color tabUnselectedColor = new Color(0.0f, 0.0f, 0.0f);
        private static Color tabSelectedColor = new Color(0.0f, 0.0f, 0.0f);
        private static Color tabUnselectedTextColor = new Color(0.0f, 0.0f, 0.0f);
        private static Color tabSelectedTextColor = new Color(0.0f, 0.0f, 0.0f);
        private GUISkin skins = HighLogic.Skin;
        private int id = Guid.NewGuid().GetHashCode();
        public static ApplicationLauncherButton ToolbarButton = null;
        public bool Visible = false;

        public static Texture launcher_icon = null;
        float AltitudeValue = 70000f;
        CelestialBody ReferenceBody = FlightGlobals.GetHomeBody();

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
                var Scenes = ApplicationLauncher.AppScenes.VAB | ApplicationLauncher.AppScenes.SPH;
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
                MainwindowPosition = GUILayout.Window(id, MainwindowPosition, MainWindow, "Orbital Decay Utilities", windowStyle);
            }
        }

        public void MainWindow(int windowID)
        {
            if (GUI.Button(new Rect(MainwindowPosition.width - 22, 3, 19, 19), "x"))
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

                default:
                    break;
            }
            GUILayout.EndVertical();
            GUI.DragWindow();
            MainwindowPosition.x = Mathf.Clamp(MainwindowPosition.x, 0f, Screen.width - MainwindowPosition.width);
            MainwindowPosition.y = Mathf.Clamp(MainwindowPosition.y, 0f, Screen.height - MainwindowPosition.height);
        }

        public void InformationTab()
        {
            double VesselMass = CalculateMass();
            double VesselArea = CalculateArea();

            GUILayout.BeginHorizontal();
            GUI.skin.label.fontSize = 16;
            GUI.skin.label.alignment = TextAnchor.MiddleCenter;
            GUILayout.Label("Vessel Information", GUILayout.Width(290));
            GUI.skin.label.alignment = TextAnchor.MiddleLeft;
            GUI.skin.label.fontSize = 12;
            GUILayout.EndHorizontal();

            GUILayout.BeginVertical();

            GUILayout.Space(3);
            GUILayout.Label("Vessel Information:");
            GUILayout.Space(2);
            GUILayout.Label("_________________________________________");
            GUILayout.Space(2);
            GUILayout.Label("Mass: " + (CalculateMass() * 1000).ToString("F2") + " Kg") ;
            GUILayout.Space(2);
            GUILayout.Label("Prograde Area: " + CalculateArea().ToString("F2") + " Square Meters");
            GUILayout.Space(2);
            GUILayout.Label("Total Area: " + (CalculateArea() * 4).ToString("F2") + " Square Meters");
            GUILayout.Space(2);
            GUILayout.Label("_________________________________________");
            GUILayout.Space(3);

            GUILayout.Label("Decay Information:");
            GUILayout.Space(2);
            GUILayout.Label("_________________________________________");
            GUILayout.Space(2);
            GUILayout.Label("Reference Body: " + ReferenceBody.name);
            GUILayout.Space(2);
            GUILayout.BeginHorizontal();
            foreach (CelestialBody body in FlightGlobals.Bodies)
            {
                if (body.atmosphere)
                {
                    if (GUILayout.Button(body.name))
                    {
                        ReferenceBody = body;
                    }
                    GUILayout.Space(2);
                }
            }

            float MaxDisplayValue = float.Parse(ReferenceBody.atmosphereDepth.ToString()) * 30f;
            if (ReferenceBody == Sun.Instance.sun)
            {
                MaxDisplayValue = float.Parse(ReferenceBody.atmosphereDepth.ToString()) * 100000f;
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(2);
            GUILayout.Label("Reference Altitude:");
            GUILayout.Space(2);
            AltitudeValue = GUILayout.HorizontalSlider(AltitudeValue, float.Parse(ReferenceBody.atmosphereDepth.ToString()), MaxDisplayValue);
            GUILayout.Space(2);
            GUILayout.Label("Altitude set: " + (AltitudeValue/1000).ToString("F1") + "Km.");
            GUILayout.Space(2);
            GUILayout.Label("Decay Rate (Atmospheric Drag): " + UserInterface.FormatDecayRateToString(DecayManager.EditorDecayRateAtmosphericDrag(CalculateMass() * 1000, CalculateArea(), ReferenceBody.Radius + AltitudeValue, 0, ReferenceBody)));
            GUILayout.Space(2);
            GUILayout.Label("Decay Rate (Radiation Pressure): " + UserInterface.FormatDecayRateSmallToString(DecayManager.EditorDecayRateRadiationPressure(CalculateMass() * 1000, CalculateArea(), ReferenceBody.Radius + AltitudeValue, 0, ReferenceBody)));
            GUILayout.Space(2);
            GUILayout.Label("Estimated Orbital Lifetime: " + UserInterface.FormatTimeUntilDecayInDaysToString(DecayManager.DecayTimePredictionEditor(CalculateArea(), CalculateMass() * 1000, ReferenceBody.Radius + AltitudeValue, 0, ReferenceBody)));
            GUILayout.Space(2);
            GUILayout.Label("_________________________________________");
            GUILayout.Space(3);

            GUILayout.Label("Station Keeping Information:");
            GUILayout.Space(2);
            GUILayout.Label("_________________________________________");
            GUILayout.Space(2);
            GUILayout.Label("Total Fuel: " + GetFuel() * 1000 + "Kg.");
            GUILayout.Space(2);
            GUILayout.Label("Useable Resources: ");
            GUILayout.BeginHorizontal();

            Dictionary<string, double> ResourceQuantites = new Dictionary<string, double>();
            double tempHold = 0;

            foreach (Part part in EditorLogic.SortedShipList)
            { 

                foreach (PartResource res in part.Resources)
                {
                    if (ResourceQuantites.ContainsKey(res.resourceName))
                    {
                        double previousValue = 0;
                        ResourceQuantites.TryGetValue(res.resourceName, out previousValue);
                        ResourceQuantites.Remove(res.resourceName);
                        ResourceQuantites.Add(res.resourceName, previousValue + res.maxAmount);
                    }
                    else
                    {
                        ResourceQuantites.Add(res.resourceName, res.maxAmount);
                    }
                    //GUILayout.Label(res.resourceName + ": " + res.maxAmount);
                }

                foreach (string resource in ResourceQuantites.Keys)
                {
                    double quantity = 0;
                    ResourceQuantites.TryGetValue(resource, out quantity);
                    GUILayout.Label(resource + " : " + quantity);

                }

            }


            GUILayout.EndHorizontal();
            GUILayout.Space(2);
            //GUILayout.Label("Maximum possible Station Keeping fuel lifetime: " + (UserInterface.FormatTimeUntilDecayInDaysToString(GetMaximumPossibleLifetime())));
            GUILayout.Space(2);
           // GUILayout.Label("Maximum possible lifetime: " + (UserInterface.FormatTimeUntilDecayInDaysToString(DecayManager.DecayTimePredictionEditor(CalculateArea(), CalculateMass() * 1000, ReferenceBody.Radius + AltitudeValue, 0, ReferenceBody) + 
           //  + DecayManager.EditorDecayRateAtmosphericDrag(CalculateMass() * 1000, CalculateArea(), ReferenceBody.Radius + AltitudeValue, 0, ReferenceBody)
           //   + GetMaximumPossibleLifetime() )));
            GUILayout.Space(2);
            GUILayout.Label("_________________________________________");
            GUILayout.Space(3);

            GUILayout.EndVertical();
        }

        public double CalculateMass()
        {
            return EditorLogic.fetch.ship.GetTotalMass();
        }

        public double CalculateArea()
        {
            double Area = 0;
            Area = (EditorLogic.fetch.ship.shipSize.y * EditorLogic.fetch.ship.shipSize.z) / 4;

            return Area;
        }

        public double GetFuel()
        {
            double Total = 0;
            float EmptyMass = 0;
            float FuelMass = 0;

            EditorLogic.fetch.ship.GetShipMass(out EmptyMass, out FuelMass);

            Total = FuelMass;

            return Total;
        }

        public double GetMaximumPossibleLifetime()
        {
            double Lifetime = 0;
            
            Part[] constructParts = EditorLogic.RootPart.FindChildParts<Part>();
            constructParts.AddUnique(EditorLogic.RootPart);

            foreach (Part p in constructParts)
            {
                Dictionary<string, double> UsableFuels = new Dictionary<string,double>();
                Dictionary<string, double> FuelRatios = new Dictionary<string,double>();

                if (GetEfficiencyEng(p) != 0)
                {
                    for (int i = 0; i < GetResources().Count; i++)
                    {
                        foreach (Propellant pro in p.FindModuleImplementing<ModuleEngines>().propellants)
                        {
                            if (pro.ToString() == GetResources().ElementAt(i))
                            {
                                // Blah blah blah 
                                UsableFuels.Add(pro.ToString(), pro.totalResourceCapacity);
                                FuelRatios.Add(pro.ToString(), pro.ratio);

                                double ResEff = 1.0 / GetEfficiencyEng(p);
                                var ClockType = Settings.Read24Hr();
                                double HoursInDay = 6;

                                if (ClockType == true)
                                {
                                    HoursInDay = 24.0;
                                }
                                else
                                {
                                    HoursInDay = 6.0;
                                }

                                Lifetime = Lifetime + ((pro.totalResourceCapacity) / ((DecayManager.EditorDecayRateRadiationPressure(CalculateMass(), CalculateArea(), ReferenceBody.Radius + AltitudeValue, 0, ReferenceBody) * ResEff * Settings.ReadResourceRateDifficulty())) / (60 * 60 * HoursInDay));

                            }
                        }
                    }
                }
            }

            return Lifetime;
        }

        public double GetPropellants(Propellant prop)
        {
            return 0;
        }

        public string GetEngines()
        {
            string Engines = "";

            return Engines;
        }

        public List<string> GetResources()
        {
            List<string> reslist = new List<string>();

            Part[] constructParts = EditorLogic.RootPart.FindChildParts<Part>();
            constructParts.AddUnique(EditorLogic.RootPart);

            foreach (Part p in constructParts)
            {

                if (p.Resources.Count != 0)
                {
                    foreach (PartResource pRes in p.Resources)
                    {
                        if (pRes.resourceName != "IntakeAir" && pRes.resourceName != "ElectricCharge")
                        {
                            reslist.Add(pRes.resourceName);
                        }
                    }
                }

                /*
                ProtoPartSnapshot protopart = p.protoPartSnapshot;
             
                {
                    foreach (ProtoPartModuleSnapshot protopartmodulesnapshot in protopart.modules)
                    {
                        if (protopartmodulesnapshot.moduleName == "ModuleOrbitalDecay")
                        {
                            ConfigNode node = protopartmodulesnapshot.moduleValues.GetNode("stationKeepData");
                            reslist.Add(node.GetValue("resources"));
                            break;
                        }
                    }
                }
                 */
            }

            return reslist;
        }

        public double GetEfficiencyEng(Part p)
        {
            double Efficiency = 0;

                if (p.FindModuleImplementing<ModuleEnginesFX>())
                {
                    Efficiency = p.FindModuleImplementing<ModuleEnginesFX>().realIsp; // Real isp? Yeah why not
                }

            return Efficiency;
        }

        public double GetEfficiencyRCS(Part p)
        {
            double Efficiency = 0;

            if (p.FindModuleImplementing<ModuleRCS>())
            {
                Efficiency = p.FindModuleImplementing<ModuleRCS>().realISP; // Real isp? Yeah why not
            }

            return Efficiency;
        }
    }
}
