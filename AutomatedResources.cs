using System.Collections.Generic;
using System;
using System.Reflection;
using System.Data;
using UnityEngine;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Plugins;


namespace Oxide.Plugins {
    [Info("AutomatedResources", "Mordenak", "0.0.1", ResourceId = 1014)]
    class AutomatedResources : RustPlugin {

        /*

        data = {
            player = {
                box1 = {
                    settings = something;
                }
                box2 = {
                    settings = something;
                }
            }
        }

        */

        Oxide.Plugins.Timer mainTimer = null;

        Dictionary<string, int> quarryStats = new Dictionary<string, int>();

        class ResourceSettings : RustPlugin {

            int resourceId = null;
            string resourceType = null;
            int resourceLevel = 1;

            public ResourceSettings(int id, string type) {
                resourceId = id;
                resourceType = type;
            }


        }

        Dictionary<string, ResourceSettings> globalData = new Dictionary<string, ResourceSettings>();


        [ChatCommand("setres")]
        void cmdCreateResource(BasePlayer player, string command, string[] args) {
            if (player.net.connection.authLevel == 0) return;

            var playerId = player.userID.ToString();

            var eyesAdjust = new Vector3(0f, 1.5f, 0f);
            var serverinput = typeof(BasePlayer).GetField("serverInput", (BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic));

            var input = serverinput.GetValue(player) as InputState;
            var currentRot = Quaternion.Euler(input.current.aimAngles) * Vector3.forward;
            var rayResult = FindBoxFromRay(player.transform.position + eyesAdjust, currentRot);
            if (rayResult is StorageContainer) {
                var box = rayResult as StorageContainer;
                if (box != null) {
                    Debug.Log(string.Format("Looking at box: {0}", box.net.ID) );
                    if (globalData.ContainsKey(playerId)) {
                        // existing player
                        Debug.Log("Apparently player data exists.");
                        Dictionary<string, string> boxSettings = new Dictionary<string, string>();
                        boxSettings.Add("type", "quarry");
                        globalData[playerId].Add(box.net.ID.ToString(), boxSettings);
                    }
                    else {
                        // player has no data, let's create it.
                        Dictionary<string, object> boxData = new Dictionary<string, object>();
                        Dictionary<string, string> boxSettings = new Dictionary<string, string>();
                        boxSettings.Add("type", "quarry");
                        boxData.Add(box.net.ID.ToString(), boxSettings);
                        globalData.Add(playerId, boxData);
                        SaveData();
                        Debug.Log("Saved data.");
                    }
                }
            }
        }

        void SaveData() {
            // Dictionary<string, object> globalData 
            Interface.GetMod().DataFileSystem.WriteObject<Dictionary<string, object>>("AutomatedResources_Data", globalData);
            //Interface.GetMod().DataFileSystem.SaveDatafile("PassiveResources_Data");
        }

        object FindBoxFromRay(Vector3 Pos, Vector3 Aim)
        {
            var hits = UnityEngine.Physics.RaycastAll(Pos, Aim);
            float distance = 1000f;
            object target = null;
            
            foreach (var hit in hits)
            {
                if (hit.collider.GetComponentInParent<StorageContainer>() != null)
                {
                    if (hit.distance < distance)
                    {
                        distance = hit.distance;
                        target = hit.collider.GetComponentInParent<StorageContainer>();
                    }
                }
                else if (hit.collider.GetComponentInParent<BasePlayer>() != null)
                {
                    if (hit.distance < distance)
                    {
                        distance = hit.distance;
                        target = hit.collider.GetComponentInParent<BasePlayer>();
                    }
                }
            }
            return target;
        }

        void ProcessResources() {
            foreach (BasePlayer player in BasePlayer.activePlayerList) {
                var playerId = player.userID.ToString();
                if (globalData.ContainsKey(playerId)) {
                    foreach (var box in globalData[playerId]) {
                        // problem here is no GetEnumerable for the ResourceSettings class... ick
                        Debug.Log(string.Format("Let's add some resources for: {0}", box.resourceId) );
                        if (box.resourceType == "quarry") {
                            foreach (var stat in quarryStats) {
                                var item = ItemManager.FindItemDefinition(stat.Key());
                                for (int i = 1; i < stat.Value(); i++) {
                                    box.inventory.Insert(item);
                                }
                            }
                        }
                        /*
                        var stone = ItemManager.FindItemDefinition("stones");
                        var wood = ItemManager.FindItemDefinition("wood");
                        var metal = ItemManager.FindItemDefinition("metal_ore");
                        //Debug.Log("Items created.");
                        player.inventory.GiveItem(ItemManager.CreateByItemID((int)wood.itemid, woodGain, false), (ItemContainer)player.inventory.containerMain );
                        player.inventory.GiveItem(ItemManager.CreateByItemID((int)stone.itemid, stoneGain, false), (ItemContainer)player.inventory.containerMain );
                        player.inventory.GiveItem(ItemManager.CreateByItemID((int)metal.itemid, metalGain, false), (ItemContainer)player.inventory.containerMain );
                        */
                    }
                }
            }
        }


        /*
        string description = Args[1];
        object error = GiveItem((BasePlayer)target, Args[1], amount, (ItemContainer)((BasePlayer)target).inventory.containerMain, out description);
        if (!(error is bool))
        {
            SendTheReply(source, error.ToString());
            return;
        }
        SendTheReply(source, string.Format("Gave {0} x {1} to {2}", description, amount.ToString(), ((BasePlayer)target).displayName.ToString()));
        */

        void Loaded() {
            startup();
        }

        void Unload() {
            teardown();
        }

        void startup() {
            quarryStats.Add("stones", 100);
            quarryStats.Add("metal_ore", 50);
            quarryStats.Add("sulfur_ore", 50);
            mainTimer = timer.Repeat(10, 0, delegate() { ProcessResources(); } );
        }

        void teardown() {
            mainTimer.Destroy();
        }


        /*
        [HookMethod("OnTick")]
        private void OnTick() {
            try {

            }
            catch (Exception ex) {
                PrintError("{0}: {1}", Title,"OnTick failed: " + ex.Message);
            }
        }
        */

    }

}
