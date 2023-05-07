using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

[Tool]
public partial class Test : Node
{
	[Export]
	public float Radius
	{
		get => _radius;
		set
		{
			if(_radius == value)
			{
				return;
			}
			_radius = value;
			MultiMesh multiMesh = GenerateMultiMesh();
			_meshInstance.Multimesh = multiMesh;
		}
	}
	private float _radius = 0.5f;
	[Export]
	private int _attemptNum = 30;

	[Export]
	private Rect2I _region = new Rect2I(0, 0, 100, 100);

	//private Mesh _mesh = new PlaneMesh()
	//{ 
	//	Size = new Vector2(0.5f, 0.5f)
	//};
	private Mesh _mesh = new BoxMesh()
	{
		Size = new Vector3(0.2f, 0.2f, 0.2f)
	};

	private Image _mask;
	private MultiMeshInstance3D _meshInstance;
	public override void _Ready()
	{
        _mask = GD.Load<Image>("res://mask.png");
		_mask.Convert(Image.Format.R8);
        if (_mask == null)
        {
            GD.Print("mask import incorrectly");
        }

        _meshInstance = new MultiMeshInstance3D()
		{
			Multimesh = GenerateMultiMesh()
		};
		AddChild(_meshInstance);
    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
	private MultiMesh GenerateMultiMesh()
	{
        Stopwatch stopwatch = Stopwatch.StartNew();
        List<Vector2> points = PoissonDiscSampling.GeneratePointsParallel(Radius, _region, _attemptNum, _mask).ToList();

        stopwatch.Stop();
        GD.Print($"GeneratePoints cost: {stopwatch.ElapsedMilliseconds} ms");
        stopwatch.Restart();

        MultiMesh multiMesh = new MultiMesh()
        {
            Mesh = _mesh,
            TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
            InstanceCount = points.Count,
        };
        for (int i = 0; i < points.Count; i++)
        {
            Transform3D transform = new(Basis.Identity, new Vector3(points[i].X, 0, points[i].Y));
            multiMesh.SetInstanceTransform(i, transform);
        }
        stopwatch.Stop();
        GD.Print($"Generate mesh cost: {stopwatch.ElapsedMilliseconds} ms");
		GD.Print($"point count: {points.Count}");
        return multiMesh;
    }
}
