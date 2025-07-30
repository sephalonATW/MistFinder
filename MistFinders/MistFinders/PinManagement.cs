using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Minimap;

namespace MistFinders
{
    [HarmonyPatch]
    internal static class PinManagement
    {
        static UnityEngine.Color mineColor = new UnityEngine.Color(0.0f, 0.70588f, 0.7804f, 1.0f);
        static UnityEngine.Color skullColor = new UnityEngine.Color(1.0f, 0.847f, 0.0f, 1.0f);

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Minimap), nameof(Minimap.UpdatePins))]
        private static void MinimapUpdatePins_PostFix()
        {
            foreach (PinData pin in Minimap.instance.m_pins)
            {
                if (pin.m_iconElement == null || pin.m_shouldDelete)
                {
                    continue;
                }

                string mineStr = MistFinders.Instance.m_locRPC.GetMapStringForLocationType(MistFinders.LocationQueryRPC.LocationType.Mine);
                string skullStr = MistFinders.Instance.m_locRPC.GetMapStringForLocationType(MistFinders.LocationQueryRPC.LocationType.Skull);

                if (pin.m_name.Equals(mineStr))
                {
                    pin.m_iconElement.color *= mineColor;
                }
                if (pin.m_name.Equals(skullStr))
                {
                    pin.m_iconElement.color *= skullColor;
                }
            }
        }

    }
}
