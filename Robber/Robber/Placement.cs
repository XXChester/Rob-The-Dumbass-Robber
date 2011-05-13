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
namespace Robber {
	public struct Placement {
		public Vector2 worldPosition;
		public Point index;
		public Placement(Vector2 worldPosition, int x, int y) {
			this.worldPosition = worldPosition;
			this.index = new Point(x, y);
		}

		public Placement(Point index) {
			this.index = index;
			this.worldPosition = new Vector2(index.X * ResourceManager.TILE_SIZE, index.Y * ResourceManager.TILE_SIZE);
		}

		public static Point getIndex(Vector2 point) {
			int x = (int)point.X / ResourceManager.TILE_SIZE;
			int y = (int)point.Y / ResourceManager.TILE_SIZE;
			return new Point(x,y);
		}
	}
}
