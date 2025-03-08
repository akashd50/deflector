class_name BehaviorManager
extends CharacterBody2D

enum MobState { IDLE, SPOTTED_PLAYER, GOING_TO_PLAYER, PLAYER_REACHED, ATTACKING }
var state: MobState = MobState.IDLE

@export var player_detection_range: float = 200.0
@export var visionConeAngle: float = 45.0  # degrees
@export var walkSpeed: float = 100
@export var attackRange: float = 100

var player: Node

func _init() -> void:
	pass
	
func flow():
	if state == MobState.IDLE:
		# Look for player
		if lookForPlayer():
			state = MobState.SPOTTED_PLAYER

	elif state == MobState.SPOTTED_PLAYER:
		# Walk to player
		if trackPlayer():
			state = MobState.GOING_TO_PLAYER

	elif state == MobState.GOING_TO_PLAYER:
		# Continue
		if goToPlayer():
			state = MobState.PLAYER_REACHED

	elif state == MobState.PLAYER_REACHED:
		# Start attacking
	elif state == MobState.ATTACKING:
		# Continue


func lookForPlayer() -> bool:
	var player = getPlayer()
	if not player:
		return false
		
	if not isWithinDetectionRange(player):
		return false

	# Check if player is within vision cone
	var toPlayer = player.global_position - global_position
	var forward_dir = Vector2.RIGHT.rotated(rotation)
	var angle_to_player = forward_dir.angle_to(toPlayer.normalized())
	if abs(angle_to_player) > deg_to_rad(visionConeAngle):
		# Rotate towards the player
		return false

	# Player is visible, save position in blackboard
	return true
	
func isWithinDetectionRange(player: Player) -> bool:
	# Calculate distance and direction to player
	var toPlayer = player.global_position - global_position
	var distance = toPlayer.length()
	return distance <= player_detection_range
	
func trackPlayer() -> bool:
	var player = getPlayer()
	if not player:
		return false
	
	var toPlayer = player.global_position - global_position
	var forward_dir = Vector2.RIGHT.rotated(rotation)
	var angle_to_player = forward_dir.angle_to(toPlayer.normalized())

	if abs(angle_to_player) > deg_to_rad(5):
		if angle_to_player > 0:
			rotate(-angle_to_player)
		else:
			rotate(angle_to_player)
	return true

func goToPlayer():
	var toPlayer = player.global_position - global_position
	var forward_dir = Vector2.RIGHT.rotated(rotation)
	if toPlayer.length() > 100:
		velocity = forward_dir * walkSpeed
		return false
	else:
		velocity = Vector2.ZERO
	return true
	
func refreshState():
	var player = getPlayer()
	if not player || not isWithinDetectionRange(player):
		state = MobState.IDLE
		return

	var toPlayer = player.global_position - global_position
	var forward_dir = Vector2.RIGHT.rotated(rotation)
	var angleToPlayer = forward_dir.angle_to(toPlayer.normalized())
	if abs(angleToPlayer) > deg_to_rad(visionConeAngle):
		state = MobState.SPOTTED_PLAYER
		return
	
		
	

func getPlayer() -> Node:
	if not player:
		player = get_tree().get_first_node_in_group("player")
	return player