using System;
using System.Collections.Generic;
using GTA;
using GTA.Math;
using GTA.Native;
using GTA.UI;
using System.Windows.Forms;


namespace ModFive
{
    class People
    {
        private Random random = new Random();
        Bounty bountySystem;
        Player me;
        private int lastReactTime = 0;

        public void HandleNpcReact()
        {
            Ped player = Game.Player.Character;
            int currentTime = Game.GameTime;
            if (player.IsInVehicle() || player.IsDead || bountySystem.bounty <= 5000 || me.IsPlayerWearingGlassesOrMask()) return;

            foreach (Ped npc in World.GetNearbyPeds(player, 50))
            {
                if (npc.IsDead || npc.IsPlayer || npc.IsInVehicle()) continue;
                Function.Call(Hash.PLAY_PED_AMBIENT_SPEECH_NATIVE, npc, "GENERIC_SHOCKED_HIGH", "SPEECH_PARAMS_FORCE");

                if (random.Next(100) < 50 && currentTime > lastReactTime + 60000)
                {
                    if (random.Next(0, 2) == 0)
                        bountySystem.HandleSpy(npc);
                    else
                        bountySystem.HandleNpcCall911(npc);

                    lastReactTime = currentTime;
                }
                Function.Call(Hash.TASK_SMART_FLEE_PED, npc, player, 100f, -1, true, true);
                npc.Task.ReactAndFlee(player);
                npc.RelationshipGroup = Function.Call<int>(Hash.GET_HASH_KEY, "HATES_PLAYER");
            }
        }
    }
}
