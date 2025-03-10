using System;
using System.Collections.Generic;
using GTA;
using GTA.Math;
using GTA.Native;
using GTA.UI;
using System.Windows.Forms;

namespace ModFive
{
    public class Bounty
    {
        public int bounty = 0;
        private Random random = new Random();
        Player me;

        public void GiveBounty(int amount)
        {
            if (me.IsPlayerWearingGlassesOrMask()) return;
            bounty += amount;
            Notification.PostTicker("You have collected a bounty of $" + bounty, false);
        }

        public void HandleSpy(Ped npc)
        {
            if (npc == null) return;
            Ped player = Game.Player.Character;
            if (player.IsInVehicle() || player.IsDead || bounty <= 5000) return;
            npc.Task.Combat(player);
            npc.RelationshipGroup = Function.Call<int>(Hash.GET_HASH_KEY, "HATES_PLAYER");
            Notification.PostTicker("The citizen is spying on you", false);
        }

        public void HandleNpcCall911(Ped npc)
        {
            Ped player = Game.Player.Character;
            if (npc == null) return;
            if (player.IsInVehicle() || player.IsDead || bounty <= 5000) return;
            Function.Call(Hash.TASK_USE_MOBILE_PHONE, npc, true);
            Script.Wait(3000);
            Game.Player.WantedLevel = 1;
            Notification.PostTicker("The citizen reported you to the police", false);
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
    }
}
