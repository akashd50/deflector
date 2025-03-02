class_name PlayerHelper

@export var velocity: Vector2 = Vector2.ZERO
@export var position: Vector2 = Vector2.ZERO
@export var rotation: float = 0

var speed: int
var player: Player

# Dash
const dashDuration: int = 80

var isDashing: bool         = false
var dashStarted: int       = 0
var walkDirection: Vector2 = Vector2.ZERO
var faceDirection: Vector2 = Vector2.ZERO

# Deflection
var isDeflecting: bool   = false
var deflectStarted: int  = 0
var deflectDuration: int = 200

# lock on
var lockedOnEnemy: CharacterBody2D

func _init(_player: Player):
	player = _player
	player.deflectVis.hide()
	player.weapon.connect("attackAnimationFinished", attackFinished)
	
# Event handlers
func handleDash(event: InputEvent) -> bool:
	if !isDashing && !player.weapon.isAttacking && event.is_action_pressed("dash"):
		isDashing = true

		stopDeflecting()

		dashStarted = Time.get_ticks_msec()
		velocity = walkDirection * (speed * 8)
		return true
	return false
	
func handleDeflect(event: InputEvent) -> bool:
	if !isDashing && !player.weapon.isAttacking:
		if (!isDeflecting && event.is_action_pressed("deflect")):
			startDeflecting()
			return true
		elif isDeflecting && event.is_action_released("deflect"):
			stopDeflecting()
			return true
	return false

func handleLockon(event: InputEvent, enemies: Array[Node]) -> bool:
	if event.is_action_pressed("lockon"):
		if lockedOnEnemy == null:
			do_lockon(enemies)
		else:
			lockedOnEnemy = null
		return true
	return false

func startDeflecting():
	isDeflecting = true
	deflectStarted = Time.get_ticks_msec()
	player.deflectionVisual.show()

func stopDeflecting():
	isDeflecting = false
	player.deflectionVisual.hide()

func handleAttack(event: InputEvent) -> bool:
	if !player.weapon.isAttacking && event.is_action_pressed("attack"):
		player.weapon.attack1()
		return true
	return false

func attackFinished(name: StringName) -> void:
	player.weapon.resetAnimation()

# Event handlers end

# Combat stuff

# Combat stuff end
func updateDirections() -> void: 
	var direction: Vector2 = getInput();
	if (direction != Vector2.ZERO):
		walkDirection = direction
		
		if lockedOnEnemy != null:
			faceDirection = lockedOnEnemy.position - player.position
		else:
			faceDirection = walkDirection
	else:
		walkDirection = Vector2.ZERO

func updateRotation():
	if player.weapon.isAttacking:
		return

	rotation = faceDirection.angle() - Vector2.UP.angle()

func updateVelocity() -> void:
	if isDashing:
		updateDash()
	else:
		updateWalk()
		
func updateDash() -> void:
	var currTime: int = Time.get_ticks_msec()
	if (currTime - dashStarted > dashDuration):
		velocity = Vector2.ZERO
		isDashing = false
	else:
		velocity -= Vector2(velocity.x * 0.1, velocity.y * 0.1)

func updateWalk() -> void:
	if isDeflecting:
		velocity = walkDirection * speed/3
	elif player.weapon.isAttacking:
		velocity = walkDirection * speed/10
	else:
		velocity = walkDirection * speed
	
func getInput() -> Vector2:
	return Input.get_vector("left", "right", "up", "down")

func do_lockon(enemies: Array[Node]):
	var closest_enemy_angle: int = 99
	var closest_enemy
	
	for enemy in enemies:
		var angle_to_enemy = abs(rotation - (enemy.position - position).angle())
		if (angle_to_enemy < closest_enemy_angle):
			closest_enemy_angle = angle_to_enemy
			closest_enemy = enemy
	
	if closest_enemy != null:
		lockedOnEnemy = closest_enemy
