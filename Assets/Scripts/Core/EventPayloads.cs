using UnityEngine;
using Cursor.Stats;

namespace Cursor.Core
{
    /// <summary>
    /// Defines the lifetime scope of an event subscription.
    /// Global: persists for the entire application lifetime.
    /// Session: valid only during a gameplay session (cleared on session start/end).
    /// Menu: valid only while in menu scenes (cleared when entering gameplay).
    /// </summary>
    public enum EventScope
    {
        Global,
        Session,
        Menu
    }

    /// <summary>
    /// Core game states. Managed by GameManager.
    /// </summary>
    public enum GameState
    {
        Menu,
        Upgrade,
        Gameplay,
        Summary
    }

    // --- Event Payload Structs ---
    // These are intentionally lightweight. Extend with fields as systems are implemented.

    /// <summary>
    /// Emitted when player HP reaches zero.
    /// </summary>
    public struct PlayerDeadEventArgs { }

    /// <summary>
    /// Emitted when a gameplay session is manually stopped.
    /// </summary>
    public struct SessionStopEventArgs { }

    /// <summary>
    /// Emitted when an enemy is killed.
    /// TODO: Add EnemyType field when EnemyType enum is defined.
    /// </summary>
    public struct EnemyKilledEventArgs
    {
        public Vector2 Position;
        public EnemyType Type;
        public Data.EnemySubtype Subtype;
        public int CollectableCount;
    }

    /// <summary>
    /// Emitted when a collectable is picked up by the player.
    /// TODO: Add CollectableType field when CollectableType enum is defined.
    /// </summary>
    public struct CollectablePickedUpEventArgs
    {
        public int Amount;
        public Gameplay.CollectableType Type;
    }

    /// <summary>
    /// Emitted when an upgrade is successfully purchased.
    /// TODO: Add Upgrade reference field when Upgrade class is defined.
    /// </summary>
    public struct UpgradePurchasedEventArgs
    {
        public string UpgradeId;
        // public Upgrade Upgrade;
    }

    /// <summary>
    /// Emitted when the game state changes (e.g. Upgrade -> Gameplay -> Summary).
    /// </summary>
    public struct GameStateChangedEventArgs
    {
        public GameState State;
    }
}
