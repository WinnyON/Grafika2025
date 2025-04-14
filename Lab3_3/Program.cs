using ImGuiNET;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Core;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using System.Numerics;
using System.Reflection;
using Szeminarium;

namespace GrafikaSzeminarium
{
    internal class Program
    {
        private static IWindow graphicWindow;

        private static IInputContext _inputContext;
        private static IKeyboard _keyboard;
        private static IMouse mouse;

        private static GL Gl;

        private static ImGuiController imGuiController;
        private static ModelObjectDescriptor[] cubes;

        private static CameraDescriptor camera = new CameraDescriptor();

        private static CubeArrangementModel cubeArrangementModel = new CubeArrangementModel();

        private static float WindowWidth = 1080;
        private static float WindowHeight = 720;
        private static float AspectRatio = 108f / 72f;

        private static bool animationInProgress = false;
        private static List<ModelObjectDescriptor> animatedCubes;
        private static float rotationProgress = 0;
        private static int rotationAxis;
        private static int rotationDirection = 1;
        private static bool selectedRotation = false;

        private static int[] mix;
        private static int[] dir;
        private static Random random;
        private static bool mixOn = false;
        private static int currentMix = 0;

        private const string ModelMatrixVariableName = "uModel";
        private const string ViewMatrixVariableName = "uView";
        private const string ProjectionMatrixVariableName = "uProjection";
        private const string NormalMatrixVariableName = "uNormal";

        private const string LightColorVariableName = "uLightColor";
        private const string LightPositionVariableName = "uLightPos";
        private const string ViewPositionVariableName = "uViewPos";

        private const string AmbientStrengthName = "uAmbientStrength";
        private const string DiffuseStrengthName = "uDiffuseStrength";
        private const string SpecularStrengthName = "uSpecularStrength";

        private const string ShinenessVariableName = "uShininess";

        private static float shininess = 50;
        private static float lightColorR = 255.0f;
        private static float lightColorG = 255.0f;
        private static float lightColorB = 255.0f;
        private static float ambient = 10;
        private static float specular = 60;
        private static float diffuse = 30;

        //private static float lightPosX = 0;
        //private static float lightPosY = 100;
        //private static float lightPosZ = 100;
        private static Vector3 lightPos = new Vector3(0.0f, 1.0f, 2.0f);

        //      private static readonly string VertexShaderSource = @"
        //      #version 330 core
        //      layout (location = 0) in vec3 vPos;
        //layout (location = 1) in vec4 vCol;

        //      uniform mat4 uModel;
        //      uniform mat4 uView;
        //      uniform mat4 uProjection;

        //out vec4 outCol;

        //      void main()
        //      {
        //	outCol = vCol;
        //          gl_Position = uProjection*uView*uModel*vec4(vPos.x, vPos.y, vPos.z, 1.0);
        //      }
        //      ";


        //      private static readonly string FragmentShaderSource = @"
        //      #version 330 core
        //      out vec4 FragColor;

        //in vec4 outCol;

        //      void main()
        //      {
        //          FragColor = outCol;
        //      }
        //      ";

        private static uint program;

        static void Main(string[] args)
        {
            random = new Random();
            WindowOptions windowOptions = WindowOptions.Default;
            windowOptions.Title = "Lab2";
            windowOptions.Size = new Silk.NET.Maths.Vector2D<int>((int)WindowWidth, (int)WindowHeight);

            graphicWindow = Window.Create(windowOptions);

            graphicWindow.Load += GraphicWindow_Load;
            graphicWindow.Update += GraphicWindow_Update;
            graphicWindow.Render += GraphicWindow_Render;
            graphicWindow.Closing += GraphicWindow_Closing;
            graphicWindow.Resize += GraphicWindow_Resize;

            graphicWindow.Run();
        }

        private static void GraphicWindow_Resize(Vector2D<int> d)
        {
            AspectRatio = (float)d.X / d.Y;
            WindowWidth = d.X;
            WindowHeight = d.Y;
        }

        private static void GraphicWindow_Closing()
        {
            //cube.Dispose();
            foreach(var cube in cubes)
            {
                cube.Dispose();
            }
            Gl.DeleteProgram(program);
        }

        private static void setSidesBasedOnCoordinates(int i, int j, int k, int[] sides)
        {
            if(i > -1)
            {
                sides[5] = 1;
            }
            if(j > -1)
            {
                sides[3] = 1;
            }
            if(k > -1)
            {
                sides[4] = 1;
            }
            if(i < 1)
            {
                sides[2] = 1;
            }
            if(j < 1)
            {
                sides[0] = 1;
            }
            if(k < 1)
            {
                sides[1] = 1;
            }
        }

        private static void GraphicWindow_Load()
        {
            Gl = graphicWindow.CreateOpenGL();
            _inputContext = graphicWindow.CreateInput();
            _keyboard = _inputContext.Keyboards[0]; // ez lehet nem jo
            var inputContext = _inputContext; //graphicWindow.CreateInput();
            imGuiController = new ImGuiController(Gl, graphicWindow, inputContext);
            foreach (var keyboard in inputContext.Keyboards)
            {
                keyboard.KeyDown += Keyboard_KeyDown;
            }

            mouse = inputContext.Mice[0];
            mouse.MouseMove += OnMouseMove;
            mouse.Cursor.CursorMode = CursorMode.Raw;
            //cube = ModelObjectDescriptor.CreateCube(Gl);
            //camera.SetZYAngle(Math.PI / 180 * 35);
            //camera.SetZXAngle(Math.PI / 180 * 500);

            //camera.SetZYAngle(Math.PI / 180);
            //camera.SetZXAngle(Math.PI / 180);

            cubes = new ModelObjectDescriptor[27];
            int index = 0;
            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    for (int k = -1; k <= 1; k++)
                    {
                        int[] sides = { 0, 0, 0, 0, 0, 0 };
                        setSidesBasedOnCoordinates(i, j, k, sides);
                        ModelObjectDescriptor.setColorArray(sides);
                        cubes[index] = ModelObjectDescriptor.CreateCube(Gl);
                        index++;
                    }
                }
            }




            Gl.ClearColor(System.Drawing.Color.DarkGray);
            
            Gl.Enable(EnableCap.CullFace);
            Gl.CullFace(TriangleFace.Back);

            Gl.Enable(EnableCap.DepthTest);
            Gl.DepthFunc(DepthFunction.Lequal);


            uint vshader = Gl.CreateShader(ShaderType.VertexShader);
            uint fshader = Gl.CreateShader(ShaderType.FragmentShader);

            Gl.ShaderSource(vshader, GetEmbeddedResourceAsString("Shaders.VertexShader.vert"));
            Gl.CompileShader(vshader);
            Gl.GetShader(vshader, ShaderParameterName.CompileStatus, out int vStatus);
            if (vStatus != (int)GLEnum.True)
                throw new Exception("Vertex shader failed to compile: " + Gl.GetShaderInfoLog(vshader));

            Gl.ShaderSource(fshader, GetEmbeddedResourceAsString("Shaders.FragmentShader.frag"));
            Gl.CompileShader(fshader);
            Gl.GetShader(fshader, ShaderParameterName.CompileStatus, out int fStatus);
            if (fStatus != (int)GLEnum.True)
                throw new Exception("Fragment shader failed to compile: " + Gl.GetShaderInfoLog(fshader));

            program = Gl.CreateProgram();
            Gl.AttachShader(program, vshader);
            Gl.AttachShader(program, fshader);
            Gl.LinkProgram(program);

            Gl.DetachShader(program, vshader);
            Gl.DetachShader(program, fshader);
            Gl.DeleteShader(vshader);
            Gl.DeleteShader(fshader);
            if ((ErrorCode)Gl.GetError() != ErrorCode.NoError)
            {

            }

            Gl.GetProgram(program, GLEnum.LinkStatus, out var status);
            if (status == 0)
            {
                Console.WriteLine($"Error linking shader {Gl.GetProgramInfoLog(program)}");
            }
        }

        private static void SetCursorMode()
        {
            if (mouse.Cursor.CursorMode == CursorMode.Raw)
            {
                mouse.Cursor.CursorMode = CursorMode.Normal;
                //firstMove = true;
            }
            else
            {
                mouse.Cursor.CursorMode = CursorMode.Raw;
            }
        }

        private static void Keyboard_KeyDown(IKeyboard keyboard, Key key, int arg3)
        {
            switch (key)
            {
                case Key.Escape:
                    SetCursorMode();
                    break;
                case Key.Number0:
                    RotateSide(0);
                    break;
                case Key.Number1:
                    RotateSide(1);
                    break;
                case Key.Number2:
                    RotateSide(2);
                    break;
                case Key.Number3:
                    RotateSide(3);
                    break;
                case Key.Number4:
                    RotateSide(4);
                    break;
                case Key.Number5:
                    RotateSide(5);
                    break;
                case Key.Number6:
                    RotateSide(6);
                    break;
                case Key.Number7:
                    RotateSide(7);
                    break;
                case Key.Number8:
                    RotateSide(8);
                    break;
                case Key.Space:
                    animationInProgress = true;
                    rotationDirection = 1;
                    break;
                case Key.Backspace:
                    animationInProgress = true;
                    rotationDirection = -1;
                    break;
                case Key.R:
                    generateRotations();
                    mixOn = true;
                    currentMix = 0;
                    animationInProgress = false;
                    break;
                case Key.Enter:
                    cubeArrangementModel.resetScale();
                    cubeArrangementModel.AnimationEnabled = !cubeArrangementModel.AnimationEnabled;
                    break;
            }
        }

        private static bool checkComplete()
        {
            int index = 0;
            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    for (int k = -1; k <= 1; k++)
                    {
                        if (!(cubes[index].Xindex == i && cubes[index].Yindex == j && cubes[index].Zindex == k))
                        {
                            return false;
                        }
                        index++;
                    }
                }
            }
            return true;
        }

        private static string GetEmbeddedResourceAsString(string resourceRelativePath)
        {
            string resourceFullPath = Assembly.GetExecutingAssembly().GetName().Name + "." + resourceRelativePath;

            using (var resStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceFullPath))
            using (var resStreamReader = new StreamReader(resStream))
            {
                var text = resStreamReader.ReadToEnd();
                return text;
            }
        }
        private static void generateRotations()
        {
            mix = new int[30];
            dir = new int[30];
            for(int i = 0; i < 30; i++)
            {
                mix[i] = random.Next(9);
                if(random.Next(2) == 0)
                {
                    dir[i] = -1;
                }
                else
                {
                    dir[i] = 1;
                }
            }
        }

        private static void RotateSide(int side)
        {
            //foreach(var cube in cubes)
            //{
            //    Console.WriteLine(cube.Xindex + " " + cube.Yindex + " " + cube.Zindex);
            //}
            selectedRotation = true;
            animatedCubes = new List<ModelObjectDescriptor>();
            rotationProgress = 0;
            switch (side)
            {
                case 0:
                    rotationAxis = 2;
                    foreach (var cube in cubes)
                    {
                        if(cube.Zindex == 1){
                            animatedCubes.Add(cube);
                            
                        }
                    }
                    break;
                case 1:
                    rotationAxis = 2;
                    foreach (var cube in cubes)
                    {
                        if (cube.Zindex == 0)
                        {
                            animatedCubes.Add(cube);
                        }
                    }
                    break;
                case 2:
                    rotationAxis = 2;
                    foreach (var cube in cubes)
                    {
                        if (cube.Zindex == -1)
                        {
                            animatedCubes.Add(cube);
                        }
                    }
                    break;
                case 3:
                    rotationAxis = 1;
                    foreach (var cube in cubes)
                    {
                        if (cube.Yindex == 1)
                        {
                            animatedCubes.Add(cube);
                        }
                    }
                    break;
                case 4:
                    rotationAxis = 1;
                    foreach (var cube in cubes)
                    {
                        if (cube.Yindex == 0)
                        {
                            animatedCubes.Add(cube);
                        }
                    }
                    break;
                case 5:
                    rotationAxis = 1;
                    foreach (var cube in cubes)
                    {
                        if (cube.Yindex == -1)
                        {
                            animatedCubes.Add(cube);
                        }
                    }
                    break;
                case 6:
                    rotationAxis = 0;
                    foreach (var cube in cubes)
                    {
                        if (cube.Xindex == 1)
                        {
                            animatedCubes.Add(cube);
                        }
                    }
                    break;
                case 7:
                    rotationAxis = 0;
                    foreach (var cube in cubes)
                    {
                        if (cube.Xindex == 0)
                        {
                            animatedCubes.Add(cube);
                        }
                    }
                    break;
                case 8:
                    rotationAxis = 0;
                    foreach (var cube in cubes)
                    {
                        if (cube.Xindex == -1)
                        {
                            animatedCubes.Add(cube);
                        }
                    }
                    break;
            }
        }

        private static void GraphicWindow_Update(double deltaTime)
        {
            // NO OpenGL
            // make it threadsafe
            cubeArrangementModel.AdvanceTime(deltaTime);

            if (animationInProgress || mixOn)
            {
                advanceRotation((float)deltaTime);
            }

            handleMovement();

            imGuiController.Update((float)deltaTime);

        }

        public static void advanceRotation(float amount)
        {
            if(mixOn)
            {
                amount *= 1.5f;
            }
            if(!animationInProgress && mixOn && currentMix < 30)
            {
                rotationDirection = dir[currentMix];
                RotateSide(mix[currentMix]);
                animationInProgress = true;
                currentMix++;
                if(currentMix == 30)
                {
                    mixOn = false;
                }
            }
            else if (!selectedRotation)
            {
                return;
            }
            rotationProgress += amount;
            if (rotationProgress > 1)
            {
                rotationProgress = 1;
                animationInProgress = false;
                selectedRotation = false;

            }
            Matrix4X4<float> rotation = Matrix4X4<float>.Identity;
            
            switch (rotationAxis)
            {
                case 0: // x
                    rotation = Matrix4X4.CreateRotationX((float)Math.PI / 2 * rotationProgress * rotationDirection);
                    break;
                case 1: // y
                    rotation = Matrix4X4.CreateRotationY((float)Math.PI / 2 * rotationProgress * rotationDirection);
                    break;
                case 2: // z
                    rotation = Matrix4X4.CreateRotationZ((float)Math.PI / 2 * rotationProgress * rotationDirection);
                    break;
            }
            
            if (rotationProgress < 1)
            {
                foreach (var cube in animatedCubes)
                {
                    cube.TempRotationMatrix = rotation;
                }
            }
            else
            {
                foreach (var cube in animatedCubes)
                {
                    cube.TempRotationMatrix = Matrix4X4<float>.Identity;
                    cube.PageRotationMatrix *= rotation;
                    int temp;
                    switch (rotationAxis)
                    {
                        case 0: // x axis
                            temp = rotationDirection * cube.Yindex;
                            cube.Yindex = -rotationDirection * cube.Zindex;
                            cube.Zindex = temp;
                            break;
                        case 1: // y axis
                            temp = rotationDirection * cube.Xindex;
                            cube.Xindex = -rotationDirection * cube.Zindex;
                            cube.Zindex = temp;
                            break;
                        case 2: // z axis
                            temp = -rotationDirection * cube.Xindex;
                            cube.Xindex = rotationDirection * cube.Yindex;
                            cube.Yindex = temp;
                            break;
                    }
                }
                if (checkComplete())
                {
                    cubeArrangementModel.resetScale();
                    cubeArrangementModel.AnimationEnabled = !cubeArrangementModel.AnimationEnabled;
                }
            }
        }

        private static void OnMouseMove(IMouse mouse, Vector2 position)
        {
            //Console.WriteLine($"Mouse moved to: {position.X}, {position.Y}");
            camera.MouseMove(position.X, position.Y);
        }

        private static void handleMovement()
        {
            if (_keyboard.IsKeyPressed(Key.A))
            {
                //camera.DecreaseTargetX();
                camera.Move(Key.A);
            }
            if (_keyboard.IsKeyPressed(Key.D))
            {
                //camera.IncreaseTargetX();
                camera.Move(Key.D);
            }
            if (_keyboard.IsKeyPressed(Key.W))
            {
                //camera.IncreaseTargetY();
                camera.Move(Key.W);
            }
            if (_keyboard.IsKeyPressed(Key.S))
            {
                //camera.DecreaseTargetY();
                camera.Move(Key.S);
            }
        }

        private static unsafe void GraphicWindow_Render(double deltaTime)
        {
            Gl.Clear(ClearBufferMask.ColorBufferBit);
            Gl.Clear(ClearBufferMask.DepthBufferBit);

            Gl.UseProgram(program);

            SetUniform3(LightColorVariableName, new Vector3(lightColorR / 255f, lightColorG / 255f, lightColorB / 255f));
            SetUniform3(LightPositionVariableName,lightPos);  //new Vector3(camera.Position.X, camera.Position.Y, camera.Position.Z)); 
            SetUniform3(ViewPositionVariableName, new Vector3(camera.Position.X, camera.Position.Y, camera.Position.Z));    
            SetUniform1(ShinenessVariableName, shininess);
            SetUniform3(AmbientStrengthName, new Vector3(ambient / 100f, ambient / 100f, ambient / 100f));
            SetUniform3(DiffuseStrengthName, new Vector3(diffuse / 100f, diffuse / 100f, diffuse / 100f));
            SetUniform3(SpecularStrengthName, new Vector3(specular / 100f, specular / 100f, specular / 100f));


            //var viewMatrix = Matrix4X4.CreateLookAt(camera.Position, camera.Target, camera.UpVector);
            var viewMatrix = camera.View;
            //var viewMatrix = Matrix4X4.CreateLookAt(camera.Position, camera.Forward, camera.UpVector);
            SetMatrix(viewMatrix, ViewMatrixVariableName);

            var projectionMatrix = Matrix4X4.CreatePerspectiveFieldOfView<float>((float)(Math.PI / 3), AspectRatio, 0.1f, 100f);
            SetMatrix(projectionMatrix, ProjectionMatrixVariableName);


            //var modelMatrixCenterCube = Matrix4X4.CreateScale((float)cubeArrangementModel.CenterCubeScale);
            
            int index = 0;
            for (int i = -1; i <= 1; i++)
            {
                for(int j = -1; j <= 1; j++)
                {
                    for(int k = -1; k <= 1; k++)
                    {
                        if (cubes[index].initialDraw)
                        {
                            Matrix4X4<float> cubeScale = Matrix4X4.CreateScale((float)cubeArrangementModel.CenterCubeScale);
                            Matrix4X4<float> trans = Matrix4X4.CreateTranslation(-i * 0.21f, j * 0.21f, k * 0.21f);
                            Matrix4X4<float> cubeModelMatrix = cubeScale * trans;
                            cubes[index].InitialPositionMatrix = cubeModelMatrix;
                            cubes[index].PageRotationMatrix = Matrix4X4<float>.Identity;
                            cubes[index].TempRotationMatrix = Matrix4X4<float>.Identity;
                            //SetMatrix(cubeModelMatrix, ModelMatrixVariableName);
                            SetModelMatrix(cubeModelMatrix);
                            cubes[index].initialDraw = false;
                            cubes[index].Xindex = i;
                            cubes[index].Yindex = j;
                            cubes[index].Zindex = k;
                        }
                        else
                        {
                            if (cubeArrangementModel.AnimationEnabled)
                            {
                                Matrix4X4<float> cubeScale = Matrix4X4.CreateScale((float)cubeArrangementModel.CenterCubeScale * 0.2f);
                                float offset = (float)cubeArrangementModel.CenterCubeScale * 0.2f + 0.01f;
                                Matrix4X4<float> translation = Matrix4X4.CreateTranslation(-i * offset, j * offset, k * offset);
                                Matrix4X4<float> cubeModelMatrix = cubeScale * translation;
                                //SetMatrix(cubeModelMatrix, ModelMatrixVariableName);
                                SetModelMatrix(cubeModelMatrix);

                            }
                            else
                            {
                                Matrix4X4<float> trans = cubes[index].InitialPositionMatrix * cubes[index].PageRotationMatrix * cubes[index].TempRotationMatrix;
                                //SetMatrix(trans, ModelMatrixVariableName);
                                SetModelMatrix(trans);
                            }
                        }
                            DrawModelObject(cubes[index]);
                        index++;
                    }
                }
            }

            ImGuiNET.ImGui.Begin("Lighting", ImGuiNET.ImGuiWindowFlags.AlwaysAutoResize | ImGuiNET.ImGuiWindowFlags.NoCollapse);
            ImGuiNET.ImGui.SliderFloat("Shininess", ref shininess, 5, 100);
            ImGuiNET.ImGui.SliderFloat("AmbientStrength", ref ambient, 1, 100);
            ImGuiNET.ImGui.SliderFloat("SpecularStrength", ref specular, 1, 100);
            ImGuiNET.ImGui.SliderFloat("DiffuseStrength", ref diffuse, 1, 100);
            ImGuiNET.ImGui.SliderFloat("LightColor R", ref lightColorR, 0, 255);
            ImGuiNET.ImGui.SliderFloat("LightColor G", ref lightColorG, 0, 255);
            ImGuiNET.ImGui.SliderFloat("LightColor B", ref lightColorB, 0, 255);
            //ImGuiNET.ImGui.SliderFloat("Light Position X", ref lightPosX, -100, 100);
            //ImGuiNET.ImGui.SliderFloat("Light Position Y", ref lightPosY, -100, 100);
            //ImGuiNET.ImGui.SliderFloat("Light Position Z", ref lightPosZ, -100, 100);
            ImGuiNET.ImGui.InputFloat3("Light Position", ref lightPos, "%.2f");
            ImGuiNET.ImGui.End();

            imGuiController.Render();

            //SetMatrix(modelMatrixCenterCube, ModelMatrixVariableName);
            //DrawModelObject(cube);

            //Matrix4X4<float> diamondScale = Matrix4X4.CreateScale(0.25f);
            //Matrix4X4<float> rotx = Matrix4X4.CreateRotationX((float)Math.PI / 4f);
            //Matrix4X4<float> rotz = Matrix4X4.CreateRotationZ((float)Math.PI / 4f);
            //Matrix4X4<float> roty = Matrix4X4.CreateRotationY((float)cubeArrangementModel.DiamondCubeLocalAngle);
            //Matrix4X4<float> trans = Matrix4X4.CreateTranslation(1f, 1f, 0f);
            //Matrix4X4<float> rotGlobalY = Matrix4X4.CreateRotationY((float)cubeArrangementModel.DiamondCubeGlobalYAngle);
            //Matrix4X4<float> dimondCubeModelMatrix = diamondScale * rotx * rotz * roty * trans * rotGlobalY;
            //SetMatrix(dimondCubeModelMatrix, ModelMatrixVariableName);
            //DrawModelObject(cube);

        }
        private static unsafe void SetModelMatrix(Matrix4X4<float> modelMatrix)
        {
            SetMatrix(modelMatrix, ModelMatrixVariableName);

            // set also the normal matrix
            int location = Gl.GetUniformLocation(program, NormalMatrixVariableName);
            if (location == -1)
            {
                throw new Exception($"{NormalMatrixVariableName} uniform not found on shader.");
            }

            // G = (M^-1)^T
            var modelMatrixWithoutTranslation = new Matrix4X4<float>(modelMatrix.Row1, modelMatrix.Row2, modelMatrix.Row3, modelMatrix.Row4);
            modelMatrixWithoutTranslation.M41 = 0;
            modelMatrixWithoutTranslation.M42 = 0;
            modelMatrixWithoutTranslation.M43 = 0;
            modelMatrixWithoutTranslation.M44 = 1;

            Matrix4X4<float> modelInvers;
            Matrix4X4.Invert<float>(modelMatrixWithoutTranslation, out modelInvers);
            Matrix3X3<float> normalMatrix = new Matrix3X3<float>(Matrix4X4.Transpose(modelInvers));

            Gl.UniformMatrix3(location, 1, false, (float*)&normalMatrix);
            CheckError();
        }

        private static unsafe void SetUniform1(string uniformName, float uniformValue)
        {
            int location = Gl.GetUniformLocation(program, uniformName);
            if (location == -1)
            {
                throw new Exception($"{uniformName} uniform not found on shader.");
            }

            Gl.Uniform1(location, uniformValue);
            CheckError();
        }

        private static unsafe void SetUniform3(string uniformName, Vector3 uniformValue)
        {
            int location = Gl.GetUniformLocation(program, uniformName);
            if (location == -1)
            {
                throw new Exception($"{uniformName} uniform not found on shader.");
            }

            Gl.Uniform3(location, uniformValue);
            CheckError();
        }

        private static unsafe void DrawModelObject(ModelObjectDescriptor modelObject)
        {
            Gl.BindVertexArray(modelObject.Vao);
            Gl.BindBuffer(GLEnum.ElementArrayBuffer, modelObject.Indices);
            Gl.DrawElements(PrimitiveType.Triangles, modelObject.IndexArrayLength, DrawElementsType.UnsignedInt, null);
            Gl.BindBuffer(GLEnum.ElementArrayBuffer, 0);
            Gl.BindVertexArray(0);
        }

        private static unsafe void SetMatrix(Matrix4X4<float> mx, string uniformName)
        {
            int location = Gl.GetUniformLocation(program, uniformName);
            if (location == -1)
            {
                throw new Exception($"{ViewMatrixVariableName} uniform not found on shader.");
            }

            Gl.UniformMatrix4(location, 1, false, (float*)&mx);
            CheckError();
        }

        public static void CheckError()
        {
            var error = (ErrorCode)Gl.GetError();
            if (error != ErrorCode.NoError)
                throw new Exception("GL.GetError() returned " + error.ToString());
        }
    }
}