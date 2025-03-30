using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using System.Numerics;
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

        private const string ModelMatrixVariableName = "uModel";
        private const string ViewMatrixVariableName = "uView";
        private const string ProjectionMatrixVariableName = "uProjection";

        private static readonly string VertexShaderSource = @"
        #version 330 core
        layout (location = 0) in vec3 vPos;
		layout (location = 1) in vec4 vCol;

        uniform mat4 uModel;
        uniform mat4 uView;
        uniform mat4 uProjection;

		out vec4 outCol;
        
        void main()
        {
			outCol = vCol;
            gl_Position = uProjection*uView*uModel*vec4(vPos.x, vPos.y, vPos.z, 1.0);
        }
        ";


        private static readonly string FragmentShaderSource = @"
        #version 330 core
        out vec4 FragColor;
		
		in vec4 outCol;

        void main()
        {
            FragColor = outCol;
        }
        ";

        private static uint program;

        static void Main(string[] args)
        {
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




            Gl.ClearColor(System.Drawing.Color.White);
            
            Gl.Enable(EnableCap.CullFace);
            Gl.CullFace(TriangleFace.Back);

            Gl.Enable(EnableCap.DepthTest);
            Gl.DepthFunc(DepthFunction.Lequal);


            uint vshader = Gl.CreateShader(ShaderType.VertexShader);
            uint fshader = Gl.CreateShader(ShaderType.FragmentShader);

            Gl.ShaderSource(vshader, VertexShaderSource);
            Gl.CompileShader(vshader);
            Gl.GetShader(vshader, ShaderParameterName.CompileStatus, out int vStatus);
            if (vStatus != (int)GLEnum.True)
                throw new Exception("Vertex shader failed to compile: " + Gl.GetShaderInfoLog(vshader));

            Gl.ShaderSource(fshader, FragmentShaderSource);
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
                case Key.Space:
                    animationInProgress = true;
                    rotationDirection = 1;
                    break;
                case Key.Backspace:
                    animationInProgress = true;
                    rotationDirection = -1;
                    break;
                //case Key.Left:
                //    camera.DecreaseZYAngle();
                //    break;
                //case Key.Right:
                //    camera.IncreaseZYAngle();
                //    break;
                //case Key.Down:
                //    camera.IncreaseDistance();
                //    break;
                //case Key.Up:
                //    camera.DecreaseDistance();
                //    break;
                //case Key.F:
                //    camera.IncreaseZXAngle();
                //    break;
                //case Key.L:
                //    camera.DecreaseZXAngle();
                //    break;
                //case Key.W:
                //    camera.DecreaseTargetZ();
                //    break;
                //case Key.S:
                //    camera.IncreaseTargetZ();
                //    break;
                //case Key.D:
                //    camera.IncreaseTargetX();
                //    break;
                //case Key.A:
                //    camera.DecreaseTargetX();
                //    break;
                //case Key.R:
                //    camera.DecreaseTargetZ();
                //    break;
                //case Key.T:
                //    camera.IncreaseTargetZ();
                //    break;
                //case Key.Space:
                //    cubeArrangementModel.AnimationEnabled = !cubeArrangementModel.AnimationEnabled;
                //break;
            }
        }

        private static void RotateSide(int side)
        {
            selectedRotation = true;
            animatedCubes = new List<ModelObjectDescriptor>();
            switch (side)
            {
                case 0:
                    foreach(var cube in cubes)
                    {
                        if(cube.Zindex == 1){
                            //Matrix4X4<float> rotation = Matrix4X4.CreateRotationZ((float)Math.PI / 2f);
                            //cube.PageRotationMatrix = rotation * cube.PageRotationMatrix;
                            animatedCubes.Add(cube);
                            rotationProgress = 0;
                            rotationAxis = 2;
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

            if (animationInProgress)
            {
                advanceRotation((float)deltaTime);
            }

            handleMovement();

        }

        public static void advanceRotation(float amount)
        {
            if (!selectedRotation)
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
            if (rotationProgress < 1)
            {
                switch (rotationAxis)
                {
                    case 0: // x
                        break;
                    case 1: // y
                        break;
                    case 2: // z
                        rotation = Matrix4X4.CreateRotationZ((float)Math.PI / 2 * rotationProgress * rotationDirection);
                        break;
                }
                foreach (var cube in animatedCubes)
                {
                    cube.TempRotationMatrix = rotation;
                }
            }
            else
            {
                rotation = Matrix4X4.CreateRotationZ((float)Math.PI / 2 * rotationProgress * rotationDirection);
                foreach (var cube in animatedCubes)
                {
                    cube.TempRotationMatrix = Matrix4X4<float>.Identity;
                    cube.PageRotationMatrix *= rotation;
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
            //foreach(var key in _keyboard.SupportedKeys)
            //{
            //    Console.WriteLine(key);
            //    camera.Move(key);
            //}

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
            //if (_keyboard.IsKeyPressed(Key.R))
            //{
            //    //camera.DecreaseTargetY();
            //}
            //if (_keyboard.IsKeyPressed(Key.T))
            //{
            //    //camera.IncreaseTargetY();
            //}
            //if (_keyboard.IsKeyPressed(Key.F))
            //{
            //    camera.IncreaseZXAngle();
            //}
            //if (_keyboard.IsKeyPressed(Key.L))
            //{
            //    camera.DecreaseZXAngle();
            //}
            //if (_keyboard.IsKeyPressed(Key.Left))
            //{
            //    //camera.DecreaseZYAngle();
            //    //camera.UpdateCamera(0.0f, -0.012f);
            //    camera.Move(Key.Left);
            //}
            //if (_keyboard.IsKeyPressed(Key.Right))
            //{
            //    //camera.IncreaseZYAngle();
            //    //camera.UpdateCamera(0.0f, 0.012f);
            //    camera.Move(Key.Right);
            //}
            //if (_keyboard.IsKeyPressed(Key.Down))
            //{
            //    //camera.IncreaseDistance();
            //    //camera.UpdateCamera(-0.012f, 0.0f);
            //    camera.Move(Key.Down);
            //}
            //if (_keyboard.IsKeyPressed(Key.Up))
            //{
            //    //camera.DecreaseDistance();
            //    //camera.UpdateCamera(0.012f, 0.0f);
            //    camera.Move(Key.Up);
            //}
        }

        private static unsafe void GraphicWindow_Render(double deltaTime)
        {
            Gl.Clear(ClearBufferMask.ColorBufferBit);
            Gl.Clear(ClearBufferMask.DepthBufferBit);

            Gl.UseProgram(program);

            //var viewMatrix = Matrix4X4.CreateLookAt(camera.Position, camera.Target, camera.UpVector);
            var viewMatrix = camera.View;
            //var viewMatrix = Matrix4X4.CreateLookAt(camera.Position, camera.Forward, camera.UpVector);
            SetMatrix(viewMatrix, ViewMatrixVariableName);

            var projectionMatrix = Matrix4X4.CreatePerspectiveFieldOfView<float>((float)(Math.PI / 2), AspectRatio, 0.1f, 100f);
            SetMatrix(projectionMatrix, ProjectionMatrixVariableName);


            //var modelMatrixCenterCube = Matrix4X4.CreateScale((float)cubeArrangementModel.CenterCubeScale);
            Matrix4X4<float> cubeScale = Matrix4X4.CreateScale((float)cubeArrangementModel.CenterCubeScale);
            int index = 0;
            for (int i = -1; i <= 1; i++)
            {
                for(int j = -1; j <= 1; j++)
                {
                    for(int k = -1; k <= 1; k++)
                    {
                        if (cubes[index].initialDraw)
                        {
                            Matrix4X4<float> trans = Matrix4X4.CreateTranslation(-i * 0.21f, j * 0.21f, k * 0.21f);
                            Matrix4X4<float> cubeModelMatrix = cubeScale * trans;
                            cubes[index].InitialPositionMatrix = cubeModelMatrix;
                            cubes[index].PageRotationMatrix = Matrix4X4<float>.Identity;
                            cubes[index].TempRotationMatrix = Matrix4X4<float>.Identity;
                            SetMatrix(cubeModelMatrix, ModelMatrixVariableName);
                            cubes[index].initialDraw = false;
                            cubes[index].Xindex = i;
                            cubes[index].Yindex = j;
                            cubes[index].Zindex = k;
                        }
                        else
                        {
                            Matrix4X4<float> trans = cubes[index].InitialPositionMatrix *  cubes[index].PageRotationMatrix * cubes[index].TempRotationMatrix;
                            SetMatrix(trans, ModelMatrixVariableName);
                        }
                            DrawModelObject(cubes[index]);
                        index++;
                    }
                }
            }
            
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