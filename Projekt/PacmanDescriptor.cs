using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using System.Numerics;
using System.Security.Cryptography;


namespace Projekt
{
    internal class PacmanDescriptor
    {
        public Vector3D<float> targetPosition = new Vector3D<float>(0.0f, 0.0f, 0.0f); // player position
        private float distanceFromTarget = 10.0f; // distance behind the target
        private float heightOffset = 5.0f; // vertical offset above the target

        private Vector3D<float> filterY = new Vector3D<float>(1.0f, 0.0f, 1.0f);
        public Vector3D<float> cameraPosition = new Vector3D<float>(0.0f, 1.0f, 0f);
        private Vector3D<float> cameraTarget = new Vector3D<float>(0.0f, 0.0f, 0.0f);
        public Vector3D<float> cameraFront = new Vector3D<float>(0.0f, 0.0f, -1.0f);
        private Vector3D<float> cameraDirection;
        public Vector3D<float> cameraRight;
        public Vector3D<float> cameraUp;

        private float lastX = 0.0f;
        private float lastY = 0.0f;
        private Boolean firstMove = true;

        private float pitch = 0.0f;
        private float yaw = -90.0f;

        private float circleRadius = 1.1f;

        public PacmanDescriptor()
        {
            Vector3D<float> up = new Vector3D<float>(0.0f, 1.0f, 0.0f);
            cameraDirection = Vector3D.Normalize(cameraPosition - cameraTarget);
            cameraRight = Vector3D.Normalize(Vector3D.Cross(up, cameraDirection));
            //cameraUp = Vector3D.Cross(cameraDirection, cameraRight);
            cameraUp = up;
        }

        public void ResetCamera()
        {
            //cameraPosition = new Vector3D<float>(0.0f, 1.0f, 0f);
            cameraTarget = new Vector3D<float>(0.0f, 0.0f, 0.0f);
            cameraFront = new Vector3D<float>(0.0f, 0.0f, -1.0f);
        }

        public Vector3D<float> Front
        {
            get
            {
                return cameraFront;
            }
        }

        public Vector3D<float> Target
        {
            get
            {
                return cameraPosition + cameraFront;
                //return cameraPosition + cameraTarget;
            }
        }

        public Matrix4X4<float> View
        {
            get
            {
                return Matrix4X4.CreateLookAt(cameraPosition, Target, cameraUp);
            }
        }

        public Vector3D<float> Position
        {
            get
            {
                return cameraPosition;
            }
        }

        public Vector3D<float> CameraUp
        {
            get
            {
                return cameraUp;
            }
        }

        public float Yaw
        {
            get
            {
                return yaw;
            }
        }

        public bool CheckWallCollision(List<Vector2D<float>> wallPositions, Vector3D<float> pos)
        {
            foreach(var wall in wallPositions)
            {
                if(CircleSquareCollision(new Vector2D<float>(pos.X, pos.Z), circleRadius - 0.1f, wall, 2f))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool CircleSquareCollision(
        Vector2D<float> circleCenter, float circleRadius,
        Vector2D<float> wall, float squareSize)
        {

             //Find closest point on square to circle
            float closestX = Math.Clamp(circleCenter.X,
                                        wall.X,
                                        wall.X + squareSize);
            float closestY = Math.Clamp(circleCenter.Y,
                                        wall.Y - squareSize,
                                        wall.Y);
            float dx = circleCenter.X - closestX;
            float dy = circleCenter.Y - closestY;
            var valami = (dx * dx + dy * dy);
            return valami < (circleRadius * circleRadius);
        }

        public void Move(Key k, List<Vector2D<float>> wallPositions, List<Vector2D<float>> exitPositions, int bonusesLeft, double dtime = 0.016)
        {
            float cameraSpeed = 5f;
            if (k == Key.W)
            {
                var newPos = cameraPosition + filterY * cameraSpeed * (float)dtime * cameraFront;
                if (CheckWallCollision(wallPositions, newPos))
                    return;
                if (bonusesLeft > 0 && CheckWallCollision(exitPositions, newPos))
                    return;
                cameraPosition += filterY * cameraSpeed * (float)dtime * cameraFront;
            }
            if (k == Key.S)
            {
                var newPos = cameraPosition - filterY * cameraSpeed * (float)dtime * cameraFront;
                if (CheckWallCollision(wallPositions, newPos))
                    return;
                if (bonusesLeft > 0 && CheckWallCollision(exitPositions, newPos))
                    return;
                cameraPosition -= filterY * cameraSpeed * (float)dtime * cameraFront;
            }
            if (k == Key.A)
            {
                var newPos = cameraPosition - filterY * cameraSpeed * (float)dtime * Vector3D.Normalize(Vector3D.Cross(cameraFront, cameraUp));
                if (CheckWallCollision(wallPositions, newPos))
                    return;
                if (bonusesLeft > 0 && CheckWallCollision(exitPositions, newPos))
                    return;
                cameraPosition -= filterY * cameraSpeed * (float)dtime * Vector3D.Normalize(Vector3D.Cross(cameraFront, cameraUp));
            }
            if (k == Key.D)
            {
                var newPos = cameraPosition + filterY * cameraSpeed * (float)dtime * Vector3D.Normalize(Vector3D.Cross(cameraFront, cameraUp));
                if (CheckWallCollision(wallPositions, newPos))
                    return;
                if (bonusesLeft > 0 && CheckWallCollision(exitPositions, newPos))
                    return;
                cameraPosition += filterY * cameraSpeed * (float)dtime * Vector3D.Normalize(Vector3D.Cross(cameraFront, cameraUp));
            }
            //Console.WriteLine(cameraPosition);
        }



        public void MouseMove(double xPos, double yPos, int cameraMode = 1)
        {
            //Console.WriteLine(cameraFront);
            if (firstMove)
            {
                lastX = (float)xPos;
                lastY = (float)yPos;
                firstMove = false;
            }

            float xOffset = (float)xPos - lastX;
            float yOffset = lastY - (float)yPos;
            lastX = (float)xPos;
            lastY = (float)yPos;

            float sensitivity = 0.1f;
            xOffset *= sensitivity;
            yOffset *= sensitivity;

            yaw -= xOffset;
            pitch -= yOffset;

            if (pitch > 89.0f)
                pitch = 89.0f;
            if (pitch < -89.0f)
                pitch = -89.0f;

            Vector3D<float> direction = new Vector3D<float>();
            direction.X = float.Cos(float.DegreesToRadians(yaw)) * float.Cos(float.DegreesToRadians(pitch));
            direction.Y = float.Sin(float.DegreesToRadians(pitch));
            direction.Z = float.Sin(float.DegreesToRadians(yaw)) * float.Cos(float.DegreesToRadians(pitch));
            if (cameraMode == 1)
            {
                direction.Y = -1; // lock camera movement
            }
            cameraFront = Vector3D.Normalize(direction);

        }
    }
}

