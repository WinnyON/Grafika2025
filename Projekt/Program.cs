using ImGuiNET;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.SDL;
using Silk.NET.Vulkan;
using Silk.NET.Windowing;
using System.Numerics;
using System.Reflection;
using System.Threading.Tasks.Dataflow;
using static System.Net.Mime.MediaTypeNames;
using Window = Silk.NET.Windowing.Window;

namespace Projekt
{
    internal static class Program
    {

        private static double animationPercentage = 0;
        private static bool gameOver = false;
        private static bool victory = false;
        private static double gameTime = 0;


        private static AudioPlayer audioPlayer;
        
        private static int cameraMode = 1;
        private static CameraDescriptor cameraDescriptor = new();
        private static PacmanDescriptor pacmanDescriptor = new();
        private static Vector3D<float> cameraHeight = new(0, 1.5f, 0);
        private static float pacmanRotation = 0;

        private static CubeArrangementModel cubeArrangementModel = new();

        private static IWindow window;

        private static IInputContext inputContext;
        private static IKeyboard _keyboard;
        private static IMouse mouse;

        private static GL Gl;

        private static ImGuiController controller;

        private static uint program;

        private static Map maze;


        private static GlObject wall;
        private static GlObject skybox;
        private static GlObject pacman;
        private static List<GlObject> ghosts;
        private static GlObject ground;
        private static GlObject bonus;
        private static List<Matrix4X4<float>> groundTransformations;
        private static List<Matrix4X4<float>> wallTransformations;
        private static List<Vector2D<float>> exitPositions;
        private static List<Vector2D<float>> wallPositions;
        private static List<Matrix4X4<float>> bonusTransformations;
        private static List<Vector3D<float>> bonusPositions;
        private static bool[] bonusNotAvailable;
        private static int bonusesLeft;
        private static Vector3D<float> pacmanPosition;
        private static List<Vector2D<int>> ghostPositions;
        private static List<Vector2D<int>> ghostNextPositions;
        private static bool[] ghostsDead;

        private static GlObject table;

        private static GlCube glCubeRotating;

        private static float Shininess = 50;

        private const string ModelMatrixVariableName = "uModel";
        private const string NormalMatrixVariableName = "uNormal";
        private const string ViewMatrixVariableName = "uView";
        private const string ProjectionMatrixVariableName = "uProjection";
        private const string TextureVariableName = "uTexture";

        private const string LightColorVariableName = "lightColor";
        private const string LightPositionVariableName = "lightPos";
        private const string ViewPosVariableName = "viewPos";
        private const string ShininessVariableName = "shininess";
        private const string AmbientStrengthVariableName = "uAmbientStrength";
        private const string DiffuseStrengthVariableName = "uDiffuseStrength";
        private const string SpecularStrengthVariableName = "uSpecularStrength";

        static void Main(string[] args)
        {
            WindowOptions windowOptions = WindowOptions.Default;
            windowOptions.Title = "Projekt";
            windowOptions.Size = new Vector2D<int>(1000, 1000);

            // on some systems there is no depth buffer by default, so we need to make sure one is created
            windowOptions.PreferredDepthBufferBits = 24;

            window = Window.Create(windowOptions);

            window.Load += Window_Load;
            window.Update += Window_Update;
            window.Render += Window_Render;
            window.Closing += Window_Closing;

            window.Run();
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
        private static void Window_Load()
        {
            //Console.WriteLine("Load");

            // set up input handling
            Gl = window.CreateOpenGL();
            inputContext = window.CreateInput();
            _keyboard = inputContext.Keyboards[0]; // ez lehet nem jo
            //var inputContext = _inputContext; //graphicWindow.CreateInput();
            foreach (var keyboard in inputContext.Keyboards)
            {
                keyboard.KeyDown += Keyboard_KeyDown;
            }

            mouse = inputContext.Mice[0];
            mouse.MouseMove += OnMouseMove;
            mouse.Cursor.CursorMode = CursorMode.Raw;

            controller = new ImGuiController(Gl, window, inputContext);

            // Handle resizes
            window.FramebufferResize += s =>
            {
                // Adjust the viewport to the new window size
                Gl.Viewport(s);
            };


            Gl.ClearColor(System.Drawing.Color.Black);

            SetUpObjects();

            LinkProgram();

            //Gl.Enable(EnableCap.CullFace);

            Gl.Enable(EnableCap.DepthTest);
            Gl.DepthFunc(DepthFunction.Lequal);

            audioPlayer = new AudioPlayer();
            audioPlayer.PlayBackgroundMusic("Resources/background_music.mp3");
        }

        private static void OnMouseMove(IMouse mouse, Vector2 position)
        {
            //Console.WriteLine($"Mouse moved to: {position.X}, {position.Y}");
            if (cameraMode == 0)
            {
                cameraDescriptor.MouseMove(position.X, position.Y);
            }
            else if (cameraMode == 1 || cameraMode == 3)
            {
                pacmanDescriptor.MouseMove(position.X, position.Y, cameraMode);
            }
            //pacmanDescriptor.UpdateCamera(mousePosition.X - position.X, mousePosition.Y - position.Y);
            //mousePosition = position;
        }

        private static void LinkProgram()
        {
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
            Gl.GetProgram(program, GLEnum.LinkStatus, out var status);
            if (status == 0)
            {
                Console.WriteLine($"Error linking shader {Gl.GetProgramInfoLog(program)}");
            }
            Gl.DetachShader(program, vshader);
            Gl.DetachShader(program, fshader);
            Gl.DeleteShader(vshader);
            Gl.DeleteShader(fshader);
        }

        private static void SetCursorMode()
        {
            if (mouse.Cursor.CursorMode == CursorMode.Raw)
            {
                mouse.Cursor.CursorMode = CursorMode.Normal;
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
                case Key.Keypad0:
                    cameraMode = 0;
                    break;
                case Key.Keypad1:
                    cameraMode = 1;
                    break;
                case Key.Keypad2:
                    cameraMode = 2;
                    pacmanDescriptor.ResetCamera();
                    break;
                case Key.Keypad3:
                    cameraMode = 3;
                    pacmanDescriptor.ResetCamera();
                    break;
                case Key.Space:
                    cubeArrangementModel.AnimationEnabeld = !cubeArrangementModel.AnimationEnabeld;
                    break;
            }
        }
        private static void handleMovement()
        {
            if (cameraMode == 0)
            {
                if (_keyboard.IsKeyPressed(Key.A))
                {
                    cameraDescriptor.Move(Key.A);
                }
                if (_keyboard.IsKeyPressed(Key.D))
                {
                    cameraDescriptor.Move(Key.D);
                }
                if (_keyboard.IsKeyPressed(Key.W))
                {
                    cameraDescriptor.Move(Key.W);
                }
                if (_keyboard.IsKeyPressed(Key.S))
                {
                    cameraDescriptor.Move(Key.S);
                }
            }
            else if (cameraMode == 1)
            {
                if (_keyboard.IsKeyPressed(Key.A))
                {
                    pacmanRotation = 0;
                    pacmanDescriptor.Move(Key.A, wallPositions, exitPositions, bonusesLeft);
                }
                if (_keyboard.IsKeyPressed(Key.D))
                {
                    pacmanRotation = (float)Math.PI;
                    pacmanDescriptor.Move(Key.D, wallPositions, exitPositions, bonusesLeft);
                }
                if (_keyboard.IsKeyPressed(Key.W))
                {
                    pacmanRotation = -(float)Math.PI / 2;
                    pacmanDescriptor.Move(Key.W, wallPositions, exitPositions, bonusesLeft);
                }
                //if (_keyboard.IsKeyPressed(Key.S))
                //{
                //    pacmanRotation = (float)Math.PI / 2;
                //    pacmanDescriptor.Move(Key.S, wallPositions, exitPositions, bonusesLeft);
                //}
            }
            else if (cameraMode == 2 || cameraMode == 3)
            {
                if (_keyboard.IsKeyPressed(Key.A))
                {
                    pacmanRotation = 0;
                    pacmanDescriptor.Move(Key.A, wallPositions, exitPositions, bonusesLeft);
                }
                if (_keyboard.IsKeyPressed(Key.D))
                {
                    pacmanRotation = (float)Math.PI;
                    pacmanDescriptor.Move(Key.D, wallPositions, exitPositions, bonusesLeft);
                }
                if (_keyboard.IsKeyPressed(Key.W))
                {
                    pacmanRotation = -(float)Math.PI / 2;
                    pacmanDescriptor.Move(Key.W, wallPositions, exitPositions, bonusesLeft);
                }
                if (_keyboard.IsKeyPressed(Key.S))
                {
                    pacmanRotation = (float)Math.PI / 2;
                    pacmanDescriptor.Move(Key.S, wallPositions, exitPositions, bonusesLeft);
                }
            }
        }


        private static void AdvanceGhostAnimation(double deltaTime)
        {
            if (!gameOver)
            {
                animationPercentage += deltaTime;
                if (animationPercentage >= 1f)
                {
                    maze.MoveGhosts(ghostPositions, ghostNextPositions);
                    ghostPositions = ghostNextPositions;
                    ghostNextPositions = maze.GetGhostNextPositions();
                    animationPercentage = 0;
                }
            }
        }

        private static void UpdatePacmanPosition()
        {
            if (!gameOver)
            {
                maze.Maze[maze.pi, maze.pj] = 0;
                maze.pi = (int)Math.Ceiling((pacmanDescriptor.Position.X - Map.Offset) / 2);
                maze.pj = (int)Math.Ceiling((pacmanDescriptor.Position.Z - Map.Offset) / 2);
                //Console.WriteLine(maze.pi);
                //Console.WriteLine(maze.pj);
                if (maze.pi < 0 || maze.pi >= maze.Maze.GetLength(0) || maze.pj < 0 || maze.pj >= maze.Maze.GetLength(1))
                {
                    victory = true;
                    gameOver = true;
                    return;
                }
                if (maze.Maze[maze.pi, maze.pj] == 0)
                {
                    maze.Maze[maze.pi, maze.pj] = -1;
                }
                else
                {
                    if(maze.pi - 1 < 0 || maze.pj - 1 < 0)
                    {
                        victory = true;
                        gameOver = true;
                        return;
                    }
                    if (maze.Maze[maze.pi - 1, maze.pj] == 0)
                    {
                        maze.pi--;
                        maze.Maze[maze.pi, maze.pj] = -1;
                    }
                    else if (maze.Maze[maze.pi, maze.pj - 1] == 0)
                    {
                        maze.pj--;
                        maze.Maze[maze.pi, maze.pj] = -1;
                    }
                    else if (maze.Maze[maze.pi - 1, maze.pj - 1] == 0)
                    {
                        maze.pi--;
                        maze.pj--;
                        maze.Maze[maze.pi, maze.pj] = -1;
                    }
                }
            }
        }

        private static void Window_Update(double deltaTime)
        {
            //Console.WriteLine($"Update after {deltaTime} [s].");
            // multithreaded
            // make sure it is threadsafe
            // NO GL calls
            if (!gameOver)
            {
                gameTime += deltaTime;
                cubeArrangementModel.AdvanceTime(deltaTime);
                AdvanceGhostAnimation(deltaTime); 
                handleMovement();
                UpdatePacmanPosition();
            }
            controller.Update((float)deltaTime);
        }

        private static unsafe void Window_Render(double deltaTime)
        {
            //Console.WriteLine($"Render after {deltaTime} [s].");

            // GL here
            Gl.Clear(ClearBufferMask.ColorBufferBit);
            Gl.Clear(ClearBufferMask.DepthBufferBit);


            Gl.UseProgram(program);

            SetViewMatrix();
            SetProjectionMatrix();

            SetLightColor();
            SetLightPosition();
            SetViewerPosition();
            SetShininess();
            SetLightStrength(0.2f, 0.3f, 0.5f);

            //DrawPulsingObject();
            DrawBonuses();
            DrawGhosts();

            DrawSkyBox();

            SetLightStrength(1f, 1f, 1f);
            DrawPacman();

            SetLightStrength(0.3f, 0.2f, 0f);
            DrawGround();
            DrawWalls();

            //ImGuiNET.ImGui.ShowDemoWindow();
            if (!gameOver)
            {
                ImGuiNET.ImGui.Begin("Settings",
                    ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar);
                ImGuiNET.ImGui.Combo("Camera Mode", ref cameraMode, "Creative\0Third Person\0Classic\0First Person");
                ImGuiNET.ImGui.End();
            }
            else
            {
                RenderGameOverWindow();
            }

            if (mouse.Cursor.CursorMode == CursorMode.Normal || gameOver)
            {
                controller.Render();
            }
        }


        private static void RenderGameOverWindow()
        {
            mouse.Cursor.CursorMode = CursorMode.Normal;
            // Set the window to be centered and non-resizable
            ImGui.SetNextWindowPos(new Vector2(window.Size.X / 2, window.Size.Y / 2),
                                  ImGuiCond.Always, new Vector2(0.5f, 0.5f));
            ImGui.SetNextWindowSize(new Vector2(300, 200), ImGuiCond.Always);

            // Begin the game over window
            if (ImGui.Begin("Game Over", ref gameOver,
                            ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse))
            {
                if (!victory)
                {
                    // Center the text horizontally
                    ImGui.SetCursorPosX((300 - ImGui.CalcTextSize("GAME OVER").X) * 0.5f);

                    // Big red "GAME OVER" text
                    ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 0.0f, 0.0f, 1.0f));
                    ImGui.Text("GAME OVER");
                    ImGui.PopStyleColor();
                }
                else
                {
                    // Center the text horizontally
                    ImGui.SetCursorPosX((300 - ImGui.CalcTextSize("VICTORY").X) * 0.5f);

                    ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.0f, 1.0f, 0.0f, 1.0f));
                    ImGui.Text("VICTORY");
                    ImGui.PopStyleColor();
                }

                    ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();

                // Display score or other game stats
                ImGui.Text("Final Score: " + (gameTime * 100 + bonusPositions.Count));
                ImGui.Text("Time Survived: " + gameTime + "s");

                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();

                // Center the buttons
                float buttonWidth = 100;
                float buttonSpacing = (300 - buttonWidth) / 3;

                ImGui.SetCursorPosX(buttonSpacing);
                //if (ImGui.Button("Restart", new Vector2(buttonWidth, 30)))
                //{
                //    //_restartGame = true;
                //    gameOver = false;
                //    victory = false;
                //    // Here you would typically reset your game state
                //}

                //ImGui.SameLine(buttonSpacing * 2 + buttonWidth);
                if (ImGui.Button("Exit", new Vector2(buttonWidth, 30)))
                {
                    //_exitGame = true;
                    window.Close();
                }
            }
            ImGui.End();
        }

        private static unsafe void SetLightColor()
        {
            int location = Gl.GetUniformLocation(program, LightColorVariableName);

            if (location == -1)
            {
                throw new Exception($"{LightColorVariableName} uniform not found on shader.");
            }

            Gl.Uniform3(location, 1f, 1f, 1f);
            CheckError();
        }

        private static unsafe void SetLightStrength(float ambient, float diffuse, float specular)
        {

            int location = Gl.GetUniformLocation(program, AmbientStrengthVariableName);

            if (location == -1)
            {
                throw new Exception($"{AmbientStrengthVariableName} uniform not found on shader.");
            }

            Gl.Uniform3(location, ambient, ambient, ambient);
            CheckError();

            location = Gl.GetUniformLocation(program, DiffuseStrengthVariableName);

            if (location == -1)
            {
                throw new Exception($"{DiffuseStrengthVariableName} uniform not found on shader.");
            }

            Gl.Uniform3(location, diffuse, diffuse, diffuse);
            CheckError();

            location = Gl.GetUniformLocation(program, SpecularStrengthVariableName);

            if (location == -1)
            {
                throw new Exception($"{SpecularStrengthVariableName} uniform not found on shader.");
            }

            Gl.Uniform3(location, specular, specular, specular);
            CheckError();
        }

        private static unsafe void SetLightPosition()
        {
            int location = Gl.GetUniformLocation(program, LightPositionVariableName);

            if (location == -1)
            {
                throw new Exception($"{LightPositionVariableName} uniform not found on shader.");
            }

            Gl.Uniform3(location, 0f, 10f, 10f);
            CheckError();
        }

        private static unsafe void SetViewerPosition()
        {
            int location = Gl.GetUniformLocation(program, ViewPosVariableName);

            if (location == -1)
            {
                throw new Exception($"{ViewPosVariableName} uniform not found on shader.");
            }
            Gl.Uniform3(location, cameraDescriptor.Position.X, cameraDescriptor.Position.Y, cameraDescriptor.Position.Z);
            CheckError();
        }

        private static unsafe void SetShininess()
        {
            int location = Gl.GetUniformLocation(program, ShininessVariableName);

            if (location == -1)
            {
                throw new Exception($"{ShininessVariableName} uniform not found on shader.");
            }

            Gl.Uniform1(location, Shininess);
            CheckError();
        }

        private static unsafe void DrawWalls()
        {
            int textureLocation = Gl.GetUniformLocation(program, TextureVariableName);
            if (textureLocation == -1)
            {
                throw new Exception($"{TextureVariableName} uniform not found on shader.");
            }
            
            for (int i = 0; i < wallTransformations.Count; i++)
            {
                Gl.Uniform1(textureLocation, 0);
                Gl.ActiveTexture(TextureUnit.Texture0);
                Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (float)GLEnum.Linear);
                Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (float)GLEnum.Linear);
                Gl.BindTexture(TextureTarget.Texture2D, wall.Texture.Value);
                SetModelMatrix(wallTransformations[i]);
                DrawModelObject(wall);
                CheckError();
                Gl.BindTexture(TextureTarget.Texture2D, 0);
                CheckError();
            }
        }

        private static unsafe void DrawGround()
        {
            int textureLocation = Gl.GetUniformLocation(program, TextureVariableName);
            if (textureLocation == -1)
            {
                throw new Exception($"{TextureVariableName} uniform not found on shader.");
            }

            for (int i = 0; i < groundTransformations.Count; i++)
            {
                Gl.Uniform1(textureLocation, 0);
                Gl.ActiveTexture(TextureUnit.Texture0);
                Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (float)GLEnum.Linear);
                Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (float)GLEnum.Linear);
                Gl.BindTexture(TextureTarget.Texture2D, ground.Texture.Value);
                SetModelMatrix(groundTransformations[i]);
                DrawModelObject(ground);
                CheckError();
                Gl.BindTexture(TextureTarget.Texture2D, 0);
                CheckError();
            }
        }

        private static unsafe void DrawPacman()
        {
            if(cameraMode == 3)
            {
                return;
            }
            //var cpos = cameraDescriptor.targetPosition;
            //var trans2 = Matrix4X4.CreateRotationY((float)Math.PI / -2) * Matrix4X4.CreateTranslation(cpos.X, cpos.Y - 8f, cpos.Z - 5f);
            Matrix4X4<float> trans = Matrix4X4.CreateTranslation(pacmanDescriptor.Position.X, 1.1f * (float)cubeArrangementModel.CenterCubeScale, pacmanDescriptor.Position.Z); // pacmanDescriptor.Position.Y
            Matrix4X4<float> rot;
            if (cameraMode == 2)
            {
                rot = Matrix4X4.CreateRotationY(pacmanRotation);
            }
            else
            {
                rot = Matrix4X4.CreateRotationY((float)Math.PI / 180 * (-pacmanDescriptor.Yaw + 180));
            }
            var modelMatrixSkyBox = rot * Matrix4X4.CreateScale(0.8f) * trans;
            SetModelMatrix(modelMatrixSkyBox);

            // set the texture
            int textureLocation = Gl.GetUniformLocation(program, TextureVariableName);
            if (textureLocation == -1)
            {
                throw new Exception($"{TextureVariableName} uniform not found on shader.");
            }
            // set texture 0
            Gl.Uniform1(textureLocation, 0);
            Gl.ActiveTexture(TextureUnit.Texture0);
            Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (float)GLEnum.Linear);
            Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (float)GLEnum.Linear);
            Gl.BindTexture(TextureTarget.Texture2D, pacman.Texture.Value);

            DrawModelObject(pacman);

            CheckError();
            Gl.BindTexture(TextureTarget.Texture2D, 0);
            CheckError();
        }

        private static unsafe void DrawSkyBox()
        {
            var modelMatrixSkyBox = Matrix4X4.CreateScale(200f);
            SetModelMatrix(modelMatrixSkyBox);

            // set the texture
            int textureLocation = Gl.GetUniformLocation(program, TextureVariableName);
            if (textureLocation == -1)
            {
                throw new Exception($"{TextureVariableName} uniform not found on shader.");
            }
            // set texture 0
            Gl.Uniform1(textureLocation, 0);
            Gl.ActiveTexture(TextureUnit.Texture0);
            Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (float)GLEnum.Linear);
            Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (float)GLEnum.Linear);
            Gl.BindTexture(TextureTarget.Texture2D, skybox.Texture.Value);

            DrawModelObject(skybox);

            CheckError();
            Gl.BindTexture(TextureTarget.Texture2D, 0);
            CheckError();
        }

        private static unsafe void DrawModelObject(GlObject modelObject)
        {
            Gl.BindVertexArray(modelObject.Vao);
            Gl.BindBuffer(GLEnum.ElementArrayBuffer, modelObject.Indices);
            Gl.DrawElements(PrimitiveType.Triangles, modelObject.IndexArrayLength, DrawElementsType.UnsignedInt, null);
            Gl.BindBuffer(GLEnum.ElementArrayBuffer, 0);
            Gl.BindVertexArray(0);
        }

        private static bool CheckBonusCollision(Vector3D<float> bonus)
        {
            var dx = pacmanDescriptor.Position.X - bonus.X;
            var dy = pacmanDescriptor.Position.Y - bonus.Y;
            var dz = pacmanDescriptor.Position.Z - bonus.Z;
            return (dx * dx + dy * dy + dz * dz) < (1.1f + 0.5f) * (1.1f + 0.5f);
        }

        private static unsafe void DrawBonuses()
        {
            int textureLocation = Gl.GetUniformLocation(program, TextureVariableName);
            if (textureLocation == -1)
            {
                throw new Exception($"{TextureVariableName} uniform not found on shader.");
            }
            var scale = Matrix4X4.CreateScale((float)cubeArrangementModel.CenterCubeScale);
            for (int i = 0; i < bonusTransformations.Count; i++)
            {
                if (!bonusNotAvailable[i] && CheckBonusCollision(bonusPositions[i]))
                {
                    bonusNotAvailable[i] = true;
                    bonusesLeft--;
                }
                if (!bonusNotAvailable[i])
                {
                    Gl.Uniform1(textureLocation, 0);
                    Gl.ActiveTexture(TextureUnit.Texture0);
                    Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (float)GLEnum.Linear);
                    Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (float)GLEnum.Linear);
                    Gl.BindTexture(TextureTarget.Texture2D, bonus.Texture.Value);
                    SetModelMatrix(scale * bonusTransformations[i]);
                    DrawModelObject(bonus);
                    CheckError();
                    Gl.BindTexture(TextureTarget.Texture2D, 0);
                    CheckError();
                }
            }
        }

        private static bool CheckGhostCollision(Vector3D<float> ghostLocation)
        {
            var dx = pacmanDescriptor.Position.X - ghostLocation.X;
            var dz = pacmanDescriptor.Position.Z - ghostLocation.Z;
            return (dx * dx + dz * dz) < (1.1f + 1f) * (1.1f + 1f);
        }

        private static (Vector3D<float>, float) GetGhostLocation(Vector2D<int> position, Vector2D<int> nextPosition)
        {
            //Console.WriteLine(position + " -> " + nextPosition);
            if (animationPercentage > 1)
            {
                animationPercentage = 1;
            }

            if (position.X > nextPosition.X)
            {
                return (new(Map.Offset + (float)position.X * 2f + 1f - (float)animationPercentage * 2f, 1f, Map.Offset + (float)position.Y * 2f - 1f), (float)Math.PI);
            }
            else if (position.X < nextPosition.X)
            {
                return (new(Map.Offset + (float)position.X * 2f + 1f + (float)animationPercentage * 2f, 1f, Map.Offset + (float)position.Y * 2f - 1f), 0f);
            }
            else if (position.Y > nextPosition.Y)
            {
                return (new(Map.Offset + (float)position.X * 2f + 1f, 1f, Map.Offset + (float)position.Y * 2f - 1f - (float)animationPercentage * 2f), (float)Math.PI / 2);
            }
            else if (position.Y < nextPosition.Y)
            {
                return (new(Map.Offset + (float)position.X * 2f + 1f, 1f, Map.Offset + (float)position.Y * 2f - 1f + (float)animationPercentage * 2f), -(float)Math.PI / 2);
            }
            return (new(Map.Offset + (float)position.X * 2f + 1f, 1f, Map.Offset + (float)position.Y * 2f - 1f), 0f);
        }

        private static unsafe void DrawGhosts()
        {
            int textureLocation = Gl.GetUniformLocation(program, TextureVariableName);
            if (textureLocation == -1)
            {
                throw new Exception($"{TextureVariableName} uniform not found on shader.");
            }
            for (int i = 0; i < ghostPositions.Count; i++)
            {
                var pair = GetGhostLocation(ghostPositions[i], ghostNextPositions[i]);
                var pos = pair.Item1;
                var transl = Matrix4X4.CreateTranslation(pos.X, pos.Y, pos.Z);
                var rot = Matrix4X4.CreateRotationY(pair.Item2);

                // solve the gradual movement somehow
                if (CheckGhostCollision(pos))
                {
                    gameOver = true;
                    //Console.WriteLine("vege");
                }
                Gl.Uniform1(textureLocation, 0);
                Gl.ActiveTexture(TextureUnit.Texture0);
                Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (float)GLEnum.Linear);
                Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (float)GLEnum.Linear);
                Gl.BindTexture(TextureTarget.Texture2D, ghosts[i%4].Texture.Value);
                SetModelMatrix(rot * transl);
                DrawModelObject(ghosts[i%4]);
                CheckError();
                Gl.BindTexture(TextureTarget.Texture2D, 0);
                CheckError();
            }
        }

        private static unsafe void SetModelMatrix(Matrix4X4<float> modelMatrix)
        {
            int location = Gl.GetUniformLocation(program, ModelMatrixVariableName);
            if (location == -1)
            {
                throw new Exception($"{ModelMatrixVariableName} uniform not found on shader.");
            }

            Gl.UniformMatrix4(location, 1, false, (float*)&modelMatrix);
            CheckError();

            var modelMatrixWithoutTranslation = new Matrix4X4<float>(modelMatrix.Row1, modelMatrix.Row2, modelMatrix.Row3, modelMatrix.Row4);
            modelMatrixWithoutTranslation.M41 = 0;
            modelMatrixWithoutTranslation.M42 = 0;
            modelMatrixWithoutTranslation.M43 = 0;
            modelMatrixWithoutTranslation.M44 = 1;

            Matrix4X4<float> modelInvers;
            Matrix4X4.Invert<float>(modelMatrixWithoutTranslation, out modelInvers);
            Matrix3X3<float> normalMatrix = new Matrix3X3<float>(Matrix4X4.Transpose(modelInvers));
            location = Gl.GetUniformLocation(program, NormalMatrixVariableName);
            if (location == -1)
            {
                throw new Exception($"{NormalMatrixVariableName} uniform not found on shader.");
            }
            Gl.UniformMatrix3(location, 1, false, (float*)&normalMatrix);
            CheckError();
        }

        private static unsafe void SetUpObjects()
        {
            maze = new Map("maze.txt");
            wall = ObjResourceReader.CreateObjectFromResource(Gl, "wall4.obj", "wall.jpg");
            wallTransformations = maze.GetTransformations();
            wallPositions = maze.GetWallPositions();
            exitPositions = maze.GetExitPositions();

            float[] face1Color = [1f, 0f, 0f, 1.0f];
            float[] face2Color = [0.0f, 1.0f, 0.0f, 1.0f];
            float[] face3Color = [0.0f, 0.0f, 1.0f, 1.0f];
            float[] face4Color = [1.0f, 0.0f, 1.0f, 1.0f];
            float[] face5Color = [0.0f, 1.0f, 1.0f, 1.0f];
            float[] face6Color = [1.0f, 1.0f, 0.0f, 1.0f];

            bonus = ObjResourceReader.CreateObjectWithColor(Gl, face1Color, "bonus_ball.obj");
            bonusTransformations = maze.GetBonusTransformations();
            bonusPositions = maze.GetBonusPositions();
            bonusNotAvailable = new bool[bonusPositions.Count];
            bonusesLeft = bonusPositions.Count;

            float[] tableColor = [System.Drawing.Color.Azure.R/256f,
                                  System.Drawing.Color.Azure.G/256f,
                                  System.Drawing.Color.Azure.B/256f,
                                  1f];
            table = GlCube.CreateSquare(Gl, tableColor);

            glCubeRotating = GlCube.CreateCubeWithFaceColors(Gl, face1Color, face2Color, face3Color, face4Color, face5Color, face6Color);

            skybox = ObjResourceReader.CreateSkybox(Gl, "skybox.png");
            pacman = ObjResourceReader.CreateObjectFromResource(Gl, "pacman.obj", "pacman.png");
            pacmanDescriptor.cameraPosition = maze.GetPacmanPosition();
            ghosts = new List<GlObject>();
            ghosts.Add(ObjResourceReader.CreateObjectFromResource(Gl, "ghost.obj", "ghost_blue.png"));
            ghosts.Add(ObjResourceReader.CreateObjectFromResource(Gl, "ghost.obj", "ghost_pink.png"));
            ghosts.Add(ObjResourceReader.CreateObjectFromResource(Gl, "ghost.obj", "ghost_red.png"));
            ghosts.Add(ObjResourceReader.CreateObjectFromResource(Gl, "ghost.obj", "ghost_grey.png"));
            ghostPositions = maze.GetGhostPositions();
            ghostNextPositions = maze.GetGhostNextPositions();
            ghostsDead = new bool[ghostPositions.Count];
            ground = ObjResourceReader.CreateObjectFromResource(Gl, "ground.obj", "ground.jpg");
            groundTransformations = maze.GetGroundTransformations();
        }

        

        private static void Window_Closing()
        {
            bonus.ReleaseGlObject();
            glCubeRotating.ReleaseGlObject();
            skybox.ReleaseGlObject();
            pacman.ReleaseGlObject();
            ground.ReleaseGlObject();
            wall.ReleaseGlObject();
            audioPlayer?.Dispose();
        }

        private static unsafe void SetProjectionMatrix()
        {
            var projectionMatrix = Matrix4X4.CreatePerspectiveFieldOfView<float>((float)Math.PI / 4f, 1024f / 768f, 0.1f, 1000);
            int location = Gl.GetUniformLocation(program, ProjectionMatrixVariableName);

            if (location == -1)
            {
                throw new Exception($"{ViewMatrixVariableName} uniform not found on shader.");
            }

            Gl.UniformMatrix4(location, 1, false, (float*)&projectionMatrix);
            CheckError();
        }

        private static unsafe void SetViewMatrix()
        {
            //var viewMatrix = Matrix4X4.CreateLookAt(camera.Position, pacmanPosition, camera.UpVector);
            //var pos = -2 * pacmanDescriptor.Position + pacmanDescriptor.Target + cameraHeight;
            Matrix4X4<float> viewMatrix;
            if (cameraMode == 0)
            {
                viewMatrix = cameraDescriptor.View;
            }
            else if (cameraMode == 1)
            {
                cameraDescriptor.cameraPosition = pacmanDescriptor.Position - pacmanDescriptor.Front * 10f + cameraHeight;
                cameraDescriptor.cameraFront = Vector3D.Normalize(pacmanDescriptor.Position - cameraDescriptor.Position);
                cameraDescriptor.cameraRight = Vector3D.Normalize(Vector3D.Cross(cameraDescriptor.cameraFront, pacmanDescriptor.cameraUp));
                cameraDescriptor.cameraUp = Vector3D.Normalize(Vector3D.Cross(cameraDescriptor.cameraRight, cameraDescriptor.cameraFront));
                viewMatrix = Matrix4X4.CreateLookAt(cameraDescriptor.cameraPosition, pacmanDescriptor.Position, cameraDescriptor.cameraUp);
            }
            else if (cameraMode == 2)
            {
                cameraDescriptor.cameraPosition = new Vector3D<float>(0f, 80f, 20f);
                var target = new Vector3D<float>(0f, 0f, 0f);
                viewMatrix = Matrix4X4.CreateLookAt(cameraDescriptor.cameraPosition, target, new Vector3D<float>(0f, 1f, 0f));

            }
            else if(cameraMode == 3)
            {
                viewMatrix = Matrix4X4.CreateLookAt(pacmanDescriptor.Position, pacmanDescriptor.Target, pacmanDescriptor.cameraUp);
            }

                int location = Gl.GetUniformLocation(program, ViewMatrixVariableName);

            if (location == -1)
            {
                throw new Exception($"{ViewMatrixVariableName} uniform not found on shader.");
            }

            Gl.UniformMatrix4(location, 1, false, (float*)&viewMatrix);
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