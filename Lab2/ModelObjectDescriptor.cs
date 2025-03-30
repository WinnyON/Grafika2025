using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Vulkan;
using System;
using System.Collections.Generic;
using System.Linq;
//using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace GrafikaSzeminarium
{
    internal class ModelObjectDescriptor:IDisposable
    {
        private bool disposedValue;

        public uint Vao { get; private set; }
        public uint Vertices { get; private set; }
        public uint Colors { get; private set; }
        public uint Indices { get; private set; }
        public uint IndexArrayLength { get; private set; }
        public bool initialDraw = true;
        public int Xindex;
        public int Yindex;
        public int Zindex;

        public Matrix4X4<float> PageRotationMatrix;
        public Matrix4X4<float> TempRotationMatrix;
        public Matrix4X4<float> InitialPositionMatrix;

        //public float rotationProgress = 1.0f;
        private GL Gl;

        private static float[] colorArray;

        public static void setColorArray(int[] sides)
        {
            colorArray = new float[] {
                1.0f, 0.0f, 0.0f, 1.0f, // felso oldal
                1.0f, 0.0f, 0.0f, 1.0f,
                1.0f, 0.0f, 0.0f, 1.0f,
                1.0f, 0.0f, 0.0f, 1.0f,

                0.0f, 1.0f, 0.0f, 1.0f, // szembe elso oldal
                0.0f, 1.0f, 0.0f, 1.0f,
                0.0f, 1.0f, 0.0f, 1.0f,
                0.0f, 1.0f, 0.0f, 1.0f,

                0.0f, 0.0f, 1.0f, 1.0f, // bal oldal
                0.0f, 0.0f, 1.0f, 1.0f,
                0.0f, 0.0f, 1.0f, 1.0f,
                0.0f, 0.0f, 1.0f, 1.0f,

                1.0f, 0.0f, 1.0f, 1.0f, // also oldal
                1.0f, 0.0f, 1.0f, 1.0f,
                1.0f, 0.0f, 1.0f, 1.0f,
                1.0f, 0.0f, 1.0f, 1.0f,

                0.0f, 1.0f, 1.0f, 1.0f, // hatso oldal
                0.0f, 1.0f, 1.0f, 1.0f,
                0.0f, 1.0f, 1.0f, 1.0f,
                0.0f, 1.0f, 1.0f, 1.0f,

                1.0f, 1.0f, 0.0f, 1.0f, // jobb oldal
                1.0f, 1.0f, 0.0f, 1.0f,
                1.0f, 1.0f, 0.0f, 1.0f,
                1.0f, 1.0f, 0.0f, 1.0f,
            };

            for (int i = 0; i < 6; i++)
            {
                if (sides[i] == 1)
                {
                    for(int j = i * 16; j < i * 16 + 16; j++)
                    {
                        colorArray[j] = 0.0f;
                        if((j + 1) % 4 == 0)
                        {
                            colorArray[j] = 1.0f;
                        }
                    }
                }
            }
        }

        public unsafe static ModelObjectDescriptor CreateCube(GL Gl)
        {
            uint vao = Gl.GenVertexArray();
            Gl.BindVertexArray(vao);

            // counter clockwise is front facing
            var vertexArray = new float[] {
                -0.5f, 0.5f, 0.5f, // felso oldal
                0.5f, 0.5f, 0.5f,
                0.5f, 0.5f, -0.5f,
                -0.5f, 0.5f, -0.5f,

                -0.5f, 0.5f, 0.5f, // szembe elso oldal
                -0.5f, -0.5f, 0.5f,
                0.5f, -0.5f, 0.5f,
                0.5f, 0.5f, 0.5f,

                -0.5f, 0.5f, 0.5f, // bal oldal
                -0.5f, 0.5f, -0.5f,
                -0.5f, -0.5f, -0.5f,
                -0.5f, -0.5f, 0.5f,

                -0.5f, -0.5f, 0.5f, // also oldal
                0.5f, -0.5f, 0.5f,
                0.5f, -0.5f, -0.5f,
                -0.5f, -0.5f, -0.5f,

                0.5f, 0.5f, -0.5f, // hatso oldal
                -0.5f, 0.5f, -0.5f,
                -0.5f, -0.5f, -0.5f,
                0.5f, -0.5f, -0.5f,

                0.5f, 0.5f, 0.5f, // jobb oldal
                0.5f, 0.5f, -0.5f,
                0.5f, -0.5f, -0.5f,
                0.5f, -0.5f, 0.5f,

            };

            //colorArray = new float[] {
            //    1.0f, 0.0f, 0.0f, 1.0f, // felso oldal
            //    1.0f, 0.0f, 0.0f, 1.0f,
            //    1.0f, 0.0f, 0.0f, 1.0f,
            //    1.0f, 0.0f, 0.0f, 1.0f,

            //    0.0f, 1.0f, 0.0f, 1.0f, // szembe elso oldal
            //    0.0f, 1.0f, 0.0f, 1.0f,
            //    0.0f, 1.0f, 0.0f, 1.0f,
            //    0.0f, 1.0f, 0.0f, 1.0f,

            //    0.0f, 0.0f, 1.0f, 1.0f, // bal oldal
            //    0.0f, 0.0f, 1.0f, 1.0f,
            //    0.0f, 0.0f, 1.0f, 1.0f,
            //    0.0f, 0.0f, 1.0f, 1.0f,

            //    1.0f, 0.0f, 1.0f, 1.0f, // also oldal
            //    1.0f, 0.0f, 1.0f, 1.0f,
            //    1.0f, 0.0f, 1.0f, 1.0f,
            //    1.0f, 0.0f, 1.0f, 1.0f,

            //    0.0f, 1.0f, 1.0f, 1.0f, // hatso oldal
            //    0.0f, 1.0f, 1.0f, 1.0f,
            //    0.0f, 1.0f, 1.0f, 1.0f,
            //    0.0f, 1.0f, 1.0f, 1.0f,

            //    1.0f, 1.0f, 0.0f, 1.0f, // jobb oldal
            //    1.0f, 1.0f, 0.0f, 1.0f,
            //    1.0f, 1.0f, 0.0f, 1.0f,
            //    1.0f, 1.0f, 0.0f, 1.0f,
            //};

            uint[] indexArray = new uint[] {
                0, 1, 2,
                0, 2, 3,

                4, 5, 6,
                4, 6, 7,

                8, 9, 10,
                10, 11, 8,

                12, 14, 13,
                12, 15, 14,

                17, 16, 19,
                17, 19, 18,

                20, 22, 21,
                20, 23, 22
            };

            uint vertices = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ArrayBuffer, vertices);
            Gl.BufferData(GLEnum.ArrayBuffer, (ReadOnlySpan<float>)vertexArray.AsSpan(), GLEnum.StaticDraw);
            Gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 0, null);
            Gl.EnableVertexAttribArray(0);
            Gl.BindBuffer(GLEnum.ArrayBuffer, 0);

            uint colors = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ArrayBuffer, colors);
            Gl.BufferData(GLEnum.ArrayBuffer, (ReadOnlySpan<float>)colorArray.AsSpan(), GLEnum.StaticDraw);
            Gl.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, 0, null);
            Gl.EnableVertexAttribArray(1);
            Gl.BindBuffer(GLEnum.ArrayBuffer, 0);

            uint indices = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ElementArrayBuffer, indices);
            Gl.BufferData(GLEnum.ElementArrayBuffer, (ReadOnlySpan<uint>)indexArray.AsSpan(), GLEnum.StaticDraw);
            Gl.BindBuffer(GLEnum.ElementArrayBuffer, 0);

            return new ModelObjectDescriptor() {Vao= vao, Vertices = vertices, Colors = colors, Indices = indices, IndexArrayLength = (uint)indexArray.Length, Gl = Gl};

        }



        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null


                // always unbound the vertex buffer first, so no halfway results are displayed by accident
                Gl.DeleteBuffer(Vertices);
                Gl.DeleteBuffer(Colors);
                Gl.DeleteBuffer(Indices);
                Gl.DeleteVertexArray(Vao);

                disposedValue = true;
            }
        }

        ~ModelObjectDescriptor()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
