using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using System.Security.Cryptography;


namespace Projekt
{
    internal class CameraDescriptor
    {
        public Vector3D<float> targetPosition = new Vector3D<float>(0.0f, 0.0f, 0.0f); // player position
        private float distanceFromTarget = 10.0f; // distance behind the target
        private float heightOffset = 5.0f; // vertical offset above the target

        private Vector3D<float> filterY = new Vector3D<float>(1.0f, 1.0f, 1.0f);
        public Vector3D<float> cameraPosition = new Vector3D<float>(3.0f, 10.0f, 0f);
        private Vector3D<float> cameraTarget = new Vector3D<float>(0.0f, 5f, 0.0f);
        public Vector3D<float> cameraFront = new Vector3D<float>(0.0f, 0.0f, -1.0f);
        private Vector3D<float> cameraDirection;
        public Vector3D<float> cameraRight;
        public Vector3D<float> cameraUp;

        private float lastX = 0.0f;
        private float lastY = 0.0f;
        private Boolean firstMove = true;

        private float pitch = 0.0f;
        private float yaw = -90.0f;


        public CameraDescriptor()
        {
            Vector3D<float> up = new Vector3D<float>(0.0f, 1.0f, 0.0f);
            cameraDirection = Vector3D.Normalize(cameraPosition - cameraTarget);
            cameraRight = Vector3D.Normalize(Vector3D.Cross(up, cameraDirection));
            //cameraUp = Vector3D.Cross(cameraDirection, cameraRight);
            cameraUp = up;
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

        private void UpdateCameraPosition()
        {
            float horizontalDistance = distanceFromTarget * float.Cos(float.DegreesToRadians(pitch));
            float verticalDistance = distanceFromTarget * float.Sin(float.DegreesToRadians(pitch));

            float offsetX = horizontalDistance * float.Sin(float.DegreesToRadians(yaw));
            float offsetZ = horizontalDistance * float.Cos(float.DegreesToRadians(yaw));

            cameraPosition.X = targetPosition.X - offsetX;
            cameraPosition.Y = targetPosition.Y + verticalDistance + heightOffset;
            cameraPosition.Z = targetPosition.Z - offsetZ;

            cameraFront = Vector3D.Normalize(targetPosition - cameraPosition);
            cameraRight = Vector3D.Normalize(Vector3D.Cross(cameraFront, new Vector3D<float>(0, 1, 0)));
            cameraUp = Vector3D.Cross(cameraRight, cameraFront);
        }


        public void Move(Key k, double dtime = 0.016)
        {
            float cameraSpeed = 10f;
            if (k == Key.W)
            {
                cameraPosition += filterY * cameraSpeed * (float)dtime * cameraFront;
            }
            if (k == Key.S)
            {
                cameraPosition -= filterY * cameraSpeed * (float)dtime * cameraFront;
            }
            if (k == Key.A)
            {
                cameraPosition -= filterY * cameraSpeed * (float)dtime * Vector3D.Normalize(Vector3D.Cross(cameraFront, cameraUp));
            }
            if (k == Key.D)
            {
                cameraPosition += filterY * cameraSpeed * (float)dtime * Vector3D.Normalize(Vector3D.Cross(cameraFront, cameraUp));
            }
            //Console.WriteLine(cameraPosition);
        }


        public void MouseMove(double xPos, double yPos)
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
            cameraFront = Vector3D.Normalize(direction);

        }
    }
}