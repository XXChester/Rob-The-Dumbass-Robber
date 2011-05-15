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
using GWNorthEngine.AI.AStar;
using GWNorthEngine.Model;
using GWNorthEngine.Model.Params;
using GWNorthEngine.Scripting;
namespace Robber {
	public class Player : Person{
		#region Class variables
		private const float MOVEMENT_SPEED = 150f / 1000f;//. player always runs
		private Text2D treasureText;
		private StaticDrawable2D treasure;
		#endregion Class variables

		#region Class propeties
		public int CapturedTreasures { get; set; }
		#endregion Class properties

		#region Constructor
		public Player(ContentManager content, Placement startingLocation)
			: base(content, "Rob", startingLocation, MOVEMENT_SPEED) {
			Text2DParams textParms = new Text2DParams();
			textParms.Font = ResourceManager.getInstance().Font;
			textParms.LightColour = ResourceManager.TEXT_COLOUR;
			textParms.Position = new Vector2(350f, 575f);
			textParms.WrittenText = "x " + this.CapturedTreasures;
			this.treasureText = new Text2D(textParms);

			StaticDrawable2DParams staticParms = new StaticDrawable2DParams();
			staticParms.Position = new Vector2(325f, 573f);
			staticParms.Texture = content.Load<Texture2D>("Treasure1");
			this.treasure = new StaticDrawable2D(staticParms);
#if WINDOWS
#if DEBUG
			ScriptManager.getInstance().registerObject(this.treasureText, "treasureText");
#endif
#endif
		}
		#endregion Constructor

		#region Support methods
		public override void updateMove(float elapsed) {

			if (this.currentKeyBoardState.IsKeyDown(Keys.Left)) {
				base.direction = Person.Direction.Left;
			} else if (this.currentKeyBoardState.IsKeyDown(Keys.Up)) {
				base.direction = Person.Direction.Up;
			} else if (this.currentKeyBoardState.IsKeyDown(Keys.Right)) {
				base.direction = Person.Direction.Right;
			} else if (this.currentKeyBoardState.IsKeyDown(Keys.Down)) {
				base.direction = Person.Direction.Down;
			} else {
				// if none of our direction keys are down than we are not moving
				if (base.previousKeyBoardState.IsKeyDown(Keys.Left) || base.previousKeyBoardState.IsKeyDown(Keys.Up) || base.previousKeyBoardState.IsKeyDown(Keys.Right) ||
					base.previousKeyBoardState.IsKeyDown(Keys.Down)) {
						base.direction = Person.Direction.None;
				}
			}


			if (base.direction != Direction.None) {
				float moveDistance = (base.movementSpeed * elapsed);
				Vector2 newPos;
				if (base.direction == Direction.Up) {
					newPos = new Vector2(base.activeSprite.Position.X, base.activeSprite.Position.Y - moveDistance);
					if (!CollisionManager.getInstance().wallCollisionFound(Helper.getBBox(newPos))) {
						base.activeSprite.Position = new Vector2(base.activeSprite.Position.X, base.activeSprite.Position.Y - moveDistance);
					}
				} else if (base.direction == Direction.Right) {
					newPos = new Vector2(base.activeSprite.Position.X + moveDistance, base.activeSprite.Position.Y);
					if (!CollisionManager.getInstance().wallCollisionFound(Helper.getBBox(newPos))) {
						base.activeSprite.Position = new Vector2(base.activeSprite.Position.X + moveDistance, base.activeSprite.Position.Y);
					}
				} else if (base.direction == Direction.Down) {
					newPos = new Vector2(this.activeSprite.Position.X, this.activeSprite.Position.Y + moveDistance);
					if (!CollisionManager.getInstance().wallCollisionFound(Helper.getBBox(newPos))) {
						this.activeSprite.Position = new Vector2(this.activeSprite.Position.X, this.activeSprite.Position.Y + moveDistance);
					}
				} else if (this.direction == Direction.Left) {
					newPos = new Vector2(base.activeSprite.Position.X - moveDistance, base.activeSprite.Position.Y);
					if (!CollisionManager.getInstance().wallCollisionFound(Helper.getBBox(newPos))) {
						base.activeSprite.Position = new Vector2(base.activeSprite.Position.X - moveDistance, base.activeSprite.Position.Y);
					}
				}
			}
			// update our placement and bounding box
			base.Placement = new Placement(Placement.getIndex(base.activeSprite.Position));
			base.BoundingBox = Helper.getBBox(base.activeSprite.Position);
			
			if (base.previousPlacement.index != base.Placement.index) {
				AIManager.getInstane().Board[base.previousPlacement.index.Y, base.previousPlacement.index.X] = base.previousTypeOfSpace;
				base.previousTypeOfSpace = AIManager.getInstane().Board[base.Placement.index.Y, base.Placement.index.X];
			}
			if (AIManager.getInstane().PlayerDetected) {// if we aren't moving we still need to report where we are if we are detected
				// if we have been detected we need to tell the AI where we are
				AIManager.getInstane().Board[base.Placement.index.Y, base.Placement.index.X] = PathFinder.TypeOfSpace.End;
			}
			this.treasureText.WrittenText = " x " + this.CapturedTreasures;
			base.updateMove(elapsed);
		}

		public new void render(SpriteBatch spriteBatch) {
			this.treasureText.render(spriteBatch);
			this.treasure.render(spriteBatch);
			base.render(spriteBatch);
		}
		#endregion Support methods

		#region Destructor
		public new void dispose() {
			if (this.treasure != null) {
				this.treasure.dispose();
			}
			base.dispose();
		}
		#endregion Destructor
	}
}
