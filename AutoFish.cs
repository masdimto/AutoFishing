using System;
using System.Collections.Generic;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using Microsoft.Xna.Framework;

namespace AutoFishingPlugin
{
    [ApiVersion(2, 1)]
    public class AutoFishing : TerrariaPlugin
    {
        public override string Name => "AutoFishing";
        public override Version Version => new Version(1, 0, 0);
        public override string Author => "CustomPlugin";
        public override string Description => "Otomatis menarik ikan dan melempar pancing kembali.";

        private HashSet<int> enabledPlayers = new HashSet<int>();
        private int tickCounter = 0;

        public AutoFishing(Main game) : base(game) { }

        public override void Initialize()
        {
            ServerApi.Hooks.GameUpdate.Register(this, OnGameUpdate);
            ServerApi.Hooks.ServerLeave.Register(this, OnServerLeave);
            Commands.ChatCommands.Add(new Command("autofish.use", ToggleAutoFish, "autofish", "af")
            {
                HelpText = "Menyalakan atau mematikan fitur auto fishing."
            });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.GameUpdate.Deregister(this, OnGameUpdate);
                ServerApi.Hooks.ServerLeave.Deregister(this, OnServerLeave);
            }
            base.Dispose(disposing);
        }

        private void OnServerLeave(LeaveEventArgs args)
        {
            enabledPlayers.Remove(args.Who);
        }

        private void ToggleAutoFish(CommandArgs args)
        {
            int index = args.Player.Index;
            if (enabledPlayers.Contains(index))
            {
                enabledPlayers.Remove(index);
                args.Player.SendInfoMessage("[AutoFish] Dimatikan.");
            }
            else
            {
                enabledPlayers.Add(index);
                args.Player.SendSuccessMessage("[AutoFish] Diaktifkan! Lempar pancingmu sekali ke air.");
            }
        }

        private void OnGameUpdate(EventArgs args)
        {
            tickCounter++;
            if (tickCounter < 15) return; // Cek setiap 0.25 detik agar ringan di server
            tickCounter = 0;

            foreach (int playerIndex in enabledPlayers)
            {
                TSPlayer tsPlayer = TShock.Players[playerIndex];
                if (tsPlayer == null || !tsPlayer.Active || tsPlayer.TPlayer.dead) continue;

                Player player = tsPlayer.TPlayer;

                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile proj = Main.projectile[i];
                    if (proj.active && proj.owner == playerIndex && proj.bobber)
                    {
                        // ai[1] < 0 menandakan ikan/item sudah menyambar pancing
                        if (proj.ai[1] < 0)
                        {
                            int caughtItemType = (int)Math.Abs(proj.ai[1]);
                            if (caughtItemType > 0)
                            {
                                tsPlayer.GiveItem(caughtItemType, 1);
                            }

                            // Reset status bobber agar otomatis lanjut memancing tanpa lempar ulang manual
                            proj.ai[1] = 0f;
                            proj.localAI[1] = 0f;
                            proj.netUpdate = true;
                        }
                    }
                }
            }
        }
    }
}
