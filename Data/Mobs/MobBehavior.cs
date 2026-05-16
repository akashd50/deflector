using System;
using Deflector.Data.BehaviorTree;
using Deflector.Data.Shared;
using Godot;

namespace Deflector.Data.Mobs;

public partial class MobBehavior : CharacterBody2D, IUsesAnimationHelper
{
	// --- Detection / vision ---
	[Export] public int DetectionRange   = 1200;
	[Export] public int PeripheralRange  = 200;
	[Export] public int VisibleConeAngle = 45;

	// --- Movement ---
	[Export] public int WalkSpeed            = 30;
	[Export] public int RunSpeed             = 100;
	[Export] public int DashSpeed            = 1200;
	[Export] public int RotationSpeed        = 3;
	[Export] public int PreferredCombatRange = 110;

	// --- Awareness model ---
	[Export] public float AwarenessRiseRate           = 120f;
	[Export] public float AwarenessPeripheralRiseRate = 220f;
	[Export] public float AwarenessDecayRate          = 10f;
	[Export] public float WaryThreshold               = 30f;
	[Export] public float AggroThreshold              = 150f;
	[Export] public float ForgetThreshold             = 5f;

	// --- Wander / strafe ---
	[Export] public int WanderLeashMinMs = 1200;
	[Export] public int WanderLeashMaxMs = 2400;
	[Export] public int StrafeFlipMinMs  = 600;
	[Export] public int StrafeFlipMaxMs  = 1300;

	// --- Investigate ---
	[Export] public int InvestigateArriveDist   = 24;
	[Export] public int InvestigateLookAroundMs = 1200;

	protected Vector2         WalkDirection = Vector2.Zero;
	protected Vector2         FaceDirection = Vector2.Zero;
	protected Sprite2D        Body;
	protected Sprite2D        Eye;
	protected MobWeaponsGroup MobWeaponsGroup;
	protected MobBlackboard   Blackboard;
	protected AnimationHelper WeaponAnimationHelper;

	private Player.Player _player;
	private Random        _random;
	private BTNode        _treeRoot;
	private BTNode        _weaponTreeRoot;

	// ----------------------------------------------------------------------------------------------------------------
	// Lifecycle
	// ----------------------------------------------------------------------------------------------------------------

	protected Player.Player TryGetPlayer()
	{
		if (_player == null && GetTree().GetFirstNodeInGroup("Player") is Player.Player player)
		{
			_player = player;
		}
		return _player;
	}

	protected void Init()
	{
		AddToGroup("Enemies");
		AddToGroup("Persist");
		Body = GetNode<Sprite2D>("BodyParts/Body");
		Eye  = GetNode<Sprite2D>("BodyParts/Eye");

		_random = new Random();
		TryGetPlayer();

		Blackboard      = new MobBlackboard();
		FaceDirection   = Vector2.Right.Rotated(Rotation);
		MobWeaponsGroup = GetNode<MobWeaponsGroup>("WeaponsGroup");
		_weaponTreeRoot = BuildWeaponTree();
		_treeRoot       = BuildBehavioralTree();
	}

	public override void _PhysicsProcess(double delta)
	{
		UpdateAwareness((float)delta);
		_treeRoot?.Evaluate();
		MoveAndCollide(Velocity * (float)delta);
		ApplyDrag();
	}

	// ----------------------------------------------------------------------------------------------------------------
	// Extensibility hooks — subclasses override these to shape per-mob behavior.
	// ----------------------------------------------------------------------------------------------------------------

	// Default mob brain. Branches are evaluated top-down each tick — the first
	// whose conditions hold wins:
	//   Engage      — in attack range and visible    → run the weapon subtree
	//   Chase       — aggro and visible              → run into attack range
	//   Investigate — has a last-known position      → walk to it, then look around
	//   Wary        — aware but no fix on the player → leashed wander toward action
	//   Idle        — fallback                       → hold position
	protected virtual BTNode BuildBehavioralTree()
	{
		return new Selector([
			new Sequence([
				new Condition(IsAware),
				new Condition(IsPlayerVisible),
				// new Condition(IsWithinAttackRange),
				new ActionNode(Engage),
			]),
			new Sequence([
				new Condition(IsAggro),
				new Condition(IsPlayerVisible),
				new ActionNode(Chase),
			]),
			new Sequence([
				new Condition(HasLastKnownPos),
				new ActionNode(Investigate),
				new ActionNode(LookAround),
			]),
			new Sequence([
				new Condition(IsAware),
				new ActionNode(Wander),
			]),
			new ActionNode(Idle),
		]);
	}

	// Weapon subtree — assembled per mob, evaluated when Engage fires. Returning
	// null means "no weapon", and Engage degrades to just facing the player.
	protected virtual BTNode BuildWeaponTree() => null;

	// Animation helper used by the weapon subtree. Default is the helper owned
	// by the mob; Mob1-style setups override to return their Weapon's helper.
	protected virtual AnimationHelper GetWeaponAnimationHelper() => WeaponAnimationHelper;

	// Notified when a weapon hitbox lands a hit. Subclasses override to react
	// (e.g. trigger a cooldown). Wired up by the weapon owner — see Mob1.
	protected virtual void OnHitDone() { }

	// ----------------------------------------------------------------------------------------------------------------
	// Awareness — drives the visibility/last-known-position model the BT reads.
	// ----------------------------------------------------------------------------------------------------------------

	private void UpdateAwareness(float delta)
	{
		var toPlayer         = ToPlayer();
		var dist             = toPlayer.Length();
		var inDetectionRange = dist <= DetectionRange;
		var inPeripheral     = dist <= PeripheralRange;
		var inCone           = inDetectionRange && IsInsideVisionCone(toPlayer);

		if (inPeripheral || inCone)
		{
			var rate             = inPeripheral ? AwarenessPeripheralRiseRate : AwarenessRiseRate;
			Blackboard.Awareness = Math.Min(100f, Blackboard.Awareness + rate * delta);
			Blackboard.OnPlayerSeen(TryGetPlayer().GlobalPosition);
			// GD.Print("Seen player", Blackboard.Awareness);
		}
		else
		{
			Blackboard.Awareness = Math.Max(0f, Blackboard.Awareness - AwarenessDecayRate * delta);
			if (Blackboard.Awareness < ForgetThreshold)
			{
				// GD.Print("Forgot player", Blackboard.Awareness);
				Blackboard.LastKnownPlayerPos = null;
			}
		}
	}

	// ----------------------------------------------------------------------------------------------------------------
	// BT conditions — pure predicates, no side effects. Wrap in Condition(...) for the tree.
	// ----------------------------------------------------------------------------------------------------------------

	protected bool IsAwarenessAbove(float threshold) => Blackboard.Awareness >= threshold;
	protected bool IsAware()                          => IsAwarenessAbove(WaryThreshold);
	protected bool IsAggro()                          => IsAwarenessAbove(AggroThreshold);
	protected bool HasLastKnownPos()                  => Blackboard.LastKnownPlayerPos.HasValue;

	public bool IsPlayerVisible()
	{
		var to   = ToPlayer();
		var dist = to.Length();
		if (dist <= PeripheralRange) return true;
		return dist <= DetectionRange && IsInsideVisionCone(to);
	}

	public bool IsWithinAttackRange()
	{
		var player = TryGetPlayer();
		if (player == null || MobWeaponsGroup == null) return false;
		return MobWeaponsGroup.IsPlayerInRange(player);
	}

	// Weapon BT predicates.
	protected bool IsWeaponAnimPlaying() => GetWeaponAnimationHelper()?.IsAnimating ?? false;
	protected bool IsWeaponDrawn()       => Blackboard.IsWeaponDrawn;
	protected bool IsOffCooldown()       => true; //Blackboard.NowMs >= Blackboard.NextAttackReadyMs;

	// ----------------------------------------------------------------------------------------------------------------
	// BT actions — drive motion/rotation and self-manage their own timers. Wrap in ActionNode(...) for the tree.
	// ----------------------------------------------------------------------------------------------------------------

	protected NodeState Idle()
	{
		// Hold position. Selector falls through to this when nothing else fires.
		return NodeState.Success;
	}

	protected NodeState Wander()
	{
		TrackTowardsPlayer();
		LeashedWander(WalkSpeed);
		return NodeState.Running;
	}

	protected NodeState Investigate()
	{
		if (!Blackboard.LastKnownPlayerPos.HasValue) return NodeState.Failure;
		if (HasArrivedAtLastKnown())                  return NodeState.Success;

		TrackTowards(Blackboard.LastKnownPlayerPos.Value, RotationSpeed);
		GoTo(Blackboard.LastKnownPlayerPos.Value, WalkSpeed * 2);
		return NodeState.Running;
	}

	protected NodeState LookAround()
	{
		if (Blackboard.LookAroundStartMs == 0)
		{
			Blackboard.LookAroundStartMs = Blackboard.NowMs;
		}

		LookAroundSweep();

		if (Blackboard.NowMs - Blackboard.LookAroundStartMs >= (ulong)InvestigateLookAroundMs)
		{
			Blackboard.LookAroundStartMs  = 0;
			Blackboard.LastKnownPlayerPos = null;
			return NodeState.Success;
		}
		return NodeState.Running;
	}

	protected NodeState Chase()
	{
		var player = TryGetPlayer();
		if (player == null) return NodeState.Failure;

		TrackTowardsPlayer();
		ApproachToRange(player.GlobalPosition, (int)MobWeaponsGroup.GetRandomWeaponRange(), RunSpeed);
		return NodeState.Running;
	}

	protected NodeState Strafe()
	{
		TrackTowardsPlayer();
		StrafeAroundPlayer(PreferredCombatRange, WalkSpeed * 2);
		return NodeState.Running;
	}

	protected NodeState FacePlayer()
	{
		TrackTowardsPlayer(RotationSpeed * 3);
		return NodeState.Running;
	}

	// Engage = "in attack range, what should the weapon do?". Dispatches into the
	// per-mob weapon subtree if one was built; otherwise just faces the player.
	protected NodeState Engage()
	{
		return _weaponTreeRoot?.Evaluate() ?? FacePlayer();
	}

	// Weapon BT leaves. Animation playback is fire-and-forget through the helper —
	// the BT polls IsAnimating via the top "AnimationLocked" branch to avoid
	// stomping on a swing in progress.

	// Sits Running while an animation is playing. Wrap with a Condition(IsWeaponAnimPlaying)
	// in front to keep it from blocking when nothing is playing.
	protected NodeState WaitForAnim()
	{
		return IsWeaponAnimPlaying() ? NodeState.Running : NodeState.Success;
	}

	// Queues a draw animation and flips the "drawn" flag so this branch won't
	// fire again. Stays Running so the AnimationLocked branch picks up the swing
	// next tick.
	protected NodeState PlayDrawAnim(string animName)
	{
		var helper = GetWeaponAnimationHelper();
		if (helper == null)     return NodeState.Failure;
		if (helper.IsAnimating) return NodeState.Running;
		helper.QueueAnimation(animName);
		Blackboard.IsWeaponDrawn = true;
		return NodeState.Running;
	}

	// Queues an attack animation. Cooldown gating is the caller's responsibility
	// (via a Condition(IsOffCooldown) in front) — the actual cooldown timer is
	// armed by OnHitDone / future fairness logic.
	protected NodeState PlayAttack(string animName)
	{
		var helper = GetWeaponAnimationHelper();
		if (helper == null)     return NodeState.Failure;
		if (helper.IsAnimating) return NodeState.Running;
		helper.QueueAnimation(animName);
		return NodeState.Running;
	}

	// "Ready stance" fallback for the weapon subtree: hold the weapon up while
	// tracking the player, doing nothing destructive. Returns Success so callers
	// in a Selector know this branch handled the tick.
	protected NodeState ReadyStance()
	{
		TrackTowardsPlayer(RotationSpeed * 3);
		return NodeState.Success;
	}

	// ----------------------------------------------------------------------------------------------------------------
	// Movement primitives
	// ----------------------------------------------------------------------------------------------------------------

	private void GoTo(Vector2 worldPos, int speed)
	{
		var dir = worldPos - GlobalPosition;
		if (dir.LengthSquared() < 1f) return;
		WalkDirection = dir.Normalized();
		Velocity     += WalkDirection * speed;
		ClampVelocity(speed);
	}

	private void ApproachToRange(Vector2 targetPos, int range, int speed)
	{
		var to = targetPos - GlobalPosition;
		if (to.Length() <= range) return;
		WalkDirection = FaceDirection;
		Velocity     += WalkDirection * speed;
		ClampVelocity(speed);
	}

	private void StrafeAroundPlayer(int preferredRadius, int speed)
	{
		if (Blackboard.NowMs >= Blackboard.StrafeUntilMs)
		{
			Blackboard.StrafeSign    = _random.Next(2) == 0 ? 1 : -1;
			Blackboard.StrafeUntilMs = Blackboard.NowMs +
				(ulong)_random.Next(StrafeFlipMinMs, StrafeFlipMaxMs);
		}

		var to   = ToPlayer();
		var dist = to.Length();
		if (dist < 0.001f) return;

		var radial  = to.Normalized();
		var tangent = new Vector2(-radial.Y, radial.X) * Blackboard.StrafeSign;

		// Pull toward the preferred combat radius along the radial axis: too
		// close -> push out; too far -> pull in. Tangent gives the orbit.
		var radialBias = 0f;
		if (dist < preferredRadius - 8)      radialBias = -1f;
		else if (dist > preferredRadius + 8) radialBias = 1f;

		WalkDirection = (tangent + radial * radialBias * 0.6f).Normalized();
		Velocity     += WalkDirection * speed;
		ClampVelocity(speed);
	}

	private void LeashedWander(int speed)
	{
		if (Blackboard.NowMs >= Blackboard.WanderUntilMs)
		{
			// Bias wander toward the player so wary mobs drift toward the
			// action instead of wandering off.
			var bias = ToPlayer().LengthSquared() > 1f
				? ToPlayer().Normalized() * 0.3f
				: Vector2.Zero;
			var rand = Vector2.FromAngle((float)(_random.NextDouble() * Math.PI * 2));
			WalkDirection            = (rand + bias).Normalized();
			Blackboard.WanderUntilMs = Blackboard.NowMs + (ulong)_random.Next(WanderLeashMinMs, WanderLeashMaxMs);
		}

		Velocity += WalkDirection * speed;
		ClampVelocity(speed);
	}

	private void LookAroundSweep()
	{
		// Sweep the head back and forth in place — visual cue that the mob is
		// searching, without translating.
		var sweep   = (float)Math.Sin(Blackboard.NowMs * 0.003) * 0.6f;
		var baseDir = Blackboard.LastKnownPlayerPos.HasValue
			? (Blackboard.LastKnownPlayerPos.Value - GlobalPosition).Normalized()
			: FaceDirection;
		var target        = baseDir.Rotated(sweep);
		var angleToTarget = FaceDirection.AngleTo(target);
		Rotate(Math.Sign(angleToTarget) * Math.Min(float.DegreesToRadians(RotationSpeed), Math.Abs(angleToTarget)));
		FaceDirection = Vector2.Right.Rotated(Rotation);
	}

	private void TrackTowardsPlayer(int? rotationSpeed = null)
	{
		var player = TryGetPlayer();
		if (player == null) return;
		TrackTowards(player.GlobalPosition, rotationSpeed ?? RotationSpeed);
	}

	private void TrackTowards(Vector2 worldPos, int rotationSpeed)
	{
		var to = worldPos - GlobalPosition;
		if (to.LengthSquared() < 0.001f) return;
		var angle = FaceDirection.AngleTo(to.Normalized());
		var step  = Math.Min(float.DegreesToRadians(rotationSpeed), Math.Abs(angle));
		Rotate(Math.Sign(angle) * step);
		FaceDirection = Vector2.Right.Rotated(Rotation);
	}

	private void ApplyDrag()
	{
		if (Velocity == Vector2.Zero) return;
		if (Math.Abs(Velocity.X) <= 1 && Math.Abs(Velocity.Y) <= 1)
		{
			Velocity = Vector2.Zero;
			return;
		}
		Velocity += Velocity * -0.05f;
	}

	private void ClampVelocity(int speed)
	{
		Velocity = Velocity.Clamp(new Vector2(-speed, -speed), new Vector2(speed, speed));
	}

	private bool HasArrivedAtLastKnown()
	{
		if (!Blackboard.LastKnownPlayerPos.HasValue) return true;
		return GlobalPosition.DistanceTo(Blackboard.LastKnownPlayerPos.Value) <= InvestigateArriveDist;
	}

	private bool IsInsideVisionCone(Vector2 toPlayer)
	{
		var angle = FaceDirection.AngleTo(toPlayer.Normalized());
		return Math.Abs(angle) <= double.DegreesToRadians(VisibleConeAngle);
	}

	private Vector2 ToPlayer()
	{
		var player = TryGetPlayer();
		return player == null ? Vector2.Zero : player.GlobalPosition - GlobalPosition;
	}

	// ----------------------------------------------------------------------------------------------------------------
	// Misc helpers
	// ----------------------------------------------------------------------------------------------------------------

	protected int ActionScoreRoll(int minScore) => _random.Next(minScore, 101);

	// ----------------------------------------------------------------------------------------------------------------
	// IUsesAnimationHelper
	// ----------------------------------------------------------------------------------------------------------------

	public void OnResetAnim()               { }
	public void OnFinishedAnim(string name) { }
	public void AfterFinishedAnim()         { }
	public void OnStartAnim()               { }

	public AnimationHelper GetAnimationHelper() => WeaponAnimationHelper;
}
