using Godot;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public static class PoissonDiscSampling
{
    public static int BlockSize { get; set; } = 32;
    public static List<Vector2> GeneratePoints(float radius, Rect2I region, int attemptNum = 30, Image mask = null)
    {
        List<Vector2> points = new List<Vector2>();
        List<Vector2> pendingPoints = new List<Vector2>();

        float cellSize = radius / MathF.Sqrt(2);
        int[,] grid = new int[(int)MathF.Ceiling(region.Size.X / cellSize), (int)MathF.Ceiling(region.Size.Y / cellSize)];

        pendingPoints.Add(new Vector2(region.Position.X + cellSize / 2, region.Position.Y + cellSize / 2));
        Random rnd = new Random();
        while (pendingPoints.Count > 0)
        {
            //int pendingIndex = rnd.Next(pendingPoints.Count);
            int pendingIndex = 0;
            Vector2 pendingCenter = pendingPoints[pendingIndex];

            bool isValid = false;

            for (int i = 0; i < attemptNum; ++i)
            {
                float angle = rnd.NextSingle() * MathF.PI * 2;
                Vector2 dir = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
                Vector2 pendingPoint = pendingCenter + dir * (rnd.NextSingle() * 2 * radius + radius);
                if (IsValidPoint(pendingPoint, region, cellSize, radius, points, grid, mask))
                {
                    points.Add(pendingPoint);
                    pendingPoints.Add(pendingPoint);
                    grid[(int)((pendingPoint.X - region.Position.X)/ cellSize), 
                        (int)((pendingPoint.Y - region.Position.Y)/ cellSize)] = points.Count;
                    isValid = true;
                    break;
                }
            }
            if (!isValid)
            {
                pendingPoints.RemoveAt(pendingIndex);
            }
        }
        return points;
    }
    public static ConcurrentBag<Vector2> GeneratePointsParallel(float radius, Rect2I region, int attemptNum = 30, Image mask = null)
    {
        ConcurrentBag<Vector2> points = new ConcurrentBag<Vector2>();

        List<Rect2I> regions = new();

        int regionXEnd = region.Position.X + region.Size.X;
        int regionYEnd = region.Position.Y + region.Size.Y; 
        for (int y = region.Position.Y; y < regionYEnd; y += BlockSize)
        {
            for (int x = region.Position.X; x < regionXEnd; x += BlockSize)
            {
                int subRegionWidth = Math.Min(BlockSize, regionXEnd - x);
                int subRegionHeight = Math.Min(BlockSize, regionYEnd - y);
                Rect2I subRegion = new(x, y, subRegionWidth, subRegionHeight);
                regions.Add(subRegion);
            }
        }

        Parallel.For(0, regions.Count, i =>
        {
            List<Vector2> subPoints = GeneratePoints(radius, regions[i], attemptNum, mask);
            foreach (Vector2 point in subPoints)
            {
                points.Add(point);
            }
        });
        return points;
    }
    private static bool IsValidPoint(in Vector2 pendingPoint, in Rect2I region, float cellSize,
        float radius, in List<Vector2> points, in int[,] grid, Image mask = null)
    {
        if (pendingPoint.X < region.Position.X || pendingPoint.Y < region.Position.Y)
        {
            return false;
        }
        if (pendingPoint.X > region.Position.X + region.Size.X || pendingPoint.Y > region.Position.Y + region.Size.Y)
        {
            return false;
        }
        if(mask != null && mask.GetPixel((int)pendingPoint.X, (int)pendingPoint.Y).R8 == 0)
        {
            return false;
        }

        int cellX = (int)((pendingPoint.X - region.Position.X)/ cellSize);
        int cellY = (int)((pendingPoint.Y - region.Position.Y) / cellSize);

        int boundStartX = 0;
        int boundEndX = (int)(region.Size.X / cellSize);
        int boundStartY = 0;
        int boundEndY = (int)(region.Size.Y / cellSize);

        int searchStartX = Math.Max(boundStartX, cellX - 2);
        int searchEndX = Math.Min(cellX + 2, boundEndX);
        int searchStartY = Math.Max(boundStartY, cellY - 2);
        int searchEndY = Math.Min(cellY + 2, boundEndY);

        for (int y = searchStartY; y <= searchEndY; ++y)
        {
            for (int x = searchStartX; x <= searchEndX; ++x)
            {
                int pointIndex = grid[x, y] - 1;
                // if there are other points here
                if (pointIndex != -1)
                {
                    float sqrDst = (pendingPoint - points[pointIndex]).LengthSquared();
                    if (radius * radius > sqrDst)
                    {
                        return false;
                    }
                }
            }
        }
        return true;
    }
}