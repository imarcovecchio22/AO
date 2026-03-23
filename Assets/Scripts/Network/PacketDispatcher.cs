using UnityEngine;
using ArgentumOnline.Network.Handlers;

namespace ArgentumOnline.Network
{
    /// <summary>
    /// Lee el primer byte del mensaje recibido y despacha al handler correspondiente.
    /// </summary>
    public class PacketDispatcher
    {
        private readonly ServerPacketHandlers _handlers = new ServerPacketHandlers();

        public void Dispatch(byte[] data)
        {
            if (data == null || data.Length == 0) return;

            var buf = new ByteBuffer(data);
            var id  = (ServerPacketID)buf.ReadByte();

            switch (id)
            {
                case ServerPacketID.GetMyCharacter:    _handlers.OnGetMyCharacter(buf);    break;
                case ServerPacketID.GetCharacter:      _handlers.OnGetCharacter(buf);      break;
                case ServerPacketID.ChangeRopa:        _handlers.OnChangeRopa(buf);        break;
                case ServerPacketID.ActPosition:       _handlers.OnActPosition(buf);       break;
                case ServerPacketID.ChangeHeading:     _handlers.OnChangeHeading(buf);     break;
                case ServerPacketID.DeleteCharacter:   _handlers.OnDeleteCharacter(buf);   break;
                case ServerPacketID.Dialog:            _handlers.OnDialog(buf);            break;
                case ServerPacketID.Console:           _handlers.OnConsole(buf);           break;
                case ServerPacketID.Pong:              NetworkManager.Instance.OnPongReceived(); break;
                case ServerPacketID.AnimFX:            _handlers.OnAnimFX(buf);            break;
                case ServerPacketID.Inmo:              _handlers.OnInmo(buf);              break;
                case ServerPacketID.UpdateHP:          _handlers.OnUpdateHP(buf);          break;
                case ServerPacketID.UpdateMaxHP:       _handlers.OnUpdateMaxHP(buf);       break;
                case ServerPacketID.UpdateMana:        _handlers.OnUpdateMana(buf);        break;
                case ServerPacketID.TeleportMe:        _handlers.OnTeleportMe(buf);        break;
                case ServerPacketID.ActOnline:         _handlers.OnActOnline(buf);         break;
                case ServerPacketID.ActPositionServer: _handlers.OnActPositionServer(buf); break;
                case ServerPacketID.ActExp:            _handlers.OnActExp(buf);            break;
                case ServerPacketID.ActMyLevel:        _handlers.OnActMyLevel(buf);        break;
                case ServerPacketID.ActGold:           _handlers.OnActGold(buf);           break;
                case ServerPacketID.ActColorName:      _handlers.OnActColorName(buf);      break;
                case ServerPacketID.ChangeHelmet:      _handlers.OnChangeHelmet(buf);      break;
                case ServerPacketID.ChangeWeapon:      _handlers.OnChangeWeapon(buf);      break;
                case ServerPacketID.Error:             _handlers.OnError(buf);             break;
                case ServerPacketID.GetNpc:            _handlers.OnGetNpc(buf);            break;
                case ServerPacketID.ChangeShield:      _handlers.OnChangeShield(buf);      break;
                case ServerPacketID.PutBodyAndHeadDead:_handlers.OnPutBodyAndHeadDead(buf);break;
                case ServerPacketID.RevivirUsuario:    _handlers.OnRevivirUsuario(buf);    break;
                case ServerPacketID.QuitarUserInvItem: _handlers.OnQuitarUserInvItem(buf); break;
                case ServerPacketID.RenderItem:        _handlers.OnRenderItem(buf);        break;
                case ServerPacketID.DeleteItem:        _handlers.OnDeleteItem(buf);        break;
                case ServerPacketID.AgregarUserInvItem:_handlers.OnAgregarUserInvItem(buf);break;
                case ServerPacketID.ChangeArrow:       _handlers.OnChangeArrow(buf);       break;
                case ServerPacketID.BlockMap:          _handlers.OnBlockMap(buf);          break;
                case ServerPacketID.ChangeObjIndex:    _handlers.OnChangeObjIndex(buf);    break;
                case ServerPacketID.OpenTrade:         _handlers.OnOpenTrade(buf);         break;
                case ServerPacketID.AprenderSpell:     _handlers.OnAprenderSpell(buf);     break;
                case ServerPacketID.CloseForce:        NetworkManager.Instance.OnCloseForce(); break;
                case ServerPacketID.NameMap:           _handlers.OnNameMap(buf);           break;
                case ServerPacketID.ChangeBody:        _handlers.OnChangeBody(buf);        break;
                case ServerPacketID.Navegando:         _handlers.OnNavegando(buf);         break;
                case ServerPacketID.UpdateAgilidad:    _handlers.OnUpdateAgilidad(buf);    break;
                case ServerPacketID.UpdateFuerza:      _handlers.OnUpdateFuerza(buf);      break;
                case ServerPacketID.PlaySound:         _handlers.OnPlaySound(buf);         break;

                default:
                    Debug.LogWarning($"[Net] Paquete desconocido ID={id} ({(byte)id})");
                    break;
            }
        }
    }
}
