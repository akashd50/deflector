class_name PlayerHelper

@export var speed = 200
@export var dash_duration = 80

@export var velocity = Vector2.ZERO
@export var position = Vector2.ZERO
@export var rotation = 0

# Dash
var is_dashing = false
var dash_started = 0
var walk_direction = Vector2.ZERO
var face_direction = Vector2.ZERO

# Deflection
var deflection_visual: Sprite2D
var is_deflecting = false
var deflect_started = 0
var deflect_duration = 200

# lock on
var locked_on_enemy: CharacterBody2D

func _init(deflectionVis: Sprite2D):
	deflection_visual = deflectionVis
	deflection_visual.hide()

func physics_process(delta: float) -> void:
	update_directions()
	update_rotation()
	
	if is_dashing:
		do_dash()
	
	if !is_dashing:
		do_walk()

func shortcut_input(event: InputEvent) -> void:
	if (!is_dashing && event.is_action_pressed("dash")):
		is_dashing = true

		is_deflecting = false
		deflection_visual.hide()

		dash_started = Time.get_ticks_msec()
		velocity = walk_direction * (speed * 8)
		return
	
	if !is_dashing:
		if (!is_deflecting && event.is_action_pressed("deflect")):
			is_deflecting = true
			deflect_started = Time.get_ticks_msec()
			deflection_visual.show()
		elif is_deflecting && event.is_action_released("deflect"):
			is_deflecting = false
			deflection_visual.hide()
		return

	if event.is_action_pressed("lockon"):
		if locked_on_enemy == null:
			do_lockon()
		else:
			locked_on_enemy = null
		return

func update_directions(): 
	var direction = getInput();
	if (direction != Vector2.ZERO):
		walk_direction = direction
		
		if locked_on_enemy != null:
			face_direction = locked_on_enemy.position - position
		else:
			face_direction = walk_direction
	else:
		walk_direction = Vector2.ZERO

func do_dash() -> void:
	var curr_time = Time.get_ticks_msec()
	if (curr_time - dash_started > dash_duration):
		velocity = Vector2.ZERO
		is_dashing = false
	else:
		velocity -= Vector2(velocity.x * 0.1, velocity.y * 0.1)

func do_walk() -> void:
	velocity = walk_direction * (speed/2 if is_deflecting else speed)
	
func update_rotation():	
	rotation = face_direction.angle() - Vector2.UP.angle()
	
func do_deflect():
	var curr_time = Time.get_ticks_msec()
	if (curr_time - deflect_started > deflect_duration):
		is_deflecting = false
		deflection_visual.hide()
	
func getInput() -> Vector2:
	return Input.get_vector("left", "right", "up", "down")

func do_lockon(enemies: Array[Node]):
	var closest_enemy_angle = 99
	var closest_enemy
	
	for enemy in enemies:
		var angle_to_enemy = abs(rotation - (enemy.position - position).angle())
		if (angle_to_enemy < closest_enemy_angle):
			closest_enemy_angle = angle_to_enemy
			closest_enemy = enemy
	
	if closest_enemy != null:
		locked_on_enemy = closest_enemy
