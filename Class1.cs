using System;
using System.Collections.Generic;
using GTA;
using GTA.Math;
using GTA.Native;
using GTA.UI;
using System.Windows.Forms;

namespace ModFive
{
    public class WantedSystem : Script
    {
        private int previousWanted = 0;
        private Random random = new Random();
        private int lastSpawnTime = 0;

        private Bounty bountySystem;
        private People Human;
        private Player Player;

        public WantedSystem()
        {
            bountySystem = new Bounty();
            previousWanted = 0;
            Tick += OnTick;
            KeyDown += OnKeyDown;
        }

        private void OnTick(object sender, EventArgs e)
        {
            int currentTime = Game.GameTime;
            int currentWantedLevel = Game.Player.WantedLevel;

            if (previousWanted > 0 && currentWantedLevel == 0)
                bountySystem.GiveBounty(previousWanted * 100);

            if (Game.Player.Character.IsDead)
                Player.OnPlayerDeath();

            if (bountySystem.bounty >= 5000)
                Human.HandleNpcReact();

            if (bountySystem.bounty >= 1000 && random.Next(0, 100) < 5 && currentTime > lastSpawnTime + 60000)
            {
                bountySystem.SpawnBountyHunter();
                lastSpawnTime = currentTime;
            }

            previousWanted = currentWantedLevel;
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F5)
            {
                bountySystem.GiveBounty(1000);
            }
            if (e.KeyCode == Keys.F6)
            {
                bountySystem.bounty = 0;
            }
        }
    }
}
