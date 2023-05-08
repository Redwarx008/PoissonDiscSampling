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
			UpdateMultiMesh();
		}
	}
	private float _radius = 0.5f;
	[Export]
	public int AttemptNum
	{
		get => _attemptNum;
		set
		{
			if(_attemptNum == value)
			{
				return;
			}
			_attemptNum = value;
			UpdateMultiMesh();
		}
	}
	private int _attemptNum = 30;

	[Export]
	public Rect2I Region
	{
		get => _region;
		set
		{
			if(_region == value)
			{
				return;
			}
			_region = value;
			UpdateMultiMesh();
		}
	}
	private Rect2I _region = new Rect2I(50, 50, 100, 100);

	
    [Export]
	public bool EnableMultithread
	{
		get => _enableMultithread;
		set
		{
			_enableMultithread = value;
			if(_enableMultithread == true)
			{
				_generatePointsFunction = PoissonDiscSampling.GeneratePointsParallel;
			}
			else
			{
				_generatePointsFunction = PoissonDiscSampling.GeneratePoints;
			}
		}
	}

	private bool _enableMultithread = true;
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

	private MultiMesh _multiMesh;

	private delegate IEnumerable<Vector2> GeneratePointsFunction(float radius, Rect2I region, 
		int attemptNum = 30, Image mask = null);

    private GeneratePointsFunction _generatePointsFunction = PoissonDiscSampling.GeneratePointsParallel;
    
	public override void _Ready()
	{
        _mask = GD.Load<Image>("res://mask.png");
		_mask.Convert(Image.Format.R8);
        if (_mask == null)
        {
            GD.Print("mask import incorrectly");
        }

        _multiMesh = new MultiMesh()
        {
            Mesh = _mesh,
            TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
        };
		UpdateMultiMesh();
        _meshInstance = new MultiMeshInstance3D()
		{
			Multimesh = _multiMesh
		};
		AddChild(_meshInstance);
    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	private void UpdateMultiMesh()
	{
        Stopwatch stopwatch = Stopwatch.StartNew();

        IEnumerable<Vector2> points = _generatePointsFunction(Radius, _region, _attemptNum);

        stopwatch.Stop();

        GD.Print($"GeneratePoints cost: {stopwatch.ElapsedMilliseconds} ms");

        stopwatch.Restart();

		int pointCount = points.Count();
        _multiMesh.InstanceCount = pointCount;

		int index = 0;
		foreach (Vector2 point in points)
		{
            Transform3D transform = new(Basis.Identity, new Vector3(point.X, 0, point.Y));
            _multiMesh.SetInstanceTransform(index++, transform);
        }
        stopwatch.Stop();

        GD.Print($"Generate mesh cost: {stopwatch.ElapsedMilliseconds} ms");
        GD.Print($"point count: {pointCount}");
    }
}
