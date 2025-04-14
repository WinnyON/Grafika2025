
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using System.Security.Cryptography;

/*namespace Szeminarium
{
    internal class CameraDescriptor
    {
        public double DistanceToOrigin { get; private set; } = 1;

        public double AngleToZYPlane { get; private set; } = 0;

        public double AngleToZXPlane { get; private set; } = 0;
        public float CameraTargetX { get; private set; } = 0;
        public float CameraTargetY { get; private set; } = 0;
        public float CameraTargetZ { get; private set; } = 0;

        const double DistanceScaleFactor = 1.01;

        const double AngleChangeStepSize = Math.PI / 180 * 0.2;
        const float CameraTargetChangeStepSize = 0.012f;
        private Vector3D<float> currentPosition = new Vector3D<float>(0f, 1f, 3f);

        /// <summary>
        /// Gets the position of the camera.
        /// </summary>
        public Vector3D<float> Position
        {
            get
            {
                //return GetPointFromAngles(DistanceToOrigin, AngleToZYPlane, AngleToZXPlane);
                return currentPosition;
            }
        }

        /// <summary>
        /// Gets the up vector of the camera.
        /// </summary>
        public Vector3D<float> UpVector
        {
            get
            {
                return Vector3D.Normalize(GetPointFromAngles(DistanceToOrigin, AngleToZYPlane, AngleToZXPlane + Math.PI / 2));
            }
        }

        /// <summary>
        /// Gets the target point of the camera view.
        /// </summary>
        public Vector3D<float> Target
        {
            get
            {
                // For the moment the camera is always pointed at the origin.
                //return Vector3D<float>.Zero;
                return new Vector3D<float>(CameraTargetX, CameraTargetY, CameraTargetZ);
            }
        }

        public void Move(Key k, double dtime = 0.016)
        {
            if( k == Key.Down)
            {
                currentPosition -= (float)dtime * CameraTargetChangeStepSize * Target;
            }
            if(k == Key.Up)
            {
                currentPosition += (float)dtime * CameraTargetChangeStepSize * Target;
            }
            if (k == Key.Left)
            {
                currentPosition -= (float)dtime * CameraTargetChangeStepSize * Vector3D.Normalize(Vector3D.Cross(Target, UpVector));
            }
            if (k == Key.Right)
            {
                currentPosition += (float)dtime * CameraTargetChangeStepSize * Vector3D.Normalize(Vector3D.Cross(Target, UpVector));
            }
            Console.WriteLine(currentPosition);
        }
        public void IncreaseTargetX()
        {
            CameraTargetX += CameraTargetChangeStepSize;
        }

        public void DecreaseTargetX()
        {
            CameraTargetX -= CameraTargetChangeStepSize;
        }

        public void IncreaseTargetY()
        {
            CameraTargetY += CameraTargetChangeStepSize;
        }

        public void DecreaseTargetY()
        {
            CameraTargetY -= CameraTargetChangeStepSize;
        }

        public void IncreaseTargetZ()
        {
            CameraTargetZ += CameraTargetChangeStepSize;
        }

        public void DecreaseTargetZ()
        {
            CameraTargetZ -= CameraTargetChangeStepSize;
        }

        public void SetZYAngle(double inc)
        {
            AngleToZYPlane += inc;
        }

        public void SetZXAngle(double inc)
        {
            AngleToZXPlane += inc;
        }

        public void IncreaseZXAngle()
        {
            AngleToZXPlane += AngleChangeStepSize;
        }

        public void DecreaseZXAngle()
        {
            AngleToZXPlane -= AngleChangeStepSize;
        }

        public void IncreaseZYAngle()
        {
            AngleToZYPlane += AngleChangeStepSize;
        }

        public void DecreaseZYAngle()
        {
            AngleToZYPlane -= AngleChangeStepSize;
        }

        public void IncreaseDistance()
        {
            DistanceToOrigin = DistanceToOrigin * DistanceScaleFactor;
        }

        public void DecreaseDistance()
        {
            DistanceToOrigin = DistanceToOrigin / DistanceScaleFactor;
        }

        private static Vector3D<float> GetPointFromAngles(double distanceToOrigin, double angleToMinZYPlane, double angleToMinZXPlane)
        {
            var x = distanceToOrigin * Math.Cos(angleToMinZXPlane) * Math.Sin(angleToMinZYPlane);
            var z = distanceToOrigin * Math.Cos(angleToMinZXPlane) * Math.Cos(angleToMinZYPlane);
            var y = distanceToOrigin * Math.Sin(angleToMinZXPlane);

            return new Vector3D<float>((float)x, (float)y, (float)z);
        }
    }
}
*/

namespace Szeminarium
{
    internal class CameraDescriptor
    {
        private Vector3D<float> cameraPosition = new Vector3D<float>(0.0f, 1.0f, 3.0f);
        private Vector3D<float> cameraTarget = new Vector3D<float>(0.0f, 0.0f, 0.0f);
        private Vector3D<float> cameraFront = new Vector3D<float>(0.0f, 0.0f, -1.0f);
        private Vector3D<float> cameraDirection;
        private Vector3D<float> cameraRight;
        private Vector3D<float> cameraUp;

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

        public Vector3D<float> Position
        {
            get
            {
                return cameraPosition;
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

        public void Move(Key k, double dtime = 0.016)
        {
            float cameraSpeed = 1.0f;
            if(k == Key.W)
            {
                cameraPosition += cameraSpeed * (float)dtime * cameraFront;
            }
            if (k == Key.S)
            {
                cameraPosition -= cameraSpeed * (float)dtime * cameraFront;
            }
            if (k == Key.A)
            {
                cameraPosition -= cameraSpeed * (float)dtime * Vector3D.Normalize(Vector3D.Cross(cameraFront, cameraUp)); 
            }
            if (k == Key.D)
            {
                cameraPosition += cameraSpeed * (float)dtime * Vector3D.Normalize(Vector3D.Cross(cameraFront, cameraUp));
            }
            Console.WriteLine(cameraPosition);
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