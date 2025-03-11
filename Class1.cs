using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GTA;
using GTA.Math;
using GTA.Native;
using GTA.UI;
using System.Windows.Forms;
using GTA.NaturalMotion;

namespace ModFive
{
    public class WantedSystem : Script
    {
        int previousWanted = 0;
        public int bounty = 0;
        private Random random = new Random();
        private int lastSpawnTime = 0;
        private int lastReactTime = 0;
        private int lastBribeTime = 0;
        private bool devMode = false;
        private List<Ped> previousPeds = new List<Ped>();

        public WantedSystem()
        {

            previousWanted = 0;
            Tick += OnTick;
            KeyDown += OnKeyDown;
            KeyUp += OnKeyUp;
        }

        private bool IsNpcPolice(Ped npc)
        {
            int group = npc.Exists() ? npc.RelationshipGroup.Hash : -1;
            return npc.Exists() && group == Function.Call<int>(Hash.GET_HASH_KEY, "COP");
        }

        private bool IsBountyHunter(Ped npc)
        {
            return npc.Exists() && npc.RelationshipGroup.Hash == Function.Call<int>(Hash.GET_HASH_KEY, "HATES_PLAYER");
        }

        private void OnPlayerKill(Ped victim)
        {
            if (IsNpcPolice(victim) || IsBountyHunter(victim))
            {
                GiveBounty(250);
            }
        }


        private void HandleNearbyNpc()
        {
            Ped player = Game.Player.Character;
            Ped[] Nearbypeds = World.GetNearbyPeds(player, 2f);
            if (player.IsInVehicle() || player.IsDead || bounty <= 0) return;
            foreach (Ped npc in Nearbypeds)
            {
                if(IsNpcPolice(npc))
                    ShowHelpText("Press E to bribe the police");
                HandleNpcReact();
            }
        }

        private void ShowHelpText(string text)
        {
            Function.Call(Hash.BEGIN_TEXT_COMMAND_DISPLAY_HELP, "STRING");
            Function.Call(Hash.ADD_TEXT_COMPONENT_SUBSTRING_PLAYER_NAME, text);
            Function.Call(Hash.END_TEXT_COMMAND_DISPLAY_HELP, 0, false, true, -1);
        }

        private void BribePolice()
        {
            Ped player = Game.Player.Character;
            if (player.IsInVehicle() || player.IsDead || bounty <= 0 || Game.GameTime <= lastBribeTime + 300000) return;

            foreach (Ped npc in World.GetNearbyPeds(player, 2f))
            {
                if (!npc.Exists() || npc.IsDead || npc.IsPlayer || npc.IsInVehicle() || !IsNpcPolice(npc)) continue;
                if (player.Money < bounty) return;
                if (random.Next(1, 100) <= 30)
                {
                    Notification.PostTicker("The police refused your bribe", false);
                    Game.Player.WantedLevel = 1;
                    return;
                }

                player.Money -= bounty;
                GiveBounty(-bounty);
                Game.Player.WantedLevel = 0;
                Notification.PostTicker("You have bribed the police", false);
                lastBribeTime = Game.GameTime;
                return;
            }
        }


        private void GiveBounty(int amount)
        {
            if (Game.Player.Character.IsWearingHelmet) return;
            bounty += amount;
            Notification.PostTicker("You have collected a bounty of $" + bounty, false);
        }

        public void OnPlayerDeath()
        {
            if (bounty > 0)
            {
                GiveBounty(-bounty);
                Notification.PostTicker("You have lost your bounty", false);
                Game.Player.Character.Money -= bounty;
            }
        }

        public void HandleNpcReact()
        {
            Ped player = Game.Player.Character;
            if (player.IsInVehicle() || player.IsDead || bounty <= 5000 || player.IsWearingHelmet) return;

            int currentTime = Game.GameTime;
            bool canReact = currentTime > lastReactTime + 60000;

            foreach (Ped npc in World.GetNearbyPeds(player, 50))
            {
                if (npc.IsDead || npc.IsPlayer || npc.IsInVehicle() || npc.IsInCombatAgainst(player) || IsNpcPolice(npc)) continue;

                Function.Call(Hash.PLAY_PED_AMBIENT_SPEECH_NATIVE, npc, "GENERIC_SHOCKED_HIGH", "SPEECH_PARAMS_FORCE");

                if (canReact && random.Next(100) < 50)
                {
                    if (random.Next(100) < 50)
                        Function.Call(Hash.TASK_USE_MOBILE_PHONE, npc, true);
                    else
                        npc.Task.Combat(player);

                    Wait(3000);
                    Game.Player.WantedLevel = 1;
                    Notification.PostTicker("The citizen reported you to the police", false);
                    lastReactTime = currentTime;
                }

                npc.Task.ReactAndFlee(player);
                npc.RelationshipGroup = Function.Call<int>(Hash.GET_HASH_KEY, "HATES_PLAYER");
            }
        }

        public void SpawnBountyHunter()
        {
            Ped player = Game.Player.Character;
            int hunterCount = random.Next(1, 6);
            Vector3[] spawnOffsets =
            {
                new Vector3(-10f, -10f, 0f),
                new Vector3(10f, -10f, 0f),
                new Vector3(-10f, 10f, 0f),
                new Vector3(10f, 10f, 0f),
                new Vector3(-10f, 10f, 0f),
            };

            List<Ped> hunters = new List<Ped>();
            Vehicle hunterVehicle = hunterCount > 2 ? CreateHunterVehicle(player.Position) : null;

            for (int i = 0; i < hunterCount; i++)
            {
                Ped hunter = CreateHunter(player.Position + spawnOffsets[i % spawnOffsets.Length]);
                if (hunter == null) continue;

                if (hunterVehicle != null)
                    AssignHunterToVehicle(hunter, hunterVehicle, i);

                hunters.Add(hunter);
            }

            Notification.PostTicker(hunterCount + " Bounty hunter(s) are after you!", true);
        }

        private Vehicle CreateHunterVehicle(Vector3 position)
        {
            VehicleHash[] suitableVehicles = new VehicleHash[]
            {
                VehicleHash.Seminole,    
                VehicleHash.Granger,     
                VehicleHash.Minivan,     
                VehicleHash.Serrano,     
                VehicleHash.Fugitive     
            };
            VehicleHash randomVehicle = suitableVehicles[new Random().Next(suitableVehicles.Length)];

            return World.CreateVehicle(new Model(randomVehicle), position + new Vector3(-15f, 15f, 0f));
        }


        private Ped CreateHunter(Vector3 spawnPos)
        {
            Model pedModel = new Model(Function.Call<int>(Hash.GET_HASH_KEY, GetRandomPedModel()));
            if (!pedModel.IsValid) return null;

            Ped hunter = World.CreatePed(pedModel, spawnPos);
            hunter.RelationshipGroup = Function.Call<int>(Hash.GET_HASH_KEY, "HATES_PLAYER");
            hunter.Armor = 100;
            hunter.Health = 500;
            hunter.Weapons.Give(GetRandomWeapon(), 100, true, true);
            hunter.Task.Combat(Game.Player.Character);
            return hunter;
        }

        private void AssignHunterToVehicle(Ped hunter, Vehicle vehicle, int index)
        {
            hunter.SetIntoVehicle(vehicle, index == 0 ? VehicleSeat.Driver : (VehicleSeat)(index - 1));
        }

        private WeaponHash GetRandomWeapon()
        {
            WeaponHash[] weapons = { 
               WeaponHash.Pistol,
               WeaponHash.Knife,
               WeaponHash.AssaultShotgun,
               WeaponHash.MicroSMG
            };
            return weapons[random.Next(weapons.Length)];
        }

        private string GetRandomGangPedModel()
        {
            string[] gangPedModels = new string[]
            {
                "g_m_m_armboss_01", "g_m_m_armgoon_01", "g_m_m_armlieut_01", "g_m_m_mexboss_01",
                "g_m_m_mexboss_02", "g_m_y_ballaeast_01", "g_m_y_ballaorig_01", "g_m_y_ballasout_01",
                "g_m_y_famca_01", "g_m_y_famdnf_01", "g_m_y_famfor_01", "g_m_y_lost_01",
                "g_m_y_lost_02", "g_m_y_lost_03", "g_m_y_mexgang_01", "g_m_y_mexgoon_01",
                "g_m_y_mexgoon_02", "g_m_y_mexgoon_03", "g_m_y_pologoon_01", "g_m_y_pologoon_02"
            };
            return gangPedModels[random.Next(gangPedModels.Length)];
        }

        private void OnTick(object sender, EventArgs e)
        {
            int currentTime = Game.GameTime;
            int currentWantedLevel = Game.Player.WantedLevel;
            Ped player = Game.Player.Character;
            Ped[] allPeds = World.GetNearbyPeds(player, 20f); ;

            if (previousWanted > 0 && currentWantedLevel == 0)
            {
                int bountyReward = previousWanted * 100;
                GiveBounty(bountyReward);
                Notification.PostTicker($"You have collected a bounty of ${bountyReward}", false);
            }

            if (player.IsDead)
                OnPlayerDeath();

            if (bounty >= 5000)
                HandleNearbyNpc();

            if (bounty >= 1000 && random.Next(0, 100) < 5 && currentTime > lastSpawnTime + 300000)
            {
                SpawnBountyHunter();
                lastSpawnTime = currentTime;
            }

            foreach (Ped ped in allPeds)
            {
                if (ped.Exists() && ped.IsDead && ped.Killer == player)
                {
                    OnPlayerKill(ped);
                }
            }

            previousPeds = allPeds.Where(p => p.Exists() && !p.IsDead).ToList();
            previousWanted = currentWantedLevel;
        }


        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if(devMode)
            {
                switch (e.KeyCode)
                {
                    case Keys.F5:
                        GiveBounty(1000);
                        break;
                    case Keys.F6:
                        bounty = 0;
                        break;
                    case Keys.F7:
                        SpawnBountyHunter();
                        break;
                }
            }
        }

        private void OnKeyUp(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Z:
                    devMode = !devMode;
                    Notification.PostTicker("Dev mode: " + devMode, false);
                    break;
                case Keys.E:
                    BribePolice();
                    break;

            }
        }

    }

}