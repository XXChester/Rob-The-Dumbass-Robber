﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using GWNorthEngine.Scripting;
namespace Robber {
	public class MapEditor {
		public enum MappingState {
			None,
			PlayerStart,
			GuardPosition,
			GuardEntry,
			Treasure,
			WayPoint,
			BoundingBox
		};
		//singleton variable
		private static MapEditor instance = new MapEditor();

		#region Class variables
		private MouseState previous;
		private MappingState mappingState;
		private const string COMMAND_NONE = "none";
		private const string COMMAND_PLAYER_POSITION = "playerposition";
		private const string COMMAND_GUARD_POSITION = "guardposition";
		private const string COMMAND_GUARD_ENTRY = "guardentry";
		private const string COMMAND_TREASURE = "treasure";
		private const string COMMAND_WAY_POINTS = "waypoint";
		private const string COMMAND_BOUNDING_BOX = "boundingbox";
		public static string XML_X = "X";
		public static string XML_Y = "Y";
		public static string XML_R = "R";
		public static string XML_G = "G";
		public static string XML_B = "B";
		public static string XML_HEADER_LEVEL_INFO = "LevelInformation";
		public static string XML_TIME = "Time";
		public static string XML_MINUTES = "Minutes";
		public static string XML_WALL_COLOUR = "Walls";
		public static string XML_FLOOR_COLOUR = "Floor";
		public static string XML_HEADER_PLAYER_INFO = "PlayerInformation";
		public static string XML_HEADER_TREASURE_INFO = "TreasureInformation";
		public static string XML_HEADER_ENTRY_INFO = "EntryInformation";
		public static string XML_HEADER_WAYPOINT_INFO = "WaypointInformation";
		public static string XML_HEADER_COLLISION_INFO = "CollisionInformation";
		public static string XML_HEADER_GUARD_INFO = "GuardInformation";
		public static string XML_HEADER_GUARD = "Guard";
		public static string XML_GUARD_STATE = "State";
		public static string XML_GUARD_DIRECTION = "Direction";
		public static string XML_GUARD_POSITION = "Position";
		
		#endregion Class variables
		
		#region Class properties

		#endregion Class properties

		#region Constructor
		public MapEditor() {
			this.mappingState = MappingState.None;
#if WINDOWS
#if DEBUG
			ScriptManager.getInstance().registerObject(this, "editor");
#endif
#endif
		}
		#endregion Constructor

		#region Support methods
		public static MapEditor getInstance() {
			return instance;
		}

		public void editMapHelp() {
			Console.WriteLine("Format: [Information] - [Command]");
			Console.WriteLine("Turn Mapping off - " + COMMAND_NONE);
			Console.WriteLine("Players starting position - " + COMMAND_PLAYER_POSITION);
			Console.WriteLine("Guard starting position(Should be a waypoint) - " + COMMAND_GUARD_POSITION );
			Console.WriteLine("Guard entry point / Exit point - " + COMMAND_GUARD_ENTRY);
			Console.WriteLine("Treasure - " + COMMAND_TREASURE);
			Console.WriteLine("Guard way points - " + COMMAND_WAY_POINTS);
			Console.WriteLine("Bounding Boxes - " + COMMAND_BOUNDING_BOX);
		}

		public void editMap(string value) {
			value = value.ToLower();
			switch (value) {
				case COMMAND_NONE:
					this.mappingState = MappingState.None;
					break;
				case COMMAND_PLAYER_POSITION:
					this.mappingState = MappingState.PlayerStart;
					break;
				case COMMAND_GUARD_POSITION:
					this.mappingState = MappingState.GuardPosition;
					break;
				case COMMAND_GUARD_ENTRY:
					this.mappingState = MappingState.GuardEntry;
					break;
				case COMMAND_TREASURE:
					this.mappingState = MappingState.Treasure;
					break;
				case COMMAND_WAY_POINTS:
					this.mappingState = MappingState.WayPoint;
					break;
				case COMMAND_BOUNDING_BOX:
					this.mappingState = MappingState.BoundingBox;
					break;
				default:
					Console.WriteLine("Failed to recognize your command, try using the editMapHelp()");
					break;
			}
			Console.WriteLine("Mapping: " + this.mappingState.ToString());
		}

		public void update() {
			if (Mouse.GetState().LeftButton == ButtonState.Pressed && this.previous.LeftButton == ButtonState.Released && Mouse.GetState().Y >= 0 && Mouse.GetState().X >= 0) {
				StringBuilder xml = new StringBuilder();
				Vector2 worldPosition = new Vector2(Mouse.GetState().X, Mouse.GetState().Y);
				Point indexPosition = Placement.getIndex(new Vector2(Mouse.GetState().X, Mouse.GetState().Y));

				switch (this.mappingState) {
					case MappingState.BoundingBox:
							xml.Append("\t\t<" + this.mappingState + ">");
							xml.Append("\n\t\t\t<" + XML_X + ">" + worldPosition.X + "</" + XML_X + ">");
							xml.Append("\n\t\t\t<" + XML_Y + ">" + worldPosition.Y + "</" + XML_Y + ">");
							xml.Append("\n</" + this.mappingState + ">");
						break;
					case MappingState.GuardPosition:
						xml.Append("\t\t<Guard>");
						xml.Append("\n\t\t\t<State></State>");
						xml.Append("\n\t\t\t<Direction></Direction>");
						xml.Append("\n\t\t\t<Position>");
						xml.Append("\n\t\t\t\t<" + XML_X + ">" + indexPosition.X + "</" + XML_X + ">");
						xml.Append("\n\t\t\t\t<" + XML_Y + ">" + indexPosition.Y + "</" + XML_Y + ">");
						xml.Append("\n\t\t\t</Position>");
						xml.Append("\n\t\t</Guard>");
						break;
					default:
						if (this.mappingState != MappingState.None) {
							xml.Append("\t\t<" + this.mappingState + ">");
							xml.Append("\n\t\t\t<" + XML_X + ">" + indexPosition.X + "</" + XML_X + ">");
							xml.Append("\n\t\t\t<" + XML_Y + ">" + indexPosition.Y + "</" + XML_Y + ">");
							xml.Append("\n\t\t</" + this.mappingState + ">");
						}
						break;
				}

				if (this.mappingState != MappingState.None) {
					ScriptManager.getInstance().log(xml.ToString());
				}
				//Console.WriteLine(Placement.getIndex(new Vector2(Mouse.GetState().X, Mouse.GetState().Y)));
				//string message = "BBox|";
				//string suffix = Mouse.GetState().X + "," + Mouse.GetState().Y;
				//string message = "Treasure|";
				//string message = "Guard|";
				//string message = "WayPoint|";
				//string message = "GuardEntry|";
				//string message = "Player|";
				//string suffix = Placement.getIndex(new Vector2(Mouse.GetState().X, Mouse.GetState().Y)).X + "," + Placement.getIndex(new Vector2(Mouse.GetState().X, Mouse.GetState().Y)).Y;
				//Console.WriteLine(suffix);
				//ScriptManager.getInstance().log(message + suffix);
			}
			this.previous = Mouse.GetState();
		}
		#endregion Support methods
	}
}
