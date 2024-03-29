﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;
using GWNorthEngine.Model;
using GWNorthEngine.Model.Params;
using GWNorthEngine.Utils;
using GWNorthEngine.AI.AStar;
using GWNorthEngine.Scripting;
namespace Robber {
	public class Timer : IRenderable{
		#region Class variables
		private Text2D firstPart;
		private Text2D secondPart;
		private Text2D timeText;
		private Text2D thirdPart;
		private Color activeTimeColour;
		private SoundEffect guardsAlertedSfx;
		private float time;
		private float initialTime;
		private const string FIRST_PART = "Time Until";
		private const string SECOND_PART = "Detection;";
		private const string THIRD_PART = "Seconds";
		public const string DETECTED_SFX_NAME = "Alarm";
		public readonly Color HIGH_TIME = Color.Green;
		public readonly Color MEDIUM_TIME = Color.Yellow;
		public readonly Color LOW_TIME = Color.Red;
		#endregion Class variables

		#region Class propeties
		public float Time { get { return this.time; } }
		public Color ActiveTimeColour { get { return this.activeTimeColour; } }
		#endregion Class properties

		#region Constructor
		public Timer(ContentManager content) {
			Text2DParams parms = new Text2DParams();
			parms.Font = ResourceManager.getInstance().Font;
			parms.LightColour = ResourceManager.TEXT_COLOUR;
			parms.Position = new Vector2(682f, 14f);
			parms.WrittenText = FIRST_PART;
			this.firstPart = new Text2D(parms);

			parms.Position = new Vector2(684f,39f);
			parms.WrittenText = SECOND_PART;
			this.secondPart = new Text2D(parms);

			parms.Position = new Vector2(700f, 87f);
			parms.WrittenText = THIRD_PART;
			this.thirdPart = new Text2D(parms);

			parms.Position = new Vector2(700f, 63f);
			parms.WrittenText = "0";
			parms.LightColour = HIGH_TIME;
			this.timeText = new Text2D(parms);
			this.activeTimeColour = HIGH_TIME;
			
			//sfxs
			this.guardsAlertedSfx = LoadingUtils.load<SoundEffect>(content, DETECTED_SFX_NAME);
#if WINDOWS
#if DEBUG
			ScriptManager.getInstance().registerObject(this.firstPart, "first");
			ScriptManager.getInstance().registerObject(this.secondPart, "second");
			ScriptManager.getInstance().registerObject(this.timeText, "time");
			ScriptManager.getInstance().registerObject(this.thirdPart, "third");
#endif
#endif
		}
		#endregion Constructor

		#region Support methods
		private string formatTimer() {
			float seconds = this.time / 1000f;
			// Strings are mutable so use a StringBuilder as it is faster and cheaper
			StringBuilder sb = new StringBuilder();
			// left padding
			int timeLength = ((int)(this.time / 1000f)).ToString().Length;
			int paddingRequired = 3 - timeLength;// we always want a 6 digit timer
			for (int i = 0; i < paddingRequired; i++) {
				sb.Append("0");
			}

			sb.Append(seconds.ToString());

			// right padding
			string localTime = (this.time / 1000f).ToString();
			if (localTime.IndexOf('.') != -1) {
				timeLength = localTime.Substring(localTime.IndexOf('.') + 1).Length;
				paddingRequired = 3 - timeLength;
			} else {// no decimal so pad 3
				sb.Append(".");// need to add the decimal
				paddingRequired = 3;
			}

			for (int i = 0; i < paddingRequired; i++) {
				sb.Append("0");
			}

			return sb.ToString();
		}

		public void updateColours(Color textColour, Color timeColour) {
			this.firstPart.LightColour = textColour;
			this.secondPart.LightColour = textColour;
			this.timeText.LightColour = timeColour;
			this.thirdPart.LightColour = textColour;
		}

		public void reset(float timeUntilDetection) {
			this.time = timeUntilDetection * 60f * 1000f;// passed in as minutes, convert to seconds, convert to miliseconds
			this.initialTime = this.time;
			float seconds = this.time / 1000f;
			if (this.time % 1000f == 0f) {
				this.timeText.WrittenText = formatTimer();
			} else {
				this.timeText.WrittenText = formatTimer();
			}
			this.activeTimeColour = HIGH_TIME;
			this.timeText.LightColour = this.activeTimeColour;
		}

		public void update(float elapsed) {
			this.time = MathHelper.Clamp(this.time - elapsed, 0f, this.time);// add the elapsed to the timer but ensure we do not negate past 0
			if (this.time <= 0f) {
				if (!AIManager.getInstance().PlayerDetected) {
					SoundManager.getInstance().sfxEngine.playSoundEffect(this.guardsAlertedSfx, loop:true);
				}
				//Alert the authorities
				AIManager.getInstance().PlayerDetected = true;
				this.timeText.WrittenText = formatTimer();
			} else {
				float seconds = this.time / 1000f;
				this.timeText.WrittenText = formatTimer();
				float factor = this.initialTime / this.time;
				if (factor >= 3f) {
					this.activeTimeColour = LOW_TIME;
					this.timeText.LightColour = this.activeTimeColour;
				} else if (factor >= 1.5f) {
					this.activeTimeColour = MEDIUM_TIME;
					this.timeText.LightColour = this.activeTimeColour;
				} else {
					this.activeTimeColour = HIGH_TIME;
					this.timeText.LightColour = this.activeTimeColour;
				}
			}
		}

		public void render(SpriteBatch spriteBatch) {
			this.firstPart.render(spriteBatch);
			this.secondPart.render(spriteBatch);
			this.timeText.render(spriteBatch);
			this.thirdPart.render(spriteBatch);
		}
		#endregion Support methods

		#region Destructor
		public void dispose() {
			/*if (this.guardsAlertedSfx != null) {
				this.guardsAlertedSfx.Dispose();
			}*/
		}
		#endregion Destructor
	}
}
