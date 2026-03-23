using System.Collections.Generic;

namespace ArgentumOnline.Renderer
{
    /// <summary>
    /// Una celda del mapa.
    /// graphics["1"]-["4"] son los grhIndex de cada capa visual.
    /// </summary>
    public class MapCell
    {
        public bool blocked;
        public Dictionary<string, int> graphics = new Dictionary<string, int>();
        public int trigger;
        public int objIndex;   // objeto en el suelo (grhIndex)
    }

    /// <summary>
    /// El mapa completo: [y][x] = MapCell.
    /// Coordenadas 1-100.
    /// </summary>
    public class MapGrid
    {
        public int MapNumber;
        public string Name;
        // [y][x]
        public MapCell[][] Cells = new MapCell[101][];

        public MapGrid()
        {
            for (int y = 0; y <= 100; y++)
            {
                Cells[y] = new MapCell[101];
                for (int x = 0; x <= 100; x++)
                    Cells[y][x] = new MapCell();
            }
        }

        public MapCell GetCell(int x, int y)
        {
            if (x < 1 || x > 100 || y < 1 || y > 100) return null;
            return Cells[y][x];
        }
    }
}
