using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using ReLogic.Graphics;
using System;
using System.Text.RegularExpressions;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace Silicate.Common.UI;

public sealed class UITileDisplay : UIState
{
    private static readonly Regex Pattern = new("(\\B[A-Z0-9])", RegexOptions.Compiled);

    private Player Player => Main.LocalPlayer;

    public UIImage Panel { get; private set; }

    public UIImage Progress { get; private set; }

    public UIImageFramed Tile { get; private set; }

    public UIText Name { get; private set; }
    public UIText Mod { get; private set; }

    public float Opacity { get; private set; }

    public override void OnInitialize()
    {
        base.OnInitialize();

        // TODO: Central UIElement for the full area of the state.
        // Will also avoid progress bar being cut out from the overflow.
        
        Panel = new UIImage(Silicate.Instance.Assets.Request<Texture2D>("Assets/Textures/UI/Panel"))
        {
            OverflowHidden = true,
            ScaleToFit = true,
            HAlign = 0.5f,
            Top = StyleDimension.FromPixels(24f),
            Height = StyleDimension.FromPixels(48f),
            OverrideSamplerState = SamplerState.PointClamp
        };

        Append(Panel);

        Tile = new UIImageFramed(TextureAssets.Tile[0], new Rectangle(9 * 18, 3 * 18, 16, 16))
        {
            MarginLeft = 8f,
            VAlign = 0.5f,
            OverrideSamplerState = SamplerState.PointClamp
        };

        Panel.Append(Tile);

        Name = new UIText(string.Empty, 0.8f)
        {
            MarginTop = 8f,
            MarginLeft = 32f,
            OverrideSamplerState = SamplerState.PointClamp
        };

        Panel.Append(Name);

        Mod = new UIText(string.Empty, 0.6f)
        {
            MarginBottom = 8f,
            MarginLeft = 32f,
            VAlign = 1f,
            OverrideSamplerState = SamplerState.PointClamp
        };

        Panel.Append(Mod);

        Progress = new UIImage(Silicate.Instance.Assets.Request<Texture2D>("Assets/Textures/UI/Progress"))
        {
            ScaleToFit = true,
            VAlign = 1f,
            Left = StyleDimension.FromPixels(-2f),
            Height = StyleDimension.FromPixels(2f),
            OverrideSamplerState = SamplerState.PointClamp
        };

        Panel.Append(Progress);
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        UpdateColors();

        UpdatePanel();
        UpdateProgress();
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        base.Draw(spriteBatch);

        spriteBatch.Draw(
            Silicate.Instance.Assets.Request<Texture2D>("Assets/Textures/UI/Panel_Left").Value,
            Panel.GetDimensions().Position() - new Vector2(4f, 0f),
            Color.White * Opacity
        );

        spriteBatch.Draw(
            Silicate.Instance.Assets.Request<Texture2D>("Assets/Textures/UI/Panel_Right").Value,
            Panel.GetDimensions().Position() + new Vector2(Panel.Width.Pixels, 0f),
            Color.White * Opacity
        );
    }

    private void UpdateColors()
    {
        Panel.Color = Color.White * Opacity;

        Tile.Color = Color.White * Opacity;

        Name.TextColor = Color.White * Opacity;
        Mod.TextColor = new Color(88, 88, 173) * Opacity;

        Progress.Color = Color.Lerp(Color.Black, Color.White, Progress.Width.Pixels / (Panel.Width.Pixels + 2f)) * Opacity;
    }

    private void UpdatePanel()
    {
        Tile tile = Framing.GetTileSafely(Player.tileTargetX, Player.tileTargetY);

        if (tile.HasTile && TileID.Search.TryGetName(tile.TileType, out string? name))
        {
            DynamicSpriteFont? font = FontAssets.MouseText.Value;

            string tileName = Pattern.Replace(name, " $1");
            Vector2 tileNameSize = font.MeasureString(tileName);

            Name.SetText(tileName);

            Mod? mod = TileLoader.GetTile(tile.TileType)?.Mod;
            string? modName = mod == null ? "Terraria" : mod.DisplayName;
            Vector2 modNameSize = font.MeasureString(modName);

            Mod.SetText(modName);

            Asset<Texture2D>? texture = TextureAssets.Tile[tile.TileType];
            Rectangle frame = new Rectangle(9 * 18, 3 * 18, 16, 16);

            if (Main.tileFrameImportant[tile.TileType])
            {
                frame = new Rectangle(tile.TileFrameX, tile.TileFrameY, 16, 16);
            }

            Tile.SetImage(texture, frame);

            float desiredWidth = MathF.Max(tileNameSize.X, modNameSize.X);
            float width = MathHelper.Lerp(Panel.Width.Pixels, desiredWidth, 0.2f) + frame.Width;

            Panel.Width.Set(MathF.Ceiling(width), 0f);

            Recalculate();

            Opacity = MathHelper.Lerp(Opacity, 1f, 0.2f);
        }
        else
        {
            Asset<Texture2D>? texture = Silicate.Instance.Assets.Request<Texture2D>("Assets/Textures/UI/Unknown");

            Tile.SetImage(texture, texture.Frame());

            Name.SetText(string.Empty);
            Mod.SetText(string.Empty);

            Opacity = MathHelper.Lerp(Opacity, 0f, 0.2f);
        }
    }

    private void UpdateProgress()
    {
        float progress = 0f;
        int index = Player.hitTile.TryFinding(Player.tileTargetX, Player.tileTargetY, 1);

        if (index != -1)
        {
            HitTile.HitTileObject? data = Player.hitTile.data[index];

            if (data == null)
            {
                return;
            }

            progress = (Panel.Width.Pixels + 2f) * data.damage / 100f;
        }

        Progress.Width.Set(MathHelper.Lerp(Progress.Width.Pixels, progress, 0.2f), 0f);
    }
}