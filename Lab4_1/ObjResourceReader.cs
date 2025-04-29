using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.SDL;
using System.Globalization;

namespace Szeminarium1_24_02_17_2
{
    internal class ObjResourceReader
    {
        private static bool normalFound = false;
        public static unsafe GlObject CreateTeapotWithColor(GL Gl, float[] faceColor)
        {
            uint vao = Gl.GenVertexArray();
            Gl.BindVertexArray(vao);

            List<float[]> objVertices;
            List<int[]> objFaces;
            List<float[]> objNormals;
            List<int[]> objFaceNormals;

            ReadObjDataForTeapot(out objVertices, out objFaces, out objNormals, out objFaceNormals);

            List<float> glVertices = new List<float>();
            List<float> glColors = new List<float>();
            List<uint> glIndices = new List<uint>();

            CreateGlArraysFromObjArrays(faceColor, objVertices, objNormals, objFaces, objFaceNormals, glVertices, glColors, glIndices);

            return CreateOpenGlObject(Gl, vao, glVertices, glColors, glIndices);
        }

        private static unsafe GlObject CreateOpenGlObject(GL Gl, uint vao, List<float> glVertices, List<float> glColors, List<uint> glIndices)
        {
            uint offsetPos = 0;
            uint offsetNormal = offsetPos + (3 * sizeof(float));
            uint vertexSize = offsetNormal + (3 * sizeof(float));

            uint vertices = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ArrayBuffer, vertices);
            Gl.BufferData(GLEnum.ArrayBuffer, (ReadOnlySpan<float>)glVertices.ToArray().AsSpan(), GLEnum.StaticDraw);
            Gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, vertexSize, (void*)offsetPos);
            Gl.EnableVertexAttribArray(0);

            Gl.EnableVertexAttribArray(2);
            Gl.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, vertexSize, (void*)offsetNormal);

            uint colors = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ArrayBuffer, colors);
            Gl.BufferData(GLEnum.ArrayBuffer, (ReadOnlySpan<float>)glColors.ToArray().AsSpan(), GLEnum.StaticDraw);
            Gl.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, 0, null);
            Gl.EnableVertexAttribArray(1);

            uint indices = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ElementArrayBuffer, indices);
            Gl.BufferData(GLEnum.ElementArrayBuffer, (ReadOnlySpan<uint>)glIndices.ToArray().AsSpan(), GLEnum.StaticDraw);

            // release array buffer
            Gl.BindBuffer(GLEnum.ArrayBuffer, 0);
            uint indexArrayLength = (uint)glIndices.Count;

            return new GlObject(vao, vertices, colors, indices, indexArrayLength, Gl);
        }

        private static unsafe void CreateGlArraysFromObjArrays(float[] faceColor, List<float[]> objVertices, List<float[]> objNormals, List<int[]> objFaces, List<int[]> objFaceNormals, List<float> glVertices, List<float> glColors, List<uint> glIndices)
        {
            Dictionary<string, int> glVertexIndices = new Dictionary<string, int>();

            //foreach (var objFace in objFaces)
            for(int j = 0; j < objFaces.Count; j++)
            {
                var objFace = objFaces[j];
                var aObjVertex = objVertices[objFace[0] - 1];
                var a = new Vector3D<float>(aObjVertex[0], aObjVertex[1], aObjVertex[2]);
                var bObjVertex = objVertices[objFace[1] - 1];
                var b = new Vector3D<float>(bObjVertex[0], bObjVertex[1], bObjVertex[2]);
                var cObjVertex = objVertices[objFace[2] - 1];
                var c = new Vector3D<float>(cObjVertex[0], cObjVertex[1], cObjVertex[2]);

                var normal = Vector3D.Normalize(Vector3D.Cross(b - a, c - a));
                if (normalFound)
                {
                    //normal = new Vector3D<float>(objNormals[objFace[0] - 1][0], objNormals[objFace[0] - 1][1], objNormals[objFace[0] - 1][2]);
                    var objFaceNormal = objFaceNormals[j];
                    normal = new Vector3D<float>(objNormals[objFaceNormal[0] - 1][0], objNormals[objFaceNormal[1] - 1][1], objNormals[objFaceNormal[2] - 1][2]);
                }
                

                // process 3 vertices
                for (int i = 0; i < objFace.Length; ++i)
                {
                    var objVertex = objVertices[objFace[i] - 1];

                    // create gl description of vertex
                    List<float> glVertex = new List<float>();
                    glVertex.AddRange(objVertex);
                    glVertex.Add(normal.X);
                    glVertex.Add(normal.Y);
                    glVertex.Add(normal.Z);
                    // add textrure, color

                    // check if vertex exists
                    var glVertexStringKey = string.Join(" ", glVertex);
                    if (!glVertexIndices.ContainsKey(glVertexStringKey))
                    {
                        glVertices.AddRange(glVertex);
                        glColors.AddRange(faceColor);
                        glVertexIndices.Add(glVertexStringKey, glVertexIndices.Count);
                    }

                    // add vertex to triangle indices
                    glIndices.Add((uint)glVertexIndices[glVertexStringKey]);
                }
            }
            normalFound = false;
        }

        static string ReplaceMultipleSpacesWithSingle(string input)
        {
            return string.Join(" ", input.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
        }

        private static unsafe void ReadObjDataForTeapot(out List<float[]> objVertices, out List<int[]> objFaces, out List<float[]> objNormals, out List<int[]> objFaceNormals)
        {
            objVertices = new List<float[]>();
            objFaces = new List<int[]>();
            objNormals = new List<float[]>();
            objFaceNormals = new List<int[]>();
            using (Stream objStream = typeof(ObjResourceReader).Assembly.GetManifestResourceStream("Szeminarium1_24_02_17_2.Resources.ember.obj"))
            using (StreamReader objReader = new StreamReader(objStream))
            {
                while (!objReader.EndOfStream)
                {
                    var line = objReader.ReadLine();

                    if (String.IsNullOrEmpty(line) || line.Trim().StartsWith("#"))
                        continue;

                    //line = ReplaceMultipleSpacesWithSingle(line);
                    line = line.Replace("  ", " ");
                    Console.WriteLine(line);

                    var lineClassifier = line.Substring(0, line.IndexOf(' '));
                    var lineData = line.Substring(lineClassifier.Length).Trim().Split(' ');

                    switch (lineClassifier)
                    {
                        case "v":
                            float[] vertex = new float[3];
                            for (int i = 0; i < vertex.Length; ++i)
                            {
                                vertex[i] = float.Parse(lineData[i], CultureInfo.InvariantCulture);
                            }
                            objVertices.Add(vertex);
                            break;
                        case "vn":
                            normalFound = true;
                            float[] normal = new float[3];
                            for (int i = 0; i < normal.Length; ++i)
                                normal[i] = float.Parse(lineData[i], CultureInfo.InvariantCulture);
                            objNormals.Add(normal);
                            break;
                        case "f":
                            int[] face = new int[3];
                            int[] indexes = new int[3];
                            for (int i = 0; i < face.Length; ++i)
                            {
                                var vertexData = lineData[i].Split("/");
                                //face[i] = int.Parse(lineData[i]);
                                face[i] = int.Parse(vertexData[0]);
                                if (normalFound)
                                {
                                    indexes[i] = int.Parse(vertexData[2]);
                                }
                            }
                            objFaces.Add(face);
                            if (normalFound)
                            {
                                objFaceNormals.Add(indexes);
                            }
                            break;
                    }
                }
            }
        }
    }
}
