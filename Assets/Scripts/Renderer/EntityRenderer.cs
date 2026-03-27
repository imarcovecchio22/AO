using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ArgentumOnline.Game;
using ArgentumOnline.Network;

namespace ArgentumOnline.Renderer
{
    /// <summary>
    /// Mantiene un EntityView por cada entidad visible.
    /// Se suscribe a los eventos de GameState para crear/mover/eliminar entidades.
    /// También renderiza al jugador local.
    /// </summary>
    public class EntityRenderer : MonoBehaviour
    {
        public static EntityRenderer Instance { get; private set; }

        private readonly Dictionary<long, EntityView> _views = new();
        private EntityView _localPlayerView;

        private Vector2 _prevPlayerTile;

        void Awake() => Instance = this;

        public void Initialize()
        {
            var gs = GameState.Instance;

            gs.OnEntityAdded   += OnEntityAdded;
            gs.OnEntityRemoved += OnEntityRemoved;
            gs.OnEntityMoved   += OnEntityMoved;
            gs.OnDialogBubble  += OnDialogBubble;

            // Jugador local
            gs.LocalPlayer.OnPositionChanged += RefreshLocalPlayer;
            _prevPlayerTile = new Vector2(gs.LocalPlayer.PosX, gs.LocalPlayer.PosY);

            // Crear vista del jugador local
            CreateLocalPlayerView();

            // Entidades que llegaron antes de que el mapa terminara de cargar
            foreach (var entity in gs.Entities.Values)
                OnEntityAdded(entity);
        }

        // ── Jugador local ─────────────────────────────────────────────────────

        private void CreateLocalPlayerView()
        {
            var p = GameState.Instance.LocalPlayer;
            _localPlayerView = EntityView.Create(p.Id, p.Name, transform);
            _localPlayerView.Heading  = p.Heading;
            _localPlayerView.IdBody   = p.IdBody;
            _localPlayerView.IdHead   = p.IdHead;
            _localPlayerView.IdHelmet = p.IdHelmet;
            _localPlayerView.IdWeapon = p.IdWeapon;
            _localPlayerView.IdShield = p.IdShield;
            _localPlayerView.Refresh();
            // El jugador local siempre está en el centro del viewport (0,0)
            _localPlayerView.SetTilePosition(0, 0);

            // Nombre propio debajo del sprite (otros jugadores lo tienen arriba)
            if (_localPlayerView.NameLabel != null)
            {
                _localPlayerView.NameLabel.transform.localPosition = new Vector3(0, -0.85f, 0);
                _localPlayerView.NameLabel.anchor = TextAnchor.UpperCenter;
                _localPlayerView.NameLabel.color  = new Color(0.90f, 0.85f, 0.45f, 1f); // dorado
            }
        }

        private void RefreshLocalPlayer()
        {
            if (_localPlayerView == null) return;
            var p = GameState.Instance.LocalPlayer;
            _prevPlayerTile = new Vector2(p.PosX, p.PosY);

            _localPlayerView.Heading = p.Heading;
            _localPlayerView.StartWalkAnimation();
            StartCoroutine(StopAnimAfterMove(_localPlayerView));

            RefreshAllPositions();
        }

        // ── Entidades remotas ─────────────────────────────────────────────────

        private void OnEntityAdded(CharacterEntity entity)
        {
            if (_views.ContainsKey(entity.Id)) return;

            var view = EntityView.Create(entity.Id, entity.Name, transform);
            view.Heading  = entity.Heading;
            view.IdBody   = entity.IdBody;
            view.IdHead   = entity.IdHead;
            view.IdHelmet = entity.IdHelmet;
            view.IdWeapon = entity.IdWeapon;
            view.IdShield = entity.IdShield;
            view.Refresh();

            _views[entity.Id] = view;
            UpdateEntityPosition(view, entity);
        }

        private void OnEntityRemoved(long id)
        {
            if (_views.TryGetValue(id, out var view))
            {
                Destroy(view.gameObject);
                _views.Remove(id);
            }
        }

        private void OnEntityMoved(CharacterEntity entity)
        {
            if (!_views.TryGetValue(entity.Id, out var view)) return;
            view.Heading = entity.Heading;

            var p  = GameState.Instance.LocalPlayer;
            int dx = entity.PosX - p.PosX;
            int dy = entity.PosY - p.PosY;
            view.SmoothMoveTo(dx, dy);

            view.StartWalkAnimation();
            StartCoroutine(StopAnimAfterMove(view));
        }

        private IEnumerator StopAnimAfterMove(EntityView view)
        {
            yield return new WaitForSeconds(0.25f);
            if (view != null) view.StopWalkAnimation();
        }

        // ── Burbujas de diálogo ───────────────────────────────────────────────

        private void OnDialogBubble(long entityId, string msg, string hexColor)
        {
            Color color = new Color(0.95f, 0.95f, 0.95f, 1f);
            if (!string.IsNullOrEmpty(hexColor))
                ColorUtility.TryParseHtmlString(hexColor, out color);

            // Jugador local
            if (entityId == GameState.Instance.LocalPlayer.Id)
            {
                _localPlayerView?.ShowBubble(msg, color);
                return;
            }

            if (_views.TryGetValue(entityId, out var view))
                view.ShowBubble(msg, color);
        }

        // ── Posicionamiento ───────────────────────────────────────────────────

        private void UpdateEntityPosition(EntityView view, CharacterEntity entity)
        {
            var p = GameState.Instance.LocalPlayer;
            int dx = entity.PosX - p.PosX;
            int dy = entity.PosY - p.PosY;
            view.SetTilePosition(dx, dy);
        }

        // ── Refresh de apariencia ─────────────────────────────────────────────

        public void RefreshLocalPlayerAppearance()
        {
            if (_localPlayerView == null) return;
            var p = GameState.Instance.LocalPlayer;
            _localPlayerView.IdHead   = p.IdHead;
            _localPlayerView.IdBody   = p.IdBody;
            _localPlayerView.IdHelmet = p.IdHelmet;
            _localPlayerView.IdWeapon = p.IdWeapon;
            _localPlayerView.IdShield = p.IdShield;
            _localPlayerView.Heading  = p.Heading;
            _localPlayerView.Refresh();
        }

        /// <summary>Devuelve el Transform de la vista de una entidad (o null si no existe).</summary>
        public Transform GetEntityTransform(long id)
        {
            if (id == GameState.Instance.LocalPlayer.Id)
                return _localPlayerView?.transform;
            return _views.TryGetValue(id, out var view) ? view.transform : null;
        }

        public void RefreshEntityView(long id)
        {
            if (!_views.TryGetValue(id, out var view)) return;
            if (!GameState.Instance.TryGetEntity(id, out var entity)) return;
            view.IdHead   = entity.IdHead;
            view.IdBody   = entity.IdBody;
            view.IdHelmet = entity.IdHelmet;
            view.IdWeapon = entity.IdWeapon;
            view.IdShield = entity.IdShield;
            view.Heading  = entity.Heading;
            view.Refresh();
        }

        private void RefreshAllPositions()
        {
            var p = GameState.Instance.LocalPlayer;
            foreach (var kvp in _views)
            {
                if (GameState.Instance.TryGetEntity(kvp.Key, out var entity))
                {
                    int dx = entity.PosX - p.PosX;
                    int dy = entity.PosY - p.PosY;
                    kvp.Value.SetTilePosition(dx, dy);
                }
            }
        }
    }
}
