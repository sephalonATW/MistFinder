using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.PlayerLoop;

namespace MistFinders
{
    internal class SE_InfestedMineFinder : StatusEffect
    {
        private bool shouldRemove = false;

        public override void UpdateStatusEffect(float dt)
        {
            Jotunn.Logger.LogInfo("SE_InfestedMineFinder UpdateStatusEffect");
            base.UpdateStatusEffect(dt);

            UnityEngine.Vector3 playerPos = Player.m_localPlayer.transform.position;
            MistFinders.Instance.m_locRPC.RequestLocations("Mistlands_DvergrTownEntrance1", "Mistlands_DvergrTownEntrance2", playerPos);

            shouldRemove = true;
            Player.m_localPlayer.Message(MessageHud.MessageType.Center, "You sniff out some mines...");
        }

        public override bool IsDone()
        {
            Jotunn.Logger.LogInfo("SE_InfestedMineFinder isDone");
            return shouldRemove || base.IsDone();
        }
    }
}
