using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace SailsClothSimulation;

[ProtoContract]
public class ClothSystemNew

{
    [ProtoMember(1)]
    public int ClothId;

    [ProtoMember(2)]
    private EnumClothType clothType;

    [ProtoMember(3)]
    private List<PointList> Points2d = new List<PointList>();

    [ProtoMember(4)]
    private List<ClothConstraint> Constraints = new List<ClothConstraint>();


    public enum EnumExtendedClothType
    {
        Rope,
        Cloth,
        Sail
    }

    private EnumExtendedClothType extendedClothType;



    public static float Resolution = 2f;

    public float StretchWarn = 0.6f;

    public float StretchRip = 0.75f;

    public bool LineDebug;

    public bool boyant;

    protected ICoreClientAPI capi;

    public ICoreAPI api;

    public Vec3d windSpeed = new Vec3d();

    public ParticlePhysics pp;

    protected NormalizedSimplexNoise noiseGen;

    protected float[] tmpMat = new float[16];

    protected Vec3f distToCam = new Vec3f();

    protected AssetLocation ropeSectionModel;

    protected MeshData debugUpdateMesh;

    protected MeshRef debugMeshRef;

    public float secondsOverStretched;

    private double minLen = 1.5;

    private double maxLen = 10.0;

    private Matrixf mat = new Matrixf();

    private float accum;
    

    [ProtoMember(5)]
    public bool Active { get; set; }

    public int widthPoints;
    public int heightPoints;
    public float restDistance;

    public HashSet<(int, int)> Anchored = new();
    private Dictionary<int, (int x, int y)> pointIndexToGrid = new Dictionary<int, (int, int)>();


    public float TimeSeconds;
    public string MaterialShaderName = "Sail";

    public MeshRef MeshRef;
    public CustomMeshDataPartFloat MeshData;

    private int vertexCount = 0;

    private ClothSystem baseSystem; // replaced all the "this" with base system to reflect the old ClothSystem to stop erroring



    public bool PinnedAnywhere
    {
        get
        {
            foreach (PointList item in Points2d)
            {
                foreach (ClothPoint point in item.Points)
                {
                    if (point.Pinned)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }

    public double MaxExtension
    {
        get
        {
            if (Constraints.Count != 0)
            {
                return Constraints.Max((ClothConstraint c) => c.Extension);
            }

            return 0.0;
        }
    }

    public Vec3d CenterPosition
    {
        get
        {
            Vec3d vec3d = new Vec3d();
            int num = 0;
            foreach (PointList item in Points2d)
            {
                foreach (ClothPoint point in item.Points)
                {
                    _ = point;
                    num++;
                }
            }

            foreach (PointList item2 in Points2d)
            {
                foreach (ClothPoint point2 in item2.Points)
                {
                    vec3d.Add(point2.Pos.X / (double)num, point2.Pos.Y / (double)num, point2.Pos.Z / (double)num);
                }
            }

            return vec3d;
        }
    }

    public ClothPoint FirstPoint => Points2d[0].Points[0];

    public ClothPoint LastPoint
    {
        get
        {
            List<ClothPoint> points = Points2d[Points2d.Count - 1].Points;
            return points[points.Count - 1];
        }
    }

    public ClothPoint[] Ends => new ClothPoint[2] { FirstPoint, LastPoint };

    public static ClothSystemNew CreateCloth(ICoreAPI api, ClothManager cm, Vec3d start, Vec3d end)
    {
        return new ClothSystemNew(api, cm, start, end, EnumExtendedClothType.Cloth, 0, 0, 0);  
    }

    public static ClothSystemNew CreateRope(ICoreAPI api, ClothManager cm, Vec3d start, Vec3d end, AssetLocation clothSectionModel)
    {
        return new ClothSystemNew(api, cm, start, end, EnumExtendedClothType.Rope, 0, 0, 0, clothSectionModel); 
    }

    public static ClothSystemNew CreateSail(ICoreAPI api, ClothManager cm, Vec3d start, Vec3d end, AssetLocation clothSectionModel)
    {
        return new ClothSystemNew(api, cm, start, end, EnumExtendedClothType.Sail, 0, 0, 0, clothSectionModel);
    }

    private ClothSystemNew()
    {
    }

    public bool ChangeRopeLength(double len)
    {
        PointList pointList = Points2d[0];
        double num = (float)pointList.Points.Count / Resolution;
        bool flag = len > 0.0;

        baseSystem = (ClothSystem)RuntimeHelpers.GetUninitializedObject(typeof(ClothSystem));

        if (flag && len + num > maxLen)
        {
            return false;
        }

        if (!flag && len + num < minLen)
        {
            return false;
        }

        int num2 = pointList.Points.Max((ClothPoint p) => p.PointIndex) + 1;
        ClothPoint firstPoint = FirstPoint;
        Entity pinnedToEntity = firstPoint.PinnedToEntity;
        BlockPos pinnedToBlockPos = firstPoint.PinnedToBlockPos;
        Vec3f pinnedToOffset = firstPoint.pinnedToOffset;
        firstPoint.UnPin();
        float num3 = 1f / Resolution;
        int num4 = Math.Abs((int)(len * (double)Resolution));
        if (flag)
        {
            for (int i = 0; i <= num4; i++)
            {
                pointList.Points.Insert(0, new ClothPoint(baseSystem, num2++, firstPoint.Pos.X + (double)(num3 * (float)(i + 1)), firstPoint.Pos.Y, firstPoint.Pos.Z));
                ClothPoint p2 = pointList.Points[0];
                ClothPoint p3 = pointList.Points[1];
                ClothConstraint item = new ClothConstraint(p2, p3);
                Constraints.Add(item);
            }
        }
        else
        {
            for (int j = 0; j <= num4; j++)
            {
                ClothPoint clothPoint = pointList.Points[0];
                pointList.Points.RemoveAt(0);
                for (int k = 0; k < Constraints.Count; k++)
                {
                    ClothConstraint clothConstraint = Constraints[k];
                    if (clothConstraint.Point1 == clothPoint || clothConstraint.Point2 == clothPoint)
                    {
                        Constraints.RemoveAt(k);
                        k--;
                    }
                }
            }
        }

        if (pinnedToEntity != null)
        {
            FirstPoint.PinTo(pinnedToEntity, pinnedToOffset);
        }

        if (pinnedToBlockPos != null)
        {
            FirstPoint.PinTo(pinnedToBlockPos, pinnedToOffset);
        }

        genDebugMesh();
        return true;
    }
    

    private ClothSystemNew(ICoreAPI api, ClothManager cm, Vec3d start, Vec3d end, EnumExtendedClothType clothType, int widthPoints, int heightPoints, float restDistance, AssetLocation ropeSectionModel = null)
    {
        this.extendedClothType = clothType;
        this.clothType = clothType == EnumExtendedClothType.Rope ? EnumClothType.Rope : EnumClothType.Cloth;
        this.ropeSectionModel = ropeSectionModel;

        this.widthPoints = widthPoints > 0 ? widthPoints : 12;
        this.heightPoints = heightPoints > 0 ? heightPoints : 8;
        this.restDistance = restDistance > 0 ? restDistance : 0.25f;

        baseSystem = (ClothSystem)RuntimeHelpers.GetUninitializedObject(typeof(ClothSystem));


        Init(api, cm);
        _ = 1f / Resolution;
        Vec3d vec3d = end - start;
        if (clothType == EnumExtendedClothType.Rope)
        {
            double num = vec3d.Length();
            PointList pointList = new PointList();
            Points2d.Add(pointList);
            int num2 = (int)(num * (double)Resolution);

            for (int i = 0; i <= num2; i++)
            {
                float num3 = (float)i / (float)num2;
                pointList.Points.Add(new ClothPoint(baseSystem, i,
                    start.X + vec3d.X * num3,
                    start.Y + vec3d.Y * num3,
                    start.Z + vec3d.Z * num3));

                if (i > 0)
                {
                    ClothPoint p = pointList.Points[i - 1];
                    ClothPoint p2 = pointList.Points[i];
                    Constraints.Add(new ClothConstraint(p, p2));
                }
            }
        }
        else if (clothType == EnumExtendedClothType.Cloth)
        {
            // modular cloth logic here
        }
        else if (clothType == EnumExtendedClothType.Sail)
        {


            Points2d.Clear();

            int pointIndex = 0;
            for (int x = 0; x < widthPoints; x++)
            {
                PointList col = new PointList();
                Points2d.Add(col);
                for (int y = 0; y < heightPoints; y++)
                {
                    Vec3d pos = start + new Vec3d(
                        (x - (widthPoints - 1) / 2.0) * restDistance, -y * restDistance, 0);
                    col.Points.Add(new ClothPoint(baseSystem, pointIndex++, pos.X, pos.Y, pos.Z));
                }
            }

            GenerateSailConstraints(widthPoints, heightPoints, restDistance);
            MeshData = new CustomMeshDataPartFloat(5) // 3 xyz pos + 2 uv
            {
                Count = 0,
                Values = new float[widthPoints * heightPoints * 8] // rough estimate
            };

            BuildPointIndexMap();
        }


        double num4 = (end - start).HorLength();
        double num5 = Math.Abs(end.Y - start.Y);
        int num6 = (int)(num4 * (double)Resolution);
        int num7 = (int)(num5 * (double)Resolution);
        int num8 = 0;
        for (int j = 0; j < num6; j++)
        {
            Points2d.Add(new PointList());
            for (int k = 0; k < num7; k++)
            {
                double num9 = (double)j / num4;
                double num10 = (double)k / num5;
                Points2d[j].Points.Add(new ClothPoint(baseSystem, num8++, start.X + vec3d.X * num9, start.Y + vec3d.Y * num10, start.Z + vec3d.Z * num9));
                if (j > 0)
                {
                    ClothPoint p3 = Points2d[j - 1].Points[k];
                    ClothPoint p4 = Points2d[j].Points[k];
                    ClothConstraint item2 = new ClothConstraint(p3, p4);
                    Constraints.Add(item2);
                }

                if (k > 0)
                {
                    ClothPoint p5 = Points2d[j].Points[k - 1];
                    ClothPoint p6 = Points2d[j].Points[k];
                    ClothConstraint item3 = new ClothConstraint(p5, p6);
                    Constraints.Add(item3);
                }
            }
        }
    }

    public void genDebugMesh()
    {
        if (capi != null)
        {
            debugMeshRef?.Dispose();
            debugUpdateMesh = new MeshData(20, 15, withNormals: false, withUv: false);
            int num = 0;
            for (int i = 0; i < Constraints.Count; i++)
            {
                _ = Constraints[i];
                int color = ((i % 2 > 0) ? (-1) : ColorUtil.BlackArgb);
                debugUpdateMesh.AddVertexSkipTex(0f, 0f, 0f, color);
                debugUpdateMesh.AddVertexSkipTex(0f, 0f, 0f, color);
                debugUpdateMesh.AddIndex(num++);
                debugUpdateMesh.AddIndex(num++);
            }

            debugUpdateMesh.mode = EnumDrawMode.Lines;
            debugMeshRef = capi.Render.UploadMesh(debugUpdateMesh);
            debugUpdateMesh.Indices = null;
            debugUpdateMesh.Rgba = null;
        }
    }

    private void Init(ICoreAPI api, ClothManager cm)
    {
        this.api = api;
        capi = api as ICoreClientAPI;

        // Try public accessor first, fallback to reflection
        var prop = cm.GetType().GetProperty("PartPhysics");
        if (prop != null)
        {
            pp = (ParticlePhysics)prop.GetValue(cm);
        }
        else
        {
            var field = cm.GetType().GetField("partPhysics", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            pp = (ParticlePhysics)field?.GetValue(cm);
        }

        noiseGen = NormalizedSimplexNoise.FromDefaultOctaves(4, 100.0, 0.9, api.World.Seed + CenterPosition.GetHashCode());
    }

    public void WalkPoints(Action<ClothPoint> onPoint)
    {
        foreach (PointList item in Points2d)
        {
            foreach (ClothPoint point in item.Points)
            {
                onPoint(point);
            }
        }
    }

    public int UpdateMesh(MeshData updateMesh, float dt)
    {
        CustomMeshDataPartFloat customFloats = updateMesh.CustomFloats;
        Vec3d cameraPos = capi.World.Player.Entity.CameraPos;
        int count = customFloats.Count;
        Vec4f vec4f = new Vec4f();
        if (Constraints.Count > 0)
        {
            vec4f = api.World.BlockAccessor.GetLightRGBs(Constraints[Constraints.Count / 2].Point1.Pos.AsBlockPos);
        }

        for (int i = 0; i < Constraints.Count; i++)
        {
            ClothConstraint clothConstraint = Constraints[i];
            Vec3d pos = clothConstraint.Point1.Pos;
            Vec3d pos2 = clothConstraint.Point2.Pos;
            double num = pos.X - pos2.X;
            double num2 = pos.Y - pos2.Y;
            double num3 = pos.Z - pos2.Z;
            float rad = (float)Math.Atan2(num, num3) + (float)Math.PI / 2f;
            float rad2 = (float)Math.Atan2(Math.Sqrt(num3 * num3 + num * num), num2) + (float)Math.PI / 2f;
            double num4 = pos.X + (pos.X - pos2.X) / 2.0;
            double num5 = pos.Y + (pos.Y - pos2.Y) / 2.0;
            double num6 = pos.Z + (pos.Z - pos2.Z) / 2.0;
            distToCam.Set((float)(num4 - cameraPos.X), (float)(num5 - cameraPos.Y), (float)(num6 - cameraPos.Z));
            Mat4f.Identity(tmpMat);
            Mat4f.Translate(tmpMat, tmpMat, 0f, 1f / 32f, 0f);
            Mat4f.Translate(tmpMat, tmpMat, distToCam.X, distToCam.Y, distToCam.Z);
            Mat4f.RotateY(tmpMat, tmpMat, rad);
            Mat4f.RotateZ(tmpMat, tmpMat, rad2);
            float rad3 = (float)i / 5f;
            Mat4f.RotateX(tmpMat, tmpMat, rad3);
            float num7 = GameMath.Sqrt(num * num + num2 * num2 + num3 * num3);
            Mat4f.Scale(tmpMat, tmpMat, new float[3] { num7, 1f, 1f });
            Mat4f.Translate(tmpMat, tmpMat, -1.5f, -1f / 32f, -0.5f);
            int num8 = count + i * 20;
            customFloats.Values[num8++] = vec4f.R;
            customFloats.Values[num8++] = vec4f.G;
            customFloats.Values[num8++] = vec4f.B;
            customFloats.Values[num8++] = vec4f.A;
            for (int j = 0; j < 16; j++)
            {
                customFloats.Values[num8 + j] = tmpMat[j];
            }
        }

        return Constraints.Count;
    }

    public void setRenderCenterPos()
    {
        for (int i = 0; i < Constraints.Count; i++)
        {
            ClothConstraint clothConstraint = Constraints[i];
            Vec3d pos = clothConstraint.Point1.Pos;
            Vec3d pos2 = clothConstraint.Point2.Pos;
            double x = pos.X + (pos.X - pos2.X) / 2.0;
            double y = pos.Y + (pos.Y - pos2.Y) / 2.0;
            double z = pos.Z + (pos.Z - pos2.Z) / 2.0;
            clothConstraint.renderCenterPos.X = x;
            clothConstraint.renderCenterPos.Y = y;
            clothConstraint.renderCenterPos.Z = z;
        }
    }

    public void CustomRender(float dt)
    {
        if (LineDebug && capi != null)
        {
            if (debugMeshRef == null)
            {
                genDebugMesh();
            }

            BlockPos asBlockPos = CenterPosition.AsBlockPos;
            for (int i = 0; i < Constraints.Count; i++)
            {
                ClothConstraint clothConstraint = Constraints[i];
                Vec3d pos = clothConstraint.Point1.Pos;
                Vec3d pos2 = clothConstraint.Point2.Pos;
                debugUpdateMesh.xyz[i * 6] = (float)(pos.X - (double)asBlockPos.X);
                debugUpdateMesh.xyz[i * 6 + 1] = (float)(pos.Y - (double)asBlockPos.Y) + 0.005f;
                debugUpdateMesh.xyz[i * 6 + 2] = (float)(pos.Z - (double)asBlockPos.Z);
                debugUpdateMesh.xyz[i * 6 + 3] = (float)(pos2.X - (double)asBlockPos.X);
                debugUpdateMesh.xyz[i * 6 + 4] = (float)(pos2.Y - (double)asBlockPos.Y) + 0.005f;
                debugUpdateMesh.xyz[i * 6 + 5] = (float)(pos2.Z - (double)asBlockPos.Z);
            }

            capi.Render.UpdateMesh(debugMeshRef, debugUpdateMesh);
            IShaderProgram program = capi.Shader.GetProgram(23);
            program.Use();
            capi.Render.LineWidth = 6f;
            capi.Render.BindTexture2d(0);
            capi.Render.GLDisableDepthTest();
            Vec3d cameraPos = capi.World.Player.Entity.CameraPos;
            mat.Set(capi.Render.CameraMatrixOrigin);
            mat.Translate((float)((double)asBlockPos.X - cameraPos.X), (float)((double)asBlockPos.Y - cameraPos.Y), (float)((double)asBlockPos.Z - cameraPos.Z));
            program.UniformMatrix("projectionMatrix", capi.Render.CurrentProjectionMatrix);
            program.UniformMatrix("modelViewMatrix", mat.Values);
            capi.Render.RenderMesh(debugMeshRef);
            program.Stop();
            capi.Render.GLEnableDepthTest();
        }
    }

    public void updateFixedStep(float dt)
    {
        accum += dt;
        if (accum > 1f)
        {
            accum = 0.25f;
        }

        float physicsTickTime = pp.PhysicsTickTime;
        while (accum >= physicsTickTime)
        {
            accum -= physicsTickTime;
            tickNow(physicsTickTime);
        }
    }

    private void tickNow(float pdt)
    {
        for (int num = Constraints.Count - 1; num >= 0; num--)
        {
            Constraints[num].satisfy(pdt);
        }

        for (int num2 = Points2d.Count - 1; num2 >= 0; num2--)
        {
            for (int num3 = Points2d[num2].Points.Count - 1; num3 >= 0; num3--)
            {
                Points2d[num2].Points[num3].update(pdt, api.World);
            }
        }

        if (extendedClothType == EnumExtendedClothType.Sail)
        {
            TimeSeconds += pdt;
            windSpeed = GetWindAtCenter() * 0.8;

            WalkPoints(p =>
            {
                var (x, y) = GetPointGridCoords(p);
                if (IsAnchored(x, y)) return;

                Vec3d accel = new Vec3d(0, -0.5, 0) + windSpeed;
                p.Velocity.Add(accel * pdt);
                p.Velocity.Mul(1 - 0.015f);
                p.Pos.Add(p.Velocity.X * pdt, p.Velocity.Y * pdt, p.Velocity.Z * pdt);
            });

            for (int i = 0; i < 4; i++)
            {
                for (int j = Constraints.Count - 1; j >= 0; j--)
                    Constraints[j].satisfy(pdt);

                EnforceAnchors();
            }
        }
    }

    public void slowTick3s()
    {
        if (!double.IsNaN(CenterPosition.X))
        {
            windSpeed = api.World.BlockAccessor.GetWindSpeedAt(CenterPosition) * (0.2 + noiseGen.Noise(0.0, api.World.Calendar.TotalHours * 50.0 % 2000.0) * 0.8);
        }
    }

    public void restoreReferences()
    {
        baseSystem = (ClothSystem)RuntimeHelpers.GetUninitializedObject(typeof(ClothSystem));

        if (!Active)
        {
            return;
        }

        Dictionary<int, ClothPoint> pointsByIndex = new Dictionary<int, ClothPoint>();
        WalkPoints(delegate (ClothPoint p)
        {
            pointsByIndex[p.PointIndex] = p;
            p.restoreReferences(baseSystem, api.World);
        });
        foreach (ClothConstraint constraint in Constraints)
        {
            constraint.RestorePoints(pointsByIndex);
        }
    }

    public void updateActiveState(EnumActiveStateChange stateChange)
    {
        if ((!Active || stateChange != EnumActiveStateChange.RegionNowLoaded) && (Active || stateChange != EnumActiveStateChange.RegionNowUnloaded))
        {
            bool active = Active;
            Active = true;
            WalkPoints(delegate (ClothPoint p)
            {
                Active &= api.World.BlockAccessor.GetChunkAtBlockPos((int)p.Pos.X, (int)p.Pos.Y, (int)p.Pos.Z) != null;
            });
            if (!active && Active)
            {
                restoreReferences();
            }
        }
    }

    public void CollectDirtyPoints(List<ClothPointPacket> packets)
    {
        for (int i = 0; i < Points2d.Count; i++)
        {
            for (int j = 0; j < Points2d[i].Points.Count; j++)
            {
                ClothPoint clothPoint = Points2d[i].Points[j];
                if (clothPoint.Dirty)
                {
                    packets.Add(new ClothPointPacket
                    {
                        ClothId = ClothId,
                        PointX = i,
                        PointY = j,
                        Point = clothPoint
                    });
                    // clothPoint.Dirty = false; SOS for some reason it kept giving me an error here so i just removed it to see if the rest of the logic works SOS
                }
            }
        }
    }



    public void updatePoint(ClothPointPacket msg)
    {
        if (msg.PointX >= Points2d.Count)
        {
            api.Logger.Error($"ClothSystem: {ClothId} got invalid Points2d update index for {msg.PointX}/{Points2d.Count}");
        }
        else if (msg.PointY >= Points2d[msg.PointX].Points.Count)
        {
            api.Logger.Error($"ClothSystem: {ClothId} got invalid Points2d[{msg.PointX}] update index for {msg.PointY}/{Points2d[msg.PointX].Points.Count}");
        }
        else
        {
            Points2d[msg.PointX].Points[msg.PointY].updateFromPoint(msg.Point, api.World);
        }
    }

    public void OnPinnnedEntityLoaded(Entity entity)
    {
        if (FirstPoint.pinnedToEntityId == entity.EntityId)
        {
            FirstPoint.restoreReferences(entity);
        }

        if (LastPoint.pinnedToEntityId == entity.EntityId)
        {
            LastPoint.restoreReferences(entity);
        }
    }

    public void GenerateSailConstraints(int widthPoints, int heightPoints, float restDistance)
    {
        Constraints.Clear();

        for (int x = 0; x < widthPoints; x++)
        {
            if (x >= Points2d.Count)
            {
                capi.Logger.Warning("Skipped x={x}, Points2d.Count={Points2d.Count}");
                continue;
            }

            for (int y = 0; y < heightPoints; y++)
            {
                if (y >= Points2d[x].Points.Count)
                {
                    capi.Logger.Warning("Skipped y={y}, Points2d[{x}].Points.Count={Points2d[x].Points.Count}");
                    continue;
                }

                var p = Points2d[x].Points[y];

                // Structural constraints
                if (x + 1 < widthPoints) Constraints.Add(new ClothConstraint(p, Points2d[x + 1].Points[y]));
                if (y + 1 < heightPoints) Constraints.Add(new ClothConstraint(p, Points2d[x].Points[y + 1]));

                // Diagonal constraints
                if (x + 1 < widthPoints && y + 1 < heightPoints)
                    Constraints.Add(new ClothConstraint(p, Points2d[x + 1].Points[y + 1]));
                if (x - 1 >= 0 && y + 1 < heightPoints)
                    Constraints.Add(new ClothConstraint(p, Points2d[x - 1].Points[y + 1]));

                // Bending constraints
                if (x + 2 < widthPoints) Constraints.Add(new ClothConstraint(p, Points2d[x + 2].Points[y]));
                if (y + 2 < heightPoints) Constraints.Add(new ClothConstraint(p, Points2d[x].Points[y + 2]));
                if (x + 2 < widthPoints && y + 2 < heightPoints)
                    Constraints.Add(new ClothConstraint(p, Points2d[x + 2].Points[y + 2]));
            }
        }
    }

    public void BuildSailMesh(int widthPoints, int heightPoints, float restDistance)
    {
        if (extendedClothType != EnumExtendedClothType.Sail) return;

        MeshData mesh = new MeshData(true);

        for (int x = 0; x < widthPoints - 1; x++)
        {
            for (int y = 0; y < heightPoints - 1; y++)
            {
                var p00 = Points2d[x].Points[y];
                var p10 = Points2d[x + 1].Points[y];
                var p11 = Points2d[x + 1].Points[y + 1];
                var p01 = Points2d[x].Points[y + 1];

                Vec3f n = ComputeNormal(x, y);

                int i00 = mesh.VerticesCount;
                mesh.AddVertex((float)p00.Pos.X, (float)p00.Pos.Y, (float)p00.Pos.Z, x / (float)(widthPoints - 1), y / (float)(heightPoints - 1));
                mesh.AddNormal(n.X, n.Y, n.Z);

                int i10 = mesh.VerticesCount;
                mesh.AddVertex((float)p10.Pos.X, (float)p10.Pos.Y, (float)p10.Pos.Z, (x + 1) / (float)(widthPoints - 1), y / (float)(heightPoints - 1));
                mesh.AddNormal(n.X, n.Y, n.Z);

                int i11 = mesh.VerticesCount;
                mesh.AddVertex((float)p11.Pos.X, (float)p11.Pos.Y, (float)p11.Pos.Z, (x + 1) / (float)(widthPoints - 1), (y + 1) / (float)(heightPoints - 1));
                mesh.AddNormal(n.X, n.Y, n.Z);

                int i01 = mesh.VerticesCount;
                mesh.AddVertex((float)p01.Pos.X, (float)p01.Pos.Y, (float)p01.Pos.Z, x / (float)(widthPoints - 1), (y + 1) / (float)(heightPoints - 1));
                mesh.AddNormal(n.X, n.Y, n.Z);


                mesh.AddIndices(i00, i10, i11, i00, i11, i01);
            }
        }

        if (MeshRef == null)
            MeshRef = capi.Render.UploadMesh(mesh);
        else
            capi.Render.UpdateMesh(MeshRef, mesh);
    }


    private void AddTriangle(MeshData mesh, int i0, int i1, int i2)
    {
        mesh.Indices.Append(i0);
        mesh.Indices.Append(i1);
        mesh.Indices.Append(i2);
    }



    // helper functions

    private Vec3f ComputeNormal(int x, int y) // calculates a surface from neighboring cloth points for shading
    {
        if (x + 1 >= widthPoints || y + 1 >= heightPoints) return new Vec3f(0, 0, 1);

        Vec3d p1 = Points2d[x].Points[y].Pos;
        Vec3d p2 = Points2d[x + 1].Points[y].Pos;
        Vec3d p3 = Points2d[x].Points[y + 1].Pos;

        Vec3d u = p2 - p1;
        Vec3d v = p3 - p1;
        Vec3d n = u.Cross(v);
        n.Normalize();

        return new Vec3f((float)n.X, (float)n.Y, (float)n.Z);
    }


    private Vec3d GetWindAtCenter()
    {
        if (api?.World == null) return Vec3d.Zero;

        // Compute approximate center point
        double cx = 0, cy = 0, cz = 0;
        int count = 0;
        foreach (var col in Points2d)
        {
            foreach (var p in col.Points)
            {
                cx += p.Pos.X;
                cy += p.Pos.Y;
                cz += p.Pos.Z;
                count++;
            }
        }

        if (count == 0) return Vec3d.Zero;
        Vec3d center = new Vec3d(cx / count, cy / count, cz / count);

        return api.World.BlockAccessor.GetWindSpeedAt(center);
    }

    private bool IsAnchored(int x, int y)
    {
        return Anchored.Contains((x, y));
    }

    private void EnforceAnchors() // resets anchored points back to their original positions each tick
    {
        foreach (var (x, y) in Anchored)
        {
            if (x < 0 || x >= Points2d.Count) continue;
            if (y < 0 || y >= Points2d[x].Points.Count) continue;

            ClothPoint p = Points2d[x].Points[y];
            p.Pos.Set(p.Pos);
            p.Velocity.Set(0, 0, 0);
        }
    }

    
    private void BuildPointIndexMap()
    {
        pointIndexToGrid.Clear();
        for (int x = 0; x < Points2d.Count; x++)
        {
            var col = Points2d[x];
            for (int y = 0; y < col.Points.Count; y++)
            {
                var p = col.Points[y];
                pointIndexToGrid[p.PointIndex] = (x, y);
            }
        }
    }

    
    private (int x, int y) GetPointGridCoords(ClothPoint p)
    {
        if (p == null) return (-1, -1);

        // fast dictionary lookup first
        if (pointIndexToGrid != null && pointIndexToGrid.TryGetValue(p.PointIndex, out var coords))
        {
            return coords;
        }

        // fallback: linear scan it is slower
        for (int x = 0; x < Points2d.Count; x++)
        {
            var col = Points2d[x];
            for (int y = 0; y < col.Points.Count; y++)
            {
                if (ReferenceEquals(col.Points[y], p)) return (x, y);
            }
        }

        return (-1, -1);
    }

}


