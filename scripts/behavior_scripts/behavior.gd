class_name Behavior

var name: StringName
var check: Callable

func _init(n: StringName, c: Callable) -> void:
	name = n
	check = c
	pass

func shouldTrigger():
	return check.call()
	