﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Robber.Interfaces;
using GWNorthEngine.Model;
using GWNorthEngine.Model.Params;
using GWNorthEngine.Utils;
using GWNorthEngine.AI.AStar;
using GWNorthEngine.Scripting;
using GWNorthEngine.Tools.TilePlacer;
namespace Robber {
	public class GameDisplay : Display{
		#region Class variables
		private Map map;
		private Player player;
		private Person[] guards;
		private Treasure[] treasures;
		private ColouredButton replayButton;
		private ColouredButton startButton;
		private Timer timer;
		private List<Point> entryExitPoints;
		private Text2D gameOverText;
		private StaticDrawable2D winBackGround;
		private StaticDrawable2D looseBackGround;
		private Text2D treasureText;
		private StaticDrawable2D treasure;
		private SoundEffect cantTouchThisSfx;
		private SoundEffect guardDetectedSfx;
		private SoundEffect introSfx;
		private SoundEffect payDaySfx;
		private SoundEffect treasureSfx;
		private float payDayDelay;
		private const float DELAY_PAY_DAY_EMOTE = 5000f;


		private GraphicsDevice device;
		private ContentManager content;
		private DebugUtils debugUtils;
		private Texture2D debugChip;
		private Texture2D debugRing;
		#endregion Class variables

		#region Class propeties
		public bool ShowAI { get; set; }
		#endregion Class properties

		#region Constructor
		public GameDisplay(GraphicsDevice device, ContentManager content) {
			this.device = device;
			this.content = content;
			this.timer = new Timer(content);

			// replay button
			ColouredButtonParams buttonParms = new ColouredButtonParams();
			buttonParms.Font = ResourceManager.getInstance().Font;
			buttonParms.Height = 25;
			buttonParms.LinesTexture = ResourceManager.getInstance().ButtonLineTexture;
			buttonParms.MouseOverColour = ResourceManager.MOUSE_OVER_COLOUR;
			buttonParms.RegularColour = ResourceManager.TEXT_COLOUR;
			buttonParms.StartX = 693;
			buttonParms.Width = 75;

			// play button
			buttonParms.StartY = 557;
			buttonParms.Text = "Replay";
			buttonParms.TextsPosition = new Vector2(698f, buttonParms.StartY - 2f);
			this.replayButton = new ColouredButton(buttonParms);

			// start button
			buttonParms.Text = "Start";
			buttonParms.TextsPosition = new Vector2(704f, buttonParms.StartY - 2f);
			this.startButton = new ColouredButton(buttonParms);

			// game over text
			Text2DParams textParams = new Text2DParams();
			textParams.Font = ResourceManager.getInstance().Font;
			textParams.Position = new Vector2(200f, 50f);
			this.gameOverText = new Text2D(textParams);

			// loosing background
			StaticDrawable2DParams staticParms = new StaticDrawable2DParams();
			staticParms.Position = new Vector2(-10f, 0f);
			staticParms.Texture = content.Load<Texture2D>("BackGround1");
			this.looseBackGround = new StaticDrawable2D(staticParms);

			// winning background
			staticParms.Texture = content.Load<Texture2D>("BackGround2");
			this.winBackGround = new StaticDrawable2D(staticParms);

			// HUD
			Text2DParams textParms = new Text2DParams();
			textParms.Font = ResourceManager.getInstance().Font;
			textParms.LightColour = ResourceManager.TEXT_COLOUR;
			textParms.Position = new Vector2(350f, 575f);
			this.treasureText = new Text2D(textParms);

			staticParms = new StaticDrawable2DParams();
			staticParms.Position = new Vector2(325f, 573f);
			staticParms.Texture = content.Load<Texture2D>("Treasure1");
			this.treasure = new StaticDrawable2D(staticParms);

			// load sound effects
			this.cantTouchThisSfx = content.Load<SoundEffect>("CantTouchThis");
			this.introSfx = content.Load<SoundEffect>("LevelEntry");
			this.payDaySfx = content.Load<SoundEffect>("PayDay");
			this.treasureSfx = content.Load<SoundEffect>("TreasureCollect");
			this.guardDetectedSfx = content.Load<SoundEffect>("Policia");
#if WINDOWS
#if DEBUG
			this.debugUtils = new DebugUtils(TextureUtils.create2DColouredTexture(device, 2, 2, Color.White));
			this.debugChip = TextureUtils.create2DColouredTexture(device, 32, 32, Color.White);
			this.debugRing = TextureUtils.create2DRingTexture(device, 112, Color.White);
#endif
#endif
		}
		#endregion Constructor

		#region Support methods
		public void reset() {
			// we need to stop our guards threads
			if (this.guards != null) {
				foreach (Guard guard in this.guards) {
					guard.RunAIThread = false;
				}
			}
			string mapInformation = Directory.GetCurrentDirectory() + "\\Scripts\\" + StateManager.getInstance().MapInformation;
			CollisionManager.getInstance().MapBoundingBoxes = new List<BoundingBox>();

			StreamReader reader = new StreamReader(mapInformation + "Indentifiers.dat");
			this.entryExitPoints = new List<Point>();
			Point playersLocation = new Point();
			Color floorColour = Color.White;
			Color wallColour = Color.Black;
			List<Point> guardLocations = new List<Point>();
			List<string> guardDirectins = new List<string>();
			List<string> guardStates = new List<string>();
			List<Point> treasureLocations = new List<Point>();
			List<Point> boundingBoxPoints = new List<Point>();
			List<Point> wayPoints = new List<Point>();
			float time = 0f;
			try {
				string[] components = null;
				string[] indexes = null;
				string line = null;
				while (!reader.EndOfStream) {
					line = reader.ReadLine();
					if (!line.StartsWith(@"//")) {
						components = line.Split(ResourceManager.SEPARATOR);
						indexes = components[1].Split(',');
						if (components[0] == ResourceManager.PLAYER_IDENTIFIER) {
							playersLocation = new Point(int.Parse(indexes[0]), int.Parse(indexes[1]));
						} else if (components[0] == ResourceManager.GUARD_IDENTIFIER) {
							guardLocations.Add(new Point(int.Parse(indexes[0]), int.Parse(indexes[1])));
							guardStates.Add(components[2]);
							guardDirectins.Add(components[3]);
						} else if (components[0] == ResourceManager.GUARD_ENTRY_IDENTIFIER) {
							this.entryExitPoints.Add(new Point(int.Parse(indexes[0]), int.Parse(indexes[1])));
						} else if (components[0] == ResourceManager.TREASURE_IDENTIFIER) {
							treasureLocations.Add(new Point(int.Parse(indexes[0]), int.Parse(indexes[1])));
						} else if (components[0] == ResourceManager.BOUNDING_BOX_IDENTIFIER) {
							boundingBoxPoints.Add(new Point(int.Parse(indexes[0]), int.Parse(indexes[1])));
						} else if (components[0] == ResourceManager.WAYPOINT_IDENTIFIER) {
							wayPoints.Add(new Point(int.Parse(indexes[0]), int.Parse(indexes[1])));
						} else if (components[0] == ResourceManager.TIME_INDENTIFIER) {
							time = float.Parse(indexes[0]);
						} else if (components[0] == ResourceManager.COLOUR_IDENTIFIER) {
							floorColour = new Color(int.Parse(indexes[0]), int.Parse(indexes[1]), int.Parse(indexes[2]));
							indexes = components[2].Split(',');
							wallColour = new Color(int.Parse(indexes[0]), int.Parse(indexes[1]), int.Parse(indexes[2]));
						}
					}
				}
			} finally {
				reader.Close();
				reader.Dispose();
			}
			// load our map
			this.map = MapLoader.load(content, mapInformation + Constants.FILE_EXTENSION, floorColour, wallColour);

			// let our AI manager know about the maps way points
			AIManager.getInstane().WayPoints = wayPoints;

			List<BoundingBox> mapBoundingBoxes = new List<BoundingBox>();
			for (int i = 0; i < boundingBoxPoints.Count; i += 2) {
				mapBoundingBoxes.Add(Helper.getBBox(boundingBoxPoints[i], boundingBoxPoints[i + 1]));

			}
			CollisionManager.getInstance().MapBoundingBoxes.AddRange(mapBoundingBoxes);

			// load player at starting point
			this.player = new Player(this.content, new Placement(playersLocation));

			// load treasure at starting points
			int treasureSize = treasureLocations.Count;
			this.treasures = new Treasure[treasureSize];
			string treasureFileName;
			Random random = new Random();
			for (int i = 0; i < treasureSize; i++) {
				treasureFileName = "Treasure" + random.Next(1, 3);
				this.treasures[i] = new Treasure(this.content, treasureFileName, new Placement(treasureLocations[i]));
			}

			// load guard(s) at starting points
			int guardSize = guardLocations.Count;
			this.guards = new Person[guardSize];
			Placement placement;
			for (int i = 0; i < guardSize; i++) {
				placement = new Placement(guardLocations[i]);
				this.guards[i] = new Guard(this.device, this.content, placement, guardStates[i], guardDirectins[i]);
			}
			this.timer.reset(time);
			this.treasureText.WrittenText = "x " + this.player.CapturedTreasures;
		}

		public override void update(float elapsed) {
			base.currentKeyBoardState = Keyboard.GetState();
			base.currentMouseState = Mouse.GetState();
			if (this.player != null) {
				this.treasureText.WrittenText = " x " + this.player.CapturedTreasures;
			}
			if (this.map != null) {
				this.map.update(elapsed);
			}
			Vector2 mousePos = new Vector2(base.currentMouseState.X, base.currentMouseState.Y);
			if (StateManager.getInstance().CurrentGameState == StateManager.GameState.Waiting) {
				this.startButton.processActorsMovement(mousePos);
				// mouse over sfx
				if (this.startButton.isActorOver(mousePos)) {
					if (!base.previousMouseOverButton) {
						if (ResourceManager.PLAY_SOUND) {
							ResourceManager.getInstance().MouseOverSfx.Play();
						}
					}
					base.previousMouseOverButton = true;
				} else {
					base.previousMouseOverButton = false;
				}
				if (base.currentMouseState.LeftButton == ButtonState.Pressed && base.prevousMouseState.LeftButton == ButtonState.Released) {
					if (this.startButton.isActorOver(mousePos)) {
						StateManager.getInstance().CurrentGameState = StateManager.GameState.Active;
					}
				}
			} else if (StateManager.getInstance().CurrentGameState == StateManager.GameState.Active) {
				// check if the player won
				foreach (Point point in this.entryExitPoints) {
					if (point == this.player.Placement.index) {
						StateManager.getInstance().CurrentGameState = StateManager.GameState.GameOver;
						if (this.player.CapturedTreasures >= 1) {
							StateManager.getInstance().TypeOfGameOver = StateManager.GameOverType.Player;
							if (ResourceManager.PLAY_SOUND) {
								this.cantTouchThisSfx.Play();
							}
						} else {
							StateManager.getInstance().TypeOfGameOver = StateManager.GameOverType.None;
						}
						break;
					}
				}
				this.timer.update(elapsed);
				this.player.update(elapsed);
				foreach (Guard guard in this.guards) {
					guard.update(elapsed);
					if (guard.BoundingBox.Intersects(this.player.BoundingBox)) {
						StateManager.getInstance().CurrentGameState = StateManager.GameState.GameOver;
						StateManager.getInstance().TypeOfGameOver = StateManager.GameOverType.Guards;
						if (ResourceManager.PLAY_SOUND) {
							ResourceManager.getInstance().PrisonCellSfx.Play();
						}
						break;
					} else if (guard.Ring.BoundingSphere.Intersects(this.player.BoundingBox)) {
						// did we JUST get detected?
						if (!AIManager.getInstane().PlayerDetected) {
							if (ResourceManager.PLAY_SOUND) {
								this.guardDetectedSfx.Play();
							}
						}
						AIManager.getInstane().PlayerDetected = true;
					}
					
				}
				foreach (Treasure treasure in this.treasures) {
					treasure.update(elapsed);
					if (treasure.BoundingBox.Intersects(this.player.BoundingBox)) {
						treasure.PickedUp = true;
						this.player.CapturedTreasures++;
						if (ResourceManager.PLAY_SOUND) {
							if (this.payDayDelay >= DELAY_PAY_DAY_EMOTE) {
								this.payDaySfx.Play();
								this.payDayDelay = 0f;
							}
							this.treasureSfx.Play(.5f, 0f, 0f);
						}
						break;
					}
				}
			} else if (StateManager.getInstance().CurrentGameState == StateManager.GameState.GameOver) {
				this.replayButton.processActorsMovement(mousePos);
				// mouse over sfx
				if (this.replayButton.isActorOver(mousePos)) {
					if (!base.previousMouseOverButton) {
						if (ResourceManager.PLAY_SOUND) {
							ResourceManager.getInstance().MouseOverSfx.Play();
						}
					}
					base.previousMouseOverButton = true;
				} else {
					base.previousMouseOverButton = false;
				}
				if (base.currentMouseState.LeftButton == ButtonState.Pressed && base.prevousMouseState.LeftButton == ButtonState.Released) {
					if (this.replayButton.isActorOver(mousePos)) {
						StateManager.getInstance().CurrentGameState = StateManager.GameState.Reset;
						StateManager.getInstance().CurrentTransitionState = StateManager.TransitionState.TransitionOut;
						reset();
						Console.WriteLine("Reset");
					}
				}
				if (StateManager.getInstance().TypeOfGameOver == StateManager.GameOverType.Guards) {
					this.gameOverText.Position = new Vector2(200f, this.gameOverText.Position.Y);
					this.gameOverText.WrittenText = "You loose; you have to be quick";
				} else if (StateManager.getInstance().TypeOfGameOver == StateManager.GameOverType.Player) {
					this.gameOverText.Position = new Vector2(125f, this.gameOverText.Position.Y);
					this.gameOverText.WrittenText = "Congratulations you won, you score is " + this.player.CapturedTreasures * 100 + this.timer.Time;
				} else if (StateManager.getInstance().TypeOfGameOver == StateManager.GameOverType.None) {
					this.gameOverText.Position = new Vector2(75f, this.gameOverText.Position.Y);
					this.gameOverText.WrittenText = "You loose, you need to capture at least 1 piece of treasure";
				}
			}

			// Transitions
			if (StateManager.getInstance().CurrentTransitionState == StateManager.TransitionState.TransitionIn) {
				Color colour = base.fadeIn(Color.White);
				this.looseBackGround.LightColour = colour;
				this.winBackGround.LightColour = colour;
				this.gameOverText.LightColour = colour;
				this.map.updateColours(base.fadeIn(this.map.FloorColour), base.fadeIn(this.map.WallColour));
				foreach (Treasure treasure in this.treasures) {
					treasure.updateColours(colour);
				}
				this.player.updateColours(colour);
				foreach (Guard guard in this.guards) {
					guard.updateColours(colour);
				}
				this.timer.updateColours(base.fadeIn(ResourceManager.TEXT_COLOUR));
				this.startButton.updateColours(base.fadeIn(ResourceManager.TEXT_COLOUR));
				this.replayButton.updateColours(base.fadeIn(ResourceManager.TEXT_COLOUR));
				if (this.startButton.isActorOver(mousePos)) {
					this.startButton.updateColours(base.fadeIn(ResourceManager.MOUSE_OVER_COLOUR));
				} else if (this.replayButton.isActorOver(mousePos)) {
					this.replayButton.updateColours(base.fadeIn(ResourceManager.MOUSE_OVER_COLOUR));
				}
			} else if (StateManager.getInstance().CurrentTransitionState == StateManager.TransitionState.TransitionOut) {
				Color colour = base.fadeOut(Color.White);
				this.looseBackGround.LightColour = colour;
				this.winBackGround.LightColour = colour;
				this.gameOverText.LightColour = colour;
				this.map.updateColours(base.fadeOut(this.map.FloorColour), base.fadeOut(this.map.WallColour));
				foreach (Treasure treasure in this.treasures) {
					treasure.updateColours(colour);
				}
				this.player.updateColours(colour);
				foreach (Guard guard in this.guards) {
					guard.updateColours(colour);
				}
				this.timer.updateColours(base.fadeOut(ResourceManager.TEXT_COLOUR));

				this.startButton.updateColours(base.fadeOut(ResourceManager.TEXT_COLOUR));
				this.replayButton.updateColours(base.fadeOut(ResourceManager.TEXT_COLOUR));
				if (this.startButton.isActorOver(mousePos)) {
					this.startButton.updateColours(base.fadeOut(ResourceManager.MOUSE_OVER_COLOUR));
				} else if (this.replayButton.isActorOver(mousePos)) {
					this.replayButton.updateColours(base.fadeOut(ResourceManager.MOUSE_OVER_COLOUR));
				}
			}

			// if our transition is up
			if (base.transitionTimeElapsed()) {
				if (StateManager.getInstance().CurrentTransitionState == StateManager.TransitionState.TransitionIn) {
					StateManager.getInstance().CurrentTransitionState = StateManager.TransitionState.None;
					if (StateManager.getInstance().PreviousGameState == StateManager.GameState.MapSelection) {
						if (ResourceManager.PLAY_SOUND) {
							this.introSfx.Play();
						}
					}
				} else if (StateManager.getInstance().CurrentTransitionState == StateManager.TransitionState.TransitionOut) {
					if (StateManager.getInstance().CurrentGameState == StateManager.GameState.Reset) {
						//reset();
						StateManager.getInstance().CurrentGameState = StateManager.GameState.Waiting;
					}
					StateManager.getInstance().CurrentTransitionState = StateManager.TransitionState.TransitionIn;
				}
			}

			if (StateManager.getInstance().CurrentTransitionState == StateManager.TransitionState.None &&
				StateManager.getInstance().CurrentGameState == StateManager.GameState.Waiting ||
				StateManager.getInstance().CurrentGameState == StateManager.GameState.Active) {
				if (base.currentKeyBoardState.IsKeyDown(Keys.Escape) && base.previousKeyBoardState.IsKeyUp(Keys.Escape)) {
					StateManager.getInstance().CurrentGameState = StateManager.GameState.InGameMenu;
					StateManager.getInstance().CurrentTransitionState = StateManager.TransitionState.TransitionOut;
				}
			}
			this.payDayDelay += elapsed;
#if DEBUG
			if (base.currentKeyBoardState.IsKeyDown(Keys.D1) && base.previousKeyBoardState.IsKeyUp(Keys.D1)) {
				if (ShowAI) {
					ShowAI = false;
				} else {
					ShowAI = true;
				}
			} else if (base.currentKeyBoardState.IsKeyDown(Keys.D2) && base.previousKeyBoardState.IsKeyUp(Keys.D2)) {
				Console.Clear();
			}
#endif
			base.update(elapsed);
		}

		public override void render(SpriteBatch spriteBatch) {
			if (StateManager.getInstance().CurrentGameState == StateManager.GameState.Active ||
				StateManager.getInstance().CurrentGameState == StateManager.GameState.Waiting || (
				StateManager.getInstance().CurrentGameState == StateManager.GameState.InGameMenu && 
				StateManager.getInstance().CurrentTransitionState == StateManager.TransitionState.TransitionOut) ||
				(StateManager.getInstance().CurrentGameState == StateManager.GameState.GameOver &&
				StateManager.getInstance().CurrentTransitionState == StateManager.TransitionState.TransitionOut)) {
				this.map.render(spriteBatch);
				this.treasureText.render(spriteBatch);
				this.treasure.render(spriteBatch);
				foreach (Treasure treasure in this.treasures) {
					treasure.render(spriteBatch);
				}
				this.player.render(spriteBatch);
				foreach (Guard guard in this.guards) {
					guard.render(spriteBatch);
				}
				this.timer.render(spriteBatch);
				if (StateManager.getInstance().CurrentGameState == StateManager.GameState.Waiting ||
					(StateManager.getInstance().CurrentGameState == StateManager.GameState.InGameMenu &&
					StateManager.getInstance().CurrentTransitionState == StateManager.TransitionState.TransitionOut) &&
					StateManager.getInstance().PreviousGameState == StateManager.GameState.Waiting) {
					this.startButton.render(spriteBatch);
				}
			} else if (StateManager.getInstance().CurrentGameState == StateManager.GameState.GameOver ||
				StateManager.getInstance().CurrentGameState == StateManager.GameState.Reset &&
				StateManager.getInstance().CurrentTransitionState == StateManager.TransitionState.TransitionOut) {
				if (StateManager.getInstance().TypeOfGameOver == StateManager.GameOverType.Guards ||
					StateManager.getInstance().TypeOfGameOver == StateManager.GameOverType.None) {
					this.looseBackGround.render(spriteBatch);
				} else {
					if (this.winBackGround != null) {
						this.winBackGround.render(spriteBatch);
					}
				}
				this.replayButton.render(spriteBatch);
				this.gameOverText.render(spriteBatch);
			}
#if DEBUG
			if (this.ShowAI) {
				Color debugColour = Color.Green;
				this.debugUtils.drawBoundingBox(spriteBatch, this.player.BoundingBox, debugColour);
				foreach (Guard guard in this.guards) {
					this.debugUtils.drawBoundingBox(spriteBatch, guard.BoundingBox,debugColour);
					DebugUtils.drawBoundingSphere(spriteBatch, guard.Ring.BoundingSphere, debugColour, this.debugRing);
				}
				foreach (BoundingBox box in CollisionManager.getInstance().MapBoundingBoxes) {
					this.debugUtils.drawBoundingBox(spriteBatch, box, debugColour);
				}
				foreach (BoundingBox box in CollisionManager.getInstance().FloorCenterBoxes) {
					this.debugUtils.drawBoundingBox(spriteBatch, box, debugColour);
				}
				foreach (Treasure treasure in this.treasures) {
					this.debugUtils.drawBoundingBox(spriteBatch, treasure.BoundingBox, debugColour);
				}
				for (int y = 0; y < 18; y++) {
					for (int x = 0; x < 21; x++) {
						if (AIManager.getInstane().Board[y, x] == PathFinder.TypeOfSpace.Unwalkable) {
							spriteBatch.Draw(this.debugChip, new Placement(new Point(x, y)).worldPosition, Color.Red);
						}
					}
				}
			}
#endif
		}
		#endregion Support methods

		#region Destructor
		public override void dispose() {
			if (this.map != null) {
				this.map.dispose();
			}
			if (this.player != null) {
				this.player.dispose();
			}
			if (this.guards != null) {
				foreach (Guard guard in this.guards) {
					guard.dispose();
				}
			}
			if (this.treasures != null) {
				foreach (Treasure treasure in this.treasures) {
					treasure.dispose();
				}
			}
			if (this.replayButton != null) {
				this.replayButton.dispose();
			}
			if (this.looseBackGround !=null) {
				this.looseBackGround.dispose();
			}
			if (this.winBackGround != null) {
				this.winBackGround.dispose();
			}
			if (this.treasure != null) {
				this.treasure.dispose();
			}
			this.timer.dispose();

			// sfxs
			if (this.cantTouchThisSfx != null) {
				this.cantTouchThisSfx.Dispose();
			}
			if (this.introSfx != null) {
				this.introSfx.Dispose();
			}
			if (this.payDaySfx != null) {
				this.payDaySfx.Dispose();
			}
			if (this.treasureSfx != null) {
				this.treasureSfx.Dispose();
			}
			if (this.guardDetectedSfx != null) {
				this.guardDetectedSfx.Dispose();
			}
#if DEBUG
			this.debugChip.Dispose();
			this.debugRing.Dispose();
#endif
		}
		#endregion Destructor
	}
}