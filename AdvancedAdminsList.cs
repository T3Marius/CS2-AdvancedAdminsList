using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Utils;
using Serilog;
using System.Numerics;

namespace AdminsList
{
    public class PluginConfig : BasePluginConfig
    {
        [JsonPropertyName("UseCenterHtmlMenu")]
        public bool UseCenterHtmlMenu { get; set; } = true;

        [JsonPropertyName("ImmunityFlag")]
        public string ImmunityFlag { get; set; } = "@css/root";

        [JsonPropertyName("ShowYourSelf")]
        public bool ShowYourSelf { get; set; } = true;

        [JsonPropertyName("ShowFlag")]
        public string ShowFlag { get; set; } = "@css/generic";

        [JsonPropertyName("ShowAdminGroups")]
        public bool ShowAdminGroups { get; set; } = true;

        [JsonPropertyName("AdminGroupsFilePath")]
        public string AdminGroupsFilePath { get; set; } = "/home/container/game/csgo/addons/counterstrikesharp/configs/admin_groups.json";

        [JsonPropertyName("Commands")]
        public Config_Commands Commands { get; set; } = new Config_Commands();

        public class Config_Commands
        {
            [JsonPropertyName("Adminslist")]
            public string[] Adminslist { get; set; } = { "adminslist" };
        }
    }

    public class AdminsList : BasePlugin, IPluginConfig<PluginConfig>
    {
        public override string ModuleName => "AdvancedAdminsList";
        public override string ModuleVersion => "1.0";
        public override string ModuleAuthor => "T3Marius";
        public PluginConfig Config { get; set; } = new PluginConfig();
        public static AdminsList Instance { get; set; } = new ();

        public override void Load(bool hotReload)
        {
            Instance = this;
            AdminGroupManager.LoadAdminGroups(Config.AdminGroupsFilePath);
            Commands.Load();
        }

        public void OnConfigParsed(PluginConfig config)
        {
            Config = config;
            AdminGroupManager.LoadAdminGroups(config.AdminGroupsFilePath);
        }

        public static class Commands
        {
            public static void Load()
            {


                PluginConfig config = Instance.Config;

                var commands = new Dictionary<IEnumerable<string>, (string description, CommandInfo.CommandCallback handler)>
                {
                    { config.Commands.Adminslist, ("Shows list of admins online", Command_Adminslist) }
                };

                foreach (var commandPair in commands)
                {
                    foreach (var command in commandPair.Key)
                    {
                        Instance.AddCommand($"css_{command}", commandPair.Value.description, commandPair.Value.handler);
                    }
                }
            }

            public static void Command_Adminslist(CCSPlayerController? player, CommandInfo command)
            {
                if (player == null) return;

                var instance = AdminsList.Instance;
                var config = instance.Config;

                var admins = instance.GetAllPlayers()
                    .Where(p => p.IsValid && !p.IsBot && AdminManager.PlayerHasPermissions(p, config.ShowFlag) && !AdminManager.PlayerHasPermissions(p, config.ImmunityFlag))
                    .ToList();

                if (config.UseCenterHtmlMenu)
                {
                    AdminMenu.DisplayAdmins(player, admins, config.ShowYourSelf, config.ShowAdminGroups);
                }
                else
                {
                    instance.DisplayAdminsInChat(player, admins, config.ShowYourSelf, config.ShowAdminGroups);
                }
            }
        }

        private IEnumerable<CCSPlayerController> GetAllPlayers()
        {
            return Utilities.GetPlayers().Where(p => p.IsValid).ToList();
        }

        private void DisplayAdminsInChat(CCSPlayerController player, List<CCSPlayerController> admins, bool showSelf, bool showGroups)
        {
            player.PrintToChat($"{ChatColors.Red}Admins Online:");
            player.PrintToChat($"{ChatColors.Red}------------------------");

            int adminNumber = 1;
            foreach (var admin in admins)
            {
                if (admin == player && !showSelf)
                {
                    continue;
                }

                string adminName = admin.PlayerName;
                string adminGroup = showGroups ? AdminGroupManager.GetAdminGroupName(admin) : string.Empty;
                player.PrintToChat($"[{adminNumber}] {ChatColors.LightRed}{adminName}{(showGroups ? $" - {adminGroup}" : string.Empty)}");
                adminNumber++;
            }

            player.PrintToChat($"{ChatColors.Red}------------------------");

            if (adminNumber == 1)
            {
                player.PrintToChat($"{ChatColors.Red}At the moment there are no admins on the server.");
            }
        }
    }

    public static class AdminMenu
    {
        public static void AddMenuOption(CCSPlayerController player, CenterHtmlMenu menu, Action<CCSPlayerController, ChatMenuOption> onSelect, string display, params object[] args)
        {
            using (new WithTemporaryCulture(player.GetLanguage()))
            {
                StringBuilder builder = new StringBuilder();
                builder.AppendFormat(AdminsList.Instance.Localizer[display, args]);

                menu.AddMenuOption(builder.ToString(), onSelect);
            }
        }

        public static void DisplayAdmins(CCSPlayerController player, List<CCSPlayerController> admins, bool showSelf, bool showGroups)
        {
            using (new WithTemporaryCulture(player.GetLanguage()))
            {
                StringBuilder builder = new StringBuilder();
                builder.AppendFormat(AdminsList.Instance.Localizer["AdminsList<title>"]);

                CenterHtmlMenu menu = new(builder.ToString(), AdminsList.Instance);

                int adminNumber = 1;
                foreach (var admin in admins)
                {
                    if (admin == player && !showSelf)
                    {
                        continue;
                    }

                    StringBuilder adminBuilder = new StringBuilder();
                    string adminName = admin.PlayerName;
                    string adminGroup = showGroups ? AdminGroupManager.GetAdminGroupName(admin) : string.Empty;
                    adminBuilder.AppendFormat(AdminsList.Instance.Localizer["menu_adminslist<admin>", $"{adminName}{(showGroups ? $" - {adminGroup}" : string.Empty)}"]);

                    menu.AddMenuOption(adminBuilder.ToString(), (CCSPlayerController _, ChatMenuOption _) => { });

                    adminNumber++;
                }

                MenuManager.OpenCenterHtmlMenu(AdminsList.Instance, player, menu);
            }
        }
    }

    public class AdminGroup
    {
        [JsonPropertyName("flags")]
        public List<string>? Flags { get; set; }

        [JsonPropertyName("immunity")]
        public int Immunity { get; set; }
    }

    public static class AdminGroupManager
    {
        public static Dictionary<string, AdminGroup>? AdminGroups { get; private set; }

        public static void LoadAdminGroups(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Admin groups file not found: {filePath}");
            }

            string json = File.ReadAllText(filePath);
            AdminGroups = JsonSerializer.Deserialize<Dictionary<string, AdminGroup>>(json);
        }

        public static string GetAdminGroupName(CCSPlayerController player)
        {
            if (AdminGroups == null)
            {
                return "No Group";
            }

            foreach (var group in AdminGroups)
            {
                if (group.Value.Flags != null && group.Value.Flags.All(flag => AdminManager.PlayerHasPermissions(player, flag)))
                {
                    return group.Key;
                }
            }
            return "No Group";
        }
    }
}
