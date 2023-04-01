using Discord;
using Discord.Rest;
using Discord.WebSocket;
using DontSayAnythingGuys.Utils;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityModManagerNet;

namespace DontSayAnythingGuys
{
    public static class Main
    {
        public static UnityModManager.ModEntry ModEntry { get; private set; }
        public static UnityModManager.ModEntry.ModLogger Logger { get; private set; }
        public static Harmony Harmony { get; private set; }
        public static Settings Settings { get; private set; }
        public static DiscordSocketClient Client { get; private set; }
        public static List<ulong> VoiceChannels { get; } = new List<ulong>();
        public static Dictionary<SystemLanguage, Dictionary<string, string>> Localizations { get; } = new Dictionary<SystemLanguage, Dictionary<string, string>>()
        {
            {
                SystemLanguage.Korean, new Dictionary<string, string> ()
                {
                    { "gui.help", "도움말" },
                    { "gui.helpLink", "https://tjwogud.notion.site/e839e26fa98b4fa08705c8c13b5de3d6" },

                    { "gui.bot", "봇: " },
                    { "gui.token", "토큰: " },
                    { "gui.channelId", "음성 채널 ID: " },
                    { "gui.userId", "유저 ID: " },
                    { "gui.name", "이름: " },
                    { "gui.id", "ID: " },
                    { "gui.search", "검색: " },
                    { "gui.channel", "음성 채널: " },
                    { "gui.tile", "타일 번호: " },

                    { "gui.none", "없음" },

                    { "gui.run", "봇 실행" },
                    { "gui.turnoff", "봇 종료" },

                    { "gui.connect", "연결" },
                    { "gui.unconnect", "연결 끊기" },

                    { "gui.connected", "연결됨" },
                    { "gui.connecting", "연결중..." },
                    { "gui.disconnected", "연결되지 않음" },
                    { "gui.disconnecting", "연결 끊는중..." },

                    { "gui.change", "변경" },
                    { "gui.select", "선택" },
                    { "gui.ok", "확인" },
                    { "gui.cancel", "취소" },

                    { "gui.runOnLaunch", "얼불춤 실행 시 봇 실행" },
                    { "gui.unmuteOnEnd", "죽거나 레벨 종료 시 음소거 끄기" }
                }
            },
            {
                SystemLanguage.English, new Dictionary<string, string> ()
                {
                    { "gui.help", "Help" },
                    { "gui.helpLink", "https://tjwogud.notion.site/0a2de4e127f944df996fb52ca8b24e05" },

                    { "gui.bot", "Bot: " },
                    { "gui.token", "Token: " },
                    { "gui.channelId", "Voice Channel ID: " },
                    { "gui.userId", "User ID: " },
                    { "gui.name", "Name: " },
                    { "gui.id", "ID: " },
                    { "gui.search", "Search: " },
                    { "gui.channel", "Voice Channel: " },
                    { "gui.tile", "Tile num: " },

                    { "gui.none", "none" },

                    { "gui.run", "Run Bot" },
                    { "gui.turnoff", "Turn off Bot" },

                    { "gui.connect", "Connect" },
                    { "gui.unconnect", "Unconnect" },

                    { "gui.connected", "Connected" },
                    { "gui.connecting", "Connecting..." },
                    { "gui.disconnected", "Not connected" },
                    { "gui.disconnecting", "Disconnecting..." },

                    { "gui.change", "Change" },
                    { "gui.select", "Select" },
                    { "gui.ok", "Ok" },
                    { "gui.cancel", "Cancel" },

                    { "gui.runOnLaunch", "Run bot on launch" },
                    { "gui.unmuteOnEnd", "Unmute user on death or level end" }
                }
            }
        };
        public static Dictionary<string, string> Localization => Localizations.TryGetValue(RDString.language, out var value) ? value : Localizations[SystemLanguage.English];

        public static void Setup(UnityModManager.ModEntry modEntry)
        {
            ModEntry = modEntry;
            Logger = ModEntry.Logger;
            Harmony = new Harmony(ModEntry.Info.Id);
            Settings = UnityModManager.ModSettings.Load<Settings>(ModEntry);

            ModEntry.OnToggle = OnToggle;
            ModEntry.OnGUI = OnGUI;
            ModEntry.OnSaveGUI = OnSaveGUI;

            Application.quitting += () => DestroyClient();
            SceneManager.activeSceneChanged += (_, __) => { if (Settings.unmuteOnEnd) UnmuteUser(); };
        }

        public static bool OnToggle(UnityModManager.ModEntry modEntry, bool active)
        {
            if (active)
            {
                Harmony.PatchAll(Assembly.GetExecutingAssembly());
                if (Settings.runOnLaunch)
                {
                    CreateClient();
                }
            }
            else
            {
                Harmony.UnpatchAll(ModEntry.Info.Id);
                DestroyClient();
            }
            return true;
        }

        private static string prevClientState = "disconnected";
        private static string prevVCState = "disconnected";
        private static string realVCState = "disconnected";

        private static List<(string name, WebTexture icon, List<(string name, string id)> vc)> channelCaches = new List<(string, WebTexture, List<(string, string)>)>();
        private static List<(string name, string id, WebTexture icon)> userCaches = new List<(string, string, WebTexture)>();

        private static bool changing_channel = false;
        private static bool selecting_channel = false;
        private static bool changing_user = false;
        private static bool selecting_user = false;

        private static string search_channel = "";
        private static string search_user = "";

        private static string error_client = "";

        private static Texture2D noneTexture = Texture2D.grayTexture.Copy().FixedResize(45, 45);

        public static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            string clientState, vcState;
            if (Event.current.type != EventType.Layout)
            {
                clientState = prevClientState;
                vcState = prevVCState;
            }
            else
            {
                clientState = Client == null ? "disconnected" : Client.ConnectionState.ToString().ToLower();
                vcState = realVCState;
            }

            prevClientState = clientState;
            prevVCState = vcState;

            if (GUILayout.Button(Localization["gui.help"], GUILayout.Width(75)))
            {
                Application.OpenURL(Localization["gui.helpLink"]);
            }

            GUILayout.Space(30);

            GUILayout.Label(Localization["gui.bot"] + Localization["gui." + clientState]);

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localization["gui.token"]);
            if (clientState == "disconnected")
            {
                if (Settings.showToken)
                {
                    Settings.token = GUILayout.TextField(Settings.token);
                }
                else
                {
                    Settings.token = GUILayout.PasswordField(Settings.token ?? "", '●');
                }
            }
            else
            {
                if (Settings.showToken)
                {
                    GUILayout.Label(Settings.token);
                }
                else
                {
                    GUILayout.Label(string.Concat(Enumerable.Repeat("●", Settings.token?.Length ?? 0)));
                }
            }
            Settings.showToken = GUILayout.Toggle(Settings.showToken, "");
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (clientState == "disconnected" && GUILayout.Button(Localization["gui.run"], GUILayout.Width(100)))
            {
                CreateClient();
            }
            else if (clientState == "connected" && GUILayout.Button(Localization["gui.turnoff"], GUILayout.Width(100)))
            {
                DestroyClient();
                selecting_channel = false;
            }
            if (clientState == "disconnected")
            {
                GUILayout.Label(error_client);
            }
            GUILayout.EndHorizontal();
            Settings.runOnLaunch = GUILayout.Toggle(Settings.runOnLaunch, (Settings.runOnLaunch ? "☑ " : "☐ ") + Localization["gui.runOnLaunch"], GUI.skin.label);

            GUILayout.Space(30);

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localization["gui.channelId"]);
            if (changing_channel)
            {
                string changed = GUILayout.TextField(Settings.channelId);
                if (changed == null)
                    Settings.channelId = "";
                else if (changed.All(c => char.IsNumber(c)))
                    Settings.channelId = changed;
                GUILayout.Space(5);
                if (GUILayout.Button(Localization["gui.ok"], GUILayout.Width(75)))
                    changing_channel = false;
            }
            else
            {
                GUILayout.Label(!Settings.channelId.IsNullOrEmpty() ? Settings.channelId : Localization["gui.none"]);
                if (vcState == "disconnected")
                {
                    if (!selecting_channel)
                    {
                        GUILayout.Space(5);
                        if (GUILayout.Button(Localization["gui.change"], GUILayout.Width(75)))
                            changing_channel = true;
                        GUILayout.Space(5);
                        if (clientState == "connected" && GUILayout.Button(Localization["gui.select"], GUILayout.Width(75)))
                        {
                            selecting_channel = true;
                            channelCaches.Clear();

                            channelCaches.AddRange(Client.Guilds.Select(g => (g.Name, new WebTexture(g.IconUrl, t => t.Readable().FixedResize(45, 45)), g.VoiceChannels.Select(vc => (vc.Name, vc.Id.ToString())).ToList())));
                        }
                    }
                    else
                    {
                        GUILayout.Space(5);
                        if (GUILayout.Button(Localization["gui.cancel"], GUILayout.Width(75)))
                            selecting_channel = false;
                    }
                }
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            if (selecting_channel)
            {
                GUILayout.Space(5);
                GUILayout.BeginHorizontal(GUI.skin.box, GUILayout.Width(300));
                GUILayout.Space(5);
                GUILayout.BeginVertical();
                GUILayout.Space(5);
                GUILayout.BeginHorizontal();
                GUILayout.Label(Localization["gui.search"]);
                search_channel = GUILayout.TextField(search_channel);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                GUILayout.Space(10);
                foreach ((string gName, WebTexture icon, List<(string name, string id)> vc) in channelCaches)
                {
                    foreach ((string cName, string id) in vc)
                    {
                        if (!search_channel.Trim().IsNullOrEmpty() && !cName.ToLower().Replace(" ", "").Contains(search_channel.ToLower().Replace(" ", "")))
                            continue;
                        GUILayout.BeginHorizontal(GUI.skin.box, GUILayout.Height(50));
                        GUILayout.Space(2.5f);
                        GUILayout.BeginVertical();
                        GUILayout.Space(2.5f);
                        if (icon.Loaded && !icon.Failed)
                        {
                            GUILayout.Label(icon.Texture, GUILayout.Width(45), GUILayout.Height(45));
                        }
                        else
                        {
                            GUILayout.Label(noneTexture, GUILayout.Width(45), GUILayout.Height(45));
                            TextAnchor prev = GUI.skin.label.alignment;
                            GUI.skin.label.alignment = TextAnchor.MiddleLeft;
                            GUI.Label(GUILayoutUtility.GetLastRect(), gName);
                            GUI.skin.label.alignment = prev;
                        }
                        GUILayout.EndVertical();
                        GUILayout.Space(10);
                        GUILayout.BeginVertical(GUILayout.Width(215));
                        GUILayout.Space(2.5f);
                        GUILayout.Label(Localization["gui.name"] + cName, GUILayout.Height(15));
                        GUILayout.Space(5);
                        GUILayout.Label(Localization["gui.id"] + id, GUILayout.Height(15));
                        GUILayout.Space(2.5f);
                        GUILayout.EndVertical();
                        GUILayout.BeginVertical();
                        GUILayout.Space(10);
                        if (GUILayout.Button(Localization["gui.select"], GUILayout.Width(75), GUILayout.Height(30)))
                        {
                            Settings.channelId = id;
                            selecting_channel = false;
                        }
                        GUILayout.EndVertical();
                        GUILayout.EndHorizontal();
                    }
                }
                GUILayout.Space(5);
                GUILayout.EndVertical();
                GUILayout.Space(5);
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            GUILayout.Label(Localization["gui.userId"]);
            if (changing_user)
            {
                string changed = GUILayout.TextField(Settings.userId);
                if (changed == null)
                    Settings.userId = "";
                else if (changed.All(c => char.IsNumber(c)))
                    Settings.userId = changed;
                GUILayout.Space(5);
                if (GUILayout.Button(Localization["gui.ok"], GUILayout.Width(75)))
                    changing_user = false;
            }
            else
            {
                GUILayout.Label(!Settings.userId.IsNullOrEmpty() ? Settings.userId : Localization["gui.none"]);
                if (!selecting_user)
                {
                    GUILayout.Space(5);
                    if (GUILayout.Button(Localization["gui.change"], GUILayout.Width(75)))
                        changing_user = true;
                    GUILayout.Space(5);
                    if (clientState == "connected" && GUILayout.Button(Localization["gui.select"], GUILayout.Width(75)))
                    {
                        selecting_user = true;
                        userCaches.Clear();

                        userCaches.AddRange(Client.Guilds.SelectMany(g => { g.DownloadUsersAsync().Wait(); return g.Users; }).GroupBy(u => u.Id).Select(g => g.First()).Where(u => !u.IsBot && !u.IsWebhook).Select(u => (u.Username, u.Id.ToString(), new WebTexture(u.GetAvatarUrl(size: 64) ?? u.GetDefaultAvatarUrl(), t => t.Readable().FixedResize(45, 45)))));
                    }
                }
                else
                {
                    GUILayout.Space(5);
                    if (GUILayout.Button(Localization["gui.cancel"], GUILayout.Width(75)))
                        selecting_user = false;
                }
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            if (selecting_user)
            {
                GUILayout.Space(5);
                GUILayout.BeginHorizontal(GUI.skin.box, GUILayout.Width(300));
                GUILayout.Space(5);
                GUILayout.BeginVertical();
                GUILayout.Space(5);
                GUILayout.BeginHorizontal();
                GUILayout.Label(Localization["gui.search"]);
                search_user = GUILayout.TextField(search_user);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                GUILayout.Space(10);
                foreach ((string name, string id, WebTexture icon) in userCaches)
                {
                    if (!search_user.Trim().IsNullOrEmpty() && !name.ToLower().Replace(" ", "").Contains(search_user.ToLower().Replace(" ", "")))
                        continue;

                    GUILayout.BeginHorizontal(GUI.skin.box, GUILayout.Height(50));
                    GUILayout.Space(2.5f);
                    GUILayout.BeginVertical();
                    GUILayout.Space(2.5f);
                    if (icon.Loaded && !icon.Failed)
                    {
                        GUILayout.Label(icon.Texture, GUILayout.Width(45), GUILayout.Height(45));
                    }
                    else
                    {
                        GUILayout.Label(noneTexture, GUILayout.Width(45), GUILayout.Height(45));
                    }
                    GUILayout.EndVertical();
                    GUILayout.Space(10);
                    GUILayout.BeginVertical(GUILayout.Width(215));
                    GUILayout.Space(2.5f);
                    GUILayout.Label(Localization["gui.name"] + name, GUILayout.Height(15));
                    GUILayout.Space(5);
                    GUILayout.Label(Localization["gui.id"] + id, GUILayout.Height(15));
                    GUILayout.Space(2.5f);
                    GUILayout.EndVertical();
                    GUILayout.BeginVertical();
                    GUILayout.Space(10);
                    if (GUILayout.Button(Localization["gui.select"], GUILayout.Width(75), GUILayout.Height(30)))
                    {
                        Settings.userId = id;
                        selecting_user = false;
                    }
                    GUILayout.EndVertical();
                    GUILayout.EndHorizontal();
                }
                GUILayout.Space(5);
                GUILayout.EndVertical();
                GUILayout.Space(5);
                GUILayout.EndHorizontal();
                GUILayout.Space(5);
            }

            GUILayout.Space(30);

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localization["gui.tile"]);
            Settings.tile = int.TryParse(GUILayout.TextField(Settings.tile.ToString()), out int i) && i >= 0 ? i : Settings.tile;
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            Settings.unmuteOnEnd = GUILayout.Toggle(Settings.unmuteOnEnd, (Settings.unmuteOnEnd ? "☑ " : "☐ ") + Localization["gui.unmuteOnEnd"], GUI.skin.label);
        }

        public static void OnSaveGUI(UnityModManager.ModEntry modEntry)
        {
            Settings.Save(modEntry);
        }

        public static void MuteUser()
        {
            try
            {
                ((SocketVoiceChannel)Client.GetChannel(ulong.Parse(Settings.channelId))).Users.First(user => user.Id.ToString() == Settings.userId).ModifyAsync(p => p.Deaf = true);
            } catch
            {
            }
        }

        public static void UnmuteUser()
        {
            try
            {
                ((SocketVoiceChannel)Client.GetChannel(ulong.Parse(Settings.channelId))).Users.First(user => user.Id.ToString() == Settings.userId).ModifyAsync(p => p.Deaf = false);
            }
            catch
            {
            }
        }

        public static void CreateClient()
        {
            if (Client != null || Settings.token.IsNullOrEmpty())
                return;

            try
            {
                Client = new DiscordSocketClient(new DiscordSocketConfig() { GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.GuildMembers });

                Client.LoginAsync(TokenType.Bot, Settings.token, true).Wait();
                Client.StartAsync();

                Client.Disconnected += e =>
                {
                    Client = null;
                    error_client = e.GetType().Name + ": " + e.Message;
                    return Task.CompletedTask;
                };

                error_client = "";
            }
            catch (Exception e)
            {
                Client = null;
                error_client = e.GetType().Name + ": " + e.Message;
                Logger.Error(e.StackTrace);
            }
        }

        public static void DestroyClient()
        {
            if (Client == null)
                return;

            async void DestroyClientAsync()
            {
                Client.Get("_disconnectedEvent").Set("_subscriptions", ImmutableArray.Create<Func<Exception, Task>>());
                await Client.StopAsync();

                error_client = "";

                Client = null;
            }

            DestroyClientAsync();
        }
    }
}
