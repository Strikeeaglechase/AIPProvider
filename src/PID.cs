using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIPLoader
{
    class PID
    {
        public float p;
        public float i;
        public float d;

        private float prevError;
        public float integral;

        public PID(float p, float i, float d)
        {
            this.p = p;
            this.i = i;
            this.d = d;
        }

        public float Update(float error, float dt)
        {
            integral += error * dt;
            integral *= (1 - 0.3f * dt);

            var derivative = (error - prevError) / dt;
            prevError = error;

            return p * error + i * integral + d * derivative;
        }
    }
}
