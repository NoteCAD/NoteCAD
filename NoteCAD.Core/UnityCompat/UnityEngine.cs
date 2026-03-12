// Unity Engine compatibility shim for NoteCAD.Core
// Provides Unity-like math types and stubs so the core CAD code compiles
// without a Unity installation. Replace with real engine types when integrating
// with a specific target engine (e.g. Godot 4, MonoGame, Avalonia).

using System;
using System.Collections.Generic;
using System.Linq;

// ---------------------------------------------------------------------------
// UnityEngine namespace – math types and minimal stubs
// ---------------------------------------------------------------------------
namespace UnityEngine
{
    // -----------------------------------------------------------------------
    // Vector2
    // -----------------------------------------------------------------------
    public struct Vector2
    {
        public float x, y;

        public Vector2(float x, float y) { this.x = x; this.y = y; }

        public static readonly Vector2 zero    = new Vector2(0, 0);
        public static readonly Vector2 one     = new Vector2(1, 1);
        public static readonly Vector2 up      = new Vector2(0, 1);
        public static readonly Vector2 right   = new Vector2(1, 0);

        public float magnitude => MathF.Sqrt(x * x + y * y);
        public Vector2 normalized { get { float m = magnitude; return m > 0 ? this / m : zero; } }

        public static Vector2 operator +(Vector2 a, Vector2 b) => new Vector2(a.x + b.x, a.y + b.y);
        public static Vector2 operator -(Vector2 a, Vector2 b) => new Vector2(a.x - b.x, a.y - b.y);
        public static Vector2 operator -(Vector2 a)            => new Vector2(-a.x, -a.y);
        public static Vector2 operator *(Vector2 a, float b)   => new Vector2(a.x * b, a.y * b);
        public static Vector2 operator *(float a, Vector2 b)   => new Vector2(a * b.x, a * b.y);
        public static Vector2 operator /(Vector2 a, float b)   => new Vector2(a.x / b, a.y / b);
        public static bool operator ==(Vector2 a, Vector2 b)   => a.x == b.x && a.y == b.y;
        public static bool operator !=(Vector2 a, Vector2 b)   => !(a == b);

        public static implicit operator Vector3(Vector2 v) => new Vector3(v.x, v.y, 0);
        public static implicit operator Vector2(Vector3 v) => new Vector2(v.x, v.y);

        public static float Dot(Vector2 a, Vector2 b) => a.x * b.x + a.y * b.y;

        public override bool Equals(object? obj) => obj is Vector2 v && this == v;
        public override int GetHashCode() => HashCode.Combine(x, y);
        public override string ToString() => $"({x:F2}, {y:F2})";
    }

    // -----------------------------------------------------------------------
    // Vector3
    // -----------------------------------------------------------------------
    public struct Vector3
    {
        public float x, y, z;

        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public Vector3(float x, float y)          { this.x = x; this.y = y; this.z = 0; }

        public static readonly Vector3 zero    = new Vector3(0, 0, 0);
        public static readonly Vector3 one     = new Vector3(1, 1, 1);
        public static readonly Vector3 up      = new Vector3(0, 1, 0);
        public static readonly Vector3 down    = new Vector3(0, -1, 0);
        public static readonly Vector3 right   = new Vector3(1, 0, 0);
        public static readonly Vector3 left    = new Vector3(-1, 0, 0);
        public static readonly Vector3 forward = new Vector3(0, 0, 1);
        public static readonly Vector3 back    = new Vector3(0, 0, -1);

        public float magnitude => MathF.Sqrt(x * x + y * y + z * z);
        public float sqrMagnitude => x * x + y * y + z * z;
        public Vector3 normalized { get { float m = magnitude; return m > 0 ? this / m : zero; } }

        public float this[int i]
        {
            get { return i == 0 ? x : i == 1 ? y : z; }
            set { if (i == 0) x = value; else if (i == 1) y = value; else z = value; }
        }

        public static Vector3 operator +(Vector3 a, Vector3 b) => new Vector3(a.x + b.x, a.y + b.y, a.z + b.z);
        public static Vector3 operator -(Vector3 a, Vector3 b) => new Vector3(a.x - b.x, a.y - b.y, a.z - b.z);
        public static Vector3 operator -(Vector3 a)            => new Vector3(-a.x, -a.y, -a.z);
        public static Vector3 operator *(Vector3 a, float b)   => new Vector3(a.x * b, a.y * b, a.z * b);
        public static Vector3 operator *(float a, Vector3 b)   => new Vector3(a * b.x, a * b.y, a * b.z);
        public static Vector3 operator /(Vector3 a, float b)   => new Vector3(a.x / b, a.y / b, a.z / b);
        public static bool operator ==(Vector3 a, Vector3 b)   => a.x == b.x && a.y == b.y && a.z == b.z;
        public static bool operator !=(Vector3 a, Vector3 b)   => !(a == b);

        public static float Dot(Vector3 a, Vector3 b) => a.x * b.x + a.y * b.y + a.z * b.z;
        public static Vector3 Cross(Vector3 a, Vector3 b) => new Vector3(
            a.y * b.z - a.z * b.y,
            a.z * b.x - a.x * b.z,
            a.x * b.y - a.y * b.x
        );
        public static float Distance(Vector3 a, Vector3 b) => (a - b).magnitude;
        public static Vector3 Lerp(Vector3 a, Vector3 b, float t) => a + (b - a) * Mathf.Clamp01(t);
        public static Vector3 Normalize(Vector3 v) => v.normalized;
        public static Vector3 Project(Vector3 v, Vector3 n) => n * (Dot(v, n) / Dot(n, n));

        public static implicit operator Vector4(Vector3 v) => new Vector4(v.x, v.y, v.z, 0);

        public override bool Equals(object? obj) => obj is Vector3 v && this == v;
        public override int GetHashCode() => HashCode.Combine(x, y, z);
        public override string ToString() => $"({x:F4}, {y:F4}, {z:F4})";
    }

    // -----------------------------------------------------------------------
    // Vector4
    // -----------------------------------------------------------------------
    public struct Vector4
    {
        public float x, y, z, w;

        public Vector4(float x, float y, float z, float w) { this.x = x; this.y = y; this.z = z; this.w = w; }
        public Vector4(float x, float y, float z)          { this.x = x; this.y = y; this.z = z; this.w = 0; }

        public static readonly Vector4 zero = new Vector4(0, 0, 0, 0);
        public static readonly Vector4 one  = new Vector4(1, 1, 1, 1);

        public float this[int i]
        {
            get { return i == 0 ? x : i == 1 ? y : i == 2 ? z : w; }
            set { if (i == 0) x = value; else if (i == 1) y = value; else if (i == 2) z = value; else w = value; }
        }

        public float magnitude => MathF.Sqrt(x * x + y * y + z * z + w * w);

        public static Vector4 operator +(Vector4 a, Vector4 b) => new Vector4(a.x + b.x, a.y + b.y, a.z + b.z, a.w + b.w);
        public static Vector4 operator -(Vector4 a, Vector4 b) => new Vector4(a.x - b.x, a.y - b.y, a.z - b.z, a.w - b.w);
        public static Vector4 operator *(Vector4 a, float b)   => new Vector4(a.x * b, a.y * b, a.z * b, a.w * b);
        public static bool operator ==(Vector4 a, Vector4 b)   => a.x == b.x && a.y == b.y && a.z == b.z && a.w == b.w;
        public static bool operator !=(Vector4 a, Vector4 b)   => !(a == b);

        public static implicit operator Vector3(Vector4 v) => new Vector3(v.x, v.y, v.z);
        public static implicit operator Vector4(Vector3 v) => new Vector4(v.x, v.y, v.z, 0);

        public override bool Equals(object? obj) => obj is Vector4 v && this == v;
        public override int GetHashCode() => HashCode.Combine(x, y, z, w);
        public override string ToString() => $"({x:F4}, {y:F4}, {z:F4}, {w:F4})";
    }

    // -----------------------------------------------------------------------
    // Mathf – wraps System.MathF
    // -----------------------------------------------------------------------
    public static class Mathf
    {
        public const float PI        = MathF.PI;
        public const float Infinity  = float.PositiveInfinity;
        public const float Epsilon   = float.Epsilon;
        public const float Deg2Rad   = PI / 180f;
        public const float Rad2Deg   = 180f / PI;

        public static float Abs(float f)                            => MathF.Abs(f);
        public static float Acos(float f)                           => MathF.Acos(f);
        public static float Asin(float f)                           => MathF.Asin(f);
        public static float Atan(float f)                           => MathF.Atan(f);
        public static float Atan2(float y, float x)                 => MathF.Atan2(y, x);
        public static float Ceil(float f)                           => MathF.Ceiling(f);
        public static int   CeilToInt(float f)                      => (int)MathF.Ceiling(f);
        public static float Clamp(float v, float min, float max)    => Math.Clamp(v, min, max);
        public static int   Clamp(int v, int min, int max)          => Math.Clamp(v, min, max);
        public static float Clamp01(float v)                        => Math.Clamp(v, 0f, 1f);
        public static float Cos(float f)                            => MathF.Cos(f);
        public static float Exp(float f)                            => MathF.Exp(f);
        public static float Floor(float f)                          => MathF.Floor(f);
        public static int   FloorToInt(float f)                     => (int)MathF.Floor(f);
        public static float Lerp(float a, float b, float t)         => a + (b - a) * Clamp01(t);
        public static float Log(float f)                            => MathF.Log(f);
        public static float Log(float f, float p)                   => MathF.Log(f, p);
        public static float Max(float a, float b)                   => MathF.Max(a, b);
        public static float Max(float a, float b, float c)          => MathF.Max(a, MathF.Max(b, c));
        public static float Min(float a, float b)                   => MathF.Min(a, b);
        public static float Min(float a, float b, float c)          => MathF.Min(a, MathF.Min(b, c));
        public static float Pow(float f, float p)                   => MathF.Pow(f, p);
        public static float Round(float f)                          => MathF.Round(f);
        public static int   RoundToInt(float f)                     => (int)MathF.Round(f);
        public static float Sign(float f)                           => MathF.Sign(f);
        public static float Sin(float f)                            => MathF.Sin(f);
        public static float Sqrt(float f)                           => MathF.Sqrt(f);
        public static float Tan(float f)                            => MathF.Tan(f);
        public static bool  Approximately(float a, float b)         => MathF.Abs(b - a) < MathF.Max(1e-6f * MathF.Max(MathF.Abs(a), MathF.Abs(b)), Epsilon * 8);
        public static float DeltaAngle(float current, float target)
        {
            float delta = (target - current) % 360f;
            if (delta > 180f)  delta -= 360f;
            if (delta < -180f) delta += 360f;
            return delta;
        }
    }

    // -----------------------------------------------------------------------
    // Quaternion
    // -----------------------------------------------------------------------
    public struct Quaternion
    {
        public float x, y, z, w;

        public Quaternion(float x, float y, float z, float w) { this.x = x; this.y = y; this.z = z; this.w = w; }

        public static readonly Quaternion identity = new Quaternion(0, 0, 0, 1);

        public Vector3 eulerAngles
        {
            get
            {
                // Convert quaternion to Euler angles (intrinsic ZXY – Unity convention)
                float sinr_cosp = 2 * (w * x + y * z);
                float cosr_cosp = 1 - 2 * (x * x + y * y);
                float rx = MathF.Atan2(sinr_cosp, cosr_cosp);

                float sinp = 2 * (w * y - z * x);
                float ry = MathF.Abs(sinp) >= 1 ? MathF.CopySign(MathF.PI / 2, sinp) : MathF.Asin(sinp);

                float siny_cosp = 2 * (w * z + x * y);
                float cosy_cosp = 1 - 2 * (y * y + z * z);
                float rz = MathF.Atan2(siny_cosp, cosy_cosp);

                return new Vector3(rx * Mathf.Rad2Deg, ry * Mathf.Rad2Deg, rz * Mathf.Rad2Deg);
            }
        }

        public static Quaternion operator *(Quaternion a, Quaternion b) => new Quaternion(
            a.w * b.x + a.x * b.w + a.y * b.z - a.z * b.y,
            a.w * b.y - a.x * b.z + a.y * b.w + a.z * b.x,
            a.w * b.z + a.x * b.y - a.y * b.x + a.z * b.w,
            a.w * b.w - a.x * b.x - a.y * b.y - a.z * b.z
        );

        public static Vector3 operator *(Quaternion q, Vector3 v)
        {
            float tx = 2 * (q.y * v.z - q.z * v.y);
            float ty = 2 * (q.z * v.x - q.x * v.z);
            float tz = 2 * (q.x * v.y - q.y * v.x);
            return new Vector3(
                v.x + q.w * tx + q.y * tz - q.z * ty,
                v.y + q.w * ty + q.z * tx - q.x * tz,
                v.z + q.w * tz + q.x * ty - q.y * tx
            );
        }

        public static bool operator ==(Quaternion a, Quaternion b) => a.x == b.x && a.y == b.y && a.z == b.z && a.w == b.w;
        public static bool operator !=(Quaternion a, Quaternion b) => !(a == b);

        public static Quaternion LookRotation(Vector3 forward, Vector3 upwards = default)
        {
            if (upwards == default) upwards = Vector3.up;
            forward = forward.normalized;
            if (forward.magnitude < float.Epsilon) return identity;

            Vector3 right = Vector3.Cross(upwards, forward).normalized;
            if (right.magnitude < float.Epsilon) right = Vector3.right;
            Vector3 up = Vector3.Cross(forward, right);

            // Convert rotation matrix to quaternion
            float m00 = right.x, m01 = up.x, m02 = forward.x;
            float m10 = right.y, m11 = up.y, m12 = forward.y;
            float m20 = right.z, m21 = up.z, m22 = forward.z;
            float trace = m00 + m11 + m22;
            if (trace > 0)
            {
                float s = 0.5f / MathF.Sqrt(trace + 1f);
                return new Quaternion((m21 - m12) * s, (m02 - m20) * s, (m10 - m01) * s, 0.25f / s);
            }
            if (m00 > m11 && m00 > m22)
            {
                float s = 2f * MathF.Sqrt(1f + m00 - m11 - m22);
                return new Quaternion(0.25f * s, (m01 + m10) / s, (m02 + m20) / s, (m21 - m12) / s);
            }
            if (m11 > m22)
            {
                float s = 2f * MathF.Sqrt(1f + m11 - m00 - m22);
                return new Quaternion((m01 + m10) / s, 0.25f * s, (m12 + m21) / s, (m02 - m20) / s);
            }
            {
                float s = 2f * MathF.Sqrt(1f + m22 - m00 - m11);
                return new Quaternion((m02 + m20) / s, (m12 + m21) / s, 0.25f * s, (m10 - m01) / s);
            }
        }

        public static Quaternion AngleAxis(float angle, Vector3 axis)
        {
            axis = axis.normalized;
            float half = angle * Mathf.Deg2Rad * 0.5f;
            float s = MathF.Sin(half);
            return new Quaternion(axis.x * s, axis.y * s, axis.z * s, MathF.Cos(half));
        }

        // Unity also exposes AxisAngle as an alias for AngleAxis
        public static Quaternion AxisAngle(Vector3 axis, float angle) => AngleAxis(angle, axis);

        public static Quaternion Euler(float x, float y, float z)
        {
            return AngleAxis(z, Vector3.forward) * AngleAxis(x, Vector3.right) * AngleAxis(y, Vector3.up);
        }

        public static float Dot(Quaternion a, Quaternion b) => a.x * b.x + a.y * b.y + a.z * b.z + a.w * b.w;

        public static Quaternion Inverse(Quaternion q) => new Quaternion(-q.x, -q.y, -q.z, q.w);

        public static Quaternion Slerp(Quaternion a, Quaternion b, float t)
        {
            float dot = Dot(a, b);
            if (dot < 0) { b = new Quaternion(-b.x, -b.y, -b.z, -b.w); dot = -dot; }
            if (dot > 0.9995f) return Lerp(a, b, t);
            float theta0 = MathF.Acos(dot);
            float theta  = theta0 * t;
            float sinTheta  = MathF.Sin(theta);
            float sinTheta0 = MathF.Sin(theta0);
            float s0 = MathF.Cos(theta) - dot * sinTheta / sinTheta0;
            float s1 = sinTheta / sinTheta0;
            return new Quaternion(s0 * a.x + s1 * b.x, s0 * a.y + s1 * b.y, s0 * a.z + s1 * b.z, s0 * a.w + s1 * b.w);
        }

        public static Quaternion Lerp(Quaternion a, Quaternion b, float t)
        {
            t = Mathf.Clamp01(t);
            return new Quaternion(a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t, a.z + (b.z - a.z) * t, a.w + (b.w - a.w) * t);
        }

        public override bool Equals(object? obj) => obj is Quaternion q && this == q;
        public override int GetHashCode() => HashCode.Combine(x, y, z, w);
        public override string ToString() => $"({x:F4}, {y:F4}, {z:F4}, {w:F4})";
    }

    // -----------------------------------------------------------------------
    // Matrix4x4
    // -----------------------------------------------------------------------
    public struct Matrix4x4
    {
        // Column-major storage (Unity convention)
        public float m00, m10, m20, m30;
        public float m01, m11, m21, m31;
        public float m02, m12, m22, m32;
        public float m03, m13, m23, m33;

        public static readonly Matrix4x4 identity = new Matrix4x4
        {
            m00 = 1, m11 = 1, m22 = 1, m33 = 1
        };
        public static readonly Matrix4x4 zero = new Matrix4x4();

        // Indexer: matrix[row, col]
        public float this[int row, int col]
        {
            get
            {
                return col switch
                {
                    0 => row == 0 ? m00 : row == 1 ? m10 : row == 2 ? m20 : m30,
                    1 => row == 0 ? m01 : row == 1 ? m11 : row == 2 ? m21 : m31,
                    2 => row == 0 ? m02 : row == 1 ? m12 : row == 2 ? m22 : m32,
                    _ => row == 0 ? m03 : row == 1 ? m13 : row == 2 ? m23 : m33,
                };
            }
            set
            {
                switch (col)
                {
                    case 0: if (row == 0) m00 = value; else if (row == 1) m10 = value; else if (row == 2) m20 = value; else m30 = value; break;
                    case 1: if (row == 0) m01 = value; else if (row == 1) m11 = value; else if (row == 2) m21 = value; else m31 = value; break;
                    case 2: if (row == 0) m02 = value; else if (row == 1) m12 = value; else if (row == 2) m22 = value; else m32 = value; break;
                    default: if (row == 0) m03 = value; else if (row == 1) m13 = value; else if (row == 2) m23 = value; else m33 = value; break;
                }
            }
        }

        // Indexer: matrix[linearIndex] (Unity also supports linear indexing)
        public float this[int i]
        {
            get
            {
                int row = i % 4, col = i / 4;
                return this[row, col];
            }
            set
            {
                int row = i % 4, col = i / 4;
                this[row, col] = value;
            }
        }

        public Vector4 GetColumn(int index) => index switch
        {
            0 => new Vector4(m00, m10, m20, m30),
            1 => new Vector4(m01, m11, m21, m31),
            2 => new Vector4(m02, m12, m22, m32),
            _ => new Vector4(m03, m13, m23, m33),
        };

        public void SetColumn(int index, Vector4 v)
        {
            switch (index)
            {
                case 0: m00 = v.x; m10 = v.y; m20 = v.z; m30 = v.w; break;
                case 1: m01 = v.x; m11 = v.y; m21 = v.z; m31 = v.w; break;
                case 2: m02 = v.x; m12 = v.y; m22 = v.z; m32 = v.w; break;
                default: m03 = v.x; m13 = v.y; m23 = v.z; m33 = v.w; break;
            }
        }

        public Vector4 GetRow(int index) => index switch
        {
            0 => new Vector4(m00, m01, m02, m03),
            1 => new Vector4(m10, m11, m12, m13),
            2 => new Vector4(m20, m21, m22, m23),
            _ => new Vector4(m30, m31, m32, m33),
        };

        public void SetRow(int index, Vector4 v)
        {
            switch (index)
            {
                case 0: m00 = v.x; m01 = v.y; m02 = v.z; m03 = v.w; break;
                case 1: m10 = v.x; m11 = v.y; m12 = v.z; m13 = v.w; break;
                case 2: m20 = v.x; m21 = v.y; m22 = v.z; m23 = v.w; break;
                default: m30 = v.x; m31 = v.y; m32 = v.z; m33 = v.w; break;
            }
        }

        public Vector3 MultiplyPoint3x4(Vector3 v) => new Vector3(
            m00 * v.x + m01 * v.y + m02 * v.z + m03,
            m10 * v.x + m11 * v.y + m12 * v.z + m13,
            m20 * v.x + m21 * v.y + m22 * v.z + m23
        );

        // Full 4x4 multiply (divides by w, handles perspective)
        public Vector3 MultiplyPoint(Vector3 v)
        {
            float w = m30 * v.x + m31 * v.y + m32 * v.z + m33;
            float rw = w != 0 ? 1f / w : 1f;
            return new Vector3(
                (m00 * v.x + m01 * v.y + m02 * v.z + m03) * rw,
                (m10 * v.x + m11 * v.y + m12 * v.z + m13) * rw,
                (m20 * v.x + m21 * v.y + m22 * v.z + m23) * rw
            );
        }

        public Vector3 MultiplyVector(Vector3 v) => new Vector3(
            m00 * v.x + m01 * v.y + m02 * v.z,
            m10 * v.x + m11 * v.y + m12 * v.z,
            m20 * v.x + m21 * v.y + m22 * v.z
        );

        public static Matrix4x4 operator *(Matrix4x4 a, Matrix4x4 b)
        {
            var r = new Matrix4x4();
            for (int row = 0; row < 4; row++)
                for (int col = 0; col < 4; col++)
                {
                    float sum = 0;
                    for (int k = 0; k < 4; k++) sum += a[row, k] * b[k, col];
                    r[row, col] = sum;
                }
            return r;
        }

        public static Matrix4x4 Translate(Vector3 v)
        {
            var m = identity;
            m.m03 = v.x; m.m13 = v.y; m.m23 = v.z;
            return m;
        }

        public static Matrix4x4 Scale(Vector3 v)
        {
            var m = identity;
            m.m00 = v.x; m.m11 = v.y; m.m22 = v.z;
            return m;
        }

        public static Matrix4x4 TRS(Vector3 pos, Quaternion rot, Vector3 scale)
        {
            return Translate(pos) * Rotate(rot) * Scale(scale);
        }

        public static Matrix4x4 Rotate(Quaternion q)
        {
            float x2 = q.x * 2, y2 = q.y * 2, z2 = q.z * 2;
            float xx = q.x * x2, yy = q.y * y2, zz = q.z * z2;
            float xy = q.x * y2, xz = q.x * z2, yz = q.y * z2;
            float wx = q.w * x2, wy = q.w * y2, wz = q.w * z2;
            var m = new Matrix4x4();
            m.m00 = 1 - (yy + zz); m.m01 = xy - wz;       m.m02 = xz + wy;       m.m03 = 0;
            m.m10 = xy + wz;       m.m11 = 1 - (xx + zz); m.m12 = yz - wx;       m.m13 = 0;
            m.m20 = xz - wy;       m.m21 = yz + wx;       m.m22 = 1 - (xx + yy); m.m23 = 0;
            m.m30 = 0;             m.m31 = 0;             m.m32 = 0;             m.m33 = 1;
            return m;
        }

        public Matrix4x4 inverse
        {
            get
            {
                // Gauss-Jordan inversion for 4x4
                var src = this;
                var inv = identity;
                for (int col = 0; col < 4; col++)
                {
                    int pivot = col;
                    for (int row = col + 1; row < 4; row++)
                        if (MathF.Abs(src[row, col]) > MathF.Abs(src[pivot, col])) pivot = row;
                    if (pivot != col)
                    {
                        for (int k = 0; k < 4; k++) { float t = src[col, k]; src[col, k] = src[pivot, k]; src[pivot, k] = t; }
                        for (int k = 0; k < 4; k++) { float t = inv[col, k]; inv[col, k] = inv[pivot, k]; inv[pivot, k] = t; }
                    }
                    float diag = src[col, col];
                    if (MathF.Abs(diag) < float.Epsilon) return zero;
                    for (int k = 0; k < 4; k++) { src[col, k] /= diag; inv[col, k] /= diag; }
                    for (int row = 0; row < 4; row++)
                    {
                        if (row == col) continue;
                        float factor = src[row, col];
                        for (int k = 0; k < 4; k++) { src[row, k] -= factor * src[col, k]; inv[row, k] -= factor * inv[col, k]; }
                    }
                }
                return inv;
            }
        }

        public Matrix4x4 transpose
        {
            get
            {
                var r = new Matrix4x4();
                for (int i = 0; i < 4; i++) for (int j = 0; j < 4; j++) r[i, j] = this[j, i];
                return r;
            }
        }

        public override string ToString() => $"[{m00:F3} {m01:F3} {m02:F3} {m03:F3}] [{m10:F3} {m11:F3} {m12:F3} {m13:F3}] [{m20:F3} {m21:F3} {m22:F3} {m23:F3}] [{m30:F3} {m31:F3} {m32:F3} {m33:F3}]";
    }

    // -----------------------------------------------------------------------
    // Plane
    // -----------------------------------------------------------------------
    public struct Plane
    {
        public Vector3 normal;
        public float distance;

        public Plane(Vector3 inNormal, Vector3 inPoint)
        {
            normal = inNormal.normalized;
            distance = -Vector3.Dot(normal, inPoint);
        }

        public Plane(Vector3 inNormal, float d)
        {
            normal = inNormal.normalized;
            distance = d;
        }

        public float GetDistanceToPoint(Vector3 point) => Vector3.Dot(normal, point) + distance;
        public bool GetSide(Vector3 point) => GetDistanceToPoint(point) > 0;

        public bool Raycast(Ray ray, out float enter)
        {
            float denom = Vector3.Dot(ray.direction, normal);
            if (MathF.Abs(denom) < float.Epsilon) { enter = 0; return false; }
            enter = -(Vector3.Dot(ray.origin, normal) + distance) / denom;
            return enter > 0;
        }
    }

    // -----------------------------------------------------------------------
    // Ray
    // -----------------------------------------------------------------------
    public struct Ray
    {
        public Vector3 origin;
        public Vector3 direction;

        public Ray(Vector3 origin, Vector3 direction) { this.origin = origin; this.direction = direction.normalized; }

        public Vector3 GetPoint(float distance) => origin + direction * distance;
    }

    // -----------------------------------------------------------------------
    // Color
    // -----------------------------------------------------------------------
    public struct Color
    {
        public float r, g, b, a;

        public Color(float r, float g, float b, float a = 1f) { this.r = r; this.g = g; this.b = b; this.a = a; }

        public static readonly Color white   = new Color(1, 1, 1);
        public static readonly Color black   = new Color(0, 0, 0);
        public static readonly Color red     = new Color(1, 0, 0);
        public static readonly Color green   = new Color(0, 1, 0);
        public static readonly Color blue    = new Color(0, 0, 1);
        public static readonly Color yellow  = new Color(1, 1, 0);
        public static readonly Color cyan    = new Color(0, 1, 1);
        public static readonly Color magenta = new Color(1, 0, 1);
        public static readonly Color grey    = new Color(0.5f, 0.5f, 0.5f);
        public static readonly Color clear   = new Color(0, 0, 0, 0);

        public static Color Lerp(Color a, Color b, float t) => new Color(
            Mathf.Lerp(a.r, b.r, t), Mathf.Lerp(a.g, b.g, t),
            Mathf.Lerp(a.b, b.b, t), Mathf.Lerp(a.a, b.a, t));

        public static bool operator ==(Color a, Color b) => a.r == b.r && a.g == b.g && a.b == b.b && a.a == b.a;
        public static bool operator !=(Color a, Color b) => !(a == b);
        public static Color operator *(Color a, float f) => new Color(a.r * f, a.g * f, a.b * f, a.a * f);
        public static Color operator +(Color a, Color b) => new Color(a.r + b.r, a.g + b.g, a.b + b.b, a.a + b.a);

        public override bool Equals(object? obj) => obj is Color c && this == c;
        public override int GetHashCode() => HashCode.Combine(r, g, b, a);
        public override string ToString() => $"RGBA({r:F3}, {g:F3}, {b:F3}, {a:F3})";
    }

    // -----------------------------------------------------------------------
    // Rect
    // -----------------------------------------------------------------------
    public struct Rect
    {
        public float x, y, width, height;

        public Rect(float x, float y, float width, float height) { this.x = x; this.y = y; this.width = width; this.height = height; }

        public float xMin => x;
        public float yMin => y;
        public float xMax => x + width;
        public float yMax => y + height;
        public Vector2 center => new Vector2(x + width * 0.5f, y + height * 0.5f);
        public Vector2 size => new Vector2(width, height);

        public bool Contains(Vector2 point) => point.x >= xMin && point.x <= xMax && point.y >= yMin && point.y <= yMax;
        public bool Contains(Vector3 point) => Contains(new Vector2(point.x, point.y));
        public bool Overlaps(Rect other) => other.xMin < xMax && other.xMax > xMin && other.yMin < yMax && other.yMax > yMin;

        public static Rect MinMaxRect(float xmin, float ymin, float xmax, float ymax) =>
            new Rect(xmin, ymin, xmax - xmin, ymax - ymin);
    }

    // -----------------------------------------------------------------------
    // Bounds
    // -----------------------------------------------------------------------
    public struct Bounds
    {
        public Vector3 center;
        public Vector3 size;

        public Bounds(Vector3 center, Vector3 size) { this.center = center; this.size = size; }

        public Vector3 min { get => center - size * 0.5f; set => size = (value - center) * -2; }
        public Vector3 max { get => center + size * 0.5f; set => size = (value - center) * 2; }

        // Unity: extents = size / 2; setting extents sets size = extents * 2
        public Vector3 extents
        {
            get => size * 0.5f;
            set => size = value * 2;
        }

        public bool Contains(Vector3 point)
        {
            var mn = min; var mx = max;
            return point.x >= mn.x && point.x <= mx.x &&
                   point.y >= mn.y && point.y <= mx.y &&
                   point.z >= mn.z && point.z <= mx.z;
        }
    }

    // -----------------------------------------------------------------------
    // Debug – logs to console
    // -----------------------------------------------------------------------
    public static class Debug
    {
        public static void Log(object? message)         => Console.WriteLine(message);
        public static void LogWarning(object? message)  => Console.Error.WriteLine($"[Warning] {message}");
        public static void LogError(object? message)    => Console.Error.WriteLine($"[Error] {message}");
        public static void Assert(bool condition, string message = "") { if (!condition) LogError("Assertion failed: " + message); }
    }

    // -----------------------------------------------------------------------
    // MonoBehaviour – empty base class (rendering layer only)
    // -----------------------------------------------------------------------
    public class MonoBehaviour { }
    public class ScriptableObject { }

    // -----------------------------------------------------------------------
    // GameObject – minimal stub
    // -----------------------------------------------------------------------
    public class GameObject
    {
        public string name { get; set; }
        public Transform transform { get; } = new Transform();
        public bool activeSelf { get; private set; } = true;

        public GameObject(string name = "GameObject") { this.name = name; }

        public T GetComponent<T>() where T : class => null!;
        public T AddComponent<T>() where T : class => null!;
        public void SetActive(bool value) { activeSelf = value; }

        public static GameObject? Find(string name) => null;
        public static T? FindObjectOfType<T>() where T : class => null;
    }

    // -----------------------------------------------------------------------
    // Transform – minimal stub
    // -----------------------------------------------------------------------
    public class Transform
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 localScale = Vector3.one;

        public Vector3 forward => rotation * Vector3.forward;
        public Vector3 right   => rotation * Vector3.right;
        public Vector3 up      => rotation * Vector3.up;
    }

    // -----------------------------------------------------------------------
    // Camera – minimal stub
    // -----------------------------------------------------------------------
    public class Camera
    {
        private static Camera? _main;
        public static Camera main => _main ??= new Camera();

        public Transform transform { get; } = new Transform();
        public int pixelWidth  { get; set; } = 1920;
        public int pixelHeight { get; set; } = 1080;

        public Ray ScreenPointToRay(Vector3 screenPoint) => new Ray(transform.position, transform.forward);
        public Ray ViewportPointToRay(Vector3 viewportPoint) => new Ray(transform.position, transform.forward);
        public Vector3 WorldToScreenPoint(Vector3 world) => new Vector3(world.x, world.y, 0);
    }

    // -----------------------------------------------------------------------
    // Screen – minimal stub
    // -----------------------------------------------------------------------
    public static class Screen
    {
        public static int   width  { get; set; } = 1920;
        public static int   height { get; set; } = 1080;
        public static float dpi    { get; set; } = 96f;
        public static bool  fullScreen { get; set; } = false;
    }

    // -----------------------------------------------------------------------
    // Input – minimal stub (returns safe defaults for non-interactive use)
    // -----------------------------------------------------------------------
    public static class Input
    {
        public static Vector3 mousePosition => Vector3.zero;
        public static bool GetMouseButton(int button)     => false;
        public static bool GetMouseButtonDown(int button) => false;
        public static bool GetMouseButtonUp(int button)   => false;
        public static bool GetKey(KeyCode key)            => false;
        public static bool GetKeyDown(KeyCode key)        => false;
        public static bool GetKeyUp(KeyCode key)          => false;
    }

    // -----------------------------------------------------------------------
    // KeyCode enum (subset)
    // -----------------------------------------------------------------------
    public enum KeyCode
    {
        None = 0, Escape = 27, Return = 13, Space = 32,
        A = 97, B, C, D, E, F, G, H, I, J, K, L, M,
        N, O, P, Q, R, S, T, U, V, W, X, Y, Z,
        Alpha0 = 48, Alpha1, Alpha2, Alpha3, Alpha4, Alpha5, Alpha6, Alpha7, Alpha8, Alpha9,
        Delete = 127, Backspace = 8, Tab = 9,
        LeftShift = 304, RightShift = 303, LeftControl = 306, LeftAlt = 308,
        F1 = 282, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12,
    }

    // -----------------------------------------------------------------------
    // GeometryUtility – stub
    // -----------------------------------------------------------------------
    public static class GeometryUtility
    {
        public static Bounds CalculateBounds(Vector3[] points, Matrix4x4 transform)
        {
            if (points == null || points.Length == 0) return new Bounds(Vector3.zero, Vector3.zero);
            var p0 = transform.MultiplyPoint3x4(points[0]);
            var mn = p0; var mx = p0;
            foreach (var pt in points)
            {
                var tp = transform.MultiplyPoint3x4(pt);
                mn.x = Mathf.Min(mn.x, tp.x); mn.y = Mathf.Min(mn.y, tp.y); mn.z = Mathf.Min(mn.z, tp.z);
                mx.x = Mathf.Max(mx.x, tp.x); mx.y = Mathf.Max(mx.y, tp.y); mx.z = Mathf.Max(mx.z, tp.z);
            }
            return new Bounds((mn + mx) * 0.5f, mx - mn);
        }
    }

    // -----------------------------------------------------------------------
    // Gizmos – stub
    // -----------------------------------------------------------------------
    public static class Gizmos
    {
        public static Color color { get; set; }
        public static void DrawLine(Vector3 from, Vector3 to) { }
        public static void DrawSphere(Vector3 center, float radius) { }
        public static void DrawWireSphere(Vector3 center, float radius) { }
        public static void DrawCube(Vector3 center, Vector3 size) { }
    }

    // -----------------------------------------------------------------------
    // TextAsset – stub
    // -----------------------------------------------------------------------
    public class TextAsset
    {
        public string text { get; set; } = "";
        public byte[] bytes { get; set; } = Array.Empty<byte>();
        public TextAsset(string text = "") { this.text = text; }
    }

    // -----------------------------------------------------------------------
    // Material – stub
    // -----------------------------------------------------------------------
    public class Material
    {
        public string name { get; set; } = "";
        public Color color { get; set; } = Color.white;
        public Material(string name = "") { this.name = name; }
    }

    // -----------------------------------------------------------------------
    // Texture2D – stub
    // -----------------------------------------------------------------------
    public class Texture2D
    {
        public int width { get; }
        public int height { get; }
        public Texture2D(int width, int height) { this.width = width; this.height = height; }
    }

    // -----------------------------------------------------------------------
    // Font – stub
    // -----------------------------------------------------------------------
    public class Font
    {
        public string name { get; set; } = "";
    }

    // -----------------------------------------------------------------------
    // Unity attributes – no-ops in the open-source build
    // -----------------------------------------------------------------------
    [System.AttributeUsage(System.AttributeTargets.Field | System.AttributeTargets.Property)]
    public sealed class SerializeFieldAttribute : System.Attribute { }

    [System.AttributeUsage(System.AttributeTargets.Field | System.AttributeTargets.Property)]
    public sealed class HideInInspectorAttribute : System.Attribute { }

    [System.AttributeUsage(System.AttributeTargets.Field | System.AttributeTargets.Property)]
    public sealed class ShowInInspectorAttribute : System.Attribute { }

    [System.AttributeUsage(System.AttributeTargets.Field)]
    public sealed class TooltipAttribute : System.Attribute
    {
        public TooltipAttribute(string tooltip) { }
    }

    [System.AttributeUsage(System.AttributeTargets.Field)]
    public sealed class RangeAttribute : System.Attribute
    {
        public RangeAttribute(float min, float max) { }
    }

    [System.AttributeUsage(System.AttributeTargets.Field | System.AttributeTargets.Class)]
    public sealed class HeaderAttribute : System.Attribute
    {
        public HeaderAttribute(string header) { }
    }

    [System.AttributeUsage(System.AttributeTargets.Class)]
    public sealed class RequireComponentAttribute : System.Attribute
    {
        public RequireComponentAttribute(System.Type type) { }
    }

    [System.AttributeUsage(System.AttributeTargets.Class)]
    public sealed class DisallowMultipleComponentAttribute : System.Attribute { }

    [System.AttributeUsage(System.AttributeTargets.Class)]
    public sealed class CreateAssetMenuAttribute : System.Attribute
    {
        public string? menuName { get; set; }
        public string? fileName { get; set; }
    }
}

// ---------------------------------------------------------------------------
// UnityEngine.UI namespace – minimal stubs for compiled references
// ---------------------------------------------------------------------------
namespace UnityEngine.UI
{
    public class Text           : MonoBehaviour { public string text { get; set; } = ""; }
    public class InputField     : MonoBehaviour { public string text { get; set; } = ""; }
    public class Button         : MonoBehaviour { }
    public class Image          : MonoBehaviour { public UnityEngine.Color color { get; set; } }
    public class Slider         : MonoBehaviour { public float value { get; set; } }
    public class Toggle         : MonoBehaviour { public bool isOn  { get; set; } }
    public class Dropdown       : MonoBehaviour { public int value  { get; set; } }
    public class ScrollRect     : MonoBehaviour { }
    public class LayoutElement  : MonoBehaviour { }
    public class ContentSizeFitter : MonoBehaviour { }
    public class RectTransform  : MonoBehaviour
    {
        public UnityEngine.Vector2 anchoredPosition { get; set; }
        public UnityEngine.Vector2 sizeDelta        { get; set; }
        public UnityEngine.Vector3 localScale        { get; set; } = UnityEngine.Vector3.one;
    }
}

// ---------------------------------------------------------------------------
// UnityEngine.UIElements namespace – minimal stubs
// ---------------------------------------------------------------------------
namespace UnityEngine.UIElements
{
    public class VisualElement { }
}

// ---------------------------------------------------------------------------
// UnityEngine.Profiling – performance profiler stub
// ---------------------------------------------------------------------------
namespace UnityEngine.Profiling
{
    public static class Profiler
    {
        public static void BeginSample(string name) { }
        public static void EndSample() { }
    }
}

// ---------------------------------------------------------------------------
// UnityEngine runtime init (global namespace attributes and enums)
// ---------------------------------------------------------------------------
public enum RuntimeInitializeLoadType
{
    AfterSceneLoad,
    BeforeSceneLoad,
    AfterAssembliesLoaded,
    BeforeSplashScreen,
    SubsystemRegistration
}

[System.AttributeUsage(System.AttributeTargets.Method)]
public sealed class RuntimeInitializeOnLoadMethodAttribute : System.Attribute
{
    public RuntimeInitializeOnLoadMethodAttribute() { }
    public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType loadType) { }
}
