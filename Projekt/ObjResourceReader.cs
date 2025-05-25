using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.SDL;
using System.Globalization;
using StbImageSharp;
using System.ComponentModel;

namespace Projekt
{
    internal class ObjResourceReader
    {
        private static bool normalFound = false;
        public static int White = 0;
        public static int Red = 1;
        public static int Gray = 2;
        public static int Green = 3;
        private static readonly List<float[]> faceColors = new List<float[]> {
            new float[] { 1.0f, 1.0f, 1.0f, 1.0f },
            new float[] { 0.82f, 0.027f, 0.055f, 1.0f },
            new float[]{ 0.612f, 0.612f, 0.631f, 1f },
            new float[]{ 0.384f, 0.961f, 0.212f } };
        public static unsafe GlObject CreateObjectWithColor(GL Gl, float[] faceColor, string resource)
        {
            uint vao = Gl.GenVertexArray();
            Gl.BindVertexArray(vao);

            List<float[]> objVertices;
            List<int[]> objFaces;
            List<float[]> objNormals;
            List<int[]> objFaceNormals;

            ReadObjData(out objVertices, out objFaces, out objNormals, out objFaceNormals, resource);

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



        // without texture
        private static unsafe void ReadObjData(out List<float[]> objVertices, out List<int[]> objFaces, out List<float[]> objNormals, out List<int[]> objFaceNormals, string resource)
        {
            objVertices = new List<float[]>();
            objFaces = new List<int[]>();
            objNormals = new List<float[]>();
            objFaceNormals = new List<int[]>();
            string fullResourceName = "Projekt.Resources." + resource;
            using (Stream objStream = typeof(ObjResourceReader).Assembly.GetManifestResourceStream(fullResourceName))
            using (StreamReader objReader = new StreamReader(objStream))
            {
                while (!objReader.EndOfStream)
                {
                    var line = objReader.ReadLine();

                    if (String.IsNullOrEmpty(line) || line.Trim().StartsWith("#"))
                        continue;

                    //line = ReplaceMultipleSpacesWithSingle(line);
                    line = line.Replace("  ", " ");
                    //Console.WriteLine(line);

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

        public static unsafe GlObject CreateObjectFromResource(GL Gl, string resourceName, string imageName)
        {

            List<float[]> objVertices = new List<float[]>();
            List<float[]> normals = new List<float[]>();
            List<float[]> objTextures = new List<float[]>();
            List<int[]> objFaces = new List<int[]>();

            string fullResourceName = "Projekt.Resources." + resourceName;
            using (var objStream = typeof(ObjResourceReader).Assembly.GetManifestResourceStream(fullResourceName))
            using (var objReader = new StreamReader(objStream))
            {
                while (!objReader.EndOfStream)
                {
                    var line = objReader.ReadLine();

                    if (string.IsNullOrWhiteSpace(line) || line.Length < 2)
                        continue;

                    var lineClassifier = line.Substring(0, line.IndexOf(' '));
                    var lineData = line.Substring(line.IndexOf(" ")).Trim().Split(' ');
                    lineData = lineData.Where(data => data.Length > 0).ToArray();
                    switch (lineClassifier)
                    {
                        case "v":
                            float[] vertex = new float[3];
                            for (int i = 0; i < vertex.Length; ++i)
                                vertex[i] = float.Parse(lineData[i], CultureInfo.InvariantCulture);
                            objVertices.Add(vertex);
                            break;
                        case "f":
                            int[] face = new int[9];
                            for (int i = 0; i < 3; ++i)
                            {
                                var coords = lineData[i].Split('/');
                                face[i * 3] = int.Parse(coords[0], CultureInfo.InvariantCulture);
                                if (coords[1] == "")
                                {
                                    face[i * 3 + 1] = -1;
                                }
                                face[i * 3 + 1] = int.Parse(coords[1], CultureInfo.InvariantCulture);
                                face[i * 3 + 2] = int.Parse(coords[2], CultureInfo.InvariantCulture);
                            }
                            objFaces.Add(face);
                            break;
                        case "vn":
                            float[] normal = new float[3];
                            for (int i = 0; i < normal.Length; ++i)
                                normal[i] = float.Parse(lineData[i], CultureInfo.InvariantCulture);
                            normals.Add(normal);
                            break;
                        case "vt":
                            float[] text = new float[2];
                            text[0] = float.Parse(lineData[0], CultureInfo.InvariantCulture);
                            text[1] = -float.Parse(lineData[1], CultureInfo.InvariantCulture);
                            objTextures.Add(text);
                            break;
                        default:
                            break;
                    }
                }
            }

            List<float> glVertices = new List<float>();
            List<float> glColors = new List<float>();
            List<uint> glIndexArray = new List<uint>();
            int counter = 0;
            foreach (var objFace in objFaces)
            {
                for (int i = 0; i < 3; ++i)
                {
                    glIndexArray.Add((uint)counter++);
                    glVertices.Add(objVertices[objFace[3 * i] - 1][0]);
                    glVertices.Add(objVertices[objFace[3 * i] - 1][1]);
                    glVertices.Add(objVertices[objFace[3 * i] - 1][2]);
                    glVertices.Add(normals[objFace[3 * i + 2] - 1][0]);
                    glVertices.Add(normals[objFace[3 * i + 2] - 1][1]);
                    glVertices.Add(normals[objFace[3 * i + 2] - 1][2]);
                    glVertices.Add(objTextures[objFace[3 * i + 1] - 1][0]);
                    glVertices.Add(objTextures[objFace[3 * i + 1] - 1][1]);
                    glColors.AddRange(faceColors[White]);
                }
            }

            uint vao = Gl.GenVertexArray();
            Gl.BindVertexArray(vao);

            uint offsetPos = 0;
            uint offsetNormals = offsetPos + 3 * sizeof(float);
            uint offsetTextures = offsetNormals + 3 * sizeof(float);
            uint vertexSize = offsetTextures + 2 * sizeof(float);

            uint vertices = Gl.GenBuffer();
            Gl.BindBuffer(BufferTargetARB.ArrayBuffer, vertices);
            Gl.BufferData(BufferTargetARB.ArrayBuffer, (ReadOnlySpan<float>)glVertices.ToArray().AsSpan(), BufferUsageARB.StaticDraw);
            Gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, vertexSize, (void*)offsetPos);
            Gl.EnableVertexAttribArray(0);
            Gl.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, true, vertexSize, (void*)offsetNormals);
            Gl.EnableVertexAttribArray(2);


            uint colors = Gl.GenBuffer();

            uint texture = Gl.GenTexture();
            // activate texture 0
            Gl.ActiveTexture(TextureUnit.Texture0);
            // bind texture
            Gl.BindTexture(TextureTarget.Texture2D, texture);

            var imageResult = ReadTextureImage(imageName);
            var textureBytes = (ReadOnlySpan<byte>)imageResult.Data.AsSpan();
            // Here we use "result.Width" and "result.Height" to tell OpenGL about how big our texture is.
            Gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba, (uint)imageResult.Width,
                (uint)imageResult.Height, 0, Silk.NET.OpenGL.PixelFormat.Rgba, Silk.NET.OpenGL.PixelType.UnsignedByte, textureBytes);
            Gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            Gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
            Gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            Gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            // unbinde texture
            Gl.BindTexture(TextureTarget.Texture2D, 0);

            Gl.EnableVertexAttribArray(3);
            Gl.VertexAttribPointer(3, 2, VertexAttribPointerType.Float, false, vertexSize, (void*)offsetTextures);

            uint indices = Gl.GenBuffer();
            Gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, indices);
            Gl.BufferData(BufferTargetARB.ElementArrayBuffer, (ReadOnlySpan<uint>)glIndexArray.ToArray().AsSpan(), BufferUsageARB.StaticDraw);

            // make sure to unbind array buffer
            Gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);

            uint indexArrayLength = (uint)glIndexArray.Count;

            Gl.BindVertexArray(0);

            return new GlObject(vao, vertices, colors, indices, indexArrayLength, Gl, texture);

        }


        public static unsafe GlObject CreateSkybox(GL Gl, String imageName)
        {
            uint vao = Gl.GenVertexArray();
            Gl.BindVertexArray(vao);

            // counter clockwise is front facing
            float[] vertexArray = [
                // top face
                -0.5f, 0.5f, 0.5f, 0f, -1f, 0f,  1f/4f, 0f/3f,
                0.5f, 0.5f, 0.5f, 0f, -1f, 0f,  2f/4f, 0f/3f,
                0.5f, 0.5f, -0.5f, 0f, -1f, 0f,  2f/4f, 1f/3f,
                -0.5f, 0.5f, -0.5f, 0f, -1f, 0f, 1f/4f, 1f/3f,

                // front face
                -0.5f, 0.5f, 0.5f, 0f, 0f, -1f, 4f/4f, 1f/3f,
                -0.5f, -0.5f, 0.5f, 0f, 0f, -1f, 4f/4f, 2f/3f,
                0.5f, -0.5f, 0.5f, 0f, 0f, -1f, 3f/4f, 2f/3f,
                0.5f, 0.5f, 0.5f, 0f, 0f, -1f,  3f/4f, 1f/3f,

                // left face
                -0.5f, 0.5f, 0.5f, 1f, 0f, 0f,  0, 1f/3f,
                -0.5f, 0.5f, -0.5f, 1f, 0f, 0f,  1f/4f, 1f/3f,
                -0.5f, -0.5f, -0.5f, 1f, 0f, 0f, 1f/4f, 2f/3f,
                -0.5f, -0.5f, 0.5f, 1f, 0f, 0f, 0f/4f, 2f/3f,

                // bottom face
                -0.5f, -0.5f, 0.5f, 0f, 1f, 0f, 1f/4f, 1f,
                0.5f, -0.5f, 0.5f,0f, 1f, 0f,  2f/4f, 1f,
                0.5f, -0.5f, -0.5f,0f, 1f, 0f, 2f/4f, 2f/3f,
                -0.5f, -0.5f, -0.5f,0f, 1f, 0f, 1f/4f, 2f/3f,

                // back face
                0.5f, 0.5f, -0.5f, 0f, 0f, 1f,2f/4f, 1f/3f,
                -0.5f, 0.5f, -0.5f,0f, 0f, 1f, 1f/4f, 1f/3f,
                -0.5f, -0.5f, -0.5f,0f, 0f, 1f, 1f/4f, 2f/3f,
                0.5f, -0.5f, -0.5f,0f, 0f, 1f,2f/4f, 2f/3f,

                // right face
                0.5f, 0.5f, 0.5f, -1f, 0f, 0f,3f/4f, 1f/3f,
                0.5f, 0.5f, -0.5f,-1f, 0f, 0f,2f/4f, 1f/3f,
                0.5f, -0.5f, -0.5f,-1f, 0f, 0f, 2f/4f, 2f/3f,
                0.5f, -0.5f, 0.5f,-1f, 0f, 0f,3f/4f, 2f/3f,
            ];
            uint[] indexArray = new uint[] {
                2, 1, 0,
                3, 2, 0,

                6, 5, 4,
                7, 6, 4,

                10, 9, 8,
                8, 11, 10,

                13, 14, 12,
                14, 15, 12,

                19, 16, 17,
                18, 19, 17,

                21, 22, 20,
                22, 23, 20
            };

            float[] colorArray = [1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f];

            uint offsetPos = 0;
            uint offsetNormals = offsetPos + 3 * sizeof(float);
            uint offsetTextures = offsetNormals + 3 * sizeof(float);
            uint vertexSize = offsetTextures + 2 * sizeof(float);

            uint vertices = Gl.GenBuffer();
            Gl.BindBuffer(BufferTargetARB.ArrayBuffer, vertices);
            Gl.BufferData(BufferTargetARB.ArrayBuffer, (ReadOnlySpan<float>)vertexArray.AsSpan(), BufferUsageARB.StaticDraw);
            Gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, vertexSize, (void*)offsetPos);
            Gl.EnableVertexAttribArray(0);
            Gl.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, true, vertexSize, (void*)offsetNormals);
            Gl.EnableVertexAttribArray(2);


            uint colors = Gl.GenBuffer();

            uint texture = Gl.GenTexture();
            // activate texture 0
            Gl.ActiveTexture(TextureUnit.Texture0);
            // bind texture
            Gl.BindTexture(TextureTarget.Texture2D, texture);

            var skyboxImageResult = ReadTextureImage(imageName);
            var textureBytes = (ReadOnlySpan<byte>)skyboxImageResult.Data.AsSpan();
            // Here we use "result.Width" and "result.Height" to tell OpenGL about how big our texture is.
            Gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba, (uint)skyboxImageResult.Width,
                (uint)skyboxImageResult.Height, 0, Silk.NET.OpenGL.PixelFormat.Rgba, Silk.NET.OpenGL.PixelType.UnsignedByte, textureBytes);
            Gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            Gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
            Gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            Gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            // unbinde texture
            Gl.BindTexture(TextureTarget.Texture2D, 0);

            Gl.EnableVertexAttribArray(3);
            Gl.VertexAttribPointer(3, 2, VertexAttribPointerType.Float, false, vertexSize, (void*)offsetTextures);

            uint indices = Gl.GenBuffer();
            Gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, indices);
            Gl.BufferData(BufferTargetARB.ElementArrayBuffer, (ReadOnlySpan<uint>)indexArray.AsSpan(), BufferUsageARB.StaticDraw);

            // make sure to unbind array buffer
            Gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);

            uint indexArrayLength = (uint)indexArray.Length;

            Gl.BindVertexArray(0);

            return new GlObject(vao, vertices, colors, indices, indexArrayLength, Gl, texture);
        }

        public static unsafe ImageResult ReadTextureImage(string textureResource)
        {
            ImageResult result;
            try
            {
                using (Stream skyboxStream
                    = typeof(GlObject).Assembly.GetManifestResourceStream("Projekt.Resources." + textureResource))
                    result = ImageResult.FromStream(skyboxStream, ColorComponents.RedGreenBlueAlpha);
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Environment.Exit(-1);
            }
            return null;
        }
    }
}
