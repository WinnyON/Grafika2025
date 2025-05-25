using Silk.NET.Maths;
using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace Projekt
{
    class Map
    {
        public int[,] Maze { get; set; }
        public int rows { get; private set; }
        public int cols { get; private set; }
        public int pi { get; set; } = 0;
        public int pj { get; set; } = 0;
        public static float Offset = -25f;
        private List<(int, int)> ghosts;
        public Map(string fileName)
        {
            string fullFilePath = "Projekt.Resources." + fileName;
            this.Maze = ReadMazeFromFile(fullFilePath);
            GenerateBonuses(50);
        }

        private bool IsValid(int i, int j)
        {
            if(i < 1 || j < 1 || j == cols - 1 || i == rows - 1)
            {
                return false;
            }
            return !(Maze[i - 1, j] > 0 || Maze[i + 1, j] > 0 || Maze[i, j - 1] > 0 || Maze[i, j + 1] > 0
                     || Maze[i-1, j-1] > 0 || Maze[i-1, j+1] > 0 || Maze[i+1, j-1] > 0 || Maze[i+1, j+1] > 0);
        }

        private void GenerateBonuses(int bonuses)
        {
            Random random = new Random();
            int maxIter = 20000;
            int i = 0;
            while(i < maxIter && bonuses > 0)
            {
                int x = random.Next(0, rows);
                int y = random.Next(0, cols);
                if (Maze[x, y] == 0 && IsValid(x, y))
                {
                    Maze[x, y] = 2;
                    bonuses--;
                }
                i++;
            }
        }

        private int[,] ReadMazeFromFile(string filePath)
        {
            var assembly = Assembly.GetExecutingAssembly();
            using (Stream stream = assembly.GetManifestResourceStream(filePath))
            using (StreamReader reader = new StreamReader(stream))
            {
                string[] lines = reader.ReadToEnd().Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                int[][] mazeData = lines
                                  .Select(line => line
                                      .Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                                      .Select(int.Parse)
                                      .ToArray()
                                  )
                                  .ToArray();

                rows = mazeData.Length;
                cols = mazeData[0].Length;
                int[,] maze = new int[rows, cols];
                for (int i = 0; i < rows; i++)
                {
                    for (int j = 0; j < cols; j++)
                    {
                        maze[i, j] = mazeData[i][j];
                        if (maze[i, j] == -1)
                        {
                            pi = i;
                            pj = j;
                        }
                    }
                }
                return maze;
            }
        }

        public void CreateMazeFromFile(string fileName)
        {
            String fullFilePath = "Projekt.Resources." + fileName;
            this.Maze = ReadMazeFromFile(fullFilePath);
        }

        public List<GlObject> GetWallObjects(GL Gl)
        {
            List<GlObject> wallObjects = new List<GlObject>();
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    if (Maze[i, j] == 1)
                    {
                        GlObject wall = ObjResourceReader.CreateObjectFromResource(Gl, "wall.obj", "wall.png");
                        wallObjects.Add(wall);
                    }
                }
            }
            return wallObjects;
        }

        

        public List<Matrix4X4<float>> GetTransformations()
        {
            float offset = -25f;
            List<Matrix4X4<float>> transformations = new List<Matrix4X4<float>>();
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    //Console.WriteLine(Maze[i, j]);
                    if (Maze[i, j] == 1)
                    {
                        
                        transformations.Add(Matrix4X4.CreateTranslation(offset + (float)i * 2f, 0f, offset + (float)j * 2f));
                    }
                }
            }
            return transformations;
        }

        public List<Vector2D<float>> GetExitPositions()
        {
            float offset = -25f;
            List<Vector2D<float>> transformations = new List<Vector2D<float>>();
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    //Console.WriteLine(Maze[i, j]);
                    if (Maze[i, j] == 3)
                    {
                        transformations.Add(new(offset + (float)i * 2f, offset + (float)j * 2f));
                    }
                }
            }
            return transformations;
        }

        public List<Vector2D<float>> GetWallPositions()
        {
            float offset = -25f;
            List<Vector2D<float>> transformations = new List<Vector2D<float>>();
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    //Console.WriteLine(Maze[i, j]);
                    if (Maze[i, j] == 1)
                    {
                        transformations.Add(new (offset + (float)i * 2f, offset + (float)j * 2f));
                    }
                }
            }
            return transformations;
        }

        public List<Vector2D<int>> GetGhostPositions()
        {
            ghosts = new List<(int, int)>();
            float offset = -25f;
            List<Vector2D<int>> transformations = new List<Vector2D<int>>();
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    //Console.WriteLine(Maze[i, j]);
                    if (Maze[i, j] == -2)
                    {
                        transformations.Add(new(i, j));
                        ghosts.Add((i, j));
                    }
                }
            }
            return transformations;
        }

        private static readonly int[] DirectionsX = { -1, 1, 0, 0 };
        private static readonly int[] DirectionsY = { 0, 0, -1, 1 };

        public static List<(int, int)> FindShortestPath(int[,] matrix, (int, int) start, (int, int) end)
        {
            int rows = matrix.GetLength(0);
            int cols = matrix.GetLength(1);

            if (matrix[start.Item1, start.Item2] == 1 || matrix[end.Item1, end.Item2] == 1)
            {
                return null;
            }

            Queue<(int, int)> queue = new Queue<(int, int)>();
            queue.Enqueue(start);

            bool[,] visited = new bool[rows, cols];
            visited[start.Item1, start.Item2] = true;

            Dictionary<(int, int), (int, int)> parent = new Dictionary<(int, int), (int, int)>();

            bool found = false;

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (current.Item1 == end.Item1 && current.Item2 == end.Item2)
                {
                    found = true;
                    break;
                }
                for (int i = 0; i < 4; i++)
                {
                    int newX = current.Item1 + DirectionsX[i];
                    int newY = current.Item2 + DirectionsY[i];
                    if (newX >= 0 && newX < rows && newY >= 0 && newY < cols &&
                        (matrix[newX, newY] == 0 || (newX == end.Item1 && newY == end.Item2)) && !visited[newX, newY])
                    {
                        visited[newX, newY] = true;
                        queue.Enqueue((newX, newY));
                        parent[(newX, newY)] = current;
                    }
                }
            }
            if (found)
            {
                List<(int, int)> path = new List<(int, int)>();
                (int, int)? current = end;

                while (current != null && current.Value != start)
                {
                    path.Add(current.Value);
                    current = parent.ContainsKey(current.Value) ? parent[current.Value] : (null as (int, int)?);
                }
                path.Add(start);
                path.Reverse();
                return path;
            }

            return null;
        }


        public List<Vector2D<int>> GetGhostNextPositions()
        {
            float offset = -25f;
            List<Vector2D<int>> transformations = new List<Vector2D<int>>();
            for(int k = 0; k < ghosts.Count; k++)
            {
                int i = ghosts[k].Item1;
                int j = ghosts[k].Item2;
                var path = FindShortestPath(Maze, (i, j), (pi, pj));
                //Console.WriteLine(path[0] + " -> " + path[1] + "\t" + Maze[path[1].Item1, path[1].Item2]);
                if (path is null || path.Count < 2)
                {
                    transformations.Add(new(i, j));
                }
                else
                {
                    var next = path[1];
                    ghosts[k] = next;
                    transformations.Add(new(next.Item1, next.Item2));
                    Maze[i, j] = 0;
                    Maze[next.Item1, next.Item2] = -2;
                }
            }
            return transformations;
        }

        public void MoveGhosts(List<Vector2D<int>> positions, List<Vector2D<int>> nextPositions)
        {
            for(int i = 0; i < positions.Count; i++)
            {
                Maze[positions[i].X, positions[i].Y] = 0;
                Maze[nextPositions[i].X, nextPositions[i].Y] = -2;
            }
        }

        public List<Vector3D<float>> GetBonusPositions()
        {
            float offset = -25f;
            List<Vector3D<float>> transformations = new List<Vector3D<float>>();
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    //Console.WriteLine(Maze[i, j]);
                    if (Maze[i, j] == 2)
                    {
                        transformations.Add(new(offset + (float)i * 2f, 1f, offset + (float)j * 2f));
                    }
                }
            }
            return transformations;
        }

        public List<Matrix4X4<float>> GetGroundTransformations()
        {
            float offset = -25f;
            List<Matrix4X4<float>> transformations = new List<Matrix4X4<float>>();
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    transformations.Add(Matrix4X4.CreateTranslation(offset + (float)i * 2f, -0.5f, offset + (float)j * 2f));
                }
            }
            return transformations;
        }

        public List<Matrix4X4<float>> GetBonusTransformations()
        {
            float offset = -25f;
            List<Matrix4X4<float>> transformations = new List<Matrix4X4<float>>();
            for (int i = 1; i < rows-1; i++)
            {
                for (int j = 1; j < cols-1; j++)
                {
                    //float plusOffset = Maze[i, j - 1] == 0 ? 0.5f : 0.5f;
                    float plusOffset = 0.5f;
                    if (Maze[i, j] == 2)
                    {
                        transformations.Add(Matrix4X4.CreateTranslation(offset + (float)i * 2f + plusOffset, 1f, offset + (float)j * 2f + plusOffset));
                    }
                }
            }
            return transformations;
        }


        public Vector3D<float> GetPacmanPosition()
        {
            float offset = -25f;
            return new Vector3D<float>(offset + (float)pi * 2f, 1f, offset + (float)pj * 2f);
        }
    }
}
