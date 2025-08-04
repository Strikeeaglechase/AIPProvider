using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityGERunner;

namespace AIPProvider
{
    public class PIDController
    {
        public float p = 1;
        public float i = 0;
        public float d = 1;

        float iValue = 0;
        public float iMin = -0.25f;
        public float iMax = 0.25f;

        private float previousError = 0;

        public float Evaluate(float target, float current, float deltaTime)
        {
            var error = target - current;
            var velocity = (previousError - error) / deltaTime;
            previousError = error;

            iValue += error * i * deltaTime;
            iValue = Mathf.Clamp(iValue, iMin, iMax);

            return error * p + -velocity * d + iValue;
        }
    }

    public class PID2Controller
    {
        public float p = 1;
        public float i = 0;
        public float d = 1;

        Vector2 iValue;
        public float iMax = 0.25f;
        private Vector2 previousError = Vector2.zero;

        public Vector2 Evaluate(Vector2 target, Vector2 current, float deltaTime)
        {
            var error = target - current;
            var velocity = (previousError - error) / deltaTime;
            previousError = error;

            iValue += error * i * deltaTime;
            if (iValue.magnitude > iMax) iValue = iValue.normalized * iMax;

            return error * p + -velocity * d + iValue;
        }
    }
}
