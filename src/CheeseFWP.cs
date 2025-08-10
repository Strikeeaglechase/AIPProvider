using AIPLoader;
using Recorder;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using UnityGERunner;
using UnityGERunner.UnityApplication;

namespace AIPProvider
{
#if false
    class CheeseFWP : IAIPProvider
    {
        private OutboundState state;
        private InboundState output;
        private ActionList actions = new ActionList();

        public float rollResponse = 0.1f;
        public float minRollAcceleration = 3f;
        public float maxAoA = 30f;

        public PIDController pitchPID = new();
        public PIDController yawPID = new();
        public PIDController rollPID = new();

        public PID2Controller aoaPID = new();

        public PIDController throttlePID = new();

        private Vector3 targetVelocity = new Vector3(0, 0, 250);

        public override SetupActions Start(SetupInfo info)
        {
            pitchPID.p = TestingValue("p", 0.39f, -2, 2, 0.05f);
            pitchPID.i = TestingValue("i", 0, -2, 2, 0.05f);
            pitchPID.d = TestingValue("d", 1, -2, 2, 0.05f);

            return new SetupActions
            {
                hardpoints = new string[] { },
                name = "AI Client"
            };
        }

        private Vector3 InvTransDir(Vector3 dir)
        {
            return Quaternion.Inverse(state.kinematics.rotation) * dir;
        }

        private void Fly()
        {
            //Debug.DrawRay(transform.position, targetVelocity.normalized, Color.red);
            var position = state.kinematics.position.vec3;
            var velocity = state.kinematics.velocity.vec3;

            DebugShape(new DebugLine(1001) { start = position, end = position + targetVelocity, color = Color.red });

            output.throttle = throttlePID.Evaluate(targetVelocity.magnitude, 0f, Time.fixedDeltaTime);


            Vector3 accelerationDir = (targetVelocity - velocity) * rollResponse + Vector3.up * 9.81f;
            Vector3 rightVec = Vector3.Cross(velocity, accelerationDir).normalized;
            Vector3 upVec = Vector3.Cross(rightVec, velocity).normalized;

            Vector3 worldRightVec = Vector3.Cross(velocity, Vector3.up).normalized;
            Vector3 worldUpVec = Vector3.Cross(worldRightVec, velocity).normalized;


            float rollForce = Mathf.InverseLerp(0, 3, Vector3.ProjectOnPlane(accelerationDir, velocity).magnitude);
            var up = state.kinematics.rotation.quat * Vector3.up;
            output.pyr.z = rollPID.Evaluate(Vector3.Dot(up, Vector3.Slerp(up, rightVec, rollForce)), 0f, Time.fixedDeltaTime);


            Vector2 angleToTarget = new Vector2(Mathf.Atan2(Vector3.Dot(-worldRightVec, targetVelocity.normalized), Vector3.Dot(velocity.normalized, targetVelocity.normalized)),
                Mathf.Atan2(Vector3.Dot(worldUpVec, targetVelocity.normalized), Vector3.Dot(velocity.normalized, targetVelocity.normalized)));

            Vector2 aoaPidOutput = aoaPID.Evaluate(angleToTarget, Vector2.zero, Time.fixedDeltaTime);
            Vector3 steerVector = Quaternion.AngleAxis(Mathf.Clamp(aoaPidOutput.x, -1f, 1f) * maxAoA, worldUpVec)
                * Quaternion.AngleAxis(Mathf.Clamp(aoaPidOutput.y, -1f, 1f) * maxAoA, worldRightVec)
                * velocity.normalized;
            //Debug.DrawRay(transform.position, steerVector);
            DebugShape(new DebugLine(1002) { start = position, end = position + steerVector * 1000, color = Color.white });


            output.pyr.x = pitchPID.Evaluate(InvTransDir(steerVector).y, 0, Time.fixedDeltaTime);
            output.pyr.y = yawPID.Evaluate(InvTransDir(steerVector).x, 0, Time.fixedDeltaTime);

            Graph("pyr", output.pyr);
            Graph("throttle", output.throttle);
        }

        public override InboundState Update(OutboundState state)
        {
            this.state = state;
            this.output = new InboundState
            {
                pyr = new NetVector { x = 0, y = 0, z = 0 },
                throttle = 100,
            };

            Fly();

            output.events = actions.ToArray();
            actions.Clear();

            Graph("vel", state.kinematics.velocity);

            return output;
        }
    }
#endif
}
