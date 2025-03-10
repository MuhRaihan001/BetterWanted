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

        public WantedSystem()
        {

            previousWanted = 0;
            Tick += OnTick;
            KeyDown += OnKeyDown;
        }

        private void GiveBounty(int amount)
        {
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
            int currentTime = Game.GameTime;
            if (player.IsInVehicle() || player.IsDead || bounty <= 5000) return;

            foreach (Ped npc in World.GetNearbyPeds(player, 50))
            {
                if (npc.IsDead || npc.IsPlayer || npc.IsInVehicle() || npc.IsInCombatAgainst(player)) continue;
                Function.Call(Hash.PLAY_PED_AMBIENT_SPEECH_NATIVE, npc, "GENERIC_SHOCKED_HIGH", "SPEECH_PARAMS_FORCE");

                if (random.Next(100) < 50 && currentTime > lastReactTime + 60000)
                {
                    Function.Call(Hash.TASK_USE_MOBILE_PHONE, npc, true);
                    Wait(3000);
                    Game.Player.WantedLevel = 1;
                    Notification.PostTicker("The citizen reported you to the police", false);
                    lastReactTime = currentTime;
                }
                Function.Call(Hash.TASK_SMART_FLEE_PED, npc, player, 100f, -1, true, true);
                npc.Task.ReactAndFlee(player);
                npc.RelationshipGroup = Function.Call<int>(Hash.GET_HASH_KEY, "HATES_PLAYER");
            }
        }

        public void SpawnBountyHunter()
        {
            Ped player = Game.Player.Character;
            int hunterCount = random.Next(1, 5);
            Vector3[] spawnOffsets =
            {
                new Vector3(-10f, -10f, 0f),
                new Vector3(10f, -10f, 0f),
                new Vector3(-10f, 10f, 0f),
                new Vector3(10f, 10f, 0f)
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
            return World.CreateVehicle(new Model(VehicleHash.Ambulance), position + new Vector3(-15f, 15f, 0f));
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
            WeaponHash[] weapons = { WeaponHash.CarbineRifle, WeaponHash.AssaultRifle, WeaponHash.SMG, WeaponHash.Pistol };
            return weapons[random.Next(weapons.Length)];
        }

        private string GetRandomPedModel()
        {
            string[] pedModels = new string[]
            {
                "a_m_m_bevhills_01", "a_m_m_business_01", "a_m_m_farmer_01", "a_m_m_hillbilly_01",
                "a_m_m_indian_01", "a_m_m_mexcntrn_01", "a_m_m_skater_01", "a_m_m_soucent_01",
                "a_m_m_tourist_01", "a_m_y_hipster_01", "a_m_y_hiker_01", "a_m_y_skater_01",
                "a_m_y_vinewood_01", "a_m_y_musclbeac_01", "a_m_y_golfer_01", "a_m_y_soucent_01"
            };
            return pedModels[random.Next(pedModels.Length)];
        }

        private void OnTick(object sender, EventArgs e)
        {
            int currentTime = Game.GameTime;
            int currentWantedLevel = Game.Player.WantedLevel;
            if (previousWanted > 0 && currentWantedLevel == 0)
            {
                GiveBounty(previousWanted * 100);
                Notification.PostTicker("You have collected a bounty of $" + (previousWanted * 100), false);
            }

            if (Game.Player.Character.IsDead)
            {
                OnPlayerDeath();
            }

            if (bounty >= 5000)
                HandleNpcReact();
            if (bounty >= 1000 && random.Next(0, 100) < 5 && currentTime > lastSpawnTime + 60000)
            {
                SpawnBountyHunter();
                lastSpawnTime = currentTime;
            }

            previousWanted = currentWantedLevel;
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            switch(e.KeyCode)
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

}