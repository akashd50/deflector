using System;
using Deflector.Data.BehaviorTree;
using Deflector.Data.Shared;
using Deflector.Data.Weapons;
using Godot;

namespace Deflector.Data.Mobs;

public partial class MobBehavior: CharacterBody2D, IUsesAnimationHelper
{
	// --- Detection / vision ---
	[Export] public int DetectionRange = 400;
	[Export] public int PeripheralRange = 60;
	[Export] public int VisibleConeAngle = 45;

	// --- Movement ---
	[Export] public int WalkSpeed = 15;
	[Export] public int RunSpeed = 100;
	[Export] public int DashSpeed = 1200;
	[Export] public int RotationSpeed = 1;
	[Export] public int PreferredCombatRange = 110;

	// --- Awareness model ---
	[Export] public float AwarenessRiseRate = 120f;
	[Export] public float AwarenessPeripheralRiseRate = 220f;
	[Export] public float AwarenessDecayRate = 25f;
	[Export] public float WaryThreshold = 30f;
	[Export] public float AggroThreshold = 70f;
	[Export] public float ForgetThreshold = 5f;

	// --- Combat fairness ---
	[Export] public int MaxAttackBudget = 2;
	[Export] public int RepositionDurationMs = 900;
	[Export] public int PostAttackCooldownMs = 350;
	[Export] public float Aggressiveness = 0.5f;

	// --- Wander / strafe ---
	[Export] public int WanderLeashMinMs = 1200;
	[Export] public int WanderLeashMaxMs = 2400;
	[Export] public int StrafeFlipMinMs = 600;
	[Export] public int StrafeFlipMaxMs = 1300;

	// --- Investigate ---
	[Export] public int InvestigateArriveDist = 24;
	[Export] public int InvestigateLookAroundMs = 1200;

	protected Vector2          WalkDirection = Vector2.Zero;
	protected Vector2          FaceDirection = Vector2.Zero;
	protected AnimatedSprite2D AnimatedSprite;
	protected State            WeaponState   = State.Idle;
	protected StateMap         WeaponStateMap;
	protected AnimationHelper  WeaponAnimationHelper;
	protected ulong            LastWeaponAttackFinishTime = 0;
	protected ulong            CurrentWeaponCooldownTime  = 0;

	protected bool            ChasePlayerDuringAttack  = false;
	protected bool            TrackPlayerDuringAttack  = false;
	protected bool            DashToPlayerDuringAttack = false;
	protected Sprite2D        Body;
	protected Sprite2D        Eye;
	protected MobWeaponsGroup MobWeaponsGroup;

	protected MobBlackboard Blackboard;

	private State         _state;
	private Player.Player _player;
	private StateMap      _stateMap;
	private Random        _random;
	private BTNode        _treeRoot;
	
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
		_state =  State.Idle;
		Blackboard = new MobBlackboard { AttackBudget = MaxAttackBudget };
		Blackboard.OnStateEntered();
		// _stateMap       = GetStateMap();
		WeaponStateMap  = GetWeaponStateMap();
		FaceDirection   = Vector2.Right.Rotated(Rotation);
		MobWeaponsGroup = GetNode<MobWeaponsGroup>("WeaponsGroup");
		_treeRoot       = BuildBehavioralTree();
	}

	protected virtual BTNode BuildBehavioralTree()
	{
		return new Selector([]);
	}

	protected virtual void OnHitDone()
	{
		// Each landed hit consumes one unit of attack budget. When the budget
		// hits zero, the state map will pick Reposition next tick — that is the
		// fairness window that lets the player counter.
		Blackboard.AttackBudget = Math.Max(0, Blackboard.AttackBudget - 1);
	}

	public override void _PhysicsProcess(double delta)
	{
		UpdateAwareness((float)delta);
		_state = _stateMap.Execute(_state);
		MoveAndCollide(Velocity * (float)delta);
		ApplyDrag();
	}
	
	// ----------------------------------------------------------------------------------------------------------------
	// Behavior tree methods
	// ----------------------------------------------------------------------------------------------------------------

	protected NodeState HasSomethingToInvestigate()
	{
		return Blackboard.Awareness >= WaryThreshold ? NodeState.Success : NodeState.Failure;
	}
	
	protected NodeState Investigate()
	{
		if (Blackboard.LastKnownPlayerPos == null)
		{
			return NodeState.Failure;
		}

		if (HasArrivedAtLastKnown())
		{
			return NodeState.Success;
		}

		TrackTowards(Blackboard.LastKnownPlayerPos.Value, RotationSpeed);
		GoTo(Blackboard.LastKnownPlayerPos.Value, WalkSpeed);
		return NodeState.Running;
	}
	
	protected NodeState SearchAreaLookAround()
	{
		// Needs work
		LookAroundSlow();

		return NodeState.Success;
	}

	// ----------------------------------------------------------------------------------------------------------------

	protected void UpdateAwareness(float delta)
	{
		var toPlayer = ToPlayer();
		var dist = toPlayer.Length();
		var inDetectionRange = dist <= DetectionRange;
		var inPeripheral = dist <= PeripheralRange;
		var inCone = inDetectionRange && IsInsideVisionCone(toPlayer);

		if (inPeripheral || inCone)
		{
			var rate = inPeripheral ? AwarenessPeripheralRiseRate : AwarenessRiseRate;
			Blackboard.Awareness = Math.Min(100f, Blackboard.Awareness + rate * delta);
			Blackboard.OnPlayerSeen(TryGetPlayer().GlobalPosition);
		}
		else
		{
			Blackboard.Awareness = Math.Max(0f, Blackboard.Awareness - AwarenessDecayRate * delta);
			if (Blackboard.Awareness < ForgetThreshold)
			{
				Blackboard.LastKnownPlayerPos = null;
			}
		}
	}

	// ---------- State map ----------

	/*protected virtual StateMap GetStateMap()
	{
		// 200ms transition cadence: fast enough to feel reactive, slow enough
		// that a state commits long enough for movement to actually express it.
		return new StateMap(200)
		{
			{ State.Idle, new StateInfo([
				new TState(State.Wary, ScoreWaryFromIdle),
				new TState(State.GoingToPlayer, ScoreGoingToPlayer),
			], Enter: OnEnterDefault, Tick: TickIdle) },

			{ State.Wary, new StateInfo([
				new TState(State.GoingToPlayer, ScoreGoingToPlayer),
				new TState(State.Investigate, ScoreInvestigate),
				new TState(State.Idle, ScoreIdle),
			], Enter: OnEnterDefault, Tick: TickWary) },

			{ State.Investigate, new StateInfo([
				new TState(State.GoingToPlayer, ScoreGoingToPlayer),
				new TState(State.Wary, ScoreWaryFromInvestigate),
				new TState(State.Idle, ScoreIdle),
			], Enter: OnEnterInvestigate, Tick: TickInvestigate) },

			{ State.GoingToPlayer, new StateInfo([
				new TState(State.Attacking, ScoreAttack),
				new TState(State.Investigate, ScoreInvestigateFromChase),
				new TState(State.Wary, ScoreWaryFromChase),
			], Enter: OnEnterChase, Tick: TickGoToPlayer) },

			{ State.Attacking, new StateInfo([
				new TState(State.Reposition, ScoreReposition),
				new TState(State.GoingToPlayer, ScoreResumeChase),
			], Enter: OnEnterAttack, Tick: AttackPlayer, Exit: AttackPlayer,
				ReEval: () => !IsAttacking()) },

			{ State.Reposition, new StateInfo([
				new TState(State.GoingToPlayer, ScoreGoingToPlayer),
				new TState(State.Wary, ScoreWaryFromReposition),
				new TState(State.Idle, ScoreIdle),
			], Enter: OnEnterReposition, Tick: TickReposition,
				ReEval: () => Blackboard.NowMs >= Blackboard.RepositionUntilMs) },
		};
	}*/

	protected virtual bool IsAttacking()
	{
		return false;
	}

	protected virtual StateMap GetWeaponStateMap()
	{
		return new StateMap(0);
	}

	// ---------- Enter handlers ----------

	private bool OnEnterDefault(State from)
	{
		Blackboard.OnStateEntered();
		return true;
	}

	private bool OnEnterInvestigate(State from)
	{
		Blackboard.OnStateEntered();
		Blackboard.WanderUntilMs = 0;
		return true;
	}

	private bool OnEnterChase(State from)
	{
		Blackboard.OnStateEntered();
		// Refresh budget when re-engaging from a non-combat state, but keep it
		// when bouncing back from Reposition (Reposition already refilled).
		if (from != State.Reposition && from != State.Attacking)
		{
			Blackboard.AttackBudget = MaxAttackBudget;
		}
		return true;
	}

	private bool OnEnterAttack(State from)
	{
		Blackboard.OnStateEntered();
		return true;
	}

	private bool OnEnterReposition(State from)
	{
		Blackboard.OnStateEntered();
		Blackboard.RepositionUntilMs = Blackboard.NowMs + (ulong)RepositionDurationMs;
		Blackboard.StrafeSign = _random.Next(2) == 0 ? 1 : -1;
		Blackboard.StrafeUntilMs = 0;
		Blackboard.AttackBudget = MaxAttackBudget;
		LastWeaponAttackFinishTime = Time.GetTicksMsec();
		CurrentWeaponCooldownTime = (ulong)PostAttackCooldownMs;
		return true;
	}
	
	public void OnResetAnim()
	{
		WeaponState = State.Idle;
	}

	public void OnFinishedAnim(string name)
	{
		// EmitSignal(SignalName.OnAnimationFinished, name);
	}

	public void AfterFinishedAnim()
	{
		WeaponState = State.Idle;
	}

	public void OnStartAnim()
	{
		WeaponState = State.Attacking;
	}

	public AnimationHelper GetAnimationHelper() => WeaponAnimationHelper;

	// ---------- Score functions ----------

	private int Jitter(int value) => Math.Max(0, value + _random.Next(-3, 4));

	/*private int ScoreIdle()
	{
		return Blackboard.Awareness < ForgetThreshold ? 80 : 0;
	}

	protected int ScoreWaryFromIdle()
	{
		return Blackboard.Awareness >= WaryThreshold ? 50 : 0;
	}

	private int ScoreGoingToPlayer()
	{
		if (Blackboard.Awareness < AggroThreshold) return 0;
		if (!IsPlayerVisible()) return 0;
		if (IsWithinAttackRange()) return 0;
		return Jitter(60 + (int)(Aggressiveness * 30));
	}

	private int ScoreInvestigate()
	{
		if (!Blackboard.LastKnownPlayerPos.HasValue) return 0;
		if (Blackboard.Awareness < WaryThreshold) return 0;
		if (Blackboard.Awareness >= AggroThreshold) return 0;
		if (IsPlayerVisible()) return 0;
		return Jitter(55);
	}

	private int ScoreInvestigateFromChase()
	{
		if (!Blackboard.LastKnownPlayerPos.HasValue) return 0;
		if (IsPlayerVisible()) return 0;
		if (Blackboard.Awareness < WaryThreshold) return 0;
		return Jitter(50);
	}

	private int ScoreWaryFromInvestigate()
	{
		if (Blackboard.Awareness < ForgetThreshold) return 0;
		if (Blackboard.Awareness >= AggroThreshold) return 0;
		if (HasArrivedAtLastKnown() && Blackboard.TimeInStateMs >= (ulong)InvestigateLookAroundMs)
		{
			Blackboard.LastKnownPlayerPos = null;
			return 60;
		}
		return 0;
	}

	private int ScoreWaryFromChase()
	{
		return Blackboard.Awareness < AggroThreshold && Blackboard.Awareness >= WaryThreshold ? 50 : 0;
	}

	private int ScoreWaryFromReposition()
	{
		return Blackboard.Awareness < AggroThreshold && Blackboard.Awareness >= WaryThreshold ? 50 : 0;
	}*/

	// ---------- Tick handlers ----------

	private bool TickIdle()
	{
		return true;
	}

	private bool TickWary()
	{
		TrackPointIfNeeded(TryGetPlayer().GlobalPosition);
		LeashedWander(WalkSpeed);
		return true;
	}

	private bool TickInvestigate()
	{
		if (!Blackboard.LastKnownPlayerPos.HasValue)
		{
			return true;
		}

		if (HasArrivedAtLastKnown())
		{
			LookAroundSlow();
		}
		else
		{
			TrackTowards(Blackboard.LastKnownPlayerPos.Value, RotationSpeed);
			GoTo(Blackboard.LastKnownPlayerPos.Value, WalkSpeed * 2);
		}
		return true;
	}

	private bool TickGoToPlayer()
	{
		TrackPointIfNeeded(TryGetPlayer().GlobalPosition);
		ApproachToRange(TryGetPlayer().GlobalPosition, (int)MobWeaponsGroup.GetRandomWeaponRange(), RunSpeed);
		return true;
	}

	private bool TickReposition()
	{
		TrackPointIfNeeded(TryGetPlayer().GlobalPosition);
		StrafeAroundPlayer(PreferredCombatRange, WalkSpeed * 2);
		return true;
	}

	// ---------- Movement primitives ----------

	private void GoTo(Vector2 worldPos, int speed)
	{
		var dir = worldPos - GlobalPosition;
		if (dir.LengthSquared() < 1f) return;
		WalkDirection = dir.Normalized();
		Velocity += WalkDirection * speed;
		ClampVelocity(speed);
	}

	private void ApproachToRange(Vector2 targetPos, int range, int speed)
	{
		var to = targetPos - GlobalPosition;
		if (to.Length() <= range) return;
		WalkDirection = FaceDirection;
		Velocity += WalkDirection * speed;
		ClampVelocity(speed);
	}

	private void StrafeAroundPlayer(int preferredRadius, int speed)
	{
		if (Blackboard.NowMs >= Blackboard.StrafeUntilMs)
		{
			Blackboard.StrafeSign = _random.Next(2) == 0 ? 1 : -1;
			Blackboard.StrafeUntilMs = Blackboard.NowMs +
				(ulong)_random.Next(StrafeFlipMinMs, StrafeFlipMaxMs);
		}

		var to = ToPlayer();
		var dist = to.Length();
		if (dist < 0.001f) return;

		var radial = to.Normalized();
		var tangent = new Vector2(-radial.Y, radial.X) * Blackboard.StrafeSign;

		// Pull toward the preferred combat radius along the radial axis: too
		// close → push out; too far → pull in. Tangent component gives the
		// circling motion.
		var radialBias = 0f;
		if (dist < preferredRadius - 8) radialBias = -1f;
		else if (dist > preferredRadius + 8) radialBias = 1f;

		var dir = (tangent + radial * radialBias * 0.6f).Normalized();
		WalkDirection = dir;
		Velocity += WalkDirection * speed;
		ClampVelocity(speed);
	}

	private void LeashedWander(int speed)
	{
		if (Blackboard.NowMs >= Blackboard.WanderUntilMs)
		{
			// Bias the wander direction lightly toward the player so wary mobs
			// drift toward the action instead of wandering off.
			var bias = ToPlayer().LengthSquared() > 1f
				? ToPlayer().Normalized() * 0.3f
				: Vector2.Zero;
			var rand = Vector2.FromAngle((float)(_random.NextDouble() * Math.PI * 2));
			WalkDirection = (rand + bias).Normalized();
			Blackboard.WanderUntilMs = Blackboard.NowMs +
				(ulong)_random.Next(WanderLeashMinMs, WanderLeashMaxMs);
		}

		Velocity += WalkDirection * speed;
		ClampVelocity(speed);
	}

	private void LookAroundSlow()
	{
		// Sweep the head back and forth in place — gives the visual cue that
		// the mob is searching, without translating.
		var sweep = (float)Math.Sin(Blackboard.TimeInStateMs * 0.003) * 0.6f;
		var baseDir = Blackboard.LastKnownPlayerPos.HasValue
			? (Blackboard.LastKnownPlayerPos.Value - GlobalPosition).Normalized()
			: FaceDirection;
		var target = baseDir.Rotated(sweep);
		var angleToTarget = FaceDirection.AngleTo(target);
		Rotate(Math.Sign(angleToTarget) * Math.Min(float.DegreesToRadians(RotationSpeed), Math.Abs(angleToTarget)));
		FaceDirection = Vector2.Right.Rotated(Rotation);
	}

	private bool HasArrivedAtLastKnown()
	{
		if (!Blackboard.LastKnownPlayerPos.HasValue) return true;
		return GlobalPosition.DistanceTo(Blackboard.LastKnownPlayerPos.Value) <= InvestigateArriveDist;
	}

	// ---------- Existing helpers (preserved) ----------

	protected bool IsWeaponInReadyState()
	{
		return WeaponState == State.Ready;
	}

	protected bool IsWeaponCooldownOver()
	{
		return Time.GetTicksMsec() - LastWeaponAttackFinishTime > CurrentWeaponCooldownTime;
	}

	private bool IsInsideVisionCone(Vector2 toPlayer)
	{
		var angle = FaceDirection.AngleTo(toPlayer.Normalized());
		return Math.Abs(angle) <= double.DegreesToRadians(VisibleConeAngle);
	}

	public bool IsPlayerVisible()
	{
		var to = ToPlayer();
		var dist = to.Length();
		if (dist <= PeripheralRange) return true;
		return dist <= DetectionRange && IsInsideVisionCone(to);
	}

	protected bool IsWithinVisibleRegion()
	{
		return IsPlayerVisible();
	}

	protected virtual bool GoToPlayerIfOutsideAttackRange(int range)
	{
		TrackPointIfNeeded(TryGetPlayer().GlobalPosition);
		var toPlayer = ToPlayer();
		if (toPlayer.Length() > range)
		{
			GoToPlayer(RunSpeed);
		}

		return true;
	}

	protected bool ActWary()
	{
		TrackPointIfNeeded(TryGetPlayer().GlobalPosition);
		LeashedWander(WalkSpeed);
		return true;
	}

	private bool TrackPointIfNeeded(Vector2 pos)
	{
		TrackPoint(pos, RotationSpeed);
		return true;
	}

	private void TrackPoint(Vector2 pos, int rotationSpeed)
	{
		TrackTowards(pos, rotationSpeed);
	}

	private void TrackTowards(Vector2 worldPos, int rotationSpeed)
	{
		var to = worldPos - GlobalPosition;
		if (to.LengthSquared() < 0.001f) return;
		var angle = FaceDirection.AngleTo(to.Normalized());
		var step = Math.Min(float.DegreesToRadians(rotationSpeed), Math.Abs(angle));
		Rotate(Math.Sign(angle) * step);
		FaceDirection = Vector2.Right.Rotated(Rotation);
	}

	protected bool AttackPlayer()
	{
		if (ChasePlayerDuringAttack)
		{
			TrackPointIfNeeded(TryGetPlayer().GlobalPosition);
			GoToPlayerIfOutsideAttackRange(0);
		}

		if (TrackPlayerDuringAttack)
		{
			TrackPoint(TryGetPlayer().GlobalPosition, RotationSpeed * 3);
		}

		if (DashToPlayerDuringAttack)
		{
			WalkDirection = FaceDirection;
			Velocity = WalkDirection * DashSpeed;
			DashToPlayerDuringAttack = false;
		}

		if (!IsAttacking())
		{
			WeaponState = WeaponStateMap.Execute(WeaponState);
		}
		return true;
	}

	protected bool IsWithinDetectionRange()
	{
		return ToPlayer().Length() <= DetectionRange;
	}
	
	protected bool IsWithinRange(int range)
	{
		return ToPlayer().Length() <= range;
	}

	protected bool IsGreaterThanRange(int range)
	{
		return ToPlayer().Length() >= range;
	}

	protected int ActionScoreRoll(int minScore)
	{
		return _random.Next(minScore, 101);
	}

	protected bool QueueAnimationAndSetCooldown(string name, ulong cooldown)
	{
		LastWeaponAttackFinishTime = Time.GetTicksMsec();
		CurrentWeaponCooldownTime = cooldown;
		return WeaponAnimationHelper.QueueAnimation(name);
	}

	protected bool QueueAnimationAndChase(string name)
	{
		ChasePlayerDuringAttack = true;
		return WeaponAnimationHelper.QueueAnimation(name);
	}

	protected bool QueueAnimationAndDashToPlayer(string name)
	{
		DashToPlayerDuringAttack = true;
		return WeaponAnimationHelper.QueueAnimation(name);
	}

	protected bool QueueAnimationAndTrackPlayer(string name)
	{
		TrackPlayerDuringAttack = true;
		return WeaponAnimationHelper.QueueAnimation(name);
	}

	protected bool StopChase()
	{
		ChasePlayerDuringAttack = false;
		return true;
	}

	protected bool StopDash()
	{
		DashToPlayerDuringAttack = false;
		return true;
	}

	protected bool StopTrack()
	{
		TrackPlayerDuringAttack = false;
		return true;
	}

	private void GoToPlayer(int speed)
	{
		WalkDirection = FaceDirection;
		Velocity += WalkDirection * speed;
		ClampVelocity(speed);
	}

	private void ApplyDrag()
	{
		if (Velocity == Vector2.Zero)
		{
			return;
		}

		if (Math.Abs(Velocity.X) <= 1 && Math.Abs(Velocity.Y) <= 1)
		{
			Velocity = Vector2.Zero;
			return;
		}

		var dragVec = Velocity * -0.05f;
		Velocity += dragVec;
	}

	private void ClampVelocity(int speed)
	{
		Velocity = Velocity.Clamp(new Vector2(-speed, -speed), new Vector2(speed, speed));
	}

	private Vector2 ToPlayer()
	{
		return TryGetPlayer().GlobalPosition - GlobalPosition;
	}
	
	private Vector2 ToPlace(Vector2 pos)
	{
		return pos - GlobalPosition;
	}
}
