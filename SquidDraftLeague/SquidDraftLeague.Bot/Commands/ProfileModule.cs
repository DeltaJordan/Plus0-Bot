﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using NLog;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.Primitives;
using SixLabors.Shapes;
using SquidDraftLeague.Bot.AirTable;
using SquidDraftLeague.Bot.Queuing.Data;
using SquidDraftLeague.Language.Resources;
using Image = SixLabors.ImageSharp.Image;
using Path = System.IO.Path;

namespace SquidDraftLeague.Bot.Commands
{
    [Name("Profile")]
    public class ProfileModule : InteractiveBase
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        [Command("profile")]
        public async Task Profile([Remainder] IUser user = null)
        {
            try
            {
                if (user == null)
                {
                    user = this.Context.User;
                }

                SdlPlayer player;
                try
                {
                    player = await AirTableClient.RetrieveSdlPlayer((IGuildUser)user);
                }
                catch (Exception e)
                {
                    Logger.Warn(e);

                    if (e is SdlAirTableException airTableException)
                        await airTableException.OutputToDiscordUser(this.Context);

                    throw;
                }

                IUserMessage message = await this.ReplyAsync("Please wait, profiles take a little bit to put together.");

                Font powerFont = Globals.KarlaFontFamily.CreateFont(160, FontStyle.Bold);
                Font nameFont = Globals.KarlaFontFamily.CreateFont(80, FontStyle.Bold);
                Font winRateFont = Globals.KarlaFontFamily.CreateFont(100, FontStyle.Bold);
                Font switchCodeFont = Globals.KarlaItalicFontFamily.CreateFont(50, FontStyle.Italic);
                Font classFont = Globals.KarlaFontFamily.CreateFont(180, FontStyle.Bold);
                Font classNameFont = Globals.KarlaFontFamily.CreateFont(26, FontStyle.Bold);
                Font roleFont = Globals.KarlaFontFamily.CreateFont(100, FontStyle.Bold);
                Font placementFont = Globals.KarlaFontFamily.CreateFont(113, FontStyle.Bold);
                Font ordinalFont = Globals.KarlaFontFamily.CreateFont(57, FontStyle.Bold);

                WebClient webClient = new WebClient();
                byte[] avatarBytes = await webClient.DownloadDataTaskAsync(user.GetAvatarUrl());

                using (Image<Rgba32> image = Image.Load(Path.Combine(Globals.AppPath, "Data", "img", "profile-template.png")))
                using (Image<Rgba32> rankImage = new Image<Rgba32>(225, 225))
                using (Image<Rgba32> avatarImage = Image.Load(avatarBytes))
                using (Image<Rgba32> roleImage = new Image<Rgba32>(455, 115))
                using (MemoryStream ms = new MemoryStream())
                {
                    string name = player.AirtableName.ToUpper();
                    string powerLevel = Math.Round(player.PowerLevel, 1).ToString(CultureInfo.InvariantCulture);

                    SizeF nameTextSize = TextMeasurer.Measure(name, new RendererOptions(nameFont));

                    if (nameTextSize.Width > 700)
                    {
                        float nameScalingFactor = 700 / nameTextSize.Width;
                        nameFont = Globals.KarlaFontFamily.CreateFont(nameFont.Size * nameScalingFactor);
                    }

                    IPathCollection nameTextGlyphs = TextBuilder.GenerateGlyphs(name,
                        new PointF(445, 45), new RendererOptions(nameFont));

                    IPathCollection powerTextGlyphs = TextBuilder.GenerateGlyphs(powerLevel,
                        new PointF(445, 110), new RendererOptions(powerFont));

                    IPathCollection switchCodeGlyphs = TextBuilder.GenerateGlyphs(player.SwitchFriendCode,
                        new PointF(420, 987), new RendererOptions(switchCodeFont));

                    // When centering, it seems everything is off by the same amount.
                    const float offset = 0;

                    string splatZonesWr = player.WinRates.ContainsKey(GameMode.SplatZones) ?
                        $"{player.WinRates[GameMode.SplatZones]:P0}" :
                        "N/A";

                    SizeF szWrSize = TextMeasurer.Measure(splatZonesWr, new RendererOptions(winRateFont));

                    float szWrX = 379 + offset + (119F - szWrSize.Width / 2);

                    IPathCollection splatZonesGlyphs = TextBuilder.GenerateGlyphs(
                        splatZonesWr, new PointF(szWrX, 420), new RendererOptions(winRateFont));

                    string rainmakerWr = player.WinRates.ContainsKey(GameMode.Rainmaker) ?
                        $"{player.WinRates[GameMode.Rainmaker]:P0}" :
                        "N/A";

                    SizeF rmWrSize = TextMeasurer.Measure(rainmakerWr, new RendererOptions(winRateFont));

                    float rmWrX = 379 + offset + (119F - rmWrSize.Width / 2);

                    IPathCollection rainmakerGlyphs = TextBuilder.GenerateGlyphs(
                        rainmakerWr, new PointF(rmWrX, 587), new RendererOptions(winRateFont));

                    string towerControlWr = player.WinRates.ContainsKey(GameMode.TowerControl) ?
                        $"{player.WinRates[GameMode.TowerControl]:P0}" :
                        "N/A";

                    SizeF tcWrSize = TextMeasurer.Measure(towerControlWr, new RendererOptions(winRateFont));

                    float tcWrX = 1005 + offset + (119F - tcWrSize.Width / 2);

                    IPathCollection towerControlGlyphs = TextBuilder.GenerateGlyphs(
                        towerControlWr, new PointF(tcWrX, 420), new RendererOptions(winRateFont));

                    string clamBlitzWr = player.WinRates.ContainsKey(GameMode.ClamBlitz) ?
                        $"{player.WinRates[GameMode.ClamBlitz]:P0}" :
                        "N/A";

                    SizeF cbWrSize = TextMeasurer.Measure(clamBlitzWr, new RendererOptions(winRateFont));

                    float cbWrX = 1005 + offset + (119F - cbWrSize.Width / 2);

                    IPathCollection clamBlitzGlyphs = TextBuilder.GenerateGlyphs(
                        clamBlitzWr, new PointF(cbWrX, 587), new RendererOptions(winRateFont));

                    string overallWrText;

                    if (!player.WinRates.ContainsKey(GameMode.SplatZones) ||
                        !player.WinRates.ContainsKey(GameMode.TowerControl) ||
                        !player.WinRates.ContainsKey(GameMode.Rainmaker) ||
                        !player.WinRates.ContainsKey(GameMode.ClamBlitz))
                    {
                        overallWrText = "N/A";
                    }
                    else
                    {
                        double overallWr =
                            (player.WinRates[GameMode.SplatZones] + player.WinRates[GameMode.TowerControl] +
                             player.WinRates[GameMode.Rainmaker] + player.WinRates[GameMode.ClamBlitz]) / 4;

                        overallWrText = $"{overallWr:P0}";
                    }

                    SizeF overallWrSize = TextMeasurer.Measure(overallWrText, new RendererOptions(winRateFont));

                    float overallWrX = 740 + offset + (119F - overallWrSize.Width / 2);

                    IPathCollection overallGlyphs = TextBuilder.GenerateGlyphs(
                        overallWrText, new PointF(overallWrX, 755), new RendererOptions(winRateFont));

                    Rgba32 classColor;
                    string classText;

                    if (player.PowerLevel >= 2200)
                    {
                        classColor = new Rgba32(255, 70, 75);
                        classText = "1";
                    }
                    else if (player.PowerLevel >= 2000)
                    {
                        classColor = new Rgba32(255, 190, 52);
                        classText = "2";
                    }
                    else if (player.PowerLevel >= 1800)
                    {
                        classColor = new Rgba32(61, 255, 99);
                        classText = "3";
                    }
                    else
                    {
                        classColor = new Rgba32(21, 205, 227);
                        classText = "4";
                    }

                    IPathCollection classNameGlyphs =
                        TextBuilder.GenerateGlyphs("CLASS", new PointF(1414.32F, 70), new RendererOptions(classNameFont));

                    IPathCollection classGlyphs =
                        TextBuilder.GenerateGlyphs(classText, new PointF(1397.17F, 67.57F),
                            new RendererOptions(classFont));

                    Rgba32 roleColor;

                    // TODO Pull from airtable.
                    const string role = "Back";

                    switch (role)
                    {
                        case "Back":
                            roleColor = Rgba32.FromHex("#4BDFFA");
                            break;
                        case "Front":
                            roleColor = Rgba32.FromHex("#EB5F5F");
                            break;
                        case "Mid":
                            roleColor = Rgba32.FromHex("#61E87B");
                            break;
                        default:
                            roleColor = Rgba32.Black;
                            break;
                    }

                    SizeF roleNameSize = TextMeasurer.Measure(role, new RendererOptions(roleFont));

                    float roleNameX = (float) roleImage.Width / 2 - roleNameSize.Width / 2;
                    float roleNameY = (float) roleImage.Height / 2 - roleNameSize.Height / 2 - 10;

                    IPathCollection roleGlyphs = TextBuilder.GenerateGlyphs(role.ToUpper(), new PointF(roleNameX, roleNameY),
                        new RendererOptions(roleFont));

                    (int placement, string ordinal) = await AirTableClient.GetPlayerStandings(player, this.Context);

                    SizeF placementSize =
                        TextMeasurer.Measure(placement.ToString(), new RendererOptions(placementFont));
                    SizeF ordinalSize =
                        TextMeasurer.Measure(ordinal, new RendererOptions(ordinalFont));

                    float standingsWidth =
                        placementSize.Width + ordinalSize.Width;

                    float placementX = 949 + (347 / 2F - standingsWidth / 2F);

                    IPathCollection placementGlyphs = TextBuilder.GenerateGlyphs(placement.ToString(),
                        new PointF(placementX, 140), new RendererOptions(placementFont));

                    float ordinalX = placementX + placementSize.Width;
                    float ordinalY = 140 + placementSize.Height - ordinalSize.Height - 5;

                    IPathCollection ordinalGlyphs = TextBuilder.GenerateGlyphs(ordinal, new PointF(ordinalX, ordinalY),
                        new RendererOptions(ordinalFont));

                    TextGraphicsOptions textGraphicsOptions = new TextGraphicsOptions(true);

                    rankImage.Mutate(e => e
                        .Fill(classColor)
                        .Apply(f => ApplyRoundedCorners(f, 30))
                    );

                    avatarImage.Mutate(e => e
                        .Resize(new Size(268, 268))
                        .Apply(img => ApplyRoundedCorners(img, 40))
                    );

                    roleImage.Mutate(e => e
                        .Fill(roleColor)
                        .Apply(f => ApplyRoundedCorners(f, 30))
                        .Fill((GraphicsOptions)textGraphicsOptions, Rgba32.White, roleGlyphs)
                    );

                    image.Mutate(e => e
                        .DrawImage(avatarImage, new Point(48, 32), 1)
                        .DrawImage(rankImage, new Point(1340, 50), 1)
                        .DrawImage(roleImage, new Point(1111, 755), 1)
                        .Fill((GraphicsOptions)textGraphicsOptions, Rgba32.Black, powerTextGlyphs)
                        .Fill((GraphicsOptions)textGraphicsOptions, Rgba32.Black, nameTextGlyphs)
                        .Fill((GraphicsOptions)textGraphicsOptions, Rgba32.Black, switchCodeGlyphs)
                        .Fill((GraphicsOptions)textGraphicsOptions, Rgba32.Black, splatZonesGlyphs)
                        .Fill((GraphicsOptions)textGraphicsOptions, Rgba32.Black, rainmakerGlyphs)
                        .Fill((GraphicsOptions)textGraphicsOptions, Rgba32.Black, towerControlGlyphs)
                        .Fill((GraphicsOptions)textGraphicsOptions, Rgba32.Black, clamBlitzGlyphs)
                        .Fill((GraphicsOptions)textGraphicsOptions, Rgba32.Black, overallGlyphs)
                        .Fill((GraphicsOptions)textGraphicsOptions, Rgba32.White, classGlyphs)
                        .Fill((GraphicsOptions)textGraphicsOptions, Rgba32.White, classNameGlyphs)
                        .Fill((GraphicsOptions)textGraphicsOptions, Rgba32.Black, placementGlyphs)
                        .Fill((GraphicsOptions)textGraphicsOptions, Rgba32.Black, ordinalGlyphs)
                    );

                    image.SaveAsPng(ms);

                    using (MemoryStream memory = new MemoryStream(ms.GetBuffer()))
                    {
                        await message.DeleteAsync();
                        await this.Context.Channel.SendFileAsync(memory, "profile.png");
                    }
                }

                Configuration.Default.MemoryAllocator.ReleaseRetainedResources();
            }
            catch (Exception e)
            {
                Logger.Error(e);
                throw;
            }
        }

        // This method can be seen as an inline implementation of an `IImageProcessor`:
        // (The combination of `IImageOperations.Apply()` + this could be replaced with an `IImageProcessor`)
        private static void ApplyRoundedCorners(Image<Rgba32> img, float cornerRadius)
        {
            IPathCollection corners = BuildCorners(img.Width, img.Height, cornerRadius);

            GraphicsOptions graphicOptions = new GraphicsOptions(true)
            {
                AlphaCompositionMode = PixelAlphaCompositionMode.DestOut // enforces that any part of this shape that has color is punched out of the background
            };

            // mutating in here as we already have a cloned original
            // use any color (not Transparent), so the corners will be clipped
            img.Mutate(x => x.Fill(graphicOptions, Rgba32.LimeGreen, corners));
        }

        private static IPathCollection BuildCorners(int imageWidth, int imageHeight, float cornerRadius)
        {
            // first create a square
            RectangularPolygon rect = new RectangularPolygon(-0.5f, -0.5f, cornerRadius, cornerRadius);

            // then cut out of the square a circle so we are left with a corner
            IPath cornerTopLeft = rect.Clip(new EllipsePolygon(cornerRadius - 0.5f, cornerRadius - 0.5f, cornerRadius));

            // corner is now a corner shape positions top left
            // lets make 3 more positioned correctly, we can do that by translating the original around the center of the image

            float rightPos = imageWidth - cornerTopLeft.Bounds.Width + 1;
            float bottomPos = imageHeight - cornerTopLeft.Bounds.Height + 1;

            // move it across the width of the image - the width of the shape
            IPath cornerTopRight = cornerTopLeft.RotateDegree(90).Translate(rightPos, 0);
            IPath cornerBottomLeft = cornerTopLeft.RotateDegree(-90).Translate(0, bottomPos);
            IPath cornerBottomRight = cornerTopLeft.RotateDegree(180).Translate(rightPos, bottomPos);

            return new PathCollection(cornerTopLeft, cornerBottomLeft, cornerTopRight, cornerBottomRight);
        }
    }
}