﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
namespace Robber {
	public class MainMenu : Display {
		#region Class variables
		private Button playButton;
		private Button exitButton;
		private StaticDrawable2D backGround;
		private StaticDrawable2D title;
		#endregion Class variables

		#region Class propeties

		#endregion Class properties

		#region Constructor
		public MainMenu(ContentManager content) {
			ColouredButtonParams buttonParms = new ColouredButtonParams();
			buttonParms.Font = ResourceManager.getInstance().Font;
			buttonParms.Height = 25;
			buttonParms.LinesTexture = ResourceManager.getInstance().ButtonLineTexture;
			buttonParms.MouseOverColour = ResourceManager.MOUSE_OVER_COLOUR;
			buttonParms.RegularColour = ResourceManager.TEXT_COLOUR;
			buttonParms.StartX = 693;
			buttonParms.Width = 75;

			// play button
			buttonParms.StartY = 515;
			buttonParms.Text = "Play";
			buttonParms.TextsPosition = new Vector2(710f, buttonParms.StartY - 2f);
			this.playButton = new ColouredButton(buttonParms);

			// exit button
			buttonParms.StartY = 557;
			buttonParms.Text = "Exit";
			buttonParms.TextsPosition = new Vector2(710f, buttonParms.StartY - 2);
			this.exitButton = new ColouredButton(buttonParms);
			
			// title
			StaticDrawable2DParams staticParms = new StaticDrawable2DParams();
			staticParms.Position = new Vector2(140f, 0f);
			staticParms.Texture = ResourceManager.getInstance().TitleTexture;
			this.title = new StaticDrawable2D(staticParms);

			// background
			staticParms.Position = new Vector2(0f, 0f);
			staticParms.Texture = content.Load<Texture2D>("BackGround1");
			this.backGround = new StaticDrawable2D(staticParms);
#if WINDOWS
#if DEBUG
			ScriptManager.getInstance().registerObject(((ColouredButton)this.playButton).Text, "playText");
#endif
#endif
		}
		#endregion Constructor

		#region Support methods
		public override void update(float elapsed) {
			base.currentKeyBoardState = Keyboard.GetState();
			base.currentMouseState = Mouse.GetState();
			Vector2 mousePos = new Vector2(base.currentMouseState.X, base.currentMouseState.Y);
			
			this.playButton.processActorsMovement(mousePos);
			this.exitButton.processActorsMovement(mousePos);
			if (base.currentMouseState.LeftButton == ButtonState.Pressed && base.prevousMouseState.LeftButton == ButtonState.Released) {
				if (this.playButton.isActorOver(mousePos)) {
					StateManager.getInstance().CurrentGameState = StateManager.GameState.InitGame;
				} else if (this.exitButton.isActorOver(mousePos)) {
					StateManager.getInstance().CurrentGameState = StateManager.GameState.Exit;
				}
			}
			base.update(elapsed);
		}

		public override void render(SpriteBatch spriteBatch) {
			this.backGround.render(spriteBatch);
			this.title.render(spriteBatch);
			this.playButton.render(spriteBatch);
			this.exitButton.render(spriteBatch);
		}
		#endregion Support methods

		#region Destructor
		public override void dispose() {
			if (this.playButton != null) {
				this.playButton.dispose();
			}
			if (this.exitButton != null) {
				this.exitButton.dispose();
			}
			if (this.backGround != null) {
				this.backGround.dispose();
			}
			if (this.title != null) {
				this.title.dispose();
			}
		}
		#endregion Destructor
	}
}