class_name Player
extends CharacterBody2D

@export var speed: int = 200

@onready var weapon: Weapon = $WeaponSword
@onready var deflectVis := $DeflectVis

var helper: PlayerHelper

# Called when the node enters the scene tree for the first time.
func _ready() -> void:	
	helper = PlayerHelper.new(self)
	helper.speed = speed

func _physics_process(delta: float) -> void:
	helper.updateDirections()
	helper.updateRotation()
	helper.updateVelocity()

	setParameters()
	move_and_slide()

func _shortcut_input(event: InputEvent) -> void:
	if helper.handleDash(event):
		setParameters()
		return
	elif helper.handleDeflect(event):
		setParameters()
		return
	elif helper.handleAttack(event):
		return
	
	if helper.handleLockon(event, getAllEnemies()):
		setParameters()

func setParameters():
	rotation = helper.rotation
	velocity = helper.velocity
	
func getAllEnemies() -> Array[Node]:
	return get_tree().get_nodes_in_group("enemies")
