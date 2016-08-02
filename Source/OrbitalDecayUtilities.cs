using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;

namespace WhitecatIndustries
{
    class OrbitalDecayUtilities : MonoBehaviour // Hopefully A new utilities class to clear up some clutter
    {
        public static Vector3d FlipYZ(Vector3d vector)
        {
            return new Vector3d(vector.x, vector.z, vector.y);
        }
    }
}
