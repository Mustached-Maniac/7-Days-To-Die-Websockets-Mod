using System.Text.RegularExpressions;
using Newtonsoft.Json;
using HarmonyLib;

namespace _7DTDWebsockets.patchs
{
    internal class RunTimePatch
    {
        public static bool IsHeadshot { get; set; } = false;

        public static void PatchAll()
        {
            Log.Out("[Websocket] Runtime patches initialized");
            Harmony harmony = new Harmony("com.gmail.kk964gaming.websockets.patch");
            harmony.PatchAll();

            foreach (var method in harmony.GetPatchedMethods())
            {
                Log.Out($"Successfully patched: {method.Name}");
            }
        }
    }

    public class Player
    {
        public string name;

        public Player(ClientInfo clientInfo)
        {
            this.name = clientInfo.playerName ?? "Unknown";
        }

        public Player(EntityPlayer player)
        {
            this.name = player.EntityName;
        }
    }

    class PlayerKillEntityEvent
    {
        public Player player;
        public string entity;
        public bool animal;
        public bool zombie;
        public string weaponType;
        public bool headshot;
        public PlayerKillEntityEvent(Player player, string entity, bool animal, bool zombie, string weaponType, bool headshot)
        {
            this.player = player;
            this.entity = entity;
            this.animal = animal;
            this.zombie = zombie;
            this.weaponType = weaponType;
            this.headshot = headshot;
        }
    }

    [HarmonyPatch(typeof(EntityAlive), "SetDead")]
    class PatchEntityDeath
    {
        static bool Prefix(EntityAlive __instance)
        {
            object obj = Traverse.Create(__instance).Field("entityThatKilledMe").GetValue();
            if (__instance is EntityPlayer) return true;
            if (obj == null) return true;
            EntityAlive whokilledMe = (EntityAlive)obj;
            if (whokilledMe == null) return true;
            if (!(whokilledMe is EntityPlayer)) return true;
            EntityPlayer player = whokilledMe as EntityPlayer;
            string ent = __instance.GetDebugName();

            bool animal = false;
            bool zombie = false;

            if (ent.ToLower().Contains("animal")) animal = true;
            if (ent.ToLower().Contains("zombie")) zombie = true;

            if (animal) ent = Regex.Replace(ent, "(animal)", "", RegexOptions.IgnoreCase);
            if (zombie) ent = Regex.Replace(ent, "(zombie)", "", RegexOptions.IgnoreCase);

            string weaponType = player.inventory.holdingItem.Name;
            bool headshot = RunTimePatch.IsHeadshot;

            _7DTDWebsockets.API.Send("PlayerKillEntity", JsonConvert.SerializeObject(new PlayerKillEntityEvent(new Player(player), ent, animal, zombie, weaponType, headshot)));

            return true;
        }
    }

    [HarmonyPatch(typeof(EntityAlive), "damageEntityLocal")]
    class PatchDamageEntityLocal
    {
        static void Postfix(DamageResponse __result)
        {
            RunTimePatch.IsHeadshot = __result.HitBodyPart == EnumBodyPartHit.Head;
        }
    }
}
