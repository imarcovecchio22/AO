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

        void Awake() => Instance = this;

        public void Initialize()
        {
            var gs = GameState.Instance;

            gs.OnEntityAdded   += OnEntityAdded;
            gs.OnEntityRemoved += OnEntityRemoved;
            gs.OnEntityMoved   += OnEntityMoved;

            // Jugador local
            gs.LocalPlayer.OnPositionChanged += RefreshLocalPlayer;

            // Crear vista del jugador local
            CreateLocalPlayerView();
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
        }

        private void RefreshLocalPlayer()
        {
            if (_localPlayerView == null) return;
            var p = GameState.Instance.LocalPlayer;
            _localPlayerView.Heading = p.Heading;
            _localPlayerView.Refresh();
            // Reposicionar todas las entidades relativas al jugador
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
            if (_views.TryGetValue(entity.Id, out var view))
                UpdateEntityPosition(view, entity);
        }

        // ── Posicionamiento ───────────────────────────────────────────────────

        private void UpdateEntityPosition(EntityView view, CharacterEntity entity)
        {
            var p = GameState.Instance.LocalPlayer;
            int dx = entity.PosX - p.PosX;
            int dy = entity.PosY - p.PosY;
            view.SetTilePosition(dx, dy);
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
