﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using GoodAI.Logging;
using TmxMapSerializer.Elements;
using VRageMath;

namespace World.GameActors.Tiles
{
    public interface ITilesetTable
    {
        int TileNumber(string tileName);
        string TileName(int tileNumber);
    }

    public class TilesetTable : ITilesetTable
    {
        private readonly Dictionary<string, int> m_namesValuesDictionary;
        private readonly Dictionary<int, string> m_valuesNamesDictionary;


        public readonly List<Tileset> Tilesets = new List<Tileset>();

        public Vector2I TileSize { get; protected set; }
        public Vector2I TileMargins { get; protected set; }
        public Vector2I TileBorder { get; protected set; } // how much the size of the tile is increased because of 
        // correct texture filtering


        public TilesetTable(Map tmxMap, StreamReader tilesetFile)
        {
            if (tmxMap != null)
            {
                Tilesets.AddRange(tmxMap.Tilesets);
                TileSize = new Vector2I(tmxMap.Tilewidth, tmxMap.Tileheight);
                TileMargins = Vector2I.One;
            }

            TileBorder = new Vector2I(TileSize.X, TileSize.Y); // this much border is needed for small resolutions

            var dataTable = new DataTable();
            string readLine = tilesetFile.ReadLine();

            Debug.Assert(readLine != null, "readLine != null");
            foreach (string header in readLine.Split(';'))
            {
                dataTable.Columns.Add(header);
            }


            while (!tilesetFile.EndOfStream)
            {
                string line = tilesetFile.ReadLine();
                Debug.Assert(line != null, "line != null");
                string[] row = line.Split(';');
                if (dataTable.Columns.Count != row.Length)
                    break;
                DataRow newRow = dataTable.NewRow();
                foreach (DataColumn column in dataTable.Columns)
                {
                    newRow[column.ColumnName] = row[column.Ordinal];
                }
                dataTable.Rows.Add(newRow);
            }

            tilesetFile.Close();

            IEnumerable<DataRow> enumerable = dataTable.Rows.Cast<DataRow>();
            var dataRows = enumerable.ToArray();

            try
            {
                m_namesValuesDictionary = dataRows.Where(x => x["IsDefault"].ToString() == "1")
                    .ToDictionary(x => x["NameOfTile"].ToString(), x => int.Parse(x["PositionInTileset"].ToString()));
            }
            catch (ArgumentException)
            {
                Log.Instance.Error("Duplicate NameOfTiles in tileset table. Try set IsDefault to 0.");
            }
            m_valuesNamesDictionary = dataRows.ToDictionary(x => int.Parse(x["PositionInTileset"].ToString()), x => x["NameOfTile"].ToString());
        }

        /// <summary>
        /// only for mocking
        /// </summary>
        public TilesetTable()
        {

        }


        public IEnumerable<Tileset> GetTilesetImages()
        {
            return Tilesets.Where(t =>
            {
                var fileName = Path.GetFileName(t.Image.Source);
                return fileName != null && fileName.StartsWith("roguelike_");
            });
        }

        public IEnumerable<Tileset> GetOverlayImages()
        {
            return Tilesets.Where(t =>
            {
                var fileName = Path.GetFileName(t.Image.Source);
                return fileName != null && fileName.StartsWith("ui_");
            });
        }

        public virtual int TileNumber(string tileName)
        {
            return m_namesValuesDictionary[tileName];
        }

        public virtual string TileName(int tileNumber)
        {
            return m_valuesNamesDictionary.ContainsKey(tileNumber) ? m_valuesNamesDictionary[tileNumber] : null;
        }
    }
}