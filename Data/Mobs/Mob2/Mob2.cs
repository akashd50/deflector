using Deflector.Data.BehaviorTree;
using Deflector.Data.Shared;
using Godot;

namespace Deflector.Data.Mobs.Mob2;

public partial class Mob2 : MobBehavior, IDamageable
{
	public const string WideSlashL2R = "wide_slash_l2r";
		
	public override void _Ready()
	{
		WeaponAnimationHelper = new AnimationHelper(GetNode<AnimationPlayer>("WeaponsGroup/AnimationPlayer"), this);
		Init();
		Body.Modulate = new Color(0.5f, 0.5f, 0.5f);
		Eye.Modulate  = new Color(1.0f, 0.3f, 0.3f);
	}

	public void TakeDamage(int damage)
	{
		GD.Print("Damage taken", damage);
	}

	// Weapon subtree, evaluated by Engage when the mob is in attack range.
	//   Locked      — an animation is playing → wait it out, don't stomp it
	//   Draw        — weapon not yet drawn    → play the draw anim once
	//   Attack      — in range + off cooldown → swing
	//   ReadyStance — fallback                → hold position, face the player
	protected override BTNode BuildWeaponTree()
	{
		return new Selector([
			new Sequence([
				new Condition(IsWeaponAnimPlaying),
				new ActionNode(WaitForAnim),
			]),
			// Weapon selection from range sequence
			new Sequence([
				new Condition(() => Blackboard.CurrentWeaponSelection == "Sword"),
				new Sequence([
					new Inverter(new Condition(IsWeaponDrawn)),
					new ActionNode(() => PlayDrawAnim("sword_draw_l")),
					new ActionNode(() =>
					{
						Blackboard.CurrentWeaponSelection = null;
						return PlayAttack("wide_slash_l2r");
					}),
				]),
			]),
			new Sequence([
				new Inverter(new Condition(IsWeaponDrawn)),
				new ActionNode(() => PlayDrawAnim("sword_draw_l")),
			]),
			// Sword attack sequence
			new Sequence([
				new Condition(IsWithinAttackRange),
				new Condition(IsOffCooldown),
				new ActionNode(() => PlayAttack("wide_slash_l2r")),
				new ActionNode(() => PlayAttack("sword_reset", () => Blackboard.IsWeaponDrawn = false)),
			]),
			new ActionNode(ReadyStance),
		]);
	}
}
