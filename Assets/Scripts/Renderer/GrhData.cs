using System.Collections.Generic;

namespace ArgentumOnline.Renderer
{
    /// <summary>
    /// Datos de un GRH (graphic) cargados desde graficos.json.
    /// Un GRH es un recorte de un spritesheet, posiblemente animado.
    /// </summary>
    [System.Serializable]
    public class GrhData
    {
        public int              numFrames;
        public string           numFile;    // número del PNG (ej: "1" → "1.png")
        public int              sX;         // origen X del recorte en el spritesheet
        public int              sY;         // origen Y del recorte en el spritesheet
        public int              width;
        public int              height;
        public Dictionary<string, string> frames;  // frame index → grhIndex (para animaciones)
        public GrhOffset        offset;
    }

    [System.Serializable]
    public class GrhOffset
    {
        public int x;
        public int y;
    }
}
