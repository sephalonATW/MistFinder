using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.PlayerLoop;

namespace MistFinders
{
    internal class SE_SkullFinder : StatusEffect
    {
        private bool shouldRemove = false;

        public override void UpdateStatusEffect(float dt)
        {
            Jotunn.Logger.LogInfo("SE_SkullFinder UpdateStatusEffect");
            base.UpdateStatusEffect(dt);

            UnityEngine.Vector3 playerPos = Player.m_localPlayer.transform.position;
            MistFinders.Instance.m_locRPC.RequestLocations("Mistlands_Excavation1", "Mistlands_Giant1", playerPos);

            shouldRemove = true;
            Player.m_localPlayer.Message(MessageHud.MessageType.Center, "You sniff out some skulls...");
        }

        public override bool IsDone()
        {
            Jotunn.Logger.LogInfo("SE_SkullFinder isDone");
            return shouldRemove || base.IsDone();
        }
    }
}
