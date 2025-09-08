using Godot;
using System;

public partial class Player : CharacterBody3D
{
	private float speed;
	private const float WALK_SPEED = 5.0f;
	private const float SPRINT_SPEED = 8.0f;
	private const float JUMP_VELOCITY = 4.8f;
	private const float SENSITIVITY = 0.004f;

	// Bob variables
	private const float BOB_FREQ = 2.4f;
	private const float BOB_AMP = 0.08f;
	private float tBob = 0.0f;

	// FOV variables
	private const float BASE_FOV = 75.0f;
	private const float FOV_CHANGE = 1.5f;

	// Gravity
	private float gravity = 9.8f;

	private Node3D head;
	private Camera3D camera;

	public override void _Ready()
	{
		Input.MouseMode = Input.MouseModeEnum.Captured;
		head = GetNode<Node3D>("Head");
		camera = GetNode<Camera3D>("Head/Camera3D");
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventMouseMotion motion)
		{
			head.RotateY(-motion.Relative.X * SENSITIVITY);
			camera.RotateX(-motion.Relative.Y * SENSITIVITY);
			camera.Rotation = new Vector3(
				Mathf.Clamp(camera.Rotation.X, Mathf.DegToRad(-40), Mathf.DegToRad(60)),
				camera.Rotation.Y,
				camera.Rotation.Z
			);
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		float dt = (float)delta;

		// Gravity
		if (!IsOnFloor())
			Velocity = new Vector3(Velocity.X, Velocity.Y - gravity * dt, Velocity.Z);

		// Jump
		if (Input.IsActionJustPressed("jump") && IsOnFloor())
			Velocity = new Vector3(Velocity.X, JUMP_VELOCITY, Velocity.Z);

		// Sprint
		speed = Input.IsActionPressed("sprint") ? SPRINT_SPEED : WALK_SPEED;

		// Movement
		Vector2 inputDir = Input.GetVector("left", "right", "up", "down");
		Vector3 direction = (head.Transform.Basis * Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();

		if (IsOnFloor())
		{
			if (direction != Vector3.Zero)
			{
				Velocity = new Vector3(direction.X * speed, Velocity.Y, direction.Z * speed);
			}
			else
			{
				Velocity = new Vector3(
					Mathf.Lerp(Velocity.X, direction.X * speed, dt * 7.0f),
					Velocity.Y,
					Mathf.Lerp(Velocity.Z, direction.Z * speed, dt * 7.0f)
				);
			}
		}
		else
		{
			Velocity = new Vector3(
				Mathf.Lerp(Velocity.X, direction.X * speed, dt * 3.0f),
				Velocity.Y,
				Mathf.Lerp(Velocity.Z, direction.Z * speed, dt * 3.0f)
			);
		}

		// Head bob
		tBob += dt * Velocity.Length() * (IsOnFloor() ? 1.0f : 0.0f);
		camera.Transform = new Transform3D(
			camera.Transform.Basis,
			_Headbob(tBob)
		);

		// FOV
		float velocityClamped = Mathf.Clamp(Velocity.Length(), 0.5f, SPRINT_SPEED * 2);
		float targetFov = BASE_FOV + FOV_CHANGE * velocityClamped;
		camera.Fov = Mathf.Lerp(camera.Fov, targetFov, dt * 8.0f);

		MoveAndSlide();
	}

	private Vector3 _Headbob(float time)
	{
		Vector3 pos = Vector3.Zero;
		pos.Y = Mathf.Sin(time * BOB_FREQ) * BOB_AMP;
		pos.X = Mathf.Cos(time * BOB_FREQ / 2) * BOB_AMP;
		return pos;
	}
}
