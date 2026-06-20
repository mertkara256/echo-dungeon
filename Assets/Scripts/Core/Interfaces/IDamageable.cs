using UnityEngine;

namespace EchoDungeon.Core.Interfaces
{
    public interface IDamageable
    {
        bool IsDead { get; }
        void TakeDamage(float amount, GameObject instigator = null);
    }
}
