using UnityEngine;
using ArgentumOnline.Game;

namespace ArgentumOnline.Network.Handlers
{
    public class ServerPacketHandlers
    {
        private GameState GS => GameState.Instance;

        // ── Entidades ────────────────────────────────────────────────────────

        public void OnGetMyCharacter(ByteBuffer buf)
        {
            long   id           = buf.ReadId();
            string name         = buf.ReadString();
            byte   idClase      = buf.ReadByte();
            ushort map          = buf.ReadShort();
            byte   posX         = buf.ReadByte();
            byte   posY         = buf.ReadByte();
            ushort idHead       = buf.ReadShort();
            ushort idHelmet     = buf.ReadShort();
            ushort idWeapon     = buf.ReadShort();
            ushort idShield     = buf.ReadShort();
            ushort idBody       = buf.ReadShort();
            ushort hp           = buf.ReadShort();
            ushort maxHp        = buf.ReadShort();
            ushort mana         = buf.ReadShort();
            ushort maxMana      = buf.ReadShort();
            byte   privileges   = buf.ReadByte();
            double exp          = buf.ReadDouble();
            double expNextLevel = buf.ReadDouble();
            byte   level        = buf.ReadByte();
            uint   gold         = buf.ReadInt();
            byte   heading      = buf.ReadByte();
            byte   inmo         = buf.ReadByte();
            byte   segura       = buf.ReadByte();
            string color        = buf.ReadString();
            string clan         = buf.ReadString();
            byte   navegando    = buf.ReadByte();
            byte   agilidad     = buf.ReadByte();
            byte   fuerza       = buf.ReadByte();

            GS.InitLocalPlayer(id, name, idClase, map, posX, posY,
                idHead, idHelmet, idWeapon, idShield, idBody,
                hp, maxHp, mana, maxMana, privileges, exp, expNextLevel, level,
                gold, heading, inmo == 1, segura == 1, color, clan, navegando == 1,
                agilidad, fuerza);

            byte invCount = buf.ReadByte();
            for (int i = 0; i < invCount; i++)
            {
                byte   idPos          = buf.ReadByte();
                uint   idItem         = buf.ReadInt();
                string itemName       = buf.ReadString();
                byte   equipped       = buf.ReadByte();
                ushort grhIndex       = buf.ReadShort();
                ushort cant           = buf.ReadShort();
                uint   valor          = buf.ReadInt();
                byte   objType        = buf.ReadByte();
                byte   validParaClase = buf.ReadByte();
                string dataObj        = buf.ReadString();
                GS.LocalPlayer.SetInventorySlot(idPos, idItem, itemName, equipped == 1,
                    grhIndex, cant, valor, objType, validParaClase == 1, dataObj);
            }

            if (maxMana > 0)
            {
                byte spellsCount = buf.ReadByte();
                Debug.Log($"[Net] Hechizos recibidos en getMyCharacter: {spellsCount}");
                for (int i = 0; i < spellsCount; i++)
                {
                    byte   idPos        = buf.ReadByte();
                    ushort idSpell      = buf.ReadShort();
                    string spellName    = buf.ReadString();
                    ushort manaRequired = buf.ReadShort();
                    Debug.Log($"[Net]   Hechizo slot={idPos} id={idSpell} nombre={spellName}");
                    GS.LocalPlayer.SetSpellSlot(idPos, idSpell, spellName, manaRequired);
                }
            }

            Debug.Log($"[Net] GetMyCharacter: {name} lv{level} @ {posX},{posY} mapa {map}");
        }

        public void OnGetCharacter(ByteBuffer buf)
        {
            long   id         = buf.ReadId();
            string name       = buf.ReadString();
            byte   idClase    = buf.ReadByte();
            ushort map        = buf.ReadShort();
            byte   posX       = buf.ReadByte();
            byte   posY       = buf.ReadByte();
            ushort idHead     = buf.ReadShort();
            ushort idHelmet   = buf.ReadShort();
            ushort idWeapon   = buf.ReadShort();
            ushort idShield   = buf.ReadShort();
            ushort idBody     = buf.ReadShort();
            byte   privileges = buf.ReadByte();
            byte   heading    = buf.ReadByte();
            string color      = buf.ReadString();
            string clan       = buf.ReadString();

            GS.AddOrUpdateEntity(id, name, EntityType.Player, idClase, map,
                posX, posY, idHead, idHelmet, idWeapon, idShield, idBody,
                heading, color, clan, privileges);
        }

        public void OnGetNpc(ByteBuffer buf)
        {
            long   id       = buf.ReadId();
            string name     = buf.ReadString();
            byte   idClase  = buf.ReadByte();
            ushort map      = buf.ReadShort();
            byte   posX     = buf.ReadByte();
            byte   posY     = buf.ReadByte();
            ushort idHead   = buf.ReadShort();
            ushort idHelmet = buf.ReadShort();
            ushort idWeapon = buf.ReadShort();
            ushort idShield = buf.ReadShort();
            ushort idBody   = buf.ReadShort();
            byte   heading  = buf.ReadByte();
            string color    = buf.ReadString();
            string clan     = buf.ReadString();

            GS.AddOrUpdateEntity(id, name, EntityType.NPC, idClase, map,
                posX, posY, idHead, idHelmet, idWeapon, idShield, idBody,
                heading, color, clan);
        }

        public void OnDeleteCharacter(ByteBuffer buf)
        {
            long id = buf.ReadId();
            GS.RemoveEntity(id);
        }

        // ── Movimiento ───────────────────────────────────────────────────────

        public void OnActPosition(ByteBuffer buf)
        {
            long id   = buf.ReadId();
            byte posX = buf.ReadByte();
            byte posY = buf.ReadByte();
            GS.MoveEntity(id, posX, posY);
        }

        public void OnChangeHeading(ByteBuffer buf)
        {
            long id      = buf.ReadId();
            byte heading = buf.ReadByte();
            GS.SetEntityHeading(id, heading);
        }

        public void OnActPositionServer(ByteBuffer buf)
        {
            byte posX    = buf.ReadByte();
            byte posY    = buf.ReadByte();
            byte heading = buf.ReadByte();
            GS.LocalPlayer.SetPosition(posX, posY, heading);
        }

        public void OnTeleportMe(ByteBuffer buf)
        {
            long   id   = buf.ReadId();
            ushort map  = buf.ReadShort();
            byte   posX = buf.ReadByte();
            byte   posY = buf.ReadByte();

            Debug.Log($"[Net] Teleport a mapa {map} @ {posX},{posY}");
            GS.Teleport(map, posX, posY);
        }

        // ── Stats ────────────────────────────────────────────────────────────

        public void OnUpdateHP(ByteBuffer buf)
        {
            ushort hp = buf.ReadShort();
            GS.LocalPlayer.SetHP(hp);
        }

        public void OnUpdateMaxHP(ByteBuffer buf)
        {
            ushort hp    = buf.ReadShort();
            ushort maxHp = buf.ReadShort();
            GS.LocalPlayer.SetHP(hp, maxHp);
        }

        public void OnUpdateMana(ByteBuffer buf)
        {
            ushort mana = buf.ReadShort();
            GS.LocalPlayer.SetMana(mana);
        }

        public void OnActExp(ByteBuffer buf)
        {
            double exp = buf.ReadDouble();
            GS.LocalPlayer.SetExp(exp);
        }

        public void OnActMyLevel(ByteBuffer buf)
        {
            double exp          = buf.ReadDouble();
            double expNextLevel = buf.ReadDouble();
            byte   level        = buf.ReadByte();
            ushort maxHp        = buf.ReadShort();
            ushort maxMana      = buf.ReadShort();
            GS.LocalPlayer.SetExp(exp, expNextLevel, level, maxHp, maxMana);
            Debug.Log($"[Net] Nivel {level}!");
        }

        public void OnActGold(ByteBuffer buf)
        {
            uint gold = buf.ReadInt();
            GS.LocalPlayer.SetGold(gold);
        }

        public void OnActOnline(ByteBuffer buf)
        {
            ushort online = buf.ReadShort();
            GS.SetOnlineCount(online);
            GS.AddConsoleMessage($"Usuarios online: {online}", "#aaddff");
        }

        public void OnUpdateAgilidad(ByteBuffer buf)
        {
            GS.LocalPlayer.Agilidad = buf.ReadByte();
            GS.LocalPlayer.NotifyStatsChanged();
        }

        public void OnUpdateFuerza(ByteBuffer buf)
        {
            GS.LocalPlayer.Fuerza = buf.ReadByte();
            GS.LocalPlayer.NotifyStatsChanged();
        }

        public void OnNameMap(ByteBuffer buf)
        {
            string name = buf.ReadString();
            GS.SetMapName(name);
        }

        // ── Apariencia ───────────────────────────────────────────────────────

        public void OnChangeRopa(ByteBuffer buf)
        {
            long id = buf.ReadId(); ushort idBody = buf.ReadShort(); byte idPos = buf.ReadByte();
            if (GS.TryGetEntity(id, out var e)) e.IdBody = idBody;
        }

        public void OnChangeWeapon(ByteBuffer buf)
        {
            long id = buf.ReadId(); ushort idWeapon = buf.ReadShort(); byte idPos = buf.ReadByte();
            if (GS.TryGetEntity(id, out var e)) e.IdWeapon = idWeapon;
        }

        public void OnChangeShield(ByteBuffer buf)
        {
            long id = buf.ReadId(); ushort idShield = buf.ReadShort(); byte idPos = buf.ReadByte();
            if (GS.TryGetEntity(id, out var e)) e.IdShield = idShield;
        }

        public void OnChangeHelmet(ByteBuffer buf)
        {
            long id = buf.ReadId(); ushort idHelmet = buf.ReadShort(); byte idPos = buf.ReadByte();
            if (GS.TryGetEntity(id, out var e)) e.IdHelmet = idHelmet;
        }

        public void OnChangeArrow(ByteBuffer buf)
        {
            long id = buf.ReadId(); byte idPos = buf.ReadByte();
            // Arrow no modifica el render todavía
        }

        public void OnChangeBody(ByteBuffer buf)
        {
            long   id       = buf.ReadId();
            ushort idHead   = buf.ReadShort();
            ushort idBody   = buf.ReadShort();
            ushort idHelmet = buf.ReadShort();
            ushort idWeapon = buf.ReadShort();
            ushort idShield = buf.ReadShort();
            if (GS.TryGetEntity(id, out var e))
            {
                e.IdHead   = idHead;
                e.IdBody   = idBody;
                e.IdHelmet = idHelmet;
                e.IdWeapon = idWeapon;
                e.IdShield = idShield;
            }
        }

        public void OnPutBodyAndHeadDead(ByteBuffer buf)
        {
            long   id       = buf.ReadId();
            ushort idHead   = buf.ReadShort();
            ushort idHelmet = buf.ReadShort();
            ushort idWeapon = buf.ReadShort();
            ushort idShield = buf.ReadShort();
            ushort idBody   = buf.ReadShort();

            var local = GS.LocalPlayer;
            if (id == local.Id)
            {
                local.IdHead   = idHead;
                local.IdBody   = idBody;
                local.IdHelmet = idHelmet;
                local.IdWeapon = idWeapon;
                local.IdShield = idShield;
                local.IsDead   = true;
                ArgentumOnline.Renderer.EntityRenderer.Instance.RefreshLocalPlayerAppearance();
            }
            else if (GS.TryGetEntity(id, out var e))
            {
                e.IdHead = idHead; e.IdBody = idBody;
                e.IdHelmet = idHelmet; e.IdWeapon = idWeapon; e.IdShield = idShield;
                e.IsDead = true;
                ArgentumOnline.Renderer.EntityRenderer.Instance.RefreshEntityView(id);
            }
        }

        public void OnRevivirUsuario(ByteBuffer buf)
        {
            long   id     = buf.ReadId();
            ushort idHead = buf.ReadShort();
            ushort idBody = buf.ReadShort();

            var local = GS.LocalPlayer;
            if (id == local.Id)
            {
                local.IdHead = idHead;
                local.IdBody = idBody;
                local.IsDead = false;
                ArgentumOnline.Renderer.EntityRenderer.Instance.RefreshLocalPlayerAppearance();
            }
            else if (GS.TryGetEntity(id, out var e))
            {
                e.IsDead = false;
                e.IdHead = idHead;
                e.IdBody = idBody;
                ArgentumOnline.Renderer.EntityRenderer.Instance.RefreshEntityView(id);
            }
        }

        public void OnActColorName(ByteBuffer buf)
        {
            long id = buf.ReadId(); string color = buf.ReadString();
            if (GS.TryGetEntity(id, out var e)) e.Color = color;
        }

        public void OnNavegando(ByteBuffer buf)
        {
            GS.LocalPlayer.Navegando = buf.ReadByte() == 1;
        }

        // ── Efectos / sonido ─────────────────────────────────────────────────

        public void OnAnimFX(ByteBuffer buf)
        {
            long   id    = buf.ReadId();
            ushort fxGrh = buf.ReadShort();
            Renderer.FXManager.Instance?.PlayFX(id, fxGrh);
        }

        public void OnPlaySound(ByteBuffer buf)
        {
            byte idSound = buf.ReadByte();
            // TODO: AudioManager.Instance.PlaySound(idSound)
        }

        public void OnInmo(ByteBuffer buf)
        {
            byte inmo = buf.ReadByte();
            byte posX = buf.ReadByte();
            byte posY = buf.ReadByte();
            GS.LocalPlayer.Inmovilizado = inmo == 1;
            GS.LocalPlayer.PosX = posX;
            GS.LocalPlayer.PosY = posY;
        }

        // ── Diálogo y consola ────────────────────────────────────────────────

        public void OnDialog(ByteBuffer buf)
        {
            long   id      = buf.ReadId();
            string msg     = buf.ReadString();
            byte   hasName = buf.ReadByte();
            string name    = hasName == 1 ? buf.ReadString() : string.Empty;
            string color   = buf.ReadString();
            byte   console = buf.ReadByte();

            if (console == 1)
                GS.AddConsoleMessage(msg, color);

            GS.ShowDialogBubble(id, msg, color);
        }

        public void OnConsole(ByteBuffer buf)
        {
            string msg      = buf.ReadString();
            byte   hasColor = buf.ReadByte();
            string color    = hasColor == 1 ? buf.ReadString() : string.Empty;
            byte   bold     = buf.ReadByte();
            byte   italica  = buf.ReadByte();
            GS.AddConsoleMessage(msg, color);
        }

        public void OnError(ByteBuffer buf)
        {
            string msg = buf.ReadString();
            Debug.LogError($"[Server] {msg}");
            GS.AddConsoleMessage(msg, "#ff4444");
        }

        // ── Inventario y mapa ────────────────────────────────────────────────

        public void OnAgregarUserInvItem(ByteBuffer buf)
        {
            byte   idPos          = buf.ReadByte();
            uint   idItem         = buf.ReadInt();
            string name           = buf.ReadString();
            byte   equipped       = buf.ReadByte();
            ushort grhIndex       = buf.ReadShort();
            ushort cant           = buf.ReadShort();
            uint   valor          = buf.ReadInt();
            byte   objType        = buf.ReadByte();
            byte   validParaClase = buf.ReadByte();
            string dataObj        = buf.ReadString();
            GS.LocalPlayer.SetInventorySlot(idPos, idItem, name, equipped == 1,
                grhIndex, cant, valor, objType, validParaClase == 1, dataObj);
        }

        public void OnQuitarUserInvItem(ByteBuffer buf)
        {
            long   id    = buf.ReadId();
            byte   idPos = buf.ReadByte();
            ushort cant  = buf.ReadShort();
            GS.LocalPlayer.RemoveFromInventory(idPos, cant);
        }

        public void OnRenderItem(ByteBuffer buf)
        {
            uint   idItem = buf.ReadInt();
            ushort idMap  = buf.ReadShort();
            byte   posX   = buf.ReadByte();
            byte   posY   = buf.ReadByte();
            Renderer.MapRenderer.Instance?.SetTileObj(idMap, posX, posY, (int)idItem);
        }

        public void OnDeleteItem(ByteBuffer buf)
        {
            ushort idMap = buf.ReadShort();
            byte   posX  = buf.ReadByte();
            byte   posY  = buf.ReadByte();
            Renderer.MapRenderer.Instance?.SetTileObj(idMap, posX, posY, 0);
        }

        public void OnBlockMap(ByteBuffer buf)
        {
            ushort idMap = buf.ReadShort();
            byte   posX  = buf.ReadByte();
            byte   posY  = buf.ReadByte();
            byte   block = buf.ReadByte();
            // TODO: MapManager.Instance.SetBlocked(idMap, posX, posY, block == 1)
        }

        public void OnChangeObjIndex(ByteBuffer buf)
        {
            ushort idMap    = buf.ReadShort();
            byte   posX     = buf.ReadByte();
            byte   posY     = buf.ReadByte();
            uint   objIndex = buf.ReadInt();
            // TODO: MapManager.Instance.SetObjIndex(idMap, posX, posY, objIndex)
        }

        // ── Comercio ─────────────────────────────────────────────────────────

        public void OnOpenTrade(ByteBuffer buf)
        {
            byte npcCount = buf.ReadByte();
            var npcItems  = new Game.NpcTradeItem[npcCount];
            for (byte i = 0; i < npcCount; i++)
            {
                npcItems[i] = new Game.NpcTradeItem
                {
                    IdPos          = i,
                    GrhIndex       = buf.ReadShort(),
                    Name           = buf.ReadString(),
                    Cantidad       = buf.ReadShort(),
                    Valor          = buf.ReadInt(),
                    ValidParaClase = buf.ReadByte() == 1,
                    DataObj        = buf.ReadString(),
                };
            }

            byte userCount = buf.ReadByte();
            var userItems  = new Game.UserTradeItem[userCount];
            for (int i = 0; i < userCount; i++)
            {
                userItems[i] = new Game.UserTradeItem
                {
                    GrhIndex       = buf.ReadShort(),
                    Name           = buf.ReadString(),
                    Cantidad       = buf.ReadShort(),
                    ValorVenta     = buf.ReadInt(),
                    Equipped       = buf.ReadByte() == 1,
                    IdPos          = buf.ReadByte(),
                    ValidParaClase = buf.ReadByte() == 1,
                    DataObj        = buf.ReadString(),
                };
            }

            GS.OpenTrade(new Game.TradeData { NpcItems = npcItems, UserItems = userItems });
        }

        public void OnAprenderSpell(ByteBuffer buf)
        {
            byte   idPos        = buf.ReadByte();
            ushort idSpell      = buf.ReadShort();
            string name         = buf.ReadString();
            ushort manaRequired = buf.ReadShort();
            GS.LocalPlayer.SetSpellSlot(idPos, idSpell, name, manaRequired);
        }
    }
}
