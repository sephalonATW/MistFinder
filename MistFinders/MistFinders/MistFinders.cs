using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Jotunn;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using System.Collections;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using static Minimap;

namespace MistFinders
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    internal class MistFinders : BaseUnityPlugin
    {
        public const string PluginGUID = "sephalon.MistFinders";
        public const string PluginName = "MistFinders";
        public const string PluginVersion = "1.0.2";

        public static MistFinders Instance;
        private ConfigEntry<int> config_mineDetectionRadius;
        private ConfigEntry<int> config_skullDetectionRadius;

        public LocationQueryRPC m_locRPC = new LocationQueryRPC();

        public void Awake()
        {
            Instance = this;
            
            PrefabManager.OnVanillaPrefabsAvailable += CreateCustomPrefabs;
            Config.SaveOnConfigSet = true;
            
            config_mineDetectionRadius = Config.Bind("Config", "MineDetectionRadius", 400, new ConfigDescription("Maximum distance from the player that a mine can be to be detected"));
            config_skullDetectionRadius = Config.Bind("Config", "SkullDetectionRadius", 400, new ConfigDescription("Maximum distance from the player that a mine can be to be detected"));
            m_locRPC.Register("LocationQuery");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), harmonyInstanceId: PluginGUID);
        }

        public void CreateCustomPrefabs()
        {
            CreateMineFinder();
            CreateSkullFinder();
            PrefabManager.OnVanillaPrefabsAvailable -= CreateCustomPrefabs;

        }

        /*********************************** CREATE FINDERS **********************************************/
        /*********************************** CREATE FINDERS **********************************************/
        /*********************************** CREATE FINDERS **********************************************/
        public void CreateMineFinder()
        {
            SE_InfestedMineFinder finder = ScriptableObject.CreateInstance<SE_InfestedMineFinder>();
            finder.name = "InfestedMineFinderStatusEffect";
            finder.m_name = "$SE_InfestedMine";
            CustomStatusEffect finderCSE = new CustomStatusEffect(finder, false);
            ItemManager.Instance.AddStatusEffect(finderCSE);

            GameObject mineFinderPrefab = PrefabManager.Instance.CreateClonedPrefab("MineFinder", "StaminaUpgrade_Greydwarf");
            ItemDrop.ItemData mineFinderItemData = mineFinderPrefab.GetComponent<ItemDrop>().m_itemData;
            ItemDrop.ItemData.SharedData mineFinderShared = mineFinderItemData.m_shared;
            mineFinderShared.m_name = "Mine Finder";
            mineFinderShared.m_description = "Eat this to release your inner Seeker and sniff out the nearby mines";
            mineFinderShared.m_itemType = ItemDrop.ItemData.ItemType.Consumable;
            mineFinderShared.m_consumeStatusEffect = finderCSE.StatusEffect;
            mineFinderShared.m_questItem = false;

            // piece_cauldron
            // crafting recipe
            ItemConfig config = new ItemConfig();
            config.Apply(mineFinderPrefab);
            config.CraftingStation = CraftingStations.Cauldron;
            config.MinStationLevel = 5; // need the Mortar and Pestle
            config.AddRequirement("TrophySeeker", 1, 1);
            config.AddRequirement("BugMeat", 10, 1);

            CustomItem mineFinderCustomItem = new CustomItem(mineFinderPrefab, false, config); 
            ItemManager.Instance.AddItem(mineFinderCustomItem);
        }

        public void CreateSkullFinder()
        {
            SE_SkullFinder finder = ScriptableObject.CreateInstance<SE_SkullFinder>();
            finder.name = "SkullFinderStatusEffect";
            finder.m_name = "$SE_Skull";
            CustomStatusEffect finderCSE = new CustomStatusEffect(finder, false);
            ItemManager.Instance.AddStatusEffect(finderCSE);

            GameObject skullFinderPrefab = PrefabManager.Instance.CreateClonedPrefab("SkullFinder", "HealthUpgrade_GDKing");
            ItemDrop.ItemData skullFinderItemData = skullFinderPrefab.GetComponent<ItemDrop>().m_itemData;
            ItemDrop.ItemData.SharedData skullFinderShared = skullFinderItemData.m_shared;
            skullFinderShared.m_name = "Skull Finder";
            skullFinderShared.m_description = "Eat this chewy treat to sniff out the nearby Jotunn skulls";
            skullFinderShared.m_itemType = ItemDrop.ItemData.ItemType.Consumable;
            skullFinderShared.m_consumeStatusEffect = finderCSE.StatusEffect;
            skullFinderShared.m_questItem = false;

            // piece_cauldron
            // crafting recipe
            ItemConfig config = new ItemConfig();
            config.Apply(skullFinderPrefab);
            config.CraftingStation = CraftingStations.Cauldron;
            config.MinStationLevel = 5; // need the Mortar and Pestle
            config.AddRequirement("Bilebag", 1, 1);
            config.AddRequirement("GiantBloodSack", 5, 1);

            CustomItem skullFinderCustomItem = new CustomItem(skullFinderPrefab, false, config);
            ItemManager.Instance.AddItem(skullFinderCustomItem);
        }

        /*********************************** LOCATION SEEKING **********************************************/
        /*********************************** LOCATION SEEKING **********************************************/
        /*********************************** LOCATION SEEKING **********************************************/
        public void AddPins(Minimap.PinType pinType, string text, System.Collections.Generic.List<UnityEngine.Vector3> locations)
        {
            foreach(UnityEngine.Vector3 point in locations) 
            {
                Minimap.instance.AddPin(point, pinType, text, true, false);
            }
        }

        // RPC for the mine locations
        internal class LocationQueryRPC : RPC
        {
            internal enum LocationType
            {
                Unknown = 0,
                Mine = 1,
                Skull = 2
            }

            public LocationQueryRPC()
            {
            }

            public void RequestLocations(string id1, string id2, UnityEngine.Vector3 center)
            {
                Jotunn.Logger.LogInfo("RequestLocations");
                ZPackage package = new ZPackage();
                package.Write(id1);
                package.Write(id2);
                package.Write(center);
                SendQuery(package);
            }

            public LocationType GetLocationTypeFromID(string id)
            {
                if (id.Equals("Mistlands_DvergrTownEntrance1"))
                {
                    return LocationType.Mine;
                }
                if (id.Equals("Mistlands_DvergrTownEntrance2"))
                {
                    return LocationType.Mine;
                }
                if (id.Equals("Mistlands_Excavation1"))
                {
                    return LocationType.Skull;
                }
                if (id.Equals("Mistlands_Giant1"))
                {
                    return LocationType.Skull;
                }
                return LocationType.Unknown;
            }

            public string GetMapStringForLocationType(LocationType locationType)
            {
                // weird capitalizations to make it less likely users manually make a different colored pin by mistake
                if (locationType == LocationType.Mine)
                {
                    return "MinE";
                }
                if (locationType == LocationType.Skull)
                {
                    return "SkulL";
                }
                return "?";
            }

            public override void ReceiveQuery(long sender, ZPackage package)
            {
                string id1 = package.ReadString();
                string id2 = package.ReadString();
                UnityEngine.Vector3 center = package.ReadVector3();

                // whomever receives the query defines the range
                float range = -1.0f;
                if (GetLocationTypeFromID(id1) == LocationType.Mine)
                {
                    range = (float)MistFinders.Instance.config_mineDetectionRadius.Value;
                } 
                else if (GetLocationTypeFromID(id1) == LocationType.Skull)
                {
                    range = (float)MistFinders.Instance.config_skullDetectionRadius.Value;
                }

                Jotunn.Logger.LogInfo($"SendFinderPackage: {id1}, {id2}, {center.ToString()}, {range}");

                // find the relevant locations
                System.Collections.Generic.List<ZoneSystem.LocationInstance> locations = new System.Collections.Generic.List<ZoneSystem.LocationInstance>();
                System.Collections.Generic.List<ZoneSystem.LocationInstance> moreLocations = new System.Collections.Generic.List<ZoneSystem.LocationInstance>();
                ZoneSystem.instance.FindLocations(id1, ref locations);
                ZoneSystem.instance.FindLocations(id2, ref moreLocations);

                // combine them into one list
                locations.AddRange(moreLocations);

                Jotunn.Logger.LogInfo($"total locations found: {locations.Count}");
                System.Collections.Generic.List<UnityEngine.Vector3> locationsInRange = new System.Collections.Generic.List<UnityEngine.Vector3>();
                foreach (ZoneSystem.LocationInstance location in locations)
                {
                    if (center.DistanceTo(location.m_position) <= range)
                    {
                        locationsInRange.Add(location.m_position);
                    }
                }

                // send the first id, then the count, and then all the vectors. We don't need anything else.
                // including the id at the beginning removes ambiguity if we happen to end up with multiple calls close together
                ZPackage responsePackage = new ZPackage();
                responsePackage.Write(id1);
                responsePackage.Write(locationsInRange.Count);
                foreach (UnityEngine.Vector3 pos in locationsInRange)
                {
                    responsePackage.Write(pos);
                }

                SendResponse(responsePackage);
            }

            public override void ReceiveResponse(long sender, ZPackage package)
            {
                Jotunn.Logger.LogInfo("ProcessFinderPackage");

                string id1 = package.ReadString();
                int count = package.ReadInt();
                System.Collections.Generic.List<UnityEngine.Vector3> locationsInRange = new System.Collections.Generic.List<UnityEngine.Vector3>();
                for (int i = 0; i < count; i++)
                {
                    UnityEngine.Vector3 v = package.ReadVector3();
                    locationsInRange.Add(v);
                }
                Jotunn.Logger.LogInfo($"Response: {id1}, {count}");

                string text = GetMapStringForLocationType(GetLocationTypeFromID(id1));

                foreach (UnityEngine.Vector3 point in locationsInRange)
                {
                    Minimap.instance.AddPin(point, Minimap.PinType.Icon3, text, true, false);
                }

                // hide the inventory UI if it's up and show the map
                InventoryGui.instance.Hide();
                UnityEngine.Vector3 playerPos = Player.m_localPlayer.transform.position;
                Minimap.instance.ShowPointOnMap(playerPos);
            }
        }

    }
}

