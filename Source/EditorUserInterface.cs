using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;
using KSP.UI.Screens;

namespace WhitecatIndustries
{
    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
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
    }
}
