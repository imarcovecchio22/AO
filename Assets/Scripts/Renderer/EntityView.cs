using UnityEngine;

namespace ArgentumOnline.Renderer
{
    /// <summary>
    /// Representa visualmente un personaje o NPC en pantalla.
    /// Tiene un SpriteRenderer por cada capa: body, head, helmet, weapon, shield.
    /// </summary>
    public class EntityView : MonoBehaviour
    {
        // Capas en orden de sortingOrder
        public SpriteRenderer BodyRenderer;
        public SpriteRenderer HeadRenderer;
        public SpriteRenderer HelmetRenderer;
        public SpriteRenderer WeaponRenderer;
        public SpriteRenderer ShieldRenderer;

        // Nombre flotante
        public TextMesh NameLabel;

        // Estado actual
        public long   EntityId;
        public byte   Heading = 2;  // Down por defecto
        public int    IdBody, IdHead, IdHelmet, IdWeapon, IdShield;

        private static int _sortBase = 100;  // sortingOrder base para entidades

        public static EntityView Create(long entityId, string entityName, Transform parent)
        {
            var go = new GameObject($"Entity_{entityId}");
            go.transform.SetParent(parent);

            var view = go.AddComponent<EntityView>();
            view.EntityId = entityId;

            // Body — capa base
            view.BodyRenderer   = CreateLayer(go, "Body",   0);
            view.HeadRenderer   = CreateLayer(go, "Head",   1);
            view.HelmetRenderer = CreateLayer(go, "Helmet", 2);
            view.WeaponRenderer = CreateLayer(go, "Weapon", 3);
            view.ShieldRenderer = CreateLayer(go, "Shield", 4);

            // Nombre
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(go.transform);
            labelGo.transform.localPosition = new Vector3(0, 0.8f, 0);
            var tm = labelGo.AddComponent<TextMesh>();
            tm.text          = entityName;
            tm.fontSize      = 10;
            tm.characterSize = 0.1f;
            tm.anchor        = TextAnchor.LowerCenter;
            tm.alignment     = TextAlignment.Center;
            tm.color         = Color.white;
            view.NameLabel   = tm;

            return view;
        }

        private static SpriteRenderer CreateLayer(GameObject parent, string name, int layerOffset)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform);
            go.transform.localPosition = Vector3.zero;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = _sortBase + layerOffset;
            return sr;
        }

        /// <summary>Actualiza todos los sprites según heading e IDs actuales.</summary>
        public void Refresh()
        {
            SetLayerSprite(BodyRenderer,   IdBody,    EntityDatabase.Instance.Bodies);
            SetHeadSprite();
            SetLayerSprite(HelmetRenderer, IdHelmet,  EntityDatabase.Instance.Helmets);
            SetLayerSprite(WeaponRenderer, IdWeapon,  EntityDatabase.Instance.Weapons);
            SetLayerSprite(ShieldRenderer, IdShield,  EntityDatabase.Instance.Shields);
        }

        private void SetLayerSprite(SpriteRenderer sr,
                                    int id,
                                    System.Collections.Generic.Dictionary<int, DirectionalGrh> db)
        {
            if (id <= 0 || !db.TryGetValue(id, out var dg))
            {
                sr.sprite = null;
                return;
            }

            int grhIndex = EntityDatabase.GetGrhForHeading(dg, Heading);
            if (grhIndex <= 0) { sr.sprite = null; return; }

            GrhDatabase.Instance.GetSprite(grhIndex, sprite =>
            {
                if (sr != null) sr.sprite = sprite;
            });
        }

        private void SetHeadSprite()
        {
            if (IdHead <= 0 || !EntityDatabase.Instance.Heads.TryGetValue(IdHead, out var headDg) ||
                IdBody  <= 0 || !EntityDatabase.Instance.Bodies.TryGetValue(IdBody, out var bodyDg))
            {
                HeadRenderer.sprite = null;
                return;
            }

            int grhIndex = EntityDatabase.GetGrhForHeading(headDg, Heading);
            if (grhIndex <= 0) { HeadRenderer.sprite = null; return; }

            // Offset de la cabeza relativo al body (en pixels → unidades Unity)
            float offsetX = bodyDg.HeadOffsetX / 32f;
            float offsetY = bodyDg.HeadOffsetY / 32f;
            HeadRenderer.transform.localPosition = new Vector3(offsetX, -offsetY, 0);

            GrhDatabase.Instance.GetSprite(grhIndex, sprite =>
            {
                if (HeadRenderer != null) HeadRenderer.sprite = sprite;
            });
        }

        /// <summary>Mueve la vista a una posición en coordenadas de tile relativas al centro del viewport.</summary>
        public void SetTilePosition(int dx, int dy)
        {
            transform.localPosition = new Vector3(dx, -dy, 0);
            // sortingOrder por Y: entidades más abajo en pantalla se dibujan encima
            int yOrder = _sortBase + (50 - dy);
            BodyRenderer.sortingOrder   = yOrder;
            HeadRenderer.sortingOrder   = yOrder + 1;
            HelmetRenderer.sortingOrder = yOrder + 2;
            WeaponRenderer.sortingOrder = yOrder + 3;
            ShieldRenderer.sortingOrder = yOrder + 4;
        }
    }
}
