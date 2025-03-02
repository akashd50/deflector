class_name Weapon
extends Node2D

@onready var animationPlayer := $AnimationPlayer
@onready var timer: Timer = $Timer

var isAttacking: bool = false

signal attackAnimationFinished(name: StringName)

func _ready() -> void:
	timer.timeout.connect(onCurrentTimeoutFinished)
	animationPlayer.connect("animation_finished", on_attack_animation_finished)

func attack1() -> void:
	isAttacking = true
	animationPlayer.play("slash-1")

func resetAnimation() -> void:
	isAttacking = false
	animationPlayer.play("RESET")

func on_attack_animation_finished(finishedAnimName: StringName):
	timer.paused = false
	timer.start(5.0)
	attackAnimationFinished.emit(finishedAnimName)
	
func onCurrentTimeoutFinished() -> void:
	timer.stop()
	print("Timeout done")
