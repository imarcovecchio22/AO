using System;

namespace ArgentumOnline.Game
{
    /// <summary>
    /// Estado completo del jugador local. Mutado por los handlers de red.
    /// Disparar los eventos correspondientes después de cada mutación.
    /// </summary>
    public class LocalPlayer
    {
        public const int InventorySize = 21;
        public const int SpellsSize    = 28;

        // ── Identidad ────────────────────────────────────────────────────────
        public long   Id;
        public string Name;
        public byte   IdClase;
        public string Color;
        public string Clan;
        public byte   Privileges;

        // ── Posición ─────────────────────────────────────────────────────────
        public ushort Map;
        public byte   PosX;
        public byte   PosY;
        public byte   Heading;

        // ── Apariencia ───────────────────────────────────────────────────────
        public ushort IdHead;
        public ushort IdHelmet;
        public ushort IdWeapon;
        public ushort IdShield;
        public ushort IdBody;

        // ── Stats ────────────────────────────────────────────────────────────
        public ushort HP;
        public ushort MaxHP;
        public ushort Mana;
        public ushort MaxMana;
        public byte   Level;
        public double Exp;
        public double ExpNextLevel;
        public uint   Gold;
        public byte   Agilidad;
        public byte   Fuerza;

        // ── Flags ────────────────────────────────────────────────────────────
        public bool Inmovilizado;
        public bool ZonaSegura;
        public bool Navegando;
        public bool IsDead;
        public bool Meditando;

        // ── Inventario (slots 0-20) ───────────────────────────────────────────
        public readonly InventorySlot[] Inventory = new InventorySlot[InventorySize];

        // ── Hechizos (slots 0-27) ─────────────────────────────────────────────
        public readonly SpellSlot[] Spells = new SpellSlot[SpellsSize];

        // ── Eventos ───────────────────────────────────────────────────────────
        public event Action OnStatsChanged;
        public event Action OnInventoryChanged;
        public event Action OnSpellsChanged;
        public event Action OnPositionChanged;
        public event Action OnGoldChanged;
        public event Action<bool> OnMeditandoChanged;

        public LocalPlayer()
        {
            for (int i = 0; i < InventorySize; i++) Inventory[i] = new InventorySlot();
            for (int i = 0; i < SpellsSize; i++)    Spells[i]    = new SpellSlot();
        }

        // ── Mutadores con eventos ─────────────────────────────────────────────

        public void SetHP(ushort hp, ushort maxHp = 0)
        {
            HP = hp;
            if (maxHp > 0) MaxHP = maxHp;
            OnStatsChanged?.Invoke();
        }

        public void SetMana(ushort mana)
        {
            Mana = mana;
            OnStatsChanged?.Invoke();
        }

        public void SetGold(uint gold)
        {
            Gold = gold;
            OnGoldChanged?.Invoke();
        }

        public void SetExp(double exp, double expNext = 0, byte level = 0, ushort maxHp = 0, ushort maxMana = 0)
        {
            Exp = exp;
            if (expNext  > 0) ExpNextLevel = expNext;
            if (level    > 0) Level        = level;
            if (maxHp    > 0) MaxHP        = maxHp;
            if (maxMana  > 0) MaxMana      = maxMana;
            OnStatsChanged?.Invoke();
        }

        public void SetPosition(byte x, byte y, byte heading)
        {
            PosX    = x;
            PosY    = y;
            Heading = heading;
            OnPositionChanged?.Invoke();
        }

        public void SetInventorySlot(byte idPos, uint idItem, string name, bool equipped,
                                     ushort grhIndex, ushort cant, uint valor, byte objType,
                                     bool validParaClase, string dataObj)
        {
            if (idPos >= InventorySize) return;
            var slot = Inventory[idPos];
            slot.IdPos         = idPos;
            slot.IdItem        = idItem;
            slot.Name          = name;
            slot.Equipped      = equipped;
            slot.GrhIndex      = grhIndex;
            slot.Cantidad      = cant;
            slot.Valor         = valor;
            slot.ObjType       = objType;
            slot.ValidParaClase= validParaClase;
            slot.DataObj       = dataObj;
            OnInventoryChanged?.Invoke();
        }

        public void RemoveFromInventory(byte idPos, ushort cant)
        {
            if (idPos >= InventorySize) return;
            var slot = Inventory[idPos];
            if (slot.Cantidad <= cant)
            {
                // Limpiar slot
                slot.IdItem   = 0;
                slot.Name     = string.Empty;
                slot.Cantidad = 0;
            }
            else
            {
                slot.Cantidad -= cant;
            }
            OnInventoryChanged?.Invoke();
        }

        public void SwapSpells(int a, int b)
        {
            if (a < 0 || a >= SpellsSize || b < 0 || b >= SpellsSize) return;
            var tmp   = Spells[a];
            Spells[a] = Spells[b];
            Spells[b] = tmp;
            OnSpellsChanged?.Invoke();
        }

        public void SetSpellSlot(byte idPos, ushort idSpell, string name, ushort manaRequired)
        {
            if (idPos >= SpellsSize) return;
            var slot = Spells[idPos];
            slot.IdPos        = idPos;
            slot.IdSpell      = idSpell;
            slot.Name         = name;
            slot.ManaRequired = manaRequired;
            OnSpellsChanged?.Invoke();
        }

        public void SetMeditando(bool value)
        {
            if (Meditando == value) return;
            Meditando = value;
            OnMeditandoChanged?.Invoke(value);
        }

        public void NotifyStatsChanged()   => OnStatsChanged?.Invoke();
        public void NotifyPositionChanged() => OnPositionChanged?.Invoke();

        public bool HasMana => MaxMana > 0;
    }
}
