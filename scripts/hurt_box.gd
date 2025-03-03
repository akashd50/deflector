class_name HurtBox
extends Area2D

func _init() -> void:
	collision_layer = 0
	collision_mask = 2

func _ready() -> void:
	connect("area_entered", onAreaEntered)

func onAreaEntered(hitbox: Hitbox):
	print("Area entered")

	if owner.has_method("takeDamage"):
		owner.takeDamage(hitbox.damage)
