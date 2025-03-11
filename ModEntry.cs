using System;
using System.Collections.Generic;
using Force.DeepCloner;
using Microsoft.Xna.Framework;
using GenericModConfigMenu;
using HappyHunting;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Projectiles;

namespace TestProjectNPCChecker
{
    /// <summary>The mod entry point.</summary>
    internal sealed class ModEntry : Mod
    {
        private static IModHelper helper;
        private static ModConfig config;

        /*********
        ** Public methods
        *********/
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            ModEntry.helper = helper;
            ModEntry.config = helper.ReadConfig<ModConfig>();
            I18n.Init(helper.Translation);

            //we want to add the event handler methods to a specific location, and there are two ways to get into a location: load a save, and warp to it
            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            helper.Events.Player.Warped += OnWarped;

            //this is for the mod config menu initialization
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
        }

        /*********
        ** Private methods
        *********/
        private static void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
        {
            //if we're loading a save and there are already arrows flying (or if we're going to fire arrows here)
            Game1.currentLocation.projectiles.OnValueAdded += OnProjectileAdded;
        }

        private static void OnWarped(object? sender, WarpedEventArgs e)
        {
            //after warping out we politely remove the method from the previous location
            if (e.OldLocation != null)
                e.OldLocation.projectiles.OnValueAdded -= OnProjectileAdded;

            //after warping in we add the method
            if (e.NewLocation != null)
            {
                //first, if there are already arrows in flight somehow, let's cover those
                foreach (Projectile projectile in e.NewLocation.projectiles)
                {
                    OnProjectileAdded(projectile);
                }

                //second, let's add the handler for all future arrows
                e.NewLocation.projectiles.OnValueAdded += OnProjectileAdded;
            }
        }

        private static void OnProjectileAdded(Projectile value)
        {
            if (!config.On) return;
            //we want to make the arrow not collide with terrain specifically if it's a projectile intended to help us against monsters
            if (value.damagesMonsters.Get()) value.IgnoreLocationCollision = true;
        }

        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            // get Generic Mod Config Menu's API (if it's installed)
            var configMenu = this.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu is null)
                return;

            // register mod
            configMenu.Register(
                mod: this.ModManifest,
                reset: () => config = new ModConfig(),
                save: () => this.Helper.WriteConfig(config)
            );

            // add some config options
            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => I18n.On_Name(),
                tooltip: () => I18n.On_Description(),
                getValue: () => config.On,
                setValue: value => config.On = value
            );
        }

    }
}