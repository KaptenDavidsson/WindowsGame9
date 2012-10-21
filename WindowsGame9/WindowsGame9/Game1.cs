using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Shadows2D;
using Ziggyware;
using Lidgren.Network;
using System.Xml.Linq;
using System.Diagnostics;
using System.Threading;
using XnaGameServer;

namespace WindowsGame9
{
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        Texture2D crossTexture;

        
        public List<Bomb> bombs = new List<Bomb>();

        ShadowmapResolver shadowmapResolver;
        LightArea lightArea1;
        LightArea lightArea2;
        RenderTarget2D screenShadows;
        QuadRenderComponent quadRender;
        Vector2 lightPosition;
        Vector2 lightPosition2;

        SpriteFont font;
        double nextSendUpdates = NetTime.Now;

        int screenWidth;
        int screenHeight;

        Dictionary<long, Player> players = new Dictionary<long, Player>();
        Player thisPlayer = new Player();

        private const float Rotation = 0;
        private const float Scale = 2.0f;
        private const float Depth = 0.5f;

        int tileSize = 20;
        Texture2D whiteBackground;
        
        public List<Weapon> weaponDefinitions = new List<Weapon>();

        AnimatedTexture bloodTexture;
        PlayerBase bloodNpc;

        MapBuilder mapBuilder;
        int stairCounter;
        AnimatedTexture artificialWallTexture;

        ServerLayer serverLayer;

        StartScreen startScreen;

        GameState currentGameState = GameState.TitleScreen;
        public enum GameState
        {
            TitleScreen = 0,
            GameStarted,
            GameEnded,
        }


        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            this.IsFixedTimeStep = false;
            //graphics.SynchronizeWithVerticalRetrace = false;

            quadRender = new QuadRenderComponent(this);
            this.Components.Add(quadRender);

            TargetElapsedTime = TimeSpan.FromSeconds(1 / 30.0);
        }
        protected override void Initialize()
        {
            graphics.PreferredBackBufferWidth = 900;
            graphics.PreferredBackBufferHeight = 800;
            //graphics.IsFullScreen = true;
            graphics.ApplyChanges();

            base.Initialize();
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);

            screenWidth = graphics.GraphicsDevice.PresentationParameters.BackBufferWidth;
            screenHeight = graphics.GraphicsDevice.PresentationParameters.BackBufferHeight;

            thisPlayer.ScreenCenter = new Vector2(screenWidth / 2, screenHeight / 2);
            thisPlayer.Position = new Vector2(1000, 700);
            thisPlayer.Life = 2000;
            thisPlayer.IsThisPlayer = true;

            mapBuilder = new MapBuilder(this, screenWidth, screenHeight, graphics, tileSize, thisPlayer, spriteBatch, players);

            shadowmapResolver = new ShadowmapResolver(GraphicsDevice, quadRender, ShadowmapSize.Size256, ShadowmapSize.Size1024);
            shadowmapResolver.LoadContent(Content);
            lightArea1 = new LightArea(GraphicsDevice, ShadowmapSize.Size1024);
            lightArea2 = new LightArea(GraphicsDevice, ShadowmapSize.Size512);

            lightPosition = thisPlayer.ScreenCenter;
            lightPosition2 = new Vector2(screenWidth / 2 - 80, screenHeight / 2);
            screenShadows = new RenderTarget2D(GraphicsDevice, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);

            crossTexture = Content.Load<Texture2D>("cross");
            font = Content.Load<SpriteFont>("myFont");

            whiteBackground = Content.Load<Texture2D>("Cards/whitebackground");

            startScreen = new StartScreen(spriteBatch, new PlayerCard { Texture = Content.Load<Texture2D>("Cards/human"), BoundingBox = new Rectangle(20, 20, 200, 200), AnimatedTexture = new AnimatedTexture(Content.Load<Texture2D>("PlayerSprites/dudespritesheet"), 4, 4, true, 20, spriteBatch, 50, 50, new Vector2(25, 25), null) },
                new PlayerCard { Texture = Content.Load<Texture2D>("Cards/alien"), BoundingBox = new Rectangle(300, 20, 200, 200), AnimatedTexture = new AnimatedTexture(Content.Load<Texture2D>("PlayerSprites/alienspritesheet"), 4, 4, true, 20, spriteBatch, 70, 70, new Vector2(25, 25), null) },
                thisPlayer, mapBuilder, Content.Load<Texture2D>("Cards/checked"), Content.Load<Texture2D>("Cards/unchecked"), font);

            LoadWeaponDefinitions();

            artificialWallTexture = new AnimatedTexture(Content.Load<Texture2D>("MapItems/artificialwall"), 8, 1, false, 1, spriteBatch, 40, 40, new Vector2(20, 20), null);

            bloodTexture = new AnimatedTexture(Content.Load<Texture2D>("WeaponSprites/blood"), 13, 1, false, 26, spriteBatch, 50, 50, new Vector2(25, 25), null);
        }

        public void LoadWeaponDefinitions()
        {
            XDocument doc = XDocument.Load("../../../Furniture.xml");

            foreach (var f in doc.Elements("items").Elements("weapondefinitions").Elements())
            {
                Weapon weapon = new Weapon
                {
                    Power = int.Parse(f.Attribute("power").Value),
                    IsPrimary = bool.Parse(f.Attribute("isprimary").Value),
                    Texture = Content.Load<Texture2D>("Cards/" + f.Attribute("name").Value),
                    Name = f.Attribute("name").Value,
                    CoolDown = int.Parse(f.Attribute("cooldown").Value),
                    DrainsPower = bool.Parse(f.Attribute("drainspower").Value),
                    Damage = int.Parse(f.Attribute("damage").Value),
                    Range = int.Parse(f.Attribute("range").Value),
                    Push = int.Parse(f.Attribute("push").Value)
                };

                if (f.Elements("animatedtexture").Count() > 0)
                    weapon.UserTexture = new AnimatedTexture(Content.Load<Texture2D>("WeaponSprites/" + f.Elements("animatedtexture").ElementAt(0).Attribute("name").Value), int.Parse(f.Elements("animatedtexture").ElementAt(0).Attribute("horizontal").Value), int.Parse(f.Elements("animatedtexture").ElementAt(0).Attribute("vertical").Value), bool.Parse(f.Elements("animatedtexture").ElementAt(0).Attribute("repeat").Value), int.Parse(f.Elements("animatedtexture").ElementAt(0).Attribute("framespersecond").Value), spriteBatch, int.Parse(f.Elements("animatedtexture").ElementAt(0).Attribute("width").Value), int.Parse(f.Elements("animatedtexture").ElementAt(0).Attribute("height").Value), new Vector2(int.Parse(f.Elements("animatedtexture").ElementAt(0).Attribute("originx").Value), int.Parse(f.Elements("animatedtexture").ElementAt(0).Attribute("originy").Value)), null);

                if (f.Elements("animatedtexture").Count() > 1)
                    weapon.EffectTexture = new AnimatedTexture(Content.Load<Texture2D>("WeaponSprites/" + f.Elements("animatedtexture").ElementAt(1).Attribute("name").Value), int.Parse(f.Elements("animatedtexture").ElementAt(1).Attribute("horizontal").Value), int.Parse(f.Elements("animatedtexture").ElementAt(1).Attribute("vertical").Value), bool.Parse(f.Elements("animatedtexture").ElementAt(1).Attribute("repeat").Value), int.Parse(f.Elements("animatedtexture").ElementAt(1).Attribute("framespersecond").Value), spriteBatch, int.Parse(f.Elements("animatedtexture").ElementAt(1).Attribute("width").Value), int.Parse(f.Elements("animatedtexture").ElementAt(1).Attribute("height").Value), new Vector2(int.Parse(f.Elements("animatedtexture").ElementAt(1).Attribute("originx").Value), int.Parse(f.Elements("animatedtexture").ElementAt(1).Attribute("originy").Value)), Content.Load<SoundEffect>("Sounds/" + f.Elements("animatedtexture").ElementAt(1).Attribute("sound").Value));

                weaponDefinitions.Add(weapon);
            }
        }



        protected override void UnloadContent()
        {
        }
        protected override void OnExiting(object sender, EventArgs args)
        {
            if (serverLayer != null)
                serverLayer.ShutDown();

            XnaServer.IsRunning = false;

            base.OnExiting(sender, args);
        }

        int velocity = 3;
        int defaultVelocity = 4;
        int previousMouseWheelState;
        bool seeThroughWalls;
        bool walkThroughWalls;
        bool secondLightSource;
        bool isCameraActive;

        protected override void Update(GameTime gameTime)
        {
            if (currentGameState == GameState.GameStarted)
            {
                if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                    this.Exit();

                //XnaServer server = new XnaServer();
                //server.Start();

                Vector2 tempDir = new Vector2(screenWidth / 2 - Mouse.GetState().X, screenHeight / 2 - Mouse.GetState().Y);
                tempDir.Normalize();
                thisPlayer.Direction = tempDir;

                tempDir = Vector2.Zero;

                if (stairCounter <= 0)
                {
                    if (!thisPlayer.IsStunned(gameTime.ElapsedGameTime.Milliseconds))
                    {
                        if (Keyboard.GetState().IsKeyDown(Keys.W))
                            tempDir = thisPlayer.Direction;
                        else if (Keyboard.GetState().IsKeyDown(Keys.S))
                            tempDir = -thisPlayer.Direction;
                        else if (Keyboard.GetState().IsKeyDown(Keys.A))
                            tempDir = new Vector2(thisPlayer.Direction.Y, -thisPlayer.Direction.X);
                        else if (Keyboard.GetState().IsKeyDown(Keys.D))
                            tempDir = new Vector2(-thisPlayer.Direction.Y, thisPlayer.Direction.X);
                    }
                    else
                    {
                        tempDir = thisPlayer.InertialVelocity / 5;
                    }

                }
                //if (Keyboard.GetState().IsKeyDown(Keys.W))
                //    tempDir = new Vector2(0, 1);
                //else if (Keyboard.GetState().IsKeyDown(Keys.S))
                //    tempDir = new Vector2(0, -1);
                //else if (Keyboard.GetState().IsKeyDown(Keys.A))
                //    tempDir = new Vector2(1, 0);
                //else if (Keyboard.GetState().IsKeyDown(Keys.D))
                //    tempDir = new Vector2(-1, 0);

                if (thisPlayer.Position.X - tempDir.X * 2 * velocity > 0 && thisPlayer.Position.X - tempDir.X * 2 * velocity < mapBuilder.foregroundContour.GetLength(0) * 20 &&
                    (mapBuilder.foregroundContourWFurniture[(int)(thisPlayer.Position.X - tempDir.X * velocity) / 20, (int)(thisPlayer.Position.Y) / 20] != Color.Black &&
                    mapBuilder.foregroundContourWFurniture[(int)(thisPlayer.Position.X - tempDir.X * 2 * velocity) / 20, (int)(thisPlayer.Position.Y) / 20] != Color.Black || walkThroughWalls))
                    thisPlayer.Position -= new Vector2(tempDir.X * velocity * gameTime.ElapsedGameTime.Milliseconds / 10, 0);

                if (thisPlayer.Position.Y - tempDir.Y * 2 * velocity > 0 && thisPlayer.Position.X - tempDir.X * 2 * velocity < mapBuilder.foregroundContour.GetLength(1) * 20 &&
                    (mapBuilder.foregroundContourWFurniture[(int)(thisPlayer.Position.X) / 20, (int)(thisPlayer.Position.Y - tempDir.Y * velocity) / 20] != Color.Black &&
                    mapBuilder.foregroundContourWFurniture[(int)(thisPlayer.Position.X) / 20, (int)(thisPlayer.Position.Y - tempDir.Y * 2 * velocity) / 20] != Color.Black || walkThroughWalls))
                    thisPlayer.Position -= new Vector2(0, tempDir.Y * velocity * gameTime.ElapsedGameTime.Milliseconds / 10);


                if (thisPlayer.Position.X + thisPlayer.ScreenCenter.X > screenWidth * 0.2 + mapBuilder.gridTexturePlacement.X && mapBuilder.gridTexturePlacement.X < mapBuilder.foregroundContour.GetLength(0) * 20 / 2)
                    mapBuilder.gridTexture = mapBuilder.BuildContourTexture(Math.Max((int)thisPlayer.Position.X - (int)thisPlayer.ScreenCenter.X, 0), Math.Max(0, (int)thisPlayer.Position.Y - (int)thisPlayer.ScreenCenter.Y));

                if (thisPlayer.Position.X + thisPlayer.ScreenCenter.X < -screenWidth * 0.2 + mapBuilder.gridTexturePlacement.X && mapBuilder.gridTexturePlacement.X > 0)
                    mapBuilder.gridTexture = mapBuilder.BuildContourTexture(Math.Max(0, (int)thisPlayer.Position.X - (int)thisPlayer.ScreenCenter.X), Math.Max(0, (int)thisPlayer.Position.Y - (int)thisPlayer.ScreenCenter.Y));

                if (thisPlayer.Position.Y + thisPlayer.ScreenCenter.Y > screenHeight * 0.2 + mapBuilder.gridTexturePlacement.Y && mapBuilder.gridTexturePlacement.Y < mapBuilder.foregroundContour.GetLength(1) * 20 / 2)
                    mapBuilder.gridTexture = mapBuilder.BuildContourTexture(Math.Max((int)thisPlayer.Position.X - (int)thisPlayer.ScreenCenter.X, 0), Math.Max(0, (int)thisPlayer.Position.Y - (int)thisPlayer.ScreenCenter.Y));

                if (thisPlayer.Position.Y + thisPlayer.ScreenCenter.Y < -screenHeight * 0.2 + mapBuilder.gridTexturePlacement.Y && mapBuilder.gridTexturePlacement.Y > 0)
                    mapBuilder.gridTexture = mapBuilder.BuildContourTexture(Math.Max((int)thisPlayer.Position.X - (int)thisPlayer.ScreenCenter.X, 0), Math.Max(0, (int)thisPlayer.Position.Y - (int)thisPlayer.ScreenCenter.Y));

                if (Mouse.GetState().LeftButton == ButtonState.Pressed && thisPlayer.SelectedPrimaryWeapon.CurrentCoolDown <= 0 &&
                    (mapBuilder.Npcs.Count > 0 || players.Count > 0))
                {
                    if (thisPlayer.SelectedPrimaryWeapon.Name != "shotgun")
                    {
                        DetectHit(mapBuilder.Npcs, HitDetected, 0);
                        List<PlayerBase> pb = new List<PlayerBase>();
                        foreach (var p in players.Values)
                            pb.Add(p);

                        DetectHit(pb, HitDetected, 0);
                        
                    }
                    else
                    {
                        DetectHit(mapBuilder.Npcs, HitDetected, .2f);
                        DetectHit(mapBuilder.Npcs, HitDetected, .1f);
                        DetectHit(mapBuilder.Npcs, HitDetected, 0);
                        DetectHit(mapBuilder.Npcs, HitDetected, -.1f);
                        DetectHit(mapBuilder.Npcs, HitDetected, -.2f);
                    }
                }

                mapBuilder.Npcs.RemoveAll(n => n.IsDead);

                if (Keyboard.GetState().IsKeyDown(Keys.D1))
                    thisPlayer.IsPrimaryWeaponActive = true;
                else if (Keyboard.GetState().IsKeyDown(Keys.D2))
                    thisPlayer.IsPrimaryWeaponActive = false;


                if (Mouse.GetState().ScrollWheelValue != previousMouseWheelState)
                {
                    if (Mouse.GetState().ScrollWheelValue > previousMouseWheelState)
                        thisPlayer.NextWeapon();
                    else
                        thisPlayer.PreviousWeapon();

                    previousMouseWheelState = Mouse.GetState().ScrollWheelValue;
                }

                if (thisPlayer.PlayerType == Player.PlayerTypes.human)
                {
                    if (Mouse.GetState().LeftButton == ButtonState.Pressed && thisPlayer.SelectedPrimaryWeapon.Power > 0 && thisPlayer.SelectedPrimaryWeapon.CurrentCoolDown <= 0)
                    {
                        switch (thisPlayer.SelectedPrimaryWeapon.Name)
                        {
                            case "flamethrower":
                                //bombs.Add(new Bomb(Content.Load<Texture2D>("WeaponSprites/bomb"), new AnimatedTexture(Content.Load<Texture2D>("WeaponSprites/explosionsprite"), 5, 5, false, 12, spriteBatch, 50, 50, new Vector2(25, 25), null),
                                //    40, 40, 10, new Vector2(thisPlayer.Position.X + screenWidth / 2 - 25, thisPlayer.Position.Y + screenHeight / 2 - 25) - thisPlayer.Direction * 30, new Vector2(0, 0), 50, 0, 50, true));
                                //bombs.Add(new Bomb(Content.Load<Texture2D>("WeaponSprites/bomb"), new AnimatedTexture(Content.Load<Texture2D>("WeaponSprites/explosionsprite"), 5, 5, false, 12, spriteBatch, 50, 50, new Vector2(25, 25), null),
                                //    80, 80, 10, new Vector2(thisPlayer.Position.X + screenWidth / 2 - 50, thisPlayer.Position.Y + screenHeight / 2 - 50) - thisPlayer.Direction * 50, new Vector2(0, 0), 50, 0, 50, true));
                                //bombs.Add(new Bomb(Content.Load<Texture2D>("WeaponSprites/bomb"), new AnimatedTexture(Content.Load<Texture2D>("WeaponSprites/explosionsprite"), 5, 5, false, 12, spriteBatch, 50, 50, new Vector2(25, 25), null),
                                //    160, 160, 10, new Vector2(thisPlayer.Position.X + screenWidth / 2 - 100, thisPlayer.Position.Y + screenHeight / 2 - 100) - thisPlayer.Direction * 110, new Vector2(0, 0), 50, 0, 50, true));
                                break;
                            default:
                                break;
                        }
                        if (thisPlayer.SelectedPrimaryWeapon.DrainsPower)
                            thisPlayer.SelectedPrimaryWeapon.Power--;
                    }

                    seeThroughWalls = false;
                    walkThroughWalls = false;
                    secondLightSource = false;
                    if (Mouse.GetState().RightButton == ButtonState.Pressed && thisPlayer.SelectedSecondaryWeapon.Power > 0)
                    {
                        switch (thisPlayer.SelectedSecondaryWeapon.Name)
                        {
                            case "grenade":
                                if (thisPlayer.SelectedSecondaryWeapon.CurrentCoolDown <= 0)
                                {
                                    bombs.Add(new Bomb(thisPlayer.SelectedSecondaryWeapon.UserTexture, thisPlayer.SelectedSecondaryWeapon.EffectTexture,
                                    30, 35, 10, new Vector2(thisPlayer.Position.X + screenWidth / 2, thisPlayer.Position.Y + screenHeight / 2),
                                    -thisPlayer.Direction * 6, 50, 500, 500, true, new Vector2(Mouse.GetState().X + thisPlayer.Position.X, Mouse.GetState().Y + thisPlayer.Position.Y)));

                                    serverLayer.SendShoting(thisPlayer, 100, weaponDefinitions, thisPlayer.SelectedSecondaryWeapon, Mouse.GetState().X, Mouse.GetState().Y);
                                }

                                break;
                            case "remotebomb":
                                if (thisPlayer.SelectedSecondaryWeapon.CurrentCoolDown <= 0)
                                {
                                    if (thisPlayer.SelectedSecondaryWeapon.Bomb == null)
                                    {
                                        Bomb bomb = new Bomb(thisPlayer.SelectedSecondaryWeapon.UserTexture, thisPlayer.SelectedSecondaryWeapon.EffectTexture,
                                            30, 35, 10, new Vector2(thisPlayer.Position.X + screenWidth / 2 - 50, thisPlayer.Position.Y + screenHeight / 2 - 50),
                                            Vector2.Zero, 50, 500, 500, false, Vector2.Zero);
                                        thisPlayer.SelectedSecondaryWeapon.Bomb = bomb;
                                        bombs.Add(bomb);

                                        serverLayer.SendShoting(thisPlayer, 100, weaponDefinitions, thisPlayer.SelectedSecondaryWeapon, Mouse.GetState().X, Mouse.GetState().Y);
                                    }
                                    else
                                    {
                                        thisPlayer.SelectedSecondaryWeapon.Bomb.Explode();
                                        thisPlayer.SelectedSecondaryWeapon.Bomb = null;

                                        serverLayer.SendShoting(thisPlayer, 100, weaponDefinitions, thisPlayer.SelectedSecondaryWeapon, Mouse.GetState().X, Mouse.GetState().Y);
                                    }
                                }
                                break;
                            case "timebomb":
                                //bombs.Add(new Bomb(Content.Load<Texture2D>("WeaponSprites/bomb"), new AnimatedTexture(Content.Load<Texture2D>("WeaponSprites/explosionsprite"), 5, 5, false, 12, spriteBatch, 50, 50, new Vector2(25, 25), Content.Load<SoundEffect>("Sounds/ExplosionSound")),
                                //    80, 80, 10, new Vector2(thisPlayer.Position.X + screenWidth / 2, thisPlayer.Position.Y + screenHeight / 2), new Vector2(0, 0), 50, 100, 200, true));
                                break;
                            case "camera":
                                isCameraActive = true;
                                lightPosition2 = new Vector2(thisPlayer.Position.X + screenWidth / 2, thisPlayer.Position.Y + screenHeight / 2);
                                break;
                            case "trap":
                            case "seetraps":
                                break;
                            case "glassfibre": secondLightSource = true;
                                break;
                            case "movefurniture":
                                break;
                            default:
                                break;
                        }
                        thisPlayer.SelectedSecondaryWeapon.Power--;
                    }
                }
                else
                {
                    if (Mouse.GetState().LeftButton == ButtonState.Pressed && thisPlayer.Power > 0)
                    {
                        switch (thisPlayer.SelectedPrimaryWeapon.Name)
                        {
                            case "electricshock":
                            case "push":
                                break;
                            default: 
                                break;
                        }
                        if (thisPlayer.SelectedPrimaryWeapon.DrainsPower)
                            thisPlayer.Power--;
                    }

                    seeThroughWalls = false;
                    walkThroughWalls = false;
                    secondLightSource = false;
                    velocity = defaultVelocity;
                    if (Mouse.GetState().RightButton == ButtonState.Pressed && thisPlayer.Power > 0)
                    {
                        switch (thisPlayer.SelectedSecondaryWeapon.Name)
                        {
                            case "seewalls": seeThroughWalls = true;
                                break;
                            case "walkwalls": walkThroughWalls = true;
                                break;
                            case "teleport":
                                if (thisPlayer.SelectedSecondaryWeapon.CurrentCoolDown <= 0)
                                {
                                    thisPlayer.Position += new Vector2(Mouse.GetState().X - screenWidth / 2,Mouse.GetState().Y - screenHeight / 2);
                                    thisPlayer.SelectedSecondaryWeapon.ResetCoolDown();
                                }
                                else
                                {
                                    thisPlayer.SelectedSecondaryWeapon.CurrentCoolDown--;
                                }
                                break;
                            case "wall":
                                mapBuilder.ArtificialWalls.Add(new ArtificialWall { Position = new Vector2(thisPlayer.Position.X + screenWidth / 2, thisPlayer.Position.Y + screenHeight / 2), Texture = artificialWallTexture });
                                break;
                            case "stun":
                            case "becon":
                            case "forcefield":
                                break;
                            case "repel":
                                break;
                            case "speed": velocity = 8;
                                break;
                            case "merge":
                                //foreach (var player in players)
                                    //if ((player.Value.Position - new Vector2(playerX, playerY)).Length() < 10)
                                        //SendMergeRequest(MergeRequestRecieved);
                                break;
                            case "disguise":
                                break;
                            default:
                                break;
                        }
                        thisPlayer.Power--;
                    }
                }


                if (Mouse.GetState().LeftButton == ButtonState.Pressed && thisPlayer.SelectedPrimaryWeapon.CurrentCoolDown <= 0)
                {
                    thisPlayer.SelectedPrimaryWeapon.ResetCoolDown();
                    if (thisPlayer.SelectedPrimaryWeapon.EffectTexture != null)
                        thisPlayer.SelectedPrimaryWeapon.EffectTexture.Start();

                    if (thisPlayer.SelectedPrimaryWeapon.UserTexture != null)
                        thisPlayer.SelectedPrimaryWeapon.UserTexture.Start();
                }

                if (Mouse.GetState().RightButton == ButtonState.Pressed && thisPlayer.SelectedSecondaryWeapon.CurrentCoolDown <= 0)
                {
                    thisPlayer.SelectedSecondaryWeapon.ResetCoolDown();
                    //thisPlayer.SelectedSecondaryWeapon.EffectTexture.Start();
                }

                if (thisPlayer.SelectedPrimaryWeapon.CurrentCoolDown > 0)
                    thisPlayer.SelectedPrimaryWeapon.CurrentCoolDown -= gameTime.ElapsedGameTime.Milliseconds;

                if (thisPlayer.SelectedSecondaryWeapon.CurrentCoolDown > 0)
                    thisPlayer.SelectedSecondaryWeapon.CurrentCoolDown -= gameTime.ElapsedGameTime.Milliseconds;

                bombs.RemoveAll(b => b.Done());

                if (thisPlayer.PlayerType == Player.PlayerTypes.human)
                {
                    Weapon pickedUpWeapon = mapBuilder.weapons.SingleOrDefault(w => w.BoundingBox.Contains((int)thisPlayer.Position.X, (int)thisPlayer.Position.Y));
                    if (pickedUpWeapon != null)
                    {
                        thisPlayer.AddWeapon(pickedUpWeapon);
                        mapBuilder.weapons.RemoveAll(w => w == pickedUpWeapon);
                    }
                }

                Recharger recharger = mapBuilder.Rechargers.SingleOrDefault(r => new Rectangle((int)r.Position.X, (int)r.Position.Y, 40, 40).Contains((int)thisPlayer.Position.Y + screenWidth / 2, (int)thisPlayer.Position.Y + screenHeight / 2));
                if (recharger != null)
                {
                    if (thisPlayer.CarriesWeapon(recharger.Type))
                    {
                        thisPlayer.RechargeWeapon(recharger);
                        mapBuilder.Rechargers.RemoveAll(r => r == recharger);
                    }
                }


                foreach (var bomb in bombs)
                {
                    if (mapBuilder.foregroundContour[(int)(bomb.Position.X) / 20, (int)(bomb.Position.Y) / 20] == Color.Black)
                        bomb.Velocity = new Vector2(-bomb.Velocity.X * .2f * gameTime.ElapsedGameTime.Milliseconds / 1000,
                            -bomb.Velocity.Y * .2f * gameTime.ElapsedGameTime.Milliseconds / 1000);

                    if (bomb.IsExploding)
                    {
                        foreach (var npc in mapBuilder.Npcs)
                        {
                            Vector2 distance = npc.Position - bomb.Position + thisPlayer.ScreenCenter;
                            if (!npc.IsBombImmune(gameTime.ElapsedGameTime.Milliseconds) && distance.Length() < 100)
                            {
                                distance.Normalize();
                                npc.TakeDamage(20, distance, 30);
                            }
                        }

                        Vector2 distance2 = thisPlayer.Position - bomb.Position + thisPlayer.ScreenCenter;
                        if (!thisPlayer.IsBombImmune(gameTime.ElapsedGameTime.Milliseconds) && distance2.Length() < 100)
                        {
                            distance2.Normalize();
                            thisPlayer.TakeDamage(20, -distance2, 30);
                            distance2.Normalize();
                        }
                    }
                }

                foreach (var staircase in mapBuilder.stairs)
                {
                    if (staircase.BoundingBox.Contains((int)(thisPlayer.Position.X + screenWidth / 2), (int)(thisPlayer.Position.Y + screenHeight / 2)))
                    {
                        if (!mapBuilder.standsOnStaircase)
                            stairCounter = 100;

                        mapBuilder.LoadMap(staircase.ToFloor);
                        break;
                    }
                    else
                    {
                        mapBuilder.standsOnStaircase = false;
                    }
                }

                if (stairCounter > 0)
                    stairCounter--;

                //foreach (var npc in mapBuilder.Npcs)
                //    npc.Move(new Vector2(thisPlayer.Position.X, thisPlayer.Position.Y), gameTime.ElapsedGameTime.Milliseconds);

                if (thisPlayer.Life <= 0)
                {
                    thisPlayer.Position = Vector2.Zero;
                    thisPlayer.Life = 2000;
                }

                if (Keyboard.GetState().IsKeyDown(Keys.Space))
                {
                    foreach (var door in mapBuilder.Doors)
                    {
                        if ((door.Position - new Vector2(thisPlayer.Position.X, thisPlayer.Position.Y)).Length() < 50 && !door.IsOpening)
                        {
                            door.IsOpen = !door.IsOpen;
                            mapBuilder.UpdateForegroundContour((int)thisPlayer.Position.X, (int)thisPlayer.Position.Y);
                            break;
                        }
                    }
                }

                mapBuilder.Doors.ForEach(d => d.DecreaseCoolDown(gameTime.ElapsedGameTime.Milliseconds));

                serverLayer.SendPosition(thisPlayer);

                // read messages
                ServerLayer.MessageRouts messageRout;
                while ((messageRout = serverLayer.MessageRout) != ServerLayer.MessageRouts.None)
                {
                    switch (messageRout)
                    {
                        case ServerLayer.MessageRouts.DiscoveryResponse:
                            // just connect to first server discovered
                            serverLayer.Connect();
                            break;
                        case ServerLayer.MessageRouts.Position:
                            serverLayer.GetPosition(players, weaponDefinitions, this, spriteBatch);
                            break;
                        case ServerLayer.MessageRouts.Fire:
                            serverLayer.GetFire(players, weaponDefinitions, thisPlayer, bloodNpc, bloodTexture, bombs);
                            break;
                        case ServerLayer.MessageRouts.MoveNpc:
                            serverLayer.GetNpcPosition(mapBuilder.Npcs);
                            break;
                    }
                }
            }
            else
            {
                currentGameState = startScreen.Update(gameTime.ElapsedGameTime.Milliseconds);
                if (currentGameState == GameState.GameStarted)
                    serverLayer = new ServerLayer();
            }

            base.Update(gameTime);
        }

        private void DetectHit(List<PlayerBase> npcs, Action<PlayerBase> hitDetected, float deviation)
        {
            if (npcs.Count == 0) return;

            Vector2 shootDir = new Vector2(Mouse.GetState().X - screenWidth / 2, Mouse.GetState().Y - screenHeight / 2);
            shootDir.Normalize();
            if (deviation != 0)
                shootDir = Vector2.Transform(shootDir, Matrix.CreateRotationZ(deviation));

            Vector2 shotCoords = new Vector2(thisPlayer.Position.X / 20, thisPlayer.Position.Y / 20);
            int traveledLength = 0;
            bool done = false;
            while (!done)
            {
                foreach (var npc in npcs)
                {
                    if (shotCoords.X > mapBuilder.foregroundContour.GetLength(0) || shotCoords.X <= 0 || shotCoords.Y > mapBuilder.foregroundContour.GetLength(1) || shotCoords.Y <= 0 ||
                        mapBuilder.foregroundContour[(int)shotCoords.X, (int)shotCoords.Y] == Color.Black ||
                        thisPlayer.SelectedPrimaryWeapon.Range != -1 && traveledLength > thisPlayer.SelectedPrimaryWeapon.Range)
                    {
                        done = true;
                        break;
                    }
                    else if (Math.Abs(shotCoords.X - npc.Position.X / 20) < 1 && Math.Abs(shotCoords.Y - npc.Position.Y / 20) < 1)
                    {
                        HitDetected(npc);
                        done = true;
                        break;
                    }
                    else
                    {
                        shotCoords = new Vector2(shotCoords.X + shootDir.X, shotCoords.Y + shootDir.Y);
                    }
                }

                traveledLength++;
            }
        }

        private void HitDetected(PlayerBase npc)
        {
            if (npc is Npc)
            {
                Vector2 shotDir = npc.Position - thisPlayer.Position;
                shotDir.Normalize();
                //npc.TakeDamage(thisPlayer.SelectedPrimaryWeapon.Damage, -shotDir, thisPlayer.SelectedPrimaryWeapon.Push);

                //if (npc.Life <= 0)
                //{
                //    thisPlayer.Score += npc.Bounty;
                //    if (thisPlayer.PlayerType == Player.PlayerTypes.alien)
                //        thisPlayer.Power += npc.Bounty * 200;

                //    npc.Bounty = 0;
                //}

                serverLayer.SendShoting(thisPlayer, mapBuilder.Npcs.IndexOf(npc), weaponDefinitions, thisPlayer.SelectedPrimaryWeapon, Mouse.GetState().X + (int)thisPlayer.Position.X, Mouse.GetState().Y + (int)thisPlayer.Position.Y);
            }
            else
            {
                serverLayer.SendShoting(thisPlayer, 100, weaponDefinitions, thisPlayer.SelectedPrimaryWeapon, Mouse.GetState().X + (int)thisPlayer.Position.X, Mouse.GetState().Y + (int)thisPlayer.Position.Y);
            }
            bloodTexture.Start();
            bloodNpc = npc;
        }

        protected override void Draw(GameTime gameTime)
        {
            if (currentGameState == GameState.GameStarted)
            {
                GraphicsDevice.Clear(Color.CornflowerBlue);


                //first light area
                lightArea1.LightPosition = lightPosition;
                lightArea1.BeginDrawingShadowCasters();

                if (!seeThroughWalls)
                    DrawCasters(lightArea1);

                lightArea1.EndDrawingShadowCasters();
                shadowmapResolver.ResolveShadows(lightArea1.RenderTarget, lightArea1.RenderTarget, lightPosition);

                //second light area
                if (secondLightSource || isCameraActive)
                {
                    lightArea2.LightPosition = lightPosition2 - thisPlayer.Position;
                    lightArea2.BeginDrawingShadowCasters();
                    DrawCasters(lightArea2);
                    lightArea2.EndDrawingShadowCasters();
                    shadowmapResolver.ResolveShadows(lightArea2.RenderTarget, lightArea2.RenderTarget, lightPosition2 - thisPlayer.Position);
                }

                GraphicsDevice.SetRenderTarget(screenShadows);
                GraphicsDevice.Clear(Color.Black);
                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive);
                spriteBatch.Draw(lightArea1.RenderTarget, lightArea1.LightPosition - lightArea1.LightAreaSize * 0.5f, Color.White);

                if (secondLightSource || isCameraActive)
                    spriteBatch.Draw(lightArea2.RenderTarget, lightArea2.LightPosition - lightArea2.LightAreaSize * 0.5f, Color.Green);

                spriteBatch.End();

                GraphicsDevice.SetRenderTarget(null);


                GraphicsDevice.Clear(Color.Black);

                DrawGround();

                spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
                foreach (var furn in mapBuilder.furnitures)
                    spriteBatch.Draw(furn.Texture, new Rectangle(furn.Rect.X - (int)thisPlayer.Position.X + (int)thisPlayer.ScreenCenter.X, furn.Rect.Y - (int)thisPlayer.Position.Y + (int)thisPlayer.ScreenCenter.Y, furn.Rect.Width, furn.Rect.Height), null, Color.White);

                foreach (var staircase in mapBuilder.stairs)
                    spriteBatch.Draw(staircase.Texture, new Rectangle(staircase.BoundingBox.X - (int)thisPlayer.Position.X + (int)thisPlayer.ScreenCenter.X, staircase.BoundingBox.Y - (int)thisPlayer.Position.Y + (int)thisPlayer.ScreenCenter.Y, staircase.BoundingBox.Width, staircase.BoundingBox.Height), null, Color.White);

                foreach (var bomb in bombs)
                {
                    //spriteBatch.Draw(bomb.Texture, new Rectangle((int)(bomb.Position.X - thisPlayer.Position.X), (int)(bomb.Position.Y - thisPlayer.Position.Y), bomb.Width, bomb.Height),
                    //    bomb.Source, Color.White);
                    bomb.Draw(gameTime.ElapsedGameTime.Milliseconds, thisPlayer.Position);
                }
                foreach (var weapon in mapBuilder.weapons)
                    spriteBatch.Draw(weapon.Texture, new Rectangle((int)(weapon.Position.X * 20 - thisPlayer.Position.X + thisPlayer.ScreenCenter.X), (int)(weapon.Position.Y * 20 - thisPlayer.Position.Y + thisPlayer.ScreenCenter.Y), 40, 40), Color.White);

                foreach (var npc in mapBuilder.Npcs)
                    npc.Draw(gameTime.ElapsedGameTime.Milliseconds, thisPlayer.Position - thisPlayer.ScreenCenter);

                foreach (var recharger in mapBuilder.Rechargers)
                    spriteBatch.Draw(recharger.Texture, new Rectangle((int)(recharger.Position.X - thisPlayer.Position.X), (int)(recharger.Position.Y - thisPlayer.Position.Y), 40, 40), null, Color.White);

                foreach (var player in players.Values)
                {
                    player.Draw(gameTime.ElapsedGameTime.Milliseconds, player.Position - thisPlayer.Position + thisPlayer.ScreenCenter, 1);

                    if (!player.SelectedPrimaryWeapon.EffectTexture.IsDone)
                        player.SelectedPrimaryWeapon.EffectTexture.Draw(gameTime.ElapsedGameTime.Milliseconds, player.Position - thisPlayer.Position + thisPlayer.ScreenCenter, player.Direction);

                    if (!player.SelectedPrimaryWeapon.UserTexture.IsDone)
                        player.SelectedPrimaryWeapon.UserTexture.Draw(gameTime.ElapsedGameTime.Milliseconds, player.Position - thisPlayer.Position + thisPlayer.ScreenCenter, player.Direction);

                }

                foreach (var wall in mapBuilder.ArtificialWalls)
                    wall.Texture.Draw(gameTime.ElapsedGameTime.Milliseconds, wall.Position - new Vector2(thisPlayer.Position.X, thisPlayer.Position.Y), new Vector2());

                spriteBatch.End();


                BlendState blendState = new BlendState();
                blendState.ColorSourceBlend = Blend.DestinationColor;
                blendState.ColorDestinationBlend = Blend.SourceColor;

                spriteBatch.Begin(SpriteSortMode.Immediate, blendState);
                spriteBatch.Draw(screenShadows, Vector2.Zero, Color.White);
                spriteBatch.End();

                //DrawScene();


                spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);

                //spriteBatch.Draw(backgroundTexture, new Rectangle(0, 0, screenWidth, screenHeight), Color.White);

                //spriteBatch.Draw(foregroundContourTexture2, new Rectangle(0, 0, screenWidth, screenHeight),
                //    new Rectangle((int)(localPlayer.X - screenWidth / 2), (int)(localPlayer.Y - screenHeight / 2), screenWidth, screenHeight), Color.White);

                //foreach (var floorTile in floorTiles)
                //    if (floorTile.X > 0 && floorTile.X < 1300 + playerX && floorTile.Y > 0 && floorTile.Y < 1000 + playerY)
                //        spriteBatch.Draw(floorTexture, new Rectangle(floorTile.X - (int)playerX, floorTile.Y - (int)playerY, 100, 100), null, Color.White);

                foreach (var wall in mapBuilder.walls)
                    if (wall.X > 0 && wall.X < screenWidth + thisPlayer.Position.X && wall.Y > 0 && wall.Y < screenHeight + thisPlayer.Position.Y)
                        spriteBatch.Draw(mapBuilder.tileTexture, new Rectangle(wall.X - (int)thisPlayer.Position.X + (int)thisPlayer.ScreenCenter.X, wall.Y - (int)thisPlayer.Position.Y + (int)thisPlayer.ScreenCenter.Y, 20, 20), null, Color.White);


                if (thisPlayer.SelectedPrimaryWeapon.EffectTexture != null && !thisPlayer.SelectedPrimaryWeapon.EffectTexture.IsDone)
                    thisPlayer.SelectedPrimaryWeapon.EffectTexture.Draw(gameTime.ElapsedGameTime.Milliseconds, new Vector2(screenWidth / 2, screenHeight / 2), thisPlayer.Direction);

                if (thisPlayer.SelectedPrimaryWeapon.UserTexture != null && !thisPlayer.SelectedPrimaryWeapon.UserTexture.IsDone)
                    thisPlayer.SelectedPrimaryWeapon.UserTexture.Draw(gameTime.ElapsedGameTime.Milliseconds, new Vector2(screenWidth / 2, screenHeight / 2), thisPlayer.Direction);

                thisPlayer.Direction = thisPlayer.Direction;
                thisPlayer.Draw(gameTime.ElapsedGameTime.Milliseconds, new Vector2(thisPlayer.Position.X, thisPlayer.Position.Y));


                if (!bloodTexture.IsDone && bloodNpc != null)
                    bloodTexture.Draw(gameTime.ElapsedGameTime.Milliseconds, bloodNpc.Position - thisPlayer.Position + thisPlayer.ScreenCenter, new Vector2());

                foreach (var door in mapBuilder.Doors)
                    spriteBatch.Draw(door.Texture, new Rectangle((int)(door.Position.X - thisPlayer.Position.X + (int)thisPlayer.ScreenCenter.X), (int)(door.Position.Y - thisPlayer.Position.Y + (int)thisPlayer.ScreenCenter.Y), 10, 60), null, Color.White, door.Angle, Vector2.Zero, SpriteEffects.None, 0f);


                spriteBatch.Draw(whiteBackground, new Vector2(10, 10), new Rectangle(0, 0, 200, 150), Color.White);
                spriteBatch.Draw(thisPlayer.SelectedPrimaryWeapon.Texture, new Vector2(10, 10), new Rectangle(0, 0, 200, 150), Color.White);
                spriteBatch.Draw(whiteBackground, new Vector2(240, 10), new Rectangle(0, 0, 200, 150), Color.White);
                spriteBatch.Draw(thisPlayer.SelectedSecondaryWeapon.Texture, new Vector2(240, 10), new Rectangle(0, 0, 200, 150), Color.White);

                if (thisPlayer.PlayerType == Player.PlayerTypes.human)
                {
                    spriteBatch.DrawString(font, thisPlayer.SelectedPrimaryWeapon.Power.ToString(), new Vector2(20, 150), Color.Red);
                    spriteBatch.DrawString(font, thisPlayer.SelectedSecondaryWeapon.Power.ToString(), new Vector2(260, 150), Color.Red);
                }
                else
                {
                    spriteBatch.DrawString(font, thisPlayer.Power.ToString(), new Vector2(20, 150), Color.Red);
                }

                spriteBatch.DrawString(font, thisPlayer.Life.ToString(), new Vector2(500, 50), Color.Red);
                spriteBatch.DrawString(font, thisPlayer.Score.ToString(), new Vector2(600, 50), Color.Red);


                foreach (var roomDescription in mapBuilder.roomDescriptions)
                    if (roomDescription.BoundingBox.Contains((int)(thisPlayer.Position.X + screenWidth / 2), (int)(thisPlayer.Position.Y + screenHeight / 2)))
                        spriteBatch.DrawString(font, roomDescription.Description, new Vector2(500, 20), Color.Red);

                spriteBatch.End();
            }
            else
            {
                startScreen.Draw();
            }

            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
            spriteBatch.Draw(crossTexture, new Rectangle(Mouse.GetState().X - 25, Mouse.GetState().Y - 25, 50, 50), null, Color.White);
            spriteBatch.End();

            base.Draw(gameTime);
        }


        private void DrawCasters(LightArea lightArea)
        {
            spriteBatch.Begin();
            spriteBatch.Draw(mapBuilder.gridTexture, lightArea.ToRelativePosition(new Vector2(-thisPlayer.Position.X + mapBuilder.gridTexturePlacement.X - mapBuilder.ResidueX + thisPlayer.ScreenCenter.X, -thisPlayer.Position.Y + mapBuilder.gridTexturePlacement.Y - mapBuilder.ResidueY + thisPlayer.ScreenCenter.Y)), Color.Black);
            spriteBatch.End();
        }

        private void DrawScene()
        {
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            spriteBatch.Draw(mapBuilder.gridTexture, new Rectangle(-(int)thisPlayer.Position.X + mapBuilder.gridTexturePlacement.X - mapBuilder.ResidueX, -(int)thisPlayer.Position.Y + mapBuilder.gridTexturePlacement.Y - mapBuilder.ResidueY, screenWidth, screenHeight), new Color(new Vector4(1, 1, 1, 1.0f)));
            spriteBatch.End();
        }
        private void DrawGround()
        {
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Opaque, SamplerState.LinearWrap, DepthStencilState.Default, RasterizerState.CullNone);
            foreach (var tile in mapBuilder.floorTiles)
                spriteBatch.Draw(tile.Texture, new Vector2(tile.Rect.X - thisPlayer.Position.X + thisPlayer.ScreenCenter.X, tile.Rect.Y - thisPlayer.Position.Y + thisPlayer.ScreenCenter.Y), new Rectangle(0, 0, tile.Rect.Width, tile.Rect.Height), Color.White, 0, Vector2.Zero, 1.0f, SpriteEffects.None, 1.0f);

            spriteBatch.End();
        }
    }
}
