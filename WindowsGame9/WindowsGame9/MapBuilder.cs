using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace WindowsGame9
{
    class MapBuilder
    {
        Game1 useForLoadingContent;
        Texture2D mapTexture;
        public Texture2D gridTexture;
        public Color[,] foregroundContour;
        public Color[,] foregroundContourWFurniture;
        public Point gridTexturePlacement;
        int screenWidth;
        int screenHeight;
        GraphicsDeviceManager graphics;
        int tileSize;
        Dictionary<long, Player> players;

        public Texture2D tileTexture;
        public List<PlayerBase> Npcs = new List<PlayerBase>();

        public List<Weapon> weapons = new List<Weapon>();
        public Dictionary<Color, Texture2D> tilesMap = new Dictionary<Color, Texture2D>();
        public List<Point> walls = new List<Point>();
        public List<FloorTile> floorTiles = new List<FloorTile>();
        public List<FloorTile> furnitures = new List<FloorTile>();
        public List<Staircase> stairs = new List<Staircase>();
        public List<RoomDescription> roomDescriptions = new List<RoomDescription>();
        public List<Recharger> Rechargers = new List<Recharger>();
        public List<ArtificialWall> ArtificialWalls = new List<ArtificialWall>();
        public List<Door> Doors = new List<Door>();

        int floor = 1;
        Player thisPlayer;
        SpriteBatch spriteBatch;
        bool init;

        public MapBuilder(Game1 useForLoadingContent, int screenWidth, int screenHeight, GraphicsDeviceManager graphics, int tileSize, Player thisPlayer, SpriteBatch spriteBatch, Dictionary<long, Player> players)
        {
            this.useForLoadingContent = useForLoadingContent;
            this.screenWidth = screenWidth;
            this.screenHeight = screenHeight;
            this.graphics = graphics;
            this.tileSize = tileSize;
            this.thisPlayer = thisPlayer;
            this.spriteBatch = spriteBatch;
            this.players = players;
        }
        public void BuildMap(string mapName)
        {
            mapTexture = useForLoadingContent.Content.Load<Texture2D>("Maps/" + mapName);

            foregroundContour = TextureTo2DArray(mapTexture);
            foregroundContourWFurniture = TextureTo2DArray(mapTexture);
            PlaceTiles();

            gridTexture = BuildContourTexture(0, 0);

            PlaceFurniture();
            PlaceFloorTiles();
        }

        public Color[,] TextureTo2DArray(Texture2D texture)
        {
            Color[] colors1D = new Color[texture.Width * texture.Height];
            texture.GetData(colors1D);

            Color[,] colors2D = new Color[texture.Width, texture.Height];
            for (int x = 0; x < texture.Width; x++)
                for (int y = 0; y < texture.Height; y++)
                    colors2D[x, y] = colors1D[x + y * texture.Width];

            return colors2D;
        }

        public Texture2D Array2DToTexture(Color[,] array2D)
        {
            Color[] colors1D = new Color[array2D.GetLength(0) * array2D.GetLength(1)];
            for (int x = 0; x < array2D.GetLength(0); x++)
                for (int y = 0; y < array2D.GetLength(1); y++)
                    colors1D[x + y * array2D.GetLength(0)] = array2D[x, y];

            Texture2D texture = new Texture2D(graphics.GraphicsDevice, array2D.GetLength(0), array2D.GetLength(1));
            texture.SetData<Color>(colors1D);
            return texture;
        }


        public int ResidueX { get; set; }
        public int ResidueY { get; set; }
        public Texture2D BuildContourTexture(int startX, int startY)
        {
            gridTexturePlacement = new Point(startX, startY);

            int startXsmall = startX / 20;
            int startYsmall = startY / 20;

            ResidueX = startX % 20;
            ResidueY = startY % 20;


            Color[] colors1D = new Color[screenWidth * screenHeight];
            for (int x = 0; x < (int)((screenWidth) / tileSize); x++)
                for (int y = 0; y < (int)((screenHeight) / tileSize); y++)
                    if (foregroundContour[x + startXsmall, y + startYsmall] == Color.Black)
                    {
                        for (int i = 0; i < 4; i++)
                            for (int j = 0; j < 20; j++)
                                colors1D[20 * x + i + 20 * y * screenWidth + j * screenWidth] = new Color(0, 200, 0, 255);

                        for (int i = 16; i < 20; i++)
                            for (int j = 0; j < 20; j++)
                                colors1D[20 * x + i + 20 * y * screenWidth + j * screenWidth] = new Color(0, 200, 0, 255);


                        for (int i = 0; i < 20; i++)
                            for (int j = 0; j < 4; j++)
                                colors1D[20 * x + i + 20 * y * screenWidth + j * screenWidth] = new Color(0, 200, 0, 255);

                        for (int i = 0; i < 20; i++)
                            for (int j = 16; j < 20; j++)
                                colors1D[20 * x + i + 20 * y * screenWidth + j * screenWidth] = new Color(0, 200, 0, 255);

                    }

            Texture2D texture = new Texture2D(graphics.GraphicsDevice, screenWidth, screenHeight);
            texture.SetData<Color>(colors1D);

            return texture;
        }

        public void PlaceTiles()
        {
            walls.Clear();

            for (int i = 0; i < foregroundContour.GetLength(0); i++)
                for (int j = 0; j < foregroundContour.GetLength(1); j++)
                    if (foregroundContour[i, j] == Color.Black)
                        walls.Add(new Point(i * 20, j * 20));
        }

        public void PlaceFloorTiles()
        {
            floorTiles.Clear();

            tileTexture = useForLoadingContent.Content.Load<Texture2D>("Tiles/stones");

            for (int i = 0; i < foregroundContour.GetLength(0); i++)
            {
                for (int j = 0; j < foregroundContour.GetLength(1); j++)
                {
                    if (foregroundContour[i, j].A > 254 && foregroundContour[i, j] != Color.Black &&
                        !floorTiles.Any(t => t.Rect.Contains(i * 20, j * 20)))
                    {
                        int width = 0;
                        int height = 0;
                        while (width + i < foregroundContour.GetLength(0))
                        {
                            if (foregroundContour[i, j] != foregroundContour[i + width, j])
                                break;
                            width++;
                        }
                        while (height + j < foregroundContour.GetLength(1))
                        {
                            if (foregroundContour[i, j] != foregroundContour[i, j + height])
                            {
                                FloorTile tile;
                                try
                                {
                                    tile = new FloorTile { Rect = new Rectangle(i * 20, j * 20, width * 20, height * 20), Texture = tilesMap[foregroundContour[i, j]] };
                                }
                                catch
                                {
                                    tile = new FloorTile { Rect = new Rectangle(i * 20, j * 20, width * 20, height * 20), Texture = useForLoadingContent.Content.Load<Texture2D>("tiles/wood1") };
                                }
                                floorTiles.Add(tile);
                                break;
                            }

                            height++;
                        }
                    }
                }
            }

        }

        public void PlaceFurniture()
        {
            furnitures.Clear();

            XDocument doc = XDocument.Load("../../../Furniture.xml");

            foreach (var f in doc.Elements("items").Elements("furnitures").Elements("floor").Where(f => f.Attribute("level").Value == floor.ToString()).Elements())
            {
                FloorTile furn = new FloorTile();
                furn.Rect = new Rectangle(int.Parse(f.Attribute("x").Value) * 20, int.Parse(f.Attribute("y").Value) * 20,
                    int.Parse(f.Attribute("width").Value) * 20, int.Parse(f.Attribute("height").Value) * 20);
                furn.Texture = useForLoadingContent.Content.Load<Texture2D>("MapItems/" + f.Attribute("name").Value);

                furnitures.Add(furn);

                if (f.Attribute("canpassthrough") == null || !bool.Parse(f.Attribute("canpassthrough").Value))
                    for (int i = int.Parse(f.Attribute("x").Value); i < int.Parse(f.Attribute("x").Value) + int.Parse(f.Attribute("width").Value); i++)
                        for (int j = int.Parse(f.Attribute("y").Value); j < int.Parse(f.Attribute("y").Value) + int.Parse(f.Attribute("height").Value); j++)
                            foregroundContourWFurniture[i, j] = Color.Black;
            }

            stairs.Clear();

            foreach (var f in doc.Elements("items").Elements("stairs").Elements())
            {
                if (int.Parse(f.Attribute("floor").Value) == floor)
                {
                    Staircase staircase = new Staircase();
                    staircase.BoundingBox = new Rectangle(int.Parse(f.Attribute("x").Value) * 20, int.Parse(f.Attribute("y").Value) * 20,
                        int.Parse(f.Attribute("width").Value) * 20, int.Parse(f.Attribute("height").Value) * 20);
                    staircase.Texture = useForLoadingContent.Content.Load<Texture2D>("MapItems/" + f.Attribute("name").Value);
                    staircase.Floor = int.Parse(f.Attribute("floor").Value);
                    staircase.ToFloor = int.Parse(f.Attribute("toFloor").Value);
                    stairs.Add(staircase);
                }
            }

            roomDescriptions.Clear();

            foreach (var f in doc.Elements("items").Elements("roomdescriptions").Elements())
            {
                if (int.Parse(f.Attribute("floor").Value) == floor)
                {
                    RoomDescription roomDescription = new RoomDescription();
                    roomDescription.BoundingBox = new Rectangle(int.Parse(f.Attribute("x").Value) * 20, int.Parse(f.Attribute("y").Value) * 20,
                        int.Parse(f.Attribute("width").Value) * 20, int.Parse(f.Attribute("height").Value) * 20);
                    roomDescription.Description = f.Attribute("description").Value;
                    roomDescriptions.Add(roomDescription);
                }
            }

            foreach (var f in doc.Elements("items").Elements("floortiles").Elements())
                tilesMap[new Color(int.Parse(f.Attribute("r").Value), int.Parse(f.Attribute("g").Value), int.Parse(f.Attribute("b").Value))] = useForLoadingContent.Content.Load<Texture2D>("tiles/" + f.Attribute("name").Value);

            weapons.Clear();

            foreach (var f in doc.Elements("items").Elements("placedweapons").Elements("floor").Where(f => f.Attribute("level").Value == floor.ToString()).Elements())
            {
                Weapon weaponDefinition = useForLoadingContent.weaponDefinitions.SingleOrDefault(w => w.Name == f.Attribute("name").Value);
                Weapon weapon = weaponDefinition.Clone();

                weapon.Position = new Vector2(int.Parse(f.Attribute("x").Value), int.Parse(f.Attribute("y").Value));
                weapons.Add(weapon);
            }

            foreach (var f in doc.Elements("items").Elements("placedweapons").Elements(thisPlayer.PlayerType.ToString()).Elements())
            {
                Weapon weaponDefinition = useForLoadingContent.weaponDefinitions.SingleOrDefault(w => w.Name == f.Attribute("name").Value);
                Weapon weapon = weaponDefinition.Clone();
                thisPlayer.AddWeapon(weapon);
            }
            thisPlayer.Score = 0;

            Npcs.Clear();
            foreach (var f in doc.Elements("items").Elements("npcs").Elements("floor").Where(f => f.Attribute("level").Value == floor.ToString()).Elements())
            {
                Npc npc = new Npc(
                new AnimatedTexture(useForLoadingContent.Content.Load<Texture2D>("PlayerSprites/zombiespritesheet"), 4,2,true,8, spriteBatch, 50,50,new Vector2(25,25), null),
                new AnimatedTexture(useForLoadingContent.Content.Load<Texture2D>("PlayerSprites/zombiedeath"), 4, 2, false, 16, spriteBatch, 50, 50, new Vector2(25, 25), null),
                players, thisPlayer, new Vector2(int.Parse(f.Attribute("x").Value), int.Parse(f.Attribute("y").Value)), 50, 10, 10);

                Npcs.Add(npc);
            }

            Rechargers.Clear();
            foreach (var f in doc.Elements("items").Elements("rechargers").Elements("floor").Where(f => f.Attribute("level").Value == floor.ToString()).Elements())
            {
                Recharger recharger = new Recharger
                {
                    Power = int.Parse(f.Attribute("power").Value),
                    Type = f.Attribute("type").Value,
                    Position = new Vector2(int.Parse(f.Attribute("x").Value), int.Parse(f.Attribute("y").Value)),
                    Texture = useForLoadingContent.Content.Load<Texture2D>("MapItems/" + f.Attribute("type").Value + "ammo")
                };

                Rechargers.Add(recharger);
            }

            Doors.Clear();
            foreach (var f in doc.Elements("items").Elements("doors").Elements("floor").Where(f => f.Attribute("level").Value == floor.ToString()).Elements())
            {
                Door door = new Door
                {
                    IsOpen = bool.Parse(f.Attribute("isopen").Value),
                    Position = new Vector2(int.Parse(f.Attribute("x").Value), int.Parse(f.Attribute("y").Value)),
                    Texture = useForLoadingContent.Content.Load<Texture2D>("MapItems/door")
                };

                Doors.Add(door);
            }

            UpdateForegroundContour(0, 0);
        }

        public bool standsOnStaircase;
        public void LoadMap(int floor)
        {
            if (!standsOnStaircase)
            {
                this.floor = floor;
                BuildMap("map" + floor);
                standsOnStaircase = true;
            }
        }

        public void UpdateForegroundContour(int startX, int startY)
        {
            foreach (var door in Doors)
            {
                for (int i = 0; i < 3; i++)
                {
                    foregroundContour[(int)door.Position.X / 20, (int)door.Position.Y / 20 + i] = door.IsOpen ? Color.White : Color.Black;
                    foregroundContourWFurniture[(int)door.Position.X / 20, (int)door.Position.Y / 20 + i] = door.IsOpen ? Color.White : Color.Black;
                }
            }
            gridTexture = BuildContourTexture(Math.Max((int)startX, 0), Math.Max(0, (int)startY));
        }

    }
}
