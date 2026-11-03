public interface IVfxDatabase
{
    // CardEffect.vfx(에셋 이름)로 공격 이펙트를 찾는다. 비거나 없으면 null.
    AttackVfxCue GetCue(string cueName);
}
