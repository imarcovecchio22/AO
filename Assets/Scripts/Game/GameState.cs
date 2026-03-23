using System;
using System.Collections.Generic;
using UnityEngine;
using ArgentumOnline.Game;

namespace ArgentumOnline.Game
{
    /// <summary>
    /// Estado central del juego. Singleton puro (no MonoBehaviour).
    /// Mutado por los handlers de red, leído por el renderer y la UI.
    /// </summary>
    public class GameState
    {
        public static readonly GameState Instance = new GameState();

        // ── Jugador local ─────────────────────────────────────────────────────
        public LocalPlayer LocalPlayer { get; private set; } = new LocalPlayer();

        // ── Entidades en el área visual ───────────────────────────────────────
        // Key: ID de la entidad (long, convertido desde double)
        private readonly Dictionary<long, CharacterEntity> _entities = new Dictionary<long, CharacterEntity>();
        public IReadOnlyDictionary<long, CharacterEntity> Entities => _entities;

        // ── Eventos globales ──────────────────────────────────────────────────
        public event Action<CharacterEntity> OnEntityAdded;
        public event Action<long>            OnEntityRemoved;
        public event Action<CharacterEntity> OnEntityMoved;
        public event Action<string, string>  OnConsoleMessage;   // msg, hexColor (puede ser "")
        public event Action<long, string, string> OnDialogBubble;  // entityId, msg, hexColor
        public event Action<ushort, byte, byte>   OnTeleport;      // map, posX, posY
        public event Action<string>          OnMapNameChanged;
        public event Action<ushort>          OnOnlineCountChanged;
        public event Action                  OnConnected;      // recibido GetMyCharacter

        // ── Init del jugador local (paquete 1 - GetMyCharacter) ───────────────

        public void InitLocalPlayer(
            long id, string name, byte idClase, ushort map, byte posX, byte posY,
            ushort idHead, ushort idHelmet, ushort idWeapon, ushort idShield, ushort idBody,
            ushort hp, ushort maxHp, ushort mana, ushort maxMana,
            byte privileges, double exp, double expNextLevel, byte level,
            uint gold, byte heading, bool inmovilizado, bool zonaSegura,
            string color, string clan, bool navegando, byte agilidad, byte fuerza)
        {
            var p = LocalPlayer;
            p.Id           = id;
            p.Name         = name;
            p.IdClase      = idClase;
            p.Map          = map;
            p.PosX         = posX;
            p.PosY         = posY;
            p.IdHead       = idHead;
            p.IdHelmet     = idHelmet;
            p.IdWeapon     = idWeapon;
            p.IdShield     = idShield;
            p.IdBody       = idBody;
            p.HP           = hp;
            p.MaxHP        = maxHp;
            p.Mana         = mana;
            p.MaxMana      = maxMana;
            p.Privileges   = privileges;
            p.Exp          = exp;
            p.ExpNextLevel = expNextLevel;
            p.Level        = level;
            p.Gold         = gold;
            p.Heading      = heading;
            p.Inmovilizado = inmovilizado;
            p.ZonaSegura   = zonaSegura;
            p.Color        = color;
            p.Clan         = clan;
            p.Navegando    = navegando;
            p.Agilidad     = agilidad;
            p.Fuerza       = fuerza;

            p.NotifyStatsChanged();
            OnConnected?.Invoke();
        }

        // ── Entidades ─────────────────────────────────────────────────────────

        public CharacterEntity AddOrUpdateEntity(
            long id, string name, EntityType type, byte idClase, ushort map,
            byte posX, byte posY, ushort idHead, ushort idHelmet, ushort idWeapon,
            ushort idShield, ushort idBody, byte heading, string color, string clan,
            byte privileges = 0)
        {
            if (!_entities.TryGetValue(id, out var entity))
            {
                entity = new CharacterEntity { Id = id };
                _entities[id] = entity;
            }

            entity.Name       = name;
            entity.Type       = type;
            entity.IdClase    = idClase;
            entity.Map        = map;
            entity.PosX       = posX;
            entity.PosY       = posY;
            entity.IdHead     = idHead;
            entity.IdHelmet   = idHelmet;
            entity.IdWeapon   = idWeapon;
            entity.IdShield   = idShield;
            entity.IdBody     = idBody;
            entity.Heading    = heading;
            entity.Color      = color;
            entity.Clan       = clan;
            entity.Privileges = privileges;
            entity.TargetPos  = new Vector2(posX, posY);

            OnEntityAdded?.Invoke(entity);
            return entity;
        }

        public void RemoveEntity(long id)
        {
            if (_entities.Remove(id))
                OnEntityRemoved?.Invoke(id);
        }

        public void MoveEntity(long id, byte posX, byte posY)
        {
            if (!_entities.TryGetValue(id, out var entity)) return;

            // Offset inicial para interpolación (32px = 1 tile, igual que el cliente web)
            var prev = entity.TargetPos;
            entity.TargetPos    = new Vector2(posX, posY);
            entity.RenderOffset = (prev - entity.TargetPos) * 32f;
            entity.PosX         = posX;
            entity.PosY         = posY;

            OnEntityMoved?.Invoke(entity);
        }

        public void SetEntityHeading(long id, byte heading)
        {
            if (_entities.TryGetValue(id, out var entity))
                entity.Heading = heading;
        }

        public bool TryGetEntity(long id, out CharacterEntity entity)
            => _entities.TryGetValue(id, out entity);

        // ── Helpers globales ──────────────────────────────────────────────────

        public void AddConsoleMessage(string msg, string hexColor = "")
            => OnConsoleMessage?.Invoke(msg, hexColor);

        public void ShowDialogBubble(long entityId, string msg, string hexColor = "")
            => OnDialogBubble?.Invoke(entityId, msg, hexColor);

        public void Teleport(ushort map, byte posX, byte posY)
        {
            ClearEntities();
            LocalPlayer.Map  = map;
            LocalPlayer.PosX = posX;
            LocalPlayer.PosY = posY;
            OnTeleport?.Invoke(map, posX, posY);
        }

        public void SetMapName(string name)
            => OnMapNameChanged?.Invoke(name);

        public void SetOnlineCount(ushort count)
            => OnOnlineCountChanged?.Invoke(count);

        /// <summary>
        /// Limpia todas las entidades al desconectar/cambiar mapa.
        /// </summary>
        public void ClearEntities()
        {
            var ids = new List<long>(_entities.Keys);
            _entities.Clear();
            foreach (var id in ids)
                OnEntityRemoved?.Invoke(id);
        }

        public void Reset()
        {
            ClearEntities();
            LocalPlayer = new LocalPlayer();
        }
    }
}
