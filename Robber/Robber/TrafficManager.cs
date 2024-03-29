using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Input;
using GWNorthEngine.Engine;
using GWNorthEngine.Engine.Params;
using GWNorthEngine.Scripting;
using GWNorthEngine.Input;
namespace Robber {
	/// <summary>
	/// This is the main type for your game
	/// </summary>
	public class TrafficManager : BaseRenderer {

		private Display mainMenu;
		private Display modeSelectMenu;
		private Display mapSelectionMenu;
		private Display inGameMenu;
		private Display instructionMenu;
		private Display gameDisplay;
		private Display gameOverDisplay;
		private Display activeDisplay;

		public TrafficManager() {
			BaseRendererParams baseParms = new BaseRendererParams();
			baseParms.MouseVisible = true;
			baseParms.WindowsText = "Rob, The Dumbass Robber";
#if DEBUG
			baseParms.RunningMode = RunningMode.Debug;
#else
			baseParms.RunningMode = RunningMode.Release;
#endif
			base.initialize(baseParms);
		}

		/// <summary>
		/// LoadContent will be called once per game and is the place to load
		/// all of your content.
		/// </summary>
		protected override void LoadContent() {
#if WINDOWS
#if DEBUG
			ScriptManager.getInstance().LogFile = "Log.log";
#endif
#endif
			ResourceManager.getInstance().init(GraphicsDevice, Content);
			this.mainMenu = new MainMenu(Content);
			this.modeSelectMenu = new ModeSelectMenu(Content);
			this.inGameMenu = new InGameMenu(Content);
			this.instructionMenu = new InstructionsMenu(Content);
			this.gameDisplay = new GameDisplay(Content);
			this.mapSelectionMenu = new MapSeletMenu(GraphicsDevice, Content);
			this.gameOverDisplay = new GameOverMenu(Content, 
				delegate() {
					return ((GameDisplay)this.gameDisplay).Score;
				}, delegate() {
					((GameDisplay)this.gameDisplay).reset();
				}
			);
		}

		/// <summary>
		/// UnloadContent will be called once per game and is the place to unload
		/// all content.
		/// </summary>
		protected override void UnloadContent() {
			this.gameDisplay.dispose();
			this.mainMenu.dispose();
			this.modeSelectMenu.dispose();
			this.inGameMenu.dispose();
			this.instructionMenu.dispose();
			this.mapSelectionMenu.dispose();
			this.gameOverDisplay.dispose();
			this.activeDisplay.dispose();
			SoundManager.getInstance().dispose();
			AIManager.getInstance().dispose();
			ResourceManager.getInstance().dispose();
			base.UnloadContent();
		}

		/// <summary>
		/// Allows the game to run logic such as updating the world,
		/// checking for collisions, gathering input, and playing audio.
		/// </summary>
		/// <param name="gameTime">Provides a snapshot of timing values.</param>
		protected override void Update(GameTime gameTime) {
#if DEBUG
			base.Window.Title = "Rob, The Dumb Ass Robber...FPS: " + FrameRate.getInstance().calculateFrameRate(gameTime) + "    X:" + 
				InputManager.getInstance().MouseX + " Y:" + InputManager.getInstance().MouseY;
#endif

			// Allows the game to exit
			if (StateManager.getInstance().CurrentGameState == StateManager.GameState.MainMenu) {
				if (InputManager.getInstance().wasKeyPressed(Keys.Escape) ||
						InputManager.getInstance().wasButtonPressed(PlayerIndex.One, Buttons.B)) {
					this.Exit();
				}
			} else if (StateManager.getInstance().CurrentGameState == StateManager.GameState.Exit) {
				this.Exit();
			}

			// Transition code
			#region Transitions
			if (StateManager.getInstance().CurrentGameState == StateManager.GameState.MainMenu) {
				if (StateManager.getInstance().PreviousGameState == StateManager.GameState.MainMenu &&
					StateManager.getInstance().CurrentTransitionState == StateManager.TransitionState.TransitionOut) {
					this.activeDisplay = this.mainMenu;
				} else if (StateManager.getInstance().PreviousGameState == StateManager.GameState.InGameMenu &&
					StateManager.getInstance().CurrentTransitionState == StateManager.TransitionState.TransitionIn) {
					this.activeDisplay = this.mainMenu;
				} else if (StateManager.getInstance().PreviousGameState == StateManager.GameState.InGameMenu &&
					StateManager.getInstance().CurrentTransitionState == StateManager.TransitionState.TransitionOut) {
					this.activeDisplay = this.inGameMenu;
				} else if (StateManager.getInstance().PreviousGameState == StateManager.GameState.ModeSelect &&
					StateManager.getInstance().CurrentTransitionState == StateManager.TransitionState.TransitionOut) {
					this.activeDisplay = this.modeSelectMenu;
				} else if (StateManager.getInstance().PreviousGameState == StateManager.GameState.Instructions &&
					StateManager.getInstance().CurrentTransitionState == StateManager.TransitionState.TransitionOut) {
					this.activeDisplay = this.instructionMenu;
				} else {
					this.activeDisplay = this.mainMenu;
				}
			} else if (StateManager.getInstance().CurrentGameState == StateManager.GameState.Instructions) {
				if (StateManager.getInstance().PreviousGameState == StateManager.GameState.MainMenu &&
					StateManager.getInstance().CurrentTransitionState == StateManager.TransitionState.TransitionOut) {
					this.activeDisplay = this.mainMenu;
				} else {
					this.activeDisplay = this.instructionMenu;
				}
			} else if (StateManager.getInstance().CurrentGameState == StateManager.GameState.ModeSelect) {
				if (StateManager.getInstance().PreviousGameState == StateManager.GameState.MainMenu &&
					StateManager.getInstance().CurrentTransitionState == StateManager.TransitionState.TransitionOut) {
					this.activeDisplay = this.mainMenu;
				} else if (StateManager.getInstance().PreviousGameState == StateManager.GameState.MapSelection &&
					 StateManager.getInstance().CurrentTransitionState == StateManager.TransitionState.TransitionOut) {
					this.activeDisplay = this.mapSelectionMenu;
				} else {
					this.activeDisplay = this.modeSelectMenu;
				}
			} else if (StateManager.getInstance().CurrentGameState == StateManager.GameState.Waiting ||
				StateManager.getInstance().CurrentGameState == StateManager.GameState.Active) {
				if (StateManager.getInstance().PreviousGameState == StateManager.GameState.MapSelection &&
					StateManager.getInstance().CurrentTransitionState == StateManager.TransitionState.TransitionIn) {
					this.activeDisplay = this.gameDisplay;
				} else if (StateManager.getInstance().PreviousGameState == StateManager.GameState.MapSelection &&
					StateManager.getInstance().CurrentTransitionState == StateManager.TransitionState.TransitionOut) {
					this.activeDisplay = this.mapSelectionMenu;
				} else if (StateManager.getInstance().PreviousGameState == StateManager.GameState.Active &&
					StateManager.getInstance().CurrentTransitionState == StateManager.TransitionState.TransitionOut) {
					this.activeDisplay = this.mainMenu;
				} else if (StateManager.getInstance().PreviousGameState == StateManager.GameState.Active &&
					StateManager.getInstance().CurrentTransitionState == StateManager.TransitionState.TransitionIn) {
					this.activeDisplay = this.gameDisplay;
				} else if (StateManager.getInstance().PreviousGameState == StateManager.GameState.InGameMenu &&
					StateManager.getInstance().CurrentTransitionState == StateManager.TransitionState.TransitionIn) {
					this.activeDisplay = gameDisplay;
				} else if (StateManager.getInstance().PreviousGameState == StateManager.GameState.InGameMenu &&
					StateManager.getInstance().CurrentTransitionState == StateManager.TransitionState.TransitionOut) {
					this.activeDisplay = this.inGameMenu;
				} else {
					this.activeDisplay = this.gameDisplay;
				}
			} else if (StateManager.getInstance().CurrentGameState == StateManager.GameState.InGameMenu) {
				if (StateManager.getInstance().PreviousGameState == StateManager.GameState.Active &&
					StateManager.getInstance().CurrentTransitionState == StateManager.TransitionState.TransitionOut) {
					this.activeDisplay = this.gameDisplay;
				} else if (StateManager.getInstance().PreviousGameState == StateManager.GameState.Active &&
					StateManager.getInstance().CurrentTransitionState == StateManager.TransitionState.TransitionIn) {
					this.activeDisplay = this.inGameMenu;
				} else if (StateManager.getInstance().PreviousGameState == StateManager.GameState.Waiting &&
					StateManager.getInstance().CurrentTransitionState == StateManager.TransitionState.TransitionOut) {
					this.activeDisplay = this.gameDisplay;
				} else if (StateManager.getInstance().PreviousGameState == StateManager.GameState.Waiting &&
					StateManager.getInstance().CurrentTransitionState == StateManager.TransitionState.TransitionIn) {
					this.activeDisplay = this.inGameMenu;
				} else if (StateManager.getInstance().PreviousGameState == StateManager.GameState.GameOver &&
					StateManager.getInstance().CurrentTransitionState == StateManager.TransitionState.TransitionIn) {
					this.activeDisplay = this.gameDisplay;
				} else if (StateManager.getInstance().PreviousGameState == StateManager.GameState.GameOver &&
					StateManager.getInstance().CurrentTransitionState == StateManager.TransitionState.TransitionOut) {
					this.activeDisplay = this.gameDisplay;
				} else {
					this.activeDisplay = this.inGameMenu;
				}
			} else if (StateManager.getInstance().CurrentGameState == StateManager.GameState.MapSelection) {
				if (StateManager.getInstance().PreviousGameState == StateManager.GameState.ModeSelect &&
					StateManager.getInstance().CurrentTransitionState == StateManager.TransitionState.TransitionOut) {
					this.activeDisplay = this.modeSelectMenu;
				} else if (StateManager.getInstance().PreviousGameState == StateManager.GameState.GameOver &&
					StateManager.getInstance().CurrentTransitionState == StateManager.TransitionState.TransitionOut) {
						this.activeDisplay = this.gameOverDisplay;
				} else {
					this.activeDisplay = this.mapSelectionMenu;
				}
			} else if (StateManager.getInstance().CurrentGameState == StateManager.GameState.GameOver) {
				if (StateManager.getInstance().PreviousGameState == StateManager.GameState.Active &&
					StateManager.getInstance().CurrentTransitionState == StateManager.TransitionState.TransitionOut) {
						this.activeDisplay = this.gameDisplay;
				} else {
					this.activeDisplay = this.gameOverDisplay;
				}
			} else if (StateManager.getInstance().CurrentGameState == StateManager.GameState.Reset) {
				if (StateManager.getInstance().PreviousGameState == StateManager.GameState.GameOver &&
					StateManager.getInstance().CurrentTransitionState == StateManager.TransitionState.TransitionOut) {
					this.activeDisplay = this.gameOverDisplay;
				} else {
					this.activeDisplay = this.gameDisplay;
				}
			} else {
				if (StateManager.getInstance().CurrentGameState == StateManager.GameState.InitGame) {
					this.activeDisplay = this.mapSelectionMenu;
					((GameDisplay)this.gameDisplay).reset();
					StateManager.getInstance().CurrentGameState = StateManager.GameState.Waiting;
					StateManager.getInstance().PreviousGameState = StateManager.GameState.MapSelection;
				} else {
					this.activeDisplay = this.gameDisplay;
				}
			}
			#endregion Transitions

			float elapsed = gameTime.ElapsedGameTime.Milliseconds;
			this.activeDisplay.update(elapsed);
			SoundManager.getInstance().update();
			base.Update(gameTime);
		}

		/// <summary>
		/// This is called when the game should draw itself.
		/// </summary>
		/// <param name="gameTime">Provides a snapshot of timing values.</param>
		protected override void Draw(GameTime gameTime) {
			GraphicsDevice.Clear(Color.Black);

			base.spriteBatch.Begin();
			this.activeDisplay.render(base.spriteBatch);
			base.spriteBatch.End();
			base.Draw(gameTime);
		}
	}
}

