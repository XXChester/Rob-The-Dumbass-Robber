﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;
using GWNorthEngine.Utils;
using GWNorthEngine.AI.AStar;
using GWNorthEngine.Tools;
using GWNorthEngine.Tools.TilePlacer;

namespace Robber {
	public class MapLoader {
		private static bool mapAsUnwalkable(string textureName, int y, int x, int height, int width) {
			width -= 1;
			height -= 1;
			bool map = true;
			if (y == 0 || y == height || x == 0 || x == width) {
				if (textureName != Tile.TILE_NAME_BOTTOM_LEFT && textureName != Tile.TILE_NAME_BOTTOM_RIGHT && textureName != Tile.TILE_NAME_TOP_LEFT && 
					textureName != Tile.TILE_NAME_TOP_RIGHT) {
					map = false;
				} else {
					// make sure we are not a corner of the board
					if ((y == 0 && x == 0) || (y == height && x == width) || (y == 0 && x == width) || (y == height && x == 0)) {
						map = false;
					}
				}
			}
			return map;
		}

		private static object loadObject<T>(XmlDocument doc, string header, string[] searchStrings) {
			return loadObject<T>(doc, header, searchStrings, 0);
		}

		private static object loadObject<T>(XmlDocument doc, string header, string[] searchStrings, int nodeDepth) {
			T[] result = null;
			if (doc.GetElementsByTagName(header) != null && doc.GetElementsByTagName(header)[nodeDepth] != null) {
				XmlNodeList nodes = doc.GetElementsByTagName(header)[nodeDepth].ChildNodes;
				XmlNode node;
				Type type = typeof(T);
				MethodInfo parseMethod = type.GetMethod("Parse", new Type[] { typeof(string) });
				for (int i = 0; i < nodes.Count; i++) {
					node = nodes[i];
					for (int j = 0; j < searchStrings.Length; j++) {
						if (node.Name == searchStrings[j]) {
							if (type == typeof(string)) {
								if (node.FirstChild != null && node.FirstChild.Value != null) {// we may just want the default
									if (result == null) {
										result = new T[searchStrings.Length];
									}
									result[j] = (T)((object)node.FirstChild.Value);
								}
							} else {
								if (node.FirstChild != null && node.FirstChild.Value != null) {// we may just want the default
									if (result == null) {
										result = new T[searchStrings.Length];
									}
									result[j] = (T)parseMethod.Invoke(type, new object[] { ((string)node.FirstChild.Value) });
								} 
							}
						}
					}
				}
			}
			return result;
		}

		private static object loadObject<T>(XmlDocument doc, MapEditor.MappingState header) {
			return loadObject<T>(doc, header.ToString(), new string[] { MapEditor.XML_X, MapEditor.XML_Y });
		}

		private static MapWalls loadMapWalls(Color wallColour, LoadResult loadResult) {
			int height = loadResult.Height;
			int width = loadResult.Width;
			int wallLayer = 1;
			MapTile[,] mapWallTiles = loadResult.Layers[wallLayer].Tiles;
			// load visual aspect of the walls
			Tile[,] wallTiles = GWNorthEngine.Tools.TilePlacer.MapLoader.initTiles<Tile>(mapWallTiles, delegate(MapTile tile) {
				if (Tile.COLOUR_OVERRIDE_TILES.Contains<string>(tile.Texture.Name)) {
					return new Tile(tile.Texture, tile.Index, Color.White, tile.TileValue);
				} else {
					return new Tile(tile.Texture, tile.Index, wallColour, tile.TileValue);
				}
			});

			// load the AI for the map
			BasePathFinder.TypeOfSpace[,] aiSpaceTypes = new BasePathFinder.TypeOfSpace[height, width];
			for (int y = 0; y < height; y++) {
				for (int x = 0; x < width; x++) {
					// override so the AI can walk the outter wall
					if (wallTiles[y, x] != null && mapAsUnwalkable(wallTiles[y, x].Texture.Name, y, x, height, width)) {
						aiSpaceTypes[y, x] = Translator.translateTileValueToAStarType(wallTiles[y, x].TileValue);
					} else {
						aiSpaceTypes[y, x] = BasePathFinder.TypeOfSpace.Walkable;
					}
				}
			}

			AIManager.getInstance().init(height, width);
			AIManager.getInstance().Board = aiSpaceTypes;

			// generate the collision detection
			Placement tilesPlacement;
			List<BoundingBox> tileBBoxes = new List<BoundingBox>();
			for (int y = 0; y < height; y++) {
				for (int x = 0; x < width; x++) {
					if (mapWallTiles[y, x] != null) {
						tilesPlacement = new Placement(new Point(x, y));
						tileBBoxes.AddRange(CollisionDetectionGenerator.generateBoundingBoxesForTexture(mapWallTiles[tilesPlacement.index.Y, tilesPlacement.index.X].Texture, tilesPlacement));
					}
				}
			}
			CollisionManager.getInstance().MapBoundingBoxes = tileBBoxes;
			return new MapWalls(wallTiles, wallColour);
		}

		private static MapFloor loadMapFloor(Color floorColour, LoadResult loadResult) {
			int height = loadResult.Height;
			int width = loadResult.Width;
			int floorLayer = 0;
			MapTile[,] mapFloorTiles = loadResult.Layers[floorLayer].Tiles;
			Tile[,] floorTiles = GWNorthEngine.Tools.TilePlacer.MapLoader.initTiles<Tile>(mapFloorTiles, delegate(MapTile tile) {
				if (Tile.COLOUR_OVERRIDE_TILES.Contains<string>(tile.Texture.Name)) {
					return new Tile(tile.Texture, tile.Index, Color.White);
				} else {
					return new Tile(tile.Texture, tile.Index, floorColour);
				}
			});
			return new MapFloor(floorTiles, floorColour);
		}


		public static void load(ContentManager content, string mapName, Color floorColour, Color wallColour, out MapWalls walls, out MapFloor floor) {
			LoadResult loadResult = GWNorthEngine.Tools.TilePlacer.MapLoader.load(content, mapName);
			walls = loadMapWalls(wallColour, loadResult);
			floor = loadMapFloor(floorColour, loadResult);
		}

		public static void loadGenericPointList(XmlDocument doc, MapEditor.MappingState header, out List<Point> positions) {
			loadGenericPointList(doc, header.ToString(), out positions);
		}

		public static void loadGenericPointList(XmlDocument doc, string header, out List<Point> positions) {
			XmlNodeList nodes = doc.GetElementsByTagName(header);
			positions = new List<Point>();
			string[] xySearch = new string[] { MapEditor.XML_X, MapEditor.XML_Y };
			int[] intResults;
			for (int i = 0; i < nodes.Count; i++) {
				intResults = (int[])loadObject<int>(doc, header, xySearch, i);
				positions.Add(new Point(intResults[0], intResults[1]));
			}

		}
		public static void loadLevelInformation(XmlDocument doc, ref Color wallColour, ref Color floorColour, ref float time) {
			string[] rgb = new string[] { MapEditor.XML_R, MapEditor.XML_G, MapEditor.XML_B };
			try {
				int[] result = (int[])loadObject<Int32>(doc, MapEditor.XML_WALL_COLOUR, rgb);
				wallColour = new Color(result[0], result[1], result[2]);

				result = (int[])loadObject<Int32>(doc, MapEditor.XML_FLOOR_COLOUR, rgb);
				floorColour = new Color(result[0], result[1], result[2]);
			} catch (Exception) {
				// do nothign else with the error as the map is obviously in development stages
			}

			if (StateManager.getInstance().GameMode == StateManager.Mode.TimeAttack) {
				try {
					time = (float)(((float[])loadObject<float>(doc, MapEditor.XML_TIME, new string[] { MapEditor.XML_MINUTES }))[0]);
				} catch (Exception) {
					// do nothing else with the error as the map is obviously in development stages
				}
			}
		}

		public static void loadPlayerInformation(XmlDocument doc, ref Point playerStart) {
			try {
				int[] result = (int[])loadObject<Int32>(doc, MapEditor.MappingState.PlayerStart);
				playerStart = new Point(result[0], result[1]);
			} catch (Exception) {
				// do nothign else with the error as the map is obviously in development stages
			}
		}

		public static void loadGuardInformation(XmlDocument doc, ref List<Point> guardPositions, ref List<string> directions, ref List<string> states) {
			try {
				XmlNodeList guardNodes = doc.GetElementsByTagName(MapEditor.XML_HEADER_GUARD);
				directions = new List<string>();
				states = new List<string>();
				string[] stateSearch = new string[] { MapEditor.XML_GUARD_STATE };
				string[] directionSearch = new string[] { MapEditor.XML_GUARD_DIRECTION };
				for (int i = 0; i < guardNodes.Count; i++) {
					states.Add(((string[])loadObject<string>(doc, MapEditor.XML_HEADER_GUARD, stateSearch, i))[0]);
					directions.Add(((string[])loadObject<string>(doc, MapEditor.XML_HEADER_GUARD, directionSearch, i))[0]);
				}
				loadGenericPointList(doc, MapEditor.XML_GUARD_POSITION, out guardPositions);
			} catch (Exception) {
				// do nothign else with the error as the map is obviously in development stages
			}
		}
	}
}
