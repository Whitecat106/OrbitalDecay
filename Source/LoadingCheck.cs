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
    [KSPAddon(KSPAddon.Startup.MainMenu, false)]
    public class LoadingCheck : MonoBehaviour
    {
        public string KSPOrbitDecayLite = KSPUtil.ApplicationRootPath + "GameData/WhitecatIndustries/OrbitDecayLite/KSPOrbitDecayLite.dll";
        public string RealSolar = KSPUtil.ApplicationRootPath + "GameData/RealSolarSystem/Plugins/RealSolarSystem.dll";

        void Start()
        {
            if (System.IO.File.Exists(RealSolar))
            {
                Settings.WriteRD(true);
                Settings.Write24H(true);
                Settings.WriteDifficulty(1.0);
                Settings.WritePlanetariumTracking(true);
                print("WhitecatIndustries - Real Solar System Detected, Multiplier Set Changed");
            }

            else
            {
                Settings.WriteDifficulty(1.0);
                Settings.Write24H(false);
                Settings.WriteRD(false);
                Settings.WritePlanetariumTracking(true);
            }
        }

    }
}