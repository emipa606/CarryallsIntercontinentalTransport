using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Random = System.Random;

namespace SPExtended;

[StaticConstructorOnStartup]
public static class SPExtra
{
    private static readonly Texture2D FillableBarTexture =
        SolidColorMaterials.NewSolidColorTexture(0.5f, 0.5f, 0.5f, 0.5f);

    private static readonly Texture2D ClearBarTexture = BaseContent.ClearTex;

    /// <summary>
    ///     Get neighbors of Tile on world map.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="offsets"></param>
    /// <param name="values"></param>
    /// <param name="index"></param>
    /// <param name="outList"></param>
    public static void GetList<T>(List<int> offsets, List<T> values, int index, List<T> outList)
    {
        outList.Clear();
        var num = offsets[index];
        var num2 = values.Count;
        if (index + 1 < offsets.Count)
        {
            num2 = offsets[index + 1];
        }

        for (var i = num; i < num2; i++)
        {
            outList.Add(values[i]);
        }
    }

    /// <summary>
    ///     Pop random value from List
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="list"></param>
    /// <returns></returns>
    public static T PopRandom<T>(ref List<T> list)
    {
        if (list is null || !list.Any())
        {
            return default;
        }

        Rand.PushState();
        var index = Rand.Range(0, list.Count);
        var item = list[index];
        Rand.PopState();
        list.Remove(item);
        return item;
    }

    /// <summary>
    ///     Grab random value from dictionary
    /// </summary>
    /// <typeparam name="T1"></typeparam>
    /// <typeparam name="T2"></typeparam>
    /// <param name="dictionary"></param>
    /// <returns></returns>
    public static KeyValuePair<T1, T2> RandomKVPFromDictionary<T1, T2>(this IDictionary<T1, T2> dictionary)
    {
        return dictionary.ElementAt(Rand.Range(0, dictionary.Count));
    }

    /// <summary>
    ///     Shuffle List pseudo randomly
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="list"></param>
    public static void SPShuffle<T>(this IList<T> list)
    {
        var rand = new Random((int)Time.deltaTime);
        var n = list.Count;
        while (n > 1)
        {
            n--;
            var k = rand.Next(n + 1);
            (list[k], list[n]) = (list[n], list[k]);
        }
    }

    /// <summary>
    ///     Initialize new list of object type T with object at index 0
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="typeObject"></param>
    /// <returns></returns>
    public static List<T> ConvertToList<T>(this T typeObject)
    {
        return [typeObject];
    }

    /// <summary>
    ///     Check if one List is entirely contained within another List
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="sourceList"></param>
    /// <param name="searchingList"></param>
    /// <returns></returns>
    public static bool ContainsAllOfList<T>(this IEnumerable<T> sourceList, IEnumerable<T> searchingList)
    {
        if (sourceList is null || searchingList is null)
        {
            return false;
        }

        return sourceList.Intersect(searchingList).Any();
    }

    /// <summary>
    ///     Unbox list of objects into List of object type T
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="objects"></param>
    /// <returns></returns>
    public static List<T> ConvertObjectList<T>(this List<object> objects)
    {
        for (var i = 0; i < objects.Count; i++)
        {
            var o = objects[i];
            if (o.GetType() != typeof(T))
            {
                objects.Remove(o);
            }
        }

        return objects.Cast<T>().ToList();
    }

    /// <summary>
    ///     Get Absolute Value of IntVec2
    /// </summary>
    /// <param name="c"></param>
    /// <returns></returns>
    public static IntVec2 Abs(this IntVec2 c)
    {
        return new IntVec2(Math.Abs(c.x), Math.Abs(c.z));
    }

    /// <summary>
    ///     Get Absolute Value of IntVec3
    /// </summary>
    /// <param name="c"></param>
    /// <returns></returns>
    public static IntVec3 Abs(this IntVec3 c)
    {
        return new IntVec3(Math.Abs(c.x), Math.Abs(c.y), Math.Abs(c.z));
    }

    /// <summary>
    ///     Calculate distance between 2 cells as float value
    /// </summary>
    /// <param name="c1"></param>
    /// <param name="c2"></param>
    /// <returns></returns>
    public static double Distance(IntVec3 c1, IntVec3 c2)
    {
        var x = Math.Abs(c1.x - c2.x);
        var y = Math.Abs(c1.z - c2.z);
        return Math.Sqrt(x.Pow(2) + y.Pow(2));
    }

    public static int Pow(this int val, int exp)
    {
        return Enumerable.Repeat(val, exp).Aggregate(1, (x, y) => x * y);
    }

    /// <summary>
    ///     Find Rot4 direction with the largest cell count
    ///     <para>Useful for taking edge cells of specific terrain and getting edge with the highest cell count</para>
    /// </summary>
    /// <param name="northCellCount"></param>
    /// <param name="eastCellCount"></param>
    /// <param name="southCellCount"></param>
    /// <param name="westCellCount"></param>
    /// <returns></returns>
    public static Rot4 Max4IntToRot(int northCellCount, int eastCellCount, int southCellCount, int westCellCount)
    {
        var ans1 = northCellCount > eastCellCount ? northCellCount : eastCellCount;
        var ans2 = southCellCount > westCellCount ? southCellCount : westCellCount;
        var ans3 = ans1 > ans2 ? ans1 : ans2;
        if (ans3 == northCellCount)
        {
            return Rot4.North;
        }

        if (ans3 == eastCellCount)
        {
            return Rot4.East;
        }

        if (ans3 == southCellCount)
        {
            return Rot4.South;
        }

        return ans3 == westCellCount ? Rot4.West : Rot4.Invalid;
    }

    /// <summary>
    ///     Get direction of river in Rot4 value. (Can be either start or end of River)
    /// </summary>
    /// <param name="map"></param>
    /// <returns></returns>
    public static Rot4 RiverDirection(Map map)
    {
        var tile = Find.WorldGrid[map.Tile];
        if (tile is not SurfaceTile surfaceTile)
        {
            return Rot4.Invalid;
        }

        var rivers = surfaceTile.Rivers;

        var angle = Find.WorldGrid.GetHeadingFromTo(map.Tile, (from r1 in rivers
            orderby -r1.river.degradeThreshold
            select r1).First().neighbor);
        if (angle < 45)
        {
            return Rot4.South;
        }

        if (angle < 135)
        {
            return Rot4.East;
        }

        if (angle < 225)
        {
            return Rot4.North;
        }

        return angle < 315 ? Rot4.West : Rot4.South;
    }

    /// <summary>
    ///     Draw vertical fillable bar
    /// </summary>
    /// <param name="rect"></param>
    /// <param name="fillPercent"></param>
    /// <param name="flip"></param>
    /// <returns></returns>
    public static Rect VerticalFillableBar(Rect rect, float fillPercent, bool flip = false)
    {
        return VerticalFillableBar(rect, fillPercent, FillableBarTexture, flip);
    }

    /// <summary>
    ///     Draw vertical fillable bar with texture
    /// </summary>
    /// <param name="rect"></param>
    /// <param name="fillPercent"></param>
    /// <param name="fillTex"></param>
    /// <param name="flip"></param>
    /// <returns></returns>
    public static Rect VerticalFillableBar(Rect rect, float fillPercent, Texture2D fillTex, bool flip = false)
    {
        var doBorder = rect is { height: > 15f, width: > 20f };
        return VerticalFillableBar(rect, fillPercent, fillTex, ClearBarTexture, doBorder, flip);
    }

    /// <summary>
    ///     Draw vertical fillable bar with background texture
    /// </summary>
    /// <param name="rect"></param>
    /// <param name="fillPercent"></param>
    /// <param name="fillTex"></param>
    /// <param name="bgTex"></param>
    /// <param name="doBorder"></param>
    /// <param name="flip"></param>
    /// <returns></returns>
    public static Rect VerticalFillableBar(Rect rect, float fillPercent, Texture2D fillTex, Texture2D bgTex,
        bool doBorder = false, bool flip = false)
    {
        if (doBorder)
        {
            GUI.DrawTexture(rect, bgTex);
            rect = rect.ContractedBy(3f);
        }

        if (bgTex != null)
        {
            GUI.DrawTexture(rect, bgTex);
        }

        if (!flip)
        {
            rect.y += rect.height;
            rect.height *= -1;
        }

        var result = rect;
        rect.height *= fillPercent;
        GUI.DrawTexture(rect, fillTex);
        return result;
    }

    /// <summary>
    ///     Convert RenderTexture to Texture2D
    ///     <para>Warning: This is very costly. Do not do often.</para>
    /// </summary>
    /// <param name="rTex"></param>
    /// <returns></returns>
    public static Texture2D ConvertToTexture2D(this RenderTexture rTex)
    {
        var tex2d = new Texture2D(512, 512, TextureFormat.RGB24, false);
        RenderTexture.active = rTex;
        tex2d.ReadPixels(new Rect(0, 0, rTex.width, rTex.height), 0, 0);
        tex2d.Apply();
        return tex2d;
    }

    /// <param name="c"></param>
    extension(IntVec3 c)
    {
        /// <summary>
        ///     Get adjacent cells that are cardinal to position c bounded by Map
        /// </summary>
        /// <param name="map"></param>
        /// <returns></returns>
        public IEnumerable<IntVec3> AdjacentCellsCardinal(Map map)
        {
            var north = new IntVec3(c.x, c.y, c.z + 1);
            var east = new IntVec3(c.x + 1, c.y, c.z);
            var south = new IntVec3(c.x, c.y, c.z - 1);
            var west = new IntVec3(c.x - 1, c.y, c.z);

            if (north.InBounds(map))
            {
                yield return north;
            }

            if (east.InBounds(map))
            {
                yield return east;
            }

            if (south.InBounds(map))
            {
                yield return south;
            }

            if (west.InBounds(map))
            {
                yield return west;
            }
        }

        /// <summary>
        ///     Get adjacent cells that are diagonal to position c bounded by Map
        /// </summary>
        /// <param name="map"></param>
        /// <returns></returns>
        public IEnumerable<IntVec3> AdjacentCellsDiagonal(Map map)
        {
            var NE = new IntVec3(c.x + 1, c.y, c.z + 1);
            var SE = new IntVec3(c.x + 1, c.y, c.z - 1);
            var SW = new IntVec3(c.x - 1, c.y, c.z - 1);
            var NW = new IntVec3(c.x - 1, c.y, c.z + 1);

            if (NE.InBounds(map))
            {
                yield return NE;
            }

            if (SE.InBounds(map))
            {
                yield return SE;
            }

            if (SW.InBounds(map))
            {
                yield return SW;
            }

            if (NW.InBounds(map))
            {
                yield return NW;
            }
        }

        /// <summary>
        ///     Get all adjacent cells to position c bounded by Map
        /// </summary>
        /// <param name="map"></param>
        /// <returns></returns>
        public IEnumerable<IntVec3> AdjacentCells8Way(Map map)
        {
            return c.AdjacentCellsCardinal(map).Concat(c.AdjacentCellsDiagonal(map));
        }
    }

    /// <summary>
    ///     Quadrant of grid or map
    /// </summary>
    public struct Quadrant
    {
        public Quadrant(byte q)
        {
            quadInt = q;
        }

        public Quadrant(int q)
        {
            quadInt = (byte)q.Clamp(1, 4);
        }

        public int AsInt
        {
            get => quadInt;
            set
            {
                value.Clamp(1, 4);
                quadInt = (byte)value;
            }
        }

        public static Quadrant Q1 => new(1);
        public static Quadrant Q2 => new(2);
        public static Quadrant Q3 => new(3);
        public static Quadrant Q4 => new(4);

        public static Quadrant Invalid => new() { quadInt = 100 };

        public static Quadrant QuadrantOfIntVec3(IntVec3 c, Map map)
        {
            if (c.x > map.Size.x / 2 && c.z >= map.Size.z / 2)
            {
                return Q1;
            }

            if (c.x >= map.Size.x / 2 && c.z < map.Size.z / 2)
            {
                return Q2;
            }

            if (c.x < map.Size.x / 2 && c.z <= map.Size.z / 2)
            {
                return Q3;
            }

            if (c.x <= map.Size.x / 2 && c.z > map.Size.z / 2)
            {
                return Q4;
            }

            if (c.x == map.Size.x / 2 && c.z == map.Size.z / 2)
            {
                return Q1;
            }

            return Invalid;
        }

        public static Quadrant QuadrantRelativeToPoint(IntVec3 c, IntVec3 point, Map map)
        {
            if (c.x > point.x && c.z >= point.z)
            {
                return Q1;
            }

            if (c.x >= point.x && c.z < point.z)
            {
                return Q2;
            }

            if (c.x < point.x && c.z <= point.z)
            {
                return Q3;
            }

            if (c.x <= point.x && c.z > point.z)
            {
                return Q4;
            }

            if (c.x == point.x && c.z == point.z)
            {
                return Q1;
            }

            return Invalid;
        }

        public static IEnumerable<IntVec3> CellsInQuadrant(Quadrant q, Map map)
        {
            switch (q.AsInt)
            {
                case 1:
                    return CellRect.WholeMap(map).Cells.Where(c2 => c2.x > map.Size.x / 2 && c2.z >= map.Size.z / 2);
                case 2:
                    return CellRect.WholeMap(map).Cells.Where(c2 => c2.x <= map.Size.x / 2 && c2.z < map.Size.z / 2);
                case 3:
                    return CellRect.WholeMap(map).Cells.Where(c2 => c2.x < map.Size.x / 2 && c2.z <= map.Size.z / 2);
                case 4:
                    return CellRect.WholeMap(map).Cells.Where(c2 => c2.x <= map.Size.x / 2 && c2.z > map.Size.z / 2);
                default:
                    throw new NotImplementedException("Quadrants");
            }
        }

        private byte quadInt;
    }
}