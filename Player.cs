using GTA.Native;
using GTA;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GTA.UI;

namespace ModFive
{
    class Player
    {
        Bounty BountySystem;
        public bool IsPlayerWearingGlassesOrMask()
        {
            Ped player = Game.Player.Character;
            if (player.IsWearingHelmet) return true;
            int glasses = Function.Call<int>(Hash.GET_PED_PROP_INDEX, player, 1);
            int mask = Function.Call<int>(Hash.GET_PED_DRAWABLE_VARIATION, player, 1);
            return glasses != -1 || mask != 0;
        }

        public void OnPlayerDeath()
        {
            if(BountySystem.bounty > 0)
            {
                BountySystem.GiveBounty(-BountySystem.bounty);
                Notification.PostTicker("You have lost your bounty", false);
                Game.Player.Character.Money -= BountySystem.bounty;
            }
        }
    }
}
