using System.Collections;
using UnityEngine;
using UnityEngine.UI;

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

        // Burbuja de diálogo
        private GameObject _bubbleGo;
        private Coroutine  _bubbleCoroutine;

        // Animación de caminata
        private Coroutine _walkAnim;

        // Interpolación de movimiento
        private Coroutine _moveCoroutine;

        // Meditación
        private Coroutine  _meditCoroutine;
        private GameObject _meditAura;

        // Estado actual
        public long   EntityId;
        public byte   Heading = 2;  // Down por defecto
        public int    IdBody, IdHead, IdHelmet, IdWeapon, IdShield;

        private static int _sortBase = 100;  // sortingOrder base para entidades

        // Los sprites de body son ~25x45px a 32PPU → ~1.4u de alto.
        // Offset vertical para que los pies queden al nivel del tile (y=0).
        private const float CharYOffset = 0.7f;   // ≈ mitad de la altura típica del body

        private static Sprite _shadowSprite;

        public static EntityView Create(long entityId, string entityName, Transform parent)
        {
            var go = new GameObject($"Entity_{entityId}");
            go.transform.SetParent(parent);

            var view = go.AddComponent<EntityView>();
            view.EntityId = entityId;

            // Sombra elíptica al nivel del suelo (antes que los demás layers)
            view.AddShadow(go);

            // Capas del personaje elevadas para que los pies toquen el suelo
            view.BodyRenderer   = CreateLayer(go, "Body",   0, CharYOffset);
            view.HeadRenderer   = CreateLayer(go, "Head",   1, CharYOffset);
            view.HelmetRenderer = CreateLayer(go, "Helmet", 2, CharYOffset);
            view.WeaponRenderer = CreateLayer(go, "Weapon", 3, CharYOffset);
            view.ShieldRenderer = CreateLayer(go, "Shield", 4, CharYOffset);

            // Nombre flotante — encima del personaje
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(go.transform);
            labelGo.transform.localPosition = new Vector3(0, CharYOffset + 1.2f, 0);
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

        private void AddShadow(GameObject parent)
        {
            if (_shadowSprite == null)
                _shadowSprite = BuildShadowSprite();

            var shadowGo = new GameObject("Shadow");
            shadowGo.transform.SetParent(parent.transform);
            shadowGo.transform.localPosition  = new Vector3(0, 0.05f, 0);
            shadowGo.transform.localScale      = new Vector3(0.7f, 0.35f, 1f);  // elipse
            var sr = shadowGo.AddComponent<SpriteRenderer>();
            sr.sprite       = _shadowSprite;
            sr.color        = new Color(0, 0, 0, 0.35f);
            sr.sortingOrder = _sortBase - 1;
        }

        private static Sprite BuildShadowSprite()
        {
            int size = 32;
            var tex  = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            float r = size / 2f;
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x + 0.5f - r, dy = y + 0.5f - r;
                float d  = Mathf.Sqrt(dx * dx + dy * dy);
                float a  = Mathf.Clamp01(1f - d / r);
                a = a * a;
                pixels[y * size + x] = new Color(1, 1, 1, a);
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private static SpriteRenderer CreateLayer(GameObject parent, string name,
                                                   int layerOffset, float yOffset = 0f)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform);
            go.transform.localPosition = new Vector3(0, yOffset, 0);
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
            // Se suma CharYOffset porque la capa Head fue creada en y=0 local y el
            // body está elevado ese mismo valor.
            float offsetX = bodyDg.HeadOffsetX / 32f;
            float offsetY = bodyDg.HeadOffsetY / 32f;
            HeadRenderer.transform.localPosition = new Vector3(offsetX, CharYOffset + (-offsetY), 0);

            GrhDatabase.Instance.GetSprite(grhIndex, sprite =>
            {
                if (HeadRenderer != null) HeadRenderer.sprite = sprite;
            });
        }

        // ── Animación de caminata ─────────────────────────────────────────────

        /// <summary>Inicia la animación de caminata en el heading actual.</summary>
        public void StartWalkAnimation()
        {
            if (_walkAnim != null) StopCoroutine(_walkAnim);
            _walkAnim = StartCoroutine(WalkAnimRoutine());
        }

        /// <summary>Detiene la animación y vuelve al frame estático (frame 1).</summary>
        public void StopWalkAnimation()
        {
            if (_walkAnim != null) { StopCoroutine(_walkAnim); _walkAnim = null; }
            Refresh();
        }

        private IEnumerator WalkAnimRoutine()
        {
            // Obtener grhIndex animado del body para el heading actual
            if (IdBody <= 0 || !EntityDatabase.Instance.Bodies.TryGetValue(IdBody, out var bodyDg))
                yield break;

            int baseGrh = EntityDatabase.GetGrhForHeading(bodyDg, Heading);
            if (!GrhDatabase.Instance.TryGetGrh(baseGrh, out var animGrh) || animGrh.numFrames <= 1)
                yield break;

            float frameSec = animGrh.speed / 1000f;
            if (frameSec <= 0) frameSec = 0.085f;

            int frame = 1;
            while (true)
            {
                string key = frame.ToString();
                if (animGrh.frames != null && animGrh.frames.TryGetValue(key, out string frameStr)
                    && int.TryParse(frameStr, out int frameGrh))
                {
                    int capturedGrh = frameGrh;
                    SpriteRenderer capturedSr = BodyRenderer;
                    GrhDatabase.Instance.GetSprite(capturedGrh, sprite =>
                    {
                        if (capturedSr != null && sprite != null) capturedSr.sprite = sprite;
                    });
                }

                frame = (frame % animGrh.numFrames) + 1;
                yield return new WaitForSeconds(frameSec);
            }
        }

        /// <summary>Mueve la vista instantáneamente (snap). Para recalcular posiciones relativas al jugador.</summary>
        public void SetTilePosition(int dx, int dy)
        {
            if (_moveCoroutine != null) { StopCoroutine(_moveCoroutine); _moveCoroutine = null; }
            transform.localPosition = new Vector3(dx, -dy, 0);
            ApplySortOrder(dy);
        }

        /// <summary>Mueve la vista con interpolación suave desde la posición actual hasta (dx, dy).</summary>
        public void SmoothMoveTo(int dx, int dy)
        {
            if (_moveCoroutine != null) StopCoroutine(_moveCoroutine);
            _moveCoroutine = StartCoroutine(SmoothMoveRoutine(dx, dy));
        }

        private IEnumerator SmoothMoveRoutine(int dx, int dy)
        {
            Vector3 start   = transform.localPosition;
            Vector3 target  = new Vector3(dx, -dy, 0);
            float   elapsed = 0f;
            const float duration = 0.22f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                transform.localPosition = Vector3.Lerp(start, target, t);
                yield return null;
            }

            transform.localPosition = target;
            ApplySortOrder(dy);
            _moveCoroutine = null;
        }

        private void ApplySortOrder(int dy)
        {
            int yOrder = _sortBase + (50 - dy);

            // Actualizar sombra si existe
            var shadowT = transform.Find("Shadow");
            if (shadowT != null)
            {
                var shadowSr = shadowT.GetComponent<SpriteRenderer>();
                if (shadowSr != null) shadowSr.sortingOrder = yOrder - 1;
            }

            BodyRenderer.sortingOrder   = yOrder;
            HeadRenderer.sortingOrder   = yOrder + 1;
            HelmetRenderer.sortingOrder = yOrder + 2;
            WeaponRenderer.sortingOrder = yOrder + 3;
            ShieldRenderer.sortingOrder = yOrder + 4;
        }

        // ── Meditación ────────────────────────────────────────────────────────

        // GRH de la animación amarilla de meditación (igual que AO original):
        // 134 = CHICO (lv 1-14), 145 = MEDIANO (lv 15-29), 156 = GRANDE (lv 30-44), 286 = XGRANDE (lv 45+)
        public const int MeditGrhChico   = 134;
        public const int MeditGrhMediano = 145;
        public const int MeditGrhGrande  = 156;
        public const int MeditGrhXGrande = 286;

        public static int MeditGrhForLevel(int level)
        {
            if (level >= 45) return MeditGrhXGrande;
            if (level >= 30) return MeditGrhGrande;
            if (level >= 15) return MeditGrhMediano;
            return MeditGrhChico;
        }

        public void StartMeditation(int grhIndex = MeditGrhChico)
        {
            if (_meditCoroutine != null) return;
            _meditCoroutine = StartCoroutine(MeditationRoutine(grhIndex));
        }

        public void StopMeditation()
        {
            if (_meditCoroutine != null) { StopCoroutine(_meditCoroutine); _meditCoroutine = null; }
            if (_meditAura != null)      { Destroy(_meditAura);            _meditAura      = null; }
        }

        private IEnumerator MeditationRoutine(int grhIndex)
        {
            if (!GrhDatabase.Instance.TryGetGrh(grhIndex, out var grh) || grh.numFrames <= 1)
                yield break;

            _meditAura = new GameObject("MeditAura");
            _meditAura.transform.SetParent(transform, false);
            // Offset vertical igual al del AO original: y=0, centrado sobre el personaje
            _meditAura.transform.localPosition = new Vector3(0, 0, 0.05f);

            var sr = _meditAura.AddComponent<SpriteRenderer>();
            sr.sortingOrder = BodyRenderer.sortingOrder + 10;  // encima del personaje

            float frameSec = grh.speed > 0 ? grh.speed / 1000f : 0.085f;
            int frame = 1;

            while (true)
            {
                string key = frame.ToString();
                if (grh.frames != null && grh.frames.TryGetValue(key, out string frameStr)
                    && int.TryParse(frameStr, out int frameGrh))
                {
                    int captured = frameGrh;
                    SpriteRenderer capturedSr = sr;
                    GrhDatabase.Instance.GetSprite(captured, sprite =>
                    {
                        if (capturedSr != null) capturedSr.sprite = sprite;
                    });
                }

                frame = (frame % grh.numFrames) + 1;
                yield return new WaitForSeconds(frameSec);
            }
        }

        // ── Burbuja de diálogo ────────────────────────────────────────────────

        public void ShowBubble(string text, Color textColor)
        {
            if (_bubbleCoroutine != null)
            {
                StopCoroutine(_bubbleCoroutine);
                if (_bubbleGo != null) Destroy(_bubbleGo);
            }
            _bubbleCoroutine = StartCoroutine(BubbleRoutine(text, textColor));
        }

        private IEnumerator BubbleRoutine(string text, Color textColor)
        {
            // Canvas world-space anclado sobre la cabeza
            _bubbleGo = new GameObject("Bubble");
            _bubbleGo.transform.SetParent(transform, false);
            // Posición: encima del name label (que está a y=0.8)
            _bubbleGo.transform.localPosition = new Vector3(0, 1.15f, -0.1f);
            // Escala: sizeDelta=300x55 → 1 unit = 1/80 world → ~3.75 tiles de ancho
            _bubbleGo.transform.localScale = Vector3.one * (1f / 80f);

            var canvas = _bubbleGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 200;

            var rt = _bubbleGo.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(300, 55);

            // Fondo oscuro semitransparente
            var bgGo  = new GameObject("BG");
            bgGo.transform.SetParent(_bubbleGo.transform, false);
            var bgRt  = bgGo.AddComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.color = Color.clear;

            // Texto del mensaje
            var txtGo  = new GameObject("Text");
            txtGo.transform.SetParent(_bubbleGo.transform, false);
            var txtRt  = txtGo.AddComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = new Vector2(8, 4);
            txtRt.offsetMax = new Vector2(-8, -4);
            var txt    = txtGo.AddComponent<Text>();
            txt.font               = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.text               = text;
            txt.fontSize           = 24;
            txt.color              = textColor;
            txt.alignment          = TextAnchor.MiddleCenter;
            txt.horizontalOverflow = HorizontalWrapMode.Wrap;
            txt.verticalOverflow   = VerticalWrapMode.Overflow;

            var shadow = txtGo.AddComponent<Shadow>();
            shadow.effectColor    = new Color(0, 0, 0, 0.8f);
            shadow.effectDistance = new Vector2(1, -1);

            // Mantener 3.5 segundos
            yield return new WaitForSeconds(3.5f);

            // Fade out en 0.6 s
            float elapsed  = 0f;
            float fadeTime = 0.6f;
            Color txStart  = txt.color;

            while (elapsed < fadeTime)
            {
                elapsed += Time.deltaTime;
                float a = 1f - (elapsed / fadeTime);
                txt.color = new Color(txStart.r, txStart.g, txStart.b, txStart.a * a);
                yield return null;
            }

            Destroy(_bubbleGo);
            _bubbleGo        = null;
            _bubbleCoroutine = null;
        }
    }
}
