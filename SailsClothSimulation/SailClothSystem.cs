using Cairo;
using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace SailsClothSimulation
{
    public class SailCloth : ClothSystem
    {
        public int WidthPoints { get; private set; }
        public int HeightPoints { get; private set; }
        public float RestDistance { get; private set; }
        public HashSet<(int x, int y)> Anchored = new();
        public string MaterialShaderName = "sail";
        public float TimeSeconds { get; private set; }

        private MeshRef meshRef;
        private CustomMeshDataPartFloat meshData;
        private ICoreClientAPI capi;

        public SailCloth(ICoreClientAPI capi, int widthPoints, int heightPoints, float restDistance, Vec3d origin)
            : base() // calls protected ClothSystem() constructor
        {
            this.capi = capi;
            this.WidthPoints = widthPoints;
            this.HeightPoints = heightPoints;
            this.RestDistance = restDistance;

            Points2d.Clear();

            // Initialize ClothPoints in a 2D grid
            int pointIndex = 0;
            for (int x = 0; x < WidthPoints; x++)
            {
                PointList col = new PointList();
                Points2d.Add(col);

                for (int y = 0; y < HeightPoints; y++)
                {
                    Vec3d pos = origin + new Vec3d(
                        (x - (WidthPoints - 1) / 2.0) * RestDistance,
                        -y * RestDistance,
                        0
                    );

                    ClothPoint cp = new ClothPoint(this, pointIndex++, pos.X, pos.Y, pos.Z);
                    col.Points.Add(cp);
                }
            }

            // Generate constraints (structural + bending)
            GenerateConstraints();

            // Initialize mesh data
            meshData = new CustomMeshDataPartFloat(capi);
        }

        private void GenerateConstraints()
        {
            Constraints.Clear();

            for (int x = 0; x < WidthPoints; x++)
            {
                for (int y = 0; y < HeightPoints; y++)
                {
                    var p = Points2d[x].Points[y];

                    // Structural constraints
                    if (x + 1 < WidthPoints) Constraints.Add(new ClothConstraint(p, Points2d[x + 1].Points[y]));
                    if (y + 1 < HeightPoints) Constraints.Add(new ClothConstraint(p, Points2d[x].Points[y + 1]));

                    // Diagonal constraints
                    if (x + 1 < WidthPoints && y + 1 < HeightPoints)
                        Constraints.Add(new ClothConstraint(p, Points2d[x + 1].Points[y + 1]));
                    if (x - 1 >= 0 && y + 1 < HeightPoints)
                        Constraints.Add(new ClothConstraint(p, Points2d[x - 1].Points[y + 1]));

                    // Bending constraints
                    if (x + 2 < WidthPoints) Constraints.Add(new ClothConstraint(p, Points2d[x + 2].Points[y]) { Stiffness = 0.35f });
                    if (y + 2 < HeightPoints) Constraints.Add(new ClothConstraint(p, Points2d[x].Points[y + 2]) { Stiffness = 0.35f });
                    if (x + 2 < WidthPoints && y + 2 < HeightPoints)
                        Constraints.Add(new ClothConstraint(p, Points2d[x + 2].Points[y + 2]) { Stiffness = 0.35f });
                }
            }
        }

        public void AnchorCorner(int x, int y)
        {
            if (x >= 0 && x < WidthPoints && y >= 0 && y < HeightPoints)
                Anchored.Add((x, y));
        }

        private bool IsAnchored(int x, int y) => Anchored.Contains((x, y));

        public void Simulate(float dt)
        {
            TimeSeconds += dt;
            windSpeed = GetWindAtCenter() * 0.8;

            WalkPoints(p =>
            {
                var (x, y) = GetPointGridCoords(p);
                if (IsAnchored(x, y)) return;

                Vec3d accel = new Vec3d(0, -0.5, 0) + windSpeed;
                p.Velocity.Add(accel * dt);
                p.Velocity.Mul(1 - 0.015f);
                p.Pos.Add(p.Velocity.X * dt, p.Velocity.Y * dt, p.Velocity.Z * dt);
            });

            // Apply constraints multiple times
            for (int i = 0; i < 4; i++)
            {
                for (int j = Constraints.Count - 1; j >= 0; j--)
                    Constraints[j].satisfy(dt);

                EnforceAnchors();
            }
        }

        private void EnforceAnchors()
        {
            foreach (var anchor in Anchored)
            {
                var p = Points2d[anchor.x].Points[anchor.y];
                Vec3d target = new Vec3d(
                    CenterPosition.X + (anchor.x - (WidthPoints - 1) / 2.0) * RestDistance,
                    CenterPosition.Y - anchor.y * RestDistance,
                    CenterPosition.Z
                );
                p.Pos = target;
                p.Velocity.Set(0, 0, 0);
            }
        }

        private Vec3d GetWindAtCenter()
        {
            try { return api.World.BlockAccessor.GetWindSpeedAt(CenterPosition.AsBlockPos); }
            catch { return new Vec3d(0.8, 0, 0.2); }
        }

        private (int x, int y) GetPointGridCoords(ClothPoint p)
        {
            for (int x = 0; x < Points2d.Count; x++)
                for (int y = 0; y < Points2d[x].Points.Count; y++)
                    if (Points2d[x].Points[y] == p) return (x, y);

            return (-1, -1);
        }

        public void BuildMesh()
        {
            meshData.Clear();

            for (int x = 0; x < WidthPoints - 1; x++)
            {
                for (int y = 0; y < HeightPoints - 1; y++)
                {
                    var p00 = Points2d[x].Points[y];
                    var p10 = Points2d[x + 1].Points[y];
                    var p11 = Points2d[x + 1].Points[y + 1];
                    var p01 = Points2d[x].Points[y + 1];

                    Vec3f n = ComputeNormal(x, y);

                    int i00 = meshData.AddVertex((Vec3f)p00.Pos, n, new Vec2f((float)x / (WidthPoints - 1), (float)y / (HeightPoints - 1)));
                    int i10 = meshData.AddVertex((Vec3f)p10.Pos, n, new Vec2f((float)(x + 1) / (WidthPoints - 1), (float)y / (HeightPoints - 1)));
                    int i11 = meshData.AddVertex((Vec3f)p11.Pos, n, new Vec2f((float)(x + 1) / (WidthPoints - 1), (float)(y + 1) / (HeightPoints - 1)));
                    int i01 = meshData.AddVertex((Vec3f)p01.Pos, n, new Vec2f((float)x / (WidthPoints - 1), (float)(y + 1) / (HeightPoints - 1)));

                    meshData.AddTriangle(i00, i10, i11);
                    meshData.AddTriangle(i00, i11, i01);
                }
            }

            if (meshRef == null)
                meshRef = capi.Render.UploadMesh(meshData.ToMeshData());
            else
                capi.Render.UpdateMesh(meshRef, meshData.ToMeshData());
        }

        private Vec3f ComputeNormal(int x, int y)
        {
            Vec3d c = Points2d[x].Points[y].Pos;
            Vec3d r = x + 1 < WidthPoints ? Points2d[x + 1].Points[y].Pos : c;
            Vec3d l = x - 1 >= 0 ? Points2d[x - 1].Points[y].Pos : c;
            Vec3d u = y - 1 >= 0 ? Points2d[x].Points[y - 1].Pos : c;
            Vec3d d = y + 1 < HeightPoints ? Points2d[x].Points[y + 1].Pos : c;

            Vec3d dx = r - l;
            Vec3d dy = d - u;
            dx.CrossCopy(dy).Normalize();
            return (Vec3f)dx;
        }
    }
}
